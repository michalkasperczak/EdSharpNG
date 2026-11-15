using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

// Independent dictionary choice, never inferred from the spelling/UI language.
public static class SlownikiSynonimow {
public static string NormalizeMode(string mode) {
mode=(mode??"").Trim().Trim('"').ToLowerInvariant();
return mode=="en" || mode=="both"?mode:"pl";
}
public static string FormatEntry(string word,string pos,string senseId,int index,int total,string mode) {
string text=Synonimy.FormatEntry(word,pos,index,total);
if(NormalizeMode(mode)=="both")text+=", "+(EnglishId(senseId)?"English":"polski");
return text;
}
static bool EnglishId(string value) {return Regex.IsMatch(value,@"\Awn30:[nvar]:[0-9]{8}\z");}
static IEnumerable<string[]> Rows(string path,int columns) {
if(new FileInfo(path).Length>67108864)throw new InvalidDataException();
using(var reader=new StreamReader(path,new UTF8Encoding(false,true),false)) {
string line;
while((line=reader.ReadLine())!=null) {
if(line.Length==0 || line.StartsWith("#",StringComparison.Ordinal))continue;
var f=line.Split('\t');if(line.Length>65536 || f.Length!=columns)throw new InvalidDataException();
foreach(string field in f)foreach(char c in field)if(Char.IsControl(c))throw new InvalidDataException();
yield return f;
}
}
}
static string englishFolder="";
static Dictionary<string,string[]> details;
static string detailsError="";
public static string Error="";
public static string GetDetails(string senseId) {
if(!senseId.StartsWith("wn30:",StringComparison.Ordinal))return Synonimy.GetDetails(senseId);
if(details==null) {
details=new Dictionary<string,string[]>(StringComparer.Ordinal);
try {
foreach(string[] f in Rows(Path.Combine(englishFolder,"wordnet-en-details.txt"),3)) {
if(!EnglishId(f[0]))throw new InvalidDataException();
details.Add(f[0],new string[]{f[1],f[2]});
}
}catch(Exception){details.Clear();detailsError="Nie można wczytać angielskich szczegółów znaczeń. Lista synonimów nadal działa.";}
}
if(detailsError.Length>0)return detailsError;
string[] value;
if(!details.TryGetValue(senseId,out value))return "Brak definicji i przykładu dla tego znaczenia w dołączonym słowniku.";
string text=value[0].Length>0?"Definicja:\r\n"+value[0]:"Brak definicji w dołączonym słowniku.";
if(value[1].Length>0)text+="\r\n\r\nPrzykład:\r\n"+value[1];
return text+"\r\n\r\nŹródło: Princeton WordNet 3.0 (English).";
}
public static List<Synonimy.Grupa> Find(string folder,string word,string mode) {
var result=new List<Synonimy.Grupa>();
Error="";
englishFolder=folder;details=null;detailsError="";
mode=NormalizeMode(mode);
if(!Synonimy.IsSingleWord(word))return result;
if(mode=="pl" || mode=="both") {
if(Synonimy.Load(Path.Combine(folder,"plwordnet-synonyms.txt"))) {result.AddRange(Synonimy.Find(word));Error=Synonimy.FormsError;}
else Error="Nie można wczytać polskiego słownika synonimów.";
if(mode=="pl")return result;
}
string key=word.ToLowerInvariant();
var english=new List<Synonimy.Grupa>();
try {
var bases=new HashSet<string>(StringComparer.Ordinal);
string exceptionPath=Path.Combine(folder,"wordnet-en-exceptions.txt");
try {
foreach(string[] f in Rows(exceptionPath,3)) {
if(!Synonimy.IsSingleWord(f[0]) || !Synonimy.IsSingleWord(f[2]) || f[1].Length!=1 || "nvar".IndexOf(f[1],StringComparison.Ordinal)<0)throw new InvalidDataException();
if(String.Equals(f[0],key,StringComparison.OrdinalIgnoreCase))bases.Add(f[2].ToLowerInvariant()+"\t"+f[1]);
}
}catch(Exception){bases.Clear();Error+=(Error.Length>0?"\r\n":"")+"Nie można odczytać angielskich form nieregularnych. Wyszukiwanie haseł podstawowych nadal działa.";}
var inflected=new List<Synonimy.Grupa>();
foreach(string[] fields in Rows(Path.Combine(folder,"wordnet-en-synonyms.txt"),4)) {
if(!Synonimy.IsSingleWord(fields[0]) || !EnglishId(fields[2]) || fields[1]!=fields[2].Substring(5,1))throw new InvalidDataException();
bool literal=String.Equals(fields[0],key,StringComparison.OrdinalIgnoreCase);
if(!literal && !bases.Contains(fields[0].ToLowerInvariant()+"\t"+fields[1]))continue;
var values=new List<string>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach(string value in fields[3].Split('|'))if(!String.Equals(value,key,StringComparison.OrdinalIgnoreCase) && seen.Add(value))values.Add(value);
if(values.Count>0)(literal?english:inflected).Add(new Synonimy.Grupa(fields[1],fields[2],values));
}
if(english.Count==0)english=inflected;
result.AddRange(english);
}catch(Exception){Error+=(Error.Length>0?"\r\n":"")+"Nie można wczytać angielskiego słownika synonimów.";}
return result;
}
}
