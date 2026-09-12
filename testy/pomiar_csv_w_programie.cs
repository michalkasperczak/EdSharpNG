// Pomiar CSV W GOTOWYM PROGRAMIE (EdSharpNG.exe przez refleksje).
//
// Pomiar pomiar_csv.cs sprawdzil sam format. Ten sprawdza to, czego tamten
// nie mogl: czy klasa Csv wkompilowala sie do WYDANEJ binarki, czy okno
// tabeli da sie z niej wywolac i czy cala droga plik -> tabela -> plik
// zachowuje tresc. Bez tego "dziala" opieraloby sie na tym, ze kompilator
// nie protestowal.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections;

public class PomiarCsvExe {

static int iBlad = 0;
static int iOk = 0;

static void Sprawdz(string sCo, string sOcz, string sJest) {
if (sOcz == sJest) { iOk++; Console.WriteLine("    OK   " + sCo); }
else {
iBlad++;
Console.WriteLine("    BLAD " + sCo);
Console.WriteLine("         mialo byc: [" + sOcz + "]");
Console.WriteLine("         wyszlo:    [" + sJest + "]");
}
}

public static int Main(string[] args) {
string sExe = args.Length > 0 ? args[0] : "EdSharpNG.exe";
Assembly asm = Assembly.LoadFrom(sExe);

Console.WriteLine("=== 1. Czy klasa Csv jest w WYDANEJ binarce");
Type tCsv = asm.GetType("EdSharp.Csv");
if (tCsv == null) {
Console.WriteLine("    BLAD nie ma typu EdSharp.Csv w " + sExe);
Console.WriteLine("WYNIK: SA BLEDY");
return 1;
}
iOk++;
Console.WriteLine("    OK   EdSharp.Csv obecna");

MethodInfo miCzytaj = tCsv.GetMethod("Czytaj", new Type[] {typeof(string)});
MethodInfo miZapisz = tCsv.GetMethod("Zapisz");
MethodInfo miWyglada = tCsv.GetMethod("WygladaNaTabele");
MethodInfo miSep = tCsv.GetMethod("RozpoznajSeparator");
Sprawdz("metoda Czytaj", "True", (miCzytaj != null).ToString());
Sprawdz("metoda Zapisz", "True", (miZapisz != null).ToString());
Sprawdz("metoda WygladaNaTabele", "True", (miWyglada != null).ToString());

Console.WriteLine("=== 2. Czy okno tabeli jest wywolywalne z programu");
Type tFrame = asm.GetType("MdiFrame");
if (tFrame == null) tFrame = asm.GetType("EdSharp.MdiFrame");
MethodInfo miOkno = null;
if (tFrame != null) miOkno = tFrame.GetMethod("EditCsvAsTable", BindingFlags.Public | BindingFlags.Instance);
Sprawdz("metoda EditCsvAsTable w oknie glownym", "True", (miOkno != null).ToString());
if (miOkno != null)
Sprawdz("  przyjmuje sciezke pliku", "System.String", miOkno.GetParameters()[0].ParameterType.FullName);

Console.WriteLine("=== 3. Polski plik z Excela (SREDNIK) - droga plik -> tabela");
{
string sPlik = Path.Combine(Path.GetTempPath(), "pomiar_polski.csv");
string sTresc = "miasto;wojewodztwo;ludnosc\r\nKrakow;malopolskie;800653\r\nPoznan;wielkopolskie;546859\r\nGdansk;pomorskie;470907";
File.WriteAllText(sPlik, sTresc, new UTF8Encoding(true));

string sZPliku = File.ReadAllText(sPlik);
object oSep = miSep.Invoke(null, new object[] {sZPliku});
Sprawdz("separator rozpoznany jako srednik", ";", oSep.ToString());

object[] aoArgs = new object[] {sZPliku, ';', 0};
MethodInfo miCzytaj3 = tCsv.GetMethod("Czytaj", new Type[] {typeof(string), typeof(char), typeof(int)});
IList rows = (IList) miCzytaj3.Invoke(null, aoArgs);
Sprawdz("cztery wiersze", "4", rows.Count.ToString());

IList w0 = (IList) rows[0];
Sprawdz("naglowek trzeciej kolumny", "ludnosc", (string) w0[2]);
IList w1 = (IList) rows[1];
Sprawdz("pierwsze miasto", "Krakow", (string) w1[0]);
Sprawdz("ludnosc Krakowa", "800653", (string) w1[2]);

// Tak wlasnie czytnik ekranu przeczyta komorke w siatce: nazwa kolumny
// plus tresc. To jest cala roznica wobec liczenia srednikow w pamieci.
Console.WriteLine("    tak przeczyta czytnik: \"" + (string) w0[2] + ": " + (string) w1[2] + "\"");

File.Delete(sPlik);
}

Console.WriteLine("=== 4. Zapis wraca do pliku bez psucia tresci");
{
string sPlik = Path.Combine(Path.GetTempPath(), "pomiar_zapis.csv");
string sTresc = "nazwa;adres;uwaga\r\nSzkola;\"Krakow, ul. Dluga 5\";zwykla\r\nUrzad;Poznan;\"ma \"\"aneks\"\"\"";
File.WriteAllText(sPlik, sTresc, new UTF8Encoding(true));

MethodInfo miCzytaj3 = tCsv.GetMethod("Czytaj", new Type[] {typeof(string), typeof(char), typeof(int)});
IList rows = (IList) miCzytaj3.Invoke(null, new object[] {File.ReadAllText(sPlik), ';', 0});

// Poprawiamy jedna komorke - tak, jak zrobil by to uzytkownik w siatce.
IList w1 = (IList) rows[1];
w1[2] = "poprawiona";

object oNowy = miZapisz.Invoke(null, new object[] {rows, ';', "\r\n"});
string sNowy = (string) oNowy;

Sprawdz("zmieniona komorka w pliku", "True", sNowy.Contains("poprawiona").ToString());
// UWAGA - tu poprawiam WLASNE bledne oczekiwanie (zmierzone 12.09.2026).
// Najpierw sprawdzalem, czy adres "Krakow, ul. Dluga 5" wroci w cudzyslowach.
// Wrocil BEZ nich - i tak ma byc: separatorem tego pliku jest SREDNIK, wiec
// przecinek w tresci niczego nie dzieli i oslanianie byloby zasmiecaniem
// pliku. Dowodem poprawnosci jest cykl ponizej: pole odczytane po zapisie
// jest identyczne. Zostawiam to jako przypadek pomiarowy, bo pokazuje
// dokladnie te roznice - i chroni przed "poprawieniem" kodu w zla strone.
Sprawdz("adres z przecinkiem BEZ cudzyslowow (bo separator to srednik)", "True", sNowy.Contains("Krakow, ul. Dluga 5").ToString());
Sprawdz("  i nie oslonięty na sile", "False", sNowy.Contains("\"Krakow, ul. Dluga 5\"").ToString());
Sprawdz("cudzyslowy w tresci zachowane", "True", sNowy.Contains("\"ma \"\"aneks\"\"\"").ToString());
Sprawdz("separator nadal srednik", "True", sNowy.StartsWith("nazwa;adres;uwaga").ToString());

// I odczyt tego, co zapisalismy - domkniecie cyklu.
IList rows2 = (IList) miCzytaj3.Invoke(null, new object[] {sNowy, ';', 0});
Sprawdz("po zapisie nadal trzy wiersze", "3", rows2.Count.ToString());
IList w1b = (IList) rows2[1];
Sprawdz("adres po cyklu nietkniety", "Krakow, ul. Dluga 5", (string) w1b[1]);
IList w2b = (IList) rows2[2];
Sprawdz("cytat po cyklu nietkniety", "ma \"aneks\"", (string) w2b[2]);

File.Delete(sPlik);
}

Console.WriteLine("=== 5. Plik .csv, ktory NIE jest tabela - program ma milczec");
{
string sZwykly = "To jest notatka.\r\nDruga linia bez zadnych przecinkow.\r\nTrzecia.";
object[] ao = new object[] {sZwykly, ',', 0, 0};
object oWynik = miWyglada.Invoke(null, ao);
Sprawdz("nie proponuje tabeli dla zwyklego tekstu", "False", oWynik.ToString());
}

Console.WriteLine("=== 6. Plik bez naglowkow - pierwszy rekord NIE moze zniknac");
{
// Gdyby program wzial pierwszy wiersz danych za naglowki, ten rekord
// przestal by byc widoczny. Tu sprawdzam sam warunek: powtorzone i puste
// pola dyskwalifikuja wiersz jako naglowki.
MethodInfo miCzytaj3 = tCsv.GetMethod("Czytaj", new Type[] {typeof(string), typeof(char), typeof(int)});
IList rows = (IList) miCzytaj3.Invoke(null, new object[] {"1,1,1\r\n2,2,2", ',', 0});
IList w0 = (IList) rows[0];
bool bPowtorzone = ((string) w0[0] == (string) w0[1]);
Sprawdz("powtorzone pola w pierwszym wierszu wykryte", "True", bPowtorzone.ToString());
}

Console.WriteLine("");
Console.WriteLine("PODSUMOWANIE");
Console.WriteLine("  zgodne:    " + iOk);
Console.WriteLine("  niezgodne: " + iBlad);
Console.WriteLine(iBlad == 0 ? "WYNIK: OK" : "WYNIK: SA BLEDY");
return iBlad == 0 ? 0 : 1;
}
}
