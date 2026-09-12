// POMIAR 5.0.70: WKLEJANIE SKOPIOWANYCH PLIKOW (format CF_HDROP).
//
// JEGO ZGLOSZENIE 05.09.2026: "Kopiowanie wielu elementow dziala, ale pliki nie
// chca sie wklejac. Czy to wina EdSharpa czy mojego schowka i programu typu
// Ditto, nie wiem." oraz "Tak. Sprawdzalem i 1 plik i zaznaczonych wiecej i
// Control+Shift+C."
//
// ZMIERZONA PRZYCZYNA PO NASZEJ STRONIE, nie w jego schowku: PickFileCopySelection
// kladlo na schowek WYLACZNIE tekst (Util.SetClipboardText).  Eksplorator Windows
// wkleja plik tylko z formatu CF_HDROP (DataFormats.FileDrop) z lista sciezek,
// wiec nie dzialalo to takze przy JEDNEJ pozycji - zgodnie z tym, co napisal.
//
// Rodziny asercji:
//   1. Util.SetClipboardFileDrop istnieje, sklada CF_HDROP, ustawia
//      "Preferred DropEffect" i idzie droga Z PONOWIENIAMI (SetClipboardData).
//   2. Kopiowanie sciezek z listy plikow WOLA ten nosnik i sprawdza ISTNIENIE
//      pliku na dysku (martwy wpis w CF_HDROP dalby blad powloki przy wklejaniu).
//   3. Kopiowanie NAZW (Control+Shift+C) formatu plikowego NIE dostaje - nazwa
//      bez katalogu nie wskazuje pliku.  Ta asercja jest ROZLACZNOSCIA: bez niej
//      "wrzuc CF_HDROP zawsze" tez byloby zielone.
//   4. Mowa rozroznia trzy skutki: pliki skopiowane, czesc wpisow martwa,
//      zaden plik nie istnieje.  Cisza w tych miejscach znaczylaby dla
//      niewidomego "skopiowalem", przy pustym wklejeniu.
//
// Pomiar po SYGNATURACH i CIALACH METOD (ldstr 0x72 + ResolveString,
// call/callvirt/newobj + ResolveMember), nie po napisach w pliku.
//
// Uruchomienie:
//   csc.exe /nologo /target:exe "/out:D:\projekty\edsharp-pr\testy\out_570.exe" \
//           "/r:D:\projekty\edsharp-pr\EdSharpNG.exe" \
//           "D:\projekty\edsharp-pr\testy\pomiar_570.cs"
//   cmd.exe /c "testy\out_570.exe EdSharpNG.exe"
//
// KONTROLA NEGATYWNA: testy/kontrola_negatywna_570.sh na binarce 5.0.69.
//
// ZMIANA DECYZJI 13.09.2026 (5.0.95), jego slowami: "zgodzilem sie na 1 skrot,
// to byl jednak blad".  Do 5.0.94 nazwe pliku kopiowal Control+Shift+C, potem
// (5.0.74) ten klawisz byl zwolniony, a Control+C robil wszystko.  Teraz wraca
// rozroznienie, ale ODWROTNIE niz pierwotnie:
//   Control+C       -> sama NAZWA pliku
//   Control+Shift+C -> PELNA SCIEZKA + plik (CF_HDROP)
// Asercje tej sondy pytaja o TRYBY nosnika ("names" / "files"), nie o klawisze,
// wiec pilnuja rozlacznosci formatu plikowego nadal poprawnie.  Nie oslabiaj
// ich - to one broni tego, zeby CF_HDROP nie lecial przy kopiowaniu nazwy.

using System;
using System.IO;
using System.Reflection;

class Pomiar570 {

static int iOk = 0;
static int iZle = 0;

static void Sprawdz(bool bWarunek, string sOpis) {
    if (bWarunek) { iOk++; Console.WriteLine("OK   " + sOpis); }
    else { iZle++; Console.WriteLine("ZLE  " + sOpis); }
}

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

static bool MaFragment(MethodBase mb, string sFragment) {
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
        if (s != null && s.IndexOf(sFragment, StringComparison.Ordinal) >= 0) return true;
    }
    return false;
}

static bool WolaMetode(MethodBase mb, string sNazwa) {
    if (mb == null) return false;
    MethodBody body = null;
    try { body = mb.GetMethodBody(); } catch { return false; }
    if (body == null) return false;
    byte[] aIl = body.GetILAsByteArray();
    if (aIl == null) return false;
    Module mod = mb.Module;
    Type[] aGenT = null, aGenM = null;
    try {
        if (mb.DeclaringType != null && mb.DeclaringType.IsGenericType) aGenT = mb.DeclaringType.GetGenericArguments();
        if (mb.IsGenericMethod) aGenM = mb.GetGenericArguments();
    } catch {}
    for (int i = 0; i + 4 < aIl.Length; i++) {
        if (aIl[i] != 0x28 && aIl[i] != 0x6F && aIl[i] != 0x73) continue;
        int iToken = BitConverter.ToInt32(aIl, i + 1);
        MemberInfo mi = null;
        try { mi = mod.ResolveMember(iToken, aGenT, aGenM); } catch { continue; }
        if (mi != null && mi.Name == sNazwa) return true;
    }
    return false;
}

static MethodInfo Metoda(Type t, string sNazwa) {
    if (t == null) return null;
    return t.GetMethod(sNazwa, BindingFlags.Instance | BindingFlags.Static
                             | BindingFlags.Public | BindingFlags.NonPublic);
}

static Type Typ(Assembly asm, string sNazwa) {
    foreach (Type t in asm.GetTypes()) if (t.Name == sNazwa) return t;
    return null;
}

static void Main(string[] aArgs) {
    string sBin = (aArgs.Length > 0) ? aArgs[0] : "EdSharpNG.exe";
    Assembly asm = Assembly.LoadFile(Path.GetFullPath(sBin));
    Console.WriteLine("Binarka: " + asm.Location);
    Console.WriteLine();

    Type tUtil = Typ(asm, "Util");
    Type tDialogEd = Typ(asm, "Dialog");
    Sprawdz(tUtil != null, "typ Util znaleziony w binarce");
    Sprawdz(tDialogEd != null, "typ Dialog znaleziony w binarce");
    if (tUtil == null || tDialogEd == null) { Podsumuj(); return; }

    // ---------- 1. NOSNIK SCHOWKA PLIKOW ----------
    // BRAK NOSNIKA NIE PRZERYWA POMIARU.  Wczesniejsza wersja robila tu
    // "return", wiec na binarce SPRZED zmiany sonda konczyla po jednej oblanej
    // asercji i dwie pozostale rodziny nie byly mierzone WCALE - kontrola
    // negatywna nie mogla wtedy pokazac, ze sonda je rozroznia.  Helpery
    // (MaLiteral, WolaMetode) na null zwracaja false, wiec dalsze asercje
    // oblewaja poprawnie, zamiast rzucac wyjatkiem.
    MethodInfo miDrop = Metoda(tUtil, "SetClipboardFileDrop");
    Sprawdz(miDrop != null, "Util.SetClipboardFileDrop istnieje (schowek PLIKOW, nie sam tekst)");
    Sprawdz(miDrop != null && miDrop.ReturnType == typeof(bool),
            "SetClipboardFileDrop zwraca bool, wiec wolajacy MOZE powiedziec o niepowodzeniu");
    Sprawdz(miDrop != null && miDrop.GetParameters().Length == 2,
            "SetClipboardFileDrop bierze liste sciezek ORAZ tekst (rownolegle formaty)");
    Sprawdz(WolaMetode(miDrop, "SetFileDropList"),
            "nosnik sklada format CF_HDROP (SetFileDropList) - tego Eksplorator wymaga do wklejenia pliku");
    Sprawdz(MaLiteral(miDrop, "Preferred DropEffect"),
            "nosnik ustawia Preferred DropEffect, wiec wklejenie KOPIUJE, a nie przenosi plik");
    Sprawdz(WolaMetode(miDrop, "SetClipboardData"),
            "zapis idzie przez Util.SetClipboardData, czyli droga Z PONOWIENIAMI (menedzer schowka)");
    Sprawdz(!WolaMetode(miDrop, "SetDataObject"),
            "nosnik NIE wola Clipboard.SetDataObject wprost (to obeszloby ponowienia)");
    Sprawdz(WolaMetode(miDrop, "SetData"),
            "obok plikow do schowka idzie TEKST ze sciezkami (wklejanie do dokumentu jak dotad)");

    // ---------- 2. KOPIOWANIE SCIEZEK WOLA NOSNIK I SPRAWDZA DYSK ----------
    MethodInfo miKopiuj = Metoda(tDialogEd, "PickFileCopySelection");
    Sprawdz(miKopiuj != null, "Dialog.PickFileCopySelection istnieje");
    Sprawdz(WolaMetode(miKopiuj, "SetClipboardFileDrop"),
            "kopiowanie z listy plikow WOLA schowek plikow (nosnik nie jest martwy)");
    Sprawdz(WolaMetode(miKopiuj, "Exists"),
            "kopiowanie sprawdza, czy sciezka ISTNIEJE na dysku (martwy wpis w CF_HDROP = blad powloki)");
    Sprawdz(WolaMetode(miKopiuj, "get_SelectedIndices"),
            "REGRESJA 5.0.69: kopiowanie nadal czyta WSZYSTKIE zaznaczone pozycje");
    Sprawdz(WolaMetode(miKopiuj, "SetClipboardText"),
            "REGRESJA 5.0.69: droga tekstowa (nazwy, dopisanie) nadal idzie przez wersje z ponowieniami");

    // ---------- 3. ROZLACZNOSC: NAZWY BEZ FORMATU PLIKOWEGO ----------
    // Bez tej asercji "wrzucaj CF_HDROP zawsze" tez byloby zielone, a nazwa
    // pliku bez katalogu nie wskazuje niczego, co powloka mogla by wkleic.
    // Galaz plikowa musi wiec byc WARUNKOWA, nie bezwarunkowa.
    //
    // ODWROCONE 13.09.2026 (5.0.95) - patrz naglowek pliku: do 5.0.94 nazwe
    // kopiowal Control+Shift+C, teraz robi to Control+C, a Control+Shift+C
    // kopiuje sciezke.  Asercje mowia o TRYBACH nosnika ("names"/"files"), a nie
    // o klawiszach, wiec pilnuja rozlacznosci niezaleznie od przypisania
    // klawiszy - i to one zostaja.  Osobno, ponizej, pytamy o same klawisze.
    Sprawdz(MaLiteral(miKopiuj, "path") && MaLiteral(miKopiuj, "name"),
            "komunikat nadal rozroznia SCIEZKE od NAZWY (dwa klawisze, dwa skutki)");
    Sprawdz(MaLiteral(miKopiuj, "names") && MaLiteral(miKopiuj, "paths"),
            "mnoga forma komunikatu tez rozroznia NAZWY od SCIEZEK");
    Sprawdz(MaFragment(miKopiuj, " as text"),
            "jest osobna droga TEKSTOWA dla wpisow, ktorych na dysku nie ma");
    // TRYB "names" MUSI BYC ROZPOZNAWANY W NOSNIKU.  Bez tego literalu nosnik
    // nie ma jak odroznic kopiowania nazwy od kopiowania sciezki i caly powrot
    // do dwoch klawiszy byl by pozorny przy zielonym buildzie.
    Sprawdz(MaLiteral(miKopiuj, "names"),
            "nosnik rozpoznaje tryb \"names\" (kopiowanie samej nazwy pliku)");
    Sprawdz(WolaMetode(miKopiuj, "GetFileName"),
            "nazwa jest WYCINANA ZE SCIEZKI (Path.GetFileName), nie brana z wiersza listy");

    // ---------- 4. MOWA ROZROZNIA TRZY SKUTKI ----------
    // ODWROCONE 13.09.2026 (5.0.95).  Ta asercja pilnowala napisu "File copied",
    // ktorego uzytkownik sam sie pozbyl 11.09.2026: "trocha mylaco mowi Copied
    // file (...) powinien mowic Copied po prostu".  Slowo "file" nazywalo FORMAT
    // schowka, nie skutek - a na schowku leza oba formaty naraz.  Asercja
    // oblewala wiec na kodzie POPRAWNYM juz w 5.0.94 (zmierzone: literalu "File
    // copied" nie ma ani w 5.0.94, ani w 5.0.95) i zaslaniala prawdziwe regresje.
    // Pytamy teraz o to, czego chcemy: skopiowanie POJEDYNCZEJ pozycji melduje
    // sie slowem "Copied" bez nazywania formatu.
    Sprawdz(MaFragment(miKopiuj, "Copied"),
            "skopiowanie melduje sie slowem \"Copied\" (bez nazywania formatu schowka)");
    Sprawdz(MaFragment(miKopiuj, "files"),
            "skopiowanie wielu plikow mowi LICZBE plikow");
    Sprawdz(MaFragment(miKopiuj, "missing"),
            "gdy czesc wpisow nie istnieje, program MOWI ILE (inaczej wkleja sie mniej bez sygnalu)");
    // ODWROCONE 13.09.2026 (5.0.95), ta sama rodzina co asercja o "File copied"
    // wyzej.  Komunikat brzmial "not found", a zostal przepisany na "file no
    // longer on disk" / "files no longer on disk" - napis mowiacy uzytkownikowi,
    // CO SIE STALO z plikiem, a nie tylko ze go nie znaleziono.  Zmierzone: "not
    // found" nie ma w tej metodzie ani w 5.0.94, ani w 5.0.95, wiec asercja
    // oblewala na kodzie poprawnym.  Pytamy o obecny napis.
    Sprawdz(MaFragment(miKopiuj, "no longer on disk"),
            "gdy wpisu nie ma na dysku, program MOWI to wprost (sciezka idzie sama, jako tekst)");
    Sprawdz(MaFragment(miKopiuj, "Clipboard is busy"),
            "nieudany zapis nadal MOWI o niepowodzeniu");

    // ---------- KONTROLA POZYTYWNA SONDY ----------
    Sprawdz(MaLiteral(miKopiuj, "No item!"),
            "KONTROLA SONDY: MaLiteral widzi literal, ktory w tej metodzie JEST");
    Sprawdz(!MaLiteral(miKopiuj, "TegoNapisuTamNieMaWcale"),
            "KONTROLA SONDY: MaLiteral NIE widzi literalu, ktorego nie ma");
    Sprawdz(!WolaMetode(miDrop, "NieMaTakiejMetodyWcale"),
            "KONTROLA SONDY: WolaMetode NIE widzi metody, ktorej nie ma");

    Podsumuj();
}

static void Podsumuj() {
    Console.WriteLine();
    Console.WriteLine("PODSUMOWANIE: " + iOk + " OK, " + iZle + " ZLE");
}

} // Pomiar570
