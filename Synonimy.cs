// Synonimy.cs - lokalny indeks polskich synonimow dla EdSharpNG.
// Format pliku v1 (UTF-8): lemma<TAB>part of speech<TAB>sense id<TAB>synonym|synonym
// Dane zrodlowe plWordNet sa przetwarzane podczas budowania; aplikacja nie
// wymaga bazy SQL, Internetu, Pythona ani dodatkowej biblioteki w czasie pracy.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class Synonimy {

public class Grupa {
public string PartOfSpeech;
public string SenseId;
public List<string> Synonyms;

public Grupa(string sPartOfSpeech, string sSenseId, List<string> lSynonyms) {
PartOfSpeech = sPartOfSpeech;
SenseId = sSenseId;
Synonyms = lSynonyms;
} // Grupa constructor

} // Grupa class

static Dictionary<string, List<Grupa>> hashGroups = new Dictionary<string, List<Grupa>>(StringComparer.Ordinal);
static string sFormsPath = "";
static string sDetailsPath = "";
static Dictionary<string, string[]> dDetails;
static string sDetailsError = "";
public static string GetDetails(string sSenseId) {
const string sMissing = "Brak definicji i przykładu dla tego znaczenia w dołączonym słowniku.";
if (string.IsNullOrEmpty(sSenseId) || sSenseId.StartsWith("omw-2.0:", StringComparison.Ordinal)) return sMissing;
if (dDetails == null) {
Dictionary<string, string[]> data = new Dictionary<string, string[]>(StringComparer.Ordinal);
try {
if (!File.Exists(sDetailsPath)) sDetailsError = "Brak pliku szczegółów znaczeń. Lista synonimów nadal działa.";
else {
if (new FileInfo(sDetailsPath).Length > 67108864) throw new InvalidDataException();
using (StreamReader reader = new StreamReader(sDetailsPath, new UTF8Encoding(false, true), false)) {
string line;
while ((line = reader.ReadLine()) != null) {
if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
string[] fields = line.Split('\t');
Guid id;
if (line.Length > 65536 || fields.Length != 3 || fields[0].Length != 32 || !Guid.TryParseExact(fields[0], "N", out id) || data.ContainsKey(fields[0])) throw new InvalidDataException();
foreach (string field in fields) foreach (char c in field) if (Char.IsControl(c)) throw new InvalidDataException();
data.Add(fields[0], new string[] { fields[1], fields[2] });
}
}
}
}
catch (Exception) { data.Clear(); sDetailsError = "Nie można wczytać szczegółów znaczeń. Lista synonimów nadal działa."; }
dDetails = data;
}
if (sDetailsError.Length > 0) return sDetailsError;
string[] detail;
if (!dDetails.TryGetValue(sSenseId, out detail)) return sMissing;
string result = detail[0].Length > 0 ? "Definicja:\r\n" + detail[0] : "Brak definicji w dołączonym słowniku.";
if (detail[1].Length > 0) result += "\r\n\r\nPrzykład:\r\n" + detail[1];
return result + "\r\n\r\nŹródło: plWordNet 5.0.";
}
public static string Error = "";
public static string FormsError = "";

static string Normalize(string sWord) {
if (sWord == null) return "";
if (sWord.IndexOf('\r') >= 0 || sWord.IndexOf('\n') >= 0) return "";
sWord = sWord.Trim();
if (sWord.Length == 0) return "";
foreach (char c in sWord) if (Char.IsWhiteSpace(c)) return "";
return sWord.ToLowerInvariant();
} // Normalize method

public static bool IsSingleWord(string sWord) {
if (sWord == null || sWord.Length == 0) return false;
if (sWord != sWord.Trim()) return false;
foreach (char c in sWord) if (Char.IsWhiteSpace(c)) return false;
return true;
} // IsSingleWord method

public static string ApplyCapitalization(string sSource, string sReplacement) {
if (sSource == null || sSource.Length == 0 || sReplacement == null || sReplacement.Length == 0) return sReplacement;
if (sSource == sSource.ToUpperInvariant()) return sReplacement.ToUpperInvariant();
if (Char.IsUpper(sSource, 0)) return Char.ToUpperInvariant(sReplacement[0]) + sReplacement.Substring(1);
return sReplacement;
} // ApplyCapitalization method

public static string FormatEntry(string sSynonym, string sPartOfSpeech, int iPosition, int iTotal) {
string sPart = "inna część mowy";
if (sPartOfSpeech == "n") sPart = "rzeczownik";
else if (sPartOfSpeech == "v") sPart = "czasownik";
else if (sPartOfSpeech == "a") sPart = "przymiotnik";
else if (sPartOfSpeech == "r") sPart = "przysłówek";
return sSynonym + ", " + sPart + ", " + iPosition + " z " + iTotal;
} // FormatEntry method

public static void ResetForTests() {
hashGroups.Clear();
sFormsPath = "";
sDetailsPath = "";
dDetails = null;
sDetailsError = "";
Error = "";
FormsError = "";
} // ResetForTests method

public static bool Load(string sPath) {
ResetForTests();
if (sPath == null || sPath.Length == 0 || !File.Exists(sPath)) {
Error = "Synonym dictionary file was not found.";
return false;
}
try {
using (StreamReader sr = new StreamReader(sPath, new UTF8Encoding(false, true), true)) {
string sLine;
int iLine = 0;
while ((sLine = sr.ReadLine()) != null) {
iLine++;
if (sLine.Length == 0 || sLine.StartsWith("#")) continue;
string[] aPart = sLine.Split(new char[] {'\t'});
if (aPart.Length != 4) {
Error = "Invalid synonym dictionary data at line " + iLine + ".";
ResetForTests();
Error = "Invalid synonym dictionary data at line " + iLine + ".";
return false;
}
string sWord = Normalize(aPart[0]);
if (sWord.Length == 0 || aPart[1].Trim().Length == 0 || aPart[2].Trim().Length == 0) {
Error = "Invalid synonym dictionary data at line " + iLine + ".";
ResetForTests();
Error = "Invalid synonym dictionary data at line " + iLine + ".";
return false;
}
HashSet<string> setSeen = new HashSet<string>(StringComparer.Ordinal);
List<string> lSynonyms = new List<string>();
foreach (string sRaw in aPart[3].Split(new char[] {'|'})) {
string sSynonym = Normalize(sRaw);
if (sSynonym.Length == 0 || sSynonym == sWord || !setSeen.Add(sSynonym)) continue;
lSynonyms.Add(sSynonym);
}
if (lSynonyms.Count == 0) continue;
List<Grupa> lGroups;
if (!hashGroups.TryGetValue(sWord, out lGroups)) {
lGroups = new List<Grupa>();
hashGroups.Add(sWord, lGroups);
}
lGroups.Add(new Grupa(aPart[1].Trim(), aPart[2].Trim(), lSynonyms));
}
}
string sCandidate = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sPath)), "plwordnet-forms.bin");
if (File.Exists(sCandidate)) sFormsPath = sCandidate;
sDetailsPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sPath)), "plwordnet-details.txt");
return true;
}
catch (Exception ex) {
ResetForTests();
Error = "Could not load synonym dictionary: " + ex.Message;
return false;
}
} // Load method

public static List<Grupa> Find(string sWord) {
FormsError = "";
string sKey = Normalize(sWord);
if (sKey.Length == 0) return new List<Grupa>();
List<Grupa> lGroups;
if (hashGroups.TryGetValue(sKey, out lGroups)) return new List<Grupa>(lGroups);
if (sFormsPath.Length == 0) return new List<Grupa>();
try {
List<Grupa> result = new List<Grupa>();
HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
Dictionary<string, List<string[]>> cache = new Dictionary<string, List<string[]>>(StringComparer.Ordinal);
foreach (string[] source in SynonimyOdmiany.Lookup(sFormsPath, "f:" + sKey)) {
if (!hashGroups.TryGetValue(source[0], out lGroups)) continue;
foreach (Grupa group in lGroups) {
// Starsze identyfikatory OMW nie maja potwierdzonego mapowania znaczen 5.0.
// Fallback zachowujemy dla lematu, nie laczymy odmiany po samym napisie.
if (group.PartOfSpeech != source[1] || group.SenseId != source[2]) continue;
List<string> values = new List<string>();
foreach (string synonym in group.Synonyms) {
List<string[]> forms;
if (!cache.TryGetValue(synonym, out forms)) {
forms = SynonimyOdmiany.Lookup(sFormsPath, "l:" + synonym);
cache.Add(synonym, forms);
}
foreach (string[] target in forms) {
if (target[1] != group.PartOfSpeech || target[2] != group.SenseId || target[0] == sKey) continue;
if (SynonimyOdmiany.TagsMatch(group.PartOfSpeech, source[3], target[3]) && seen.Add(group.PartOfSpeech + "\t" + target[0])) values.Add(target[0]);
}
}
if (values.Count > 0) result.Add(new Grupa(group.PartOfSpeech, group.SenseId, values));
}
}
return result;
}
catch (Exception) {
FormsError = "Nie można odczytać indeksu odmiany słownika synonimów.";
return new List<Grupa>();
}
} // Find method

} // Synonimy class
