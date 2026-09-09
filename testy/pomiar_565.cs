// Sonda 5.0.65 na ZBUDOWANEJ binarce (refleksja + napisy w .exe).
//
// UWAGA: PIEC ASERCJI TEJ SONDY JEST PRZESTARZALYCH DECYZJA CZLOWIEKA.
// Zmierzone 05.09.2026: ta sonda daje 53 OK / 5 ZLE i daje DOKLADNIE TEN SAM
// wynik na binarce 5.0.67 (rewizja 0ab8a5e) oraz na kazdej nowszej - czyli NIE
// jest to regresja, tylko asercje opisujace swiat sprzed pozniejszych decyzji:
//   - "Already guarded" i "Not guarded": Kasperczak zdecydowal 04.09, ze Guard
//     to JEDEN przelacznik na Control+F7 (ustalenie edsharpng-114), wiec te dwa
//     komunikaty z pary komend juz nie istnieja,
//   - "chord Control+Shift+F7 NADAL jest w binarce" i "pozycja menu No Guard
//     nietknieta": ta sama decyzja zdjela komende No Guard calkiem, a asercja
//     zostala napisana pod stan, w ktorym miala jeszcze byc,
//   - "wiersz listy rzucil wyjatek: niezgodnosc liczby parametrow": sygnatura
//     GetMarkdownLinkAddressSpeech dostala DRUGI parametr w 5.0.67 (numer
//     wiersza pod strzalka w lewo), a ta sonda wola ja po staremu.
// Aktualny stan tych spraw mierza pomiar_566 (Guard jako przelacznik) i
// pomiar_567 (wiersz listy odsylaczy), oba 0 ZLE.  NIE naprawiaj tu niczego w
// kodzie programu na podstawie tych piatki - najpierw sprawdz wynik na
// binarce odniesienia.
//
// CO MIERZY - piec decyzji Kasperczaka z 03.09.2026 (jego plik odpowiedzi):
//
//   A. Control+Shift+H (Hard Line Break) SCHODZI DO MENU bez skrotu, litera H
//      zwolniona.  Wariant B, jego slowa: "Chyba lepsze".
//   B. Extra Speech Toggle USUNIETY CALKIEM razem z wpisem w pliku ustawien.
//      Wariant B, jego slowa: "Tak.  Usunac.  To bylo glownie pod Jaws".
//   C. Extract i Yield with Regular Expression POLACZONE w jedna komende z
//      wyborem dzialania w okienku.  Wariant B: "Odpowiedz: B".
//   D. Wiersz listy odsylaczy niesie SAM TYTUL, adres dopowiada strzalka w lewo.
//      Wariant B.
//   E. Lista odsylaczy parsuje TEZ adresy e-mail (jego wariant A z dopiskiem:
//      "parsujac linki, adresy stron mailowe itp.").
//
// DLACZEGO NAPISY, A NIE TYLKO REFLEKSJA: chord, nazwa pozycji menu i komunikat
// mowy sa literalami, wiec siedza w napisach .exe dokladnie wtedy, gdy kod ich
// uzywa.  Refleksja pokaze istnienie metody, ale NIE pokaze, ze chord zszedl.
//
// KONTROLA WAZNOSCI: ta sama sonda na binarce 5.0.64 MUSI skonczyc kodem 3, bo
// tam metody App.ClearExtraSpeechOption NIE MA.
//
// PULAPKA, KTORA TA SONDA OMIJA (lekcja z 5.0.64, najwazniejsza w tym projekcie):
// pytanie o PROG ("co najmniej N uzyc") przepuszcza jedno zapomniane miejsce.
// Dlatego przy zdjeciu chordu pytamy o DOKLADNA LICZBE wystapien napisu chordu w
// binarce, a nie o to, czy jest ich "malo".  Zbior jest zamkniety i wyliczalny:
// Control+Shift+H po zmianie NIE MOZE wystapic ANI RAZU, bo zaden inny chord go
// nie zawiera jako podnapisu.
//
// PULAPKA WYROWNANIA (lekcja z tej samej iteracji): dekodowanie UTF-16 tylko od
// bajtu ZEROWEGO gubi kazdy literal na NIEPARZYSTYM offsecie, a tak lezy
// wiekszosc napisow.  Sonda pyta o OBA wyrownania plus Latin1.
//
// Uzycie:
//   csc.exe /out:pomiar_565.exe pomiar_565.cs
//   pomiar_565.exe <sciezka do EdSharpNG.exe>

using System;
using System.IO;
using System.Reflection;
using System.Text;

class Pomiar565 {

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
string sLat = Encoding.GetEncoding(28591).GetString(ab);
return sLat.Contains(s);
}

// DOKLADNA LICZBA wystapien napisu we WSZYSTKICH trzech odczytach razem.
// Nie prog, nie "czy jest" - liczba, bo tylko ona lapie jedno zapomniane
// miejsce przy zdejmowaniu chordu.
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

// ILE METOD BINARKI REALNIE WOLA METODE O TEJ NAZWIE.
//
// PO CO TO ISTNIEJE - NAJWAZNIEJSZY WYNIK KONTROLI GLUCHOTY 03.09.2026:
// pierwsza wersja tej sondy pytala o napisy i ISTNIENIE metod, i przepuscila
// TRZY z czterech podstawionych bledow.  Zakomentowanie wolania
// ClearExtraSpeechOption dalo zielone 37/0, bo metoda nadal ISTNIALA - tylko
// nikt jej nie uzywal.  Skutek u uzytkownika bylby dokladnie taki, przed jakim
// sie zabezpieczalem: kto ma mowe wylaczona na dysku, ten zostaje z cisza.
//
// Dlatego czytamy CIALO metod (IL) i rozwiazujemy tokeny wywolan.  Metoda,
// ktora istnieje, ale nikt jej nie wola, jest tu widoczna jako zero.
static int LiczWolania(Assembly asm, string sNazwaWolanej) {
int iIle = 0;
foreach (Type t in asm.GetTypes()) {
MethodBase[] aAll = ZbierzMetody(t);
foreach (MethodBase mb in aAll) {
if (WolaMetode(asm, mb, sNazwaWolanej)) iIle++;
}
}
return iIle;
}

static MethodBase[] ZbierzMetody(Type t) {
System.Collections.Generic.List<MethodBase> ls = new System.Collections.Generic.List<MethodBase>();
BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
try {foreach (MethodInfo mi in t.GetMethods(bf)) ls.Add(mi);} catch {}
try {foreach (ConstructorInfo ci in t.GetConstructors(bf)) ls.Add(ci);} catch {}
return ls.ToArray();
}

static bool WolaMetode(Assembly asm, MethodBase mb, string sNazwaWolanej) {
byte[] abIl = null;
try {
MethodBody body = mb.GetMethodBody();
if (body == null) return false;
abIl = body.GetILAsByteArray();
}
catch {return false;}
if (abIl == null) return false;

// call = 0x28, callvirt = 0x6F, newobj = 0x73.  Za opkodem stoi 4-bajtowy
// token metody.  Nie parsujemy calego IL co do bajtu: skanujemy pozycje i
// probujemy rozwiazac token - zly token rzuca wyjatek i jest pomijany.  Dla
// pytania "czy ta metoda jest gdzies wolana" to wystarcza, a falszywe
// trafienie jest praktycznie niemozliwe, bo token musi rozwiazac sie na
// metode o DOKLADNIE tej nazwie.
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

// CZY CIALO TEJ KONKRETNEJ METODY LADUJE TEN LITERAL.
//
// Po co osobno od LiczNapis: kompilator SCALA identyczne literaly, wiec sam
// napis w binarce nie mowi, KTORA metoda go uzywa (zmierzone 03.09.2026 -
// zakomentowanie dopisywania schematu mailto zostawialo napis nietkniety, bo
// ten sam literal jest w normalizacji adresow).  Opkod ldstr = 0x72, za nim
// 4-bajtowy token napisu rozwiazywany przez ResolveString.
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

static int Main(string[] args) {
if (args.Length < 1) {
Console.WriteLine("Uzycie: pomiar_565.exe <sciezka do EdSharpNG.exe>");
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

// ASERCJA NOSNA I KONTROLA WAZNOSCI W JEDNYM: metody czyszczacej wpis
// ExtraSpeech nie ma w zadnej wczesniejszej wersji.
MethodInfo miClear = tApp.GetMethod("ClearExtraSpeechOption",
BindingFlags.Public | BindingFlags.Static);
if (miClear == null) {
Console.WriteLine("ZLE: metody App.ClearExtraSpeechOption NIE MA w binarce.");
Console.WriteLine("To jest kontrola waznosci sondy: na binarce 5.0.64 i starszej");
Console.WriteLine("ten wynik jest OCZEKIWANY, bo usuniecia przelacznika mowy tam nie ma.");
return 3;
}
Ok("metoda App.ClearExtraSpeechOption istnieje (refleksja)");

Console.WriteLine();
Console.WriteLine("--- A. HARD LINE BREAK: CHORD ZDJETY, KOMENDA ZOSTAJE");
// DOKLADNA LICZBA, nie prog.  Zaden inny chord nie zawiera "Control+Shift+H"
// jako podnapisu, wiec zbior jest zamkniety: po zmianie musi byc ZERO.
int iH = LiczNapis(ab, "Control+Shift+H");
Sprawdz(iH == 0,
"napis chordu \"Control+Shift+H\" wystepuje DOKLADNIE 0 razy (zmierzone: " + iH + ")");
Sprawdz(MaNapis(ab, "Hard Line Break"),
"sama komenda Hard Line Break ZOSTAJE (napis pozycji menu jest w binarce)");
// Kontrola, ze nie zabralismy calej rodziny Control+Shift: sasiedzi zyja.
Sprawdz(MaNapis(ab, "Control+Shift+J"),
"sasiad Control+Shift+J (Join Lines) nietknietiy - nie zdjelismy calej rodziny");

Console.WriteLine();
Console.WriteLine("--- B. EXTRA SPEECH TOGGLE USUNIETY, DZIENNIK MOWY ZOSTAJE");
Sprawdz(!MaNapis(ab, "Extra Speech Toggle"),
"pozycja menu \"Extra Speech Toggle\" ZNIKNELA z binarki");
Sprawdz(MaNapis(ab, "Extra Speech Log"),
"dziennik mowy \"Extra Speech Log\" ZOSTAJE - to osobna funkcja");
Sprawdz(MaNapis(ab, "Alt+Shift+X"),
"chord dziennika mowy Alt+Shift+X nietknietiy");
Sprawdz(MaNapis(ab, "ExtraSpeechDropped"),
"znacznik jednorazowego czyszczenia \"ExtraSpeechDropped\" jest w binarce");
// TO JEST NAJWAZNIEJSZA ASERCJA CALEJ SONDY.  Bez czyszczenia wpisu user,
// ktory mial mowe wylaczona, zostalby z cisza NA STALE i bez wlacznika.
Sprawdz(MaNapis(ab, "E&xtraSpeech"),
"klucz pliku ustawien \"E&xtraSpeech\" nadal jest w kodzie - czyli MAMY CO czyscic");
Sprawdz(MaNapis(ab, "Speech.log"),
"plik dziennika mowy nadal jest uzywany");
// ASERCJA, KTORA ZLAPALA BLAD PRZEPUSZCZONY PRZEZ PIERWSZA WERSJE SONDY:
// metoda moze ISTNIEC i nie byc wolana.  Wtedy klucz nie schodzi z dysku i
// user z wylaczona mowa zostaje z cisza - dokladnie to, przed czym mial
// chronic ten kod.
int iWolClear = LiczWolania(asm, "ClearExtraSpeechOption");
Sprawdz(iWolClear >= 1,
"ClearExtraSpeechOption jest REALNIE WOLANA w kodzie (miejsc: " + iWolClear + ")");

Console.WriteLine();
Console.WriteLine("--- C. DWIE KOMENDY WYRAZEN REGULARNYCH POLACZONE W JEDNA");
Sprawdz(MaNapis(ab, "Regular Expression Tool"),
"nowa jedna pozycja menu \"Regular Expression Tool\" jest w binarce");
Sprawdz(MaNapis(ab, "Control+Shift+Y"),
"chord Control+Shift+Y ZOSTAJE przy polaczonej komendzie");
Sprawdz(!MaNapis(ab, "Extract with Regular Expression ..."),
"stara pozycja menu \"Extract with Regular Expression ...\" ZNIKNELA");
Sprawdz(!MaNapis(ab, "Yield with Regular Expression ..."),
"stara pozycja menu \"Yield with Regular Expression ...\" ZNIKNELA");
// FUNKCJA MUSI ZOSTAC, nie tylko pozycja menu zniknac: oba dzialania zyja
// dalej, tylko wybiera sie je w okienku.
Sprawdz(MaNapis(ab, "Count matches"),
"wybor \"Count matches\" w okienku istnieje (dzialanie liczenia nie zginelo)");
Sprawdz(MaNapis(ab, "Extract matches to a new window"),
"wybor wypisania trafien do nowego okna istnieje (dzialanie Extract nie zginelo)");
Sprawdz(MaNapis(ab, "Extract All with Regular Expression"),
"tytul okienka wzorca dla wypisywania nadal jest");
Sprawdz(MaNapis(ab, "Yield All with Regular Expression"),
"tytul okienka wzorca dla liczenia nadal jest");
// Kontrola, ze nie zabralismy bliskiej, ale INNEJ komendy.
Sprawdz(MaNapis(ab, "Control+Shift+R"),
"Replace with Regular Expression (Control+Shift+R) nietknieta");

Console.WriteLine();
Console.WriteLine("--- D. LISTA ODSYLACZY: SAM TYTUL W WIERSZU, ADRES PO STRZALCE");
Sprawdz(MaNapis(ab, "Left Arrow reads the web address"),
"podpowiedz okna mowi o strzalce w lewo czytajacej adres");
Sprawdz(MaNapis(ab, "Address only, "),
"komunikat dla odsylacza bez tresci (\"Address only\") jest w binarce");
Sprawdz(MaNapis(ab, "This link has no address"),
"komunikat dla odsylacza bez adresu jest w binarce");
MethodInfo miRow = null, miAddr = null;
foreach (Type t in asm.GetTypes()) {
if (miRow == null) miRow = t.GetMethod("GetMarkdownLinkListLine",
BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
if (miAddr == null) miAddr = t.GetMethod("GetMarkdownLinkAddressSpeech",
BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
}
Sprawdz(miRow != null,
"metoda GetMarkdownLinkListLine istnieje (refleksja) - wiersz liczony osobno od mowy");
Sprawdz(miAddr != null,
"metoda GetMarkdownLinkAddressSpeech istnieje (refleksja)");
// ISTNIENIE METODY NIE WYSTARCZA - drugi blad przepuszczony przez pierwsza
// wersje sondy.  Wiersz listy moze nadal iso przez STARA metode mowy, a nowa
// lezec nieuzywana; user slyszal by wtedy adres w kazdym wierszu, wbrew jego
// decyzji.  Pytamy o REALNE WOLANIE.
int iWolRow = LiczWolania(asm, "GetMarkdownLinkListLine");
Sprawdz(iWolRow >= 1,
"GetMarkdownLinkListLine jest REALNIE WOLANA (miejsc: " + iWolRow + ")");
int iWolAddr = LiczWolania(asm, "GetMarkdownLinkAddressSpeech");
Sprawdz(iWolAddr >= 1,
"GetMarkdownLinkAddressSpeech jest REALNIE WOLANA (miejsc: " + iWolAddr + ")");
// I POMIAR ZACHOWANIA, nie tylko obecnosci: wynik wiersza NIE MOZE zawierac
// adresu, gdy odsylacz ma tresc.  Wolamy metode na sztucznym odsylaczu przez
// refleksje, czyli mierzymy to, co uslyszy uzytkownik.
if (miRow != null) {
try {
Type tLink = null;
foreach (Type t in asm.GetTypes()) if (t.Name == "MarkdownLink") {tLink = t; break;}
if (tLink == null) Zle("nie ma typu MarkdownLink - nie moge zmierzyc wiersza zachowaniem");
else {
object oLink = Activator.CreateInstance(tLink, true);
tLink.GetField("Title", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(oLink, "Strona Kasperczaka");
tLink.GetField("Url", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(oLink, "https://przyklad.pl/bardzo/dluga/sciezka");
string sRow = (string) miRow.Invoke(null, new object[] {oLink});
Sprawdz(sRow == "Strona Kasperczaka",
"ZACHOWANIE: wiersz listy to DOKLADNIE tresc odsylacza, bez adresu (zwrocone: \"" + sRow + "\")");
if (miAddr != null) {
string sAddr = (string) miAddr.Invoke(null, new object[] {oLink});
Sprawdz(sAddr == "https://przyklad.pl/bardzo/dluga/sciezka",
"ZACHOWANIE: strzalka w lewo dopowiada SAM adres (zwrocone: \"" + sAddr + "\")");
}
}
}
catch (Exception ex) {Zle("pomiar zachowania wiersza listy rzucil wyjatek: " + ex.Message);}
}
Sprawdz(MaNapis(ab, "No links!"),
"5.0.52 nietknieta: pusta lista odsylaczy nadal mowi \"No links!\"");
Sprawdz(MaNapis(ab, "Copied as Markdown"),
"5.0.60 nietknieta: Control+C na liscie nadal kopiuje jako Markdown");

Console.WriteLine();
Console.WriteLine("--- E. ADRESY E-MAIL JAKO ODSYLACZE, TEZ W PLIKACH NIE-MARKDOWN");
Sprawdz(MaNapis(ab, "mailto:"),
"schemat mailto jest w binarce (kopiowanie z formatowaniem umie go dopisac)");
// NAPIS "mailto:" NIE ROZSTRZYGA - piaty podstawiony blad przeszedl na nim
// zielono, bo ten sam napis siedzi w normalizacji adresow od dawna.  Liczenie
// wystapien tez NIE dziala: zmierzone 03.09.2026, kompilator SCALA identyczne
// literaly, wiec dwa miejsca w kodzie daja JEDEN napis w binarce (asercja o
// dwoch wystapieniach oblewala na POPRAWNEJ binarce - falszywy alarm).
// Rozstrzyga IL: czy metoda kopiujaca z formatowaniem laduje ten literal.
bool bMailtoWKopiowaniu = false;
foreach (Type t in asm.GetTypes()) {
foreach (MethodBase mb in ZbierzMetody(t)) {
if (mb.Name != "CopyMarkdownLinkAsRichText") continue;
if (MaLiteral(mb, "mailto:")) {bMailtoWKopiowaniu = true; break;}
}
if (bMailtoWKopiowaniu) break;
}
Sprawdz(bMailtoWKopiowaniu,
"IL metody CopyMarkdownLinkAsRichText laduje literal \"mailto:\" - schemat REALNIE dopisywany");
MethodInfo miBare = null;
foreach (Type t in asm.GetTypes()) {
if (miBare == null) miBare = t.GetMethod("AddMarkdownBareLinksFromLine",
BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
}
Sprawdz(miBare != null,
"metoda AddMarkdownBareLinksFromLine istnieje (refleksja) - parser golych adresow zyje");
// Wzorzec adresu pocztowego jest polem statycznym klasy - pytamy refleksja,
// bo napis wyrazenia regularnego moze byc scalony przez kompilator.
FieldInfo fiMail = null;
foreach (Type t in asm.GetTypes()) {
if (fiMail == null) fiMail = t.GetField("MarkdownMailAddressRegex",
BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
}
Sprawdz(fiMail != null,
"pole MarkdownMailAddressRegex istnieje (refleksja) - wzorzec adresu pocztowego jest");
// Kontrola, ze stare trzy wzorce zyja: dolozylismy czwarty, nie podmienilismy.
FieldInfo fiWww = null, fiBareUrl = null, fiAuto = null;
foreach (Type t in asm.GetTypes()) {
if (fiWww == null) fiWww = t.GetField("MarkdownWwwUrlRegex", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
if (fiBareUrl == null) fiBareUrl = t.GetField("MarkdownBareUrlRegex", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
if (fiAuto == null) fiAuto = t.GetField("MarkdownAutoLinkRegex", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
}
Sprawdz(fiWww != null && fiBareUrl != null && fiAuto != null,
"trzy stare wzorce adresow (www, http, <http>) NADAL sa - dolozylismy czwarty");
// POMIAR ZACHOWANIA PARSERA, NIE OBECNOSCI WZORCA - trzeci blad przepuszczony
// przez pierwsza wersje sondy.  Wzorzec moze byc zadeklarowany i nie wejsc do
// tablicy parsera; adres pocztowy nie trafil by wtedy na liste, mimo ze pole
// istnieje.  Wolamy PRAWDZIWY parser GetMarkdownLinks na probce tekstu.
MethodInfo miLinks = null;
foreach (Type t in asm.GetTypes()) {
if (miLinks == null) miLinks = t.GetMethod("GetMarkdownLinks",
BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
}
if (miLinks == null) Zle("nie ma metody GetMarkdownLinks - nie moge zmierzyc parsera zachowaniem");
else {
try {
string sProbka = "Pisz na kasperczak@przyklad.pl w sprawie testow.\r\nStrona www.przyklad.pl tez dziala.\r\n";
object oList = miLinks.Invoke(null, new object[] {sProbka});
System.Collections.IEnumerable en = oList as System.Collections.IEnumerable;
int iMail = 0, iWww = 0, iRazem = 0;
if (en != null) foreach (object oL in en) {
iRazem++;
Type tL = oL.GetType();
string sUrl = (string) tL.GetField("Url", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(oL);
if (sUrl != null && sUrl.Contains("kasperczak@przyklad.pl")) iMail++;
if (sUrl != null && sUrl.Contains("przyklad.pl") && sUrl.IndexOf('@') < 0) iWww++;
}
Sprawdz(iMail == 1,
"ZACHOWANIE: parser znajduje DOKLADNIE JEDEN adres pocztowy w probce (znalazl: " + iMail + ")");
Sprawdz(iWww == 1,
"ZACHOWANIE: adres strony nadal znajdowany, bez zdublowania (znalazl: " + iWww + ")");
Sprawdz(iRazem == 2,
"ZACHOWANIE: razem DOKLADNIE dwa odsylacze, wiec zaden nie wchodzi dwa razy (razem: " + iRazem + ")");

// KONTROLA NEGATYWNA SAMEGO WZORCA: rzecz, ktora adresem pocztowym NIE JEST,
// nie moze wejsc na liste.  Bez tej asercji wzorzec lapiacy wszystko tez byl
// by zielony, a falszywy odsylacz jest gorszy niz jego brak.
string sProbka2 = "Wersja 5.0.65 i sciezka C:\\katalog\\plik.txt oraz tekst bez adresu.\r\n";
object oList2 = miLinks.Invoke(null, new object[] {sProbka2});
int iRazem2 = 0;
System.Collections.IEnumerable en2 = oList2 as System.Collections.IEnumerable;
if (en2 != null) foreach (object oL in en2) iRazem2++;
Sprawdz(iRazem2 == 0,
"KONTROLA NEGATYWNA: numer wersji i sciezka pliku NIE sa odsylaczem (znalazl: " + iRazem2 + ")");
}
catch (Exception ex) {Zle("pomiar zachowania parsera rzucil wyjatek: " + ex.Message);}
}

Console.WriteLine();
Console.WriteLine("--- G. GUARD DOCUMENT MOWI SKUTEK, NIE NAZWE KOMENDY");
// JEGO ZGLOSZENIE 03.09.2026: "nacisnalem ctrl+f7 i mi zabezpieczyl dokument
// a nacisnalem ctrl+shift+f7 nic mi nie powiedzial a kiedys mowil".
//
// DLACZEGO NIE WYSTARCZA PYTAC O NAPISY: cztery nowe komunikaty sa
// literalami, wiec beda w binarce nawet wtedy, gdy nikt ich nie wypowie -
// dokladnie ta pulapka przepuscila trzy z czterech podstawionych bledow przy
// pierwszej wersji tej sondy (patrz komentarz przy LiczWolania).  Dlatego
// pytamy o CIALO metody menuItem_Click: to ona rozstrzyga, czy komunikat
// naprawde idzie do mowy przy nacisnieciu klawisza.
MethodInfo miClick = null;
try {
Type tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame != null) miClick = tFrame.GetMethod("menuItem_Click",
BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
}
catch {}
Sprawdz(miClick != null,
"metoda MdiFrame.menuItem_Click jest dostepna refleksji (nosnik obu komend)");

if (miClick != null) {
// Cztery komunikaty, po dwa na komende, bo rozstrzygamy stan PRZED zmiana.
// Napis MUSI byc ladowany PRZEZ TA metode, nie gdziekolwiek w binarce.
Sprawdz(MaLiteral(miClick, "Guard on"),
"menuItem_Click laduje \"Guard on\" (Control+F7 na dokumencie bez ochrony)");
Sprawdz(MaLiteral(miClick, "Already guarded"),
"menuItem_Click laduje \"Already guarded\" (Control+F7 na JUZ zabezpieczonym)");
Sprawdz(MaLiteral(miClick, "Guard off"),
"menuItem_Click laduje \"Guard off\" (Control+Shift+F7 realnie zdejmuje ochrone)");
Sprawdz(MaLiteral(miClick, "Not guarded"),
"menuItem_Click laduje \"Not guarded\" (Control+Shift+F7 gdy ochrony NIE BYLO)");
}

// OBA CHORDY ZOSTAJA.  On sam napisal "ja bym w ogole te funkcje wyrzucil ze
// skrotow klawiszowych do menu, ale tu nie jestem pewny jeszcze", wiec
// zdjecia NIE robimy - to wahanie, nie decyzja, a zdjecie funkcji jest
// trudniejsze do cofniecia niz zostawienie.  Ta asercja pilnuje, ze zmiana
// mowy przypadkiem chordow nie ruszyla.
int iCF7 = LiczNapis(ab, "Control+F7");
int iCSF7 = LiczNapis(ab, "Control+Shift+F7");
Sprawdz(iCF7 >= 1,
"chord Control+F7 (Guard Document) NADAL jest w binarce (zmierzone: " + iCF7 + ")");
Sprawdz(iCSF7 >= 1,
"chord Control+Shift+F7 (No Guard) NADAL jest w binarce (zmierzone: " + iCSF7 + ")");
Sprawdz(MaNapis(ab, "Guard Document"),
"pozycja menu Guard Document nietknieta");
Sprawdz(MaNapis(ab, "No Guard"),
"pozycja menu No Guard nietknieta");

// SetGuard MUSI zwracac stan poprzedni - cala zmiana na tym stoi.  Gdyby
// ktos zmienil ja na void, kod przestalby sie kompilowac, ale gdyby zmienil
// na zwracanie stanu NOWEGO, build byl by zielony i komunikat KLAMALBY.
try {
Type tRtb = asm.GetType("EdSharp.HomerRichTextBox");
MethodInfo miSetGuard = (tRtb == null) ? null : tRtb.GetMethod("SetGuard",
BindingFlags.Public | BindingFlags.Instance);
Sprawdz(miSetGuard != null && miSetGuard.ReturnType == typeof(bool),
"HomerRichTextBox.SetGuard zwraca bool, czyli stan poprzedni (na tym stoi rozroznienie)");
}
catch (Exception ex) {Zle("odczyt SetGuard rzucil wyjatek: " + ex.Message);}

// ZAPIS FLAGI NIETKNIETY: mowa to jedna sprawa, trwalosc druga.  SaveGuardFlag
// pisze do OBU magazynow (Favorites i Recent) i to zostaje bez zmian.
int iWolSave = LiczWolania(asm, "SaveGuardFlag");
Sprawdz(iWolSave >= 1,
"SaveGuardFlag nadal jest WOLANA (trwalosc flagi guardu nietknieta, wolan: " + iWolSave + ")");

Console.WriteLine();
Console.WriteLine("--- F. WERSJE POPRZEDNIE NIETKNIETE (regresja)");
MethodInfo miMig = tApp.GetMethod("MigrateBookmarksOutOfFavorites",
BindingFlags.Public | BindingFlags.Static);
Sprawdz(miMig != null,
"5.0.64 nietknieta: migracja zakladek z ulubionych nadal jest (refleksja)");
Sprawdz(MaNapis(ab, "BookmarksSplit"),
"5.0.64 nietknieta: znacznik BookmarksSplit nadal jest");
Sprawdz(!MaNapis(ab, "cleared too"),
"5.0.64 nietknieta: nieprawdziwy komunikat o kasowaniu zakladek nadal nie istnieje");
Sprawdz(!MaNapis(ab, "SayAllTempFile"),
"5.0.63 nietknieta: wolanie skryptu JAWS SayAllTempFile nadal nie istnieje");
Sprawdz(MaNapis(ab, "Control+Alt+F9"),
"5.0.63 nietknieta: lista komentarzy nadal na Control+Alt+F9");
Sprawdz(MaNapis(ab, "ExtensionDefaultMigrated"),
"5.0.59 nietknieta: migracja domyslnego rozszerzenia nadal jest");
Sprawdz(MaNapis(ab, "NamedBookmarks"),
"5.0.55 nietknieta: zakladki z nazwa nadal maja wlasna sekcje");

Console.WriteLine();
Console.WriteLine("PODSUMOWANIE: " + iOk + " OK, " + iZle + " ZLE");
return (iZle == 0) ? 0 : 1;
} // Main method

} // Pomiar565 class
