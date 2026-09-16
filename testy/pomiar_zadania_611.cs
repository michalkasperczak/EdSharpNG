// pomiar_zadania_611.cs - POMIAR CHECKLIST MARKDOWN (EdSharpNG 5.0.111+).
//
// Mierzy CZYSTE funkcje z Zadania.cs: rozpoznanie skladni, przelaczanie stanu,
// tworzenie i zdejmowanie pola, postep, podpisy dla czytnika.  Nie potrzebuje
// Windows ani czytnika - kompiluje sie golym csc razem z Zadania.cs.
//
// KONTROLA, ktora czyni ten pomiar roznicujacym: Zadania.cs w wersji sprzed
// zmiany NIE ISTNIEJE, wiec kompilacja na starej rewizji pada - to jest
// dowod negatywny.  Osobno mierzone jest to, czego sam plik nie zapewnia:
// ze checklista NIE jest mylona ze zwyklym punktorem (asercje 30-33).

using System;
using EdSharp;

public class PomiarZadania {

private static int iOk = 0;
private static int iZle = 0;

private static void Sprawdz(string sCo, bool bWarunek) {
	if (bWarunek) {iOk++; Console.WriteLine("OK   " + sCo);}
	else {iZle++; Console.WriteLine("ZLE  " + sCo);}
}

private static void Rowne(string sCo, string sOczekiwane, string sMam) {
	bool b = (sOczekiwane == sMam);
	if (b) {iOk++; Console.WriteLine("OK   " + sCo);}
	else {iZle++; Console.WriteLine("ZLE  " + sCo + " | oczekiwane [" + sOczekiwane + "] mam [" + sMam + "]");}
}

public static int Main(string[] args) {

// --- ROZPOZNANIE SKLADNI ---
Sprawdz("1 niezrobione", Zadania.CzyZadanie("- [ ] kupic chleb"));
Sprawdz("2 zrobione male x", Zadania.CzyZadanie("- [x] kupic chleb"));
Sprawdz("3 zrobione wielkie X", Zadania.CzyZadanie("- [X] kupic chleb"));
Sprawdz("4 gwiazdka jako znacznik", Zadania.CzyZadanie("* [ ] zadanie"));
Sprawdz("5 plus jako znacznik", Zadania.CzyZadanie("+ [ ] zadanie"));
Sprawdz("6 wciecie zachowane", Zadania.CzyZadanie("    - [ ] podzadanie"));
Sprawdz("7 pusta pozycja bez tresci", Zadania.CzyZadanie("- [ ]"));
Sprawdz("8 zwykly punktor to NIE zadanie", !Zadania.CzyZadanie("- kupic chleb"));
Sprawdz("9 numerowana to NIE zadanie", !Zadania.CzyZadanie("1. kupic chleb"));
Sprawdz("10 zwykly tekst to NIE zadanie", !Zadania.CzyZadanie("kupic chleb"));
Sprawdz("11 nawiasy w tresci to NIE zadanie", !Zadania.CzyZadanie("- tekst [ ] w srodku"));
Sprawdz("12 bez spacji po znaczniku", !Zadania.CzyZadanie("-[ ] zadanie"));
Sprawdz("13 dwa znaki w polu to NIE zadanie", !Zadania.CzyZadanie("- [xx] zadanie"));
Sprawdz("14 tabulator jako wciecie", Zadania.CzyZadanie("\t- [ ] zadanie"));

// --- STAN ---
Sprawdz("15 stan niezrobione", !Zadania.CzyZrobione("- [ ] a"));
Sprawdz("16 stan zrobione", Zadania.CzyZrobione("- [x] a"));
Sprawdz("17 stan wielkie X czytane jako zrobione", Zadania.CzyZrobione("- [X] a"));
Sprawdz("18 stan zwyklego punktora", !Zadania.CzyZrobione("- a"));

// --- TRESC ---
Rowne("19 tresc bez znacznika", "kupic chleb", Zadania.TrescZadania("- [ ] kupic chleb"));
Rowne("20 tresc z wcieciem", "podzadanie", Zadania.TrescZadania("   - [x] podzadanie"));
Rowne("21 tresc pustej pozycji", "", Zadania.TrescZadania("- [ ]"));
Rowne("22 tresc polskie znaki", "zazolc gesla jazn", Zadania.TrescZadania("- [ ] zazolc gesla jazn"));

// --- PRZELACZANIE ---
bool bPo;
Rowne("23 przelacz na zrobione", "- [x] a", Zadania.PrzelaczWiersz("- [ ] a", out bPo));
Sprawdz("24 przelacz zglasza nowy stan", bPo);
Rowne("25 przelacz na niezrobione", "- [ ] a", Zadania.PrzelaczWiersz("- [x] a", out bPo));
Sprawdz("26 przelacz zglasza nowy stan (odwrotnie)", !bPo);
Rowne("27 wielkie X wraca jako male x po dwoch przelaczeniach", "- [x] a",
	Zadania.PrzelaczWiersz(Zadania.PrzelaczWiersz("- [X] a", out bPo), out bPo));
Sprawdz("28 przelacz na NIE-zadaniu daje null", Zadania.PrzelaczWiersz("- a", out bPo) == null);
Rowne("29 przelacz zachowuje CR", "- [x] a\r", Zadania.PrzelaczWiersz("- [ ] a\r", out bPo));

// --- PRZELACZNIK NIE JEST KOMENDA JEDNOKIERUNKOWA (lekcja 31.08.2026) ---
string sRaz = Zadania.PrzelaczWiersz("- [ ] a", out bPo);
string sDwa = Zadania.PrzelaczWiersz(sRaz, out bPo);
Sprawdz("30 dwa przelaczenia daja ROZNE wyniki", sRaz != sDwa);
Rowne("31 drugie przelaczenie wraca do stanu poczatkowego", "- [ ] a", sDwa);

// --- TWORZENIE I ZDEJMOWANIE POLA ---
Rowne("32 zwykly tekst dostaje pole", "- [ ] kupic chleb", Zadania.DodajPole("kupic chleb"));
Rowne("33 punktor zamienia sie w zadanie", "- [ ] kupic chleb", Zadania.DodajPole("- kupic chleb"));
Rowne("34 numerowana zamienia sie w zadanie", "- [ ] kupic chleb", Zadania.DodajPole("1. kupic chleb"));
Rowne("35 wciecie zachowane przy dodaniu", "    - [ ] a", Zadania.DodajPole("    a"));
Rowne("36 juz-zadanie bez zmian (brak mnozenia nawiasow)", "- [x] a", Zadania.DodajPole("- [x] a"));
Rowne("37 pusty wiersz bez zmian", "", Zadania.DodajPole(""));
Rowne("38 zdjecie pola wraca do TEKSTU ZWYKLEGO, nie do punktora", "kupic chleb", Zadania.ZdejmijPole("- [ ] kupic chleb"));
Sprawdz("39 po zdjeciu nie ma ani punktora, ani numeru",
	!Zadania.ZdejmijPole("- [x] a").TrimStart().StartsWith("-")
	&& !Zadania.ZdejmijPole("- [x] a").TrimStart().StartsWith("1"));
Rowne("40 zdjecie zachowuje wciecie", "   a", Zadania.ZdejmijPole("   - [ ] a"));
Rowne("41 zdjecie na NIE-zadaniu bez zmian", "- a", Zadania.ZdejmijPole("- a"));
Rowne("42 CR zachowany przy dodaniu", "- [ ] a\r", Zadania.DodajPole("a\r"));

// --- ZAKRES WIERSZY ---
bool bZdjete;
string[] aWe = new string[] {"a", "b", ""};
string[] aWy = Zadania.PrzelaczZakres(aWe, out bZdjete);
Sprawdz("43 zakres: trzy wiersze dostaja pola", aWy[0] == "- [ ] a" && aWy[1] == "- [ ] b");
Rowne("44 zakres: pusty wiersz nietkniety", "", aWy[2]);
Sprawdz("45 zakres: dodanie nie jest zdjeciem", !bZdjete);
string[] aWy2 = Zadania.PrzelaczZakres(aWy, out bZdjete);
Sprawdz("46 zakres: powtorne wywolanie ZDEJMUJE pola", bZdjete && aWy2[0] == "a" && aWy2[1] == "b");
string[] aMix = Zadania.PrzelaczZakres(new string[] {"- [ ] a", "b"}, out bZdjete);
Sprawdz("47 zakres mieszany: dokladamy wszedzie, nie zdejmujemy", !bZdjete && aMix[1] == "- [ ] b");
Rowne("48 zakres mieszany: gotowa pozycja bez zmian", "- [ ] a", aMix[0]);

// --- STAN NA ZAKRESIE ---
int iZmienione;
string[] aStan = Zadania.PrzelaczStanZakresu(new string[] {"- [ ] a", "- [x] b"}, out bPo, out iZmienione);
Sprawdz("49 zakres mieszany stanow: zaznaczamy wszystkie", bPo && aStan[0] == "- [x] a" && aStan[1] == "- [x] b");
Sprawdz("50 zakres: liczba zmienionych", iZmienione == 1);
string[] aStan2 = Zadania.PrzelaczStanZakresu(aStan, out bPo, out iZmienione);
Sprawdz("51 zakres wszystko zrobione: odznaczamy", !bPo && aStan2[0] == "- [ ] a" && aStan2[1] == "- [ ] b");
Sprawdz("52 zakres bez zadan daje null", Zadania.PrzelaczStanZakresu(new string[] {"a", "b"}, out bPo, out iZmienione) == null);

// --- POSTEP ---
int iZr, iWsz;
Zadania.Postep(new string[] {"- [x] a", "- [ ] b", "- [x] c", "zwykly tekst"}, out iZr, out iWsz);
Sprawdz("53 postep: 2 z 3 (zwykly tekst nie liczy sie)", iZr == 2 && iWsz == 3);
Rowne("54 postep: zdanie mowione", "2 of 3 done, 67 percent", Zadania.OpisPostepu(2, 3));
Rowne("55 postep: brak pozycji", "No task list items!", Zadania.OpisPostepu(0, 0));
Rowne("56 postep: wszystko zrobione", "4 of 4 done, 100 percent", Zadania.OpisPostepu(4, 4));

// --- PODPISY DLA CZYTNIKA ---
Rowne("57 okno: stan NA POCZATKU pozycji (niezrobione)", "to do: kupic chleb", Zadania.PozycjaDoOkna("- [ ] kupic chleb"));
Rowne("58 okno: stan NA POCZATKU pozycji (zrobione)", "done: kupic chleb", Zadania.PozycjaDoOkna("- [x] kupic chleb"));
Rowne("59 okno: pozycja bez tresci ma opis, nie pustke", "to do: (empty)", Zadania.PozycjaDoOkna("- [ ]"));
Rowne("60 podglad: slowo zamiast nawiasow (zrobione)", "[done] ", Zadania.ZnacznikDoPodgladu(true));
Rowne("61 podglad: slowo zamiast nawiasow (niezrobione)", "[to do] ", Zadania.ZnacznikDoPodgladu(false));

// --- DLUGOSC PREFIKSU (kursor na tresci, nie na znaczniku) ---
Sprawdz("62 prefiks '- [ ] ' ma 6 znakow", Zadania.DlugoscPrefiksu("- [ ] a") == 6);
Sprawdz("63 prefiks z wcieciem liczy wciecie", Zadania.DlugoscPrefiksu("  - [x] a") == 8);
Sprawdz("64 prefiks na NIE-zadaniu to zero", Zadania.DlugoscPrefiksu("- a") == 0);

Console.WriteLine();
Console.WriteLine("WYNIK: " + iOk + "/" + (iOk + iZle) + " OK, " + iZle + " ZLE");
return (iZle == 0) ? 0 : 1;
} // Main method

} // PomiarZadania class
