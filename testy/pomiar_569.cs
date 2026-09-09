// POMIAR 5.0.69: wielokrotny wybor i kopiowanie NA LISCIE PLIKOW (Alt+L, Alt+R).
//
// JEGO ZGLOSZENIE 05.09.2026: "Nie dziala zaznaczanie i kopiowanie wielu
// Control+C i nie dziala Control+Shift+C na listach, Alt+L to na pewno
// sprawdzilem. To pliki i sciezki."
//
// Zgloszenie bylo trafne i kod je potwierdzal: w 5.0.68 lista plikow byla
// z wielokrotnego wyboru JAWNIE wylaczona (lst.SelectionMode = SelectionMode.One),
// a jej wlasna obsluga Control+C czytala TYLKO lb.SelectedIndex, czyli jedna
// pozycje.  Control+Shift+C na tej liscie nie istnial wcale.
//
// Rodziny asercji:
//   1. Lista plikow NIE cofa juz trybu do jednowyborowego.
//   2. Kopiowanie na liscie plikow czyta WSZYSTKIE zaznaczone pozycje
//      (SelectedIndices) i idzie przez jeden nosnik PickFileCopySelection.
//   3. Trzy klawisze kopiowania (Control+C sciezki, Control+Shift+C nazwy,
//      Alt+C dopisanie) sa obsluzone w liscie plikow ORAZ oddane jej przez
//      domyslna obsluge Lbc (inaczej schowek dostawalby dwa zapisy).
//   4. Klawisze NISZCZACE (Delete, Shift+Delete) zostaja przy JEDNEJ pozycji
//      i przy kilku zaznaczonych ODMAWIAJA z powiedzeniem, ile jest zaznaczonych.
//
// Pomiar idzie po SYGNATURACH i po CIALACH METOD (ldstr 0x72 + ResolveString,
// call/callvirt/newobj + ResolveMember), nie po napisach w pliku: literal siedzi
// w binarce takze wtedy, gdy nikt go nie uzywa.
//
// Uruchomienie:
//   csc.exe /nologo /target:exe "/out:D:\projekty\edsharp-pr\testy\out_569.exe" \
//           "/r:D:\projekty\edsharp-pr\EdSharpNG.exe" \
//           "D:\projekty\edsharp-pr\testy\pomiar_569.cs"
//   cmd.exe /c "testy\out_569.exe EdSharpNG.exe"
//
// KONTROLA NEGATYWNA: testy/kontrola_negatywna_569.sh na binarce 5.0.68 MUSI
// oblac czesc asercji w KAZDEJ rodzinie - inaczej zielony wynik nie odroznia
// naprawy od gluchej sondy.

using System;
using System.IO;
using System.Reflection;

class Pomiar569 {

static int iOk = 0;
static int iZle = 0;

static void Sprawdz(bool bWarunek, string sOpis) {
    if (bWarunek) { iOk++; Console.WriteLine("OK   " + sOpis); }
    else { iZle++; Console.WriteLine("ZLE  " + sOpis); }
}

// Czy cialo metody laduje DOKLADNIE ten literal (ldstr = 0x72, potem token).
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

// Czy cialo metody laduje literal ZAWIERAJACY podany fragment.  Komunikaty
// dla uzytkownika skladane sa z liczby i tekstu, wiec pelnego napisu w IL nie ma.
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

// Czy cialo metody WOLA metode o tej nazwie.
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

// Czy cialo metody laduje TE STALA liczbowa (ldc.i4 = 0x20 + int32, oraz
// krotkie formy ldc.i4.s = 0x1F + bajt i ldc.i4.0..8 = 0x16..0x1E).
// Uzywane do rozpoznania KTORY chord jest sprawdzany: Keys.Control|Keys.C to
// jedna liczba, Keys.Control|Keys.Shift|Keys.C inna.
static bool MaStala(MethodBase mb, int iWartosc) {
    if (mb == null) return false;
    MethodBody body = null;
    try { body = mb.GetMethodBody(); } catch { return false; }
    if (body == null) return false;
    byte[] aIl = body.GetILAsByteArray();
    if (aIl == null) return false;
    for (int i = 0; i < aIl.Length; i++) {
        if (aIl[i] == 0x20 && i + 4 < aIl.Length) {
            if (BitConverter.ToInt32(aIl, i + 1) == iWartosc) return true;
        }
        else if (aIl[i] == 0x1F && i + 1 < aIl.Length) {
            if ((sbyte)aIl[i + 1] == iWartosc) return true;
        }
        else if (aIl[i] >= 0x16 && aIl[i] <= 0x1E) {
            if (aIl[i] - 0x16 == iWartosc) return true;
        }
    }
    return false;
}

// Ile razy cialo metody laduje TE stala liczbowa.  Potrzebne tam, gdzie sama
// OBECNOSC chordu nie rozroznia stanu przed i po: handleListBoxCopyKeys znala
// Control+Shift+C juz w 5.0.68, bo oddawala go LISCIE ODSYLACZY - dopiero
// DRUGIE wystapienie znaczy, ze oddaje go TAKZE liscie plikow.
static int LiczStala(MethodBase mb, int iWartosc) {
    if (mb == null) return 0;
    MethodBody body = null;
    try { body = mb.GetMethodBody(); } catch { return 0; }
    if (body == null) return 0;
    byte[] aIl = body.GetILAsByteArray();
    if (aIl == null) return 0;
    int iIle = 0;
    for (int i = 0; i < aIl.Length; i++) {
        if (aIl[i] == 0x20 && i + 4 < aIl.Length) {
            if (BitConverter.ToInt32(aIl, i + 1) == iWartosc) iIle++;
        }
        else if (aIl[i] == 0x1F && i + 1 < aIl.Length) {
            if ((sbyte)aIl[i + 1] == iWartosc) iIle++;
        }
        else if (aIl[i] >= 0x16 && aIl[i] <= 0x1E) {
            if (aIl[i] - 0x16 == iWartosc) iIle++;
        }
    }
    return iIle;
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

// Wszystkie metody typu RAZEM Z GENEROWANYMI (domkniecia lambd stoja w
// osobnych metodach <Nazwa>b__N, a cialo obslugi klawiszy listy plikow to
// wlasnie domkniecie przypisane do lst.KeyDown).  Pulapka zmierzona przy
// 5.0.68: pytanie o cialo metody macierzystej daje FALSZYWE ZERO.
static MethodBase[] MetodyZGenerowanymi(Type t) {
    if (t == null) return new MethodBase[0];
    System.Collections.Generic.List<MethodBase> l = new System.Collections.Generic.List<MethodBase>();
    BindingFlags bf = BindingFlags.Instance | BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    foreach (MethodInfo mi in t.GetMethods(bf)) l.Add(mi);
    foreach (ConstructorInfo ci in t.GetConstructors(bf)) l.Add(ci);
    foreach (Type tNested in t.GetNestedTypes(bf)) {
        foreach (MethodInfo mi in tNested.GetMethods(bf)) l.Add(mi);
        foreach (ConstructorInfo ci in tNested.GetConstructors(bf)) l.Add(ci);
    }
    return l.ToArray();
}

static void Main(string[] aArgs) {
    string sBin = (aArgs.Length > 0) ? aArgs[0] : "EdSharpNG.exe";
    // LoadFile, nie LoadFrom: LoadFrom dla dwoch plikow o tej samej tozsamosci
    // oddaje TEN SAM obiekt, wiec kontrola negatywna pytalaby nowa binarke.
    Assembly asm = Assembly.LoadFile(Path.GetFullPath(sBin));
    Console.WriteLine("Binarka: " + asm.Location);
    Console.WriteLine();

    Type tDialogEd = Typ(asm, "Dialog");
    Type tLbcDialog = Typ(asm, "LbcDialog");
    Sprawdz(tDialogEd != null, "typ Dialog znaleziony w binarce");
    Sprawdz(tLbcDialog != null, "typ LbcDialog znaleziony w binarce");
    if (tDialogEd == null || tLbcDialog == null) { Podsumuj(); return; }

    MethodInfo miPickFile = Metoda(tDialogEd, "PickFile");
    Sprawdz(miPickFile != null, "Dialog.PickFile istnieje (lista plikow Alt+L / Alt+R)");
    if (miPickFile == null) { Podsumuj(); return; }

    // ---------- 1. LISTA PLIKOW BIERZE WIELOKROTNY WYBOR ----------
    // Istota zgloszenia: w 5.0.68 ta lista JAWNIE cofala tryb do jednowyborowego,
    // wiec Shift nie zaznaczal na niej nic.
    Sprawdz(!WolaMetode(miPickFile, "set_SelectionMode"),
            "PickFile NIE ustawia juz trybu zaznaczania (nie cofa listy do jednowyborowej)");
    // Tryb dziedziczy po addListBox, ktory stawia MultiExtended dla kazdej listy.
    MethodInfo miDodajListe = null;
    foreach (MethodInfo mi in tLbcDialog.GetMethods(BindingFlags.Instance | BindingFlags.Public
                                                  | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
        if (mi.Name != "addListBox") continue;
        ParameterInfo[] aP = mi.GetParameters();
        if (aP.Length == 3 && aP[0].ParameterType != typeof(string)) { miDodajListe = mi; break; }
    }
    Sprawdz(miDodajListe != null && WolaMetode(miDodajListe, "set_SelectionMode"),
            "addListBox nadal USTAWIA tryb wielokrotny, wiec lista plikow go dziedziczy");

    // ---------- 2. NOSNIK KOPIOWANIA ----------
    MethodInfo miKopiuj = Metoda(tDialogEd, "PickFileCopySelection");
    Sprawdz(miKopiuj != null, "Dialog.PickFileCopySelection istnieje (jeden nosnik kopiowania z listy plikow)");
    Sprawdz(miKopiuj != null && miKopiuj.GetParameters().Length == 5,
            "PickFileCopySelection bierze 5 parametrow (lista, sciezki, nazwy, co kopiowac, czy dopisac)");
    Sprawdz(WolaMetode(miKopiuj, "get_SelectedIndices"),
            "kopiowanie czyta WSZYSTKIE zaznaczone pozycje (SelectedIndices), nie sama biezaca");
    Sprawdz(WolaMetode(miKopiuj, "Join"),
            "zaznaczone pozycje sa skladane w jeden blok (string.Join), po jednej w wierszu");
    Sprawdz(WolaMetode(miKopiuj, "SetClipboardText"),
            "zapis idzie przez Util.SetClipboardText, czyli droga Z PONOWIENIAMI");
    Sprawdz(!WolaMetode(miKopiuj, "SetText"),
            "kopiowanie NIE wola Clipboard.SetText wprost (bez ponowien)");
    Sprawdz(WolaMetode(miKopiuj, "GetClipboardText"),
            "dopisanie do schowka najpierw CZYTA schowek droga z ponowieniami");
    // Liczba pozycji w komunikacie: niewidomy nie widzi podswietlenia.
    Sprawdz(MaFragment(miKopiuj, " items"),
            "komunikat MOWI LICZBE zabranych pozycji (podswietlenia nie slychac)");
    Sprawdz(MaLiteral(miKopiuj, "path") && MaLiteral(miKopiuj, "name"),
            "komunikat rozroznia SCIEZKE od NAZWY (dwa rozne klawisze, dwa rozne skutki)");
    Sprawdz(MaFragment(miKopiuj, "Clipboard is busy"),
            "nieudany zapis MOWI o niepowodzeniu (klawisz nie brzmi tak samo przy porazce)");

    // ---------- 3. TRZY KLAWISZE KOPIOWANIA W LISCIE PLIKOW ----------
    // Cialo obslugi klawiszy to DOMKNIECIE przypisane do lst.KeyDown, wiec
    // szukamy w metodach generowanych typu Dialog, nie w ciele PickFile.
    const int KEY_C = 67;
    const int CTRL = 0x20000, SHIFT = 0x10000, ALT = 0x40000;
    bool bCtrlC = false, bCtrlShiftC = false, bAltC = false, bWolaNosnik = false;
    int iWolan = 0;
    foreach (MethodBase mb in MetodyZGenerowanymi(tDialogEd)) {
        if (!WolaMetode(mb, "PickFileCopySelection")) continue;
        bWolaNosnik = true;
        iWolan++;
        if (MaStala(mb, CTRL | KEY_C)) bCtrlC = true;
        if (MaStala(mb, CTRL | SHIFT | KEY_C)) bCtrlShiftC = true;
        if (MaStala(mb, ALT | KEY_C)) bAltC = true;
    }
    Sprawdz(bWolaNosnik, "obsluga klawiszy listy plikow WOLA PickFileCopySelection (nosnik nie jest martwy)");
    Sprawdz(bCtrlC, "lista plikow rozpoznaje Control+C (kod " + (CTRL | KEY_C) + ")");
    Sprawdz(bCtrlShiftC, "lista plikow rozpoznaje Control+Shift+C (kod " + (CTRL | SHIFT | KEY_C) + ") - tego klawisza NIE BYLO");
    Sprawdz(bAltC, "lista plikow rozpoznaje Alt+C (kod " + (ALT | KEY_C) + ")");

    // Domyslna obsluga Lbc MUSI oddac wszystkie trzy klawisze liscie plikow,
    // inaczej schowek dostaje dwa zapisy i program mowi dwa rozne komunikaty.
    // Pytanie o samo ISTNIENIE chordu w tej metodzie NIE rozroznia stanu przed
    // i po: Control+Shift+C oraz Alt+C stoja tam takze w 5.0.68 (jeden po to,
    // by oddac go LISCIE ODSYLACZY, drugi to wlasna obsluga dopisywania).
    // Rozstrzyga LICZBA wystapien - drugie znaczy galaz listy plikow.
    MethodInfo miKopiujLbc = Metoda(tLbcDialog, "handleListBoxCopyKeys");
    Sprawdz(miKopiujLbc != null, "LbcDialog.handleListBoxCopyKeys istnieje");
    Sprawdz(MaLiteral(miKopiujLbc, "edsharp-filelist"),
            "domyslna obsluga rozpoznaje znacznik listy plikow");
    Sprawdz(LiczStala(miKopiujLbc, CTRL | SHIFT | KEY_C) >= 2,
            "domyslna obsluga oddaje Control+Shift+C DWOM listom: odsylaczy i plikow (wystapien: "
            + LiczStala(miKopiujLbc, CTRL | SHIFT | KEY_C) + ")");
    Sprawdz(LiczStala(miKopiujLbc, ALT | KEY_C) >= 2,
            "domyslna obsluga oddaje Alt+C liscie plikow obok wlasnej obslugi dopisywania (wystapien: "
            + LiczStala(miKopiujLbc, ALT | KEY_C) + ")");

    // ---------- 4. KLAWISZE NISZCZACE ZOSTAJA PRZY JEDNEJ POZYCJI ----------
    bool bOdmowa = false, bLiczy = false;
    foreach (MethodBase mb in MetodyZGenerowanymi(tDialogEd)) {
        if (!WolaMetode(mb, "PickFileDeleteFromDisk") && !WolaMetode(mb, "PickFileRemoveEntry")) continue;
        if (MaFragment(mb, "at a time")) bOdmowa = true;
        if (WolaMetode(mb, "get_SelectedIndices")) bLiczy = true;
    }
    Sprawdz(bOdmowa,
            "usuwanie przy kilku zaznaczonych ODMAWIA z powiedzeniem dlaczego (nie milczy)");
    Sprawdz(bLiczy,
            "odmowa opiera sie na LICZBIE zaznaczonych pozycji (SelectedIndices), nie na zgadywaniu");

    // ---------- KONTROLA POZYTYWNA SONDY ----------
    Sprawdz(MaLiteral(miKopiuj, "No item!"),
            "KONTROLA SONDY: MaLiteral widzi literal, ktory w ciele tej metody JEST");
    Sprawdz(!MaLiteral(miKopiuj, "TegoNapisuTamNieMaWcale"),
            "KONTROLA SONDY: MaLiteral NIE widzi literalu, ktorego nie ma");
    Sprawdz(!WolaMetode(miKopiuj, "NieMaTakiejMetodyWcale"),
            "KONTROLA SONDY: WolaMetode NIE widzi metody, ktorej nie ma");
    Sprawdz(!MaStala(miKopiujLbc, 123456789),
            "KONTROLA SONDY: MaStala NIE widzi stalej, ktorej nie ma");

    Podsumuj();
}

static void Podsumuj() {
    Console.WriteLine();
    Console.WriteLine("PODSUMOWANIE: " + iOk + " OK, " + iZle + " ZLE");
}

} // Pomiar569
