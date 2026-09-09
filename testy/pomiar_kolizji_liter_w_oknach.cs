// Kolizje liter dostepu w REALNYCH oknach programu, przez refleksje
// na zbudowanej binarce. Odpowiada na pytanie: czy zmiana cokolwiek
// naprawia w oknach, ktore user faktycznie otwiera - a nie tylko w
// przykladach wymyslonych do testu.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

public class H
{
    static int iPass = 0, iFail = 0;
    static Assembly asm;

    static void Check(string sName, bool bOk, string sDetail)
    {
        if (bOk) { iPass++; Console.WriteLine("PASS " + sName + " :: " + sDetail); }
        else { iFail++; Console.WriteLine("FAIL " + sName + " :: " + sDetail); }
    }

    static object NewDialog(string sTitle)
    {
        Type t = asm.GetType("Homer.LbcDialog");
        return Activator.CreateInstance(t, new object[] { sTitle, null });
    }

    static object Call(object o, string sMethod, params object[] aArgs)
    {
        foreach (MethodInfo m in o.GetType().GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            if (m.Name == sMethod && m.GetParameters().Length == aArgs.Length)
                return m.Invoke(o, aArgs);
        throw new Exception("BRAK metody " + sMethod + "/" + aArgs.Length);
    }

    static object Field(object o, string sName)
    {
        return o.GetType().GetField(sName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
    }

    static void Walk(Control ctl, List<Control> ls)
    {
        if (ctl == null) return;
        ls.Add(ctl);
        foreach (Control c in ctl.Controls) Walk(c, ls);
    }

    // Litery, ktore okno realnie oferuje: z etykiet, pol wyboru i
    // przyciskow. Kazda para identycznych liter to jedno pole albo
    // przycisk, do ktorego user nie dojdzie klawiszem.
    static string LettersOf(object dlg, string[] aButtons)
    {
        foreach (string s in aButtons)
        {
            string sPlain = s.Replace("&", "");
            bool bNo = string.Equals(sPlain, "OK", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(sPlain, "Cancel", StringComparison.OrdinalIgnoreCase);
            // Odtworzenie tego, co robi runWithButtons w NOWYM kodzie.
            if (!bNo) Call(dlg, "claimAccessKey", s);
        }
        Call(dlg, "assignQueuedAccessKeys");
        Form frm = (Form) Field(dlg, "frm");
        List<Control> ls = new List<Control>();
        Walk(frm, ls);
        string sLet = "";
        foreach (Control c in ls)
        {
            if (c is Form) continue;
            string sT = c.Text ?? "";
            int i = sT.IndexOf('&');
            if (i >= 0 && i + 1 < sT.Length) sLet += Char.ToUpper(sT[i + 1]);
        }
        // Przyciski dokladamy osobno: w sondzie nie budujemy rzedu
        // przyciskow, wiec ich litery trzeba doliczyc z tego, co
        // claimAccessKey im przydzielil.
        return sLet;
    }

    // Litery STAREGO sposobu: kazdy przycisk dostawal "&" + nazwa,
    // wiec OK zajmowal O, a Cancel zajmowal C - niezaleznie od pol.
    static string OldLetters(string[] aFieldLabels, string[] aButtons)
    {
        string sLet = "";
        foreach (string s in aFieldLabels)
        {
            int i = s.IndexOf('&');
            if (i >= 0 && i + 1 < s.Length) sLet += Char.ToUpper(s[i + 1]);
        }
        foreach (string s in aButtons)
        {
            string sPlain = s.Replace("&", "");
            if (sPlain.Length > 0) sLet += Char.ToUpper(sPlain[0]);
        }
        return sLet;
    }

    static List<char> Dups(string s)
    {
        List<char> ls = new List<char>();
        for (int i = 0; i < s.Length; i++)
            for (int j = i + 1; j < s.Length; j++)
                if (s[i] == s[j] && !ls.Contains(s[i])) ls.Add(s[i]);
        return ls;
    }

    static string Show(List<char> ls)
    {
        if (ls.Count == 0) return "brak";
        string s = "";
        foreach (char c in ls) s += c + " ";
        return s.Trim();
    }

    [STAThread]
    static void Main()
    {
        asm = Assembly.LoadFrom("EdSharpNG.exe");

        // Etykiety wziete DOSLOWNIE z EdSharp.cs - to sa okna, ktore
        // Kasperczak realnie otwiera.
        string[] aOkCancel = new string[] { "OK", "Cancel" };

        // --- Okno ustawien kompilatora (Create Compiler setting, l.5230).
        // Osiem pol, wszystkie z jawna litera od autora.
        string[] aCompiler = new string[] { "&Name", "&CompileCommand", "&JumpPosition",
            "&AbbreviateOutput", "&NavigatePart", "&QuotePrefix", "&ExtensionDefault",
            "&GoToEnvironment" };
        {
            string sOld = OldLetters(aCompiler, aOkCancel);
            List<char> lsOld = Dups(sOld);
            // KONTROLA, ze objaw byl realny: stary sposob MUSI miec
            // kolizje, inaczej cala zmiana nie miala by po co powstac.
            Check("stare-okno-kompilatora-mialo-kolizje", lsOld.Count > 0,
                  "stare litery: " + sOld + ", kolizje: " + Show(lsOld));

            object dlg = NewDialog("Create Compiler setting");
            foreach (string s in aCompiler) Call(dlg, "addInputBox", s, "");
            string sNew = LettersOf(dlg, aOkCancel);
            Check("nowe-okno-kompilatora-bez-kolizji", Dups(sNew).Count == 0,
                  "nowe litery pol: " + sNew);
        }

        // --- Okno zamiany z wyrazeniem (l.3450): Match / Replace.
        {
            string[] aFields = new string[] { "&Match", "&Replace" };
            object dlg = NewDialog("Replace with Regular Expression");
            foreach (string s in aFields) Call(dlg, "addInputBox", s, "");
            string sNew = LettersOf(dlg, aOkCancel);
            Check("okno-zamiany-bez-kolizji", Dups(sNew).Count == 0, "litery: " + sNew);
        }

        // --- Okno obliczania daty (l.6586): Year / Month / Week / Day.
        {
            string[] aFields = new string[] { "&Year", "&Month", "&Week", "&Day" };
            object dlg = NewDialog("Calculate Date");
            foreach (string s in aFields) Call(dlg, "addInputBox", s, "");
            string sNew = LettersOf(dlg, aOkCancel);
            Check("okno-daty-bez-kolizji", Dups(sNew).Count == 0, "litery: " + sNew);
        }

        // --- Okno przypisu (5.0.41): tresc + miejsce, litery jawne.
        {
            object dlg = NewDialog("Insert Footnote");
            Call(dlg, "addInputBox", "Footnote &text", "");
            List<string> lsPlaces = new List<string>();
            lsPlaces.Add("End of document");
            Call(dlg, "addComboPickBox", "Footnote text &goes to", lsPlaces, "End of document", "");
            string sNew = LettersOf(dlg, aOkCancel);
            Check("okno-przypisu-bez-kolizji", Dups(sNew).Count == 0, "litery: " + sNew);
        }

        // --- Okno szukania plikow (l.7574, File Find): pola budowane
        // w biegu z nazw kryteriow, wiec kolizja jest tu najbardziej
        // prawdopodobna - i wlasnie dlatego mechanizm musi byc
        // automatyczny, a nie recznie wpisane ampersandy.
        {
            string[] aFields = new string[] { "Folder", "File pattern", "Find text",
                "File extension", "Follow subfolders", "Filter" };
            object dlg = NewDialog("File Find");
            foreach (string s in aFields) Call(dlg, "addInputBox", s, "");
            string sNew = LettersOf(dlg, aOkCancel);
            Check("okno-szukania-bez-kolizji", Dups(sNew).Count == 0, "litery: " + sNew);
            // KONTROLA ROZNICUJACA: piec z szesciu pol zaczyna sie na
            // F, wiec bez mechanizmu doboru litery mialyby ta sama.
            string sOld = OldLetters(new string[] { "&Folder", "&File pattern", "&Find text",
                "&File extension", "&Follow subfolders", "&Filter" }, aOkCancel);
            Check("stare-okno-szukania-mialo-kolizje", Dups(sOld).Count > 0,
                  "stare litery: " + sOld + ", kolizje: " + Show(Dups(sOld)));
        }

        // --- Okno z przyciskami akcji, gdzie przycisk i pole chca tej
        // samej litery. Pomiar POKAZUJE SKUTEK, nie tylko brak kolizji:
        // pole zostaje bez litery, bo przycisk mial pierwszenstwo.
        {
            object dlg = NewDialog("Reset Configuration");
            Call(dlg, "addInputBox", "Main setting", "");
            string sNew = LettersOf(dlg, new string[] { "&Main", "&Both", "&New", "Cancel" });
            Check("przycisk-wygrywa-pole-bez-litery", Dups(sNew).Count == 0,
                  "litery pol: " + (sNew.Length == 0 ? "(zadnej - M wzial przycisk)" : sNew));
        }

        Console.WriteLine("WYNIK " + iPass + "/" + (iPass + iFail));
    }
}
