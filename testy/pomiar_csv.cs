// Pomiar czytania i zapisywania CSV - klasa EdSharp.Csv.
//
// Kompiluje sie RAZEM z Csv.cs, bez calego EdSharpNG, bo Csv.cs nie zaleza
// od Windows Forms - dzieki temu pomiar chodzi tu, na Linuksie, pod Mono.
//
// Mierzone sa te pulapki formatu, na ktorych recznie pisane czytniki CSV
// najczesciej sie wykladaja - kazda jako osobny przypadek z ODPOWIEDZIA,
// ktora ma wyjsc, nie tylko "czy sie nie wysypalo".

using System;
using System.Collections.Generic;
using System.Text;
using EdSharp;

public class PomiarCsv {

static int iBlad = 0;
static int iOk = 0;

static void Sprawdz(string sCo, string sOczekiwane, string sWyszlo) {
if (sOczekiwane == sWyszlo) { iOk++; Console.WriteLine("    OK   " + sCo); }
else {
iBlad++;
Console.WriteLine("    BLAD " + sCo);
Console.WriteLine("         mialo byc: [" + sOczekiwane + "]");
Console.WriteLine("         wyszlo:    [" + sWyszlo + "]");
}
}

// Wiersze sklejone kreska, komorki w wierszu slashem - zeby cala tabele dalo
// sie porownac jednym napisem i od razu bylo widac, gdzie sie rozjechala.
static string Splasz(List<List<string>> w) {
List<string> a = new List<string>();
foreach (List<string> r in w) a.Add(string.Join("/", r.ToArray()));
return string.Join(" | ", a.ToArray());
}

public static int Main(string[] args) {

Console.WriteLine("=== 1. Zwyczajny plik z naglowkami");
{
string s = "imie,nazwisko,rok\r\nAnna,Kowalska,1978\r\nJan,Nowak,1965";
Sprawdz("trzy wiersze po trzy pola", "imie/nazwisko/rok | Anna/Kowalska/1978 | Jan/Nowak/1965", Splasz(Csv.Czytaj(s)));
Sprawdz("separator", ",", Csv.RozpoznajSeparator(s).ToString());
}

Console.WriteLine("=== 2. Polski Excel - SREDNIK");
{
// Ten przypadek jest sedno sprawy dla Michala: polski Excel zapisuje CSV
// ze srednikiem, bo przecinek jest u nas znakiem dziesietnym. Wczytany z
// zalozeniem przecinka daje jedna kolumne ze smieciami.
string s = "miasto;ludnosc;powierzchnia\r\nKrakow;800653;326,8\r\nPoznan;546859;261,9";
Sprawdz("separator to srednik", ";", Csv.RozpoznajSeparator(s).ToString());
Sprawdz("przecinek dziesietny ZOSTAJE w polu", "miasto/ludnosc/powierzchnia | Krakow/800653/326,8 | Poznan/546859/261,9", Splasz(Csv.Czytaj(s)));
}

Console.WriteLine("=== 3. Przecinek W SRODKU pola (cudzyslowy)");
{
string s = "nazwa,adres\r\nSzkola,\"Krakow, ul. Dluga 5\"\r\nUrzad,\"Warszawa, Plac Bankowy 3\"";
Sprawdz("adres z przecinkiem to JEDNO pole", "nazwa/adres | Szkola/Krakow, ul. Dluga 5 | Urzad/Warszawa, Plac Bankowy 3", Splasz(Csv.Czytaj(s)));
}

Console.WriteLine("=== 4. Cudzyslow w tresci (podwojony)");
{
string s = "kto,cytat\r\nJan,\"powiedzial \"\"tak\"\" i wyszedl\"";
Sprawdz("\"\" zamienia sie na jeden cudzyslow", "kto/cytat | Jan/powiedzial \"tak\" i wyszedl", Splasz(Csv.Czytaj(s)));
}

Console.WriteLine("=== 5. KONIEC WIERSZA w srodku pola");
{
// Najczestszy blad recznych czytnikow: dzielenie tekstu po liniach.
string s = "id,uwagi\r\n1,\"pierwsza linia\r\ndruga linia\"\r\n2,krotka";
List<List<string>> w = Csv.Czytaj(s);
Sprawdz("dwa wiersze danych, nie trzy", "3", w.Count.ToString());
Sprawdz("pole trzyma oba wiersze", "pierwsza linia\r\ndruga linia", w[1][1]);
}

Console.WriteLine("=== 6. Rozne konce wierszy");
{
Sprawdz("LF (Linux)", "a/b | c/d", Splasz(Csv.Czytaj("a,b\nc,d")));
Sprawdz("CRLF (Windows)", "a/b | c/d", Splasz(Csv.Czytaj("a,b\r\nc,d")));
Sprawdz("CR (stary Mac)", "a/b | c/d", Splasz(Csv.Czytaj("a,b\rc,d")));
Sprawdz("CRLF to JEDEN koniec, nie dwa", "2", Csv.Czytaj("a,b\r\nc,d").Count.ToString());
}

Console.WriteLine("=== 7. Puste pola i ogon pliku");
{
Sprawdz("puste pole w srodku", "a//c", Splasz(Csv.Czytaj("a,,c")));
Sprawdz("plik z koncem linii NIE dodaje pustego wiersza", "1", Csv.Czytaj("a,b\r\n").Count.ToString());
Sprawdz("puste pole na koncu wiersza", "a/b/", Splasz(Csv.Czytaj("a,b,")));
}

Console.WriteLine("=== 8. Spacje");
{
Sprawdz("spacje wokol pola BEZ cudzyslowow - obciete", "a/b", Splasz(Csv.Czytaj(" a , b ")));
Sprawdz("spacje W cudzyslowach - ZOSTAJA", " a ", Csv.Czytaj("\" a \",b")[0][0]);
}

Console.WriteLine("=== 9. Tabulator i kreska pionowa");
{
Sprawdz("tabulator", "\t", Csv.RozpoznajSeparator("a\tb\tc\r\n1\t2\t3").ToString());
Sprawdz("kreska pionowa", "|", Csv.RozpoznajSeparator("a|b|c\r\n1|2|3").ToString());
}

Console.WriteLine("=== 10. Czy to WYGLADA na tabele");
{
char c; int iK, iW;
Sprawdz("zwyczajna tabela: tak", "True", Csv.WygladaNaTabele("a,b\r\n1,2\r\n3,4", out c, out iK, out iW).ToString());
Sprawdz("  kolumny", "2", iK.ToString());
Sprawdz("  wiersze", "3", iW.ToString());

Sprawdz("zwykly tekst bez przecinkow: nie", "False", Csv.WygladaNaTabele("To jest zwykly tekst.\r\nDruga linia tekstu.", out c, out iK, out iW).ToString());
Sprawdz("jeden wiersz: nie", "False", Csv.WygladaNaTabele("a,b,c", out c, out iK, out iW).ToString());
Sprawdz("postrzepione kolumny: nie", "False", Csv.WygladaNaTabele("a,b,c\r\n1,2\r\n3,4,5,6", out c, out iK, out iW).ToString());
Sprawdz("pusty plik: nie", "False", Csv.WygladaNaTabele("", out c, out iK, out iW).ToString());
}

Console.WriteLine("=== 11. Zapis - oslanianie tylko gdy trzeba");
{
List<List<string>> w = new List<List<string>>();
w.Add(new List<string>(new string[] {"nazwa", "adres", "uwaga"}));
w.Add(new List<string>(new string[] {"Szkola", "Krakow, ul. Dluga", "ma \"aneks\""}));
w.Add(new List<string>(new string[] {"Urzad", "Poznan", "dwie\r\nlinie"}));
string s = Csv.Zapisz(w, ',', "\r\n");
string sOcz = "nazwa,adres,uwaga\r\nSzkola,\"Krakow, ul. Dluga\",\"ma \"\"aneks\"\"\"\r\nUrzad,Poznan,\"dwie\r\nlinie\"";
Sprawdz("plik zapisany", sOcz, s);
Sprawdz("proste pola BEZ cudzyslowow", "True", (s.IndexOf("nazwa,adres,uwaga") == 0).ToString());
}

Console.WriteLine("=== 12. Zapis i odczyt daja to samo (cykl)");
{
// Najwazniejszy pomiar dla EDYCJI: uzytkownik otwiera plik, zapisuje go
// bez zmian i tresc musi zostac ta sama. Inaczej samo otwarcie tabeli
// psuje dane.
string[] asProby = new string[] {
"a,b,c\r\n1,2,3",
"nazwa,adres\r\nSzkola,\"Krakow, ul. Dluga 5\"",
"kto,cytat\r\nJan,\"powiedzial \"\"tak\"\"\"",
"id,uwagi\r\n1,\"pierwsza\r\ndruga\"",
"miasto;powierzchnia\r\nKrakow;326,8"
};
foreach (string sProba in asProby) {
char cSep = Csv.RozpoznajSeparator(sProba);
string sPo = Csv.Zapisz(Csv.Czytaj(sProba, cSep, 0), cSep, "\r\n");
Sprawdz("cykl: " + sProba.Replace("\r\n", "\\n").Substring(0, Math.Min(34, sProba.Length)), sProba, sPo);
}
}

Console.WriteLine("=== 13. Duzy plik - czy nie mysli za dlugo");
{
StringBuilder sb = new StringBuilder();
sb.Append("kolumna1,kolumna2,kolumna3,kolumna4,kolumna5\r\n");
for (int i = 0; i < 20000; i++)
sb.Append("wartosc" + i + ",\"pole, z przecinkiem\",123.45,tekst,ostatnie\r\n");
string sDuzy = sb.ToString();

DateTime dtStart = DateTime.Now;
List<List<string>> w = Csv.Czytaj(sDuzy);
double dSek = (DateTime.Now - dtStart).TotalSeconds;

Sprawdz("wszystkie wiersze wczytane", "20001", w.Count.ToString());
Console.WriteLine("    czas dla 20 000 wierszy: " + dSek.ToString("0.00") + " s");
if (dSek > 3.0) { iBlad++; Console.WriteLine("    BLAD za dlugo - powyzej 3 s"); }
else iOk++;
}

Console.WriteLine("");
Console.WriteLine("PODSUMOWANIE");
Console.WriteLine("  zgodne:   " + iOk);
Console.WriteLine("  niezgodne: " + iBlad);
Console.WriteLine(iBlad == 0 ? "WYNIK: OK" : "WYNIK: SA BLEDY");
return iBlad == 0 ? 0 : 1;
}
}
