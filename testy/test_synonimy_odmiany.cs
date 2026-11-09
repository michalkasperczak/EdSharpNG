// Wykonywalne testy binarnego indeksu odmiany; bez Pythona i SQL w runtime.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

public static class TestSynonimyOdmiany {
static int iFailures;
static void Check(bool bValue, string sName) {
Console.WriteLine((bValue ? "PASS " : "FAIL ") + sName);
if (!bValue) iFailures++;
} // Check method

static ulong Hash(string sValue) {
ulong hash = 14695981039346656037UL;
foreach (byte value in Encoding.UTF8.GetBytes(sValue)) { hash ^= value; hash = unchecked(hash * 1099511628211UL); }
return hash;
} // Hash method

static void WriteString(BinaryWriter writer, string value) {
byte[] data = Encoding.UTF8.GetBytes(value);
writer.Write(data.Length);
writer.Write(data);
} // WriteString method

static void Fixture(string sPath, Dictionary<string, string> records, bool compressed = false) {
List<KeyValuePair<ulong, ulong>> directory = new List<KeyValuePair<ulong, ulong>>();
using (FileStream stream = File.Create(sPath))
using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8)) {
writer.Write(Encoding.ASCII.GetBytes(compressed ? "EDSYNF02" : "EDSYNF01"));
writer.Write((uint) records.Count);
writer.Write((ulong) 0);
writer.Write((ulong) 0);
foreach (KeyValuePair<string, string> record in records) {
directory.Add(new KeyValuePair<ulong, ulong>(Hash(record.Key), (ulong) stream.Position));
WriteString(writer, record.Key);
if (compressed) {
byte[] original = Encoding.UTF8.GetBytes(record.Value);
byte[] packed;
using (MemoryStream memory = new MemoryStream()) {
using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true)) gzip.Write(original, 0, original.Length);
packed = memory.ToArray();
}
writer.Write(packed.Length);
writer.Write(original.Length);
writer.Write(packed);
}
else WriteString(writer, record.Value);
}
ulong offset = (ulong) stream.Position;
directory.Sort(delegate(KeyValuePair<ulong, ulong> a, KeyValuePair<ulong, ulong> b) { return a.Key.CompareTo(b.Key); });
foreach (KeyValuePair<ulong, ulong> record in directory) { writer.Write(record.Key); writer.Write(record.Value); }
ulong length = (ulong) stream.Length;
stream.Position = 12;
writer.Write(offset);
writer.Write(length);
}
} // Fixture method

public static int Main(string[] args) {
if (args.Length != 1) { Console.WriteLine("Expected explicit fixture directory."); return 2; }
Directory.CreateDirectory(args[0]);
Type reader = typeof(Synonimy).Assembly.GetType("SynonimyOdmiany");
Check(reader != null, "binary morphology reader exists");
if (reader != null) {
string path = Path.Combine(args[0], "forms-reader.bin");
Dictionary<string, string> records = new Dictionary<string, string>();
records.Add("f:domami", "dom\tn\t0123456789abcdef0123456789abcdef\tsubst:pl:inst:m3\n");
records.Add("l:chałupa", "chałupami\tn\t0123456789abcdef0123456789abcdef\tsubst:pl:inst:f\n");
records.Add("f:radości", "radość\tn\taaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\tsubst:sg:gen:f\n");
records.Add("l:uciecha", "uciechy\tn\tbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\tsubst:sg:gen:f\n");
Fixture(path, records);
MethodInfo lookup = reader.GetMethod("Lookup");
List<string[]> rows = (List<string[]>) lookup.Invoke(null, new object[] { path, "l:chałupa" });
Check(rows.Count == 1 && rows[0].Length == 4 && rows[0][0] == "chałupami" && rows[0][3] == "subst:pl:inst:f", "UTF8 keys and values match binary directory");
rows = (List<string[]>) lookup.Invoke(null, new object[] { path, "f:nieistniejące" });
Check(rows.Count == 0, "missing key has no analyses");
string compressedPath = Path.Combine(args[0], "forms-reader-v2.bin");
Fixture(compressedPath, records, true);
bool compressedPass = false;
try {
rows = (List<string[]>) lookup.Invoke(null, new object[] { compressedPath, "l:chałupa" });
compressedPass = rows.Count == 1 && rows[0][0] == "chałupami" && rows[0][3] == "subst:pl:inst:f";
}
catch (TargetInvocationException) { }
Check(compressedPass, "compressed v2 preserves exact UTF8 morphology rows");
string dictionaryPath = Path.Combine(args[0], "plwordnet-synonyms.txt");
File.WriteAllText(dictionaryPath, "dom\tn\t0123456789abcdef0123456789abcdef\tchałupa|lokum\nradość\tn\tomw-2.0:joy\tuciecha\n", Encoding.UTF8);
Fixture(Path.Combine(args[0], "plwordnet-forms.bin"), records);
Check(Synonimy.Load(dictionaryPath), "lemma dictionary loads beside morphology index");
List<Synonimy.Grupa> groups = Synonimy.Find("DOMAMI");
Check(groups.Count == 1 && groups[0].Synonyms.Count == 1 && groups[0].Synonyms[0] == "chałupami", "instrumental plural yields attested instrumental plural, not bare lemma");
Check(Synonimy.Find("dom")[0].Synonyms[0] == "chałupa", "literal lemma keeps existing lookup priority");
Check(Synonimy.Find("radość")[0].Synonyms[0] == "uciecha", "legacy fallback remains available for literal lemma");
Check(Synonimy.Find("radości").Count == 0, "legacy fallback never joins morphology through unrelated senses");
Check(SynonimyOdmiany.TagsMatch("a", "adj:sg:gen:m1.m2.m3.n:pos", "adj:sg:gen:m1.m2.m3.n:pos"), "adjective preserves number case gender and degree");
Check(!SynonimyOdmiany.TagsMatch("a", "adj:sg:gen:m1.m2.m3.n:pos", "adj:sg:nom:f:pos"), "adjective rejects different case or gender");
Check(SynonimyOdmiany.TagsMatch("v", "praet:sg:m1.m2.m3:perf", "praet:sg:m1.m2.m3:perf"), "verb keeps attested tense number gender and aspect");
Check(!SynonimyOdmiany.TagsMatch("v", "praet:sg:m1.m2.m3:perf", "praet:sg:m1.m2.m3:imperf"), "verb does not silently change aspect");
Check(SynonimyOdmiany.TagsMatch("r", "adv:com", "adv:com"), "adverb preserves comparative degree");
Check(!SynonimyOdmiany.TagsMatch("r", "adv:com", "adv:pos"), "adverb rejects different degree");
byte[] invalid = File.ReadAllBytes(compressedPath);
int valueOffset = 28 + 4 + BitConverter.ToInt32(invalid, 28);
Array.Copy(BitConverter.GetBytes(Int32.MaxValue), 0, invalid, valueOffset + 4, 4);
File.WriteAllBytes(compressedPath, invalid);
bool invalidRejected = false;
try { lookup.Invoke(null, new object[] { compressedPath, "f:domami" }); }
catch (TargetInvocationException ex) { invalidRejected = ex.InnerException is InvalidDataException; }
Check(invalidRejected, "oversized compressed declaration rejected before allocation");
File.WriteAllBytes(Path.Combine(args[0], "plwordnet-forms.bin"), invalid);
Check(Synonimy.Find("domami").Count == 0 && Synonimy.FormsError.Length > 0, "corrupt morphology returns explicit error without a replacement");
Check(Synonimy.Find("dom")[0].Synonyms[0] == "chałupa", "corrupt optional morphology does not break literal dictionary");
}
Console.WriteLine("FAILURES=" + iFailures);
return iFailures == 0 ? 0 : 1;
} // Main method
} // TestSynonimyOdmiany class
