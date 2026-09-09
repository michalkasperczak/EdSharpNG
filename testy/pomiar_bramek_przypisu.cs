// Pomiar BRAMEK MIEJSCA KURSORA przy wstawianiu przypisu (zlecenie
// 1787832545532-3, iteracja 2) na ZBUDOWANYM EdSharpNG.exe.
//
// PO CO: pomiar_przypisu_w_bloku_kodu.py udowodnil, ze znacznik wstawiony w
// bloku kodu jest MARTWY (zero przypisow Worda, goly tekst w akapicie), a
// tresc przypisu wstawionego w tresci innego przypisu ZNIKA z docx.  Program
// mowil przy tym "Footnote N inserted".  Bramki maja to zatrzymac.
//
// Sondujemy binarke, a nie kod: napisy komunikatow MUSZA byc w pliku, a
// logike miejsca kursora liczymy przez refleksje tymi SAMYMI metodami, ktore
// wola bramka (MarkdownReview_FindFenceRanges, IsMarkdownIndexInFence,
// IsMarkdownFootnoteDefinitionLine).
//
// KONTROLE WAZNOSCI (bez nich zielony wynik nic nie dowodzi):
//   - KONTROLA POZYTYWNA: istniejacy napis "Footnote " MUSI byc w binarce,
//     a wymyslony napis MUSI nie byc - to lapie glucha sonde,
//   - KONTROLA DYSKRYMINACYJNA: kursor w ZWYKLYM zdaniu i w NAGLOWKU MUSI
//     przejsc obie bramki.  Gdyby bramka odmawiala wszedzie, "odmawia w
//     bloku kodu" nie znaczyloby nic - a przypis stalby sie bezuzyteczny.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

public class PomiarBramekPrzypisu {

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
// musi byc parzysty - dlatego szukamy BAJTOWO w obu kodowaniach, nie przez
// dekodowanie calego pliku (lekcja z 27.08: dekodowanie gubilo trafienia).
static bool BinHas(byte[] aBin, string sText) {
if (BinHasBytes(aBin, Encoding.Unicode.GetBytes(sText))) return true;
return BinHasBytes(aBin, Encoding.ASCII.GetBytes(sText));
}

static MethodInfo M(string sName) {
MethodInfo mi = tFrame.GetMethod(sName,
BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
return mi;
}

// Czy bramka przepuscilaby kursor stojacy w tym miejscu?  Powtarzamy DOKLADNIE
// dwa warunki, ktore ma kod: ogrodzenie kodu i wiersz z trescia przypisu.
static bool BramkaPrzepuszcza(string sText, int iCursor, out string sPowod) {
sPowod = "";
object oFences = M("MarkdownReview_FindFenceRanges").Invoke(null, new object[] {sText});
bool bInFence = (bool) M("IsMarkdownIndexInFence").Invoke(null, new object[] {oFences, iCursor});
if (bInFence) {sPowod = "blok kodu"; return false;}
string sLine = (string) M("GetTextLineAtIndex").Invoke(null, new object[] {sText, iCursor});
object[] aArgs = new object[] {sLine, null, null};
bool bIsDef = (bool) M("IsMarkdownFootnoteDefinitionLine").Invoke(null, aArgs);
if (bIsDef) {sPowod = "tresc przypisu " + Convert.ToString(aArgs[1]); return false;}
return true;
}

public static int Main(string[] args) {
string sExe = (args.Length > 0) ? args[0] : @"C:\EdSharp\EdSharpNG.exe";
if (!File.Exists(sExe)) {Console.WriteLine("BRAK " + sExe); return 3;}
Console.WriteLine("Binarka: " + sExe);
Console.WriteLine("Zbudowana: " + File.GetLastWriteTime(sExe).ToString("yyyy-MM-dd HH:mm:ss"));
Console.WriteLine();

byte[] aBin = File.ReadAllBytes(sExe);
asm = Assembly.LoadFrom(sExe);
tFrame = null;
foreach (Type t in asm.GetTypes()) {
if (t.Name == "Frame" || t.Name == "EdSharp") {
if (t.GetMethod("InsertMarkdownFootnote", BindingFlags.NonPublic | BindingFlags.Instance) != null) {tFrame = t; break;}
}
}
if (tFrame == null) {
foreach (Type t in asm.GetTypes()) {
if (t.GetMethod("InsertMarkdownFootnote", BindingFlags.NonPublic | BindingFlags.Instance) != null) {tFrame = t; break;}
}
}
if (tFrame == null) {Console.WriteLine("NIE ZNALAZLEM typu z InsertMarkdownFootnote"); return 3;}
Console.WriteLine("Typ: " + tFrame.FullName);
Console.WriteLine();

// ---------- KONTROLE WAZNOSCI SONDY ----------
Ok(BinHas(aBin, "Footnote "), "KONTROLA POZYTYWNA: istniejacy napis JEST w binarce", "brak");
Ok(!BinHas(aBin, "Cannot put a footnote inside a bathtub!"),
"KONTROLA NEGATYWNA: wymyslony napis NIE jest w binarce", "sonda glucha");

// ---------- NOWE KOMUNIKATY ODMOWY ----------
Ok(BinHas(aBin, "Cannot put a footnote inside a code block!"),
"komunikat odmowy dla bloku kodu jest w binarce", "brak");
Ok(BinHas(aBin, "Cannot put a footnote inside the text of footnote "),
"komunikat odmowy dla przypisu w przypisie jest w binarce", "brak");

// ---------- BRAMKA: BLOK KODU ----------
string sDoc = "# Tytul\n\nZdanie zwykle.\n\n```\nprzyklad kodu\n```\n\n## Rozdzial\n\n[^1]: Tresc przypisu.\n";
int iKod = sDoc.IndexOf("przyklad kodu") + 4;
string sPowod;
Ok(!BramkaPrzepuszcza(sDoc, iKod, out sPowod),
"kursor W BLOKU KODU jest ODRZUCANY", "przepuszczony");
Ok(sPowod == "blok kodu", "powod odmowy to wlasnie blok kodu", sPowod);

// ---------- BRAMKA: TRESC INNEGO PRZYPISU ----------
int iDef = sDoc.IndexOf("Tresc przypisu.") + 3;
Ok(!BramkaPrzepuszcza(sDoc, iDef, out sPowod),
"kursor W TRESCI INNEGO PRZYPISU jest ODRZUCANY", "przepuszczony");
Ok(sPowod.StartsWith("tresc przypisu 1"), "odmowa podaje numer tamtego przypisu", sPowod);

// ---------- KONTROLA DYSKRYMINACYJNA: ZWYKLE ZDANIE MUSI PRZEJSC ----------
int iZdanie = sDoc.IndexOf("Zdanie zwykle.") + 6;
Ok(BramkaPrzepuszcza(sDoc, iZdanie, out sPowod),
"KONTROLA: kursor w ZWYKLYM zdaniu PRZECHODZI", "odrzucony: " + sPowod);

// ---------- KONTROLA DYSKRYMINACYJNA: NAGLOWEK MUSI PRZEJSC ----------
// To nie jest kaprys: po skoku spisem tresci (Shift+F6) kursor stoi wlasnie
// w naglowku, a pomiar eksportu pokazal, ze przypis w naglowku ZYJE.
int iNaglowek = sDoc.IndexOf("## Rozdzial") + 5;
Ok(BramkaPrzepuszcza(sDoc, iNaglowek, out sPowod),
"KONTROLA: kursor w NAGLOWKU PRZECHODZI (tam przypis zyje)", "odrzucony: " + sPowod);

// ---------- KONIEC WIERSZA Z TRESCIA PRZYPISU ----------
// Najczestsze miejsce kursora to koniec wiersza - i wlasnie tam helper
// wiersza mial defekt naprawiony w iteracji 1.  Bramka MUSI dzialac tez tu.
int iKoniecDef = sDoc.IndexOf("Tresc przypisu.") + "Tresc przypisu.".Length;
Ok(!BramkaPrzepuszcza(sDoc, iKoniecDef, out sPowod),
"kursor na KONCU wiersza z trescia przypisu tez ODRZUCANY", "przepuszczony");

// ---------- ZERO REGRESJI: ISTNIEJACE KOMENDY NADAL SA ----------
Ok(tFrame.GetField("menuMiscInsertFootnote", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null,
"komenda wstawiania przypisu nadal istnieje", "brak");
Ok(tFrame.GetField("menuMiscTableOfContents", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null,
"komenda spisu tresci nadal istnieje (zero regresji)", "brak");

Console.WriteLine();
Console.WriteLine("PASS: " + iPass + "   FAIL: " + iFail);
if (iFail == 0) Console.WriteLine("WYNIK: BRAMKI MIEJSCA KURSORA DZIALAJA, ZWYKLE ZDANIE I NAGLOWEK PRZECHODZA");
return (iFail == 0) ? 0 : 1;
}

}
