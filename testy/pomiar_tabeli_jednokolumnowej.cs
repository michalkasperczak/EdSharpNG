// Pomiar: czy tabela JEDNOKOLUMNOWA jest tabela dla NASZEGO wlasnego eksportu
// HTML (ten sam kod, ktory robi podglad w przegladarce) - przez refleksje na
// zbudowanym EdSharpNG.exe.
using System;
using System.Reflection;

public class T {
public static int Main(string[] args) {
Assembly asm = Assembly.LoadFrom(args[0]);
Type t = asm.GetType("EdSharp.MdiFrame");
MethodInfo mi = null;
foreach (MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
if (m.Name == "MarkdownDocumentToHtml") {mi = m; break;}
}
if (mi == null) {Console.WriteLine("BRAK MarkdownDocumentToHtml"); return 3;}
string LF = "\n";

string sJedna = "| A |" + LF + "| --- |" + LF + "| 1 |" + LF;
string sDwie = "| A | B |" + LF + "| --- | --- |" + LF + "| 1 | 2 |" + LF;

string h1 = (string) mi.Invoke(null, new object[] {sJedna, "t"});
string h2 = (string) mi.Invoke(null, new object[] {sDwie, "t"});

bool bJedna = h1.Contains("<table>") && h1.Contains("<th>");
bool bDwie = h2.Contains("<table>") && h2.Contains("<th>");
Console.WriteLine("tabela DWUKOLUMNOWA jest tabela w naszym HTML: " + bDwie + "   (KONTROLA POZYTYWNA)");
Console.WriteLine("tabela JEDNOKOLUMNOWA jest tabela w naszym HTML: " + bJedna);
if (!bJedna) {
int i = h1.IndexOf("<p>");
Console.WriteLine("  co zamiast tabeli: " + (i >= 0 ? h1.Substring(i, Math.Min(120, h1.Length - i)) : "(brak akapitu)"));
}
return (bDwie && bJedna) ? 0 : 1;
}
}
