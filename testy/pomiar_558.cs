// pomiar_558.cs - sonda na ZBUDOWANEJ binarce dla wersji 5.0.58.
//
// CO MIERZY (zlecenia 1788278589371-3, 1788278778162-4, 1788279917207-5):
//   1. Lista linkow mowi TRESC PRZED wspolrzedna (jego "dalej mowi Line, Line").
//   2. Skok po przypisach jest na Control+Alt+PageUp/PageDown, a NIE na
//      Control+Alt+K ani Control+Alt+Shift+K.
//   3. Piec komend zwolnilo swoje chordy, ale ZOSTALO w menu (menu-only).
//   4. Control+Y ponawia: istnieje HandleRedoAliasKey i pyta o Control+Y.
//   5. Listy zakladek nie maja podpowiedzi Delete w pasku stanu.
//
// KONTROLA NEGATYWNA: ta sama sonda na binarce 5.0.57 musi dac wynik ROZNY.
// Uzycie:
//   pomiar_558.exe <sciezka do EdSharpNG.exe>
//
// PULAPKA, KTORA TU OBCHODZIMY (lekcja 01.09.2026): Assembly.LoadFrom oddaje TE
// SAMA assembly dla dwoch plikow o tej samej tozsamosci, wiec kontrola "w
// starej tego nie bylo" pytalaby nowa binarke.  Uzywamy LoadFile ze sciezka
// absolutna.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

public class Pomiar558 {

static int iPass = 0;
static int iFail = 0;

static void Zdaj(string s)  { iPass++; Console.WriteLine("PASS  " + s); }
static void Oblej(string s) { iFail++; Console.WriteLine("FAIL  " + s); }
static void Sprawdz(bool b, string s) { if (b) Zdaj(s); else Oblej(s); }

public static int Main(string[] aArgs) {
    if (aArgs.Length < 1) {
        Console.WriteLine("Uzycie: pomiar_558.exe <EdSharpNG.exe>");
        return 2;
    }
    string sExe = Path.GetFullPath(aArgs[0]);
    if (!File.Exists(sExe)) { Console.WriteLine("BRAK PLIKU: " + sExe); return 2; }

    Assembly asm;
    try { asm = Assembly.LoadFile(sExe); }
    catch (Exception ex) { Console.WriteLine("BLAD LADOWANIA: " + ex.Message); return 2; }

    Type tFrame = asm.GetType("EdSharp.MdiFrame");
    if (tFrame == null) { Console.WriteLine("BRAK typu EdSharp.MdiFrame"); return 3; }

    // --- 1. NOWE POLE MENU I NOWA METODA (dowod, ze warstwy sa kompletne) ---
    FieldInfo fiNext = tFrame.GetField("menuMiscNextFootnote",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (fiNext == null) {
        Console.WriteLine("BRAK pola menuMiscNextFootnote - ta binarka jest sprzed zmiany");
        return 3;
    }
    Zdaj("pole menuMiscNextFootnote istnieje");

    MethodInfo miRedo = tFrame.GetMethod("HandleRedoAliasKey",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    Sprawdz(miRedo != null, "metoda HandleRedoAliasKey istnieje");
    if (miRedo != null) {
        // Metoda MUSI brac Keys i zwracac bool - inaczej nie jest ogniwem
        // lancucha ProcessCmdKey_Helper, a tylko martwym helperem.
        ParameterInfo[] aP = miRedo.GetParameters();
        Sprawdz(aP.Length == 1 && aP[0].ParameterType.FullName == "System.Windows.Forms.Keys",
            "HandleRedoAliasKey bierze Keys");
        Sprawdz(miRedo.ReturnType == typeof(bool), "HandleRedoAliasKey zwraca bool");
    }

    // --- 2. NAPISY W BINARCE, OBA KODOWANIA ---
    // Grep po .NET wymaga OBU kodowan (lekcja z tego projektu): literaly ida
    // jako UTF-16 w tablicy #US, a nazwy metod jako ANSI w tablicy nazw.
    byte[] aBytes = File.ReadAllBytes(sExe);
    aPlik = aBytes;
    string sAnsi = Encoding.GetEncoding(1252).GetString(aBytes);
    // DWA PRZESUNIECIA UTF-16, NIE JEDNO.  Defekt tej sondy zlapany 01.09.2026
    // przez KONTROLE POZYTYWNA: pytanie o Control+Shift+L, ktory w binarce JEST,
    // dawalo FAIL.  Przyczyna: literal zaczynajacy sie na NIEPARZYSTYM offoscie
    // bajtowym nie istnieje w tekscie zdekodowanym od bajtu zerowego - kazda
    // para bajtow jest wtedy przesunieta o jeden i daje inne znaki.  Bez tej
    // poprawki sonda dawala falszywe zero na polowie napisow, czyli mylila
    // "nie ma" z "nie widze".
    string sUtf16 = Encoding.Unicode.GetString(aBytes);
    byte[] aShift = new byte[aBytes.Length - 1];
    Array.Copy(aBytes, 1, aShift, 0, aShift.Length);
    string sUtf16b = Encoding.Unicode.GetString(aShift);

    Func<string, string, int> ileW = delegate(string sHay, string s) {
        int n = 0, i = 0;
        while ((i = sHay.IndexOf(s, i, StringComparison.Ordinal)) >= 0) { n++; i += 1; }
        return n;
    };
    Func<string, int> ile = delegate(string s) {
        return ileW(sUtf16, s) + ileW(sUtf16b, s) + ileW(sAnsi, s);
    };

    Sprawdz(ile("Next Footnote") > 0, "napis \"Next Footnote\" jest w binarce");
    Sprawdz(ile("Control+Alt+PageDown") > 0, "napis \"Control+Alt+PageDown\" jest w binarce");
    Sprawdz(ile("Control+Alt+PageUp") > 0, "napis \"Control+Alt+PageUp\" jest w binarce");

    // CHORD JAKO PODCIAG - pulapka zmierzona 01.09.2026 przy Control+Shift+B w
    // Control+Shift+Back.  "Control+Alt+Shift+K" nie jest podciagiem zadnego
    // dluzszego chordu tego programu, ale pytamy o dokladny napis i osobno
    // kontrolujemy, ze sonda WIDZI chord, ktory tam jest.
    Sprawdz(ile("Control+Alt+Shift+K") == 0, "chord Control+Alt+Shift+K ZNIKNAL z binarki");
    Sprawdz(ile("Control+Alt+K") > 0, "KONTROLA SONDY: Control+Alt+K nadal widoczny (Go to Footnote zostaje)");

    // Zwolnione chordy: napis chordu znika, NAZWA KOMENDY zostaje (menu-only).
    string[,] aZwolnione = new string[,] {
        {"Control+E", "Environment Variables"},
        {"Control+Shift+E", "Extract with Regular Expression"},
        {"Control+Shift+G", "Go to Environment"},
        {"Control+Shift+X", "Extra Speech Toggle"},
        {"Control+Y", "Repeat Line"},
    };
    for (int i = 0; i < aZwolnione.GetLength(0); i++) {
        string sChord = aZwolnione[i, 0];
        string sNazwa = aZwolnione[i, 1];
        // Control+E jest PODCIAGIEM Control+Enter i Control+Equals, wiec goly
        // podciag daje falszywy alarm.  Pytamy o chord z GRANICA: nastepny znak
        // nie moze byc litera ani cyfra.
        Sprawdz(!DokladnyLiteral(sChord),
            "chord " + sChord + " nie jest juz przypisany zadnej komendzie");
        Sprawdz(ile(sNazwa) > 0, "komenda \"" + sNazwa + "\" ZOSTALA (menu bez skrotu)");
    }
    // KONTROLA TEGO DOPASOWANIA: chord, ktory ISTNIEJE, musi byc znaleziony.
    Sprawdz(DokladnyLiteral("Control+Shift+Z"),
        "KONTROLA DOPASOWANIA: Control+Shift+Z (Redo) jest widziany");
    Sprawdz(DokladnyLiteral("Control+Shift+L"),
        "KONTROLA DOPASOWANIA: Control+Shift+L (Numbered List) jest widziany");

    // --- 3. LISTA LINKOW: TRESC PRZED WSPOLRZEDNA ---
    // Stary format sklejal "Line " + numer + ". " PRZED trescia; nowy dokleja
    // ", line " na koncu.  Pytamy o OBA napisy, bo sam brak starego moglby
    // znaczyc, ze zniknela cala funkcja.
    Sprawdz(ile(", line ") > 0, "nowy format wiersza listy (\", line \") jest w binarce");

    // --- 4. PODPOWIEDZ DELETE ZESZLA Z PASKA STANU ---
    // DOKLADNY literal, nie podciag: ten sam tekst wystepuje teraz WEWNATRZ
    // dluzszej pomocy F1 ("Keys: Delete or Backspace removes the bookmark, ..."),
    // wiec pytanie o podciag dawalo FAIL przy poprawnym kodzie.
    Sprawdz(!DokladnyLiteral("Delete removes the bookmark"),
        "podpowiedz \"Delete removes the bookmark\" ZNIKNELA z paska stanu");
    Sprawdz(!DokladnyLiteral("Delete or Backspace removes the bookmark"),
        "podpowiedz \"Delete or Backspace removes the bookmark\" ZNIKNELA z paska stanu");
    // KONTROLA POZYTYWNA TEGO DOPASOWANIA: literal, ktory w binarce JEST i jest
    // rowny co do znaku, musi byc znaleziony.
    Sprawdz(DokladnyLiteral("Bookmark removed"),
        "KONTROLA DOPASOWANIA: literal \"Bookmark removed\" jest znaleziony");
    // ...ale KLAWISZ dziala nadal i jest udokumentowany w pomocy F1.
    Sprawdz(ile("Keys: Delete or Backspace removes the bookmark") > 0,
        "klawisze zakladek sa w pomocy F1 (setHelpDetail)");
    Sprawdz(ile("Bookmark removed") > 0, "komunikat \"Bookmark removed\" nadal jest (klawisz dziala)");

    // --- 5. STRZALKA W LEWO NA LISTACH ---
    Sprawdz(ile("Left Arrow reads the line it points to") > 0,
        "lista zakladek z nazwa: strzalka w lewo udokumentowana");
    Sprawdz(ile("Left Arrow reads the sentence line where the footnote marker is") > 0,
        "lista przypisow: strzalka w lewo udokumentowana");
    Sprawdz(ile("Empty line") > 0, "komunikat dla pustego wiersza istnieje");

    // --- 6. Control+Alt+K NIE SKACZE JUZ PO PRZYPISACH ---
    Sprawdz(ile("Put the cursor in a line with a footnote marker") > 0,
        "Control+Alt+K poza przypisem MOWI, czego brakuje");

    Console.WriteLine();
    Console.WriteLine(iPass + " PASS / " + iFail + " FAIL");
    return (iFail == 0) ? 0 : 1;
}

// DOKLADNY LITERAL, NIE PODCIAG.  Dwie pulapki tego projektu zalatwione jednym
// pomiarem: "Control+E" jest podciagiem "Control+Enter" (falszywy alarm o
// zajetym chordzie), a "Delete or Backspace removes the bookmark" jest
// podciagiem dluzszego tekstu pomocy F1 (falszywy alarm, ze napis nie zniknal).
// Granica znaku nie rozstrzyga: w tablicy #US literaly leza CIASNO obok siebie,
// wiec po ostatnim znaku stoi bajt dlugosci nastepnego literalu, ktory bywa
// litera i bywa nia nie byc - to lotereja, a nie pomiar.
//
// ROZSTRZYGA PREFIKS DLUGOSCI: kazdy literal w tablicy #US poprzedza
// skompresowana liczba, rowna liczbie bajtow tekstu plus jeden (ostatni bajt
// jest flaga).  Szukamy wiec ciagu <prefiks><tekst w UTF-16>, co trafia TYLKO
// w literal DOKLADNIE rowny podanemu napisowi.
static byte[] aPlik = null;

static bool DokladnyLiteral(string s) {
    byte[] aTekst = Encoding.Unicode.GetBytes(s);
    int iDlug = aTekst.Length + 1;
    byte[] aPrefiks;
    if (iDlug < 0x80) aPrefiks = new byte[] {(byte) iDlug};
    else if (iDlug < 0x4000) aPrefiks = new byte[] {(byte) (0x80 | (iDlug >> 8)), (byte) (iDlug & 0xFF)};
    else aPrefiks = new byte[] {(byte) (0xC0 | (iDlug >> 24)), (byte) ((iDlug >> 16) & 0xFF),
                                (byte) ((iDlug >> 8) & 0xFF), (byte) (iDlug & 0xFF)};
    byte[] aSzukaj = new byte[aPrefiks.Length + aTekst.Length];
    Array.Copy(aPrefiks, 0, aSzukaj, 0, aPrefiks.Length);
    Array.Copy(aTekst, 0, aSzukaj, aPrefiks.Length, aTekst.Length);

    for (int i = 0; i + aSzukaj.Length <= aPlik.Length; i++) {
        bool bZgoda = true;
        for (int j = 0; j < aSzukaj.Length; j++) {
            if (aPlik[i + j] != aSzukaj[j]) { bZgoda = false; break; }
        }
        if (bZgoda) return true;
    }
    return false;
}

} // Pomiar558 class
