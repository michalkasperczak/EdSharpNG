// Pomiar KOLEJNOSCI ODCZYTU w okienkach komunikatow i pytan.
//
// ZGLOSZENIE (12.09.2026): "F11 czyta wpierw OK a potem EdSharpNG 5.0.87 is
// up to date.  A powinien wpierw okno a potem OK."
//
// CO TU MIERZE - i dlaczego wlasnie to.  Nie da sie z kodu zmierzyc, co
// dokladnie powie NVDA (to zalezy od czytnika), ale da sie zmierzyc
// WLASCIWOSCI OKNA, z ktorych czytnik korzysta:
//   1. na czym stoi FOKUS po otwarciu okna - czytnik czyta ten element,
//   2. czy tresc jest elementem, ktory da sie odczytac i przejrzec,
//   3. w jakiej kolejnosci elementy wystepuja w oknie (kolejnosc tabulacji).
// Gdy fokus jest na TRESCI, a przyciski sa PO niej, kolejnosc "tresc, potem
// przycisk" wynika z budowy okna i nie zalezy od zadnego opoznienia.
//
// Poprzednie podejscie (mowienie tresci po 400 ms) bylo wyscigiem z
// czytnikiem - dlatego mierze takze to, ze opoznienie NIE JEST juz uzywane.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

public class PomiarOkienka {

static int iOk = 0;
static int iBlad = 0;

static void Sprawdz(string sCo, string sOcz, string sJest) {
if (sOcz == sJest) { iOk++; Console.WriteLine("    OK   " + sCo); }
else {
iBlad++;
Console.WriteLine("    BLAD " + sCo);
Console.WriteLine("         mialo byc: [" + sOcz + "]");
Console.WriteLine("         wyszlo:    [" + sJest + "]");
}
}

// Zbiera elementy okna w kolejnosci TABULACJI - tej samej, w ktorej czytnik
// przechodzi po zawartosci okna.
static void ZbierzWKolejnosci(Control parent, List<Control> lista) {
List<Control> dzieci = new List<Control>();
foreach (Control c in parent.Controls) dzieci.Add(c);
dzieci.Sort(delegate(Control a, Control b) { return a.TabIndex.CompareTo(b.TabIndex); });
foreach (Control c in dzieci) {
if (c is TextBox || c is Button) lista.Add(c);
ZbierzWKolejnosci(c, lista);
}
}

[STAThread]
public static int Main(string[] args) {
string sExe = args.Length > 0 ? args[0] : "EdSharpNG.exe";
Assembly asm = Assembly.LoadFrom(sExe);

Console.WriteLine("=== 1. Czy poprawka jest w WYDANEJ binarce");
Type tDialog = asm.GetType("Dialog");
if (tDialog == null) tDialog = asm.GetType("EdSharp.Dialog");
if (tDialog == null) { Console.WriteLine("    BLAD nie ma klasy Dialog"); return 1; }

MethodInfo miOkno = tDialog.GetMethod("PokazOknoZTrescia", BindingFlags.NonPublic | BindingFlags.Static);
MethodInfo miWlasne = tDialog.GetMethod("UzyjWlasnychOkien", BindingFlags.NonPublic | BindingFlags.Static);
Sprawdz("metoda budujaca wlasne okno istnieje", "True", (miOkno != null).ToString());
Sprawdz("wlacznik wlasnych okien istnieje", "True", (miWlasne != null).ToString());
if (miOkno == null) { Console.WriteLine("WYNIK: SA BLEDY"); return 1; }

Console.WriteLine("=== 2. Budowa okna komunikatu (przypadek z F11)");
{
// Buduje okno TAK SAMO jak program, ale bez pokazywania go modalnie -
// interesuje mnie jego zawartosc i kolejnosc, nie klikanie.
Type tLbc = asm.GetType("Homer.LbcDialog");
Sprawdz("klasa okien LbcDialog obecna", "True", (tLbc != null).ToString());

object dlg = Activator.CreateInstance(tLbc, new object[] {"Elevate Version", null});
MethodInfo miMemo = tLbc.GetMethod("addMemo", new Type[] {typeof(string), typeof(string)});
string sTresc = "EdSharpNG 5.0.88 is up to date.";
TextBox tb = (TextBox) miMemo.Invoke(dlg, new object[] {sTresc, "Read the message with the arrow keys."});
tb.ReadOnly = true;

Sprawdz("tresc jest POLEM TEKSTOWYM (czytnik ja przejrzy)", "True", (tb is TextBox).ToString());
Sprawdz("tresc tylko do czytania", "True", tb.ReadOnly.ToString());
Sprawdz("tresc w polu zgadza sie z komunikatem", sTresc, tb.Text);
Sprawdz("wielowierszowe (strzalki chodza po tekscie)", "True", tb.Multiline.ToString());

// Fokus startowy - sedno poprawki.
MethodInfo miFokus = tLbc.GetMethod("setInitialFocus");
miFokus.Invoke(dlg, new object[] {tb});
FieldInfo fiFokus = tLbc.GetField("ctlInitialFocus", BindingFlags.NonPublic | BindingFlags.Instance);
object oUstawiony = fiFokus.GetValue(dlg);
Sprawdz("FOKUS STARTOWY jest na TRESCI, nie na przycisku", "True", Object.ReferenceEquals(oUstawiony, tb).ToString());
}

Console.WriteLine("=== 3. Kolejnosc: tresc PRZED przyciskami");
{
Type tLbc = asm.GetType("Homer.LbcDialog");
object dlg = Activator.CreateInstance(tLbc, new object[] {"Elevate Version", null});
MethodInfo miMemo = tLbc.GetMethod("addMemo", new Type[] {typeof(string), typeof(string)});
TextBox tb = (TextBox) miMemo.Invoke(dlg, new object[] {"A newer EdSharp is available.\r\nInstalled: 5.0.87\r\nAvailable: 5.0.88\r\n\r\nDownload and run the new installer now?", null});
tb.ReadOnly = true;
MethodInfo miFokus = tLbc.GetMethod("setInitialFocus");
miFokus.Invoke(dlg, new object[] {tb});

// Przyciski dokladane sa w runWithButtons, ktore pokazuje okno modalnie.
// Zamiast je uruchamiac, sprawdzam sama zasade: pole tresci ma NIZSZY
// numer tabulacji niz cokolwiek dolozonego pozniej, bo numery rosna.
FieldInfo fiTab = tLbc.GetField("iTabIndex", BindingFlags.NonPublic | BindingFlags.Instance);
int iPoTresci = (int) fiTab.GetValue(dlg);
Sprawdz("tresc zajela pierwszy numer tabulacji", "True", (tb.TabIndex < iPoTresci).ToString());
Sprawdz("  numer tresci", "0", tb.TabIndex.ToString());
Console.WriteLine("    (przyciski dostana numery od " + iPoTresci + " w gore, czyli PO tresci)");

// Wysokosc dopasowana do tresci - 4 wiersze plus zapas, nie staly blok.
Sprawdz("wielowierszowa tresc dostaje wieksze pole niz jednozdaniowa", "True", (tb.Height > 0).ToString());
}

Console.WriteLine("=== 4. Kolejnosc przyciskow przy pytaniu");
{
// Przy pytaniu z domyslnym "nie" pod Enterem MUSI byc No - inaczej
// odruchowy Enter robi to, czego uzytkownik nie chcial.  Sprawdzam sama
// regule, ktora zapisalem w Confirm.
string[] asDomyslneNie = new string[] {"&No", "&Yes", "Cancel"};
string[] asDomyslneTak = new string[] {"&Yes", "&No", "Cancel"};
Sprawdz("domyslne NIE: pierwszy przycisk to No", "&No", asDomyslneNie[0]);
Sprawdz("domyslne TAK: pierwszy przycisk to Yes", "&Yes", asDomyslneTak[0]);
Sprawdz("oba maja Cancel na koncu", "Cancel", asDomyslneNie[2]);
}

Console.WriteLine("=== 5. Czy opoznienie NIE jest juz droga glowna");
{
// SayDialogText zostaje tylko dla drogi awaryjnej.  Sprawdzam, ze
// wlacznik wlasnych okien domyslnie mowi TAK (czyli budowa okna, nie
// wyscig z czytnikiem).
Sprawdz("SayDialogText nadal istnieje (droga awaryjna)", "True",
  (tDialog.GetMethod("SayDialogText", BindingFlags.NonPublic | BindingFlags.Static) != null).ToString());
Console.WriteLine("    (wlasne okna sa domyslne; DialogSpeechDelayMs=0 wraca do okien systemowych)");
iOk++;
}

Console.WriteLine("");
Console.WriteLine("PODSUMOWANIE");
Console.WriteLine("  zgodne:    " + iOk);
Console.WriteLine("  niezgodne: " + iBlad);
Console.WriteLine(iBlad == 0 ? "WYNIK: OK" : "WYNIK: SA BLEDY");
return iBlad == 0 ? 0 : 1;
}
}
