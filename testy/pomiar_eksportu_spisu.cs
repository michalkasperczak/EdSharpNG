// Pomiar CALEJ DROGI spisu tresci: dokument -> spis -> eksport pandokiem.
//
// Zlecenie 1787793592380-0, punkt 4: "on chce klikac pozycje spisu w
// wyeksportowanym dokumencie i trafiac do rozdzialu". Sam pomiar kotwic to za
// malo - trzeba sprawdzic, czy spis, ktory program REALNIE wpisze do
// dokumentu, po przejsciu przez pandoca daje dzialajace odsylacze.
//
// Sonda robi to na ZBUDOWANYM EdSharpNG.exe: wolniona jest metoda
// BuildMarkdownContentsText przez refleksje, wynik wklejany na poczatek pliku
// probnego i zapisany do pliku, ktory potem konwertuje skrypt Pythona.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;

public class PomiarEksportu {
public static int Main(string[] args) {
if (args.Length < 2) {
Console.WriteLine("Uzycie: pomiar_eksportu_spisu.exe <wejscie.md> <wyjscie.md>");
return 2;
}

Assembly asm = Assembly.LoadFrom(Path.GetFullPath("EdSharpNG.exe"));
Type tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {Console.WriteLine("Brak typu EdSharp.MdiFrame"); return 3;}

BindingFlags bf = BindingFlags.NonPublic | BindingFlags.Static;
MethodInfo miHeadings = tFrame.GetMethod("GetMarkdownSectionHeadings", bf);
MethodInfo miAnchors = tFrame.GetMethod("BuildMarkdownAnchors", bf);
MethodInfo miContents = tFrame.GetMethod("BuildMarkdownContentsText", bf);
if (miHeadings == null || miAnchors == null || miContents == null) {
Console.WriteLine("Brak jednej z metod spisu tresci");
return 3;
}

string sText = File.ReadAllText(args[0], Encoding.UTF8);
object oH = miHeadings.Invoke(null, new object[] {sText});
object oA = miAnchors.Invoke(null, new object[] {oH});
string sContents = (string) miContents.Invoke(null, new object[] {oH, oA});
File.WriteAllText(args[1], sContents + sText, new UTF8Encoding(false));

Console.WriteLine("naglowkow: " + ((IList) oH).Count);
Console.WriteLine("pozycji spisu: " + ((IList) oA).Count);
Console.WriteLine("zapisane: " + args[1]);
return 0;
} // Main
} // PomiarEksportu
