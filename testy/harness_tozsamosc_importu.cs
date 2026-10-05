// POMIAR TOZSAMOSCI ZAIMPORTOWANEGO DOKUMENTU: historia ostatnich plikow i
// kopiowanie sciezki.
//
// CO MIERZY I DLACZEGO TAK.  Zgloszenie Michala (05.10.2026): otworzyl
// przygotowany artykul .docx, po zamknieciu NIE znalazl go na Alt+R; polecenie
// kopiowania sciezki tez nie dalo sciezki.  Diagnoza
// (docs/DIAGNOZA-QUILL-IMPORT-HISTORIA-2026-10-05.md) ustalila przyczyne: po
// imporcie child.File trzyma sama nazwe robocza Markdowna BEZ katalogu
// ("Artykul.md"), a wszystkie trzy miejsca, ktore mowia o pliku - handler
// zamkniecia MdiChild, SetRecent i menuMiscPathToClipboard - pytaja wlasnie o
// child.File i milczkiem odpadaja na warunku "brak ukosnika".
//
// Dlatego ten pomiar laduje PRAWDZIWA binarke przez refleksje i chodzi
// RZECZYWISTA droga otwarcia (MdiFrame.OpenOrActivateWindow z kluczem Import),
// a nie zadna atrapa konwertera.  Zrodla to dokumenty zbudowane naprawde,
// Pandokiem, przez MdiFrame.KonwertujDoPliku - tak samo jak robi to
// harness_integracja_oryginalu.cs.  Plik, ktorego nie da sie zbudowac dostepnym
// konwerterem, jest JAWNIE meldowany jako pominiety; nie udajemy jego formatu.
//
// CZEGO TEN POMIAR NIE ROBI.  Nie dotyka schowka systemowego.  Prywatny pulpit
// dzieli schowek z sesja uzytkownika, wiec fizyczny SetClipboardText/paste
// nalezy do osobnego etapu na widocznym pulpicie.  Tutaj mierzymy WARTOSC, ktora
// komenda kopiowania sciezki bierze (child.IdentityPath) - i nie nazywamy tego
// pomiarem schowka.
//
// Pomiar ma ROZNICOWAC stara binarke od nowej: na 5.0.114 punkty A/B/C dla
// formatow importowanych musza FAILowac (brak wpisu w historii, zla wartosc
// sciezki), a punkty kontrolne MD/TXT i bezpieczenstwo oryginalu PASSowac.
using System;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;

class ImportIdentityProbe {
static Assembly a; static Type app, ft, ct, util, zapis; static Form frame;
static int pass, fail; static List<string> failed = new List<string>();
static List<string> skipped = new List<string>();
static List<string> limits = new List<string>();

// Potwierdzenie okna "Restore Session".  Odzysk zmienionego dokumentu PYTA,
// wiec bez tego PrzywrocSesje stoi na modalnym oknie i pomiar nigdy nie wraca.
[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string cls, string name);
[DllImport("user32.dll")] static extern IntPtr GetDlgItem(IntPtr h, int id);
[DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
[DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
static bool ClickYes(Control c) {
Button b = c as Button;
if (b != null && b.Text.Replace("&", "") == "Yes") { b.PerformClick(); return true; }
foreach (Control x in c.Controls) if (ClickYes(x)) return true;
return false;
}
static void AcceptRecovery() {
foreach (Form f in Application.OpenForms) if (f != frame && f.Text == "Restore Session") { ClickYes(f); return; }
IntPtr h = FindWindow("#32770", "Restore Session"); uint pid; GetWindowThreadProcessId(h, out pid);
if (h != IntPtr.Zero && pid == (uint) System.Diagnostics.Process.GetCurrentProcess().Id)
PostMessage(GetDlgItem(h, 6), 0xF5, IntPtr.Zero, IntPtr.Zero);
}
// Odzysk z pytaniem: timer klika Yes, dopoki PrzywrocSesje nie wroci.
static int RestoreWithConfirm() {
Timer t = new Timer(); t.Interval = 100; t.Tick += delegate { AcceptRecovery(); }; t.Start();
try { return (int) Call(frame, "PrzywrocSesje"); }
finally { t.Stop(); t.Dispose(); }
}
// Granica pomiaru, nie zaliczenie: zapisujemy ja do raportu osobno.
static void Limit(string what) { Console.WriteLine("LIMIT " + what); limits.Add(what); }

static void Check(bool b, string label) {
Console.WriteLine((b ? "PASS " : "FAIL ") + label);
if (b) pass++; else { fail++; failed.Add(label); }
}
static void Skip(string label, string why) {
Console.WriteLine("SKIP " + label + " -- " + why);
skipped.Add(label + " (" + why + ")");
}
static void Set(string k, object v) { app.GetField(k).SetValue(null, v); }
static object Call(object obj, string name, params object[] args) {
return obj.GetType().InvokeMember(name, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, obj, args);
}
static object Static(Type t, string n, params object[] args) {
return t.InvokeMember(n, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, null, args);
}
static object Child() { return ft.GetProperty("Child").GetValue(frame, null); }
static RichTextBox Box() { return (RichTextBox) ct.GetField("RTB").GetValue(Child()); }
static string FileName() { return (string) ct.GetProperty("File").GetValue(Child(), null); }

// Tozsamosc czytana przez refleksje, bo STARA binarka tej wlasnosci NIE MA.
// Brak wlasnosci to nie wyjatek, tylko wynik: zwracamy child.File, czyli
// dokladnie to, czym stara wersja karmila historie i schowek.  Tak pomiar
// moze ocenic obie wersje tym samym kryterium.
static bool bHasIdentity = false;
static string Identity(object child) {
PropertyInfo pi = ct.GetProperty("IdentityPath");
if (pi == null) return (string) ct.GetProperty("File").GetValue(child, null);
return (string) pi.GetValue(child, null);
}

// SHA MIERZONEJ BINARKI W LOGU.  Staging dzieli jedna nazwe EdSharpNG.exe
// miedzy kandydata i odniesienie, wiec bez tego nie da sie po fakcie
// dowiedziec, KTORY plik dal ten wynik - a to jedyne, co odroznia dowod od
// anegdoty.
static string Sha256(string sPath) {
using (var sha = System.Security.Cryptography.SHA256.Create())
using (var fs = File.OpenRead(sPath))
return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLower();
}

static string RecentEntry(string sPath) {
return (string) Static(app, "ReadValue", "Recent", sPath, "");
}
static void ClearRecent() {
string[] aKeys = (string[]) Static(app, "ReadSectionKeys", "Recent");
foreach (string k in aKeys) Static(app, "DeleteKey", "Recent", k);
}
static void CloseAll() {
foreach (Form f in frame.MdiChildren) {
((RichTextBox) ct.GetField("RTB").GetValue(f)).Modified = false;
f.Close();
}
Application.DoEvents();
}

// Budowa PRAWDZIWEGO pliku zrodlowego dostepnym konwerterem.  Zwraca false,
// gdy konwerter nie istnieje albo nie zbudowal pliku - wtedy format jest
// pomijany JAWNIE, a nie zastepowany atrapa.
static bool BuildFixture(string sMarkdown, string sTarget, out string sWhy) {
sWhy = "";
try {
object w = Static(ft, "KonwertujDoPliku", sMarkdown, "md", sTarget, "source.md");
bool bOk = (bool) w.GetType().GetField("Udane").GetValue(w);
if (!bOk) { sWhy = "konwerter odmowil: " + (string) w.GetType().GetField("Powod").GetValue(w); return false; }
if (!File.Exists(sTarget) || new FileInfo(sTarget).Length == 0) { sWhy = "konwerter nie zbudowal pliku"; return false; }
return true;
}
catch (Exception ex) { sWhy = "wyjatek konwertera: " + ex.Message; return false; }
}

[STAThread] static int Main(string[] args) {
try {
a = Assembly.LoadFrom(Path.GetFullPath(args[0]));
app = a.GetType("EdSharp.App"); ft = a.GetType("EdSharp.MdiFrame"); ct = a.GetType("EdSharp.MdiChild");
util = a.GetType("EdSharp.Util"); zapis = a.GetType("EdSharp.ZapisFormatow");
bHasIdentity = ct.GetProperty("IdentityPath") != null;
Console.WriteLine("BINARKA: " + Path.GetFullPath(args[0]));
Console.WriteLine("SHA256:  " + Sha256(Path.GetFullPath(args[0])));
Console.WriteLine("WERSJA:  " + (string) app.GetField("VersionString").GetValue(null));
Console.WriteLine("IdentityPath obecna: " + bHasIdentity);
Console.WriteLine();

string baseDir = Path.GetDirectoryName(Path.GetFullPath(args[0]));
// Polskie znaki i SPACJA w nazwie katalogu: zgloszenie dotyczylo pliku z
// takiej wlasnie sciezki ("D:\Projekty Codex\Hermes\Nowe").
string dir = Path.Combine(baseDir, "QA tożsamość Żółć");
Directory.CreateDirectory(dir);
string ini = Path.Combine(dir, "test.ini");
File.Copy(args[1], ini, true);
Set("ProgramDir", baseDir); Set("DataDir", dir); Set("ProgramName", "EdSharpNG");
Set("NetDir", RuntimeEnvironment.GetRuntimeDirectory());
Set("TempFile", Path.Combine(dir, "temp.txt"));
Set("DefaultIniFile", ini); Set("IniFile", ini);
Set("HotkeyIniFile", Path.Combine(baseDir, "Hotkeys.ini"));
Set("SpeechLog", Path.Combine(dir, "speech.log")); Set("ErrorLog", Path.Combine(dir, "error.log"));
Set("BomDictionary", Static(util, "GetBomDictionary"));
Static(app, "SetConfigurationValues");
Static(app, "WriteOption", "OpenPrevious", "N");
Static(app, "WriteOption", "RestoreSession", "Y");
Static(app, "WriteOption", "SaveImportedOriginalFormat", "Y");
Static(a.GetType("EdSharp.Sesja"), "Przygotuj", dir);
frame = (Form) Activator.CreateInstance(ft); Set("Frame", frame); frame.Show();
Application.DoEvents();
FieldInfo linkField = ct.GetField("OriginalDocument");

string seed = "# Żółty nagłówek\n\nPierwsza treść i [odnośnik](https://example.org).\n\n- chleb\n- mleko\n";

// ====================================================================
// INWENTARZ: KTORE FORMATY PRODUKT UMIE ZAIMPORTOWAC
// ====================================================================
// Zakres sprawdzenia nie moze pochodzic z mojej pamieci, tylko z tabeli
// [Import] w EdSharp.ini - to ona decyduje, co da sie otworzyc.  Wypis idzie
// do raportu, zeby bylo czarno na bialym, czego pomiar NIE dotknal i czemu.
Console.WriteLine("--- INWENTARZ TABELY [Import] (zrodlo zakresu pomiaru)");
Type iniT = a.GetType("EdSharp.Ini");
string[] aImport = (string[]) Static(iniT, "ReadSectionKeys", ini, "Import");
SortedDictionary<string, List<string>> dSrc = new SortedDictionary<string, List<string>>();
foreach (string k in aImport) {
int i2 = k.IndexOf('2');
if (i2 <= 0) continue;
string src = k.Substring(0, i2), dst = k.Substring(i2 + 1);
if (!dSrc.ContainsKey(src)) dSrc[src] = new List<string>();
dSrc[src].Add(dst);
}
foreach (KeyValuePair<string, List<string>> kv in dSrc)
Console.WriteLine("   ." + kv.Key + " -> " + string.Join(",", kv.Value.ToArray()));
Console.WriteLine("   RAZEM zrodlowych rozszerzen importu: " + dSrc.Count);
// .odt nie ma w tej tabeli ZADNEGO wpisu, wiec EdSharp go nie importuje -
// to nie luka pomiaru, tylko stan produktu.  Pilnuje tego tutaj, zeby
// ewentualne pozniejsze dodanie odt2md nie przeszlo bez sprawdzenia tozsamosci.
Check(!dSrc.ContainsKey("odt"), "inwentarz: .odt nadal NIE jest formatem importu (brak wpisu odt2*)");
// .doc PRODUKT UMIE CZYTAC (doc2txt.cmd -> WdVert/GetText), ale ten pomiar nie
// umie WYTWORZYC prawdziwej probki .doc: nie ma wpisu md2doc, a instalowanie
// Worda ani LibreOffice nie wchodzi w zakres.  To ograniczenie pomiaru, nie
// brak w produkcie - sciezka .doc zostaje niezmierzona, a nie zepsuta.
if (dSrc.ContainsKey("doc"))
Limit(".doc ma wpis importu (" + string.Join(",", dSrc["doc"].ToArray()) + "), ale brak md2doc do wytworzenia probki - sciezka niezmierzona");
Console.WriteLine();

// ====================================================================
// A. IMPORTY Z PRAWEM ZAPISU WSTECZ (OriginalFormatSupported w produkcie)
// ====================================================================
// Te formaty dostaja i tozsamosc, i OriginalDocument.  Sprawdzamy jedno i
// drugie, zeby naprawa historii nie przemycila prawa nadpisania.
Console.WriteLine("--- A. IMPORT FORMATOW Z ZAPISEM WSTECZ (docx, epub, html, rtf)");
foreach (string ext in new string[] {"docx", "epub", "html", "rtf"}) {
string original = Path.Combine(dir, "Artykuł z spacją." + ext);
string why;
if (!BuildFixture(seed, original, out why)) { Skip("fixture " + ext, why); continue; }
Check(true, "fixture jest prawdziwym plikiem " + ext + " (" + new FileInfo(original).Length + " B)");

// ODCISK LICZONY PRZED IMPORTEM, nie po nim.  Liczony po imporcie
// porownywalby plik z samym soba i przechodzil nawet wtedy, gdyby
// import nadpisal oryginal - czyli nie mierzylby nic.
string beforeHash = (string) Static(util, "FileSha256", original);

ClearRecent();
Call(frame, "OpenOrActivateWindow", original, 2, "", "", ext + "2md");
Application.DoEvents();
if (!Box().Text.Contains("Żółty")) { Check(false, "import doszedl do skutku " + ext); CloseAll(); continue; }
Check(true, "import doszedl do skutku " + ext);

// A1. Historia dostaje wpis JUZ przy imporcie.  To jest glowny objaw:
// na starej wersji tego wpisu nie ma, bo SetRecent nie jest wolane, a
// handler zamkniecia odpada na braku ukosnika.
Check(RecentEntry(original).Length > 0, "historia ma wpis zaraz po imporcie " + ext);

// A2. Kluczem historii jest PELNA sciezka zrodla, nie nazwa robocza.
Check(RecentEntry(Path.GetFileName(original)).Length == 0
&& RecentEntry(Path.GetFileNameWithoutExtension(original) + ".md").Length == 0,
"historia NIE ma wpisu pod nazwa robocza " + ext);

// A3. Wartosc, ktora bierze kopiowanie sciezki (Alt+Shift+P).
// NIE jest to pomiar schowka - schowek mierzy osobny etap GUI.
Check(Identity(Child()) == original, "tozsamosc dla kopiowania sciezki to pelna sciezka zrodla " + ext);
Check(Identity(Child()).Contains(@"\"), "tozsamosc ma separator katalogu, wiec komenda nie odpadnie " + ext);

// A4. Bufor NADAL jest Markdownem.  Gdyby naprawa wpisala DOCX do
// child.File, Control+S nadpisalby dokument Worda surowym tekstem.
Check(Path.GetExtension(FileName()) == ".md", "bufor zostaje Markdownem, child.File nietkniety " + ext);
Check(!FileName().Contains(@"\"), "child.File nadal jest nazwa robocza bez katalogu " + ext);

// A5. Sam import i zamkniecie NIE zmieniaja oryginalu.  Odcisk pochodzi
// z chwili PRZED importem (wyzej), wiec to pomiar, nie tautologia.
int iPos = Math.Min(30, Box().TextLength);
Box().SelectionStart = iPos;
CloseAll();
Check((string) Static(util, "FileSha256", original) == beforeHash, "import i zamkniecie nie zmieniaja oryginalu " + ext);

// A6. Po zamknieciu wpis ZOSTAJE (i to pod pelna sciezka).
Check(RecentEntry(original).Length > 0, "wpis historii przezywa zamkniecie " + ext);

// A7. Alt+R -> ponowne otwarcie wraca do WLASCIWEGO zrodla przez
// GetViewLevel, czyli tak, jak robi to lista ostatnich plikow.
int iLevel = (int) Call(frame, "GetViewLevel", original);
Call(frame, "OpenOrActivateWindow", original, iLevel);
Application.DoEvents();
bool bReopened = frame.MdiChildren.Length == 1 && Box().Text.Contains("Żółty");
Check(bReopened, "ponowne otwarcie z historii daje tresc tego samego dokumentu " + ext);

// A8. Brak duplikatu otwartego importu: druga prosba o ten sam plik
// uaktywnia istniejace okno.  Bez tego Alt+R robilby drugie okno z
// dwiema niezaleznymi kopiami zmian.
if (bReopened) {
object active = Child(); int windows = frame.MdiChildren.Length;
Call(frame, "OpenOrActivateWindow", original, iLevel);
Application.DoEvents();
Check(frame.MdiChildren.Length == windows && Object.ReferenceEquals(active, Child()),
"powtorne otwarcie uaktywnia to samo okno, nie tworzy duplikatu " + ext);
}
CloseAll();
}

// ====================================================================
// B. IMPORTY BEZ PRAWA ZAPISU WSTECZ
// ====================================================================
// PDF, .doc i podobne ida do czystego tekstu i NIE maja drogi powrotnej.
// Taki dokument ma prawo byc w historii pod wlasna nazwa, ale NIE MOZE
// zyskac prawa nadpisania oryginalu.  To osobny punkt, bo tu naprawa
// najlatwiej przesadzilaby w druga strone.
Console.WriteLine();
Console.WriteLine("--- B. IMPORT BEZ ZAPISU WSTECZ (tozsamosc TAK, prawo zapisu NIE)");
// KTORY FORMAT JEST JEDNOKIERUNKOWY, DECYDUJE PRODUKT, NIE TEN PLIK.
// Lista wpisana z pamieci twierdzila, ze epub3 nie ma drogi powrotnej, a
// OriginalFormatSupported wymienia go obok docx/odt/rtf.  Dopoki import
// epub3 byl zepsuty, nikt tego nie zauwazyl: zapis wstecz nie mial na czym
// sie uruchomic.  Formaty ze zdolnoscia zapisu mierzy sekcja H i I.
MethodInfo miSupported = ft.GetMethod("OriginalFormatSupported",
BindingFlags.Static | BindingFlags.NonPublic);
Check(miSupported != null, "B kontrakt OriginalFormatSupported dostepny do pomiaru");
foreach (string ext in new string[] {"pdf", "doc", "odt", "epub3", "rst", "tex"}) {
string original = Path.Combine(dir, "Bez powrotu." + ext);
string why;
if (!BuildFixture(seed, original, out why)) { Skip("fixture " + ext, why); continue; }
if (miSupported != null && (bool) miSupported.Invoke(null, new object[] {original})) {
Skip("jednokierunkowosc " + ext, "produkt uznaje " + ext + " za format z zapisem wstecz (OriginalFormatSupported) - mierzy to sekcja H/I");
continue;
}
string sKey = (string) Call(frame, "PreferredImportKey", original);
if (sKey.Length == 0) sKey = ext + "2txt";
if (((string) Static(a.GetType("EdSharp.Ini"), "ReadValue", ini, "Import", sKey, "")).Length == 0) {
Skip("import " + ext, "brak wpisu Import " + sKey);
continue;
}
ClearRecent();
Call(frame, "OpenOrActivateWindow", original, 2, "", "", sKey);
Application.DoEvents();
if (frame.MdiChildren.Length == 0 || Box().TextLength == 0) { Skip("import " + ext, "konwerter nie dal tresci"); CloseAll(); continue; }
// CZY TO BYL IMPORT, CZY CICHY POWROT DO OTWARCIA SUROWEGO.  Gdy konwersja
// nie dala tekstu, OpenOrActivateWindow zeruje iConvert i otwiera plik
// surowo - wtedy child.File jest PELNA sciezka, a nie nazwa robocza.  Taki
// przypadek trzeba zameldowac jako pominiety format, bo mierzylibysmy
// droge surowa pod nazwa importu i raport klamalby o pokryciu.
if (FileName() == original) {
Skip("import " + ext, "konwersja padla, program wrocil do otwarcia surowego (" + sKey + ")");
CloseAll(); continue;
}
Check(true, "import doszedl do skutku " + ext);
Check(RecentEntry(original).Length > 0, "historia ma wpis dla importu bez zapisu wstecz " + ext);
Check(Identity(Child()) == original, "tozsamosc to pelna sciezka zrodla " + ext);
// GRANICA, KTOREJ NAPRAWA NIE WOLNO PRZEKROCZYC.
// ODCISK PRZED PROBA ZAPISU.  Liczony po TrySaveOriginalDocument
// porownywalby plik z samym soba PO ewentualnym nadpisaniu, czyli
// przechodzilby takze wtedy, gdy zapis wstecz doszedl do skutku.
string beforeHashB = (string) Static(util, "FileSha256", original);
object[] tryArgs = new object[] {Child(), ""};
bool bApplicable = (bool) ft.GetMethod("TrySaveOriginalDocument").Invoke(frame, tryArgs);
Check(!bApplicable || ((string) tryArgs[1]).Length > 0,
"utrwalenie tozsamosci NIE nadalo prawa zapisu wstecz " + ext);
Check((string) Static(util, "FileSha256", original) == beforeHashB, "oryginal nietkniety " + ext);
CloseAll();
}

// ====================================================================
// C. KONTROLA: PLIKI OTWIERANE SUROWO (MD, TXT) - NIC SIE NIE ZMIENIA
// ====================================================================
Console.WriteLine();
Console.WriteLine("--- C. KONTROLA MD/TXT (droga surowa nietknieta)");
foreach (string ext in new string[] {"md", "txt"}) {
string plain = Path.Combine(dir, "Kontrolny plik." + ext);
File.WriteAllText(plain, "# Żółty nagłówek\r\n\r\nKontrolna treść.\r\n", new UTF8Encoding(true));
ClearRecent();
Call(frame, "OpenOrActivateWindow", plain, 0);
Application.DoEvents();
Check(Box().Text.Contains("Żółty"), "otwarcie surowe " + ext);
Check(FileName() == plain, "child.File to pelna sciezka, jak dotad " + ext);
Check(Identity(Child()) == plain, "tozsamosc rowna child.File dla pliku nieimportowanego " + ext);
// Droga surowa NIE dostaje wpisu przy otwarciu (tego nie zmieniamy) -
// wpis powstaje przy zamknieciu z przesunietym kursorem, jak dotad.
Box().SelectionStart = Math.Min(10, Box().TextLength);
CloseAll();
Check(RecentEntry(plain).Length > 0, "zamkniecie zapisuje historie dla pliku surowego " + ext);
}

// ====================================================================
// D. SAVE AS MARKDOWN ODCZEPIA TOZSAMOSC
// ====================================================================
Console.WriteLine();
Console.WriteLine("--- D. ODCZEPIENIE PO JAWNYM ZAPISZ JAKO MARKDOWN");
{
string original = Path.Combine(dir, "Do odczepienia.docx");
string why;
if (!BuildFixture(seed, original, out why)) Skip("fixture docx dla odczepienia", why);
else {
Call(frame, "OpenOrActivateWindow", original, 2, "", "", "docx2md");
Application.DoEvents();
string detached = Path.Combine(dir, "Odczepiony.md");
Call(Child(), "SaveTextOrRtfFile", detached);
Check(linkField.GetValue(Child()) == null, "jawny zapis Markdown odczepia OriginalDocument");
Check(Identity(Child()) == detached, "po odczepieniu tozsamosc to NOWY plik, nie stary DOCX");
Check(RecentEntry(detached).Length > 0, "historia dostaje nowy plik po Zapisz jako");
CloseAll();
}
}

// ====================================================================
// E. ODZYSK SESJI ZACHOWUJE WLASCIWA TOZSAMOSC
// ====================================================================
Console.WriteLine();
Console.WriteLine("--- E. SESJA ZAPAMIETUJE ZRODLO, NIE NAZWE ROBOCZA");
{
string original = Path.Combine(dir, "Sesyjny artykuł.docx");
string why;
if (!BuildFixture(seed, original, out why)) Skip("fixture docx dla sesji", why);
else {
Call(frame, "OpenOrActivateWindow", original, 2, "", "", "docx2md");
Application.DoEvents();
IList session = (IList) Call(frame, "ZbierzSesje", false);
object sw = session[session.Count - 1]; Type st = sw.GetType();
Check((string) st.GetField("Plik").GetValue(sw) == original, "sesja zapamietuje sciezke zrodla");
Check((string) st.GetField("OriginalFormatFile").GetValue(sw) == original, "sesja zachowuje powiazanie do zapisu wstecz");
Static(a.GetType("EdSharp.Sesja"), "Zapisz", session);
CloseAll();
Check((int) Call(frame, "PrzywrocSesje") == 1, "odzysk sesji otwiera dokument");
Check(Box().Text.Contains("Żółty") && Identity(Child()) == original, "po odzysku tozsamosc nadal wskazuje zrodlo");
CloseAll();
}
}

// ====================================================================
// F. ODZYSK IMPORTU BEZ OriginalDocument (GLOWNY BLOKER)
// ====================================================================
// ZbierzSesje zapisuje okno.Plik = ImportedFrom takze dla importu BEZ prawa
// zapisu wstecz (PDF, .doc, konwersja do czystego tekstu).  PrzywrocSesje
// musi zrozumiec te tozsamosc, inaczej dzieja sie dwie rzeczy:
//
//   F1 (import NIEZMIENIONY): sesja ma Plik=PDF, linked=false, wiec kod
//      otwiera plik i szuka okna po candidate.File == sFile.  Po imporcie
//      child.File to NAZWA ROBOCZA ("Bez powrotu.md"), a sFile to pelna
//      sciezka PDF - warunek nie trafia, restored zostaje null i leci
//      wyjatek "The document could not be opened." mimo OTWARTEGO okna.
//
//   F2 (import ZMIENIONY, z kopia odzysku): wchodzi w galaz
//      'else if (hasFile) { restored.File = sFile; }', czyli bufor z
//      ODZYSKANYM TEKSTEM dostaje jako cel zapisu fizyczny PDF/DOCX.
//      Control+S zapisuje wtedy surowy tekst na binarnym oryginale.
//      To utrata danych uzytkownika, nie usterka wygody.
Console.WriteLine();
Console.WriteLine("--- F. ODZYSK IMPORTU BEZ PRAWA ZAPISU WSTECZ (tozsamosc + bezpieczenstwo)");
{
// Zrodlo bez drogi powrotnej.  PDF jest tu wlasciwym wyborem: jest na
// liscie formatow binarnych GetViewLevel, a NIE jest na liscie
// OriginalFormatSupported, wiec import nie dostaje OriginalDocument.
string original = Path.Combine(dir, "Odzysk bez linku.pdf");
string why;
if (!BuildFixture(seed, original, out why)) Skip("fixture pdf dla odzysku bez linku", why);
else {
string sKey = (string) Call(frame, "PreferredImportKey", original);
if (sKey.Length == 0) sKey = "pdf2txt";
ClearRecent();
Call(frame, "OpenOrActivateWindow", original, 2, "", "", sKey);
Application.DoEvents();
if (frame.MdiChildren.Length == 0 || Box().TextLength == 0 || FileName() == original)
Skip("odzysk bez linku", "import pdf nie doszedl do skutku (" + sKey + ")");
else {
Check(linkField.GetValue(Child()) == null, "F0 import pdf NIE ma OriginalDocument (zalozenie sekcji)");
Check(Identity(Child()) == original, "F0 tozsamosc importu pdf to zrodlo");

// ---- F1: NIEZMIENIONY import wraca bez bledu --------------------
IList session = (IList) Call(frame, "ZbierzSesje", false);
object sw = session[session.Count - 1]; Type st = sw.GetType();
Check((string) st.GetField("Plik").GetValue(sw) == original,
"F1 sesja zapamietuje zrodlo importu bez linku");
Check(((string) st.GetField("OriginalFormatFile").GetValue(sw)).Length == 0,
"F1 sesja NIE nadaje prawa zapisu wstecz importowi bez linku");
Static(a.GetType("EdSharp.Sesja"), "Zapisz", session);
CloseAll();
int iRestored = (int) Call(frame, "PrzywrocSesje");
Check(iRestored == 1, "F1 odzysk niezmienionego importu bez linku melduje sukces");
Check(frame.MdiChildren.Length == 1 && Box().TextLength > 0,
"F1 odzysk niezmienionego importu otwiera dokument z trescia");
if (frame.MdiChildren.Length == 1) {
Check(Identity(Child()) == original, "F1 po odzysku tozsamosc nadal wskazuje pdf");
Check(FileName() != original,
"F1 child.File po odzysku NIE jest pdf (inaczej Control+S nadpisze oryginal)");
}
CloseAll();

// ---- F2: ZMIENIONY import nie dostaje binarnego celu zapisu -----
ClearRecent();
Call(frame, "OpenOrActivateWindow", original, 2, "", "", sKey);
Application.DoEvents();
Box().AppendText("\nDOPISANA TRESC LOKALNA\n");
Box().Modified = true;
string beforePdf = (string) Static(util, "FileSha256", original);
IList dirty = (IList) Call(frame, "ZbierzSesje", true);
object dw = dirty[dirty.Count - 1];
Check((bool) dw.GetType().GetField("Zmieniony").GetValue(dw)
&& ((string) dw.GetType().GetField("Odzysk").GetValue(dw)).Length > 0,
"F2 zmieniony import dostaje kopie odzysku");
Static(a.GetType("EdSharp.Sesja"), "Zapisz", dirty);
CloseAll();
int iRec = RestoreWithConfirm();
Check(iRec == 1, "F2 odzysk zmienionego importu bez linku melduje sukces");
if (frame.MdiChildren.Length == 1) {
Check(Box().Text.Contains("DOPISANA TRESC LOKALNA"), "F2 odzysk zachowuje lokalne zmiany");
// TO JEST TA ASERCJA.  Cel zapisu NIE MOZE byc binarnym pdf.
Check(FileName() != original,
"F2 child.File po odzysku NIE jest pdf (ochrona przed nadpisaniem surowym tekstem)");
Check(!Path.GetExtension(FileName()).Equals(".pdf", StringComparison.OrdinalIgnoreCase),
"F2 rozszerzenie celu zapisu nie jest .pdf");
Check(Identity(Child()) == original, "F2 tozsamosc po odzysku nadal wskazuje zrodlo");
Check(linkField.GetValue(Child()) == null, "F2 odzysk NIE nadaje prawa zapisu wstecz");
// FIZYCZNA PROBA ZAPISU.  Nie pytamy o zamiar, tylko wolamy
// rzeczywista droge Control+S i patrzymy na BAJTY pdf.
try {
object[] args2 = new object[] {Child(), ""};
ft.GetMethod("TrySaveOriginalDocument").Invoke(frame, args2);
} catch {}
Check((string) Static(util, "FileSha256", original) == beforePdf,
"F2 proba zapisu wstecz NIE zmienila bajtow pdf");
}
CloseAll();
Check((string) Static(util, "FileSha256", original) == beforePdf,
"F2 oryginal pdf nietkniety przez caly cykl odzysku");
}
}
}

// ====================================================================
// G. BRAK ORYGINALU PRZY ISTNIEJACEJ KOPII ODZYSKU
// ====================================================================
// Zrodlo zniklo (przeniesione, pendrive wyjety), ale kopia odzysku jest.
// Tresc czlowieka musi wrocic, a cel zapisu NIE moze wskazywac nieobecnego
// pliku binarnego.
Console.WriteLine();
Console.WriteLine("--- G. ODZYSK, GDY ZRODLO IMPORTU ZNIKLO");
{
string original = Path.Combine(dir, "Znikajacy.pdf");
string why;
if (!BuildFixture(seed, original, out why)) Skip("fixture pdf dla znikajacego zrodla", why);
else {
string sKey = (string) Call(frame, "PreferredImportKey", original);
if (sKey.Length == 0) sKey = "pdf2txt";
Call(frame, "OpenOrActivateWindow", original, 2, "", "", sKey);
Application.DoEvents();
if (frame.MdiChildren.Length == 0 || FileName() == original)
Skip("znikajace zrodlo", "import pdf nie doszedl do skutku");
else {
Box().AppendText("\nTRESC DO URATOWANIA\n");
Box().Modified = true;
IList dirty = (IList) Call(frame, "ZbierzSesje", true);
Static(a.GetType("EdSharp.Sesja"), "Zapisz", dirty);
CloseAll();
File.Delete(original);
int iRec = RestoreWithConfirm();
Check(iRec == 1, "G odzysk dziala, gdy zrodlo importu zniklo");
if (frame.MdiChildren.Length == 1) {
Check(Box().Text.Contains("TRESC DO URATOWANIA"), "G tresc czlowieka wrocila z kopii odzysku");
Check(!Path.GetExtension(FileName()).Equals(".pdf", StringComparison.OrdinalIgnoreCase),
"G cel zapisu nie jest nieobecnym pdf");
}
CloseAll();
}
}
}

// ====================================================================
// H. OPCJA SaveImportedOriginalFormat: RZECZYWISTA PROBA PRZY N I PRZY Y
// ====================================================================
// Poprzedni przebieg ustawial opcje globalnie na Y i nigdy nie mierzyl
// zachowania przy N.  Tutaj mierzymy OBA stany na prawdziwym DOCX, bo
// wlasnie DOCX ma prawo zapisu wstecz - i wlasnie tam wylaczona opcja musi
// to prawo odebrac.
Console.WriteLine();
Console.WriteLine("--- H. OPCJA ZAPISU WSTECZ: DOCX PRZY N I PRZY Y");
{
string original = Path.Combine(dir, "Przelacznik.docx");
string why;
if (!BuildFixture(seed, original, out why)) Skip("fixture docx dla przelacznika", why);
else {
// --- H1: opcja WYLACZONA (N).  Zapis wstecz NIE moze dojsc do skutku.
Static(app, "WriteOption", "SaveImportedOriginalFormat", "N");
Call(frame, "OpenOrActivateWindow", original, 2, "", "", "docx2md");
Application.DoEvents();
if (!Box().Text.Contains("Żółty")) Skip("przelacznik N", "import docx nie doszedl do skutku");
else {
string hashN = (string) Static(util, "FileSha256", original);
Box().AppendText("\nZmiana przy wylaczonej opcji\n");
Box().Modified = true;
object[] argsN = new object[] {Child(), ""};
bool bAppliedN = (bool) ft.GetMethod("TrySaveOriginalDocument").Invoke(frame, argsN);
Check(!bAppliedN, "H1 przy N droga zapisu wstecz NIE przyjmuje zadania (docx)");
Check((string) Static(util, "FileSha256", original) == hashN,
"H1 przy N bajty docx nietkniete");
Check(Path.GetExtension(FileName()) == ".md", "H1 przy N cel zapisu zostaje Markdownem");
}
CloseAll();

// --- H2: opcja WLACZONA (Y).  Zapis wstecz MA dojsc do skutku.
Static(app, "WriteOption", "SaveImportedOriginalFormat", "Y");
Call(frame, "OpenOrActivateWindow", original, 2, "", "", "docx2md");
Application.DoEvents();
if (!Box().Text.Contains("Żółty")) Skip("przelacznik Y", "import docx nie doszedl do skutku");
else {
string hashY = (string) Static(util, "FileSha256", original);
Box().AppendText("\nZmiana przy wlaczonej opcji\n");
Box().Modified = true;
object[] argsY = new object[] {Child(), ""};
bool bAppliedY = (bool) ft.GetMethod("TrySaveOriginalDocument").Invoke(frame, argsY);
Check(bAppliedY && ((string) argsY[1]).Length == 0,
"H2 przy Y zapis wstecz doszedl do skutku bez bledu (docx)");
Check((string) Static(util, "FileSha256", original) != hashY,
"H2 przy Y bajty docx NAPRAWDE sie zmienily");
Check(Path.GetExtension(FileName()) == ".md", "H2 cel zapisu nadal Markdown, nie docx");
}
CloseAll();
// Stan opcji przywrocony, zeby kolejne sekcje mierzyly to, co deklaruja.
Static(app, "WriteOption", "SaveImportedOriginalFormat", "Y");
}
}

// ====================================================================
// I. CZYTNIK epub3: KONWERSJA MA DZIALAC ALBO NIE OTWIERAC NIC
// ====================================================================
// Pandoc 3.x NIE MA czytnika "epub3" (zmierzone: `Unknown input format
// 'epub3'`, kod wyjscia 21), a tabela [Import] wola wlasnie -f epub3 dla
// szesciu wpisow epub32*.  Skutkiem byl import-failure, a potem powrot do
// otwarcia SUROWEGO - czyli czytnik ekranu czytal bajty ZIP-a.
// Zapis wstecz dla epub3 nie jest tu opisem harnessu: pytamy o
// OriginalFormatSupported, czyli o rzeczywisty kontrakt produktu.
Console.WriteLine();
Console.WriteLine("--- I. CZYTNIK epub3 (import nie moze konczyc sie surowym ZIP-em)");
{
string original = Path.Combine(dir, "Książka.epub3");
string why;
if (!BuildFixture(seed, original, out why)) Skip("fixture epub3", why);
else {
// Plik MUSI byc prawdziwym epub3, czyli spakowanym archiwum.
byte[] aHead = new byte[2];
using (FileStream fs = File.OpenRead(original)) fs.Read(aHead, 0, 2);
Check(aHead[0] == (byte) 'P' && aHead[1] == (byte) 'K', "I fixture epub3 jest archiwum ZIP (PK)");

string sKey = (string) Call(frame, "PreferredImportKey", original);
if (sKey.Length == 0) sKey = "epub32md";
string sCmd = (string) Static(a.GetType("EdSharp.Ini"), "ReadValue", ini, "Import", sKey, "");
Check(sCmd.Length > 0, "I tabela [Import] ma wpis dla epub3 (" + sKey + ")");

ClearRecent();
Call(frame, "OpenOrActivateWindow", original, 2, "", "", sKey);
Application.DoEvents();

// DWA dopuszczalne konce, jeden niedopuszczalny.
// Dopuszczalne: (a) import sie udal i widzimy TEKST, (b) program odmowil
// otwarcia (format spakowany).  Niedopuszczalne: okno z surowym ZIP-em.
bool bOpened = frame.MdiChildren.Length > 0;
string sText2 = bOpened ? Box().Text : "";
bool bRawZip = bOpened && (sText2.StartsWith("PK") || sText2.Contains("mimetype")
|| sText2.Contains("META-INF") || FileName() == original);
Check(!bRawZip, "I epub3 NIE zostaje otwarty jako surowe archiwum ZIP");
if (bOpened && !bRawZip) {
Check(sText2.Contains("Żółty") || sText2.Contains("nagłówek"),
"I import epub3 dal czytelny tekst dokumentu");
Check(Path.GetExtension(FileName()) == ".md", "I bufor epub3 jest Markdownem");
Check(Identity(Child()) == original, "I tozsamosc epub3 to pelna sciezka zrodla");
Check(RecentEntry(original).Length > 0, "I historia ma wpis dla epub3");
// ZAPIS WSTECZ DO EPUB3, bo OriginalFormatSupported obiecuje go obok docx.
// Przy zepsutym czytniku ta obietnica byla martwa - nie bylo czego zapisac.
if (linkField.GetValue(Child()) != null) {
string sHashI = (string) Static(util, "FileSha256", original);
Box().AppendText("\r\n\r\nDopisek do epub3.\r\n");
object[] argsI = new object[] {Child(), ""};
bool bDidI = (bool) ft.GetMethod("TrySaveOriginalDocument").Invoke(frame, argsI);
Check(bDidI && ((string) argsI[1]).Length == 0,
"I zapis wstecz epub3 doszedl do skutku bez bledu");
Check((string) Static(util, "FileSha256", original) != sHashI,
"I bajty epub3 NAPRAWDE sie zmienily po zapisie wstecz");
Check(Path.GetExtension(FileName()) == ".md", "I cel zapisu nadal Markdown, nie epub3");
}
else Limit("epub3 zaimportowany bez OriginalDocument - zapisu wstecz nie da sie zmierzyc");
}
else if (!bOpened) Limit("epub3 nie otwarty wcale (odmowa otwarcia zamiast importu)");
CloseAll();
}
}

// ====================================================================
// J. NORMALIZACJA POLECENIA: -f epub3 -> -f epub, -t epub3 NIETKNIETE
// ====================================================================
// Pomiar na samej funkcji, bez plikow: tu chodzi o kontrakt zamiany, a nie
// o konkretna ksiazke.  Profil uzytkownika ma pierwszenstwo nad EdSharp.ini,
// wiec poprawka w samym INI nie wystarcza - normalizacja musi lapac oba.
Console.WriteLine();
Console.WriteLine("--- J. NORMALIZACJA -f epub3 (profil uzytkownika tez przechodzi tu)");
{
string sIn = "pandoc.exe \"%SourceLong%\" -f epub3 -t gfm-raw_html -o %Target%";
string sOut = (string) Static(zapis, "NormalizujPolecenie", sIn);
Check(sOut.IndexOf("-f epub3") < 0, "J czytnik epub3 zamieniony (nie ma juz -f epub3)");
Check(sOut.IndexOf("-f epub ") >= 0, "J czytnikiem jest epub");
// ZAPIS do epub3 jest POPRAWNY i nie wolno go ruszyc - inaczej zabralibysmy
// dzialajacy eksport md2epub3/rst2epub3/tex2epub3.
string sW = (string) Static(zapis, "NormalizujPolecenie",
"pandoc.exe \"%SourceLong%\" -f gfm -t epub3 -o %Target%");
Check(sW.IndexOf("-t epub3") >= 0, "J zapis -t epub3 NIETKNIETY");
Check(sW.IndexOf("-f gfm") >= 0, "J format wejscia zapisu nietkniety");
// Kontrola roznicujaca: funkcja nie zamienia wszystkiego jak leci.
string sInne = (string) Static(zapis, "NormalizujPolecenie",
"pandoc.exe \"%SourceLong%\" -f docx -t gfm -o %Target%");
Check(sInne.IndexOf("-f docx") >= 0, "J inne formaty wejscia nietkniete (kontrola)");
}

// ====================================================================
// K. ANULOWANE ZAMKNIECIE NIE PODMIENIA CELU ZAPISU
// ====================================================================
// Handler Closing zapisuje pozycje do historii pod TOZSAMOSCIA.  Robil to
// przypisaniem do prywatnego pola sFile, czyli do celu zapisu - a gdy
// zamkniecie zostalo ANULOWANE, okno zostawalo z celem ustawionym na
// zrodlo importu i nastepny Control+S pisal Markdown na docx.
Console.WriteLine();
Console.WriteLine("--- K. ANULOWANE ZAMKNIECIE: cel zapisu bez zmian");
{
string original = Path.Combine(dir, "Anulowane.docx");
string why;
if (!BuildFixture(seed, original, out why)) Skip("fixture docx dla anulowanego zamkniecia", why);
else {
Call(frame, "OpenOrActivateWindow", original, 2, "", "", "docx2md");
Application.DoEvents();
if (!Box().Text.Contains("Żółty")) Skip("anulowane zamkniecie", "import docx nie doszedl do skutku");
else {
string sCelPrzed = FileName();
Box().SelectionStart = Math.Min(20, Box().TextLength);
object child = Child();
// ANULUJEMY zamkniecie tak, jak robi to uzytkownik w pytaniu o zapis.
CancelEventArgs e = new CancelEventArgs();
e.Cancel = true;
try {
MethodInfo mi = ct.GetMethod("OnClosing", BindingFlags.Instance | BindingFlags.NonPublic);
if (mi != null) mi.Invoke(child, new object[] {e});
} catch {}
Application.DoEvents();
Check(frame.MdiChildren.Length == 1, "K okno nadal otwarte po anulowanym zamknieciu");
string sCelPo = FileName();
Check(sCelPo == sCelPrzed,
"K cel zapisu NIETKNIETY przez anulowane zamkniecie (bylo: " + sCelPrzed + ", jest: " + sCelPo + ")");
Check(!Path.GetExtension(sCelPo).Equals(".docx", StringComparison.OrdinalIgnoreCase),
"K cel zapisu nie stal sie docx");
// Historia MA dostac wpis pod tozsamoscia - poprawka nie moze tego zabrac.
Check(RecentEntry(original).Length > 0 || Box().SelectionStart == 0,
"K historia nadal dostaje wpis pod tozsamoscia zrodla");
}
CloseAll();
}
}

Console.WriteLine();
Console.WriteLine("POMINIETE FORMATY (" + skipped.Count + "):");
foreach (string s in skipped) Console.WriteLine("  - " + s);
Console.WriteLine();
if (failed.Count > 0) {
Console.WriteLine("NIEZALICZONE (" + failed.Count + "):");
foreach (string s in failed) Console.WriteLine("  - " + s);
Console.WriteLine();
}
Console.WriteLine("RESULT: " + pass + " PASS / " + fail + " FAIL / " + skipped.Count + " SKIP");
return fail == 0 ? 0 : 1;
}
catch (Exception e) { Console.WriteLine(e); return 2; }
finally { if (frame != null) { try { CloseAll(); } catch {} frame.Dispose(); } }
}
}
