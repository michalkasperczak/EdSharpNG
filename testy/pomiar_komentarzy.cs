// Pomiar komentarzy wewnetrznych (5.0.43) PRZEZ REFLEKSJE na ZBUDOWANEJ
// binarce EdSharpNG.exe.  Mierzy prawdziwe metody programu, a nie kopie logiki
// przepisana do testu - to jedyny sposob, zeby wynik mowil o tym, co dostanie
// Kasperczak.
//
// Budowanie i uruchamianie: patrz testy/kontrola_negatywna_543.sh.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

class PomiarKomentarzy {

static int iOk = 0, iBad = 0;

static void Ok(string sName, bool bPass, string sDetail) {
if (bPass) {iOk++; Console.WriteLine("OK   " + sName + (sDetail.Length > 0 ? "  [" + sDetail + "]" : ""));}
else {iBad++; Console.WriteLine("FAIL " + sName + "  [" + sDetail + "]");}
}

static Type tFrame, tComment;
static MethodInfo miGetComments, miFindAt, miSpeech, miSanitize, miLineNumber;

// Odczyt pol struktury komentarza przez refleksje - klasa jest prywatna, wiec
// nie da sie jej rzutowac, ale pola czytamy.
static int Start(object o) {return (int) tComment.GetField("Start").GetValue(o);}
static int End(object o) {return (int) tComment.GetField("End").GetValue(o);}
static string Text(object o) {return (string) tComment.GetField("Text").GetValue(o);}

static IList Comments(string sText) {
return (IList) miGetComments.Invoke(null, new object[] {sText});
}

public static void Main(string[] args) {
string sExe = (args.Length > 0) ? args[0] : "EdSharpNG.exe";
Assembly asm = Assembly.LoadFrom(sExe);

// ---- KONTROLE POZYTYWNE SONDY ----
// Bez nich "0 komentarzy" nie odroznia naprawy od sondy, ktora nic nie widzi.
tFrame = asm.GetType("EdSharp.MdiFrame");
Ok("sonda: typ MdiFrame istnieje", tFrame != null, tFrame == null ? "BRAK - sonda mierzy nic" : "");
if (tFrame == null) {Podsumuj(); return;}

tComment = asm.GetType("EdSharp.MdiFrame+MarkdownComment");
Ok("sonda: klasa MarkdownComment istnieje", tComment != null, tComment == null ? "BRAK" : "");
if (tComment == null) {Podsumuj(); return;}

BindingFlags bfS = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
BindingFlags bfI = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
miGetComments = tFrame.GetMethod("GetMarkdownComments", bfS);
miFindAt = tFrame.GetMethod("FindMarkdownCommentAtIndex", bfS);
miSpeech = tFrame.GetMethod("GetMarkdownCommentSpeech", bfS);
miSanitize = tFrame.GetMethod("SanitizeMarkdownCommentBody", bfS);
miLineNumber = tFrame.GetMethod("GetTextLineNumberAtIndex", bfS);
Ok("sonda: GetMarkdownComments istnieje", miGetComments != null, miGetComments == null ? "BRAK" : "");
Ok("sonda: FindMarkdownCommentAtIndex istnieje", miFindAt != null, miFindAt == null ? "BRAK" : "");
Ok("sonda: GetMarkdownCommentSpeech istnieje", miSpeech != null, miSpeech == null ? "BRAK" : "");
Ok("sonda: SanitizeMarkdownCommentBody istnieje", miSanitize != null, miSanitize == null ? "BRAK" : "");
Ok("sonda: GetTextLineNumberAtIndex istnieje", miLineNumber != null, miLineNumber == null ? "BRAK" : "");
if (miGetComments == null || miFindAt == null || miSpeech == null
|| miSanitize == null || miLineNumber == null) {Podsumuj(); return;}

// Sonda MUSI widziec metody obslugi, inaczej klawisz moze byc martwy przy
// zielonym buildzie.
Ok("sonda: InsertOrEditMarkdownComment istnieje",
tFrame.GetMethod("InsertOrEditMarkdownComment", bfI) != null, "");
Ok("sonda: GoToMarkdownComment istnieje",
tFrame.GetMethod("GoToMarkdownComment", bfI) != null, "");
Ok("sonda: ShowMarkdownCommentList istnieje",
tFrame.GetMethod("ShowMarkdownCommentList", bfI) != null, "");

// ---- ROZPOZNAWANIE ----
string sDoc = "# Rozdzial\n\nAkapit.\n\n<!-- pierwsza uwaga -->\n\nDrugi akapit z <!-- druga uwaga --> w srodku.\n";
IList c = Comments(sDoc);
Ok("dwa komentarze w dokumencie", c.Count == 2, "znaleziono " + c.Count);
if (c.Count == 2) {
Ok("tresc pierwszego bez znacznikow", Text(c[0]) == "pierwsza uwaga", "[" + Text(c[0]) + "]");
Ok("tresc drugiego bez znacznikow", Text(c[1]) == "druga uwaga", "[" + Text(c[1]) + "]");
Ok("zakres pierwszego pokrywa caly znacznik",
sDoc.Substring(Start(c[0]), End(c[0]) - Start(c[0])) == "<!-- pierwsza uwaga -->",
sDoc.Substring(Start(c[0]), End(c[0]) - Start(c[0])));
}

// Komentarz WIELOWIERSZOWY - uwaga robocza czesto ma kilka zdan.
string sMulti = "Tekst.\n\n<!-- pierwsza linia\ndruga linia\ntrzecia -->\n\nKoniec.\n";
IList cm = Comments(sMulti);
Ok("komentarz wielowierszowy rozpoznany", cm.Count == 1, "znaleziono " + cm.Count);
if (cm.Count == 1) {
Ok("tresc wielowierszowa zwinieta w jedna linie dla czytnika",
Text(cm[0]) == "pierwsza linia druga linia trzecia", "[" + Text(cm[0]) + "]");
}

// ---- KONTROLE NEGATYWNE: co NIE MOZE byc komentarzem ----
// Bez nich "rozpoznaje komentarze" nie odroznia sie od "rozpoznaje wszystko".
string sFence = "Tekst.\n\n```\nblok kodu z <!-- to jest przyklad --> w srodku\n```\n\nKoniec.\n";
Ok("KONTROLA NEG: komentarz w bloku kodu NIE jest komentarzem",
Comments(sFence).Count == 0, "znaleziono " + Comments(sFence).Count);

string sTilde = "Tekst.\n\n~~~\n<!-- przyklad w drugim rodzaju ogrodzenia -->\n~~~\n\nKoniec.\n";
Ok("KONTROLA NEG: komentarz w ogrodzeniu z tyld NIE jest komentarzem",
Comments(sTilde).Count == 0, "znaleziono " + Comments(sTilde).Count);

Ok("KONTROLA NEG: zwykly znacznik HTML nie jest komentarzem",
Comments("Tekst z <b>pogrubieniem</b> i <br> w srodku.\n").Count == 0, "");
Ok("KONTROLA NEG: niedomkniety komentarz nie jest komentarzem",
Comments("Tekst <!-- zaczete i nigdy nie zamkniete\n").Count == 0, "");
Ok("KONTROLA NEG: pusty dokument nie wywala metody",
Comments("").Count == 0, "");
Ok("KONTROLA NEG: null nie wywala metody",
Comments(null).Count == 0, "");

// Komentarz PUSTY jest komentarzem - ma znaczniki, wiec da sie w nim stanac
// i go poprawic.  To NIE to samo, co brak komentarza.
IList ce = Comments("Tekst <!---->  koniec.\n");
Ok("komentarz pusty jest rozpoznany", ce.Count == 1, "znaleziono " + ce.Count);
if (ce.Count == 1) Ok("pusty ma pusta tresc", Text(ce[0]).Length == 0, "[" + Text(ce[0]) + "]");

// ---- KURSOR W KOMENTARZU ----
string sOne = "Ala <!-- uwaga --> ma kota.\n";
IList c1 = Comments(sOne);
int iStart = Start(c1[0]), iEnd = End(c1[0]);
Ok("kursor na poczatku komentarza go znajduje",
(int) miFindAt.Invoke(null, new object[] {c1, iStart}) == 0, "");
Ok("kursor w srodku komentarza go znajduje",
(int) miFindAt.Invoke(null, new object[] {c1, iStart + 5}) == 0, "");
Ok("kursor na KONCU komentarza go znajduje (tam stoi po wstawieniu)",
(int) miFindAt.Invoke(null, new object[] {c1, iEnd}) == 0, "");
Ok("KONTROLA NEG: kursor przed komentarzem go NIE znajduje",
(int) miFindAt.Invoke(null, new object[] {c1, iStart - 1}) == -1, "");
Ok("KONTROLA NEG: kursor za komentarzem go NIE znajduje",
(int) miFindAt.Invoke(null, new object[] {c1, iEnd + 1}) == -1, "");

// ---- ZABEZPIECZENIE TRESCI ----
// To jest wlasciwa ochrona pliku: "-->" w tresci zamknelby komentarz i reszta
// uwagi wyladowalaby w widocznym dokumencie.
string sSan = (string) miSanitize.Invoke(null, new object[] {"koniec --> i dalej"});
Ok("zamykacz w tresci zneutralizowany", sSan.IndexOf("-->") < 0, "[" + sSan + "]");
string sSan2 = (string) miSanitize.Invoke(null, new object[] {"myslnik -- podwojny"});
Ok("podwojny myslnik zneutralizowany", sSan2.IndexOf("--") < 0, "[" + sSan2 + "]");
string sSan3 = (string) miSanitize.Invoke(null, new object[] {"cztery ---- myslniki"});
Ok("ciag czterech myslnikow zneutralizowany", sSan3.IndexOf("--") < 0, "[" + sSan3 + "]");
// KONTROLA POZYTYWNA: zwykla tresc NIE MOZE byc ruszana - bez niej metoda
// psujaca kazdy tekst tez by przeszla.
string sSan4 = (string) miSanitize.Invoke(null, new object[] {"zwykla uwaga o tekscie"});
Ok("KONTROLA: zwykla tresc nietknieta", sSan4 == "zwykla uwaga o tekscie", "[" + sSan4 + "]");
string sSan5 = (string) miSanitize.Invoke(null, new object[] {"pojedynczy - myslnik zostaje"});
Ok("KONTROLA: pojedynczy myslnik zostaje", sSan5 == "pojedynczy - myslnik zostaje", "[" + sSan5 + "]");

// Zabezpieczona tresc MUSI dac komentarz, ktory nasz wlasny parser widzi jako
// JEDEN komentarz - inaczej ochrona rozbijalaby dokument.
string sRound = "Tekst <!-- " + sSan + " --> koniec.\n";
IList cr = Comments(sRound);
Ok("zabezpieczona tresc daje DOKLADNIE jeden komentarz", cr.Count == 1, "znaleziono " + cr.Count);

// ---- MOWA ----
string sSp = (string) miSpeech.Invoke(null, new object[] {c1[0]});
Ok("czytnik slyszy TRESC PRZED slowem komentarz", sSp.StartsWith("uwaga"), "[" + sSp + "]");
Ok("wypowiedz zawiera slowo comment", sSp.IndexOf("comment") >= 0, "[" + sSp + "]");
string sSpE = (string) miSpeech.Invoke(null, new object[] {ce[0]});
Ok("pusty komentarz nazwany wprost", sSpE == "empty comment", "[" + sSpE + "]");

// ---- NUMER WIERSZA W LISCIE ----
string sLines = "pierwszy\ndrugi\ntrzeci\n";
Ok("numer wiersza liczony OD JEDYNKI, nie od zera",
(int) miLineNumber.Invoke(null, new object[] {sLines, 0}) == 1,
"" + miLineNumber.Invoke(null, new object[] {sLines, 0}));
Ok("numer wiersza w drugim wierszu to 2",
(int) miLineNumber.Invoke(null, new object[] {sLines, 10}) == 2,
"" + miLineNumber.Invoke(null, new object[] {sLines, 10}));
// KONTROLA ROZNICUJACA: trzy roznie polozone offsety musza dac TRZY ROZNE
// numery - bez niej metoda zwracajaca stala tez by przeszla.
int l1 = (int) miLineNumber.Invoke(null, new object[] {sLines, 0});
int l2 = (int) miLineNumber.Invoke(null, new object[] {sLines, 10});
int l3 = (int) miLineNumber.Invoke(null, new object[] {sLines, 17});
Ok("KONTROLA ROZNICUJACA: trzy offsety daja trzy rozne wiersze",
l1 != l2 && l2 != l3 && l1 != l3, l1 + "/" + l2 + "/" + l3);

// ---- KOMENTARZ NIE MYLI SIE Z PRZYPISEM ANI Z TABELA ----
string sMix = "# Tytul\n\n| Imie | Wiek |\n| --- | --- |\n| Ala | 30 |\n\nPrzypis[^1] tutaj.\n\n<!-- uwaga do tabeli -->\n\n[^1]: tresc przypisu\n";
IList cx = Comments(sMix);
Ok("komentarz obok tabeli i przypisu rozpoznany", cx.Count == 1, "znaleziono " + cx.Count);
if (cx.Count == 1) Ok("i ma wlasna tresc", Text(cx[0]) == "uwaga do tabeli", "[" + Text(cx[0]) + "]");

Podsumuj();
}

static void Podsumuj() {
Console.WriteLine();
Console.WriteLine("WYNIK: " + iOk + "/" + (iOk + iBad));
Environment.Exit(iBad == 0 ? 0 : 1);
}

} // PomiarKomentarzy class
