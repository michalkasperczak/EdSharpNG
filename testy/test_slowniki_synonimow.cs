using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
public static class TestSlownikiSynonimow {
static int failures;
static bool threw;
static void Check(bool ok,string label) { Console.WriteLine((ok?"PASS ":"FAIL ")+label);if(!ok)failures++; }
static Type Provider { get { return typeof(Synonimy).Assembly.GetType("SlownikiSynonimow"); } }
static List<Synonimy.Grupa> Find(string folder,string word,string mode) {
threw=false;if(Provider==null)return new List<Synonimy.Grupa>();
try{return (List<Synonimy.Grupa>)Provider.GetMethod("Find").Invoke(null,new object[]{folder,word,mode});}catch(TargetInvocationException){threw=true;return new List<Synonimy.Grupa>();}
}
static string Error() {var f=Provider==null?null:Provider.GetField("Error");return f==null?"":(string)f.GetValue(null);}
static string Details(string id) {
var method=Provider==null?null:Provider.GetMethod("GetDetails");try{return method==null?"[missing details]":(string)method.Invoke(null,new object[]{id});}catch(TargetInvocationException){return "[exception]";}
}
public static int Main() {
string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
try {
File.WriteAllText(Path.Combine(folder,"wordnet-en-synonyms.txt"),"bank\tn\twn30:n:00000001\tdepository|bank\nbank\tn\twn30:n:00000002\triverbank\n",new UTF8Encoding(false));
var found=Find(folder,"BANK","en");
Check(found.Count==2 && found[0].Synonyms.Count==1 && found[0].Synonyms[0]=="depository" && found[1].Synonyms[0]=="riverbank","English meanings distinct, case insensitive, excludes self");
File.WriteAllText(Path.Combine(folder,"plwordnet-synonyms.txt"),"list\tn\t"+new string('a',32)+"\tpismo\n",new UTF8Encoding(false));
File.AppendAllText(Path.Combine(folder,"wordnet-en-synonyms.txt"),"list\tn\twn30:n:00000003\tlisting\n",new UTF8Encoding(false));
var pl=Find(folder,"list","pl");var en=Find(folder,"list","en");var both=Find(folder,"list","both");
Check(pl.Count==1 && pl[0].Synonyms[0]=="pismo" && en.Count==1 && en[0].Synonyms[0]=="listing" && both.Count==2 && both[0].Synonyms[0]=="pismo" && both[1].Synonyms[0]=="listing","explicit PL EN both isolates identical word and keeps PL first");
foreach(string mode in new string[]{"EN"," en ","\"EN\""}) {
var normalized=Find(folder,"list",mode);
Check(normalized.Count==1 && normalized[0].Synonyms[0]=="listing","runtime honors the same legacy English spellings as Settings: "+mode);
}
foreach(string mode in new string[]{"BOTH"," both ","\"Both\""})Check(Find(folder,"list",mode).Count==2,"legacy both spellings preserve both languages: "+mode);
File.WriteAllText(Path.Combine(folder,"wordnet-en-details.txt"),"wn30:n:00000001\ta financial institution\tI went to the bank.\nwn30:n:00000002\tsloping land beside a river\t\n",new UTF8Encoding(false));
File.WriteAllText(Path.Combine(folder,"plwordnet-details.txt"),new string('a',32)+"\tkorespondencja\t\n",new UTF8Encoding(false));
string financial=Details("wn30:n:00000001"),river=Details("wn30:n:00000002"),polish=Details(new string('a',32));
Check(financial.Contains("financial institution") && financial.Contains("I went to the bank.") && financial.Contains("WordNet 3.0") && !financial.Contains("sloping land") && river.Contains("sloping land") && !river.Contains("financial institution") && polish.Contains("korespondencja") && !polish.Contains("financial institution"),"details dispatch exact language and sense, no lemma joins");
File.Delete(Path.Combine(folder,"wordnet-en-details.txt"));Find(folder,"bank","en");
Check(Details("wn30:n:00000001").Contains("Lista synonimów nadal działa") && Find(folder,"bank","en").Count==2,"missing English details nonfatal and explicit");
foreach(string bad in new string[]{"wn30:n:00000001\tdef\texample\textra\n","not-a-sense\tdef\t\n","wn30:n:00000001\tde\u0001f\t\n","wn30:n:00000001\tfirst\t\nwn30:n:00000001\tsecond\t\n"}) {
File.WriteAllText(Path.Combine(folder,"wordnet-en-details.txt"),bad,new UTF8Encoding(false));Find(folder,"bank","en");
Check(Details("wn30:n:00000001").Contains("Lista synonimów nadal działa"),"invalid English details rejected as whole optional resource");
}
string index=Path.Combine(folder,"wordnet-en-synonyms.txt"),goodIndex=File.ReadAllText(index);
File.Delete(index);both=Find(folder,"list","both");
Check(both.Count==1 && both[0].Synonyms[0]=="pismo" && Error().Length>0,"missing EN warns but does not disable PL in both mode");
foreach(string bad in new string[]{"bank\tn\twn30:n:00000004\tunsafe\nother\tn\tnot-id\twrong\n","bank\tv\twn30:n:00000001\twrong\n","bank\tn\twn30:n:00000001\twrong\textra\n"}) {
File.WriteAllText(index,bad,new UTF8Encoding(false));
Check(Find(folder,"bank","en").Count==0 && Error().Length>0,"malformed English base cannot return partial or wrong POS meanings");
}
File.WriteAllText(index,goodIndex,new UTF8Encoding(false));
File.AppendAllText(index,"go\tv\twn30:v:00000005\ttravel\ngo\tn\twn30:n:00000006\tboard game\n",new UTF8Encoding(false));
File.WriteAllText(Path.Combine(folder,"wordnet-en-exceptions.txt"),"went\tv\tgo\n",new UTF8Encoding(false));
var irregular=Find(folder,"went","en");
Check(irregular.Count==1 && irregular[0].PartOfSpeech=="v" && irregular[0].Synonyms[0]=="travel" && Find(folder,"goed","en").Count==0,"only source-attested irregular form resolves, exact POS, output lemma not guessed inflection");
var invalidMode=Find(folder,"list","unknown");Check(invalidMode.Count==1 && invalidMode[0].Synonyms[0]=="pismo","invalid mode defaults PL not interface or automatic English");
foreach(string input in new string[]{null,"","bank money"," bank","bank\n"})Check(Find(folder,input,"en").Count==0 && !threw && Error().Length==0,"invalid query explicit empty result not an exception or dictionary failure");
File.WriteAllText(Path.Combine(folder,"wordnet-en-exceptions.txt"),"bad-exception\n",new UTF8Encoding(false));
Check(Find(folder,"bank","en").Count==2 && Error().Length>0,"corrupt optional exceptions warn but preserve literal English lookup");
File.Delete(Path.Combine(folder,"wordnet-en-exceptions.txt"));
Check(Find(folder,"bank","en").Count==2 && Error().Contains("form nieregularnych"),"missing optional exceptions warn but preserve literal English lookup");
Check(Find(folder,"went","en").Count==0 && Error().Contains("form nieregularnych"),"missing optional exceptions explicitly explain missing irregular input");
File.AppendAllText(index,"NASA\tn\twn30:n:00000007\tNational Aeronautics and Space Administration\n",new UTF8Encoding(false));
var acronym=Find(folder,"nasa","en");Check(acronym.Count==1 && acronym[0].Synonyms[0]=="National Aeronautics and Space Administration","source capitalized lemma searchable case-insensitively, phrase capitalization preserved");
var format=typeof(SlownikiSynonimow).GetMethod("FormatEntry");Check(format!=null,"language label formatter exists");
if(format!=null) {
string enLabel=(string)format.Invoke(null,new object[]{"shore","n","wn30:n:00000001",2,3,"both"});
string plLabel=(string)format.Invoke(null,new object[]{"brzeg","n","aa",1,3,"both"});
string single=(string)format.Invoke(null,new object[]{"shore","n","wn30:n:00000001",2,3,"en"});
Check(enLabel.StartsWith("shore, ") && enLabel.Contains("English") && enLabel.Contains("2 z 3") && plLabel.Contains("polski") && single==Synonimy.FormatEntry("shore","n",2,3),"both labels languages without leaking IDs or changing word-first single-mode display");
}
} finally {Directory.Delete(folder,true);}
Console.WriteLine("FAILURES="+failures);return failures==0?0:1;
}
}
