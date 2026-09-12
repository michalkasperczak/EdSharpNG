// POMIAR: SPRAWDZANIE PISOWNI BEZ WORDA (zadanie 4, 12.09.2026).
//
// PO CO. Dzis SpellCheck (F7) uruchamia Microsoft Word przez COM: tworzy nowy
// dokument, wkleja tekst, wola CheckSpelling, czyta wynik i zamyka dokument.
// Michal: "EdSharp kiedys uruchamial Word i sprawdzal, teraz chyba bylo tak,
// ze jakos lzej bez Worda". Lzej znaczy: bez Worda w ogole.
//
// CO SPRAWDZAM. Windows ma WLASNE sprawdzanie pisowni - Spell Checking API,
// obecne od Windows 8. Uzywa go Edge, Poczta, pole wyszukiwania. Jest czescia
// systemu, wiec nie dokladamy zadnej obcej biblioteki do paczki.
// Pytania, na ktore ten pomiar musi odpowiedziec, zanim cokolwiek przepisze:
//   1. czy API w ogole odpowiada;
//   2. czy zna POLSKI (bez tego zadanie nie ma sensu - chodzi o polski slownik);
//   3. czy naprawde wskazuje polskie bledy i czy podaje sensowne podpowiedzi,
//      w tym dla slow z ogonkami;
//   4. czy NIE zglasza jako bledu poprawnych polskich slow (falszywy alarm jest
//      gorszy od przeoczenia: kaze poprawiac to, co bylo dobre);
//   5. ile to trwa - bo caly sens zmiany to "lzej".
//
// CZEGO NIE SPRAWDZA: jak to zabrzmi w czytniku ekranu. To slyszy tylko Michal.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class PomiarPisownia {

// ---- Spell Checking API: tylko to, co naprawde wolam ----------------------

[ComImport, Guid("00000101-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IEnumString {
// Napisy odbieram jako WSKAZNIKI, nie jako string[].  Automatyczne
// przepisywanie na string[] zwracalo puste wartosci (zmierzone: lista miala
// jeden element, ktory byl null) - bo napisy sa przydzielone po stronie COM i
// trzeba je przeczytac i zwolnic samemu.
[PreserveSig] int Next(int celt, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] rgelt, out int pceltFetched);
void Skip(int celt);
void Reset();
void Clone(out IEnumString ppenum);
}

[ComImport, Guid("B7C82D61-FBE8-4B47-9B27-6C0D2E0DE0A3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface ISpellingError {
uint StartIndex { get; }
uint Length { get; }
uint CorrectiveAction { get; }
string Replacement { [return: MarshalAs(UnmanagedType.LPWStr)] get; }
}

[ComImport, Guid("803E3BD4-2828-4410-8290-418D1D73C762"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IEnumSpellingError {
[PreserveSig] int Next(out ISpellingError value);
}

// Identyfikator z naglowka spellcheck.idl z Windows SDK (koncowka F197E412770B).
// Poprzednia proba miala tu zmyslona koncowke i system slusznie odmowil
// ("Taki interfejs nie jest obslugiwany") - dlatego identyfikatory bierze sie
// ze zrodla, a nie z pamieci.
[ComImport, Guid("B6FD0B71-E2BC-4653-8D05-F197E412770B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface ISpellChecker {
string LanguageTag { [return: MarshalAs(UnmanagedType.LPWStr)] get; }
IEnumSpellingError Check([MarshalAs(UnmanagedType.LPWStr)] string text);
IEnumString Suggest([MarshalAs(UnmanagedType.LPWStr)] string word);
void Add([MarshalAs(UnmanagedType.LPWStr)] string word);
void Ignore([MarshalAs(UnmanagedType.LPWStr)] string word);
void AutoCorrect([MarshalAs(UnmanagedType.LPWStr)] string from, [MarshalAs(UnmanagedType.LPWStr)] string to);
byte GetOptionValue([MarshalAs(UnmanagedType.LPWStr)] string optionId);
IEnumString OptionIds { get; }
string Id { [return: MarshalAs(UnmanagedType.LPWStr)] get; }
string LocalizedName { [return: MarshalAs(UnmanagedType.LPWStr)] get; }
// dalej sa zdarzenia i ComprehensiveCheck - nie wolam, wiec nie deklaruje
}

[ComImport, Guid("8E018A9D-2415-4677-BF08-794EA61F94BB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface ISpellCheckerFactory {
IEnumString SupportedLanguages { get; }
[PreserveSig] int IsSupported([MarshalAs(UnmanagedType.LPWStr)] string languageTag, out bool value);
ISpellChecker CreateSpellChecker([MarshalAs(UnmanagedType.LPWStr)] string languageTag);
}

[ComImport, Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC")]
class SpellCheckerFactory {}

static List<string> ZbierzNapisy(IEnumString en) {
List<string> l = new List<string>();
if (en == null) return l;
IntPtr[] a = new IntPtr[1];
int iGot;
while (en.Next(1, a, out iGot) == 0 && iGot == 1) {
if (a[0] == IntPtr.Zero) continue;
l.Add(Marshal.PtrToStringUni(a[0]));
Marshal.FreeCoTaskMem(a[0]);   // napis jest nasz, trzeba go zwolnic
a[0] = IntPtr.Zero;
}
return l;
}

public static void Main() {
int iBad = 0;

Console.WriteLine("=== 1. Czy systemowe sprawdzanie pisowni odpowiada ===");
Console.WriteLine();
ISpellCheckerFactory factory = null;
try {
factory = (ISpellCheckerFactory) new SpellCheckerFactory();
Console.WriteLine("Spell Checking API: ODPOWIADA (bez Worda, bez obcych bibliotek)");
}
catch (Exception ex) {
Console.WriteLine("NIE DZIALA: {0}", ex.Message);
Console.WriteLine("=> zadanie 4 tą drogą nie przejdzie; trzeba szukac dalej.");
return;
}
Console.WriteLine();

Console.WriteLine("=== 2. Czy zna polski ===");
Console.WriteLine();
List<string> lJezyki = ZbierzNapisy(factory.SupportedLanguages);
Console.WriteLine("Jezykow razem: {0}", lJezyki.Count);
foreach (string s in lJezyki) Console.WriteLine("  obslugiwany: {0}", s);
bool bPolski = false;
foreach (string s in lJezyki) if (s.StartsWith("pl", StringComparison.OrdinalIgnoreCase)) { bPolski = true; Console.WriteLine("  polski: {0}", s); }
// Pytam takze wprost - lista i pytanie o konkretny jezyk to dwie rozne drogi
// w tym API i moga sie roznic.
bool bWprost = false;
try { factory.IsSupported("pl-PL", out bWprost); } catch {}
Console.WriteLine("  pytanie wprost o pl-PL: {0}", bWprost ? "obslugiwany" : "nieobslugiwany");
if (bWprost) bPolski = true;
if (!bPolski) {
Console.WriteLine("  POLSKIEGO NIE MA NA TEJ MASZYNIE.");
Console.WriteLine("  UWAGA: to maszyna Hermesa (WSL/Windows serwerowy), nie komputer Michala.");
Console.WriteLine("  Slownik polski jest czescia pakietu jezykowego Windows - na komputerze");
Console.WriteLine("  z polskim Windowsem powinien byc. Pomiar trzeba powtorzyc TAM.");
iBad++;
}
Console.WriteLine();
if (!bPolski) { Console.WriteLine("Bledow: {0}", iBad); return; }

ISpellChecker sc = factory.CreateSpellChecker("pl-PL");
Console.WriteLine("Utworzony sprawdzacz: {0} ({1})", sc.LanguageTag, sc.LocalizedName);
Console.WriteLine();

Console.WriteLine("=== 3. Czy wskazuje polskie bledy i podpowiada ===");
Console.WriteLine();
// Slowa z bledem, W TYM z ogonkami - ogonki sa tu calym sednem, bo to one
// odpadaly w kazdym poprzednim podejsciu do polskiego.
string[] aZle = new string[] {"zolw", "wszystkkich", "gesla", "ktury", "napewno", "wogole"};
foreach (string sSlowo in aZle) {
IEnumSpellingError en = sc.Check(sSlowo);
ISpellingError err;
bool bZlapane = (en != null && en.Next(out err) == 0 && err != null);
List<string> lPodp = ZbierzNapisy(sc.Suggest(sSlowo));
string sPodp = (lPodp.Count == 0) ? "(brak podpowiedzi)" : String.Join(", ", lPodp.ToArray());
if (lPodp.Count > 5) sPodp = String.Join(", ", lPodp.GetRange(0, 5).ToArray()) + ", ...";
Console.WriteLine("{0,-14} blad: {1,-3}  podpowiedzi: {2}", sSlowo, bZlapane ? "TAK" : "NIE", sPodp);
if (!bZlapane) iBad++;
}
Console.WriteLine();

Console.WriteLine("=== 4. Czy NIE krzyczy na poprawne polskie slowa ===");
Console.WriteLine();
// Falszywy alarm jest gorszy od przeoczenia: kaze poprawiac to, co bylo dobre.
// Dlatego sprawdzam to osobno i na slowach z ogonkami oraz odmienionych.
// UWAGA na wlasne literowki w tym miejscu: mialem tu "\u017Ard\u0142o" bez "o" i
// pomiar pokazal "falszywy alarm", choc sprawdzacz mial RACJE.  Slowa
// kontrolne musza byc naprawde poprawne, inaczej mierzy sie wlasny blad.
string[] aDobre = new string[] {"\u017C\u00F3\u0142w", "wszystkich", "\u017Ar\u00F3d\u0142o", "za\u017C\u00F3\u0142\u0107", "g\u0119\u015Bl\u0105", "ja\u017A\u0144", "niepodleg\u0142o\u015Bci", "Kasperczak"};
foreach (string sSlowo in aDobre) {
IEnumSpellingError en = sc.Check(sSlowo);
ISpellingError err;
bool bZlapane = (en != null && en.Next(out err) == 0 && err != null);
Console.WriteLine("{0,-18} zglaszane jako blad: {1}", sSlowo, bZlapane ? "TAK - FALSZYWY ALARM" : "nie (dobrze)");
if (bZlapane) iBad++;
}
Console.WriteLine();

Console.WriteLine("=== 5. Czy to naprawde lzej - pomiar czasu ===");
Console.WriteLine();
string sTekst = "";
for (int i = 0; i < 200; i++) sTekst += "To jest wiersz numer " + i + " z bledem wszystkkich oraz ze slowem zazolc gesla jazn. ";
Console.WriteLine("Tekst probny: {0} znakow", sTekst.Length);

// Rozgrzewka - pierwsze wywolanie placi za wczytanie slownika.
sc.Check("rozgrzewka");

Stopwatch sw = Stopwatch.StartNew();
int iBledy = 0;
IEnumSpellingError enAll = sc.Check(sTekst);
if (enAll != null) {
ISpellingError e2;
while (enAll.Next(out e2) == 0 && e2 != null) iBledy++;
}
sw.Stop();
Console.WriteLine("Sprawdzenie calosci: {0} ms, znalezionych bledow: {1}", sw.ElapsedMilliseconds, iBledy);
Console.WriteLine();
Console.WriteLine("Dla porownania - droga przez Worda to: uruchomienie Worda (sekundy,");
Console.WriteLine("pierwszy raz dziesiatki sekund), nowy dokument, wklejenie tekstu,");
Console.WriteLine("przelaczenie okna na Worda, okno dialogowe Worda, powrot.");
Console.WriteLine();

Console.WriteLine("Bledow w pomiarze: {0}", iBad);
} // Main method

} // PomiarPisownia class
