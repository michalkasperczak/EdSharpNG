using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

public static class TestSynonimyCoverage {
static int Count(List<Synonimy.Grupa> groups) { int n = 0; foreach (Synonimy.Grupa g in groups) n += g.Synonyms.Count; return n; }
static bool Wanted(string part, string tag) {
if (part == "n") return tag.StartsWith("subst:pl:inst:") || tag.StartsWith("subst:sg:inst:");
if (part == "a") return tag.StartsWith("adj:sg:gen:") || tag.StartsWith("adj:pl:inst:");
if (part == "v") return tag.StartsWith("fin:sg:pri:") || tag.StartsWith("fin:sg:ter:") || tag.StartsWith("praet:sg:");
return part == "r" && tag == "adv:com";
}
public static int Main(string[] args) {
Console.OutputEncoding = new UTF8Encoding(false);
if (args.Length != 3) return 2;
string folder = args[0], baseline = args[1], report = args[2];
string dictionary = Path.Combine(folder, "plwordnet-synonyms.txt"), forms = Path.Combine(folder, "plwordnet-forms.bin");
if (!Synonimy.Load(dictionary)) return 2;
HashSet<string> literal = new HashSet<string>(StringComparer.Ordinal);
foreach (string line in File.ReadLines(dictionary, Encoding.UTF8)) if (line.Length > 0 && !line.StartsWith("#")) literal.Add(line.Split('\t')[0]);
int lemmas = 0, hits = 0, selected = 0, formHits = 0;
HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
List<long> times = new List<long>();
using (StreamWriter lemmaOutput = new StreamWriter(report + ".lemmas.tsv", false, new UTF8Encoding(false)))
using (StreamWriter output = new StreamWriter(report, false, new UTF8Encoding(false))) {
lemmaOutput.WriteLine("lemma\tsynonyms\tliteral");
output.WriteLine("lemma\tform\tsynonyms\tlookup_ms\tvalues");
foreach (string line in File.ReadLines(baseline, Encoding.UTF8)) {
string lemma = line.Trim();
if (lemma.Length == 0 || lemma.StartsWith("#")) continue;
lemmas++; int lemmaCount = Count(Synonimy.Find(lemma)); if (lemmaCount > 0) hits++;
lemmaOutput.WriteLine(lemma + "\t" + lemmaCount + "\t" + literal.Contains(lemma));
List<string[]> analyses = SynonimyOdmiany.Lookup(forms, "l:" + lemma);
analyses.Sort(delegate(string[] a, string[] b) { int c = String.CompareOrdinal(a[0], b[0]); return c != 0 ? c : String.CompareOrdinal(a[3], b[3]); });
int perLemma = 0;
foreach (string[] row in analyses) {
string form = row[0];
if (!Wanted(row[1], row[3]) || form == lemma || literal.Contains(form) || !seen.Add(form)) continue;
Stopwatch timer = Stopwatch.StartNew(); List<Synonimy.Grupa> result = Synonimy.Find(form); timer.Stop();
if (Synonimy.FormsError.Length > 0) { Console.WriteLine(Synonimy.FormsError); return 1; }
int count = Count(result); selected++; if (count > 0) formHits++; times.Add(timer.ElapsedMilliseconds);
List<string> values = new List<string>(); foreach (Synonimy.Grupa g in result) foreach (string value in g.Synonyms) values.Add(value);
output.WriteLine(lemma + "\t" + form + "\t" + count + "\t" + timer.ElapsedMilliseconds + "\t" + String.Join("|", values.ToArray()));
if (++perLemma == 2) break;
}
}
}
times.Sort();
Console.WriteLine("LEMMAS=" + lemmas + " LEMMA_HITS=" + hits + " FORMS=" + selected + " FORM_HITS=" + formHits);
Console.WriteLine("MEDIAN_MS=" + (times.Count > 0 ? times[times.Count / 2] : 0) + " MAX_MS=" + (times.Count > 0 ? times[times.Count - 1] : 0));
Console.WriteLine("PEAK_WORKING_SET_BYTES=" + Process.GetCurrentProcess().PeakWorkingSet64);
string[,] checks = { {"domami", "chałupami"}, {"ważnego", "istotnego"}, {"sprawdził", "skontrolował"}, {"mam", "posiadam"}, {"zamkiem", "suwakiem"}, {"szybciej", "prędzej"} };
int failures = 0;
for (int i = 0; i < checks.GetLength(0); i++) {
bool found = false; foreach (Synonimy.Grupa group in Synonimy.Find(checks[i,0])) if (group.Synonyms.Contains(checks[i,1])) found = true;
Console.WriteLine((found ? "PASS " : "FAIL ") + checks[i,0] + " -> " + checks[i,1]); if (!found) failures++;
}
if (lemmas < 100 || selected < 100) failures++;
Console.WriteLine("FAILURES=" + failures); return failures == 0 ? 0 : 1;
}
}
