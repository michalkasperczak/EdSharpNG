// FormySynonimow.cs - poświadczone źródłowo formy wybranego synonimu.
// Nie tworzy końcówek; polskie dane pochodzą z plwordnet-forms.bin, a angielskie
// wyłącznie z wyjątków Princeton WordNet 3.0.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public static class FormySynonimow {
public class Forma {
public string Tekst;
public string Opis;
internal string SortTag;
internal int SourceOrder;
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
case "sg": return JezykInterfejsu.Wybierz("singular", "liczba pojedyncza"); case "pl": return JezykInterfejsu.Wybierz("plural", "liczba mnoga");
case "nom": return JezykInterfejsu.Wybierz("nominative", "mianownik"); case "gen": return JezykInterfejsu.Wybierz("genitive", "dopełniacz"); case "dat": return JezykInterfejsu.Wybierz("dative", "celownik"); case "acc": return JezykInterfejsu.Wybierz("accusative", "biernik"); case "inst": return JezykInterfejsu.Wybierz("instrumental", "narzędnik"); case "loc": return JezykInterfejsu.Wybierz("locative", "miejscownik"); case "voc": return JezykInterfejsu.Wybierz("vocative", "wołacz");
case "m1": return JezykInterfejsu.Wybierz("masculine personal", "rodzaj męskoosobowy"); case "m2": return JezykInterfejsu.Wybierz("masculine inanimate", "rodzaj męskorzeczowy"); case "m3": return JezykInterfejsu.Wybierz("masculine animate", "rodzaj męskorzeczowy"); case "f": return JezykInterfejsu.Wybierz("feminine", "rodzaj żeński"); case "n": return JezykInterfejsu.Wybierz("neuter", "rodzaj nijaki");
case "pos": return JezykInterfejsu.Wybierz("positive degree", "stopień równy"); case "com": return JezykInterfejsu.Wybierz("comparative", "stopień wyższy"); case "sup": return JezykInterfejsu.Wybierz("superlative", "stopień najwyższy");
case "inf": return JezykInterfejsu.Wybierz("infinitive", "bezokolicznik"); case "fin": return JezykInterfejsu.Wybierz("personal form", "forma osobowa"); case "bedzie": return JezykInterfejsu.Wybierz("future tense", "czas przyszły"); case "impt": return JezykInterfejsu.Wybierz("imperative mood", "tryb rozkazujący"); case "praet": return JezykInterfejsu.Wybierz("past tense", "czas przeszły"); case "imps": return JezykInterfejsu.Wybierz("impersonal form", "forma bezosobowa"); case "pcon": return JezykInterfejsu.Wybierz("contemporary adverbial participle", "imiesłów przysłówkowy współczesny"); case "pant": return JezykInterfejsu.Wybierz("anterior adverbial participle", "imiesłów przysłówkowy uprzedni"); case "ger": return JezykInterfejsu.Wybierz("verbal noun", "rzeczownik odczasownikowy"); case "ppas": return JezykInterfejsu.Wybierz("passive adjectival participle", "imiesłów przymiotnikowy bierny"); case "pact": return JezykInterfejsu.Wybierz("active adjectival participle", "imiesłów przymiotnikowy czynny"); case "winien": return JezykInterfejsu.Wybierz("should form", "forma winien");
case "pri": return JezykInterfejsu.Wybierz("first person", "pierwsza osoba"); case "sec": return JezykInterfejsu.Wybierz("second person", "druga osoba"); case "ter": return JezykInterfejsu.Wybierz("third person", "trzecia osoba");
case "perf": return JezykInterfejsu.Wybierz("perfective aspect", "aspekt dokonany"); case "imperf": return JezykInterfejsu.Wybierz("imperfective aspect", "aspekt niedokonany");
case "aff": return JezykInterfejsu.Wybierz("affirmative form", "forma twierdząca"); case "neg": return JezykInterfejsu.Wybierz("negative form", "forma przecząca");
default: return "";
}
} // PolishName method

static string PolishDescription(string sPart, string sTag) {
string prefix = sPart == "n" ? JezykInterfejsu.Wybierz("noun", "rzeczownik") : sPart == "v" ? JezykInterfejsu.Wybierz("verb", "czasownik") : sPart == "a" ? JezykInterfejsu.Wybierz("adjective", "przymiotnik") : JezykInterfejsu.Wybierz("adverb", "przysłówek");
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

static void Add(Dictionary<string, Forma> dForms, string sText, string sDescription, string sTag, int iSourceOrder) {
string key = sText + "\t" + sDescription;
if (!dForms.ContainsKey(key)) {
Forma form = new Forma(sText, sDescription);
form.SortTag = sTag;
form.SourceOrder = iSourceOrder;
dForms.Add(key, form);
}
} // Add method

static List<Forma> Sorted(Dictionary<string, Forma> dForms) {
List<Forma> result = new List<Forma>(dForms.Values);
result.Sort(delegate(Forma x, Forma y) { int value = StringComparer.Ordinal.Compare(x.Tekst, y.Tekst); return value != 0 ? value : StringComparer.Ordinal.Compare(x.Opis, y.Opis); });
return result;
} // Sorted method

static int GroupRank(string sValue, string[] values) {
int result = Int32.MaxValue;
foreach (string part in sValue.Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries)) {
for (int i = 0; i < values.Length; i++) if (part == values[i] && i < result) result = i;
}
return result;
} // GroupRank method

static int TagRank(string[] tags, string[] values) {
foreach (string tag in tags) {
int value = GroupRank(tag, values);
if (value != Int32.MaxValue) return value;
}
return Int32.MaxValue;
} // TagRank method

static int CompareRank(int x, int y) {
bool xKnown = x != Int32.MaxValue, yKnown = y != Int32.MaxValue;
if (xKnown != yKnown) return xKnown ? -1 : 1;
return xKnown ? x.CompareTo(y) : 0;
} // CompareRank method

static bool AllKnown(string[] tags, string[] values) {
for (int i = 1; i < tags.Length; i++) foreach (string part in tags[i].Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries)) {
bool found = false;
foreach (string value in values) if (part == value) { found = true; break; }
if (!found) return false;
}
return true;
} // AllKnown method

static bool KnownPolishTag(string[] tags) {
if (tags.Length == 0) return false;
string[] numbers = { "sg", "pl" }, cases = { "nom", "gen", "dat", "acc", "inst", "loc", "voc" }, genders = { "m1", "m2", "m3", "f", "n" }, degrees = { "pos", "com", "sup" }, verbKinds = { "inf", "fin", "bedzie", "praet", "impt", "imps", "pcon", "pant", "ger", "ppas", "pact", "winien" };
if (tags[0] == "subst" || tags[0] == "depr") return TagRank(tags, numbers) != Int32.MaxValue && TagRank(tags, cases) != Int32.MaxValue && TagRank(tags, genders) != Int32.MaxValue && AllKnown(tags, new string[] { "sg", "pl", "nom", "gen", "dat", "acc", "inst", "loc", "voc", "m1", "m2", "m3", "f", "n", "col", "ncol", "pt" });
if (tags[0] == "adj") return TagRank(tags, degrees) != Int32.MaxValue && TagRank(tags, numbers) != Int32.MaxValue && TagRank(tags, cases) != Int32.MaxValue && TagRank(tags, genders) != Int32.MaxValue && AllKnown(tags, new string[] { "pos", "com", "sup", "sg", "pl", "nom", "gen", "dat", "acc", "inst", "loc", "voc", "m1", "m2", "m3", "f", "n" });
if (tags[0] == "adv") return TagRank(tags, degrees) != Int32.MaxValue && AllKnown(tags, degrees);
foreach (string kind in verbKinds) if (tags[0] == kind) return AllKnown(tags, new string[] { "sg", "pl", "nom", "gen", "dat", "acc", "inst", "loc", "voc", "m1", "m2", "m3", "f", "n", "pri", "sec", "ter", "perf", "imperf", "aff", "neg", "agl", "nagl" });
return false;
} // KnownPolishTag method

static bool VerbKind(string sKind) {
return sKind == "inf" || sKind == "fin" || sKind == "bedzie" || sKind == "praet" || sKind == "impt" || sKind == "imps" || sKind == "pcon" || sKind == "pant" || sKind == "ger" || sKind == "ppas" || sKind == "pact" || sKind == "winien";
} // VerbKind method

static int ComparePolish(Forma x, Forma y) {
string[] xTags = x.SortTag.Split(new char[] {':'}, StringSplitOptions.RemoveEmptyEntries);
string[] yTags = y.SortTag.Split(new char[] {':'}, StringSplitOptions.RemoveEmptyEntries);
string xKind = xTags.Length == 0 ? "" : xTags[0], yKind = yTags.Length == 0 ? "" : yTags[0];
// Deprecjatyw to poświadczony wariant rzeczownika, nie osobny paradygmat.
if (xKind == "depr") xKind = "subst";
if (yKind == "depr") yKind = "subst";
int value;
bool xKnownTag = KnownPolishTag(xTags), yKnownTag = KnownPolishTag(yTags);
if (xKnownTag != yKnownTag) return xKnownTag ? -1 : 1;
if (!xKnownTag) {
value = StringComparer.Ordinal.Compare(x.SortTag, y.SortTag);
return value != 0 ? value : x.SourceOrder.CompareTo(y.SourceOrder);
}
if (!VerbKind(xKind) && !VerbKind(yKind)) {
value = StringComparer.Ordinal.Compare(xKind, yKind);
if (value != 0) return value;
}
string[] numbers = { "sg", "pl" }, cases = { "nom", "gen", "dat", "acc", "inst", "loc", "voc" }, genders = { "m1", "m2", "m3", "f", "n" };
if (xKind == "subst") {
value = CompareRank(TagRank(xTags, numbers), TagRank(yTags, numbers)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, cases), TagRank(yTags, cases)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, genders), TagRank(yTags, genders)); if (value != 0) return value;
}
else if (xKind == "adj") {
string[] degrees = { "pos", "com", "sup" };
value = CompareRank(TagRank(xTags, degrees), TagRank(yTags, degrees)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, numbers), TagRank(yTags, numbers)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, genders), TagRank(yTags, genders)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, cases), TagRank(yTags, cases)); if (value != 0) return value;
}
else if (xKind == "adv") {
string[] degrees = { "pos", "com", "sup" };
value = CompareRank(TagRank(xTags, degrees), TagRank(yTags, degrees)); if (value != 0) return value;
}
else if (xKind == "inf" || xKind == "fin" || xKind == "bedzie" || xKind == "praet" || xKind == "impt" || xKind == "imps" || xKind == "pcon" || xKind == "pant" || xKind == "ger" || xKind == "ppas" || xKind == "pact" || xKind == "winien") {
string[] verbKinds = { "inf", "fin", "bedzie", "praet", "impt", "imps", "pcon", "pant", "ger", "ppas", "pact", "winien" }, persons = { "pri", "sec", "ter" }, aspects = { "perf", "imperf" }, polarity = { "aff", "neg" };
value = CompareRank(TagRank(xTags, verbKinds), TagRank(yTags, verbKinds)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, numbers), TagRank(yTags, numbers)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, persons), TagRank(yTags, persons)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, genders), TagRank(yTags, genders)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, cases), TagRank(yTags, cases)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, aspects), TagRank(yTags, aspects)); if (value != 0) return value;
value = CompareRank(TagRank(xTags, polarity), TagRank(yTags, polarity)); if (value != 0) return value;
}
return x.SourceOrder.CompareTo(y.SourceOrder);
} // ComparePolish method

static List<Forma> SortedPolish(Dictionary<string, Forma> dForms) {
List<Forma> result = new List<Forma>(dForms.Values);
result.Sort(ComparePolish);
return result;
} // SortedPolish method

static List<Forma> PolishForms(string sFolder, string sWord, string sPart, string sSense) {
Dictionary<string, Forma> forms = new Dictionary<string, Forma>(StringComparer.Ordinal);
string path = Path.Combine(sFolder, "plwordnet-forms.bin");
if (!File.Exists(path)) { Error = JezykInterfejsu.Wybierz("The Polish synonym-form index is missing. The synonym list still works.", "Brak indeksu form polskich synonimów. Lista synonimów nadal działa."); return new List<Forma>(); }
try {
HashSet<string> seenLemmas = new HashSet<string>(StringComparer.Ordinal);
List<string> lemmas = new List<string>();
foreach (string[] row in SynonimyOdmiany.Lookup(path, "f:" + sWord.ToLowerInvariant())) if (row[1] == sPart && row[2] == sSense && seenLemmas.Add(row[0])) lemmas.Add(row[0]);
int sourceOrder = 0;
foreach (string lemma in lemmas) foreach (string[] row in SynonimyOdmiany.Lookup(path, "l:" + lemma)) if (row[1] == sPart && row[2] == sSense) Add(forms, row[0], PolishDescription(sPart, row[3]), row[3], sourceOrder++);
return SortedPolish(forms);
}
catch (Exception) { Error = JezykInterfejsu.Wybierz("The Polish synonym-form index could not be read. The synonym list still works.", "Nie można odczytać indeksu form polskich synonimów. Lista synonimów nadal działa."); return new List<Forma>(); }
} // PolishForms method

static IEnumerable<string[]> Rows(string sPath, int iColumns) {
if (!File.Exists(sPath) || new FileInfo(sPath).Length > 67108864) throw new InvalidDataException();
using (StreamReader reader = new StreamReader(sPath, utf8, false)) {
foreach (string[] row in Rows(reader, iColumns)) yield return row;
}
} // Rows method

static IEnumerable<string[]> Rows(TextReader reader, int iColumns) {
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
} // Rows method

static string EnglishMetadataKey(string sForm, string sPart, string sLemma) {
return sForm + "\t" + sPart + "\t" + sLemma;
} // EnglishMetadataKey method

static bool EnglishPtbTag(string sPart, string sTag) {
if (sPart == "n") return sTag == "NNS";
if (sPart == "a") return sTag == "JJR" || sTag == "JJS";
if (sPart == "r") return sTag == "RBR" || sTag == "RBS";
return sPart == "v" && (sTag == "VB" || sTag == "VBP" || sTag == "VBZ" || sTag == "VBD" || sTag == "VBN" || sTag == "VBG");
} // EnglishPtbTag method

[DataContract] sealed class MetadataManifest {
[DataMember(Name = "outputs", IsRequired = true)] public MetadataOutputs Outputs;
}
[DataContract] sealed class MetadataOutputs {
[DataMember(Name = "metadata", IsRequired = true)] public MetadataFingerprint Metadata;
}
[DataContract] sealed class MetadataFingerprint {
[DataMember(Name = "bytes", IsRequired = true)] public long Bytes;
[DataMember(Name = "sha256", IsRequired = true)] public string Sha256;
}

static byte[] VerifiedEnglishMetadata(string sFolder, string sPath) {
string manifestPath = Path.Combine(sFolder, "wordnet-en-form-metadata-manifest.json");
if (new FileInfo(sPath).Length > 67108864 || new FileInfo(manifestPath).Length > 65536) throw new InvalidDataException();
MetadataManifest manifest;
using (FileStream stream = File.OpenRead(manifestPath)) manifest = (MetadataManifest)new DataContractJsonSerializer(typeof(MetadataManifest)).ReadObject(stream);
if (manifest == null || manifest.Outputs == null || manifest.Outputs.Metadata == null) throw new InvalidDataException();
MetadataFingerprint expected = manifest.Outputs.Metadata;
if (expected.Sha256 == null || !Regex.IsMatch(expected.Sha256, "\\A[0-9a-fA-F]{64}\\z")) throw new InvalidDataException();
byte[] bytes = File.ReadAllBytes(sPath);
if (bytes.LongLength != expected.Bytes) throw new InvalidDataException();
using (SHA256 sha = SHA256.Create()) {
string actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
if (!String.Equals(actual, expected.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException();
}
return bytes;
} // VerifiedEnglishMetadata method

static Dictionary<string, string> EnglishMetadata(string sFolder) {
string path = Path.Combine(sFolder, "wordnet-en-form-metadata.txt");
if (!File.Exists(path)) return null;
try {
Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
int count = 0;
// Hash and parse the same bytes; a replacement cannot slip between the two.
using (StreamReader reader = new StreamReader(new MemoryStream(VerifiedEnglishMetadata(sFolder, path)), utf8, false)) {
foreach (string[] row in Rows(reader, 5)) {
if (++count > 200000 || !Synonimy.IsSingleWord(row[0]) || !Part(row[1]) || !Synonimy.IsSingleWord(row[2]) || !Regex.IsMatch(row[4], "\\AAGID2016\\.01\\.19:[0-9]+(?:,[0-9]+)*\\z")) throw new InvalidDataException();
string[] tags = row[3].Split('|');
if (tags.Length == 0) throw new InvalidDataException();
HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
foreach (string tag in tags) if (!EnglishPtbTag(row[1], tag) || !seen.Add(tag)) throw new InvalidDataException();
string key = EnglishMetadataKey(row[0], row[1], row[2]);
if (result.ContainsKey(key)) throw new InvalidDataException();
result.Add(key, row[3]);
}
}
return result;
}
catch (Exception) { Error = JezykInterfejsu.Wybierz("Optional English-form ordering metadata could not be read. The synonym list still works.", "Nie można odczytać opcjonalnych metadanych kolejności angielskich form. Lista synonimów nadal działa."); return null; }
} // EnglishMetadata method

static int EnglishTagRank(string sPart, string sTag) {
if (sPart == "n") return sTag == "NNS" ? 0 : Int32.MaxValue;
if (sPart == "a") return sTag == "JJR" ? 0 : sTag == "JJS" ? 1 : Int32.MaxValue;
if (sPart == "r") return sTag == "RBR" ? 0 : sTag == "RBS" ? 1 : Int32.MaxValue;
if (sPart != "v") return Int32.MaxValue;
if (sTag == "VB") return 0;
if (sTag == "VBP") return 1;
if (sTag == "VBZ") return 2;
if (sTag == "VBD") return 3;
if (sTag == "VBN") return 4;
if (sTag == "VBG") return 5;
return Int32.MaxValue;
} // EnglishTagRank method

static List<Forma> SortedEnglish(Dictionary<string, Forma> dForms, string sFolder, string sPart, string sLemma) {
List<Forma> result = Sorted(dForms);
Dictionary<string, string> metadata = EnglishMetadata(sFolder);
if (metadata == null) return result;
List<Forma> known = new List<Forma>();
List<int> positions = new List<int>();
for (int i = 0; i < result.Count; i++) {
result[i].SourceOrder = i;
string tag;
if (metadata.TryGetValue(EnglishMetadataKey(result[i].Tekst, sPart, sLemma), out tag) && tag.IndexOf('|') < 0 && EnglishPtbTag(sPart, tag)) { known.Add(result[i]); positions.Add(i); }
}
known.Sort(delegate(Forma x, Forma y) {
string xTag = metadata[EnglishMetadataKey(x.Tekst, sPart, sLemma)], yTag = metadata[EnglishMetadataKey(y.Tekst, sPart, sLemma)];
int value = EnglishTagRank(sPart, xTag).CompareTo(EnglishTagRank(sPart, yTag));
return value != 0 ? value : x.SourceOrder.CompareTo(y.SourceOrder);
});
for (int i = 0; i < positions.Count; i++) result[positions[i]] = known[i];
return result;
} // SortedEnglish method

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
if (row[1] == sPart && String.Equals(row[2], lemma, StringComparison.OrdinalIgnoreCase)) Add(forms, row[0], JezykInterfejsu.Wybierz("English irregular form, attested in Princeton WordNet 3.0; exception list, not a full paradigm", "angielska forma nieregularna, poświadczona w Princeton WordNet 3.0; zestaw wyjątków, nie pełny paradygmat"));
}
return SortedEnglish(forms, sFolder, sPart, lemma);
}
catch (Exception) { Error = JezykInterfejsu.Wybierz("English irregular forms could not be read. The synonym list still works.", "Nie można odczytać angielskich form nieregularnych. Lista synonimów nadal działa."); return new List<Forma>(); }
} // EnglishForms method

public static List<Forma> GetForms(string folder, string word, string pos, string senseId) {
Error = "";
if (folder == null || !Synonimy.IsSingleWord(word) || !Part(pos) || String.IsNullOrEmpty(senseId)) return new List<Forma>();
if (PolishSense(senseId)) return PolishForms(folder, word, pos, senseId);
if (EnglishSense(senseId) && senseId.Substring(5, 1) == pos) return EnglishForms(folder, word, pos, senseId);
return new List<Forma>();
} // GetForms method
} // FormySynonimow class
