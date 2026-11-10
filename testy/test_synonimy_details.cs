using System;
using System.IO;
using System.Text;
using System.Reflection;
public static class TestSynonimyDetails {
static int failures;
static void Check(bool ok, string label) { Console.WriteLine((ok ? "PASS " : "FAIL ") + label); if(!ok) failures++; }
static string Get(string id) {
MethodInfo m=typeof(Synonimy).GetMethod("GetDetails");
return m==null ? "[missing GetDetails]" : (string)m.Invoke(null,new object[]{id});
}
public static int Main() {
string root=Path.Combine(Path.GetTempPath(), "eds-details-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try {
string id=new string('a',32), other=new string('b',32);
string index=Path.Combine(root,"plwordnet-synonyms.txt"), details=Path.Combine(root,"plwordnet-details.txt");
File.WriteAllText(index,"dom\tn\t"+id+"\tchałupa\ndom\tn\t"+other+"\trodzina\n",new UTF8Encoding(false));
File.WriteAllText(details,id+"\tbudynek mieszkalny\tTo jest mój dom.\n"+other+"\trodzina mieszkająca razem\t\n",new UTF8Encoding(false));
Check(Synonimy.Load(index),"base loads");
string text=Get(id);
Check(text.Contains("budynek mieszkalny") && text.Contains("To jest mój dom.") && text.Contains("plWordNet 5.0"),"source definition and example");
Check(!text.Contains("rodzina mieszkająca"),"homonym details do not mix");
Check(Get(other).Contains("rodzina mieszkająca") && !Get(other).Contains("To jest mój"),"other sense isolated");
Check(Get("omw-2.0:n123").Contains("Brak definicji"),"legacy no fabricated definition");
Check(Get(new string('c',32)).Contains("Brak definicji"),"missing meaning explicit");
Check(Synonimy.Find("dom").Count==2,"details do not alter synonyms");
string sub=Path.Combine(root,"missing");Directory.CreateDirectory(sub);File.Copy(index,Path.Combine(sub,"base.txt"));Synonimy.Load(Path.Combine(sub,"base.txt"));
Check(Get(id).Contains("Brak pliku"),"missing optional file explicit");
Check(Synonimy.Find("dom").Count==2,"missing optional file does not disable synonyms");
sub=Path.Combine(root,"bad");Directory.CreateDirectory(sub);File.Copy(index,Path.Combine(sub,"base.txt"));
File.WriteAllBytes(Path.Combine(sub,"plwordnet-details.txt"),new byte[]{0xff,0xfe,0xfd});Synonimy.Load(Path.Combine(sub,"base.txt"));
Check(Get(id).Contains("Nie można wczytać"),"bad UTF8 explicit");
Check(Synonimy.Find("dom").Count==2,"corrupt optional file does not disable synonyms");
sub=Path.Combine(root,"duplicate");Directory.CreateDirectory(sub);File.Copy(index,Path.Combine(sub,"base.txt"));
File.WriteAllText(Path.Combine(sub,"plwordnet-details.txt"),id+"\tfirst\t\n"+id+"\tsecond\t\n",new UTF8Encoding(false));Synonimy.Load(Path.Combine(sub,"base.txt"));
Check(Get(id).Contains("Nie można wczytać"),"conflicting duplicate rejected");
} finally { Directory.Delete(root,true); }
Console.WriteLine("FAILURES="+failures);return failures==0?0:1;
}
}
