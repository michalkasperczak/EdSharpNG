// POMIAR 5.0.68: trzy rodziny zmian z przegladu "co z tej listy jest juz zrobione"
// (jego wiadomosc 05.09.2026 "Czesc z tego moze juz nawet zrobiles").
//
//   1. FOKUS PO ZAMKNIECIU MENU - wspolny nosnik FocusChildEditControl dla
//      uaktywnienia okna i dla zamkniecia menu (MenuDeactivate).
//   2. PONAWIANIE ZAPISU DO SCHOWKA na kazdej drodze - Lbc dostaje wlasne
//      setClipboard/getClipboard z petla, a Util.SetClipboardText ZWRACA
//      wynik, zeby wolajacy mial jak powiedziec o niepowodzeniu.
//   3. WIELOKROTNY WYBOR SHIFTEM na listach - SelectionMode.MultiExtended
//      plus selectOnly jako jedyna droga do PRZESTAWIENIA kursora listy.
//
// Pomiar idzie po SYGNATURACH i po CIALACH METOD (opkod ldstr 0x72 +
// ResolveString oraz odwolania do metod/pol przez ldsfld/call), a nie po
// napisach w pliku: literal siedzi w binarce takze wtedy, gdy nikt go nie
// uzywa, a kompilator scala identyczne literaly.
//
// Uruchomienie:
//   csc.exe /nologo /target:exe "/out:D:\projekty\edsharp-pr\testy\out_568.exe" \
//           "/r:D:\projekty\edsharp-pr\EdSharpNG.exe" \
//           "D:\projekty\edsharp-pr\testy\pomiar_568.cs"
//   cmd.exe /c "testy\out_568.exe EdSharpNG.exe"
//
// KONTROLA NEGATYWNA: testy/kontrola_negatywna_568.sh na binarce 5.0.67 MUSI
// oblac czesc asercji - inaczej zielony wynik nie odroznia naprawy od gluchej
// sondy.

using System;
using System.IO;
using System.Reflection;

class Pomiar568 {

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

// Czy cialo metody WOLA metode o tej nazwie: szuka opkodow call (0x28),
// callvirt (0x6F) i newobj (0x73), rozwiazuje token i porownuje nazwe.
// Sprawdzenie po NAZWIE, nie po tozsamosci obiektu: metoda moze byc
// przeciazona albo lezec w innym module (Homer.LbcDialog vs EdSharp).
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
    // LoadFile, nie LoadFrom: LoadFrom dla dwoch plikow o tej samej tozsamosci
    // oddaje TEN SAM obiekt, wiec kontrola negatywna pytalaby nowa binarke.
    Assembly asm = Assembly.LoadFile(Path.GetFullPath(sBin));
    Console.WriteLine("Binarka: " + asm.Location);
    Console.WriteLine();

    Type tFrame = Typ(asm, "MdiFrame");
    Type tDialog = Typ(asm, "LbcDialog");
    Type tTextBox = Typ(asm, "LbcTextBox");
    Type tUtil = Typ(asm, "Util");
    Sprawdz(tFrame != null, "typ MdiFrame znaleziony w binarce");
    Sprawdz(tDialog != null, "typ LbcDialog znaleziony w binarce");
    Sprawdz(tTextBox != null, "typ LbcTextBox znaleziony w binarce");
    Sprawdz(tUtil != null, "typ Util znaleziony w binarce");
    if (tFrame == null || tDialog == null || tTextBox == null || tUtil == null) { Podsumuj(); return; }

    // ---------- 1. FOKUS PO ZAMKNIECIU MENU ----------
    MethodInfo miFokus = Metoda(tFrame, "FocusChildEditControl");
    Sprawdz(miFokus != null, "metoda FocusChildEditControl istnieje (fokus ma NAZWANY nosnik, nie domkniecie)");
    Sprawdz(miFokus != null && miFokus.GetParameters().Length == 0,
            "FocusChildEditControl jest bezparametrowa (wolana z dwoch zdarzen)");
    // BeginInvoke jest tu konieczne, nie kosmetyczne - fokus ustawiony wprost
    // w obsludze zdarzenia zostaje nadpisany przez Windows.
    Sprawdz(WolaMetode(miFokus, "BeginInvoke"),
            "FocusChildEditControl odklada ustawienie fokusu przez BeginInvoke");
    // WOLANIE Focus() STOI W METODZIE GENEROWANEJ, NIE W CIELE NOSNIKA.
    // Zmierzone testy/gdzie_568.cs: kompilator przenosi cialo domkniecia
    // BeginInvoke do osobnej metody <FocusChildEditControl>b__e, wiec pytanie o
    // Focus wprost w FocusChildEditControl daje FALSZYWE ZERO - dokladnie
    // pulapka opisana w pomiar_567.cs.  Pytamy wiec o metode generowana TEGO
    // nosnika: jej nazwa zawiera nazwe metody macierzystej, wiec asercja nadal
    // wiaze fokus z TA funkcja, a nie z dowolnym Focus w binarce.
    bool bFokusUstawiony = false;
    foreach (MethodInfo mi in tFrame.GetMethods(BindingFlags.Instance | BindingFlags.Static
                                              | BindingFlags.Public | BindingFlags.NonPublic
                                              | BindingFlags.DeclaredOnly)) {
        if (mi.Name.IndexOf("FocusChildEditControl") < 0) continue;
        if (mi.Name == "FocusChildEditControl") continue;   // sam nosnik, nie jego domkniecie
        if (WolaMetode(mi, "Focus")) { bFokusUstawiony = true; break; }
    }
    Sprawdz(bFokusUstawiony,
            "domkniecie FocusChildEditControl faktycznie ustawia fokus (wolanie Focus)");
    // ISTOTA: dwa ZRODLA muszą siegac do tego jednego nosnika.  Konstruktor
    // ramy podpina Activated i MenuDeactivate; szukamy wiec wolania nosnika
    // w metodach generowanych dla tych domkniec ORAZ podpiecia MenuDeactivate.
    bool bZFokusu = false, bMenuDeactivate = false;
    foreach (Type t in asm.GetTypes()) {
        foreach (MethodInfo mi in t.GetMethods(BindingFlags.Instance | BindingFlags.Static
                                             | BindingFlags.Public | BindingFlags.NonPublic
                                             | BindingFlags.DeclaredOnly)) {
            if (WolaMetode(mi, "FocusChildEditControl")) bZFokusu = true;
            if (WolaMetode(mi, "add_MenuDeactivate")) bMenuDeactivate = true;
        }
        foreach (ConstructorInfo ci in t.GetConstructors(BindingFlags.Instance | BindingFlags.Static
                                                      | BindingFlags.Public | BindingFlags.NonPublic
                                                      | BindingFlags.DeclaredOnly)) {
            if (WolaMetode(ci, "FocusChildEditControl")) bZFokusu = true;
            if (WolaMetode(ci, "add_MenuDeactivate")) bMenuDeactivate = true;
        }
    }
    Sprawdz(bZFokusu, "FocusChildEditControl jest WOLANA z kodu (nie martwa metoda)");
    Sprawdz(bMenuDeactivate,
            "program podpina sie pod MenuDeactivate paska menu (istota zgloszenia o fokusie po menu)");

    // ---------- 2. PONAWIANIE ZAPISU DO SCHOWKA ----------
    MethodInfo miSetLbc = Metoda(tTextBox, "setClipboard");
    MethodInfo miGetLbc = Metoda(tTextBox, "getClipboard");
    MethodInfo miSetUtil = Metoda(tUtil, "SetClipboardText");
    Sprawdz(miSetLbc != null, "LbcTextBox.setClipboard istnieje");
    Sprawdz(miGetLbc != null, "LbcTextBox.getClipboard istnieje (odczyt tez ma ponowienia)");
    Sprawdz(miSetUtil != null, "Util.SetClipboardText istnieje");
    // Petla ponowien: Thread.Sleep w ciele jest jej jedynym widocznym sladem
    // na poziomie IL, a bez uspienia "petla" krecilaby sie bez sensu.
    Sprawdz(WolaMetode(miSetLbc, "Sleep"),
            "setClipboard USYPIA miedzy probami (petla ponowien, nie jednorazowy zapis)");
    Sprawdz(WolaMetode(miGetLbc, "Sleep"),
            "getClipboard USYPIA miedzy probami (petla ponowien)");
    // Wynik zapisu MUSI wracac do wolajacego - inaczej klawisz mowi
    // "skopiowane" przy pustym schowku, a to dla niewidomego najgorszy wariant.
    Sprawdz(miSetLbc != null && miSetLbc.ReturnType == typeof(bool),
            "setClipboard zwraca bool (wolajacy wie, czy zapis doszedl)");
    Sprawdz(miSetUtil != null && miSetUtil.ReturnType == typeof(bool),
            "Util.SetClipboardText zwraca bool, nie void (istota zmiany)");
    // Wygody tekstowe i klawisze list nie moga juz pisac wprost do schowka.
    MethodInfo miKopiujKlawisze = Metoda(tDialog, "handleListBoxCopyKeys");
    Sprawdz(miKopiujKlawisze != null, "handleListBoxCopyKeys istnieje");
    Sprawdz(WolaMetode(miKopiujKlawisze, "setClipboard"),
            "kopiowanie na liscie idzie przez setClipboard z ponowieniami");
    Sprawdz(!WolaMetode(miKopiujKlawisze, "SetText"),
            "kopiowanie na liscie NIE wola Clipboard.SetText wprost (bez ponowien)");

    // ---------- 3. WIELOKROTNY WYBOR SHIFTEM ----------
    MethodInfo miDodajListe = null;
    foreach (MethodInfo mi in tDialog.GetMethods(BindingFlags.Instance | BindingFlags.Public
                                               | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
        if (mi.Name != "addListBox") continue;
        ParameterInfo[] aP = mi.GetParameters();
        if (aP.Length == 3 && aP[0].ParameterType != typeof(string)) { miDodajListe = mi; break; }
    }
    Sprawdz(miDodajListe != null, "addListBox(lista, wybrany, podpowiedz) istnieje");
    Sprawdz(WolaMetode(miDodajListe, "set_SelectionMode"),
            "addListBox USTAWIA tryb zaznaczania listy (istota wielokrotnego wyboru)");
    MethodInfo miSelectOnly = Metoda(tDialog, "selectOnly");
    Sprawdz(miSelectOnly != null,
            "LbcDialog.selectOnly istnieje (jedyna droga do PRZESTAWIENIA kursora listy)");
    Sprawdz(miSelectOnly != null && WolaMetode(miSelectOnly, "ClearSelected"),
            "selectOnly CZYSCI poprzednie zaznaczenie (inaczej kursor zostawialby slad)");
    // Szukanie w liscie (Control+J, F3) przestawia kursor, wiec MUSI iść tedy.
    MethodInfo miSzukajNast = Metoda(tDialog, "findNextInListBox");
    MethodInfo miSzukajPierw = Metoda(tDialog, "promptAndFindInListBox");
    Sprawdz(miSzukajNast != null && WolaMetode(miSzukajNast, "selectOnly"),
            "F3 na liscie przestawia zaznaczenie przez selectOnly, nie dokłada pozycji");
    Sprawdz(miSzukajPierw != null && WolaMetode(miSzukajPierw, "selectOnly"),
            "Control+J na liscie przestawia zaznaczenie przez selectOnly");
    // Kopiowanie musi czytac WSZYSTKIE zaznaczone pozycje, nie sama biezaca.
    Sprawdz(WolaMetode(miKopiujKlawisze, "get_SelectedIndices"),
            "Control+C czyta WSZYSTKIE zaznaczone pozycje (SelectedIndices), nie sama biezaca");
    // LISTA PLIKOW: ASERCJA PRZEPISANA 05.09.2026 PO JEGO ZGLOSZENIU.
    // W 5.0.68 pytala, czy lista plikow JAWNIE cofa tryb do jednowyborowego -
    // taki byl wtedy zamysl.  On zglosil to jako WADE ("nie dziala zaznaczanie
    // i kopiowanie wielu Control+C na listach, Alt+L to na pewno sprawdzilem"),
    // bo lista plikow i sciezek jest wlasnie tym miejscem, gdzie kopiowanie
    // kilku pozycji ma sens.  Wielokrotny wybor objal wiec TEZ te liste
    // (5.0.69), a wyjatek zostal tylko na klawiszach NISZCZACYCH.
    // To NIE regresja, a zmiana decyzji czlowieka - pytanie przestawione na
    // stan aktualny i MOCNIEJSZE: sprawdza obie strony tej decyzji naraz.
    // Aktualny stan tej rodziny mierzy w calosci pomiar_569.
    MethodInfo miPickFile = Metoda(tFrame, "PickFile");
    if (miPickFile == null) {
        Type tDialogEd = Typ(asm, "Dialog");
        miPickFile = Metoda(tDialogEd, "PickFile");
    }
    Sprawdz(miPickFile != null && !WolaMetode(miPickFile, "set_SelectionMode"),
            "lista plikow NIE cofa juz trybu do jednowyborowego (bierze wielokrotny wybor jak kazda inna)");

    // ---------- KONTROLA POZYTYWNA SONDY ----------
    // Gdyby WolaMetode zawsze zwracalo false, wszystkie asercje o BRAKU
    // przechodzilyby przez przypadek, a asercje o obecnosci oblewaly.  Ten
    // literal stoi w ciele metody WPROST (nie w lambdzie), wiec rozstrzyga o
    // widocznosci literalow, a Sleep wyzej o widocznosci wolan.
    Sprawdz(MaLiteral(miKopiujKlawisze, "No item"),
            "KONTROLA SONDY: MaLiteral widzi literal, ktory w ciele tej metody JEST");
    Sprawdz(!WolaMetode(miSetLbc, "NieMaTakiejMetodyWcale"),
            "KONTROLA SONDY: WolaMetode NIE widzi metody, ktorej nie ma");

    Podsumuj();
}

static void Podsumuj() {
    Console.WriteLine();
    Console.WriteLine("PODSUMOWANIE: " + iOk + " OK, " + iZle + " ZLE");
}

} // Pomiar568
