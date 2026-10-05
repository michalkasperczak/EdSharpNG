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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;

class ImportIdentityProbe {
static Assembly a; static Type app, ft, ct, util, zapis; static Form frame;
static int pass, fail; static List<string> failed = new List<string>();
static List<string> skipped = new List<string>();

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

// A5. Sam import i zamkniecie NIE zmieniaja oryginalu.
string beforeHash = (string) Static(util, "FileSha256", original);
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
foreach (string ext in new string[] {"pdf", "doc", "odt", "epub3", "rst", "tex"}) {
string original = Path.Combine(dir, "Bez powrotu." + ext);
string why;
if (!BuildFixture(seed, original, out why)) { Skip("fixture " + ext, why); continue; }
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
object[] tryArgs = new object[] {Child(), ""};
bool bApplicable = (bool) ft.GetMethod("TrySaveOriginalDocument").Invoke(frame, tryArgs);
string beforeHash = (string) Static(util, "FileSha256", original);
Check(!bApplicable || ((string) tryArgs[1]).Length > 0,
"utrwalenie tozsamosci NIE nadalo prawa zapisu wstecz " + ext);
Check((string) Static(util, "FileSha256", original) == beforeHash, "oryginal nietkniety " + ext);
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
