// KOPIA klasy MazoviaEncoding wycieta z EdSharp.cs WYLACZNIE do pomiaru
// (testy/pomiar_mazovia.cs).  Nie edytowac tutaj - zmiany robic w EdSharp.cs
// i wygenerowac kopie ponownie, inaczej pomiar zmierzy nieistniejacy kod.
using System;
using System.Collections.Generic;
using System.Text;

// KODOWANIE MAZOVIA (strona kodowa 667), DOLOZONE 12.09.2026.
// Zadanie 9 z listy Kasperczaka: "Mazovia, Latin II (CP852), Windows-1250.
// Wczytanie + automatyczna konwersja do UTF-8".
//
// DLACZEGO WLASNA KLASA, A NIE Encoding.GetEncoding: .NET NIE ZNA Mazovii.
// Windows-1250 (strona 1250) i Latin II (strona 852) sa w systemie i wystarczy
// je zawolac po numerze - Mazovii nie ma tam wcale, wiec tablica musi byc
// nasza.  To NIE jest zgadywanie: Mazovia to strona 437 z siedemnastoma
// pozycjami podmienionymi na polskie litery, a pozycje te maja ustalone wartosci
// (Wikipedia "Mazovia encoding", tablica strony 667; ten sam uklad w justapedia
// i w opisie konwertera PLC Gryszkalisa).
//
// GORNA POLOWA BIERZE SIE ZE STRONY 437 W CZASIE DZIALANIA, nie z przepisanej
// recznie listy 128 znakow: ramki i znaki matematyczne sa w Mazovii DOKLADNIE
// takie jak w 437 (to byl caly sens tego kodowania - Norton Commander mial
// rysowac ramki poprawnie), a przepisywanie ich z palca to 128 okazji na
// literowke, ktorej nikt nie zauwazy.  Podmieniamy tylko te 17 pozycji, ktore
// FAKTYCZNIE sie roznia (wszystkie w zakresie 0x86-0xA7).
//
// ZAPIS: dokument wczytany jako Mazovia zapisuje sie w UTF-8 - robi to
// GetSaveEncoding, bo 667 nie jest na liscie kodowan zostawianych w spokoju.
// O to wlasnie chodzilo w zadaniu: plik raz otwarty w naszym edytorze przestaje
// byc pulapka na polskie litery.
public class MazoviaEncoding : Encoding {
// Znaki dla bajtow 0x80-0xFF.  Indeks 0 to bajt 0x80.
private static char[] aMap = null;
private static Dictionary<char, byte> dBack = null;

// Pozycje, w ktorych Mazovia rozni sie od strony kodowej 437.
// Bajt, potem znak Unicode.
private static int[] aDiffByte = new int[] {
0x86, 0x8D, 0x8F, 0x90, 0x91, 0x92, 0x95, 0x98,
0x9C, 0x9E, 0xA0, 0xA1, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7
};
private static char[] aDiffChar = new char[] {
'\u0105', // 86 a z ogonkiem
'\u0107', // 8D c z kreska
'\u0104', // 8F A z ogonkiem
'\u0118', // 90 E z ogonkiem
'\u0119', // 91 e z ogonkiem
'\u0142', // 92 l z kreska
'\u0106', // 95 C z kreska
'\u015A', // 98 S z kreska
'\u0141', // 9C L z kreska
'\u015B', // 9E s z kreska
'\u0179', // A0 Z z kreska
'\u017B', // A1 Z z kropka
'\u00D3', // A3 O z kreska
'\u0144', // A4 n z kreska
'\u0143', // A5 N z kreska
'\u017A', // A6 z z kreska
'\u017C'  // A7 z z kropka
};

private static void build() {
if (aMap != null) return;
char[] a = new char[128];
// Podstawa: strona kodowa 437.  Gdyby jej w systemie nie bylo (co sie nie
// zdarza na Windowsie, ale kod ma nie wybuchac), zostaja znaki zapytania -
// polskie litery i tak beda poprawne, bo ida z podmiany ponizej.
try {
Encoding en437 = Encoding.GetEncoding(437);
byte[] aBytes = new byte[128];
for (int i = 0; i < 128; i++) aBytes[i] = (byte)(128 + i);
string s = en437.GetString(aBytes);
for (int i = 0; i < 128 && i < s.Length; i++) a[i] = s[i];
}
catch {
for (int i = 0; i < 128; i++) a[i] = '?';
}
for (int i = 0; i < aDiffByte.Length; i++) a[aDiffByte[i] - 128] = aDiffChar[i];
Dictionary<char, byte> d = new Dictionary<char, byte>();
for (int i = 0; i < 128; i++) if (!d.ContainsKey(a[i])) d[a[i]] = (byte)(128 + i);
dBack = d;
aMap = a;
} // build method

public MazoviaEncoding() { build(); }

public override int CodePage { get { return 667; } }
public override string EncodingName { get { return "Polish (Mazovia)"; } }
public override string WebName { get { return "cp667"; } }
public override bool IsSingleByte { get { return true; } }

public override int GetByteCount(char[] aChars, int iIndex, int iCount) { return iCount; }
public override int GetCharCount(byte[] aBytes, int iIndex, int iCount) { return iCount; }
public override int GetMaxByteCount(int iCharCount) { return iCharCount; }
public override int GetMaxCharCount(int iByteCount) { return iByteCount; }

public override int GetBytes(char[] aChars, int iCharIndex, int iCharCount, byte[] aBytes, int iByteIndex) {
build();
for (int i = 0; i < iCharCount; i++) {
char c = aChars[iCharIndex + i];
byte b;
if (c < 128) b = (byte)c;
else if (dBack.TryGetValue(c, out b)) {}
// Znak, ktorego w Mazovii NIE MA, idzie jako pytajnik - tak samo jak w
// kazdym jednobajtowym kodowaniu .NET-u.  Zapis do Mazovii i tak nie jest
// nasza droga wyjscia (zapisujemy w UTF-8), ale kodowanie musi dzialac w
// obie strony, bo .NET wola GetBytes np. przy liczeniu dlugosci.
else b = (byte)'?';
aBytes[iByteIndex + i] = b;
}
return iCharCount;
} // GetBytes method

public override int GetChars(byte[] aBytes, int iByteIndex, int iByteCount, char[] aChars, int iCharIndex) {
build();
for (int i = 0; i < iByteCount; i++) {
byte b = aBytes[iByteIndex + i];
aChars[iCharIndex + i] = (b < 128) ? (char)b : aMap[b - 128];
}
return iByteCount;
} // GetChars method
} // MazoviaEncoding class
