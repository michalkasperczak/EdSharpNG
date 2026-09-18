using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using EdSharp;
class SaveSafety {
static int pass, fail;
static string dir=@"C:\EdSharpSaveTest\hardening";
static string target;
static void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(ok)pass++;else fail++;}
static string Expand(string cmd,string src,string dst){target=dst;return cmd.Replace("%SourceLong%",src).Replace("%Target%",dst);}
static int Run(string command){
string exe,args;command=command.Trim();if(command.StartsWith("\"")){int e=command.IndexOf('"',1);exe=command.Substring(1,e-1);args=command.Substring(e+1).Trim();}else{int e=command.IndexOf(' ');exe=command.Substring(0,e);args=command.Substring(e+1);}
ProcessStartInfo p=new ProcessStartInfo(exe,args);p.UseShellExecute=false;p.CreateNoWindow=true;
using(Process process=Process.Start(p)){process.WaitForExit();return process.ExitCode;}}
static int Main(){Directory.CreateDirectory(dir);
string old=Path.Combine(dir,"existing.docx"); File.WriteAllText(old,"ORIGINAL");
Func<string,string> config=delegate(string key){return "fake.exe";};
Func<string,string> missing=delegate(string cmd){return "";};
WynikZapisu w=ZapisFormatow.Konwertuj("# document","md",old,config,Expand,delegate(string cmd){File.Copy(@"C:\EdSharpSaveTest\praca_harness\nowy.docx",target,true);return 3;},missing);
Check(!w.Udane,"nonzero exit rejects even a nonempty valid document"); Check(File.ReadAllText(old)=="ORIGINAL","failed process leaves destination intact");
File.WriteAllText(old,"ORIGINAL");
w=ZapisFormatow.Konwertuj("# document","md",old,config,Expand,delegate(string cmd){File.WriteAllText(target,"truncated document");return 0;},missing);
Check(!w.Udane,"invalid DOCX content is rejected");Check(File.ReadAllText(old)=="ORIGINAL","invalid output leaves destination intact");
string legacy="\"C:\\EdSharpSaveTest\\Convert\\Pandoc\\pandoc.exe -f markdown_github -t html -s \"%SourceLong%\" -o %Target%\"";
w=ZapisFormatow.Konwertuj("# Żółty nagłówek\n\nZażółć gęślą jaźń.","md",Path.Combine(dir,"legacy.html"),delegate(string key){return legacy;},Expand,Run,missing);
Check(w.Udane,"real user's legacy quoted Pandoc export command works");
if(w.Udane)Check(File.ReadAllText(w.PlikWynikowy).Contains("Zażółć gęślą jaźń"),"actual Polish letters survive conversion");
string modern="\"C:\\Program  Files\\Pandoc\\pandoc.exe\" \"a  b.md\" --reference-doc \"missing.docx\" -o \"a  b.docx\"";
string without=ZapisFormatow.UsunBrakujacyWzorzec(modern,delegate(string path){return false;});
Check(without.Contains("Program  Files") && without.Contains("a  b.docx"),"missing reference fix preserves spaces inside paths");
Check(Directory.GetFiles(dir,"*.edsharp-*").Length==0,"temporary conversion files cleaned");
Console.WriteLine("RESULT: "+pass+" PASS / "+fail+" FAIL");return fail==0?0:1;
}}
