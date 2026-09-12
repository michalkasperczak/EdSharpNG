// POMIAR: przypadki brzegowe dociagania skladnikow (5.0.85).
//
// Trzy rzeczy, ktorych poprzedni pomiar NIE sprawdzil, a od ktorych zalezy, czy
// uzytkownik dostanie sensowne zachowanie:
//
// 1. NAJWIEKSZY skladnik - Pandoc, 42 MB.  Tidy (1,4 MB) udal sie w 1,7 s, ale
//    to nie dowodzi, ze duze pobranie nie przekroczy limitu czasu ani ze
//    rozpakowanie tak duzego archiwum przejdzie.
// 2. BRAK INTERNETU - program musi MILCZEC i zostawic to, co jest, a nie
//    zepsuc dzialajacego narzedzia w polowie podmiany.
// 3. NAZWANIE BRAKU przy konwersji - gdy narzedzia faktycznie nie ma, komunikat
//    ma podac jego nazwe, a nie wiersz polecenia.
//
// Uruchamiac: bash uruchom_pomiar.sh testy/pomiar_skladniki_brzegowe.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;

class PomiarBrzegowe {

static Type tSkl;
static string sConvert;

static void Main() {
string sExe = @"C:\EdSharpBuild\EdSharpNG.exe";
Assembly a = Assembly.LoadFrom(sExe);
tSkl = a.GetType("EdSharp.Skladniki");
sConvert = Path.Combine(Path.GetDirectoryName(sExe), "Convert");

bool b1 = TestPandoc();
bool b2 = TestBezInternetu();
bool b3 = TestNazwaBraku();

Console.WriteLine();
Console.WriteLine("PODSUMOWANIE");
Console.WriteLine("  Pandoc 42 MB:            {0}", b1 ? "OK" : "NIE");
Console.WriteLine("  brak internetu:          {0}", b2 ? "OK" : "NIE");
Console.WriteLine("  nazwanie brakujacego:    {0}", b3 ? "OK" : "NIE");
Console.WriteLine(b1 && b2 && b3 ? "WYNIK: OK" : "WYNIK: sprawdz recznie");
}

static string Uzupelnij(bool bTylkoBrakujace) {
MethodInfo mi = tSkl.GetMethod("SprawdzIUzupelnij");
return (string) mi.Invoke(null, new object[] { bTylkoBrakujace });
}

static IList Brakujace() {
return (IList) tSkl.GetMethod("Brakujace").Invoke(null, null);
}

// 1. NAJWIEKSZY SKLADNIK.  Odkladam pandoca na bok i patrze, czy program
// dociagnie 42 MB w calosci.
static bool TestPandoc() {
Console.WriteLine("=== 1. Pandoc (42 MB) - czy duze pobranie przechodzi");
string sDir = Path.Combine(sConvert, "Pandoc");
string sKopia = Path.Combine(Path.GetTempPath(), "PandocKopia_" + Guid.NewGuid().ToString("N"));
bool bByl = Directory.Exists(sDir);
if (bByl) Directory.Move(sDir, sKopia);

DateTime dt = DateTime.Now;
string sZrobione = Uzupelnij(true);
double dSek = (DateTime.Now - dt).TotalSeconds;

string sExe = Path.Combine(sDir, "pandoc.exe");
bool bJest = File.Exists(sExe);
long lRozmiar = bJest ? new FileInfo(sExe).Length : 0;
Console.WriteLine("    meldunek: \"{0}\"", sZrobione);
Console.WriteLine("    czas: {0:F1} s", dSek);
Console.WriteLine("    pandoc.exe: {0}", bJest ? lRozmiar + " B" : "NIE MA");

// Sprawdzam, ze to DZIALAJACY program, nie sam plik o wlasciwej nazwie.
if (bJest) {
try {
System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(sExe, "--version");
psi.UseShellExecute = false;
psi.RedirectStandardOutput = true;
psi.CreateNoWindow = true;
System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
string sWyj = p.StandardOutput.ReadLine();
p.WaitForExit();
Console.WriteLine("    uruchomiony: {0}", sWyj);
} catch (Exception ex) {
Console.WriteLine("    NIE URUCHAMIA SIE: {0}", ex.Message);
bJest = false;
}
}

if (!bJest && bByl && Directory.Exists(sKopia)) {
Directory.Move(sKopia, sDir);
Console.WriteLine("    pobranie padlo - oddalem poprzedniego pandoca");
} else if (Directory.Exists(sKopia)) {
try { Directory.Delete(sKopia, true); } catch {}
}
Console.WriteLine();
return bJest;
}

// 2. BRAK INTERNETU.  Nie moge wylaczyc sieci na maszynie, wiec odcinam ja
// programowi: podstawiam proxy pod adres, ktory nie odpowiada.  Dla kodu
// pobierajacego to nieodrozninalne od braku lacznosci.
static bool TestBezInternetu() {
Console.WriteLine("=== 2. Brak internetu - czy milczy i nie psuje tego, co jest");
string sDir = Path.Combine(sConvert, "Tidy");
bool bBylo = File.Exists(Path.Combine(sDir, "tidy.exe"));
Console.WriteLine("    Tidy przed proba: {0}", bBylo ? "jest" : "nie ma");

IWebProxy zapasowe = WebRequest.DefaultWebProxy;
bool bOk;
try {
// 9 to port, na ktorym nic nie nasluchuje.
WebRequest.DefaultWebProxy = new WebProxy("127.0.0.1", 9);
DateTime dt = DateTime.Now;
string sZrobione = Uzupelnij(false);
double dSek = (DateTime.Now - dt).TotalSeconds;
Console.WriteLine("    meldunek: \"{0}\" (puste = milczy, tak ma byc)", sZrobione);
Console.WriteLine("    czas: {0:F1} s", dSek);
bool bNadal = File.Exists(Path.Combine(sDir, "tidy.exe"));
Console.WriteLine("    Tidy po probie: {0}", bNadal ? "nadal jest" : "ZNIKNAL (blad)");
bOk = (sZrobione.Length == 0) && (bNadal == bBylo);
} finally {
WebRequest.DefaultWebProxy = zapasowe;
}
Console.WriteLine();
return bOk;
}

// 3. NAZWANIE BRAKU.  Zabieram narzedzie i sprawdzam, czy program potrafi
// powiedziec, KTOREGO brakuje, patrzac na wiersz polecenia konwersji.
static bool TestNazwaBraku() {
Console.WriteLine("=== 3. Czy nazywa brakujace narzedzie przy konwersji");
MethodInfo mi = tSkl.GetMethod("BrakujaceDlaPolecenia");
string sDir = Path.Combine(sConvert, "Xpdf");
string sKopia = Path.Combine(Path.GetTempPath(), "XpdfKopia_" + Guid.NewGuid().ToString("N"));
bool bByl = Directory.Exists(sDir);
if (bByl) Directory.Move(sDir, sKopia);

string sPolecenie = sConvert + @"\Xpdf\pdftotext.exe ""plik.pdf"" wynik.txt";
string sOdp = (string) mi.Invoke(null, new object[] { sPolecenie });
Console.WriteLine("    polecenie z pdftotext.exe, gdy Xpdf NIE MA -> \"{0}\"", sOdp);
bool bOk = (sOdp == "Xpdf");

// Kontrola: polecenie, ktore nie dotyczy zadnego z naszych narzedzi, musi
// dac pusto - inaczej program obwinialby skladniki o cudze bledy.
string sObce = @"C:\Windows\System32\cmd.exe /c echo nic";
string sOdp2 = (string) mi.Invoke(null, new object[] { sObce });
Console.WriteLine("    obce polecenie -> \"{0}\" (musi byc puste)", sOdp2);
bOk = bOk && (sOdp2.Length == 0);

if (bByl && Directory.Exists(sKopia)) Directory.Move(sKopia, sDir);
Console.WriteLine();
return bOk;
}
}
