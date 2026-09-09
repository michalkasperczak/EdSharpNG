// Pomiar ZAKLADEK Z NAZWA (Control+Shift+B, Alt+Shift+B) na ZBUDOWANEJ binarce
// EdSharpNG.exe.  Zlecenie 1788204906696-3, czesc nie podjeta w iteracji 1.
//
// PO CO SONDA NA BINARCE, A NIE GREP PO KODZIE: Kasperczak zglosil "ctrl+shift+B
// i alt+shift+B dalej nie dzialaja", czyli pytanie brzmi "czy komenda JEST W
// PROGRAMIE", a nie "czy jest w pliku zrodlowym".  Refleksja pyta zbudowana
// binarke o metody i o napisy komunikatow.
//
// KONTROLA NEGATYWNA JEST WBUDOWANA: sonda przyjmuje DWIE binarki - biezaca i
// wersje SPRZED zmiany.  Kazda asercja "to jest w nowej" ma para "tego nie ma w
// starej".  Bez tego zielony wynik nie odroznia naprawy od gluchej sondy.
//
// Uzycie:
//   pomiar_zakladek_z_nazwa.exe <sciezka_nowej.exe> <sciezka_starej.exe>
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

public class PomiarZakladekZNazwa {

static int iPass = 0;
static int iFail = 0;

static void Ok(bool b, string sName, string sGot) {
if (b) {iPass++; Console.WriteLine("PASS  " + sName);}
else {iFail++; Console.WriteLine("FAIL  " + sName + "  -> " + sGot);}
}

static bool BinHasBytes(byte[] aBin, byte[] aNeedle) {
if (aNeedle.Length == 0) return false;
for (int i = 0; i + aNeedle.Length <= aBin.Length; i++) {
bool bOk = true;
for (int j = 0; j < aNeedle.Length; j++) {
if (aBin[i + j] != aNeedle[j]) {bOk = false; break;}
}
if (bOk) return true;
}
return false;
}

// Napis w binarce .NET siedzi w UTF-16LE albo ASCII, a offset nie musi byc
// parzysty - szukamy bajtowo w obu kodowaniach (lekcja z 27.08.2026).
static bool BinHas(byte[] aBin, string sText) {
if (BinHasBytes(aBin, Encoding.Unicode.GetBytes(sText))) return true;
return BinHasBytes(aBin, Encoding.ASCII.GetBytes(sText));
}

// SZUKANIE PODCIAGU KLAMIE NA CHORDACH.  Zmierzone przy tej sondzie:
// "Control+Shift+B" jest PODCIAGIEM "Control+Shift+Back" (Delete Left, komenda
// obecna od zawsze), wiec kontrola negatywna na starej binarce dawala falszywe
// "chord juz byl".  Napisy w tablicy #US assembly .NET maja PRZED soba prefiks
// dlugosci w bajtach (dla napisow do 63 znakow jeden bajt: 2*dlugosc+1), wiec
// dopasowanie z prefiksem odrzuca dluzszy napis o tym samym poczatku.
static bool BinHasExact(byte[] aBin, string sText) {
byte[] aU = Encoding.Unicode.GetBytes(sText);
if (aU.Length + 1 > 0x7F) return BinHas(aBin, sText);
byte[] aWithLen = new byte[aU.Length + 1];
aWithLen[0] = (byte) (aU.Length + 1);
Array.Copy(aU, 0, aWithLen, 1, aU.Length);
return BinHasBytes(aBin, aWithLen);
}

static Type FindFrame(Assembly asm) {
foreach (Type t in asm.GetTypes()) {
if (t.GetMethod("InsertMarkdownFootnote",
BindingFlags.NonPublic | BindingFlags.Instance) != null) return t;
}
return null;
}

static MethodInfo M(Type t, string sName) {
if (t == null) return null;
return t.GetMethod(sName,
BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
}

static Type FindDialog(Assembly asm) {
foreach (Type t in asm.GetTypes()) {
if (t.Name == "Dialog") return t;
}
return null;
}

public static int Main(string[] args) {
if (args.Length < 2) {
Console.WriteLine("Uzycie: pomiar_zakladek_z_nazwa.exe <nowa.exe> <stara.exe>");
return 3;
}
string sNew = args[0];
string sOld = args[1];
if (!File.Exists(sNew)) {Console.WriteLine("BRAK " + sNew); return 3;}
if (!File.Exists(sOld)) {Console.WriteLine("BRAK " + sOld); return 3;}

Console.WriteLine("Nowa:  " + sNew + "  (" + File.GetLastWriteTime(sNew).ToString("yyyy-MM-dd HH:mm:ss") + ")");
Console.WriteLine("Stara: " + sOld + "  (" + File.GetLastWriteTime(sOld).ToString("yyyy-MM-dd HH:mm:ss") + ")");
Console.WriteLine();

byte[] aNew = File.ReadAllBytes(sNew);
byte[] aOld = File.ReadAllBytes(sOld);
Ok(aNew.Length != aOld.Length || !BinHasBytes(aNew, aOld),
"binarki sa ROZNE (inaczej caly pomiar bylby porownaniem pliku z soba)",
"identyczne");

Assembly asmNew = Assembly.LoadFile(Path.GetFullPath(sNew));
Type tNew = FindFrame(asmNew);
if (tNew == null) {Console.WriteLine("NIE ZNALAZLEM typu okna glownego w nowej binarce"); return 3;}
Console.WriteLine("Typ okna glownego: " + tNew.FullName);
Console.WriteLine();

// ---------- 1. KONTROLE WAZNOSCI SONDY ----------
Console.WriteLine("-- 1. KONTROLE WAZNOSCI --");
Ok(BinHas(aNew, "Go to Bookmark"),
"KONTROLA POZYTYWNA: napis istniejacy od dawna JEST w nowej binarce", "brak");
Ok(!BinHas(aNew, "Set Sausage Bookmark"),
"KONTROLA NEGATYWNA: napis wymyslony NIE jest w nowej binarce", "sonda glucha");
Ok(BinHas(aOld, "Go to Bookmark"),
"KONTROLA POZYTYWNA na STAREJ: ten sam stary napis tam jest", "sonda nie czyta starej");
// KONTROLA SAMEGO DOPASOWANIA PELNEGO.  Bez niej "chordu nie ma w starej" nie
// odroznia naprawy od sondy, ktora nie widzi zadnego chordu.
Ok(BinHasExact(aOld, "Control+Shift+Back"),
"KONTROLA: dopasowanie PELNE widzi istniejacy dlugi chord w starej binarce", "nie widzi");
Ok(BinHas(aOld, "Control+Shift+B"),
"KONTROLA: dopasowanie PODCIAGIEM celowo trafia w Control+Shift+Back - dlatego jest odrzucone", "nie trafia");
Ok(!BinHasExact(aOld, "Control+Shift+Bacon"),
"KONTROLA: dopasowanie PELNE nie zmyslia chordu", "zmyslia");

// ---------- 2. POZYCJE MENU I CHORDY ----------
Console.WriteLine();
Console.WriteLine("-- 2. NOWE KOMENDY W BINARCE (a w starej ICH NIE MA) --");
Ok(BinHas(aNew, "Set &Named Bookmark ..."), "pozycja menu Set Named Bookmark", "brak");
Ok(!BinHas(aOld, "Set &Named Bookmark ..."), "w 5.0.55 tej pozycji NIE BYLO", "byla");
Ok(BinHas(aNew, "Named Bookmark &List ..."), "pozycja menu Named Bookmark List", "brak");
Ok(!BinHas(aOld, "Named Bookmark &List ..."), "w 5.0.55 tej pozycji NIE BYLO", "byla");
Ok(BinHasExact(aNew, "Control+Shift+B"), "chord Control+Shift+B jest w nowej binarce (dopasowanie PELNE, nie podciag)", "brak");
Ok(!BinHasExact(aOld, "Control+Shift+B"), "chord Control+Shift+B nie byl uzywany w 5.0.55", "byl");
Ok(BinHasExact(aNew, "Alt+Shift+B"), "chord Alt+Shift+B jest w nowej binarce (dopasowanie PELNE, nie podciag)", "brak");
Ok(!BinHasExact(aOld, "Alt+Shift+B"), "chord Alt+Shift+B nie byl uzywany w 5.0.55", "byl");

// ---------- 3. KOMUNIKATY MOWIONE ----------
Console.WriteLine();
Console.WriteLine("-- 3. KOMUNIKATY, KTORE USLYSZY NIEWIDOMY --");
Ok(BinHas(aNew, "Named bookmark set"), "potwierdzenie wstawienia", "brak");
Ok(BinHas(aNew, "Bookmark renamed"), "potwierdzenie zmiany nazwy", "brak");
Ok(BinHas(aNew, "Named bookmark removed"), "potwierdzenie usuniecia", "brak");
Ok(BinHas(aNew, "No named bookmark!"), "komunikat pustej listy", "brak");
Ok(BinHas(aNew, "No name, no bookmark set!"), "odmowa przy pustej nazwie", "brak");
Ok(BinHas(aNew, "Set Named Bookmark"), "tytul okna przy wstawianiu", "brak");
Ok(BinHas(aNew, "Rename Bookmark"), "tytul okna przy zmianie nazwy", "brak");
Ok(BinHas(aNew, "Named Bookmarks"), "tytul listy", "brak");
Ok(!BinHas(aOld, "Named bookmark set"), "zadnego z tych komunikatow nie bylo w 5.0.55", "byl");
Ok(!BinHas(aOld, "No named bookmark!"), "komunikatu pustej listy nie bylo w 5.0.55", "byl");

// ---------- 4. MAGAZYN: SEKCJA I FORMAT KLUCZA ----------
Console.WriteLine();
Console.WriteLine("-- 4. MAGAZYN JEST OSOBNY OD ULUBIONYCH --");
Ok(BinHas(aNew, "NamedBookmarks"), "nazwa sekcji NamedBookmarks jest w binarce", "brak");
Ok(!BinHas(aOld, "NamedBookmarks"), "w 5.0.55 takiej sekcji nie bylo", "byla");
Ok(BinHas(aNew, "Favorites"),
"KONTROLA: sekcja Favorites (zakladki zwykle i ulubione) NADAL istnieje", "zniknela");

MethodInfo miKey = M(tNew, "NamedBookmarkKey");
Ok(miKey != null, "metoda klucza NamedBookmarkKey istnieje", "brak");
if (miKey != null) {
string sKey = (string) miKey.Invoke(null, new object[] {@"C:\pliki\notatki.md", 41});
Ok(sKey == @"C:\pliki\notatki.md|41",
"klucz to sciezka pionowa kreska numer wiersza", sKey);
// KLUCZ NA ZAKLADKE, NIE JEDNA WARTOSC NA PLIK: bufor GetPrivateProfileString
// ma 260 znakow, wiec lista w jednej wartosci ucinalaby sie po kilkunastu
// nazwach.  Ten pomiar pokazuje, ze klucz zawiera numer wiersza, czyli jest
// jeden na zakladke.
Ok(sKey.EndsWith("|41"), "numer wiersza jest CZESCIA klucza", sKey);
}

// ---------- 5. LISTA: ODCZYT, SORTOWANIE, ODSIEW OBCYCH PLIKOW ----------
Console.WriteLine();
Console.WriteLine("-- 5. ODCZYT LISTY (na prawdziwym pliku ustawien) --");
MethodInfo miRead = M(tNew, "ReadNamedBookmarks");
Ok(miRead != null, "metoda ReadNamedBookmarks istnieje", "brak");
MethodInfo miWrite = M(tNew, "WriteNamedBookmark");
Ok(miWrite != null, "metoda WriteNamedBookmark istnieje", "brak");
MethodInfo miReadOne = M(tNew, "ReadNamedBookmark");
Ok(miReadOne != null, "metoda ReadNamedBookmark istnieje", "brak");
MethodInfo miDel = M(tNew, "DeleteNamedBookmark");
Ok(miDel != null, "metoda DeleteNamedBookmark istnieje", "brak");

Assembly asmOld = Assembly.LoadFile(Path.GetFullPath(sOld));
Type tOld = FindFrame(asmOld);
Ok(M(tOld, "ReadNamedBookmarks") == null,
"KONTROLA ROZNICUJACA: w 5.0.55 metody ReadNamedBookmarks NIE BYLO", "byla");
Ok(M(tOld, "InsertMarkdownFootnote") != null,
"KONTROLA POZYTYWNA na starej: stara metoda przypisow tam jest", "sonda nie widzi starej");

// ---------- 6. DIALOG LISTY ----------
Console.WriteLine();
Console.WriteLine("-- 6. OKNO LISTY (osobne od listy zakladek zwyklych) --");
Type tDlgNew = FindDialog(asmNew);
Ok(tDlgNew != null, "klasa Dialog znaleziona", "brak");
MethodInfo miPickNamed = (tDlgNew == null) ? null : tDlgNew.GetMethod("PickNamedBookmark",
BindingFlags.Public | BindingFlags.Static);
Ok(miPickNamed != null, "Dialog.PickNamedBookmark istnieje", "brak");
MethodInfo miPickPlain = (tDlgNew == null) ? null : tDlgNew.GetMethod("PickBookmark",
BindingFlags.Public | BindingFlags.Static);
Ok(miPickPlain != null,
"KONTROLA: stara Dialog.PickBookmark (lista zakladek zwyklych) NADAL istnieje - DWIE osobne listy",
"zniknela");
Type tDlgOld = FindDialog(asmOld);
Ok(tDlgOld != null && tDlgOld.GetMethod("PickNamedBookmark",
BindingFlags.Public | BindingFlags.Static) == null,
"KONTROLA ROZNICUJACA: w 5.0.55 PickNamedBookmark NIE BYLO", "bylo");

// ---------- 7. CO ZOSTALO NIETKNIETE ----------
Console.WriteLine();
Console.WriteLine("-- 7. ZAKLADKI ZWYKLE NIETKNIETE --");
Ok(BinHas(aNew, "Set Bookmar&k"), "Control+B nadal wstawia zakladke zwykla", "brak");
Ok(BinHas(aNew, "Next Bookmark"), "Shift+PageDown nadal chodzi po zakladkach", "brak");
Ok(BinHas(aNew, "Prior Bookmark"), "Shift+PageUp nadal chodzi po zakladkach", "brak");
Ok(BinHas(aNew, "Clear Bookmark"), "Clear Bookmark nadal w menu", "brak");
Ok(M(tNew, "InsertMarkdownFootnote") != null, "przypisy nietkniete", "brak");

Console.WriteLine();
Console.WriteLine("== " + iPass + " PASS, " + iFail + " FAIL ==");
return (iFail == 0) ? 0 : 1;
}

} // PomiarZakladekZNazwa class
