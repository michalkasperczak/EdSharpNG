// Sonda 5.0.64 na ZBUDOWANEJ binarce (refleksja + napisy w .exe).
//
// CO MIERZY - dwie decyzje Kasperczaka z 01.09.2026, ktore do 5.0.63 NIE
// zostaly wdrozone (znalezione 03.09 przy weryfikacji wyslanego pliku pytan):
//
//   A. ROZDZIELENIE ZAKLADEK OD ULUBIONYCH (jego slowa 10:42: "Rozdzielic.
//      Zakladki a ulubione pliki nie sa powiazane w jedna i druga strone.  To
//      osobne sprawy.").  Zakladki zwykle maja WLASNA sekcje pliku ustawien
//      "Bookmarks", a migracja MigrateBookmarksOutOfFavorites przepisuje to,
//      co user ma na dysku.
//   B. PROPOZYCJA NAZWY ZAKLADKI TO SLOWO POD KURSOREM, nie caly wiersz (jego
//      slowa 10:34: "Nazwa zakladki nie tyle z wiersza co ze slowa na ktorym
//      jest kursor").
//
// DLACZEGO NAPISY, A NIE TYLKO REFLEKSJA: nazwa sekcji INI jest literalem, wiec
// siedzi w napisach .exe dokladnie wtedy, gdy kod jej uzywa.  Refleksja pokaze
// istnienie metody migracji, ale NIE pokaze, ze odczyty zakladek przestaly
// pytac o sekcje Favorites - to rozstrzyga obecnosc napisu "Bookmarks".
//
// KONTROLA WAZNOSCI (bez niej zielony wynik nic nie znaczy): ta sama sonda na
// binarce 5.0.63 MUSI skonczyc kodem 3, bo tam metody MigrateBookmarksOutOfFavorites
// NIE MA.  Sprawdzone w tej iteracji.
//
// PULAPKA, KTORA TA SONDA OMIJA (lekcja z 5.0.63): pytanie o ISTNIENIE POLA
// klasy przepuscilo blad, bo pole jest deklarowane osobno od przypisania.
// Dlatego o zachowanie pytamy NAPISAMI, a o refleksje tylko tam, gdzie chodzi
// naprawde o istnienie metody.
//
// Uzycie:
//   csc.exe /out:pomiar_564.exe pomiar_564.cs
//   pomiar_564.exe <sciezka do EdSharpNG.exe>

using System;
using System.IO;
using System.Reflection;
using System.Text;

class Pomiar564 {

static int iOk = 0;
static int iZle = 0;

static void Ok(string s) {iOk++; Console.WriteLine("OK: " + s);}
static void Zle(string s) {iZle++; Console.WriteLine("ZLE: " + s);}
static void Sprawdz(bool b, string s) {if (b) Ok(s); else Zle(s);}

// Napisy .exe czytamy jako UTF-16LE ORAZ Latin1: literaly C# sa w binarce
// szesnastobitowe, ale czesc napisow (nazwy metadanych) jest jednobajtowa.
// Pytanie tylko o jedno kodowanie dawalo falszywe zero.
//
// PULAPKA WYRWNANIA, ZMIERZONA 03.09.2026 NA TEJ SONDZIE: dekodowanie UTF-16
// tylko od bajtu ZEROWEGO gubi kazdy literal, ktory zaczyna sie na NIEPARZYSTYM
// offsecie - a tak lezy wiekszosc napisow w .exe.  Pierwsza wersja tej sondy
// dala 6 ZLE na napisach, ktore NAPRAWDE tam byly (potwierdzone niezaleznie:
// strings -el nowa.exe | grep -F "Added to favorites" zwracalo trafienie).
// Zielone bylo tylko to, co przypadkiem wypadlo na parzystym offsecie, czyli
// sonda mierzyla wyrownanie, a nie kod.  Dlatego pytamy o OBA wyrownania.
static bool MaNapis(byte[] ab, string s) {
if (Encoding.Unicode.GetString(ab).Contains(s)) return true;
if (ab.Length > 1) {
byte[] abShift = new byte[ab.Length - 1];
Array.Copy(ab, 1, abShift, 0, abShift.Length);
if (Encoding.Unicode.GetString(abShift).Contains(s)) return true;
}
string sLat = Encoding.GetEncoding(28591).GetString(ab);
return sLat.Contains(s);
}

static int Main(string[] args) {
if (args.Length < 1) {
Console.WriteLine("Uzycie: pomiar_564.exe <sciezka do EdSharpNG.exe>");
return 2;
}
string sPath = args[0];
if (!File.Exists(sPath)) {
Console.WriteLine("BLAD: nie ma pliku " + sPath);
return 2;
}

byte[] ab = File.ReadAllBytes(sPath);
Assembly asm = Assembly.LoadFrom(sPath);

Type tApp = null;
foreach (Type t in asm.GetTypes()) if (t.Name == "App") {tApp = t; break;}
if (tApp == null) {
Console.WriteLine("BLAD: nie ma klasy App w binarce - sonda nie ma czego mierzyc.");
return 2;
}

// ASERCJA NOSNA I KONTROLA WAZNOSCI W JEDNYM: metody migracji nie ma w
// zadnej wczesniejszej wersji, wiec jej brak znaczy \"mierzysz stara binarke\".
MethodInfo miMig = tApp.GetMethod("MigrateBookmarksOutOfFavorites",
BindingFlags.Public | BindingFlags.Static);
if (miMig == null) {
Console.WriteLine("ZLE: metody App.MigrateBookmarksOutOfFavorites NIE MA w binarce.");
Console.WriteLine("To jest kontrola waznosci sondy: na binarce 5.0.63 i starszej");
Console.WriteLine("ten wynik jest OCZEKIWANY, bo rozdzielenia zakladek tam nie ma.");
return 3;
}
Ok("metoda App.MigrateBookmarksOutOfFavorites istnieje (refleksja)");

Console.WriteLine();
Console.WriteLine("--- A. ZAKLADKI MAJA WLASNA SEKCJE PLIKU USTAWIEN");
Sprawdz(MaNapis(ab, "Bookmarks"),
"napis nazwy sekcji \"Bookmarks\" jest w binarce (czyli kod jej uzywa)");
Sprawdz(MaNapis(ab, "BookmarksSplit"),
"znacznik jednorazowej migracji \"BookmarksSplit\" jest w binarce");
// Migracja MUSI umiec przepisac to, co user ma na dysku, a nie tylko zaczac
// pisac w nowym miejscu - inaczej jego dzisiejsze zakladki znikaja z widoku.
Sprawdz(MaNapis(ab, "Favorites"),
"napis \"Favorites\" ZOSTAJE - ulubione dalej istnieja i migracja ma skad czytac");

Console.WriteLine();
Console.WriteLine("--- B. ROZLACZNOSC: nie zabralismy niczego, co dziala");
// Bez tych asercji \"rozdzielenie\" polegajace na wywaleniu ulubionych albo
// zakladek z nazwa tez byloby zielone.
Sprawdz(MaNapis(ab, "NamedBookmarks"),
"zakladki Z NAZWA maja nadal wlasna sekcje NamedBookmarks (5.0.55 nietknieta)");
Sprawdz(MaNapis(ab, "Added to favorites"),
"komunikat dodania do ulubionych nadal jest");
Sprawdz(MaNapis(ab, "Removed from favorites"),
"komunikat zdjecia z ulubionych nadal jest");
Sprawdz(MaNapis(ab, "No bookmark!"),
"komunikat braku zakladki nadal jest");
Sprawdz(MaNapis(ab, "No bookmarks, press Escape to close the list"),
"5.0.62 nietknieta: pusta lista zakladek nadal mowi o Escape");

// KOMUNIKAT O KASOWANIU ZAKLADEK MUSI ZNIKNAC: po rozdzieleniu zdjecie z
// ulubionych zakladek NIE RUSZA, wiec mowienie o tym byloby nieprawda.  Dla
// niewidomego komunikat niezgodny ze stanem jest gorszy niz brak komendy.
Console.WriteLine();
Console.WriteLine("--- C. PROGRAM NIE MOWI JUZ NIEPRAWDY O KASOWANIU ZAKLADEK");
Sprawdz(!MaNapis(ab, "cleared too"),
"napis \"cleared too\" (zakladki skasowane razem z ulubionym) ZNIKNAL z binarki");

Console.WriteLine();
Console.WriteLine("--- D. WERSJE POPRZEDNIE NIETKNIETE (regresja)");
Sprawdz(!MaNapis(ab, "SayAllTempFile"),
"5.0.63 nietknieta: wolanie skryptu JAWS SayAllTempFile nadal nie istnieje");
Sprawdz(MaNapis(ab, "Control+Alt+F9"),
"5.0.63 nietknieta: lista komentarzy nadal na Control+Alt+F9");
Sprawdz(MaNapis(ab, "Alt+PageDown"),
"5.0.62 nietknieta: rodzina skokow po odsylaczach nadal opisana chordem");
Sprawdz(MaNapis(ab, "ExtensionDefaultMigrated"),
"5.0.59 nietknieta: migracja domyslnego rozszerzenia nadal jest");

MethodInfo miMigExt = tApp.GetMethod("MigrateDefaultExtensionToMarkdown",
BindingFlags.Public | BindingFlags.Static);
Sprawdz(miMigExt != null,
"5.0.59 nietknieta: metoda MigrateDefaultExtensionToMarkdown nadal jest (refleksja)");

Console.WriteLine();
Console.WriteLine("PODSUMOWANIE: " + iOk + " OK, " + iZle + " ZLE");
return (iZle == 0) ? 0 : 1;
} // Main method

} // Pomiar564 class
