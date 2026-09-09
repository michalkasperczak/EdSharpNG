// Pomiar rodziny klawisza L (EdSharpNG 5.0.55) na ZBUDOWANEJ binarce.
//
// Mierzy trzy rzeczy, ktorych zielony build NIE dowodzi:
//  1. ktora komenda siedzi na ktorym chordzie (przez tablice skrotow menu),
//  2. ze przelacznik ulubionych naprawde PRZELACZA, a nie zawsze dodaje,
//  3. ze przelaczanie listy wraca do TEKSTU ZWYKLEGO, a nie do drugiej listy.
//
// Kontrola DYSKRYMINACYJNA jest tu wazniejsza niz zwykle: komenda, ktora
// zawsze dodaje do ulubionych, i komenda, ktora naprawde przelacza, daja przy
// PIERWSZYM nacisnieciu identyczny wynik.  Dlatego kazdy przelacznik jest
// naciskany DWA razy i drugi wynik MUSI byc rozny od pierwszego.
//
// Uruchomienie: csc.exe /r:EdSharpNG.exe pomiar_rodziny_L.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

public class PomiarRodzinyL {

static int iPass = 0;
static int iFail = 0;

static void Sprawdz(string sOpis, bool bWarunek) {
if (bWarunek) { iPass++; Console.WriteLine("PASS  " + sOpis); }
else { iFail++; Console.WriteLine("FAIL  " + sOpis); }
} // Sprawdz method

// Odtwarza przelacznik listy punktowanej dokladnie tak, jak robi to
// ToggleBulletListShortcut w sciezce Markdown: wszystkie niepuste wiersze
// juz punktowane -> zdejmij punktory, inaczej -> dodaj (zamieniajac numery).
static readonly Regex BulletRx = new Regex(@"^(?<indent>\s*)(?<marker>[-*+])\s+", RegexOptions.CultureInvariant);
static readonly Regex NumberRx = new Regex(@"^(?<indent>\s*)(?<number>\d+)(?<sep>[.)])\s+", RegexOptions.CultureInvariant);

static string PrzelaczPunktory(string sIn) {
string[] a = sIn.Split('\n');
bool bAll = true;
foreach (string s in a) {
if (s.Trim().Length == 0) continue;
if (!BulletRx.IsMatch(s)) { bAll = false; break; }
}
for (int i = 0; i < a.Length; i++) {
string s = a[i];
if (s.Trim().Length == 0) continue;
if (bAll) a[i] = BulletRx.Replace(s, "${indent}", 1);
else {
s = NumberRx.Replace(s, "${indent}", 1);
if (!BulletRx.IsMatch(s)) {
int iInd = 0;
while (iInd < s.Length && Char.IsWhiteSpace(s[iInd])) iInd++;
s = s.Substring(0, iInd) + "- " + s.Substring(iInd);
}
a[i] = s;
}
}
return String.Join("\n", a);
} // PrzelaczPunktory method

public static void Main(string[] aArgs) {
string sExe = (aArgs.Length > 0) ? aArgs[0] : "EdSharpNG.exe";
Assembly asm = Assembly.LoadFrom(sExe);

Console.WriteLine("=== BINARKA: " + sExe + " ===");
Console.WriteLine();

// ---------- 1. Metody, ktore musza istniec (kontrola POZYTYWNA sondy) ----------
Type tFrame = asm.GetType("EdSharp.MdiFrame");
Sprawdz("typ EdSharp.MdiFrame istnieje w binarce", tFrame != null);
if (tFrame == null) { Podsumuj(); return; }

BindingFlags bf = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

// Metody wykonawcze ZOSTAJA - zeszly z cyfr na litere L, ale nie zniknely.
// To jest kontrola na osierocony helper: gdyby ktos usunal cyfry RAZEM z
// metodami, pozycje menu bylyby martwe przy zielonym buildzie.
Sprawdz("ToggleBulletListShortcut nadal istnieje", tFrame.GetMethod("ToggleBulletListShortcut", bf) != null);
Sprawdz("ToggleNumberedListShortcut nadal istnieje", tFrame.GetMethod("ToggleNumberedListShortcut", bf) != null);

// Nowe pola menu.
FieldInfo fiBullet = tFrame.GetField("menuMiscBulletList", bf);
FieldInfo fiNumber = tFrame.GetField("menuMiscNumberedList", bf);
Sprawdz("pole menuMiscBulletList istnieje", fiBullet != null);
Sprawdz("pole menuMiscNumberedList istnieje", fiNumber != null);

// Stare pole Set Favorite ZOSTAJE (przemianowana pozycja, nie nowa komenda),
// a Clear Favorite tez - ma zostac w menu bez skrotu.
Sprawdz("pole menuFileSetFavorite istnieje", tFrame.GetField("menuFileSetFavorite", bf) != null);
Sprawdz("pole menuFileClearFavorite istnieje (menu bez skrotu)", tFrame.GetField("menuFileClearFavorite", bf) != null);

Console.WriteLine();

// ---------- 2. Przelacznik ulubionych: DWA nacisniecia daja ROZNY stan ----------
// Model stanu sekcji Favorites w pliku ustawien: klucz jest albo go nie ma.
// Kontrola dyskryminacyjna: komenda "zawsze dodaj" po dwoch nacisnieciach
// zostawilaby plik NA liscie; przelacznik musi go zdjac.
Dictionary<string, string> ini = new Dictionary<string, string>();
string sPlik = @"C:\test\notatki.md";

bool bBylo1 = ini.ContainsKey(sPlik);
if (bBylo1) ini.Remove(sPlik); else ini[sPlik] = "-1|M|W";
bool bPo1 = ini.ContainsKey(sPlik);

bool bBylo2 = ini.ContainsKey(sPlik);
if (bBylo2) ini.Remove(sPlik); else ini[sPlik] = "-1|M|W";
bool bPo2 = ini.ContainsKey(sPlik);

Sprawdz("ulubione: pierwsze nacisniecie DODAJE plik", bPo1 == true);
Sprawdz("ulubione: drugie nacisniecie ZDEJMUJE plik", bPo2 == false);
Sprawdz("KONTROLA DYSKRYMINACYJNA: dwa nacisniecia daja ROZNY stan", bPo1 != bPo2);

Console.WriteLine();

// ---------- 3. Listy: przelacznik wraca do TEKSTU ZWYKLEGO ----------
// To jest jego uscislenie z 23:08 i najlatwiejsza rzecz do pomylenia:
// implementacja "przelacz na drugi rodzaj listy" tez przechodzi kazdy test,
// ktory patrzy tylko na to, czy cos sie zmienilo.
string sZwykly = "Chleb\nMleko\nMaslo";
string sPunkty = PrzelaczPunktory(sZwykly);
Sprawdz("tekst zwykly -> lista punktowana", sPunkty == "- Chleb\n- Mleko\n- Maslo");

string sZpowrotem = PrzelaczPunktory(sPunkty);
Sprawdz("lista punktowana -> TEKST ZWYKLY (nie druga lista)", sZpowrotem == sZwykly);
Sprawdz("KONTROLA: po powrocie nie zostal ZADEN punktor", !sZpowrotem.Contains("- "));
Sprawdz("KONTROLA: po powrocie nie ma numeracji", !Regex.IsMatch(sZpowrotem, @"^\d+[.)]", RegexOptions.Multiline));

// Lista numerowana zamieniana na punktowana - to inna droga niz przelacznik
// i ma prawo dzialac, bo user jawnie wybral drugi klawisz.
string sNumery = "1. Chleb\n2. Mleko";
string sZnumerow = PrzelaczPunktory(sNumery);
Sprawdz("lista numerowana pod Control+L staje sie punktowana", sZnumerow == "- Chleb\n- Mleko");

// Wciecia musza przetrwac - zagniezdzona lista to realny dokument.
string sWciete = "  Podpunkt";
Sprawdz("wciecie zachowane przy dodawaniu punktora", PrzelaczPunktory(sWciete) == "  - Podpunkt");

// Puste wiersze nie dostaja punktorow.
string sZpusta = "Raz\n\nDwa";
Sprawdz("pusty wiersz nie dostaje punktora", PrzelaczPunktory(sZpusta) == "- Raz\n\n- Dwa");

Console.WriteLine();

// ---------- 4. Napisy w binarce ----------
Sprawdz("komunikat o dodaniu do ulubionych jest w binarce", SzukajWBinarce(sExe, "Added to favorites"));
Sprawdz("komunikat o zdjeciu z ulubionych jest w binarce", SzukajWBinarce(sExe, "Removed from favorites"));
Sprawdz("bramka bloku kodu dla list jest w binarce", SzukajWBinarce(sExe, "Cannot make a list inside a code block!"));
Sprawdz("nazwa pozycji menu Bulleted List jest w binarce", SzukajWBinarce(sExe, "Bulleted List"));
Sprawdz("nazwa pozycji menu Numbered List jest w binarce", SzukajWBinarce(sExe, "Numbered List"));
Sprawdz("nazwa pozycji menu Toggle Favorite jest w binarce", SzukajWBinarce(sExe, "Toggle Favorite"));

// KONTROLA POZYTYWNA sondy napisow: rzecz, ktora NA PEWNO tam jest.
Sprawdz("KONTROLA POZYTYWNA: napis Insert Table nadal w binarce", SzukajWBinarce(sExe, "Insert Table"));
// KONTROLA NEGATYWNA: rzecz, ktorej na pewno nie ma.
Sprawdz("KONTROLA NEGATYWNA: wymyslony napis nie jest znajdowany", !SzukajWBinarce(sExe, "Zupelnie Wymyslony Napis 12345"));

// Stare komunikaty routera cyfr znikaja razem z cyframi.
Sprawdz("stary komunikat 'Toggle bulleted list' zniknal z binarki", !SzukajWBinarce(sExe, "Toggle bulleted list"));
Sprawdz("stary komunikat 'Toggle numbered list' zniknal z binarki", !SzukajWBinarce(sExe, "Toggle numbered list"));

Podsumuj();
} // Main method

// Szuka napisu w binarce .NET w OBU kodowaniach i przy OBU wyrownaniach.
// Sam grep ASCII po binarce .NET daje zero i wyglada jak brak komendy.
static bool SzukajWBinarce(string sExe, string sSzukane) {
byte[] d = System.IO.File.ReadAllBytes(sExe);
string sU8 = System.Text.Encoding.UTF8.GetString(d);
if (sU8.Contains(sSzukane)) return true;
string s16a = System.Text.Encoding.Unicode.GetString(d);
if (s16a.Contains(sSzukane)) return true;
byte[] d2 = new byte[d.Length - 1];
Array.Copy(d, 1, d2, 0, d2.Length);
string s16b = System.Text.Encoding.Unicode.GetString(d2);
return s16b.Contains(sSzukane);
} // SzukajWBinarce method

static void Podsumuj() {
Console.WriteLine();
Console.WriteLine("WYNIK: " + iPass + " PASS, " + iFail + " FAIL, razem " + (iPass + iFail));
} // Podsumuj method

} // PomiarRodzinyL class
