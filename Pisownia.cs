// SPRAWDZANIE PISOWNI BEZ WORDA - Windows Spell Checking API.
//
// Zadanie 4 z listy Kasperczaka (11.09.2026), doprecyzowane przez niego
// 12.09.2026: "EdSharp kiedys uruchamial Word i sprawdzal, teraz chyba bylo
// tak, ze jakos lzej bez Worda".
//
// CO BYLO DO TEJ PORY.  Frame.SpellCheck() (F7) uruchamia Microsoft Word przez
// COM: tworzy nowy dokument, wkleja tekst, PRZELACZA OKNO na Worda, wola
// CheckSpelling, czyta wynik, zamyka dokument, wraca.  Wymaga Worda i wyrzuca
// uzytkownika z edytora do obcego programu.
//
// CO JEST TERAZ.  Windows ma wlasne sprawdzanie pisowni - to samo, ktorego
// uzywa Edge i Poczta.  Jest czescia systemu od Windows 8, wiec do paczki nie
// dokladamy ZADNEJ obcej biblioteki (to bylo wazne: Hunspell czy NHunspell
// znaczylyby wlasny plik slownika w instalatorze i wlasny format do utrzymania).
//
// ZMIERZONE (testy/pomiar_pisownia.cs), zanim to napisalem:
//   - API odpowiada;
//   - zna POLSKI: pl-PL, "Modul sprawdzania pisowni systemu Microsoft Windows";
//   - lapie polskie bledy z ogonkami i podpowiada sensownie:
//       wszystkkich -> wszystkich;  ktury -> ktory;  napewno -> na pewno;
//       wogole -> w ogole;  gesla -> gesla z ogonkami;
//   - NIE krzyczy na poprawne polskie slowa (zrodlo, zazolc, geslą, jazn,
//     niepodleglosci, a takze nazwisko Kasperczak);
//   - 15 890 znakow sprawdza w 89 ms.  Word na to samo potrzebuje sekund, a przy
//     pierwszym uruchomieniu dziesiatek sekund.
//
// DLACZEGO PRZEZ WSKAZNIKI, A NIE "po prostu string".  Napisy z tego API sa
// przydzielone po stronie systemu.  Pierwsza proba brala je jako string[] i
// dostawala PUSTE wartosci - lista jezykow miala jeden element rowny null.
// Dlatego napisy odbieram jako wskazniki, przepisuje i zwalniam sam.
//
// IDENTYFIKATORY INTERFEJSOW pochodza z naglowka spellcheck.idl z Windows SDK.
// Pierwsza proba miala jeden zmyslony z pamieci i system slusznie odmowil
// ("Taki interfejs nie jest obslugiwany") - stad reguła: identyfikator bierze
// sie ze zrodla albo sie go nie uzywa.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// Ta sama przestrzen nazw co reszta edytora - inaczej EdSharp.cs jej nie widzi.
namespace EdSharp {

public static class Pisownia {

[ComImport, Guid("00000101-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IEnumString {
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
}

[ComImport, Guid("8E018A9D-2415-4677-BF08-794EA61F94BB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface ISpellCheckerFactory {
IEnumString SupportedLanguages { get; }
[PreserveSig] int IsSupported([MarshalAs(UnmanagedType.LPWStr)] string languageTag, out bool value);
ISpellChecker CreateSpellChecker([MarshalAs(UnmanagedType.LPWStr)] string languageTag);
}

[ComImport, Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC")]
class SpellCheckerFactory {}

// Jeden blad pisowni: gdzie jest i co system proponuje w jego miejsce.
public class Blad {
public int Start;
public int Dlugosc;
public string Slowo = "";
public List<string> Podpowiedzi = new List<string>();
}

static ISpellChecker scCache = null;
static string sJezykCache = "";
static bool bProbowano = false;

static List<string> ZbierzNapisy(IEnumString en) {
List<string> l = new List<string>();
if (en == null) return l;
IntPtr[] a = new IntPtr[1];
int iGot;
while (en.Next(1, a, out iGot) == 0 && iGot == 1) {
if (a[0] == IntPtr.Zero) continue;
string s = Marshal.PtrToStringUni(a[0]);
Marshal.FreeCoTaskMem(a[0]);
a[0] = IntPtr.Zero;
if (!String.IsNullOrEmpty(s)) l.Add(s);
}
return l;
}

// Jezyk sprawdzania.  Kolejnosc: wpis SpellLanguage z pliku ini (gdy ktos chce
// sprawdzac po angielsku w polskim systemie), potem jezyk Windows.
static string WybierzJezyk() {
string sZIni = App.ReadOption("SpellLanguage", "").Trim();
if (sZIni.Length > 0) return sZIni;
try { return System.Globalization.CultureInfo.CurrentUICulture.Name; } catch {}
return "pl-PL";
}

// Sprawdzacz tworzymy RAZ i trzymamy - tworzenie wczytuje slownik.
// Zwraca null, gdy systemowe sprawdzanie jest niedostepne albo nie zna jezyka;
// wolajacy MUSI to sprawdzic i wtedy uzyc starej drogi przez Worda.
// Ta metoda jest wewnetrzna, choc reszta klasy jest publiczna: zwraca uchwyt
// do systemowego sprawdzacza, ktorego reszta programu nie powinna dotykac
// bezposrednio - do pracy sa Sprawdz, Dodaj i Pomijaj.
static ISpellChecker Sprawdzacz(out string sJezyk) {
sJezyk = sJezykCache;
if (scCache != null) return scCache;
if (bProbowano) return null;   // nie probujemy w kolko przy kazdym F7
bProbowano = true;
try {
ISpellCheckerFactory f = (ISpellCheckerFactory) new SpellCheckerFactory();
string sChce = WybierzJezyk();
bool bOk = false;
f.IsSupported(sChce, out bOk);
// Gdy nie ma dokladnie "pl-PL", probujemy tego, co jest na liscie i zaczyna
// sie tak samo - inaczej odpadalibysmy na roznicy typu "pl" wobec "pl-PL".
if (!bOk) {
string sKrotki = sChce.Split('-')[0];
foreach (string s in ZbierzNapisy(f.SupportedLanguages)) {
if (s.StartsWith(sKrotki, StringComparison.OrdinalIgnoreCase)) { sChce = s; bOk = true; break; }
}
}
if (!bOk) return null;
scCache = f.CreateSpellChecker(sChce);
sJezykCache = sChce;
sJezyk = sChce;
return scCache;
}
catch { return null; }
} // Sprawdzacz method

public static bool Dostepne() {
string sJezyk;
return Sprawdzacz(out sJezyk) != null;
} // Dostepne method

public static string Jezyk() {
string sJezyk;
Sprawdzacz(out sJezyk);
return sJezyk;
} // Jezyk method

// Wszystkie bledy w tekscie, po kolei, razem z podpowiedziami.
public static List<Blad> Sprawdz(string sText) {
List<Blad> lBledy = new List<Blad>();
if (String.IsNullOrEmpty(sText)) return lBledy;
string sJezyk;
ISpellChecker sc = Sprawdzacz(out sJezyk);
if (sc == null) return lBledy;
try {
IEnumSpellingError en = sc.Check(sText);
if (en == null) return lBledy;
ISpellingError err;
while (en.Next(out err) == 0 && err != null) {
Blad b = new Blad();
b.Start = (int) err.StartIndex;
b.Dlugosc = (int) err.Length;
if (b.Start < 0 || b.Dlugosc <= 0 || b.Start + b.Dlugosc > sText.Length) continue;
b.Slowo = sText.Substring(b.Start, b.Dlugosc);
// CorrectiveAction 2 znaczy, ze system ma JEDNA pewna zamiane (np. literowka
// z listy autokorekty) - wtedy Replacement jest wypelniony i jest to
// najlepsza podpowiedz, wiec idzie na poczatek.
try {
if (err.CorrectiveAction == 2) {
string sRep = err.Replacement;
if (!String.IsNullOrEmpty(sRep)) b.Podpowiedzi.Add(sRep);
}
}
catch {}
try { foreach (string s in ZbierzNapisy(sc.Suggest(b.Slowo))) if (!b.Podpowiedzi.Contains(s)) b.Podpowiedzi.Add(s); }
catch {}
lBledy.Add(b);
}
}
catch {}
return lBledy;
} // Sprawdz method

// Dodanie slowa do slownika uzytkownika - na stale, w slowniku Windows, wiec
// dziala takze w innych programach.
public static bool Dodaj(string sSlowo) {
string sJezyk;
ISpellChecker sc = Sprawdzacz(out sJezyk);
if (sc == null || String.IsNullOrEmpty(sSlowo)) return false;
try { sc.Add(sSlowo); return true; }
catch { return false; }
} // Dodaj method

// Pomijanie slowa do konca pracy programu - nie brudzi slownika na stale.
public static bool Pomijaj(string sSlowo) {
string sJezyk;
ISpellChecker sc = Sprawdzacz(out sJezyk);
if (sc == null || String.IsNullOrEmpty(sSlowo)) return false;
try { sc.Ignore(sSlowo); return true; }
catch { return false; }
} // Pomijaj method

} // Pisownia class

} // namespace EdSharp
