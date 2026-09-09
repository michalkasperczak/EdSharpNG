// Pomiar parsera listy linkow WPROST NA ZBUDOWANEJ BINARCE.
// Refleksja, bo GetMarkdownLinks, GetMarkdownLinkSpeech i GetMarkdownLinkMarkup
// sa prywatne.  Zielony build nie dowodzi, ze lista widzi wlasciwe odsylacze i
// ze kursor stanie na TRESCI: dlatego kazda rodzina przypadkow ma tu KONTROLE
// NEGATYWNA - tekst, w ktorym linku byc NIE MOZE (blok kodu, nawias bez adresu,
// zwykly nawias kwadratowy).
//
// UZYCIE:  pomiar_listy_linkow.exe [sciezka do EdSharpNG.exe]
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

static IList Linki(string s) {
MethodInfo mi = tFrame.GetMethod("GetMarkdownLinks", BindingFlags.NonPublic | BindingFlags.Static);
if (mi == null) throw new Exception("brak GetMarkdownLinks");
return (IList) mi.Invoke(null, new object[] {s});
}

static string Mowa(object link) {
MethodInfo mi = tFrame.GetMethod("GetMarkdownLinkSpeech", BindingFlags.NonPublic | BindingFlags.Static);
if (mi == null) throw new Exception("brak GetMarkdownLinkSpeech");
return (string) mi.Invoke(null, new object[] {link});
}

static string Markup(object link, string sText) {
MethodInfo mi = tFrame.GetMethod("GetMarkdownLinkMarkup", BindingFlags.NonPublic | BindingFlags.Static);
if (mi == null) throw new Exception("brak GetMarkdownLinkMarkup");
return (string) mi.Invoke(null, new object[] {link, sText});
}

static int Pole(object link, string sNazwa) {
return (int) link.GetType().GetField(sNazwa).GetValue(link);
}

static string PoleS(object link, string sNazwa) {
return (string) link.GetType().GetField(sNazwa).GetValue(link);
}

static bool PoleB(object link, string sNazwa) {
return (bool) link.GetType().GetField(sNazwa).GetValue(link);
}

public static void Main(string[] args) {
Assembly asm = Assembly.LoadFrom(System.IO.Path.GetFullPath(args.Length > 0 ? args[0] : "EdSharpNG.exe"));
tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {Console.WriteLine("BLAD: brak typu MdiFrame"); Environment.Exit(2);}

Console.WriteLine("=== 1. POSTAC [tresc](adres): tresc, adres, rodzaj ===");
string s1 = "Zobacz [stronę Hermesa](https://example.org/hermes) w tekscie.\n";
IList l1 = Linki(s1);
A(l1.Count == 1, "jeden odsylacz w zdaniu, jest " + l1.Count);
if (l1.Count == 1) {
A(PoleS(l1[0], "Title") == "stronę Hermesa", "tresc to 'stronę Hermesa', jest '" + PoleS(l1[0], "Title") + "'");
A(PoleS(l1[0], "Url") == "https://example.org/hermes", "adres znormalizowany, jest '" + PoleS(l1[0], "Url") + "'");
A(!PoleB(l1[0], "Image"), "to NIE obrazek");
A(PoleB(l1[0], "Inline"), "postac nawiasowa rozpoznana");
// KURSOR NA TRESCI, nie na nawiasie - to samo wymaganie co przy wyroznieniach.
A(s1.Substring(Pole(l1[0], "TextStart")).StartsWith("stronę Hermesa"), "TextStart wskazuje TRESC, nie nawias");
A(s1[Pole(l1[0], "Start")] == '[', "Start wskazuje nawias otwierajacy");
A(Mowa(l1[0]) == "stronę Hermesa, https://example.org/hermes, link", "mowa: tresc, adres, rodzaj - jest '" + Mowa(l1[0]) + "'");
A(Markup(l1[0], s1) == "[stronę Hermesa](https://example.org/hermes)", "Control+C daje caly odsylacz, jest '" + Markup(l1[0], s1) + "'");
}

Console.WriteLine("=== 2. OBRAZEK: wykrzyknik nalezy do odsylacza ===");
string s2 = "Rysunek ![opis obrazka](obraz.png) w tekscie.\n";
IList l2 = Linki(s2);
A(l2.Count == 1, "jeden odsylacz, jest " + l2.Count);
if (l2.Count == 1) {
A(PoleB(l2[0], "Image"), "rozpoznany jako obrazek");
A(s2[Pole(l2[0], "Start")] == '!', "Start wskazuje WYKRZYKNIK, nie nawias - inaczej edycja zostawilaby go w tekscie");
A(Mowa(l2[0]).EndsWith(", image"), "mowa konczy sie slowem image, jest '" + Mowa(l2[0]) + "'");
A(Markup(l2[0], s2) == "![opis obrazka](obraz.png)", "Control+C bierze takze wykrzyknik, jest '" + Markup(l2[0], s2) + "'");
}

Console.WriteLine("=== 3. ODSYLACZ WEWNETRZNY (#kotwica) - MUSI byc na liscie ===");
// Normalizacja przepuszcza tylko http, ftp i mailto, wiec bez osobnej sciezki
// spis tresci wygladalby jak dokument BEZ odsylaczy.
string s3 = "Patrz [Instalacja](#instalacja) nizej.\n";
IList l3 = Linki(s3);
A(l3.Count == 1, "odsylacz wewnetrzny jest na liscie, jest " + l3.Count);
if (l3.Count == 1) {
A(PoleS(l3[0], "Url") == "#instalacja", "adres to '#instalacja', jest '" + PoleS(l3[0], "Url") + "'");
A(PoleS(l3[0], "Title") == "Instalacja", "tresc to 'Instalacja'");
}

Console.WriteLine("=== 4. GOLE ADRESY: <adres>, http, www ===");
string s4 = "Raz http://example.org/a dwa <https://example.org/b> trzy www.example.org/c.\n";
IList l4 = Linki(s4);
A(l4.Count == 3, "trzy gole adresy, jest " + l4.Count);
if (l4.Count == 3) {
A(!PoleB(l4[0], "Inline"), "goly adres NIE jest postacia nawiasowa");
A(PoleS(l4[0], "Title").Length == 0, "goly adres nie ma tresci");
A(Mowa(l4[0]) == "http://example.org/a, link", "mowa golego adresu to adres i rodzaj, jest '" + Mowa(l4[0]) + "'");
// KROPKA KONCZACA ZDANIE nie jest czescia adresu - inaczej edycja by ja zjadla.
A(!Markup(l4[2], s4).EndsWith("."), "kropka konczaca zdanie NIE wchodzi do adresu, jest '" + Markup(l4[2], s4) + "'");
A(PoleS(l4[2], "Url") == "https://www.example.org/c", "www dostaje https, jest '" + PoleS(l4[2], "Url") + "'");
}

Console.WriteLine("=== 5. KOLEJNOSC: wg pozycji w tekscie, nie wg rodzaju ===");
string s5 = "goly http://example.org/pierwszy potem [drugi](http://example.org/drugi) i http://example.org/trzeci\n";
IList l5 = Linki(s5);
A(l5.Count == 3, "trzy odsylacze, jest " + l5.Count);
if (l5.Count == 3) {
A(Pole(l5[0], "Start") < Pole(l5[1], "Start") && Pole(l5[1], "Start") < Pole(l5[2], "Start"), "posortowane po pozycji");
A(PoleB(l5[1], "Inline"), "sredni to postac nawiasowa - czyli sortowanie MIESZA rodzaje, a nie grupuje");
}

Console.WriteLine("=== 6. KONTROLE NEGATYWNE (lista nie moze widziec linku, gdzie go nie ma) ===");
A(Linki("Zwykly tekst bez zadnego adresu.\n").Count == 0, "tekst bez adresu = zero odsylaczy");
A(Linki("```\nhttp://example.org/w-kodzie\n```\n").Count == 0, "goly adres w bloku kodu POMIJANY");
A(Linki("```\n[tresc](http://example.org/w-kodzie)\n```\n").Count == 0, "odsylacz w bloku kodu POMIJANY");
A(Linki("Przypis [^1] w zdaniu.\n").Count == 0, "znacznik przypisu to NIE odsylacz");
A(Linki("Nawias [sam tekst] bez adresu.\n").Count == 0, "nawias kwadratowy bez adresu to NIE odsylacz");
A(Linki("").Count == 0, "pusty tekst = zero odsylaczy");

Console.WriteLine("=== 7. KONTROLE POZYTYWNE PARSERA (nie jest gluchy) ===");
A(Linki("Poza kodem http://example.org/a\n```\nhttp://example.org/b\n```\nznowu poza http://example.org/c\n").Count == 2,
"adresy PRZED i PO bloku kodu widoczne, w bloku nie - jest " + Linki("Poza kodem http://example.org/a\n```\nhttp://example.org/b\n```\nznowu poza http://example.org/c\n").Count);
A(Linki("mailto [napisz](mailto:kto@example.org)\n").Count == 1, "adres pocztowy jest odsylaczem");
IList lDup = Linki("<https://example.org/x>\n");
A(lDup.Count == 1, "adres w nawiasach ostrych liczony RAZ, nie dwa razy (dwa wyrazenia trafiaja w to samo miejsce), jest " + lDup.Count);

Console.WriteLine("=== 8. ZAKRES ODSYLACZA, ktory F2 przepisuje ===");
string s8 = "przed [tytul](http://example.org/z) po\n";
IList l8 = Linki(s8);
if (l8.Count == 1) {
string sWyciety = s8.Substring(Pole(l8[0], "Start"), Pole(l8[0], "End") - Pole(l8[0], "Start"));
A(sWyciety == "[tytul](http://example.org/z)", "zakres Start-End to DOKLADNIE odsylacz, jest '" + sWyciety + "'");
A(!sWyciety.StartsWith(" ") && !sWyciety.EndsWith(" "), "zakres nie zabiera spacji z otoczenia");
}
else A(false, "jeden odsylacz do pomiaru zakresu, jest " + l8.Count);

Console.WriteLine();
Console.WriteLine("RAZEM: " + ok + " PASS / " + bad + " FAIL");
Environment.Exit(bad == 0 ? 0 : 1);
}
}
