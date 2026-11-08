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
public static string Error = "";

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
Error = "";
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
return true;
}
catch (Exception ex) {
ResetForTests();
Error = "Could not load synonym dictionary: " + ex.Message;
return false;
}
} // Load method

public static List<Grupa> Find(string sWord) {
string sKey = Normalize(sWord);
if (sKey.Length == 0) return new List<Grupa>();
List<Grupa> lGroups;
if (!hashGroups.TryGetValue(sKey, out lGroups)) return new List<Grupa>();
return new List<Grupa>(lGroups);
} // Find method

} // Synonimy class
