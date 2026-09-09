// POMIAR 5.0.71: JEDNO ZRODLO PRAWDY O WERSJI ORAZ AKTUALIZACJA Z NASZEGO REPO.
//
// CO ZMIERZONE PRZED ZMIANA (na binarce 5.0.70, commit 83f1b52):
//   a) App.VersionString = "5.0.0", a okno About mialo literal
//      "EdSharpNG 5.0.1 (beta)" i stala date "August 5, 2026".  Instalator mowil
//      5.0.70.  Trzy rozne numery tej samej rzeczy - po zainstalowaniu paczki
//      nie bylo JAK sprawdzic, ktora wersje sie ma.
//   b) Komenda Elevate Version (F11) miala sOwnerRepo = "JamalMazrui/EdSharp",
//      czyli repozytorium autora ORYGINALNEGO programu, i pobierala z niego
//      "EdSharp_Setup.exe" (dzis tag v5.0.40), a nastepnie URUCHAMIALA ten
//      instalator.  Klawisz nazwany "aktualizuj" nadpisywal EdSharpNG cudzym
//      programem, bez ostrzezenia.
//
// Rodziny asercji:
//   1. WERSJA JAKO STALA: App.VersionString istnieje, jest 5.0.71 i NIE jest
//      juz "5.0.0" ani "5.0.1".
//   2. OKNO About CZYTA STALA, nie literal: cialo obslugi menu wola
//      App.VersionString oraz Util.GetProgramBuildDate, a starych literalow
//      "EdSharpNG 5.0.1 (beta)" i "August 5, 2026" w nim NIE MA.
//   3. NOSNIK DATY istnieje, jest statyczny, zwraca string i bierze date z PLIKU
//      programu (Assembly.GetExecutingAssembly + File.GetLastWriteTime), a nie
//      z DateTime.Now - inaczej mowilby dzisiejszy dzien zamiast dnia wydania.
//   4. AKTUALIZACJA Z NASZEGO REPO: cialo ElevateVersion niesie literal
//      "michaldziwisz/EdSharp" i "EdSharpNG_Setup.exe", a starych
//      "JamalMazrui/EdSharp" oraz "EdSharp_Setup.exe" w nim NIE MA.  Ta druga
//      polowa jest ROZLACZNOSCIA: dopisanie nowego adresu obok starego tez
//      byloby zielone przy samej asercji "nowy jest".
//   5. BRAK WYDANIA TO NIE AWARIA SIECI: FetchLatestReleaseTag ma przeciazenie
//      z parametrem wyjsciowym na kod HTTP, getPage tez, a ElevateVersion mowi
//      osobnym komunikatem o braku wydania.
//
// Pomiar po SYGNATURACH i CIALACH METOD (ldstr 0x72 + ResolveString,
// call/callvirt/newobj + ResolveMember), nie po napisach w pliku.
//
// PULAPKA, ktora ten plik obchodzi (ta sama, co w pomiar_567 i 568): cialo
// domkniecia (delegata, lambdy) kompilator przenosi do metody GENEROWANEJ o
// nazwie zawierajacej nazwe macierzysta, wiec pytanie o cialo samej metody
// nosnika daje falszywe zero.  Dlatego literaly i wolania szukamy TAKZE w
// metodach generowanych danego nosnika (MetodyZNosnikiem).
//
// Uruchomienie (katalog roboczy MUSI byc na dysku Windows - dla .NET /tmp jest
// lokalizacja siecowa \\wsl.localhost i Assembly.LoadFile rzuca wyjatek):
//   csc.exe /nologo /target:exe "/out:D:\projekty\edsharp-pr\testy\out_571.exe" \
//           "/r:D:\projekty\edsharp-pr\EdSharpNG.exe" \
//           "D:\projekty\edsharp-pr\testy\pomiar_571.cs"
//   cmd.exe /c "testy\out_571.exe EdSharpNG.exe"
//
// KONTROLA NEGATYWNA: testy/kontrola_negatywna_571.sh na binarce 5.0.70.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

class Pomiar571 {

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
    // Typy zagniezdzone to m.in. klasy domkniec kompilatora.
    try {
        foreach (Type tn in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            l.AddRange(WszystkieMetody(tn));
    } catch {}
    return l;
}

// Metoda o tej nazwie ORAZ metody generowane, ktorych nazwa ja zawiera.
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

static int Main(string[] args) {
    string sExe = (args.Length > 0) ? args[0] : "EdSharpNG.exe";
    Assembly asm;
    try { asm = Assembly.LoadFile(Path.GetFullPath(sExe)); }
    catch (Exception ex) {
        Console.WriteLine("AWARIA POMIARU: nie moge wczytac " + sExe + ": " + ex.GetType().Name + ": " + ex.Message);
        return 5;
    }

    Type tApp   = asm.GetType("EdSharp.App");
    Type tUtil  = asm.GetType("EdSharp.Util");
    Type tFrame = asm.GetType("EdSharp.MdiFrame");
    Type tWeb   = null;
    foreach (Type t in asm.GetTypes()) if (t.FullName == "Homer.Web") { tWeb = t; break; }

    Sprawdz(tApp   != null, "typ EdSharp.App istnieje");
    Sprawdz(tUtil  != null, "typ EdSharp.Util istnieje");
    Sprawdz(tFrame != null, "typ EdSharp.MdiFrame istnieje");
    Sprawdz(tWeb   != null, "typ Homer.Web istnieje");

    // ---------- RODZINA 1: wersja jako jedna stala ----------
    FieldInfo fiVer = (tApp == null) ? null
        : tApp.GetField("VersionString", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
    Sprawdz(fiVer != null, "App.VersionString istnieje");
    string sVer = "";
    if (fiVer != null) { try { sVer = (string) fiVer.GetRawConstantValue(); } catch {} }
    Sprawdz(sVer == "5.0.71", "App.VersionString to 5.0.71 (jest: '" + sVer + "')");
    Sprawdz(sVer != "5.0.0", "App.VersionString NIE jest juz starym 5.0.0");
    Sprawdz(sVer != "5.0.1", "App.VersionString NIE jest 5.0.1 (stary napis okna About)");

    // ---------- RODZINA 2: okno About czyta stala, nie literal ----------
    // Obsluga menu to jedna wielka metoda menuItem_Click w MdiFrame (plus jej
    // metody generowane).  Pytamy o CALY nosnik i jego domkniecia.
    List<MethodBase> lMenu = MetodyZNosnikiem(tFrame, "menuItem_Click");
    Sprawdz(lMenu.Count > 0, "MdiFrame.menuItem_Click istnieje (nosnik obslugi menu)");
    // PULAPKA POMIARU ZLAPANA W TYM BIEGU (nie blad kodu): App.VersionString to
    // const, a const kompilator INLINUJE w miejscu uzycia - w IL nie ma zadnego
    // ldsfld, wiec pytanie "czy wola stala" musi oblac przy POPRAWNYM kodzie.
    // Zmierzone sonda testy/gdzie_571.cs: w ciele obslugi menu stoi literal
    // "EdSharpNG 5.0.71 (beta)".
    // Asercja przepisana i przy tym MOCNIEJSZA: napis okna About musi zawierac
    // DOKLADNIE aktualna wartosc App.VersionString.  To wiaze oba miejsca - na
    // 5.0.70 stala miala 5.0.0, a okno mowilo 5.0.1, wiec ta sama asercja tam
    // oblewa (patrz kontrola negatywna).
    Sprawdz(sVer.Length > 0 && KtorasMaLiteral(lMenu, "EdSharpNG " + sVer + " (beta)"),
        "napis okna About zawiera DOKLADNIE wartosc App.VersionString (jedno zrodlo prawdy)");
    Sprawdz(KtorasMaWolanie(lMenu, "GetProgramBuildDate"),
        "okno About wola Util.GetProgramBuildDate (data z pliku, nie napis)");
    Sprawdz(!KtorasMaLiteral(lMenu, "EdSharpNG 5.0.1 (beta)"),
        "STARY literal 'EdSharpNG 5.0.1 (beta)' zniknal z obslugi menu");
    Sprawdz(!KtorasMaLiteral(lMenu, "August 5, 2026"),
        "STARA stala data 'August 5, 2026' zniknela z obslugi menu");
    Sprawdz(KtorasMaLiteral(lMenu, "(beta)"),
        "okno About nadal mowi '(beta)' (numer skladany, nie wycieta etykieta)");

    // ---------- RODZINA 3: nosnik daty wydania ----------
    MethodInfo miData = (tUtil == null) ? null
        : tUtil.GetMethod("GetProgramBuildDate", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
    Sprawdz(miData != null, "Util.GetProgramBuildDate istnieje");
    Sprawdz(miData != null && miData.IsStatic, "GetProgramBuildDate jest statyczna");
    Sprawdz(miData != null && miData.ReturnType == typeof(string), "GetProgramBuildDate zwraca string");
    Sprawdz(miData != null && miData.GetParameters().Length == 0, "GetProgramBuildDate nie bierze parametrow");
    Sprawdz(miData != null && MaWolanie(miData, "GetExecutingAssembly"),
        "GetProgramBuildDate pyta o PLIK programu (GetExecutingAssembly)");
    Sprawdz(miData != null && MaWolanie(miData, "GetLastWriteTime"),
        "GetProgramBuildDate bierze CZAS ZAPISU pliku (GetLastWriteTime)");
    // Rozlacznosc: data wydania nie moze byc dzisiejsza data.
    Sprawdz(miData != null && !MaWolanie(miData, "get_Now"),
        "GetProgramBuildDate NIE uzywa DateTime.Now (to byla by data uruchomienia)");

    // ---------- RODZINA 4: aktualizacja z NASZEGO repozytorium ----------
    List<MethodBase> lElev = MetodyZNosnikiem(tFrame, "ElevateVersion");
    Sprawdz(lElev.Count > 0, "MdiFrame.ElevateVersion istnieje");
    Sprawdz(KtorasMaLiteral(lElev, "michaldziwisz/EdSharp"),
        "ElevateVersion siega do NASZEGO repozytorium michaldziwisz/EdSharp");
    Sprawdz(!KtorasMaLiteral(lElev, "JamalMazrui/EdSharp"),
        "ROZLACZNOSC: literal upstreamu JamalMazrui/EdSharp zniknal z ElevateVersion");
    Sprawdz(KtorasMaLiteral(lElev, "EdSharpNG_Setup.exe"),
        "ElevateVersion pobiera NASZ artefakt EdSharpNG_Setup.exe");
    Sprawdz(!KtorasMaLiteral(lElev, "\"EdSharp_Setup.exe\"") && !MaDokladny(lElev, "EdSharp_Setup.exe"),
        "ROZLACZNOSC: stara nazwa pliku EdSharp_Setup.exe zniknela (dokladne dopasowanie)");

    // ---------- RODZINA 5: brak wydania to nie awaria sieci ----------
    Sprawdz(tUtil != null && MaPrzeciazenieZKodem(tUtil, "FetchLatestReleaseTag"),
        "Util.FetchLatestReleaseTag ma przeciazenie z parametrem wyjsciowym na kod HTTP");
    Sprawdz(tUtil != null && tUtil.GetMethod("FetchLatestReleaseTag", new Type[] { typeof(string) }) != null,
        "stare jednoargumentowe FetchLatestReleaseTag ZOSTAJE (inni wolajacy nie padaja)");
    Sprawdz(tWeb != null && MaPrzeciazenieZKodem(tWeb, "getPage"),
        "Homer.Web.getPage ma przeciazenie oddajace kod HTTP");
    Sprawdz(KtorasMaLiteral(lElev, "No release has been published"),
        "ElevateVersion mowi OSOBNO o braku wydania");
    Sprawdz(KtorasMaLiteral(lElev, "check your internet connection"),
        "ElevateVersion nadal mowi o awarii sieci, gdy odpowiedzi nie bylo wcale");
    // Ta sama pulapka const: numer wersji w komunikacie jest INLINOWANY jako
    // literal, wiec pytamy o literal z DOKLADNA wartoscia stalej.
    Sprawdz(sVer.Length > 0 && KtorasMaLiteral(lElev, sVer),
        "komunikat o braku wydania podaje NUMER uruchomionej wersji (wartosc stalej)");

    Console.WriteLine();
    Console.WriteLine("WYNIK: " + iOk + " OK / " + iZle + " ZLE");
    // BRAMKA: zero asercji RAZEM znaczy awarie pomiaru, nie zielony wynik
    // (pulapka zmierzona przy 5.0.70: sonda nie startowala i milczala).
    if (iOk + iZle == 0) {
        Console.WriteLine("AWARIA POMIARU: zero asercji wykonanych");
        return 5;
    }
    return (iZle == 0) ? 0 : 1;
}

// Dokladne dopasowanie literalu (nie podciag) - pulapka prefiksu zmierzona przy
// 5.0.70: "EdSharp_Setup.exe" jest PODCIAGIEM "EdSharpNG_Setup.exe"? NIE jest,
// ale odwrotnie tez trzeba sprawdzic, wiec porownujemy CALE napisy.
static bool MaDokladny(List<MethodBase> l, string sSzukany) {
    foreach (MethodBase mb in l) {
        byte[] aIl = Il(mb);
        if (aIl == null) continue;
        Module mod = mb.Module;
        for (int i = 0; i + 4 < aIl.Length; i++) {
            if (aIl[i] != 0x72) continue;
            int iToken = BitConverter.ToInt32(aIl, i + 1);
            string s = null;
            try { s = mod.ResolveString(iToken); } catch { continue; }
            if (s == sSzukany) return true;
        }
    }
    return false;
}

// Czy typ ma metode o tej nazwie z parametrem WYJSCIOWYM typu int.
static bool MaPrzeciazenieZKodem(Type t, string sNazwa) {
    BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    foreach (MethodInfo mi in t.GetMethods(bf)) {
        if (mi.Name != sNazwa) continue;
        foreach (ParameterInfo pi in mi.GetParameters())
            if (pi.IsOut && pi.ParameterType == typeof(int).MakeByRefType()) return true;
    }
    return false;
}

}
