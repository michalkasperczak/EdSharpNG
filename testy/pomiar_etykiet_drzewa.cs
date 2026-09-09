// Pomiar etykiet drzewa Document Navigation (F6) po ZBUDOWANYM EdSharpNG.exe.
//
// PO CO: Kasperczak zglosil 26.08.2026 (zlecenia -5 i -6), ze w drzewie F6
// czytnik oglasza glebokosc galezi ("poziom 1" na naglowku drugiego poziomu),
// a on chce slyszec PRAWDZIWY poziom naglowka Markdown. Wybral wariant:
// "tresc naglowek dwa pierwszy poziom moze byc", czyli poziom DOPISANY do
// tekstu pozycji.
//
// CZEGO TEN POMIAR NIE ROZSTRZYGA: jak to zabrzmi w NVDA. Mierzy TEKST, ktory
// program wklada do pozycji drzewa - a wiec to, co czytnik ma do przeczytania.
// Fokusu klawiatury nie dalo sie 26.08 zdobyc (wisi cudze systemowe okno
// zapory), wiec pomiar przez zywy czytnik zostaje po stronie Kasperczaka.
//
// Metoda: refleksja po prywatnych metodach EdSharpNG.exe, wiec mierzymy kod,
// ktory realnie u niego poleci.
//
// Budowa i uruchomienie (z WSL):
//   /mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe /nologo \
//       /r:EdSharpNG.exe /out:testy\pomiar_etykiet_drzewa.exe testy\pomiar_etykiet_drzewa.cs
//   ./testy/pomiar_etykiet_drzewa.exe <sciezka do pliku md>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

public class PomiarEtykietDrzewa {

static int iZle = 0;

static void Spr(string sOpis, bool bOk, string sDetal) {
Console.WriteLine((bOk ? "  [OK ] " : "  [ZLE] ") + sOpis + ": " + sDetal);
if (!bOk) iZle++;
}

public static int Main(string[] args) {
if (args.Length < 1) {Console.WriteLine("Podaj sciezke do pliku .md."); return 2;}
string sPath = args[0];
if (!File.Exists(sPath)) {Console.WriteLine("Nie ma pliku: " + sPath); return 2;}
string sText = File.ReadAllText(sPath);

Assembly asm = Assembly.LoadFrom(Path.GetFullPath("EdSharpNG.exe"));
Type tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {Console.WriteLine("Nie znalazlem klasy MdiFrame."); return 2;}

MethodInfo miHeadings = tFrame.GetMethod("GetMarkdownSectionHeadings", BindingFlags.NonPublic | BindingFlags.Static);
MethodInfo miLabel = tFrame.GetMethod("GetDocumentNavigationNodeLabel", BindingFlags.NonPublic | BindingFlags.Static);
MethodInfo miTitle = tFrame.GetMethod("GetMarkdownSectionHeadingTitle", BindingFlags.NonPublic | BindingFlags.Static);
if (miHeadings == null || miLabel == null || miTitle == null) {
Console.WriteLine("Nie znalazlem metod drzewa (GetDocumentNavigationNodeLabel?)."); return 2;}

IList headings = (IList) miHeadings.Invoke(null, new object[] {sText});
Console.WriteLine("Naglowkow w pliku (poza blokami kodu): " + headings.Count);
if (headings.Count == 0) {Console.WriteLine("Plik bez naglowkow - pomiar nic nie rozstrzygnie."); return 2;}

Type tHeading = null;
List<string> etykiety = new List<string>();
List<int> poziomy = new List<int>();
List<string> tytuly = new List<string>();
Console.WriteLine();
Console.WriteLine("=== ETYKIETY, KTORE CZYTNIK DOSTANIE ===");
foreach (object h in headings) {
if (tHeading == null) tHeading = h.GetType();
int iLevel = (int) tHeading.GetField("Level").GetValue(h);
string sLabel = (string) miLabel.Invoke(null, new object[] {h});
string sTitle = (string) miTitle.Invoke(null, new object[] {h});
etykiety.Add(sLabel); poziomy.Add(iLevel); tytuly.Add(sTitle);
Console.WriteLine("  poziom " + iLevel + "   " + sLabel);
}

Console.WriteLine();
Console.WriteLine("=== WERYFIKACJA ===");

// 1. Kazda etykieta konczy sie "heading N".
int iBez = 0;
string sPrzyklad = "";
for (int i = 0; i < etykiety.Count; i++) {
if (!Regex.IsMatch(etykiety[i], @", heading [1-6]$")) {iBez++; if (sPrzyklad == "") sPrzyklad = etykiety[i];}
}
Spr("kazda pozycja konczy sie 'heading N'", iBez == 0,
iBez == 0 ? ("wszystkie " + etykiety.Count) : (iBez + " bez dopisku, np. " + sPrzyklad));

// 2. Liczba w etykiecie = poziom naglowka z pliku, nie glebokosc galezi.
int iRozne = 0;
for (int i = 0; i < etykiety.Count; i++) {
Match m = Regex.Match(etykiety[i], @", heading ([1-6])$");
if (!m.Success || Int32.Parse(m.Groups[1].Value) != poziomy[i]) iRozne++;
}
Spr("liczba w etykiecie = poziom naglowka (liczba kratek)", iRozne == 0,
iRozne == 0 ? "zgodne dla wszystkich" : (iRozne + " rozbieznosci"));

// 3. KONTROLA ROZSTRZYGAJACA. Poziom naglowka i glebokosc galezi to dwie
//    rozne liczby, a pomiar musi je odroznic - inaczej zielony wynik nie
//    dowodzi niczego. Liczymy glebokosc dokladnie tak, jak buduje sie drzewo:
//    naglowek glebszy od poprzednika staje sie jego dzieckiem.
List<int> glebokosci = new List<int>();
List<int> stos = new List<int>();
for (int i = 0; i < poziomy.Count; i++) {
while (stos.Count > 0 && stos[stos.Count - 1] >= poziomy[i]) stos.RemoveAt(stos.Count - 1);
glebokosci.Add(stos.Count);   // 0 = korzen, tyle oglasza czytnik jako "poziom"
stos.Add(poziomy[i]);
}
int iRozniace = 0;
string sPrzykladRoznicy = "";
for (int i = 0; i < poziomy.Count; i++) {
if (poziomy[i] != glebokosci[i]) {
iRozniace++;
if (sPrzykladRoznicy == "") sPrzykladRoznicy = "\"" + etykiety[i] + "\" (czytnik powie o galezi: poziom " + glebokosci[i] + ")";
}
}
Spr("kontrola: sa naglowki, gdzie poziom != glebokosc galezi", iRozniace > 0,
iRozniace > 0 ? (iRozniace + " z " + poziomy.Count + ", np. " + sPrzykladRoznicy)
: "BRAK - na tym pliku pomiar nie odroznia naszego dopisku od glebokosci galezi");

// 4. Tytul przed przecinkiem nietkniety - nawigacja literowa (test 1.4)
//    dopasowuje POCZATEK etykiety, wiec poziom nie moze wejsc przed tytul.
int iZlyPoczatek = 0;
for (int i = 0; i < etykiety.Count; i++) {
if (!etykiety[i].StartsWith(tytuly[i])) iZlyPoczatek++;
}
Spr("etykieta zaczyna sie tytulem (nawigacja literowa dziala)", iZlyPoczatek == 0,
iZlyPoczatek == 0 ? "wszystkie zaczynaja sie tytulem" : (iZlyPoczatek + " zaczyna sie czyms innym"));

// 5. Kontrola negatywna na SAMEJ etykiecie: naglowek bez tytulu nie moze
//    zgubic poziomu (dawniej wracalo samo "section").
object hPusty = Activator.CreateInstance(tHeading, true);
tHeading.GetField("Start").SetValue(hPusty, 0);
tHeading.GetField("Level").SetValue(hPusty, 3);
tHeading.GetField("Title").SetValue(hPusty, "");
string sPusty = (string) miLabel.Invoke(null, new object[] {hPusty});
Spr("naglowek bez tytulu tez mowi poziom", sPusty == "section, heading 3", "\"" + sPusty + "\"");

// 6. Kontrola negatywna odtwarzajaca STAN PRZED ZMIANA: sama nazwa naglowka,
//    bez dopisku, NIE spelnia wymagania. Bez tego punktu nie wiadomo, czy
//    test w ogole potrafi zlapac brak poziomu.
bool bStaryPrzechodzi = Regex.IsMatch(tytuly[0], @", heading [1-6]$");
Spr("kontrola negatywna: stara etykieta (sam tytul) NIE przechodzi", !bStaryPrzechodzi,
"stara etykieta \"" + tytuly[0] + "\" " + (bStaryPrzechodzi ? "przechodzi - test slepy" : "odrzucona"));

Console.WriteLine();
Console.WriteLine(iZle == 0 ? "WSZYSTKO ZALICZONE" : ("NIEZALICZONYCH: " + iZle));
return iZle == 0 ? 0 : 1;
}
}
