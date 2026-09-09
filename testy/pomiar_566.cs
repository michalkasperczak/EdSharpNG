// Sonda 5.0.66 na ZBUDOWANEJ binarce (refleksja + IL + napisy w .exe).
//
// CO MIERZY - dwie decyzje Kasperczaka:
//
//   A. GUARD JAKO JEDEN PRZELACZNIK NA Control+F7, Control+Shift+F7 WOLNY.
//      Jego slowa 03.09.2026: "Ja bym dal wspolny skrot Ctrl-F7 wlacz/wylacz
//      zabezpieczenie i tyle, a z shiftem bedzie wolny".  Komenda No Guard
//      znika ze WSZYSTKICH warstw, a Guard Document przelacza.
//   B. Control+O NA PLIKU .RTF DAJE LISTE WARIANTOW, nie pytanie tak-nie
//      (ustalenie edsharpng-106, wariant B: "bardziej spojne").
//
// DLACZEGO NIE WYSTARCZA PYTAC O NAPISY: literal siedzi w binarce takze wtedy,
// gdy nikt go nie wypowiada, a metoda moze istniec i nie byc wolana - ta
// pulapka przepuscila trzy z czterech podstawionych bledow przy pierwszej
// wersji sondy 5.0.65.  Dlatego przy KAZDEJ asercji o zachowaniu pytamy o
// CIALO konkretnej metody (opkod ldstr / call), a nie o obecnosc napisu.
//
// PULAPKA PROGU (lekcja z 5.0.64, najwazniejsza w tym projekcie): pytanie "co
// najmniej N" przepuszcza jedno zapomniane miejsce.  Chord zdejmowany
// mierzymy DOKLADNA LICZBA wystapien, bo zbior jest zamkniety.
//
// PULAPKA WYROWNANIA: dekodowanie UTF-16 tylko od bajtu ZEROWEGO gubi kazdy
// literal na NIEPARZYSTYM offsecie.  Sonda pyta o oba wyrownania plus Latin1.
//
// KONTROLA WAZNOSCI: na binarce 5.0.65 ta sonda MUSI skonczyc kodem 3, bo tam
// pole MdiFrame.iOpenRichText jeszcze nie istnieje.
//
// Uzycie:
//   csc.exe /out:pomiar_566.exe pomiar_566.cs
//   pomiar_566.exe <sciezka do EdSharpNG.exe>

using System;
using System.IO;
using System.Reflection;
using System.Text;

class Pomiar566 {

static int iOk = 0;
static int iZle = 0;

static void Ok(string s) {iOk++; Console.WriteLine("OK: " + s);}
static void Zle(string s) {iZle++; Console.WriteLine("ZLE: " + s);}
static void Sprawdz(bool b, string s) {if (b) Ok(s); else Zle(s);}

static bool MaNapis(byte[] ab, string s) {
if (Encoding.Unicode.GetString(ab).Contains(s)) return true;
if (ab.Length > 1) {
byte[] abShift = new byte[ab.Length - 1];
Array.Copy(ab, 1, abShift, 0, abShift.Length);
if (Encoding.Unicode.GetString(abShift).Contains(s)) return true;
}
return Encoding.GetEncoding(28591).GetString(ab).Contains(s);
}

static int LiczNapis(byte[] ab, string s) {
int iSuma = 0;
iSuma += Zlicz(Encoding.Unicode.GetString(ab), s);
if (ab.Length > 1) {
byte[] abShift = new byte[ab.Length - 1];
Array.Copy(ab, 1, abShift, 0, abShift.Length);
iSuma += Zlicz(Encoding.Unicode.GetString(abShift), s);
}
iSuma += Zlicz(Encoding.GetEncoding(28591).GetString(ab), s);
return iSuma;
}

static int Zlicz(string sHay, string sNeedle) {
int i = 0, iOd = 0;
while (true) {
int iAt = sHay.IndexOf(sNeedle, iOd, StringComparison.Ordinal);
if (iAt < 0) break;
i++;
iOd = iAt + 1;
}
return i;
}

static MethodBase[] ZbierzMetody(Type t) {
System.Collections.Generic.List<MethodBase> ls = new System.Collections.Generic.List<MethodBase>();
BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
try {foreach (MethodInfo mi in t.GetMethods(bf)) ls.Add(mi);} catch {}
try {foreach (ConstructorInfo ci in t.GetConstructors(bf)) ls.Add(ci);} catch {}
return ls.ToArray();
}

// CZY CIALO TEJ METODY LADUJE TEN LITERAL (opkod ldstr = 0x72).
static bool MaLiteral(MethodBase mb, string sLiteral) {
byte[] abIl = null;
try {
MethodBody body = mb.GetMethodBody();
if (body == null) return false;
abIl = body.GetILAsByteArray();
}
catch {return false;}
if (abIl == null) return false;
for (int i = 0; i + 4 < abIl.Length; i++) {
if (abIl[i] != 0x72) continue;
int iToken = BitConverter.ToInt32(abIl, i + 1);
try {
string s = mb.Module.ResolveString(iToken);
if (s != null && s == sLiteral) return true;
}
catch {}
}
return false;
}

// CZY CIALO TEJ METODY WOLA METODE O TEJ NAZWIE (call/callvirt/newobj).
static bool WolaMetode(MethodBase mb, string sNazwaWolanej) {
byte[] abIl = null;
try {
MethodBody body = mb.GetMethodBody();
if (body == null) return false;
abIl = body.GetILAsByteArray();
}
catch {return false;}
if (abIl == null) return false;
for (int i = 0; i + 4 < abIl.Length; i++) {
byte b = abIl[i];
if (b != 0x28 && b != 0x6F && b != 0x73) continue;
int iToken = BitConverter.ToInt32(abIl, i + 1);
try {
MethodBase mbCalled = mb.Module.ResolveMethod(iToken);
if (mbCalled != null && mbCalled.Name == sNazwaWolanej) return true;
}
catch {}
}
return false;
}

static int LiczWolania(Assembly asm, string sNazwaWolanej) {
int iIle = 0;
foreach (Type t in asm.GetTypes())
foreach (MethodBase mb in ZbierzMetody(t))
if (WolaMetode(mb, sNazwaWolanej)) iIle++;
return iIle;
}

static int Main(string[] args) {
if (args.Length < 1) {
Console.WriteLine("Uzycie: pomiar_566.exe <sciezka do EdSharpNG.exe>");
return 2;
}
string sPath = args[0];
if (!File.Exists(sPath)) {
Console.WriteLine("BLAD: nie ma pliku " + sPath);
return 2;
}

byte[] ab = File.ReadAllBytes(sPath);
Assembly asm = Assembly.LoadFrom(sPath);

Type tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {
Console.WriteLine("BLAD: nie ma typu EdSharp.MdiFrame - sonda nie ma czego mierzyc.");
return 2;
}

// KONTROLA WAZNOSCI SONDY W JEDNYM Z ASERCJA NOSNA: stalej iOpenRichText nie
// ma w zadnej wczesniejszej wersji, bo powstala na potrzeby listy wariantow.
FieldInfo fiRich = tFrame.GetField("iOpenRichText",
BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
if (fiRich == null) {
Console.WriteLine("ZLE: stalej MdiFrame.iOpenRichText NIE MA w binarce.");
Console.WriteLine("To jest kontrola waznosci sondy: na binarce 5.0.65 i starszej");
Console.WriteLine("ten wynik jest OCZEKIWANY, bo listy wariantow otwarcia .rtf tam nie ma.");
return 3;
}
Ok("stala MdiFrame.iOpenRichText istnieje (refleksja)");
Sprawdz(fiRich.FieldType == typeof(int) && ((int) fiRich.GetRawConstantValue()) == -2,
"iOpenRichText ma wartosc -2, czyli NIE koliduje z 0, 1, 2 ani -1 uzywanymi przez konwersje");

MethodInfo miClick = tFrame.GetMethod("menuItem_Click",
BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
Sprawdz(miClick != null, "metoda MdiFrame.menuItem_Click dostepna refleksji (nosnik obu zmian)");

Console.WriteLine();
Console.WriteLine("--- A. GUARD: JEDEN PRZELACZNIK, CONTROL+SHIFT+F7 WOLNY");

// DOKLADNA LICZBA, nie prog.  Zaden inny chord nie zawiera "Control+Shift+F7"
// jako podnapisu, wiec zbior jest zamkniety: po zmianie musi byc ZERO.
int iCSF7 = LiczNapis(ab, "Control+Shift+F7");
Sprawdz(iCSF7 == 0,
"napis chordu \"Control+Shift+F7\" wystepuje DOKLADNIE 0 razy (zmierzone: " + iCSF7 + ")");
Sprawdz(!MaNapis(ab, "No Guard"),
"pozycja menu \"No Guard\" ZNIKNELA z binarki");
// Komenda ZOSTAJE, tylko przelacza: chord i nazwa musza byc.
int iCF7 = LiczNapis(ab, "Control+F7");
Sprawdz(iCF7 >= 1,
"chord Control+F7 NADAL jest w binarce (zmierzone: " + iCF7 + ")");
Sprawdz(MaNapis(ab, "Guard Document"),
"pozycja menu Guard Document ZOSTAJE");
// SASIEDZI TEJ SAMEJ RODZINY KLAWISZA F7 NIETKNIETI - kontrola, ze nie
// zdjelismy calej rodziny, tylko jeden chord.
Sprawdz(MaNapis(ab, "Shift+F7"),
"sasiad Shift+F7 (Thesaurus) nietkniety");
Sprawdz(MaNapis(ab, "Alt+Shift+F7"),
"sasiad Alt+Shift+F7 (Translate Language) nietkniety");

if (miClick != null) {
// PRZELACZNIK MUSI MOWIC OBA KIERUNKI, i to Z TEJ METODY.
Sprawdz(MaLiteral(miClick, "Guard on"),
"menuItem_Click laduje \"Guard on\" (przelacznik wlacza ochrone)");
Sprawdz(MaLiteral(miClick, "Guard off"),
"menuItem_Click laduje \"Guard off\" (ten sam klawisz ja zdejmuje)");
// KOMUNIKATY DWOCH KOMEND JEDNOKIERUNKOWYCH MUSZA ZNIKNAC: przy
// przelaczniku "Already guarded" i "Not guarded" sa nieosiagalne, wiec
// zostawienie ich w kodzie znaczylo by, ze stara galaz nadal zyje.
Sprawdz(!MaLiteral(miClick, "Already guarded"),
"menuItem_Click NIE laduje juz \"Already guarded\" (galaz komendy jednokierunkowej zniknela)");
Sprawdz(!MaLiteral(miClick, "Not guarded"),
"menuItem_Click NIE laduje juz \"Not guarded\"");
// STAN Z GetUserGuard, NIE Z rtb.ReadOnly.  To jest asercja o CICHYM
// BLEDZIE: podglad Markdown wlacza ochrone na czas podgladu, wiec surowy
// odczyt kontrolki kazalby przelacznikowi zdjac ochrone, ktorej user nigdy
// nie wlaczyl - i utrwalic to w pliku ustawien.
Sprawdz(WolaMetode(miClick, "GetUserGuard"),
"menuItem_Click WOLA GetUserGuard - stan brany ze zrodla prawdy, nie z rtb.ReadOnly");
Sprawdz(MaLiteral(miClick, "Close the preview first!"),
"menuItem_Click laduje bramke podgladu (\"Close the preview first!\")");
}

// TRWALOSC FLAGI NIETKNIETA: przelacznik to jedna sprawa, zapis druga.
int iWolSave = LiczWolania(asm, "SaveGuardFlag");
Sprawdz(iWolSave >= 1,
"SaveGuardFlag nadal jest WOLANA (trwalosc flagi guardu, wolan: " + iWolSave + ")");
try {
Type tRtb = asm.GetType("EdSharp.HomerRichTextBox");
MethodInfo miSetGuard = (tRtb == null) ? null : tRtb.GetMethod("SetGuard",
BindingFlags.Public | BindingFlags.Instance);
Sprawdz(miSetGuard != null && miSetGuard.ReturnType == typeof(bool),
"HomerRichTextBox.SetGuard nadal zwraca bool");
}
catch (Exception ex) {Zle("odczyt SetGuard rzucil wyjatek: " + ex.Message);}

Console.WriteLine();
Console.WriteLine("--- B. CONTROL+O NA .RTF: LISTA WARIANTOW ZAMIAST PYTANIA TAK-NIE");

// CZTERY POZYCJE LISTY, KAZDA OSOBNO.  Pytamy o CIALO menuItem_Click, bo to
// tam siedzi galaz Control+O; sam napis w binarce nie mowi, kto go uzywa.
if (miClick != null) {
Sprawdz(MaLiteral(miClick, "Open RTF File As"),
"menuItem_Click laduje tytul listy \"Open RTF File As\"");
Sprawdz(MaLiteral(miClick, "Convert to Markdown"),
"pozycja \"Convert to Markdown\" jest w ciele Control+O");
Sprawdz(MaLiteral(miClick, "Open as rich text, keeping formatting"),
"pozycja \"Open as rich text, keeping formatting\" jest w ciele Control+O");
Sprawdz(MaLiteral(miClick, "Open as plain text, showing RTF source"),
"pozycja \"Open as plain text, showing RTF source\" jest w ciele Control+O");
Sprawdz(MaLiteral(miClick, "Other conversion ..."),
"pozycja \"Other conversion ...\" prowadzi do tabeli Import");
Sprawdz(MaLiteral(miClick, "rtf2md"),
"klucz tabeli Import \"rtf2md\" podany WPROST - nie pytamy o format drugi raz");
// LISTA, NIE MessageBox: Dialog.Pick musi byc wolany z tego ciala.
Sprawdz(WolaMetode(miClick, "Pick"),
"menuItem_Click WOLA Dialog.Pick, czyli pokazuje LISTE");
}

// KLUCZ rtf2md MUSI ISTNIEC W TABELI IMPORT - inaczej wariant "Convert to
// Markdown" wskazuje na nic i konwersja cicho nie zajdzie.  Mierzymy PLIK
// ustawien programu, nie kod.
string sIni = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sPath)), "EdSharp.ini");
if (!File.Exists(sIni)) Zle("nie ma EdSharp.ini obok binarki - nie moge sprawdzic klucza rtf2md");
else {
string sIniText = File.ReadAllText(sIni, Encoding.Default);
Sprawdz(sIniText.Contains("\nrtf2md="),
"tabela Import w EdSharp.ini MA klucz rtf2md (wariant Markdown ma czym konwertowac)");
Sprawdz(sIniText.Contains("pandoc.exe") && sIniText.Contains("-t gfm"),
"konwersja rtf2md idzie przez pandoc do gfm");
// MARTWY WPIS W SEKCJI Keys: usuniecie komendy nie usuwa jej wpisu w
// ustawieniach (lekcja z 5.0.44, osierocony NavigatePart).
Sprawdz(!sIniText.Contains("No Guard="),
"sekcja Keys w EdSharp.ini NIE MA juz martwego wpisu \"No Guard\"");
}

// PYTANIE TAK-NIE ZOSTAJE TAM, GDZIE JEST NA MIEJSCU: Paste File nadal pyta
// (to inna komenda i on jej nie dotyczyl).  Kontrola, ze nie zamienilismy
// wszystkich wystapien hurtem.
int iTreat = LiczNapis(ab, "Treat as rich text?");
Sprawdz(iTreat >= 1,
"napis \"Treat as rich text?\" nadal JEST - Paste File niedotknieta (zmierzone: " + iTreat + ")");
bool bPasteNadalPyta = false;
if (miClick != null) bPasteNadalPyta = MaLiteral(miClick, "Treat as rich text?");
Sprawdz(bPasteNadalPyta,
"menuItem_Click nadal laduje \"Treat as rich text?\" dla Paste File (zmiana jest WEZLOWA, nie hurtowa)");

// OVERLOAD Z KLUCZEM IMPORT MUSI ISTNIEC I MIEC PIEC ARGUMENTOW.
MethodInfo miOpen5 = tFrame.GetMethod("OpenOrActivateWindow",
BindingFlags.Public | BindingFlags.Instance, null,
new Type[] {typeof(string), typeof(int), typeof(string), typeof(string), typeof(string)}, null);
Sprawdz(miOpen5 != null,
"OpenOrActivateWindow ma wersje z kluczem Import (piec argumentow)");
MethodInfo miOpen4 = tFrame.GetMethod("OpenOrActivateWindow",
BindingFlags.Public | BindingFlags.Instance, null,
new Type[] {typeof(string), typeof(int), typeof(string), typeof(string)}, null);
Sprawdz(miOpen4 != null,
"stara wersja z czterema argumentami ZOSTAJE - trzy inne miejsca w kodzie ja wolaja");
// KONWERTER MUSI PRZYJAC TEN KLUCZ, inaczej stala by sie martwym argumentem.
Type tCom = asm.GetType("EdSharp.COM");
MethodInfo miConv5 = (tCom == null) ? null : tCom.GetMethod("ConvertFile2String",
BindingFlags.Public | BindingFlags.Static, null,
new Type[] {typeof(string), typeof(int).MakeByRefType(), typeof(string).MakeByRefType(), typeof(bool), typeof(string)}, null);
Sprawdz(miConv5 != null,
"COM.ConvertFile2String przyjmuje klucz Import (piec argumentow)");
if (miOpen5 != null) Sprawdz(WolaMetode(miOpen5, "ConvertFile2String"),
"wersja piecioargumentowa REALNIE wola konwerter (klucz nie ginie w drodze)");

Console.WriteLine();
Console.WriteLine("--- C. WERSJE POPRZEDNIE NIETKNIETE (regresja)");
Type tApp = asm.GetType("EdSharp.App");
Sprawdz(tApp != null && tApp.GetMethod("ClearExtraSpeechOption", BindingFlags.Public | BindingFlags.Static) != null,
"5.0.65 nietknieta: App.ClearExtraSpeechOption nadal jest");
Sprawdz(MaNapis(ab, "Regular Expression Tool"),
"5.0.65 nietknieta: polaczona komenda Regular Expression Tool nadal jest");
Sprawdz(LiczNapis(ab, "Control+Shift+H") == 0,
"5.0.65 nietknieta: Control+Shift+H nadal zwolniony");
Sprawdz(tApp != null && tApp.GetMethod("MigrateBookmarksOutOfFavorites", BindingFlags.Public | BindingFlags.Static) != null,
"5.0.64 nietknieta: migracja zakladek z ulubionych nadal jest");
Sprawdz(MaNapis(ab, "BookmarksSplit"),
"5.0.64 nietknieta: znacznik BookmarksSplit nadal jest");
Sprawdz(!MaNapis(ab, "cleared too"),
"5.0.64 nietknieta: nieprawdziwy komunikat o kasowaniu zakladek nadal nie istnieje");
Sprawdz(MaNapis(ab, "Control+Alt+F9"),
"5.0.63 nietknieta: lista komentarzy nadal na Control+Alt+F9");
Sprawdz(MaNapis(ab, "Saving as plain text, not rich text"),
"5.0.62 nietknieta: ostrzezenie o zapisie tekstem w pliku .rtf nadal jest");
Type tChild = asm.GetType("EdSharp.MdiChild");
Sprawdz(tChild != null && tChild.GetField("IsRichTextDocument",
BindingFlags.Public | BindingFlags.Instance) != null,
"5.0.62 nietknieta: pole IsRichTextDocument (prowieniencja dokumentu) nadal jest");

Console.WriteLine();
Console.WriteLine("PODSUMOWANIE: " + iOk + " OK / " + iZle + " ZLE");
return (iZle == 0) ? 0 : 1;
}

} // Pomiar566 class
