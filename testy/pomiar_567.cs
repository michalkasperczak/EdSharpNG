// POMIAR 5.0.67: numer wiersza SCHODZI z pozycji listy odsylaczy do
// dopowiedzenia strzalka w lewo (zgloszenie Kasperczaka 04.09.2026, DRUGIE
// w tej sprawie po 01.09).
//
// Pomiar idzie po CIALACH METOD, nie po napisach w pliku: literal ", line "
// siedzi w binarce takze wtedy, gdy nikt go nie uzywa, a kompilator scala
// identyczne literaly (pulapka z references/pulapki-interopu-i-falszywych-zer.md).
// Rozstrzyga wiec: ktora metoda laduje ktory literal (opkod ldstr 0x72 +
// ResolveString) i jaka sygnature ma metoda dopowiedzenia.
//
// Uruchomienie:
//   csc.exe /nologo /target:exe "/out:D:\projekty\edsharp-pr\testy\out_567.exe" \
//           "/r:D:\projekty\edsharp-pr\EdSharpNG.exe" \
//           "D:\projekty\edsharp-pr\testy\pomiar_567.cs"
//   cmd.exe /c "testy\out_567.exe EdSharpNG.exe"
//
// KONTROLA NEGATYWNA: ta sama sonda na binarce 5.0.66 MUSI dac inny wynik
// (tam ShowMarkdownLinkList laduje ", line ", a GetMarkdownLinkAddressSpeech
// ma jeden parametr).  Bez tego zielony wynik nie odroznia naprawy od
// gluchej sondy.

using System;
using System.IO;
using System.Reflection;

class Pomiar567 {

static int iOk = 0;
static int iZle = 0;

static void Sprawdz(bool bWarunek, string sOpis) {
    if (bWarunek) { iOk++; Console.WriteLine("OK   " + sOpis); }
    else { iZle++; Console.WriteLine("ZLE  " + sOpis); }
}

// Czy cialo metody laduje DOKLADNIE ten literal (opkod ldstr = 0x72,
// po nim 4-bajtowy token napisu).
static bool MaLiteral(MethodBase mb, string sSzukany) {
    if (mb == null) return false;
    MethodBody body = null;
    try { body = mb.GetMethodBody(); } catch { return false; }
    if (body == null) return false;
    byte[] aIl = body.GetILAsByteArray();
    if (aIl == null) return false;
    Module mod = mb.Module;
    for (int i = 0; i + 4 < aIl.Length; i++) {
        if (aIl[i] != 0x72) continue;
        int iToken = BitConverter.ToInt32(aIl, i + 1);
        string s = null;
        try { s = mod.ResolveString(iToken); } catch { continue; }
        if (s == sSzukany) return true;
    }
    return false;
}

static MethodInfo Metoda(Type t, string sNazwa) {
    return t.GetMethod(sNazwa, BindingFlags.Instance | BindingFlags.Static
                             | BindingFlags.Public | BindingFlags.NonPublic);
}

static void Main(string[] aArgs) {
    string sBin = (aArgs.Length > 0) ? aArgs[0] : "EdSharpNG.exe";
    // LoadFile, nie LoadFrom: LoadFrom dla dwoch plikow o tej samej tozsamosci
    // oddaje TEN SAM obiekt, wiec kontrola negatywna pytalaby nowa binarke
    // (pulapka 12 z references/pulapki-sond-i-kompilacji-z-wsl.md).
    Assembly asm = Assembly.LoadFile(Path.GetFullPath(sBin));
    Console.WriteLine("Binarka: " + asm.Location);
    Console.WriteLine();

    Type tChild = null;
    // TYP TO MdiFrame, NIE MdiChild.  Pierwsza wersja tej sondy pytala o
    // MdiChild i dostala "metoda nie istnieje" dla CZTERECH metod naraz przy
    // typie znalezionym - czyli falszywe zero z patrzenia w zly typ, nie brak
    // kodu.  Rozstrzygnela diagnostyka testy/gdzie_567.cs, ktora wypisala
    // wprost: ShowMarkdownLinkList -> EdSharp.MdiFrame.  Liczenie klamer w
    // zrodle potwierdza: linia 11469 lezy w MdiFrame zagniezdzonym w MdiChild.
    // REGULA: gdy kilka asercji o ISTNIENIU pada naraz, podejrzewaj sonde,
    // nie kod.
    foreach (Type t in asm.GetTypes()) {
        if (t.Name == "MdiFrame") { tChild = t; break; }
    }
    Sprawdz(tChild != null, "typ MdiFrame znaleziony w binarce");
    if (tChild == null) { Podsumuj(); return; }

    MethodInfo miLista = Metoda(tChild, "ShowMarkdownLinkList");
    MethodInfo miAdres = Metoda(tChild, "GetMarkdownLinkAddressSpeech");
    MethodInfo miWiersz = Metoda(tChild, "GetMarkdownLinkListLine");
    MethodInfo miNumer = Metoda(tChild, "GetTextLineNumberAtIndex");

    Sprawdz(miLista != null, "metoda ShowMarkdownLinkList istnieje");
    Sprawdz(miAdres != null, "metoda GetMarkdownLinkAddressSpeech istnieje");
    Sprawdz(miWiersz != null, "metoda GetMarkdownLinkListLine istnieje");
    Sprawdz(miNumer != null, "metoda GetTextLineNumberAtIndex istnieje (nie osierocona)");
    if (miLista == null || miAdres == null) { Podsumuj(); return; }

    // ISTOTA ZGLOSZENIA: metoda budujaca WIERSZE listy nie moze juz laadowac
    // literalu ", line " - to jego wymawiane przy kazdej pozycji.
    Sprawdz(!MaLiteral(miLista, ", line "),
            "ShowMarkdownLinkList NIE laduje literalu \", line \" (istota zgloszenia)");

    // A dopowiedzenie strzalka w lewo MA go laadowac - inaczej wspolrzedna
    // znikla by calkiem, a to nie bylo zadaniem.
    Sprawdz(MaLiteral(miAdres, ", line "),
            "GetMarkdownLinkAddressSpeech laduje \", line \" (wspolrzedna przeniesiona, nie skasowana)");

    // Sygnatura nosnika: dopowiedzenie musi PRZYJMOWAC numer wiersza.
    // Bez tej asercji ktos moglby dolozyc literal, ktory nigdy nie dostaje liczby.
    bool bDwaParam = false, bIntDrugi = false;
    if (miAdres != null) {
        ParameterInfo[] aP = miAdres.GetParameters();
        bDwaParam = (aP.Length == 2);
        bIntDrugi = bDwaParam && (aP[1].ParameterType == typeof(int));
    }
    Sprawdz(bDwaParam, "GetMarkdownLinkAddressSpeech ma DWA parametry (link, numer wiersza)");
    Sprawdz(bIntDrugi, "drugi parametr GetMarkdownLinkAddressSpeech jest typu int");

    // Wiersz listy zostaje sama trescia - ta metoda nigdy nie miala wspolrzednej
    // i nadal nie moze jej miec.
    Sprawdz(!MaLiteral(miWiersz, ", line "),
            "GetMarkdownLinkListLine nie laduje \", line \" (wiersz to sama tresc)");

    // KONTROLA POZYTYWNA SONDY: gdyby MaLiteral zawsze zwracalo false, wszystkie
    // asercje o BRAKU przechodzilyby przez przypadek.  Literal MUSI lezec w
    // CIELE TEJ METODY, a nie w domknieciu: pierwsza wersja tej kontroli pytala
    // o "Copied as Markdown", ktory siedzi w anonimowej funkcji obslugi
    // klawiszy (lst.KeyDown += delegate...).  Kompilator przenosi cialo takiej
    // funkcji do OSOBNEJ metody generowanej, wiec ShowMarkdownLinkList tego
    // literalu nie laduje - kontrola oblewala przy poprawnym kodzie i przy
    // sprawnej sondzie.  "No links!" jest w ciele wprost (galaz zerowej liczby
    // odsylaczy), wiec rozstrzyga o widocznosci literalow tej metody.
    // REGULA: pytajac o literal metody, sprawdz najpierw, czy nie stoi on
    // wewnatrz lambdy albo delegata.
    Sprawdz(MaLiteral(miLista, "No links!"),
            "KONTROLA SONDY: MaLiteral widzi literal, ktory w ciele tej metody JEST");

    // Pomoc w okienku musi mowic o numerze wiersza, bo inaczej program robi
    // jedno, a Ctrl+F1 czyta drugie.
    Sprawdz(MaLiteral(miLista, "Keys: Left Arrow reads the web address and the line number, Control+C copies the link as Markdown, Control+Shift+C copies it as a formatted link, F2 edits the title and address, Enter goes to the link in the text, Escape closes the list."),
            "podpowiedz okienka mowi o adresie ORAZ numerze wiersza");

    Podsumuj();
}

static void Podsumuj() {
    Console.WriteLine();
    Console.WriteLine("PODSUMOWANIE: " + iOk + " OK, " + iZle + " ZLE");
}

} // Pomiar567
