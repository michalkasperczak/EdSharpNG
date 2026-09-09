// POMIAR KOLIZJI CONTROL+ALT Z POLSKIMI LITERAMI - kierunek odwrotny.
//
// PYTANIE KASPERCZAKA (30.08.2026 11:45): "Alt+Ctrl to tez to samo co prawy
// Alt, a prawy Alt to polska literka".  Czyli: czy nasze skroty Control+Alt
// zabieraja mu pisanie polskich znakow.
//
// DLACZEGO NIE ToUnicodeEx: pierwsza wersja tej sondy pytala "co daje ten
// klawisz przy AltGr" i zwracala wyniki WEWNETRZNIE SPRZECZNE (AltGr+L jako
// "B", przy poprawnym AltGr+O jako o z kreska).  Zamiast zgadywac, ktore z
// tych zwrotow sa prawdziwe, pytam UKLAD w druga strone: VkKeyScanEx dla
// ZNAKU zwraca klawisz ORAZ potrzebne modyfikatory.  To jest dokladnie ta
// informacja, ktorej potrzebuje - i nie wymaga interpretacji bufora.
//
// VkKeyScanEx zwraca 16 bitow: dolny bajt to kod klawisza, gorny to
// modyfikatory (bit 0 Shift, bit 1 Control, bit 2 Alt).  Control+Alt razem,
// czyli maska 6, to wlasnie prawy Alt.
//
// KONTROLE (bez nich zielony wynik nic nie znaczy):
//  - POZYTYWNA: litera 'a' MUSI wyjsc bez modyfikatorow, a 'A' z Shiftem.
//  - NEGATYWNA: znak, ktorego uklad NIE MA, musi zwrocic -1.
using System;
using System.Runtime.InteropServices;

class KolizjeAltGr {

[DllImport("user32.dll")]
static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

[DllImport("user32.dll", CharSet = CharSet.Unicode)]
static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

static string NazwaKlawisza(int vk) {
if (vk >= 0x41 && vk <= 0x5A) return ((char) vk).ToString();
if (vk >= 0x30 && vk <= 0x39) return ((char) vk).ToString();
switch (vk) {
case 0xBD: return "myslnik";
case 0xBB: return "rowna sie";
case 0xBA: return "srednik";
case 0xBF: return "ukosnik";
case 0xDB: return "lewy nawias kwadratowy";
case 0xDD: return "prawy nawias kwadratowy";
case 0xDC: return "kreska pionowa";
case 0xDE: return "apostrof";
case 0xBC: return "przecinek";
case 0xBE: return "kropka";
case 0xC0: return "tylda";
default: return "vk 0x" + vk.ToString("X2");
}
}

static string OpisModyfikatorow(int mods) {
if (mods == 0) return "bez modyfikatorow";
string s = "";
if ((mods & 1) != 0) s += "Shift+";
if ((mods & 2) != 0) s += "Control+";
if ((mods & 4) != 0) s += "Alt+";
return s.TrimEnd('+');
}

static int iPass = 0, iFail = 0;
static void ok(string s) { iPass++; Console.WriteLine("PASS  " + s); }
static void bad(string s) { iFail++; Console.WriteLine("FAIL  " + s); }

static void Main(string[] args) {
string sKlid = (args.Length > 0) ? args[0] : "00000415";
IntPtr hkl = LoadKeyboardLayout(sKlid, 0);
Console.WriteLine("=== uklad KLID " + sKlid + ", HKL 0x" + hkl.ToInt64().ToString("X8") + " ===");
Console.WriteLine();

// --- KONTROLE POPRAWNOSCI SONDY ---
Console.WriteLine("--- KONTROLE SONDY (bez nich wynik nizej nic nie znaczy) ---");
short sA = VkKeyScanEx('a', hkl);
if (sA != -1 && (sA & 0xFF) == 0x41 && ((sA >> 8) & 7) == 0) ok("litera 'a' to klawisz A bez modyfikatorow");
else bad("litera 'a' zwrocila " + sA + " - sonda nie czyta ukladu");
short sAA = VkKeyScanEx('A', hkl);
if (sAA != -1 && (sAA & 0xFF) == 0x41 && ((sAA >> 8) & 7) == 1) ok("litera 'A' to klawisz A z Shiftem");
else bad("litera 'A' zwrocila " + sAA);
// Znak, ktorego na polskiej klawiaturze nie ma - kontrola negatywna.
short sBrak = VkKeyScanEx('\u4E2D', hkl);
if (sBrak == -1) ok("znak chinski nie ma klawisza (zwrot -1) - sonda umie odpowiedziec NIE");
else bad("znak chinski zwrocil " + sBrak + " - sonda zwraca cos zawsze, czyli klamie");
Console.WriteLine();

// --- POLSKIE ZNAKI DIAKRYTYCZNE ---
string sPolskie = "\u0105\u0107\u0119\u0142\u0144\u00F3\u015B\u017A\u017C";
string[] nazwy = new string[] {"a z ogonkiem", "c z kreska", "e z ogonkiem",
"l z kreska", "n z kreska", "o z kreska", "s z kreska", "z z kreska", "z z kropka"};

System.Collections.Generic.List<int> zajete = new System.Collections.Generic.List<int>();
Console.WriteLine("--- KTORYM KLAWISZEM POWSTAJE POLSKA LITERA ---");
for (int i = 0; i < sPolskie.Length; i++) {
short r = VkKeyScanEx(sPolskie[i], hkl);
if (r == -1) {
Console.WriteLine("  " + nazwy[i] + ": BRAK w tym ukladzie");
continue;
}
int vk = r & 0xFF;
int mods = (r >> 8) & 7;
bool bAltGr = (mods & 6) == 6;
if (bAltGr) zajete.Add(vk);
Console.WriteLine("  " + nazwy[i] + ": " + OpisModyfikatorow(mods) + NazwaKlawisza(vk)
+ (bAltGr ? "   <- CHORD CONTROL+ALT ZAJETY PRZEZ PISANIE" : ""));
}
Console.WriteLine();

// --- NASZE CHORDY CONTROL+ALT ---
Console.WriteLine("--- CZY NASZE SKROTY CONTROL+ALT ZABIERAJA PISANIE ---");
int[] nasze = new int[] {0x4B, 0x78, 0x26, 0x28, 0x30};
string[] naszeOpis = new string[] {"Control+Alt+K (skok przypisu)",
"Control+Alt+F9 (lista komentarzy)", "Control+Alt+strzalka w gore (przesuniecie sekcji)",
"Control+Alt+strzalka w dol (przesuniecie sekcji)", "Control+Alt+0 (nieuzywany od 5.0.45)"};
for (int i = 0; i < nasze.Length; i++) {
bool bKoliduje = zajete.Contains(nasze[i]);
if (!bKoliduje) ok(naszeOpis[i] + " nie koliduje z zadna polska litera");
else bad(naszeOpis[i] + " ZABIERA polska litere");
}
Console.WriteLine();

// --- KTORE LITERY ALFABETU SA WOLNE POD CONTROL+ALT ---
Console.WriteLine("--- LITERY WOLNE I ZAJETE POD CONTROL+ALT (do przyszlych skrotow) ---");
string sZajete = "", sWolne = "";
for (int vk = 0x41; vk <= 0x5A; vk++) {
if (zajete.Contains(vk)) sZajete += (char) vk;
else sWolne += (char) vk;
}
Console.WriteLine("  ZAJETE przez pisanie: " + sZajete);
Console.WriteLine("  WOLNE dla skrotow:    " + sWolne);
Console.WriteLine();
Console.WriteLine("WYNIK: " + iPass + " PASS, " + iFail + " FAIL");
}

} // KolizjeAltGr class
