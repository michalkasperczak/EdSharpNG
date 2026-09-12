// CSV - czytanie i zapisywanie plikow z wartosciami rozdzielonymi przecinkiem.
//
// Zadanie 10 z listy Kasperczaka (11.09.2026): "CSV ma sie otwierac jako tabela
// w naszym systemie-kreatorze tabel".
//
// Do 5.0.86 plik CSV otwieral sie jak kazdy inny tekst: jeden dlugi wiersz na
// rekord, przecinki w srodku. Dla osoby czytajacej ekran czytnikiem to jest
// nie do przejscia - zeby dowiedziec sie, co stoi w trzeciej kolumnie, trzeba
// liczyc przecinki w pamieci. Kreator tabel (Insert Table, Control+Shift+T) ma
// juz dostepna siatke, w ktorej czytnik przy kazdej komorce mowi naglowek
// kolumny. Ta klasa jest mostem: zamienia plik CSV na wiersze komorek, ktore
// ta siatka przyjmuje, i z powrotem.
//
// DLACZEGO WLASNY CZYTNIK, A NIE GOTOWA BIBLIOTEKA.  EdSharpNG kompiluje sie
// jednym poleceniem csc bez menedzera pakietow; dolozenie zaleznosci znaczyloby
// przebudowe calego budowania. Sam format jest maly - caly RFC 4180 to kilka
// regul - a jego pulapki (cudzyslowy, przecinek w polu, koniec wiersza w polu)
// sa tutaj obsluzone i zmierzone.
//
// SEPARATOR ROZPOZNAJEMY SAMI.  Polski Excel zapisuje CSV ze SREDNIKIEM, bo
// przecinek jest u nas znakiem dziesietnym. Plik ze srednikami wczytany z
// zalozeniem przecinka daje jedna kolumne ze smieciami - czyli dokladnie to, z
// czym uzytkownik przyszedl. Zgadujemy z tresci, nie z ustawien systemu.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EdSharp {

public static class Csv {

// Znaki brane pod uwage jako separator, w kolejnosci sprawdzania.
// Przecinek pierwszy - jest domyslny w formacie; srednik zaraz za nim, bo
// tak zapisuje polski Excel; potem tabulator (eksport z wielu programow)
// i kreska pionowa (spotykana w eksportach z baz danych).
static readonly char[] acKandydaci = new char[] { ',', ';', '\t', '|' };

// ROZPOZNANIE SEPARATORA.
//
// Regula: wygrywa ten znak, ktory daje NAJROWNIEJSZE wiersze - czyli tyle
// samo pol w kazdym wierszu - a przy remisie ten, ktory daje ich WIECEJ niz
// jedno. Liczenie samych wystapien nie wystarcza: zdanie z przecinkami w
// polu tekstowym potrafi ich miec wiecej niz jest separatorow.
//
// Liczymy na pierwszych kilkunastu wierszach, nie na calym pliku: plik
// moze miec setki tysiecy wierszy, a separator widac od razu.
public static char RozpoznajSeparator(string sTekst) {
if (string.IsNullOrEmpty(sTekst)) return ',';

char cNajlepszy = ',';
int iNajlepszaLiczbaKolumn = 0;
bool bNajlepszyRowny = false;

foreach (char c in acKandydaci) {
List<List<string>> wiersze = Czytaj(sTekst, c, 15);
if (wiersze.Count == 0) continue;

int iPierwszy = wiersze[0].Count;
if (iPierwszy < 2) continue; // jedna kolumna = ten znak nie dzieli niczego

bool bRowny = true;
foreach (List<string> w in wiersze) {
// Ostatni wiersz bywa urwany, bo czytamy tylko poczatek pliku - nie
// traktujemy tego jako dowodu nierownosci.
if (w.Count != iPierwszy) { bRowny = false; break; }
}

// Rowny bije nierowny; przy dwoch rownych wygrywa ten z wieksza liczba
// kolumn (wiecej kolumn = dokladniejszy podzial, a nie przypadek).
bool bLepszy =
  (bRowny && !bNajlepszyRowny) ||
  (bRowny == bNajlepszyRowny && iPierwszy > iNajlepszaLiczbaKolumn);

if (bLepszy) {
cNajlepszy = c;
iNajlepszaLiczbaKolumn = iPierwszy;
bNajlepszyRowny = bRowny;
}
}

return cNajlepszy;
}

// CZY TO W OGOLE WYGLADA NA CSV.
//
// Pytanie o otwarcie jako tabela ma sie pojawiac tylko wtedy, gdy naprawde
// jest co pokazac. Plik z rozszerzeniem .csv, ktory w srodku jest zwyklym
// tekstem, ma sie otworzyc po cichu jak tekst - pytanie bez sensownej
// odpowiedzi to przerzucanie decyzji na uzytkownika (zasada z 5.0.77).
//
// Warunek: co najmniej dwa wiersze, co najmniej dwie kolumny i rowna liczba
// kolumn w kazdym sprawdzanym wierszu.
public static bool WygladaNaTabele(string sTekst, out char cSeparator, out int iKolumn, out int iWierszy) {
cSeparator = ',';
iKolumn = 0;
iWierszy = 0;
if (string.IsNullOrEmpty(sTekst)) return false;

cSeparator = RozpoznajSeparator(sTekst);
List<List<string>> probka = Czytaj(sTekst, cSeparator, 15);
if (probka.Count < 2) return false;

iKolumn = probka[0].Count;
if (iKolumn < 2) return false;

for (int i = 0; i < probka.Count; i++) {
if (probka[i].Count != iKolumn) return false;
}

// Liczbe wierszy podajemy z CALEGO pliku - probka sluzyla tylko do oceny.
iWierszy = Czytaj(sTekst, cSeparator, 0).Count;
return true;
}

public static List<List<string>> Czytaj(string sTekst) {
return Czytaj(sTekst, RozpoznajSeparator(sTekst), 0);
}

// WLASCIWY CZYTNIK (RFC 4180).
//
// Reguly, ktore ten kod realizuje i ktore latwo przeoczyc:
// - pole moze byc w cudzyslowach i wtedy separator w srodku NIE dzieli pola,
// - w polu w cudzyslowach podwojony cudzyslow ("") znaczy jeden cudzyslow,
// - pole w cudzyslowach moze zawierac KONIEC WIERSZA - rekord ciagnie sie
//   wtedy przez kilka linii pliku i dzielenie tekstu po liniach jest bledem
//   (to najczestsza pomylka w recznie pisanych czytnikach CSV),
// - konce wierszy: CRLF, LF i samo CR (stare Maki) traktujemy tak samo.
//
// iMaksWierszy = 0 znaczy caly plik; wieksze od zera - tylko tyle wierszy
// (uzywane przy rozpoznawaniu separatora, zeby nie czytac calego pliku).
public static List<List<string>> Czytaj(string sTekst, char cSeparator, int iMaksWierszy) {
List<List<string>> wynik = new List<List<string>>();
if (string.IsNullOrEmpty(sTekst)) return wynik;

List<string> wiersz = new List<string>();
StringBuilder pole = new StringBuilder();
bool bWCudzyslowach = false;
bool bPoleBylowCudzyslowach = false;

int i = 0;
int iDl = sTekst.Length;

while (i < iDl) {
char c = sTekst[i];

if (bWCudzyslowach) {
if (c == '"') {
// Podwojony cudzyslow w srodku pola to jeden znak cudzyslowu.
if (i + 1 < iDl && sTekst[i + 1] == '"') { pole.Append('"'); i += 2; continue; }
bWCudzyslowach = false;
i++;
continue;
}
pole.Append(c);
i++;
continue;
}

if (c == '"' && pole.Length == 0) {
bWCudzyslowach = true;
bPoleBylowCudzyslowach = true;
i++;
continue;
}

if (c == cSeparator) {
wiersz.Add(Wykoncz(pole, bPoleBylowCudzyslowach));
pole.Length = 0;
bPoleBylowCudzyslowach = false;
i++;
continue;
}

if (c == '\r' || c == '\n') {
wiersz.Add(Wykoncz(pole, bPoleBylowCudzyslowach));
pole.Length = 0;
bPoleBylowCudzyslowach = false;
wynik.Add(wiersz);
wiersz = new List<string>();
// CRLF to JEDEN koniec wiersza, nie dwa.
if (c == '\r' && i + 1 < iDl && sTekst[i + 1] == '\n') i += 2;
else i++;
if (iMaksWierszy > 0 && wynik.Count >= iMaksWierszy) return wynik;
continue;
}

pole.Append(c);
i++;
}

// Ostatni rekord bez konca wiersza na koncu pliku.  Pusty ogon (plik
// konczacy sie znakiem nowej linii) pomijamy - inaczej kazda tabela
// dostawalaby na koncu pusty wiersz.
if (pole.Length > 0 || bPoleBylowCudzyslowach || wiersz.Count > 0) {
wiersz.Add(Wykoncz(pole, bPoleBylowCudzyslowach));
wynik.Add(wiersz);
}

return wynik;
}

// Spacje wokol pola obcinamy TYLKO wtedy, gdy pole nie bylo w cudzyslowach.
// Cudzyslowy sa jawna deklaracja "to jest dokladna tresc" - obcinanie w nich
// czegokolwiek byloby zmienianiem danych uzytkownika.
static string Wykoncz(StringBuilder pole, bool bBylowCudzyslowach) {
string s = pole.ToString();
return bBylowCudzyslowach ? s : s.Trim();
}

// ZAPIS.
//
// W cudzyslowy bierzemy pole tylko wtedy, gdy MUSIMY: gdy zawiera separator,
// cudzyslow, koniec wiersza albo spacje na brzegu. Branie wszystkiego w
// cudzyslowy jest poprawne, ale produkuje plik nieczytelny dla czlowieka,
// a te pliki ludzie ogladaja takze w edytorze.
public static string Zapisz(List<List<string>> wiersze, char cSeparator, string sKoniecWiersza) {
if (string.IsNullOrEmpty(sKoniecWiersza)) sKoniecWiersza = "\r\n";
StringBuilder sb = new StringBuilder();
if (wiersze == null) return "";

for (int r = 0; r < wiersze.Count; r++) {
List<string> w = wiersze[r];
for (int c = 0; c < w.Count; c++) {
if (c > 0) sb.Append(cSeparator);
sb.Append(Oslon(w[c] == null ? "" : w[c], cSeparator));
}
if (r < wiersze.Count - 1) sb.Append(sKoniecWiersza);
}
return sb.ToString();
}

static string Oslon(string s, char cSeparator) {
bool bTrzeba =
  s.IndexOf(cSeparator) >= 0 ||
  s.IndexOf('"') >= 0 ||
  s.IndexOf('\r') >= 0 ||
  s.IndexOf('\n') >= 0 ||
  (s.Length > 0 && (s[0] == ' ' || s[s.Length - 1] == ' '));

if (!bTrzeba) return s;
return "\"" + s.Replace("\"", "\"\"") + "\"";
}

// Ile kolumn ma najszerszy wiersz.  Pliki z natury bywaja postrzepione
// (ostatnia kolumna pusta = brak separatora na koncu), a siatka potrzebuje
// jednej, stalej liczby kolumn.
public static int NajwiecejKolumn(List<List<string>> wiersze) {
int iMax = 0;
if (wiersze == null) return 0;
foreach (List<string> w in wiersze) if (w.Count > iMax) iMax = w.Count;
return iMax;
}

} // Csv class
} // EdSharp namespace
