using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace EdSharp {
[DataContract]
public sealed class UpdateDocument {
 [DataMember] public string File { get; set; }
 [DataMember] public string Title { get; set; }
 [DataMember] public string ImportedFrom { get; set; }
 [DataMember] public string OriginalPath { get; set; }
 [DataMember] public string OriginalFingerprint { get; set; }
 [DataMember] public string Text { get; set; }
 [DataMember] public string Rtf { get; set; }
 [DataMember] public string Bookmarks { get; set; }
 [DataMember] public string FileLineBreakKind { get; set; }
 [DataMember] public int SelectionStart { get; set; }
 [DataMember] public int SelectionLength { get; set; }
 [DataMember] public int EncodingCodePage { get; set; }
 [DataMember] public bool Modified { get; set; }
 [DataMember] public bool IsRichTextDocument { get; set; }
 [DataMember] public bool IsUnixLineBreak { get; set; }
 [DataMember] public bool EncodingHasBom { get; set; }
 [DataMember] public bool ReadOnly { get; set; }
 [DataMember] public bool MarkdownReview { get; set; }
 [DataMember] public bool MarkdownNoSync { get; set; }
 [DataMember] public int ViewSelectionStart { get; set; }
 [DataMember] public int ViewSelectionLength { get; set; }
 [DataMember] public int WindowState { get; set; }
 [DataMember] public long FileTimeTicks { get; set; }
 [DataMember] public int FileTimeKind { get; set; }
 [DataMember] public bool FileTimeChecked { get; set; }
 public UpdateDocument() { File=""; Title=""; ImportedFrom=""; OriginalPath=""; OriginalFingerprint=""; Text=""; Rtf=""; Bookmarks=""; FileLineBreakKind=""; }
}

[DataContract]
public sealed class UpdateSession {
 [DataMember] public int Schema { get; set; }
 [DataMember] public string InstallerPath { get; set; }
 [DataMember] public string InstallerSha256 { get; set; }
 [DataMember] public string ApplicationPath { get; set; }
 [DataMember] public string TargetVersion { get; set; }
 [DataMember] public int ParentPid { get; set; }
 [DataMember] public long ParentStartTicksUtc { get; set; }
 [DataMember] public int ActiveDocument { get; set; }
 [DataMember] public List<UpdateDocument> Documents { get; set; }
 public UpdateSession() { Schema=1; InstallerPath=""; InstallerSha256=""; ApplicationPath=""; TargetVersion=""; ActiveDocument=-1; Documents=new List<UpdateDocument>(); }
}

[DataContract]
public sealed class UpdateResult {
 [DataMember] public bool Success { get; set; }
 [DataMember] public int InstallerExitCode { get; set; }
 [DataMember] public string Message { get; set; }
 public UpdateResult() { Message=""; }
}

public static class UpdateStore {
 const string Checkpoint="checkpoint.json", Checksum="checkpoint.sha256", State="state.txt", Result="result.json";
 static readonly Encoding Utf8=new UTF8Encoding(false, true);

 public static string Create(string dataDir, UpdateSession s) {
  if (String.IsNullOrEmpty(dataDir)) throw new InvalidDataException("Brak katalogu danych aktualizacji.");
  ValidateSession(s, true);
  string root=Path.Combine(Path.GetFullPath(dataDir), "Aktualizacja");
  Directory.CreateDirectory(root);
  string folder=null;
  for (int i=0;i<16;i++) { string candidate=Path.Combine(root, Guid.NewGuid().ToString("N")); try { Directory.CreateDirectory(candidate); if (Directory.Exists(candidate)) { folder=candidate; break; } } catch (IOException) {} }
  if (folder==null) throw new IOException("Nie można utworzyć katalogu checkpointu aktualizacji.");
  byte[] json;
  try { json=Serialize(s); WriteNew(Path.Combine(folder, Checkpoint), json); WriteNew(Path.Combine(folder, Checksum), Utf8.GetBytes(Sha256Bytes(json))); WriteNew(Path.Combine(folder, State), Utf8.GetBytes("prepared")); Read(folder); if (ReadState(folder)!="prepared") throw new InvalidDataException("Niezweryfikowany stan checkpointu."); return folder; }
  catch { throw; }
 }

 public static UpdateSession Read(string folder) {
  string full=ValidFolder(folder);
  byte[] json=File.ReadAllBytes(Path.Combine(full, Checkpoint));
  string declared=ReadStrictSha(Path.Combine(full, Checksum));
  if (!String.Equals(Sha256Bytes(json), declared, StringComparison.Ordinal)) throw new InvalidDataException("Suma checkpointu nie zgadza się.");
  UpdateSession session;
  try { session=(UpdateSession)new DataContractJsonSerializer(typeof(UpdateSession)).ReadObject(new MemoryStream(json, false)); }
  catch (Exception e) { throw new InvalidDataException("Checkpoint nie jest poprawnym JSON-em.",e); }
  ValidateSession(session, false);
  return session;
 }

 public static string ReadState(string folder) {
  Read(folder);
  return ReadStateLightweight(folder);
 }

 internal static string ReadStateLightweight(string folder) {
  string value=ReadTextExact(Path.Combine(ValidFolder(folder),State));
  if (value!="prepared" && value!="committed" && value!="cancelled" && value!="restored") throw new InvalidDataException("Niepoprawny stan aktualizacji.");
  return value;
 }

 public static void SetState(string folder, string state) {
  if (state!="prepared" && state!="committed" && state!="cancelled" && state!="restored") throw new InvalidDataException("Niepoprawny stan aktualizacji.");
  string current=ReadState(folder);
  bool allowed=(current=="prepared" && (state=="committed" || state=="cancelled" || state=="restored")) || (current=="cancelled" && state=="restored") || (current=="committed" && state=="restored");
  if (!allowed) throw new InvalidOperationException("Niedozwolone przejście stanu aktualizacji.");
  AtomicWrite(Path.Combine(ValidFolder(folder),State), Utf8.GetBytes(state));
  if (ReadState(folder)!=state) throw new IOException("Nie można potwierdzić zapisu stanu aktualizacji.");
 }

 public static UpdateResult ReadResult(string folder) {
  Read(folder); string path=Path.Combine(ValidFolder(folder),Result); if (!File.Exists(path)) return null;
  try { UpdateResult r=(UpdateResult)new DataContractJsonSerializer(typeof(UpdateResult)).ReadObject(new MemoryStream(File.ReadAllBytes(path),false)); if (r==null || r.Message==null) throw new InvalidDataException("Niepoprawny wynik aktualizacji."); return r; }
  catch (Exception e) { throw new InvalidDataException("Niepoprawny wynik aktualizacji.",e); }
 }

 public static void WriteResult(string folder, UpdateResult result) {
  Read(folder); if (result==null || result.Message==null) throw new InvalidDataException("Niepoprawny wynik aktualizacji.");
  AtomicWrite(Path.Combine(ValidFolder(folder),Result), Serialize(result));
  if (ReadResult(folder)==null) throw new IOException("Nie można potwierdzić wyniku aktualizacji.");
 }

 public static void StartHelper(string folder, string helperSourcePath) {
  Read(folder); string full=ValidFolder(folder);
  if (String.IsNullOrEmpty(helperSourcePath) || !Path.IsPathRooted(helperSourcePath) || !File.Exists(helperSourcePath)) throw new FileNotFoundException("Brak samodzielnego helpera aktualizacji.",helperSourcePath);
  string sourceHash=Sha256(helperSourcePath); if (new FileInfo(helperSourcePath).Length==0) throw new InvalidDataException("Helper aktualizacji jest pusty.");
  string copied=Path.Combine(full,"AktualizacjaRunner.exe"); if (File.Exists(copied)) throw new IOException("Helper jest już skopiowany do checkpointu.");
  File.Copy(helperSourcePath,copied,false);
  if (!String.Equals(sourceHash,Sha256(copied),StringComparison.Ordinal)) throw new InvalidDataException("Kopia helpera nie przeszła kontroli SHA-256.");
  using (EventWaitHandle cancellation=new EventWaitHandle(false,EventResetMode.ManualReset,CancellationName(full)))
  using (Process p=Process.Start(new ProcessStartInfo { FileName=copied, Arguments=Quote(full), UseShellExecute=false, CreateNoWindow=true, WindowStyle=ProcessWindowStyle.Hidden })) {
  if (p==null) throw new InvalidOperationException("Nie uruchomiono helpera aktualizacji.");
  string ready=Path.Combine(full,"ready"); Stopwatch clock=Stopwatch.StartNew();
  while (clock.ElapsedMilliseconds<5000) { if (File.Exists(ready)) return; if (p.HasExited) break; Thread.Sleep(50); }
  CancelPrepared(full);
  cancellation.Set();
  throw new TimeoutException("Helper aktualizacji nie potwierdził gotowości.");
  }
 }

 internal static string CancellationName(string folder) { return "Local\\EdSharpUpdateCancel_"+new DirectoryInfo(ValidFolder(folder)).Name; }
 public static void CancelHelper(string folder) {
  try { using (EventWaitHandle cancellation=EventWaitHandle.OpenExisting(CancellationName(folder))) cancellation.Set(); }
  catch (WaitHandleCannotBeOpenedException) { }
 }

 internal static string Sha256(string path) { using (FileStream f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) using (SHA256 h=SHA256.Create()) return Hex(h.ComputeHash(f)); }
 internal static string Quote(string value) {
  StringBuilder quoted=new StringBuilder("\""); int slashes=0;
  foreach(char c in value) {
   if(c=='\\') { slashes++; continue; }
   quoted.Append('\\',c=='\"' ? slashes*2+1 : slashes); slashes=0; quoted.Append(c);
  }
  quoted.Append('\\',slashes*2); quoted.Append('\"'); return quoted.ToString();
 }
 internal static string ValidFolder(string folder) { if (String.IsNullOrEmpty(folder) || !Path.IsPathRooted(folder) || !Directory.Exists(folder)) throw new DirectoryNotFoundException("Brak katalogu checkpointu."); return Path.GetFullPath(folder); }
 internal static void ValidateSession(UpdateSession s, bool filesMustExist) {
  if (s==null || s.Schema!=1 || s.Documents==null || s.InstallerPath==null || s.InstallerSha256==null || s.ApplicationPath==null || s.TargetVersion==null) throw new InvalidDataException("Niepoprawny checkpoint aktualizacji.");
  if (!Path.IsPathRooted(s.InstallerPath) || !Path.IsPathRooted(s.ApplicationPath) || s.TargetVersion.Length==0 || s.ParentPid<=0 || s.ParentStartTicksUtc<=0 || s.ParentStartTicksUtc>DateTime.UtcNow.Ticks) throw new InvalidDataException("Niepoprawne metadane checkpointu.");
  if (!IsStrictSha(s.InstallerSha256) || s.ActiveDocument < -1 || (s.Documents.Count==0 && s.ActiveDocument!=-1) || (s.Documents.Count>0 && (s.ActiveDocument<0 || s.ActiveDocument>=s.Documents.Count))) throw new InvalidDataException("Niepoprawne dane checkpointu.");
  if (filesMustExist && (!File.Exists(s.InstallerPath) || !File.Exists(s.ApplicationPath) || !String.Equals(Sha256(s.InstallerPath),s.InstallerSha256,StringComparison.Ordinal))) throw new InvalidDataException("Niepoprawny instalator lub program checkpointu.");
  foreach (UpdateDocument d in s.Documents) { if (d==null || d.File==null || d.Title==null || d.ImportedFrom==null || d.OriginalPath==null || d.OriginalFingerprint==null || d.Text==null || d.Rtf==null || d.Bookmarks==null || d.FileLineBreakKind==null || d.SelectionStart<0 || d.SelectionLength<0 || d.ViewSelectionStart<0 || d.ViewSelectionLength<0 || d.EncodingCodePage<0 || d.WindowState<0 || d.WindowState>2) throw new InvalidDataException("Niepoprawny dokument checkpointu."); }
 }
 static bool IsStrictSha(string value) { if (value==null || value.Length!=64) return false; for (int i=0;i<value.Length;i++) if (!((value[i]>='0'&&value[i]<='9')||(value[i]>='a'&&value[i]<='f'))) return false; return true; }
 static void CancelPrepared(string folder) { try { if (ReadState(folder)=="prepared") SetState(folder,"cancelled"); } catch {} }
 static byte[] Serialize(object value) { try { using (MemoryStream m=new MemoryStream()) { new DataContractJsonSerializer(value.GetType()).WriteObject(m,value); return m.ToArray(); } } catch (Exception e) { throw new InvalidDataException("Nie można zapisać checkpointu.",e); } }
 static void WriteNew(string path, byte[] data) { using (FileStream f=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) { f.Write(data,0,data.Length); f.Flush(true); } if (!Equal(File.ReadAllBytes(path),data)) throw new IOException("Nie można potwierdzić zapisu checkpointu."); }
 static void AtomicWrite(string path, byte[] data) { string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp"; WriteNew(temp,data); try { if (File.Exists(path)) File.Replace(temp,path,null,true); else File.Move(temp,path); } finally { if (File.Exists(temp)) File.Delete(temp); } if (!Equal(File.ReadAllBytes(path),data)) throw new IOException("Nie można potwierdzić atomowego zapisu."); }
 static string ReadStrictSha(string path) { string value=ReadTextExact(path); if (!IsStrictSha(value)) throw new InvalidDataException("Niepoprawna suma checkpointu."); return value; }
 static string ReadTextExact(string path) { return Utf8.GetString(File.ReadAllBytes(path)); }
 static string Sha256Bytes(byte[] data) { using (SHA256 h=SHA256.Create()) return Hex(h.ComputeHash(data)); }
 static string Hex(byte[] bytes) { StringBuilder b=new StringBuilder(bytes.Length*2); foreach(byte x in bytes) b.Append(x.ToString("x2")); return b.ToString(); }
 public static string FindInstallerSha256(string notes, string assetUrl, string assetName) {
  if(String.IsNullOrEmpty(notes) || String.IsNullOrEmpty(assetUrl) || String.IsNullOrEmpty(assetName)) return "";
  Uri uri;
  if(!Uri.TryCreate(assetUrl,UriKind.Absolute,out uri) || !String.Equals(Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath)),assetName,StringComparison.Ordinal)) return "";
  string[] lines=notes.Replace("\r","").Split('\n');
  int entries=0;
  string result="";
  for(int i=0;i<lines.Length;i++) {
   bool exact=false;
   foreach(Match entry in Regex.Matches(lines[i],"`([^`]+)`")) if(String.Equals(entry.Groups[1].Value,assetName,StringComparison.Ordinal)) { exact=true; break; }
   if(!exact) continue;
   entries++;
   MatchCollection matches=Regex.Matches(lines[i],"\\b([0-9a-fA-F]{64})\\b");
   if(entries!=1 || matches.Count!=1) return "";
   result=matches[0].Groups[1].Value.ToLowerInvariant();
  }
  return entries==1 ? result : "";
 }
 static bool Equal(byte[] a,byte[] b) { if (a.Length!=b.Length)return false; for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false; return true; }
}

[DataContract]
public sealed class AutomaticUpdatePackage {
 [DataMember] public string InstallerPath { get; set; }
 [DataMember] public string InstallerSha256 { get; set; }
 [DataMember] public string TargetVersion { get; set; }
 [DataMember] public long VerifiedUtcTicks { get; set; }
 [DataMember] public string State { get; set; }
 public AutomaticUpdatePackage() { InstallerPath=""; InstallerSha256=""; TargetVersion=""; State=""; }
}

// Cache is deliberately separate from a one-off update checkpoint.  A ready
// package has already passed the release SHA-256 check; it is taken exactly
// once before any document opens, so a declined UAC prompt or failed installer
// cannot cause a restart loop.
public static class AutomaticUpdateStore {
 public static readonly TimeSpan MaximumAge=TimeSpan.FromDays(1);
 static readonly Encoding Utf8=new UTF8Encoding(false,true);
 static string Folder(string dataDir) { if(String.IsNullOrEmpty(dataDir)) throw new InvalidDataException("Brak katalogu danych automatycznej aktualizacji."); return Path.Combine(Path.GetFullPath(dataDir),"Aktualizacja","automatyczna"); }
 static string Marker(string dataDir) { return Path.Combine(Folder(dataDir),"pakiet.json"); }
 static string CachedInstaller(string dataDir) { return Path.Combine(Folder(dataDir),"EdSharpNG_Setup.exe"); }

 public static bool SaveVerified(string dataDir,string installer,string expectedSha256,string targetVersion,DateTime verifiedUtc) {
  try {
   if(!File.Exists(installer) || !StrictSha(expectedSha256) || !StrictVersion(targetVersion) || verifiedUtc.Kind!=DateTimeKind.Utc || verifiedUtc.Ticks<=0 || verifiedUtc> DateTime.UtcNow.AddMinutes(5)) return false;
   if(!String.Equals(Hash(installer),expectedSha256,StringComparison.OrdinalIgnoreCase)) return false;
   AutomaticUpdatePackage old=Read(dataDir);
   if(old!=null && old.State=="attempted" && String.Equals(old.TargetVersion,targetVersion,StringComparison.Ordinal)) return false;
   string folder=Folder(dataDir); Directory.CreateDirectory(folder);
   string destination=CachedInstaller(dataDir), temporary=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
   try {
    File.Copy(installer,temporary,false);
    if(!String.Equals(Hash(temporary),expectedSha256,StringComparison.OrdinalIgnoreCase)) return false;
    if(File.Exists(destination)) File.Replace(temporary,destination,null,true); else File.Move(temporary,destination);
   }
   finally { if(File.Exists(temporary)) File.Delete(temporary); }
   AutomaticUpdatePackage package=new AutomaticUpdatePackage { InstallerPath=destination, InstallerSha256=expectedSha256.ToLowerInvariant(), TargetVersion=targetVersion, VerifiedUtcTicks=verifiedUtc.Ticks, State="ready" };
   Write(dataDir,package);
   return IsSame(Read(dataDir),package);
  }
  catch { return false; }
 }

 public static bool CanPrepare(string dataDir,string targetVersion) {
  try {
   if(!StrictVersion(targetVersion)) return false;
   AutomaticUpdatePackage old=Read(dataDir);
   if(old==null || !String.Equals(old.TargetVersion,targetVersion,StringComparison.Ordinal)) return true;
   // Invalid or expired cache entries were never handed off and can be refreshed.
   if(old.State=="attempted") return false;
   return old.State!="ready" || !Valid(old,DateTime.UtcNow);
  }
  catch { return false; }
 }

 public static bool TryTakeReady(string dataDir,string currentVersion,DateTime nowUtc,out AutomaticUpdatePackage package) {
  package=null;
  try {
   AutomaticUpdatePackage stored=Read(dataDir);
   if(stored==null || stored.State!="ready") return false;
   if(nowUtc.Kind!=DateTimeKind.Utc || !StrictVersion(currentVersion) || !Valid(stored,nowUtc) || CompareVersions(stored.TargetVersion,currentVersion)<=0) { Reject(dataDir,stored); return false; }
   stored.State="attempted";
   Write(dataDir,stored);
   AutomaticUpdatePackage confirmed=Read(dataDir);
   if(confirmed==null || confirmed.State!="attempted" || !Valid(confirmed,nowUtc)) return false;
   package=confirmed;
   return true;
  }
  catch { return false; }
 }

 static bool Valid(AutomaticUpdatePackage package,DateTime nowUtc) {
  if(package==null || !StrictSha(package.InstallerSha256) || !StrictVersion(package.TargetVersion) || String.IsNullOrEmpty(package.InstallerPath) || !Path.IsPathRooted(package.InstallerPath) || package.VerifiedUtcTicks<=0) return false;
  DateTime verified; try { verified=new DateTime(package.VerifiedUtcTicks,DateTimeKind.Utc); } catch { return false; }
  if(verified>nowUtc.AddMinutes(5) || nowUtc-verified>MaximumAge || !File.Exists(package.InstallerPath)) return false;
  return String.Equals(Hash(package.InstallerPath),package.InstallerSha256,StringComparison.OrdinalIgnoreCase);
 }

 static void Reject(string dataDir,AutomaticUpdatePackage package) { try { if(package==null) return; package.State="rejected"; Write(dataDir,package); } catch {} }
 static AutomaticUpdatePackage Read(string dataDir) {
  try {
   string path=Marker(dataDir); if(!File.Exists(path)) return null;
   return (AutomaticUpdatePackage)new DataContractJsonSerializer(typeof(AutomaticUpdatePackage)).ReadObject(new MemoryStream(File.ReadAllBytes(path),false));
  }
  catch { return null; }
 }
 static void Write(string dataDir,AutomaticUpdatePackage package) {
  string folder=Folder(dataDir); Directory.CreateDirectory(folder); byte[] bytes;
  using(MemoryStream stream=new MemoryStream()) { new DataContractJsonSerializer(typeof(AutomaticUpdatePackage)).WriteObject(stream,package); bytes=stream.ToArray(); }
  string path=Marker(dataDir), temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try { using(FileStream stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) { stream.Write(bytes,0,bytes.Length); stream.Flush(true); } if(File.Exists(path)) File.Replace(temporary,path,null,true); else File.Move(temporary,path); }
  finally { if(File.Exists(temporary)) File.Delete(temporary); }
 }
 static bool IsSame(AutomaticUpdatePackage a,AutomaticUpdatePackage b) { return a!=null && b!=null && a.State==b.State && a.TargetVersion==b.TargetVersion && a.InstallerSha256==b.InstallerSha256 && a.VerifiedUtcTicks==b.VerifiedUtcTicks && a.InstallerPath==b.InstallerPath; }
 static bool StrictSha(string value) { if(value==null || value.Length!=64) return false; for(int i=0;i<value.Length;i++) if(!((value[i]>='0'&&value[i]<='9')||(value[i]>='a'&&value[i]<='f')||(value[i]>='A'&&value[i]<='F'))) return false; return true; }
 static bool StrictVersion(string value) { if(String.IsNullOrEmpty(value) || !Regex.IsMatch(value,"^[0-9]+(\\.[0-9]+){1,3}$")) return false; return true; }
 static int CompareVersions(string left,string right) { string[] a=left.Split('.'),b=right.Split('.'); int n=Math.Max(a.Length,b.Length); for(int i=0;i<n;i++) { int x=i<a.Length?Int32.Parse(a[i]):0,y=i<b.Length?Int32.Parse(b[i]):0; if(x!=y) return x<y?-1:1; } return 0; }
 static string Hash(string path) { using(FileStream stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) using(SHA256 hash=SHA256.Create()) { StringBuilder result=new StringBuilder(); foreach(byte value in hash.ComputeHash(stream)) result.Append(value.ToString("x2")); return result.ToString(); } }
}
}
