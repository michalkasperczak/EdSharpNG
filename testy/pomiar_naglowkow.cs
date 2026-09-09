// Pomiar drzewa naglowkow F6 na PRAWDZIWYM pliku Kasperczaka.
//
// PO CO: w tescie 1.9 (linia z kratka wewnatrz bloku kodu nie moze byc
// naglowkiem) Kasperczak napisal "Blad", ale zaraz potem "niby normalnie nie
// widzi tego testowego bloku kodu" - czyli opis przeczy ocenie. Zamiast zgadywac,
// mierzymy: wczytujemy JEGO plik i wypisujemy dokladnie te naglowki, ktore
// program wyliczy dla drzewa F6, razem z poziomem.
//
// Metoda: refleksja po prywatnych metodach ZBUDOWANEGO EdSharpNG.exe, wiec
// mierzymy kod, ktory realnie poleci u niego, a nie jego kopie.
//
// Budowa i uruchomienie (z WSL):
//   /mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe /nologo \
//       /r:EdSharpNG.exe /out:testy\pomiar_naglowkow.exe testy\pomiar_naglowkow.cs
//   ./testy/pomiar_naglowkow.exe <sciezka do pliku md>

using System;
using System.Collections;
using System.IO;
using System.Reflection;

public class PomiarNaglowkow {

public static int Main(string[] args) {
if (args.Length < 1) {Console.WriteLine("Podaj sciezke do pliku."); return 2;}
string sPath = args[0];
if (!File.Exists(sPath)) {Console.WriteLine("Nie ma pliku: " + sPath); return 2;}
string sText = File.ReadAllText(sPath);

Assembly asm = Assembly.LoadFrom(Path.GetFullPath("EdSharpNG.exe"));
Type tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {
Console.WriteLine("Nie znalazlem klasy MdiFrame. Typy w zbudowanym programie:");
foreach (Type t in asm.GetTypes()) Console.WriteLine("  " + t.FullName);
return 2;
}

MethodInfo miHeadings = tFrame.GetMethod("GetMarkdownSectionHeadings", BindingFlags.NonPublic | BindingFlags.Static);
MethodInfo miFences = tFrame.GetMethod("MarkdownReview_FindFenceRanges", BindingFlags.NonPublic | BindingFlags.Static);
if (miHeadings == null || miFences == null) {Console.WriteLine("Nie znalazlem metod wyliczajacych naglowki."); return 2;}

IList fences = (IList) miFences.Invoke(null, new object[] {sText});
Console.WriteLine("Bloki kodu (ograniczone potrojnym apostrofem) znalezione w pliku: " + fences.Count);
foreach (object o in fences) {
int[] r = (int[]) o;
string sSnippet = sText.Substring(r[0], Math.Min(40, r[1] - r[0])).Replace("\r", " ").Replace("\n", " ");
Console.WriteLine("  znaki " + r[0] + ".." + r[1] + "   poczatek: " + sSnippet);
}

IList headings = (IList) miHeadings.Invoke(null, new object[] {sText});
Console.WriteLine();
Console.WriteLine("Naglowki, ktore program pokaze w drzewie F6: " + headings.Count);
Type tHeading = null;
foreach (object h in headings) {
if (tHeading == null) tHeading = h.GetType();
FieldInfo fiStart = tHeading.GetField("Start");
FieldInfo fiLevel = tHeading.GetField("Level");
FieldInfo fiTitle = tHeading.GetField("Title");
int iStart = (int) fiStart.GetValue(h);
int iLevel = (int) fiLevel.GetValue(h);
string sTitle = (string) fiTitle.GetValue(h);
// Czy naglowek wpadl w blok kodu? Jesli tak, to blad testu 1.9.
bool bInFence = false;
foreach (object o in fences) {int[] r = (int[]) o; if (iStart >= r[0] && iStart < r[1]) {bInFence = true; break;}}
Console.WriteLine("  poziom " + iLevel + "   znak " + iStart + (bInFence ? "   *** WEWNATRZ BLOKU KODU - BLAD ***   " : "   ") + sTitle);
}

// Kontrola negatywna: naglowek udawany wewnatrz bloku kodu MUSI byc pominiety.
// Wstrzykujemy taki blok i sprawdzamy, ze licznik naglowkow sie nie zmienia.
string sInjected = sText + "\r\n\r\n```\r\n# kontrola negatywna, to nie jest naglowek\r\n## ten tez nie\r\n```\r\n";
IList after = (IList) miHeadings.Invoke(null, new object[] {sInjected});
Console.WriteLine();
Console.WriteLine("KONTROLA NEGATYWNA: dorzucony blok kodu z dwiema kratkami.");
Console.WriteLine("  naglowkow przed: " + headings.Count + ", po: " + after.Count);
if (after.Count == headings.Count) Console.WriteLine("  OK - kratki w bloku kodu pominiete.");
else {Console.WriteLine("  BLAD - blok kodu wpuscil " + (after.Count - headings.Count) + " udawanych naglowkow."); return 1;}

// Druga kontrola: ten sam tekst BEZ ogrodzenia musi dolozyc dwa naglowki,
// inaczej pomiar w ogole nie odrozniałby bloku kodu od zwyklego tekstu.
string sBare = sText + "\r\n\r\n# kontrola negatywna, to jest naglowek\r\n## ten tez\r\n";
IList bare = (IList) miHeadings.Invoke(null, new object[] {sBare});
Console.WriteLine("  te same dwie linie BEZ bloku kodu daja naglowkow: " + bare.Count + " (oczekiwano " + (headings.Count + 2) + ")");
if (bare.Count != headings.Count + 2) {Console.WriteLine("  BLAD - pomiar nie rozroznia bloku kodu od tekstu."); return 1;}

return 0;
}
}
