// ZAPIS FORMATOW - "Zapisz jako" naprawde zmienia FORMAT, a nie tylko nazwe.
//
// ZGLOSZENIE (docs/UWAGI-MK-18.09.2026.md, wiersz 273): "Mam otwarty jakis plik,
// naciskam Ctrl+Shift+S (...) i mam tylko format wszystkie pliki txt (...).  Ja
// myslalem ze moge zapisywac plik markdown w kazdym formacie czyli rowniez epub
// pdf i tak dalej."
//
// CO BYLO ZLE.  Control+Shift+S wolalo Dialog.SaveFile z filtrem
// "All files / Text files / Rich Text Format files", a potem
// child.SaveTextOrRtfFile - ta metoda wpisuje do pliku TRESC KONTROLKI bajt w
// bajt.  Wpisanie nazwy "raport.docx" dawalo wiec plik z rozszerzeniem .docx,
// w ktorym siedzial czysty Markdown.  Word otwiera taki plik jako uszkodzony
// albo jako tekst ze znacznikami.  Dla osoby niewidomej to najgorszy wariant:
// program powiedzial "zapisane", nazwa pliku brzmi poprawnie, a dokumentu NIE MA.
//
// CO ROBI TERAZ.  Gdy dokument jest w formacie, z ktorego umiemy konwertowac
// (Markdown i pokrewne), a cel jest formatem BOGATYM (docx, odt, epub, html,
// rtf, pdf), zapis przechodzi przez prawdziwy konwerter z sekcji [Export]
// pliku ustawien - czyli dokladnie ten sam mechanizm, ktory obsluguje
// Alt+Shift+E (Export Format).  Wynikiem jest PRAWDZIWY dokument.
//
// TRZY REGULY, KTORE TU PILNUJEMY (bez nich funkcja szkodzi zamiast pomagac):
//
// 1. BUFOR EDYCJI ZOSTAJE PRZY MARKDOWNIE.  Zapis do docx NIE przestawia
//    child.File na ten docx.  Gdyby przestawial, kolejne Control+S wpisaloby
//    Markdown bajt w bajt do pliku .docx i zniszczyloby wlasnie stworzony
//    dokument - ten sam blad, tylko o jeden krok dalej.  Dlatego konwersja jest
//    EKSPORTEM: plik powstaje, a edytujemy dalej to, co edytowalismy.
//    Z tego samego powodu NIE zerujemy znacznika "zmodyfikowany": niezapisane
//    zmiany w Markdownie nadal sa niezapisane i program musi o nie zapytac
//    przy zamykaniu.
//
// 2. NIEUDANA KONWERSJA NIE MOZE SKASOWAC ISTNIEJACEGO PLIKU.  Stara droga
//    (Util.ConvertString2FileFormat) robi File.Delete(sTarget) PRZED
//    uruchomieniem konwertera - gdy konwerter nie wstanie (brak Pandoca, brak
//    silnika PDF), uzytkownik zostaje BEZ starej i BEZ nowej wersji pliku.
//    Tutaj konwerter pisze do pliku tymczasowego W KATALOGU CELU i dopiero
//    sprawdzony wynik zastepuje cel.  Gdy sie nie udalo, cel jest nietkniety.
//
// 3. BRAK NARZEDZIA TO KOMUNIKAT, NIE CISZA I NIE FALSZYWY SUKCES.  Metoda
//    zwraca wynik z powodem, a strona wolajaca ma co powiedziec.  "Done"
//    pada TYLKO wtedy, gdy plik naprawde istnieje i nie jest pusty.
//
// ZALEZNOSCI WSTRZYKIWANE.  Ten plik NIE zna App, Util, Ini ani Dialog -
// dostaje trzy delegaty (czytanie wpisu, rozwiniecie wiersza polecenia,
// uruchomienie).  Dzieki temu kompiluje sie i mierzy w osobnym programie
// testowym, bez WinForms i bez calego EdSharp.cs (testy/harness_zapis_formatow.cs).

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EdSharp {

// Wynik zapisu z konwersja.  Osobny typ, bo "udalo sie / nie udalo" to za malo:
// strona wolajaca musi umiec POWIEDZIEC, czego zabraklo.
public class WynikZapisu {
public bool Udane;
public string Powod = "";        // pusty, gdy udane
public string BrakujaceNarzedzie = "";
public string Polecenie = "";    // do diagnostyki
public string PlikWynikowy = "";
}

public static class ZapisFormatow {

// ---------------------------------------------------------------- formaty ---

// FORMATY BOGATE - te, dla ktorych "zapisz jako" MUSI przejsc przez konwerter.
// Zapisanie do nich tresci kontrolki bajt w bajt daje plik uszkodzony.
static readonly string[] FORMATY_BOGATE = new string[] {
"docx", "odt", "epub", "epub3", "html", "htm", "rtf", "pdf"
};

// FORMATY ZRODLOWE, z ktorych umiemy konwertowac.  Lista jest krotka
// SWIADOMIE: dla zwyklego .txt zadnego wpisu txt2* w [Export] nie ma, wiec
// zapis .txt jako .rtf ma dzialac PO STAREMU (tresc kontrolki + ostrzezenie
// "Saving as plain text, not rich text").  Nowa droga nie ma prawa odebrac
// niczego drodze starej - stad osobna bramka, a nie "konwertuj wszystko".
static readonly string[] FORMATY_ZRODLOWE = new string[] {
"md", "markdown", "rst", "tex", "html", "htm", "tt"
};

// Rozszerzenie bez kropki, malymi literami.  Puste, gdy nie ma czego brac.
public static string Rozszerzenie(string sFile) {
if (String.IsNullOrEmpty(sFile)) return "";
try {
string s = Path.GetExtension(sFile);
if (String.IsNullOrEmpty(s)) return "";
return s.TrimStart('.').ToLower();
}
catch { return ""; }
}

public static bool FormatBogaty(string sExt) {
if (String.IsNullOrEmpty(sExt)) return false;
sExt = sExt.TrimStart('.').ToLower();
foreach (string s in FORMATY_BOGATE) if (s == sExt) return true;
return false;
}

public static bool FormatZrodlowy(string sExt) {
if (String.IsNullOrEmpty(sExt)) return false;
sExt = sExt.TrimStart('.').ToLower();
foreach (string s in FORMATY_ZRODLOWE) if (s == sExt) return true;
return false;
}

// Klucz sekcji [Export] dla pary formatow, np. "md2docx".  Puste, gdy para nie
// ma sensu.  Markdown ma w pliku ustawien klucze "md2*", a nie "markdown2*" -
// dlatego nazwe dlugiego rozszerzenia skracamy.
public static string KluczKonwersji(string sSourceExt, string sTargetExt) {
string sZ = (sSourceExt == null) ? "" : sSourceExt.TrimStart('.').ToLower();
string sDo = (sTargetExt == null) ? "" : sTargetExt.TrimStart('.').ToLower();
if (sZ.Length == 0 || sDo.Length == 0) return "";
if (sZ == "markdown") sZ = "md";
if (sZ == sDo) return "";
return sZ + "2" + sDo;
}

// CZY TEN ZAPIS MA PRZEJSC PRZEZ KONWERTER.  Trzy warunki naraz, kazdy z
// innego powodu:
//   - zrodlo jest formatem, z ktorego umiemy konwertowac (patrz wyzej),
//   - cel jest formatem bogatym (dla .txt i .md zapisujemy po staremu),
//   - w pliku ustawien NAPRAWDE jest wpis dla tej pary.
// Ostatni warunek jest najwazniejszy: on sprawia, ze funkcja nie obiecuje
// formatu, ktorego nikt nie skonfigurowal.  fnCzytajWpis dostaje klucz i
// zwraca wiersz polecenia albo puste.
public static bool WymagaKonwersji(string sSourceExt, string sTargetExt, Func<string, string> fnCzytajWpis) {
if (fnCzytajWpis == null) return false;
if (!FormatZrodlowy(sSourceExt)) return false;
if (!FormatBogaty(sTargetExt)) return false;
string sKlucz = KluczKonwersji(sSourceExt, sTargetExt);
if (sKlucz.Length == 0) return false;
string sCmd = "";
try { sCmd = fnCzytajWpis(sKlucz); } catch { return false; }
return !String.IsNullOrEmpty(sCmd) && sCmd.Trim().Length > 0;
}

// ------------------------------------------------------------------ filtr ---

// FILTR OKNA ZAPISU.  Uzytkownik zglosil wprost, ze w oknie "nie wiem czy jest
// MD Markdown".  Lista formatow jest tym, co czytnik ekranu wymawia w polu
// "typ pliku", wiec to ONA informuje, co program potrafi zapisac.
//
// KOLEJNOSC MA ZNACZENIE: pierwsza pozycja jest domyslna (FilterIndex = 1).
// Na pierwszym miejscu stoi Markdown, bo to format roboczy programu -
// wszystko bogate importujemy do Markdowna i w nim edytujemy.
//
// Pozycje bogate dopisujemy TYLKO dla dokumentu, z ktorego umiemy
// konwertowac.  Inaczej okno obiecywaloby .docx dla pliku .txt, a zapis
// zrobilby plik .docx z czystym tekstem w srodku - czyli dokladnie ten blad,
// ktory naprawiamy.
public static string BudujFiltr(string sSourceExt, Func<string, string> fnCzytajWpis) {
List<string> l = new List<string>();
l.Add("Markdown files (*.md)|*.md");
l.Add("Text files (*.txt)|*.txt");

if (FormatZrodlowy(sSourceExt) && fnCzytajWpis != null) {
// Nazwy mowione pelnym slowem, nie samym rozszerzeniem: czytnik ekranu
// wymawia "docx" jako litery, a "Word document" jest zrozumiale od razu.
AddJesliUmiemy(l, sSourceExt, fnCzytajWpis, "docx", "Word document (*.docx)|*.docx");
// ODT NIE MA WPISU W EdSharp.ini (sprawdzone 18.09.2026: grep "^md2" pokazuje
// docx, epub, epub3, htm, html, mw, pdf, rst, rtf, tex, txt - ODT NIE MA).
// Pozycja zostaje tu SWIADOMIE: AddJesliUmiemy pominie ja, dopoki wpisu nie
// ma, a gdy ktos doda "md2odt", format pojawi sie w oknie bez zmiany kodu.
// Dlatego lista formatow NIE jest obietnica - jest pytaniem do ustawien.
AddJesliUmiemy(l, sSourceExt, fnCzytajWpis, "odt", "OpenDocument text (*.odt)|*.odt");
AddJesliUmiemy(l, sSourceExt, fnCzytajWpis, "epub", "EPUB e-book (*.epub)|*.epub");
AddJesliUmiemy(l, sSourceExt, fnCzytajWpis, "html", "Web page (*.html)|*.html");
AddJesliUmiemy(l, sSourceExt, fnCzytajWpis, "rtf", "Rich Text Format (*.rtf)|*.rtf");
AddJesliUmiemy(l, sSourceExt, fnCzytajWpis, "pdf", "PDF document (*.pdf)|*.pdf");
}
else {
// Stary zestaw dla dokumentow, ktorych nie konwertujemy - zeby nie zabrac
// nikomu tego, co bylo.
l.Add("Rich Text Format files (*.rtf)|*.rtf");
}

l.Add("All files (*.*)|*.*");
return String.Join("|", l.ToArray());
}

static void AddJesliUmiemy(List<string> l, string sSourceExt, Func<string, string> fnCzytajWpis, string sExt, string sPozycja) {
if (WymagaKonwersji(sSourceExt, sExt, fnCzytajWpis)) l.Add(sPozycja);
}

// ------------------------------------------------- poprawka wpisu w locie ---

// WPIS BEZ "-s" DAJE URYWEK, NIE DOKUMENT.  Zmierzone 18.09.2026 na Pandocu
// 3.11:
//   md2rtf  (tak jak stoi w EdSharp.ini) -> plik zaczyna sie od "{\pard \ql..."
//                                           czyli BEZ naglowka "{\rtf1" i bez
//                                           tablicy czcionek - Word nie widzi
//                                           w tym dokumentu RTF,
//   md2html (tak jak stoi w EdSharp.ini) -> same "<h1>...", ZERO <html>, <head>
//                                           i deklaracji kodowania; przegladarka
//                                           zgaduje strone kodowa, wiec polskie
//                                           litery moga sie rozjechac.
//   z "-s": rtf zaczyna sie od "{\rtf1\ansi\deff0{\fonttbl...", html ma
//           <!DOCTYPE html> i <meta charset="utf-8">.
//
// Formaty spakowane (docx, odt, epub) sa standalone Z DEFINICJI - sprawdzone,
// ze "-s" nic tam nie zmienia (10927 B bez, 10928 B z), wiec ich nie ruszamy.
//
// POPRAWIAMY W LOCIE, W KODZIE, A NIE W PLIKU USTAWIEN UZYTKOWNIKA.  Plik
// ustawien uzytkownika BIJE plik programu, wiec poprawka w repozytorium nie
// dotarlaby do nikogo, kto juz ma swoj EdSharp.ini - a takich jest wiekszosc
// (references/konwersja-formatow.md).
static readonly string[] WYMAGAJA_STANDALONE = new string[] { "rtf", "html", "htm" };

public static string DopiszStandalone(string sCommand, string sTargetExt) {
if (String.IsNullOrEmpty(sCommand)) return sCommand;
string sExt = (sTargetExt == null) ? "" : sTargetExt.TrimStart('.').ToLower();
bool bTrzeba = false;
foreach (string s in WYMAGAJA_STANDALONE) if (s == sExt) bTrzeba = true;
if (!bTrzeba) return sCommand;
if (sCommand.IndexOf("pandoc", StringComparison.OrdinalIgnoreCase) < 0) return sCommand;

// Nie dopisujemy drugi raz - wpis moze juz byc poprawny.
if (System.Text.RegularExpressions.Regex.IsMatch(sCommand, @"(^|\s)(-s|--standalone)(\s|$)")) return sCommand;

// Wstawiamy PRZED "-o", bo tam jest miejsce na przelacznik, a nie na koncu
// za nazwa pliku wyjsciowego.
int i = sCommand.IndexOf(" -o ");
if (i < 0) return sCommand.Trim() + " -s";
return sCommand.Substring(0, i) + " -s" + sCommand.Substring(i);
}

// ------------------------------------------- brakujacy wzorzec dokumentu ---

// BRAKUJACY "--reference-doc" WYWRACA CALA KONWERSJE.
//
// Wpis md2docx w EdSharp.ini (wiersz 114) zada pliku "%DataDir%\reference.docx".
// Zmierzone 18.09.2026: TEGO PLIKU NIE MA ANI w katalogu budowania, ANI w
// repozytorium (find po calym drzewie: zero trafien).  Pandoc konczy sie wtedy
// bledem "withBinaryFile: does not exist", kod wyjscia 1, i NIE tworzy pliku
// wyjsciowego.  Czyli najwazniejszy format z calego zgloszenia - Word - nie
// dzialalby wcale, a uzytkownik uslyszalby tylko, ze zapis sie nie udal.
//
// Wzorzec jest RZECZA OPCJONALNA: sluzy do nadania stylow firmowych.  Bez niego
// Pandoc uzywa stylow wbudowanych i dokument ma poprawne naglowki - czyli to,
// czego tu potrzebujemy.  Dlatego gdy plik wzorca nie istnieje, wycinamy sam
// przelacznik, a nie przerywamy zapis.
public static string UsunBrakujacyWzorzec(string sCommand, Func<string, bool> fnIstnieje) {
if (String.IsNullOrEmpty(sCommand)) return sCommand;

// Dopasowanie: --reference-doc "sciezka" albo --reference-doc=sciezka
System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
sCommand, "--reference-doc[= ]\\s*(\"[^\"]*\"|\\S+)");
if (!m.Success) return sCommand;

string sPlik = m.Groups[1].Value.Trim('"');
bool bJest = false;
try { if (fnIstnieje != null) bJest = fnIstnieje(sPlik); } catch {}
if (bJest) return sCommand;

// Wycinamy przelacznik wraz z argumentem i porzadkujemy odstepy.
string sBez = sCommand.Remove(m.Index, m.Length);
return System.Text.RegularExpressions.Regex.Replace(sBez, @"\s{2,}", " ").Trim();
}

// -------------------------------------------------------------------- PDF ---

// PDF JEST INNY NIZ POZOSTALE I TRZEBA TO POWIEDZIEC WPROST.
//
// ZMIERZONE 18.09.2026 na Pandocu 3.11 (C:\EdSharpSaveTest):
//   docx, odt, epub, epub3, html, rtf  -- powstaja SAMYM Pandokiem, bez niczego
//                                         wiecej (10927 B, 7578 B, 5565 B, ...),
//   pdf                                -- "'pdflatex' not found. Please select a
//                                         different --pdf-engine or install
//                                         'pdflatex'" i ZERO plikow na wyjsciu.
// Sprawdzone tez --pdf-engine=typst, =wkhtmltopdf, =weasyprint: zadnego z nich
// nie ma.  Wpis md2pdf w EdSharp.ini brzmi "-t latex -o %Target%", ale Pandoc
// rozpoznaje cel po rozszerzeniu .pdf i mimo "-t latex" ZADA silnika PDF -
// czyli ten wpis nie zadziala nigdy bez doinstalowania silnika.
//
// Dlatego przy PDF sprawdzamy silnik ZAWCZASU i mowimy, czego brakuje, zamiast
// pozwolic konwerterowi milczaco nie zrobic nic.  Skladniki.Brakujace() tego
// nie wychwyci: silnik PDF nie jest naszym skladnikiem i nigdy nie byl.
static readonly string[] SILNIKI_PDF = new string[] {
"pdflatex.exe", "xelatex.exe", "lualatex.exe", "tectonic.exe",
"typst.exe", "wkhtmltopdf.exe", "weasyprint.exe", "prince.exe", "context.exe"
};

// Ktory silnik PDF jest na maszynie.  fnJest dostaje nazwe pliku
// wykonywalnego i mowi, czy da sie go znalezc (PATH albo katalog Convert) -
// wstrzykiwane, bo w programie testowym podstawiamy wlasna odpowiedz.
public static string SilnikPdf(Func<string, bool> fnJest) {
if (fnJest == null) return "";
foreach (string s in SILNIKI_PDF) {
try { if (fnJest(s)) return s; } catch {}
}
return "";
}

// Domyslne szukanie: PATH systemu plus katalogi Convert.  Osobno, zeby
// logika wyzej dala sie zmierzyc bez dotykania dysku.
public static bool JestWSciezce(string sExe, string[] asKatalogi) {
if (String.IsNullOrEmpty(sExe)) return false;
try {
if (asKatalogi != null) {
foreach (string sDir in asKatalogi) {
if (String.IsNullOrEmpty(sDir)) continue;
if (!Directory.Exists(sDir)) continue;
try {
string[] as2 = Directory.GetFiles(sDir, sExe, SearchOption.AllDirectories);
if (as2.Length > 0) return true;
} catch {}
}
}
string sPath = Environment.GetEnvironmentVariable("PATH");
if (String.IsNullOrEmpty(sPath)) return false;
foreach (string sDir in sPath.Split(';')) {
if (String.IsNullOrEmpty(sDir)) continue;
try { if (File.Exists(Path.Combine(sDir.Trim('"'), sExe))) return true; } catch {}
}
}
catch {}
return false;
}

public static string KomunikatBrakuSilnikaPdf() {
// Mowimy CO zrobic, nie tylko ze sie nie udalo.  Podajemy jedna konkretna
// propozycje (MiKTeX), bo "zainstaluj silnik PDF" nie jest instrukcja.
return "Zapis do PDF wymaga zewnetrznego silnika, ktorego nie ma na tym komputerze.\n\n"
+ "Pandoc sam nie tworzy plikow PDF - potrzebuje do tego programu skladajacego "
+ "strony (pdflatex, xelatex, typst, wkhtmltopdf albo weasyprint).\n\n"
+ "Do czasu instalacji zapisz dokument jako DOCX, ODT albo HTML - te formaty "
+ "powstaja od razu, a PDF zrobisz z nich Wordem albo przegladarka "
+ "(Drukuj do pliku PDF).";
}

// -------------------------------------------------------------- konwersja ---

// ZAPIS Z KONWERSJA.  sText to tresc bufora, sSourceExt format tej tresci,
// sTargetFile plik wskazany przez uzytkownika w oknie zapisu.
//
// Delegaty (zadnej zaleznosci od reszty programu):
//   fnCzytajWpis(klucz)                -> wiersz polecenia z sekcji [Export]
//   fnRozwin(polecenie, zrodlo, cel)   -> Util.ExpandCommandLine
//   fnUruchom(polecenie)               -> Util.RunHideWait
//   fnBrakujace(polecenie)             -> nazwa brakujacego skladnika albo puste
public static WynikZapisu Konwertuj(
string sText, string sSourceExt, string sTargetFile,
Func<string, string> fnCzytajWpis,
Func<string, string, string, string> fnRozwin,
Func<string, int> fnUruchom,
Func<string, string> fnBrakujace) {

WynikZapisu w = new WynikZapisu();
w.PlikWynikowy = sTargetFile;

if (String.IsNullOrEmpty(sTargetFile)) { w.Powod = "Nie podano pliku docelowego."; return w; }
if (fnCzytajWpis == null || fnRozwin == null || fnUruchom == null) { w.Powod = "Brak obslugi konwersji."; return w; }

string sTargetExt = Rozszerzenie(sTargetFile);
string sKlucz = KluczKonwersji(sSourceExt, sTargetExt);
if (sKlucz.Length == 0) { w.Powod = "Nie znam konwersji do tego formatu."; return w; }

string sCmdWzor = "";
try { sCmdWzor = fnCzytajWpis(sKlucz); } catch {}
if (String.IsNullOrEmpty(sCmdWzor) || sCmdWzor.Trim().Length == 0) {
w.Powod = "W ustawieniach nie ma wpisu " + sKlucz + ".";
return w;
}

// Urywek zamiast dokumentu - poprawka wpisu w locie (patrz DopiszStandalone).
sCmdWzor = DopiszStandalone(sCmdWzor, sTargetExt);

// KATALOG CELU MUSI BYC ZAPISYWALNY, I TO SPRAWDZAMY PRZED KONWERSJA.
// Inaczej konwerter dziala kilka sekund, a dopiero na koncu okazuje sie,
// ze nie ma gdzie zapisac - a uzytkownik czeka i nie wie na co.
string sDir = "";
try { sDir = Path.GetDirectoryName(Path.GetFullPath(sTargetFile)); } catch {}
if (String.IsNullOrEmpty(sDir) || !Directory.Exists(sDir)) {
w.Powod = "Katalog docelowy nie istnieje.";
return w;
}

// PLIK TYMCZASOWY LEZY W KATALOGU CELU i ma DOCELOWE ROZSZERZENIE.
//
// Oba szczegoly sa konieczne:
//   - ten sam katalog, zeby podmiana byla przeniesieniem w obrebie jednego
//     wolumenu, a nie kopiowaniem przez pol dysku,
//   - to samo rozszerzenie, bo Pandoc rozpoznaje format wyjsciowy PO
//     ROZSZERZENIU pliku; nazwa "raport.docx.tmp" dalaby blad formatu albo
//     plik w zlym formacie.
string sTemp = Path.Combine(sDir,
Path.GetFileNameWithoutExtension(sTargetFile) + ".edsharp-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "." + sTargetExt);

// ZRODLO DLA KONWERTERA W UTF-8 BEZ ZNACZNIKA BOM.  Pandoc czyta wejscie jako
// UTF-8; zapis w stronie kodowej systemu psul polskie litery, a znacznik BOM
// potrafi wyladowac w pierwszym naglowku dokumentu.
string sSrc = Path.Combine(sDir,
Path.GetFileNameWithoutExtension(sTargetFile) + ".edsharp-src-" + Guid.NewGuid().ToString("N").Substring(0, 8)
+ "." + ((sSourceExt == null) ? "txt" : sSourceExt.TrimStart('.').ToLower()));

try {
File.WriteAllText(sSrc, sText == null ? "" : sText, new UTF8Encoding(false));
}
catch (Exception ex) {
w.Powod = "Nie moge przygotowac pliku zrodlowego: " + ex.Message;
Sprzataj(sSrc); Sprzataj(sTemp);
return w;
}

string sCmd = "";
try { sCmd = fnRozwin(sCmdWzor, sSrc, sTemp); }
catch (Exception ex) {
w.Powod = "Nie moge zlozyc polecenia konwersji: " + ex.Message;
Sprzataj(sSrc); Sprzataj(sTemp);
return w;
}
// DOPIERO TERAZ, PO ROZWINIECIU.  Sciezka wzorca stoi we wpisie jako
// "%DataDir%\reference.docx" - przed rozwinieciem nie da sie sprawdzic, czy
// plik istnieje, bo to jeszcze nie jest sciezka.
sCmd = UsunBrakujacyWzorzec(sCmd, File.Exists);
w.Polecenie = sCmd;

// BRAK NARZEDZIA NIE MOZE BYC AWARIA PROGRAMU.  Bezpiecznik przeniesiony
// swiadomie z COM.Source2TargetFormat i Util.ConvertString2FileFormat:
// Process.Start rzuca Win32Exception, gdy exe nie istnieje, i bez tego
// bloku uzytkownik dostawal okno "Unexpected Event" zamiast zdania o tym,
// czego brakuje.
try {
fnUruchom(sCmd);
}
catch (System.ComponentModel.Win32Exception) {
w.Powod = "Nie moge uruchomic konwertera.";
try { if (fnBrakujace != null) w.BrakujaceNarzedzie = fnBrakujace(sCmd); } catch {}
Sprzataj(sSrc); Sprzataj(sTemp);
return w;
}
catch (Exception ex) {
w.Powod = "Konwersja nie doszla do skutku: " + ex.Message;
Sprzataj(sSrc); Sprzataj(sTemp);
return w;
}

// WYNIK MUSI ISTNIEC I NIE BYC PUSTY.  Pandoc przy bledzie potrafi zostawic
// plik zerowej dlugosci - plik "jest", wiec samo File.Exists zameldowalo by
// sukces, a dokument bylby pusty.  Zmierzone przy braku silnika PDF.
long iLen = -1;
try { if (File.Exists(sTemp)) iLen = new FileInfo(sTemp).Length; } catch {}
if (iLen <= 0) {
w.Powod = "Konwerter nie utworzyl pliku.";
try { if (fnBrakujace != null) w.BrakujaceNarzedzie = fnBrakujace(sCmd); } catch {}
Sprzataj(sSrc); Sprzataj(sTemp);
return w;
}

// DOPIERO TERAZ RUSZAMY PLIK UZYTKOWNIKA.  Do tej chwili cel jest nietkniety,
// wiec KAZDE wyjscie powyzej zostawia stara wersje na miejscu.
try {
if (File.Exists(sTargetFile)) File.Delete(sTargetFile);
File.Move(sTemp, sTargetFile);
}
catch (Exception ex) {
w.Powod = "Nie moge zapisac pliku docelowego: " + ex.Message;
Sprzataj(sSrc); Sprzataj(sTemp);
return w;
}

Sprzataj(sSrc);
w.Udane = File.Exists(sTargetFile);
if (!w.Udane) w.Powod = "Plik docelowy nie powstal.";
return w;
} // Konwertuj

static void Sprzataj(string sFile) {
try { if (!String.IsNullOrEmpty(sFile) && File.Exists(sFile)) File.Delete(sFile); } catch {}
}

// Komunikat po udanym zapisie z konwersja.  Musi powiedziec DWIE rzeczy:
// ze plik powstal, i ze dalej edytujemy Markdown - bo inaczej uzytkownik
// niewidomy nie ma skad wiedziec, ktory plik przyjmie nastepne Control+S.
public static string KomunikatSukcesu(string sTargetExt, string sNazwaBufora) {
string sExt = (sTargetExt == null) ? "" : sTargetExt.TrimStart('.').ToUpper();
string s = "Saved as " + sExt;
if (!String.IsNullOrEmpty(sNazwaBufora)) s += ", still editing " + sNazwaBufora;
return s;
}

} // ZapisFormatow class
} // EdSharp namespace
