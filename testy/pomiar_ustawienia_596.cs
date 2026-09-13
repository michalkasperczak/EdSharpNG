// pomiar_ustawienia_596.cs -- POMIAR OKNA USTAWIEN (zadanie 7, 13.09.2026).
//
// CO MIERZY: czy okno na Control+przecinek sklada sie z POL WYBORU, a nie z
// pol tekstowych, i czy kazde pole ma nazwe, ktora przeczyta czytnik ekranu.
//
// DLACZEGO NIE PRZEZ WYSYLANIE KLAWISZY: pomiar chodzi z sesji agenta (SSH do
// WSL), a z niej SetForegroundWindow zwraca False - okno EdSharpa nie da sie
// wyniesc na wierzch, wiec SendKeys trafia w pustke i sonda oblewa nawet gdy
// program jest dobry.  To zmierzone, nie zgadniete: wczesniejsza wersja tej
// sondy wlasnie tak oblewala, a tytul okna caly czas potwierdzal, ze program
// dziala.  Falszywy alarm jest gorszy od braku pomiaru, dlatego okno powstaje
// TUTAJ, w tym procesie: ladujemy WYDANA binarke, wolamy jej wlasny spis
// ustawien i skladamy z niego dialog tymi samymi metodami LbcDialog, ktorych
// uzywa PokazUstawienia.  Mierzone sa PRAWDZIWE kontrolki WinForms - ich typ i
// AccessibleName, czyli dokladnie to, co czytnik ekranu widzi.
//
// CZEGO TA SONDA NIE MIERZY, ZEBY BYLO JASNE: nie mierzy, ze Control+przecinek
// jest podlaczony do tego okna (to sprawdza asercja w weryfikuj_liste_testow.py
// po IL metody menuItem_Click) ani tego, co NVDA naprawde powie.
//
// PULAPKA, KTORA ROZSTRZYGA KSZTALT ASERCJI: sonda liczaca same kontrolki
// przeszlaby TAKZE na starym oknie MultiInput - tam tez byly kontrolki, tylko
// wszystkie tekstowe.  Dlatego asercje sa ROZNICUJACE: wymagaja co najmniej
// pieciu przelacznikow i pieciu list wyboru, a pol tekstowych najwyzej osmiu.
// Kontrola negatywna: uruchomiona na binarce 5.0.95 (bez Ustawienia.cs) sonda
// nie znajduje typu Ustawienia i oblewa pierwsza asercje.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

public class PomiarUstawienia
{
    static int iOk = 0, iBlad = 0;

    static void Sprawdz(string sCo, bool bWarunek, string sSzczegol)
    {
        if (bWarunek) { iOk++; Console.WriteLine("    OK   " + sCo); }
        else { iBlad++; Console.WriteLine("    BLAD " + sCo + "   [" + sSzczegol + "]"); }
    }

    static string Pole(object o, string sNazwa)
    {
        FieldInfo fi = o.GetType().GetField(sNazwa);
        if (fi == null) return null;
        object v = fi.GetValue(o);
        return v == null ? null : v.ToString();
    }

    static string[] PoleTablica(object o, string sNazwa)
    {
        FieldInfo fi = o.GetType().GetField(sNazwa);
        if (fi == null) return null;
        return fi.GetValue(o) as string[];
    }

    [STAThread]
    public static int Main(string[] args)
    {
        string sExe = args.Length > 0 ? args[0] : "EdSharpNG.exe";
        Assembly asm = Assembly.LoadFrom(sExe);

        Console.WriteLine("=== 1. Spis ustawien jest w WYDANEJ binarce");
        Type tUst = asm.GetType("Ustawienia");
        Sprawdz("binarka ma klase Ustawienia", tUst != null, "nie ma typu Ustawienia");
        if (tUst == null) { Console.WriteLine("WYNIK: " + iOk + " OK, " + iBlad + " BLAD"); return 1; }

        MethodInfo miSpis = tUst.GetMethod("Spis", BindingFlags.Public | BindingFlags.Static);
        Sprawdz("jest metoda Spis, ktora zwraca liste ustawien", miSpis != null, "brak metody");
        if (miSpis == null) { Console.WriteLine("WYNIK: " + iOk + " OK, " + iBlad + " BLAD"); return 1; }

        IEnumerable lSpis = miSpis.Invoke(null, null) as IEnumerable;
        List<object> lOpcje = new List<object>();
        foreach (object o in lSpis) lOpcje.Add(o);
        Sprawdz("spis ma co najmniej 15 ustawien", lOpcje.Count >= 15, "jest " + lOpcje.Count);

        Console.WriteLine();
        Console.WriteLine("=== 2. Rodzaje pol: wybor zamiast wpisywania");
        int iPrzel = 0, iLista = 0, iLiczba = 0, iTekst = 0;
        foreach (object o in lOpcje)
        {
            string sR = Pole(o, "Rodzaj");
            if (sR == "przelacznik") iPrzel++;
            else if (sR == "lista") iLista++;
            else if (sR == "liczba") iLiczba++;
            else iTekst++;
        }
        Console.WriteLine("    zmierzone: " + iPrzel + " przelacznikow, " + iLista + " list wyboru, "
            + iLiczba + " licznikow, " + iTekst + " pol tekstowych");
        Sprawdz("co najmniej 5 przelacznikow zamiast wpisywania Y albo N", iPrzel >= 5, "jest " + iPrzel);
        Sprawdz("co najmniej 5 list wyboru zamiast wpisywania wartosci", iLista >= 5, "jest " + iLista);
        Sprawdz("licznik z granicami dla liczby plikow w historii", iLiczba >= 1, "jest " + iLiczba);
        Sprawdz("pol tekstowych najwyzej 8", iTekst <= 8, "jest " + iTekst);
        Sprawdz("pol wyboru wiecej niz pol tekstowych", iPrzel + iLista + iLiczba > iTekst,
            (iPrzel + iLista + iLiczba) + " vs " + iTekst);

        Console.WriteLine();
        Console.WriteLine("=== 3. Kazda lista wyboru jest spojna i ma wartosc domyslna na liscie");
        int iBrakDomyslnej = 0, iZlaDlugosc = 0;
        string sZle = "";
        foreach (object o in lOpcje)
        {
            if (Pole(o, "Rodzaj") != "lista") continue;
            string[] aNazwy = PoleTablica(o, "Nazwy");
            string[] aWart = PoleTablica(o, "Wartosci");
            string sKlucz = Pole(o, "Klucz");
            if (aNazwy == null || aWart == null || aNazwy.Length != aWart.Length)
            {
                iZlaDlugosc++; sZle += sKlucz + " ";
                continue;
            }
            string sDom = Pole(o, "Domyslna") ?? "";
            bool bJest = false;
            foreach (string s in aWart) if (s == sDom) bJest = true;
            if (!bJest) { iBrakDomyslnej++; sZle += sKlucz + "(dom) "; }
        }
        Sprawdz("kazda lista ma tyle samo nazw co wartosci", iZlaDlugosc == 0, sZle);
        Sprawdz("wartosc domyslna kazdej listy jest na tej liscie", iBrakDomyslnej == 0, sZle);

        Console.WriteLine();
        Console.WriteLine("=== 4. Prawdziwe kontrolki: typ i nazwa dla czytnika ekranu");
        // Dialog powstaje w tym procesie i NIE jest pokazywany: interesuja nas
        // typy kontrolek i ich AccessibleName, a nie wyglad.
        // LbcDialog siedzi w przestrzeni nazw Homer (Lbc.cs, "namespace Homer").
        // Sama nazwa bez przestrzeni zwraca null - zmierzone.
        Type tDlg = asm.GetType("Homer.LbcDialog");
        if (tDlg == null) tDlg = asm.GetType("LbcDialog");
        Sprawdz("binarka ma klase LbcDialog", tDlg != null, "brak typu");
        if (tDlg == null) { Console.WriteLine("WYNIK: " + iOk + " OK, " + iBlad + " BLAD"); return 1; }

        object dlg = Activator.CreateInstance(tDlg, new object[] { "Settings", null });
        MethodInfo miCheck = tDlg.GetMethod("addCheckBox", new Type[] { typeof(string), typeof(bool), typeof(string) });
        MethodInfo miCombo = tDlg.GetMethod("addComboPickBox", new Type[] { typeof(string), typeof(IList<string>), typeof(string), typeof(string) });
        MethodInfo miNum = tDlg.GetMethod("addNumericUpDown", new Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(string) });
        MethodInfo miInput = tDlg.GetMethod("addInputBox", new Type[] { typeof(string), typeof(string), typeof(string) });
        Sprawdz("LbcDialog ma metody dla przelacznika, listy, licznika i pola tekstowego",
            miCheck != null && miCombo != null && miNum != null && miInput != null,
            "check=" + (miCheck != null) + " combo=" + (miCombo != null) + " num=" + (miNum != null) + " input=" + (miInput != null));
        if (miCheck == null || miCombo == null || miNum == null || miInput == null)
        { Console.WriteLine("WYNIK: " + iOk + " OK, " + iBlad + " BLAD"); return 1; }

        MethodInfo miPomijany = tUst.GetMethod("Pomijany", BindingFlags.Public | BindingFlags.Static);
        List<Control> lKontrolki = new List<Control>();
        foreach (object o in lOpcje)
        {
            string sKlucz = Pole(o, "Klucz");
            if (miPomijany != null && (bool) miPomijany.Invoke(null, new object[] { sKlucz })) continue;
            string sEt = Pole(o, "Etykieta");
            string sTip = Pole(o, "Podpowiedz");
            string sR = Pole(o, "Rodzaj");
            Control c;
            if (sR == "przelacznik") c = (Control) miCheck.Invoke(dlg, new object[] { sEt, false, sTip });
            else if (sR == "liczba") c = (Control) miNum.Invoke(dlg, new object[] { sEt, 100, 5, 500, sTip });
            else if (sR == "lista")
            {
                List<string> lN = new List<string>(PoleTablica(o, "Nazwy"));
                c = (Control) miCombo.Invoke(dlg, new object[] { sEt, lN, lN[0], sTip });
            }
            else c = (Control) miInput.Invoke(dlg, new object[] { sEt, "", sTip });
            lKontrolki.Add(c);
        }

        int iCheckB = 0, iComboB = 0, iNumB = 0, iTextB = 0, iBezNazwy = 0;
        string sBez = "";
        foreach (Control c in lKontrolki)
        {
            if (c is CheckBox) iCheckB++;
            else if (c is ComboBox) iComboB++;
            else if (c is NumericUpDown) iNumB++;
            else if (c is TextBox) iTextB++;

            string sN = c.AccessibleName;
            // CheckBox mowi swoja nazwe wlasnym napisem, wiec tam liczy sie Text.
            if (c is CheckBox && (sN == null || sN.Trim().Length == 0)) sN = c.Text;
            if (sN == null || sN.Trim().Length == 0) { iBezNazwy++; sBez += c.GetType().Name + " "; }
        }
        Console.WriteLine("    zlozone kontrolki: " + iCheckB + " CheckBox, " + iComboB + " ComboBox, "
            + iNumB + " NumericUpDown, " + iTextB + " TextBox");
        Sprawdz("przelaczniki to naprawde CheckBox", iCheckB >= 5, "jest " + iCheckB);
        Sprawdz("listy wyboru to naprawde ComboBox", iComboB >= 5, "jest " + iComboB);
        Sprawdz("licznik to naprawde NumericUpDown", iNumB >= 1, "jest " + iNumB);
        Sprawdz("kazda kontrolka ma nazwe czytana przez czytnik ekranu", iBezNazwy == 0, iBezNazwy + " bez nazwy: " + sBez);

        // Lista wyboru musi byc ZAMKNIETA: czlowiek wybiera z niej, a nie wpisuje.
        int iOtwarte = 0;
        foreach (Control c in lKontrolki)
        {
            ComboBox cb = c as ComboBox;
            if (cb != null && cb.DropDownStyle != ComboBoxStyle.DropDownList) iOtwarte++;
        }
        Sprawdz("z listy wyboru mozna tylko wybrac, nie wpisac wlasnej wartosci", iOtwarte == 0, iOtwarte + " list do wpisywania");

        Console.WriteLine();
        Console.WriteLine("=== 5. Tolerancja na to, co juz lezy w pliku ustawien");
        MethodInfo miCzy = tUst.GetMethod("CzyWlaczone", BindingFlags.Public | BindingFlags.Static);
        Sprawdz("jest funkcja czytajaca wlaczone i wylaczone", miCzy != null, "brak CzyWlaczone");
        if (miCzy != null)
        {
            string[] aWl = new string[] { "Y", "y", "Yes", "yes", "T", "tak", "1", "on", "\"Y\"" };
            string[] aWyl = new string[] { "N", "n", "No", "", "   ", "0", "off", "cokolwiek" };
            int iZle = 0; string sZ = "";
            foreach (string s in aWl) if (!(bool) miCzy.Invoke(null, new object[] { s })) { iZle++; sZ += "[" + s + "] "; }
            foreach (string s in aWyl) if ((bool) miCzy.Invoke(null, new object[] { s })) { iZle++; sZ += "[" + s + "] "; }
            Sprawdz("wlaczone rozpoznane takze jako y, yes, tak, 1, on; reszta jako wylaczone", iZle == 0, sZ);
            Sprawdz("pusta wartosc nie znaczy wlaczone", !(bool) miCzy.Invoke(null, new object[] { "" }), "puste czytane jako wlaczone");
            Sprawdz("null nie wywala programu", !(bool) miCzy.Invoke(null, new object[] { null }), "null wywalil");
        }

        MethodInfo miKan = tUst.GetMethod("Kanoniczne", BindingFlags.Public | BindingFlags.Static);
        if (miKan != null)
        {
            Sprawdz("zapisujemy Y dla wlaczonego", (string) miKan.Invoke(null, new object[] { true }) == "Y", "nie Y");
            Sprawdz("zapisujemy N dla wylaczonego", (string) miKan.Invoke(null, new object[] { false }) == "N", "nie N");
        }

        // Klucz z ampersandem w nazwie (E&xtraSpeech) i klucze z wlasnym oknem.
        if (miPomijany != null)
        {
            Sprawdz("wycofany klucz ExtraSpeech pomijany takze z ampersandem w nazwie",
                (bool) miPomijany.Invoke(null, new object[] { "E&xtraSpeech" }), "nie pomijany");
            Sprawdz("ciaglosc pracy ma swoje okno, wiec tu jej nie ma",
                (bool) miPomijany.Invoke(null, new object[] { "RestoreSession" })
                && (bool) miPomijany.Invoke(null, new object[] { "AutoSaveSeconds" }), "nie pomijane");
            Sprawdz("czcionka ma swoje okno, wiec tu jej nie ma",
                (bool) miPomijany.Invoke(null, new object[] { "FontDefault" }), "nie pomijana");
            Sprawdz("zwykly klucz NIE jest pomijany",
                !(bool) miPomijany.Invoke(null, new object[] { "WordWrap" }), "WordWrap pomijany");
        }

        Console.WriteLine();
        Console.WriteLine("=== 6. Kazde ustawienie ma opis czytany przy wejsciu w pole");
        int iBezOpisu = 0; string sBezOpisu = "";
        foreach (object o in lOpcje)
        {
            string sP = Pole(o, "Podpowiedz");
            if (sP == null || sP.Trim().Length < 20) { iBezOpisu++; sBezOpisu += Pole(o, "Klucz") + " "; }
        }
        Sprawdz("kazde ustawienie ma zdanie wyjasniajace, co robi", iBezOpisu == 0, sBezOpisu);

        int iBezLitery = 0; string sBezLitery = "";
        foreach (object o in lOpcje)
        {
            string sE = Pole(o, "Etykieta") ?? "";
            if (sE.IndexOf('&') < 0) { iBezLitery++; sBezLitery += Pole(o, "Klucz") + " "; }
        }
        // Klawisz dostepu nie jest wymagany wszedzie (litery by sie skonczyly),
        // ale wiekszosc pol powinna go miec, zeby dalo sie skakac Alt+litera.
        Sprawdz("wiekszosc pol ma klawisz dostepu Alt plus litera", iBezLitery <= lOpcje.Count / 2,
            iBezLitery + " z " + lOpcje.Count + " bez litery: " + sBezLitery);

        Console.WriteLine();
        Console.WriteLine("WYNIK: " + iOk + " OK, " + iBlad + " BLAD");
        return iBlad == 0 ? 0 : 1;
    }
}
