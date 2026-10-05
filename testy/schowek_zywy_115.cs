// ZYWY POMIAR SCHOWKA I TOZSAMOSCI NA PRAWDZIWYM PULPICIE (5.0.115).
//
// CZYM TO SIE ROZNI OD harness_tozsamosc_importu.cs.  Tamten pomiar laduje
// binarke przez refleksje i czyta WARTOSC, ktora komenda kopiowania sciezki
// bierze (child.IdentityPath).  TUTAJ nie ma refleksji do logiki: uruchamiamy
// PRAWDZIWY proces EdSharpNG.exe, fizyczny Alt+Shift+P idzie przez NVDA, a
// wynik odczytujemy z SYSTEMOWEGO schowka Windows i wklejamy do wlasnego
// bufora.  Dopiero to jest dowodem, ze niewidomy uzytkownik dostanie sciezke.
//
// OCHRONA ZASTANEJ ZAWARTOSCI SCHOWKA.  Schowek nalezy do uzytkownika, nie do
// pomiaru.  Robimy kopie WSZYSTKICH formatow przed pomiarem i oddajemy ja na
// koncu, w tej samej kolejnosci (SetDataObject z pelnym DataObject), zeby nie
// zgubic ani jednego formatu.  Zastanej TRESCI NIE LOGUJEMY - to prywatne dane.
// Logujemy wylacznie nazwy formatow i dlugosci, bo tyle wystarczy do kontroli
// poprawnosci przywrocenia.
//
// ZAJETY SCHOWEK.  Nowy komunikat "Clipboard is busy, path not copied!" testujemy
// KROTKIM, kontrolowanym OpenClipboard we WLASNYM procesie, z wlasnym finally -
// zeby po pomiarze schowek NA PEWNO wrocil do uzytku.  Bez tego blokada
// przezylaby test i uderzyla w uzytkownika.
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class SchowekZywy {
[DllImport("user32.dll", SetLastError = true)] static extern bool OpenClipboard(IntPtr hWnd);
[DllImport("user32.dll", SetLastError = true)] static extern bool CloseClipboard();

static int pass, fail;
static List<string> failed = new List<string>();
static void Check(bool b, string label) {
Console.WriteLine((b ? "PASS " : "FAIL ") + label);
if (b) pass++; else { fail++; failed.Add(label); }
}
static void Info(string s) { Console.WriteLine("INFO " + s); }

// KOPIA WSZYSTKICH FORMATOW.  GetDataObject zwraca zywy obiekt zrodla; po
// zamknieciu procesu-wlasciciela dane znikaja, wiec kopiujemy je do wlasnego
// DataObject TERAZ, format po formacie.
static DataObject Kopia(out string sOpis) {
StringBuilder sb = new StringBuilder();
DataObject kopia = new DataObject();
try {
IDataObject zrodlo = Clipboard.GetDataObject();
if (zrodlo == null) { sOpis = "schowek pusty"; return null; }
string[] aFormaty = zrodlo.GetFormats(false);
int iWziete = 0;
foreach (string f in aFormaty) {
try {
object dane = zrodlo.GetData(f, false);
if (dane == null) continue;
kopia.SetData(f, dane);
iWziete++;
// NAZWA I ROZMIAR, NIGDY TRESC.
string sRozmiar = (dane is string) ? ((string) dane).Length + " znakow" : dane.GetType().Name;
sb.Append(f + "(" + sRozmiar + ") ");
} catch {}
}
sOpis = iWziete + " z " + aFormaty.Length + " formatow: " + sb.ToString().Trim();
return iWziete > 0 ? kopia : null;
}
catch (Exception ex) { sOpis = "nie udalo sie odczytac: " + ex.Message; return null; }
}

static void Oddaj(DataObject kopia) {
if (kopia == null) return;
for (int i = 0; i < 10; i++) {
try { Clipboard.SetDataObject(kopia, true, 10, 100); return; }
catch { Thread.Sleep(120); }
}
Console.WriteLine("UWAGA: nie udalo sie oddac zastanej zawartosci schowka");
}

static string Tekst() {
for (int i = 0; i < 20; i++) {
try { return Clipboard.ContainsText() ? Clipboard.GetText(TextDataFormat.UnicodeText) : ""; }
catch { Thread.Sleep(100); }
}
return "";
}

[STAThread] static int Main(string[] args) {
// args[0] = tryb
string tryb = args.Length > 0 ? args[0] : "";

if (tryb == "backup") {
// Kopia do pliku: proces pomiaru konczy sie miedzy etapami, wiec
// zawartosc musi przezyc poza pamiecia. Zapisujemy TYLKO tekst i
// liste formatow - binarnych formatow nie da sie przeniesc przez plik
// bez ryzyka, wiec je JAWNIE meldujemy jako nieprzeniesione.
string sOpis;
DataObject k = Kopia(out sOpis);
Info("zastany schowek: " + sOpis);
string sTekst = Tekst();
File.WriteAllText(args[1], sTekst, new UTF8Encoding(false));
File.WriteAllText(args[1] + ".formaty", sOpis, new UTF8Encoding(false));
Info("zapisano kopie tekstowa (" + sTekst.Length + " znakow) do " + Path.GetFileName(args[1]));
return 0;
}

if (tryb == "restore") {
string sTekst = File.Exists(args[1]) ? File.ReadAllText(args[1], Encoding.UTF8) : "";
if (sTekst.Length == 0) {
// Pusta kopia: NIE czyscimy schowka, bo w miedzyczasie uzytkownik
// mogl tam cos wlozyc i skasowalibysmy mu to.
Info("kopia byla pusta - schowka nie ruszam (mogl zmienic sie w miedzyczasie)");
return 0;
}
for (int i = 0; i < 10; i++) {
try { Clipboard.SetText(sTekst, TextDataFormat.UnicodeText); Info("oddano zastany tekst (" + sTekst.Length + " znakow)"); return 0; }
catch { Thread.Sleep(120); }
}
Console.WriteLine("UWAGA: nie udalo sie oddac zastanej zawartosci");
return 1;
}

if (tryb == "read") {
// ODCZYT SCHOWKA PO Alt+Shift+P: dwie drogi, bo jedna moze klamac.
//   1) Clipboard.GetText  - czysty odczyt Windows Unicode,
//   2) WKLEJENIE do wlasnego pola tekstowego (Control+V, prawdziwa droga
//      uzytkownika) - to lapie przypadek, w ktorym schowek ma tekst, ale
//      nie da sie go wkleic.
string sOdczyt = Tekst();
Console.WriteLine("SCHOWEK_UNICODE=" + sOdczyt);

TextBox tb = new TextBox();
Form f = new Form();
f.Controls.Add(tb);
f.ShowInTaskbar = false;
f.WindowState = FormWindowState.Minimized;
f.Show();
tb.Focus();
tb.Clear();
tb.Paste();
Application.DoEvents();
string sWklejone = tb.Text;
f.Close(); f.Dispose();
Console.WriteLine("WKLEJONE=" + sWklejone);

// OCZEKIWANA SCIEZKA Z PLIKU UTF-8, NIE Z ARGUMENTU cmd.exe.  Pierwszy
// przebieg porownywal schowek z argumentem podanym przez cmd.exe i dostal
// dwa FAILe na sciezce "QA tożsamość Żółć\Artykuł z spacją.docx": konsola
// Windows przepuszcza argumenty w stronicy kodowej OEM, wiec polskie znaki
// docieraly do sondy POKRECONE.  Schowek byl poprawny (mowa NVDA pokazala
// pelna sciezke, a plik ze schowka ISTNIAL na dysku) - klamalo porownanie.
// To byl blad pomiaru, nie produktu, dlatego oczekiwana wartosc czytamy
// teraz z pliku zapisanego w UTF-8, ktory nie przechodzi przez powloke.
string sOczekiwana = args.Length > 1 ? args[1] : "";
if (sOczekiwana.Length > 0 && File.Exists(sOczekiwana) && sOczekiwana.EndsWith(".oczekiwana", StringComparison.OrdinalIgnoreCase))
sOczekiwana = File.ReadAllText(sOczekiwana, Encoding.UTF8).Trim();
if (sOczekiwana.Length > 0) {
Check(sOdczyt == sOczekiwana, "schowek Windows zawiera DOKLADNIE pelna sciezke zrodla");
Check(sWklejone == sOczekiwana, "wklejenie Control+V do wlasnego bufora daje te sama pelna sciezke");
Check(sOdczyt.Contains("\\"), "sciezka ma separator katalogu (nie sama nazwa robocza)");
Check(!sOdczyt.EndsWith(".md"), "schowek NIE zawiera nazwy roboczej Markdowna");
Check(File.Exists(sOdczyt), "sciezka ze schowka wskazuje ISTNIEJACY plik na dysku");
}
Console.WriteLine("RESULT: " + pass + " PASS / " + fail + " FAIL");
return fail == 0 ? 0 : 1;
}

if (tryb == "zajmij") {
// BLOKADA NA ZADANA LICZBE MILISEKUND, z GWARANTOWANYM zwolnieniem.
// finally jest tu najwazniejsza linijka calego pliku: bez niej zajety
// schowek przezyje pomiar i uderzy w uzytkownika.
int ms = Int32.Parse(args[1]);
bool bOtwarty = false;
try {
for (int i = 0; i < 50 && !bOtwarty; i++) {
bOtwarty = OpenClipboard(IntPtr.Zero);
if (!bOtwarty) Thread.Sleep(50);
}
if (!bOtwarty) { Console.WriteLine("BLAD: nie udalo sie zajac schowka"); return 2; }
// ZNACZNIKI CZASU, BO INACZEJ NIE DA SIE DOWIESC NAKLADANIA SIE OKIEN.
// Pierwsze dwa przebiegi oblaly sie wlasnie na tym: blokada wygasala,
// zanim fizyczny Alt+Shift+P doszedl przez mostek NVDA (kazda tura
// narzedzia to kilka sekund), wiec produkt kopiowal sciezke POPRAWNIE i
// komunikat o zajetym schowku nie mial prawa sie pojawic.  Bez czasow w
// logu wyglada to jak brak komunikatu w produkcie, a nie jak zle
// zestrojony pomiar.  Czas UTC w ISO 8601 i z milisekundami.
Console.WriteLine("ZAJETY " + DateTime.UtcNow.ToString("o"));
Console.Out.Flush();
Thread.Sleep(ms);
return 0;
}
finally {
if (bOtwarty) { CloseClipboard(); Console.WriteLine("ZWOLNIONY " + DateTime.UtcNow.ToString("o")); }
}
}

Console.WriteLine("Tryby: backup <plik> | restore <plik> | read [oczekiwana] | zajmij <ms>");
return 2;
}
}
