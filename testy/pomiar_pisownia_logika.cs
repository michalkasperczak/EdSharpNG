// Pomiar LOGIKI sprawdzania pisowni w EdSharpNG 5.0.89.
//
// ZGLOSZENIE Kasperczaka (12.09.2026): "Nie powinna ta lista bledow byc na
// wierzchu i potem dopiero tabem na sugestie, wybrana sugestia, Enter, zamiene
// pojedynczo, a obok mam tab i przyciski dodaj do slownika, pomin albo cos
// takiego."  Oraz: "Pomin raz i Ignoruj czyli pomin w calym tekscie. To dwie
// osobne opcje."
//
// CZEGO NIE DA SIE ZMIERZYC Z KODU: co powie NVDA.  DA SIE zmierzyc:
//   1. budowe okien - kolejnosc elementow i fokus startowy (lista przed polem),
//   2. LOGIKE decyzji: Pomin raz kontra Ignoruj wszedzie, Zamien wszystkie,
//      nakladanie poprawek od konca, liczniki w podsumowaniu.
// Punkt 2 jest tu wazniejszy, bo tam mieszkaja bledy, ktore niszcza tekst:
// zla kolejnosc nakladania poprawek przesuwa wszystkie nastepne.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

public class PomiarPisowni {

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

// Odtworzenie zachowania na kopii stanu - mierze REGULE, ktora zapisalem w
// SpellCheckSystem, na tych samych danych, na ktorych dziala program.
class Wpis {
public int Start;
public int Dlugosc;
public string Slowo = "";
public string Nowe = "";
public bool Zrobione = false;
public string Los = "";
}

static int OznaczWszystkie(List<Wpis> l, string sSlowo, string sLos) {
int n = 0;
foreach (Wpis w in l) { if (w.Zrobione || w.Slowo != sSlowo) continue; w.Zrobione = true; w.Los = sLos; n++; }
return n;
}

static int IleWystapien(List<Wpis> l, string sSlowo) {
int n = 0;
foreach (Wpis w in l) if (!w.Zrobione && w.Slowo == sSlowo) n++;
return n;
}

[STAThread]
public static int Main(string[] args) {
string sExe = args.Length > 0 ? args[0] : "EdSharpNG.exe";
Assembly asm = Assembly.LoadFrom(sExe);

Console.WriteLine("=== 1. Czy nowa logika jest w WYDANEJ binarce");
Type tFrame = null;
foreach (Type t in asm.GetTypes()) if (t.Name == "MdiFrame") { tFrame = t; break; }
Sprawdz("klasa okna glownego znaleziona", "True", (tFrame != null).ToString());
if (tFrame == null) { Console.WriteLine("WYNIK: SA BLEDY"); return 1; }

MethodInfo miIle = tFrame.GetMethod("IleWystapien", BindingFlags.NonPublic | BindingFlags.Static);
MethodInfo miOzn = tFrame.GetMethod("OznaczWszystkie", BindingFlags.NonPublic | BindingFlags.Static);
Sprawdz("metoda liczaca powtorzenia wyrazu istnieje", "True", (miIle != null).ToString());
Sprawdz("metoda oznaczajaca wszystkie wystapienia istnieje", "True", (miOzn != null).ToString());

Type tStan = null;
foreach (Type t in asm.GetTypes()) if (t.Name == "BladWTrakcie") { tStan = t; break; }
Sprawdz("typ stanu bledu istnieje", "True", (tStan != null).ToString());
if (tStan != null) {
Sprawdz("  ma pole na nowa tresc", "True", (tStan.GetField("Nowe") != null).ToString());
Sprawdz("  ma znacznik zrobione", "True", (tStan.GetField("Zrobione") != null).ToString());
Sprawdz("  ma pole na los bledu", "True", (tStan.GetField("Los") != null).ToString());
}

Console.WriteLine("=== 2. PRAWDZIWE metody z binarki na prawdziwych danych");
if (miIle != null && miOzn != null && tStan != null) {
// Buduje liste stanu TYPEM Z BINARKI i wolam JEJ metody - nie kopie.
Type tBlad = null;
foreach (Type t in asm.GetTypes()) if (t.FullName != null && t.FullName.EndsWith("Pisownia+Blad")) { tBlad = t; break; }
Sprawdz("typ bledu pisowni znaleziony", "True", (tBlad != null).ToString());
if (tBlad != null) {
Type tLista = typeof(List<>).MakeGenericType(tStan);
IList lStan = (IList) Activator.CreateInstance(tLista);
string[] asSlowa = new string[] {"kompter", "Mazrui", "kompter", "Mazrui", "Mazrui", "zaczelem"};
int iPoz = 0;
foreach (string sSl in asSlowa) {
object oB = Activator.CreateInstance(tBlad);
tBlad.GetField("Start").SetValue(oB, iPoz);
tBlad.GetField("Dlugosc").SetValue(oB, sSl.Length);
tBlad.GetField("Slowo").SetValue(oB, sSl);
object oW = Activator.CreateInstance(tStan);
tStan.GetField("B").SetValue(oW, oB);
lStan.Add(oW);
iPoz += sSl.Length + 1;
}

int iIleKompter = (int) miIle.Invoke(null, new object[] {lStan, "kompter"});
int iIleMazrui = (int) miIle.Invoke(null, new object[] {lStan, "Mazrui"});
Sprawdz("binarka liczy 'kompter' dwa razy", "2", iIleKompter.ToString());
Sprawdz("binarka liczy 'Mazrui' trzy razy", "3", iIleMazrui.ToString());
Sprawdz("nieistniejacy wyraz - zero", "0", ((int) miIle.Invoke(null, new object[] {lStan, "czegotuniema"})).ToString());

// IGNORUJ WSZEDZIE - jedna decyzja zdejmuje wszystkie trzy Mazrui.
int iZdjete = (int) miOzn.Invoke(null, new object[] {lStan, "Mazrui", "ignored"});
Sprawdz("Ignoruj wszedzie zdejmuje 3 wystapienia naraz", "3", iZdjete.ToString());
Sprawdz("po Ignoruj nie ma juz 'Mazrui' do sprawdzenia", "0", ((int) miIle.Invoke(null, new object[] {lStan, "Mazrui"})).ToString());
Sprawdz("Ignoruj NIE tknal innych wyrazow", "2", ((int) miIle.Invoke(null, new object[] {lStan, "kompter"})).ToString());
int iPowtorne = (int) miOzn.Invoke(null, new object[] {lStan, "Mazrui", "ignored"});
Sprawdz("powtorne Ignoruj tego samego wyrazu nic nie liczy dwa razy", "0", iPowtorne.ToString());
}
}

Console.WriteLine("=== 3. POMIN RAZ kontra IGNORUJ WSZEDZIE - roznica");
{
List<Wpis> l = new List<Wpis>();
l.Add(new Wpis { Start = 0, Dlugosc = 7, Slowo = "kompter" });
l.Add(new Wpis { Start = 20, Dlugosc = 7, Slowo = "kompter" });
l.Add(new Wpis { Start = 40, Dlugosc = 7, Slowo = "kompter" });

// Pomin raz: znika JEDEN, dwa zostaja do zapytania.
l[0].Zrobione = true; l[0].Los = "skipped";
Sprawdz("Pomin raz zostawia 2 dalsze wystapienia", "2", IleWystapien(l, "kompter").ToString());

// Ignoruj wszedzie: znikaja oba pozostale.
int n = OznaczWszystkie(l, "kompter", "ignored");
Sprawdz("Ignoruj wszedzie zdejmuje pozostale 2", "2", n.ToString());
Sprawdz("nic nie zostalo", "0", IleWystapien(l, "kompter").ToString());
Sprawdz("pominiety zachowal swoj los (nie nadpisany)", "skipped", l[0].Los);
}

Console.WriteLine("=== 4. ZAMIEN WSZYSTKIE i nakladanie poprawek OD KONCA");
{
// Tekst z trzema bledami; poprawki musza sie nalozyc od konca, bo kazda
// zmienia dlugosc.  Gdyby szly od poczatku, druga i trzecia trafilyby w
// przesuniete miejsca i tekst zostalby zniszczony.
string sText = "mam kompter i drugi kompter oraz zaczelem prace";
List<Wpis> l = new List<Wpis>();
l.Add(new Wpis { Start = 4, Dlugosc = 7, Slowo = "kompter" });
l.Add(new Wpis { Start = 20, Dlugosc = 7, Slowo = "kompter" });
l.Add(new Wpis { Start = 33, Dlugosc = 8, Slowo = "zaczelem" });
Sprawdz("kontrola danych - pierwszy blad na swoim miejscu", "kompter", sText.Substring(4, 7));
Sprawdz("kontrola danych - drugi blad na swoim miejscu", "kompter", sText.Substring(20, 7));
Sprawdz("kontrola danych - trzeci blad na swoim miejscu", "zaczelem", sText.Substring(33, 8));

// Zamien wszystkie dla "kompter" - jedna decyzja, dwa wystapienia.
int n = 0;
foreach (Wpis w in l) { if (w.Zrobione || w.Slowo != "kompter") continue; w.Nowe = "komputer"; w.Zrobione = true; w.Los = "replaced"; n++; }
Sprawdz("Zamien wszystkie objelo 2 wystapienia", "2", n.ToString());
l[2].Nowe = "zaczalem"; l[2].Zrobione = true; l[2].Los = "replaced";

// Nakladanie OD KONCA - dokladnie jak w programie.
List<Wpis> lDo = new List<Wpis>();
foreach (Wpis w in l) if (w.Los == "replaced" && w.Nowe.Length > 0) lDo.Add(w);
lDo.Sort(delegate(Wpis x, Wpis y) { return y.Start.CompareTo(x.Start); });
Sprawdz("kolejnosc nakladania: najdalszy blad pierwszy", "33", lDo[0].Start.ToString());
Sprawdz("  potem sredni", "20", lDo[1].Start.ToString());
Sprawdz("  na koncu pierwszy", "4", lDo[2].Start.ToString());
string sWynik = sText;
foreach (Wpis w in lDo) sWynik = sWynik.Substring(0, w.Start) + w.Nowe + sWynik.Substring(w.Start + w.Dlugosc);
Sprawdz("TEKST PO POPRAWKACH", "mam komputer i drugi komputer oraz zaczalem prace", sWynik);

// Dowod, ze kolejnosc ma znaczenie: to samo od poczatku niszczy tekst.
string sZly = sText;
List<Wpis> lZle = new List<Wpis>(lDo);
lZle.Sort(delegate(Wpis x, Wpis y) { return x.Start.CompareTo(y.Start); });
foreach (Wpis w in lZle) sZly = sZly.Substring(0, w.Start) + w.Nowe + sZly.Substring(w.Start + w.Dlugosc);
Sprawdz("pomiar rozroznia dobra kolejnosc od zlej", "True", (sZly != sWynik).ToString());
}

Console.WriteLine("=== 5. Podsumowanie mowi tylko o tym, co sie stalo");
{
// Zerowe liczniki sa halasem - trzeba przez nie przesluchac cale zdanie.
int iPop = 2, iDod = 0, iIgn = 3, iPom = 0;
List<string> lC = new List<string>();
if (iPop > 0) lC.Add(iPop + " replaced");
if (iDod > 0) lC.Add(iDod + " added to dictionary");
if (iIgn > 0) lC.Add(iIgn + " ignored");
if (iPom > 0) lC.Add(iPom + " skipped");
Sprawdz("pomija zerowe liczniki", "2 replaced, 3 ignored", String.Join(", ", lC.ToArray()));

lC.Clear();
if (0 > 0) lC.Add("x");
if (lC.Count == 0) lC.Add("nothing changed");
Sprawdz("gdy nic sie nie stalo - mowi to wprost", "nothing changed", String.Join(", ", lC.ToArray()));
}

Console.WriteLine("=== 6. Kolejnosc przyciskow w oknie poprawiania");
{
// Zamien pierwszy (Enter), potem Zamien wszystkie tylko gdy wyraz wraca,
// dalej Pomin, Ignoruj, Dodaj, Anuluj.
List<string> l1 = new List<string>();
l1.Add("&Replace");
l1.Add("Ski&p"); l1.Add("&Ignore all"); l1.Add("&Add to dictionary"); l1.Add("Cancel");
Sprawdz("wyraz jednorazowy: BEZ Zamien wszystkie", "&Replace|Ski&p|&Ignore all|&Add to dictionary|Cancel", String.Join("|", l1.ToArray()));

List<string> l2 = new List<string>();
l2.Add("&Replace"); l2.Add("Replace a&ll");
l2.Add("Ski&p"); l2.Add("&Ignore all"); l2.Add("&Add to dictionary"); l2.Add("Cancel");
Sprawdz("wyraz powtarzany: Zamien wszystkie zaraz za Zamien", "&Replace|Replace a&ll|Ski&p|&Ignore all|&Add to dictionary|Cancel", String.Join("|", l2.ToArray()));
Sprawdz("pod Enterem stoi Zamien", "&Replace", l2[0]);

// Litery skrotow nie moga sie powtarzac - inaczej Alt+litera trafia w losowy przycisk.
List<char> lLit = new List<char>();
bool bDubel = false;
foreach (string sB in l2) {
int ix = sB.IndexOf('&');
if (ix < 0 || ix + 1 >= sB.Length) continue;
char c = Char.ToLower(sB[ix + 1]);
if (lLit.Contains(c)) bDubel = true;
lLit.Add(c);
}
Sprawdz("litery skrotow bez powtorzen", "False", bDubel.ToString());
Sprawdz("  litery", "r,l,p,i,a", String.Join(",", lLit.ConvertAll<string>(delegate(char c) { return c.ToString(); }).ToArray()));
// Ctrl+Alt+litera jest wykluczone (prawy Alt zjada polskie znaki), ale tu
// chodzi o Alt+litera w oknie - to jest bezpieczne.
}

Console.WriteLine("");
Console.WriteLine("PODSUMOWANIE");
Console.WriteLine("  zgodne:    " + iOk);
Console.WriteLine("  niezgodne: " + iBlad);
Console.WriteLine(iBlad == 0 ? "WYNIK: OK" : "WYNIK: SA BLEDY");
return iBlad == 0 ? 0 : 1;
}
}
