// Zmiany.cs - SLEDZENIE ZMIAN (recenzja tekstu) W PLIKU MARKDOWN.
//
// Zlecenie Kasperczaka 17.09.2026: "jak mozna by zrobic sledzenie zmian i
// wspolna prace na dwoch EdSharpach".  Projekt i decyzje:
// docs/SLEDZENIE-ZMIAN-I-WSPOLNA-PRACA.md.  To jest KROK 1a z rozdzialu 4.1:
// samo rozpoznawanie zmian, bez okien i bez ukrywania znacznikow.
//
// PO CO OSOBNY PLIK.  Tak samo jak Zadania.cs i Csv.cs: cala praca na tekscie
// siedzi tutaj jako funkcje CZYSTE (wejscie: napis; wyjscie: napis albo lista),
// bez kontrolki edycyjnej i bez okien.  Dzieki temu daje sie ZMIERZYC golym
// kompilatorem, bez Windows i bez czytnika.  EdSharp.cs zawola te funkcje i
// dolozy mowe, okno "Zmiany" i skoki.
//
// SKLADNIA: CriticMarkup - ustalona, opisana, ma implementacje w innych
// edytorach (m.in. wtyczka do Obsidiana), i cala zostaje w zwyklym pliku
// tekstowym:
//   {++ dopisane ++}          wstawka recenzenta
//   {-- skasowane --}         usuniecie
//   {~~ stare ~> nowe ~~}     podmiana
//   {== podswietlone ==}      podswietlenie (nie zmienia tresci)
//   {>> komentarz <<}         uwaga recenzenta (nie jest trescia dokumentu)
//
// DWA PRZEJSCIA PRZEZ TEKST, NIE JEDNO.  "Przyjmij wszystko" i "odrzuc
// wszystko" to nie to samo dla kazdego rodzaju:
//   dopisanie    przyjete -> zostaje tresc,      odrzucone -> znika calkiem
//   usuniecie    przyjete -> znika calkiem,      odrzucone -> zostaje tresc
//   podmiana     przyjeta -> zostaje NOWE,       odrzucona -> zostaje STARE
//   podswietlenie oba przypadki -> zostaje tresc bez znacznikow
//   komentarz    oba przypadki -> znika calkiem (to nie tresc dokumentu)
//
// DLACZEGO POZYCJE, A NIE SAM WYNIK.  Kazda zmiana niesie miejsce i dlugosc w
// SUROWYM tekscie oraz tresc stara i nowa.  To jest ta "lista operacji" z
// rozdzialu 3.4 projektu: gdyby kiedys doszla praca na zywo (Etherpad, Yjs),
// podmienia sie dostawce operacji, a nie przepisuje cala funkcje.  Gotowy
// wynik po przyjeciu tego by nie dal.
//
// ZASTRZEZENIE O ZAGNIEZDZENIU.  CriticMarkup nie definiuje zmiany w zmianie i
// my jej TEZ nie obslugujemy: szukamy pierwszego domkniecia i nie wchodzimy
// glebiej.  Tekst niedomkniety (otwarty nawias bez zamkniecia) NIE jest zmiana
// - zostaje w dokumencie jako zwykle znaki.  Cisza jest tu gorsza od niczego,
// wiec CzyNiedomkniete() pozwala wolajacemu o tym powiedziec.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EdSharp {

// Rodzaj zmiany.  Nazwy po angielsku jak reszta widocznych napisow w programie
// (menu i komunikaty EdSharpa sa angielskie), opisy dla czytnika skladane w
// OpisDoOkna.
public enum RodzajZmiany {
	Dopisanie,
	Usuniecie,
	Podmiana,
	Podswietlenie,
	Komentarz
} // RodzajZmiany enum

// JEDNA zmiana znaleziona w tekscie.  Pola tylko do czytania - lista zmian jest
// zdjeciem stanu tekstu, a nie czyms, co wolajacy poprawia u siebie.
public class Zmiana {
	public readonly RodzajZmiany Rodzaj;
	// Miejsce i dlugosc w SUROWYM tekscie, razem ze znacznikami.
	public readonly int Start;
	public readonly int Dlugosc;
	// Tresc przed i po.  Dopisanie ma puste Stare, usuniecie puste Nowe,
	// komentarz trzyma swoja tresc w Nowe (nie ma "starej" wersji uwagi).
	public readonly string Stare;
	public readonly string Nowe;

	public Zmiana(RodzajZmiany rodzaj, int start, int dlugosc, string stare, string nowe) {
		Rodzaj = rodzaj;
		Start = start;
		Dlugosc = dlugosc;
		Stare = stare == null ? "" : stare;
		Nowe = nowe == null ? "" : nowe;
	} // Zmiana constructor

	public int Koniec { get { return Start + Dlugosc; } }
} // Zmiana class

public class Zmiany {

// Jeden wzorzec na wszystkie piec rodzajow.  Grupy nazwane, bo numery przy
// pieciu alternatywach sa nie do czytania przy poprawianiu kodu.
// (?s) - kropka lapie tez koniec wiersza: zmiana moze isc przez kilka wierszy.
// Wszystkie *? sa NIEZACHLANNE, inaczej pierwsza zmiana zjadlaby caly plik do
// ostatniego domkniecia.
private const string WzorzecTekst =
	@"(?s)\{(?:"
	+ @"\+\+(?<dop>.*?)\+\+"
	+ @"|--(?<usu>.*?)--"
	+ @"|~~(?<sta>.*?)~>(?<now>.*?)~~"
	+ @"|==(?<pod>.*?)=="
	+ @"|>>(?<kom>.*?)<<"
	+ @")\}";

public static readonly Regex ZmianaRegex = new Regex(WzorzecTekst, RegexOptions.CultureInvariant);

// Otwarcia znacznikow - do wykrycia tekstu niedomknietego.
private static readonly Regex OtwarcieRegex = new Regex(@"\{(?:\+\+|--|~~|==|>>)", RegexOptions.CultureInvariant);

// ---------------------------------------------------------------- ROZPOZNANIE

// WSZYSTKIE zmiany w tekscie, w kolejnosci wystepowania.  Pusta lista, gdy nie
// ma zadnej - nigdy null, zeby wolajacy nie musial tego sprawdzac.
public static List<Zmiana> Znajdz(string sTekst) {
	List<Zmiana> lista = new List<Zmiana>();
	if (String.IsNullOrEmpty(sTekst)) return lista;
	foreach (Match m in ZmianaRegex.Matches(sTekst)) {
		Zmiana z = ZMeczu(m);
		if (z != null) lista.Add(z);
	} // next m
	return lista;
} // Znajdz method

private static Zmiana ZMeczu(Match m) {
	if (m == null || !m.Success) return null;
	if (m.Groups["dop"].Success)
		return new Zmiana(RodzajZmiany.Dopisanie, m.Index, m.Length, "", m.Groups["dop"].Value);
	if (m.Groups["usu"].Success)
		return new Zmiana(RodzajZmiany.Usuniecie, m.Index, m.Length, m.Groups["usu"].Value, "");
	if (m.Groups["sta"].Success)
		return new Zmiana(RodzajZmiany.Podmiana, m.Index, m.Length, m.Groups["sta"].Value, m.Groups["now"].Value);
	if (m.Groups["pod"].Success)
		return new Zmiana(RodzajZmiany.Podswietlenie, m.Index, m.Length, m.Groups["pod"].Value, m.Groups["pod"].Value);
	if (m.Groups["kom"].Success)
		return new Zmiana(RodzajZmiany.Komentarz, m.Index, m.Length, "", m.Groups["kom"].Value);
	return null;
} // ZMeczu method

public static bool CzyMaZmiany(string sTekst) {
	if (String.IsNullOrEmpty(sTekst)) return false;
	return ZmianaRegex.IsMatch(sTekst);
} // CzyMaZmiany method

// Zmiana, w ktorej stoi kursor, albo null.  Kursor DOTYKAJACY konca zmiany
// (iPozycja == Koniec) jest juz za nia - inaczej "przyjmij tu" po wyjsciu ze
// zmiany dzialaloby na zmianie, ktorej uzytkownik wlasnie opuscil.
public static Zmiana WMiejscu(string sTekst, int iPozycja) {
	if (String.IsNullOrEmpty(sTekst) || iPozycja < 0) return null;
	foreach (Zmiana z in Znajdz(sTekst)) {
		if (iPozycja >= z.Start && iPozycja < z.Koniec) return z;
	} // next z
	return null;
} // WMiejscu method

// Najblizsza zmiana ZA pozycja kursora, albo null.  To jest skok "nastepna
// zmiana".  Zmiana, w ktorej kursor wlasnie stoi, jest pomijana - skok ma
// ruszac z miejsca.
public static Zmiana Nastepna(string sTekst, int iPozycja) {
	if (String.IsNullOrEmpty(sTekst)) return null;
	foreach (Zmiana z in Znajdz(sTekst)) {
		if (z.Start > iPozycja) return z;
	} // next z
	return null;
} // Nastepna method

// Najblizsza zmiana PRZED pozycja kursora, albo null.  Skok "poprzednia
// zmiana": gdy kursor stoi w srodku zmiany, cofamy sie do tej wczesniejszej.
public static Zmiana Poprzednia(string sTekst, int iPozycja) {
	if (String.IsNullOrEmpty(sTekst)) return null;
	Zmiana wynik = null;
	foreach (Zmiana z in Znajdz(sTekst)) {
		if (z.Start < iPozycja && (wynik == null || z.Start > wynik.Start)) {
			// Gdy kursor jest W tej zmianie, nie jest ona "poprzednia".
			if (iPozycja < z.Koniec) continue;
			wynik = z;
		} // end if
	} // next z
	return wynik;
} // Poprzednia method

// Numer wiersza (od 1) dla pozycji w tekscie - do odczytu "wiersz 12".
// Liczymy same znaki konca wiersza, wiec CRLF i LF daja ten sam numer.
public static int NumerWiersza(string sTekst, int iPozycja) {
	if (String.IsNullOrEmpty(sTekst) || iPozycja <= 0) return 1;
	int iKoniec = iPozycja < sTekst.Length ? iPozycja : sTekst.Length;
	int iWiersz = 1;
	for (int i = 0; i < iKoniec; i++) {
		if (sTekst[i] == '\n') iWiersz++;
	} // next i
	return iWiersz;
} // NumerWiersza method

// -------------------------------------------------------- PRZYJMIJ I ODRZUC

// Tresc, ktora ZOSTAJE po przyjeciu jednej zmiany.
public static string TrescPoPrzyjeciu(Zmiana z) {
	if (z == null) return "";
	switch (z.Rodzaj) {
		case RodzajZmiany.Dopisanie: return z.Nowe;
		case RodzajZmiany.Usuniecie: return "";
		case RodzajZmiany.Podmiana: return z.Nowe;
		case RodzajZmiany.Podswietlenie: return z.Nowe;
		case RodzajZmiany.Komentarz: return "";
	} // end switch
	return "";
} // TrescPoPrzyjeciu method

// Tresc, ktora ZOSTAJE po odrzuceniu jednej zmiany.
public static string TrescPoOdrzuceniu(Zmiana z) {
	if (z == null) return "";
	switch (z.Rodzaj) {
		case RodzajZmiany.Dopisanie: return "";
		case RodzajZmiany.Usuniecie: return z.Stare;
		case RodzajZmiany.Podmiana: return z.Stare;
		case RodzajZmiany.Podswietlenie: return z.Nowe;
		case RodzajZmiany.Komentarz: return "";
	} // end switch
	return "";
} // TrescPoOdrzuceniu method

// Przyjmij albo odrzuc JEDNA zmiane.  Zwraca nowy tekst, albo null gdy zmiany
// nie ma pod ta pozycja - null znaczy "nie ma czego przyjac" i wolajacy ma o
// tym powiedziec, zamiast po cichu nic nie zrobic (ta sama zasada co
// Zadania.PrzelaczWiersz).
public static string PrzyjmijWMiejscu(string sTekst, int iPozycja) {
	return ZastosujJedna(sTekst, iPozycja, true);
} // PrzyjmijWMiejscu method

public static string OdrzucWMiejscu(string sTekst, int iPozycja) {
	return ZastosujJedna(sTekst, iPozycja, false);
} // OdrzucWMiejscu method

private static string ZastosujJedna(string sTekst, int iPozycja, bool bPrzyjmij) {
	if (String.IsNullOrEmpty(sTekst)) return null;
	Zmiana z = WMiejscu(sTekst, iPozycja);
	if (z == null) return null;
	string sTresc = bPrzyjmij ? TrescPoPrzyjeciu(z) : TrescPoOdrzuceniu(z);
	return sTekst.Substring(0, z.Start) + sTresc + sTekst.Substring(z.Koniec);
} // ZastosujJedna method

// Tekst po przyjeciu WSZYSTKICH zmian.  To jest zarazem tresc, ktora ma
// brzmiec w edytorze przy ukrytych znacznikach (krok 1c projektu) - dlatego
// jedna funkcja, a nie dwie liczace to samo dwa razy.
public static string TekstPoPrzyjeciu(string sTekst) {
	return ZastosujWszystkie(sTekst, true);
} // TekstPoPrzyjeciu method

public static string TekstPoOdrzuceniu(string sTekst) {
	return ZastosujWszystkie(sTekst, false);
} // TekstPoOdrzuceniu method

private static string ZastosujWszystkie(string sTekst, bool bPrzyjmij) {
	if (String.IsNullOrEmpty(sTekst)) return sTekst == null ? "" : sTekst;
	List<Zmiana> lista = Znajdz(sTekst);
	if (lista.Count == 0) return sTekst;
	StringBuilder sb = new StringBuilder(sTekst.Length);
	int iSkad = 0;
	foreach (Zmiana z in lista) {
		// Zmiany z Znajdz ida po kolei i nie zachodza na siebie, ale straznik
		// zostaje: gdyby kiedys doszlo zagniezdzenie, lepiej pominac niz
		// wyrzucic wyjatek na oczach uzytkownika.
		if (z.Start < iSkad) continue;
		sb.Append(sTekst, iSkad, z.Start - iSkad);
		sb.Append(bPrzyjmij ? TrescPoPrzyjeciu(z) : TrescPoOdrzuceniu(z));
		iSkad = z.Koniec;
	} // next z
	if (iSkad < sTekst.Length) sb.Append(sTekst, iSkad, sTekst.Length - iSkad);
	return sb.ToString();
} // ZastosujWszystkie method

// ------------------------------------------------------------ WPISANIE ZMIANY

// Zapis jednej zmiany jako tekst ze znacznikami - do wpisywania poprawek jako
// recenzent (krok 2 projektu).  Tutaj, bo to odwrotnosc rozpoznawania i musi
// sie z nim zgadzac znak w znak.
public static string ZapiszDopisanie(string sTresc) {
	return "{++" + Bezpieczna(sTresc) + "++}";
} // ZapiszDopisanie method

public static string ZapiszUsuniecie(string sTresc) {
	return "{--" + Bezpieczna(sTresc) + "--}";
} // ZapiszUsuniecie method

public static string ZapiszPodmiane(string sStare, string sNowe) {
	return "{~~" + Bezpieczna(sStare) + "~>" + Bezpieczna(sNowe) + "~~}";
} // ZapiszPodmiane method

public static string ZapiszPodswietlenie(string sTresc) {
	return "{==" + Bezpieczna(sTresc) + "==}";
} // ZapiszPodswietlenie method

public static string ZapiszKomentarz(string sTresc) {
	return "{>>" + Bezpieczna(sTresc) + "<<}";
} // ZapiszKomentarz method

// Tresc, ktora sama zawiera znacznik, rozwalilaby zapis - a CriticMarkup nie
// ma znaku ucieczki.  Wstawiamy woskie spacje miedzy powtorzone znaki, zeby
// tresc zostala czytelna, a znacznik przestal byc znacznikiem.  Lepsze to niz
// zapisanie czegos, czego wlasne rozpoznawanie nie odczyta.
private static string Bezpieczna(string sTresc) {
	if (String.IsNullOrEmpty(sTresc)) return "";
	string s = sTresc;
	s = s.Replace("++}", "+ +}").Replace("--}", "- -}").Replace("~~}", "~ ~}");
	s = s.Replace("==}", "= =}").Replace("<<}", "< <}").Replace("~>", "~ >");
	s = s.Replace("{++", "{ ++").Replace("{--", "{ --").Replace("{~~", "{ ~~");
	s = s.Replace("{==", "{ ==").Replace("{>>", "{ >>");
	return s;
} // Bezpieczna method

// ------------------------------------------------------------------- DLA MOWY

// Podpis zmiany w oknie "Zmiany".  RODZAJ IDZIE NA POCZATEK - czytnik czyta
// pozycje od lewej i uzytkownik ma uslyszec, co to za zmiana, przed jej
// trescia (ta sama zasada co Zadania.PozycjaDoOkna).
public static string OpisDoOkna(Zmiana z, string sTekst) {
	if (z == null) return "";
	string sWiersz = ", line " + NumerWiersza(sTekst, z.Start).ToString(CultureInfo.InvariantCulture);
	switch (z.Rodzaj) {
		case RodzajZmiany.Dopisanie:
			return "inserted: " + Skrot(z.Nowe) + sWiersz;
		case RodzajZmiany.Usuniecie:
			return "deleted: " + Skrot(z.Stare) + sWiersz;
		case RodzajZmiany.Podmiana:
			return "replaced: was " + Skrot(z.Stare) + ", now " + Skrot(z.Nowe) + sWiersz;
		case RodzajZmiany.Podswietlenie:
			return "highlighted: " + Skrot(z.Nowe) + sWiersz;
		case RodzajZmiany.Komentarz:
			return "comment: " + Skrot(z.Nowe) + sWiersz;
	} // end switch
	return "";
} // OpisDoOkna method

// Sama nazwa rodzaju - do odczytu przy skoku po zmianach w dokumencie, gdzie
// tresc uzytkownik i tak zaraz uslyszy z wiersza.
public static string NazwaRodzaju(RodzajZmiany rodzaj) {
	switch (rodzaj) {
		case RodzajZmiany.Dopisanie: return "inserted";
		case RodzajZmiany.Usuniecie: return "deleted";
		case RodzajZmiany.Podmiana: return "replaced";
		case RodzajZmiany.Podswietlenie: return "highlighted";
		case RodzajZmiany.Komentarz: return "comment";
	} // end switch
	return "change";
} // NazwaRodzaju method

// Tytul okna niesie licznik - tak samo jak okno listy zadan niesie postep.
public static string OpisLiczby(string sTekst) {
	int iIle = Znajdz(sTekst).Count;
	if (iIle == 0) return "no changes";
	if (iIle == 1) return "1 change";
	return iIle.ToString(CultureInfo.InvariantCulture) + " changes";
} // OpisLiczby method

// Tresc do wymowienia: koniec wiersza i tabulator na spacje, dlugie fragmenty
// skrocone.  Czytnik czytajacy trzy akapity w jednej pozycji listy jest
// bezuzyteczny, a pelna tresc uzytkownik uslyszy spacja w oknie.
private const int MaxSkrot = 60;

public static string Skrot(string sTresc) {
	if (String.IsNullOrEmpty(sTresc)) return "(empty)";
	string s = sTresc.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
	while (s.IndexOf("  ", StringComparison.Ordinal) >= 0) s = s.Replace("  ", " ");
	s = s.Trim();
	if (s.Length == 0) return "(empty)";
	if (s.Length <= MaxSkrot) return s;
	return s.Substring(0, MaxSkrot) + "...";
} // Skrot method

// ------------------------------------------------------------- TEKST ZEPSUTY

// Czy w tekscie jest OTWARTY znacznik bez domkniecia.  Taki tekst nie jest
// zmiana i zostaje w dokumencie jako zwykle znaki - ale uzytkownik ma prawo
// wiedziec, ze cos jest niedomkniete, zamiast szukac zmiany, ktorej program
// nie widzi.
public static bool CzyNiedomkniete(string sTekst) {
	if (String.IsNullOrEmpty(sTekst)) return false;
	MatchCollection otwarcia = OtwarcieRegex.Matches(sTekst);
	if (otwarcia.Count == 0) return false;
	// Otwarcia policzone w SUROWYM tekscie obejmuja tez te lezace WEWNATRZ
	// tresci znalezionych zmian (np. nawias w komentarzu recenzenta).  Liczy
	// sie tylko otwarcie lezace POZA kazda znaleziona zmiana - to jest
	// otwarcie, ktorego nikt nie domknal.
	List<Zmiana> lista = Znajdz(sTekst);
	foreach (Match m in otwarcia) {
		bool bWewnatrz = false;
		foreach (Zmiana z in lista) {
			if (m.Index >= z.Start && m.Index < z.Koniec) { bWewnatrz = true; break; }
		} // next z
		if (!bWewnatrz) return true;
	} // next m
	return false;
} // CzyNiedomkniete method

} // Zmiany class

} // EdSharp namespace
