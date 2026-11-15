// TDD acceptance probe for optional, build-time AGID English order metadata.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

public static class TestEnglishFormsOrder {
static int failures;
static int assertions;
const string Description = "angielska forma nieregularna, poświadczona w Princeton WordNet 3.0; zestaw wyjątków, nie pełny paradygmat";

static void Check(bool value, string name) {
assertions++;
Console.WriteLine((value ? "PASS " : "FAIL ") + name);
if (!value) failures++;
}

static void Text(string path, string content) { File.WriteAllText(path, content, new UTF8Encoding(false)); }

static void Metadata(string folder, string content) {
string path = Path.Combine(folder, "wordnet-en-form-metadata.txt");
Text(path, content);
byte[] bytes = File.ReadAllBytes(path);
string hash;
using (SHA256 sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
Text(Path.Combine(folder, "wordnet-en-form-metadata-manifest.json"), "{\"outputs\":{\"metadata\":{\"bytes\":" + bytes.Length + ",\"sha256\":\"" + hash + "\"}}}");
}

static List<string[]> Rows(string path, int columns) {
List<string[]> rows = new List<string[]>();
using (StreamReader reader = new StreamReader(path, new UTF8Encoding(false, true), false)) {
string line;
while ((line = reader.ReadLine()) != null) {
if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
string[] fields = line.Split('\t');
if (fields.Length != columns) throw new InvalidDataException("unexpected source row");
rows.Add(fields);
}
}
return rows;
}

static List<string> Actual(Type provider, string folder, string lemma, string part, string sense, List<string> descriptions) {
List<string> result = new List<string>();
IEnumerable forms = (IEnumerable)provider.GetMethod("GetForms").Invoke(null, new object[] { folder, lemma, part, sense });
foreach (object form in forms) {
result.Add((string)form.GetType().GetField("Tekst").GetValue(form));
descriptions.Add((string)form.GetType().GetField("Opis").GetValue(form));
}
return result;
}

static bool Same(List<string> actual, params string[] expected) {
if (actual.Count != expected.Length) return false;
for (int i = 0; i < expected.Length; i++) if (actual[i] != expected[i]) return false;
return true;
}

static bool SameTextAndDescriptions(List<string> actual, List<string> descriptions, params string[] expected) {
if (!Same(actual, expected)) return false;
foreach (string description in descriptions) if (description != Description) return false;
return true;
}

static void Fixture(string folder) {
if (Directory.Exists(folder)) Directory.Delete(folder, true);
Directory.CreateDirectory(folder);
Text(Path.Combine(folder, "wordnet-en-synonyms.txt"),
"go\tv\twn30:v:00000001\tmove\n" +
"good\ta\twn30:a:00000002\tfine\n" +
"be\tv\twn30:v:00000003\texist\n");
Text(Path.Combine(folder, "wordnet-en-exceptions.txt"),
"gone\tv\tgo\nwent\tv\tgo\n" +
"best\ta\tgood\nbetter\ta\tgood\n" +
"am\tv\tbe\nare\tv\tbe\nbeen\tv\tbe\nbeing\tv\tbe\nis\tv\tbe\nwas\tv\tbe\nwere\tv\tbe\n");
Metadata(folder,
"gone\tv\tgo\tVBN\tAGID2016.01.19:1\n" +
"went\tv\tgo\tVBD\tAGID2016.01.19:1\n" +
"best\ta\tgood\tJJS\tAGID2016.01.19:2\n" +
"better\ta\tgood\tJJR\tAGID2016.01.19:2\n" +
"am\tv\tbe\tVBP\tAGID2016.01.19:3\n" +
"are\tv\tbe\tVBP\tAGID2016.01.19:3\n" +
"is\tv\tbe\tVBZ\tAGID2016.01.19:3\n" +
"was\tv\tbe\tVBD\tAGID2016.01.19:3\n" +
"were\tv\tbe\tVBD\tAGID2016.01.19:3\n" +
"been\tv\tbe\tVBN\tAGID2016.01.19:3\n" +
"being\tv\tbe\tVBG\tAGID2016.01.19:3\n");
}

static string Error(Type provider) { return (string)provider.GetField("Error").GetValue(null); }

static string SenseFor(string folder, string lemma, string part) {
foreach (string[] row in Rows(Path.Combine(folder, "wordnet-en-synonyms.txt"), 4)) if (row[0] == lemma && row[1] == part) return row[2];
throw new InvalidDataException("missing source sense for " + lemma + "/" + part);
}

static void FixtureTests(Type provider, string folder) {
Fixture(folder);
List<string> descriptions = new List<string>();
Check(SameTextAndDescriptions(Actual(provider, folder, "go", "v", "wn30:v:00000001", descriptions), descriptions, "went", "gone"), "AGID VBD precedes VBN instead of lexical gone,went");
descriptions.Clear();
Check(SameTextAndDescriptions(Actual(provider, folder, "good", "a", "wn30:a:00000002", descriptions), descriptions, "better", "best"), "AGID JJR precedes JJS instead of lexical best,better");
descriptions.Clear();
Check(SameTextAndDescriptions(Actual(provider, folder, "be", "v", "wn30:v:00000003", descriptions), descriptions, "am", "are", "is", "was", "were", "been", "being"), "AGID be slots order finite present, past, then participles");
Check(Error(provider).Length == 0, "valid optional metadata has no provider warning");

File.Delete(Path.Combine(folder, "wordnet-en-form-metadata.txt"));
descriptions.Clear();
Check(SameTextAndDescriptions(Actual(provider, folder, "go", "v", "wn30:v:00000001", descriptions), descriptions, "gone", "went"), "missing optional metadata retains lexical English list");
Check(Error(provider).Length == 0, "missing optional metadata is not an error");

Metadata(folder,
"gone\tv\tgo\tVBD|VBN\tAGID2016.01.19:1\n" +
"went\tv\tgo\tVBD\tAGID2016.01.19:1\n" +
"bogus\tv\tgo\tVBZ\tAGID2016.01.19:1\n");
descriptions.Clear();
Check(SameTextAndDescriptions(Actual(provider, folder, "go", "v", "wn30:v:00000001", descriptions), descriptions, "gone", "went"), "ambiguous candidate tag keeps its original slot while known slots remain stable");
Check(Error(provider).Length == 0, "ambiguous candidate is valid optional metadata");

Metadata(folder, "GONE\tv\tgo\tVBN\tAGID2016.01.19:1\n");
descriptions.Clear();
Check(SameTextAndDescriptions(Actual(provider, folder, "go", "v", "wn30:v:00000001", descriptions), descriptions, "gone", "went"), "metadata keys require exact form matching without casefold expansion");
Check(Error(provider).Length == 0, "unmatched exact metadata key does not warn");

Metadata(folder, "gone\tv\tgo\tZZZ\tAGID2016.01.19:1\n");
descriptions.Clear();
Check(SameTextAndDescriptions(Actual(provider, folder, "go", "v", "wn30:v:00000001", descriptions), descriptions, "gone", "went"), "broken optional metadata does not disable English forms");
Check(Error(provider).IndexOf("metadanych", StringComparison.Ordinal) >= 0, "broken optional metadata gives explicit provider warning");
}

static void RealAgidTests(Type provider, string folder) {
List<string[]> exceptions = Rows(Path.Combine(folder, "wordnet-en-exceptions.txt"), 3);
List<string[]> metadata = Rows(Path.Combine(folder, "wordnet-en-form-metadata.txt"), 5);
Check(exceptions.Count == 3485, "real Princeton exception resource keeps 3485 source rows");
Check(metadata.Count == 2929, "real AGID metadata covers 2929 source exception triples");
string[] lemmas = { "go", "good", "be" };
string[] parts = { "v", "a", "v" };
string[][] expected = {
new string[] { "went", "gone" },
new string[] { "better", "best" },
new string[] { "am", "are", "is", "was", "were", "been" }
};
for (int i = 0; i < lemmas.Length; i++) {
List<string> descriptions = new List<string>();
List<string> actual = Actual(provider, folder, lemmas[i], parts[i], SenseFor(folder, lemmas[i], parts[i]), descriptions);
Check(SameTextAndDescriptions(actual, descriptions, expected[i]), "real AGID metadata orders " + lemmas[i] + " from its pinned tags");
Check(Error(provider).Length == 0, "real AGID metadata has no provider warning for " + lemmas[i]);
}
}

static void Baseline125Tests(Type provider, string folder) {
Check(!File.Exists(Path.Combine(folder, "wordnet-en-form-metadata.txt")), "baseline125 has no optional AGID metadata resource");
List<string[]> rows = Rows(Path.Combine(folder, "wordnet-en-exceptions.txt"), 3);
string[] lemmas = { "go", "good" };
string[] parts = { "v", "a" };
for (int i = 0; i < lemmas.Length; i++) {
List<string> expected = new List<string>();
foreach (string[] row in rows) if (row[1] == parts[i] && row[2] == lemmas[i]) expected.Add(row[0]);
expected.Sort(StringComparer.Ordinal);
List<string> descriptions = new List<string>();
List<string> actual = Actual(provider, folder, lemmas[i], parts[i], SenseFor(folder, lemmas[i], parts[i]), descriptions);
Check(SameTextAndDescriptions(actual, descriptions, expected.ToArray()), "baseline125 without metadata retains lexical order for " + lemmas[i]);
Check(Error(provider).Length == 0, "baseline125 " + lemmas[i] + " has no provider error");
}
}

public static int Main(string[] args) {
Console.OutputEncoding = new UTF8Encoding(false);
if (args.Length < 2 || args.Length > 3) { Console.WriteLine("Expected EdSharpNG.exe, fixture/Synonyms directory, optional baseline125."); return 2; }
Assembly assembly = Assembly.Load(File.ReadAllBytes(args[0]));
Type provider = assembly.GetType("FormySynonimow");
Check(provider != null, "FormySynonimow is available in the actual tested assembly");
if (provider != null) {
if (args.Length == 3 && args[2] == "baseline125") Baseline125Tests(provider, args[1]);
else if (args.Length == 3 && args[2] == "real-agid") RealAgidTests(provider, args[1]);
else FixtureTests(provider, args[1]);
}
Console.WriteLine("ASSERTIONS=" + assertions + "; FAILURES=" + failures);
return failures == 0 ? 0 : 1;
}
}
