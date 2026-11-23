// SKLADNIKI - pobieranie i aktualizowanie narzedzi Convert bez udzialu uzytkownika.
//
// Zadanie 8 z listy Kasperczaka (11.09.2026): "Sprawdzanie i instalacja
// potrzebnych komponentow (pandoc itd.) mozliwie bez udzialu uzytkownika.
// Jesli konieczny restart programu: pytanie o zapis otwartych plikow
// i automatyczny restart."
//
// CO BYLO ZLE.  Narzedzia Convert (pandoc, tidy, xpdf, liblouis, astyle)
// pobieral TYLKO skrypt budowania na mojej maszynie (FetchConvertTools.ps1).
// Do instalatora nie sa pakowane wcale - sprawdzone: zero wystapien "Convert\"
// w EdSharp_Setup.iss.  U uzytkownika konwersje po prostu nie dzialaly, a jak
// juz cos mowily, to pokazywaly wiersz polecenia zamiast powiedziec wprost
// "brakuje narzedzia".
//
// CO ROBI TERAZ.  Program przy starcie sprawdza po cichu, w tle, czy narzedzia
// sa i czy sa aktualne; brakujace i przedawnione dociaga sam.  Zadnego okna,
// zadnego pytania - meldunek trafia na pasek wiadomosci, tak samo jak
// sprawdzanie wersji programu z 5.0.81.  Gdy nie ma internetu, milczy: nikt go
// o to nie prosil, wiec nie ma po co meldowac, ze sie nie udalo.
//
// RESTART NIE JEST POTRZEBNY - i to jest ustalenie, nie przeoczenie.  Narzedzia
// Convert to osobne programy, wolane dopiero w chwili konwersji
// (Util.ExpandCommandLine + RunHideWait).  Nic ich nie trzyma w pamieci, wiec
// swiezo dociagniety pandoc dziala natychmiast, bez zamykania edytora.  Punkt
// o restarcie z zadania 8 dotyczylby wymiany samego EdSharpNG.exe - a to robi
// instalator (5.0.81).
//
// ZMIERZONE (12.09.2026, testy/pomiar_skladniki.cs) - wszystkie piec zrodel
// odpowiada i oddaje prawdziwe archiwum ZIP:
//   Pandoc 3.11      41 761 100 B
//   Tidy 5.8.0        1 370 807 B
//   liblouis v3.39.0  4 971 161 B
//   Xpdf 4.06        12 826 385 B
//   AStyle 3.6          930 771 B
// Bez tego pomiaru nie mialbym prawa napisac "pobieram najnowsza wersje".

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace EdSharp {

public static class Skladniki {

// Tylko prywatny harness ustawia ten katalog przez refleksje.  W produkcji
// pozostaje pusty, wiec nie zmienia katalogu profilu uzytkownika.
static string KatalogUzytkownikaDlaTestow = "";

// Wynik nalezy do watku wywolujacego: menu odczytuje go po swoim sprawdzeniu,
// a rownolegly cichy check startowy nie moze podmienic komunikatu.  Odczyt
// metadanych i aktywacja archiwum to rozne operacje: ich stan nie moze sie
// wzajemnie udawac, ze wszystko jest aktualne.
[ThreadStatic] static bool OstatniOdczytUdany;
[ThreadStatic] static bool OstatniaAktywacjaUdana;
[ThreadStatic] static bool OstatnieOdtworzenieNiepelne;
public static bool OstatniaKontrolaOdczytuUdana() { return OstatniOdczytUdany; }
public static bool OstatniaAktywacjaSkladnikowUdana() { return OstatniaAktywacjaUdana; }
public static bool OstatniaKontrolaSkladnikowUdana() { return OstatniOdczytUdany && OstatniaAktywacjaUdana; }
public static bool OstatnieOdtworzenieSkladnikowNiepelne() { return OstatnieOdtworzenieNiepelne; }
public static string KomunikatBleduAktywacjiPoAngielsku() {
if (OstatnieOdtworzenieNiepelne) return "Could not fully restore the previous component after an update error. Check the components before converting again.";
return "Some component updates could not be activated. Existing components were kept. Check available storage and try again.";
}
public static string KomunikatBleduAktywacjiPoPolsku() {
if (OstatnieOdtworzenieNiepelne) return "Nie udało się w pełni przywrócić poprzedniego składnika po błędzie aktualizacji. Sprawdź składniki przed kolejną konwersją.";
return "Nie udało się aktywować wszystkich aktualizacji składników. Zachowano istniejące składniki. Sprawdź wolne miejsce na dysku i spróbuj ponownie.";
}

static void RozpocznijKontroleSkladnikow() {
OstatniOdczytUdany = true;
OstatniaAktywacjaUdana = true;
OstatnieOdtworzenieNiepelne = false;
}

// Prywatny szew harnessu: produkcja pozostawia oba delegaty puste, wiec nie
// zmienia transportu ani aktywacji.  Test moze rozdzielic odczyt od podmiany
// bez sieci i bez dotykania prawdziwego katalogu uzytkownika.
static Func<Skladnik,string[]> NajnowszaDlaTestow;
static Func<Skladnik,string,string,bool> ZainstalujDlaTestow;
static Action AwariaPrzedZapisemLockaDlaTestow = null;

// Jeden skladnik: gdzie ma wyladowac, po czym poznac, ze sie udalo, i skad go
// wziac.  Trzymam to w kodzie, a nie w pliku obok programu, bo plik obok
// uzytkownik moglby stracic przy aktualizacji - a wtedy program przestalby
// wiedziec, czego mu brakuje.
public class Skladnik {
public string Nazwa;
public string Katalog;
public string Plik;
public bool Splaszcz;   // true = jeden program: przenies zawartosc folderu z exe
public string Repo;     // wydania GitHuba, np. "jgm/pandoc"
public string Wzorzec;  // ktory zalacznik z wydania
public string Strona;   // albo: strona, z ktorej czytam numer wersji
public string WzorWersji;
public string Szablon;  // adres z {v} w miejscu wersji
public string Adres;    // albo: adres na sztywno
}

static List<Skladnik> Lista() {
List<Skladnik> l = new List<Skladnik>();

Skladnik s = new Skladnik();
s.Nazwa = "Pandoc"; s.Katalog = "Pandoc"; s.Plik = "pandoc.exe"; s.Splaszcz = true;
s.Repo = "jgm/pandoc"; s.Wzorzec = "windows-x86_64\\.zip$";
l.Add(s);

s = new Skladnik();
s.Nazwa = "Tidy"; s.Katalog = "Tidy"; s.Plik = "tidy.exe"; s.Splaszcz = true;
s.Repo = "htacg/tidy-html5"; s.Wzorzec = "win64.*\\.zip$";
l.Add(s);

s = new Skladnik();
s.Nazwa = "liblouis"; s.Katalog = "liblouis"; s.Plik = "lou_translate.exe"; s.Splaszcz = false;
s.Repo = "liblouis/liblouis"; s.Wzorzec = "win(64|32).*\\.zip$";
l.Add(s);

s = new Skladnik();
s.Nazwa = "Xpdf"; s.Katalog = "Xpdf"; s.Plik = "pdftotext.exe"; s.Splaszcz = true;
s.Strona = "https://www.xpdfreader.com/download.html";
s.WzorWersji = "Current version:\\s*([0-9.]+)";
s.Szablon = "https://dl.xpdfreader.com/xpdf-tools-win-{v}.zip";
l.Add(s);

s = new Skladnik();
s.Nazwa = "AStyle"; s.Katalog = "astyle"; s.Plik = "astyle.exe"; s.Splaszcz = true;
s.Adres = "https://master.dl.sourceforge.net/project/astyle/astyle/astyle%203.6/astyle-3.6-x64.zip";
l.Add(s);

return l;
}

static string KatalogConvert() {
// NIE polegam na App.ProgramDir.  Zmierzone (12.09.2026): ono jest ustawiane
// dopiero w trakcie startu programu, a moj kod potrafi zostac wolany
// wczesniej albo z watku w tle - wtedy App.ProgramDir jest PUSTE i
// Path.Combine wyrzuca wyjatek "wartosc nie moze byc zerowa".  Wyjatek
// lecial do bloku catch, czyli po cichu: program po prostu nigdy nie
// zauwazylby, ze skladnikow brakuje.  Zamiast tego biore katalog, w ktorym
// naprawde lezy plik programu.
string sDir = "";
try { sDir = App.ProgramDir; } catch {}
if (string.IsNullOrEmpty(sDir)) {
try {
sDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
} catch {}
}
if (string.IsNullOrEmpty(sDir)) sDir = Directory.GetCurrentDirectory();
return Path.Combine(sDir, "Convert");
}

// GDZIE WOLNO PISAC.  Zmierzone 13.09.2026: program stoi w Program Files, a
// manifest ma asInvoker, wiec zapis do wlasnego katalogu Convert jest
// ODRZUCANY (UnauthorizedAccessException).  Bez tego dociaganie skladnikow
// cicho przepadalo w bloku catch - narzedzia nie bylo, a program milczal.
//
// Dlatego przy braku prawa do wlasnego katalogu piszemy do profilu
// uzytkownika, gdzie prawo mamy zawsze i bez pytania o administratora.
// Kolejnosc szukania (KatalogiSzukania) obejmuje oba miejsca, wiec narzedzie
// dociagniete do profilu jest widziane tak samo jak to z instalacji.
public static string KatalogConvertUzytkownika() {
if (!String.IsNullOrEmpty(KatalogUzytkownikaDlaTestow)) return KatalogUzytkownikaDlaTestow;
string sBase = "";
try {
sBase = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
} catch {}
if (string.IsNullOrEmpty(sBase)) return KatalogConvert();
return Path.Combine(Path.Combine(sBase, "EdSharp"), "Convert");
}

// Czy do podanego katalogu naprawde mozemy pisac?  Nie zgaduje z uprawnien -
// probuje zapisac plik i sprzatam po sobie.  Prawa NTFS potrafia klamac.
static bool MoznaPisac(string sDir) {
try {
Directory.CreateDirectory(sDir);
string sProba = Path.Combine(sDir, "proba_" + Guid.NewGuid().ToString("N") + ".tmp");
File.WriteAllText(sProba, "x");
File.Delete(sProba);
return true;
} catch { return false; }
}

// Katalog, do ktorego dociagamy: wlasny gdy wolno, inaczej profil.
public static string KatalogDoZapisu() {
string sWlasny = KatalogConvert();
if (MoznaPisac(sWlasny)) return sWlasny;
return KatalogConvertUzytkownika();
}

// Oba miejsca, w ktorych szukamy narzedzia.  Kolejnosc nie jest werdyktem:
// o wyborze decyduja wersje z lockow nalezacych do tych katalogow.
static string[] KatalogiSzukania() {
string sWlasny = KatalogConvert();
string sUzyt = KatalogConvertUzytkownika();
if (String.Compare(sWlasny, sUzyt, true) == 0) return new string[] { sWlasny };
return new string[] { sWlasny, sUzyt };
}

static string PlikWersji(string sKatalog) {
return Path.Combine(sKatalog, "Tools.lock");
}

static Dictionary<string,string> CzytajWersje(string sKatalog) {
Dictionary<string,string> d = new Dictionary<string,string>();
try {
string sPlik = PlikWersji(sKatalog);
if (!File.Exists(sPlik)) return d;
foreach (string sLine in File.ReadAllLines(sPlik)) {
int i = sLine.IndexOf('=');
if (i > 0) d[sLine.Substring(0, i).Trim()] = sLine.Substring(i + 1).Trim();
}
} catch {}
return d;
}

static void ZapiszWersje(string sPlik, Dictionary<string,string> d) {
List<string> l = new List<string>();
foreach (KeyValuePair<string,string> kv in d) l.Add(kv.Key + "=" + kv.Value);
l.Sort();
File.WriteAllLines(sPlik, l.ToArray(), Encoding.ASCII);
}

static bool WersjaLiczbowa(string sWersja, out int[] aWersja) {
aWersja = null;
if (String.IsNullOrEmpty(sWersja)) return false;
sWersja = sWersja.Trim();
if (sWersja.StartsWith("v", StringComparison.OrdinalIgnoreCase)) sWersja = sWersja.Substring(1);
if (!Regex.IsMatch(sWersja, "^[0-9]+(?:\\.[0-9]+)*$")) return false;
string[] a = sWersja.Split('.');
aWersja = new int[a.Length];
for (int i = 0; i < a.Length; i++) if (!Int32.TryParse(a[i], out aWersja[i])) return false;
return true;
}

static int PorownajWersje(string sLewa, string sPrawa, out bool bPewne) {
bPewne = false;
int[] aLewa, aPrawa;
if (!WersjaLiczbowa(sLewa, out aLewa) || !WersjaLiczbowa(sPrawa, out aPrawa)) return 0;
bPewne = true;
int iMax = Math.Max(aLewa.Length, aPrawa.Length);
for (int i = 0; i < iMax; i++) {
int iLewa = i < aLewa.Length ? aLewa[i] : 0;
int iPrawa = i < aPrawa.Length ? aPrawa[i] : 0;
if (iLewa != iPrawa) return iLewa < iPrawa ? -1 : 1;
}
return 0;
}

// Osobno od sieci: nie pobieraj na slepo, gdy numer dostawcy jest nieznany.
static bool CzyPotrzebnaAktualizacja(string sMam, string sNajnowsza) {
bool bPewne;
return PorownajWersje(sMam, sNajnowsza, out bPewne) < 0 && bPewne;
}

class KopiaSkladnika {
public string Baza = "";
public string Sciezka = "";
public string Wersja = "";
// Poprawna oznacza PE z liczbowym wpisem locka; starsza dzialajaca kopia
// moze nie miec metadanych i wtedy pozostaje Legacy, a nie "brakiem".
public bool Wykonywalna = false;
public bool Legacy = false;
public bool Poprawna = false;
}

static string SciezkaExeW(string sBaza, Skladnik s) {
try {
string sDir = Path.Combine(sBaza, s.Katalog);
if (!Directory.Exists(sDir)) return "";
string[] a = Directory.GetFiles(sDir, s.Plik, SearchOption.AllDirectories);
return a.Length > 0 ? a[0] : "";
} catch { return ""; }
}

// Nie uruchamiamy nieznanego pliku, aby sprawdzic, czy jest programem.  Ten
// lekki odczyt naglowka PE odroznia brak/uszkodzenie od kopii wykonywalnej i
// wystarcza dla narzedzi, ktore sami zarzadzamy po ich s.Plik.
static bool CzyPlikPe(string sPlik) {
try {
using (FileStream fs = File.OpenRead(sPlik))
using (BinaryReader br = new BinaryReader(fs)) {
if (fs.Length < 64 || br.ReadByte() != 'M' || br.ReadByte() != 'Z') return false;
fs.Position = 0x3c;
int iPe = br.ReadInt32();
if (iPe < 64 || iPe > fs.Length - 24) return false;
fs.Position = iPe;
return br.ReadByte() == 'P' && br.ReadByte() == 'E' && br.ReadByte() == 0 && br.ReadByte() == 0;
}
} catch { return false; }
}

static KopiaSkladnika KopiaW(string sBaza, Skladnik s) {
KopiaSkladnika k = new KopiaSkladnika();
k.Baza = sBaza;
k.Sciezka = SciezkaExeW(sBaza, s);
if (k.Sciezka.Length == 0) return k;
if (!CzyPlikPe(k.Sciezka)) return k;
k.Wykonywalna = true;
Dictionary<string,string> d = CzytajWersje(sBaza);
if (d.ContainsKey(s.Nazwa)) k.Wersja = d[s.Nazwa];
int[] a;
k.Poprawna = WersjaLiczbowa(k.Wersja, out a);
k.Legacy = !k.Poprawna;
return k;
}

// Jedyny resolver uzywany przez konwersje, komunikat o braku i decyzje sieci.
// Nie wybiera przypadkowego EXE z profilu: kopia jest poprawna dopiero z
// wlasnym, rozpoznawalnym wpisem Tools.lock.  Remis i niepewnosc: instalacja.
static KopiaSkladnika WybierzKopie(Skladnik s) {
string[] aBazy = KatalogiSzukania();
KopiaSkladnika kInst = KopiaW(aBazy[0], s);
if (aBazy.Length == 1) return kInst;
KopiaSkladnika kProfil = KopiaW(aBazy[1], s);
if (!kInst.Wykonywalna) return kProfil.Wykonywalna ? kProfil : kInst;
if (!kProfil.Wykonywalna) return kInst;
// Przy nieznanej wersji nie da sie uczciwie nazwac profilu nowszym.  Zostawiam
// wiec dzialajaca instalacje; sama legacy kopia nadal nie jest brakiem.
if (!kInst.Poprawna || !kProfil.Poprawna) return kInst;
bool bPewne;
int i = PorownajWersje(kInst.Wersja, kProfil.Wersja, out bPewne);
return bPewne && i < 0 ? kProfil : kInst;
}

static bool Jest(Skladnik s) {
return WybierzKopie(s).Wykonywalna;
}

public static string SciezkaExe(Skladnik s) {
KopiaSkladnika k = WybierzKopie(s);
return k.Wykonywalna ? k.Sciezka : "";
}

// Wlacza nowoczesne szyfrowanie po lacznosci HTTPS.  Wolane przed KAZDYM
// pobraniem, bo .NET Framework 4.x domyslnie proponuje stare wersje, ktore
// serwery odrzucaja - a odrzucenie objawia sie jak brak internetu.
// Nowsze wersje (TLS 1.3) wpisuje liczba, bo nazwa nie istnieje w tej wersji
// .NET i kod by sie nie skompilowal; nieznana wartosc jest po prostu
// pomijana przez system.
static void UstawSzyfrowanie() {
try {
ServicePointManager.SecurityProtocol =
  SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
} catch {}
try {
ServicePointManager.SecurityProtocol |= (SecurityProtocolType) 12288; // TLS 1.3
} catch {}
}

static string PobierzTekst(string sUrl) {
try {
// TLS 1.2 MUSI byc ustawione jawnie.  Zmierzone (12.09.2026): bez tego
// pobieranie padalo w 0,2 s, bo .NET Framework domyslnie proponuje stare
// szyfrowanie (SSL 3 / TLS 1.0), ktorego GitHub i pozostale serwery juz nie
// przyjmuja.  Program milczal - wyjatek szedl do catch - wiec objaw byl
// taki, jakby po prostu nie bylo internetu.
UstawSzyfrowanie();
HttpWebRequest req = (HttpWebRequest) WebRequest.Create(sUrl);
req.UserAgent = "EdSharpNG";
req.Timeout = 30000;
req.AllowAutoRedirect = true;
using (HttpWebResponse resp = (HttpWebResponse) req.GetResponse())
using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) {
return sr.ReadToEnd();
}
} catch { return ""; }
}

// Ustala, jaka wersja jest najnowsza i skad ja wziac.  Zwraca false, gdy
// zrodlo nie odpowiada - wtedy zostawiam to, co jest, i nic nie mowie.
static bool Najnowsza(Skladnik s, out string sWersja, out string sAdres) {
sWersja = "";
sAdres = "";
if (NajnowszaDlaTestow != null) {
string[] a = NajnowszaDlaTestow(s);
if (a == null || a.Length != 2 || String.IsNullOrEmpty(a[0]) || String.IsNullOrEmpty(a[1])) return false;
sWersja = a[0];
sAdres = a[1];
return true;
}

if (s.Repo != null) {
string sJson = PobierzTekst("https://api.github.com/repos/" + s.Repo + "/releases/latest");
if (sJson.Length == 0) return false;
Match m = Regex.Match(sJson, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
if (!m.Success) return false;
sWersja = m.Groups[1].Value;
foreach (Match mm in Regex.Matches(sJson, "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"")) {
string sU = mm.Groups[1].Value;
string sPlik = sU.Substring(sU.LastIndexOf('/') + 1);
if (Regex.IsMatch(sPlik, s.Wzorzec)) { sAdres = sU; return true; }
}
return false;
}

if (s.Strona != null) {
string sHtml = PobierzTekst(s.Strona);
if (sHtml.Length == 0) return false;
Match m = Regex.Match(sHtml, s.WzorWersji);
if (!m.Success) return false;
sWersja = m.Groups[1].Value;
sAdres = s.Szablon.Replace("{v}", sWersja);
return true;
}

if (s.Adres != null) {
sAdres = s.Adres;
Match m = Regex.Match(s.Adres, "([0-9]+(?:\\.[0-9]+)+)");
sWersja = m.Success ? m.Groups[1].Value : "?";
return true;
}

return false;
}

// Pobiera archiwum i rozpakowuje na miejsce.  Do folderu docelowego wchodzi
// dopiero na koncu - najpierw wszystko ma sie udac w katalogu tymczasowym.
// Dzieki temu nieudane pobranie nie zostawia uzytkownika bez dzialajacego
// narzedzia, ktore mial wczesniej.
static bool Zainstaluj(Skladnik s, string sAdres, string sWersja) {
if (ZainstalujDlaTestow != null) return ZainstalujDlaTestow(s, sAdres, sWersja);
string sKatalog = KatalogDoZapisu();
string sTmp = Path.Combine(sKatalog, ".edsharp-components-" + Guid.NewGuid().ToString("N"));
try {
Directory.CreateDirectory(sTmp);
string sZip = Path.Combine(sTmp, "pobrane.zip");

UstawSzyfrowanie();
HttpWebRequest req = (HttpWebRequest) WebRequest.Create(sAdres);
req.UserAgent = "EdSharpNG";
req.Timeout = 300000;
req.AllowAutoRedirect = true;
using (HttpWebResponse resp = (HttpWebResponse) req.GetResponse())
using (Stream st = resp.GetResponseStream())
using (FileStream fs = new FileStream(sZip, FileMode.Create, FileAccess.Write)) {
byte[] ab = new byte[65536];
int iRead;
while ((iRead = st.Read(ab, 0, ab.Length)) > 0) fs.Write(ab, 0, iRead);
}

return ZainstalujZArchiwum(s, sZip, sKatalog, sWersja);
} catch {
return false;
} finally {
try { if (Directory.Exists(sTmp)) Directory.Delete(sTmp, true); } catch {}
}
}

static string NazwaMuteksuAktualizacji(string sKatalog) {
using (SHA256 sha = SHA256.Create()) {
byte[] a = sha.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(sKatalog).ToUpperInvariant()));
StringBuilder sb = new StringBuilder();
foreach (byte b in a) sb.Append(b.ToString("X2"));
return "Local\\EdSharpNG.Components." + sb.ToString();
}
}

static void DiagnostykaAktualizacji(string s) {
try { System.Diagnostics.Trace.WriteLine("EdSharpNG components: " + s); } catch {}
}

// Rozpakowuje do waskiego stagingu pod docelowym Convert, pasywnie waliduje
// wlasciwy s.Plik jako PE i aktywuje katalog oraz Tools.lock pod jednym
// muteksem. Backup sprzatamy dopiero po commit i tylko best-effort.
static bool ZainstalujZArchiwum(Skladnik s, string sZip, string sKatalog, string sWersja) {
string sStage = Path.Combine(sKatalog, ".edsharp-components-" + Guid.NewGuid().ToString("N"));
string sZrodlo = "";
string sCel = Path.Combine(sKatalog, s.Katalog);
string sBackup = sCel + ".old-" + Guid.NewGuid().ToString("N");
string sNowy = Path.Combine(sStage, "next");
string sNowyLock = Path.Combine(sStage, "Tools.lock");
string sLock = PlikWersji(sKatalog);
string sLockBackup = Path.Combine(sStage, "old-Tools.lock");
bool bPrzeniesionoStary = false;
bool bPrzeniesionoNowy = false;
bool bBylLock = false;
bool bKopiaLocka = false;
bool bDotknietoLock = false;
bool bZachowajStage = false;
bool bCommit = false;
bool bBlokada = false;
Mutex m = null;
try {
m = new Mutex(false, NazwaMuteksuAktualizacji(sKatalog));
try { bBlokada = m.WaitOne(30000); } catch (AbandonedMutexException) { bBlokada = true; }
if (!bBlokada) { DiagnostykaAktualizacji("timeout waiting for component activation lock"); return false; }
Directory.CreateDirectory(sKatalog);
Directory.CreateDirectory(sStage);
using (FileStream fs = File.OpenRead(sZip)) {
if (fs.ReadByte() != 'P' || fs.ReadByte() != 'K') return false;
}
string sRozp = Path.Combine(sStage, "x");
ZipFile.ExtractToDirectory(sZip, sRozp);
sZrodlo = sRozp;
string[] aDirs = Directory.GetDirectories(sRozp);
string[] aFiles = Directory.GetFiles(sRozp);
if (aDirs.Length == 1 && aFiles.Length == 0) sZrodlo = aDirs[0];
if (s.Splaszcz) {
string[] aExe = Directory.GetFiles(sZrodlo, s.Plik, SearchOption.AllDirectories);
if (aExe.Length == 0) return false;
sZrodlo = Path.GetDirectoryName(aExe[0]);
}
KopiujDrzewo(sZrodlo, sNowy);
string sNowyExe = SciezkaExeW(sStage, new Skladnik { Katalog = "next", Plik = s.Plik });
if (!CzyPlikPe(sNowyExe)) return false;

Dictionary<string,string> d = CzytajWersje(sKatalog);
d[s.Nazwa] = sWersja;
ZapiszWersje(sNowyLock, d);
bBylLock = File.Exists(sLock);
if (bBylLock) { File.Copy(sLock, sLockBackup, true); bKopiaLocka = true; }
if (Directory.Exists(sCel)) { Directory.Move(sCel, sBackup); bPrzeniesionoStary = true; }
Directory.Move(sNowy, sCel); bPrzeniesionoNowy = true;
bDotknietoLock = true;
if (AwariaPrzedZapisemLockaDlaTestow != null) AwariaPrzedZapisemLockaDlaTestow();
if (bBylLock) File.Replace(sNowyLock, sLock, null); else File.Move(sNowyLock, sLock);
KopiaSkladnika k = KopiaW(sKatalog, s);
if (!k.Poprawna || k.Wersja != sWersja) throw new IOException("Nie udalo sie aktywowac skladnika i jego wersji.");
bCommit = true;
try { if (Directory.Exists(sBackup)) Directory.Delete(sBackup, true); }
catch { DiagnostykaAktualizacji("committed component backup retained: " + sBackup); }
return true;
} catch {
if (bCommit) return true;
bool bRollback = true;
if (bPrzeniesionoNowy && Directory.Exists(sCel)) {
try { Directory.Move(sCel, sCel + ".failed-" + Guid.NewGuid().ToString("N")); }
catch { bRollback = false; }
}
if (bPrzeniesionoStary && Directory.Exists(sBackup)) {
if (!Directory.Exists(sCel)) {
try { Directory.Move(sBackup, sCel); } catch { bRollback = false; }
} else bRollback = false;
}
if (bDotknietoLock) {
if (bBylLock) {
if (!bKopiaLocka || !File.Exists(sLockBackup)) bRollback = false;
else {
try { File.Copy(sLockBackup, sLock, true); } catch { bRollback = false; }
}
} else {
try { if (File.Exists(sLock)) File.Delete(sLock); } catch { bRollback = false; }
}
}
if (!bRollback) {
bZachowajStage = true;
OstatnieOdtworzenieNiepelne = true;
DiagnostykaAktualizacji("incomplete component rollback; backup retained at " + sBackup + "; stage retained at " + sStage);
}
return false;
} finally {
try { if (!bZachowajStage && Directory.Exists(sStage)) Directory.Delete(sStage, true); } catch {}
try { if (bBlokada) m.ReleaseMutex(); } catch {}
try { if (m != null) m.Close(); } catch {}
}
}

static void KopiujDrzewo(string sZ, string sDo) {
Directory.CreateDirectory(sDo);
foreach (string sPlik in Directory.GetFiles(sZ)) {
File.Copy(sPlik, Path.Combine(sDo, Path.GetFileName(sPlik)), true);
}
foreach (string sDir in Directory.GetDirectories(sZ)) {
KopiujDrzewo(sDir, Path.Combine(sDo, Path.GetFileName(sDir)));
}
}

// Sprawdza wszystkie skladniki i dociaga, czego brakuje albo co sie
// przedawnilo.  Zwraca krotki meldunek albo puste, gdy nie bylo nic do
// zrobienia.  NIE pokazuje okien - wolane z watku w tle.
public static string SprawdzIUzupelnij(bool bTylkoBrakujace) {
RozpocznijKontroleSkladnikow();
List<string> lZrobione = new List<string>();

foreach (Skladnik s in Lista()) {
KopiaSkladnika kMam = WybierzKopie(s);
bool bJest = kMam.Wykonywalna;

// Tryb ostrozny: przy starcie zajmuje sie tylko tym, czego NIE MA.
// Podmiana dzialajacego narzedzia w tle byla by niegrzeczna - moglaby
// trafic w chwile, gdy uzytkownik wlasnie z niego korzysta.
if (bTylkoBrakujace && bJest) continue;

string sWersja, sAdres;
if (!Najnowsza(s, out sWersja, out sAdres)) { OstatniOdczytUdany = false; continue; }

if (bJest && !CzyPotrzebnaAktualizacja(kMam.Wersja, sWersja)) continue;

if (Zainstaluj(s, sAdres, sWersja)) {
lZrobione.Add(s.Nazwa + " " + sWersja);
} else OstatniaAktywacjaUdana = false;
}

if (lZrobione.Count == 0) return "";
return string.Join(", ", lZrobione.ToArray());
}

// Czego brakuje w tej chwili - do meldunku dla uzytkownika, gdy pyta sam
// (menu) albo gdy konwersja nie doszla do skutku.
public static List<string> Brakujace() {
List<string> l = new List<string>();
foreach (Skladnik s in Lista()) if (!Jest(s)) l.Add(s.Nazwa);
return l;
}

// Czy konkretne narzedzie jest na miejscu.  Sluzy do sensownego komunikatu
// przy nieudanej konwersji: "brakuje Pandoc" mowi wiecej niz wiersz
// polecenia, ktory uzytkownik zobaczyl do tej pory.
static Skladnik SkladnikZPierwszegoProgramu(string sPolecenie, out int iPocz, out int iDlugosc, out string sProgram) {
iPocz = 0; iDlugosc = 0; sProgram = "";
if (String.IsNullOrEmpty(sPolecenie)) return null;
while (iPocz < sPolecenie.Length && Char.IsWhiteSpace(sPolecenie[iPocz])) iPocz++;
if (iPocz >= sPolecenie.Length) return null;
if (sPolecenie[iPocz] == '"') {
iPocz++;
int iKoniec = sPolecenie.IndexOf('"', iPocz);
if (iKoniec < 0) return null;
iDlugosc = iKoniec - iPocz;
} else {
int iKoniec = iPocz;
while (iKoniec < sPolecenie.Length && !Char.IsWhiteSpace(sPolecenie[iKoniec])) iKoniec++;
iDlugosc = iKoniec - iPocz;
}
if (iDlugosc <= 0) return null;
sProgram = sPolecenie.Substring(iPocz, iDlugosc);
string sNazwa;
try { sNazwa = Path.GetFileName(sProgram); } catch { return null; }
foreach (Skladnik s in Lista()) if (String.Equals(sNazwa, s.Plik, StringComparison.OrdinalIgnoreCase)) return s;
return null;
}

static bool CzySciezkaZarzadzana(string sSciezka, Skladnik s) {
try {
string sPelna = Path.GetFullPath(sSciezka).TrimEnd('\\');
foreach (string sBaza in KatalogiSzukania()) {
string sKatalog = Path.GetFullPath(Path.Combine(sBaza, s.Katalog)).TrimEnd('\\');
if (sPelna.StartsWith(sKatalog + "\\", StringComparison.OrdinalIgnoreCase)) return true;
// ExpandCommandLine zamienia %ProgDir% na 8.3.  Porownanie samych dlugich
// liter nie rozpozna takiej poprawnej sciezki, wiec porownujemy takze alias
// katalogu zarzadzanego, bez uruchamiania wskazanego pliku.
string sKrotki = EdSharp.Win32.GetShortPath(sKatalog).TrimEnd('\\');
if (sPelna.StartsWith(sKrotki + "\\", StringComparison.OrdinalIgnoreCase)) return true;
}
} catch {}
return false;
}

public static string BrakujaceDlaPolecenia(string sPolecenie) {
int iPocz, iDlugosc; string sProgram;
Skladnik s = SkladnikZPierwszegoProgramu(sPolecenie, out iPocz, out iDlugosc, out sProgram);
return s != null && CzySciezkaZarzadzana(sProgram, s) && !Jest(s) ? s.Nazwa : "";
}

// PRZEKIEROWANIE SCIEZKI DO PROFILU.  Wpisy w EdSharp.ini wskazuja
// %ProgDir%\Convert\..., czyli katalog instalacji.  Gdy narzedzie zostalo
// dociagniete do profilu (bo do Program Files nie wolno pisac), wpis prowadzi
// w puste miejsce i konwersja pada - mimo ze narzedzie JEST.  Dlatego przed
// uruchomieniem podmieniamy sciezke na ta, w ktorej plik naprawde lezy.
//
// Przepisuje tylko pierwszy program polecenia i tylko wtedy, gdy jest to nasza
// sciezka Convert.  Wlasny program, PATH oraz sama nazwa w argumentach zostaja
// nietkniete.  Wybrana kopia jest wspolna z Brakujace oraz aktualizatorem.
public static string NaprawSciezkeNarzedzia(string sPolecenie) {
if (String.IsNullOrEmpty(sPolecenie)) return sPolecenie;
try {
int iPocz, iDlugosc; string sStara;
Skladnik s = SkladnikZPierwszegoProgramu(sPolecenie, out iPocz, out iDlugosc, out sStara);
if (s == null || !CzySciezkaZarzadzana(sStara, s)) return sPolecenie;
string sNowa = SciezkaExe(s);
if (sNowa.Length == 0 || String.Equals(sStara, sNowa, StringComparison.OrdinalIgnoreCase)) return sPolecenie;
bool bBylCudzyslow = iPocz > 0 && sPolecenie[iPocz - 1] == '"';
if (!bBylCudzyslow && sNowa.IndexOf(' ') >= 0) sNowa = "\"" + sNowa + "\"";
return sPolecenie.Substring(0, iPocz) + sNowa + sPolecenie.Substring(iPocz + iDlugosc);
} catch {}
return sPolecenie;
}

} // class Skladniki
} // namespace EdSharp
