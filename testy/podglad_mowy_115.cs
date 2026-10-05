// ODCZYT PRAWDZIWEGO PODGLADU MOWY NVDA (Speech Viewer).
//
// DLACZEGO NIE speak_text I NIE SAMA ROLA.  speak_text dowodzi tylko tego, ze
// mostek umie mowic - nie tego, ze PRODUKT cos powiedzial.  Rola obiektu
// dowodzi, ze kontrolka istnieje, a nie ze czytnik ja przeczytal.  Dowodem jest
// TEKST, ktory NVDA naprawde wypowiedziala, a ten widac w okienku Podgladu mowy.
//
// Czytamy zawartosc pola tekstowego okna "Podglad mowy NVDA" przez WM_GETTEXT
// (wxPython TextCtrl to klasyczna kontrolka Edit, wiec to dziala bez UIA).
// Zwracamy OGON od zadanego znacznika, zeby nie mieszac starej mowy z nowa.
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

class PodgladMowy {
[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string c, string n);
[DllImport("user32.dll", CharSet = CharSet.Unicode)]
static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string name);
[DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, EnumProc cb, IntPtr p);
[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowTextLength(IntPtr h);
[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
static extern IntPtr SendMessageTimeout(IntPtr h, uint msg, IntPtr w, StringBuilder l, uint flags, uint ms, out IntPtr res);
delegate bool EnumProc(IntPtr h, IntPtr p);

const uint WM_GETTEXT = 0x000D;
const uint WM_GETTEXTLENGTH = 0x000E;

static List<IntPtr> dzieci = new List<IntPtr>();
static bool Zbierz(IntPtr h, IntPtr p) { dzieci.Add(h); return true; }

static string TekstOkna(IntPtr h) {
IntPtr res;
SendMessageTimeout(h, WM_GETTEXTLENGTH, IntPtr.Zero, null, 2, 2000, out res);
int len = res.ToInt32();
if (len <= 0) return "";
StringBuilder sb = new StringBuilder(len + 2);
SendMessageTimeout(h, WM_GETTEXT, new IntPtr(len + 1), sb, 2, 5000, out res);
return sb.ToString();
}

static int Main(string[] args) {
// Tytul okna moze byc polski albo angielski - probujemy oba, a jesli
// zadnego nie ma, melduje to JAWNIE jako brak warunku pomiaru.
IntPtr okno = IntPtr.Zero;
foreach (string t in new string[] {"Podgląd mowy NVDA", "NVDA Speech Viewer", "Podglad mowy NVDA"}) {
okno = FindWindow(null, t);
if (okno != IntPtr.Zero) { Console.Error.WriteLine("OKNO: " + t); break; }
}
if (okno == IntPtr.Zero) { Console.WriteLine("BRAK_OKNA_PODGLADU_MOWY"); return 3; }

EnumChildWindows(okno, new EnumProc(Zbierz), IntPtr.Zero);
string sNajdluzszy = "";
foreach (IntPtr h in dzieci) {
StringBuilder k = new StringBuilder(128);
GetClassName(h, k, 128);
string sT = TekstOkna(h);
if (sT.Length > sNajdluzszy.Length) sNajdluzszy = sT;
}
if (sNajdluzszy.Length == 0) { Console.WriteLine("PUSTY_PODGLAD"); return 4; }

// OGON OD ZNACZNIKA.  Bez tego raport pokazywalby mowe z calej sesji i nie
// dowodzilby, ze to TEN produkt powiedzial TO teraz.
string sZnacznik = args.Length > 0 ? args[0] : "";
string sWynik = sNajdluzszy;
if (sZnacznik.Length > 0) {
int i = sNajdluzszy.LastIndexOf(sZnacznik, StringComparison.OrdinalIgnoreCase);
if (i >= 0) sWynik = sNajdluzszy.Substring(i + sZnacznik.Length);
else Console.Error.WriteLine("UWAGA: znacznika nie ma w podgladzie - pokazuje ogon");
}
if (sWynik.Length > 4000) sWynik = sWynik.Substring(sWynik.Length - 4000);
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine(sWynik.Trim());
return 0;
}
}
