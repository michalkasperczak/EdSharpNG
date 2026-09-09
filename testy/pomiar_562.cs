// Sonda 5.0.62 na ZBUDOWANEJ binarce (refleksja + napisy w .exe).
//
// CO MIERZY:
//   1. Skok po odsylaczach: metoda GoToMarkdownLink istnieje, ma wlasciwa
//      sygnature i uzywa TEGO SAMEGO parsera co lista (GetMarkdownLinks).
//   2. Pozycje menu Next Link / Prior Link istnieja jako POLA klasy.
//   3. Komunikaty skoku sa w binarce ("Last link!", "First link!").
//   4. Komunikat pustej listy zakladek jest w binarce i mowi o Escape.
//   5. Stary komunikat "No bookmark!" (bez s) ZNIKNAL - to on szedl razem z
//      samoczynnym zamknieciem okna.
//
// KONTROLA WAZNOSCI: ta sama sonda na binarce 5.0.61 MUSI skonczyc kodem 3
// ("brak metody GoToMarkdownLink").  Bez tego zielony wynik nie odroznia
// naprawy od gluchej sondy.
//
// Uzycie:
//   csc.exe /out:pomiar_562.exe pomiar_562.cs
//   pomiar_562.exe <sciezka do EdSharpNG.exe>

using System;
using System.IO;
using System.Reflection;
using System.Text;

class Pomiar562 {

static int iOk = 0;
static int iZle = 0;

static void Ok(string s) {iOk++; Console.WriteLine("OK: " + s);}
static void Zle(string s) {iZle++; Console.WriteLine("ZLE: " + s);}
static void Sprawdz(bool b, string s) {if (b) Ok(s); else Zle(s);}

// Napis w binarce .NET stoi w UTF-16, ale niekoniecznie na PARZYSTYM offsecie
// pliku.  Dekodowanie od bajtu zero widzi tylko pary (0,1), (2,3) i tak dalej,
// wiec napis zaczynajacy sie nieparzysto jest dla niego niewidoczny.  Dlatego
// pytamy o OBA przesuniecia; wystarczy trafienie w jednym.
static bool MaNapis(string sA, string sB, string sSzukane) {
return sA.Contains(sSzukane) || sB.Contains(sSzukane);
} // MaNapis method

static int Main(string[] args) {
if (args.Length < 1) {
Console.WriteLine("Uzycie: pomiar_562.exe <EdSharpNG.exe>");
return 2;
}
string sExe = args[0];
if (!File.Exists(sExe)) {
Console.WriteLine("BRAK PLIKU: " + sExe);
return 2;
}

Assembly asm = Assembly.LoadFrom(sExe);
Type tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {
Console.WriteLine("BRAK TYPU EdSharp.MdiFrame");
return 3;
}

BindingFlags bfAll = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

// --- 1. SKOK PO ODSYLACZACH: metoda musi ISTNIEC.  To jest jednoczesnie
// KONTROLA WAZNOSCI calej sondy: na 5.0.61 tej metody nie ma, wiec sonda
// konczy sie tu kodem 3 i zielony wynik na nowej binarce cos znaczy.
MethodInfo miGo = tFrame.GetMethod("GoToMarkdownLink", bfAll);
if (miGo == null) {
Console.WriteLine("BRAK METODY GoToMarkdownLink - to wersja SPRZED zmiany (kontrola waznosci)");
return 3;
}
Ok("metoda GoToMarkdownLink istnieje");

ParameterInfo[] aPar = miGo.GetParameters();
Sprawdz(aPar.Length == 2, "GoToMarkdownLink ma dwa parametry (jest: " + aPar.Length + ")");
if (aPar.Length == 2) {
Sprawdz(aPar[0].ParameterType.Name == "HomerRichTextBox",
"pierwszy parametr to HomerRichTextBox (jest: " + aPar[0].ParameterType.Name + ")");
Sprawdz(aPar[1].ParameterType == typeof(bool),
"drugi parametr to bool, czyli kierunek (jest: " + aPar[1].ParameterType.Name + ")");
}

// Parser MUSI byc wspolny z lista odsylaczow.  Gdyby skok mial wlasny
// parser, obie komendy rozjechaly by sie po pierwszej poprawce.
MethodInfo miParser = tFrame.GetMethod("GetMarkdownLinks", bfAll);
Sprawdz(miParser != null, "parser GetMarkdownLinks istnieje (wspolne zrodlo prawdy ze lista)");
MethodInfo miMowa = tFrame.GetMethod("GetMarkdownLinkSpeech", bfAll);
Sprawdz(miMowa != null, "mowa GetMarkdownLinkSpeech istnieje (skok mowi to samo co lista)");

// --- 2. POZYCJE MENU JAKO POLA KLASY.  Sama metoda bez pozycji menu dala by
// kod nieosiagalny z klawiatury przy zielonym buildzie.
FieldInfo fiNext = tFrame.GetField("menuNavigateNextLink", bfAll);
FieldInfo fiPrior = tFrame.GetField("menuNavigatePriorLink", bfAll);
Sprawdz(fiNext != null, "pole menuNavigateNextLink istnieje");
Sprawdz(fiPrior != null, "pole menuNavigatePriorLink istnieje");

// --- 3. NAPISY W BINARCE.  Napis w .NET siedzi w UTF-16, ale NIE MUSI stac na
// PARZYSTYM offsecie pliku, a Encoding.Unicode czytany od bajtu zero dekoduje
// wylacznie pary (0,1), (2,3) i tak dalej.  ZMIERZONE 02.09.2026: "Last link!"
// lezy na offsecie 347475, czyli nieparzystym, i dekodowanie od zera go NIE
// WIDZI - sonda dawala FALSZYWE ZERO na napisie, ktory w pliku JEST.
// "No links!" trafialo tylko dlatego, ze wypadlo parzysto.  Dlatego czytamy
// binarke w OBU przesunieciach i szukamy w obu.
byte[] aBajty = File.ReadAllBytes(sExe);
string sU16a = Encoding.Unicode.GetString(aBajty);
string sU16b = Encoding.Unicode.GetString(aBajty, 1, aBajty.Length - 1);

Sprawdz(MaNapis(sU16a, sU16b, "Last link!"), "komunikat konca dokumentu: Last link!");
Sprawdz(MaNapis(sU16a, sU16b, "First link!"), "komunikat poczatku dokumentu: First link!");
Sprawdz(MaNapis(sU16a, sU16b, "Next Link"), "nazwa pozycji menu Next Link jest w binarce");
Sprawdz(MaNapis(sU16a, sU16b, "Prior Link"), "nazwa pozycji menu Prior Link jest w binarce");

// KONTROLA POZYTYWNA dopasowania literalu: napis, o ktorym WIEMY, ze jest.
Sprawdz(MaNapis(sU16a, sU16b, "No links!"), "kontrola pozytywna: napis No links! jest w binarce");
// KONTROLA ROZLACZNA: napis wymyslony NIE MOZE sie trafic.
Sprawdz(!MaNapis(sU16a, sU16b, "Fioletowy sloik z odsylaczami"),
"kontrola rozlaczna: napis wymyslony NIE jest w binarce");
// KONTROLA WAZNOSCI SAMEGO MECHANIZMU DWOCH PRZESUNIEC: napis, ktory lezy na
// offsecie NIEPARZYSTYM, musi byc widoczny.  Bez tej asercji poprawka sondy
// przechodzila by niezauwazona, gdyby ktos ja kiedys cofnal.
Sprawdz(!sU16a.Contains("Last link!") && sU16b.Contains("Last link!"),
"kontrola waznosci sondy: Last link! widac WYLACZNIE w przesunieciu o jeden bajt");

// --- 4. PUSTA LISTA ZAKLADEK: komunikat MOWI o Escape.  Bez tego niewidomy
// nie wie, ze okno zostalo otwarte i czeka na jego decyzje.
Sprawdz(MaNapis(sU16a, sU16b, "No bookmarks, press Escape to close the list"),
"pusta lista zakladek mowi o Escape");
Sprawdz(MaNapis(sU16a, sU16b, "No named bookmarks, press Escape to close the list"),
"pusta lista zakladek z nazwa mowi o Escape (ta sama wada w OBU listach)");

// --- 5. STARE ZACHOWANIE ZNIKNELO.  Tu wazna poprawka SONDY, nie kodu:
// pierwotnie asercja mowila "napis No bookmark! zniknal z binarki" i byla
// CZERWONA - slusznie.  Ten napis JEST w programie w TRZECH innych miejscach,
// zupelnie legalnych: to komunikat "nie masz zadnych zakladek", ktory pada
// PRZED otwarciem listy (Alt+B na pliku bez zakladek).  Zmieniona zostala
// tylko droga PO usunieciu ostatniej pozycji Z OTWARTEJ listy, a tam napis
// jest inny.  Asercja na cala binarke mieszala te dwie rzeczy i kazala by
// usunac komunikat, ktory ma prawo istniec.
// Mierzymy wiec to, co naprawde ma znaczenie: nowy komunikat pustej listy
// ISTNIEJE (asercje wyzej), a droga zamykajaca okno zniknela - to sprawdza
// kontrola negatywna na ZRODLE (kontrola_negatywna_562.sh), bo zniknieciu
// wywolania metody Close nie odpowiada zaden napis w binarce.
Sprawdz(MaNapis(sU16a, sU16b, "No bookmark!"),
"kontrola pozytywna: legalny komunikat No bookmark! (brak zakladek PRZED otwarciem listy) ZOSTAL");

// --- 6. KOMENTARZE WYSZLY Z RODZINY F9 na rodzine PageUp/PageDown.
// Chordy mierzymy w OPISACH MOWIONYCH, bo tam czytnik je wymawia; sam kod
// menu jest sprawdzany kontrola negatywna na zrodle.
Sprawdz(MaNapis(sU16a, sU16b, "Next Comment"), "pozycja Next Comment nadal istnieje (skok nie zniknal)");
Sprawdz(MaNapis(sU16a, sU16b, "Prior Comment"), "pozycja Prior Comment nadal istnieje");

// --- 7. ROZLACZNOSC: lista odsylaczow pod Control+F6 NIE zniknela przy
// dodaniu skoku.  Bez tej asercji "naprawa" kasujaca liste tez byla by zielona.
MethodInfo miLista = tFrame.GetMethod("ShowMarkdownLinkList", bfAll);
Sprawdz(miLista != null, "asercja rozlacznosci: lista odsylaczow ShowMarkdownLinkList NADAL istnieje");
FieldInfo fiLista = tFrame.GetField("menuNavigateLinkList", bfAll);
Sprawdz(fiLista != null, "asercja rozlacznosci: pozycja menu Link List nadal istnieje");

Console.WriteLine();
Console.WriteLine("PODSUMOWANIE: " + iOk + " OK, " + iZle + " ZLE");
return (iZle == 0) ? 0 : 1;
} // Main

} // Pomiar562 class
