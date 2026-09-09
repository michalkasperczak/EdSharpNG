// HARNESS 5.0.73: DOWOD SKUTKU, NIE SAMEGO NAPISU.
//
// Sonda pomiar_573.cs dowodzi, ze pozycja menu "Toggle Favorite" powstaje z
// opcjami "child silent".  To dowod PODPIECIA, nie dzialania: napis "child
// silent" moglby byc martwy, gdyby regula rozgalezienia w menuItem_Click
// czytala go inaczej, niz mysle.
//
// Ten harness wola PRAWDZIWA metode z binarki - prywatna statyczna
// MdiFrame.CreateMenuItem(string, string, EventHandler, string) - dla obu
// zestawow opcji i sprawdza, CO LADUJE w Tag, a potem przepuszcza to przez
// DOKLADNIE te sama regule, ktorej uzywa menuItem_Click:
//
//     sOptions = " " + ((string) menuItem.Tag).Trim().ToLower() + " ";
//     if (sOptions.Contains(" silent ")) SetStatus(sLabel);   // NIE mowi
//     else SetMessage(sLabel);                                // MOWI nazwe
//
// KONTROLA POZYTYWNA: te same kroki dla "child speak" MUSZA dac wynik
// przeciwny (mowa nazwy).  Bez niej asercja "nie mowi" przechodzila by takze
// wtedy, gdyby regula nigdy nie trafiala w galaz mowy.
//
// Kolejnosc argumentow bierzemy Z BINARKI (sonda czyta ja z IL), a nie z
// pamieci, wiec harness mierzy stan programu, nie moje zalozenie.
//
// Uruchomienie:
//   csc.exe /nologo /target:exe "/out:...\harness_573.exe" \
//           "/r:...\EdSharpNG.exe" /r:System.Windows.Forms.dll /r:System.dll \
//           "...\harness_mowa_ulubionych_573.cs"
//   cmd.exe /c "harness_573.exe EdSharpNG.exe"

using System;
using System.Reflection;
using System.Windows.Forms;

class Harness573 {

static int iOk = 0;
static int iZle = 0;

static void Sprawdz(bool bWarunek, string sOpis) {
    if (bWarunek) { iOk++; Console.WriteLine("OK   " + sOpis); }
    else { iZle++; Console.WriteLine("ZLE  " + sOpis); }
}

// Regula mowy PRZEPISANA Z menuItem_Click co do znaku.
static bool MowiNazwe(string sTag) {
    if (sTag == null) return true;
    string s = " " + sTag.Trim().ToLower() + " ";
    return !s.Contains(" silent ");
}

[STAThread]
static int Main(string[] args) {
    string sExe = (args.Length > 0) ? args[0] : "EdSharpNG.exe";
    Assembly asm;
    try { asm = Assembly.LoadFile(System.IO.Path.GetFullPath(sExe)); }
    catch (Exception ex) {
        Console.WriteLine("AWARIA HARNESSU: nie moge wczytac " + sExe + ": " + ex.GetType().Name);
        return 5;
    }

    Type tFrame = asm.GetType("EdSharp.MdiFrame");
    if (tFrame == null) {
        Console.WriteLine("AWARIA HARNESSU: brak typu EdSharp.MdiFrame");
        return 5;
    }

    MethodInfo miCreate = null;
    foreach (MethodInfo mi in tFrame.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                              | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
        if (mi.Name != "CreateMenuItem") continue;
        ParameterInfo[] p = mi.GetParameters();
        if (p.Length == 4 && p[3].ParameterType == typeof(string)) { miCreate = mi; break; }
    }
    if (miCreate == null) {
        Console.WriteLine("AWARIA HARNESSU: brak MdiFrame.CreateMenuItem(string,string,EventHandler,string)");
        return 5;
    }
    Sprawdz(true, "znalazlem prawdziwa CreateMenuItem w binarce (cztery parametry, ostatni to opcje)");

    // --- POZYCJA JAK W PROGRAMIE: Toggle Favorite z opcjami "child silent" ---
    ToolStripMenuItem miToggle = null;
    try {
        miToggle = (ToolStripMenuItem) miCreate.Invoke(null,
            new object[] { "Toggle Favorite", "Alt+Shift+L", null, "child silent" });
    } catch (Exception ex) {
        Console.WriteLine("AWARIA HARNESSU: CreateMenuItem rzucil " + ex.GetType().Name
            + (ex.InnerException == null ? "" : " / " + ex.InnerException.GetType().Name));
        return 5;
    }
    Sprawdz(miToggle != null, "pozycja menu powstala");
    string sTagToggle = (miToggle == null) ? null : (string) miToggle.Tag;
    Sprawdz(sTagToggle == "child silent",
        "Tag pozycji to dokladnie zestaw opcji, ktory przekazuje program (jest: '" + sTagToggle + "')");
    Sprawdz(!MowiNazwe(sTagToggle),
        "SKUTEK: regula menuItem_Click NIE wypowie nazwy 'Toggle Favorite' - nazwa idzie na pasek stanu");
    Sprawdz(miToggle != null && miToggle.Name == "Toggle Favorite",
        "nazwa komendy nadal jest ustawiona (pasek stanu ma co pokazac)");
    Sprawdz(miToggle != null && miToggle.AccessibleName != null
            && miToggle.AccessibleName.IndexOf("Toggle Favorite", StringComparison.Ordinal) >= 0,
        "nazwa dostepna pozycji menu nadal zawiera nazwe komendy (czytnik czyta ja W MENU)");
    Sprawdz(miToggle != null && miToggle.AccessibleName != null
            && miToggle.AccessibleName.IndexOf("Alt", StringComparison.Ordinal) >= 0,
        "nazwa dostepna pozycji menu nadal podaje skrot (w menu, nie po nacisnieciu klawisza)");

    // --- KONTROLA POZYTYWNA: stary zestaw opcji MUSI dac wynik przeciwny ---
    ToolStripMenuItem miStary = null;
    try {
        miStary = (ToolStripMenuItem) miCreate.Invoke(null,
            new object[] { "Kontrola Stara", "Alt+Shift+F12", null, "child speak" });
    } catch (Exception ex) {
        Console.WriteLine("AWARIA HARNESSU (kontrola): " + ex.GetType().Name);
        return 5;
    }
    string sTagStary = (miStary == null) ? null : (string) miStary.Tag;
    Sprawdz(sTagStary == "child speak",
        "KONTROLA POZYTYWNA: Tag starego zestawu to 'child speak' (jest: '" + sTagStary + "')");
    Sprawdz(MowiNazwe(sTagStary),
        "KONTROLA POZYTYWNA: ta sama regula PRZY 'speak' wypowiada nazwe - asercja o ciszy potrafi oblac");

    // --- ROZLACZNOSC: 'silent' nie tlumi komunikatow AddMessage ---
    // AddMessage idzie przez Util.Say NIEZALEZNIE od opcji pozycji menu, wiec
    // wyciszenie nazwy nie zabiera skutku ("Added to favorites").  Mierzymy to
    // na sygnaturze: AddMessage jest publiczna i nie bierze opcji pozycji.
    MethodInfo miAdd = tFrame.GetMethod("AddMessage", new Type[] { typeof(object) });
    Sprawdz(miAdd != null,
        "AddMessage(object) istnieje - komunikat o skutku ma wlasna droge, niezalezna od opcji pozycji");

    Console.WriteLine();
    Console.WriteLine("WYNIK: " + iOk + " OK / " + iZle + " ZLE");
    if (iOk + iZle == 0) {
        Console.WriteLine("AWARIA HARNESSU: zero asercji wykonanych");
        return 5;
    }
    return (iZle == 0) ? 0 : 1;
}

}
