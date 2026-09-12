// pomiar_ini_bom.cs -- czy BOM na poczatku Hotkeys.ini psuje czytanie
// sekcji przez GetPrivateProfileString (tak czyta Ini.ReadValue).
using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

class PomiarIniBom {
[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
static extern int GetPrivateProfileString(string sSection, string sKey, string sDefault, StringBuilder sbReturn, int iSize, string sFile);

static string Czytaj(string sFile, string sKey) {
StringBuilder sb = new StringBuilder(1024);
GetPrivateProfileString("Hotkeys", sKey, "", sb, 1024, sFile);
return sb.ToString();
}

static void Main(string[] aArgs) {
string sZrodlo = aArgs.Length > 0 ? aArgs[0] : @"C:\EdSharp\Hotkeys.ini";
byte[] aB = File.ReadAllBytes(sZrodlo);
bool bBom = (aB.Length > 2 && aB[0] == 0xEF && aB[1] == 0xBB && aB[2] == 0xBF);
Console.WriteLine("plik: {0}", sZrodlo);
Console.WriteLine("ma BOM: {0}", bBom);

string sZBom = Path.Combine(Path.GetTempPath(), "hk_z_bom.ini");
string sBezBom = Path.Combine(Path.GetTempPath(), "hk_bez_bom.ini");
File.WriteAllBytes(sZBom, aB);
File.WriteAllBytes(sBezBom, bBom ? SubArr(aB, 3) : aB);

string[] aKlucze = new string[] {"Launch EdSharp", "Spell Check", "Alternate Menu", "Save File As", "Word Spelling Menu"};
Console.WriteLine("\n{0,-22} {1,-32} {2}", "klucz", "Z BOM", "BEZ BOM");
foreach (string sK in aKlucze) {
string a = Czytaj(sZBom, sK);
string b = Czytaj(sBezBom, sK);
Console.WriteLine("{0,-22} [{1,-30}] [{2}]", sK, Skroc(a), Skroc(b));
}
}
static string Skroc(string s) { if (s.Length > 30) return s.Substring(0, 27) + "..."; return s; }
static byte[] SubArr(byte[] a, int i) { byte[] r = new byte[a.Length - i]; Array.Copy(a, i, r, 0, r.Length); return r; }
}
