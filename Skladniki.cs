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
using System.Text;
using System.Text.RegularExpressions;

namespace EdSharp {

public static class Skladniki {

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

// Oba miejsca, w ktorych szukamy narzedzia - kolejnosc ma znaczenie: to z
// instalacji wygrywa, bo jest wspolne dla wszystkich uzytkownikow maszyny.
static string[] KatalogiSzukania() {
string sWlasny = KatalogConvert();
string sUzyt = KatalogConvertUzytkownika();
if (String.Compare(sWlasny, sUzyt, true) == 0) return new string[] { sWlasny };
return new string[] { sWlasny, sUzyt };
}

static string PlikWersji() {
// Plik wersji lezy tam, gdzie naprawde piszemy - inaczej przy instalacji w
// Program Files nie dalby sie zapisac i program za kazdym startem uwazalby,
// ze nic nie zostalo pobrane.
return Path.Combine(KatalogDoZapisu(), "Tools.lock");
}

// Czy narzedzie w ogole jest?  Szukam w calym drzewie, bo niektore archiwa
// pakuja exe w podkatalog.  Sprawdzam OBA miejsca: katalog instalacji i
// katalog w profilu uzytkownika (tam trafia to, co dociagnelismy bez praw
// administratora).
static bool Jest(Skladnik s) {
return SciezkaExe(s).Length > 0;
}

// Pelna sciezka do pliku wykonywalnego skladnika albo puste, gdy go nie ma.
// Potrzebna nie tylko do sprawdzenia - podmieniamy nia sciezke w wierszu
// polecenia, gdy narzedzie lezy w profilu, a wpis w ini wskazuje instalacje.
public static string SciezkaExe(Skladnik s) {
foreach (string sBaza in KatalogiSzukania()) {
string sDir = Path.Combine(sBaza, s.Katalog);
if (!Directory.Exists(sDir)) continue;
try {
string[] a = Directory.GetFiles(sDir, s.Plik, SearchOption.AllDirectories);
if (a.Length > 0) return a[0];
} catch {}
}
return "";
}

static Dictionary<string,string> CzytajWersje() {
Dictionary<string,string> d = new Dictionary<string,string>();
try {
if (!File.Exists(PlikWersji())) return d;
foreach (string sLine in File.ReadAllLines(PlikWersji())) {
int i = sLine.IndexOf('=');
if (i > 0) d[sLine.Substring(0, i).Trim()] = sLine.Substring(i + 1).Trim();
}
} catch {}
return d;
}

static void ZapiszWersje(Dictionary<string,string> d) {
try {
List<string> l = new List<string>();
foreach (KeyValuePair<string,string> kv in d) l.Add(kv.Key + "=" + kv.Value);
l.Sort();
Directory.CreateDirectory(KatalogConvert());
File.WriteAllLines(PlikWersji(), l.ToArray(), Encoding.ASCII);
} catch {}
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
static bool Zainstaluj(Skladnik s, string sAdres) {
string sTmp = Path.Combine(Path.GetTempPath(), "EdSharpNG_" + Guid.NewGuid().ToString("N"));
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

// Sprawdzam, ze to naprawde archiwum, a nie strona z komunikatem o
// blokadzie.  ZIP zaczyna sie od liter PK.  Bez tego rozpakowywanie
// wyrzuciloby blad w miejscu, w ktorym trudniej zrozumiec przyczyne.
using (FileStream fs = File.OpenRead(sZip)) {
int b1 = fs.ReadByte();
int b2 = fs.ReadByte();
if (b1 != 'P' || b2 != 'K') return false;
}

string sRozp = Path.Combine(sTmp, "x");
ZipFile.ExtractToDirectory(sZip, sRozp);

// Archiwa czesto maja jeden folder na wierzchu - wchodze do niego.
string sZrodlo = sRozp;
string[] aDirs = Directory.GetDirectories(sRozp);
string[] aFiles = Directory.GetFiles(sRozp);
if (aDirs.Length == 1 && aFiles.Length == 0) sZrodlo = aDirs[0];

// Przy narzedziu jednoplikowym biore folder, w ktorym lezy exe - reszta
// archiwum (zrodla, dokumentacja) jest niepotrzebna.
if (s.Splaszcz) {
string[] aExe = Directory.GetFiles(sZrodlo, s.Plik, SearchOption.AllDirectories);
if (aExe.Length == 0) return false;
sZrodlo = Path.GetDirectoryName(aExe[0]);
}

string sCel = Path.Combine(KatalogDoZapisu(), s.Katalog);
if (Directory.Exists(sCel)) {
try { Directory.Delete(sCel, true); } catch {}
}
Directory.CreateDirectory(sCel);
KopiujDrzewo(sZrodlo, sCel);

return Jest(s);
} catch {
return false;
} finally {
try { if (Directory.Exists(sTmp)) Directory.Delete(sTmp, true); } catch {}
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
List<string> lZrobione = new List<string>();
Dictionary<string,string> dWersje = CzytajWersje();

foreach (Skladnik s in Lista()) {
bool bJest = Jest(s);

// Tryb ostrozny: przy starcie zajmuje sie tylko tym, czego NIE MA.
// Podmiana dzialajacego narzedzia w tle byla by niegrzeczna - moglaby
// trafic w chwile, gdy uzytkownik wlasnie z niego korzysta.
if (bTylkoBrakujace && bJest) continue;

string sWersja, sAdres;
if (!Najnowsza(s, out sWersja, out sAdres)) continue;

string sMam = dWersje.ContainsKey(s.Nazwa) ? dWersje[s.Nazwa] : "";
if (bJest && sMam == sWersja) continue;

if (Zainstaluj(s, sAdres)) {
dWersje[s.Nazwa] = sWersja;
ZapiszWersje(dWersje);
lZrobione.Add(s.Nazwa + " " + sWersja);
}
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
public static string BrakujaceDlaPolecenia(string sPolecenie) {
if (sPolecenie == null) return "";
string sMale = sPolecenie.ToLower();
foreach (Skladnik s in Lista()) {
if (sMale.Contains(s.Plik.ToLower()) && !Jest(s)) return s.Nazwa;
}
return "";
}

// PRZEKIEROWANIE SCIEZKI DO PROFILU.  Wpisy w EdSharp.ini wskazuja
// %ProgDir%\Convert\..., czyli katalog instalacji.  Gdy narzedzie zostalo
// dociagniete do profilu (bo do Program Files nie wolno pisac), wpis prowadzi
// w puste miejsce i konwersja pada - mimo ze narzedzie JEST.  Dlatego przed
// uruchomieniem podmieniamy sciezke na ta, w ktorej plik naprawde lezy.
//
// Podmieniam tylko wtedy, gdy sciezka z wpisu NIE istnieje, a nasza istnieje -
// instalacja systemowa zawsze wygrywa, gdy jest kompletna.
public static string NaprawSciezkeNarzedzia(string sPolecenie) {
if (String.IsNullOrEmpty(sPolecenie)) return sPolecenie;
try {
string sMale = sPolecenie.ToLower();
foreach (Skladnik s in Lista()) {
string sExe = s.Plik.ToLower();
int i = sMale.IndexOf(sExe);
if (i < 0) continue;

// Wycinam sciezke do exe z wiersza polecenia: od poczatku (albo od
// cudzyslowu) do konca nazwy pliku.
int iKon = i + sExe.Length;
int iPocz = 0;
for (int j = i; j >= 0; j--) {
char c = sPolecenie[j];
if (c == '"') { iPocz = j + 1; break; }
}
string sStara = sPolecenie.Substring(iPocz, iKon - iPocz);
if (File.Exists(sStara)) return sPolecenie;   // instalacja kompletna

string sNowa = SciezkaExe(s);
if (sNowa.Length == 0) return sPolecenie;     // nie mamy nic lepszego

return sPolecenie.Substring(0, iPocz) + sNowa + sPolecenie.Substring(iKon);
}
} catch {}
return sPolecenie;
}

} // class Skladniki
} // namespace EdSharp
