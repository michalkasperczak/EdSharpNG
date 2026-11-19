// DialogRozwijaniaSnippetow.cs - wybór ręcznie rozwijanego snippetu z podglądem.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EdSharp {

public static class DialogRozwijaniaSnippetow {
sealed class Pozycja {
public string Sciezka;
public string WyswietlanaNazwa;
public Pozycja(string sciezka, string wyswietlanaNazwa) { Sciezka = sciezka; WyswietlanaNazwa = wyswietlanaNazwa; }
public override string ToString() { return WyswietlanaNazwa; }
} // Pozycja class

static string NazwaPliku(string sciezka) {
try { return Path.GetFileName(sciezka); }
catch { return sciezka ?? ""; }
} // NazwaPliku method

static string Podglad(string sciezka) {
if (String.IsNullOrEmpty(sciezka) || !File.Exists(sciezka)) return JezykInterfejsu.Wybierz("Snippet file is no longer available.", "Plik fragmentu nie jest już dostępny.");
try { return Util.File2String(sciezka); }
catch { return JezykInterfejsu.Wybierz("Could not read this snippet.", "Nie można odczytać tego fragmentu."); }
} // Podglad method

// Zwraca pełną ścieżkę dokładnie tej pozycji, którą wskazuje lista; pusty wynik oznacza Escape/anulowanie.
public static string Pick(IWin32Window owner, IList<string> sciezki) {
if (sciezki == null || sciezki.Count == 0) return "";
List<Pozycja> pozycje = new List<Pozycja>();
Dictionary<string, int> liczbaNazw = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
foreach (string sciezka in sciezki) {
if (String.IsNullOrEmpty(sciezka)) continue;
string nazwa = NazwaPliku(sciezka);
if (!liczbaNazw.ContainsKey(nazwa)) liczbaNazw[nazwa] = 0;
liczbaNazw[nazwa]++;
pozycje.Add(new Pozycja(sciezka, nazwa));
}
if (pozycje.Count == 0) return "";
foreach (Pozycja pozycja in pozycje) {
if (liczbaNazw[pozycja.WyswietlanaNazwa] > 1) {
try { pozycja.WyswietlanaNazwa += " (" + Path.GetDirectoryName(pozycja.Sciezka) + ")"; }
catch {}
}
}

using (Form dialog = new Form()) {
dialog.Text = "Expand Snippet";
dialog.AccessibleName = "Expand Snippet";
dialog.StartPosition = FormStartPosition.CenterParent;
dialog.FormBorderStyle = FormBorderStyle.Sizable;
dialog.MinimizeBox = false;
dialog.MaximizeBox = false;
dialog.ShowInTaskbar = false;
dialog.KeyPreview = true;
dialog.ClientSize = new Size(640, 430);
dialog.MinimumSize = new Size(480, 300);

Label opis = new Label();
opis.Text = "Matching snippets: " + pozycje.Count + ". Use Up and Down to choose, Tab for preview, Enter to insert, Escape to cancel.";
opis.AutoSize = false;
opis.Height = 34;
opis.Dock = DockStyle.Top;
opis.Padding = new Padding(10, 8, 10, 2);

ListBox lista = new ListBox();
lista.Dock = DockStyle.Top;
lista.Height = 155;
lista.SelectionMode = SelectionMode.One;
lista.IntegralHeight = false;
lista.TabIndex = 0;
lista.AccessibleName = "Matching snippets, " + pozycje.Count + " results";
lista.AccessibleDescription = JezykInterfejsu.Wybierz("Up and Down choose a snippet. Tab moves to its read-only preview. Enter inserts. Escape cancels.", "Strzałki w górę i w dół wybierają fragment. Tab przechodzi do podglądu tylko do odczytu. Enter wstawia. Escape anuluje.");
for (int i = 0; i < pozycje.Count; i++) {
pozycje[i].WyswietlanaNazwa += ", " + (i + 1) + " of " + pozycje.Count;
lista.Items.Add(pozycje[i]);
}
lista.SelectedIndex = 0;

Label etykietaPodgladu = new Label();
etykietaPodgladu.Text = "Preview (read-only):";
etykietaPodgladu.AutoSize = false;
etykietaPodgladu.Height = 24;
etykietaPodgladu.Dock = DockStyle.Top;
etykietaPodgladu.Padding = new Padding(10, 6, 10, 0);

TextBox podglad = new TextBox();
podglad.Dock = DockStyle.Fill;
podglad.Multiline = true;
podglad.ReadOnly = true;
podglad.ScrollBars = ScrollBars.Both;
podglad.WordWrap = false;
podglad.AcceptsTab = false;
podglad.TabIndex = 1;
podglad.AccessibleName = "Snippet preview, read-only";
podglad.AccessibleDescription = JezykInterfejsu.Wybierz("Full text of the selected snippet. Enter inserts the selected snippet. Escape cancels.", "Pełny tekst wybranego fragmentu. Enter wstawia wybrany fragment. Escape anuluje.");

FlowLayoutPanel przyciski = new FlowLayoutPanel();
przyciski.Dock = DockStyle.Bottom;
przyciski.Height = 42;
przyciski.FlowDirection = FlowDirection.RightToLeft;
przyciski.Padding = new Padding(6);

Button anuluj = new Button();
anuluj.Text = "Cancel";
anuluj.DialogResult = DialogResult.Cancel;
anuluj.AutoSize = true;
anuluj.TabIndex = 3;
Button wstaw = new Button();
wstaw.Text = "&Insert";
wstaw.AutoSize = true;
wstaw.TabIndex = 2;

przyciski.Controls.Add(anuluj);
przyciski.Controls.Add(wstaw);
dialog.Controls.Add(podglad);
dialog.Controls.Add(etykietaPodgladu);
dialog.Controls.Add(lista);
dialog.Controls.Add(opis);
dialog.Controls.Add(przyciski);
dialog.AcceptButton = wstaw;
dialog.CancelButton = anuluj;

Action odswiezPodglad = delegate {
Pozycja pozycja = lista.SelectedItem as Pozycja;
podglad.Text = pozycja == null ? "" : Podglad(pozycja.Sciezka);
podglad.SelectionStart = 0;
podglad.SelectionLength = 0;
};
lista.SelectedIndexChanged += delegate { odswiezPodglad(); };
odswiezPodglad();

Action zatwierdz = delegate {
Pozycja pozycja = lista.SelectedItem as Pozycja;
if (pozycja == null) return;
if (!File.Exists(pozycja.Sciezka)) {
odswiezPodglad();
Util.Say(podglad.Text);
return;
}
dialog.DialogResult = DialogResult.OK;
dialog.Close();
};
wstaw.Click += delegate { zatwierdz(); };
dialog.KeyDown += delegate(object sender, KeyEventArgs e) {
if (e.KeyData == Keys.Enter) { zatwierdz(); e.Handled = true; e.SuppressKeyPress = true; }
else if (e.KeyData == Keys.Escape) { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); e.Handled = true; e.SuppressKeyPress = true; }
};

if (dialog.ShowDialog(owner) != DialogResult.OK) return "";
Pozycja wybrana = lista.SelectedItem as Pozycja;
return (wybrana != null && File.Exists(wybrana.Sciezka)) ? wybrana.Sciezka : "";
}
} // Pick method
} // DialogRozwijaniaSnippetow class
} // EdSharp namespace
