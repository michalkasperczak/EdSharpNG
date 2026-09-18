// HARNESS ZAPISU DO FORMATU ZRODLOWEGO (OriginalFormatSave).
//
// MIERZY SKUTEK NA DYSKU, nie deklaracje w kodzie: po kazdym punkcie czyta
// bajty plikow i liczy zawartosc katalogu.  Wywolanie zwrotne jest UDAWANE -
// prawdziwy Pandoc mierzymy w harness_zapis_formatow.cs; tutaj przedmiotem
// pomiaru jest TRANSAKCJA, wiec musimy umiec wymusic kazda awarie konwertera
// (urywek pliku, wyjatek, plik zerowy, cudza edycja w trakcie).
//
// KONTROLA NEGATYWNA JEST W SRODKU (punkt 0): sonda najpierw odtwarza NAIWNY
// zapis (File.WriteAllText Markdownem po pliku .docx) i ZADA, zeby oryginal
// zginal.  Gdyby ten punkt nie przechodzil, znaczyloby to, ze pomiar nie
// odroznia transakcji od jej braku.
//
// URUCHOMIENIE (z WSL):
//   mkdir -p /mnt/c/EdSharpOriginalSaveTest
//   cp OriginalFormatSave.cs ZapisFormatow.cs testy/harness_zapis_oryginalu.cs /mnt/c/EdSharpOriginalSaveTest/
//   cd /mnt/c/EdSharpOriginalSaveTest
//   csc.exe /nologo /out:harness.exe harness_zapis_oryginalu.cs OriginalFormatSave.cs ZapisFormatow.cs
//   ./harness.exe
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using EdSharp;

public class HarnessZapisOryginalu {
static int iPass = 0, iFail = 0;
static StringBuilder log = new StringBuilder();

static void W(string s) { log.AppendLine(s); Console.WriteLine(s); }
static void Sprawdz(bool b, string s) {
if (b) { iPass++; W("PASS: " + s); } else { iFail++; W("FAIL: " + s); }
}

static string sKat = @"C:\EdSharpOriginalSaveTest";
static string sPraca;

const string MD = "# Naglowek\r\n\r\nAkapit, polskie litery: zazolc gesla jazn.\r\n";

// Udawany "prawdziwy dokument": naglowek ZIP, zeby bylo widac na oczy, ze
// tresc nie jest Markdownem.  Konwertera tu nie mierzymy.
static byte[] Dokument(string sZnacznik) {
List<byte> a = new List<byte>(new byte[] { 0x50, 0x4B, 0x03, 0x04 });
a.AddRange(Encoding.UTF8.GetBytes(sZnacznik));
return a.ToArray();
}

static bool JestZip(string sFile) {
try {
using (FileStream fs = File.OpenRead(sFile)) {
if (fs.Length < 4) return false;
byte[] a = new byte[4];
fs.Read(a, 0, 4);
return a[0] == 0x50 && a[1] == 0x4B;
}
}
catch { return false; }
}

static string Tresc(string sFile) {
try { return Encoding.UTF8.GetString(File.ReadAllBytes(sFile)); } catch { return "<blad odczytu>"; }
}

// Ile plikow przejsciowych zostalo w katalogu.  Transakcja nie ma prawa
// zostawic ani jednego - ani po sukcesie, ani po awarii.
static int Smieci(string sDir) {
int i = 0;
foreach (string s in Directory.GetFiles(sDir)) if (Path.GetFileName(s).Contains(".edsharp-orig-")) i++;
return i;
}

static string[] Kopie(string sDir) {
string sK = Path.Combine(sDir, ".edsharp-backups");
if (!Directory.Exists(sK)) return new string[0];
string[] a = Directory.GetFiles(sK);
Array.Sort(a);
return a;
}

static string NowyKatalog(string sNazwa) {
string s = Path.Combine(sPraca, sNazwa);
Directory.CreateDirectory(s);
return s;
}

// Plik zrodlowy do pomiaru: prawdziwe bajty na dysku + skojarzenie z odciskiem.
static string ZrobZrodlo(string sDir, string sNazwa, string sZnacznik) {
string s = Path.Combine(sDir, sNazwa);
File.WriteAllBytes(s, Dokument(sZnacznik));
return s;
}

public static int Main() {
sPraca = Path.Combine(sKat, "praca-" + DateTime.Now.ToString("HHmmss"));
Directory.CreateDirectory(sPraca);
W("== HARNESS ZAPISU DO FORMATU ZRODLOWEGO ==");
W("katalog pomiaru: " + sPraca);
W("");

// ---------------------------------------------------------------- punkt 0 ---
W("== 0. KONTROLA NEGATYWNA: naiwny zapis niszczy dokument ==");
{
string sDir = NowyKatalog("negatywna");
string sOrig = ZrobZrodlo(sDir, "raport.docx", "wersja-pierwotna");
Sprawdz(JestZip(sOrig), "warunek pomiaru: plik wyjsciowy jest dokumentem (naglowek PK)");
File.WriteAllText(sOrig, MD);         // dokladnie to, czego NIE wolno robic
Sprawdz(!JestZip(sOrig), "naiwne Control+S zamienia dokument na Markdown - sonda to widzi");
Sprawdz(Kopie(sDir).Length == 0, "i nie zostawia po starej wersji ZADNEGO sladu");
}

// ---------------------------------------------------------------- punkt 1 ---
W("");
W("== 1. ZAPIS NORMALNY, POWTORZONY TRZY RAZY ==");
{
string sDir = NowyKatalog("normalny");
string sOrig = ZrobZrodlo(sDir, "raport.docx", "wersja-0");
OriginalDocumentLink link = OriginalFormatSave.Capture(sOrig);
Sprawdz(link.Fingerprint.Length == 64, "Capture daje odcisk SHA-256 (64 znaki): " + link.Fingerprint.Substring(0, 12) + "...");
Sprawdz(link.Path == sOrig, "Capture zapamietuje pelna sciezke");

string sPierwszyOdcisk = link.Fingerprint;
List<string> aKopie = new List<string>();
for (int i = 1; i <= 3; i++) {
string sZnacznik = "wersja-" + i;
string sBlad, sKopia;
bool b = OriginalFormatSave.TrySave(link, MD + sZnacznik,
delegate(string sMd, string sStage) { return Konwertuj(sStage, sZnacznik, sMd); },
out sBlad, out sKopia);
Sprawdz(b, "zapis nr " + i + " udal sie" + (b ? "" : "  [" + sBlad + "]"));
Sprawdz(Tresc(sOrig).Contains(sZnacznik), "oryginal ma teraz tresc z zapisu nr " + i);
Sprawdz(JestZip(sOrig), "oryginal nadal jest dokumentem, nie Markdownem (zapis nr " + i + ")");
Sprawdz(!String.IsNullOrEmpty(sKopia) && File.Exists(sKopia), "zapis nr " + i + " zwrocil istniejaca kopie zapasowa: " + (sKopia == null ? "" : Path.GetFileName(sKopia)));
Sprawdz(Tresc(sKopia).Contains("wersja-" + (i - 1)), "kopia nr " + i + " trzyma DOKLADNIE poprzednia wersje (wersja-" + (i - 1) + ")");
if (!String.IsNullOrEmpty(sKopia)) aKopie.Add(sKopia);
Sprawdz(Smieci(sDir) == 0, "po zapisie nr " + i + " brak plikow przejsciowych w katalogu");
}
Sprawdz(aKopie.Count == 3 && aKopie[0] != aKopie[1] && aKopie[1] != aKopie[2] && aKopie[0] != aKopie[2],
"trzy zapisy daly trzy ROZNE kopie (stara wersja nigdy nie jest nadpisana)");
Sprawdz(Kopie(sDir).Length == 3, "w .edsharp-backups leza wszystkie trzy kopie");
Sprawdz(link.Fingerprint != sPierwszyOdcisk, "odcisk w skojarzeniu zostal odswiezony po zapisie");
Sprawdz(link.Fingerprint == OriginalFormatSave.SumaKontrolna(sOrig), "i zgadza sie z plikiem, ktory naprawde lezy na dysku");
}

// ---------------------------------------------------------------- punkt 2 ---
W("");
W("== 2. KONWERTER ZGLASZA BLAD, ZOSTAWIAJAC URYWEK PLIKU ==");
{
string sDir = NowyKatalog("blad-urywek");
string sOrig = ZrobZrodlo(sDir, "raport.docx", "nietkniety");
OriginalDocumentLink link = OriginalFormatSave.Capture(sOrig);
string sOdcisk = link.Fingerprint;
string sBlad, sKopia;
bool b = OriginalFormatSave.TrySave(link, MD,
delegate(string sMd, string sStage) {
File.WriteAllBytes(sStage, Dokument("urywek"));   // plik JEST, ale konwerter mowi ze padl
WynikZapisu w = new WynikZapisu();
w.Udane = false; w.Powod = "Brak narzedzia pandoc.";
return w;
},
out sBlad, out sKopia);
Sprawdz(!b, "zapis nie udal sie");
Sprawdz(sBlad == "Brak narzedzia pandoc.", "powod od konwertera wraca do wolajacego doslownie: " + sBlad);
Sprawdz(Tresc(sOrig).Contains("nietkniety") && JestZip(sOrig), "ORYGINAL NIETKNIETY");
Sprawdz(link.Fingerprint == sOdcisk, "odcisk w skojarzeniu NIE zmienil sie");
Sprawdz(sKopia == "", "nie powstala zadna kopia zapasowa (nie bylo czego zapisywac)");
Sprawdz(Smieci(sDir) == 0, "urywek pliku przejsciowego zostal usuniety");
Sprawdz(Kopie(sDir).Length == 0, "katalog kopii nie zostal zasmiecony");
}

// ---------------------------------------------------------------- punkt 3 ---
W("");
W("== 3. KONWERTER RZUCA WYJATEK ==");
{
string sDir = NowyKatalog("wyjatek");
string sOrig = ZrobZrodlo(sDir, "ksiazka.epub", "nietkniety");
OriginalDocumentLink link = OriginalFormatSave.Capture(sOrig);
string sBlad, sKopia;
bool b = OriginalFormatSave.TrySave(link, MD,
delegate(string sMd, string sStage) {
File.WriteAllBytes(sStage, Dokument("polowa"));
throw new InvalidOperationException("konwerter sie wywrocil");
},
out sBlad, out sKopia);
Sprawdz(!b, "wyjatek w wywolaniu zwrotnym NIE wychodzi na zewnatrz - jest bledem, nie awaria programu");
Sprawdz(sBlad.Contains("konwerter sie wywrocil"), "komunikat wyjatku trafia do powodu: " + sBlad);
Sprawdz(Tresc(sOrig).Contains("nietkniety"), "ORYGINAL NIETKNIETY");
Sprawdz(Smieci(sDir) == 0, "brak plikow przejsciowych po wyjatku");
}

// ---------------------------------------------------------------- punkt 4 ---
W("");
W("== 4. KONWERTER MELDUJE SUKCES, A PLIKU NIE MA ALBO JEST PUSTY ==");
{
string sDir = NowyKatalog("pusty-wynik");
string sOrig = ZrobZrodlo(sDir, "raport.docx", "nietkniety");
OriginalDocumentLink link = OriginalFormatSave.Capture(sOrig);

string sBlad, sKopia;
bool b = OriginalFormatSave.TrySave(link, MD,
delegate(string sMd, string sStage) {
File.WriteAllBytes(sStage, new byte[0]);          // plik zerowej dlugosci
WynikZapisu w = new WynikZapisu(); w.Udane = true; return w;
},
out sBlad, out sKopia);
Sprawdz(!b, "SUKCES konwertera + plik zerowy = BLAD (nie wierzymy deklaracji)");
Sprawdz(sBlad == "Konwerter nie utworzyl pliku.", "powod nazywa rzecz po imieniu: " + sBlad);
Sprawdz(Tresc(sOrig).Contains("nietkniety"), "ORYGINAL NIETKNIETY po pustym wyniku");
Sprawdz(Smieci(sDir) == 0, "pusty plik przejsciowy usuniety");

bool b2 = OriginalFormatSave.TrySave(link, MD,
delegate(string sMd, string sStage) {
WynikZapisu w = new WynikZapisu(); w.Udane = true; return w;   // nic nie zapisal
},
out sBlad, out sKopia);
Sprawdz(!b2, "SUKCES konwertera + BRAK pliku = BLAD");
Sprawdz(Tresc(sOrig).Contains("nietkniety"), "ORYGINAL NIETKNIETY po braku wyniku");
Sprawdz(Kopie(sDir).Length == 0, "przez caly punkt nie powstala zadna kopia");
}

// ---------------------------------------------------------------- punkt 5 ---
W("");
W("== 5. CUDZA EDYCJA ORYGINALU **PRZED** KONWERSJA ==");
{
string sDir = NowyKatalog("edycja-przed");
string sOrig = ZrobZrodlo(sDir, "raport.docx", "wersja-nasza");
OriginalDocumentLink link = OriginalFormatSave.Capture(sOrig);
// Ktos otworzyl plik w Wordzie i zapisal swoje zmiany:
File.WriteAllBytes(sOrig, Dokument("CUDZA-PRACA"));
bool bWolane = false;
string sBlad, sKopia;
bool b = OriginalFormatSave.TrySave(link, MD,
delegate(string sMd, string sStage) {
bWolane = true;
File.WriteAllBytes(sStage, Dokument("nasze"));
WynikZapisu w = new WynikZapisu(); w.Udane = true; return w;
},
out sBlad, out sKopia);
Sprawdz(!b, "zapis wstrzymany");
Sprawdz(!bWolane, "konwerter NIE zostal nawet uruchomiony (sprawdzamy odcisk PRZED konwersja)");
Sprawdz(sBlad.Contains("zmienil sie poza edytorem"), "powod mowi o zmianie z zewnatrz: " + sBlad);
Sprawdz(Tresc(sOrig).Contains("CUDZA-PRACA"), "CUDZA PRACA NIETKNIETA");
Sprawdz(Smieci(sDir) == 0, "brak plikow przejsciowych");
}

// ---------------------------------------------------------------- punkt 6 ---
W("");
W("== 6. CUDZA EDYCJA ORYGINALU **W TRAKCIE** KONWERSJI ==");
{
string sDir = NowyKatalog("edycja-w-trakcie");
string sOrig = ZrobZrodlo(sDir, "raport.docx", "wersja-nasza");
OriginalDocumentLink link = OriginalFormatSave.Capture(sOrig);
string sBlad, sKopia;
bool b = OriginalFormatSave.TrySave(link, MD,
delegate(string sMd, string sStage) {
// Konwersja trwa sekundy - w tym czasie Word zapisuje swoja wersje.
File.WriteAllBytes(sOrig, Dokument("CUDZA-PRACA-W-TRAKCIE"));
File.WriteAllBytes(sStage, Dokument("nasze"));
WynikZapisu w = new WynikZapisu(); w.Udane = true; return w;
},
out sBlad, out sKopia);
Sprawdz(!b, "zapis wstrzymany mimo UDANEJ konwersji");
Sprawdz(sBlad.Contains("podczas konwersji"), "powod wskazuje moment: " + sBlad);
Sprawdz(Tresc(sOrig).Contains("CUDZA-PRACA-W-TRAKCIE"), "CUDZA PRACA NIETKNIETA");
Sprawdz(Smieci(sDir) == 0, "gotowy, ale nieuzyty wynik konwersji zostal usuniety");
Sprawdz(Kopie(sDir).Length == 0, "nie powstala kopia zapasowa cudzej wersji (nic nie podmienialismy)");
}

// ---------------------------------------------------------------- punkt 7 ---
W("");
W("== 7. PLIK ZRODLOWY NIE ISTNIEJE ALBO NIE DA SIE GO PRZECZYTAC ==");
{
string sDir = NowyKatalog("brak-zrodla");

// 7a. Capture na nieistniejacym pliku MUSI RZUCIC, a nie zwrocic pusty odcisk.
bool bRzucil = false;
try { OriginalFormatSave.Capture(Path.Combine(sDir, "nie-ma-mnie.docx")); }
catch (FileNotFoundException) { bRzucil = true; }
catch (DirectoryNotFoundException) { bRzucil = true; }
catch (IOException) { bRzucil = true; }
Sprawdz(bRzucil, "Capture na brakujacym pliku RZUCA wyjatek (nie udaje, ze skojarzenia nie ma)");

// 7b. Plik zniknal miedzy otwarciem a zapisem.
string sOrig = ZrobZrodlo(sDir, "raport.docx", "byl-i-znikl");
OriginalDocumentLink link = OriginalFormatSave.Capture(sOrig);
File.Delete(sOrig);
bool bWolane = false;
string sBlad, sKopia;
bool b = OriginalFormatSave.TrySave(link, MD,
delegate(string sMd, string sStage) { bWolane = true; WynikZapisu w = new WynikZapisu(); w.Udane = true; File.WriteAllBytes(sStage, Dokument("x")); return w; },
out sBlad, out sKopia);
Sprawdz(!b && !bWolane, "zapis do usunietego oryginalu pada BEZ uruchamiania konwertera");
Sprawdz(sBlad.Contains("nie istnieje"), "powod: " + sBlad);
Sprawdz(!File.Exists(sOrig), "i nie tworzymy pliku od nowa w cudzym miejscu");

// 7c. Plik istnieje, ale jest zablokowany na wylacznosc (FileShare.None).
string sLock = ZrobZrodlo(sDir, "zajety.docx", "zajety-przez-worda");
OriginalDocumentLink link2 = new OriginalDocumentLink();
link2.Path = sLock;
link2.Fingerprint = OriginalFormatSave.SumaKontrolna(sLock);
using (FileStream fs = new FileStream(sLock, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
bWolane = false;
bool b3 = OriginalFormatSave.TrySave(link2, MD,
delegate(string sMd, string sStage) { bWolane = true; WynikZapisu w = new WynikZapisu(); w.Udane = true; File.WriteAllBytes(sStage, Dokument("x")); return w; },
out sBlad, out sKopia);
Sprawdz(!b3 && !bWolane, "plik otwarty na wylacznosc: zapis pada przed konwersja");
Sprawdz(sBlad.Contains("Nie moge przeczytac"), "powod mowi o odczycie, nie o formacie: " + sBlad);
}
Sprawdz(Tresc(sLock).Contains("zajety-przez-worda"), "zablokowany oryginal NIETKNIETY");
Sprawdz(Smieci(sDir) == 0, "brak plikow przejsciowych w calym punkcie 7");
}

// ---------------------------------------------------------------- punkt 8 ---
W("");
W("== 8. CEL ZABLOKOWANY DO ZAPISU W CHWILI PODMIANY ==");
{
string sDir = NowyKatalog("cel-zablokowany");
string sOrig = ZrobZrodlo(sDir, "raport.docx", "wersja-na-dysku");
OriginalDocumentLink link = OriginalFormatSave.Capture(sOrig);
string sOdcisk = link.Fingerprint;
string sBlad, sKopia;
// FileShare.Read pozwala nam policzyc odcisk, ale blokuje ZAPIS - dokladnie
// to robi podglad w Wordzie albo indeksator.
using (FileStream fs = new FileStream(sOrig, FileMode.Open, FileAccess.Read, FileShare.Read)) {
bool b = OriginalFormatSave.TrySave(link, MD,
delegate(string sMd, string sStage) {
File.WriteAllBytes(sStage, Dokument("nowa-wersja"));
WynikZapisu w = new WynikZapisu(); w.Udane = true; return w;
},
out sBlad, out sKopia);
Sprawdz(!b, "podmiana zablokowanego pliku pada");
Sprawdz(sBlad.Contains("Nie moge podmienic"), "powod: " + sBlad);
}
Sprawdz(Tresc(sOrig).Contains("wersja-na-dysku") && JestZip(sOrig), "ORYGINAL ZACHOWANY bajt w bajt");
Sprawdz(OriginalFormatSave.SumaKontrolna(sOrig) == sOdcisk, "i jego odcisk sie nie zmienil");
Sprawdz(link.Fingerprint == sOdcisk, "odcisk w skojarzeniu tez zostal stary");
Sprawdz(Smieci(sDir) == 0, "nieuzyty wynik konwersji usuniety");
}

// ---------------------------------------------------------------- punkt 9 ---
W("");
W("== 9. NAZWY ZE SPACJAMI I ZNAKAMI NIEANGIELSKIMI ==");
{
string sDir = NowyKatalog("nazwy dziwne");
string sOrig = ZrobZrodlo(sDir, "Zazolc gesla jazn - raport (2026).docx", "wersja-0");
OriginalDocumentLink link = OriginalFormatSave.Capture(sOrig);
string sBlad, sKopia;
bool b = OriginalFormatSave.TrySave(link, MD,
delegate(string sMd, string sStage) {
Sprawdz(Path.GetExtension(sStage) == ".docx", "plik przejsciowy ma DOCELOWE rozszerzenie (konwertery ida po rozszerzeniu): " + Path.GetFileName(sStage));
Sprawdz(Path.GetDirectoryName(sStage) == sDir, "i lezy w katalogu oryginalu (podmiana w obrebie wolumenu)");
Sprawdz(!File.Exists(sStage), "konwerter dostaje nazwe WOLNA, nie istniejacy plik");
File.WriteAllBytes(sStage, Dokument("wersja-1"));
WynikZapisu w = new WynikZapisu(); w.Udane = true; return w;
},
out sBlad, out sKopia);
Sprawdz(b, "zapis pliku ze spacjami i polskimi literami udal sie" + (b ? "" : "  [" + sBlad + "]"));
Sprawdz(Tresc(sOrig).Contains("wersja-1"), "oryginal podmieniony");
Sprawdz(File.Exists(sKopia) && Path.GetFileName(sKopia).StartsWith("Zazolc gesla jazn - raport (2026)-001"), "kopia zachowala nazwe i numer: " + Path.GetFileName(sKopia));
Sprawdz(Smieci(sDir) == 0, "brak plikow przejsciowych");
}

// --------------------------------------------------------------- punkt 10 ---
W("");
W("== 10. SKOJARZENIE PUSTE ALBO BEZ ODCISKU ==");
{
string sDir = NowyKatalog("bez-odcisku");
string sOrig = ZrobZrodlo(sDir, "raport.docx", "nietkniety");
string sBlad, sKopia;
bool b = OriginalFormatSave.TrySave(null, MD, delegate(string a, string c) { return null; }, out sBlad, out sKopia);
Sprawdz(!b && sBlad.Contains("Brak skojarzenia"), "brak skojarzenia = blad z powodem: " + sBlad);

OriginalDocumentLink link = new OriginalDocumentLink();
link.Path = sOrig;            // odcisku nikt nie policzyl
bool bWolane = false;
bool b2 = OriginalFormatSave.TrySave(link, MD,
delegate(string sMd, string sStage) { bWolane = true; WynikZapisu w = new WynikZapisu(); w.Udane = true; File.WriteAllBytes(sStage, Dokument("x")); return w; },
out sBlad, out sKopia);
Sprawdz(!b2 && !bWolane, "PUSTY odcisk nie moze zdac egzaminu na 'plik sie nie zmienil'");
Sprawdz(sBlad.Contains("Brak odcisku"), "powod: " + sBlad);
Sprawdz(Tresc(sOrig).Contains("nietkniety"), "ORYGINAL NIETKNIETY");

bool b3 = OriginalFormatSave.TrySave(OriginalFormatSave.Capture(sOrig), MD, null, out sBlad, out sKopia);
Sprawdz(!b3 && sBlad.Contains("Brak obslugi konwersji"), "brak wywolania zwrotnego = blad z powodem: " + sBlad);
}

// --------------------------------------------------------------- punkt 11 ---
W("");
W("== 11. CALY KATALOG PO POMIARZE: ZERO SMIECI ==");
{
int iSmieci = 0;
List<string> aZnalezione = new List<string>();
foreach (string s in Directory.GetFiles(sPraca, "*", SearchOption.AllDirectories)) {
if (Path.GetFileName(s).Contains(".edsharp-orig-")) { iSmieci++; aZnalezione.Add(s); }
}
Sprawdz(iSmieci == 0, "w calym drzewie pomiaru nie ma ANI JEDNEGO pliku przejsciowego" + (iSmieci == 0 ? "" : ": " + String.Join(", ", aZnalezione.ToArray())));
}

W("");
W("==================================================");
W("PASS: " + iPass + "   FAIL: " + iFail);
W("==================================================");
try { File.WriteAllText(Path.Combine(sKat, "harness_zapis_oryginalu.txt"), log.ToString()); } catch {}
return iFail == 0 ? 0 : 1;
}

// Udawany konwerter, ktory sie udaje.
static WynikZapisu Konwertuj(string sStage, string sZnacznik, string sMd) {
File.WriteAllBytes(sStage, Dokument(sZnacznik + "|" + (sMd == null ? 0 : sMd.Length)));
WynikZapisu w = new WynikZapisu();
w.Udane = true;
w.PlikWynikowy = sStage;
return w;
}
}
