// HARNESS ZAPISU FORMATOW (zgloszenie MK 18.09.2026, wiersz 273 UWAGI).
//
// MIERZY SKUTEK NA DYSKU, nie deklaracje w kodzie: wola PRAWDZIWY Pandoc,
// czyta powstale pliki i sprawdza ich PIERWSZE BAJTY.  To jedyny sposob, zeby
// odroznic prawdziwy .docx (archiwum ZIP, zaczyna sie od "PK") od Markdowna
// zapisanego pod nazwa .docx - a wlasnie to robil stary Control+Shift+S.
//
// KONTROLA NEGATYWNA JEST W SRODKU (punkt 0): harness najpierw odtwarza STARE
// zachowanie (tresc kontrolki wpisana do pliku .docx) i ZADA, zeby wyszedl
// plik BEZ naglowka ZIP.  Gdyby ten punkt przechodzil inaczej, znaczyloby to,
// ze sonda nie odroznia naprawy od stanu przed naprawa.
//
// URUCHOMIENIE (z WSL):
//   cp ZapisFormatow.cs testy/harness_zapis_formatow.cs /mnt/c/EdSharpSaveTest/
//   cd /mnt/c/EdSharpSaveTest
//   csc.exe /nologo /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /out:harness.exe harness_zapis_formatow.cs ZapisFormatow.cs
//   ./harness.exe
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using EdSharp;

public class HarnessZapisFormatow {
static int iPass = 0, iFail = 0;
static StringBuilder log = new StringBuilder();

static void W(string s) { log.AppendLine(s); Console.WriteLine(s); }
static void Sprawdz(bool b, string s) {
if (b) { iPass++; W("PASS: " + s); } else { iFail++; W("FAIL: " + s); }
}

const string MD =
"# Naglowek pierwszy\r\n\r\nAkapit z **pogrubieniem** i [linkiem](https://example.com).\r\n\r\n"
+ "- punkt jeden\r\n- punkt dwa\r\n\r\n## Naglowek drugi\r\n\r\nPolskie litery: zażółć gęślą jaźń.\r\n";

static string sKat = @"C:\EdSharpSaveTest";
static string sPandoc = Path.Combine(sKat, @"Convert\Pandoc\pandoc.exe");

// Encoding.Latin1 NIE ISTNIEJE w .NET Framework 4.8 (dodane w .NET 5) - a program
// jest budowany wlasnie na 4.8, wiec sonda musi uzywac tego, co tam jest.
// Strona 28591 (ISO-8859-1) mapuje bajt 1:1 na znak, wiec pozwala szukac napisu
// w pliku BINARNYM bez wyjatku o zlym kodowaniu.
static readonly Encoding ENC_BAJT = Encoding.GetEncoding(28591);

// Odtworzenie wpisow z sekcji [Export] pliku EdSharp.ini - te same polecenia,
// tylko ze sciezka do pandoca podstawiona na stala, bo tu nie ma App.ProgDir.
static string CzytajWpis(string sKlucz) {
Dictionary<string, string> d = new Dictionary<string, string>();
d["md2docx"] = "\"" + sPandoc + "\" \"%SourceLong%\" -f gfm -t docx --reference-doc \"%DataDir%" + "\\" + "reference.docx\" -o %Target%";
d["md2odt"]  = "\"" + sPandoc + "\" \"%SourceLong%\" -f gfm -t odt -o %Target%";
d["md2epub"] = "\"" + sPandoc + "\" \"%SourceLong%\" -f gfm -t epub -o %Target%";
d["md2html"] = "\"" + sPandoc + "\" \"%SourceLong%\" -f gfm -t html -o %Target%";
d["md2htm"]  = "\"" + sPandoc + "\" \"%SourceLong%\" -f gfm -t html -o %Target%";
d["md2rtf"]  = "\"" + sPandoc + "\" \"%SourceLong%\" -f gfm -t rtf -o %Target%";
d["md2pdf"]  = "\"" + sPandoc + "\" \"%SourceLong%\" -f gfm -t latex -o %Target%";
return d.ContainsKey(sKlucz) ? d[sKlucz] : "";
}

// Odpowiednik Util.ExpandCommandLine w zakresie, ktory tu uzywamy.
static string Rozwin(string sCmd, string sSource, string sTarget) {
sCmd = sCmd.Replace("%SourceLong%", sSource);
sCmd = sCmd.Replace("%Source%", sSource);
sCmd = sCmd.Replace("%TargetLong%", sTarget);
sCmd = sCmd.Replace("%Target%", sTarget);
// Tak jak Util.ExpandCommandLine (EdSharp.cs 24063) - inaczej nie da sie
// zmierzyc przypadku brakujacego wzorca dokumentu.
sCmd = sCmd.Replace("%DataDir%", sKat);
sCmd = sCmd.Replace("%ProgDir%", sKat);
return sCmd.Trim();
}

// Odpowiednik Util.RunHideWait - TAK SAMO dzieli polecenie po pierwszej parze
// cudzyslowow i NIE przepuszcza go przez powloke (UseShellExecute = false).
// Bez tego pomiar nie odtwarza programu (references/konwersja-formatow.md).
static int Uruchom(string sCommand) {
string sExe, sArgs;
sCommand = sCommand.Trim();
if (sCommand.StartsWith("\"")) {
int iEnd = sCommand.IndexOf('"', 1);
sExe = sCommand.Substring(1, iEnd - 1);
sArgs = sCommand.Substring(iEnd + 1).Trim();
}
else {
int i = sCommand.IndexOf(' ');
if (i < 0) { sExe = sCommand; sArgs = ""; }
else { sExe = sCommand.Substring(0, i); sArgs = sCommand.Substring(i + 1).Trim(); }
}
ProcessStartInfo psi = new ProcessStartInfo(sExe, sArgs);
psi.UseShellExecute = false;
psi.CreateNoWindow = true;
psi.WindowStyle = ProcessWindowStyle.Hidden;
Process p = Process.Start(psi);
if (p != null) p.WaitForExit();
return (p != null) ? p.ExitCode : -1;
}

static string BrakujaceNic(string sCmd) { return ""; }

static byte[] Pierwsze(string sFile, int iLen) {
try {
byte[] ab = File.ReadAllBytes(sFile);
int n = Math.Min(iLen, ab.Length);
byte[] a = new byte[n];
Array.Copy(ab, a, n);
return a;
} catch { return new byte[0]; }
}

static bool JestZip(string sFile) {
byte[] ab = Pierwsze(sFile, 2);
return ab.Length == 2 && ab[0] == 0x50 && ab[1] == 0x4B;   // "PK"
}

public static int Main(string[] args) {
string sPraca = Path.Combine(sKat, "praca_harness");
if (Directory.Exists(sPraca)) { try { Directory.Delete(sPraca, true); } catch {} }
Directory.CreateDirectory(sPraca);

W("== KONTROLA SONDY: czy Pandoc w ogole jest ==");
Sprawdz(File.Exists(sPandoc), "pandoc.exe jest na miejscu (" + sPandoc + ")");
if (!File.Exists(sPandoc)) { Zapisz(); return 3; }

// ---------------------------------------------------------------- punkt 0 ---
W("");
W("== 0. KONTROLA NEGATYWNA: STARE zachowanie Control+Shift+S ==");
// Tak dzialal stary kod: Dialog.SaveFile daje nazwe, child.SaveTextOrRtfFile
// wpisuje TRESC KONTROLKI bajt w bajt.  Rozszerzenie .docx, zawartosc Markdown.
string sStary = Path.Combine(sPraca, "stary.docx");
File.WriteAllText(sStary, MD, new UTF8Encoding(true));
Sprawdz(File.Exists(sStary), "stara droga utworzyla plik o nazwie .docx");
Sprawdz(!JestZip(sStary), "KONTROLA NEGATYWNA: stary plik .docx NIE jest archiwum ZIP, czyli NIE jest dokumentem Worda");
Sprawdz(File.ReadAllText(sStary).Contains("# Naglowek pierwszy"),
"KONTROLA NEGATYWNA: w pliku .docx siedzi surowy Markdown ze znacznikiem #");

// ---------------------------------------------------------------- punkt 1 ---
W("");
W("== 1. NOWA DROGA: md -> docx, odt, epub, html, rtf ==");
string[,] aa = new string[,] {
{"docx", "zip"}, {"odt", "zip"}, {"epub", "zip"}, {"html", "text"}, {"rtf", "text"}
};
for (int i = 0; i < aa.GetLength(0); i++) {
string sExt = aa[i, 0], sTyp = aa[i, 1];
string sCel = Path.Combine(sPraca, "nowy." + sExt);
WynikZapisu w = ZapisFormatow.Konwertuj(MD, "md", sCel, CzytajWpis, Rozwin, Uruchom, BrakujaceNic);
Sprawdz(w.Udane, sExt + ": konwersja zameldowala sukces" + (w.Udane ? "" : "  [" + w.Powod + "]"));
if (!w.Udane) continue;
long iLen = new FileInfo(sCel).Length;
Sprawdz(iLen > 0, sExt + ": plik nie jest pusty (" + iLen + " B)");
if (sTyp == "zip") {
Sprawdz(JestZip(sCel), sExt + ": plik JEST archiwum ZIP, czyli prawdziwym dokumentem (naglowek PK)");
Sprawdz(!File.ReadAllText(sCel, ENC_BAJT).Contains("# Naglowek pierwszy"),
sExt + ": w pliku NIE MA surowego Markdowna - tresc zostala przekonwertowana");
}
else {
string s = File.ReadAllText(sCel);
Sprawdz(!s.Contains("# Naglowek pierwszy"), sExt + ": znacznik Markdowna '#' zniknal - tresc przeszla przez konwerter");
if (sExt == "html") Sprawdz(s.Contains("<h1") && s.Contains("<li"), "html: naglowek <h1> i lista <li> sa w pliku");
if (sExt == "html") Sprawdz(s.Contains("<html") && s.ToLower().Contains("charset"),
"html: plik jest KOMPLETNY (<html> + deklaracja kodowania), a nie urywkiem");
if (sExt == "rtf") Sprawdz(s.StartsWith("{\\rtf"), "rtf: plik zaczyna sie od {\\rtf");
if (sExt == "rtf") Sprawdz(s.Contains("fonttbl"), "rtf: plik ma tablice czcionek, czyli jest dokumentem dla Worda");
}
}

// ---------------------------------------------------------------- punkt 2 ---
W("");
W("== 2. BUFOR EDYCJI ZOSTAJE PRZY MARKDOWNIE ==");
// Metoda konwersji NIE MA prawa ruszyc pliku zrodlowego ani go podmienic.
string sMd = Path.Combine(sPraca, "dokument.md");
File.WriteAllText(sMd, MD, new UTF8Encoding(false));
DateTime dtPrzed = File.GetLastWriteTimeUtc(sMd);
string sCelD = Path.Combine(sPraca, "dokument.docx");
WynikZapisu w2 = ZapisFormatow.Konwertuj(MD, "md", sCelD, CzytajWpis, Rozwin, Uruchom, BrakujaceNic);
Sprawdz(w2.Udane, "zapis do docx udal sie");
Sprawdz(File.Exists(sMd), "plik .md nadal istnieje");
Sprawdz(File.GetLastWriteTimeUtc(sMd) == dtPrzed, "plik .md NIE zostal dotkniety przez konwersje");
Sprawdz(File.ReadAllText(sMd).Contains("# Naglowek pierwszy"), "tresc Markdowna w pliku .md jest nietknieta");

// ---------------------------------------------------------------- punkt 3 ---
W("");
W("== 3. NIEUDANA KONWERSJA NIE KASUJE ISTNIEJACEGO PLIKU ==");
// Najwazniejszy punkt calej zmiany.  Stara droga
// (Util.ConvertString2FileFormat) robi File.Delete(sTarget) PRZED
// uruchomieniem konwertera - przy braku narzedzia uzytkownik traci stary plik.
string sIstn = Path.Combine(sPraca, "istniejacy.docx");
File.WriteAllBytes(sIstn, Encoding.UTF8.GetBytes("WAZNA STARA TRESC UZYTKOWNIKA"));
long iPrzed = new FileInfo(sIstn).Length;
// Wpis, ktorego narzedzie NIE ISTNIEJE - dokladnie przypadek "brak Pandoca".
Func<string, string> fnZly = delegate(string s) {
return "\"" + Path.Combine(sKat, @"Convert\NieMaTakiego\brak.exe") + "\" \"%SourceLong%\" -o %Target%";
};
WynikZapisu w3 = ZapisFormatow.Konwertuj(MD, "md", sIstn, fnZly, Rozwin, Uruchom, BrakujaceNic);
Sprawdz(!w3.Udane, "brak narzedzia zameldowany jako PORAZKA, nie jako sukces");
Sprawdz(w3.Powod.Length > 0, "porazka ma powod do powiedzenia: \"" + w3.Powod + "\"");
Sprawdz(File.Exists(sIstn), "ISTNIEJACY plik uzytkownika PRZETRWAL nieudana konwersje");
Sprawdz(File.Exists(sIstn) && new FileInfo(sIstn).Length == iPrzed, "tresc istniejacego pliku jest nietknieta (" + iPrzed + " B)");
Sprawdz(File.ReadAllText(sIstn).Contains("WAZNA STARA TRESC"), "stara tresc jest dokladnie ta sama");

// ---------------------------------------------------------------- punkt 4 ---
W("");
W("== 4. PO NIEUDANEJ KONWERSJI NIE ZOSTAJA SMIECI ==");
string[] asPliki = Directory.GetFiles(sPraca, "*.edsharp-*");
Sprawdz(asPliki.Length == 0, "zero plikow tymczasowych po wszystkich probach (znaleziono " + asPliki.Length + ")");

// ---------------------------------------------------------------- punkt 5 ---
W("");
W("== 5. PDF: UCZCIWY KOMUNIKAT ZAMIAST CICHEJ PORAZKI ==");
string sSilnik = ZapisFormatow.SilnikPdf(delegate(string s) {
return ZapisFormatow.JestWSciezce(s, new string[] { Path.Combine(sKat, "Convert") });
});
W("   silnik PDF znaleziony: " + (sSilnik.Length == 0 ? "(zaden)" : sSilnik));
string sCelPdf = Path.Combine(sPraca, "dokument.pdf");
WynikZapisu w5 = ZapisFormatow.Konwertuj(MD, "md", sCelPdf, CzytajWpis, Rozwin, Uruchom, BrakujaceNic);
if (sSilnik.Length == 0) {
Sprawdz(!w5.Udane, "bez silnika PDF konwersja meldouje PORAZKE (a nie falszywy sukces)");
Sprawdz(!File.Exists(sCelPdf), "zaden plik .pdf nie zostal po sobie zostawiony");
Sprawdz(ZapisFormatow.KomunikatBrakuSilnikaPdf().Contains("pdflatex"),
"komunikat o braku silnika wymienia konkretne programy do zainstalowania");
Sprawdz(ZapisFormatow.KomunikatBrakuSilnikaPdf().Contains("DOCX"),
"komunikat podaje obejscie tu i teraz (zapisz jako DOCX/ODT/HTML)");
}
else {
Sprawdz(w5.Udane, "silnik PDF jest, wiec PDF powstal");
Sprawdz(File.Exists(sCelPdf) && Pierwsze(sCelPdf, 4).Length == 4, "plik PDF ma tresc");
}

// ---------------------------------------------------------------- punkt 6 ---
W("");
W("== 6. FILTR OKNA ZAPISU MOWI, CO PROGRAM POTRAFI ==");
string sFiltr = ZapisFormatow.BudujFiltr("md", CzytajWpis);
W("   filtr md: " + sFiltr);
Sprawdz(sFiltr.StartsWith("Markdown files"), "Markdown jest PIERWSZA pozycja, czyli domyslna");
Sprawdz(sFiltr.Contains("*.docx"), "filtr oferuje docx (zgloszenie MK: 'nie wiem czy jest MD')");
Sprawdz(sFiltr.Contains("*.epub"), "filtr oferuje epub");
Sprawdz(sFiltr.Contains("*.pdf"), "filtr oferuje pdf");
Sprawdz(sFiltr.Contains("*.md"), "filtr oferuje md");
Sprawdz(sFiltr.Contains("Word document"), "nazwy sa mowione slowem, nie samym rozszerzeniem");

// STARA DROGA NIE MOZE STRACIC NICZEGO (dodatek nie blokuje podstawy).
string sFiltrTxt = ZapisFormatow.BudujFiltr("txt", CzytajWpis);
W("   filtr txt: " + sFiltrTxt);
Sprawdz(!sFiltrTxt.Contains("*.docx"), "dla .txt NIE obiecujemy docx (nie ma wpisu txt2docx)");
Sprawdz(sFiltrTxt.Contains("*.rtf"), "dla .txt zostaje stara pozycja rtf");
Sprawdz(sFiltrTxt.Contains("*.*"), "filtr zawsze ma 'All files'");

// ---------------------------------------------------------------- punkt 7 ---
W("");
W("== 7. BRAMKA: KTORY ZAPIS IDZIE PRZEZ KONWERTER ==");
Sprawdz(ZapisFormatow.WymagaKonwersji("md", "docx", CzytajWpis), "md -> docx: przez konwerter");
Sprawdz(ZapisFormatow.WymagaKonwersji("md", "epub", CzytajWpis), "md -> epub: przez konwerter");
Sprawdz(!ZapisFormatow.WymagaKonwersji("md", "md", CzytajWpis), "md -> md: PO STAREMU (to nie konwersja)");
Sprawdz(!ZapisFormatow.WymagaKonwersji("md", "txt", CzytajWpis), "md -> txt: PO STAREMU (txt nie jest formatem bogatym)");
Sprawdz(!ZapisFormatow.WymagaKonwersji("txt", "docx", CzytajWpis), "txt -> docx: PO STAREMU (brak wpisu txt2docx)");
Sprawdz(!ZapisFormatow.WymagaKonwersji("txt", "rtf", CzytajWpis), "txt -> rtf: PO STAREMU, czyli ostrzezenie o zapisie tekstem zostaje");
Sprawdz(!ZapisFormatow.WymagaKonwersji("md", "docx", delegate(string s) { return ""; }),
"gdy w ustawieniach NIE MA wpisu, konwersji nie obiecujemy");
Sprawdz(ZapisFormatow.KluczKonwersji("markdown", "docx") == "md2docx", "rozszerzenie .markdown mapuje sie na klucz md2*");

// ---------------------------------------------------------------- punkt 8 ---
W("");
W("== 8. NADPISANIE ISTNIEJACEGO PLIKU UDANA KONWERSJA ==");
string sNad = Path.Combine(sPraca, "nadpisz.docx");
File.WriteAllText(sNad, "stara wersja dokumentu");
WynikZapisu w8 = ZapisFormatow.Konwertuj(MD, "md", sNad, CzytajWpis, Rozwin, Uruchom, BrakujaceNic);
Sprawdz(w8.Udane, "nadpisanie udalo sie");
Sprawdz(JestZip(sNad), "po nadpisaniu plik JEST prawdziwym docx (PK), a nie stara trescia");
Sprawdz(!File.ReadAllText(sNad, ENC_BAJT).Contains("stara wersja dokumentu"), "stara tresc zostala zastapiona");

// ---------------------------------------------------------------- punkt 9 ---
W("");
W("== 9. POLSKIE LITERY PRZECHODZA PRZEZ KONWERSJE ==");
string sPl = Path.Combine(sPraca, "polskie.html");
WynikZapisu w9 = ZapisFormatow.Konwertuj(MD, "md", sPl, CzytajWpis, Rozwin, Uruchom, BrakujaceNic);
Sprawdz(w9.Udane, "konwersja do html udala sie");
if (w9.Udane) {
string sTresc = File.ReadAllText(sPl, new UTF8Encoding(false));
Sprawdz(sTresc.Contains("zażółć gęślą jaźń"), "tekst z polskimi literami jest w pliku poprawnie (UTF-8)");
}

// --------------------------------------------------------------- punkt 10 ---
W("");
W("== 10. WPIS BEZ '-s' DAWAL URYWEK, NIE DOKUMENT (poprawka w locie) ==");
// KONTROLA NEGATYWNA NA SUROWYM WPISIE Z EdSharp.ini: uruchamiam polecenie
// DOKLADNIE tak, jak stoi w pliku ustawien (bez DopiszStandalone) i pokazuje,
// ze wynik NIE jest dokumentem.  Bez tego punktu nie wiadomo, czy '-s' cokolwiek
// zmienia, czy tylko wyglada madrze.
string sSurowy = Path.Combine(sPraca, "surowy.rtf");
string sSrcRaw = Path.Combine(sPraca, "surowy_src.md");
File.WriteAllText(sSrcRaw, MD, new UTF8Encoding(false));
Uruchom(Rozwin(CzytajWpis("md2rtf"), sSrcRaw, sSurowy));
if (File.Exists(sSurowy)) {
string sR = File.ReadAllText(sSurowy);
Sprawdz(!sR.StartsWith("{\\rtf"), "KONTROLA NEGATYWNA: surowy wpis md2rtf z ini daje plik BEZ naglowka {\\rtf");
Sprawdz(!sR.Contains("fonttbl"), "KONTROLA NEGATYWNA: surowy wpis md2rtf nie daje tablicy czcionek - to urywek");
}
else Sprawdz(false, "KONTROLA NEGATYWNA: surowy wpis md2rtf w ogole nie utworzyl pliku");

// Sama poprawka wpisu - na napisach, bez uruchamiania.
string sWpisRtf = CzytajWpis("md2rtf");
string sPoRtf = ZapisFormatow.DopiszStandalone(sWpisRtf, "rtf");
Sprawdz(sPoRtf.Contains(" -s "), "do wpisu md2rtf dopisany zostal przelacznik -s");
Sprawdz(sPoRtf.IndexOf(" -s ") < sPoRtf.IndexOf(" -o "), "przelacznik -s stoi PRZED -o, czyli w miejscu na argumenty");
Sprawdz(ZapisFormatow.DopiszStandalone(sPoRtf, "rtf") == sPoRtf, "drugie wywolanie NIE dopisuje -s po raz drugi");
Sprawdz(ZapisFormatow.DopiszStandalone(CzytajWpis("md2html"), "html").Contains(" -s "), "wpis md2html tez dostaje -s");
// Formaty spakowane sa standalone z definicji - tam nie ruszamy niczego.
Sprawdz(ZapisFormatow.DopiszStandalone(CzytajWpis("md2docx"), "docx") == CzytajWpis("md2docx"),
"wpisu md2docx NIE ruszamy (docx jest standalone z definicji)");
Sprawdz(ZapisFormatow.DopiszStandalone(CzytajWpis("md2epub"), "epub") == CzytajWpis("md2epub"),
"wpisu md2epub NIE ruszamy");
// Wpis, ktory JUZ ma --standalone, zostaje bez zmian.
string sJuz = "\"pandoc.exe\" \"%SourceLong%\" -f gfm -t rtf --standalone -o %Target%";
Sprawdz(ZapisFormatow.DopiszStandalone(sJuz, "rtf") == sJuz, "wpis z --standalone zostaje nietkniety");
// Polecenie NIE-pandokowe zostawiamy w spokoju (np. Tidy).
string sTidy = "\"tidy.exe\" -config cfg -o %Target% %Source%";
Sprawdz(ZapisFormatow.DopiszStandalone(sTidy, "html") == sTidy, "polecenia innego narzedzia niz Pandoc nie ruszamy");

// --------------------------------------------------------------- punkt 11 ---
W("");
W("== 11. BRAKUJACY reference.docx NIE MOZE WYWRACAC ZAPISU DO WORDA ==");
// KONTROLA NEGATYWNA NA SUROWYM WPISIE md2docx Z ini: z zadanym, a
// nieistniejacym wzorcem Pandoc konczy sie bledem i NIE tworzy pliku.
string sWzor = Path.Combine(sKat, "reference.docx");
Sprawdz(!File.Exists(sWzor), "warunek pomiaru: wzorca reference.docx faktycznie NIE MA");
string sSurDocx = Path.Combine(sPraca, "surowy.docx");
string sSrcD = Path.Combine(sPraca, "surowy_src2.md");
File.WriteAllText(sSrcD, MD, new UTF8Encoding(false));
Uruchom(Rozwin(CzytajWpis("md2docx"), sSrcD, sSurDocx));
Sprawdz(!File.Exists(sSurDocx) || new FileInfo(sSurDocx).Length == 0,
"KONTROLA NEGATYWNA: surowy wpis md2docx z brakujacym wzorcem NIE daje dokumentu");

// Po wycieciu przelacznika ten sam wpis ma dzialac.
string sRozw = Rozwin(CzytajWpis("md2docx"), sSrcD, Path.Combine(sPraca, "bezwzorca.docx"));
Sprawdz(sRozw.Contains("--reference-doc"), "warunek pomiaru: rozwiniete polecenie ma --reference-doc");
string sCzyste = ZapisFormatow.UsunBrakujacyWzorzec(sRozw, File.Exists);
Sprawdz(!sCzyste.Contains("--reference-doc"), "brakujacy wzorzec zostal WYCIETY z polecenia");
Sprawdz(!sCzyste.Contains("reference.docx"), "sciezka wzorca tez zniknela, nie zostala jako blad skladni");
Sprawdz(sCzyste.Contains("-t docx") && sCzyste.Contains("-o "), "reszta polecenia jest nietknieta");

// ISTNIEJACEGO wzorca NIE WOLNO wycinac - inaczej zgubilibysmy styl firmowy.
string sIstnWzor = Path.Combine(sPraca, "wzorzec.docx");
File.Copy(Path.Combine(sPraca, "nowy.docx"), sIstnWzor, true);
string sZWzor = "\"" + sPandoc + "\" \"a.md\" -t docx --reference-doc \"" + sIstnWzor + "\" -o \"b.docx\"";
Sprawdz(ZapisFormatow.UsunBrakujacyWzorzec(sZWzor, File.Exists) == sZWzor,
"ISTNIEJACY wzorzec zostaje w poleceniu bez zmian");

// I calosc razem: zapis do docx prawdziwym wpisem z ini musi sie udac.
string sDocxReal = Path.Combine(sPraca, "realny.docx");
WynikZapisu w11 = ZapisFormatow.Konwertuj(MD, "md", sDocxReal, CzytajWpis, Rozwin, Uruchom, BrakujaceNic);
Sprawdz(w11.Udane, "zapis do docx PRAWDZIWYM wpisem z ini udal sie" + (w11.Udane ? "" : "  [" + w11.Powod + "]"));
Sprawdz(w11.Udane && JestZip(sDocxReal), "i wyszedl prawdziwy dokument Worda (naglowek PK)");

// --------------------------------------------------------------- punkt 12 ---
W("");
W("== 12. FILTR ZBUDOWANY Z PRAWDZIWEGO EdSharp.ini ==");
// Poprzednie punkty uzywaja tablicy wpisow odtworzonej w harnessie.  Ten
// czyta PRAWDZIWY plik ustawien z repozytorium, zeby lista formatow w oknie
// zgadzala sie z tym, co uzytkownik ma naprawde.
string sIni = @"C:\EdSharpSaveTest\EdSharp.ini";
if (!File.Exists(sIni)) Sprawdz(false, "warunek pomiaru: brak kopii EdSharp.ini w katalogu pomiaru");
else {
Dictionary<string, string> dIni = new Dictionary<string, string>();
bool bWSekcji = false;
foreach (string sL in File.ReadAllLines(sIni)) {
string sT = sL.Trim();
if (sT.StartsWith("[")) { bWSekcji = sT.ToLower() == "[export]"; continue; }
if (!bWSekcji || sT.Length == 0 || sT.StartsWith(";")) continue;
int iR = sT.IndexOf('=');
if (iR <= 0) continue;
dIni[sT.Substring(0, iR).Trim().ToLower()] = sT.Substring(iR + 1).Trim();
}
W("   wpisow w sekcji [Export]: " + dIni.Count);
Func<string, string> fnIni = delegate(string sK) {
string sV; return dIni.TryGetValue(sK.ToLower(), out sV) ? sV : "";
};
string sFiltrIni = ZapisFormatow.BudujFiltr("md", fnIni);
W("   filtr md z prawdziwego ini: " + sFiltrIni);
Sprawdz(sFiltrIni.Contains("*.docx"), "prawdziwe ini daje docx (wpis md2docx istnieje)");
Sprawdz(sFiltrIni.Contains("*.epub"), "prawdziwe ini daje epub");
Sprawdz(sFiltrIni.Contains("*.html"), "prawdziwe ini daje html");
Sprawdz(sFiltrIni.Contains("*.rtf"), "prawdziwe ini daje rtf");
Sprawdz(sFiltrIni.Contains("*.pdf"), "prawdziwe ini daje pdf");
// ODT NIE MA WPISU - i filtr NIE MOZE go obiecywac.
Sprawdz(!dIni.ContainsKey("md2odt"), "warunek pomiaru: w ini faktycznie NIE MA wpisu md2odt");
Sprawdz(!sFiltrIni.Contains("*.odt"), "filtr NIE obiecuje odt, bo nie ma czym go zrobic");
Sprawdz(sFiltrIni.StartsWith("Markdown files"), "Markdown nadal pierwszy, czyli domyslny");
}

// -------------------------------------------------------------- podsumowanie
W("");
W("==================================================");
W("PASS: " + iPass + "   FAIL: " + iFail);
W("==================================================");
Zapisz();
return iFail == 0 ? 0 : 1;
}

static void Zapisz() {
try { File.WriteAllText(Path.Combine(sKat, "harness_zapis_formatow.txt"), log.ToString()); } catch {}
}
}
