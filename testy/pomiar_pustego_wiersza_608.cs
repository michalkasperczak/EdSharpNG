// pomiar_pustego_wiersza_608.cs -- CZY PROGRAM MILCZY NA PUSTYM WIERSZU.
//
// ZGLOSZENIE MICHALA 16.09.2026: "Puste empty line.  Empty line niepotrzebne."
// NVDA na pustym wierszu MOWI JUZ SAM ("puste" / "blank").  Nasz komunikat byl
// DRUGIM glosem na to samo, wiec user slyszal pusty wiersz dwa razy.
//
// HISTORIA POMYLKI, ZEBY SIE NIE POWTORZYLA: ta sonda w pierwszej wersji
// sprawdzala, czy liczba komunikatow ROWNA SIE liczbie pustych wierszy - i dala
// PASS na binarce, ktora Michal slyszal jako zepsuta.  Byla GLUCHA nie
// technicznie, ale POJECIOWO: mierzyla CZESTOSC komunikatu, gdy problemem bylo
// jego ISTNIENIE.  Zanim napiszesz pomiar, ustal, CZY funkcja ma sie odezwac -
// nie tylko JAK CZESTO.
//
// CO MIERZY: przejazd strzalka w dol przez plik o znanej tresci, potem liczy w
// Speech.log komunikaty "Empty line" (i stare brzmienie "LineFeed").
// POPRAWNY WYNIK: ZERO.
//
// KONTROLA, ZE SONDA SLYSZY (bez niej zero nic nie dowodzi): plik zawiera
// wiersz z samym TABULATOREM, ktorego czytnik nie oglasza, wiec "TabChar" MUSI
// byc w logu.  Zero "Empty line" PRZY zerze "TabChar" znaczy, ze mowa nie
// trafia do logu - wynik niewazny, a nie "naprawione".
//
// KONTROLA NA STAREJ BINARCE: uruchom z 5.0.108 w argumencie - MUSI dac FAIL.
// Pomiar, ktory nie zapala sie na wersji z bledem, nie mierzy niczego.
//
// URUCHOMIENIE: testy/uruchamiacze/pomiar_puste_wiersze.cmd

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Automation;

class PomiarPustegoWiersza
{
    static string Dane()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EdSharp");
    }

    // Wiersze pliku probnego.  Pierwszy i ostatni NIEpuste, zeby przejazd
    // zaczynal sie i konczyl na tresci.  Wiersz 5 to sam tabulator - to nasza
    // kontrola, ze mowa w ogole dochodzi do logu.
    static string[] Wiersze()
    {
        return new string[] {
            "Pierwszy wiersz z trescia",
            "",
            "Drugi wiersz z trescia",
            "",
            "\t",
            "Trzeci wiersz, dosc dlugi, zeby przeskok w dol byl wiekszy niz jeden znak",
            "",
            "Ostatni wiersz"
        };
    }

    static int PustychWierszy()
    {
        int i = 0;
        foreach (string s in Wiersze()) if (s.Length == 0) i++;
        return i;
    }

    static void Main(string[] args)
    {
        string sDane = Dane();
        string sIni = Path.Combine(sDane, "EdSharp.ini");
        string sLog = Path.Combine(sDane, "Speech.log");
        string sProba = Path.Combine(Path.GetTempPath(), "proba_puste_wiersze.txt");

        File.WriteAllText(sProba, string.Join("\r\n", Wiersze()) + "\r\n", new UTF8Encoding(false));
        Console.WriteLine("Plik probny: " + sProba);
        Console.WriteLine("Pustych wierszy w pliku: " + PustychWierszy() + " (program ma o nich MILCZEC)");

        // Mowa do pliku.  Ampersand jest CZESCIA nazwy klucza.
        string sKopia = sIni + ".pomiar";
        if (File.Exists(sIni)) File.Copy(sIni, sKopia, true);
        string sTresc = File.Exists(sIni) ? File.ReadAllText(sIni) : "[Options]\r\n";
        if (sTresc.Contains("E&xtraSpeech="))
            sTresc = System.Text.RegularExpressions.Regex.Replace(sTresc, "E&xtraSpeech=\"[^\"]*\"", "E&xtraSpeech=\"N\"");
        else
            sTresc = sTresc.Replace("[Options]", "[Options]\r\nE&xtraSpeech=\"N\"");
        File.WriteAllText(sIni, sTresc);
        Console.WriteLine("Mowa przekierowana do Speech.log");

        string sExe = args.Length > 0 ? args[0] : @"C:\EdSharpBuild\EdSharpNG.exe";
        Console.WriteLine("Mierzona binarka: " + sExe);
        Process p = Process.Start(sExe, "\"" + sProba + "\"");
        Thread.Sleep(9000);

        try
        {
            AutomationElement okno = null;
            for (int i = 0; i < 20 && okno == null; i++)
            {
                okno = AutomationElement.RootElement.FindFirst(TreeScope.Children,
                    new PropertyCondition(AutomationElement.ProcessIdProperty, p.Id));
                if (okno == null) Thread.Sleep(600);
            }
            if (okno == null) { Console.WriteLine("BLAD: nie widze okna programu - pomiar niewazny."); return; }
            Console.WriteLine("Okno: " + okno.Current.Name);
            okno.SetFocus();
            Thread.Sleep(1500);

            // Kursor na sam poczatek, potem przejazd w dol przez caly plik.
            SendKeys("^{HOME}");
            Thread.Sleep(1200);
            long iOdciecie = File.Exists(sLog) ? new FileInfo(sLog).Length : 0;
            Console.WriteLine("Log przed przejazdem: " + iOdciecie + " B");

            for (int i = 0; i < Wiersze().Length - 1; i++)
            {
                SendKeys("{DOWN}");
                Thread.Sleep(900);
            }
            // Ruch W BOK w pustym wierszu: dawniej mnozyl komunikaty.  Wracam na
            // pusty wiersz i przesuwam sie w nim, zeby wylapac takze te
            // powtorzenia, a nie tylko jedno wejscie do wiersza.
            SendKeys("^{HOME}");
            Thread.Sleep(900);
            SendKeys("{DOWN}");
            Thread.Sleep(900);
            for (int i = 0; i < 4; i++) { SendKeys("{RIGHT}"); Thread.Sleep(500); }
            for (int i = 0; i < 4; i++) { SendKeys("{LEFT}"); Thread.Sleep(500); }
            Thread.Sleep(1500);

            string sPo = "";
            using (FileStream fs = new FileStream(sLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(iOdciecie, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(fs)) sPo = sr.ReadToEnd();
            }

            int iEmpty = 0, iTab = 0;
            foreach (string s in sPo.Split('\n'))
            {
                // Stare brzmienie tez lapie - zamiana slowa nie jest naprawa.
                if (s.Contains("Empty line") || s.Contains("LineFeed")) iEmpty++;
                if (s.Contains("TabChar")) iTab++;
            }

            Console.WriteLine("--- co program powiedzial w czasie przejazdu ---");
            Console.WriteLine(sPo.Trim());
            Console.WriteLine("--- wynik ---");
            Console.WriteLine("\"Empty line\"/\"LineFeed\": " + iEmpty + " (nalezy sie 0)");
            Console.WriteLine("\"TabChar\": " + iTab + " (nalezy sie co najmniej 1 - kontrola slyszenia)");

            if (iTab == 0)
                Console.WriteLine("SONDA GLUCHA: brak \"TabChar\" - mowa nie trafia do logu, wynik NIC nie dowodzi.");
            else if (iEmpty == 0)
                Console.WriteLine("PASS: program milczy na pustym wierszu, glos oddany czytnikowi.");
            else
                Console.WriteLine("FAIL: program nadal oglasza pusty wiersz " + iEmpty + " raz(y) - to jest zgloszenie Michala.");
        }
        finally
        {
            try { if (!p.HasExited) p.Kill(); } catch { }
            Thread.Sleep(1200);
            if (File.Exists(sKopia)) { File.Copy(sKopia, sIni, true); File.Delete(sKopia); }
            Console.WriteLine("Ustawienia przywrocone.");
        }
    }

    // WOLAJ SendKeys WPROST, NIE PRZEZ REFLEKSJE.  Type.GetType z krotka nazwa
    // zestawu ("System.Windows.Forms") zwraca NULL, bo zestaw nie jest jeszcze
    // wczytany do procesu - a wtedy pomiar wywala sie na NullReferenceException
    // PO uruchomieniu programu i po podmianie ustawien.  Zmierzone 16.09.2026.
    // Zestaw jest podany w /reference, wiec wystarczy zwykle wywolanie.
    static void SendKeys(string s)
    {
        System.Windows.Forms.SendKeys.SendWait(s);
    }
}
