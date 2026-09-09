// Pomiar PRZYPISOW markdownowych na ZBUDOWANYM EdSharpNG.exe (refleksja).
//
// Zlecenie 1787832545532-3 (ustalenia Kasperczaka 27.08.2026 13:51-14:04).
// Mierzymy metody, ktore realnie poleca u usera - nie kopie kodu, tylko to,
// co zostalo skompilowane do binarki:
//   GetMarkdownFootnoteRefs           - znaczniki [^1] w tekscie
//   GetMarkdownFootnoteDefs           - tresci [^1]: ... na koncu
//   GetNextMarkdownFootnoteLabel      - numeracja automatyczna
//   BuildMarkdownFootnoteDefinition   - tresc dopisywana na koniec
//   IsMarkdownFootnoteDefinitionLine  - rozpoznanie wiersza z trescia
//   FindMarkdownFootnoteRefAtIndex    - ktory znacznik jest "pod kursorem"
//   FindMarkdownFootnoteByLabel       - parowanie znacznika z trescia
//
// KONTROLE WAZNOSCI (bez nich zielony wynik nic nie dowodzi):
//   - sonda MUSI znalezc pola menuMiscInsertFootnote, menuMiscGoToFootnote,
//     menuMiscFootnoteList (dowod, ze komendy sa w binarce),
//   - KONTROLA NEGATYWNA: menuMiscRepeatLine MUSI istniec, a wymyslone
//     menuMiscNieMaTakiego MUSI nie istniec - to lapie glucha sonde,
//   - blok kodu: znacznik w ogrodzeniu NIE moze byc policzony jako przypis
//     (kontrola dyskryminacyjna: ten sam tekst poza blokiem JEST liczony),
//   - zwykly nawias kwadratowy w zdaniu NIE moze udawac przypisu.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

public class PomiarPrzypisow {

static Assembly asm;
static Type tFrame;
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

// Napis w binarce .NET moze byc w UTF-16LE (literaly) albo w ASCII (nazwy
// metadanych), a jego offset nie musi byc parzysty - dlatego sprawdzamy OBA
// kodowania na poziomie bajtow, nie przez dekodowanie calego pliku.
static bool BinHas(byte[] aBin, string sText) {
if (BinHasBytes(aBin, Encoding.Unicode.GetBytes(sText))) return true;
return BinHasBytes(aBin, Encoding.ASCII.GetBytes(sText));
}

static MethodInfo M(string sName) {
MethodInfo mi = tFrame.GetMethod(sName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
if (mi == null) throw new Exception("Brak metody " + sName);
return mi;
}

// Lista MarkdownFootnote jest typu prywatnego, wiec czytamy pola refleksja.
static int Count(object oList) {
if (oList == null) return 0;
return (int) oList.GetType().GetProperty("Count").GetValue(oList, null);
}

static object Item(object oList, int i) {
return oList.GetType().GetProperty("Item").GetValue(oList, new object[] {i});
}

static string Label(object oNote) {
return (string) oNote.GetType().GetField("Label").GetValue(oNote);
}

static string TextOf(object oNote) {
return (string) oNote.GetType().GetField("Text").GetValue(oNote);
}

static int StartOf(object oNote) {
return (int) oNote.GetType().GetField("Start").GetValue(oNote);
}

public static int Main(string[] args) {
asm = Assembly.LoadFrom(Path.GetFullPath("EdSharpNG.exe"));
tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {Console.WriteLine("Brak typu EdSharp.MdiFrame"); return 3;}

// ---------- 1. POLA MENU + KONTROLA NEGATYWNA ----------
string[] asMusiByc = {"menuMiscInsertFootnote", "menuMiscGoToFootnote", "menuMiscFootnoteList",
                      "menuMiscRepeatLine", "menuMiscTableOfContents"};
foreach (string s in asMusiByc) {
FieldInfo fi = tFrame.GetField(s, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
Ok(fi != null, "pole istnieje: " + s, "null");
}
FieldInfo fiNo = tFrame.GetField("menuMiscNieMaTakiego", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
Ok(fiNo == null, "KONTROLA NEGATYWNA: wymyslone pole nie istnieje", "sonda jest glucha albo zbyt hojna");

MethodInfo miRefs = M("GetMarkdownFootnoteRefs");
MethodInfo miDefs = M("GetMarkdownFootnoteDefs");
MethodInfo miNext = M("GetNextMarkdownFootnoteLabel");
MethodInfo miBuild = M("BuildMarkdownFootnoteDefinition");
MethodInfo miIsDef = M("IsMarkdownFootnoteDefinitionLine");
MethodInfo miAtIndex = M("FindMarkdownFootnoteRefAtIndex");
MethodInfo miByLabel = M("FindMarkdownFootnoteByLabel");

// ---------- 1b. KLAWISZE I USUNIETE KOMENDY ----------
// Zgoda Kasperczaka 27.08.2026 ("Tak. Mozna podmienic ten klawisz.") zwolnila
// Control+F6 i Alt+F6. Mierzymy OBA fakty: stare komendy zniknely, a nowe
// stoja dokladnie na tych klawiszach - samo "pole istnieje" tego nie dowodzi.
string[] asUsuniete = {"menuNavigateSearchForTopic", "menuNavigateSearchForTopicAgain"};
foreach (string s in asUsuniete) {
FieldInfo fi = tFrame.GetField(s, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
Ok(fi == null, "pole USUNIETE: " + s, "nadal jest");
}
// Skroty czytamy z KeyMap zbudowanej binarki, czyli z tego, co program realnie
// zarejestrowal przy starcie - a nie z tekstu zrodla.
// SKROTY: KeyMap wypelnia sie dopiero przy budowaniu menu, czyli po starcie
// GUI. Zmierzone 27.08.2026: na samej zaladowanej binarce KeyMap.getKey zwraca
// None dla KAZDEJ komendy, takze dla "Go to Contents", ktorej skrotu nie
// ruszalismy - wiec ta droga nie mierzy wiazania komenda-skrot, tylko pusta
// tabele. Falszywie zielony wynik bylby tu gorszy od braku pomiaru.
// Dlatego wiazanie mierzymy DWOMA innymi drogami:
//   (a) tutaj: obecnosc nazw komend i skrotow w binarce (sonda po napisach,
//       kodowanie UTF-16LE, bo w ASCII .NET daje falszywe zero),
//   (b) osobno: URUCHOMIENIEM programu i sprawdzeniem, ze NIE pokazuje okna
//       alarmu o zajetym skrocie (testy/pomiar_startu_bez_alarmu.py).
// Sama sonda po napisach nie dowodzi wiazania - dowodzi go (b).
byte[] aBin = File.ReadAllBytes(Path.GetFullPath("EdSharpNG.exe"));
// Sonda BAJTOWA. Zmierzone 27.08.2026: dekodowanie calego pliku jako Unicode
// (Encoding.Unicode.GetString) gubi napisy, bo ich offsety nie musza byc
// parzyste - kontrola pozytywna "Repeat Line" oblala, czyli sonda byla
// zepsuta, a nie kod. Szukamy wiec ciagu bajtow w obu kodowaniach naraz.
// AKTUALIZACJA 5.0.46: przypisy zeszly z rodziny F6 do rodziny K, bo Control+F6
// ma dostac liste linkow (jego slowa: "to CTRL-F6 linki fajne"). Wstawianie jest
// na Control+Shift+K, lista na Alt+K.
// POPRAWKA 5.0.54: asercja na "Alt+F6" byla MARTWA od 5.0.47, kiedy kontekstowy
// skok przeszedl na Control+Alt+K i rodzina F6 wyszla z przypisow zupelnie. Ta
// sonda oblewala JEDNA asercje na binarce 5.0.53 i 5.0.54 identycznie, wiec
// mierzyla nieaktualne wymaganie, a nie defekt. Poprawiony chord: Control+Alt+K.
string[] asObecne = {"Insert Footnote", "Go to Footnote", "Footnote List", "Control+Shift+K", "Control+Alt+K",
                     "Alt+K", "Footnotes work only on Markdown files!", "No footnotes!",
                     "Repeat Line"};
foreach (string s in asObecne) Ok(BinHas(aBin, s), "napis w binarce: " + s, "brak");
// KONTROLA ROZNICUJACA przeniesienia: samo "nowy klawisz jest" nie odroznia
// przeniesienia od dopisania drugiej komendy. Stary chord wstawiania MUSI zniknac
// z binarki. Control+Shift+F6 tez - zwolnil sie po zejsciu listy przypisow.
string[] asNieobecne = {"Search for Topic", "No topic to search for again!", "Wymyslony Napis Ktorego Nie Ma",
                        "Control+Shift+F6"};
foreach (string s in asNieobecne) Ok(!BinHas(aBin, s), "napis NIEOBECNY w binarce: " + s, "jest");

// ---------- 2. ZNACZNIKI I TRESCI W PROSTYM DOKUMENCIE ----------
string sDoc = "# Tytul\n\nZdanie z przypisem[^1] i drugim[^2].\n\nAkapit bez przypisu.\n\n[^1]: Pierwsza tresc.\n[^2]: Druga tresc.\n";
object oRefs = miRefs.Invoke(null, new object[] {sDoc});
object oDefs = miDefs.Invoke(null, new object[] {sDoc});
Ok(Count(oRefs) == 2, "dwa znaczniki w tekscie", "policzono " + Count(oRefs));
Ok(Count(oDefs) == 2, "dwie tresci przypisow", "policzono " + Count(oDefs));
if (Count(oRefs) == 2) {
Ok(Label(Item(oRefs, 0)) == "1" && Label(Item(oRefs, 1)) == "2", "etykiety znacznikow 1 i 2",
   Label(Item(oRefs, 0)) + "," + Label(Item(oRefs, 1)));
}
if (Count(oDefs) == 2) {
Ok(TextOf(Item(oDefs, 0)) == "Pierwsza tresc.", "tresc przypisu 1 odczytana", TextOf(Item(oDefs, 0)));
Ok(TextOf(Item(oDefs, 1)) == "Druga tresc.", "tresc przypisu 2 odczytana", TextOf(Item(oDefs, 1)));
}

// KLUCZOWE: wiersz [^1]: tresc NIE jest liczony jako znacznik w tekscie.
// Bez tego skok "ze zdania do tresci" trafialby sam w siebie.
Ok(Count(oRefs) == 2, "wiersz z trescia nie udaje znacznika", "policzono " + Count(oRefs));

// ---------- 3. BLOK KODU: kontrola dyskryminacyjna ----------
string sFence = "# Tytul\n\n```\nprzyklad[^9] w kodzie\n```\n\nZdanie[^1].\n\n[^1]: Tresc.\n";
object oFenceRefs = miRefs.Invoke(null, new object[] {sFence});
Ok(Count(oFenceRefs) == 1, "znacznik w bloku kodu POMINIETY", "policzono " + Count(oFenceRefs));
if (Count(oFenceRefs) == 1) Ok(Label(Item(oFenceRefs, 0)) == "1", "policzony jest ten POZA blokiem", Label(Item(oFenceRefs, 0)));
// Ten sam tekst BEZ ogrodzenia musi dac dwa - inaczej pominiecie wyzej moze
// wynikac z gluchoty parsera, a nie ze swiadomosci bloku kodu.
string sNoFence = "# Tytul\n\nprzyklad[^9] w tekscie\n\nZdanie[^1].\n\n[^1]: Tresc.\n";
object oNoFenceRefs = miRefs.Invoke(null, new object[] {sNoFence});
Ok(Count(oNoFenceRefs) == 2, "KONTROLA: ten sam tekst poza blokiem daje dwa", "policzono " + Count(oNoFenceRefs));

// ---------- 4. ZWYKLY NAWIAS NIE JEST PRZYPISEM ----------
string sBrackets = "Zdanie z [linkiem](http://a.pl) i [zwyklym nawiasem].\n";
object oNoRefs = miRefs.Invoke(null, new object[] {sBrackets});
Ok(Count(oNoRefs) == 0, "zwykly nawias kwadratowy nie jest przypisem", "policzono " + Count(oNoRefs));

// ---------- 5. NUMERACJA AUTOMATYCZNA ----------
Ok((string) miNext.Invoke(null, new object[] {""}) == "1", "pusty dokument -> numer 1", "inny");
Ok((string) miNext.Invoke(null, new object[] {sDoc}) == "3", "po 1 i 2 -> numer 3", (string) miNext.Invoke(null, new object[] {sDoc}));
// Numer liczymy po NAJWYZSZYM, nie po liczbie przypisow: wstawienie w srodku
// nie moze wyprodukowac duplikatu etykiety.
string sGap = "A[^1] B[^7].\n\n[^1]: x\n[^7]: y\n";
Ok((string) miNext.Invoke(null, new object[] {sGap}) == "8", "dziura w numeracji -> najwyzszy plus jeden", (string) miNext.Invoke(null, new object[] {sGap}));
// Etykieta nieliczbowa nie moze wywrocic numeracji.
string sNamed = "A[^uwaga].\n\n[^uwaga]: x\n";
Ok((string) miNext.Invoke(null, new object[] {sNamed}) == "1", "etykieta slowna nie psuje numeracji", (string) miNext.Invoke(null, new object[] {sNamed}));

// ---------- 6. TRESC DOPISYWANA NA KONIEC ----------
// Pusta linia przed trescia jest WYMOGIEM Markdown: bez niej tresc wpadnie do
// ostatniego akapitu i przestanie byc przypisem.
string sB1 = (string) miBuild.Invoke(null, new object[] {"Ostatni akapit bez konca linii", "3", "Tresc"});
Ok(sB1.StartsWith("\n\n"), "brak konca linii -> dwie nowe linie", sB1.Replace("\n", "\\n"));
Ok(sB1.EndsWith("[^3]: Tresc\n"), "tresc w skladni markdownowej", sB1.Replace("\n", "\\n"));
string sB2 = (string) miBuild.Invoke(null, new object[] {"Akapit\n", "3", "Tresc"});
Ok(sB2.StartsWith("\n[^3]"), "jeden koniec linii -> jedna nowa linia", sB2.Replace("\n", "\\n"));
string sB3 = (string) miBuild.Invoke(null, new object[] {"Akapit\n\n", "3", "Tresc"});
Ok(sB3.StartsWith("[^3]"), "pusta linia jest -> nic nie dokladamy", sB3.Replace("\n", "\\n"));
string sB4 = (string) miBuild.Invoke(null, new object[] {"", "1", "Tresc"});
Ok(sB4 == "[^1]: Tresc\n", "pusty dokument -> sama tresc", sB4.Replace("\n", "\\n"));

// ---------- 7. ROZPOZNANIE WIERSZA Z TRESCIA ----------
object[] aArgs = new object[] {"[^1]: Tresc przypisu", null, null};
bool bIs = (bool) miIsDef.Invoke(null, aArgs);
Ok(bIs && (string) aArgs[1] == "1" && (string) aArgs[2] == "Tresc przypisu", "wiersz z trescia rozpoznany", "nie");
aArgs = new object[] {"   [^1]: Wciete trzy spacje", null, null};
Ok((bool) miIsDef.Invoke(null, aArgs), "wciecie do trzech spacji dozwolone", "nie");
aArgs = new object[] {"Zdanie z [^1] w srodku", null, null};
Ok(!(bool) miIsDef.Invoke(null, aArgs), "znacznik w zdaniu to NIE tresc", "uznane za tresc");
aArgs = new object[] {"[^1] bez dwukropka", null, null};
Ok(!(bool) miIsDef.Invoke(null, aArgs), "bez dwukropka to NIE tresc", "uznane za tresc");

// ---------- 8. ZNACZNIK POD KURSOREM ----------
// Ma dzialac, gdy kursor stoi gdziekolwiek w zdaniu ze znacznikiem, a nie
// dokladnie na nawiasie - inaczej funkcja byla bezuzyteczna w praktyce.
int iOn = sDoc.IndexOf("[^1]");
int iAt = (int) miAtIndex.Invoke(null, new object[] {oRefs, sDoc, iOn});
Ok(iAt == 0, "kursor NA znaczniku", "zwrocono " + iAt);
int iStartOfLine = sDoc.IndexOf("Zdanie z");
iAt = (int) miAtIndex.Invoke(null, new object[] {oRefs, sDoc, iStartOfLine});
Ok(iAt == 0, "kursor na POCZATKU wiersza -> pierwszy znacznik w wierszu", "zwrocono " + iAt);
int iEndOfLine = sDoc.IndexOf("drugim[^2]") + "drugim[^2].".Length;
iAt = (int) miAtIndex.Invoke(null, new object[] {oRefs, sDoc, iEndOfLine});
Ok(iAt == 1, "kursor na KONCU wiersza -> ostatni znacznik w wierszu", "zwrocono " + iAt);
int iOther = sDoc.IndexOf("Akapit bez przypisu");
iAt = (int) miAtIndex.Invoke(null, new object[] {oRefs, sDoc, iOther});
Ok(iAt < 0, "wiersz bez znacznika -> nic nie zgadujemy", "zwrocono " + iAt);

// ---------- 9. PAROWANIE ZNACZNIKA Z TRESCIA ----------
Ok((int) miByLabel.Invoke(null, new object[] {oDefs, "2"}) == 1, "tresc znaleziona po etykiecie", "nie");
Ok((int) miByLabel.Invoke(null, new object[] {oDefs, "9"}) == -1, "brak tresci -> minus jeden", "zle");
// Sierotki w obie strony: znacznik bez tresci i tresc bez znacznika. Program
// ma je odrozniac, bo dla kazdej mowi INNY komunikat.
string sOrphanRef = "Zdanie[^5].\n";
Ok(Count(miRefs.Invoke(null, new object[] {sOrphanRef})) == 1 && Count(miDefs.Invoke(null, new object[] {sOrphanRef})) == 0,
   "znacznik bez tresci widziany jako sierotka", "nie");
string sOrphanDef = "Zdanie bez znacznika.\n\n[^5]: Tresc sama.\n";
Ok(Count(miRefs.Invoke(null, new object[] {sOrphanDef})) == 0 && Count(miDefs.Invoke(null, new object[] {sOrphanDef})) == 1,
   "tresc bez znacznika widziana jako sierotka", "nie");

// ---------- 10. CRLF, czyli realne pliki Windows ----------
string sCrLf = "# Tytul\r\n\r\nZdanie[^1].\r\n\r\n[^1]: Tresc.\r\n";
Ok(Count(miRefs.Invoke(null, new object[] {sCrLf})) == 1, "CRLF: znacznik policzony", "nie");
object oCrDefs = miDefs.Invoke(null, new object[] {sCrLf});
Ok(Count(oCrDefs) == 1, "CRLF: tresc policzona", "policzono " + Count(oCrDefs));
if (Count(oCrDefs) == 1) Ok(TextOf(Item(oCrDefs, 0)) == "Tresc.", "CRLF: tresc bez ogona karetki", "[" + TextOf(Item(oCrDefs, 0)) + "]");

// ---------- 11. HELPER WIERSZA: on obsluguje TEZ spis tresci ----------
// Poprawka "kursor na koncu wiersza" siedzi w GetTextLineAtIndex, wspolnym z
// nawigacja po spisie tresci (Shift+F6).  Wiec mierzymy go OSOBNO, razem z
// kontrola, ze nie zaczal zwracac zlego wiersza w pozostalych przypadkach.
MethodInfo miLine = M("GetTextLineAtIndex");
string sL = "pierwszy\ndrugi\ntrzeci\n";
Ok((string) miLine.Invoke(null, new object[] {sL, 0}) == "pierwszy", "wiersz na indeksie 0", "zle");
Ok((string) miLine.Invoke(null, new object[] {sL, 3}) == "pierwszy", "wiersz w srodku pierwszego", "zle");
Ok((string) miLine.Invoke(null, new object[] {sL, 8}) == "pierwszy", "KONIEC wiersza nadal ten wiersz", "[" + (string) miLine.Invoke(null, new object[] {sL, 8}) + "]");
Ok((string) miLine.Invoke(null, new object[] {sL, 9}) == "drugi", "poczatek drugiego wiersza", "[" + (string) miLine.Invoke(null, new object[] {sL, 9}) + "]");
Ok((string) miLine.Invoke(null, new object[] {sL, 14}) == "drugi", "koniec drugiego wiersza", "[" + (string) miLine.Invoke(null, new object[] {sL, 14}) + "]");
Ok((string) miLine.Invoke(null, new object[] {sL, 15}) == "trzeci", "poczatek trzeciego wiersza", "[" + (string) miLine.Invoke(null, new object[] {sL, 15}) + "]");
string sLc = "pierwszy\r\ndrugi\r\n";
Ok((string) miLine.Invoke(null, new object[] {sLc, 8}) == "pierwszy", "CRLF: koniec wiersza (na karetce)", "[" + (string) miLine.Invoke(null, new object[] {sLc, 8}) + "]");
Ok((string) miLine.Invoke(null, new object[] {sLc, 10}) == "drugi", "CRLF: poczatek nastepnego", "[" + (string) miLine.Invoke(null, new object[] {sLc, 10}) + "]");

// SPIS TRESCI, ta sama poprawka: pozycja spisu ma byc rozpoznana takze gdy
// kursor stoi na jej koncu.  To regresja, ktora poprawka usuwa - mierzona na
// realnej metodzie spisu, nie na kopii.
MethodInfo miEntry = M("IsMarkdownContentsEntryLine");
string sContents = "# Contents\n\n- [Wstep](#wstep)\n\n# Wstep\n";
int iEntryEnd = sContents.IndexOf("(#wstep)") + "(#wstep)".Length;
Ok((bool) miEntry.Invoke(null, new object[] {(string) miLine.Invoke(null, new object[] {sContents, iEntryEnd})}),
   "spis tresci: pozycja rozpoznana z kursorem na jej KONCU", "nie rozpoznana");

Console.WriteLine();
Console.WriteLine("PASS: " + iPass + "   FAIL: " + iFail);
return (iFail == 0) ? 0 : 1;
} // Main

} // PomiarPrzypisow class
