// Pomiar ROUND-TRIP magazynu zakladek z nazwa na PRAWDZIWYM pliku ustawien,
// przez metody ZBUDOWANEJ binarki EdSharpNG.exe (refleksja).
//
// PO CO OSOBNY POMIAR: pomiar_zakladek_z_nazwa.cs dowodzi, ze komendy i metody
// SA w programie.  Nie dowodzi, ze zapis i odczyt naprawde sie zgadzaja.  Ten
// pomiar wola WriteNamedBookmark / ReadNamedBookmarks na pliku .ini pisanym
// tym samym WritePrivateProfileString, ktorego uzywa program.
//
// SZCZEGOLNIE WAZNE TU: plik ustawien pisany przez API Win32 jest w kodowaniu
// ANSI, a wyliczanie kluczy sekcji idzie WLASNYM parserem tekstowym - to byla
// przyczyna znikajacych ulubionych z polskimi znakami w nazwie (17.08.2026).
// Dlatego jeden przypadek ma POLSKIE ZNAKI w nazwie pliku i w nazwie zakladki.
//
// Uzycie: pomiar_magazynu_zakladek.exe <sciezka_do_EdSharpNG.exe>
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

public class PomiarMagazynuZakladek {

static int iPass = 0;
static int iFail = 0;
static Type tFrame;
static Type tApp;

static void Ok(bool b, string sName, string sGot) {
if (b) {iPass++; Console.WriteLine("PASS  " + sName);}
else {iFail++; Console.WriteLine("FAIL  " + sName + "  -> " + sGot);}
}

static MethodInfo M(Type t, string sName) {
return t.GetMethod(sName,
BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
}

static void SetIniFile(string sPath) {
// App.IniFile decyduje, gdzie ida odczyty i zapisy.  Podstawiamy plik w temp,
// zeby pomiar nie ruszal ustawien uzytkownika.
FieldInfo fi = tApp.GetField("IniFile",
BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
if (fi != null) {fi.SetValue(null, sPath); return;}
PropertyInfo pi = tApp.GetProperty("IniFile",
BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
if (pi != null && pi.CanWrite) {pi.SetValue(null, sPath, null); return;}
throw new Exception("nie moge podstawic App.IniFile");
}

static void Write(string sFile, int iRow, string sName) {
M(tFrame, "WriteNamedBookmark").Invoke(null, new object[] {sFile, iRow, sName});
}

static string ReadOne(string sFile, int iRow) {
return (string) M(tFrame, "ReadNamedBookmark").Invoke(null, new object[] {sFile, iRow});
}

static void Delete(string sFile, int iRow) {
M(tFrame, "DeleteNamedBookmark").Invoke(null, new object[] {sFile, iRow});
}

static void ReadAll(string sFile, out List<int> rows, out List<string> names) {
rows = new List<int>();
names = new List<string>();
M(tFrame, "ReadNamedBookmarks").Invoke(null, new object[] {sFile, rows, names});
}

static string Join(List<int> rows) {
StringBuilder sb = new StringBuilder();
for (int i = 0; i < rows.Count; i++) {
if (i > 0) sb.Append(",");
sb.Append(rows[i]);
}
return sb.ToString();
}

static string JoinS(List<string> names) {
return String.Join(",", names.ToArray());
}

public static int Main(string[] args) {
string sExe = (args.Length > 0) ? args[0] : @"C:\tmp\pomiar_zakladki\nowa.exe";
if (!File.Exists(sExe)) {Console.WriteLine("BRAK " + sExe); return 3;}
Console.WriteLine("Binarka: " + sExe + "  (" + File.GetLastWriteTime(sExe).ToString("yyyy-MM-dd HH:mm:ss") + ")");

Assembly asm = Assembly.LoadFile(Path.GetFullPath(sExe));
tFrame = null;
tApp = null;
foreach (Type t in asm.GetTypes()) {
if (t.Name == "App") tApp = t;
if (M(t, "ReadNamedBookmarks") != null) tFrame = t;
}
if (tFrame == null) {Console.WriteLine("BRAK metody ReadNamedBookmarks w binarce"); return 3;}
if (tApp == null) {Console.WriteLine("BRAK typu App w binarce"); return 3;}
Console.WriteLine("Typ: " + tFrame.FullName);

string sIni = Path.Combine(Path.GetTempPath(), "pomiar_zakladek_" + Guid.NewGuid().ToString("N") + ".ini");
SetIniFile(sIni);
Console.WriteLine("Plik ustawien pomiaru: " + sIni);
Console.WriteLine();

string sPlik = @"C:\pliki\notatki.md";
string sInny = @"C:\pliki\inny.md";
string sPolski = @"C:\pliki\ogłoszenia-parafialne.md";

List<int> rows;
List<string> names;

// ---------- 1. PUSTY MAGAZYN ----------
Console.WriteLine("-- 1. PUSTY MAGAZYN --");
ReadAll(sPlik, out rows, out names);
Ok(rows.Count == 0, "na czystym pliku ustawien lista jest pusta", "" + rows.Count);
Ok(ReadOne(sPlik, 5).Length == 0, "odczyt pojedynczej zakladki daje pusty napis", ReadOne(sPlik, 5));

// ---------- 2. ZAPIS I ODCZYT ----------
Console.WriteLine();
Console.WriteLine("-- 2. ZAPIS I ODCZYT (round-trip przez prawdziwy plik) --");
Write(sPlik, 41, "Rozdzial o zaleznosciach");
Ok(ReadOne(sPlik, 41) == "Rozdzial o zaleznosciach",
"zapisana nazwa wraca w tej samej postaci", ReadOne(sPlik, 41));
Ok(File.Exists(sIni), "plik ustawien realnie powstal na dysku", "nie ma pliku");
string sRaw = File.ReadAllText(sIni, Encoding.Default);
Ok(sRaw.Contains("[NamedBookmarks]"), "sekcja NamedBookmarks jest w pliku", "brak sekcji");
Ok(!sRaw.Contains("[Favorites]"),
"KONTROLA: zapis zakladki z nazwa NIE dotknal sekcji Favorites (osobny magazyn)",
"dotknal");

// ---------- 3. SORTOWANIE PO NUMERZE WIERSZA ----------
Console.WriteLine();
Console.WriteLine("-- 3. LISTA SORTOWANA PO WIERSZU, NIE PO KOLEJNOSCI ZAPISU --");
Write(sPlik, 7, "Wstep");
Write(sPlik, 120, "Zakonczenie");
ReadAll(sPlik, out rows, out names);
Ok(rows.Count == 3, "widac trzy zakladki", "" + rows.Count);
Ok(Join(rows) == "7,41,120", "wiersze rosnaco, mimo zapisu 41,7,120", Join(rows));
Ok(JoinS(names) == "Wstep,Rozdzial o zaleznosciach,Zakonczenie",
"nazwy w tej samej kolejnosci co wiersze", JoinS(names));

// ---------- 4. ODSIEW ZAKLADEK INNEGO PLIKU ----------
Console.WriteLine();
Console.WriteLine("-- 4. ZAKLADKI INNEGO PLIKU NIE WCHODZA NA LISTE --");
Write(sInny, 3, "Obca zakladka");
ReadAll(sPlik, out rows, out names);
Ok(rows.Count == 3, "lista pierwszego pliku nadal ma trzy pozycje", "" + rows.Count);
Ok(!JoinS(names).Contains("Obca"), "nazwa z obcego pliku nie wyciekla", JoinS(names));
ReadAll(sInny, out rows, out names);
Ok(rows.Count == 1 && names[0] == "Obca zakladka",
"KONTROLA: drugi plik widzi SWOJA zakladke (odsiew nie odrzuca wszystkiego)",
Join(rows) + " " + JoinS(names));

// ---------- 5. ZMIANA NAZWY W TYM SAMYM WIERSZU ----------
Console.WriteLine();
Console.WriteLine("-- 5. ZMIANA NAZWY NIE MNOZY POZYCJI --");
Write(sPlik, 41, "Rozdzial o zaleznosciach (poprawiony)");
ReadAll(sPlik, out rows, out names);
Ok(rows.Count == 3, "po zmianie nazwy nadal trzy pozycje", "" + rows.Count);
Ok(ReadOne(sPlik, 41) == "Rozdzial o zaleznosciach (poprawiony)",
"czyta sie NOWA nazwa", ReadOne(sPlik, 41));

// ---------- 6. USUNIECIE ----------
Console.WriteLine();
Console.WriteLine("-- 6. USUNIECIE ZDEJMUJE DOKLADNIE JEDNA POZYCJE --");
Delete(sPlik, 41);
ReadAll(sPlik, out rows, out names);
Ok(rows.Count == 2, "zostaly dwie pozycje", "" + rows.Count);
Ok(Join(rows) == "7,120", "zostaly wlasnie te dwie", Join(rows));
Ok(ReadOne(sPlik, 41).Length == 0, "usunieta zakladka nie czyta sie pojedynczo", ReadOne(sPlik, 41));
ReadAll(sInny, out rows, out names);
Ok(rows.Count == 1, "KONTROLA: usuniecie nie ruszylo drugiego pliku", "" + rows.Count);

// ---------- 7. POLSKIE ZNAKI (plik ustawien jest ANSI) ----------
Console.WriteLine();
Console.WriteLine("-- 7. POLSKIE ZNAKI W NAZWIE PLIKU I ZAKLADKI --");
Write(sPolski, 12, "Ogłoszenia z zeszłego tygodnia");
Ok(ReadOne(sPolski, 12) == "Ogłoszenia z zeszłego tygodnia",
"nazwa z polskimi znakami wraca bez zmian", ReadOne(sPolski, 12));
ReadAll(sPolski, out rows, out names);
Ok(rows.Count == 1 && names.Count == 1,
"zakladka pliku o polskiej nazwie JEST na liscie (to lapie blad ANSI kontra UTF-8)",
"" + rows.Count);
if (names.Count == 1) {
Ok(names[0] == "Ogłoszenia z zeszłego tygodnia", "i ma poprawna nazwe", names[0]);
}
ReadAll(sPlik, out rows, out names);
Ok(rows.Count == 2, "KONTROLA: plik o nazwie ASCII nadal ma swoje dwie", "" + rows.Count);

// ---------- 8. DUZO ZAKLADEK (klucz na zakladke, nie jedna wartosc) ----------
Console.WriteLine();
Console.WriteLine("-- 8. TRZYDZIESCI ZAKLADEK: LISTA SIE NIE UCINA --");
string sDuzo = @"C:\pliki\duzy.md";
for (int i = 1; i <= 30; i++) Write(sDuzo, i * 10, "Zakladka numer " + i);
ReadAll(sDuzo, out rows, out names);
Ok(rows.Count == 30,
"widac wszystkie 30 (jedna dluga wartosc INI ucielaby sie na 260 znakach)",
"" + rows.Count);
Ok(rows.Count == 30 && rows[0] == 10 && rows[29] == 300,
"pierwsza i ostatnia na swoim miejscu", (rows.Count > 0 ? rows[0] + ".." + rows[rows.Count-1] : "brak"));

// ---------- 9. KONTROLA GLUCHEJ SONDY ----------
Console.WriteLine();
Console.WriteLine("-- 9. KONTROLA: SONDA NIE JEST GLUCHA --");
ReadAll(@"C:\pliki\nigdy-nie-zapisany.md", out rows, out names);
Ok(rows.Count == 0, "plik bez zakladek daje pusta liste (a nie cudze wpisy)", "" + rows.Count);
Write(sDuzo, 999, "   ");
Ok(ReadOne(sDuzo, 999).Length == 0,
"nazwa z samych odstepow czyta sie jako pusta, wiec nie tworzy pozycji-widma",
"[" + ReadOne(sDuzo, 999) + "]");
ReadAll(sDuzo, out rows, out names);
Ok(rows.Count == 30, "i nie wchodzi na liste", "" + rows.Count);

try {File.Delete(sIni);} catch {}
Console.WriteLine();
Console.WriteLine("== " + iPass + " PASS, " + iFail + " FAIL ==");
return (iFail == 0) ? 0 : 1;
}

} // PomiarMagazynuZakladek class
