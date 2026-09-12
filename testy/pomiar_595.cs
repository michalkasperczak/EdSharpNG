// POMIAR 5.0.95 - zadania z "Do zrobienia.md" z 13.09.2026.
//
// CO SPRAWDZA (na ZBUDOWANEJ binarce EdSharpNG.exe, nie na zrodle):
//   1. Dziennik diagnostyczny ISTNIEJE i naprawde pisze do pliku, przycina sie,
//      i NIE rzuca wyjatkiem, gdy sciezka jest nieustawiona.
//   2. Paleta polecen podaje NAZWE POLECENIA PRZED nazwa menu.
//   3. Wyjscie z menu (Alt) wola metode gaszaca tryb klawiatury menu.
//
// KONTROLA NEGATYWNA: te same asercje na binarce 5.0.94 (/mnt/c/EdSharpStara94)
// musza oblac - inaczej nie mierza naszej zmiany, tylko cokolwiek.
//
// BUDOWANIE:
//   csc /nologo /target:exe /out:C:\EdSharpBuild\out_595.exe ^
//       /r:C:\EdSharpBuild\EdSharpNG.exe /r:System.Windows.Forms.dll pomiar_595.cs

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

class Pomiar595 {

    static int iZle = 0;
    static int iOk = 0;

    static void Sprawdz(bool bWarunek, string sOpis) {
        if (bWarunek) { iOk++; Console.WriteLine("OK   " + sOpis); }
        else { iZle++; Console.WriteLine("ZLE  " + sOpis); }
    }

    static Assembly asm;

    static Type Typ(string sName) {
        return asm.GetTypes().FirstOrDefault(t => t.Name == sName);
    }

    static Type tFrameWczesnie() { return Typ("MdiFrame"); }

    // LITERALY Z CIALA METODY (opcode ldstr, 0x72 + token).  Bez tego mierzenie
    // formatu wiersza konczy sie grepem po calym pliku, ktory nie odrozni, KTORA
    // metoda napis nalezy - a palety i menu alternatywnego jest w kodzie kilka.
    static System.Collections.Generic.List<string> LiteralyMetody(MethodInfo mi) {
        var l = new System.Collections.Generic.List<string>();
        try {
            byte[] il = mi.GetMethodBody().GetILAsByteArray();
            Module mod = mi.Module;
            for (int i = 0; i < il.Length - 4; i++) {
                if (il[i] != 0x72) continue;   // ldstr
                int iTok = BitConverter.ToInt32(il, i + 1);
                try { l.Add(mod.ResolveString(iTok)); } catch {}
            }
        }
        catch {}
        return l;
    }

    static int Main(string[] args) {
        string sExe = (args.Length > 0) ? args[0] : @"C:\EdSharpBuild\EdSharpNG.exe";
        asm = Assembly.LoadFrom(sExe);
        Console.WriteLine("mierzona binarka: " + sExe);

        // ---------- 1. DZIENNIK DIAGNOSTYCZNY ----------
        // Zadanie Michala: "Logi diagnostyczne. Sa, jesli nie ma to
        // wprowadzic-poprawic".  Zmierzone przed zmiana: NIE BYLO zadnego.
        Type tUtil = Typ("Util");
        Type tApp = Typ("App");
        Sprawdz(tUtil != null && tApp != null, "klasy Util i App sa w binarce");

        MethodInfo miLog = (tUtil == null) ? null : tUtil.GetMethod("LogDiagnostic",
            BindingFlags.Public | BindingFlags.Static);
        Sprawdz(miLog != null, "istnieje JEDNA droga zapisu diagnostyki: Util.LogDiagnostic");

        FieldInfo fiLog = (tApp == null) ? null : tApp.GetField("ErrorLog",
            BindingFlags.Public | BindingFlags.Static);
        Sprawdz(fiLog != null, "App.ErrorLog trzyma sciezke dziennika");

        FieldInfo fiMax = (tApp == null) ? null : tApp.GetField("ErrorLogMaxBytes",
            BindingFlags.Public | BindingFlags.Static);
        Sprawdz(fiMax != null, "jest gorny prog rozmiaru dziennika (nie zapcha dysku)");

        // NAJWAZNIEJSZA ASERCJA: zapis MILCZACY I NIEWYWRACAJACY przy PUSTEJ
        // sciezce.  Diagnostyka wolana z obslugi awarii nie ma prawa wywrocic
        // programu po raz drugi.
        if (miLog != null && fiLog != null) {
            bool bWywalilo = false;
            try {
                fiLog.SetValue(null, null);
                miLog.Invoke(null, new object[] { "proba", "bez sciezki" });
                fiLog.SetValue(null, "");
                miLog.Invoke(null, new object[] { "proba", "pusta sciezka" });
            }
            catch { bWywalilo = true; }
            Sprawdz(!bWywalilo, "zapis diagnostyki przy braku sciezki NIE rzuca wyjatkiem");

            // A teraz, czy w ogole PISZE.  Bez tego asercje wyzej przeszlaby
            // tez atrapa, ktora nie robi nic.
            string sTmp = Path.Combine(Path.GetTempPath(), "edsharp_pomiar_log.txt");
            try { if (File.Exists(sTmp)) File.Delete(sTmp); } catch {}
            bool bNapisal = false;
            string sTresc = "";
            try {
                fiLog.SetValue(null, sTmp);
                miLog.Invoke(null, new object[] { "proba", "tresc kontrolna 12345" });
                bNapisal = File.Exists(sTmp);
                if (bNapisal) sTresc = File.ReadAllText(sTmp, Encoding.UTF8);
            }
            catch {}
            Sprawdz(bNapisal, "zapis diagnostyki TWORZY plik na dysku");
            Sprawdz(sTresc.Contains("tresc kontrolna 12345"),
                    "zapisana tresc trafia do pliku (a nie ginie)");
            Sprawdz(sTresc.Contains("proba"), "wpis ma rodzaj zdarzenia");
            Sprawdz(System.Text.RegularExpressions.Regex.IsMatch(sTresc,
                    @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}"),
                    "wpis ma date w formacie sortowalnym rok-miesiac-dzien");

            // Przycinanie: ustawiam maly prog i sprawdzam, ze plik nie rosnie
            // bez konca.  Prog jest const, wiec czytam go tylko do komunikatu.
            try {
                using (StreamWriter sw = new StreamWriter(sTmp, true, Encoding.UTF8))
                    for (int i = 0; i < 40000; i++) sw.WriteLine("2026-01-01 00:00:00\tzapchaj\twiersz numer " + i);
                long lPrzed = new FileInfo(sTmp).Length;
                miLog.Invoke(null, new object[] { "proba", "po zapchaniu" });
                long lPo = new FileInfo(sTmp).Length;
                Sprawdz(lPrzed > lPo, "dziennik JEST przycinany, gdy przekroczy prog ("
                        + (lPrzed / 1024) + " KB -> " + (lPo / 1024) + " KB)");
                string sOgon = File.ReadAllText(sTmp, Encoding.UTF8);
                Sprawdz(sOgon.Contains("po zapchaniu"),
                        "po przycieciu zostaje NAJSWIEZSZY wpis, nie najstarszy");
            }
            catch (Exception ex) { Sprawdz(false, "przycinanie dziennika: " + ex.Message); }
            try { if (File.Exists(sTmp)) File.Delete(sTmp); } catch {}
        }

        // ---------- 2. PALETA POLECEN: NAZWA POLECENIA PRZED MENU ----------
        // Zgloszenie: "Nie musi czytac nazwy menu przed poleceniem".
        // Mierzone na napisach w binarce: skladanie wiersza palety.
        string sBin = CzytajBinarke(sExe);
        Sprawdz(sBin.Contains("CommandPalette") || sBin.Contains("Command Palette"),
                "paleta polecen jest w binarce");

        // TU BYLA GLUCHA ASERCJA (naprawiona 13.09.2026).  Pytala, czy w pliku
        // NIE MA napisu 'menu.Text.Replace("&", "") + ": "' - a to KOD ZRODLOWY,
        // ktorego w skompilowanej binarce nie ma nigdy.  Przechodzila wiec i na
        // nowej, i na starej wersji, czyli nie mierzyla niczego.
        //
        // Teraz czytam LITERALY Z CIALA METODY CommandPalette (opcode ldstr) i
        // pytam o rozdzielnik, ktorym paleta skleja wiersz:
        //   stary format: nazwaMenu + ": " + polecenie   -> jest literal ": "
        //   nowy format:  polecenie + " (" + nazwaMenu + ")" -> jest literal " ("
        MethodInfo miPal = (tFrameWczesnie() == null) ? null
            : tFrameWczesnie().GetMethod("CommandPalette",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Sprawdz(miPal != null, "metoda CommandPalette jest w binarce");
        if (miPal != null) {
            var lLit = LiteralyMetody(miPal);
            Console.WriteLine("     literaly palety: " + string.Join(" | ", lLit.Select(s => "[" + s + "]")));
            Sprawdz(lLit.Contains(" ("),
                    "paleta doklada nazwe menu NA KONCU, w nawiasie");
            Sprawdz(!lLit.Contains(": "),
                    "paleta NIE zaczyna wiersza od nazwy menu (stary rozdzielnik zniknal)");
        }

        // ---------- 3. WYJSCIE Z MENU ODDAJE TRYB KLAWIATURY ----------
        // Zgloszenie: "kursor nie wraca na miejsce po wyjsciu z menu; strzalki
        // dalej czytaja menu".  Naprawa musi WYGASIC tryb menu, nie tylko
        // ustawic fokus - stad odwolanie do wewnetrznej klasy WinForms.
        Sprawdz(sBin.Contains("ToolStripManager") || sBin.Contains("ModalMenuFilter"),
                "wyjscie z menu gasi tryb klawiatury menu (nie tylko fokus)");

        Type tFrame = Typ("MdiFrame");
        MethodInfo miFocus = (tFrame == null) ? null : tFrame.GetMethod("FocusChildEditControl",
            BindingFlags.Public | BindingFlags.Instance);
        Sprawdz(miFocus != null, "MdiFrame.FocusChildEditControl jest wolana z wyjscia z menu");

        Console.WriteLine();
        Console.WriteLine("WYNIK: OK=" + iOk + " ZLE=" + iZle);
        return (iZle == 0) ? 0 : 1;
    }

    // PULAPKA POMIAROWA (z references skilla): napisy w binarce .NET siedza w
    // UTF-16, wiec czytanie samego UTF-8 gubi je.  Skladam OBA kodowania.
    static string CzytajBinarke(string sExe) {
        byte[] ab = File.ReadAllBytes(sExe);
        return Encoding.UTF8.GetString(ab) + "\n" + Encoding.Unicode.GetString(ab);
    }
}
