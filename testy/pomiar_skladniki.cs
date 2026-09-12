// POMIAR: czy da sie pobrac skladniki Convert z ich prawdziwych zrodel (zadanie 8).
//
// PO CO.  Dzis narzedzia Convert (pandoc, tidy, xpdf, liblouis, astyle) pobiera
// TYLKO skrypt budowania u mnie - do instalatora NIE sa wcale pakowane
// (sprawdzone: 0 wystapien "Convert\" w EdSharp_Setup.iss).  Skutek u
// uzytkownika: konwersje po cichu nie dzialaja, a Dialog.Show pokazuje tylko
// wiersz polecenia, wiec nie wiadomo, ze BRAKUJE narzedzia.
//
// CZEGO NIE ZGADUJE.  Nie zakladam, ze adresy z Tools.inix nadal odpowiadaja
// ani ze GitHub API zwroci to, czego oczekuje regula.  Mierze KAZDE zrodlo
// osobno i wypisuje prawdziwa dlugosc pliku oraz pierwsze dwa bajty (ZIP musi
// zaczynac sie od "PK").  Bez tego pomiaru nie mam prawa napisac w programie
// "pobieram najnowsza wersje".
//
// Uruchamiac: bash uruchom_pomiar.sh testy/pomiar_skladniki.cs

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

class PomiarSkladniki {

static string Pobierz(string sUrl, out int iStatus, out string sBlad) {
iStatus = 0;
sBlad = "";
try {
HttpWebRequest req = (HttpWebRequest) WebRequest.Create(sUrl);
req.UserAgent = "EdSharpNG";
req.Timeout = 60000;
req.AllowAutoRedirect = true;
using (HttpWebResponse resp = (HttpWebResponse) req.GetResponse()) {
iStatus = (int) resp.StatusCode;
using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) {
return sr.ReadToEnd();
}
}
} catch (Exception ex) {
sBlad = ex.Message;
return "";
}
}

// Pobiera POCZATEK pliku - nie cale archiwum.  Wystarczy, zeby sprawdzic, ze
// serwer daje ZIP, a nie strone HTML z komunikatem o blokadzie.
static bool SprawdzArchiwum(string sUrl, out long lRozmiar, out string sPodpis, out string sBlad) {
lRozmiar = 0;
sPodpis = "";
sBlad = "";
try {
HttpWebRequest req = (HttpWebRequest) WebRequest.Create(sUrl);
req.UserAgent = "EdSharpNG";
req.Timeout = 90000;
req.AllowAutoRedirect = true;
using (HttpWebResponse resp = (HttpWebResponse) req.GetResponse()) {
lRozmiar = resp.ContentLength;
using (Stream st = resp.GetResponseStream()) {
byte[] ab = new byte[2];
int iRead = st.Read(ab, 0, 2);
if (iRead == 2) sPodpis = ((char) ab[0]).ToString() + ((char) ab[1]).ToString();
}
}
return sPodpis == "PK";
} catch (Exception ex) {
sBlad = ex.Message;
return false;
}
}

static void ZbadajGitHub(string sNazwa, string sRepo, string sWzorzec) {
Console.WriteLine("--- {0} (github {1})", sNazwa, sRepo);
int iStatus;
string sBlad;
string sJson = Pobierz("https://api.github.com/repos/" + sRepo + "/releases/latest", out iStatus, out sBlad);
if (sJson.Length == 0) {
Console.WriteLine("    NIE UDALO SIE odpytac wydania: {0}", sBlad);
return;
}
Match mTag = Regex.Match(sJson, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
string sWersja = mTag.Success ? mTag.Groups[1].Value : "(nie znaleziono)";
Console.WriteLine("    najnowsze wydanie: {0}", sWersja);

// Szukam adresu zalacznika pasujacego do wzorca.
string sAdres = "";
foreach (Match m in Regex.Matches(sJson, "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"")) {
string s = m.Groups[1].Value;
string sPlik = s.Substring(s.LastIndexOf('/') + 1);
if (Regex.IsMatch(sPlik, sWzorzec)) { sAdres = s; break; }
}
if (sAdres.Length == 0) {
Console.WriteLine("    ZALACZNIK pasujacy do /{0}/ NIE ISTNIEJE w tym wydaniu", sWzorzec);
return;
}
Console.WriteLine("    zalacznik: {0}", sAdres.Substring(sAdres.LastIndexOf('/') + 1));
long lRozmiar;
string sPodpis;
bool bOk = SprawdzArchiwum(sAdres, out lRozmiar, out sPodpis, out sBlad);
Console.WriteLine("    pobranie: {0}  rozmiar={1} B  podpis=\"{2}\" {3}",
  bOk ? "OK" : "BLAD", lRozmiar, sPodpis, sBlad);
}

static void ZbadajStrone(string sNazwa, string sUrlStrony, string sWzorzecWersji, string sSzablon) {
Console.WriteLine("--- {0} (wersja ze strony)", sNazwa);
int iStatus;
string sBlad;
string sHtml = Pobierz(sUrlStrony, out iStatus, out sBlad);
if (sHtml.Length == 0) {
Console.WriteLine("    strona NIE ODPOWIADA: {0}", sBlad);
return;
}
Match m = Regex.Match(sHtml, sWzorzecWersji);
if (!m.Success) {
Console.WriteLine("    WZORZEC WERSJI nie pasuje do tresci strony (zmienil sie uklad strony)");
return;
}
string sWersja = m.Groups[1].Value;
string sAdres = sSzablon.Replace("{v}", sWersja);
Console.WriteLine("    wersja na stronie: {0}", sWersja);
long lRozmiar;
string sPodpis;
bool bOk = SprawdzArchiwum(sAdres, out lRozmiar, out sPodpis, out sBlad);
Console.WriteLine("    pobranie: {0}  rozmiar={1} B  podpis=\"{2}\" {3}",
  bOk ? "OK" : "BLAD", lRozmiar, sPodpis, sBlad);
}

static void ZbadajAdres(string sNazwa, string sAdres) {
Console.WriteLine("--- {0} (adres na sztywno)", sNazwa);
long lRozmiar;
string sPodpis;
string sBlad;
bool bOk = SprawdzArchiwum(sAdres, out lRozmiar, out sPodpis, out sBlad);
Console.WriteLine("    pobranie: {0}  rozmiar={1} B  podpis=\"{2}\" {3}",
  bOk ? "OK" : "BLAD", lRozmiar, sPodpis, sBlad);
}

static void Main() {
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
Console.WriteLine("POMIAR ZRODEL SKLADNIKOW CONVERT");
Console.WriteLine();

// Te same zrodla, ktore sa dzis w Tools.inix - sprawdzam, czy nadal zyja.
ZbadajGitHub("Pandoc", "jgm/pandoc", "windows-x86_64\\.zip$");
ZbadajGitHub("Tidy", "htacg/tidy-html5", "win64.*\\.zip$");
ZbadajGitHub("liblouis", "liblouis/liblouis", "win(64|32).*\\.zip$");
ZbadajStrone("Xpdf", "https://www.xpdfreader.com/download.html",
  "Current version:\\s*([0-9.]+)", "https://dl.xpdfreader.com/xpdf-tools-win-{v}.zip");
ZbadajAdres("AStyle", "https://master.dl.sourceforge.net/project/astyle/astyle/astyle%203.6/astyle-3.6-x64.zip");

Console.WriteLine();
Console.WriteLine("KONIEC POMIARU");
}
}
