// pomiar_zmiany_1a.cs - POMIAR KROKU 1a sledzenia zmian (Zmiany.cs).
//
// Kompilowany golym csc razem z Zmiany.cs, bez Windows Forms i bez czytnika -
// tak samo jak pomiar_zadania_611.cs i pomiar_csv.cs.
//
// Uruchomienie z WSL:  bash uruchom_pomiar.sh testy/pomiar_zmiany_1a.cs Zmiany.cs
using System;
using System.Collections.Generic;
using EdSharp;

public class PomiarZmiany {

static int iOk = 0;
static int iBlad = 0;

static void Sprawdz(string sNazwa, string sOczekiwane, string sOtrzymane) {
	if (sOczekiwane == sOtrzymane) {
		iOk++;
	} else {
		iBlad++;
		Console.WriteLine("BLAD: " + sNazwa);
		Console.WriteLine("  oczekiwane: [" + Poka(sOczekiwane) + "]");
		Console.WriteLine("  otrzymane:  [" + Poka(sOtrzymane) + "]");
	} // end if
} // Sprawdz method

static void SprawdzInt(string sNazwa, int iOczekiwane, int iOtrzymane) {
	Sprawdz(sNazwa, iOczekiwane.ToString(), iOtrzymane.ToString());
} // SprawdzInt method

static void SprawdzBool(string sNazwa, bool bOczekiwane, bool bOtrzymane) {
	Sprawdz(sNazwa, bOczekiwane ? "prawda" : "falsz", bOtrzymane ? "prawda" : "falsz");
} // SprawdzBool method

static string Poka(string s) {
	if (s == null) return "<null>";
	return s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
} // Poka method

public static int Main(string[] args) {
	Console.WriteLine("=== POMIAR 1a: rozpoznawanie zmian, przyjmij i odrzuc ===");
	Console.WriteLine();
	try {
		Mierz();
	} catch (Exception ex) {
		// Bez tego pierwszy blad rozpoznawania wywala pomiar na liscie zmian i
		// NIE WIDZIMY pozostalych grup - a wtedy jedna usterka udaje jedna
		// usterke, choc moze byc ich dziesiec.
		iBlad++;
		Console.WriteLine("BLAD: pomiar przerwany wyjatkiem - " + ex.GetType().Name + ": " + ex.Message);
	} // end try
	Console.WriteLine();
	Console.WriteLine("=== WYNIK: " + iOk + " OK, " + iBlad + " BLAD, razem " + (iOk + iBlad) + " ===");
	return iBlad == 0 ? 0 : 1;
} // Main method

static void Mierz() {
	// ---------- GRUPA 1: rozpoznanie kazdego z pieciu rodzajow
	Console.WriteLine("-- grupa 1: piec rodzajow znacznikow");
	List<Zmiana> l;

	l = Zmiany.Znajdz("Ala {++ma kota++} w domu.");
	SprawdzInt("1.1 dopisanie: jedna zmiana", 1, l.Count);
	Sprawdz("1.2 dopisanie: rodzaj", "Dopisanie", l[0].Rodzaj.ToString());
	Sprawdz("1.3 dopisanie: nowa tresc", "ma kota", l[0].Nowe);
	Sprawdz("1.4 dopisanie: starej tresci nie ma", "", l[0].Stare);

	l = Zmiany.Znajdz("Ala {--nie--} ma kota.");
	Sprawdz("1.5 usuniecie: rodzaj", "Usuniecie", l[0].Rodzaj.ToString());
	Sprawdz("1.6 usuniecie: stara tresc", "nie", l[0].Stare);
	Sprawdz("1.7 usuniecie: nowej tresci nie ma", "", l[0].Nowe);

	l = Zmiany.Znajdz("Ala ma {~~psa~>kota~~}.");
	Sprawdz("1.8 podmiana: rodzaj", "Podmiana", l[0].Rodzaj.ToString());
	Sprawdz("1.9 podmiana: stare", "psa", l[0].Stare);
	Sprawdz("1.10 podmiana: nowe", "kota", l[0].Nowe);

	l = Zmiany.Znajdz("To jest {==wazne==} zdanie.");
	Sprawdz("1.11 podswietlenie: rodzaj", "Podswietlenie", l[0].Rodzaj.ToString());
	Sprawdz("1.12 podswietlenie: tresc", "wazne", l[0].Nowe);

	l = Zmiany.Znajdz("Zdanie. {>>To trzeba skrocic<<}");
	Sprawdz("1.13 komentarz: rodzaj", "Komentarz", l[0].Rodzaj.ToString());
	Sprawdz("1.14 komentarz: tresc", "To trzeba skrocic", l[0].Nowe);

	// ---------- GRUPA 2: kilka zmian naraz i NIEZACHLANNOSC
	Console.WriteLine("-- grupa 2: kilka zmian w jednym tekscie");

	// Gdyby wzorzec byl zachlanny, PIERWSZA zmiana zjadlaby tekst do
	// ostatniego domkniecia i dostalibysmy jedna zmiane zamiast trzech.
	string sTrzy = "A {++jeden++} B {--dwa--} C {~~trzy~>cztery~~} D";
	l = Zmiany.Znajdz(sTrzy);
	SprawdzInt("2.1 trzy zmiany, nie jedna (niezachlannosc)", 3, l.Count);
	Sprawdz("2.2 pierwsza tresc", "jeden", l[0].Nowe);
	Sprawdz("2.3 druga tresc", "dwa", l[1].Stare);
	Sprawdz("2.4 trzecia tresc", "cztery", l[2].Nowe);
	SprawdzBool("2.5 kolejnosc rosnaca", true, l[0].Start < l[1].Start && l[1].Start < l[2].Start);

	// Dwa dopisania obok siebie - klasyczny przypadek na zachlannosc.
	l = Zmiany.Znajdz("{++raz++}{++dwa++}");
	SprawdzInt("2.6 dwie zmiany bez odstepu", 2, l.Count);
	Sprawdz("2.7 druga z nich", "dwa", l[1].Nowe);

	// ---------- GRUPA 3: zmiana przez kilka wierszy
	Console.WriteLine("-- grupa 3: zmiana przechodzaca przez koniec wiersza");
	string sWiele = "Poczatek.\r\n{++Nowy akapit,\r\ndrugi wiersz.++}\r\nKoniec.";
	l = Zmiany.Znajdz(sWiele);
	SprawdzInt("3.1 znaleziona mimo konca wiersza w srodku", 1, l.Count);
	Sprawdz("3.2 tresc z koncem wiersza", "Nowy akapit,\r\ndrugi wiersz.", l[0].Nowe);
	Sprawdz("3.3 tekst po przyjeciu", "Poczatek.\r\nNowy akapit,\r\ndrugi wiersz.\r\nKoniec.", Zmiany.TekstPoPrzyjeciu(sWiele));
	Sprawdz("3.4 tekst po odrzuceniu", "Poczatek.\r\n\r\nKoniec.", Zmiany.TekstPoOdrzuceniu(sWiele));

	// ---------- GRUPA 4: przyjmij wszystko i odrzuc wszystko
	Console.WriteLine("-- grupa 4: przyjmij i odrzuc wszystko (piec rodzajow razem)");
	string sPelny = "Ala {++bardzo ++}lubi {--stare --}koty {~~male~>duze~~} i {==rude==}.{>>uwaga<<}";
	Sprawdz("4.1 przyjmij wszystko", "Ala bardzo lubi koty duze i rude.", Zmiany.TekstPoPrzyjeciu(sPelny));
	Sprawdz("4.2 odrzuc wszystko", "Ala lubi stare koty male i rude.", Zmiany.TekstPoOdrzuceniu(sPelny));

	// Tekst bez zmian musi wyjsc NIETKNIETY - i to samo w obie strony.
	string sCzysty = "Zwykly tekst, bez zadnych znacznikow. 2 + 2 = 4.";
	Sprawdz("4.3 tekst bez zmian, przyjmij", sCzysty, Zmiany.TekstPoPrzyjeciu(sCzysty));
	Sprawdz("4.4 tekst bez zmian, odrzuc", sCzysty, Zmiany.TekstPoOdrzuceniu(sCzysty));
	SprawdzBool("4.5 CzyMaZmiany na czystym tekscie", false, Zmiany.CzyMaZmiany(sCzysty));
	SprawdzBool("4.6 CzyMaZmiany na tekscie ze zmiana", true, Zmiany.CzyMaZmiany(sPelny));

	// Puste i null nie moga wywalac programu.
	Sprawdz("4.7 pusty tekst", "", Zmiany.TekstPoPrzyjeciu(""));
	Sprawdz("4.8 null", "", Zmiany.TekstPoPrzyjeciu(null));
	SprawdzInt("4.9 null nie daje zmian", 0, Zmiany.Znajdz(null).Count);

	// ---------- GRUPA 5: pojedyncza zmiana pod kursorem
	Console.WriteLine("-- grupa 5: przyjmij i odrzuc w miejscu kursora");
	string sDwie = "Ala {++ma++} kota {--rudego--}.";
	// "Ala " to 4 znaki, wiec zmiana zaczyna sie na 4.
	int iStart = sDwie.IndexOf("{++");
	Sprawdz("5.1 przyjmij pierwsza", "Ala ma kota {--rudego--}.", Zmiany.PrzyjmijWMiejscu(sDwie, iStart + 2));
	Sprawdz("5.2 odrzuc pierwsza", "Ala  kota {--rudego--}.", Zmiany.OdrzucWMiejscu(sDwie, iStart + 2));
	int iDruga = sDwie.IndexOf("{--");
	Sprawdz("5.3 przyjmij druga", "Ala {++ma++} kota .", Zmiany.PrzyjmijWMiejscu(sDwie, iDruga + 1));
	Sprawdz("5.4 odrzuc druga", "Ala {++ma++} kota rudego.", Zmiany.OdrzucWMiejscu(sDwie, iDruga + 1));

	// Kursor POZA zmiana musi dac null - "nie ma czego przyjac", zeby
	// wolajacy powiedzial to uzytkownikowi, a nie milczal.
	SprawdzBool("5.5 kursor poza zmiana daje null", true, Zmiany.PrzyjmijWMiejscu(sDwie, 1) == null);
	SprawdzBool("5.6 tekst bez zmian daje null", true, Zmiany.PrzyjmijWMiejscu(sCzysty, 3) == null);

	// Kursor DOKLADNIE na koncu zmiany jest juz ZA nia.
	Zmiana zPierwsza = Zmiany.Znajdz(sDwie)[0];
	SprawdzBool("5.7 kursor na koncu zmiany jest za nia", true, Zmiany.WMiejscu(sDwie, zPierwsza.Koniec) == null);
	SprawdzBool("5.8 kursor na pierwszym znaku zmiany jest w niej", true, Zmiany.WMiejscu(sDwie, zPierwsza.Start) != null);

	// ---------- GRUPA 6: skoki po zmianach
	Console.WriteLine("-- grupa 6: skoki nastepna i poprzednia");
	Zmiana zN = Zmiany.Nastepna(sDwie, 0);
	Sprawdz("6.1 nastepna od poczatku", "ma", zN.Nowe);
	Zmiana zN2 = Zmiany.Nastepna(sDwie, zN.Start);
	Sprawdz("6.2 nastepna pomija te, w ktorej stoimy", "rudego", zN2.Stare);
	SprawdzBool("6.3 nastepna za ostatnia daje null", true, Zmiany.Nastepna(sDwie, sDwie.Length) == null);

	Zmiana zP = Zmiany.Poprzednia(sDwie, sDwie.Length);
	Sprawdz("6.4 poprzednia od konca", "rudego", zP.Stare);
	Zmiana zP2 = Zmiany.Poprzednia(sDwie, zP.Start);
	Sprawdz("6.5 poprzednia dalej wstecz", "ma", zP2.Nowe);
	SprawdzBool("6.6 poprzednia przed pierwsza daje null", true, Zmiany.Poprzednia(sDwie, 0) == null);
	// Kursor w SRODKU drugiej zmiany: poprzednia to pierwsza, nie ta sama.
	Sprawdz("6.7 poprzednia z wnetrza zmiany to ta wczesniejsza", "ma", Zmiany.Poprzednia(sDwie, iDruga + 3).Nowe);

	// ---------- GRUPA 7: numer wiersza
	Console.WriteLine("-- grupa 7: numer wiersza dla odczytu");
	string sWiersze = "raz\r\ndwa\r\n{++trzy++}\r\ncztery";
	Zmiana zW = Zmiany.Znajdz(sWiersze)[0];
	SprawdzInt("7.1 zmiana jest w trzecim wierszu", 3, Zmiany.NumerWiersza(sWiersze, zW.Start));
	SprawdzInt("7.2 poczatek tekstu to wiersz 1", 1, Zmiany.NumerWiersza(sWiersze, 0));
	SprawdzInt("7.3 pozycja za koncem nie wywala", 4, Zmiany.NumerWiersza(sWiersze, 9999));
	// Plik z samym LF (bez CR) musi dawac te same numery.
	string sLf = "raz\ndwa\n{++trzy++}";
	SprawdzInt("7.4 to samo dla konca wiersza LF", 3, Zmiany.NumerWiersza(sLf, Zmiany.Znajdz(sLf)[0].Start));

	// ---------- GRUPA 8: zapis zmiany (odwrotnosc rozpoznania)
	Console.WriteLine("-- grupa 8: zapis zmiany i cykl zapis-odczyt");
	Sprawdz("8.1 zapis dopisania", "{++nowy++}", Zmiany.ZapiszDopisanie("nowy"));
	Sprawdz("8.2 zapis usuniecia", "{--stary--}", Zmiany.ZapiszUsuniecie("stary"));
	Sprawdz("8.3 zapis podmiany", "{~~a~>b~~}", Zmiany.ZapiszPodmiane("a", "b"));
	Sprawdz("8.4 zapis podswietlenia", "{==tu==}", Zmiany.ZapiszPodswietlenie("tu"));
	Sprawdz("8.5 zapis komentarza", "{>>uwaga<<}", Zmiany.ZapiszKomentarz("uwaga"));

	// CYKL: to, co zapisalismy, musi dac sie odczytac z powrotem.
	string sCykl = Zmiany.ZapiszPodmiane("kot ma Ale", "Ala ma kota");
	l = Zmiany.Znajdz(sCykl);
	SprawdzInt("8.6 cykl: zapisana zmiana jest rozpoznana", 1, l.Count);
	Sprawdz("8.7 cykl: stare wraca", "kot ma Ale", l[0].Stare);
	Sprawdz("8.8 cykl: nowe wraca", "Ala ma kota", l[0].Nowe);

	// Tresc z polskimi znakami musi przejsc bez uszkodzenia.
	string sPl = Zmiany.ZapiszDopisanie("zazolc gesla jazn: ąćęłńóśźż");
	Sprawdz("8.9 polskie litery w zmianie", "zazolc gesla jazn: ąćęłńóśźż", Zmiany.Znajdz(sPl)[0].Nowe);

	// Tresc, ktora SAMA zawiera znacznik, nie moze rozwalic zapisu.
	// CriticMarkup nie ma znaku ucieczki, wiec rozdzielamy znaki spacja -
	// tresc zostaje czytelna, a zapis nadal da sie odczytac.
	string sPodstepny = Zmiany.ZapiszDopisanie("tu byl ++} znacznik");
	l = Zmiany.Znajdz(sPodstepny);
	SprawdzInt("8.10 tresc ze znacznikiem w srodku: nadal jedna zmiana", 1, l.Count);
	SprawdzBool("8.11 tresc ze znacznikiem: nic nie zginelo", true, l[0].Nowe.Contains("znacznik"));

	// ---------- GRUPA 9: opisy dla czytnika
	Console.WriteLine("-- grupa 9: co uslyszy czytnik");
	string sOpis = "Ala {++ma kota++} w domu.";
	Zmiana zO = Zmiany.Znajdz(sOpis)[0];
	Sprawdz("9.1 rodzaj na POCZATKU opisu", "inserted: ma kota, line 1", Zmiany.OpisDoOkna(zO, sOpis));

	string sOpis2 = "Ala {~~psa~>kota~~}.";
	Sprawdz("9.2 podmiana czytana jako bylo-jest", "replaced: was psa, now kota, line 1",
		Zmiany.OpisDoOkna(Zmiany.Znajdz(sOpis2)[0], sOpis2));

	Sprawdz("9.3 nazwa rodzaju do skoku", "deleted", Zmiany.NazwaRodzaju(RodzajZmiany.Usuniecie));

	// Licznik w tytule okna.
	Sprawdz("9.4 licznik: brak zmian", "no changes", Zmiany.OpisLiczby(sCzysty));
	Sprawdz("9.5 licznik: jedna", "1 change", Zmiany.OpisLiczby(sOpis));
	Sprawdz("9.6 licznik: trzy", "3 changes", Zmiany.OpisLiczby(sTrzy));

	// Skrot: koniec wiersza i tabulator nie moga trafic do mowy jako znaki.
	Sprawdz("9.7 skrot sklada wiersze w jeden", "raz dwa", Zmiany.Skrot("raz\r\ndwa"));
	Sprawdz("9.8 skrot z tabulatora", "a b", Zmiany.Skrot("a\tb"));
	Sprawdz("9.9 pusta tresc mowi (empty)", "(empty)", Zmiany.Skrot(""));
	Sprawdz("9.10 same spacje mowia (empty)", "(empty)", Zmiany.Skrot("   \r\n  "));
	string sDlugi = new String('x', 100);
	SprawdzBool("9.11 dlugi fragment skrocony", true, Zmiany.Skrot(sDlugi).Length < 70);
	SprawdzBool("9.12 skrot konczy sie wielokropkiem", true, Zmiany.Skrot(sDlugi).EndsWith("..."));

	// Zmiana z pusta trescia (recenzent kliknal za szybko) tez musi sie czytac.
	string sPusta = "Ala {++++} ma kota.";
	l = Zmiany.Znajdz(sPusta);
	SprawdzInt("9.13 pusta zmiana jest rozpoznana", 1, l.Count);
	Sprawdz("9.14 pusta zmiana czyta sie jako (empty)", "inserted: (empty), line 1", Zmiany.OpisDoOkna(l[0], sPusta));

	// ---------- GRUPA 10: tekst zepsuty i przypadki graniczne
	Console.WriteLine("-- grupa 10: znaczniki niedomkniete i przypadki graniczne");
	SprawdzBool("10.1 otwarcie bez domkniecia wykryte", true, Zmiany.CzyNiedomkniete("Ala {++ma kota"));
	SprawdzBool("10.2 poprawny tekst nie jest zepsuty", false, Zmiany.CzyNiedomkniete(sPelny));
	SprawdzBool("10.3 czysty tekst nie jest zepsuty", false, Zmiany.CzyNiedomkniete(sCzysty));
	// Nawias klamrowy z otwarciem WEWNATRZ komentarza nie jest brakiem
	// domkniecia - inaczej program krzyczalby na poprawnym pliku.
	SprawdzBool("10.4 znacznik w tresci komentarza nie jest bledem", false,
		Zmiany.CzyNiedomkniete("Zdanie {>>tu bylo {++cos++} kiedys<<}"));
	// Niedomkniety tekst NIE jest zmiana i zostaje w dokumencie.
	SprawdzInt("10.5 niedomkniete nie daje zmiany", 0, Zmiany.Znajdz("Ala {++ma kota").Count);
	Sprawdz("10.6 niedomkniete zostaje w tekscie nietkniete", "Ala {++ma kota", Zmiany.TekstPoPrzyjeciu("Ala {++ma kota"));

	// Zwykly nawias klamrowy w tekscie (np. w kodzie) nie moze udawac zmiany.
	string sKod = "if (a) { b++; }";
	SprawdzInt("10.7 kod z nawiasem to nie zmiana", 0, Zmiany.Znajdz(sKod).Count);
	Sprawdz("10.8 kod zostaje nietkniety", sKod, Zmiany.TekstPoPrzyjeciu(sKod));
	SprawdzBool("10.9 kod nie jest zglaszany jako zepsuty", false, Zmiany.CzyNiedomkniete(sKod));

	// Znacznik na samym poczatku i na samym koncu tekstu.
	Sprawdz("10.10 zmiana na poczatku tekstu", "raz dwa", Zmiany.TekstPoPrzyjeciu("{++raz++} dwa"));
	Sprawdz("10.11 zmiana na koncu tekstu", "raz dwa", Zmiany.TekstPoPrzyjeciu("raz {++dwa++}"));
	Sprawdz("10.12 caly tekst jest jedna zmiana", "raz", Zmiany.TekstPoPrzyjeciu("{++raz++}"));
} // Mierz method

} // PomiarZmiany class
