// POMIAR: brak Ude.dll NIE MOZE blokowac otwierania plikow (5.0.95).
//
// SKAD TA SONDA: 13.09.2026 wydana paczka nie otwierala ZADNEGO pliku. Kazde
// otwarcie konczylo sie "Cannot open file! Opening temporary copy.", a potem
// mylacym "Nie mozna odnalezc pliku EdSharp.tmp". Przyczyna: binarka byla
// zbudowana z Ude (symbol HAVEUDE), ale instalator nie pakowal Ude.dll (flaga
// "skipifsourcedoesntexist" pomijala ja po cichu). .NET rzucal wtedy
// FileNotFoundException przy KOMPILACJI metody DetectEncodingNoBom - czyli
// PRZED wejsciem w jej try/catch, wiec try nie lapal tego wcale.
//
// DWA WARUNKI, oba mierzone tutaj:
//   1. Wolanie Ude siedzi w OSOBNEJ metodzie DetectEncodingUde, a metoda
//      wolajaca owija to w try - wtedy brak biblioteki degraduje sie do
//      domyslnego utf8b, zamiast psuc otwieranie plikow.
//   2. Ude.dll LEZY OBOK binarki (czyli instalator ja spakowal).
//
// URUCHOMIENIE (z katalogu z EdSharpNG.exe):
//   csc /nologo /target:exe /out:out_ude.exe /r:EdSharpNG.exe pomiar_ude_595.cs
//   out_ude.exe EdSharpNG.exe
using System;
using System.IO;
using System.Reflection;
using System.Text;

class PomiarUde {
    static int iZle = 0, iOk = 0;
    static Assembly asm;

    static void Sprawdz(bool b, string sOpis) {
        if (b) { iOk++; Console.WriteLine("  OK   " + sOpis); }
        else { iZle++; Console.WriteLine("  ZLE  " + sOpis); }
    }

    static Type Typ(string s) {
        foreach (Type t in asm.GetTypes()) if (t.Name == s) return t;
        return null;
    }

    static MethodInfo Metoda(string sKlasa, string sMetoda) {
        Type t = Typ(sKlasa);
        if (t == null) return null;
        foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                               | BindingFlags.Instance | BindingFlags.Static))
            if (mi.Name == sMetoda) return mi;
        return null;
    }

    static int Main(string[] args) {
        string sExe = (args.Length > 0) ? args[0] : "EdSharpNG.exe";
        string sKatalog = Path.GetDirectoryName(Path.GetFullPath(sExe));
        asm = Assembly.LoadFrom(Path.GetFullPath(sExe));
        Console.WriteLine("POMIAR Ude 5.0.95 na " + sExe);
        Console.WriteLine();

        // ---------- 1. STRUKTURA: wolanie Ude w osobnej metodzie ----------
        Console.WriteLine("1. brak Ude.dll nie moze wywrocic otwierania");
        bool bMaUde = false;
        try {
            byte[] d = File.ReadAllBytes(Path.GetFullPath(sExe));
            string sL = Encoding.GetEncoding("latin1").GetString(d);
            // WZORZEC: same "CharsetDetector" w UTF-8.  Pelne "Ude.CharsetDetector"
            // NIE WYSTEPUJE - metadane .NET trzymaja przestrzen nazw ("Ude") i nazwe
            // typu ("CharsetDetector") w OSOBNYCH napisach, wiec pytanie o zlaczona
            // nazwe dawalo False na binarce, ktora Ude wola. Zlapala to kontrola
            // z punktu 3 (odczyt dzialal) zestawiona z tym False.
            bMaUde = sL.Contains("CharsetDetector");
        } catch {}
        Console.WriteLine("     (binarka wola Ude: " + bMaUde + ")");

        if (bMaUde) {
            Sprawdz(Metoda("Util", "DetectEncodingUde") != null,
                    "wolanie Ude WYDZIELONE do DetectEncodingUde (inaczej try nie lapie braku biblioteki)");
            // ---------- 2. Ude.dll lezy obok binarki ----------
            Sprawdz(File.Exists(Path.Combine(sKatalog, "Ude.dll")),
                    "Ude.dll lezy obok binarki (instalator ja spakowal)");
        } else {
            Console.WriteLine("     binarka NIE wola Ude - punkty 1-2 nie dotycza");
            iOk += 2;
        }

        // ---------- 3. ROZSTRZYGAJACE: plik da sie odczytac ----------
        // To jest wlasciwy dowod. Wolamy dokladnie te droge, ktora padala:
        // File2String -> GetFileEncoding -> DetectEncodingNoBom.
        Console.WriteLine();
        Console.WriteLine("3. odczyt pliku dziala (droga, ktora padala)");
        string sProbka = Path.Combine(Path.GetTempPath(), "pomiar_ude_probka.md");
        try {
            // Polskie litery przez \u, zeby ten plik zrodlowy zostal ASCII.
            File.WriteAllText(sProbka, "# Naglowek\r\n\r\nZa\u017c\u00f3\u0142\u0107 g\u0119\u015bl\u0105 ja\u017a\u0144.\r\n",
                              new UTF8Encoding(true));
            MethodInfo miF2S = null;
            Type tUtil = Typ("Util");
            if (tUtil != null)
                foreach (MethodInfo mi in tUtil.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                                           | BindingFlags.Static)) {
                    if (mi.Name != "File2String") continue;
                    ParameterInfo[] ap = mi.GetParameters();
                    if (ap.Length == 2 && ap[1].ParameterType.IsByRef) { miF2S = mi; break; }
                }
            if (miF2S == null) {
                Sprawdz(false, "metoda File2String(string, ref Encoding) istnieje");
            } else {
                string sText = null;
                bool bRzucilo = false;
                string sBlad = "";
                try {
                    object[] aArgs = new object[] { sProbka, Encoding.UTF8 };
                    sText = (string) miF2S.Invoke(null, aArgs);
                } catch (Exception ex) {
                    bRzucilo = true;
                    Exception real = (ex is TargetInvocationException) ? ex.InnerException : ex;
                    sBlad = real.GetType().Name + ": " + real.Message;
                }
                Sprawdz(!bRzucilo, "odczyt pliku NIE rzuca wyjatku" + (bRzucilo ? " (rzucil: " + sBlad + ")" : ""));
                // KONTROLA ROZNICUJACA: nie wystarczy "nie rzucilo" - tresc musi
                // wrocic z polskimi literami, inaczej pusty odczyt tez byl by zielony.
                Sprawdz(sText != null && sText.Contains("Za\u017c\u00f3\u0142\u0107"),
                        "odczytana tresc ma polskie litery (nie pusta, nie uszkodzona)");
            }
        } finally {
            try { if (File.Exists(sProbka)) File.Delete(sProbka); } catch {}
        }

        Console.WriteLine();
        Console.WriteLine("RAZEM: " + iOk + " OK, " + iZle + " ZLE");
        return (iZle == 0) ? 0 : 1;
    }
}
