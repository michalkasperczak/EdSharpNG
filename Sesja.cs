// CIAGLOSC PRACY - sesja robocza i autozapis.
//
// Zadanie 12 z listy Kasperczaka (11.09.2026), doprecyzowane 12.09.2026:
// 1. po instalacji nowej wersji program ma wstac w stanie, w jakim byl
//    (otwarte pliki, pozycje kursora, zakladki),
// 2. autozapis, zeby nie tracic tekstu,
// 3. NIE otwierac pustego "NoName", gdy jest co przywrocic.
// Do tego jego warunek z 12.09.2026, dotyczacy obu rzeczy naraz:
// "trzeba bedzie jakos wlaczyc i wylaczyc bo nie kazdy moze sobie czegos
// takiego zyczyc, tak samo jak auto zapisu".
//
// CO PROGRAM MIAL JUZ WCZESNIEJ, I DLACZEGO TO NIE WYSTARCZALO.
// W kodzie istniala sekcja [Previous]: przy WYCHODZENIU z programu CloseWindow
// zapisywalo sciezke kazdego okna, a start je otwieral. Trzy dziury, kazda
// realna:
//   - warunek zapisu brzmial "!rtb.Modified", czyli plik z niezapisana zmiana
//     NIE wchodzil na liste w ogole - a to wlasnie ten plik czlowiek chce
//     odzyskac,
//   - zapis dzial sie TYLKO przy uporzadkowanym wyjsciu; po zaniku pradu,
//     awarii albo zabiciu procesu lista nie powstawala,
//   - domyslna wartosc OpenPrevious w EdSharp.ini to "N", wiec u nikogo, kto
//     nie znalazl tego klucza recznie, nie dzialalo to wcale.
// Ta klasa nie jest wiec ozdoba do istniejacej funkcji, tylko jej dokonczeniem:
// sesja zapisuje sie W TRAKCIE pracy, przezywa awarie i obejmuje takze pozycje
// kursora oraz zakladki.
//
// DLACZEGO OSOBNY PLIK SESJI, A NIE KLUCZE W EdSharp.ini.
// EdSharp.ini to USTAWIENIA - rzecz, ktora uzytkownik moze otworzyc i edytowac
// (Manual Options robi doslownie to). Stan sesji zmienia sie co kilka sekund i
// nie jest do czytania. Trzymanie jednego w drugim znaczyloby, ze plik
// ustawien jest bez przerwy nadpisywany pod czlowiekiem, ktory go czyta.
// Sesja siedzi wiec w Sesja.ini w katalogu danych programu, obok Speech.log.
//
// ZAPIS SESJI JEST ATOMOWY (przez plik tymczasowy i podmiane). Zapis w kolko,
// co kilka sekund, w tym takze w chwili zamykania systemu, prosi sie o
// przerwanie w polowie. Plik sesji uszkodzony w polowie jest GORSZY od braku
// pliku: program wstalby wtedy z polowa okien i bez ostrzezenia. Podmiana
// gotowego pliku jest niepodzielna, wiec albo widac stara sesje, albo nowa.
//
// AUTOZAPIS NIE DOTYKA PLIKU UZYTKOWNIKA.
// Kopia idzie do OSOBNEGO pliku w katalogu danych programu, nie do dokumentu na
// dysku. To rozstrzygniecie, nie szczegol: autozapis piszacy do pliku
// zrodlowego odbieralby mozliwosc zamkniecia dokumentu bez zapisania zmian -
// czyli cofniecia calej pracy przez "nie zapisuj". Tak dziala Word i tak ma
// dzialac to: dokument zmienia sie WYLACZNIE wtedy, gdy czlowiek naciska zapis.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace EdSharp {

// Jedno okno w zapisanej sesji.
public class SesjaOkno {
public string Plik = "";
public string OriginalFormatFile = "";
public string OriginalFormatHash = "";
public int Kursor = 0;
public string Zakladki = "";
// Sciezka kopii autozapisu, gdy okno mialo niezapisane zmiany.
public string Odzysk = "";
// Czy tresc rozni sie od pliku na dysku (czyli: czy jest co odzyskiwac).
public bool Zmieniony = false;
} // SesjaOkno

public static class Sesja {

// Nazwy kluczy w [Options]. Trzymane w stalych, bo padaja w kilku miejscach:
// przy odczycie, przy zapisie i w oknie Configuration Options.
public const string OpcjaSesja = "RestoreSession";
public const string OpcjaAutozapis = "AutoSaveSeconds";

// Ile sekund miedzy zapisami, gdy autozapis jest wlaczony domyslnie.
// Trzydziesci: dosc rzadko, by nie bylo slychac pracy dysku przy pisaniu,
// dosc czesto, by strata po awarii byla mniejsza niz jeden akapit.
public const int SekundyDomyslne = 30;

// Dolna granica. Wartosc 1 sekunda z pliku ustawien znaczylaby zapis calego
// dokumentu kilkadziesiat razy na minute - przy duzym pliku program by stanal.
public const int SekundyMinimum = 5;

static string sKatalog = "";
static string sPlikSesji = "";
static string sKatalogOdzysku = "";

// Katalog na kopie odzysku i plik sesji. Wolane raz, przy starcie.
public static void Przygotuj(string sDataDir) {
sKatalog = sDataDir;
sPlikSesji = Path.Combine(sDataDir, "Sesja.ini");
sKatalogOdzysku = Path.Combine(sDataDir, "Odzysk");
try { if (!Directory.Exists(sKatalogOdzysku)) Directory.CreateDirectory(sKatalogOdzysku); }
catch {}
} // Przygotuj

public static string PlikSesji { get { return sPlikSesji; } }
public static string KatalogOdzysku { get { return sKatalogOdzysku; } }

// Czy przywracanie sesji jest wlaczone. Domyslnie NIE.
//
// DLACZEGO DOMYSLNIE WYLACZONE: przywracanie sesji zmienia to, co uzytkownik
// widzi po uruchomieniu programu - a tego nie wolno zmieniac komus, kto o to
// nie prosil (jego warunek: "nie kazdy moze sobie czegos takiego zyczyc").
// Kto wlaczy, dostaje wszystko; kto nie tknie ustawien, ma program dokladnie
// taki jak dotad.
public static bool SesjaWlaczona(string sWartosc) {
return Wlaczone(sWartosc);
} // SesjaWlaczona

// Zamiana wartosci z pliku ustawien na tak/nie. Przyjmuje Y, Yes, T, Tak, 1
// oraz odpowiedniki przeczace, bo plik ustawien czlowiek wypelnia recznie i
// wpisze to, co mu przyjdzie do glowy.
public static bool Wlaczone(string sWartosc) {
if (sWartosc == null) return false;
string s = sWartosc.Trim().Trim('"').ToLower();
if (s.Length == 0) return false;
if (s.StartsWith("y") || s.StartsWith("t") || s == "1" || s.StartsWith("on")) return true;
return false;
} // Wlaczone

// Ile sekund miedzy autozapisami; 0 znaczy WYLACZONY.
//
// Jedna wartosc zamiast dwoch kluczy (wlacznik osobno, czestotliwosc osobno):
// dwa klucze pozwalaja na stan sprzeczny - "wlaczony, co 0 sekund" - i trzeba
// go potem rozstrzygac. Liczba mowi wszystko: 0 to nie, 30 to co pol minuty.
public static int SekundyAutozapisu(string sWartosc) {
if (sWartosc == null) return 0;
string s = sWartosc.Trim().Trim('"').ToLower();
if (s.Length == 0) return 0;
if (s == "n" || s == "no" || s == "nie" || s == "off") return 0;
if (s == "y" || s == "yes" || s == "t" || s == "tak" || s == "on") return SekundyDomyslne;
int i;
if (!Int32.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out i)) return 0;
if (i <= 0) return 0;
if (i < SekundyMinimum) return SekundyMinimum;
return i;
} // SekundyAutozapisu

// Nazwa pliku kopii odzysku dla danego dokumentu.
//
// Nazwa musi byc: (1) rozna dla roznych dokumentow, takze o tej samej nazwie w
// dwoch folderach, (2) taka sama przy kazdym zapisie tego samego dokumentu, by
// kopie nie mnozyly sie bez konca, (3) legalna jako nazwa pliku Windows -
// wiec pelna sciezka z dwukropkiem i ukosnikami wprost nie przejdzie.
// Stad skrot liczbowy pelnej sciezki dopisany do samej nazwy: czlowiek widzi w
// katalogu Odzysk nazwe swojego pliku, a skrot rozstrzyga dwuznacznosci.
public static string NazwaOdzysku(string sPlik, int iOkno) {
string sBaza;
if (sPlik == null) sPlik = "";
if (sPlik.IndexOf('\\') >= 0) {
sBaza = Path.GetFileName(sPlik);
uint uSkrot = Skrot(sPlik.ToLower());
sBaza = sBaza + "." + uSkrot.ToString("x8");
}
else {
// Dokument nigdy nie zapisany na dysk (NoName1 i podobne). Sciezki nie ma,
// wiec rozstrzyga numer okna - inaczej dwa nowe dokumenty pisalyby po sobie.
sBaza = (sPlik.Length > 0 ? sPlik : "NoName") + "." + iOkno.ToString();
}
foreach (char c in Path.GetInvalidFileNameChars()) sBaza = sBaza.Replace(c, '_');
return sBaza + ".odzysk";
} // NazwaOdzysku

// Skrot tekstu (FNV-1a, 32 bity). Wlasny, bo chodzi o odroznianie sciezek, a
// nie o kryptografie, i musi dawac ZA KAZDYM URUCHOMIENIEM ten sam wynik -
// czego String.GetHashCode w .NET nie gwarantuje miedzy procesami.
public static uint Skrot(string sText) {
uint uHash = 2166136261;
if (sText == null) return uHash;
foreach (char c in sText) {
uHash ^= (uint) c;
uHash *= 16777619;
}
return uHash;
} // Skrot

// ZAPIS SESJI. Zwraca true, gdy plik naprawde powstal.
//
// Kolejnosc na liscie = kolejnosc otwierania okien, i tak samo wraca przy
// przywracaniu; inaczej Control+1 po restarcie trafialby w inne okno niz przed.
public static bool Zapisz(List<SesjaOkno> listaOkien) {
if (sPlikSesji == null || sPlikSesji.Length == 0) return false;
try {
StringBuilder sb = new StringBuilder();
sb.Append("[Sesja]\r\n");
sb.Append("Zapisano=\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"\r\n");
sb.Append("Okien=\"" + (listaOkien == null ? 0 : listaOkien.Count).ToString() + "\"\r\n");
if (listaOkien != null) {
for (int i = 0; i < listaOkien.Count; i++) {
SesjaOkno okno = listaOkien[i];
if (okno == null) continue;
sb.Append("\r\n[Okno" + (i + 1).ToString() + "]\r\n");
sb.Append("Plik=\"" + (okno.Plik ?? "") + "\"\r\n");
sb.Append("OriginalFormatFile=\"" + (okno.OriginalFormatFile ?? "") + "\"\r\n");
sb.Append("OriginalFormatHash=\"" + (okno.OriginalFormatHash ?? "") + "\"\r\n");
sb.Append("Kursor=\"" + okno.Kursor.ToString() + "\"\r\n");
sb.Append("Zakladki=\"" + (okno.Zakladki ?? "") + "\"\r\n");
sb.Append("Odzysk=\"" + (okno.Odzysk ?? "") + "\"\r\n");
sb.Append("Zmieniony=\"" + (okno.Zmieniony ? "Y" : "N") + "\"\r\n");
}
}
// PODMIANA, NIE PISANIE NA MIEJSCU. Patrz komentarz na gorze pliku:
// zapis przerwany w polowie zostawilby sesje z polowa okien.
// Kodowanie BEZ znacznika BOM: ten plik czyta WinAPI
// GetPrivateProfileString, ktore przy BOM nie widzi pierwszej sekcji
// (zmierzone 12.09.2026, testy/pomiar_ini_bom.cs - to sam kasowalo skroty w
// palecie polecen).
string sTymczasowy = sPlikSesji + ".tmp";
File.WriteAllText(sTymczasowy, sb.ToString(), new UTF8Encoding(false));
if (File.Exists(sPlikSesji)) File.Delete(sPlikSesji);
File.Move(sTymczasowy, sPlikSesji);
return true;
}
catch { return false; }
} // Zapisz

// ODCZYT SESJI. Pusta lista, gdy pliku nie ma albo jest nieczytelny.
//
// WLASNY ODCZYT, NIE Ini.ReadValue - z dwoch mierzalnych powodow.
// (1) Ini.ReadValue ma bufor 260 znakow, a lista zakladek w duzym dokumencie
//     jest dluzsza; wartosc wrocilaby UCIETA, czyli czesc zakladek przepadlaby
//     po cichu, bez zadnego bledu.
// (2) Ini.ReadValue idzie przez GetPrivateProfileString, ktore o kodowanie
//     pliku i o BOM potyka sie tak, jak to zmierzono 12.09.2026.
// Ten plik pisze i czyta ta sama klasa, wiec format jest w pelni nasz i nie ma
// po co wolac systemu.
public static List<SesjaOkno> Czytaj() {
List<SesjaOkno> lista = new List<SesjaOkno>();
sKiedyZapisano = "";
if (sPlikSesji == null || sPlikSesji.Length == 0) return lista;
if (!File.Exists(sPlikSesji)) return lista;
try {
string[] aWiersze = File.ReadAllLines(sPlikSesji, new UTF8Encoding(false));
SesjaOkno okno = null;
foreach (string sWiersz in aWiersze) {
string s = (sWiersz ?? "").Trim();
if (s.Length == 0) continue;
if (s.StartsWith("[")) {
// Nowa sekcja: poprzednie okno domykamy, jesli mialo tresc.
DodajOkno(lista, okno);
okno = s.StartsWith("[Okno", StringComparison.OrdinalIgnoreCase) ? new SesjaOkno() : null;
continue;
}
int iRownosc = s.IndexOf('=');
if (iRownosc <= 0) continue;
string sKlucz = s.Substring(0, iRownosc).Trim();
string sWartosc = Odkoduj(s.Substring(iRownosc + 1));
if (okno == null) {
if (Util.Equiv(sKlucz, "Zapisano")) sKiedyZapisano = sWartosc;
continue;
}
if (Util.Equiv(sKlucz, "Plik")) okno.Plik = sWartosc;
else if (Util.Equiv(sKlucz, "OriginalFormatFile")) okno.OriginalFormatFile = sWartosc;
else if (Util.Equiv(sKlucz, "OriginalFormatHash")) okno.OriginalFormatHash = sWartosc;
else if (Util.Equiv(sKlucz, "Kursor")) {
int iKursor = 0;
Int32.TryParse(sWartosc, out iKursor);
okno.Kursor = iKursor < 0 ? 0 : iKursor;
}
else if (Util.Equiv(sKlucz, "Zakladki")) okno.Zakladki = sWartosc;
else if (Util.Equiv(sKlucz, "Odzysk")) okno.Odzysk = sWartosc;
else if (Util.Equiv(sKlucz, "Zmieniony")) okno.Zmieniony = Wlaczone(sWartosc);
}
DodajOkno(lista, okno);
// Zdrowy rozsadek zamiast slepej wiary w plik: dwiescie okien to nie sesja,
// to plik uszkodzony albo podrzucony.
if (lista.Count > 200) lista.RemoveRange(200, lista.Count - 200);
}
catch { lista.Clear(); }
return lista;
} // Czytaj

// Okno wchodzi na liste tylko wtedy, gdy ma z czego je odtworzyc.
static void DodajOkno(List<SesjaOkno> lista, SesjaOkno okno) {
if (okno == null) return;
if ((okno.Plik ?? "").Trim().Length == 0 && (okno.Odzysk ?? "").Trim().Length == 0) return;
lista.Add(okno);
} // DodajOkno

// Data zapisu ostatnio wczytanej sesji - do zdania w oknie pytania.
static string sKiedyZapisano = "";
public static string KiedyZapisano { get { return sKiedyZapisano; } }

// Zdjecie cudzyslowow, ktorymi Ini opakowuje wartosci (WriteQuote).
public static string Odkoduj(string sWartosc) {
if (sWartosc == null) return "";
string s = sWartosc.Trim();
if (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\"")) s = s.Substring(1, s.Length - 2);
return s;
} // Odkoduj

// Skasowanie sesji - po uporzadkowanym przywroceniu albo gdy uzytkownik
// wylaczy funkcje. Kopie odzysku sprzatamy osobno, dopiero po tym, jak
// czlowiek zdecydowal, co z nimi.
public static void Wyczysc() {
try { if (sPlikSesji != null && sPlikSesji.Length > 0 && File.Exists(sPlikSesji)) File.Delete(sPlikSesji); }
catch {}
} // Wyczysc

// Usuniecie jednej kopii odzysku.
public static void UsunOdzysk(string sPlikOdzysku) {
try { if (sPlikOdzysku != null && sPlikOdzysku.Length > 0 && File.Exists(sPlikOdzysku)) File.Delete(sPlikOdzysku); }
catch {}
} // UsunOdzysk

// Kopie odzysku starsze niz podana liczba dni.
//
// Po co w ogole sprzatanie: kazda awaria zostawia kopie, a odzysk odrzucony
// przez uzytkownika ("nie, nie przywracaj") tez. Bez tego katalog rosnie bez
// konca, a w nim leza fragmenty dokumentow - takze takich, ktore czlowiek
// swiadomie postanowil porzucic.
public static int SprzatnijStareOdzyski(int iDni) {
int iUsuniete = 0;
if (sKatalogOdzysku == null || sKatalogOdzysku.Length == 0) return 0;
if (iDni <= 0) return 0;
try {
DateTime dtGranica = DateTime.Now.AddDays(-iDni);
foreach (string sPlik in Directory.GetFiles(sKatalogOdzysku, "*.odzysk")) {
try {
if (File.GetLastWriteTime(sPlik) < dtGranica) { File.Delete(sPlik); iUsuniete++; }
}
catch {}
}
}
catch {}
return iUsuniete;
} // SprzatnijStareOdzyski

// Opis sesji dla okna pytania: ile okien i z kiedy.
//
// Pytanie musi powiedziec, CO sie przywroci - inaczej czlowiek wybiera w
// ciemno. Przy czytniku ekranu to jedyne zdanie, z ktorego dowie sie, ze
// sesja jest np. z zeszlego tygodnia i moze nie byc tym, czego szuka.
public static string OpisSesji(List<SesjaOkno> lista) {
if (lista == null || lista.Count == 0) return "";
int iZmienionych = 0;
foreach (SesjaOkno okno in lista) if (okno != null && okno.Zmieniony && (okno.Odzysk ?? "").Length > 0) iZmienionych++;
string sKiedy = sKiedyZapisano;
StringBuilder sb = new StringBuilder();
sb.Append(lista.Count.ToString());
sb.Append(lista.Count == 1 ? " file was open" : " files were open");
if (sKiedy.Length > 0) sb.Append(" on " + sKiedy);
sb.Append(".");
if (iZmienionych > 0) {
sb.Append("\r\n");
sb.Append(iZmienionych.ToString());
sb.Append(iZmienionych == 1 ? " has unsaved changes that were recovered." : " have unsaved changes that were recovered.");
}
return sb.ToString();
} // OpisSesji

} // Sesja class

} // EdSharp namespace
