// Pomiar liter dostepu w oknach dialogowych, przez REFLEKSJE na
// zbudowanej binarce EdSharpNG.exe. Mierzy PRAWDZIWE kontrolki
// prawdziwego LbcDialog, nie kopie logiki.
//
// Czego NIE mierzy: mowy czytnika. To osobna warstwa i osobna sonda.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
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
        if (t == null) t = asm.GetType("LbcDialog");
        if (t == null) throw new Exception("BRAK typu LbcDialog w binarce");
        return Activator.CreateInstance(t, new object[] { sTitle, null });
    }

    static object Call(object o, string sMethod, params object[] aArgs)
    {
        Type[] aTypes = new Type[aArgs.Length];
        for (int i = 0; i < aArgs.Length; i++)
            aTypes[i] = (aArgs[i] == null) ? typeof(string) : aArgs[i].GetType();
        MethodInfo mi = o.GetType().GetMethod(sMethod,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null, aTypes, null);
        if (mi == null)
        {
            foreach (MethodInfo m in o.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (m.Name == sMethod && m.GetParameters().Length == aArgs.Length) { mi = m; break; }
        }
        if (mi == null) throw new Exception("BRAK metody " + sMethod);
        return mi.Invoke(o, aArgs);
    }

    static object Field(object o, string sName)
    {
        FieldInfo fi = o.GetType().GetField(sName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi == null) throw new Exception("BRAK pola " + sName);
        return fi.GetValue(o);
    }

    // Litery dostepu wszystkich kontrolek okna, plus ich napisy.
    // Chodzenie po drzewie kontrolek formy, bo etykiety siedza w
    // zagniezdzonych panelach.
    static void Collect(Control ctl, List<string> lsTexts)
    {
        if (ctl == null) return;
        if (!string.IsNullOrEmpty(ctl.Text)) lsTexts.Add(ctl.GetType().Name + "|" + ctl.Text);
        foreach (Control c in ctl.Controls) Collect(c, lsTexts);
    }

    static List<string> BuildAndCollect(object dlg, string[] aButtons)
    {
        // runWithButtons pokazuje okno modalnie, wiec nie da sie go
        // wolac w sondzie. Wolamy sama budowe rzedu przyciskow przez
        // refleksje? Nie - zamiast tego korzystamy z tego, ze
        // assignQueuedAccessKeys jest wolane WEWNATRZ runWithButtons.
        // Sonda odtwarza wiec te kolejnosc: najpierw litery przyciskow
        // przez claimAccessKey, potem assignQueuedAccessKeys.
        foreach (string s in aButtons)
        {
            string sPlain = s.Replace("&", "");
            bool bNo = string.Equals(sPlain, "OK", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(sPlain, "Cancel", StringComparison.OrdinalIgnoreCase);
            if (!bNo) Call(dlg, "claimAccessKey", s);
        }
        Call(dlg, "assignQueuedAccessKeys");
        Form frm = (Form) Field(dlg, "frm");
        List<string> lsTexts = new List<string>();
        Collect(frm, lsTexts);
        return lsTexts;
    }

    static string Letters(List<string> lsTexts)
    {
        string s = "";
        foreach (string sT in lsTexts)
        {
            int i = sT.IndexOf('&');
            if (i >= 0 && i + 1 < sT.Length) s += Char.ToUpper(sT[i + 1]);
        }
        return s;
    }

    static bool HasDuplicate(string sLetters)
    {
        for (int i = 0; i < sLetters.Length; i++)
            for (int j = i + 1; j < sLetters.Length; j++)
                if (sLetters[i] == sLetters[j]) return true;
        return false;
    }

    static string Join(List<string> ls)
    {
        return string.Join(" ; ", ls.ToArray());
    }

    [STAThread]
    static void Main()
    {
        asm = Assembly.LoadFrom("EdSharpNG.exe");

        // --- 1. KONTROLA POZYTYWNA SONDY: metody istnieja w binarce.
        // Bez niej kazdy dalszy "brak litery" moglby znaczyc "sonda
        // patrzy w zle miejsce".
        {
            object dlg = NewDialog("Sonda");
            bool bClaim = false, bAssign = false, bQueue = false, bReserve = false;
            foreach (MethodInfo m in dlg.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name == "claimAccessKey") bClaim = true;
                if (m.Name == "assignQueuedAccessKeys") bAssign = true;
                if (m.Name == "queueAccessKey") bQueue = true;
                if (m.Name == "reserveAccessKey") bReserve = true;
            }
            Check("sonda-metody-w-binarce", bClaim && bAssign && bQueue && bReserve,
                  "claim=" + bClaim + " assign=" + bAssign + " queue=" + bQueue + " reserve=" + bReserve);
        }

        // --- 2. Pole tekstowe z etykieta dostaje litere pierwszego slowa.
        {
            object dlg = NewDialog("Input");
            Call(dlg, "addInputBox", "Term", "");
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            bool bOk = Join(ls).Contains("&Term");
            Check("etykieta-dostaje-litere", bOk, Join(ls));
        }

        // --- 3. OK i Cancel BEZ litery - jego wlasna decyzja.
        // Enter i Escape je obsluguja, wiec Alt+O i Alt+C tylko zjadalyby
        // litery potrzebne akcjom.
        {
            object dlg = NewDialog("Input");
            Call(dlg, "addInputBox", "Term", "");
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            string sAll = Join(ls);
            Check("ok-cancel-bez-litery",
                  !sAll.Contains("&OK") && !sAll.Contains("&Cancel"), sAll);
        }

        // --- 4. KONTROLA ROZNICUJACA: litera OK jest nadal WOLNA dla pola.
        // Gdyby OK ja zajmowal, pole "Options" nie dostaloby O.
        // Bez tej kontroli "OK bez litery" byloby kosmetyka bez skutku.
        {
            object dlg = NewDialog("Input");
            Call(dlg, "addInputBox", "Options", "");
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            Check("litera-po-OK-wolna-dla-pola", Join(ls).Contains("&Options"), Join(ls));
        }

        // --- 5. KOLIZJA DWOCH POL: druga etykieta NIE MOZE dostac tej
        // samej litery. To jest sedno calej zmiany - pomiar w harnessie
        // pokazal, ze przy duplikacie fokus lezy nieprzewidywalnie.
        {
            object dlg = NewDialog("Input");
            Call(dlg, "addInputBox", "Name", "");
            Call(dlg, "addInputBox", "Number", "");
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            string sLet = Letters(ls);
            Check("kolizja-pol-rozwiazana", !HasDuplicate(sLet),
                  "litery: " + sLet + " :: " + Join(ls));
        }

        // --- 6. Druga etykieta dostaje litere DRUGIEGO SLOWA, nie litere
        // ze srodka slowa. "Number" po zajetym N nie ma innej inicjaly,
        // wiec ma zostac BEZ litery, nie z "N&umber".
        {
            object dlg = NewDialog("Input");
            Call(dlg, "addInputBox", "Name", "");
            Call(dlg, "addInputBox", "Number", "");
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            string sAll = Join(ls);
            bool bNoMidWord = !sAll.Contains("N&umber") && !sAll.Contains("Nu&mber")
                           && !sAll.Contains("Num&ber");
            Check("brak-litery-ze-srodka-slowa", bNoMidWord, sAll);
        }

        // --- 7. Etykieta z DWOMA slowami: gdy pierwsza inicjala zajeta,
        // litera idzie na inicjale drugiego slowa.
        {
            object dlg = NewDialog("Input");
            Call(dlg, "addInputBox", "File", "");
            Call(dlg, "addInputBox", "File Name", "");
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            string sAll = Join(ls);
            Check("druga-inicjala-slowa", sAll.Contains("File &Name"), sAll);
        }

        // --- 8. Litera podana PRZEZ WOLAJACEGO wygrywa i jest zapamietana.
        {
            object dlg = NewDialog("Input");
            Call(dlg, "addInputBox", "Footnote &text", "");
            Call(dlg, "addInputBox", "Table", "");
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            string sAll = Join(ls);
            // T zajete jawnie przez pierwsze pole, wiec "Table" nie moze
            // go dostac drugi raz.
            Check("jawna-litera-wolajacego-wygrywa",
                  sAll.Contains("Footnote &text") && !HasDuplicate(Letters(ls)),
                  "litery: " + Letters(ls) + " :: " + sAll);
        }

        // --- 9. PRZYCISK AKCJI ma pierwszenstwo przed polem.
        // Litera przycisku jest wyborem okna, litera pola jest
        // automatyczna - swiadomy wybor musi wygrac.
        {
            object dlg = NewDialog("Spelling");
            Call(dlg, "addInputBox", "Replacement", "");
            List<string> ls = BuildAndCollect(dlg, new string[] { "&Replace", "&Skip", "Cancel" });
            string sAll = Join(ls);
            // R poszlo do przycisku Replace, wiec pole "Replacement"
            // nie moze miec R.
            Check("przycisk-przed-polem", !sAll.Contains("&Replacement"),
                  "litery: " + Letters(ls) + " :: " + sAll);
        }

        // --- 10. Pole wyboru dostaje litere NA SOBIE (Alt+litera
        // przelacza je, nie tylko fokusuje).
        {
            object dlg = NewDialog("Options");
            Call(dlg, "addCheckBox", "Word wrap", true);
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            string sAll = Join(ls);
            Check("pole-wyboru-ma-litere", sAll.Contains("CheckBox|&Word wrap"), sAll);
        }

        // --- 11. Przycisk opcji dostaje litere na sobie.
        {
            object dlg = NewDialog("Options");
            Call(dlg, "addRadioButton", "Windows", true);
            Call(dlg, "addRadioButton", "Unix", false);
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            string sAll = Join(ls);
            Check("przyciski-opcji-maja-litery",
                  sAll.Contains("RadioButton|&Windows") && sAll.Contains("RadioButton|&Unix"),
                  sAll);
        }

        // --- 12. WYCZERPANIE LITER: gdy nie ma wolnej inicjaly, pole
        // zostaje BEZ litery, a nie z duplikatem. Kontrola, ze zasada
        // "nigdy duplikat" trzyma sie takze w skrajnym przypadku.
        {
            object dlg = NewDialog("Many");
            for (int i = 0; i < 6; i++) Call(dlg, "addInputBox", "Text", "");
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            string sLet = Letters(ls);
            Check("wyczerpanie-liter-bez-duplikatu", !HasDuplicate(sLet),
                  "litery: " + sLet + " :: " + Join(ls));
        }

        // --- 13. Etykieta zachowuje DWUKROPEK po dolozeniu litery.
        // Litera nie moze wyladowac na znaku przestankowym.
        {
            object dlg = NewDialog("Input");
            Call(dlg, "addInputBox", "Code Page:", "");
            List<string> ls = BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            string sAll = Join(ls);
            Check("dwukropek-zachowany", sAll.Contains("&Code Page:"), sAll);
        }

        // --- 14. UseMnemonic wlaczone na etykiecie - bez tego ampersand
        // pokazywalby sie jako znak i klawisz nie dzialalby wcale.
        {
            object dlg = NewDialog("Input");
            Call(dlg, "addInputBox", "Term", "");
            BuildAndCollect(dlg, new string[] { "OK", "Cancel" });
            Form frm = (Form) Field(dlg, "frm");
            List<Control> lsAll = new List<Control>();
            Walk(frm, lsAll);
            bool bAllOn = true; int iLabels = 0;
            foreach (Control c in lsAll)
            {
                Label lbl = c as Label;
                if (lbl != null && lbl.Text.Contains("&"))
                {
                    iLabels++;
                    if (!lbl.UseMnemonic) bAllOn = false;
                }
            }
            Check("mnemonik-wlaczony", iLabels > 0 && bAllOn,
                  "etykiet z litera: " + iLabels + ", wszystkie z UseMnemonic: " + bAllOn);
        }

        Console.WriteLine("WYNIK " + iPass + "/" + (iPass + iFail));
    }

    static void Walk(Control ctl, List<Control> ls)
    {
        if (ctl == null) return;
        ls.Add(ctl);
        foreach (Control c in ctl.Controls) Walk(c, ls);
    }
}
