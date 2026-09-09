// HARNESS ZACHOWANIA 5.0.70: czy schowek FAKTYCZNIE dostaje pliki.
//
// Sonda pomiar_570.cs dowodzi, ze kod wola SetFileDropList - nie dowodzi, ze
// schowek po tym wolaniu zawiera format CF_HDROP, ktory Eksplorator uzna za
// plik.  Ten harness wola PRAWDZIWA metode Util.SetClipboardFileDrop ze
// ZBUDOWANEJ binarki i czyta schowek z powrotem przez API systemowe.
//
// KONTROLA NEGATYWNA: ta sama sonda z argumentem "stara" zapisuje schowek tak,
// jak robil to 5.0.69 (sam Clipboard.SetText ze sciezkami) i MUSI pokazac, ze
// pliku w schowku NIE MA.  Bez tego zielony wynik nie odroznia naprawy od
// tego, ze Windows sam cos dokladа.
//
// Uruchomienie:
//   csc.exe /nologo /target:exe "/out:...\testy\out_h570.exe" \
//           "/r:...\EdSharpNG.exe" "...\testy\harness_schowek_plikow_570.cs"
//   cmd.exe /c "testy\out_h570.exe <binarka> [stara]"

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

public class HarnessSchowek570 {

static int iOk = 0, iZle = 0;

// ODCZYT SCHOWKA W SONDZIE TEZ MUSI MIEC PONOWIENIA.  Zmierzone przy tym
// biegu: jedna asercja kontroli negatywnej oblala RAZ, a w trzech kolejnych
// biegach przeszla - schowek byl chwilowo zajety przez inny proces i
// Clipboard.ContainsText oddal false przy poprawnie zapisanej tresci.  Sonda
// bez ponowien mierzy wiec loterie, a nie zachowanie programu; sam program tej
// wady nie ma, bo chodzi przez Util.GetClipboardText z petla.
static bool MaTekstZ(string sFragment) {
    for (int i = 0; i < 10; i++) {
        try {
            if (Clipboard.ContainsText()) {
                string s = Clipboard.GetText();
                if (s != null && s.Contains(sFragment)) return true;
            }
        }
        catch (Exception) {}
        System.Threading.Thread.Sleep(40);
    }
    return false;
}

static StringCollection PlikiZeSchowka() {
    for (int i = 0; i < 10; i++) {
        try {
            StringCollection sc = Clipboard.GetFileDropList();
            if (sc != null && sc.Count > 0) return sc;
            if (i == 9) return sc;
        }
        catch (Exception) {}
        System.Threading.Thread.Sleep(40);
    }
    return null;
}
static void Sprawdz(bool b, string s) {
    if (b) { iOk++; Console.WriteLine("OK   " + s); }
    else { iZle++; Console.WriteLine("ZLE  " + s); }
}

[STAThread]
public static int Main(string[] aArgs) {
    string sBin = (aArgs.Length > 0) ? aArgs[0] : "EdSharpNG.exe";
    bool bStara = aArgs.Length > 1 && aArgs[1] == "stara";
    Console.WriteLine("Binarka: " + Path.GetFullPath(sBin) + (bStara ? "   (droga 5.0.69, oczekiwany BRAK pliku)" : ""));
    Console.WriteLine();

    // Dwa PRAWDZIWE pliki na dysku - powloka wkleja tylko to, co istnieje.
    string sKat = Path.Combine(Path.GetTempPath(), "edsharp_drop570");
    Directory.CreateDirectory(sKat);
    string sA = Path.Combine(sKat, "pierwszy.txt");
    string sB = Path.Combine(sKat, "drugi.txt");
    File.WriteAllText(sA, "A");
    File.WriteAllText(sB, "B");
    string sTekst = sA + "\r\n" + sB;

    bool bWynik;
    if (bStara) {
        // DROGA SPRZED ZMIANY: sam tekst ze sciezkami.
        Clipboard.SetText(sTekst);
        bWynik = true;
    }
    else {
        Assembly asm = Assembly.LoadFile(Path.GetFullPath(sBin));
        Type tUtil = null;
        foreach (Type t in asm.GetTypes()) if (t.Name == "Util") { tUtil = t; break; }
        if (tUtil == null) { Console.WriteLine("ZLE  brak typu Util"); return 3; }
        MethodInfo mi = tUtil.GetMethod("SetClipboardFileDrop",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (mi == null) { Console.WriteLine("ZLE  brak Util.SetClipboardFileDrop"); return 3; }
        List<string> ls = new List<string>();
        ls.Add(sA); ls.Add(sB);
        bWynik = (bool) mi.Invoke(null, new object[] { ls, sTekst });
    }
    Sprawdz(bWynik, "zapis do schowka zwrocil powodzenie");

    // ---- ODCZYT SCHOWKA PRZEZ API SYSTEMOWE, nie przez nasz kod ----
    StringCollection sc = PlikiZeSchowka();
    int iPlikow = (sc == null) ? 0 : sc.Count;
    Console.WriteLine("     plikow w schowku (CF_HDROP): " + iPlikow);
    if (bStara) {
        Sprawdz(iPlikow == 0,
            "KONTROLA NEGATYWNA: droga 5.0.69 NIE kladzie pliku na schowek (dlatego nie dalo sie wklejac)");
        Sprawdz(MaTekstZ("pierwszy.txt"),
            "droga 5.0.69 kladla sam TEKST ze sciezkami");
    }
    else {
        Sprawdz(iPlikow == 2, "schowek zawiera DWA pliki (CF_HDROP), a nie sam tekst");
        bool bMaOba = iPlikow == 2 && sc.Contains(sA) && sc.Contains(sB);
        Sprawdz(bMaOba, "w schowku sa DOKLADNIE te sciezki, ktore skopiowano");
        // ROWNOLEGLY TEKST: bez tego wklejanie sciezki do dokumentu przestaloby dzialac.
        Sprawdz(MaTekstZ("pierwszy.txt"),
            "obok plikow w schowku jest TEKST ze sciezkami (wklejanie do dokumentu bez zmian)");
        // DROPEFFECT: bez tego czesc powlok PRZENOSI plik zamiast kopiowac,
        // czyli plik znika ze zrodla bez zadnego sygnalu dla niewidomego.
        object oEffect = Clipboard.GetData("Preferred DropEffect");
        int iEffect = -1;
        MemoryStream ms = oEffect as MemoryStream;
        if (ms != null) {
            byte[] a = ms.ToArray();
            if (a.Length >= 4) iEffect = BitConverter.ToInt32(a, 0);
        }
        Console.WriteLine("     Preferred DropEffect = " + iEffect + " (Copy=" + (int) DragDropEffects.Copy + ")");
        Sprawdz(iEffect == (int) DragDropEffects.Copy,
            "schowek mowi powloce KOPIUJ, wiec wklejenie nie przenosi pliku ze zrodla");
        // ROZLACZNOSC: plik na dysku zostal nietkniety.
        Sprawdz(File.Exists(sA) && File.Exists(sB),
            "same pliki na dysku sa nietkniete (kopiowanie niczego nie przenioslo)");
    }

    Console.WriteLine();
    Console.WriteLine("PODSUMOWANIE: " + iOk + " OK, " + iZle + " ZLE");
    return iZle == 0 ? 0 : 1;
}
}
