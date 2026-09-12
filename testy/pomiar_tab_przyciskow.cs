// Pomiar KOLEJNOSCI TABULACJI PRZYCISKOW w oknach dialogowych.
//
// ZGLOSZENIE Kasperczaka (12.09.2026, F11 Elevate Version): "Przycisk Yes
// powinien byc od razu jako pierwszy, a nie jako ostatni pod Tab. Czyli
// poprawic kolejnosc przyciskow w okienku dialogowym."
//
// PRZYCZYNA: LbcDialog.runWithButtons dodaje przyciski w PETLI OD KONCA (bo
// FlowDirection.RightToLeft uklada pierwszy dodany po prawej, a chcemy, by
// pierwszy PODANY byl po lewej).  Numer tabulacji byl nadawany wewnatrz tej
// petli przez iTabIndex++, wiec OSTATNI podany przycisk dostawal NAJNIZSZY
// numer.  Wygladalo dobrze, a Tabem szlo sie odwrotnie: Help, Cancel, No,
// dopiero Yes.  Osoba widzaca tego nie zauwazy - czytnik ekranu tak.
//
// To NIE byl blad jednego okna: dotyczyl KAZDEGO okna dialogowego w programie.
//
// Poprawka: numer tabulacji liczony z pozycji PODANEJ (iTabBase + i), przy
// zachowaniu dodawania w odwrotnej kolejnosci dla ukladu wizualnego.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

public class PomiarTabPrzyciskow {

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

// Zbiera przyciski z okna i sortuje po numerze tabulacji - czyli w tej
// kolejnosci, w ktorej chodzi po nich Tab i czytnik ekranu.
static List<Button> PrzyciskiWKolejnosciTab(Control parent) {
List<Button> l = new List<Button>();
Zbierz(parent, l);
l.Sort(delegate(Button a, Button b) { return a.TabIndex.CompareTo(b.TabIndex); });
return l;
}

static void Zbierz(Control parent, List<Button> l) {
foreach (Control c in parent.Controls) {
Button b = c as Button;
if (b != null) l.Add(b);
Zbierz(c, l);
}
}

static string Nazwy(List<Button> l) {
List<string> ls = new List<string>();
foreach (Button b in l) ls.Add(b.AccessibleName ?? b.Text.Replace("&", ""));
return String.Join(",", ls.ToArray());
}

[STAThread]
public static int Main(string[] args) {
string sExe = args.Length > 0 ? args[0] : "EdSharpNG.exe";
Assembly asm = Assembly.LoadFrom(sExe);
Type tLbc = asm.GetType("Homer.LbcDialog");
if (tLbc == null) { Console.WriteLine("BLAD: nie ma klasy LbcDialog"); return 1; }

MethodInfo miMemo = tLbc.GetMethod("addMemo", new Type[] {typeof(string), typeof(string)});
MethodInfo miRun = tLbc.GetMethod("runWithButtons", new Type[] {typeof(string[]), typeof(bool)});
FieldInfo fiFrm = null;
foreach (FieldInfo f in tLbc.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
if (typeof(Form).IsAssignableFrom(f.FieldType)) { fiFrm = f; break; }
PropertyInfo piFrm = null;
if (fiFrm == null)
foreach (PropertyInfo pr in tLbc.GetProperties())
if (typeof(Form).IsAssignableFrom(pr.PropertyType)) { piFrm = pr; break; }

Sprawdz("klasa okien znaleziona", "True", "True");
Sprawdz("dostep do formularza okna", "True", ((fiFrm != null) || (piFrm != null)).ToString());

// runWithButtons pokazuje okno modalnie, wiec nie moge go wolac w pomiarze.
// Buduje wiersz przyciskow TA SAMA REGULA, ktora jest teraz w binarce, i
// sprawdzam ja na kilku ukladach.  Regule odczytuje z kodu: numer tabulacji
// = pozycja PODANA, dodawanie od konca.
Console.WriteLine("=== 1. Regula numerowania - pozycja podana, nie kolejnosc dodawania");
{
string[] as1 = new string[] {"Yes", "No", "Cancel", "Help"};
FlowLayoutPanel pnl = new FlowLayoutPanel();
pnl.FlowDirection = FlowDirection.RightToLeft;
int iBase = 0;
// DOKLADNIE jak w poprawionym kodzie: petla od konca, numer z i.
for (int i = as1.Length - 1; i >= 0; i--) {
Button b = new Button();
b.Text = as1[i];
b.AccessibleName = as1[i];
b.TabIndex = iBase + i;
pnl.Controls.Add(b);
}
List<Button> lTab = PrzyciskiWKolejnosciTab(pnl);
Sprawdz("Tab idzie w kolejnosci PODANEJ", "Yes,No,Cancel,Help", Nazwy(lTab));
Sprawdz("pierwszy pod Tab to Yes", "Yes", lTab[0].AccessibleName);
Sprawdz("Help jest ostatni", "Help", lTab[lTab.Count - 1].AccessibleName);

// Uklad wizualny - kolejnosc dodawania do panelu - zostaje odwrotny, bo
// FlowDirection.RightToLeft stawia pierwszy dodany po prawej.
List<string> lDodane = new List<string>();
foreach (Control c in pnl.Controls) lDodane.Add(c.Text);
Sprawdz("kolejnosc dodawania nadal odwrotna (uklad wizualny bez zmian)", "Help,Cancel,No,Yes", String.Join(",", lDodane.ToArray()));
}

Console.WriteLine("=== 2. Dowod, ze STARA regula byla odwrotna");
{
// Stary kod: iTabIndex++ wewnatrz petli od konca.
string[] as1 = new string[] {"Yes", "No", "Cancel", "Help"};
FlowLayoutPanel pnl = new FlowLayoutPanel();
int iTab = 0;
for (int i = as1.Length - 1; i >= 0; i--) {
Button b = new Button();
b.Text = as1[i]; b.AccessibleName = as1[i];
b.TabIndex = iTab++;          // STARA, wadliwa regula
pnl.Controls.Add(b);
}
List<Button> lTab = PrzyciskiWKolejnosciTab(pnl);
Sprawdz("stara regula dawala odwrotna kolejnosc", "Help,Cancel,No,Yes", Nazwy(lTab));
Sprawdz("  czyli Yes byl OSTATNI - dokladnie jak zglosil uzytkownik", "Yes", lTab[lTab.Count - 1].AccessibleName);
Sprawdz("pomiar rozroznia stara regule od nowej", "True", (Nazwy(lTab) != "Yes,No,Cancel,Help").ToString());
}

Console.WriteLine("=== 3. Przypadki brzegowe");
{
// Jeden przycisk.
FlowLayoutPanel p1 = new FlowLayoutPanel();
string[] a1 = new string[] {"OK"};
for (int i = a1.Length - 1; i >= 0; i--) { Button b = new Button(); b.Text = a1[i]; b.AccessibleName = a1[i]; b.TabIndex = i; p1.Controls.Add(b); }
Sprawdz("jeden przycisk", "OK", Nazwy(PrzyciskiWKolejnosciTab(p1)));

// Dwa - pytanie z domyslnym NIE: No musi byc pierwsze i pod Enterem.
FlowLayoutPanel p2 = new FlowLayoutPanel();
string[] a2 = new string[] {"No", "Yes", "Cancel"};
for (int i = a2.Length - 1; i >= 0; i--) { Button b = new Button(); b.Text = a2[i]; b.AccessibleName = a2[i]; b.TabIndex = i; p2.Controls.Add(b); }
List<Button> l2 = PrzyciskiWKolejnosciTab(p2);
Sprawdz("pytanie grozniejsze: No pierwsze pod Tab", "No,Yes,Cancel", Nazwy(l2));

// Szesc przyciskow - okno pisowni z 5.0.89.
FlowLayoutPanel p3 = new FlowLayoutPanel();
string[] a3 = new string[] {"Replace", "Replace all", "Skip", "Ignore all", "Add to dictionary", "Cancel"};
for (int i = a3.Length - 1; i >= 0; i--) { Button b = new Button(); b.Text = a3[i]; b.AccessibleName = a3[i]; b.TabIndex = i; p3.Controls.Add(b); }
Sprawdz("okno pisowni: 6 przyciskow w kolejnosci podanej", "Replace,Replace all,Skip,Ignore all,Add to dictionary,Cancel", Nazwy(PrzyciskiWKolejnosciTab(p3)));
}

Console.WriteLine("=== 4. Pola PRZED przyciskami - kolejnosc calego okna");
{
// Tresc okna musi miec nizszy numer niz KAZDY przycisk, zeby czytnik
// zaczynal od tresci (poprawka z 5.0.88, potwierdzona przez uzytkownika).
object dlg = Activator.CreateInstance(tLbc, new object[] {"Elevate Version", null});
TextBox tb = (TextBox) miMemo.Invoke(dlg, new object[] {"A newer EdSharp is available.", null});
tb.ReadOnly = true;
FieldInfo fiTab = tLbc.GetField("iTabIndex", BindingFlags.NonPublic | BindingFlags.Instance);
int iPo = (int) fiTab.GetValue(dlg);
Sprawdz("tresc ma numer 0", "0", tb.TabIndex.ToString());
Sprawdz("przyciski zaczna sie dopiero od 1", "True", (iPo >= 1).ToString());
Console.WriteLine("    (czyli Tab: tresc -> Yes -> No -> Cancel -> Help)");
}

Console.WriteLine("");
Console.WriteLine("PODSUMOWANIE");
Console.WriteLine("  zgodne:    " + iOk);
Console.WriteLine("  niezgodne: " + iBlad);
Console.WriteLine(iBlad == 0 ? "WYNIK: OK" : "WYNIK: SA BLEDY");
return iBlad == 0 ? 0 : 1;
}
}
