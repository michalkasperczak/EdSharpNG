// Executable tests for source-attested inflection choices; no generated endings.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

public static class TestFormySynonimow {
static int failures;
const string SenseA = "0123456789abcdef0123456789abcdef";
const string SenseB = "fedcba9876543210fedcba9876543210";
static void Check(bool value, string name) { Console.WriteLine((value ? "PASS " : "FAIL ") + name); if (!value) failures++; }
static ulong Hash(string value) { ulong hash = 14695981039346656037UL; foreach (byte b in Encoding.UTF8.GetBytes(value)) { hash ^= b; hash = unchecked(hash * 1099511628211UL); } return hash; }
static void WriteText(BinaryWriter writer, string value) { byte[] bytes = Encoding.UTF8.GetBytes(value); writer.Write(bytes.Length); writer.Write(bytes); }

// Same EDSYNF01 fixture shape used by test_synonimy_odmiany.cs.
static void Fixture(string path, Dictionary<string, string> records) {
List<KeyValuePair<ulong, ulong>> directory = new List<KeyValuePair<ulong, ulong>>();
using (FileStream stream = File.Create(path))
using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8)) {
writer.Write(Encoding.ASCII.GetBytes("EDSYNF01")); writer.Write((uint)records.Count); writer.Write((ulong)0); writer.Write((ulong)0);
foreach (KeyValuePair<string, string> record in records) { directory.Add(new KeyValuePair<ulong, ulong>(Hash(record.Key), (ulong)stream.Position)); WriteText(writer, record.Key); WriteText(writer, record.Value); }
ulong directoryOffset = (ulong)stream.Position; directory.Sort(delegate(KeyValuePair<ulong, ulong> a, KeyValuePair<ulong, ulong> b) { return a.Key.CompareTo(b.Key); });
foreach (KeyValuePair<ulong, ulong> item in directory) { writer.Write(item.Key); writer.Write(item.Value); }
ulong length = (ulong)stream.Length; stream.Position = 12; writer.Write(directoryOffset); writer.Write(length);
}
}

static object Invoke(Type provider, string folder, string word, string pos, string sense) { return provider.GetMethod("GetForms").Invoke(null, new object[] { folder, word, pos, sense }); }
static string Error(Type provider) { return (string)provider.GetField("Error").GetValue(null); }
static List<string> Texts(object list) { List<string> result = new List<string>(); foreach (object item in (System.Collections.IEnumerable)list) result.Add((string)item.GetType().GetField("Tekst").GetValue(item)); return result; }
static List<string> Descriptions(object list) { List<string> result = new List<string>(); foreach (object item in (System.Collections.IEnumerable)list) result.Add((string)item.GetType().GetField("Opis").GetValue(item)); return result; }
static bool Contains(List<string> values, string expected) { foreach (string value in values) if (value == expected) return true; return false; }
static bool ContainsFragment(List<string> values, string expected) { foreach (string value in values) if (value.IndexOf(expected, StringComparison.Ordinal) >= 0) return true; return false; }
static int Count(object list) { int result = 0; foreach (object ignored in (System.Collections.IEnumerable)list) result++; return result; }

public static int Main(string[] args) {
Console.OutputEncoding = new UTF8Encoding(false);
if (args.Length != 2) { Console.WriteLine("Expected synthetic and real Synonyms directories."); return 2; }
Directory.CreateDirectory(args[0]);
Type provider = typeof(Synonimy).Assembly.GetType("FormySynonimow");
Check(provider != null, "public source-attested forms provider exists");
if (provider != null) {
Check(provider.GetNestedType("Forma") != null && provider.GetField("Error") != null && provider.GetMethod("GetForms") != null, "required public forms API exists");
var records = new Dictionary<string, string>();
records.Add("f:chałupami", "chałupa\tn\t" + SenseA + "\tsubst:pl:inst:f\n");
records.Add("l:chałupa", "chałupa\tn\t" + SenseA + "\tsubst:sg:nom:f\nchałupami\tn\t" + SenseA + "\tsubst:pl:inst:f\n");
records.Add("f:zamek", "zamek\tn\t" + SenseA + "\tsubst:sg:nom:m3\nzamek\tn\t" + SenseB + "\tsubst:sg:nom:m3\n");
records.Add("l:zamek", "zamkiem\tn\t" + SenseA + "\tsubst:sg:inst:m3\nzamka\tn\t" + SenseB + "\tsubst:sg:gen:m3\n");
Fixture(Path.Combine(args[0], "plwordnet-forms.bin"), records);
object forms = Invoke(provider, args[0], "chałupami", "n", SenseA);
List<string> texts = Texts(forms), descriptions = Descriptions(forms);
Check(Count(forms) == 2 && Contains(texts, "chałupa") && Contains(texts, "chałupami"), "inflected selected Polish synonym resolves only its attested lexical lemma forms");
Check(ContainsFragment(descriptions, "liczba") && !ContainsFragment(descriptions, "subst:"), "Polish descriptions use grammar names rather than raw SGJP tags");
records["l:chałupa"] += "chałupy\tn\t" + SenseA + "\tsubst:pl:nom.acc.voc:f\n";
Fixture(Path.Combine(args[0], "plwordnet-forms.bin"), records);
descriptions = Descriptions(Invoke(provider, args[0], "chałupami", "n", SenseA));
Check(ContainsFragment(descriptions, "mianownik, biernik, wołacz"), "compressed source cases are preserved as readable Polish names");
Check(Count(Invoke(provider, args[0], "chałupami", "n", SenseB)) == 0 && Error(provider).Length == 0, "wrong Polish source sense has no forms");
Check(Count(Invoke(provider, args[0], "chałupami", "v", SenseA)) == 0 && Error(provider).Length == 0, "wrong Polish part of speech has no forms");
forms = Invoke(provider, args[0], "zamek", "n", SenseA); Check(Contains(Texts(forms), "zamkiem") && !Contains(Texts(forms), "zamka"), "homonym does not borrow forms from another exact source sense");
forms = Invoke(provider, args[0], "zamek", "n", SenseB); Check(Contains(Texts(forms), "zamka") && !Contains(Texts(forms), "zamkiem"), "other homonym keeps its own exact source sense forms");
File.WriteAllBytes(Path.Combine(args[0], "plwordnet-forms.bin"), new byte[] { 0, 1, 2 });
Check(Count(Invoke(provider, args[0], "chałupami", "n", SenseA)) == 0 && Error(provider).Length > 0, "corrupt optional Polish forms resource has explicit isolated error");
File.WriteAllText(Path.Combine(args[0], "plwordnet-synonyms.txt"), "dom\tn\t" + SenseA + "\tchałupa\n", new UTF8Encoding(false));
Check(Synonimy.Load(Path.Combine(args[0], "plwordnet-synonyms.txt")) && Synonimy.Find("dom").Count == 1, "corrupt forms resource does not disable ordinary synonym lookup");
File.Delete(Path.Combine(args[0], "plwordnet-forms.bin"));
Check(Count(Invoke(provider, args[0], "chałupami", "n", SenseA)) == 0 && Error(provider).Length > 0, "missing optional Polish forms resource has explicit isolated error");
Fixture(Path.Combine(args[0], "plwordnet-forms.bin"), records);
File.WriteAllText(Path.Combine(args[0], "wordnet-en-synonyms.txt"), "go\tv\twn30:v:00000001\tmove|travel\ngood\ta\twn30:a:00000001\tfine\n", new UTF8Encoding(false));
File.WriteAllText(Path.Combine(args[0], "wordnet-en-exceptions.txt"), "went\tv\tgo\ngone\tv\tgo\n", new UTF8Encoding(false));
forms = Invoke(provider, args[0], "went", "v", "wn30:v:00000001"); texts = Texts(forms); descriptions = Descriptions(forms);
Check(Contains(texts, "went") && Contains(texts, "gone") && !Contains(texts, "goed"), "English returns only attested exception forms and never invents goed");
Check(ContainsFragment(descriptions, "zestaw wyjątków") && ContainsFragment(descriptions, "nie pełny paradygmat"), "English exception description is honest about incomplete grammar scope");
Check(Count(Invoke(provider, args[0], "good", "a", "wn30:a:00000001")) == 0 && Error(provider).Length == 0, "English source lemma without attested exception variants has an empty forms list");
Check(Count(Invoke(provider, args[0], "went", "n", "wn30:v:00000001")) == 0 && Error(provider).Length == 0, "English does not cross part of speech");
Check(Count(Invoke(provider, args[0], "went", "v", "wn30:v:00000002")) == 0 && Error(provider).Length == 0, "English does not cross source sense");
File.WriteAllText(Path.Combine(args[0], "wordnet-en-exceptions.txt"), "broken\n", new UTF8Encoding(false));
Check(Count(Invoke(provider, args[0], "went", "v", "wn30:v:00000001")) == 0 && Error(provider).Length > 0, "corrupt optional English exceptions resource has explicit isolated error");

string[] ordinary = { "dom", "domami", "ważny", "ważnego", "sprawdzić", "sprawdziłem", "sprawdził", "mam", "zamek", "dobry", "dobrego", "piękny", "pięknego", "ładny", "ładnymi", "samochód", "samochodami", "szybko", "szybciej", "lepiej", "czytać", "czytaliśmy", "radość", "radości", "strach", "strachu", "nadzieja", "nadziei", "prosty", "prostego", "głośny", "głośnego", "kot", "kotami", "kobieta", "kobietami", "pracować", "pracowaliśmy", "zrobić", "zrobiłem", "bank", "światło", "lista", "pomoc", "dziecko", "człowiek", "miasto", "droga", "domowy" };
int realCases = 0, realWithForms = 0;
foreach (string word in ordinary) {
if (!Synonimy.Load(Path.Combine(args[1], "plwordnet-synonyms.txt"))) continue;
List<Synonimy.Grupa> groups = Synonimy.Find(word); if (groups.Count == 0 || groups[0].Synonyms.Count == 0) continue;
object realForms = Invoke(provider, args[1], groups[0].Synonyms[0], groups[0].PartOfSpeech, groups[0].SenseId);
if (Count(realForms) > 0) realWithForms++;
Check(Error(provider).Length == 0, "real Polish resource accepts exact selected source " + word); realCases++;
}
Check(realCases >= 30, "real resource exercised at least 30 ordinary Polish selections");
Console.WriteLine("PL_SELECTIONS=" + realCases + "; PL_WITH_FORMS=" + realWithForms);
string[] englishExceptions = { "went", "gone", "children", "mice", "better", "best", "was", "were", "did", "made", "ran", "saw" };
int englishCases = 0;
foreach (string form in englishExceptions) {
List<Synonimy.Grupa> groups = SlownikiSynonimow.Find(args[1], form, "en"); if (groups.Count == 0) continue;
Invoke(provider, args[1], form, groups[0].PartOfSpeech, groups[0].SenseId);
Check(Error(provider).Length == 0, "real English exception/source selection " + form); englishCases++;
}
Check(englishCases >= 6, "real resource exercised English exception selections");
}
Console.WriteLine("FAILURES=" + failures); return failures == 0 ? 0 : 1;
}
}
