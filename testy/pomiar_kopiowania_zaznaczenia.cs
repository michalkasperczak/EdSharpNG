// Pomiar KOPIOWANIA ZAZNACZENIA Z FORMATOWANIEM (Control+Shift+C na wielu
// wierszach), przez metody ZBUDOWANEJ binarki EdSharpNG.exe (refleksja).
//
// PO CO REFLEKSJA, A NIE GREP PO KODZIE: pytanie brzmi "co naprawde znajdzie
// sie w schowku", a nie "czy w kodzie jest metoda".  Generator RTF wolamy
// wprost i sprawdzamy WYNIK - obecnosc sterownika \b tam, gdzie ma byc
// pogrubienie, i BRAK gwiazdek, ktore mialy zniknac.
//
// KONTROLA NEGATYWNA: ten sam plik uruchomiony na binarce 5.0.56 musi
// zakonczyc sie komunikatem "BRAK metody BuildRtfFromMarkdownLines", czyli nie
// moglby byc zielony przed zmiana.  Bez tego zielony wynik nie odroznia
// naprawy od gluchej sondy.
//
// PULAPKA, KTORA TU PILNUJEMY (lekcja z 01.09.2026): Assembly.LoadFrom oddaje
// TE SAMA assembly dla dwoch plikow o tej samej tozsamosci, wiec uzywamy
// Assembly.LoadFile ze sciezka absolutna.
//
// Uzycie: pomiar_kopiowania_zaznaczenia.exe <sciezka_do_EdSharpNG.exe>
using System;
using System.IO;
using System.Reflection;
using System.Text;

public class PomiarKopiowaniaZaznaczenia {

static int iPass = 0;
static int iFail = 0;
static Type tFrame;

static void Ok(bool b, string sName, string sGot) {
if (b) {iPass++; Console.WriteLine("PASS  " + sName);}
else {iFail++; Console.WriteLine("FAIL  " + sName + "  -> " + sGot);}
}

static MethodInfo M(Type t, string sName) {
return t.GetMethod(sName,
BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
}

static string Rtf(string[] aLines) {
return (string) M(tFrame, "BuildRtfFromMarkdownLines").Invoke(null, new object[] {aLines});
}

static string Inline(string sLine) {
return (string) M(tFrame, "RtfEncodeMarkdownInline").Invoke(null, new object[] {sLine});
}

static bool HasMarkup(string[] aLines) {
return (bool) M(tFrame, "SelectionHasMarkdownMarkup").Invoke(null, new object[] {aLines});
}

public static int Main(string[] args) {
if (args.Length < 1) {Console.WriteLine("podaj sciezke do EdSharpNG.exe"); return 2;}
string sExe = Path.GetFullPath(args[0]);
Assembly asm = Assembly.LoadFile(sExe);
Console.WriteLine("mierzona binarka: " + asm.Location);
Console.WriteLine("rozmiar: " + new FileInfo(sExe).Length + " B");
Console.WriteLine();

tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {Console.WriteLine("BRAK typu EdSharp.MdiFrame"); return 2;}

foreach (string sNeeded in new string[] {"BuildRtfFromMarkdownLines", "RtfEncodeMarkdownInline", "RtfEncodeMarkdownSpan", "SelectionHasMarkdownMarkup", "TryCopyMarkdownSelection", "SplitTextLines"}) {
if (M(tFrame, sNeeded) == null) {
Console.WriteLine("BRAK metody " + sNeeded + " - ta binarka nie ma kopiowania zaznaczenia");
return 3;
}
}

// ---- 1. POGRUBIENIE I POCHYLENIE: gwiazdki znikaja, sterowniki wchodza ----
string s1 = Inline("zwykly **grubo** i *skosnie* koniec");
Ok(s1.IndexOf("\\b ") >= 0, "pogrubienie daje sterownik \\b", s1);
Ok(s1.IndexOf("\\b0 ") >= 0, "pogrubienie jest ZAMYKANE (\\b0)", s1);
Ok(s1.IndexOf("\\i ") >= 0, "pochylenie daje sterownik \\i", s1);
Ok(s1.IndexOf("\\i0 ") >= 0, "pochylenie jest ZAMYKANE (\\i0)", s1);
Ok(s1.IndexOf('*') < 0, "GWIAZDKI ZNIKLY z wyniku", s1);
Ok(s1.IndexOf("grubo") >= 0 && s1.IndexOf("skosnie") >= 0, "tresc obu wyroznien zostala", s1);
Ok(s1.IndexOf("zwykly") >= 0 && s1.IndexOf("koniec") >= 0, "tekst wokol wyroznien zostal", s1);

// KONTROLA POZYTYWNA SONDY: bez znacznikow NIE MA sterownika pogrubienia.
// Bez tej asercji sonda szukajaca "\\b" byla by zielona nawet wtedy, gdyby
// generator wstawial pogrubienie wszedzie.
string s1b = Inline("caly wiersz bez zadnych znacznikow");
Ok(s1b.IndexOf("\\b ") < 0 && s1b.IndexOf("\\i ") < 0, "KONTROLA: czysty tekst NIE dostaje wyroznien", s1b);

// ---- 2. PODKRESLNIKI ROWNORZEDNE Z GWIAZDKAMI ----
string s2 = Inline("__grubo__ oraz _skos_");
Ok(s2.IndexOf("\\b ") >= 0 && s2.IndexOf("\\i ") >= 0, "podkreslniki dzialaja jak gwiazdki", s2);
Ok(s2.IndexOf('_') < 0, "podkreslniki znikly z wyniku", s2);

// ---- 3. PRZEKRESLENIE I KOD ----
string s3 = Inline("~~skreslone~~ i `kod`");
Ok(s3.IndexOf("\\strike") >= 0, "przekreslenie daje \\strike", s3);
Ok(s3.IndexOf("kod") >= 0, "tresc kodu zostala", s3);
Ok(s3.IndexOf('`') < 0, "grawisy znikly z wyniku", s3);

// ---- 4. GWIAZDKA NIEDOMKNIETA ZOSTAJE TRESCIA ----
// W edytorze tekstu gwiazdka bywa po prostu gwiazdka.  Wazne jest, zeby
// niedomkniety znacznik nie wyciekl na dalsza czesc dokumentu.
string s4 = Inline("mnozenie 2 * 3 i tyle");
Ok(s4.IndexOf("mnozenie") >= 0, "wiersz z pojedyncza gwiazdka nie ginie", s4);
Ok(s4.EndsWith("\\i0 "), "niedomkniety znacznik jest ZAMKNIETY na koncu", s4);

// ---- 5. ODSYLACZ STAJE SIE POLEM HYPERLINK ----
string s5 = Inline("wejdz na [moja strone](https://example.com/a) dzisiaj");
Ok(s5.IndexOf("HYPERLINK") >= 0, "odsylacz daje pole HYPERLINK Worda", s5);
Ok(s5.IndexOf("https://example.com/a") >= 0, "adres jest w polu", s5);
Ok(s5.IndexOf("moja strone") >= 0, "tytul odsylacza jest widoczna trescia", s5);
Ok(s5.IndexOf('[') < 0 && s5.IndexOf(']') < 0, "nawiasy skladni odsylacza znikly", s5);
Ok(s5.IndexOf("dzisiaj") >= 0, "tekst PO odsylaczu nie zostal zgubiony", s5);

// ---- 6. NAGLOWEK ----
string[] a6 = new string[] {"# Tytul dokumentu", "tresc akapitu"};
string r6 = Rtf(a6);
Ok(r6.IndexOf("\\fs36") >= 0, "naglowek poziomu 1 dostaje duzy stopien pisma", r6);
Ok(r6.IndexOf("\\b\\fs") >= 0, "naglowek jest pogrubiony", r6);
Ok(r6.IndexOf("Tytul dokumentu") >= 0, "tresc naglowka zostala", r6);
Ok(r6.IndexOf('#') < 0, "KRATKI znikly z wyniku", r6);
string[] a6b = new string[] {"### Trzeci poziom", "x"};
string r6b = Rtf(a6b);
Ok(r6b.IndexOf("\\fs28") >= 0, "naglowek poziomu 3 ma INNY stopien niz poziom 1", r6b);
Ok(r6b.IndexOf("\\fs36") < 0, "KONTROLA: poziom 3 nie udaje poziomu 1", r6b);

// ---- 7. LISTA PUNKTOWANA JAKO PRAWDZIWA LISTA ----
string[] a7 = new string[] {"- pierwszy", "- drugi", "- trzeci"};
string r7 = Rtf(a7);
Ok(r7.IndexOf("\\*\\listtable") >= 0, "RTF ma tablice list", r7);
Ok(r7.IndexOf("\\ls1") >= 0, "pozycje wiaza sie z lista punktowana", r7);
Ok(r7.IndexOf("\\s1") >= 0, "pozycje maja styl List Paragraph", r7);
Ok(r7.IndexOf("pierwszy") >= 0 && r7.IndexOf("trzeci") >= 0, "wszystkie pozycje sa w wyniku", r7);
int iMyslniki = 0;
foreach (char c in r7) if (c == '-') iMyslniki++;
// W samym naglowku RTF sa liczby ujemne (\fi-360), wiec myslniki tam BYC MUSZA.
// Liczymy tylko to, ze zaden nie zostal jako PUNKTOR tekstu.
Ok(r7.IndexOf("- pierwszy") < 0, "myslnik punktora NIE zostal jako tekst", r7);

// ---- 8. LISTA NUMEROWANA: NUMERACJA LICZONA OD NOWA ----
string[] a8 = new string[] {"1. jeden", "2. dwa", "3. trzy"};
string r8 = Rtf(a8);
Ok(r8.IndexOf("\\ls2") >= 0, "lista numerowana ma WLASNA definicje listy", r8);
Ok(r8.IndexOf("{\\listtext\\f0 1.") >= 0, "pierwsza pozycja ma numer 1", r8);
Ok(r8.IndexOf("{\\listtext\\f0 3.") >= 0, "trzecia pozycja ma numer 3", r8);
// KONTROLA ROZNICUJACA: lista punktowana i numerowana MUSZA dac rozny wynik.
Ok(r7 != r8, "KONTROLA: punktowana i numerowana to NIE ten sam RTF", "identyczne");
// Punktor sprawdzamy w TRESCI POZYCJI, nie w calym dokumencie: tablica
// definicji list w naglowku deklaruje OBA rodzaje zawsze, wiec pytanie o
// \'b7 w calosci dawalo FALSZYWY ALARM (zlapane przy pierwszym przebiegu).
Ok(r8.IndexOf("{\\listtext\\f1 \\'b7") < 0, "lista numerowana nie ma punktora w pozycjach", r8);
Ok(r7.IndexOf("{\\listtext\\f1 \\'b7") >= 0, "KONTROLA: lista punktowana punktor w pozycjach MA", r7);

// Numeracja wlasna, nie przepisana: pozycje 5. i 9. w zrodle staja sie 1. i 2.
string[] a8b = new string[] {"5. piaty", "9. dziewiaty"};
string r8b = Rtf(a8b);
Ok(r8b.IndexOf("{\\listtext\\f0 1.") >= 0 && r8b.IndexOf("{\\listtext\\f0 2.") >= 0,
"numeracja liczona od nowa, nie przepisana ze zrodla", r8b);

// ---- 9. BLOK KODU JEST DOSLOWNY ----
// Gwiazdka w programie nie jest pogrubieniem.
string[] a9 = new string[] {"```", "int a = b * c * d;", "```"};
string r9 = Rtf(a9);
Ok(r9.IndexOf("\\b ") < 0 && r9.IndexOf("\\i ") < 0, "blok kodu NIE dostaje wyroznien", r9);
Ok(r9.IndexOf("b * c * d") >= 0, "gwiazdki w kodzie ZOSTAJA doslownie", r9);
// KONTROLA POZYTYWNA: ten sam wiersz POZA blokiem kodu wyroznienie dostaje.
string[] a9b = new string[] {"int a = b * c * d;", "drugi wiersz"};
string r9b = Rtf(a9b);
Ok(r9b.IndexOf("\\i ") >= 0, "KONTROLA: ten sam wiersz poza kodem JEST przetwarzany", r9b);

// ---- 10. BRAMKA: BEZ ZNACZNIKOW NIE PRZEJMUJEMY KOPIOWANIA ----
Ok(!HasMarkup(new string[] {"zwykly wiersz", "drugi zwykly wiersz"}),
"czysty tekst NIE trafia do naszej sciezki", "przejal");
Ok(HasMarkup(new string[] {"zwykly", "- pozycja listy"}),
"jedna pozycja listy WYSTARCZA, zeby przejac", "nie przejal");
Ok(HasMarkup(new string[] {"## naglowek", "tekst"}),
"naglowek wystarcza, zeby przejac", "nie przejal");
Ok(HasMarkup(new string[] {"tekst **grubo**", "tekst"}),
"pogrubienie wystarcza, zeby przejac", "nie przejal");

// ---- 11. POLSKIE ZNAKI ----
// RtfEncodeInline ma je zamieniac na sekwencje \\uN, bo RTF w cp1250 sam ich
// nie niesie.  To ta sama rodzina pulapki, ktora zjadala ulubione (17.08.2026).
string s11 = Inline("**zażółć** gęślą jaźń");
Ok(s11.IndexOf("\\u") >= 0, "polskie litery ida jako sekwencje \\uN", s11);
Ok(s11.IndexOf("\\b ") >= 0, "pogrubienie na polskim slowie dziala", s11);

// ---- 12. RTF JEST DOMKNIETY ----
string[] a12 = new string[] {"# Tytul", "", "- raz", "- dwa", "", "Akapit z **grubym**."};
string r12 = Rtf(a12);
Ok(r12.StartsWith("{\\rtf1"), "wynik zaczyna sie naglowkiem RTF", r12.Substring(0, Math.Min(20, r12.Length)));
Ok(r12.EndsWith("}"), "wynik jest domkniety klamra", r12.Substring(Math.Max(0, r12.Length - 20)));
int iOpen = 0, iClose = 0;
for (int i = 0; i < r12.Length; i++) {
if (r12[i] == '\\') {i++; continue;}
if (r12[i] == '{') iOpen++;
if (r12[i] == '}') iClose++;
}
Ok(iOpen == iClose, "klamry RTF sa zbalansowane", "otwarte " + iOpen + " zamkniete " + iClose);
Ok(r12.IndexOf("Tytul") >= 0 && r12.IndexOf("raz") >= 0 && r12.IndexOf("Akapit") >= 0,
"mieszany blok zachowal tresc wszystkich rodzajow wierszy", r12);

// ---- 13. PUSTY I ZDEGENEROWANY WEJSCIE NIE WYWALA ----
Ok(Rtf(new string[0]).Length == 0, "puste wejscie daje pusty wynik", "niepusty");
Ok(Inline("").Length == 0, "pusty wiersz daje pusty wynik", "niepusty");
string[] a13 = new string[] {null, "- pozycja"};
Ok(Rtf(a13).IndexOf("pozycja") >= 0, "wiersz null nie wywala generatora", "wywalil");

Console.WriteLine();
Console.WriteLine("WYNIK: " + iPass + " PASS / " + iFail + " FAIL");
return iFail == 0 ? 0 : 1;
}
}
