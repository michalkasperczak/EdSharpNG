// pomiar_altgr_597.cs -- BEZPIECZNIK PRAWEGO ALTA + PRZYWROCONE SKOKI PO
// KOMENTARZACH (5.0.97, 13.09.2026).
//
// DWA POLECENIA MICHALA Z TEGO DNIA:
//
// 1. "Na przyszlosc Alt Ctrl zrob taki bezpiecznik, zeby lewy Alt Ctrl zawsze
//    byl mozliwy i nie kolidowal z polskimi literami z prawym Altem."
//    Czyli: skrot na lewym Control+Alt MA dzialac, a prawy Alt ma pisac litere.
//    Stary straznik bronil sie tepo - ZABRANIAL takich skrotow w ogole.
//
// 2. "Nie mowilem, zebys Alt Shift Page Up Page Down likwidowal, jezeli chodzi
//    o nawigacje po tych komentarzach."  W 5.0.96 zdjalem za duzo: mial zniknac
//    TYLKO F9 i Control+Alt+F9.
//
// CZEGO TA SONDA NIE MIERZY: nie udaje, ze sprawdza zachowanie klawiatury na
// zywo - to jest mierzone osobno, skryptem na dzialajacym programie.  Tutaj
// pilnujemy, zeby KOD I PLIKI TOWARZYSZACE mowily to samo, bo wlasnie
// rozjechanie sie tych trzech miejsc (kod, Hotkeys.ini, podrecznik) juz raz
// wypuscilo do niego wersje z klamliwym podsumowaniem skrotow.

using System;
using System.IO;
using System.Reflection;

class PomiarAltGr {
  static int ok = 0, zle = 0;
  static void Spr(string opis, bool warunek) {
    Console.WriteLine((warunek ? "  OK   " : "  ZLE  ") + opis);
    if (warunek) ok++; else zle++;
  }

  static string CzytajObok(string sExe, string sPlik) {
    string sDir = Path.GetDirectoryName(Path.GetFullPath(sExe));
    string sPath = Path.Combine(sDir, sPlik);
    return File.Exists(sPath) ? File.ReadAllText(sPath) : null;
  }

  static bool MaMetode(Assembly asm, string sTyp, string sMetoda) {
    foreach (Type t in asm.GetTypes()) {
      if (t.Name != sTyp) continue;
      foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                                             | BindingFlags.Static | BindingFlags.Instance)) {
        if (mi.Name == sMetoda) return true;
      }
    }
    return false;
  }

  static bool MaPole(Assembly asm, string sTyp, string sPole) {
    foreach (Type t in asm.GetTypes()) {
      if (t.Name != sTyp) continue;
      foreach (FieldInfo fi in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                                           | BindingFlags.Static | BindingFlags.Instance)) {
        if (fi.Name == sPole) return true;
      }
    }
    return false;
  }

  static void Main(string[] args) {
    string sExe = args.Length > 0 ? args[0] : "EdSharpNG.exe";
    if (!File.Exists(sExe)) { Console.WriteLine("BLAD: nie ma " + sExe); Environment.Exit(2); }
    Assembly asm = Assembly.LoadFrom(Path.GetFullPath(sExe));

    Console.WriteLine("1. bezpiecznik prawego Alta jest W PROGRAMIE");
    // GetKeyState musi byc zadeklarowany, bo bez niego nie da sie zapytac o
    // strone Alta - to jest jedyne zrodlo tej wiedzy w chwili nacisniecia.
    Spr("Win32.GetKeyState zadeklarowany", MaMetode(asm, "Win32", "GetKeyState"));
    Spr("stala VK_RMENU (prawy Alt) istnieje", MaPole(asm, "Win32", "VK_RMENU"));
    // Straznik przy PRZYPISYWANIU zostaje - ma tylko przestac zabraniac.
    Spr("Util.IsTypingChord nadal istnieje (nie wyrzucony)", MaMetode(asm, "Util", "IsTypingChord"));
    // UWAGA na nazwe klasy: okno glowne to MdiFrame, nie "Frame" (App.Frame to
    // tylko nazwa POLA).  Pytanie o "Frame" wypadalo na NIE i wygladalo jak brak
    // bezpiecznika w programie - zmierzone.
    Spr("ProcessCmdKey_Helper istnieje (tam siedzi bezpiecznik)",
        MaMetode(asm, "MdiFrame", "ProcessCmdKey_Helper"));

    Console.WriteLine();
    Console.WriteLine("2. skoki po komentarzach WROCILY na Alt+Shift+PageUp/PageDown");
    string sHot = CzytajObok(sExe, "Hotkeys.ini");
    Spr("Hotkeys.ini lezy obok programu", sHot != null);
    if (sHot != null) {
      Spr("Next Comment ma Alt+Shift+PageDown", sHot.Contains("Next Comment=Alt+Shift+PageDown,"));
      Spr("Prior Comment ma Alt+Shift+PageUp", sHot.Contains("Prior Comment=Alt+Shift+PageUp,"));
      // A rodzina F9 ma zostac zdjeta - to bylo jego pierwsze polecenie.
      Spr("Insert Comment BEZ klawisza", sHot.Contains("Insert Comment=,"));
      Spr("Comment List BEZ klawisza", sHot.Contains("Comment List=,"));
      Spr("nigdzie nie wrocil Alt+F9 przy komentarzach", !sHot.Contains("Insert Comment=Alt+F9"));
      Spr("nigdzie nie wrocil Control+Alt+F9", !sHot.Contains("Comment List=Control+Alt+F9"));
    }

    Console.WriteLine();
    Console.WriteLine("3. podsumowanie skrotow dla czlowieka mowi TO SAMO");
    string sTxt = CzytajObok(sExe, "EdSharp_Hotkeys.txt");
    Spr("EdSharp_Hotkeys.txt lezy obok programu", sTxt != null);
    if (sTxt != null) {
      Spr("Next Comment z klawiszem", sTxt.Contains("Next Comment, Alt+Shift+PageDown"));
      Spr("Prior Comment z klawiszem", sTxt.Contains("Prior Comment, Alt+Shift+PageUp"));
      Spr("Insert Comment opisany jako bez klawisza",
          sTxt.Contains("Insert Comment, (no key assigned)"));
      Spr("Comment List opisany jako bez klawisza",
          sTxt.Contains("Comment List, (no key assigned)"));
    }

    Console.WriteLine();
    Console.WriteLine("4. KONTROLA SONDY: pytania o rzeczy nieistniejace wypadaja na NIE");
    Spr("nie ma metody Win32.NieMaTakiej", !MaMetode(asm, "Win32", "NieMaTakiej"));
    Spr("nie ma pola Win32.VK_NIEISTNIEJE", !MaPole(asm, "Win32", "VK_NIEISTNIEJE"));

    Console.WriteLine();
    Console.WriteLine("RAZEM: " + ok + " OK, " + zle + " ZLE");
    Environment.Exit(zle == 0 ? 0 : 1);
  }
}
