// POMIAR 5.0.73: MOWA PRZELACZNIKA ULUBIONYCH PODAJE SKUTEK, NIE NAZWE KOMENDY.
//
// JEGO ZGLOSZENIE 06.09.2026: "Alt-l Toggle favorite.  Nie potrzebnie, niech
// mowi Added favorites, Remove favorites."  Zmierzone przed zmiana (binarka
// 5.0.72, commit 4a3a644): pozycja menu "Toggle Favorite" byla tworzona z
// opcjami "child speak", a menuItem_Click przy braku slowa "silent" wypowiada
// menuItem.Name - czyli po JEDNYM nacisnieciu szly DWIE wypowiedzi: "Toggle
// Favorite", a potem "Added to favorites".
//
// Rodziny asercji:
//   1. OPCJE POZYCJI MENU: literal "child silent" jest w ciele MdiFrame (nosnik
//      tworzenia menu), a rozstrzygajaco - metoda tworzaca menu NIE zawiera
//      literalu "child speak" bezposrednio przy Toggle Favorite.  Poniewaz
//      literal "child speak" wystepuje w binarce wielokrotnie (dziesiatki innych
//      komend), asercja NIE MOZE brzmiec "nie ma child speak" - musi pytac o
//      KOLEJNOSC ladowania literalow w IL konstruktora MdiFrame: po literalu
//      "Toggle Favorite" najblizszym literalem opcji musi byc "child silent".
//   2. CLEAR FAVORITE MOWI: handler niesie literale "Removed from favorites"
//      ORAZ "Not in favorites" (nowy - komenda dotad milczala calkowicie).
//   3. TOGGLE NADAL MOWI OBA KIERUNKI: "Added to favorites" i
//      "Removed from favorites" sa w tym samym nosniku.
//   4. CZYTANIE OBECNOSCI PRZEZ WARTOSC DOMYSLNA: galaz Clear Favorite wola
//      ReadValue (rozstrzygniecie "czy plik jest na liscie") oraz DeleteKey.
//      Bez ReadValue komunikat "Not in favorites" nie mialby na czym stanac.
//   5. WERSJA: App.VersionString to 5.0.73, a okno About sklada z niej napis.
//
// Pomiar po CIALACH METOD (ldstr 0x72 + ResolveString, call/callvirt 0x28/0x6F
// + ResolveMember), nie po napisach w pliku.
//
// PULAPKA OBEJSCIA (jak 567/568/571/572): cialo domkniecia kompilator przenosi
// do metody GENEROWANEJ, wiec szukamy takze w metodach generowanych nosnika.
//
// Uruchomienie (katalog roboczy MUSI byc na dysku Windows - dla .NET /tmp jest
// lokalizacja siecowa \\wsl.localhost i Assembly.LoadFile rzuca wyjatek):
//   csc.exe /nologo /target:exe "/out:D:\projekty\edsharp-pr\testy\out_573.exe" \
//           "/r:D:\projekty\edsharp-pr\EdSharpNG.exe" \
//           "D:\projekty\edsharp-pr\testy\pomiar_573.cs"
//   cmd.exe /c "testy\out_573.exe EdSharpNG.exe"
//
// KONTROLA NEGATYWNA: testy/kontrola_negatywna_573.sh na binarce 5.0.72.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

class Pomiar573 {

static int iOk = 0;
static int iZle = 0;

static void Sprawdz(bool bWarunek, string sOpis) {
    if (bWarunek) { iOk++; Console.WriteLine("OK   " + sOpis); }
    else { iZle++; Console.WriteLine("ZLE  " + sOpis); }
}

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

// Wszystkie literale ciala metody W KOLEJNOSCI ladowania.  Kolejnosc jest tutaj
// istotna: opcje pozycji menu ida do CreateMenuItem zaraz po jej nazwie, wiec
// pytanie "jakie opcje ma Toggle Favorite" da sie postawic tylko na sasiedztwie.
static List<string> Literale(MethodBase mb) {
    List<string> l = new List<string>();
    byte[] aIl = Il(mb);
    if (aIl == null) return l;
    Module mod = mb.Module;
    for (int i = 0; i + 4 < aIl.Length; i++) {
        if (aIl[i] != 0x72) continue;
        int iToken = BitConverter.ToInt32(aIl, i + 1);
        string s = null;
        try { s = mod.ResolveString(iToken); } catch { continue; }
        if (s != null) l.Add(s);
    }
    return l;
}

static bool MaLiteral(MethodBase mb, string sSzukany) {
    foreach (string s in Literale(mb))
        if (s.IndexOf(sSzukany, StringComparison.Ordinal) >= 0) return true;
    return false;
}

static bool MaWolanie(MethodBase mb, string sNazwaCzlonu) {
    byte[] aIl = Il(mb);
    if (aIl == null) return false;
    Module mod = mb.Module;
    for (int i = 0; i + 4 < aIl.Length; i++) {
        bool bCall = (aIl[i] == 0x28 || aIl[i] == 0x6F || aIl[i] == 0x73);
        bool bFld  = (aIl[i] == 0x7E || aIl[i] == 0x7B);
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

// OPCJE POZYCJI MENU: pierwszy literal po nazwie komendy, ktory wyglada jak
// zestaw opcji (zawiera "child" albo "frame").  Zwraca null, gdy nazwy nie ma.
static string OpcjePozycji(List<MethodBase> lMetody, string sNazwaKomendy) {
    foreach (MethodBase mb in lMetody) {
        List<string> l = Literale(mb);
        for (int i = 0; i < l.Count; i++) {
            if (l[i] != sNazwaKomendy) continue;
            for (int j = i + 1; j < l.Count && j <= i + 6; j++) {
                if (l[j].IndexOf("child", StringComparison.Ordinal) >= 0
                 || l[j].IndexOf("frame", StringComparison.Ordinal) >= 0) return l[j];
            }
        }
    }
    return null;
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
    Type tFrame  = asm.GetType("EdSharp.MdiFrame");
    Type tDialog = asm.GetType("EdSharp.Dialog");

    Sprawdz(tApp    != null, "typ EdSharp.App istnieje");
    Sprawdz(tFrame  != null, "typ EdSharp.MdiFrame istnieje");
    Sprawdz(tDialog != null, "typ EdSharp.Dialog istnieje");

    List<MethodBase> lFrame = WszystkieMetody(tFrame);
    List<MethodBase> lMenu  = MetodyZNosnikiem(tFrame, "menuItem_Click");
    Sprawdz(lFrame.Count > 0, "MdiFrame ma metody (nosnik menu i handler komend)");
    Sprawdz(lMenu.Count > 0, "MdiFrame.menuItem_Click istnieje (handler komend)");

    // KONTROLA POZYTYWNA SONDY: czytanie opcji MUSI dzialac na komendzie, ktorej
    // opcji nie ruszalismy.  Bez tego "Toggle Favorite ma child silent" nie
    // odroznia poprawnego kodu od sondy, ktora zawsze zwraca to samo.
    string sKontrolaSpeak = OpcjePozycji(lFrame, "&New");
    Sprawdz(sKontrolaSpeak != null && sKontrolaSpeak.IndexOf("speak", StringComparison.Ordinal) >= 0,
        "KONTROLA SONDY: czytanie opcji dziala - komenda New ma nadal 'speak' (jest: '"
        + (sKontrolaSpeak == null ? "brak" : sKontrolaSpeak) + "')");
    string sKontrolaSilent = OpcjePozycji(lFrame, "Recent Files ...");
    Sprawdz(sKontrolaSilent != null && sKontrolaSilent.IndexOf("silent", StringComparison.Ordinal) >= 0,
        "KONTROLA SONDY: czytanie opcji rozroznia 'silent' - Recent Files ma 'frame silent' (jest: '"
        + (sKontrolaSilent == null ? "brak" : sKontrolaSilent) + "')");

    // ---------- RODZINA 1: opcje pozycji Toggle Favorite ----------
    string sOpcjeToggle = OpcjePozycji(lFrame, "Toggle Favorite");
    Sprawdz(sOpcjeToggle != null, "pozycja menu 'Toggle Favorite' istnieje (nazwa w ciele nosnika menu)");
    Sprawdz(sOpcjeToggle != null && sOpcjeToggle.IndexOf("silent", StringComparison.Ordinal) >= 0,
        "Toggle Favorite ma opcje 'silent' - nazwa komendy NIE jest wypowiadana (jest: '"
        + (sOpcjeToggle == null ? "brak" : sOpcjeToggle) + "')");
    Sprawdz(sOpcjeToggle != null && sOpcjeToggle.IndexOf("speak", StringComparison.Ordinal) < 0,
        "Toggle Favorite NIE ma juz opcji 'speak' (zadna druga wypowiedz na jedno nacisniecie)");
    Sprawdz(sOpcjeToggle != null && sOpcjeToggle.IndexOf("child", StringComparison.Ordinal) >= 0,
        "Toggle Favorite zostaje komenda okna dokumentu ('child') - dziala tylko przy otwartym pliku");

    string sOpcjeClear = OpcjePozycji(lFrame, "Clear Favorite");
    Sprawdz(sOpcjeClear != null, "pozycja menu 'Clear Favorite' istnieje");
    Sprawdz(sOpcjeClear != null && sOpcjeClear.IndexOf("silent", StringComparison.Ordinal) >= 0,
        "Clear Favorite ma opcje 'silent' (jest: '" + (sOpcjeClear == null ? "brak" : sOpcjeClear) + "')");

    // ---------- RODZINA 2: Clear Favorite w koncu MOWI ----------
    Sprawdz(KtorasMaLiteral(lMenu, "Not in favorites"),
        "NOWY komunikat 'Not in favorites' - plik, ktorego na liscie nie bylo, nie konczy sie cisza");
    Sprawdz(KtorasMaLiteral(lMenu, "Removed from favorites"),
        "komunikat 'Removed from favorites' jest (zdjecie z listy)");

    // ---------- RODZINA 3: toggle mowi oba kierunki ----------
    Sprawdz(KtorasMaLiteral(lMenu, "Added to favorites"),
        "komunikat 'Added to favorites' jest (dodanie do listy)");

    // ---------- RODZINA 4: obecnosc pliku czytana wartoscia domyslna ----------
    Sprawdz(KtorasMaWolanie(lMenu, "ReadValue"),
        "handler czyta ReadValue (rozstrzygniecie, czy plik JUZ jest na liscie)");
    Sprawdz(KtorasMaWolanie(lMenu, "DeleteKey"),
        "handler wola DeleteKey (faktyczne zdjecie wpisu z sekcji Favorites)");
    Sprawdz(KtorasMaWolanie(lMenu, "AddMessage"),
        "komunikaty ida przez AddMessage (droga, ktora czytnik slyszy)");

    // ---------- RODZINA 5: numer wersji ----------
    FieldInfo fiVer = (tApp == null) ? null
        : tApp.GetField("VersionString", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
    string sVer = "";
    if (fiVer != null) { try { sVer = (string) fiVer.GetRawConstantValue(); } catch {} }
    Sprawdz(sVer == "5.0.73", "App.VersionString to 5.0.73 (jest: '" + sVer + "')");
    // Stala jest INLINOWANA (pulapka zmierzona przy 5.0.71: w IL nie ma ldsfld),
    // wiec pytamy o literal okna About z DOKLADNA wartoscia stalej.
    Sprawdz(sVer.Length > 0 && KtorasMaLiteral(lMenu, "EdSharpNG " + sVer + " (beta)"),
        "okno About mowi dokladnie wartosc App.VersionString");

    // ---------- REGRESJA SASIEDZTWA: praca z 5.0.72 stoi ----------
    // Ta iteracja dotyka tej samej rodziny funkcji (lista plikow, ulubione), wiec
    // pytamy wprost, czy poprzednie wydanie nie zostalo przy okazji rozbrojone.
    MethodInfo miUsun = (tDialog == null) ? null
        : tDialog.GetMethod("PickFileRemoveSelection", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
    Sprawdz(miUsun != null, "REGRESJA: nosnik z 5.0.72 (PickFileRemoveSelection) nadal istnieje");
    MethodInfo miKopia = (tDialog == null) ? null
        : tDialog.GetMethod("PickFileCopySelection", BindingFlags.NonPublic | BindingFlags.Static);
    Sprawdz(miKopia != null, "REGRESJA: nosnik z 5.0.70 (PickFileCopySelection) nadal istnieje");

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
