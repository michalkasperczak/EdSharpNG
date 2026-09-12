using System;
using System.Collections.Generic;
using System.Reflection;
class T {
static void Main() {
Assembly a = Assembly.LoadFrom(@"C:\EdSharpBuild\EdSharpNG.exe");
Type t = a.GetType("EdSharp.Pisownia");
Console.WriteLine("klasa Pisownia w programie: {0}", t != null ? "jest" : "BRAK");
MethodInfo mD = t.GetMethod("Dostepne");
MethodInfo mJ = t.GetMethod("Jezyk");
MethodInfo mS = t.GetMethod("Sprawdz");
Console.WriteLine("dostepne: {0}", mD.Invoke(null, null));
Console.WriteLine("jezyk: {0}", mJ.Invoke(null, null));
object o = mS.Invoke(null, new object[] {"Ten tekst ma wszystkkich bledow i ktury raz napewno."});
System.Collections.IEnumerable en = (System.Collections.IEnumerable) o;
int n = 0;
foreach (object b in en) {
Type tb = b.GetType();
string sl = (string) tb.GetField("Slowo").GetValue(b);
System.Collections.IEnumerable p = (System.Collections.IEnumerable) tb.GetField("Podpowiedzi").GetValue(b);
List<string> lp = new List<string>();
foreach (object x in p) { lp.Add(x.ToString()); if (lp.Count >= 3) break; }
Console.WriteLine("  blad: {0,-14} -> {1}", sl, String.Join(", ", lp.ToArray()));
n++;
}
Console.WriteLine("bledow znalezionych: {0}", n);
// Oczekuje 4: wszystkkich, bledow (bez ogonkow!), ktury, napewno.  Pierwsza
// wersja testu czekala na 3 i przeoczyla, ze "bledow" TEZ jest bledem.
Console.WriteLine(n == 4 ? "WYNIK: OK" : "WYNIK: sprawdz recznie");
}
}
