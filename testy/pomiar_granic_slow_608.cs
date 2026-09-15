// pomiar_granic_slow_608.cs -- CZY SLOWO OD POLSKIEJ LITERY JEST PRZYKLEJANE.
//
// ZGLOSZENIE MICHALA 15.09.2026: "wrocil stary problem, ze gdy jakies slowo
// obok zaczyna sie od polskiej litery, to podczas nawigacji po slowach jest
// ono przyklejane do poprzedniego slowa.  Michala Sledzinskiego, Wszystkich
// swietych itp.  Najgorsze, ze teraz jest dobrze, ale w preview faktycznie byl
// taki problem."
//
// TO OSTATNIE ZDANIE ROZSTRZYGA, CO MIERZYC: ta sama tresc w DWOCH kontrolkach.
// Kontrolka edycyjna (HomerRichTextBox) tworzy okno klasy RICHEDIT50W i tam
// jest dobrze.  Kontrolka PODGLADU (MarkdownReviewTextBox) do 5.0.107 zostawala
// na starej klasie RICHEDIT20W, ktora ma inna tablice podzialu na slowa i
// polskiej litery na poczatku slowa nie uznaje za granice.  Pomiar musi wiec
// przejsc slowa W PODGLADZIE, nie tylko w edytorze.
//
// KONTROLA POZYTYWNA: w tekscie probnym jest 5 slow zaczynajacych sie polska
// litera.  Jesli sonda zobaczy tyle samo przystankow co slow, to znaczy, ze
// podzial dziala; jesli MNIEJ, to slowa sa zlepiane.  Dodatkowo mierzymy ten
// sam tekst w edytorze - kontrola ROZNICUJACA: jesli edytor tez zlepia, to
// przyczyna nie jest w klasie okna podgladu.
//
// URUCHOMIENIE: testy/uruchamiacze/pomiar_granice_slow.cmd

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Automation;
// TextPatternRange lezy w OSOBNEJ przestrzeni nazw System.Windows.Automation.Text,
// nie w System.Windows.Automation.  Bez tego CS0246.  Zmierzone 16.09.2026.
using System.Windows.Automation.Text;

class PomiarGranicSlow
{
    // Kazde z tych slow zaczyna sie POLSKA litera z ogonkiem - wlasnie na nich
    // Michal widzial zlepianie ("Michala Sledzinskiego", "Wszystkich swietych").
    // MUSZA tu byc prawdziwe polskie znaki, bo caly blad polega na tym, ze
    // stara tablica podzialu na slowa nie uznaje ich za poczatek slowa.
    static readonly string[] SLOWA_POLSKIE = { "Śledzińskiego", "świętych", "żółw", "ćma", "źdźbło" };

    static string TekstProbny()
    {
        return
            "# Proba granic slow\r\n" +
            "\r\n" +
            "Ksiazka Michala " + SLOWA_POLSKIE[0] + " lezy obok.\r\n" +
            "Parafia Wszystkich " + SLOWA_POLSKIE[1] + " oraz zarowka.\r\n" +
            "Maly " + SLOWA_POLSKIE[2] + " i nocna " + SLOWA_POLSKIE[3] + " oraz " + SLOWA_POLSKIE[4] + " trawy.\r\n";
    }

    static string Dane()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EdSharp");
    }

    // Nazwa klasy okna, ktore ma teraz fokus.  To jest sedno pomiaru: RICHEDIT50W
    // dzieli slowa poprawnie, RICHEDIT20W nie.
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern IntPtr GetFocus();
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder sb, int iMax);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool bAttach);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    static string KlasaPodKursorem()
    {
        IntPtr hOkno = GetForegroundWindow();
        uint pid;
        uint idWatku = GetWindowThreadProcessId(hOkno, out pid);
        AttachThreadInput(GetCurrentThreadId(), idWatku, true);
        try
        {
            IntPtr h = GetFocus();
            if (h == IntPtr.Zero) return "(brak fokusu)";
            StringBuilder sb = new StringBuilder(256);
            GetClassName(h, sb, sb.Capacity);
            return sb.ToString();
        }
        finally { AttachThreadInput(GetCurrentThreadId(), idWatku, false); }
    }

    // POZYCJA KARETKI I TEKST WPROST Z OKNA KONTROLKI.
    //
    // DWIE POPRZEDNIE WERSJE SONDY BYLY GLUCHE - to jest zmierzone, nie
    // przypuszczenie:
    //  1. Control+Shift+Prawo z czytaniem zaznaczenia: w podgladzie
    //     zaznaczenie sie KUMULUJE, jeden przystanek zwrocil poltora wiersza.
    //  2. UIA TextUnit.Word: na STAREJ wersji 5.0.107, gdzie podglad stal na
    //     klasie RichEdit20W, sonda dala PASS 5/5 - czyli NIE ROZNICUJE.
    //     UIA liczy slowa wlasnym podzialem, a nie tym, ktorego uzywa
    //     Control+Prawo w kontrolce.
    //
    // Czytnik ekranu jedzie karetka, wiec mierzymy KARETKE: EM_EXGETSEL po
    // kazdym Control+Prawo, na oknie, ktore ma fokus.  To dziala tak samo w
    // RichEdit20W i RICHEDIT50W, wiec porownanie wersji jest uczciwe.

    const int WM_GETTEXT = 0x000D;
    const int WM_GETTEXTLENGTH = 0x000E;
    const int EM_EXGETSEL = 0x0400 + 52;
    // EM_FINDWORDBREAK z WB_MOVEWORDRIGHT pyta kontrolke o DOKLADNIE ten podzial
    // na slowa, ktorego uzywa Control+Prawo.  Uzywamy go, bo przejazd klawiszami
    // okazal sie niestabilny (karetka dawala tylko 2 przystanki, zaleznie od
    // fokusu i czasu reakcji), a EM_FINDWORDBREAK odpowiada zawsze i tak samo.
    const int EM_FINDWORDBREAK = 0x0400 + 76;
    const int WB_MOVEWORDRIGHT = 5;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct CHARRANGE { public int cpMin; public int cpMax; }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, StringBuilder lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref CHARRANGE cr);

    static IntPtr OknoZFokusem(out string sKlasa)
    {
        IntPtr hOkno = GetForegroundWindow();
        uint pid;
        uint idWatku = GetWindowThreadProcessId(hOkno, out pid);
        AttachThreadInput(GetCurrentThreadId(), idWatku, true);
        try
        {
            IntPtr h = GetFocus();
            StringBuilder sb = new StringBuilder(256);
            if (h != IntPtr.Zero) GetClassName(h, sb, sb.Capacity);
            sKlasa = sb.ToString();
            return h;
        }
        finally { AttachThreadInput(GetCurrentThreadId(), idWatku, false); }
    }

    static string TekstKontrolki(IntPtr h)
    {
        int iLen = (int)SendMessage(h, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
        StringBuilder sb = new StringBuilder(iLen + 2);
        SendMessage(h, WM_GETTEXT, (IntPtr)(iLen + 1), sb);
        // WM_GETTEXT oddaje konce wierszy jako "\r\n", ale POZYCJE, ktorymi liczy
        // EM_FINDWORDBREAK, sa w buforze wewnetrznym, gdzie koniec wiersza to
        // JEDEN znak "\r".  Bez tej zamiany offsety rozjezdzaja sie o jeden znak
        // na kazdy wiersz i sonda pokazuje bzdurny podzial ("a Michal",
        // "a Śledzińskieg") - co wyglada jak zlepianie slow, a jest bledem sondy.
        // Zmierzone 16.09.2026.
        return sb.ToString().Replace("\r\n", "\r");
    }

    static int Karetka(IntPtr h)
    {
        CHARRANGE cr = new CHARRANGE();
        SendMessage(h, EM_EXGETSEL, IntPtr.Zero, ref cr);
        return cr.cpMin;
    }

    // Przejazd po granicach slow PYTANIEM KONTROLKI, nie klawiszami.
    // Zwraca liste offsetow, na ktorych Control+Prawo by stanal.
    static List<int> PrzystankiKaretki(IntPtr h, int iKroki)
    {
        List<int> l = new List<int>();
        int iPoz = 0;
        l.Add(0);
        for (int i = 0; i < iKroki; i++)
        {
            int iNast = (int)SendMessage(h, EM_FINDWORDBREAK, (IntPtr)WB_MOVEWORDRIGHT, (IntPtr)iPoz);
            if (iNast <= iPoz) break;    // koniec tekstu
            l.Add(iNast);
            iPoz = iNast;
        }
        return l;
    }

    // CZY KARETKA STANELA NA POCZATKU KAZDEGO SLOWA OD POLSKIEJ LITERY.
    // Zlepianie objawia sie tym, ze Control+Prawo PRZESKAKUJE poczatek takiego
    // slowa - czytnik czyta je wtedy razem z poprzednim wyrazem.
    // Wypisuje, co kontrolka uwaza za kolejne slowa.  Bez tego widoku nie da sie
    // odroznic zlepiania slow od bledu w samej sondzie.
    static void WypiszPodzial(string sTekst, List<int> lPoz)
    {
        Console.WriteLine("  podzial wedlug kontrolki:");
        for (int i = 0; i < lPoz.Count; i++)
        {
            int iOd = lPoz[i];
            int iDo = (i + 1 < lPoz.Count) ? lPoz[i + 1] : sTekst.Length;
            if (iOd >= sTekst.Length) break;
            if (iDo > sTekst.Length) iDo = sTekst.Length;
            string s = sTekst.Substring(iOd, iDo - iOd).Replace("\r", "\\r").Replace("\n", "\\n");
            Console.WriteLine("    " + iOd + ": [" + s + "]");
        }
    }

    static int IleTrafionych(string sTekst, List<int> lPozycje, out List<string> lNietrafione)
    {
        lNietrafione = new List<string>();
        int iOk = 0;
        foreach (string p in SLOWA_POLSKIE)
        {
            int iGdzie = sTekst.IndexOf(p);
            if (iGdzie < 0) { lNietrafione.Add(p + " (brak w tekscie kontrolki)"); continue; }
            if (lPozycje.Contains(iGdzie)) iOk++;
            else lNietrafione.Add(p + " (poczatek na " + iGdzie + ", karetka tam nie stanela)");
        }
        return iOk;
    }

    static void Main(string[] args)
    {
        string sProba = Path.Combine(Path.GetTempPath(), "proba_granice_slow.md");
        File.WriteAllText(sProba, TekstProbny(), new UTF8Encoding(false));
        Console.WriteLine("Plik probny: " + sProba);
        Console.WriteLine("Slow zaczynajacych sie polska litera: " + SLOWA_POLSKIE.Length);

        string sExe = args.Length > 0 ? args[0] : @"C:\EdSharpBuild\EdSharpNG.exe";
        Process p = Process.Start(sExe, "\"" + sProba + "\"");
        Thread.Sleep(9000);

        int iKod = 0;
        try
        {
            AutomationElement okno = null;
            for (int i = 0; i < 20 && okno == null; i++)
            {
                okno = AutomationElement.RootElement.FindFirst(TreeScope.Children,
                    new PropertyCondition(AutomationElement.ProcessIdProperty, p.Id));
                if (okno == null) Thread.Sleep(600);
            }
            if (okno == null) { Console.WriteLine("BLAD: nie widze okna - pomiar niewazny."); return; }
            okno.SetFocus();
            Thread.Sleep(1500);

            Console.WriteLine();
            Console.WriteLine("=== 1. EDYTOR (kontrola roznicujaca) ===");
            string sKlasaEd;
            IntPtr hEd = OknoZFokusem(out sKlasaEd);
            Console.WriteLine("Klasa okna z fokusem: " + sKlasaEd);
            string sTekstEd = TekstKontrolki(hEd);
            List<int> lPozEd = PrzystankiKaretki(hEd, 40);
            List<string> lZleEd;
            int iOkEdytor = IleTrafionych(sTekstEd, lPozEd, out lZleEd);
            Console.WriteLine("Przystankow karetki: " + lPozEd.Count);
            Console.WriteLine("Slow trafionych: " + iOkEdytor + " z " + SLOWA_POLSKIE.Length);
            foreach (string s in lZleEd) Console.WriteLine("  PRZESKOCZONE: " + s);
            WypiszPodzial(sTekstEd, lPozEd);

            Console.WriteLine();
            Console.WriteLine("=== 2. PODGLAD (to zglaszal Michal) ===");
            SendKeys("{ESC}");          // Escape na pliku .md otwiera podglad
            Thread.Sleep(3000);
            string sKlasa;
            IntPtr hPod = OknoZFokusem(out sKlasa);
            Console.WriteLine("Klasa okna z fokusem: " + sKlasa);
            if (sKlasa.Contains("RICHEDIT50W")) Console.WriteLine("  (nowa klasa - ta dzieli slowa poprawnie)");
            else if (sKlasa.Contains("RichEdit20"))
                Console.WriteLine("  UWAGA: STARA klasa okna - to jest przyczyna zlepiania.");
            string sTekstPod = TekstKontrolki(hPod);
            List<int> lPozPod = PrzystankiKaretki(hPod, 40);
            List<string> lZlePod;
            int iOkPodglad = IleTrafionych(sTekstPod, lPozPod, out lZlePod);
            Console.WriteLine("Przystankow karetki: " + lPozPod.Count);
            Console.WriteLine("Slow trafionych: " + iOkPodglad + " z " + SLOWA_POLSKIE.Length);
            foreach (string s in lZlePod) Console.WriteLine("  PRZESKOCZONE: " + s);

            Console.WriteLine();
            Console.WriteLine("=== WYNIK ===");
            if (lPozEd.Count <= 1 || lPozPod.Count <= 1 || sTekstEd.Length == 0 || sTekstPod.Length == 0)
            {
                Console.WriteLine("SONDA GLUCHA: karetka nie rusza albo tekst pusty - wynik nic nie dowodzi.");
                iKod = 2;
            }
            else if (iOkEdytor == SLOWA_POLSKIE.Length && iOkPodglad == SLOWA_POLSKIE.Length)
                Console.WriteLine("PASS: karetka staje na poczatku kazdego slowa od polskiej litery,"
                    + " w edytorze i w podgladzie.");
            else
            {
                Console.WriteLine("FAIL: edytor " + iOkEdytor + "/" + SLOWA_POLSKIE.Length
                    + ", podglad " + iOkPodglad + "/" + SLOWA_POLSKIE.Length
                    + " - slowa od polskiej litery sa przeskakiwane (zlepiane w mowie).");
                iKod = 1;
            }
        }
        finally
        {
            try { if (!p.HasExited) p.Kill(); } catch { }
        }
        Environment.Exit(iKod);
    }

    static void SendKeys(string s)
    {
        System.Windows.Forms.SendKeys.SendWait(s);
    }
}
