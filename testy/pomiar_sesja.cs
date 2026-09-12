// POMIAR CIAGLOSCI PRACY - logika sesji i autozapisu.
//
// Mierzy klase Sesja skompilowana z tego samego zrodla, ktore idzie do
// programu. Sprawdza to, na czym mozna cicho polec: zapis i odczyt w kolko
// (round-trip), sciezki z polskimi znakami i spacjami, wartosci ustawien
// wpisywane recznie przez czlowieka, plik sesji uszkodzony w polowie oraz
// dlugie listy zakladek - te ostatnie ucinal by staremu odczytowi bufor 260
// znakow.
//
// URUCHOMIENIE: pomiar_sesja.cmd na Windows.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EdSharp;

public class PomiarSesja {

static int iOk = 0;
static int iBlad = 0;

static void Sprawdz(string sOpis, bool bWarunek) {
if (bWarunek) { iOk++; Console.WriteLine("OK   " + sOpis); }
else { iBlad++; Console.WriteLine("BLAD " + sOpis); }
}

static void Rowne(string sOpis, string sOczekiwane, string sFaktyczne) {
bool b = (sOczekiwane ?? "") == (sFaktyczne ?? "");
if (b) { iOk++; Console.WriteLine("OK   " + sOpis); }
else { iBlad++; Console.WriteLine("BLAD " + sOpis + " | oczekiwano [" + sOczekiwane + "] jest [" + sFaktyczne + "]"); }
}

static void RowneInt(string sOpis, int iOczekiwane, int iFaktyczne) {
if (iOczekiwane == iFaktyczne) { iOk++; Console.WriteLine("OK   " + sOpis); }
else { iBlad++; Console.WriteLine("BLAD " + sOpis + " | oczekiwano " + iOczekiwane + " jest " + iFaktyczne); }
}

public static int Main(string[] args) {
Console.OutputEncoding = Encoding.UTF8;
string sKat = Path.Combine(Path.GetTempPath(), "PomiarSesja" + DateTime.Now.Ticks.ToString());
Directory.CreateDirectory(sKat);
Sesja.Przygotuj(sKat);

Console.WriteLine("== 1. Wartosci ustawien wpisywane recznie ==");
// Czlowiek wpisze do pliku ustawien cokolwiek, wiec kazdy wariant musi dac
// przewidywalny skutek. Y/Tak/1/on znaczy tak; wszystko inne - nie.
Sprawdz("Y wlacza", Sesja.SesjaWlaczona("Y"));
Sprawdz("y wlacza", Sesja.SesjaWlaczona("y"));
Sprawdz("Yes wlacza", Sesja.SesjaWlaczona("Yes"));
Sprawdz("Tak wlacza", Sesja.SesjaWlaczona("Tak"));
Sprawdz("T wlacza", Sesja.SesjaWlaczona("T"));
Sprawdz("1 wlacza", Sesja.SesjaWlaczona("1"));
Sprawdz("on wlacza", Sesja.SesjaWlaczona("on"));
Sprawdz("wartosc w cudzyslowach wlacza", Sesja.SesjaWlaczona("\"Y\""));
Sprawdz("N nie wlacza", !Sesja.SesjaWlaczona("N"));
Sprawdz("Nie nie wlacza", !Sesja.SesjaWlaczona("Nie"));
Sprawdz("0 nie wlacza", !Sesja.SesjaWlaczona("0"));
Sprawdz("pusta nie wlacza", !Sesja.SesjaWlaczona(""));
Sprawdz("null nie wlacza", !Sesja.SesjaWlaczona(null));
Sprawdz("bzdura nie wlacza", !Sesja.SesjaWlaczona("moze"));

Console.WriteLine();
Console.WriteLine("== 2. Sekundy autozapisu ==");
RowneInt("0 wylacza", 0, Sesja.SekundyAutozapisu("0"));
RowneInt("N wylacza", 0, Sesja.SekundyAutozapisu("N"));
RowneInt("puste wylacza", 0, Sesja.SekundyAutozapisu(""));
RowneInt("null wylacza", 0, Sesja.SekundyAutozapisu(null));
RowneInt("liczba ujemna wylacza", 0, Sesja.SekundyAutozapisu("-5"));
RowneInt("bzdura wylacza", 0, Sesja.SekundyAutozapisu("czasem"));
RowneInt("Y daje domyslne", Sesja.SekundyDomyslne, Sesja.SekundyAutozapisu("Y"));
RowneInt("Tak daje domyslne", Sesja.SekundyDomyslne, Sesja.SekundyAutozapisu("Tak"));
RowneInt("60 daje 60", 60, Sesja.SekundyAutozapisu("60"));
RowneInt("wartosc w cudzyslowach", 45, Sesja.SekundyAutozapisu("\"45\""));
// Dolna granica chroni przed zapisem calego dokumentu kilkadziesiat razy na
// minute - przy duzym pliku program by na tym stanal.
RowneInt("1 podnoszone do minimum", Sesja.SekundyMinimum, Sesja.SekundyAutozapisu("1"));
RowneInt("2 podnoszone do minimum", Sesja.SekundyMinimum, Sesja.SekundyAutozapisu("2"));
Sprawdz("minimum nie wieksze od domyslnego", Sesja.SekundyMinimum <= Sesja.SekundyDomyslne);

Console.WriteLine();
Console.WriteLine("== 3. Zapis i odczyt sesji w kolko ==");
List<SesjaOkno> lista = new List<SesjaOkno>();
SesjaOkno o1 = new SesjaOkno();
// Sciezka z polskimi znakami i spacjami - w tym pliku najlatwiej polec na
// kodowaniu, a takie sciezki ma u siebie kazdy ("Moje dokumenty", "Umowa
// zażółć.txt").
o1.Plik = @"C:\Users\Michal\Moje dokumenty\Umowa zażółć gęślą jaźń.txt";
o1.Kursor = 12345;
o1.Zakladki = "10 20 30";
o1.Zmieniony = false;
lista.Add(o1);

SesjaOkno o2 = new SesjaOkno();
o2.Plik = "";
o2.Kursor = 7;
o2.Odzysk = Path.Combine(Sesja.KatalogOdzysku, "NoName.1.odzysk");
o2.Zmieniony = true;
lista.Add(o2);

Sprawdz("zapis sie udal", Sesja.Zapisz(lista));
Sprawdz("plik sesji istnieje", File.Exists(Sesja.PlikSesji));

// Plik sesji czyta WinAPI-podobny format, ale przede wszystkim: NIE MOZE miec
// znacznika BOM (zmierzone 12.09.2026 - BOM kasuje pierwsza sekcje).
byte[] aBajty = File.ReadAllBytes(Sesja.PlikSesji);
bool bBom = aBajty.Length >= 3 && aBajty[0] == 0xEF && aBajty[1] == 0xBB && aBajty[2] == 0xBF;
Sprawdz("plik sesji BEZ znacznika BOM", !bBom);

List<SesjaOkno> wczytana = Sesja.Czytaj();
RowneInt("wrocily dwa okna", 2, wczytana.Count);
if (wczytana.Count == 2) {
Rowne("sciezka z polskimi znakami cala", o1.Plik, wczytana[0].Plik);
RowneInt("kursor pierwszego okna", 12345, wczytana[0].Kursor);
Rowne("zakladki pierwszego okna", "10 20 30", wczytana[0].Zakladki);
Sprawdz("pierwsze okno niezmienione", !wczytana[0].Zmieniony);
Rowne("nowy dokument bez sciezki", "", wczytana[1].Plik);
RowneInt("kursor drugiego okna", 7, wczytana[1].Kursor);
Sprawdz("drugie okno zmienione", wczytana[1].Zmieniony);
Rowne("sciezka kopii odzysku", o2.Odzysk, wczytana[1].Odzysk);
}
Sprawdz("data zapisu wrocila", Sesja.KiedyZapisano.Length > 0);

Console.WriteLine();
Console.WriteLine("== 4. Dluga lista zakladek (stary odczyt ucinal na 260 znakach) ==");
// To jest sedno decyzji o wlasnym odczycie: Ini.ReadValue ma bufor 260 znakow,
// a zakladki w duzym dokumencie sa dluzsze. Ucieta wartosc znaczy zakladki
// przepadle PO CICHU, bez zadnego bledu.
StringBuilder sbZak = new StringBuilder();
for (int i = 1; i <= 300; i++) { if (i > 1) sbZak.Append(" "); sbZak.Append((i * 137).ToString()); }
string sDlugie = sbZak.ToString();
Sprawdz("lista zakladek dluzsza niz 260 znakow", sDlugie.Length > 260);
List<SesjaOkno> lista2 = new List<SesjaOkno>();
SesjaOkno o3 = new SesjaOkno();
o3.Plik = @"C:\duzy.txt";
o3.Zakladki = sDlugie;
lista2.Add(o3);
Sesja.Zapisz(lista2);
List<SesjaOkno> w2 = Sesja.Czytaj();
RowneInt("jedno okno", 1, w2.Count);
if (w2.Count == 1) {
RowneInt("dlugosc listy zakladek zachowana", sDlugie.Length, (w2[0].Zakladki ?? "").Length);
Rowne("tresc listy zakladek zachowana", sDlugie, w2[0].Zakladki);
}

Console.WriteLine();
Console.WriteLine("== 5. Plik sesji uszkodzony ==");
// Zapis co kilka sekund, takze przy zamykaniu systemu, prosi sie o przerwanie
// w polowie. Program NIE MOZE sie na tym wywalic - ma zachowac sie jak przy
// braku sesji.
File.WriteAllText(Sesja.PlikSesji, "[Sesja]\r\nZapisano=\"2026-09-12", new UTF8Encoding(false));
List<SesjaOkno> w3 = Sesja.Czytaj();
Sprawdz("uciety plik nie wywala odczytu", w3 != null);
RowneInt("uciety plik daje zero okien", 0, w3.Count);

File.WriteAllText(Sesja.PlikSesji, "zupelne smieci\0\0\0 bez zadnych sekcji", new UTF8Encoding(false));
List<SesjaOkno> w4 = Sesja.Czytaj();
RowneInt("smieci daja zero okien", 0, w4.Count);

// Sekcja okna bez sciezki i bez kopii to okno, ktorego nie ma z czego
// odtworzyc - nie moze wejsc na liste, bo program otwieralby puste okna.
File.WriteAllText(Sesja.PlikSesji, "[Sesja]\r\nOkien=\"1\"\r\n\r\n[Okno1]\r\nPlik=\"\"\r\nKursor=\"5\"\r\nOdzysk=\"\"\r\n", new UTF8Encoding(false));
RowneInt("okno bez sciezki i bez kopii pomijane", 0, Sesja.Czytaj().Count);

// Kursor ujemny albo nieliczbowy z uszkodzonego pliku nie moze wyjsc na
// zewnatrz - w programie poszedlby wprost do RTB.Index.
File.WriteAllText(Sesja.PlikSesji, "[Sesja]\r\n\r\n[Okno1]\r\nPlik=\"C:\\a.txt\"\r\nKursor=\"-99\"\r\n", new UTF8Encoding(false));
List<SesjaOkno> w5 = Sesja.Czytaj();
if (w5.Count == 1) RowneInt("kursor ujemny podnoszony do zera", 0, w5[0].Kursor);
File.WriteAllText(Sesja.PlikSesji, "[Sesja]\r\n\r\n[Okno1]\r\nPlik=\"C:\\a.txt\"\r\nKursor=\"abc\"\r\n", new UTF8Encoding(false));
List<SesjaOkno> w6 = Sesja.Czytaj();
if (w6.Count == 1) RowneInt("kursor nieliczbowy daje zero", 0, w6[0].Kursor);

Console.WriteLine();
Console.WriteLine("== 6. Nazwy kopii odzysku ==");
// Nazwa musi byc stala dla tego samego pliku i rozna dla roznych - inaczej
// kopie mnoza sie bez konca albo dwa dokumenty pisza po sobie.
string sA = Sesja.NazwaOdzysku(@"C:\praca\raport.txt", 1);
string sA2 = Sesja.NazwaOdzysku(@"C:\praca\raport.txt", 5);
Rowne("ta sama sciezka daje ta sama nazwe niezaleznie od numeru okna", sA, sA2);
string sB = Sesja.NazwaOdzysku(@"D:\inne\raport.txt", 1);
Sprawdz("ta sama nazwa pliku w dwoch folderach daje ROZNE kopie", sA != sB);
Sprawdz("nazwa zawiera nazwe pliku", sA.IndexOf("raport.txt") >= 0);
Sprawdz("nazwa konczy sie .odzysk", sA.EndsWith(".odzysk"));
// Nazwa musi byc legalna w Windows - pelna sciezka z dwukropkiem nie przejdzie.
bool bLegalna = true;
foreach (char c in Path.GetInvalidFileNameChars()) if (sA.IndexOf(c) >= 0) bLegalna = false;
Sprawdz("nazwa legalna jako nazwa pliku Windows", bLegalna);
string sN1 = Sesja.NazwaOdzysku("", 1);
string sN2 = Sesja.NazwaOdzysku("", 2);
Sprawdz("dwa nowe dokumenty daja rozne kopie", sN1 != sN2);
// Skrot musi byc powtarzalny miedzy uruchomieniami - String.GetHashCode tego
// nie gwarantuje, dlatego jest wlasny FNV-1a.
Sprawdz("skrot powtarzalny", Sesja.Skrot("test") == Sesja.Skrot("test"));
Sprawdz("skrot rozny dla roznych tekstow", Sesja.Skrot(@"c:\a.txt") != Sesja.Skrot(@"c:\b.txt"));
Sprawdz("skrot dziala dla polskich znakow", Sesja.Skrot("zażółć") != Sesja.Skrot("zazolc"));

Console.WriteLine();
Console.WriteLine("== 7. Sprzatanie ==");
string sStary = Path.Combine(Sesja.KatalogOdzysku, "stary.odzysk");
string sNowy = Path.Combine(Sesja.KatalogOdzysku, "nowy.odzysk");
File.WriteAllText(sStary, "tresc");
File.WriteAllText(sNowy, "tresc");
File.SetLastWriteTime(sStary, DateTime.Now.AddDays(-30));
RowneInt("stara kopia usunieta, nowa zostaje", 1, Sesja.SprzatnijStareOdzyski(14));
Sprawdz("stara kopia nie istnieje", !File.Exists(sStary));
Sprawdz("nowa kopia istnieje", File.Exists(sNowy));
Sesja.UsunOdzysk(sNowy);
Sprawdz("UsunOdzysk usuwa", !File.Exists(sNowy));
Sesja.UsunOdzysk(Path.Combine(Sesja.KatalogOdzysku, "nie ma takiego.odzysk"));
Sprawdz("usuniecie nieistniejacej kopii nie wywala", true);
Sesja.Zapisz(lista);
Sesja.Wyczysc();
Sprawdz("Wyczysc usuwa plik sesji", !File.Exists(Sesja.PlikSesji));
RowneInt("po wyczyszczeniu zero okien", 0, Sesja.Czytaj().Count);

Console.WriteLine();
Console.WriteLine("== 8. Zdanie w oknie pytania ==");
// Przy czytniku ekranu to JEDYNE zdanie, z ktorego czlowiek dowie sie, co
// wlasciwie przywraca - musi podac liczbe plikow i date.
Sesja.Zapisz(lista);
List<SesjaOkno> w7 = Sesja.Czytaj();
string sOpis = Sesja.OpisSesji(w7);
Sprawdz("opis mowi o dwoch plikach", sOpis.IndexOf("2 files") >= 0);
Sprawdz("opis podaje date", sOpis.IndexOf("on 20") >= 0);
Sprawdz("opis wspomina o odzyskanych zmianach", sOpis.ToLower().IndexOf("unsaved") >= 0);
Rowne("pusta lista daje pusty opis", "", Sesja.OpisSesji(new List<SesjaOkno>()));
Rowne("null daje pusty opis", "", Sesja.OpisSesji(null));
List<SesjaOkno> jeden = new List<SesjaOkno>();
SesjaOkno oj = new SesjaOkno();
oj.Plik = @"C:\jeden.txt";
jeden.Add(oj);
Sesja.Zapisz(jeden);
Sesja.Czytaj();
string sOpis1 = Sesja.OpisSesji(jeden);
Sprawdz("liczba pojedyncza przy jednym pliku", sOpis1.IndexOf("1 file was open") >= 0);

Console.WriteLine();
Console.WriteLine("== 9. Brak pliku sesji ==");
Sesja.Wyczysc();
RowneInt("brak pliku daje zero okien", 0, Sesja.Czytaj().Count);
Sprawdz("brak pliku nie wywala", true);

try { Directory.Delete(sKat, true); } catch {}

Console.WriteLine();
Console.WriteLine("WYNIK: " + iOk + " OK, " + iBlad + " BLAD");
return iBlad == 0 ? 0 : 1;
}

} // PomiarSesja
