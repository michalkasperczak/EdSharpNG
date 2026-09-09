// DIAGNOSTYKA 5.0.71: dlaczego asercja "wolanie App.VersionString" oblewa.
// Wypisuje literaly znalezione w ciele obslugi menu i ElevateVersion, zeby
// rozstrzygnac, czy numer wersji jest tam wpisany przez kompilator (const jest
// INLINOWANE, wiec zadnego ldsfld nie bedzie) czy nie ma go wcale.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

class Gdzie571 {

static byte[] Il(MethodBase mb) {
    MethodBody body = null;
    try { body = mb.GetMethodBody(); } catch { return null; }
    if (body == null) return null;
    try { return body.GetILAsByteArray(); } catch { return null; }
}

static void Wypisz(MethodBase mb, string sFiltr) {
    byte[] aIl = Il(mb);
    if (aIl == null) return;
    Module mod = mb.Module;
    for (int i = 0; i + 4 < aIl.Length; i++) {
        if (aIl[i] != 0x72) continue;
        int iToken = BitConverter.ToInt32(aIl, i + 1);
        string s = null;
        try { s = mod.ResolveString(iToken); } catch { continue; }
        if (s == null) continue;
        if (sFiltr.Length > 0 && s.IndexOf(sFiltr, StringComparison.Ordinal) < 0) continue;
        Console.WriteLine("  [" + mb.DeclaringType.Name + "." + mb.Name + "] literal: "
            + s.Replace("\n", "\\n").Substring(0, Math.Min(120, s.Length)));
    }
}

static List<MethodBase> Metody(Type t, string sNazwa) {
    List<MethodBase> l = new List<MethodBase>();
    if (t == null) return l;
    BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    foreach (MethodInfo mi in t.GetMethods(bf))
        if (mi.Name.IndexOf(sNazwa, StringComparison.Ordinal) >= 0) l.Add(mi);
    foreach (Type tn in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        foreach (MethodInfo mi in tn.GetMethods(bf))
            if (mi.Name.IndexOf(sNazwa, StringComparison.Ordinal) >= 0) l.Add(mi);
    return l;
}

static int Main(string[] args) {
    string sExe = (args.Length > 0) ? args[0] : "EdSharpNG.exe";
    Assembly asm = Assembly.LoadFile(Path.GetFullPath(sExe));
    Type tFrame = asm.GetType("EdSharp.MdiFrame");
    Type tApp = asm.GetType("EdSharp.App");

    FieldInfo fi = tApp.GetField("VersionString", BindingFlags.Public | BindingFlags.Static);
    Console.WriteLine("VersionString: IsLiteral=" + fi.IsLiteral + " IsStatic=" + fi.IsStatic
        + " wartosc=" + fi.GetRawConstantValue());
    Console.WriteLine("(IsLiteral=True znaczy const: kompilator WSTAWIA wartosc w miejsce uzycia,");
    Console.WriteLine(" wiec w IL nie ma zadnego ldsfld - pytanie o wolanie stalej jest bledne.)");
    Console.WriteLine();

    Console.WriteLine("LITERALY '5.0.71' w obsludze menu:");
    foreach (MethodBase mb in Metody(tFrame, "menuItem_Click")) Wypisz(mb, "5.0.71");
    Console.WriteLine();
    Console.WriteLine("LITERALY '5.0.71' w ElevateVersion:");
    foreach (MethodBase mb in Metody(tFrame, "ElevateVersion")) Wypisz(mb, "5.0.71");
    Console.WriteLine();
    Console.WriteLine("LITERALY 'beta' w obsludze menu:");
    foreach (MethodBase mb in Metody(tFrame, "menuItem_Click")) Wypisz(mb, "beta");
    Console.WriteLine();
    Console.WriteLine("LITERALY 'No release' w ElevateVersion:");
    foreach (MethodBase mb in Metody(tFrame, "ElevateVersion")) Wypisz(mb, "No release");
    return 0;
}
}
