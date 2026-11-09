using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

public static class TestSynonimyRealForms {
public static int Main(string[] args) {
Console.OutputEncoding = new UTF8Encoding(false);
if (args.Length != 1) return 2;
string folder = args[0];
Stopwatch timer = Stopwatch.StartNew();
if (!Synonimy.Load(Path.Combine(folder, "plwordnet-synonyms.txt"))) { Console.WriteLine("LOAD_ERROR\t" + Synonimy.Error); return 1; }
Console.WriteLine("LOAD_MS\t" + timer.ElapsedMilliseconds);
string[] queries = { "dom", "domami", "ważny", "ważnego", "sprawdzić", "sprawdziłem", "sprawdził", "mam", "zamkiem", "dobrego", "pięknego", "ładnymi", "samochodami", "szybciej", "lepiej", "czytaliśmy", "radości", "strachu", "nadziei", "prostego", "głośnego", "kotami", "kobietami", "pracowaliśmy", "zrobiłem", "zznieistniejacezz" };
foreach (string query in queries) {
timer.Restart();
List<Synonimy.Grupa> groups = Synonimy.Find(query);
long elapsed = timer.ElapsedMilliseconds;
List<string> values = new List<string>();
foreach (Synonimy.Grupa group in groups) foreach (string value in group.Synonyms) values.Add(value);
Console.WriteLine("RESULT\t" + query + "\t" + values.Count + "\t" + elapsed + "\t" + String.Join("|", values.ToArray()));
if (Synonimy.FormsError.Length > 0) { Console.WriteLine("FORMS_ERROR\t" + Synonimy.FormsError); return 1; }
if (query == "domami" || query == "ważnego" || query == "sprawdziłem" || query == "sprawdził" || query == "mam") {
List<string[]> analyses = SynonimyOdmiany.Lookup(Path.Combine(folder, "plwordnet-forms.bin"), "f:" + query);
foreach (string[] analysis in analyses) Console.WriteLine("ANALYSIS\t" + query + "\t" + String.Join("\t", analysis));
}
}
return 0;
}
}
