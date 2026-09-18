// PDF PRZEZ PRZEGLADARKE, KTORA JUZ JEST NA KOMPUTERZE.
//
// ZGLOSZENIE: zapis do PDF konczyl sie zdaniem "zainstaluj silnik PDF"
// (ZapisFormatow.KomunikatBrakuSilnikaPdf).  Pandoc bez pdflatex/typst nie
// tworzy PDF, a uzytkownik chce PLIK, nie instrukcje instalacji TeXa.
//
// Windows 10/11 ma Microsoft Edge w standardzie, a Edge (Chromium) umie
// zlozyc strone HTML w PDF bez okna i bez instalowania czegokolwiek:
//   msedge.exe --headless --print-to-pdf=<plik> <zrodlo.html>
//
// TEN PLIK ROBI TYLKO TO JEDNO.  Nie dotyka bufora edycji, nie pokazuje okien,
// nie decyduje, kiedy zapis jest bezpieczny - dostaje gotowy HTML i sciezke
// pliku wynikowego, ktory MA JESZCZE NIE ISTNIEC.  Transakcje (plik tymczasowy
// w katalogu celu, podmiana dopiero po sprawdzeniu wyniku) prowadzi wolajacy.
//
// CZTERY REGULY, KTORE TU PILNUJEMY:
//
// 1. BRAK PRZEGLADARKI TO KOMUNIKAT, NIE WYJATEK.  Process.Start rzuca
//    Win32Exception, gdy exe nie ma; bez tego uzytkownik dostaje okno
//    "Unexpected Event" zamiast zdania o tym, czego brakuje.
//
// 2. NIE MA CICHEGO SUKCESU.  Edge potrafi zakonczyc sie kodem 0 i nie zapisac
//    nic, wiec sprawdzamy: plik istnieje, ma dlugosc > 0 i zaczyna sie od
//    podpisu "%PDF-".  Dopiero to jest sukces.
//
// 3. NIE CZEKAMY BEZ KONCA I NIE ZAKLESZCZAMY SIE.  Strumienie Edge czytamy
//    asynchronicznie (ReadToEnd przy WaitForExit blokuje sie, gdy bufor potoku
//    sie zapelni), a calosc ma limit czasu; po przekroczeniu proces gubimy.
//
// 4. NIE TYKAMY PROFILU UZYTKOWNIKA.  Kazde uruchomienie dostaje wlasny,
//    tymczasowy --user-data-dir, ktory po zakonczeniu sprzatamy.  Bez tego
//    headless dobija sie do dzialajacej przegladarki uzytkownika albo psuje
//    jej profil.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace EdSharp {

public static class PdfExport {

// Limit calego skladania strony.  Edge na probce (naglowek, akapit z linkiem,
// lista) konczy w ~2 s; 45 s to zapas na zimny start i wolny dysk, a nie
// wartosc typowa.
public const int TIMEOUT_MS = 45000;

// Gdzie szukac przegladarki.  Edge stabilny instaluje sie w
// "Program Files (x86)\Microsoft\Edge\Application" (tak jest na maszynie
// testowej) albo w "Program Files\..." - sprawdzamy oba, bo zaleznie od
// kanalu i wersji Windows bywa roznie.  Zmiennych srodowiskowych uzywamy
// zamiast wpisanych na sztywno liter dysku.
static readonly string[] PODKATALOGI_EDGE = new string[] {
@"Microsoft\Edge\Application\msedge.exe",
@"Microsoft\Edge Beta\Application\msedge.exe",
@"Microsoft\Edge Dev\Application\msedge.exe"
};

// Pelna sciezka do przegladarki albo "" gdy zadnej nie ma.
public static string FindBrowser() {
List<string> lBazy = new List<string>();
lBazy.Add(Environment.GetEnvironmentVariable("ProgramFiles"));
lBazy.Add(Environment.GetEnvironmentVariable("ProgramFiles(x86)"));
lBazy.Add(Environment.GetEnvironmentVariable("ProgramW6432"));
foreach (string sBaza in lBazy) {
if (String.IsNullOrEmpty(sBaza)) continue;
foreach (string sRel in PODKATALOGI_EDGE) {
string s = "";
try { s = Path.Combine(sBaza, sRel); } catch { continue; }
try { if (File.Exists(s)) return s; } catch {}
}
}
return "";
}

// Komunikat na wypadek braku Edge.  Mowi, co zrobic, a nie tylko ze sie nie
// udalo.
public static string KomunikatBrakuPrzegladarki() {
return "Zapis do PDF wymaga przegladarki Microsoft Edge, ktorej nie widze na tym komputerze.\n\n"
+ "Edge jest czescia Windows 10 i 11 - jesli zostal usuniety, mozna go zainstalowac "
+ "ze strony Microsoftu.\n\n"
+ "Do tego czasu zapisz dokument jako DOCX, ODT albo HTML.";
}

// GLOWNE WEJSCIE.  sHtml - gotowy dokument HTML, sOutputFile - NIEISTNIEJACY
// plik .pdf (sciezka bezwzgledna), sError - powod niepowodzenia.
public static bool Render(string sHtml, string sOutputFile, out string sError) {
return Render(sHtml, sOutputFile, null, out sError);
}

// Ta sama praca z jawnie podana przegladarka - osobno, zeby program testowy
// mogl sprawdzic zachowanie przy braku exe bez odinstalowywania Edge.
public static bool Render(string sHtml, string sOutputFile, string sBrowserExe, out string sError) {
sError = "";

if (String.IsNullOrEmpty(sHtml)) { sError = "Brak tresci do zapisania."; return false; }
if (String.IsNullOrEmpty(sOutputFile)) { sError = "Brak nazwy pliku wynikowego."; return false; }

string sOut;
try { sOut = Path.GetFullPath(sOutputFile); }
catch (Exception ex) { sError = "Zla sciezka pliku wynikowego: " + ex.Message; return false; }

// PLIK WYNIKOWY MUSI BYC NOWY.  Ta metoda nie nadpisuje niczego: nie wie,
// czy trafila w plik tymczasowy, czy w dokument uzytkownika.  Wolajacy
// podaje swiezy plik w stagingu i sam decyduje o podmianie.
try {
if (File.Exists(sOut) || Directory.Exists(sOut)) {
sError = "Plik wynikowy juz istnieje: " + sOut;
return false;
}
}
catch (Exception ex) { sError = "Nie moge sprawdzic pliku wynikowego: " + ex.Message; return false; }

string sOutDir = "";
try { sOutDir = Path.GetDirectoryName(sOut); } catch {}
if (String.IsNullOrEmpty(sOutDir) || !Directory.Exists(sOutDir)) {
sError = "Katalog pliku wynikowego nie istnieje: " + sOutDir;
return false;
}

string sExe = String.IsNullOrEmpty(sBrowserExe) ? FindBrowser() : sBrowserExe;
if (String.IsNullOrEmpty(sExe)) { sError = KomunikatBrakuPrzegladarki(); return false; }
bool bJest = false;
try { bJest = File.Exists(sExe); } catch {}
if (!bJest) { sError = KomunikatBrakuPrzegladarki(); return false; }

// WLASNY KATALOG ROBOCZY: zrodlo HTML i profil przegladarki.  Nazwa z
// identyfikatorem, zeby dwa zapisy naraz sobie nie weszly w droge.
string sWork = "";
try {
sWork = Path.Combine(Path.GetTempPath(), "edsharp-pdf-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sWork);
}
catch (Exception ex) { sError = "Nie moge przygotowac katalogu roboczego: " + ex.Message; return false; }

string sSrc = Path.Combine(sWork, "dokument.html");
string sProfil = Path.Combine(sWork, "profil");

try {
// UTF-8 BEZ ZNACZNIKA BOM.  Kodowanie deklaruje sam HTML; BOM przed
// <!DOCTYPE> potrafi wyladowac jako widoczny znak na pierwszej stronie.
File.WriteAllText(sSrc, sHtml, new UTF8Encoding(false));
Directory.CreateDirectory(sProfil);
}
catch (Exception ex) {
sError = "Nie moge przygotowac zrodla HTML: " + ex.Message;
Sprzataj(sWork);
return false;
}

string sStdErr = "";
int iKod = -1;
bool bTimeout = false;

try {
ProcessStartInfo psi = new ProcessStartInfo();
psi.FileName = sExe;
psi.Arguments = BudujArgumenty(sProfil, sOut, sSrc);
psi.UseShellExecute = false;
psi.CreateNoWindow = true;
psi.RedirectStandardOutput = true;
psi.RedirectStandardError = true;
psi.WorkingDirectory = sWork;

using (Process p = new Process()) {
p.StartInfo = psi;
StringBuilder sbErr = new StringBuilder();
StringBuilder sbOut = new StringBuilder();
// ASYNCHRONICZNE CZYTANIE.  Synchroniczne ReadToEnd przed WaitForExit
// zakleszcza sie, gdy drugi strumien zapelni bufor potoku.
p.ErrorDataReceived += delegate(object o, DataReceivedEventArgs e) {
if (e.Data != null) lock (sbErr) sbErr.AppendLine(e.Data);
};
p.OutputDataReceived += delegate(object o, DataReceivedEventArgs e) {
if (e.Data != null) lock (sbOut) sbOut.AppendLine(e.Data);
};
p.Start();
p.BeginErrorReadLine();
p.BeginOutputReadLine();

if (!p.WaitForExit(TIMEOUT_MS)) {
bTimeout = true;
try { p.Kill(); } catch {}
try { p.WaitForExit(5000); } catch {}
}
else {
iKod = p.ExitCode;
}
lock (sbErr) sStdErr = sbErr.ToString();
if (sStdErr.Length == 0) { lock (sbOut) sStdErr = sbOut.ToString(); }
}
}
catch (System.ComponentModel.Win32Exception) {
// Exe nie da sie uruchomic (brak pliku, brak praw) - to komunikat, nie awaria.
sError = KomunikatBrakuPrzegladarki();
Sprzataj(sWork);
return false;
}
catch (Exception ex) {
sError = "Nie moge uruchomic przegladarki: " + ex.Message;
Sprzataj(sWork);
return false;
}

// PROFIL I ZRODLO JUZ NIE SA POTRZEBNE - proces sie zakonczyl albo zostal
// zabity.  Sprzatamy zawsze, takze przy bledzie, bo to NASZE smieci.
Sprzataj(sWork);

if (bTimeout) {
sError = "Przegladarka nie skonczyla skladania pliku w " + (TIMEOUT_MS / 1000) + " s.";
UsunNieudany(sOut);
return false;
}

string sPowod = "";
if (!WynikJestPdf(sOut, out sPowod)) {
sError = sPowod
+ (iKod == 0 ? "" : " Kod wyjscia przegladarki: " + iKod + ".")
+ SkrotBledu(sStdErr);
UsunNieudany(sOut);
return false;
}

return true;
}

// Argumenty wiersza polecen.  Tylko to, co potrzebne do zlozenia pliku:
// zadnego --no-sandbox, zadnych aktualizacji i zadnego ruchu w siec.
static string BudujArgumenty(string sProfil, string sOut, string sSrc) {
StringBuilder sb = new StringBuilder();
sb.Append("--headless");
sb.Append(" --disable-gpu");
sb.Append(" --no-first-run");
sb.Append(" --no-default-browser-check");
sb.Append(" --disable-extensions");
sb.Append(" --disable-sync");
sb.Append(" --disable-default-apps");
sb.Append(" --disable-component-update");
sb.Append(" --disable-background-networking");
sb.Append(" --no-pdf-header-footer");
// PDF OTAGOWANY: naglowki, listy i linki zostaja strukturami dokumentu,
// wiec czytnik ekranu ma co czytac.  Bez tego wychodzi plaski tekst.
sb.Append(" --export-tagged-pdf");
sb.Append(" " + Cytuj("--user-data-dir=" + sProfil));
sb.Append(" " + Cytuj("--print-to-pdf=" + sOut));
// ZRODLO JAKO URL PLIKU.  Sciezka z odstepami albo polskimi literami podana
// wprost bywa rozumiana jako fraza szukania.
sb.Append(" " + Cytuj(UrlPliku(sSrc)));
return sb.ToString();
}

static string UrlPliku(string sPath) {
try { return new Uri(sPath).AbsoluteUri; }
catch { return sPath; }
}

static string Cytuj(string s) {
if (s == null) return "\"\"";
// Sciezki Windows koncza sie czasem odwrotnym ukosnikiem; podwojenie go
// przed cudzysłowem zamykajacym chroni przed zjedzeniem cytatu.
string sT = s;
int i = sT.Length;
while (i > 0 && sT[i - 1] == '\\') i--;
sT = sT + new string('\\', sT.Length - i);
return "\"" + sT.Replace("\"", "\\\"") + "\"";
}

// Plik istnieje, ma tresc i jest PDF-em.
static bool WynikJestPdf(string sOut, out string sPowod) {
sPowod = "";
long iLen = -1;
try { if (File.Exists(sOut)) iLen = new FileInfo(sOut).Length; } catch {}
if (iLen < 0) { sPowod = "Przegladarka nie utworzyla pliku PDF."; return false; }
if (iLen == 0) { sPowod = "Przegladarka utworzyla pusty plik PDF."; return false; }
byte[] ab = new byte[5];
int iRead = 0;
try {
using (FileStream fs = new FileStream(sOut, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
iRead = fs.Read(ab, 0, 5);
}
}
catch (Exception ex) { sPowod = "Nie moge sprawdzic pliku PDF: " + ex.Message; return false; }
if (iRead < 5 || ab[0] != (byte)'%' || ab[1] != (byte)'P' || ab[2] != (byte)'D'
|| ab[3] != (byte)'F' || ab[4] != (byte)'-') {
sPowod = "Plik nie jest dokumentem PDF.";
return false;
}
return true;
}

// Niedokonczony wynik znika.  Metoda przyjmuje tylko nieistniejaca sciezke,
// wiec kasujemy wylacznie to, co sama utworzyla.
static void UsunNieudany(string sOut) {
try { if (File.Exists(sOut)) File.Delete(sOut); } catch {}
}

static string SkrotBledu(string sErr) {
if (String.IsNullOrEmpty(sErr)) return "";
string s = sErr.Replace("\r", " ").Replace("\n", " ").Trim();
if (s.Length == 0) return "";
if (s.Length > 300) s = s.Substring(0, 300) + "...";
return " Przegladarka zglosila: " + s;
}

// Katalog roboczy ginie razem z profilem.  Edge potrafi jeszcze chwile
// trzymac pliki profilu, dlatego kilka prob.
static void Sprzataj(string sDir) {
if (String.IsNullOrEmpty(sDir)) return;
for (int i = 0; i < 5; i++) {
try {
if (!Directory.Exists(sDir)) return;
Directory.Delete(sDir, true);
return;
}
catch { try { System.Threading.Thread.Sleep(200); } catch {} }
}
}

} // PdfExport
} // namespace
