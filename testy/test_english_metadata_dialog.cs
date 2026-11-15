using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using EdSharp;
class TestEnglishMetadataDialog {
static int count, failed;
static void Check(bool ok, string name) { count++; if (!ok) failed++; Console.WriteLine((ok ? "PASS " : "FAIL ") + name); }
static IEnumerable<Control> All(Control root) { foreach (Control c in root.Controls) { yield return c; foreach (Control d in All(c)) yield return d; } }
[STAThread] static int Main(string[] args) {
Application.EnableVisualStyles(); App.BomDictionary = Util.GetBomDictionary(); App.ExtraSpeech = false;
string dir = args[0]; Directory.CreateDirectory(dir); App.SpeechLog = Path.Combine(dir, "speech.log");
File.WriteAllText(Path.Combine(dir,"wordnet-en-synonyms.txt"), "good\ta\twn30:a:00000001\tfine\n");
File.WriteAllText(Path.Combine(dir,"wordnet-en-exceptions.txt"), "best\ta\tgood\nbetter\ta\tgood\n");
File.WriteAllText(Path.Combine(dir,"wordnet-en-form-metadata.txt"), "best\ta\tgood\tJJS\tAGID2016.01.19:1\nbetter\ta\tgood\tJJR\tAGID2016.01.19:1\n");
File.WriteAllText(Path.Combine(dir,"wordnet-en-form-metadata-manifest.json"), "{}");
var timer = new Timer(); timer.Interval = 100; timer.Tick += delegate {
Form f = Application.OpenForms.Cast<Form>().FirstOrDefault(x => x.Text == "Odmiany"); if (f == null) return; timer.Stop();
ListBox list = All(f).OfType<ListBox>().FirstOrDefault(); TextBox warning = All(f).OfType<TextBox>().FirstOrDefault();
Check(list != null && list.Items.Count == 2, "optional metadata warning does not hide source forms");
Check(warning != null && warning.ReadOnly && warning.Text.Contains("metadanych"), "warning remains readonly and available");
Check(warning != null && warning.Focused, "warning receives initial focus for screen reader");
Button insert = All(f).OfType<Button>().FirstOrDefault(x => x.Text == "&Wstaw");
if (insert != null) insert.PerformClick(); else { f.DialogResult = DialogResult.Cancel; f.Close(); }
}; timer.Start();
string selected = EdSharp.Dialog.PickSynonymForm(null, dir, "good", "a", "wn30:a:00000001"); timer.Dispose();
Check(selected == "best", "can still insert first source form in lexical fallback order");
Console.WriteLine("ASSERTIONS=" + count + "; FAILURES=" + failed); return failed == 0 ? 0 : 1;
}
}
