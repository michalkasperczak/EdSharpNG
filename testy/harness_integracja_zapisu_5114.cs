using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
class SaveIntegration {
static int pass,fail;
static void Check(bool ok,string label){Console.WriteLine((ok?"PASS ":"FAIL ")+label);if(ok)pass++;else fail++;}
[STAThread] static int Main(string[] args){
Assembly asm=Assembly.LoadFrom(Path.GetFullPath(args[0]));Type app=asm.GetType("EdSharp.App"),frame=asm.GetType("EdSharp.MdiFrame");
string baseDir=Path.GetDirectoryName(Path.GetFullPath(args[0]));
string dir=Path.Combine(baseDir,"QA formats Żółty");Directory.CreateDirectory(dir);
foreach(string name in new string[]{"ProgramDir","DataDir"})app.GetField(name).SetValue(null,name=="ProgramDir"?baseDir:dir);
app.GetField("NetDir").SetValue(null,RuntimeEnvironment.GetRuntimeDirectory());app.GetField("TempFile").SetValue(null,Path.Combine(dir,"temp.txt"));
string source="# Żółty nagłówek\n\nZażółć gęślą jaźń i [link](https://example.org).\n\n- chleb\n- mleko\n";
string fixture=Path.Combine(dir,"source.md");File.WriteAllText(fixture,source,new UTF8Encoding(false));
string ini=Path.Combine(dir,"export.ini");File.Copy(args[1],ini,true);
app.GetField("IniFile").SetValue(null,ini);app.GetField("DefaultIniFile").SetValue(null,ini);
MethodInfo method=frame.GetMethod("KonwertujDoPliku",BindingFlags.Static|BindingFlags.NonPublic);
if(method==null){Console.WriteLine("BLOCKED missing integrated save method");return 2;}
foreach(string ext in new string[]{"docx","epub","html","rtf","pdf"}){
string path=Path.Combine(dir,"wynik Żółty raport."+ext);
object result=method.Invoke(null,new object[]{source,"md",path,fixture});
bool ok=(bool)result.GetType().GetField("Udane").GetValue(result);string reason=(string)result.GetType().GetField("Powod").GetValue(result);
Check(ok,"actual executable exports to path with spaces/Polish: "+ext+" "+reason);
Check(File.ReadAllText(fixture)==source,"source unchanged: "+ext);
}
string legacy="[Export]\r\nmd2docx=\"%ProgDir%\\Convert\\Pandoc\\pandoc.exe -f markdown_github -t docx -s \"%SourceLong%\" -o %Target%\"\r\n";
string legacyIni=Path.Combine(dir,"legacy.ini");File.WriteAllText(legacyIni,legacy,Encoding.ASCII);
app.GetField("IniFile").SetValue(null,legacyIni);
object l=method.Invoke(null,new object[]{source,"md",Path.Combine(dir,"legacy Żółty.docx"),fixture});
Check((bool)l.GetType().GetField("Udane").GetValue(l),"actual executable uses user's legacy INI through real Ini reader: "+l.GetType().GetField("Powod").GetValue(l));
Console.WriteLine("RESULT: "+pass+" PASS / "+fail+" FAIL");return fail==0?0:1;
}}
