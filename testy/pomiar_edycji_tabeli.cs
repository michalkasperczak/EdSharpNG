// Pomiar edycji GOTOWEJ tabeli - przez REFLEKSJE na zbudowanym EdSharpNG.exe.
// Mierzy prawdziwe metody z binarki, nie kopie logiki.
using System;
using System.Collections.Generic;
using System.Reflection;

public class T {
static int iPass = 0, iFail = 0;

static void Ok(bool b, string sName, string sGot) {
if (b) {iPass++; Console.WriteLine("PASS " + sName);}
else {iFail++; Console.WriteLine("FAIL " + sName + "  -> " + sGot);}
}

static Assembly asm;
static Type tFrame;
static object oFrame;

static object Call(string sMethod, params object[] args) {
MethodInfo mi = null;
foreach (MethodInfo m in tFrame.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
if (m.Name != sMethod) continue;
if (m.GetParameters().Length != args.Length) continue;
mi = m; break;
}
if (mi == null) throw new Exception("BRAK metody " + sMethod + "/" + args.Length);
return mi.Invoke(mi.IsStatic ? null : oFrame, args);
}

static MethodInfo Find(string sMethod, int iArgs) {
foreach (MethodInfo m in tFrame.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
if (m.Name == sMethod && m.GetParameters().Length == iArgs) return m;
}
return null;
}

// FindMarkdownTableAtCursor(sText, iCursor, fences, out start, out end, out rows, out aligns)
static bool Znajdz(string sText, int iCursor, out int iStart, out int iEnd, out List<string> rows, out List<string> aligns) {
MethodInfo miFences = null;
foreach (MethodInfo m in tFrame.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
if (m.Name == "MarkdownReview_FindFenceRanges") {miFences = m; break;}
}
object oFences = miFences.Invoke(null, new object[] {sText});
MethodInfo mi = Find("FindMarkdownTableAtCursor", 7);
if (mi == null) throw new Exception("BRAK FindMarkdownTableAtCursor");
object[] a = new object[] {sText, iCursor, oFences, 0, 0, null, null};
bool b = (bool) mi.Invoke(oFrame, a);
iStart = (int) a[3];
iEnd = (int) a[4];
rows = (List<string>) a[5];
aligns = (List<string>) a[6];
return b;
}

static List<string> Komorki(string sRow) {
MethodInfo mi = Find("ParseMarkdownTableRowCells", 1);
return (List<string>) mi.Invoke(null, new object[] {sRow});
}

static List<string> Wyrownania(string sSep) {
MethodInfo mi = Find("ParseMarkdownTableAlignments", 1);
return (List<string>) mi.Invoke(null, new object[] {sSep});
}

static string Kreski(List<string> aligns, int iCol) {
MethodInfo mi = Find("MarkdownTableDashCell", 2);
return (string) mi.Invoke(null, new object[] {aligns, iCol});
}

static string Ucieczka(string sValue) {
MethodInfo mi = Find("EscapeMarkdownTableCell", 1);
return (string) mi.Invoke(oFrame, new object[] {sValue});
}

public static int Main(string[] args) {
asm = Assembly.LoadFrom(args[0]);
tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {Console.WriteLine("BRAK typu EdSharp.MdiFrame"); return 3;}
// Nie tworzymy okna - metody sa instancyjne, ale nie ruszaja stanu formy.
oFrame = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(tFrame);

// ---- KONTROLE POZYTYWNE SONDY: metody, ktore MUSZA byc w binarce ----
Ok(Find("RunMarkdownTableWizard", 1) != null, "sonda: RunMarkdownTableWizard istnieje", "brak");
Ok(Find("FindMarkdownTableAtCursor", 7) != null, "sonda: FindMarkdownTableAtCursor istnieje", "brak");
Ok(Find("FillGridFromMarkdownTable", 2) != null, "sonda: FillGridFromMarkdownTable istnieje", "brak");
Ok(Find("BuildMarkdownTableFromGrid", 2) != null, "sonda: BuildMarkdownTableFromGrid z wyrownaniami", "brak");
Ok(Find("EscapeMarkdownTableCell", 1) != null, "sonda kontrolna: stara ucieczka komorki nadal jest", "brak");

string LF = "\n";
string sTab = "| Imie | Wiek |" + LF + "| --- | --- |" + LF + "| Ala | 30 |" + LF + "| Ola | 41 |" + LF;
string sDok = "Tekst nad tabela." + LF + LF + sTab + LF + "Tekst pod tabela." + LF;
int iTabAt = sDok.IndexOf("| Imie");

int iS, iE; List<string> rows, al;

// 1. Kursor w wierszu naglowka tabeli -> rozpoznana
bool b = Znajdz(sDok, iTabAt + 3, out iS, out iE, out rows, out al);
Ok(b, "1. kursor w naglowku tabeli: rozpoznana", "false");
Ok(iS == iTabAt, "2. poczatek zakresu = pierwsza linia tabeli", iS + " vs " + iTabAt);
Ok(rows != null && rows.Count == 3, "3. trzy wiersze tresci (bez wiersza kreskek)", rows == null ? "null" : rows.Count.ToString());
Ok(rows != null && rows[0].Contains("Imie") && rows[2].Contains("Ola"), "4. wiersze w kolejnosci z pliku", rows == null ? "null" : rows[0]);
Ok(sDok.Substring(iS, iE - iS).EndsWith("| Ola | 41 |"), "5. koniec zakresu na ostatnim wierszu tabeli, bez konca linii", "\"" + sDok.Substring(Math.Max(0, iE - 14), Math.Min(14, iE)) + "\"");

// 6. Kursor w OSTATNIM wierszu tabeli - ten sam zakres
int iOstatni = sDok.IndexOf("| Ola");
int iS2, iE2; List<string> r2, a2;
bool b2 = Znajdz(sDok, iOstatni + 2, out iS2, out iE2, out r2, out a2);
Ok(b2 && iS2 == iS && iE2 == iE, "6. kursor w ostatnim wierszu daje ten sam zakres", b2 + " " + iS2 + "/" + iE2);

// 7. Kursor w wierszu KRESKEK tez ma otworzyc tabele
int iKreski = sDok.IndexOf("| --- |");
bool b7 = Znajdz(sDok, iKreski + 1, out iS2, out iE2, out r2, out a2);
Ok(b7 && iS2 == iS, "7. kursor w wierszu kreskek: tabela rozpoznana", b7.ToString());

// ---- KONTROLE NEGATYWNE ROZPOZNANIA ----
// 8. Kursor w tekscie NAD tabela - nie tabela (wstawiamy nowa)
Ok(!Znajdz(sDok, 3, out iS2, out iE2, out r2, out a2), "8. kursor w akapicie: NIE tabela", "true");
// 9. Kursor w tekscie POD tabela
int iPod = sDok.IndexOf("Tekst pod");
Ok(!Znajdz(sDok, iPod + 3, out iS2, out iE2, out r2, out a2), "9. kursor pod tabela: NIE tabela", "true");
// 10. Tekst z kreskami pionowymi, ale BEZ wiersza kreskek - nie tabela
string sNie = "Kolumna a | kolumna b" + LF + "wartosc 1 | wartosc 2" + LF;
Ok(!Znajdz(sNie, 4, out iS2, out iE2, out r2, out a2), "10. kreski bez wiersza kreskek: NIE tabela", "true");
// 11. Tabela w BLOKU KODU - nie tykamy
string sFence = "```" + LF + "| a | b |" + LF + "| --- | --- |" + LF + "| 1 | 2 |" + LF + "```" + LF;
Ok(!Znajdz(sFence, sFence.IndexOf("| a") + 2, out iS2, out iE2, out r2, out a2), "11. tabela w bloku kodu: NIE ruszana", "true");
// 12. Pusty dokument
Ok(!Znajdz("", 0, out iS2, out iE2, out r2, out a2), "12. pusty dokument nie wywala metody", "true");

// ---- PARSOWANIE KOMOREK: odwrotnosc ucieczki ----
List<string> c = Komorki("| Ala | 30 |");
Ok(c.Count == 2 && c[0] == "Ala" && c[1] == "30", "13. dwie komorki, obcieta spacja", c.Count + ":" + String.Join(",", c.ToArray()));
c = Komorki("| a \\| b | c |");
Ok(c.Count == 2 && c[0] == "a | b", "14. kreska zaslonieta ukosnikiem zostaje TRESCIA komorki", c.Count + ":" + String.Join("~", c.ToArray()));
c = Komorki("| | x |");
Ok(c.Count == 2 && c[0] == "" && c[1] == "x", "15. pusta komorka nie znika", c.Count + ":" + String.Join("~", c.ToArray()));
c = Komorki("| a | b");
Ok(c.Count == 2 && c[1] == "b", "16. wiersz bez konczacej kreski tez sie parsuje", c.Count + ":" + String.Join("~", c.ToArray()));

// 17. ROUND-TRIP: ucieczka i parsowanie sa odwrotne
string sTresc = "cena | netto";
string sEsc = Ucieczka(sTresc);
c = Komorki("| " + sEsc + " | x |");
Ok(c.Count == 2 && c[0] == sTresc, "17. round-trip tresci z kreska: wraca bez zmian", "\"" + c[0] + "\" (esc=\"" + sEsc + "\")");

// ---- WYROWNANIA KOLUMN ----
al = Wyrownania("| :--- | ---: | :---: | --- |");
Ok(al.Count == 4 && al[0] == "left" && al[1] == "right" && al[2] == "center" && al[3] == "", "18. cztery wyrownania rozpoznane", String.Join(",", al.ToArray()));
Ok(Kreski(al, 1) == " ---: |", "19. wyrownanie w prawo odtworzone w wierszu kreskek", Kreski(al, 1));
Ok(Kreski(al, 3) == " --- |", "20. brak wyrownania zostaje bez dwukropka", Kreski(al, 3));
Ok(Kreski(null, 0) == " --- |", "21. brak listy wyrownan: zwykle kreski", Kreski(null, 0));

// 22. Wyrownania z tabeli w dokumencie doczytane
string sTab2 = "| A | B |" + LF + "| :--- | ---: |" + LF + "| 1 | 2 |" + LF;
Znajdz(sTab2, 2, out iS2, out iE2, out r2, out a2);
Ok(a2 != null && a2.Count == 2 && a2[1] == "right", "22. wyrownania z pliku doczytane przy edycji", a2 == null ? "null" : String.Join(",", a2.ToArray()));

// 23. Tabela na SAMYM POCZATKU pliku
string sTab3 = "| A |" + LF + "| --- |" + LF + "| 1 |" + LF;
Ok(Znajdz(sTab3, 0, out iS2, out iE2, out r2, out a2) && iS2 == 0, "23. tabela na poczatku pliku rozpoznana", "nie");

// 24. Tabela bez konca linii na koncu pliku
string sTab4 = "| A |" + LF + "| --- |" + LF + "| 1 |";
bool b24 = Znajdz(sTab4, sTab4.Length, out iS2, out iE2, out r2, out a2);
Ok(b24 && iE2 == sTab4.Length, "24. ostatni wiersz bez konca linii: zakres do konca pliku", b24 + " " + iE2 + "/" + sTab4.Length);

// 25. Wiersze CRLF (nasze pliki maja CRLF na dysku, kontrolka daje LF - obie drogi musza dzialac)
string sTabCrlf = "| A | B |\r\n| --- | --- |\r\n| 1 | 2 |\r\n";
bool b25 = Znajdz(sTabCrlf, 2, out iS2, out iE2, out r2, out a2);
Ok(b25 && r2.Count == 2 && !r2[0].Contains("\r"), "25. tabela z CRLF: wiersze bez znaku CR", b25 + " " + (r2 == null ? "null" : r2.Count + " \"" + r2[0] + "\""));

// 26. DWIE tabele rozdzielone pusta linia - kursor w drugiej bierze DRUGA
string sDwie = "| A |" + LF + "| --- |" + LF + "| 1 |" + LF + LF + "| B |" + LF + "| --- |" + LF + "| 2 |" + LF;
int iDruga = sDwie.IndexOf("| B |");
bool b26 = Znajdz(sDwie, iDruga + 2, out iS2, out iE2, out r2, out a2);
Ok(b26 && iS2 == iDruga, "26. druga tabela nie zlewa sie z pierwsza", b26 + " start=" + iS2 + " oczekiwany=" + iDruga);

Console.WriteLine();
Console.WriteLine("WYNIK: " + iPass + "/" + (iPass + iFail));
return iFail == 0 ? 0 : 1;
}
}
