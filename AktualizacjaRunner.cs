using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace EdSharp {
internal interface IUpdateRunnerHost {
 int RunInstaller(string installerPath, string arguments);
 void Restart(string applicationPath, string checkpointName);
 void Error(string text);
}

internal sealed class WindowsUpdateRunnerHost : IUpdateRunnerHost {
 const int MaxErrorLogBytes=8192, MaxErrorChars=512;
 readonly string errorLog;
 public WindowsUpdateRunnerHost(string folder) { errorLog=Path.Combine(Path.GetFullPath(folder),"runner-error.log"); }
 public int RunInstaller(string installerPath, string arguments) {
  using (Process p=Process.Start(new ProcessStartInfo { FileName=installerPath, Arguments=arguments, UseShellExecute=true, Verb="runas" })) {
   if (p==null) throw new InvalidOperationException("Instalator nie został uruchomiony.");
   p.WaitForExit(); return p.ExitCode;
  }
 }
 public void Restart(string applicationPath, string checkpointName) {
  Process.Start(new ProcessStartInfo { FileName=applicationPath, Arguments="--resume-update "+UpdateStore.Quote(checkpointName), UseShellExecute=true });
 }
 public void Error(string text) {
  try {
   if (File.Exists(errorLog) && new FileInfo(errorLog).Length>=MaxErrorLogBytes) return;
   string safe=(text??"").Replace('\r',' ').Replace('\n',' ');
   if (safe.Length>MaxErrorChars) safe=safe.Substring(0,MaxErrorChars);
   byte[] line=new UTF8Encoding(false).GetBytes(DateTime.UtcNow.ToString("o")+" "+safe+Environment.NewLine);
   using (FileStream f=new FileStream(errorLog,FileMode.Append,FileAccess.Write,FileShare.Read,4096,FileOptions.WriteThrough)) { f.Write(line,0,line.Length); f.Flush(true); }
  } catch {}
 }
}

internal static class UpdateRunner {
 internal static int Run(string folder, IUpdateRunnerHost host) {
  if (host==null) throw new ArgumentNullException("host");
  UpdateSession session;
  try { session=UpdateStore.Read(folder); }
  catch (Exception e) { host.Error("Nieprawidłowy checkpoint aktualizacji: "+e.Message); return 2; }
  Process parent=null;
  EventWaitHandle cancellation=null;
  try {
   try {
    parent=Process.GetProcessById(session.ParentPid);
    if (parent.HasExited || parent.StartTime.ToUniversalTime().Ticks!=session.ParentStartTicksUtc) { host.Error("Proces macierzysty aktualizacji nie pasuje do checkpointu."); return 3; }
   } catch (Exception e) { host.Error("Nie można otworzyć procesu macierzystego aktualizacji: "+e.Message); return 3; }
   try {
    if (parent.HasExited) { host.Error("Proces macierzysty zakończył się przed gotowością helpera."); return 3; }
    try { cancellation=EventWaitHandle.OpenExisting(UpdateStore.CancellationName(folder)); }
    catch (WaitHandleCannotBeOpenedException) { }
    CreateReady(folder);
    Stopwatch clock=Stopwatch.StartNew();
    while (!parent.WaitForExit(150)) {
     if (cancellation!=null && cancellation.WaitOne(0)) return 0;
     string liveState;
     try { liveState=UpdateStore.ReadStateLightweight(folder); }
     catch (Exception e) { host.Error("Nie można odczytać stanu aktualizacji: "+e.Message); return 5; }
     if (liveState=="cancelled" && !parent.HasExited) return 0;
     if (clock.ElapsedMilliseconds>=600000) { host.Error("Przekroczono czas oczekiwania na zamknięcie EdSharpa."); return 4; }
    }
    if (cancellation!=null && cancellation.WaitOne(0)) return 0;
   } catch (Exception e) { host.Error("Helper aktualizacji nie może bezpiecznie czekać: "+e.Message); return 4; }
  } finally { if (cancellation!=null) cancellation.Dispose(); if (parent!=null) parent.Dispose(); }
  string state;
  try { UpdateStore.Read(folder); state=UpdateStore.ReadState(folder); }
  catch (Exception e) { host.Error("Nie można odczytać stanu aktualizacji: "+e.Message); return 5; }
  if (state=="prepared" || state=="cancelled") return RestartOnly(session, folder, host);
  if (state!="committed") { host.Error("Checkpoint aktualizacji jest już odtworzony albo ma niedozwolony stan."); return 6; }
  UpdateResult result=new UpdateResult();
  try {
   if (!File.Exists(session.InstallerPath) || !String.Equals(UpdateStore.Sha256(session.InstallerPath),session.InstallerSha256,StringComparison.Ordinal)) throw new InvalidDataException("Instalator nie przeszedł ponownej kontroli SHA-256.");
   string arguments="/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /NOCLOSEAPPLICATIONS /DIR="+UpdateStore.Quote(Path.GetDirectoryName(session.ApplicationPath))+" /LOG="+UpdateStore.Quote(Path.Combine(Path.GetFullPath(folder),"installer.log"));
   int code=host.RunInstaller(session.InstallerPath,arguments);
   result.InstallerExitCode=code; result.Success=(code==0); result.Message=code==0 ? "Instalator zakończył się powodzeniem." : "Instalator zakończył się kodem "+code+".";
  } catch (Win32Exception e) {
   result.InstallerExitCode=e.NativeErrorCode==1223 ? 1223 : -1; result.Success=false; result.Message=e.NativeErrorCode==1223 ? "Użytkownik anulował monit UAC." : "Nie uruchomiono instalatora: "+e.Message;
  } catch (Exception e) { result.InstallerExitCode=-1; result.Success=false; result.Message="Nie uruchomiono instalatora: "+e.Message; }
  try { UpdateStore.WriteResult(folder,result); } catch (Exception e) { host.Error("Nie można zapisać wyniku instalatora: "+e.Message); }
  return RestartOnly(session,folder,host);
 }

 static int RestartOnly(UpdateSession session, string folder, IUpdateRunnerHost host) {
  try { host.Restart(session.ApplicationPath,new DirectoryInfo(Path.GetFullPath(folder)).Name); return 0; }
  catch (Exception e) { host.Error("Nie można wznowić EdSharpa: "+e.Message); return 8; }
 }
 static void CreateReady(string folder) {
  string path=Path.Combine(Path.GetFullPath(folder),"ready");
  using (FileStream f=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,1,FileOptions.WriteThrough)) { f.WriteByte(1); f.Flush(true); }
 }
}

internal static class AktualizacjaRunner {
 [STAThread]
 static int Main(string[] args) {
  if (args==null || args.Length!=1 || String.IsNullOrEmpty(args[0]) || !Path.IsPathRooted(args[0]) || !String.Equals(Path.GetFullPath(args[0]),args[0],StringComparison.OrdinalIgnoreCase)) return 64;
  WindowsUpdateRunnerHost host=new WindowsUpdateRunnerHost(args[0]);
  try { return UpdateRunner.Run(args[0],host); }
  catch (Exception e) { try { host.Error("Nieobsłużony błąd helpera: "+e.Message); } catch {} return 70; }
 }
}
}
