// ZAPIS DO FORMATU ZRODLOWEGO - sama TRANSAKCJA na pliku, bez interfejsu.
//
// Po co to jest.  Gdy uzytkownik wlaczy opcje (domyslnie WYLACZONA), Control+S
// dokumentu zaimportowanego z .docx/.epub ma nadpisac TEN plik, a nie zapisac
// Markdown obok.  Uzytkownik swiadomie zgadza sie na utrate typografii - liczy
// sie struktura.  Ten plik odpowiada WYLACZNIE za to, zeby taka podmiana nie
// mogla zniszczyc dokumentu; sama konwersja jest w wywolaniu zwrotnym (parent).
//
// TRZY REGULY, KTORYCH TU PILNUJEMY:
//
// 1. NIE RUSZAMY ORYGINALU, DOPOKI NIE MAMY GOTOWEGO ZASTEPNIKA.  Konwerter
//    pisze do pliku przejsciowego W KATALOGU ORYGINALU (ten sam wolumen, to
//    samo rozszerzenie - Pandoc rozpoznaje format wyjsciowy po rozszerzeniu).
//    Kazde wyjscie bledem zostawia oryginal bajt w bajt taki, jaki byl.
//
// 2. SUKCES WYWOLANIA ZWROTNEGO NIE JEST DOWODEM, ZE PLIK POWSTAL.  Zmierzone
//    na Pandocu: przy bledzie zostawia plik zerowej dlugosci.  Dlatego po
//    konwersji sprawdzamy istnienie ORAZ niezerowa dlugosc.  Sprawdzenie, czy
//    to poprawny docx/epub, nalezy do strony wolajacej - tu pilnujemy tylko
//    tego, czego brak jest zawsze bledem.
//
// 3. ZMIANA ORYGINALU Z ZEWNATRZ PRZERYWA ZAPIS.  Odcisk SHA-256 z chwili
//    otwarcia jest sprawdzany PRZED konwersja i PONOWNIE po niej.  Gdy plik
//    zmienil sie w miedzyczasie (ktos edytowal go w Wordzie), zapis pada i
//    NIE dotyka pliku - cudza praca nie ginie.
//
// UCZCIWIE O WYSCIGU.  Miedzy ostatnim sprawdzeniem odcisku a File.Replace
// zostaje waskie okno, w ktorym inny program moze zapisac oryginal; nie da sie
// go zamknac bez trzymania wylacznego uchwytu przez caly czas konwersji.
// Dlatego podmiana idzie przez File.Replace z KOPIA ZAPASOWA: plik, ktory
// naprawde zostal zastapiony, laduje w .edsharp-backups i jest odzyskiwalny.
//
// ZALEZNOSCI.  Zaden odnosnik do App, Util, Ini, Dialog ani WinForms - stad
// mierzalne osobnym programem (testy/harness_zapis_oryginalu.cs).

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EdSharp {

// Zwiazek bufora z plikiem, z ktorego powstal.  Odcisk jest z chwili odczytu i
// jest aktualizowany TYLKO po udanym zapisie.
public sealed class OriginalDocumentLink {
public string Path = "";
public string Fingerprint = "";
}

public static class OriginalFormatSave {

const string KATALOG_KOPII = ".edsharp-backups";

// Odcisk SHA-256 pliku, malymi literami.  Rzuca, gdy pliku nie ma albo nie da
// sie go przeczytac - CISZA W TYM MIEJSCU BYLABY NAJGORSZA: dalibysmy pusty
// odcisk, ktory pozniej "zgadza sie" z kolejnym pustym i pozwolilby nadpisac
// plik, ktorego nigdy nie widzielismy.
public static string SumaKontrolna(string sPath) {
if (String.IsNullOrEmpty(sPath)) throw new ArgumentException("Brak sciezki pliku.");
using (FileStream fs = new FileStream(sPath, FileMode.Open, FileAccess.Read, FileShare.Read))
using (SHA256 alg = SHA256.Create()) {
byte[] a = alg.ComputeHash(fs);
StringBuilder sb = new StringBuilder(a.Length * 2);
for (int i = 0; i < a.Length; i++) sb.Append(a[i].ToString("x2", CultureInfo.InvariantCulture));
return sb.ToString();
}
}

// Zapamietanie pliku zrodlowego przy otwieraniu.  NIE UKRYWA bledu: brak pliku
// albo brak dostepu to wyjatek (FileNotFoundException / IOException /
// UnauthorizedAccessException), ktory strona wolajaca ma zlapac i powiedziec.
// Zwrocenie "nic" udawaloby, ze skojarzenia po prostu nie ma, a wtedy Control+S
// po cichu zapisalby Markdown gdzie indziej.
public static OriginalDocumentLink Capture(string sPath) {
if (String.IsNullOrEmpty(sPath)) throw new ArgumentException("Brak sciezki pliku zrodlowego.");
string sFull = Path.GetFullPath(sPath);
OriginalDocumentLink link = new OriginalDocumentLink();
link.Path = sFull;
link.Fingerprint = SumaKontrolna(sFull);
return link;
}

// Transakcja zapisu.  fnKonwertuj(markdown, plikPrzejsciowy) ma zbudowac
// dokument POD PODANA NAZWA (rozszerzenie jest juz docelowe) i zwrocic wynik;
// prawdziwa konwersja i sprawdzenie formatu naleza do strony wolajacej.
//
// true  -> oryginal podmieniony, sBackupPath wskazuje zachowana stara wersje,
//          link.Fingerprint odswiezony.
// false -> sBlad mowi dlaczego, PLIKI NA DYSKU SA NIETKNIETE (poza usunieciem
//          wlasnego pliku przejsciowego).
public static bool TrySave(
OriginalDocumentLink link, string sMarkdown,
Func<string, string, WynikZapisu> fnKonwertuj,
out string sBlad, out string sBackupPath) {

sBlad = "";
sBackupPath = "";

if (link == null || String.IsNullOrEmpty(link.Path)) { sBlad = "Brak skojarzenia z plikiem zrodlowym."; return false; }
if (fnKonwertuj == null) { sBlad = "Brak obslugi konwersji."; return false; }

string sOrig;
string sDir;
try {
sOrig = Path.GetFullPath(link.Path);
sDir = Path.GetDirectoryName(sOrig);
}
catch (Exception ex) { sBlad = "Zla sciezka pliku zrodlowego: " + ex.Message; return false; }
if (String.IsNullOrEmpty(sDir) || !Directory.Exists(sDir)) { sBlad = "Katalog pliku zrodlowego nie istnieje."; return false; }
if (!File.Exists(sOrig)) { sBlad = "Plik zrodlowy nie istnieje: " + sOrig; return false; }

// KONTROLA PRZED KONWERSJA.  Bez niej wynik konwertera nadpisalby plik, ktory
// w miedzyczasie zmienil kto inny.
string sTeraz;
try { sTeraz = SumaKontrolna(sOrig); }
catch (Exception ex) { sBlad = "Nie moge przeczytac pliku zrodlowego: " + ex.Message; return false; }
if (String.IsNullOrEmpty(link.Fingerprint)) { sBlad = "Brak odcisku pliku zrodlowego."; return false; }
if (sTeraz != link.Fingerprint) { sBlad = "Plik zrodlowy zmienil sie poza edytorem - zapis wstrzymany."; return false; }

string sExt = Path.GetExtension(sOrig);            // z kropka albo puste
string sBase = Path.GetFileNameWithoutExtension(sOrig);
string sStage = Path.Combine(sDir, sBase + ".edsharp-orig-" + Guid.NewGuid().ToString("N").Substring(0, 8) + sExt);

WynikZapisu w = null;
try { w = fnKonwertuj(sMarkdown == null ? "" : sMarkdown, sStage); }
catch (Exception ex) {
Sprzataj(sStage);
sBlad = "Konwersja nie doszla do skutku: " + ex.Message;
return false;
}
if (w == null || !w.Udane) {
// Nieudany konwerter potrafi zostawic urywek pliku - usuwamy go, zeby nie
// zaslanial katalogu uzytkownika.
Sprzataj(sStage);
sBlad = (w != null && !String.IsNullOrEmpty(w.Powod)) ? w.Powod : "Konwersja nie udala sie.";
return false;
}

// SUKCES ZGLOSZONY PRZEZ WYWOLANIE ZWROTNE NIE WYSTARCZA.
long iLen = -1;
try { if (File.Exists(sStage)) iLen = new FileInfo(sStage).Length; } catch {}
if (iLen <= 0) {
Sprzataj(sStage);
sBlad = "Konwerter nie utworzyl pliku.";
return false;
}

// KONTROLA PO KONWERSJI.  Konwerter dziala sekundy - w tym czasie plik mogl
// zostac zmieniony.  Wtedy oryginalu NIE RUSZAMY.
try {
if (SumaKontrolna(sOrig) != link.Fingerprint) {
Sprzataj(sStage);
sBlad = "Plik zrodlowy zmienil sie podczas konwersji - zapis wstrzymany.";
return false;
}
}
catch (Exception ex) {
Sprzataj(sStage);
sBlad = "Nie moge sprawdzic pliku zrodlowego po konwersji: " + ex.Message;
return false;
}

// KOPIA ZAPASOWA JEST WARUNKIEM, NIE DODATKIEM.  Nie da sie jej pominac:
// File.Replace bierze jej sciezke i sam przenosi tam stara tresc, wiec
// oryginal nigdy nie jest kasowany "na chwile przed" zapisem.
string sKopie = Path.Combine(sDir, KATALOG_KOPII);
try { if (!Directory.Exists(sKopie)) Directory.CreateDirectory(sKopie); }
catch (Exception ex) {
Sprzataj(sStage);
sBlad = "Nie moge utworzyc katalogu kopii zapasowych: " + ex.Message;
return false;
}
string sKopia = WolnaNazwaKopii(sKopie, sBase, sExt);
if (String.IsNullOrEmpty(sKopia)) {
Sprzataj(sStage);
sBlad = "Nie moge wybrac nazwy kopii zapasowej.";
return false;
}

try {
// ignoreMetadataErrors = true: brak uprawnien do przeniesienia atrybutow
// nie moze wywracac zapisu tresci.
File.Replace(sStage, sOrig, sKopia, true);
}
catch (Exception ex) {
Sprzataj(sStage);
sBlad = "Nie moge podmienic pliku zrodlowego: " + ex.Message;
return false;
}

// Po podmianie oryginal musi istniec i miec tresc, a kopia musi byc na dysku.
long iNowy = -1;
try { if (File.Exists(sOrig)) iNowy = new FileInfo(sOrig).Length; } catch {}
if (iNowy <= 0) { sBlad = "Po podmianie plik zrodlowy jest pusty."; return false; }
if (!File.Exists(sKopia)) { sBlad = "Kopia zapasowa nie powstala."; return false; }

// ODCISK ODSWIEZAMY DOPIERO TERAZ.  Wczesniejsza aktualizacja przy nieudanym
// zapisie kazalaby nam uwierzyc, ze znamy plik, ktorego nie zapisalismy.
try { link.Fingerprint = SumaKontrolna(sOrig); }
catch (Exception ex) { sBlad = "Zapis udal sie, ale nie moge odczytac nowego odcisku: " + ex.Message; return false; }

Sprzataj(sStage);              // File.Replace zwykle je usuwa; na wszelki wypadek
sBackupPath = sKopia;
return true;
}

// Kolejna wolna nazwa kopii: "raport-001.docx".  Rozszerzenie zostaje, zeby
// kopie dalo sie otworzyc dwuklikiem, a numer rosnie - stara wersja nigdy nie
// jest nadpisywana przez nowsza.
static string WolnaNazwaKopii(string sKatalog, string sBase, string sExt) {
for (int i = 1; i <= 9999; i++) {
string s = Path.Combine(sKatalog, sBase + "-" + i.ToString("000", CultureInfo.InvariantCulture) + sExt);
if (!File.Exists(s)) return s;
}
return "";
}

static void Sprzataj(string sFile) {
try { if (!String.IsNullOrEmpty(sFile) && File.Exists(sFile)) File.Delete(sFile); } catch {}
}

} // OriginalFormatSave
} // EdSharp
