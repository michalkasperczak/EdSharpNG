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
//    Blad PRZED File.Replace zostawia oryginal bajt w bajt taki, jaki byl.
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
// GDZIE JEST PUNKT BEZ POWROTU.  File.Replace jest granica: PRZED nia kazde
// wyjscie bledem zostawia dysk nietkniety, PO niej dokument jest juz
// podmieniony i wycofac sie nie da.  Dlatego zaraz po udanym File.Replace
// oddajemy sciezke kopii i odswiezamy odcisk, JESZCZE PRZED koncowymi
// sprawdzeniami - one moga tylko dodac ostrzezenie (zwrot false z powodem), ale
// nigdy nie udaja, ze zapisu nie bylo.  Inaczej uzytkownik nie mialby jak
// wrocic do poprzedniej wersji, a stary odcisk blokowalby mu nastepny zapis
// falszywym "plik zmienil sie poza edytorem".
//
// ODCISK NOWEJ TRESCI POCHODZI Z PLIKU PRZEJSCIOWEGO, nie z oryginalu po
// podmianie: czytanie po File.Replace przyjeloby za nasza tresc cudzy zapis,
// ktory wszedl w te szpare, i przy nastepnym Control+S cicho nadpisalibysmy
// cudza prace.
//
// NAZWA KOPII JEST REZERWOWANA ATOMOWO (FileMode.CreateNew), bo samo
// sprawdzenie File.Exists pozwalaloby dwom rownoleglym zapisom wybrac to samo
// "-001" - drugi File.Replace skasowalby kopie pierwszego.
//
// ZALEZNOSCI.  Zaden odnosnik do App, Util, Ini, Dialog ani WinForms - stad
// mierzalne osobnym programem (testy/harness_zapis_oryginalu.cs).

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

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
// false -> sBlad mowi dlaczego. Przed podmiana oryginal jest nietkniety;
//          po podmianie komunikat ostrzega, a sBackupPath podaje dostepna kopie.
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

// ODCISK NOWEJ TRESCI LICZYMY TERAZ, Z PLIKU PRZEJSCIOWEGO - NIE PO PODMIANIE.
// Gdybysmy czytali go z oryginalu po File.Replace, w szpare miedzy podmiana a
// odczytem moglby wejsc cudzy zapis i przyjelibysmy JEGO bajty za wlasne; przy
// nastepnym Control+S straz "plik zmienil sie poza edytorem" juz by nie
// zadzialala i cicho nadpisalibysmy cudza prace.  Tresc pliku przejsciowego to
// dokladnie to, co za chwile znajdzie sie w oryginale.
string sNowyOdcisk;
try { sNowyOdcisk = SumaKontrolna(sStage); }
catch (Exception ex) {
Sprzataj(sStage);
sBlad = "Nie moge odczytac wyniku konwersji: " + ex.Message;
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

// WYBOR NAZWY KOPII I PODMIANA MUSZA BYC JEDNA CALOSCIA, WYLACZNA W SKALI
// SYSTEMU.  Zmierzone sonda na File.Replace: dwa rownolegle Replace na tym
// samym oryginale potrafia OBA zglosic sukces, a mimo to jedna z kopii
// zapasowych zostaje skasowana - ginie wersja dokumentu, dla ktorej robimy
// kopie.  Sama rezerwacja nazwy tego nie zamyka (wyscig jest w systemie
// plikow, nie w wyborze nazwy), dlatego bierzemy nazwany muteks - dziala tez
// miedzy DWOMA INSTANCJAMI edytora, nie tylko miedzy watkami.
string sKopia = "";
Mutex mtx = null;
bool bMam = false;
try {
try {
mtx = new Mutex(false, NazwaMuteksu(sOrig));
try { bMam = mtx.WaitOne(30000); }
catch (AbandonedMutexException) { bMam = true; }   // poprzednik padl - wchodzimy
}
catch (Exception ex) {
Sprzataj(sStage);
sBlad = "Cannot lock the original document for saving: " + ex.Message;
return false;
}
if (!bMam) {
Sprzataj(sStage);
sBlad = "Another save is still using this document. Please try again.";
return false;
}
// A writer may have waited behind another save. Recheck AFTER acquiring ownership.
try {
if (SumaKontrolna(sOrig) != link.Fingerprint) {
Sprzataj(sStage);
sBlad = "The original changed while waiting to save. Save stopped.";
return false;
}
} catch (Exception ex) {
Sprzataj(sStage);
sBlad = "Cannot verify the original before replacement: " + ex.Message;
return false;
}

sKopia = WolnaNazwaKopii(sKopie, sBase, sExt);
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
Sprzataj(sKopia);              // zwalniamy zarezerwowana nazwe - podmiany nie bylo
sBlad = "Nie moge podmienic pliku zrodlowego: " + ex.Message;
return false;
}

// SPRAWDZENIE POD MUTEKSEM: kopia MUSI istniec i miec tresc.  Zmierzone: przy
// rownoleglym Replace kopia potrafi zniknac, choc Replace zglosil sukces.
long iKopia = -1;
try { if (File.Exists(sKopia)) iKopia = new FileInfo(sKopia).Length; } catch {}
if (iKopia <= 0) {
sBackupPath = "";
link.Fingerprint = sNowyOdcisk;      // dokument JEST podmieniony - odcisk musi to opisywac
Sprzataj(sStage);
sBlad = "Plik zostal podmieniony, ale kopia zapasowa nie powstala - rownolegly zapis tego samego pliku.";
return false;
}
}
finally {
if (mtx != null) { try { if (bMam) mtx.ReleaseMutex(); } catch {} try { mtx.Close(); } catch {} }
}

// OD TEJ LINII PODMIANA JEST FAKTEM: oryginal ma nowa tresc, a stara lezy w
// kopii.  Dlatego od tego miejsca NIE WOLNO zwracac "nie zapisano" bez oddania
// sciezki kopii i bez odswiezenia odcisku - inaczej wolajacy nie wie, ze
// dokument juz sie zmienil, nie ma jak wrocic do poprzedniej wersji, a stary
// odcisk kazalby przy nastepnym zapisie skłamać, ze "ktos zmienil plik poza
// edytorem", i zablokowalby zapis WLASNEJ pracy uzytkownika.
sBackupPath = sKopia;
link.Fingerprint = sNowyOdcisk;
Sprzataj(sStage);              // File.Replace zwykle je usuwa; na wszelki wypadek

// Sprawdzenia po podmianie sa OSTRZEZENIEM, nie wycofaniem - wycofac sie juz
// nie da.  Zwracamy false tylko po to, zeby wolajacy powiedzial o klopocie;
// sBackupPath i odcisk sa juz ustawione powyzej.
long iNowy = -1;
try { if (File.Exists(sOrig)) iNowy = new FileInfo(sOrig).Length; } catch {}
if (iNowy <= 0) { sBlad = "Po podmianie plik zrodlowy jest pusty. Poprzednia wersja jest w kopii zapasowej."; return false; }

return true;
}

// Nazwa muteksu wspolna dla WSZYSTKICH procesow piszacych ten sam plik.
// Sciezka jest nieporownywalna wielkoscia liter na Windows, wiec normalizujemy;
// znaki niedozwolone w nazwie obiektu jadra zastepuje odcisk sciezki.
static string NazwaMuteksu(string sOrig) {
string sKlucz = sOrig.ToLowerInvariant();
StringBuilder sb = new StringBuilder("Local\\EdSharpOriginalSave-");
using (SHA256 alg = SHA256.Create()) {
byte[] a = alg.ComputeHash(Encoding.UTF8.GetBytes(sKlucz));
for (int i = 0; i < 16; i++) sb.Append(a[i].ToString("x2", CultureInfo.InvariantCulture));
}
return sb.ToString();
}

// Rezerwacja nazwy kopii: nazwa jest nie tylko WOLNA, ale od razu ZAJETA
// pustym plikiem utworzonym atomowo (CreateNew).  Bez tego dwa rownolegle
// zapisy tego samego dokumentu (dwie instancje edytora) wybieraja to samo
// "-001", a drugi File.Replace KASUJE kopie pierwszego - ginie dokladnie ta
// wersja, dla ktorej kopie robimy.  File.Replace nadpisuje ten pusty plik
// stara trescia oryginalu.
static string WolnaNazwaKopii(string sKatalog, string sBase, string sExt) {
for (int i = 1; i <= 9999; i++) {
string s = Path.Combine(sKatalog, sBase + "-" + i.ToString("000", CultureInfo.InvariantCulture) + sExt);
try {
using (new FileStream(s, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {}
return s;
}
catch (IOException) { continue; }          // ktos wlasnie zajal te nazwe
catch (UnauthorizedAccessException) { return ""; }
}
return "";
}

static void Sprzataj(string sFile) {
try { if (!String.IsNullOrEmpty(sFile) && File.Exists(sFile)) File.Delete(sFile); } catch {}
}

} // OriginalFormatSave
} // EdSharp
