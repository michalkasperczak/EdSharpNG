// pomiar_prawy_alt.cs -- CZY DA SIE ROZPOZNAC PRAWY ALT OD LEWEGO?
//
// PO CO: Michal chce bezpiecznika, ktory nie odbiera calego Control+Alt, a
// tylko nie wchodzi w droge polskim literom z PRAWEGO Alta: "zrob taki
// bezpiecznik, zeby lewy Alt Ctrl zawsze byl mozliwy i nie kolidowal z polskimi
// literami z prawym Altem".
//
// Dzisiejszy straznik w programie jest tepy: jesli chord Control+Alt+litera
// wpisuje znak na ukladzie, ZABRANIA przypisania go w ogole - a wiec zabiera
// tez lewy Control+Alt, ktory z pisaniem nie koliduje.
//
// ZANIM to przepisze, trzeba ZMIERZYC, czy Windows w ogole odroznia strone:
//   1. czy GetKeyState(VK_RMENU) mowi prawde, gdy prawy Alt jest wcisniety,
//   2. czy przy prawym Alcie widac tez Control (bo AltGr udaje Control+Alt),
//   3. czy przy LEWYM Alt+Control prawy Alt zostaje niewcisniety.
//
// Bez punktu 3 caly pomysl nie ma sensu: nie dalo by sie odroznic pisania od
// skrotu.

using System;
using System.Runtime.InteropServices;
using System.Threading;

class PomiarPrawyAlt {
  [DllImport("user32.dll")] static extern short GetKeyState(int nVirtKey);
  [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
  [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

  const int VK_LCONTROL = 0xA2, VK_RCONTROL = 0xA3;
  const int VK_LMENU = 0xA4, VK_RMENU = 0xA5;
  const int VK_CONTROL = 0x11, VK_MENU = 0x12;
  const uint KEYDOWN = 0, KEYUP = 2, EXTENDED = 1;

  static int ok = 0, zle = 0;
  static void Spr(string opis, bool warunek) {
    Console.WriteLine((warunek ? "  OK   " : "  ZLE  ") + opis);
    if (warunek) ok++; else zle++;
  }
  static bool Wcisniety(int vk) { return (GetKeyState(vk) & 0x8000) != 0; }

  static void Main() {
    Console.WriteLine("1. PRAWY ALT wcisniety (tak wchodza polskie litery)");
    keybd_event((byte) VK_RMENU, 0, EXTENDED | KEYDOWN, UIntPtr.Zero);
    Thread.Sleep(120);
    bool rmenu = Wcisniety(VK_RMENU);
    bool lmenu = Wcisniety(VK_LMENU);
    bool menu  = Wcisniety(VK_MENU);
    keybd_event((byte) VK_RMENU, 0, EXTENDED | KEYUP, UIntPtr.Zero);
    Thread.Sleep(120);

    Spr("prawy Alt widac jako VK_RMENU", rmenu);
    Spr("lewy Alt przy tym NIE jest wcisniety", !lmenu);
    Spr("ogolny VK_MENU tez sie zglasza (wiec sam VK_MENU nie wystarcza)", menu);

    Console.WriteLine();
    Console.WriteLine("2. LEWY Control + LEWY Alt (chord, ktory ma dzialac)");
    keybd_event((byte) VK_LCONTROL, 0, KEYDOWN, UIntPtr.Zero);
    keybd_event((byte) VK_LMENU, 0, KEYDOWN, UIntPtr.Zero);
    Thread.Sleep(120);
    bool rmenu2 = Wcisniety(VK_RMENU);
    bool lmenu2 = Wcisniety(VK_LMENU);
    bool ctrl2  = Wcisniety(VK_CONTROL);
    keybd_event((byte) VK_LMENU, 0, KEYUP, UIntPtr.Zero);
    keybd_event((byte) VK_LCONTROL, 0, KEYUP, UIntPtr.Zero);
    Thread.Sleep(120);

    Spr("lewy Alt widac", lmenu2);
    Spr("Control widac", ctrl2);
    Spr("PRAWY Alt NIE jest wcisniety - to rozstrzyga cala sprawe", !rmenu2);

    Console.WriteLine();
    Console.WriteLine("3. po puszczeniu klawiszy stan wraca do zera");
    Spr("prawy Alt zwolniony", !Wcisniety(VK_RMENU));
    Spr("lewy Alt zwolniony", !Wcisniety(VK_LMENU));
    Spr("Control zwolniony", !Wcisniety(VK_CONTROL));

    Console.WriteLine();
    Console.WriteLine("RAZEM: " + ok + " OK, " + zle + " ZLE");
    Console.WriteLine(zle == 0
      ? "WNIOSEK: strone Alta DA SIE rozpoznac - bezpiecznik moze przepuszczac lewy Control+Alt."
      : "WNIOSEK: NIE DA SIE tego oprzec na GetKeyState - trzeba innej drogi.");
    Environment.Exit(zle == 0 ? 0 : 1);
  }
}
