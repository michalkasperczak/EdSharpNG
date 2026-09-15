// pomiar_stabilnosci_kursora_608.cs -- CZY KURSOR WRACA NA POCZATEK PLIKU.
//
// ZGLOSZENIE MICHALA 15.09.2026: "kursor mniej stabilny, preview podglad tez
// czesto nie podaza, wraca do poczatku pliku".
//
// CO MIERZYMY: stawiamy kursor w srodku dlugiego pliku, wpisujemy znak (co
// wymusza przerysowanie podgladu) i sprawdzamy, GDZIE kursor jest po chwili.
// Jesli wrocil na 0 albo blisko zera, blad wystepuje.
//
// KONTROLA ROZNICUJACA: ten sam pomiar puszczamy na starej wersji, gdzie blad
// byl.  Bez tego zielony wynik na nowej wersji nic nie dowodzi.
//
// URUCHOMIENIE: testy/uruchamiacze/pomiar_stabilnosc_kursora.cmd

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Automation;

class PomiarStabilnosciKursora
{
    const int WM_GETTEXTLENGTH = 0x000E;
    const int EM_EXGETSEL = 0x0400 + 52;
    const int EM_EXSETSEL = 0x0400 + 55;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct CHARRANGE { public int cpMin; public int cpMax; }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref CHARRANGE cr);
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

    // POZYCJA KURSORA CZYTANA PRZEZ UIA, NIE KOMUNIKATEM.
    // EM_EXGETSEL przekazuje WSKAZNIK na strukture, a Windows nie tlumaczy
    // wskaznikow przy komunikatach miedzy procesami - sonda dostawala wiec
    // zawsze 0 i wygladalo to na kursor stojacy na poczatku pliku.  Zmierzone
    // 16.09.2026.  (EM_FINDWORDBREAK w drugiej sondzie dziala, bo przekazuje
    // same liczby.)  UIA czyta zaznaczenie poprawnie przez granice procesu.
    static AutomationElement s_pole = null;

    static int Karetka(AutomationElement okno)
    {
        try
        {
            if (s_pole == null)
                s_pole = okno.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.IsTextPatternAvailableProperty, true));
            if (s_pole == null) return -1;
            TextPattern tp = s_pole.GetCurrentPattern(TextPattern.Pattern) as TextPattern;
            if (tp == null) return -1;
            System.Windows.Automation.Text.TextPatternRange[] aSel = tp.GetSelection();
            if (aSel == null || aSel.Length == 0) return -1;
            System.Windows.Automation.Text.TextPatternRange r = tp.DocumentRange.Clone();
            r.MoveEndpointByRange(System.Windows.Automation.Text.TextPatternRangeEndpoint.End,
                aSel[0], System.Windows.Automation.Text.TextPatternRangeEndpoint.Start);
            string s = r.GetText(-1);
            return (s == null) ? -1 : s.Length;
        }
        catch { return -1; }
    }

    static int DlugoscTekstu(AutomationElement okno)
    {
        try
        {
            if (s_pole == null)
                s_pole = okno.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.IsTextPatternAvailableProperty, true));
            if (s_pole == null) return -1;
            TextPattern tp = s_pole.GetCurrentPattern(TextPattern.Pattern) as TextPattern;
            if (tp == null) return -1;
            string s = tp.DocumentRange.GetText(-1);
            return (s == null) ? -1 : s.Length;
        }
        catch { return -1; }
    }

    static string TekstProbny()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("# Proba stabilnosci kursora\r\n\r\n");
        for (int i = 1; i <= 60; i++)
            sb.Append("Wiersz numer " + i + " sluzy do tego, zeby plik byl dlugi i zeby bylo dokad wrocic.\r\n");
        return sb.ToString();
    }

    static void Main(string[] args)
    {
        string sProba = Path.Combine(Path.GetTempPath(), "proba_stabilnosc_kursora.md");
        File.WriteAllText(sProba, TekstProbny(), new UTF8Encoding(false));

        string sExe = args.Length > 0 ? args[0] : @"C:\EdSharpBuild\EdSharpNG.exe";
        Console.WriteLine("Program: " + sExe);
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

            string sKlasa;
            IntPtr h = OknoZFokusem(out sKlasa);
            int iDl = DlugoscTekstu(okno);
            Console.WriteLine("Kontrolka: " + sKlasa + ", znakow: " + iDl);
            if (iDl < 500) { Console.WriteLine("SONDA GLUCHA: plik sie nie wczytal."); Environment.Exit(2); }

            // KURSOR STAWIAMY KLAWIATURA, NIE KOMUNIKATEM.
            // EM_EXSETSEL wyslany do okna kontrolki nic nie zmienil (karetka
            // zostawala na 0) - zmierzone 16.09.2026.  Program ma wlasna obsluge
            // pozycji kursora i to ona jest zrodlem prawdy, wiec jedziemy tak,
            // jak jedzie czlowiek: Ctrl+Home i 30 razy strzalka w dol.
            SendKeys("^{HOME}");
            Thread.Sleep(700);
            for (int i = 0; i < 30; i++) { SendKeys("{DOWN}"); Thread.Sleep(90); }
            Thread.Sleep(900);
            int iPrzed = Karetka(okno);
            Console.WriteLine("Kursor po zjechaniu w dol: " + iPrzed);
            if (iPrzed < 100) { Console.WriteLine("SONDA GLUCHA: kursor nie ruszyl z poczatku pliku."); Environment.Exit(2); }

            // KONTROLA POZYTYWNA calego pomiaru: zanim uznamy brak skoku za
            // sukces, sprawdzmy, ze pisanie w ogole dziala - dlugosc tekstu ma
            // wzrosnac.  Inaczej "kursor nie skoczyl" znaczylo by tylko tyle, ze
            // nic sie nie stalo.
            Console.WriteLine();
            Console.WriteLine("=== proba: 8 znakow wpisanych w srodku pliku ===");
            int iNajdalszySkok = 0;
            for (int i = 0; i < 8; i++)
            {
                SendKeys("x");
                Thread.Sleep(450);
                int iTeraz = Karetka(okno);
                int iOczek = iPrzed + 1;
                int iRoznica = Math.Abs(iTeraz - iOczek);
                if (iRoznica > iNajdalszySkok) iNajdalszySkok = iRoznica;
                Console.WriteLine("  po znaku " + (i + 1) + ": kursor " + iTeraz + " (oczekiwany " + iOczek + ")");
                if (iTeraz < 100) Console.WriteLine("    SKOK NA POCZATEK PLIKU");
                iPrzed = iTeraz;
            }

            int iDlPo = DlugoscTekstu(okno);
            Console.WriteLine();
            Console.WriteLine("Znakow przed: " + iDl + ", po: " + iDlPo);

            Console.WriteLine();
            Console.WriteLine("=== proba 2: PODGLAD - czy kursor zrodla podaza i nie wraca na poczatek ===");
            // TO JEST WLASCIWY SCENARIUSZ ZE ZGLOSZENIA: "preview podglad tez
            // czesto nie podaza, wraca do poczatku pliku".  Wchodzimy w podglad
            // (Escape na pliku .md), jedziemy w nim w dol, wychodzimy Escape i
            // patrzymy, GDZIE stoi kursor w dokumencie.  Ma stac tam, gdzie
            // czytalismy, a nie na poczatku pliku.
            SendKeys("^{HOME}");
            Thread.Sleep(600);
            SendKeys("{ESC}");           // wejscie w podglad
            Thread.Sleep(3500);
            s_pole = null;               // podglad to INNA kontrolka
            for (int i = 0; i < 25; i++) { SendKeys("{DOWN}"); Thread.Sleep(110); }
            Thread.Sleep(900);
            int iWPodgladzie = Karetka(okno);
            Console.WriteLine("Kursor w podgladzie po zjechaniu: " + iWPodgladzie);
            SendKeys("{ESC}");           // powrot do edycji
            Thread.Sleep(2500);
            s_pole = null;               // znowu kontrolka edycyjna
            int iPoPowrocie = Karetka(okno);
            Console.WriteLine("Kursor w dokumencie po powrocie: " + iPoPowrocie);

            Console.WriteLine();
            Console.WriteLine("=== WYNIK ===");
            if (iDlPo <= iDl)
            {
                Console.WriteLine("SONDA GLUCHA: tekst nie urosl - klawisze nie dochodza, wynik nic nie dowodzi.");
                iKod = 2;
            }
            else if (iWPodgladzie < 100)
            {
                Console.WriteLine("FAIL: kursor w PODGLADZIE nie ruszyl z poczatku (stoi na "
                    + iWPodgladzie + ") - podglad nie podaza za nawigacja.");
                iKod = 1;
            }
            else if (iPoPowrocie < 100)
            {
                Console.WriteLine("FAIL: po wyjsciu z podgladu kursor w dokumencie WROCIL NA POCZATEK PLIKU"
                    + " (stoi na " + iPoPowrocie + ", a w podgladzie byl na " + iWPodgladzie + ").");
                iKod = 1;
            }
            else if (iNajdalszySkok == 0)
                Console.WriteLine("PASS: kursor stoi tam, gdzie powinien - przy pisaniu"
                    + " i po powrocie z podgladu (podglad " + iWPodgladzie
                    + ", dokument po powrocie " + iPoPowrocie + ").");
            else
            {
                Console.WriteLine("FAIL: kursor odskoczyl o " + iNajdalszySkok + " znakow od miejsca pisania.");
                iKod = 1;
            }
        }
        finally
        {
            try
            {
                // Zamknij bez zapisywania - plik probny i tak jest tymczasowy.
                if (!p.HasExited) p.Kill();
            }
            catch { }
        }
        Environment.Exit(iKod);
    }

    static void SendKeys(string s)
    {
        System.Windows.Forms.SendKeys.SendWait(s);
    }
}
