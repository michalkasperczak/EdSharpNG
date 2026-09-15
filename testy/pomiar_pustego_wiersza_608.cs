// pomiar_pustego_wiersza_608.cs -- CZY "Empty line" PADA TYLKO NA PUSTYM WIERSZU.
//
// ZGLOSZENIE MICHALA 15.09.2026: "Teraz ciagle czyta Empty line na pustych
// liniach."  Slowo "ciagle" znaczy tu: bez przerwy, takze tam, gdzie nie
// powinno.  Pomiar ma to rozstrzygnac liczbami, nie wrazeniem.
//
// CO MIERZY: przejazd strzalka w dol przez plik, w ktorym WIADOMO, ktore
// wiersze sa puste, a potem porownanie liczby komunikatow "Empty line" w
// Speech.log z liczba pustych wierszy w pliku.
//
// KONTROLA POZYTYWNA (bez niej wynik nic nie dowodzi): plik zawiera 3 puste
// wiersze, wiec poprawny wynik to DOKLADNIE 3 komunikaty.  Zero komunikatow
// znaczy, ze sonda jest glucha (np. mowa nie idzie do logu), a nie ze naprawa
// dziala.
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
    // zaczynal sie i konczyl na tresci.  Wiersz 5 to sam tabulator - NIE jest
    // pusty, wiec nie moze dac "Empty line" (tam nalezy sie "TabChar").
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
        Console.WriteLine("Pustych wierszy w pliku: " + PustychWierszy());

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
                if (s.Contains("Empty line")) iEmpty++;
                if (s.Contains("TabChar")) iTab++;
            }

            Console.WriteLine("--- co program powiedzial w czasie przejazdu ---");
            Console.WriteLine(sPo.Trim());
            Console.WriteLine("--- wynik ---");
            Console.WriteLine("\"Empty line\": " + iEmpty + " (nalezy sie " + PustychWierszy() + ")");
            Console.WriteLine("\"TabChar\": " + iTab + " (nalezy sie 1)");

            if (iEmpty == 0)
                Console.WriteLine("SONDA GLUCHA: zero komunikatow - mowa nie trafia do logu, wynik nic nie dowodzi.");
            else if (iEmpty == PustychWierszy())
                Console.WriteLine("PASS: pusty wiersz zglaszany raz, tylko gdy jest pusty.");
            else if (iEmpty > PustychWierszy())
                Console.WriteLine("FAIL: komunikatow WIECEJ niz pustych wierszy - to jest zgloszenie Michala.");
            else
                Console.WriteLine("FAIL: komunikatow MNIEJ niz pustych wierszy - program milczy tam, gdzie ma mowic.");
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
