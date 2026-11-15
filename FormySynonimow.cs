// FormySynonimow.cs - poświadczone źródłowo formy wybranego synonimu.
// Nie tworzy końcówek; polskie dane pochodzą z plwordnet-forms.bin, a angielskie
// wyłącznie z wyjątków Princeton WordNet 3.0.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public static class FormySynonimow {
public class Forma {
public string Tekst;
public string Opis;
public Forma(string sTekst, string sOpis) { Tekst = sTekst; Opis = sOpis; }
} // Forma class

public static string Error = "";
static readonly UTF8Encoding utf8 = new UTF8Encoding(false, true);

static bool PolishSense(string sSense) {
if (sSense == null || sSense.Length != 32) return false;
foreach (char c in sSense) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
return true;
} // PolishSense method

static bool EnglishSense(string sSense) {
return sSense != null && Regex.IsMatch(sSense, "\\Awn30:[nvar]:[0-9]{8}\\z");
} // EnglishSense method

static bool Part(string sPart) {
return sPart == "n" || sPart == "v" || sPart == "a" || sPart == "r";
} // Part method

static string PolishName(string sTag) {
switch (sTag) {
case "sg": return "liczba pojedyncza"; case "pl": return "liczba mnoga";
case "nom": return "mianownik"; case "gen": return "dopełniacz"; case "dat": return "celownik"; case "acc": return "biernik"; case "inst": return "narzędnik"; case "loc": return "miejscownik"; case "voc": return "wołacz";
case "m1": return "rodzaj męskoosobowy"; case "m2": return "rodzaj męskozwierzęcy"; case "m3": return "rodzaj męskorzeczowy"; case "f": return "rodzaj żeński"; case "n": return "rodzaj nijaki";
case "pos": return "stopień równy"; case "com": return "stopień wyższy"; case "sup": return "stopień najwyższy";
case "inf": return "bezokolicznik"; case "fin": return "forma osobowa"; case "bedzie": return "czas przyszły"; case "impt": return "tryb rozkazujący"; case "praet": return "czas przeszły"; case "imps": return "forma bezosobowa"; case "pcon": return "imiesłów przysłówkowy współczesny"; case "pant": return "imiesłów przysłówkowy uprzedni"; case "ger": return "rzeczownik odczasownikowy"; case "ppas": return "imiesłów przymiotnikowy bierny"; case "pact": return "imiesłów przymiotnikowy czynny"; case "winien": return "forma winien";
case "pri": return "pierwsza osoba"; case "sec": return "druga osoba"; case "ter": return "trzecia osoba";
case "perf": return "aspekt dokonany"; case "imperf": return "aspekt niedokonany";
case "aff": return "forma twierdząca"; case "neg": return "forma przecząca";
default: return "";
}
} // PolishName method

static string PolishDescription(string sPart, string sTag) {
string prefix = sPart == "n" ? "rzeczownik" : sPart == "v" ? "czasownik" : sPart == "a" ? "przymiotnik" : "przysłówek";
List<string> names = new List<string>();
foreach (string token in sTag.Split(new char[] { ':', '.' }, StringSplitOptions.RemoveEmptyEntries)) {
string name = PolishName(token);
if (name.Length > 0 && !names.Contains(name)) names.Add(name);
}
return names.Count == 0 ? prefix : prefix + ": " + String.Join(", ", names.ToArray());
} // PolishDescription method

static void Add(Dictionary<string, Forma> dForms, string sText, string sDescription) {
string key = sText + "\t" + sDescription;
if (!dForms.ContainsKey(key)) dForms.Add(key, new Forma(sText, sDescription));
} // Add method

static List<Forma> Sorted(Dictionary<string, Forma> dForms) {
List<Forma> result = new List<Forma>(dForms.Values);
result.Sort(delegate(Forma x, Forma y) { int value = StringComparer.Ordinal.Compare(x.Tekst, y.Tekst); return value != 0 ? value : StringComparer.Ordinal.Compare(x.Opis, y.Opis); });
return result;
} // Sorted method

static List<Forma> PolishForms(string sFolder, string sWord, string sPart, string sSense) {
Dictionary<string, Forma> forms = new Dictionary<string, Forma>(StringComparer.Ordinal);
string path = Path.Combine(sFolder, "plwordnet-forms.bin");
if (!File.Exists(path)) { Error = "Brak indeksu form polskich synonimów. Lista synonimów nadal działa."; return new List<Forma>(); }
try {
HashSet<string> lemmas = new HashSet<string>(StringComparer.Ordinal);
foreach (string[] row in SynonimyOdmiany.Lookup(path, "f:" + sWord.ToLowerInvariant())) if (row[1] == sPart && row[2] == sSense) lemmas.Add(row[0]);
foreach (string lemma in lemmas) foreach (string[] row in SynonimyOdmiany.Lookup(path, "l:" + lemma)) if (row[1] == sPart && row[2] == sSense) Add(forms, row[0], PolishDescription(sPart, row[3]));
return Sorted(forms);
}
catch (Exception) { Error = "Nie można odczytać indeksu form polskich synonimów. Lista synonimów nadal działa."; return new List<Forma>(); }
} // PolishForms method

static IEnumerable<string[]> Rows(string sPath, int iColumns) {
if (!File.Exists(sPath) || new FileInfo(sPath).Length > 67108864) throw new InvalidDataException();
using (StreamReader reader = new StreamReader(sPath, utf8, false)) {
string line;
while ((line = reader.ReadLine()) != null) {
if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
string[] fields = line.Split('\t');
if (line.Length > 65536 || fields.Length != iColumns) throw new InvalidDataException();
foreach (string field in fields) {
if (field.Length == 0) throw new InvalidDataException();
foreach (char c in field) if (Char.IsControl(c)) throw new InvalidDataException();
}
yield return fields;
}
}
} // Rows method

static List<Forma> EnglishForms(string sFolder, string sWord, string sPart, string sSense) {
Dictionary<string, Forma> forms = new Dictionary<string, Forma>(StringComparer.Ordinal);
try {
string lemma = "";
string synonyms = Path.Combine(sFolder, "wordnet-en-synonyms.txt");
foreach (string[] row in Rows(synonyms, 4)) {
if (!Synonimy.IsSingleWord(row[0]) || !EnglishSense(row[2]) || row[1].Length != 1 || row[1] != row[2].Substring(5, 1)) throw new InvalidDataException();
if (row[1] == sPart && row[2] == sSense && String.Equals(row[0], sWord, StringComparison.OrdinalIgnoreCase)) { lemma = row[0]; break; }
}
if (lemma.Length == 0) {
string exceptionsForLemma = Path.Combine(sFolder, "wordnet-en-exceptions.txt");
foreach (string[] row in Rows(exceptionsForLemma, 3)) {
if (!Synonimy.IsSingleWord(row[0]) || !Synonimy.IsSingleWord(row[2]) || row[1].Length != 1 || !Part(row[1])) throw new InvalidDataException();
if (row[1] == sPart && String.Equals(row[0], sWord, StringComparison.OrdinalIgnoreCase)) { lemma = row[2]; break; }
}
if (lemma.Length == 0) return new List<Forma>();
bool member = false;
foreach (string[] row in Rows(synonyms, 4)) {
if (!Synonimy.IsSingleWord(row[0]) || !EnglishSense(row[2]) || row[1].Length != 1 || row[1] != row[2].Substring(5, 1)) throw new InvalidDataException();
if (row[1] == sPart && row[2] == sSense && String.Equals(row[0], lemma, StringComparison.OrdinalIgnoreCase)) { member = true; break; }
}
if (!member) return new List<Forma>();
}
string exceptions = Path.Combine(sFolder, "wordnet-en-exceptions.txt");
foreach (string[] row in Rows(exceptions, 3)) {
if (!Synonimy.IsSingleWord(row[0]) || !Synonimy.IsSingleWord(row[2]) || row[1].Length != 1 || !Part(row[1])) throw new InvalidDataException();
if (row[1] == sPart && String.Equals(row[2], lemma, StringComparison.OrdinalIgnoreCase)) Add(forms, row[0], "angielska forma nieregularna, poświadczona w Princeton WordNet 3.0; zestaw wyjątków, nie pełny paradygmat");
}
return Sorted(forms);
}
catch (Exception) { Error = "Nie można odczytać angielskich form nieregularnych. Lista synonimów nadal działa."; return new List<Forma>(); }
} // EnglishForms method

public static List<Forma> GetForms(string folder, string word, string pos, string senseId) {
Error = "";
if (folder == null || !Synonimy.IsSingleWord(word) || !Part(pos) || String.IsNullOrEmpty(senseId)) return new List<Forma>();
if (PolishSense(senseId)) return PolishForms(folder, word, pos, senseId);
if (EnglishSense(senseId) && senseId.Substring(5, 1) == pos) return EnglishForms(folder, word, pos, senseId);
return new List<Forma>();
} // GetForms method
} // FormySynonimow class
