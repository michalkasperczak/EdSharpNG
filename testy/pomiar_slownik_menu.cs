// pomiar_slownik_menu.cs -- POMIAR trzech zmian slownika z 5.0.91
//
// Mierzy LOGIKE, nie GUI, bo GUI trzyma czlowiek. Trzy rzeczy:
//   1. UCIECZKA ESCAPE: czy okno dostaje wyjscie niezaleznie od NAZW
//      przyciskow. Stara regula (Escape tylko gdy jest przycisk "Cancel"
//      albo "Close") oblewala okno listy bledow ("Correct / Add to
//      dictionary / Ignore all / Finish") - i wlasnie na to Kasperczak
//      trafil: "Esc nie wychodzi z okienka wywolanego F7".
//   2. GRANICE WYRAZU pod kursorem - dla menu na klawiszu Aplikacje.
//      Polskie znaki, kursor w srodku wyrazu, na jego brzegu, za wyrazem,
//      lacznik, apostrof, kropka na koncu zdania.
//   3. ZAKRES SPRAWDZANIA F7: zaznaczenie > kursor > calosc.
//
// Kompilacja: csc /out:pomiar_slownik_menu.exe pomiar_slownik_menu.cs
using System;
using System.Collections.Generic;

class PomiarSlownikMenu {

// --- 1. UCIECZKA -----------------------------------------------------
// STARA regula: Escape zamyka tylko wtedy, gdy wsrod przyciskow jest
// doslownie "Cancel" albo "Close".
static bool StaraUcieczka(string[] aBtn) {
foreach (string s in aBtn) {
string t = s.Replace("&", "").Trim();
if (String.Equals(t, "Cancel", StringComparison.OrdinalIgnoreCase)) return true;
if (String.Equals(t, "Close", StringComparison.OrdinalIgnoreCase)) return true;
}
return false;
}
// NOWA regula (5.0.91): kazde okno ma ucieczke. Gdy nie ma przycisku
// Cancel/Close, Escape zamyka okno wprost i wynik jest pusty.
static bool NowaUcieczka(string[] aBtn) { return true; }

// --- 2. GRANICE WYRAZU -----------------------------------------------
// Odwzorowanie SpellingWordMenu z EdSharp.cs. Zwraca wyraz albo "".
static string WyrazPodKursorem(string sText, int iPos) {
if (sText.Length == 0) return "";
if (iPos >= sText.Length) iPos = sText.Length - 1;
if (iPos < 0) iPos = 0;
if (!Char.IsLetter(sText[iPos]) && iPos > 0 && Char.IsLetter(sText[iPos - 1])) iPos--;
if (!Char.IsLetter(sText[iPos])) return "";
int iStart = iPos;
while (iStart > 0 && (Char.IsLetter(sText[iStart - 1]) || sText[iStart - 1] == '\'' || sText[iStart - 1] == '-')) iStart--;
int iEnd = iPos;
while (iEnd + 1 < sText.Length && (Char.IsLetter(sText[iEnd + 1]) || sText[iEnd + 1] == '\'' || sText[iEnd + 1] == '-')) iEnd++;
while (iEnd > iStart && !Char.IsLetter(sText[iEnd])) iEnd--;
return sText.Substring(iStart, iEnd - iStart + 1);
}

// --- 3. ZAKRES F7 ----------------------------------------------------
// Zwraca "selected" / "cursor" / "all" i pozycje poczatku.
static string ZakresF7(int iSelStart, int iSelLen, out int iBaza) {
if (iSelLen > 0) { iBaza = iSelStart; return "selected"; }
if (iSelStart > 0) { iBaza = iSelStart; return "cursor"; }
iBaza = 0; return "all";
}

static int iZgodne = 0, iNiezgodne = 0;
static void Sprawdz(string sOpis, object oOczek, object oJest) {
bool bOk = String.Equals(Convert.ToString(oOczek), Convert.ToString(oJest));
if (bOk) iZgodne++; else iNiezgodne++;
Console.WriteLine("{0} {1}\n    oczekiwane: [{2}]\n    jest:       [{3}]",
bOk ? "ZGODNE  " : "NIEZGODNE", sOpis, oOczek, oJest);
}

static void Main() {
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("== POMIAR SLOWNIKA I MENU NA WYRAZIE (5.0.91) ==\n");

Console.WriteLine("-- 1. Ucieczka Escape z okna --");
string[] aListaBledow = new string[] {"Correct", "&Add to dictionary", "&Ignore all", "Finish"};
string[] aZwykle = new string[] {"OK", "Cancel"};
string[] aTylkoOk = new string[] {"OK"};
string[] aZamknij = new string[] {"Close"};
string[] aDone = new string[] {"Done"};

// To jest DOKLADNIE zgloszony blad: lista bledow pisowni bez ucieczki.
Sprawdz("lista bledow F7 - STARA regula (tu byl blad)", "False", StaraUcieczka(aListaBledow).ToString());
Sprawdz("lista bledow F7 - NOWA regula", "True", NowaUcieczka(aListaBledow).ToString());
Sprawdz("okno OK/Cancel - stara", "True", StaraUcieczka(aZwykle).ToString());
Sprawdz("okno OK/Cancel - nowa", "True", NowaUcieczka(aZwykle).ToString());
Sprawdz("okno tylko OK - stara (tez oblewalo)", "False", StaraUcieczka(aTylkoOk).ToString());
Sprawdz("okno tylko OK - nowa", "True", NowaUcieczka(aTylkoOk).ToString());
Sprawdz("okno Close - stara", "True", StaraUcieczka(aZamknij).ToString());
Sprawdz("okno Done - stara (oblewalo)", "False", StaraUcieczka(aDone).ToString());
Sprawdz("okno Done - nowa", "True", NowaUcieczka(aDone).ToString());

Console.WriteLine("\n-- 2. Wyraz pod kursorem --");
string s1 = "Zażółć gęślą jaźń.";
Sprawdz("polskie znaki, kursor w srodku (poz. 2)", "Zażółć", WyrazPodKursorem(s1, 2));
Sprawdz("polskie znaki, kursor na pierwszej literze", "Zażółć", WyrazPodKursorem(s1, 0));
Sprawdz("kursor na ostatniej literze wyrazu", "Zażółć", WyrazPodKursorem(s1, 5));
Sprawdz("kursor na spacji ZA wyrazem", "Zażółć", WyrazPodKursorem(s1, 6));
Sprawdz("drugi wyraz", "gęślą", WyrazPodKursorem(s1, 8));
Sprawdz("ostatni wyraz przed kropka", "jaźń", WyrazPodKursorem(s1, 14));
Sprawdz("kursor na kropce koncowej", "jaźń", WyrazPodKursorem(s1, 17));

string s2 = "Bielsko-Biała to miasto";
Sprawdz("wyraz z lacznikiem, kursor przed lacznikiem", "Bielsko-Biała", WyrazPodKursorem(s2, 3));
Sprawdz("wyraz z lacznikiem, kursor za lacznikiem", "Bielsko-Biała", WyrazPodKursorem(s2, 9));

string s3 = "d'Artagnan przybył";
Sprawdz("apostrof w wyrazie", "d'Artagnan", WyrazPodKursorem(s3, 4));

Sprawdz("pusty tekst", "", WyrazPodKursorem("", 0));
Sprawdz("same spacje", "", WyrazPodKursorem("   ", 1));
Sprawdz("kursor za koncem tekstu", "test", WyrazPodKursorem("test", 99));
Sprawdz("kursor na samotnym przecinku po spacji", "", WyrazPodKursorem("a , b", 2));
Sprawdz("wyraz jednoliterowy", "a", WyrazPodKursorem("a b", 0));
Sprawdz("lacznik na koncu nie nalezy do wyrazu", "test", WyrazPodKursorem("test- dalej", 1));

Console.WriteLine("\n-- 3. Zakres sprawdzania F7 --");
int iBaza;
Sprawdz("zaznaczenie ma pierwszenstwo", "selected", ZakresF7(50, 10, out iBaza));
Sprawdz("  baza = poczatek zaznaczenia", 50, iBaza);
Sprawdz("kursor w srodku - od kursora", "cursor", ZakresF7(120, 0, out iBaza));
Sprawdz("  baza = pozycja kursora", 120, iBaza);
Sprawdz("kursor na poczatku - calosc", "all", ZakresF7(0, 0, out iBaza));
Sprawdz("  baza = 0", 0, iBaza);
Sprawdz("zaznaczenie od poczatku pliku", "selected", ZakresF7(0, 30, out iBaza));
Sprawdz("  baza = 0", 0, iBaza);

Console.WriteLine("\n== WYNIK: {0} zgodnych, {1} niezgodnych ==", iZgodne, iNiezgodne);
Console.WriteLine(iNiezgodne == 0 ? "OK" : "SA NIEZGODNOSCI");
}
}
