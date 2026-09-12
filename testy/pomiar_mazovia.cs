// POMIAR TABLICY MAZOVIA (strona kodowa 667) - zadanie 9, 12.09.2026.
//
// Po co osobny pomiar, a nie "wyglada dobrze": tablica kodowania to 17 par
// bajt-znak przepisanych z opisu.  Literowka w jednej parze daje jedna polska
// litere zamieniona na inna w KAZDYM otwartym pliku, i nikt tego nie zauwazy,
// dopoki nie trafi na slowo z ta wlasnie litera.  Wiec: bajty w jedna strone,
// bajty w druga, i porownanie ze wzorcem znak po znaku.
//
// Kompilacja i uruchomienie (Windows, tam gdzie jest csc):
//   csc /nologo /out:pomiar_mazovia.exe pomiar_mazovia.cs mazovia_kopia.cs
// Na Linuksie: mono, tak samo.
//
// UWAGA: ten plik NIE jest czescia programu.  Kopia klasy do testu jest
// wstrzykiwana osobno, zeby pomiar nie wymagal budowania calego EdSharpa.

using System;
using System.Collections.Generic;
using System.Text;

public class PomiarMazovia {

// Wzorzec: 17 pozycji, w ktorych Mazovia rozni sie od strony 437.
// Zrodlo: tablica strony kodowej 667 (Wikipedia "Mazovia encoding",
// justapedia, opis konwertera PLC).
static int[] aByte = new int[] {
0x86, 0x8D, 0x8F, 0x90, 0x91, 0x92, 0x95, 0x98,
0x9C, 0x9E, 0xA0, 0xA1, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7
};
static char[] aChar = new char[] {
'\u0105','\u0107','\u0104','\u0118','\u0119','\u0142','\u0106','\u015A',
'\u0141','\u015B','\u0179','\u017B','\u00D3','\u0144','\u0143','\u017A','\u017C'
};

public static int Main(string[] args) {
int iBad = 0;
Encoding en = new MazoviaEncoding();

Console.WriteLine("== 1. Bajt -> znak (17 polskich pozycji)");
for (int i = 0; i < aByte.Length; i++) {
byte[] ab = new byte[] { (byte)aByte[i] };
string s = en.GetString(ab);
bool bOk = (s.Length == 1 && s[0] == aChar[i]);
if (!bOk) iBad++;
Console.WriteLine("  {0:X2} -> U+{1:X4} (oczekiwano U+{2:X4}) {3}",
aByte[i], (int)s[0], (int)aChar[i], bOk ? "OK" : "BLAD");
}

Console.WriteLine("== 2. Znak -> bajt (droga powrotna)");
for (int i = 0; i < aChar.Length; i++) {
byte[] ab = en.GetBytes(aChar[i].ToString());
bool bOk = (ab.Length == 1 && ab[0] == (byte)aByte[i]);
if (!bOk) iBad++;
Console.WriteLine("  U+{0:X4} -> {1:X2} (oczekiwano {2:X2}) {3}",
(int)aChar[i], ab[0], aByte[i], bOk ? "OK" : "BLAD");
}

Console.WriteLine("== 3. Wszystkie 32 polskie litery przez pelne zdanie");
// Pangram z wszystkimi polskimi znakami diakrytycznymi, malymi i wielkimi.
string sPangram = "Zazolc gesla jazn " +
"\u0105\u0107\u0119\u0142\u0144\u00F3\u015B\u017A\u017C " +
"\u0104\u0106\u0118\u0141\u0143\u00D3\u015A\u0179\u017B";
byte[] aEnc = en.GetBytes(sPangram);
string sBack = en.GetString(aEnc);
bool bRound = (sBack == sPangram);
if (!bRound) iBad++;
Console.WriteLine("  tam i z powrotem: {0}", bRound ? "OK" : "BLAD");
if (!bRound) {
Console.WriteLine("  bylo: " + sPangram);
Console.WriteLine("  jest: " + sBack);
}
// Zaden polski znak nie moze wyjsc jako pytajnik - to znaczyloby brak w tablicy.
int iQuestion = 0;
for (int i = 0; i < aEnc.Length; i++) if (aEnc[i] == (byte)'?' && sPangram[i] != '?') iQuestion++;
if (iQuestion > 0) iBad++;
Console.WriteLine("  znaki zgubione (pytajniki): {0} {1}", iQuestion, iQuestion == 0 ? "OK" : "BLAD");

Console.WriteLine("== 4. ASCII nietkniety (0-127)");
int iAsciiBad = 0;
for (int i = 0; i < 128; i++) {
string s = en.GetString(new byte[] { (byte)i });
if (s.Length != 1 || s[0] != (char)i) iAsciiBad++;
}
if (iAsciiBad > 0) iBad++;
Console.WriteLine("  niezgodnosci: {0} {1}", iAsciiBad, iAsciiBad == 0 ? "OK" : "BLAD");

Console.WriteLine("== 5. Ramki jak w stronie 437 (sens Mazovii)");
// 0xC4 pozioma, 0xB3 pionowa, 0xDA lewy gorny rog, 0xB0 raster.
int[] aFrame = new int[] { 0xC4, 0xB3, 0xDA, 0xB0, 0xDB };
char[] aFrameWant = new char[] { '\u2500', '\u2502', '\u250C', '\u2591', '\u2588' };
int iFrameBad = 0;
for (int i = 0; i < aFrame.Length; i++) {
string s = en.GetString(new byte[] { (byte)aFrame[i] });
bool bOk = (s.Length == 1 && s[0] == aFrameWant[i]);
if (!bOk) iFrameBad++;
Console.WriteLine("  {0:X2} -> U+{1:X4} (oczekiwano U+{2:X4}) {3}",
aFrame[i], (int)s[0], (int)aFrameWant[i], bOk ? "OK" : "BLAD");
}
if (iFrameBad > 0) iBad++;

Console.WriteLine("== 6. Mazovia vs windows-1250: te same litery, INNE bajty");
// Dowod, ze rozroznienie kodowan ma sens: to samo slowo daje inne bajty.
try {
Encoding en1250 = Encoding.GetEncoding(1250);
string sWord = "\u017Baba \u0142\u00F3d\u017A";
byte[] a667 = en.GetBytes(sWord);
byte[] a1250 = en1250.GetBytes(sWord);
bool bDiff = false;
for (int i = 0; i < a667.Length && i < a1250.Length; i++) if (a667[i] != a1250[i]) { bDiff = true; break; }
Console.WriteLine("  bajty roznia sie: {0} {1}", bDiff, bDiff ? "OK" : "BLAD (to samo, wiec cos nie tak)");
if (!bDiff) iBad++;
}
catch (Exception ex) { Console.WriteLine("  strona 1250 niedostepna: " + ex.Message); }

Console.WriteLine();
Console.WriteLine(iBad == 0 ? "WYNIK: wszystko OK" : "WYNIK: BLEDOW " + iBad);
return iBad == 0 ? 0 : 1;
} // Main
} // PomiarMazovia
