// Real provider regression: valid TSV with changed tags must not be trusted.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

class TestEnglishMetadataIntegrity {
static int checks, failures;
static readonly UTF8Encoding utf8 = new UTF8Encoding(false);
static void Check(bool ok, string name) { checks++; Console.WriteLine((ok ? "PASS " : "FAIL ") + name); if (!ok) failures++; }
static string Forms(Type provider, string folder) {
List<string> words = new List<string>();
foreach (object form in (IEnumerable)provider.GetMethod("GetForms").Invoke(null, new object[] { folder, "go", "v", "wn30:v:00000001" })) words.Add((string)form.GetType().GetField("Tekst").GetValue(form));
return String.Join(",", words.ToArray());
}
static string Error(Type provider) { return (string)provider.GetField("Error").GetValue(null); }
public static int Main(string[] args) {
Console.OutputEncoding = utf8;
Type provider = Assembly.Load(File.ReadAllBytes(args[0])).GetType("FormySynonimow");
string folder = args[1]; Directory.CreateDirectory(folder);
File.WriteAllText(Path.Combine(folder, "wordnet-en-synonyms.txt"), "go\tv\twn30:v:00000001\tmove\n", utf8);
File.WriteAllText(Path.Combine(folder, "wordnet-en-exceptions.txt"), "gone\tv\tgo\nwent\tv\tgo\n", utf8);
string path = Path.Combine(folder, "wordnet-en-form-metadata.txt");
string manifest = Path.Combine(folder, "wordnet-en-form-metadata-manifest.json");
string valid = "gone\tv\tgo\tVBN\tAGID2016.01.19:1\nwent\tv\tgo\tVBD\tAGID2016.01.19:1\n";
File.WriteAllText(path, valid, utf8);
byte[] data = File.ReadAllBytes(path); string hash;
using (SHA256 sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
File.WriteAllText(manifest, "{\"outputs\":{\"metadata\":{\"bytes\":" + data.Length + ",\"sha256\":\"" + hash + "\"}}}", utf8);
Check(Forms(provider, folder) == "went,gone", "valid manifest allows source-proven ordering");
Check(Error(provider).Length == 0, "valid manifest does not warn");
File.WriteAllText(path, valid.Replace("gone\tv\tgo\tVBN", "gone\tv\tgo\tVBD").Replace("went\tv\tgo\tVBD", "went\tv\tgo\tVBN"), utf8);
Check(Forms(provider, folder) == "gone,went", "changed metadata leaves all original lexical forms available");
Check(Error(provider).IndexOf("metadanych", StringComparison.Ordinal) >= 0, "syntactically valid tag swap gives explicit integrity warning");
File.WriteAllText(path, valid, utf8);
Check(Forms(provider, folder) == "went,gone" && Error(provider).Length == 0, "repair is recognized on next request without restart");
string validManifest = File.ReadAllText(manifest, utf8);
string[] invalidManifests = { null, "not json", "{}", validManifest.Replace("\"bytes\":" + data.Length, "\"bytes\":1"), validManifest.Replace(hash, new string('0', 64)) };
foreach (string broken in invalidManifests) {
if (broken == null) File.Delete(manifest); else File.WriteAllText(manifest, broken, utf8);
Check(Forms(provider, folder) == "gone,went", "missing/malformed/incomplete/incorrect manifest retains all forms");
Check(Error(provider).IndexOf("metadanych", StringComparison.Ordinal) >= 0, "missing/malformed/incomplete/incorrect manifest warns");
}
File.WriteAllText(manifest, validManifest, utf8);
File.Delete(path);
Check(Forms(provider, folder) == "gone,went" && Error(provider).Length == 0, "missing optional metadata remains silent even with manifest present");
Console.WriteLine("ASSERTIONS=" + checks + "; FAILURES=" + failures);
return failures == 0 ? 0 : 1;
}
}
