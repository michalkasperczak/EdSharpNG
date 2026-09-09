// Pomiar parsera wyroznien i list Markdown WPROST NA ZBUDOWANEJ BINARCE.
// Refleksja, bo GetMarkdownEmphases i GetMarkdownListStarts sa prywatne.
// Zielony build nie dowodzi, ze parser trafia w tresc: dlatego kazdy przypadek
// ma tu KONTROLE NEGATYWNA - tekst, ktory NIE MOZE zostac wyrozniemiem
// (gwiazdka punktora listy, mnozenie w zdaniu, blok kodu, nazwa_z_podkresleniem).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class H {
static int ok = 0, bad = 0;
static Type tFrame;

static void A(bool b, string opis) {
if (b) {ok++; Console.WriteLine("  PASS " + opis);}
else {bad++; Console.WriteLine("  FAIL " + opis);}
}

static IList Emf(string s) {
MethodInfo mi = tFrame.GetMethod("GetMarkdownEmphases", BindingFlags.NonPublic | BindingFlags.Static);
if (mi == null) throw new Exception("brak GetMarkdownEmphases");
return (IList) mi.Invoke(null, new object[] {s});
}

static IList<int> Listy(string s) {
MethodInfo mi = tFrame.GetMethod("GetMarkdownListStarts", BindingFlags.NonPublic | BindingFlags.Static);
if (mi == null) throw new Exception("brak GetMarkdownListStarts");
return (IList<int>) mi.Invoke(null, new object[] {s});
}

static int Poz(string s) {
MethodInfo mi = tFrame.GetMethod("CountMarkdownListItemsFrom", BindingFlags.NonPublic | BindingFlags.Static);
if (mi == null) throw new Exception("brak CountMarkdownListItemsFrom");
return (int) mi.Invoke(null, new object[] {s, 0});
}

static string Tresc(object note, string sText) {
Type t = note.GetType();
int ts = (int) t.GetField("TextStart").GetValue(note);
int te = (int) t.GetField("TextEnd").GetValue(note);
return sText.Substring(ts, te - ts);
}

static int Poziom(object note) {
return (int) note.GetType().GetField("Level").GetValue(note);
}

public static void Main(string[] args) {
Assembly asm = Assembly.LoadFrom(System.IO.Path.GetFullPath(args.Length > 0 ? args[0] : "EdSharpNG.exe"));
tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {Console.WriteLine("BLAD: brak typu MdiFrame"); Environment.Exit(2);}

Console.WriteLine("=== WYROZNIENIA: rodzaj po LICZBIE znacznikow ===");
string s1 = "Zwykly **pogrubiony** i *pochylony* oraz ***oba*** koniec.";
IList e1 = Emf(s1);
A(e1.Count == 3, "trzy wyroznienia w zdaniu, jest " + e1.Count);
if (e1.Count == 3) {
A(Tresc(e1[0], s1) == "pogrubiony", "pierwsze to tresc 'pogrubiony', jest '" + Tresc(e1[0], s1) + "'");
A(Poziom(e1[0]) == 2, "pierwsze ma poziom 2 (bold), jest " + Poziom(e1[0]));
A(Tresc(e1[1], s1) == "pochylony", "drugie to 'pochylony', jest '" + Tresc(e1[1], s1) + "'");
A(Poziom(e1[1]) == 1, "drugie ma poziom 1 (italic), jest " + Poziom(e1[1]));
A(Tresc(e1[2], s1) == "oba", "trzecie to 'oba', jest '" + Tresc(e1[2], s1) + "'");
A(Poziom(e1[2]) == 3, "trzecie ma poziom 3 (bold italic), jest " + Poziom(e1[2]));
}

Console.WriteLine("=== KURSOR NA TRESCI, NIE NA GWIAZDCE (jego wymog z 15:41) ===");
string s2 = "aa **bb** cc";
IList e2 = Emf(s2);
if (e2.Count == 1) {
int ts = (int) e2[0].GetType().GetField("TextStart").GetValue(e2[0]);
int st = (int) e2[0].GetType().GetField("Start").GetValue(e2[0]);
A(s2[ts] == 'b', "TextStart wskazuje litere tresci, wskazuje '" + s2[ts] + "'");
A(s2[st] == '*', "Start wskazuje gwiazdke (czyli oba pola sa rozne), wskazuje '" + s2[st] + "'");
A(ts == st + 2, "TextStart o dwa znaki dalej niz Start");
}
else A(false, "jedno wyroznienie w 'aa **bb** cc', jest " + e2.Count);

Console.WriteLine("=== KONTROLE NEGATYWNE: co NIE MOZE byc wyroznieniem ===");
A(Emf("* pozycja listy\n* druga\n").Count == 0, "punktor listy z gwiazdka NIE jest wyroznieniem");
A(Emf("- pozycja\n- druga\n").Count == 0, "punktor z myslnikiem tez nie");
A(Emf("2 * 3 * 4 = 24").Count == 0, "mnozenie w zdaniu (gwiazdka ze spacja) nie jest wyroznieniem");
A(Emf("plik nazwa_z_podkresleniem_txt dalej").Count == 0, "podkreslnik W SRODKU SLOWA nie jest pochyleniem");
A(Emf("```\nkod z **gwiazdkami** w bloku\n```\n").Count == 0, "gwiazdki w bloku kodu pomijane");
A(Emf("otwarte **bez domkniecia w wierszu\nnastepny").Count == 0, "niedomkniete wyroznienie odrzucone");

Console.WriteLine("=== KONTROLA POZYTYWNA PARSERA (nie jest gluchy) ===");
A(Emf("_pochylone podkreslnikiem_").Count == 1, "podkreslnik na granicy slowa DZIALA jako pochylenie");
A(Emf("__pogrubione podkreslnikami__").Count == 1, "dwa podkresleniki to pogrubienie");
IList eMix = Emf("* punkt z **pogrubieniem** w tresci\n");
A(eMix.Count == 1, "w POZYCJI LISTY wyroznienie nadal widoczne, jest " + eMix.Count);

Console.WriteLine("=== LISTY: POCZATEK listy, nie kazda pozycja (ustalenie edsharpng-46) ===");
string sL = "Wstep.\n\n- jeden\n- dwa\n- trzy\n\nAkapit.\n\n1. pierwsza\n2. druga\n\nKoniec.\n";
IList<int> l = Listy(sL);
A(l.Count == 2, "DWIE listy w tekscie o pieciu pozycjach, jest " + l.Count);
if (l.Count == 2) {
A(sL.Substring(l[0]).StartsWith("- jeden"), "pierwsza lista zaczyna sie na '- jeden'");
A(sL.Substring(l[1]).StartsWith("1. pierwsza"), "druga lista zaczyna sie na '1. pierwsza'");
}
A(Listy("Sam akapit bez listy.\n").Count == 0, "KONTROLA NEGATYWNA: brak listy = zero poczatkow");
A(Listy("```\n- to nie lista, to kod\n```\n").Count == 0, "lista w bloku kodu pomijana");

Console.WriteLine("=== LICZNIK POZYCJI (mowa mowi, ile ich jest) ===");
A(Poz("- a\n- b\n- c\n\nakapit\n") == 3, "trzy pozycje przed pustym wierszem, jest " + Poz("- a\n- b\n- c\n\nakapit\n"));
A(Poz("- jedna\n\nakapit\n") == 1, "jedna pozycja, jest " + Poz("- jedna\n\nakapit\n"));
A(Poz("akapit bez listy\n") == 0, "KONTROLA NEGATYWNA licznika: zero pozycji");

Console.WriteLine();
Console.WriteLine("RAZEM: " + ok + " PASS / " + bad + " FAIL");
Environment.Exit(bad == 0 ? 0 : 1);
}
}
