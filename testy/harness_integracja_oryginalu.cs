using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;
class OriginIntegration {
static Assembly a; static Type app,ft,ct; static Form frame; static int pass,fail;
[DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string cls,string name);
[DllImport("user32.dll")] static extern IntPtr GetDlgItem(IntPtr h,int id);
[DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
[DllImport("user32.dll")] static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
static bool ClickYes(Control c){Button b=c as Button;if(b!=null && b.Text.Replace("&","")=="Yes"){b.PerformClick();return true;}foreach(Control x in c.Controls)if(ClickYes(x))return true;return false;}
static void AcceptRecovery(){
foreach(Form f in Application.OpenForms) if(f!=frame && f.Text=="Restore Session"){ClickYes(f);return;}
IntPtr h=FindWindow("#32770","Restore Session");uint pid;GetWindowThreadProcessId(h,out pid);
if(h!=IntPtr.Zero && pid==(uint)System.Diagnostics.Process.GetCurrentProcess().Id)PostMessage(GetDlgItem(h,6),0xF5,IntPtr.Zero,IntPtr.Zero);
}
static void Check(bool b,string label){Console.WriteLine((b?"PASS ":"FAIL ")+label);if(b)pass++;else fail++;}
static void Set(string k,object v){app.GetField(k).SetValue(null,v);}
static object Call(object obj,string name,params object[] args){return obj.GetType().InvokeMember(name,BindingFlags.InvokeMethod|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,obj,args);}
static object Static(Type t,string n,params object[] args){return t.InvokeMember(n,BindingFlags.InvokeMethod|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static,null,null,args);}
static object Child(){return ft.GetProperty("Child").GetValue(frame,null);}
static RichTextBox Box(){return (RichTextBox)ct.GetField("RTB").GetValue(Child());}
static string FileName(){return (string)ct.GetProperty("File").GetValue(Child(),null);}
static string Hash(string path){return (string)Static(a.GetType("EdSharp.Util"),"FileSha256",path);}
static void CloseAll(){foreach(Form f in frame.MdiChildren){((RichTextBox)ct.GetField("RTB").GetValue(f)).Modified=false;f.Close();}Application.DoEvents();}
[STAThread] static int Main(string[] args){try{
a=Assembly.LoadFrom(Path.GetFullPath(args[0]));app=a.GetType("EdSharp.App");ft=a.GetType("EdSharp.MdiFrame");ct=a.GetType("EdSharp.MdiChild");
string baseDir=Path.GetDirectoryName(Path.GetFullPath(args[0])),dir=Path.Combine(baseDir,"QA original Żółty");Directory.CreateDirectory(dir);
string ini=Path.Combine(dir,"test.ini");File.Copy(args[1],ini,true);
Set("ProgramDir",baseDir);Set("DataDir",dir);Set("ProgramName","EdSharpNG");Set("NetDir",RuntimeEnvironment.GetRuntimeDirectory());
Set("TempFile",Path.Combine(dir,"temp.txt"));Set("DefaultIniFile",ini);Set("IniFile",ini);Set("HotkeyIniFile",Path.Combine(baseDir,"Hotkeys.ini"));Set("SpeechLog",Path.Combine(dir,"speech.log"));Set("ErrorLog",Path.Combine(dir,"error.log"));
Set("BomDictionary",Static(a.GetType("EdSharp.Util"),"GetBomDictionary"));Static(app,"SetConfigurationValues");Static(app,"WriteOption","OpenPrevious","N");Static(app,"WriteOption","RestoreSession","Y");Static(app,"WriteOption","SaveImportedOriginalFormat","Y");
Static(a.GetType("EdSharp.Sesja"),"Przygotuj",dir);
frame=(Form)Activator.CreateInstance(ft);Set("Frame",frame);frame.Show();Application.DoEvents();
FieldInfo linkField=ct.GetField("OriginalDocument");
string seed="# Żółty nagłówek\n\nPierwsza treść i [odnośnik](https://example.org).\n\n- chleb\n- mleko\n";
foreach(string ext in new string[]{"docx","epub","html","rtf"}){
string original=Path.Combine(dir,"Artykuł."+ext);
object w=Static(ft,"KonwertujDoPliku",seed,"md",original,"source.md");Check((bool)w.GetType().GetField("Udane").GetValue(w),"fixture real "+ext);
string before=Hash(original);Call(frame,"OpenOrActivateWindow",original,2,"","",ext+"2md");Application.DoEvents();
Check(Box().Text.Contains("Żółty"),"actual import "+ext);
Check(linkField!=null && linkField.GetValue(Child())!=null,"import retains original identity "+ext);
if(linkField==null){CloseAll();continue;}
Box().AppendText("\n\nNowa treść: ZAŻÓŁĆ "+ext+".\n");Box().Modified=true;
// Invoke actual Ctrl+S menu event, not a standalone conversion helper.
((ToolStripMenuItem)ft.GetField("menuFileSave",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(frame)).PerformClick();
Check(Hash(original)!=before,"Ctrl+S updates original "+ext);Check(!Box().Modified,"successful original save clears dirty flag "+ext);
Check(Path.GetExtension(FileName())==".md","buffer identity stays Markdown, never binary "+ext);
string[] backups=Directory.GetFiles(Path.Combine(dir,".edsharp-backups"),"*."+ext);bool oldFound=false;foreach(string b in backups)if(Hash(b)==before)oldFound=true;
Check(oldFound,"backup contains previous original "+ext);
IList session=(IList)Call(frame,"ZbierzSesje",false);object sw=session[session.Count-1];Type st=sw.GetType();
Check((string)st.GetField("OriginalFormatFile").GetValue(sw)==original,"frame session retains association "+ext);
Static(a.GetType("EdSharp.Sesja"),"Zapisz",session);CloseAll();
Check((int)Call(frame,"PrzywrocSesje")==1,"clean imported session restores without recovery copy "+ext);
Check(Box().Text.Contains("ZAŻÓŁĆ"),"reopened original contains actual edited content "+ext);
Check(linkField.GetValue(Child())!=null,"restored document remains linked "+ext);
int windows=frame.MdiChildren.Length;object active=Child();Call(frame,"OpenOrActivateWindow",original,2,"","",ext+"2md");
Check(frame.MdiChildren.Length==windows && Object.ReferenceEquals(active,Child()),"opening linked original again activates same buffer "+ext);
string current=Hash(original);
Box().AppendText("\nPending second save\n");Box().Modified=true;
object[] repeated=new object[]{Child(),""};
Check((bool)ft.GetMethod("TrySaveOriginalDocument").Invoke(frame,repeated) && (string)repeated[1]=="" && !Box().Modified,"repeat save uses refreshed fingerprint "+ext);
current=Hash(original);
Box().Modified=true;
MethodInfo snippet=ft.GetMethod("SaveSnippetFile",BindingFlags.Static|BindingFlags.NonPublic);
Check(snippet!=null,"snippet has a copy-only path for imported documents "+ext);
if(snippet!=null){object link=linkField.GetValue(Child());string identity=FileName();string sp=Path.Combine(dir,"snippet-"+ext+".md");snippet.Invoke(null,new object[]{Child(),Box().Text,sp,true});Check(Object.ReferenceEquals(link,linkField.GetValue(Child())) && FileName()==identity && Box().Modified && File.ReadAllText(sp)==Box().Text,"snippet preserves source link, identity and unsaved flag "+ext);}
MethodInfo csv=ft.GetMethod("CanEditCsvDocument",BindingFlags.Static|BindingFlags.NonPublic);
Check(csv!=null && !(bool)csv.Invoke(null,new object[]{Child()}),"CSV editor rejects imported unsaved Markdown buffer "+ext);
MethodInfo fullExport=ft.GetMethod("KonwertujDokumentDoPliku",BindingFlags.Static|BindingFlags.NonPublic);
object same=fullExport.Invoke(null,new object[]{Child(),Box().Text,"md",original});
Check((bool)same.GetType().GetField("Udane").GetValue(same) && !Box().Modified,"full Save As to linked original clears dirty state with option ON "+ext);
// External valid document change, retaining the original buffer on failure.
Static(ft,"KonwertujDoPliku",seed+"\nExternal content\n","md",original,"source.md");
string external=Hash(original);Box().AppendText("\nUnsaved local\n");Box().Modified=true;
object[] conflict=new object[]{Child(),""};
Check((bool)ft.GetMethod("TrySaveOriginalDocument").Invoke(frame,conflict) && ((string)conflict[1]).Length>0,"external edit stops original route, no fallback "+ext);
Check(Hash(original)==external && Box().Modified,"failed original save preserves external bytes and unsaved flag "+ext);
MethodInfo safeExport=ft.GetMethod("KonwertujDokumentDoPliku",BindingFlags.Static|BindingFlags.NonPublic);
Check(safeExport!=null,"Save As and Export share original safety route "+ext);
if(safeExport!=null){object exResult=safeExport.Invoke(null,new object[]{Child(),Box().Text,"md",original});Check(!(bool)exResult.GetType().GetField("Udane").GetValue(exResult) && Hash(original)==external,"explicit export cannot bypass external-change guard "+ext);}
// Restore a recoverable session: source has changed, so the old hash must survive.
IList recovery=(IList)Call(frame,"ZbierzSesje",true);Static(a.GetType("EdSharp.Sesja"),"Zapisz",recovery);CloseAll();
Timer confirm=new Timer();confirm.Interval=150;confirm.Tick+=delegate{AcceptRecovery();};confirm.Start();
int restored=(int)Call(frame,"PrzywrocSesje");confirm.Stop();confirm.Dispose();
Check(restored==1 && Box().Text.Contains("Unsaved local") && Box().Modified,"crash recovery keeps local edits "+ext);
conflict=new object[]{Child(),""};ft.GetMethod("TrySaveOriginalDocument").Invoke(frame,conflict);
Check(((string)conflict[1]).Length>0 && Hash(original)==external,"crash recovery does not adopt changed source fingerprint "+ext);
current=Hash(original);Static(app,"WriteOption","SaveImportedOriginalFormat","N");
object[] tryArgs=new object[]{Child(),""};bool applicable=(bool)ft.GetMethod("TrySaveOriginalDocument").Invoke(frame,tryArgs);
Check(!applicable && Hash(original)==current,"toggle OFF immediately bypasses original save "+ext);
Static(app,"WriteOption","SaveImportedOriginalFormat","Y");
// SaveTextOrRtfFile is the ordinary Save As path; explicit MD save detaches.
Call(Child(),"SaveTextOrRtfFile",Path.Combine(dir,"Detached-"+ext+".md"));
Check(linkField.GetValue(Child())==null,"explicit Markdown save detaches original "+ext);
CloseAll();}
// Two-window recovery with an unreadable original must never reuse the first window.
string normal=Path.Combine(dir,"First.md");File.WriteAllText(normal,"FIRST ORIGINAL");
Call(frame,"OpenOrActivateWindow",normal,0);Box().Text="FIRST LOCAL";Box().Modified=true;
string lockedSource=Path.Combine(dir,"Locked.docx");Static(ft,"KonwertujDoPliku",seed,"md",lockedSource,"source.md");
Call(frame,"OpenOrActivateWindow",lockedSource,2,"","","docx2md");Box().Text="SECOND LOCAL";Box().Modified=true;
IList both=(IList)Call(frame,"ZbierzSesje",true);Static(a.GetType("EdSharp.Sesja"),"Zapisz",both);CloseAll();
Timer answers=new Timer();answers.Interval=100;answers.Tick+=delegate{AcceptRecovery();};answers.Start();
using(FileStream locked=new FileStream(lockedSource,FileMode.Open,FileAccess.Read,FileShare.None)){
int n=(int)Call(frame,"PrzywrocSesje");
bool first=false,second=false;
foreach(Form f in frame.MdiChildren){string txt=((RichTextBox)ct.GetField("RTB").GetValue(f)).Text;object link=linkField.GetValue(f);if(txt=="FIRST LOCAL" && link==null)first=true;if(txt=="SECOND LOCAL" && link!=null)second=true;}
Check(n==2 && frame.MdiChildren.Length==2,"locked original restores into separate document windows");
Check(first && second,"each recovered text belongs to its own document and source link");
}
answers.Stop();CloseAll();
// A recovery copy which cannot be read must not be consumed, even after clean exit.
object rec=both[1];Type recordType=rec.GetType();string recoveryPath=(string)recordType.GetField("Odzysk").GetValue(rec);
File.WriteAllText(recoveryPath,"SECOND LOCAL");both.RemoveAt(0);Static(a.GetType("EdSharp.Sesja"),"Zapisz",both);
answers.Start();
using(FileStream locked=new FileStream(recoveryPath,FileMode.Open,FileAccess.Read,FileShare.Delete)) {
int n=(int)Call(frame,"PrzywrocSesje");
Check(n==0 && frame.MdiChildren.Length==0,"unreadable recovery is not counted as restored");
Check(File.Exists(recoveryPath),"failed recovery copy is not deleted");
}
answers.Stop();answers.Dispose();
IList retained=(IList)Call(frame,"ZbierzSesje",false);
Check(retained.Count==1 && (string)recordType.GetField("Odzysk").GetValue(retained[0])==recoveryPath,"failed recovery remains in next session snapshot");
ft.GetField("bSesjaWlaczona",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(frame,true);
Call(frame,"ExitApp");
Check(File.Exists(recoveryPath),"normal exit also preserves unconsumed recovery bytes");
IList afterExit=(IList)Static(a.GetType("EdSharp.Sesja"),"Czytaj");
Check(afterExit.Count==1 && (string)recordType.GetField("Odzysk").GetValue(afterExit[0])==recoveryPath,"normal exit retains failed recovery in session on disk");
Console.WriteLine("RESULT: "+pass+" PASS / "+fail+" FAIL");return fail==0?0:1;
}catch(Exception e){Console.WriteLine(e);return 2;}finally{if(frame!=null){CloseAll();frame.Dispose();}}}
}
