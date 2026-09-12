// POMIAR: czy gotowy program NAPRAWDE dociaga skladniki (5.0.85, zadanie 8).
//
// PO CO OSOBNY POMIAR.  Pomiar zrodel (pomiar_skladniki.cs) sprawdzil tylko,
// ze serwery odpowiadaja.  To NIE dowodzi, ze moj kod w programie pobiera,
// rozpakowuje i uklada pliki tam, gdzie EdSharp ich szuka.  Tutaj wolam
// prawdziwa klase Skladniki z ZBUDOWANEGO EdSharpNG.exe.
//
// Biore Tidy (1,4 MB), nie Pandoca (42 MB) - ten sam mechanizm, a pomiar
// konczy sie w kilka sekund.  Pandoc jest wolany dokladnie ta sama sciezka
// kodu.
//
// Uruchamiac: bash uruchom_pomiar.sh testy/pomiar_skladniki_w_programie.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

class PomiarSkladnikowWProgramie {

static void Main() {
string sExe = @"C:\EdSharpBuild\EdSharpNG.exe";
Assembly a = Assembly.LoadFrom(sExe);
Type tSkl = a.GetType("EdSharp.Skladniki");
if (tSkl == null) { Console.WriteLine("BLAD: brak klasy EdSharp.Skladniki w programie"); return; }

// Program szuka skladnikow obok siebie - czyli w katalogu, z ktorego
// zostal zaladowany.
string sConvert = Path.Combine(Path.GetDirectoryName(sExe), "Convert");
Console.WriteLine("katalog skladnikow: {0}", sConvert);
Console.WriteLine();

Console.WriteLine("=== 1. Co program widzi jako BRAKUJACE");
MethodInfo miBrak = tSkl.GetMethod("Brakujace");
IList lBrak = (IList) miBrak.Invoke(null, null);
Console.WriteLine("brakuje: {0}", lBrak.Count == 0 ? "(nic)" : string.Join(", ", Wypisz(lBrak)));
Console.WriteLine();

// Zabieram Tidy, zeby zmierzyc DOCIAGANIE, a nie sam fakt, ze cos lezy na
// dysku.  Kopie odkladam - gdyby pobranie padlo, oddaje stan sprzed pomiaru.
string sTidy = Path.Combine(sConvert, "Tidy");
string sKopia = Path.Combine(Path.GetTempPath(), "TidyKopia_" + Guid.NewGuid().ToString("N"));
bool bBylTidy = Directory.Exists(sTidy);
if (bBylTidy) {
Console.WriteLine("=== 2. Zabieram Tidy, zeby zmierzyc pobieranie");
Directory.Move(sTidy, sKopia);
Console.WriteLine("Tidy odlozony na czas pomiaru");
} else {
Console.WriteLine("=== 2. Tidy i tak nie ma - mierze pobieranie od zera");
}
lBrak = (IList) miBrak.Invoke(null, null);
Console.WriteLine("teraz brakuje: {0}", string.Join(", ", Wypisz(lBrak)));
bool bTidyWBraku = false;
foreach (object o in lBrak) if (o.ToString() == "Tidy") bTidyWBraku = true;
Console.WriteLine("czy program zauwazyl brak Tidy: {0}", bTidyWBraku ? "TAK" : "NIE (blad)");
Console.WriteLine();

Console.WriteLine("=== 3. Wolam dociaganie brakujacych (tak jak przy starcie)");
MethodInfo miUzup = tSkl.GetMethod("SprawdzIUzupelnij");
DateTime dtStart = DateTime.Now;
string sZrobione = (string) miUzup.Invoke(null, new object[] { true });
double dSek = (DateTime.Now - dtStart).TotalSeconds;
Console.WriteLine("zwrocony meldunek: \"{0}\"", sZrobione);
Console.WriteLine("czas: {0:F1} s", dSek);
Console.WriteLine();

Console.WriteLine("=== 4. Czy plik naprawde lezy tam, gdzie program go szuka");
string sTidyExe = Path.Combine(sTidy, "tidy.exe");
bool bJestPlik = File.Exists(sTidyExe);
// Szukam tez glebiej - program przeszukuje cale drzewo.
if (!bJestPlik && Directory.Exists(sTidy)) {
string[] aZnalezione = Directory.GetFiles(sTidy, "tidy.exe", SearchOption.AllDirectories);
if (aZnalezione.Length > 0) { bJestPlik = true; sTidyExe = aZnalezione[0]; }
}
Console.WriteLine("tidy.exe: {0}", bJestPlik ? sTidyExe : "NIE MA");
if (bJestPlik) Console.WriteLine("rozmiar: {0} B", new FileInfo(sTidyExe).Length);

Console.WriteLine();
Console.WriteLine("=== 5. Czy zapisal numer wersji");
string sLock = Path.Combine(sConvert, "Tools.lock");
if (File.Exists(sLock)) {
foreach (string sLinia in File.ReadAllLines(sLock))
if (sLinia.StartsWith("Tidy")) Console.WriteLine("w Tools.lock: {0}", sLinia);
} else Console.WriteLine("Tools.lock NIE ISTNIEJE (blad)");

Console.WriteLine();
Console.WriteLine("=== 6. Czy sensownie nazywa brakujacy skladnik przy konwersji");
MethodInfo miDla = tSkl.GetMethod("BrakujaceDlaPolecenia");
// Udaje polecenie konwersji przez narzedzie, ktorego NIE MA.
string sNieistniejacy = Path.Combine(sConvert, "NicTakiego");
string sOdp = (string) miDla.Invoke(null, new object[] {
  sConvert + @"\Pandoc\pandoc.exe plik.docx -t html" });
Console.WriteLine("dla polecenia z pandoc.exe: \"{0}\" (puste = pandoc jest na miejscu)", sOdp);

// Oddaje stan sprzed pomiaru, gdy pobranie padlo.
if (bBylTidy && !bJestPlik) {
if (Directory.Exists(sKopia)) { Directory.Move(sKopia, sTidy); Console.WriteLine("\npobranie padlo - oddalem poprzedni Tidy"); }
} else if (Directory.Exists(sKopia)) {
try { Directory.Delete(sKopia, true); } catch {}
}

Console.WriteLine();
bool bOk = bTidyWBraku && bJestPlik && sZrobione.Contains("Tidy");
Console.WriteLine(bOk ? "WYNIK: OK - program sam dociagnal brakujacy skladnik" : "WYNIK: sprawdz recznie");
}

static string[] Wypisz(IList l) {
List<string> ls = new List<string>();
foreach (object o in l) ls.Add(o.ToString());
return ls.ToArray();
}
}
