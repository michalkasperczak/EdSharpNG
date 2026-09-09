// Sonda 5.0.63 na ZBUDOWANEJ binarce (refleksja + napisy w .exe).
//
// CO MIERZY - zwolnienie golego F9 i Shift+F9 (jego decyzja 03.09.2026):
//   1. Metoda COM.JFWRunFunction NIE ISTNIEJE w binarce.  To jest asercja
//      NOSNA calej sondy, bo ta metoda byla JEDYNA droga, ktora obsluga F9
//      wolala skrypt JAWS - jej brak w zbudowanej binarce dowodzi, ze zmiana
//      naprawde weszla do kodu wykonywanego, a nie tylko do zrodla.
//   2. Napis "SayAllTempFile", czyli NAZWA funkcji skryptu JAWS wolanej po
//      COM, zniknal z binarki.  Nazwa funkcji jest literalem, wiec siedzi w
//      napisach .exe dokladnie wtedy, gdy wywolanie istnieje.
//   3. ROZLACZNOSC: mowienie przez JAWS-a (COM.JFWSay) ZOSTAJE.  Bez tej
//      asercji "naprawa" polegajaca na wywaleniu calej obslugi JAWS-a tez
//      bylaby zielona, a odebralaby mowe uzytkownikom JAWS-a.
//   4. ROZLACZNOSC: drugie, niezalezne czytanie calego tekstu (Read All na
//      Alt+F8, wlasna mowa programu, bez JAWS-a) nadal jest pozycja menu.
//   5. ROZLACZNOSC: rodzina komentarzy nietknieta - Alt+F9 wstawianie,
//      Control+Alt+F9 lista, skoki na Alt+Shift+PageUp/PageDown.
//
// KONTROLA WAZNOSCI: ta sama sonda na binarce 5.0.62 MUSI skonczyc kodem 3
// ("metoda COM.JFWRunFunction WCIAZ JEST").  Bez tego zielony wynik nie
// odroznia zwolnienia klawisza od gluchej sondy.
//
// Uzycie:
//   csc.exe /out:pomiar_563.exe pomiar_563.cs
//   pomiar_563.exe <sciezka do EdSharpNG.exe>

using System;
using System.IO;
using System.Reflection;
using System.Text;

class Pomiar563 {

static int iOk = 0;
static int iZle = 0;

static void Ok(string s) {iOk++; Console.WriteLine("OK: " + s);}
static void Zle(string s) {iZle++; Console.WriteLine("ZLE: " + s);}
static void Sprawdz(bool b, string s) {if (b) Ok(s); else Zle(s);}

// Napis w binarce .NET stoi w UTF-16, ale niekoniecznie na PARZYSTYM offsecie
// pliku.  Dekodowanie od bajtu zero widzi tylko pary (0,1), (2,3) i tak dalej,
// wiec napis zaczynajacy sie nieparzysto jest dla niego niewidoczny.  Dlatego
// pytamy o OBA przesuniecia; wystarczy trafienie w jednym.
static bool MaNapis(string sA, string sB, string sSzukane) {
return sA.Contains(sSzukane) || sB.Contains(sSzukane);
} // MaNapis method

static int Main(string[] args) {
if (args.Length < 1) {
Console.WriteLine("Uzycie: pomiar_563.exe <EdSharpNG.exe>");
return 2;
}
string sExe = args[0];
if (!File.Exists(sExe)) {
Console.WriteLine("BRAK PLIKU: " + sExe);
return 2;
}

Assembly asm = Assembly.LoadFrom(sExe);
BindingFlags bfAll = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

Type tCom = asm.GetType("EdSharp.COM");
if (tCom == null) {
Console.WriteLine("BRAK TYPU EdSharp.COM - sonda nie ma czego mierzyc");
return 2;
}

// --- 1. ASERCJA NOSNA + KONTROLA WAZNOSCI W JEDNYM.
// Na 5.0.62 ta metoda ISTNIEJE, wiec sonda konczy sie tu kodem 3 i zielony
// wynik na nowej binarce cos znaczy.
MethodInfo miRun = tCom.GetMethod("JFWRunFunction", bfAll, null, new Type[] {typeof(string)}, null);
MethodInfo miRun2 = null;
foreach (MethodInfo mi in tCom.GetMethods(bfAll)) {
if (mi.Name == "JFWRunFunction") {miRun2 = mi; break;}
}
if (miRun != null || miRun2 != null) {
Console.WriteLine("METODA COM.JFWRunFunction WCIAZ JEST - to wersja SPRZED zmiany (kontrola waznosci)");
return 3;
}
Ok("COM.JFWRunFunction nie istnieje w binarce (zadne przeciazenie)");

// --- 2. NAZWA FUNKCJI SKRYPTU JAWS ZNIKNELA Z NAPISOW.
byte[] aBytes = File.ReadAllBytes(sExe);
string sU16a = Encoding.Unicode.GetString(aBytes);
string sU16b = Encoding.Unicode.GetString(aBytes, 1, aBytes.Length - 1);
string sAscii = Encoding.ASCII.GetString(aBytes);

Sprawdz(!MaNapis(sU16a, sU16b, "SayAllTempFile") && !sAscii.Contains("SayAllTempFile"),
"napis SayAllTempFile zniknal z binarki (wolanie skryptu JAWS przestalo istniec)");

// KONTROLA WAZNOSCI SAMEGO KANALU NAPISOW: gdyby czytanie napisow bylo gluche,
// asercja wyzej bylaby zielona zawsze.  Pytamy wiec o napis, ktory ISTNIEC MUSI.
Sprawdz(MaNapis(sU16a, sU16b, "FreedomSci.JawsApi"),
"kontrola waznosci kanalu napisow: napis FreedomSci.JawsApi JEST widoczny");

// --- 3. ROZLACZNOSC: mowienie przez JAWS-a zostaje.
bool bJfwSay = false;
foreach (MethodInfo mi in tCom.GetMethods(bfAll)) {
if (mi.Name == "JFWSay") {bJfwSay = true; break;}
}
Sprawdz(bJfwSay, "asercja rozlaczna: COM.JFWSay ZOSTAJE (mowienie przez JAWS-a to zywa droga)");

// --- 4. ROZLACZNOSC: drugie czytanie calego tekstu, wlasna mowa programu.
Type tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {
Zle("BRAK TYPU EdSharp.MdiFrame");
}
else {
FieldInfo fiReadAll = tFrame.GetField("menuQueryReadAll", bfAll);
Sprawdz(fiReadAll != null,
"asercja rozlaczna: pozycja menu Read All (Alt+F8) nadal istnieje jako pole");

// --- 5. ROZLACZNOSC: rodzina komentarzy nietknieta.
// UWAGA, ZMIERZONA GLUCHOTA SONDY (podstawiony blad 3, 03.09.2026): pytanie
// o ISTNIENIE POLA klasy tej rodziny NIE ROZSTRZYGA.  Pole
// menuMiscCommentList jest zadeklarowane osobno od przypisania, wiec
// podmiana CreateMenuItem(...) na null daje binarke, w ktorej pole nadal
// jest, komendy nie ma, a sonda swiecila na zielono 12/12.  Rozstrzyga
// NAPIS CHORDU: literal "Control+Alt+F9" idzie do binarki dokladnie wtedy,
// gdy wywolanie CreateMenuItem z tym chordem istnieje - na binarce z bledem
// 3 tego napisu (i napisu "Comment List ...") NIE BYLO.
string[] aKom = new string[] {"menuMiscInsertComment", "menuMiscCommentList",
"menuMiscNextComment", "menuMiscPriorComment"};
foreach (string sPole in aKom) {
Sprawdz(tFrame.GetField(sPole, bfAll) != null,
"asercja rozlaczna: pole " + sPole + " nietkniete");
}
// To jest asercja MOCNA, w przeciwienstwie do czterech powyzej.
string[,] aChordy = new string[,] {
{"Comment List ...", "Control+Alt+F9"},
{"Insert Comment ...", "Alt+F9"},
{"Next Comment", "Alt+Shift+PageDown"},
{"Prior Comment", "Alt+Shift+PageUp"}};
for (int iKom = 0; iKom < aChordy.GetLength(0); iKom++) {
string sNazwa = aChordy[iKom, 0];
string sChord = aChordy[iKom, 1];
Sprawdz(MaNapis(sU16a, sU16b, sNazwa) && MaNapis(sU16a, sU16b, sChord),
"asercja rozlaczna MOCNA: komenda \"" + sNazwa + "\" nadal przypisana do " + sChord);
}
}

// --- 6. KOMUNIKATY, KTORE MUSZA ZOSTAC (kontrola, ze nie zabralem funkcji
// przy zabieraniu klawisza).  Gdyby zniknely, znaczylo by, ze zdjalem cos
// wiecej niz obsluge dwoch chordow.
string[] aNapisy = new string[] {"No comments!", "Last comment!", "First comment!"};
foreach (string s in aNapisy) {
Sprawdz(MaNapis(sU16a, sU16b, s), "asercja rozlaczna: komunikat \"" + s + "\" nadal w binarce");
}

Console.WriteLine();
Console.WriteLine("PODSUMOWANIE: " + iOk + " OK, " + iZle + " ZLE");
return iZle == 0 ? 0 : 1;
} // Main method

} // Pomiar563 class
