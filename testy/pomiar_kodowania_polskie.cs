// POMIAR ROZPOZNAWANIA STARYCH POLSKICH KODOWAN (zadanie 9, 12.09.2026).
//
// Sprawdza PickPolishLegacyEncoding: czy z samych bajtow pliku wychodzi to
// kodowanie, w ktorym plik naprawde zapisano.  Bez tego pomiaru "rozpoznawanie"
// jest tylko obietnica - a pomylka daje polskie litery zamienione na inne znaki
// i utrwala to przy zapisie.
//
// Sposob: bierzemy prawdziwe polskie zdania, zapisujemy KAZDE w kazdym z trzech
// kodowan (windows-1250, Latin II, Mazovia) i pytamy funkcje, co to jest.
// Odpowiedz musi zgadzac sie z tym, czym plik faktycznie jest.
//
// Kompilacja: bash uruchom_pomiar.sh testy/pomiar_kodowania_polskie.cs testy/mazovia_kopia.cs testy/legacy_kopia.cs

using System;
using System.Collections.Generic;
using System.Text;

public class PomiarKodowaniaPolskie {

// Zdania z gestwina polskich znakow - takie, jakie realnie siedza w starych
// plikach: pismo urzedowe, notatka, tabelka DOS-owa.
static string[] aTexts = new string[] {
"Za\u017C\u00F3\u0142\u0107 g\u0119\u015Bl\u0105 ja\u017A\u0144. \u0179d\u017Ab\u0142o \u015Bci\u0119\u0142o \u0142\u0105k\u0119.",

"Uprzejmie informuj\u0119, \u017Ce w zwi\u0105zku z Pa\u0144stwa wnioskiem z dnia 12 wrze\u015Bnia "
+ "przekazuj\u0119 nast\u0119puj\u0105ce wyja\u015Bnienia dotycz\u0105ce mo\u017Cliwo\u015Bci uzyskania za\u015Bwiadczenia. "
+ "Sprawa zosta\u0142a przekazana do Wydzia\u0142u \u015Awiadcze\u0144 Rodzinnych.",

"Notatka: kupi\u0107 \u015Bwie\u017Ce bu\u0142ki, ser \u017C\u00F3\u0142ty, \u015Bmietan\u0119 i \u0107wik\u0142\u0119. "
+ "Zadzwoni\u0107 do Ma\u0142gorzaty w sprawie ksi\u0105\u017Cki o \u017Beglarstwie.",

// Krotki tekst - najtrudniejszy przypadek, bo malo materialu.
"\u0141\u00F3d\u017A, \u017Cabia 5",

// Tabelka DOS-owa z ramkami: sprawdza, czy ramki nie przewazaja nad literami.
"\u250C\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2510\r\n"
+ "\u2502 Nazwisko    \u2502\r\n"
+ "\u251C\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2524\r\n"
+ "\u2502 \u017Bura\u0144ski    \u2502\r\n"
+ "\u2502 \u015Awi\u0105tek     \u2502\r\n"
+ "\u2502 \u0141\u0105cki      \u2502\r\n"
+ "\u2514\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2518\r\n"
};

static string[] aNames = new string[] {
"pangram", "pismo urzedowe", "notatka", "krotki adres", "tabelka DOS z ramkami"
};

public static int Main(string[] args) {
int iBad = 0, iAll = 0;

Encoding en1250, en852, en667;
try { en1250 = Encoding.GetEncoding(1250); }
catch (Exception ex) { Console.WriteLine("BRAK strony 1250: " + ex.Message); return 2; }
try { en852 = Encoding.GetEncoding(852); }
catch (Exception ex) { Console.WriteLine("BRAK strony 852: " + ex.Message); return 2; }
en667 = new MazoviaEncoding();

Encoding[] aEnc = new Encoding[] { en1250, en852, en667 };
string[] aEncName = new string[] { "windows-1250", "Latin II (852)", "Mazovia (667)" };
int[] aEncCode = new int[] { 1250, 852, 667 };

for (int iT = 0; iT < aTexts.Length; iT++) {
Console.WriteLine("== tekst: " + aNames[iT]);
for (int iE = 0; iE < aEnc.Length; iE++) {
// Ramki w windows-1250 nie istnieja, wiec tabelki nie ma sensu w niej zapisywac -
// taki plik nigdy nie powstal i pytanie o niego byloby zmyslone.
if (iT == 4 && aEncCode[iE] == 1250) {
Console.WriteLine("  {0,-16} pominiete (ramek nie ma w tej stronie kodowej)", aEncName[iE]);
continue;
}
byte[] aBytes = aEnc[iE].GetBytes(aTexts[iT]);
Encoding enGuess = LegacyProbe.PickPolishLegacyEncoding(aBytes);
int iGuess = enGuess.CodePage;
bool bOk = (iGuess == aEncCode[iE]);
iAll++;
if (!bOk) iBad++;
Console.WriteLine("  zapisane jako {0,-16} -> rozpoznane {1,-6} {2}",
aEncName[iE], iGuess, bOk ? "OK" : "BLAD");
// Gdy rozpoznanie jest zle, pokaz co z tego wychodzi - to widac od razu.
if (!bOk) {
string sBad = enGuess.GetString(aBytes);
string sGood = aEnc[iE].GetString(aBytes);
Console.WriteLine("    powinno: " + Skrot(sGood));
Console.WriteLine("    wyszlo:  " + Skrot(sBad));
}
}
}

// PRZYPADKI KONTROLNE - czy zmiana nie psuje tego, co dzialalo wczesniej.
// Do tej pory funkcji nie bylo i zawsze wychodzila strona systemowa (1250).
// Tekst bez polskich liter nie ma sie z czego rozstrzygac, wiec MUSI zostac
// przy 1250 - inaczej pogorszylbym przypadek, ktory dzialal.
Console.WriteLine();
Console.WriteLine("== przypadki kontrolne (nie wolno pogorszyc)");
string[] aCtrlText = new string[] {
"Gru\u00DFe aus M\u00FCnchen, Herr M\u00FCller. Stra\u00DFe 5.",
"Caf\u00E9 na rogu, r\u00E9sum\u00E9 i na\u00EFve.",
""
};
string[] aCtrlName = new string[] {
"niemiecki bez polskich liter", "francuski bez polskich liter", "pusty plik"
};
for (int i = 0; i < aCtrlText.Length; i++) {
byte[] aB = en1250.GetBytes(aCtrlText[i]);
int iG = LegacyProbe.PickPolishLegacyEncoding(aB).CodePage;
bool bOk = (iG == 1250);
iAll++;
if (!bOk) iBad++;
Console.WriteLine("  {0,-32} -> {1,-6} {2}", aCtrlName[i], iG, bOk ? "OK" : "BLAD (pogorszenie!)");
}

Console.WriteLine();
Console.WriteLine("Sprawdzonych przypadkow: {0}, bledow: {1}", iAll, iBad);
Console.WriteLine(iBad == 0 ? "WYNIK: wszystko OK" : "WYNIK: BLEDOW " + iBad);
return iBad == 0 ? 0 : 1;
} // Main

static string Skrot(string s) {
s = s.Replace("\r", " ").Replace("\n", " ");
return s.Length > 70 ? s.Substring(0, 70) + "..." : s;
} // Skrot
} // PomiarKodowaniaPolskie
