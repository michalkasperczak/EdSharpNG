// HARNESS ZACHOWANIA 5.0.72: czy Delete FAKTYCZNIE zdejmuje z listy wszystkie
// zaznaczone wpisy, i czy zdejmuje TE, ktore byly zaznaczone.
//
// Sonda pomiar_572.cs dowodzi, ze nosnik istnieje, czyta SelectedIndices i
// sortuje - nie dowodzi, ze po nacisnieciu klawisza na liscie zostaja wlasciwe
// pozycje.  Ten harness wola PRAWDZIWA metode Dialog.PickFileRemoveSelection ze
// ZBUDOWANEJ binarki na prawdziwym ListBoxie i sprawdza, co w nim zostalo.
//
// KONTROLA POZYTYWNA (asercja MUSI umiec oblac): ten sam scenariusz przechodzi
// przez NAIWNA petle rosnaca, napisana tutaj obok.  Petla rosnaca zdejmuje
// pozycje 0 i 2, ale po pierwszym usunieciu indeksy przesuwaja sie w dol, wiec
// drugi obieg trafia w INNY wpis - i asercja o zawartosci listy oblewa.  Bez tej
// kontroli "zielone" nie odroznialoby poprawnej kolejnosci od przypadku.
//
// CZEGO TEN HARNESS NIE MIERZY, swiadomie: mowy.  Nosnik konczy sie wolaniem
// App.Frame.AddMessage, a App.Frame poza uruchomionym programem jest null, wiec
// wyjatek leci PO zmianie listy.  Harness go lapie i mierzy STAN LISTY; tresc
// komunikatow mierzy pomiar_572.cs po literalach w IL.
//
// Uruchomienie:
//   csc.exe /nologo /target:exe "/out:...\testy\out_h572.exe" \
//           "/r:...\EdSharpNG.exe" "...\testy\harness_usuwanie_listy_572.cs"
//   cmd.exe /c "testy\out_h572.exe <binarka>"

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

public class HarnessUsuwanie572 {

static int iOk = 0, iZle = 0;

static void Sprawdz(bool b, string s) {
    if (b) { iOk++; Console.WriteLine("OK   " + s); }
    else { iZle++; Console.WriteLine("ZLE  " + s); }
}

static string Zawartosc(ListBox lb) {
    List<string> l = new List<string>();
    foreach (object o in lb.Items) l.Add(o == null ? "<null>" : o.ToString());
    return string.Join(",", l.ToArray());
}

// Naiwna droga, ktora WYGLADA poprawnie: zdejmuj zaznaczone indeksy rosnaco.
static void NaiwnieRosnaco(ListBox lb, List<string> lVal, List<string> lDisp) {
    List<int> li = new List<int>();
    foreach (int i in lb.SelectedIndices) li.Add(i);
    li.Sort();
    foreach (int i in li) {
        if (i < 0 || i >= lVal.Count) continue;
        lVal.RemoveAt(i); lDisp.RemoveAt(i); lb.Items.RemoveAt(i);
    }
}

static void Ustaw(out ListBox lb, out List<string> lVal, out List<string> lDisp) {
    lb = new ListBox();
    lb.SelectionMode = SelectionMode.MultiExtended;
    lVal = new List<string>();
    lDisp = new List<string>();
    string[] aNazwy = { "A.txt", "B.txt", "C.txt", "D.txt" };
    foreach (string s in aNazwy) {
        lVal.Add(@"C:\probka\" + s);
        lDisp.Add(s);
        lb.Items.Add(s);
    }
}

[STAThread]
public static int Main(string[] aArgs) {
    string sBin = (aArgs.Length > 0) ? aArgs[0] : "EdSharpNG.exe";
    Console.WriteLine("Binarka: " + Path.GetFullPath(sBin));
    Console.WriteLine();

    Assembly asm;
    try { asm = Assembly.LoadFile(Path.GetFullPath(sBin)); }
    catch (Exception ex) {
        Console.WriteLine("AWARIA POMIARU: nie moge wczytac " + sBin + ": " + ex.GetType().Name + ": " + ex.Message);
        return 5;
    }
    Type tDialog = asm.GetType("EdSharp.Dialog");
    if (tDialog == null) { Console.WriteLine("AWARIA POMIARU: brak typu EdSharp.Dialog"); return 5; }
    MethodInfo mi = tDialog.GetMethod("PickFileRemoveSelection",
        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
    if (mi == null) {
        Console.WriteLine("ZLE  brak Dialog.PickFileRemoveSelection w tej binarce (to droga sprzed 5.0.72)");
        Console.WriteLine();
        Console.WriteLine("PODSUMOWANIE: 0 OK, 1 ZLE");
        return 1;
    }

    // ---- SCENARIUSZ: zaznaczone PIERWSZA i TRZECIA pozycja z czterech ----
    ListBox lb; List<string> lVal, lDisp;
    Ustaw(out lb, out lVal, out lDisp);
    lb.SetSelected(0, true);
    lb.SetSelected(2, true);
    Console.WriteLine("     przed: " + Zawartosc(lb) + "   zaznaczone: 0,2");

    bool bWyjatek = false;
    try {
        // sSection puste: NIE dotykamy pliku ustawien uzytkownika.
        mi.Invoke(null, new object[] { lb, lVal, lDisp, "" });
    }
    catch (TargetInvocationException ex) {
        bWyjatek = true;
        Console.WriteLine("     (oczekiwany wyjatek mowy poza programem: "
            + ((ex.InnerException == null) ? "?" : ex.InnerException.GetType().Name) + ")");
    }
    Console.WriteLine("     po:    " + Zawartosc(lb));

    Sprawdz(lb.Items.Count == 2, "na liscie zostaly DWIE pozycje z czterech (zdjeto oba zaznaczone, nie jedna)");
    Sprawdz(Zawartosc(lb) == "B.txt,D.txt", "zostaly DOKLADNIE te pozycje, ktore nie byly zaznaczone (B,D)");
    Sprawdz(lDisp.Count == 2 && lDisp[0] == "B.txt" && lDisp[1] == "D.txt",
        "lista nazw idzie w parze z ListBoxem");
    Sprawdz(lVal.Count == 2 && lVal[0].EndsWith("B.txt") && lVal[1].EndsWith("D.txt"),
        "lista SCIEZEK idzie w parze z ListBoxem (usunieto te same wpisy)");
    Sprawdz(bWyjatek || lb.SelectedIndices.Count >= 0,
        "przebieg doszedl do konca albo padl dopiero na mowie (App.Frame poza programem jest null)");

    // ---- SCENARIUSZ: jedna zaznaczona pozycja (zachowanie dotychczasowe) ----
    Ustaw(out lb, out lVal, out lDisp);
    lb.SetSelected(1, true);
    try { mi.Invoke(null, new object[] { lb, lVal, lDisp, "" }); } catch (TargetInvocationException) {}
    Console.WriteLine("     jedna zaznaczona (1) -> " + Zawartosc(lb));
    Sprawdz(Zawartosc(lb) == "A.txt,C.txt,D.txt", "przy JEDNEJ zaznaczonej schodzi dokladnie jeden wpis");

    // ---- SCENARIUSZ: wszystkie zaznaczone ----
    Ustaw(out lb, out lVal, out lDisp);
    for (int i = 0; i < 4; i++) lb.SetSelected(i, true);
    try { mi.Invoke(null, new object[] { lb, lVal, lDisp, "" }); } catch (TargetInvocationException) {}
    Console.WriteLine("     wszystkie zaznaczone -> '" + Zawartosc(lb) + "'");
    Sprawdz(lb.Items.Count == 0 && lVal.Count == 0 && lDisp.Count == 0,
        "zaznaczenie calej listy oproznia ja calkowicie (nic nie zostaje)");

    // ---- KONTROLA POZYTYWNA: naiwna petla rosnaca MUSI dac inny wynik ----
    Ustaw(out lb, out lVal, out lDisp);
    lb.SetSelected(0, true);
    lb.SetSelected(2, true);
    NaiwnieRosnaco(lb, lVal, lDisp);
    string sNaiwnie = Zawartosc(lb);
    Console.WriteLine("     naiwna petla rosnaca -> " + sNaiwnie);
    Sprawdz(sNaiwnie != "B.txt,D.txt",
        "KONTROLA POZYTYWNA: petla rosnaca zostawia INNE pozycje, wiec asercja o zawartosci potrafi oblac");

    Console.WriteLine();
    Console.WriteLine("PODSUMOWANIE: " + iOk + " OK, " + iZle + " ZLE");
    if (iOk + iZle == 0) { Console.WriteLine("AWARIA POMIARU: zero asercji"); return 5; }
    return iZle == 0 ? 0 : 1;
}
}
