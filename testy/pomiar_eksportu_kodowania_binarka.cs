// Pomiar NA ZBUDOWANEJ BINARCE (refleksja), ze zapis z koncami Maca i Linuksa
// idzie w UTF-8, a nie w ANSI systemu.
//
// Mierzymy PRAWDZIWE metody z EdSharpNG.exe (Util.Convert2UnixLineBreak,
// Convert2MacLineBreak, Convert2Ascii), a nie ich kopie - kopia logiki
// dowodzilaby tylko tego, ze umiem przepisac kod.
//
// PULAPKA REFLEKSJI, ktora tu obowiazuje: typ to EdSharp.Util, nie Util
// (EdSharp.cs ma namespace EdSharp).
using System;
using System.IO;
using System.Reflection;
using System.Text;

public class P
{
    static int ok = 0, zle = 0;
    static Type tUtil;

    static void A(string opis, bool war, string detal)
    {
        Console.WriteLine((war ? "  OK    " : "  UWAGA ") + opis + "  |  " + detal);
        if (war) ok++; else zle++;
    }

    static string Wolaj(string metoda, string arg)
    {
        MethodInfo mi = tUtil.GetMethod(metoda, BindingFlags.Public | BindingFlags.Static);
        if (mi == null) return null;
        return (string)mi.Invoke(null, new object[] {arg});
    }

    static string Hex(byte[] b, int ile)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < b.Length && i < ile; i++) sb.Append(b[i].ToString("x2") + " ");
        return sb.ToString().Trim();
    }

    static bool CzyUtf8(byte[] b)
    {
        try {new UTF8Encoding(false, true).GetString(b);} catch (Exception) {return false;}
        foreach (byte x in b) if (x > 0x7f) return true;   // musi byc wielobajtowy
        return false;
    }

    public static int Main()
    {
        string exe = @"D:\projekty\edsharp-pr\EdSharpNG.exe";
        Assembly asm = Assembly.LoadFrom(exe);
        tUtil = asm.GetType("EdSharp.Util");

        // KONTROLE POZYTYWNE SONDY: bez nich "nie widze bledu" moze znaczyc
        // "nie widze niczego, bo pomylilem nazwe typu".
        A("P1 KONTROLA: typ EdSharp.Util jest w binarce", tUtil != null,
          (tUtil == null) ? "BRAK - dalsze wyniki NIC NIE ZNACZA" : tUtil.FullName);
        if (tUtil == null) {Console.WriteLine("SONDA GLUCHA"); return 2;}

        string tekst = "Za\u017c\u00f3\u0142\u0107 g\u0119\u015bl\u0105 ja\u017a\u0144\r\nDruga\r\n";

        string unx = Wolaj("Convert2UnixLineBreak", tekst);
        string mac = Wolaj("Convert2MacLineBreak", tekst);
        string asc = Wolaj("Convert2Ascii", tekst);
        A("P2 KONTROLA: Convert2UnixLineBreak istnieje i zwraca tekst",
          unx != null && unx.Length > 0, "");
        A("P3 KONTROLA: Convert2MacLineBreak istnieje i zwraca tekst",
          mac != null && mac.Length > 0, "");
        A("P4 KONTROLA: Convert2Ascii istnieje i zwraca tekst",
          asc != null && asc.Length > 0, "");
        if (unx == null || mac == null || asc == null) {Console.WriteLine("SONDA GLUCHA"); return 2;}

        Directory.CreateDirectory(@"C:\tmp\eksport");
        Encoding enUtf8 = new UTF8Encoding(false);

        // --- tak, jak robi to program po zmianie ---
        string fUnx = @"C:\tmp\eksport\bin_unx.txt";
        File.WriteAllText(fUnx, unx, enUtf8);
        byte[] bUnx = File.ReadAllBytes(fUnx);
        Console.WriteLine("  bajty unx: " + Hex(bUnx, 14));
        A("1 plik z koncami Linuksa jest w UTF-8 (polskie litery wielobajtowe)",
          CzyUtf8(bUnx), "");
        A("1b konce wiersza faktycznie Unix (zero CR)",
          !unx.Contains("\r") && unx.Contains("\n"), "");

        string fMac = @"C:\tmp\eksport\bin_mac.txt";
        File.WriteAllText(fMac, mac, enUtf8);
        byte[] bMac = File.ReadAllBytes(fMac);
        A("2 plik z koncami Maca jest w UTF-8", CzyUtf8(bMac), "");
        A("2b konce wiersza faktycznie Mac (CR bez LF)",
          mac.Contains("\r") && !mac.Contains("\n"), "");

        A("3 brak znacznika BOM (plik Linuksa musi zaczynac sie od tresci)",
          !(bUnx.Length > 2 && bUnx[0] == 0xEF && bUnx[1] == 0xBB && bUnx[2] == 0xBF),
          Hex(bUnx, 3));

        A("4 tresc wraca bez strat po odczycie jako UTF-8",
          File.ReadAllText(fUnx, new UTF8Encoding(false)).Contains("Za\u017c\u00f3\u0142\u0107"),
          "");

        // --- KONTROLA, ze nie zepsulem pozycji ASCII ---
        string fAsc = @"C:\tmp\eksport\bin_asc.txt";
        File.WriteAllText(fAsc, asc, Encoding.Default);
        byte[] bAsc = File.ReadAllBytes(fAsc);
        bool czyste = true;
        foreach (byte b in bAsc) if (b > 0x7f) czyste = false;
        A("5 KONTROLA: pozycja ASCII nadal jednobajtowa (jej nie ruszamy)",
          czyste, Hex(bAsc, 10));
        A("5b KONTROLA: Convert2Ascii faktycznie zdjal ogonki",
          !asc.Contains("\u017c") && !asc.Contains("\u0107"), "");

        // --- KONTROLA ROZNICUJACA: stary sposob MUSI dac inny wynik ---
        // Bez tego "UTF-8 dziala" nie odroznia naprawy od stanu sprzed niej.
        string fStary = @"C:\tmp\eksport\bin_stary.txt";
        File.WriteAllText(fStary, unx, Encoding.Default);
        byte[] bStary = File.ReadAllBytes(fStary);
        A("6 KONTROLA ROZNICUJACA: stary sposob (ANSI) daje INNE bajty",
          !CzyUtf8(bStary) && bStary.Length != bUnx.Length,
          "ANSI " + bStary.Length + " bajtow, UTF-8 " + bUnx.Length + " bajtow");

        Console.WriteLine();
        Console.WriteLine("zaliczone: " + ok + " z " + (ok + zle));
        return (zle == 0) ? 0 : 1;
    }
}
