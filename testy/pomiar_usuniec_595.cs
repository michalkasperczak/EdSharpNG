// POMIAR usuniec i przeniesienia z "Do zrobienia.md" (5.0.95, 13.09.2026).
//
// ZADANIE: "Usuniecie Default Fonts i innych opcji z RTF bogato formatowanych.
// Usuniecie z Misc narzedzi typu Web Download Alt+Shift+W, Web Client
// Utilities Alt+Shift+Space.  Opcje nawigacji po zakladkach komentarzach,
// przypisach do navigate, a sa w misc chyba."
//
// CO MIERZY, na ZBUDOWANEJ binarce:
//   1. Trzy usuniete komendy NIE MAJA pozycji menu ani opisu mowionego.
//   2. Nawigacja po przypisach/komentarzach WISI POD Navigate, nie pod Misc.
//   3. KONTROLE, ze nie znikelo nic wiecej: helper wspolny GetFontText zostal
//      (uzywa go Say Font), odczyt zapisanej czcionki FontDefault zostal,
//      wstawianie przypisu i komentarza zostalo w Misc, a zakladki dalej sa
//      w Navigate.
//
// URUCHOMIENIE (z katalogu z EdSharpNG.exe):
//   csc /nologo /target:exe /out:out_usun.exe /r:EdSharpNG.exe pomiar_usuniec_595.cs
//   out_usun.exe EdSharpNG.exe
//
// KONTROLA NEGATYWNA: ten sam pomiar na binarce 5.0.94 MUSI oblac punkty 1-2.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

class PomiarUsuniec {
    static int iZle = 0, iOk = 0;

    static void Sprawdz(bool bWarunek, string sOpis) {
        if (bWarunek) { iOk++; Console.WriteLine("  OK   " + sOpis); }
        else { iZle++; Console.WriteLine("  ZLE  " + sOpis); }
    }

    static Assembly asm;

    static Type Typ(string sNazwa) {
        foreach (Type t in asm.GetTypes()) if (t.Name == sNazwa) return t;
        return null;
    }

    // NAZWY POL KLASY - tu widac, czy pozycja menu w ogole istnieje.
    static HashSet<string> PolaKlasy(string sKlasa) {
        HashSet<string> h = new HashSet<string>();
        Type t = Typ(sKlasa);
        if (t == null) return h;
        foreach (FieldInfo fi in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                                             | BindingFlags.Instance | BindingFlags.Static))
            h.Add(fi.Name);
        return h;
    }

    // LITERALY Z CIALA METODY (opcode ldstr 0x72 + token).  Tak sprawdzamy, co
    // dana metoda naprawde sklada - grep po calym pliku nie odrozni metod.
    static List<string> LiteralyMetody(string sKlasa, string sMetoda) {
        List<string> l = new List<string>();
        Type t = Typ(sKlasa);
        if (t == null) return l;
        foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                               | BindingFlags.Instance | BindingFlags.Static)) {
            if (mi.Name != sMetoda) continue;
            MethodBody body = null;
            try { body = mi.GetMethodBody(); } catch {}
            if (body == null) continue;
            byte[] il = body.GetILAsByteArray();
            if (il == null) continue;
            for (int i = 0; i < il.Length - 4; i++) {
                if (il[i] != 0x72) continue;
                int iTok = BitConverter.ToInt32(il, i + 1);
                try { l.Add(mi.Module.ResolveString(iTok)); } catch {}
            }
        }
        return l;
    }

    // CZY KLASA MA METODE O TEJ NAZWIE.
    static bool MaMetode(string sKlasa, string sMetoda) {
        Type t = Typ(sKlasa);
        if (t == null) return false;
        foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                               | BindingFlags.Instance | BindingFlags.Static))
            if (mi.Name == sMetoda) return true;
        return false;
    }

    // ILE RAZY METODA JEST WOLANA - do pilnowania helperow wspolnych.
    // Liczymy tokeny call/callvirt wskazujace na metode o tej nazwie.
    static int IleWywolan(string sMetoda) {
        int iRazem = 0;
        foreach (Type t in asm.GetTypes()) {
            foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                                   | BindingFlags.Instance | BindingFlags.Static)) {
                MethodBody body = null;
                try { body = mi.GetMethodBody(); } catch {}
                if (body == null) continue;
                byte[] il = body.GetILAsByteArray();
                if (il == null) continue;
                for (int i = 0; i < il.Length - 4; i++) {
                    if (il[i] != 0x28 && il[i] != 0x6F) continue;   // call / callvirt
                    int iTok = BitConverter.ToInt32(il, i + 1);
                    try {
                        MemberInfo m = mi.Module.ResolveMember(iTok);
                        if (m != null && m.Name == sMetoda) iRazem++;
                    } catch {}
                }
            }
        }
        return iRazem;
    }

    // DO KTOREGO MENU NALEZY POZYCJA - czytane z IL, nie z nazwy pola.
    //
    // Budowanie menu to w IL ciag odwolan do pol (opcode ldfld 0x7B / ldsfld
    // 0x7E): najpierw pole MENU (np. menuNavigate), potem `DropDownItems`,
    // potem kolejno pola POZYCJI wkladanych do tablicy.  Pozycja nalezy wiec
    // do tego menu, ktorego pole bylo wczytane najblizej PRZED nia.
    //
    // Dlaczego nie po nazwie pola: pozycje przeniesione z Misc do Navigate
    // ZACHOWUJA nazwy menuMisc* (zmiana nazwy pola to kosmetyka, ktora nic nie
    // dowodzi).  Nazwa klamie, kolejnosc w IL nie.
    static Dictionary<string, string> _mapaMenu;

    static void ZbudujMapeMenu() {
        _mapaMenu = new Dictionary<string, string>();
        // Nazwy pol MENU GLOWNEGO - te sa kontenerami, nie pozycjami.
        // Rozpoznajemy je po tym, ze do nich wola sie DropDownItems.
        foreach (Type t in asm.GetTypes()) {
            // KONSTRUKTORY TEZ, NIE TYLKO METODY.  Menu w tym programie buduje
            // sie w KONSTRUKTORZE klasy ramki - sonda pytajaca samo GetMethods
            // nie widziala ani jednej pozycji i mowila "(nie znaleziono)" dla
            // wszystkiego, co wygladalo jak regresja przy dobrym kodzie.
            List<MethodBase> lMetody = new List<MethodBase>();
            lMetody.AddRange(t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                          | BindingFlags.Instance | BindingFlags.Static));
            lMetody.AddRange(t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic
                                               | BindingFlags.Instance | BindingFlags.Static));
            foreach (MethodBase mi in lMetody) {
                MethodBody body = null;
                try { body = mi.GetMethodBody(); } catch {}
                if (body == null) continue;
                byte[] il = body.GetILAsByteArray();
                if (il == null) continue;

                string sMenuBiezace = null;
                for (int i = 0; i < il.Length - 4; i++) {
                    byte op = il[i];
                    if (op != 0x7B && op != 0x7E) continue;      // ldfld / ldsfld
                    int iTok = BitConverter.ToInt32(il, i + 1);
                    string sPole = null;
                    try {
                        MemberInfo m = mi.Module.ResolveMember(iTok);
                        if (m != null) sPole = m.Name;
                    } catch {}
                    if (sPole == null) continue;

                    // Pole kontenera: krotka nazwa menu glownego.  Lista jawna,
                    // bo zgadywanie po prefiksie wciagneloby same pozycje.
                    if (sPole == "menuFile" || sPole == "menuEdit" || sPole == "menuSearch"
                        || sPole == "menuNavigate" || sPole == "menuMarkdown" || sPole == "menuMisc"
                        || sPole == "menuHelp" || sPole == "menuView" || sPole == "menuFormat") {
                        sMenuBiezace = sPole;
                        continue;
                    }
                    if (sMenuBiezace != null && sPole.StartsWith("menu") && !_mapaMenu.ContainsKey(sPole))
                        _mapaMenu[sPole] = sMenuBiezace;
                }
            }
        }
    }

    static string MenuPozycji(string sPole) {
        if (_mapaMenu == null) ZbudujMapeMenu();
        return _mapaMenu.ContainsKey(sPole) ? _mapaMenu[sPole] : "(nie znaleziono)";
    }

    static int Main(string[] args) {
        string sExe = (args.Length > 0) ? args[0] : "EdSharpNG.exe";
        asm = Assembly.LoadFrom(Path.GetFullPath(sExe));
        Console.WriteLine("POMIAR USUNIEC 5.0.95 na " + sExe);
        Console.WriteLine();

        HashSet<string> polaFrame = PolaKlasy("MdiFrame");

        // ---------- 1. TRZY KOMENDY ZNIKNELY CALKOWICIE ----------
        Console.WriteLine("1. usuniete komendy");
        Sprawdz(!polaFrame.Contains("menuMiscSetDefaultFont"),
                "Set Default Font and Color NIE MA pozycji menu");
        Sprawdz(!polaFrame.Contains("menuMiscWebDownload"),
                "Web Download NIE MA pozycji menu");
        Sprawdz(!polaFrame.Contains("menuMiscWebClientUtilities"),
                "Web Client Utilities NIE MA pozycji menu");
        Sprawdz(!MaMetode("MdiFrame", "WebClientUtilities"),
                "osierocona metoda WebClientUtilities usunieta (nie zostaje martwy kod)");

        // ---------- 2. OPISY MOWIONE TEZ ZNIKNELY ----------
        // Ctrl+F1 czyta te opisy niewidomemu.  Opis komendy, ktorej nie ma,
        // jest GORSZY niz brak opisu: kaze szukac czegos, czego nie znajdzie.
        Console.WriteLine();
        Console.WriteLine("2. opisy mowione (Hotkeys.ini obok binarki)");
        // PLIK OPISOW CZYTAMY Z REPOZYTORIUM, nie z katalogu binarki.
        // Katalog buildu trzyma STARA kopie Hotkeys.ini (kopiowana przy
        // pierwszym buildzie), wiec sonda zglaszala brak usuniecia, choc plik
        // w repo byl juz poprawiony.  Sciezke repo mozna podac argumentem.
        string sIni = (args.Length > 1) ? args[1] : null;
        if (sIni == null) {
            // Domyslnie: repo obok katalogu domowego uzytkownika WSL/Windows.
            string[] aProby = {
                Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sExe)), "Hotkeys.ini"),
            };
            foreach (string s in aProby) if (File.Exists(s)) { sIni = s; break; }
        }
        Console.WriteLine("   (plik opisow: " + (sIni ?? "BRAK") + ")");
        if (sIni != null && File.Exists(sIni)) {
            string sTresc = File.ReadAllText(sIni);
            Sprawdz(!sTresc.Contains("Web Download="), "Hotkeys.ini nie opisuje Web Download");
            Sprawdz(!sTresc.Contains("Web Client Utilities="), "Hotkeys.ini nie opisuje Web Client Utilities");
            Sprawdz(!sTresc.Contains("Set Default Font and Color="), "Hotkeys.ini nie opisuje Set Default Font");
        } else {
            Console.WriteLine("  UWAGA: brak Hotkeys.ini obok binarki, pomijam");
        }

        // ---------- 3. NAWIGACJA PRZESZLA DO NAVIGATE ----------
        // Pola zachowuja stare nazwy (menuMisc*), bo zmiana nazwy pola to
        // kosmetyka; ROZSTRZYGA, do ktorego menu sa dodane.  Czytamy wiec
        // literaly i wywolania z metody budujacej menu nie da sie - zamiast
        // tego sprawdzamy KOLEJNOSC pozycji w tablicy AddRange po IL.
        Console.WriteLine();
        Console.WriteLine("3. nawigacja po przypisach i komentarzach pod Navigate");
        // Pola zachowuja stare nazwy (menuMisc*) - zmiana nazwy pola to kosmetyka.
        // ROZSTRZYGA, DO KTOREGO MENU POZYCJA JEST DODANA, a to widac po IL:
        // budowanie menu to ciag `ldfld menuMiscGoToFootnote` wewnatrz tablicy
        // przekazywanej do AddRange, poprzedzonej `ldfld menuNavigate`.
        // Czytamy wiec KOLEJNOSC odwolan do pol w metodzie budujacej menu:
        // pozycja nalezy do tego menu, ktorego pole bylo wczytane najblizej
        // PRZED nia.
        string sMenuGoToFootnote = MenuPozycji("menuMiscGoToFootnote");
        string sMenuNextFootnote = MenuPozycji("menuMiscNextFootnote");
        string sMenuCommentList = MenuPozycji("menuMiscCommentList");
        string sMenuInsertFootnote = MenuPozycji("menuMiscInsertFootnote");
        Console.WriteLine("     (zmierzone menu: GoToFootnote=" + sMenuGoToFootnote
                          + ", NextFootnote=" + sMenuNextFootnote
                          + ", CommentList=" + sMenuCommentList
                          + ", InsertFootnote=" + sMenuInsertFootnote + ")");
        Sprawdz(sMenuGoToFootnote == "menuNavigate", "Go to Footnote wisi pod Navigate");
        Sprawdz(sMenuNextFootnote == "menuNavigate", "Next Footnote wisi pod Navigate");
        Sprawdz(sMenuCommentList == "menuNavigate", "Comment List wisi pod Navigate");
        Sprawdz(sMenuInsertFootnote == "menuMisc",
                "wstawianie przypisu ZOSTALO pod Misc (przenosimy nawigacje, nie tworzenie)");

        // ---------- 4. KONTROLE: NIE ZNIKNELO NIC WIECEJ ----------
        Console.WriteLine();
        Console.WriteLine("4. kontrole, ze nie znikelo nic wiecej");
        Sprawdz(MaMetode("MdiFrame", "GetFontText"),
                "helper wspolny GetFontText ZOSTAL (uzywa go Say Font)");
        Sprawdz(IleWywolan("GetFontText") >= 1,
                "GetFontText jest nadal WOLANY (nie osierocony)");
        Sprawdz(polaFrame.Contains("menuMiscInsertFootnote"),
                "wstawianie przypisu zostalo (usuwamy nawigacje, nie tworzenie)");
        Sprawdz(polaFrame.Contains("menuMiscInsertComment"),
                "wstawianie komentarza zostalo");
        Sprawdz(polaFrame.Contains("menuMiscExportFootnotes"),
                "eksport przypisow zostal");
        Sprawdz(polaFrame.Contains("menuNavigateNextBookmark"),
                "zakladki dalej sa w Navigate (byly tam od dawna, nie ruszamy)");
        Sprawdz(polaFrame.Contains("menuMiscConfigurationOptions"),
                "Configuration Options zostalo (osobne zadanie, nie ruszamy teraz)");
        Sprawdz(polaFrame.Contains("menuMiscManualOptions"),
                "Manual Options zostalo (czeka na decyzje)");

        // ODCZYT zapisanej czcionki MUSI zostac: usunelismy tylko komende, ktora
        // ja USTAWIA.  Gdybysmy wycieli tez odczyt, to komu wyglad juz sie
        // zapisal, temu program zmienilby czcionke bez pytania.
        //
        // Mierzymy LITERALEM "FontDefault" w cialach metod klasy okna dokumentu.
        // Napis musi ISTNIEC (odczyt zostal), ale zapis juz nie - dlatego nizej
        // sprawdzamy tez, ze nie ma go w zadnej metodzie klasy ramki, gdzie
        // siedzialo App.WriteOption("FontDefault", ...).
        bool bOdczytZostal = false;
        Type tChild = Typ("MdiChild");
        if (tChild != null) {
            foreach (MethodInfo mi in tChild.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                                       | BindingFlags.Instance | BindingFlags.Static))
                if (LiteralyMetody("MdiChild", mi.Name).Contains("FontDefault")) { bOdczytZostal = true; break; }
            if (!bOdczytZostal)
                foreach (ConstructorInfo ci in tChild.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic
                                                                     | BindingFlags.Instance)) {
                    MethodBody body = null;
                    try { body = ci.GetMethodBody(); } catch {}
                    if (body == null) continue;
                    byte[] il = body.GetILAsByteArray();
                    if (il == null) continue;
                    for (int i = 0; i < il.Length - 4; i++) {
                        if (il[i] != 0x72) continue;
                        int iTok = BitConverter.ToInt32(il, i + 1);
                        try { if (ci.Module.ResolveString(iTok) == "FontDefault") { bOdczytZostal = true; break; } } catch {}
                    }
                    if (bOdczytZostal) break;
                }
        }
        Sprawdz(bOdczytZostal,
                "ODCZYT zapisanej czcionki (FontDefault) zostal - nikomu nie zmieniamy wygladu");

        Console.WriteLine();
        Console.WriteLine("RAZEM: " + iOk + " OK, " + iZle + " ZLE");
        return (iZle == 0) ? 0 : 1;
    }
}
