// POMIAR 5.0.72: DELETE NA LISCIE PLIKOW ZDEJMUJE WSZYSTKIE ZAZNACZONE WPISY.
//
// CO ZMIERZONE PRZED ZMIANA (na binarce 5.0.71, commit e4bf903): galaz
// Keys.Delete / Keys.Back w PickFile ODMAWIALA przy kilku zaznaczonych pozycjach
// literalem "N items selected; removing works on one entry at a time!" i wolala
// PickFileRemoveEntry tylko dla pozycji pod kursorem.  Zgloszenie Kasperczaka
// 06.09.2026: "Nie da sie usunac zaznaczonych dwoch. 2 items selected; removing
// works on one entry at a time!" - czyli wariant B pytania edsharpng-120.
//
// Rodziny asercji:
//   1. NOSNIK: Dialog.PickFileRemoveSelection istnieje, jest prywatny i
//      statyczny, zwraca void i bierze cztery parametry (ListBox, dwie listy
//      napisow, sekcja).
//   2. NOSNIK CZYTA ZAZNACZENIE I IDZIE OD KONCA: wola SelectedIndices, sortuje
//      (Sort) i wola PickFileRemoveEntry.  Kolejnosc malejaca jest warunkiem
//      poprawnosci - usuniecie pozycji przesuwa indeksy powyzej.
//   3. WOLANIE Z LISTY: cialo PickFile (albo jego metoda generowana) wola
//      PickFileRemoveSelection, a STAREGO literalu odmowy "removing works on one
//      entry at a time" nie ma NIGDZIE w binarce.  Bez tej drugiej polowy
//      dopisanie nowej drogi obok starej tez byloby zielone.
//   4. MOWA MOWI LICZBE: nosnik niesie "entries from list" oraz nadal
//      "Removed from list" (jedna pozycja) i "List is now empty".
//   5. ROZLACZNOSC WOBEC KASOWANIA Z DYSKU: Shift+Delete zostaje
//      jednopozycyjny.  Literal "deleting from disk works on one file at a time"
//      w binarce ZOSTAJE, a PickFileDeleteFromDisk nadal czyta SelectedIndex
//      (jedna pozycja), nie SelectedIndices.
//   6. WERSJA: App.VersionString to 5.0.72, a okno About sklada z niej napis.
//
// Pomiar po SYGNATURACH i CIALACH METOD (ldstr 0x72 + ResolveString,
// call/callvirt/newobj + ResolveMember), nie po napisach w pliku.
//
// PULAPKA, ktora ten plik obchodzi (jak pomiar_567, 568 i 571): cialo domkniecia
// (delegata KeyDown) kompilator przenosi do metody GENEROWANEJ o nazwie
// zawierajacej nazwe macierzysta, wiec pytanie o cialo samej metody PickFile
// daje falszywe zero.  Dlatego szukamy TAKZE w metodach generowanych nosnika.
//
// Uruchomienie (katalog roboczy MUSI byc na dysku Windows - dla .NET /tmp jest
// lokalizacja siecowa \\wsl.localhost i Assembly.LoadFile rzuca wyjatek):
//   csc.exe /nologo /target:exe "/out:D:\projekty\edsharp-pr\testy\out_572.exe" \
//           "/r:D:\projekty\edsharp-pr\EdSharpNG.exe" \
//           "D:\projekty\edsharp-pr\testy\pomiar_572.cs"
//   cmd.exe /c "testy\out_572.exe EdSharpNG.exe"
//
// KONTROLA NEGATYWNA: testy/kontrola_negatywna_572.sh na binarce 5.0.71.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

class Pomiar572 {

static int iOk = 0;
static int iZle = 0;

static void Sprawdz(bool bWarunek, string sOpis) {
    if (bWarunek) { iOk++; Console.WriteLine("OK   " + sOpis); }
    else { iZle++; Console.WriteLine("ZLE  " + sOpis); }
}

// Wszystkie metody typu, RAZEM z generowanymi (domkniecia).
static List<MethodBase> WszystkieMetody(Type t) {
    List<MethodBase> l = new List<MethodBase>();
    if (t == null) return l;
    BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    try { foreach (MethodInfo mi in t.GetMethods(bf)) l.Add(mi); } catch {}
    try { foreach (ConstructorInfo ci in t.GetConstructors(bf)) l.Add(ci); } catch {}
    try {
        foreach (Type tn in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            l.AddRange(WszystkieMetody(tn));
    } catch {}
    return l;
}

static List<MethodBase> MetodyZNosnikiem(Type t, string sNazwa) {
    List<MethodBase> l = new List<MethodBase>();
    foreach (MethodBase mb in WszystkieMetody(t))
        if (mb.Name == sNazwa || mb.Name.IndexOf(sNazwa, StringComparison.Ordinal) >= 0)
            l.Add(mb);
    return l;
}

static byte[] Il(MethodBase mb) {
    if (mb == null) return null;
    MethodBody body = null;
    try { body = mb.GetMethodBody(); } catch { return null; }
    if (body == null) return null;
    try { return body.GetILAsByteArray(); } catch { return null; }
}

static bool MaLiteral(MethodBase mb, string sSzukany) {
    byte[] aIl = Il(mb);
    if (aIl == null) return false;
    Module mod = mb.Module;
    for (int i = 0; i + 4 < aIl.Length; i++) {
        if (aIl[i] != 0x72) continue;
        int iToken = BitConverter.ToInt32(aIl, i + 1);
        string s = null;
        try { s = mod.ResolveString(iToken); } catch { continue; }
        if (s != null && s.IndexOf(sSzukany, StringComparison.Ordinal) >= 0) return true;
    }
    return false;
}

static bool MaWolanie(MethodBase mb, string sNazwaCzlonu) {
    byte[] aIl = Il(mb);
    if (aIl == null) return false;
    Module mod = mb.Module;
    for (int i = 0; i + 4 < aIl.Length; i++) {
        bool bCall = (aIl[i] == 0x28 || aIl[i] == 0x6F || aIl[i] == 0x73);
        bool bFld  = (aIl[i] == 0x7E || aIl[i] == 0x7B);   // ldsfld / ldfld
        if (!bCall && !bFld) continue;
        int iToken = BitConverter.ToInt32(aIl, i + 1);
        MemberInfo mi = null;
        try { mi = mod.ResolveMember(iToken); } catch { continue; }
        if (mi == null) continue;
        string sPelna = ((mi.DeclaringType == null) ? "" : mi.DeclaringType.Name + ".") + mi.Name;
        if (sPelna.IndexOf(sNazwaCzlonu, StringComparison.Ordinal) >= 0) return true;
        if (mi.Name == sNazwaCzlonu) return true;
    }
    return false;
}

static bool KtorasMaLiteral(List<MethodBase> l, string s) {
    foreach (MethodBase mb in l) if (MaLiteral(mb, s)) return true;
    return false;
}

static bool KtorasMaWolanie(List<MethodBase> l, string s) {
    foreach (MethodBase mb in l) if (MaWolanie(mb, s)) return true;
    return false;
}

// Literal w CALEJ binarce, nie w jednej metodzie: pytanie "czy stary napis
// zniknal" musi obejmowac kazde miejsce, w ktorym moglby zostac.
static bool GdziekolwiekLiteral(Assembly asm, string sSzukany) {
    Type[] aTypy;
    try { aTypy = asm.GetTypes(); } catch (ReflectionTypeLoadException ex) { aTypy = ex.Types; }
    foreach (Type t in aTypy) {
        if (t == null) continue;
        foreach (MethodBase mb in WszystkieMetody(t))
            if (MaLiteral(mb, sSzukany)) return true;
    }
    return false;
}

static int Main(string[] args) {
    string sExe = (args.Length > 0) ? args[0] : "EdSharpNG.exe";
    Assembly asm;
    try { asm = Assembly.LoadFile(Path.GetFullPath(sExe)); }
    catch (Exception ex) {
        Console.WriteLine("AWARIA POMIARU: nie moge wczytac " + sExe + ": " + ex.GetType().Name + ": " + ex.Message);
        return 5;
    }

    Type tApp    = asm.GetType("EdSharp.App");
    Type tDialog = asm.GetType("EdSharp.Dialog");
    Type tFrame  = asm.GetType("EdSharp.MdiFrame");

    Sprawdz(tApp    != null, "typ EdSharp.App istnieje");
    Sprawdz(tDialog != null, "typ EdSharp.Dialog istnieje");
    Sprawdz(tFrame  != null, "typ EdSharp.MdiFrame istnieje");

    // KONTROLA POZYTYWNA SONDY: nosnik, ktory ISTNIEJE od 5.0.69, musi byc
    // widoczny.  Bez niej "nie widze nowego nosnika" nie odroznia braku kodu od
    // sondy patrzacej w zly typ.
    MethodInfo miKopia = (tDialog == null) ? null
        : tDialog.GetMethod("PickFileCopySelection", BindingFlags.NonPublic | BindingFlags.Static);
    Sprawdz(miKopia != null, "KONTROLA SONDY: stary nosnik PickFileCopySelection jest widoczny");

    // ---------- RODZINA 1: nosnik zdejmowania zaznaczenia ----------
    MethodInfo miUsun = (tDialog == null) ? null
        : tDialog.GetMethod("PickFileRemoveSelection", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
    Sprawdz(miUsun != null, "Dialog.PickFileRemoveSelection istnieje");
    Sprawdz(miUsun != null && miUsun.IsStatic, "PickFileRemoveSelection jest statyczna");
    Sprawdz(miUsun != null && miUsun.ReturnType == typeof(void), "PickFileRemoveSelection zwraca void");
    ParameterInfo[] aPar = (miUsun == null) ? new ParameterInfo[0] : miUsun.GetParameters();
    Sprawdz(aPar.Length == 4, "PickFileRemoveSelection bierze cztery parametry (jest: " + aPar.Length + ")");
    Sprawdz(aPar.Length == 4 && aPar[0].ParameterType.Name == "ListBox",
        "pierwszy parametr to ListBox (lista, ktora zdejmuje wpisy)");
    Sprawdz(aPar.Length == 4 && aPar[3].ParameterType == typeof(string),
        "czwarty parametr to sekcja pliku ustawien (string)");

    // ---------- RODZINA 2: czyta zaznaczenie, idzie od konca ----------
    Sprawdz(miUsun != null && MaWolanie(miUsun, "SelectedIndices"),
        "PickFileRemoveSelection czyta SelectedIndices (cale zaznaczenie, nie pozycje pod kursorem)");
    Sprawdz(miUsun != null && MaWolanie(miUsun, "PickFileRemoveEntry"),
        "PickFileRemoveSelection wola PickFileRemoveEntry (jedna droga zdejmowania wpisu, tez z sekcji INI)");
    Sprawdz(miUsun != null && MaWolanie(miUsun, "Sort"),
        "PickFileRemoveSelection sortuje indeksy (warunek chodzenia od konca)");
    Sprawdz(miUsun != null && MaWolanie(miUsun, "selectOnly"),
        "PickFileRemoveSelection przestawia kursor listy przez selectOnly");

    // ---------- RODZINA 3: lista wola nosnik, stara odmowa zniknela ----------
    List<MethodBase> lPick = MetodyZNosnikiem(tDialog, "PickFile");
    Sprawdz(lPick.Count > 0, "Dialog.PickFile istnieje (nosnik listy plikow)");
    Sprawdz(KtorasMaWolanie(lPick, "PickFileRemoveSelection"),
        "galaz Delete w liscie plikow wola PickFileRemoveSelection");
    Sprawdz(!GdziekolwiekLiteral(asm, "removing works on one entry at a time"),
        "ROZLACZNOSC: stary literal odmowy 'removing works on one entry at a time' zniknal z CALEJ binarki");

    // ---------- RODZINA 4: mowa mowi liczbe ----------
    Sprawdz(miUsun != null && MaLiteral(miUsun, "entries from list"),
        "mowa podaje LICZBE zdjetych wpisow ('entries from list')");
    Sprawdz(miUsun != null && MaLiteral(miUsun, "Removed from list"),
        "jedna pozycja nadal mowi 'Removed from list' (nie liczba przy jednym wpisie)");
    Sprawdz(miUsun != null && MaLiteral(miUsun, "List is now empty"),
        "opustoszala lista mowi 'List is now empty'");
    Sprawdz(miUsun != null && MaWolanie(miUsun, "AddMessage"),
        "PickFileRemoveSelection mowi przez AddMessage (droga omijajaca tlumienie mowy)");

    // ---------- RODZINA 5: kasowanie z dysku ZOSTAJE jednopozycyjne ----------
    // To nie jest kontrola sondy, a wymaganie: jego decyzja dotyczyla WYLACZNIE
    // zdejmowania wpisu z listy.  Gdyby ta zmiana rozlala sie na Shift+Delete,
    // pomylka kosztowalaby plik.
    Sprawdz(GdziekolwiekLiteral(asm, "deleting from disk works on one file at a time"),
        "Shift+Delete NADAL odmawia przy kilku zaznaczonych (literal odmowy jest)");
    MethodInfo miDysk = (tDialog == null) ? null
        : tDialog.GetMethod("PickFileDeleteFromDisk", BindingFlags.NonPublic | BindingFlags.Static);
    Sprawdz(miDysk != null, "Dialog.PickFileDeleteFromDisk istnieje");
    Sprawdz(miDysk != null && MaWolanie(miDysk, "get_SelectedIndex"),
        "PickFileDeleteFromDisk czyta SelectedIndex (JEDNA pozycja)");
    Sprawdz(miDysk != null && !MaWolanie(miDysk, "SelectedIndices"),
        "ROZLACZNOSC: PickFileDeleteFromDisk NIE czyta calego zaznaczenia");
    Sprawdz(miDysk != null && MaWolanie(miDysk, "Confirm"),
        "kasowanie z dysku nadal pyta o potwierdzenie");

    // ---------- RODZINA 6: numer wersji ----------
    FieldInfo fiVer = (tApp == null) ? null
        : tApp.GetField("VersionString", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
    string sVer = "";
    if (fiVer != null) { try { sVer = (string) fiVer.GetRawConstantValue(); } catch {} }
    Sprawdz(sVer == "5.0.72", "App.VersionString to 5.0.72 (jest: '" + sVer + "')");
    // Stala jest INLINOWANA (pulapka zmierzona przy 5.0.71: w IL nie ma ldsfld),
    // wiec pytamy o literal okna About z DOKLADNA wartoscia stalej.
    List<MethodBase> lMenu = MetodyZNosnikiem(tFrame, "menuItem_Click");
    Sprawdz(sVer.Length > 0 && KtorasMaLiteral(lMenu, "EdSharpNG " + sVer + " (beta)"),
        "okno About mowi dokladnie wartosc App.VersionString");

    Console.WriteLine();
    Console.WriteLine("WYNIK: " + iOk + " OK / " + iZle + " ZLE");
    // BRAMKA: zero asercji RAZEM znaczy awarie pomiaru, nie zielony wynik.
    if (iOk + iZle == 0) {
        Console.WriteLine("AWARIA POMIARU: zero asercji wykonanych");
        return 5;
    }
    return (iZle == 0) ? 0 : 1;
}

}
