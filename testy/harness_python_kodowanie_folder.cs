using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

// Harness dla trzech poprawek przeniesionych z galezi autora do EdSharpNG 5.0.31.
// Cialo metod skopiowane 1:1 z EdSharp.cs. Mierzymy TEKSTOWA logike:
// (1) wybor kolumny przy bledzie wciecia, (2) skracanie komunikatu Pythona,
// (3) arytmetyczny bezpiecznik przeciw falszywemu rozpoznaniu UTF-16.
// To NIE jest dowod mowy czytnika - tego nie mamy czym zmierzyc.
class T {

static int iPass = 0, iFail = 0;

static void Check(string sName, bool bOk, string sGot) {
if (bOk) { iPass++; Console.WriteLine("PASS  " + sName); }
else { iFail++; Console.WriteLine("FAIL  " + sName + "  -> " + sGot); }
}

// ---- kopia logiki wyboru pozycji z menuMiscCompile ----------------------
static string JumpColumn(string sOutput, string sJumpPosition, out string sLineOut) {
sLineOut = "";
Match m = Regex.Match(sOutput, sJumpPosition);
if (!m.Success) return "";
string sText = m.Value;
Match mn = Regex.Match(sText, @"\d+");
if (!mn.Success) return "";
string sLine = mn.Value;
sLineOut = sLine;
sText = sText.Substring(mn.Index + sLine.Length);
Match mc = Regex.Match(sText, @"\d+");
string sColumn = mc.Success ? mc.Value : "1";
if (sOutput.IndexOf("IndentationError", StringComparison.OrdinalIgnoreCase) >= 0 || sOutput.IndexOf("TabError", StringComparison.OrdinalIgnoreCase) >= 0) sColumn = "1";
return sColumn;
}

// ---- kopia logiki skracania --------------------------------------------
static string Abbreviate(string sOutput, string sStored, string sDefault) {
string sAbbreviateOutput = sStored;
if (sDefault.Length > 0 && (sAbbreviateOutput == "\\r" || sAbbreviateOutput == "\r" || sAbbreviateOutput.Trim().Length == 0)) sAbbreviateOutput = sDefault;
return Regex.Replace(sOutput, sAbbreviateOutput, "\n", RegexOptions.Multiline).Trim();
}

// ---- kopia bezpiecznika UTF-16 ----------------------------------------
static string EncodingVerdict(byte[] aBytes, string sDetected) {
string sVerdict = sDetected;
if (sDetected == "utf16le" || sDetected == "utf16be" || sDetected == "utf32") {
int iSample = Math.Min(aBytes.Length, 4096);
bool bAnyZero = false;
for (int i = 0; i < iSample; i++) if (aBytes[i] == 0) { bAnyZero = true; break; }
if (!bAnyZero) return "utf8b";
}
return sVerdict;
}

static void Main() {
string sPyJump = @"line \d+";
string sPyAbbr = @"(^[ \t]*File "".*?"", )|(^Traceback \(most recent call last\):[ \t]*\r?\n)";

// 1. Zwykly blad wykonania: kursor idzie na wskazany wiersz, kolumna 1
//    (Python nie podaje kolumny), a wiersz musi byc odczytany poprawnie.
string sRun = "Traceback (most recent call last):\r\n  File \"C:\\prace\\skrypt.py\", line 4, in <module>\r\n    print(x)\r\nNameError: name 'x' is not defined\r\n";
string sLine;
string sCol = JumpColumn(sRun, sPyJump, out sLine);
Check("1a wiersz z traceback = 4", sLine == "4", "wiersz=" + sLine);
Check("1b kolumna zwyklego bledu = 1", sCol == "1", "kolumna=" + sCol);

// 2. Blad wciecia: Python stawia znacznik na KONCU wiersza; my musimy
//    wrocic na kolumne 1, bo poprawka jest na poczatku wiersza.
string sIndent = "  File \"C:\\prace\\skrypt.py\", line 7\r\n    print(x)\r\n    ^\r\nIndentationError: unexpected indent\r\n";
sCol = JumpColumn(sIndent, sPyJump, out sLine);
Check("2a wiersz bledu wciecia = 7", sLine == "7", "wiersz=" + sLine);
Check("2b kolumna przy IndentationError = 1", sCol == "1", "kolumna=" + sCol);

// 2c. TabError to ten sam przypadek.
string sTab = "  File \"C:\\prace\\skrypt.py\", line 12\r\n\tprint(x)\r\n\t^\r\nTabError: inconsistent use of tabs and spaces in indentation\r\n";
sCol = JumpColumn(sTab, sPyJump, out sLine);
Check("2c kolumna przy TabError = 1", sCol == "1", "kolumna=" + sCol);

// 3. Skracanie: sciezka do pliku i naglowek traceback wypadaja, wiec mowa
//    zaczyna sie od tego, co ma znaczenie.
string sShort = Abbreviate(sRun, "\\r", sPyAbbr);
Check("3a naglowek Traceback usuniety", sShort.IndexOf("Traceback") < 0, sShort);
Check("3b sciezka pliku usunieta", sShort.IndexOf("C:\\prace") < 0, sShort);
Check("3c numer wiersza zostaje", sShort.IndexOf("line 4") >= 0, sShort);
Check("3d tresc bledu zostaje", sShort.IndexOf("NameError") >= 0, sShort);

// 3e. KONTROLA NEGATYWNA: stary sposob (bez domyslnego wzorca) MUSI
//     zostawic i sciezke, i naglowek. Bez tego test nie dowodzi niczego.
string sOld = Abbreviate(sRun, "\\r", "");
Check("3e kontrola: bez poprawki sciezka ZOSTAJE", sOld.IndexOf("C:\\prace") >= 0, sOld);
Check("3f kontrola: bez poprawki Traceback ZOSTAJE", sOld.IndexOf("Traceback") >= 0, sOld);

// 3g. Wlasny wzorzec uzytkownika wygrywa nad domyslnym.
string sUser = Abbreviate("alfa\r\nbeta\r\n", "beta", sPyAbbr);
Check("3g wlasny wzorzec uzytkownika wygrywa", sUser.IndexOf("beta") < 0 && sUser.IndexOf("alfa") >= 0, sUser);

// 4. Bezpiecznik kodowania: zwykly tekst jednobajtowy bez zer, ktory
//    detektor bledne uznal za UTF-16 - arytmetyka wygrywa.
byte[] aAscii = Encoding.GetEncoding(1250).GetBytes("Parafia Wszystkich Swietych, ogloszenia parafialne.\r\n");
Check("4a falszywy UTF-16 odrzucony", EncodingVerdict(aAscii, "utf16le") == "utf8b", EncodingVerdict(aAscii, "utf16le"));

// 4b. KONTROLA NEGATYWNA: prawdziwy UTF-16 (ma bajty zerowe) NIE moze byc
//     odrzucony, bo wtedy zepsulibysmy czytanie realnych plikow UTF-16.
byte[] aWide = Encoding.Unicode.GetBytes("Prawdziwy plik szesnastobitowy");
Check("4b prawdziwy UTF-16 zostaje UTF-16", EncodingVerdict(aWide, "utf16le") == "utf16le", EncodingVerdict(aWide, "utf16le"));

// 4c. Bezpiecznik nie rusza innych kodowan.
Check("4c windows-1250 nietkniete", EncodingVerdict(aAscii, "windows1250") == "windows1250", EncodingVerdict(aAscii, "windows1250"));

// 4d. Pusta probka nie wywala sie.
Check("4d pusta zawartosc bez wyjatku", EncodingVerdict(new byte[0], "utf16le") == "utf8b", EncodingVerdict(new byte[0], "utf16le"));

// 5. Ostatni otwarty folder: sprawdzamy sam warunek sciezki, ktorym kod
//    decyduje, czy w ogole probuje przestawic biezacy katalog.
Check("5a pelna sciezka rozpoznana", @"C:\prace\plik.md".IndexOf(@":\") > 0, "");
Check("5b sama nazwa pliku pomijana", "plik.md".IndexOf(@":\") <= 0, "");
Check("5c sciezka UNC pomijana (brak dwukropka)", @"\\serwer\udzial\plik.md".IndexOf(@":\") <= 0, "");

Console.WriteLine();
Console.WriteLine("WYNIK: " + iPass + " PASS, " + iFail + " FAIL");
} // Main method

} // T class
