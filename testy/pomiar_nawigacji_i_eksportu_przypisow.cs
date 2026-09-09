// Pomiar na ZBUDOWANEJ binarce EdSharpNG.exe: nawigacja po przypisach
// (Control+Alt+K / Control+Alt+Shift+K) oraz eksport przypisow w dwoch
// wariantach (zlecenia 1788204326709-1 i 1788204113403-0).
//
// PO CO SONDA, GDY BUILD JEST ZIELONY: zielony build nie odrozni komendy, ktora
// dziala, od komendy, ktora tylko sie kompiluje.  Mierzymy REFLEKSJA te same
// metody, ktore wola handler menu, oraz napisy komunikatow BAJTOWO w pliku exe.
//
// KONTROLE WAZNOSCI (bez nich zielony wynik nic nie dowodzi):
//   - POZYTYWNA: napis, ktory w binarce BYC MUSI ("Footnote "), oraz napis
//     wymyslony, ktorego byc NIE MOZE - to lapie glucha sonde,
//   - DYSKRYMINACYJNA przy eksporcie: warianty "tylko przypisy" i "ze zdaniami"
//     MUSZA dac ROZNY tekst.  Gdyby dawaly ten sam, "dwie opcje" byly by jedna,
//     a sonda i tak swiecila na zielono,
//   - DYSKRYMINACYJNA przy nawigacji: skok w przod i w tyl z tego samego miejsca
//     MUSZA trafic w INNE znaczniki, a na koncach dokumentu MUSZA odmowic.
//     Komenda, ktora zawsze idzie na pierwszy przypis, przeszla by test "skacze".
//
// Kompilacja i uruchomienie (z katalogu repozytorium):
//   CSC=/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe
//   mkdir -p /mnt/c/tmp/fnnav && cp EdSharpNG.exe /mnt/c/tmp/fnnav/
//   cp testy/pomiar_nawigacji_i_eksportu_przypisow.cs /mnt/c/tmp/fnnav/
//   cd /mnt/c/tmp/fnnav && "$CSC" /nologo /out:p.exe /r:EdSharpNG.exe p.cs && ./p.exe
// (csc.exe MUSI pracowac na dysku Windows - przez \\wsl.localhost nie widzi
// plikow metadanych, error CS0006.)
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

public class PomiarNawigacjiIEksportuPrzypisow {

static Assembly asm;
static Type tFrame;
static int iPass = 0;
static int iFail = 0;

static void Ok(bool b, string sName, string sGot) {
if (b) {iPass++; Console.WriteLine("PASS  " + sName);}
else {iFail++; Console.WriteLine("FAIL  " + sName + "  -> " + sGot);}
}

static bool BinHasBytes(byte[] aBin, byte[] aNeedle) {
if (aNeedle.Length == 0) return false;
for (int i = 0; i + aNeedle.Length <= aBin.Length; i++) {
bool bOk = true;
for (int j = 0; j < aNeedle.Length; j++) {
if (aBin[i + j] != aNeedle[j]) {bOk = false; break;}
}
if (bOk) return true;
}
return false;
}

// Napis w binarce .NET moze byc w UTF-16LE albo w ASCII, a jego offset nie
// musi byc parzysty - dlatego szukamy BAJTOWO w obu kodowaniach.
static bool BinHas(byte[] aBin, string sText) {
if (BinHasBytes(aBin, Encoding.Unicode.GetBytes(sText))) return true;
return BinHasBytes(aBin, Encoding.ASCII.GetBytes(sText));
}

static MethodInfo M(string sName) {
MethodInfo mi = tFrame.GetMethod(sName,
BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
return mi;
}

static string Export(string sText, bool bWithSentences) {
MethodInfo mi = M("BuildMarkdownFootnoteExport");
return (string) mi.Invoke(null, new object[] {sText, bWithSentences});
}

static string Sentence(string sText, int iIndex) {
MethodInfo mi = M("GetMarkdownSentenceAt");
return (string) mi.Invoke(null, new object[] {sText, iIndex});
}

// Znaczniki przypisow w tekscie - lista obiektow MarkdownFootnote; czytamy z
// nich pola Start/End/Label refleksja, bo klasa jest prywatna.
static List<int[]> Refs(string sText, out List<string> lsLabels) {
MethodInfo mi = M("GetMarkdownFootnoteRefs");
object o = mi.Invoke(null, new object[] {sText});
List<int[]> result = new List<int[]>();
lsLabels = new List<string>();
IEnumerable en = (IEnumerable) o;
foreach (object item in en) {
Type t = item.GetType();
int iStart = (int) t.GetField("Start", BindingFlags.Public | BindingFlags.Instance).GetValue(item);
int iEnd = (int) t.GetField("End", BindingFlags.Public | BindingFlags.Instance).GetValue(item);
string sLabel = (string) t.GetField("Label", BindingFlags.Public | BindingFlags.Instance).GetValue(item);
result.Add(new int[] {iStart, iEnd});
lsLabels.Add(sLabel);
}
return result;
}

public static int Main(string[] args) {
string sExe = "EdSharpNG.exe";
if (!File.Exists(sExe)) {Console.WriteLine("BRAK " + sExe); return 2;}
asm = Assembly.LoadFrom(Path.GetFullPath(sExe));
tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {
foreach (Type t in asm.GetTypes()) {if (t.Name == "MdiFrame") {tFrame = t; break;}}
}
if (tFrame == null) {Console.WriteLine("BRAK typu MdiFrame"); return 2;}
byte[] aBin = File.ReadAllBytes(sExe);

Console.WriteLine("== KONTROLA POZYTYWNA SONDY ==");
Ok(BinHas(aBin, "Footnote "), "napis istniejacy JEST w binarce", "brak");
Ok(!BinHas(aBin, "Zzyzx footnote quibble"), "napis wymyslony NIE JEST w binarce", "jest");
Ok(M("GoToMarkdownFootnoteOrBack") != null, "stara metoda skoku kontekstowego nadal jest", "brak");
Ok(M("ShowMarkdownFootnoteList") != null, "metoda listy przypisow nadal jest", "brak");

Console.WriteLine();
Console.WriteLine("== 1. METODY NOWYCH KOMEND ISTNIEJA ==");
Ok(M("GoToMarkdownFootnoteMarker") != null, "GoToMarkdownFootnoteMarker", "brak");
Ok(M("BuildMarkdownFootnoteExport") != null, "BuildMarkdownFootnoteExport", "brak");
Ok(M("ExportMarkdownFootnotes") != null, "ExportMarkdownFootnotes", "brak");
Ok(M("GetMarkdownSentenceAt") != null, "GetMarkdownSentenceAt", "brak");

Console.WriteLine();
Console.WriteLine("== 2. NAPISY KOMEND I KOMUNIKATOW W BINARCE ==");
Ok(BinHas(aBin, "Prior Footnote"), "pozycja menu Prior Footnote", "brak");
Ok(BinHas(aBin, "Control+Alt+Shift+K"), "chord Control+Alt+Shift+K", "brak");
Ok(BinHas(aBin, "Export Footnotes"), "pozycja menu Export Footnotes", "brak");
Ok(BinHas(aBin, "Footnotes with their sentences"), "opcja ze zdaniami w oknie", "brak");
Ok(BinHas(aBin, "Footnotes only"), "opcja tylko przypisy w oknie", "brak");
Ok(BinHas(aBin, "Last footnote!"), "komunikat konca dokumentu", "brak");
Ok(BinHas(aBin, "First footnote!"), "komunikat poczatku dokumentu", "brak");
Ok(BinHas(aBin, " exported"), "komunikat po eksporcie", "brak");
Ok(BinHas(aBin, "Footnotes with no marker in the text:"), "naglowek przypisow bez znacznika", "brak");

// Dokument probny: dwa przypisy w roznych zdaniach, trzeci w drugim rozdziale,
// jeden znacznik w bloku kodu (nie jest przypisem) i jedna tresc bez znacznika.
string LF = "\n";
string sDoc =
"# Rozdzial pierwszy" + LF + LF +
"Pierwsze zdanie z przypisem[^1]. Drugie zdanie bez niczego. Trzecie zdanie z przypisem[^2]." + LF + LF +
"```" + LF +
"To nie przypis[^9] bo blok kodu." + LF +
"```" + LF + LF +
"# Rozdzial drugi" + LF + LF +
"Zdanie z trzecim przypisem[^3]." + LF + LF +
"[^1]: Tresc pierwszego." + LF +
"[^2]: Tresc drugiego." + LF +
"[^3]: Tresc trzeciego." + LF +
"[^7]: Tresc bez znacznika." + LF;

Console.WriteLine();
Console.WriteLine("== 3. ZNACZNIKI: co sonda w ogole widzi (kontrola wejscia) ==");
List<string> lsLabels;
List<int[]> refs = Refs(sDoc, out lsLabels);
Ok(refs.Count == 3, "trzy znaczniki w tekscie (blok kodu pominiety)", "" + refs.Count);
Ok(lsLabels.Count == 3 && lsLabels[0] == "1" && lsLabels[1] == "2" && lsLabels[2] == "3",
"kolejnosc etykiet 1,2,3", String.Join(",", lsLabels.ToArray()));
Ok(!lsLabels.Contains("9"), "znacznik z bloku kodu NIE wchodzi", "wchodzi");

Console.WriteLine();
Console.WriteLine("== 4. EKSPORT: dwa warianty daja ROZNY tekst (kontrola dyskryminacyjna) ==");
string sOnly = Export(sDoc, false);
string sWith = Export(sDoc, true);
Ok(sOnly.Length > 0, "wariant tylko przypisy niepusty", "pusty");
Ok(sWith.Length > 0, "wariant ze zdaniami niepusty", "pusty");
Ok(sOnly != sWith, "warianty ROZNE", "identyczne");
Ok(sWith.Length > sOnly.Length, "wariant ze zdaniami dluzszy", sOnly.Length + " vs " + sWith.Length);

Ok(sOnly.Contains("1. Tresc pierwszego."), "tylko przypisy: numer i tresc", sOnly.Replace(LF, "|"));
Ok(!sOnly.Contains("Pierwsze zdanie"), "tylko przypisy: BEZ zdania", sOnly.Replace(LF, "|"));
Ok(sWith.Contains("Pierwsze zdanie z przypisem."), "ze zdaniami: zdanie obecne, znacznik usuniety", sWith.Replace(LF, "|"));
Ok(!sWith.Contains("[^1]"), "ze zdaniami: surowy znacznik NIE wystepuje", sWith.Replace(LF, "|"));
Ok(sWith.Contains("Tresc pierwszego."), "ze zdaniami: tresc przypisu obecna", "brak");

Console.WriteLine();
Console.WriteLine("== 5. EKSPORT: kolejnosc i kompletnosc ==");
int i1 = sOnly.IndexOf("Tresc pierwszego");
int i2 = sOnly.IndexOf("Tresc drugiego");
int i3 = sOnly.IndexOf("Tresc trzeciego");
Ok(i1 >= 0 && i2 > i1 && i3 > i2, "kolejnosc wedle znacznikow w tekscie", i1 + "," + i2 + "," + i3);
Ok(sOnly.Contains("Footnotes with no marker in the text:"), "naglowek sekcji sierot", "brak");
int iOrphan = sOnly.IndexOf("Tresc bez znacznika");
Ok(iOrphan > i3, "sierota PO normalnych przypisach", "" + iOrphan);
Ok(!sOnly.Contains("[^9]") && !sOnly.Contains("blok kodu"), "przypis z bloku kodu poza eksportem", "jest");

Console.WriteLine();
Console.WriteLine("== 6. ZDANIE: granice liczone jak w nawigacji po zdaniach ==");
string sSent1 = Sentence(sDoc, refs[0][0]);
string sSent2 = Sentence(sDoc, refs[1][0]);
Ok(sSent1 == "Pierwsze zdanie z przypisem.", "pierwsze zdanie dokladnie", "[" + sSent1 + "]");
Ok(sSent2 == "Trzecie zdanie z przypisem.", "trzecie zdanie dokladnie", "[" + sSent2 + "]");
Ok(sSent1 != sSent2, "rozne znaczniki daja ROZNE zdania (dyskryminacja)", "te same");
Ok(!sSent1.Contains("Drugie zdanie"), "zdanie NIE wciaga sasiada", "[" + sSent1 + "]");
// Zdanie w innym rozdziale nie moze wciagnac naglowka.
string sSent3 = Sentence(sDoc, refs[2][0]);
Ok(sSent3 == "Zdanie z trzecim przypisem.", "zdanie w drugim rozdziale bez naglowka", "[" + sSent3 + "]");

Console.WriteLine();
Console.WriteLine("== 7. DOKUMENT BEZ PRZYPISOW: eksport pusty (kontrola negatywna tresci) ==");
string sEmptyDoc = "# Tytul" + LF + LF + "Zdanie bez zadnego przypisu." + LF;
string sEmptyExport = Export(sEmptyDoc, true);
Ok(sEmptyExport.Length == 0, "eksport dokumentu bez przypisow jest PUSTY", "[" + sEmptyExport + "]");
// Ta sama sonda na dokumencie Z przypisami dala tekst - czyli nie jest gluche.
Ok(sWith.Length > 0, "kontrola: ta sama sonda widzi tekst przy przypisach", "nie widzi");

Console.WriteLine();
Console.WriteLine("== 8. STRAZNIK PISANIA nie zabiera Control+Alt+Shift+K ==");
Type tUtil = asm.GetType("EdSharp.Util");
if (tUtil == null) {foreach (Type t in asm.GetTypes()) {if (t.Name == "Util") {tUtil = t; break;}}}
MethodInfo miTyping = (tUtil == null) ? null : tUtil.GetMethod("IsTypingChord",
BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
if (miTyping == null) {
Console.WriteLine("SKIP  straznik pisania niedostepny refleksja");
}
else {
// Keys.K = 75, Control = 0x20000, Alt = 0x40000, Shift = 0x10000.
Type tKeys = typeof(System.Windows.Forms.Keys);
object oChord = Enum.ToObject(tKeys, 75 | 0x20000 | 0x40000 | 0x10000);
object oChordNoShift = Enum.ToObject(tKeys, 75 | 0x20000 | 0x40000);
bool bTyping = (bool) miTyping.Invoke(null, new object[] {oChord});
bool bTypingNoShift = (bool) miTyping.Invoke(null, new object[] {oChordNoShift});
Ok(!bTyping, "Control+Alt+Shift+K nie wpisuje znaku na tym ukladzie", "wpisuje");
Ok(!bTypingNoShift, "Control+Alt+K nie wpisuje znaku (kontrola, dziala od 5.0.48)", "wpisuje");
// KONTROLA POZYTYWNA STRAZNIKA: na ukladzie bez polskich liter straznik
// milczy dla WSZYSTKIEGO, wiec sam wynik "nie wpisuje" nie dowodzi, ze
// straznik pracuje.  Sprawdzamy, ze metoda odrzuca chord BEZ Control+Alt -
// to jej jawna, bezwarunkowa sciezka.
object oPlainK = Enum.ToObject(tKeys, 75);
bool bPlain = (bool) miTyping.Invoke(null, new object[] {oPlainK});
Ok(!bPlain, "goly K nie jest chordem pisania (sciezka bezwarunkowa)", "jest");
}

Console.WriteLine();
Console.WriteLine("== 9. WYBOR NASTEPNEGO/POPRZEDNIEGO ZNACZNIKA (jedno zrodlo prawdy) ==");
MethodInfo miFind = M("FindMarkdownFootnoteMarkerIndex");
Ok(miFind != null, "FindMarkdownFootnoteMarkerIndex istnieje", "brak");
if (miFind != null) {
MethodInfo miRefs = M("GetMarkdownFootnoteRefs");
object oRefs = miRefs.Invoke(null, new object[] {sDoc});
// Kursor na samym poczatku: w przod pierwszy znacznik, w tyl NIC.
int iF0 = (int) miFind.Invoke(null, new object[] {oRefs, 0, true});
int iB0 = (int) miFind.Invoke(null, new object[] {oRefs, 0, false});
Ok(iF0 == 0, "z poczatku w przod: pierwszy znacznik", "" + iF0);
Ok(iB0 == -1, "z poczatku w tyl: odmowa (bez zawijania)", "" + iB0);
// Kursor za drugim znacznikiem: w przod trzeci, w tyl drugi.  To wlasnie ta
// asercja odrzuca komende, ktora zawsze idzie na pierwszy przypis.
int iAfter2 = refs[1][1] + 1;
int iF2 = (int) miFind.Invoke(null, new object[] {oRefs, iAfter2, true});
int iB2 = (int) miFind.Invoke(null, new object[] {oRefs, iAfter2, false});
Ok(iF2 == 2, "ze srodka w przod: TRZECI znacznik", "" + iF2);
Ok(iB2 == 1, "ze srodka w tyl: DRUGI znacznik", "" + iB2);
Ok(iF2 != iB2, "przod i tyl z tego samego miejsca daja INNY wynik", "ten sam");
// Kursor za ostatnim znacznikiem: w przod odmowa, w tyl ostatni.
int iAfterLast = refs[2][1] + 1;
int iFEnd = (int) miFind.Invoke(null, new object[] {oRefs, iAfterLast, true});
int iBEnd = (int) miFind.Invoke(null, new object[] {oRefs, iAfterLast, false});
Ok(iFEnd == -1, "z konca w przod: odmowa (bez zawijania)", "" + iFEnd);
Ok(iBEnd == 2, "z konca w tyl: ostatni znacznik", "" + iBEnd);
// KONTROLA POZYTYWNA WYBORU: dokument bez przypisow daje -1 w obie strony.
object oNoRefs = miRefs.Invoke(null, new object[] {sEmptyDoc});
int iNone = (int) miFind.Invoke(null, new object[] {oNoRefs, 0, true});
Ok(iNone == -1, "dokument bez przypisow: brak wyboru", "" + iNone);
}

Console.WriteLine();
Console.WriteLine("== 10. STARY SLEPY ZAULEK USUNIETY ==");
// Komunikat "postaw kursor w wierszu ze znacznikiem" byl jedyna odpowiedzia
// skoku kontekstowego, gdy kursor stal poza przypisem.  Teraz komenda idzie do
// nastepnego przypisu, wiec ten napis nie ma prawa zostac w binarce.
Ok(!BinHas(aBin, "Put the cursor in a line with a footnote marker"),
"komunikat slepego zaulka NIE jest w binarce", "jest");

Console.WriteLine();
Console.WriteLine("WYNIK: " + iPass + " PASS, " + iFail + " FAIL");
return (iFail == 0) ? 0 : 1;
}

} // PomiarNawigacjiIEksportuPrzypisow class
