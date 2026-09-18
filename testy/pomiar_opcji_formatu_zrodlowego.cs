using System;
using System.IO;
using System.Collections;
using System.Reflection;
class OriginOptionProbe {
static int pass,fail;
static void Check(bool ok,string name){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(ok)pass++;else fail++;}
[STAThread] static int Main(string[] args){
Assembly a=Assembly.LoadFrom(Path.GetFullPath(args[0]));Type settings=a.GetType("Ustawienia");
IEnumerable options=(IEnumerable)settings.GetMethod("Spis").Invoke(null,null);object found=null;int count=0;
foreach(object item in options){if((string)item.GetType().GetField("Klucz").GetValue(item)=="SaveImportedOriginalFormat"){found=item;count++;}}
Check(count==1,"original-format save option is present exactly once");
if(found==null)return 1;
Type option=found.GetType();Check((string)option.GetField("Rodzaj").GetValue(found)=="przelacznik","setting is a checkbox, not an INI-only switch");
Check((string)option.GetField("Domyslna").GetValue(found)=="N","factory default is OFF");
Check(((string)option.GetField("Etykieta").GetValue(found)).Contains("Control+S"),"label names the changed key");
string help=(string)option.GetField("Podpowiedz").GetValue(found);
Check(help.Contains("Markdown") && help.Contains("backup"),"help explains conversion and backups");
Type session=a.GetType("EdSharp.Sesja"), window=a.GetType("EdSharp.SesjaOkno");
FieldInfo sf=window.GetField("OriginalFormatFile"),sh=window.GetField("OriginalFormatHash");
Check(sf!=null && sh!=null,"session can retain source identity and fingerprint");if(sf==null||sh==null)return 1;
string dir=Path.Combine(Path.GetTempPath(),"EdSharpOriginOption-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
try {
session.GetMethod("Przygotuj").Invoke(null,new object[]{dir});
object w=Activator.CreateInstance(window);window.GetField("Plik").SetValue(w,"working.md");
string source=Path.Combine(dir,"Żółty dokument.docx"),hash=new string('a',64);sf.SetValue(w,source);sh.SetValue(w,hash);
Type listType=typeof(System.Collections.Generic.List<>).MakeGenericType(window);IList list=(IList)Activator.CreateInstance(listType);list.Add(w);
Check((bool)session.GetMethod("Zapisz").Invoke(null,new object[]{list}),"session written");
IList back=(IList)session.GetMethod("Czytaj").Invoke(null,null);
Check(back.Count==1 && (string)sf.GetValue(back[0])==source && (string)sh.GetValue(back[0])==hash,"source path and fingerprint roundtrip without truncation");
File.WriteAllText(Path.Combine(dir,"Sesja.ini"),"[Sesja]\nOkien=1\n[Okno1]\nPlik=legacy.md\n");
back=(IList)session.GetMethod("Czytaj").Invoke(null,null);
Check(back.Count==1 && (string)sf.GetValue(back[0])=="" && (string)sh.GetValue(back[0])=="","legacy sessions have no invented original association");
} finally {Directory.Delete(dir,true);}
Console.WriteLine("RESULT: "+pass+" PASS / "+fail+" FAIL");return fail==0?0:1;
}}
