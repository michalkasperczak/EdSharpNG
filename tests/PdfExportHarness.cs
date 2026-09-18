// PROGRAM TESTOWY DO PdfExport.  Mierzy realne zlozenie PDF przez Edge oraz
// zachowanie przy bledach.  Uruchamiany z Windows (potrzebuje msedge.exe);
// nie dotyka GUI EdSharpa ani czytnika ekranu.
//
// Kompilacja i uruchomienie: tests/run-pdf-export-harness.sh

using System;
using System.IO;
using System.Text;
using EdSharp;

public static class PdfExportHarness {

static int iBlad = 0;
static int iTest = 0;

static void Ok(bool bWarunek, string sOpis) {
iTest++;
if (bWarunek) Console.WriteLine("OK   " + sOpis);
else { iBlad++; Console.WriteLine("BLAD " + sOpis); }
}

// Polska tresc: naglowek, akapit z linkiem, lista.  Dokladnie ten HTML
// sprawdza potem skrypt Pythona w gotowym PDF.
const string HTML_PROBKA =
"<!DOCTYPE html><html lang=\"pl\"><head><meta charset=\"utf-8\">"
+ "<title>Probka EdSharp</title></head><body>"
+ "<h1>Zazolc gesla jazn</h1>"
+ "<p>Akapit ze <a href=\"https://example.org/sciezka\">odnosnikiem testowym</a> w srodku.</p>"
+ "<h2>Podnaglowek z polskimi znakami: ąćęłńóśźż</h2>"
+ "<ul><li>Pierwszy punkt listy</li><li>Drugi punkt listy</li></ul>"
+ "</body></html>";

public static int Main(string[] args) {
string sStaging = (args.Length > 0) ? args[0] : @"C:\EdSharpPdfTest";
Console.OutputEncoding = Encoding.UTF8;
Directory.CreateDirectory(sStaging);

int iTempPrzed = LiczKatalogiRobocze();

// 1. FindBrowser znajduje zainstalowana przegladarke.
string sExe = PdfExport.FindBrowser();
Console.WriteLine("FindBrowser: " + (sExe == "" ? "(nic)" : sExe));
Ok(sExe != "" && File.Exists(sExe), "FindBrowser zwraca istniejacy msedge.exe");

string sErr;

// 2. REALNY ZAPIS PDF.  Plik zostaje na dysku - sprawdza go potem Python.
string sPdf = Path.Combine(sStaging, "harness-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".pdf");
DateTime dtStart = DateTime.UtcNow;
bool bR = PdfExport.Render(HTML_PROBKA, sPdf, out sErr);
double dSek = (DateTime.UtcNow - dtStart).TotalSeconds;
Console.WriteLine("Render: " + bR + " w " + dSek.ToString("F1") + " s, blad=[" + sErr + "]");
Ok(bR, "Render konczy sie sukcesem");
Ok(bR && sErr == "", "przy sukcesie komunikat bledu jest pusty");
Ok(File.Exists(sPdf), "plik PDF powstal");
long iLen = File.Exists(sPdf) ? new FileInfo(sPdf).Length : -1;
Console.WriteLine("Dlugosc PDF: " + iLen);
Ok(iLen > 1000, "plik PDF nie jest pusty");
Ok(PodpisPdf(sPdf), "plik zaczyna sie od %PDF-");
Console.WriteLine("PDF-DO-SPRAWDZENIA: " + sPdf);

// 3. ISTNIEJACY PLIK WYNIKOWY JEST NIETKNIETY.  Metoda nie nadpisuje niczego.
string sZajety = Path.Combine(sStaging, "zajety-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".pdf");
const string TRESC_STARA = "STARA TRESC - NIE WOLNO JEJ RUSZAC";
File.WriteAllText(sZajety, TRESC_STARA);
bool bR2 = PdfExport.Render(HTML_PROBKA, sZajety, out sErr);
Ok(!bR2, "Render odmawia, gdy plik wynikowy istnieje");
Ok(sErr.IndexOf("juz istnieje") >= 0, "komunikat mowi, ze plik juz istnieje: [" + sErr + "]");
Ok(File.Exists(sZajety) && File.ReadAllText(sZajety) == TRESC_STARA,
"istniejacy plik zostal nietkniety");
File.Delete(sZajety);

// 4. BRAK PRZEGLADARKI: komunikat, nie wyjatek.
string sBrak = Path.Combine(sStaging, "brak-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".pdf");
bool bR3 = PdfExport.Render(HTML_PROBKA, sBrak, @"C:\Nie\Ma\Takiego\msedge.exe", out sErr);
Ok(!bR3, "Render odmawia przy braku przegladarki");
Ok(sErr.IndexOf("Microsoft Edge") >= 0, "komunikat nazywa brakujacy program: [" + Skrot(sErr) + "]");
Ok(!File.Exists(sBrak), "przy braku przegladarki nie zostaje zaden plik wynikowy");

// 5. PROGRAM, KTORY NIE JEST PRZEGLADARKA: zaden falszywy sukces.
string sObcy = Path.Combine(sStaging, "obcy-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".pdf");
string sWhoami = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "whoami.exe");
if (File.Exists(sWhoami)) {
bool bR4 = PdfExport.Render(HTML_PROBKA, sObcy, sWhoami, out sErr);
Ok(!bR4, "Render odmawia, gdy uruchomiony program nie tworzy PDF");
Ok(sErr.IndexOf("nie utworzyla pliku PDF") >= 0, "komunikat mowi o braku pliku: [" + Skrot(sErr) + "]");
Ok(!File.Exists(sObcy), "po nieudanej probie nie zostaje plik wynikowy");
}
else Console.WriteLine("POMINIETE: brak whoami.exe");

// 6. NIE-PDF POD SCIEZKA WYNIKOWA: podpis jest sprawdzany, nie zakladany.
//    Symulacja: program kopiujacy tekst tam, gdzie mial byc PDF, to nadal blad.
string sSmiec = Path.Combine(sStaging, "smiec-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".pdf");
string sCmd = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
if (File.Exists(sCmd)) {
// Podajemy wlasny "silnik", ktory zapisuje NIE-PDF pod sciezka celu.
string sBat = Path.Combine(sStaging, "fejk-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".cmd");
File.WriteAllText(sBat, "@echo off\r\n> \"" + sSmiec + "\" echo to nie jest pdf\r\n", Encoding.ASCII);
bool bR5 = PdfExport.Render(HTML_PROBKA, sSmiec, sBat, out sErr);
Ok(!bR5, "Render odmawia, gdy wynik nie jest PDF-em");
Ok(sErr.IndexOf("nie jest dokumentem PDF") >= 0, "komunikat mowi, ze to nie PDF: [" + Skrot(sErr) + "]");
Ok(!File.Exists(sSmiec), "nie-PDF zostal usuniety");
File.Delete(sBat);
}

// 7. Puste wejscia.
Ok(!PdfExport.Render("", Path.Combine(sStaging, "x.pdf"), out sErr) && sErr != "",
"pusty HTML jest odrzucony z komunikatem");
Ok(!PdfExport.Render(HTML_PROBKA, "", out sErr) && sErr != "",
"pusta nazwa pliku jest odrzucona z komunikatem");
Ok(!PdfExport.Render(HTML_PROBKA, @"C:\NieMaTakiegoKatalogu-" + Guid.NewGuid().ToString("N") + @"\a.pdf", out sErr)
&& sErr.IndexOf("Katalog") >= 0,
"brak katalogu celu jest odrzucony z komunikatem");

// 8. SPRZATANIE: zadnego katalogu roboczego ani profilu po sobie.
int iTempPo = LiczKatalogiRobocze();
Console.WriteLine("Katalogi edsharp-pdf-* w TEMP: przed=" + iTempPrzed + " po=" + iTempPo);
Ok(iTempPo <= iTempPrzed, "po testach nie zostaja katalogi robocze edsharp-pdf-*");

Console.WriteLine("---");
Console.WriteLine("Testow: " + iTest + ", bledow: " + iBlad);
return (iBlad == 0) ? 0 : 1;
}

static int LiczKatalogiRobocze() {
try { return Directory.GetDirectories(Path.GetTempPath(), "edsharp-pdf-*").Length; }
catch { return 0; }
}

static bool PodpisPdf(string sFile) {
try {
byte[] ab = new byte[5];
using (FileStream fs = File.OpenRead(sFile)) { if (fs.Read(ab, 0, 5) < 5) return false; }
return Encoding.ASCII.GetString(ab) == "%PDF-";
}
catch { return false; }
}

static string Skrot(string s) {
if (s == null) return "";
s = s.Replace("\r", " ").Replace("\n", " ");
return (s.Length > 90) ? s.Substring(0, 90) + "..." : s;
}

}
