// Zadania.cs - LISTY ZADAN (checklisty) MARKDOWN.
//
// Zlecenie Kasperczaka 16.09.2026: "Przed komentarzami musimy zrobic obsluge
// Checklisty markdown."  Ustalone skroty: Control+Shift+X przelacza pozycje
// zrobione/niezrobione, Control+Shift+F2 robi z wierszy checkliste i z powrotem,
// Control+Shift+F7 otwiera okno listy zadan, Alt+Shift+F2 mowi postep.
//
// PO CO OSOBNY PLIK.  Cala praca na tekscie siedzi tutaj jako funkcje CZYSTE
// (wejscie: wiersz albo tablica wierszy; wyjscie: nowy tekst), bez kontrolki
// edycyjnej i bez okien.  Dzieki temu daje sie ZMIERZYC golym kompilatorem, bez
// Windows i bez czytnika - tak jak Csv.cs i Sesja.cs.  EdSharp.cs wola te
// funkcje i dokłada mowe, bramki na typ pliku i bloki kodu.
//
// SKLADNIA.  Zapis z GitHub Flavored Markdown: znacznik listy, potem pole w
// nawiasach kwadratowych.  "- [ ] kupic chleb" (niezrobione), "- [x] kupic
// chleb" (zrobione).  Przyjmujemy tez wielkie X, bo tak zapisuja niektore
// programy, ale SAMI piszemy zawsze male - inaczej ten sam plik mialby dwa
// rozne zapisy tego samego stanu.
//
// UWAGA NA KOLEJNOSC ROZPOZNAWANIA.  Kazda pozycja checklisty jest TAKZE
// zwykla pozycja listy punktowanej ("- ..."), wiec kod pytajacy "czy to
// punktor" odpowie TAK.  Dlatego wszedzie, gdzie ma znaczenie roznica, pytaj
// NAJPIERW o checkliste, a dopiero potem o zwykla liste.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EdSharp {

public class Zadania {

// Wcięcie, znacznik listy, pole stanu.  Po nawiasie zamykajacym wymagamy
// spacji ALBO konca wiersza: "- [ ]" bez tresci to nadal pozycja checklisty
// (pusta), a nie zwykly punktor z tekstem "[ ]".
public static readonly Regex PrefiksRegex = new Regex(
	@"^(?<indent>[ \t]*)(?<marker>[-*+])[ \t]+\[(?<state>[ xX])\](?:[ \t]+|(?=$))",
	RegexOptions.CultureInvariant);

// Zwykly punktor - taki sam wzorzec, jaki ma EdSharp.cs.  Powtorzony tutaj,
// zeby ten plik dawal sie skompilowac i zmierzyc SAM.
private static readonly Regex PunktorRegex = new Regex(@"^(?<indent>[ \t]*)(?<marker>[-*+])[ \t]+", RegexOptions.CultureInvariant);
private static readonly Regex NumerRegex = new Regex(@"^(?<indent>[ \t]*)(?<number>\d+)[.)][ \t]+", RegexOptions.CultureInvariant);

public static bool CzyZadanie(string sLine) {
	if (String.IsNullOrEmpty(sLine)) return false;
	return PrefiksRegex.IsMatch(ObetnijCr(sLine));
} // CzyZadanie method

public static bool CzyZrobione(string sLine) {
	if (String.IsNullOrEmpty(sLine)) return false;
	Match m = PrefiksRegex.Match(ObetnijCr(sLine));
	if (!m.Success) return false;
	string sStan = m.Groups["state"].Value;
	return (sStan == "x" || sStan == "X");
} // CzyZrobione method

// Tresc pozycji BEZ znacznika i bez pola stanu - to ona idzie do okna listy
// zadan i do mowy.  Puste pole daje pusty napis, nie null.
public static string TrescZadania(string sLine) {
	if (String.IsNullOrEmpty(sLine)) return "";
	string s = ObetnijCr(sLine);
	Match m = PrefiksRegex.Match(s);
	if (!m.Success) return "";
	return s.Substring(m.Index + m.Length).Trim();
} // TrescZadania method

// Ile znakow zajmuje wcięcie plus znacznik plus pole stanu.  Kursor
// postawiony na poczatku wiersza checklisty stoi na znaczniku, a nie na
// tresci - dokladnie ten sam problem, ktory EdSharp juz naprawil dla zwyklych
// list (MarkdownReview_SkipListMarkerAt).
public static int DlugoscPrefiksu(string sLine) {
	if (String.IsNullOrEmpty(sLine)) return 0;
	Match m = PrefiksRegex.Match(ObetnijCr(sLine));
	if (!m.Success) return 0;
	return m.Length;
} // DlugoscPrefiksu method

// PRZELACZENIE STANU JEDNEJ POZYCJI.  Zwraca nowy wiersz albo null, gdy
// wiersz nie jest pozycja checklisty - null znaczy "nie ma czego przelaczyc"
// i wolajacy ma o tym powiedziec, zamiast po cichu nic nie robic.
public static string PrzelaczWiersz(string sLine, out bool bZrobionePo) {
	bZrobionePo = false;
	if (sLine == null) return null;
	bool bCr = sLine.EndsWith("\r");
	string s = bCr ? sLine.Substring(0, sLine.Length - 1) : sLine;

	Match m = PrefiksRegex.Match(s);
	if (!m.Success) return null;

	int iBox = s.IndexOf('[', m.Index);
	if (iBox < 0 || iBox + 2 > s.Length) return null;

	bool bBylo = CzyZrobione(s);
	bZrobionePo = !bBylo;
	char cNowy = bZrobionePo ? 'x' : ' ';
	string sNowy = s.Substring(0, iBox + 1) + cNowy + s.Substring(iBox + 2);
	return bCr ? sNowy + "\r" : sNowy;
} // PrzelaczWiersz method

// USTAWIENIE STANU WPROST (bez przelaczania).  Uzywa tego okno listy zadan,
// gdzie po zmianie wielu pozycji chcemy stan ZNANY, nie odwrocony.
public static string UstawWiersz(string sLine, bool bZrobione) {
	if (sLine == null) return null;
	if (CzyZadanie(sLine) && CzyZrobione(sLine) == bZrobione) return sLine;
	bool bNic;
	return PrzelaczWiersz(sLine, out bNic);
} // UstawWiersz method

// ZROBIENIE Z WIERSZA POZYCJI CHECKLISTY.  Zwykly tekst, punktor i pozycja
// numerowana daja "- [ ] tresc"; pozycja, ktora JUZ jest checklista, zostaje
// bez zmian (zeby powtorne wywolanie nie mnozylo nawiasow).
public static string DodajPole(string sLine) {
	if (sLine == null) return null;
	bool bCr = sLine.EndsWith("\r");
	string s = bCr ? sLine.Substring(0, sLine.Length - 1) : sLine;
	if (s.Trim().Length == 0) return sLine;
	if (PrefiksRegex.IsMatch(s)) return sLine;

	string sIndent;
	string sTresc;
	Match mP = PunktorRegex.Match(s);
	Match mN = NumerRegex.Match(s);
	if (mP.Success) {
		sIndent = mP.Groups["indent"].Value;
		sTresc = s.Substring(mP.Index + mP.Length);
	}
	else if (mN.Success) {
		sIndent = mN.Groups["indent"].Value;
		sTresc = s.Substring(mN.Index + mN.Length);
	}
	else {
		int iWciecie = 0;
		while (iWciecie < s.Length && (s[iWciecie] == ' ' || s[iWciecie] == '\t')) iWciecie++;
		sIndent = s.Substring(0, iWciecie);
		sTresc = s.Substring(iWciecie);
	}

	string sNowy = sIndent + "- [ ] " + sTresc;
	return bCr ? sNowy + "\r" : sNowy;
} // DodajPole method

// ZDJECIE POLA.  Wraca do TEKSTU ZWYKLEGO, nie do punktora - taka sama
// zasada, jaka Kasperczak ustalil 31.08.2026 dla Control+L: powrot idzie do
// zwyklego tekstu, a zamiana rodzaju listy to dwa nacisniecia.
public static string ZdejmijPole(string sLine) {
	if (sLine == null) return null;
	bool bCr = sLine.EndsWith("\r");
	string s = bCr ? sLine.Substring(0, sLine.Length - 1) : sLine;

	Match m = PrefiksRegex.Match(s);
	if (!m.Success) return sLine;
	string sNowy = m.Groups["indent"].Value + s.Substring(m.Index + m.Length);
	return bCr ? sNowy + "\r" : sNowy;
} // ZdejmijPole method

// PRZELACZNIK NA ZAKRESIE WIERSZY.  Gdy WSZYSTKIE niepuste wiersze sa juz
// checklista - zdejmujemy pola; w kazdym innym wypadku dokladamy je wszedzie.
// Ta sama regula, co przy liscie punktowanej, wiec uzytkownik nie musi uczyc
// sie drugiego zachowania.
public static string[] PrzelaczZakres(string[] aLines, out bool bZdjete) {
	bZdjete = false;
	if (aLines == null) return null;

	bool bWszystkie = true;
	bool bCokolwiek = false;
	for (int i = 0; i < aLines.Length; i++) {
		string s = ObetnijCr(aLines[i] ?? "");
		if (s.Trim().Length == 0) continue;
		bCokolwiek = true;
		if (!PrefiksRegex.IsMatch(s)) {bWszystkie = false; break;}
	}
	if (!bCokolwiek) return aLines;

	bZdjete = bWszystkie;
	string[] aWynik = new string[aLines.Length];
	for (int i = 0; i < aLines.Length; i++) {
		string s = aLines[i] ?? "";
		if (ObetnijCr(s).Trim().Length == 0) {aWynik[i] = s; continue;}
		aWynik[i] = bWszystkie ? ZdejmijPole(s) : DodajPole(s);
	}
	return aWynik;
} // PrzelaczZakres method

// PRZELACZENIE STANU NA ZAKRESIE.  Gdy wszystkie pozycje sa zrobione -
// odznaczamy; inaczej zaznaczamy wszystkie.  Bez tej reguly zaznaczanie
// zaznaczonego dawaloby mieszanke stanow, ktorej przy czytniku nie widac.
public static string[] PrzelaczStanZakresu(string[] aLines, out bool bZrobionePo, out int iZmienione) {
	bZrobionePo = false;
	iZmienione = 0;
	if (aLines == null) return null;

	bool bWszystkieZrobione = true;
	bool bCokolwiek = false;
	for (int i = 0; i < aLines.Length; i++) {
		string s = aLines[i] ?? "";
		if (!CzyZadanie(s)) continue;
		bCokolwiek = true;
		if (!CzyZrobione(s)) {bWszystkieZrobione = false; break;}
	}
	if (!bCokolwiek) return null;

	bZrobionePo = !bWszystkieZrobione;
	string[] aWynik = new string[aLines.Length];
	for (int i = 0; i < aLines.Length; i++) {
		string s = aLines[i] ?? "";
		aWynik[i] = s;
		if (!CzyZadanie(s)) continue;
		if (CzyZrobione(s) == bZrobionePo) continue;
		string sNowy = UstawWiersz(s, bZrobionePo);
		if (sNowy == null) continue;
		aWynik[i] = sNowy;
		iZmienione++;
	}
	return aWynik;
} // PrzelaczStanZakresu method

// POSTEP.  Liczy pozycje w podanych wierszach; bramke na bloki kodu zaklada
// wolajacy, bo tylko on ma caly tekst dokumentu.
public static void Postep(string[] aLines, out int iZrobione, out int iWszystkie) {
	iZrobione = 0;
	iWszystkie = 0;
	if (aLines == null) return;
	for (int i = 0; i < aLines.Length; i++) {
		string s = aLines[i] ?? "";
		if (!CzyZadanie(s)) continue;
		iWszystkie++;
		if (CzyZrobione(s)) iZrobione++;
	}
} // Postep method

// Zdanie mowione o postepie.  Program jest anglojezyczny, wiec komunikat po
// angielsku - tak jak wszystkie pozostale.
public static string OpisPostepu(int iZrobione, int iWszystkie) {
	if (iWszystkie <= 0) return "No task list items!";
	int iProcent = (int) Math.Round((100.0 * iZrobione) / iWszystkie);
	return iZrobione.ToString(CultureInfo.InvariantCulture) + " of "
		+ iWszystkie.ToString(CultureInfo.InvariantCulture) + " done, "
		+ iProcent.ToString(CultureInfo.InvariantCulture) + " percent";
} // OpisPostepu method

// Podpis pozycji w oknie listy zadan.  STAN IDZIE NA POCZATEK, bo czytnik
// czyta pozycje od lewej i uzytkownik ma usłyszec stan od razu, a nie po
// wysluchaniu calej tresci zadania.
public static string PozycjaDoOkna(string sLine) {
	string sTresc = TrescZadania(sLine);
	if (sTresc.Length == 0) sTresc = "(empty)";
	return (CzyZrobione(sLine) ? "done: " : "to do: ") + sTresc;
} // PozycjaDoOkna method

// Jak pozycja ma sie CZYTAC w podgladzie Markdown.  Podglad jest zwykla
// kontrolka tekstowa, wiec prawdziwego pola wyboru tam nie ma - slowo jest
// jedyna informacja, ktora czytnik poda.
public static string ZnacznikDoPodgladu(bool bZrobione) {
	return bZrobione ? "[done] " : "[to do] ";
} // ZnacznikDoPodgladu method

private static string ObetnijCr(string s) {
	if (s == null) return "";
	return s.EndsWith("\r") ? s.Substring(0, s.Length - 1) : s;
} // ObetnijCr method

} // Zadania class

} // EdSharp namespace
