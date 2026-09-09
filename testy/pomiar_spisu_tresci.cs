// Pomiar markdownowego spisu tresci na ZBUDOWANYM EdSharpNG.exe (refleksja).
//
// Zlecenie 1787793592380-0. Mierzymy metody, ktore realnie poleca u usera:
//   GetMarkdownAnchorText        - kotwica naglowka (musi zgadzac sie z pandoc)
//   BuildMarkdownAnchors         - kotwice z sufiksem powtorki
//   TryGetMarkdownContentsBlock  - granice bloku spisu
//   BuildMarkdownContentsText    - tresc spisu z wcieciami
//   IsMarkdownContentsEntryLine  - rozpoznanie pozycji spisu
//   GetMarkdownPlainText         - tytul bez znacznikow Markdown
//
// KONTROLE WAZNOSCI (bez nich zielony wynik nic nie dowodzi):
//   - kotwice porownujemy z LISTA Z PANDOCA podana w argumencie (plik
//     kotwice_pandoc.txt), a nie z wlasnym wyobrazeniem,
//   - sonda MUSI znalezc pole menuMiscTableOfContents i menuNavigateGoToContents,
//   - sonda MUSI potwierdzic BRAK menuNavigateGoToPart, menuNavigateNextPart,
//     menuNavigatePriorPart (zlecenie 1787793592388-1),
//   - kontrola negatywna: pole menuMiscRepeatLine MUSI istniec (dowodzi, ze
//     odczyt pol dziala i "brak" wyzej nie jest gluchota sondy).
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

public class PomiarSpisu {

static Assembly asm;
static Type tFrame;
static int iPass = 0;
static int iFail = 0;

static void Ok(bool b, string sName, string sGot) {
if (b) {iPass++; Console.WriteLine("PASS  " + sName);}
else {iFail++; Console.WriteLine("FAIL  " + sName + "  -> " + sGot);}
}

static MethodInfo M(string sName) {
MethodInfo mi = tFrame.GetMethod(sName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
if (mi == null) throw new Exception("Brak metody " + sName);
return mi;
}

public static int Main(string[] args) {
asm = Assembly.LoadFrom(Path.GetFullPath("EdSharpNG.exe"));
tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {Console.WriteLine("Brak typu EdSharp.MdiFrame"); return 3;}

// ---------- 1. POLA MENU ----------
string[] asMusiByc = {"menuMiscTableOfContents", "menuNavigateGoToContents", "menuMiscRepeatLine",
                      "menuNavigateNextSection", "menuNavigatePriorSection", "menuMiscSectionBreak"};
foreach (string s in asMusiByc) {
FieldInfo fi = tFrame.GetField(s, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
Ok(fi != null, "pole istnieje: " + s, "null");
}
string[] asNieMoze = {"menuNavigateGoToPart", "menuNavigateNextPart", "menuNavigatePriorPart",
                      "menuMiscTextContents", "menuNavigateGoToSection"};
foreach (string s in asNieMoze) {
FieldInfo fi = tFrame.GetField(s, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
Ok(fi == null, "pole USUNIETE: " + s, "nadal jest");
}

// ---------- 2. KOTWICE wobec PANDOCA ----------
MethodInfo miAnchor = M("GetMarkdownAnchorText");
string sPandoc = args.Length > 0 ? args[0] : "kotwice_pandoc.txt";
if (File.Exists(sPandoc)) {
int iLine = 0;
foreach (string sRaw in File.ReadAllLines(sPandoc, Encoding.UTF8)) {
iLine++;
string sL = sRaw.Trim();
if (sL.Length == 0 || sL.StartsWith("#")) continue;
int iTab = sL.IndexOf('\t');
if (iTab < 0) continue;
string sTitle = sL.Substring(0, iTab);
string sWant = sL.Substring(iTab + 1).Trim();
string sGot = (string) miAnchor.Invoke(null, new object[] {sTitle});
Ok(sGot == sWant, "kotwica jak pandoc [" + sTitle + "]", "nasze=" + sGot + " pandoc=" + sWant);
}
}
else {
Console.WriteLine("FAIL  brak pliku kotwic z pandoca: " + sPandoc);
iFail++;
}

// ---------- 3. POWTORZONE TYTULY ----------
Type tHeading = asm.GetType("EdSharp.MdiFrame+MarkdownSectionHeading");
if (tHeading == null) {
foreach (Type t in tFrame.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
if (t.Name == "MarkdownSectionHeading") tHeading = t;
}
Ok(tHeading != null, "typ MarkdownSectionHeading dostepny", "null");

MethodInfo miHeadings = M("GetMarkdownSectionHeadings");
MethodInfo miBuildAnchors = M("BuildMarkdownAnchors");
MethodInfo miContentsText = M("BuildMarkdownContentsText");
MethodInfo miIsEntry = M("IsMarkdownContentsEntryLine");
MethodInfo miPlain = M("GetMarkdownPlainText");
MethodInfo miBlock = M("TryGetMarkdownContentsBlock");

string sDoc = "# Rozdzial\r\n\r\ntresc\r\n\r\n## Rozdzial\r\n\r\ntresc\r\n\r\n## Rozdzial\r\n\r\ntresc\r\n";
object oHeadings = miHeadings.Invoke(null, new object[] {sDoc});
object oAnchors = miBuildAnchors.Invoke(null, new object[] {oHeadings});
IList lAnchors = (IList) oAnchors;
Ok(lAnchors.Count == 3, "trzy naglowki maja trzy kotwice", "" + lAnchors.Count);
Ok((string) lAnchors[0] == "rozdzial" && (string) lAnchors[1] == "rozdzial-1" && (string) lAnchors[2] == "rozdzial-2",
   "powtorki dostaja sufiks jak w pandoc",
   String.Join(",", new string[] {(string) lAnchors[0], (string) lAnchors[1], (string) lAnchors[2]}));

// ---------- 4. TRESC SPISU ----------
string sContents = (string) miContentsText.Invoke(null, new object[] {oHeadings, oAnchors});
Ok(sContents.StartsWith("# Contents\n\n"), "spis zaczyna sie naglowkiem Contents", sContents.Substring(0, Math.Min(20, sContents.Length)));
Ok(sContents.Contains("- [Rozdzial](#rozdzial)\n"), "pozycja pierwszego poziomu bez wciecia", "brak");
Ok(sContents.Contains("  - [Rozdzial](#rozdzial-1)\n"), "podrozdzial ma wciecie dwoch spacji", "brak");

// plik zaczynajacy sie od h2 nie dostaje wciecia na starcie
string sDoc2 = "## Alfa\r\n\r\ntresc\r\n\r\n### Beta\r\n\r\ntresc\r\n";
object oH2 = miHeadings.Invoke(null, new object[] {sDoc2});
object oA2 = miBuildAnchors.Invoke(null, new object[] {oH2});
string sC2 = (string) miContentsText.Invoke(null, new object[] {oH2, oA2});
Ok(sC2.Contains("- [Alfa](#alfa)\n") && sC2.Contains("  - [Beta](#beta)\n"),
   "najwyzszy poziom w pliku jest poziomem zerowym wciecia", sC2);

// ---------- 5. ROZPOZNANIE POZYCJI SPISU ----------
Ok((bool) miIsEntry.Invoke(null, new object[] {"- [Alfa](#alfa)"}), "minus plus link = pozycja spisu", "nie");
Ok((bool) miIsEntry.Invoke(null, new object[] {"  * [Alfa](#alfa)"}), "gwiazdka z wcieciem = pozycja spisu", "nie");
Ok((bool) miIsEntry.Invoke(null, new object[] {"1. [Alfa](#alfa)"}), "lista numerowana = pozycja spisu", "nie");
Ok(!(bool) miIsEntry.Invoke(null, new object[] {"- [Alfa](http://x.pl)"}), "link ZEWNETRZNY to NIE pozycja spisu", "uznane za pozycje");
Ok(!(bool) miIsEntry.Invoke(null, new object[] {"- [Alfa](#alfa) i jeszcze tekst"}), "link plus tekst to NIE pozycja spisu", "uznane za pozycje");
Ok(!(bool) miIsEntry.Invoke(null, new object[] {"zwykly wiersz"}), "zwykly wiersz to NIE pozycja spisu", "uznane za pozycje");

// ---------- 6. TYTUL BEZ ZNACZNIKOW ----------
Ok((string) miPlain.Invoke(null, new object[] {"Test `kod` i **pogrubienie**"}) == "Test kod i pogrubienie", "grawisy i gwiazdki znikaja", (string) miPlain.Invoke(null, new object[] {"Test `kod` i **pogrubienie**"}));
Ok((string) miPlain.Invoke(null, new object[] {"[Link](http://x.pl) w naglowku"}) == "Link w naglowku", "z linku zostaje sam tekst", (string) miPlain.Invoke(null, new object[] {"[Link](http://x.pl) w naglowku"}));

// ---------- 7. BLOK SPISU ----------
string sZeSpisem = "# Contents\r\n\r\n- [Alfa](#alfa)\r\n  - [Beta](#beta)\r\n\r\n# Alfa\r\n\r\ntresc\r\n\r\n## Beta\r\n\r\ntresc\r\n";
object[] aArgs = new object[] {sZeSpisem, 0, 0};
bool bHave = (bool) miBlock.Invoke(null, aArgs);
int iStart = (int) aArgs[1];
int iEnd = (int) aArgs[2];
Ok(bHave, "blok spisu rozpoznany", "nie");
Ok(iStart == 0, "blok obejmuje naglowek Contents", "" + iStart);
Ok(sZeSpisem.Substring(iStart, iEnd - iStart).Contains("[Beta](#beta)"), "blok obejmuje wszystkie pozycje", sZeSpisem.Substring(iStart, iEnd - iStart));
Ok(!sZeSpisem.Substring(iStart, iEnd - iStart).Contains("# Alfa"), "blok NIE zjada pierwszego rozdzialu", sZeSpisem.Substring(iStart, iEnd - iStart));

// dokument bez spisu
object[] aArgs2 = new object[] {sDoc, 0, 0};
Ok(!(bool) miBlock.Invoke(null, aArgs2), "dokument bez spisu: brak bloku", "znaleziony");

// blok w ogrodzeniu kodu NIE jest spisem (przykladowy spis w dokumentacji)
string sWFence = "# Tytul\r\n\r\n```\r\n- [Alfa](#alfa)\r\n```\r\n\r\ntresc\r\n";
object[] aArgs3 = new object[] {sWFence, 0, 0};
Ok(!(bool) miBlock.Invoke(null, aArgs3), "pozycja w bloku kodu to NIE spis", "uznane za spis");

// naglowek o INNYM tytule nad spisem nie wchodzi do bloku
string sInny = "# Moje uwagi\r\n\r\n- [Alfa](#alfa)\r\n\r\n# Alfa\r\n\r\ntresc\r\n";
object[] aArgs4 = new object[] {sInny, 0, 0};
bool b4 = (bool) miBlock.Invoke(null, aArgs4);
Ok(b4 && !sInny.Substring((int) aArgs4[1], (int) aArgs4[2] - (int) aArgs4[1]).Contains("Moje uwagi"),
   "obcy naglowek nad spisem zostaje nietkniety", "wciagniety do bloku");

// ---------- 8. NAGLOWEK BEZ LITERY I CYFRY: pozycja BEZ linku ----------
// Zmierzone pandokiem: "## !!!" nie dostaje zadnego celu, wiec nasz link
// prowadzilby w nicosc. Pozycja spisu ma wtedy byc bez linku.
string sBezLiter = "## !!!\r\n\r\ntresc\r\n\r\n## Alfa\r\n\r\ntresc\r\n";
object oHb = miHeadings.Invoke(null, new object[] {sBezLiter});
object oAb = miBuildAnchors.Invoke(null, new object[] {oHb});
IList lAb = (IList) oAb;
Ok((string) lAb[0] == "", "naglowek z samych znakow: kotwica PUSTA", "[" + lAb[0] + "]");
Ok((string) lAb[1] == "alfa", "KONTROLA: normalny naglowek obok ma kotwice", "[" + lAb[1] + "]");
string sCb = (string) miContentsText.Invoke(null, new object[] {oHb, oAb});
Ok(sCb.Contains("- !!!\n"), "pozycja bez celu idzie BEZ linku", sCb);
Ok(!sCb.Contains("](#)"), "nie powstaje link z pusta kotwica", sCb);
Ok(sCb.Contains("- [Alfa](#alfa)\n"), "KONTROLA: pozycja z celem nadal ma link", sCb);

Console.WriteLine();
Console.WriteLine("WYNIK: " + iPass + " PASS, " + iFail + " FAIL");
return iFail == 0 ? 0 : 1;
} // Main
} // PomiarSpisu
