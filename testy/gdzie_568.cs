// DIAGNOSTYKA: gdzie w binarce lezy wolanie Focus() z okolicy
// FocusChildEditControl.  Sonda pomiar_568 oblala asercje "FocusChildEditControl
// wola Focus", a podejrzenie jest takie, ze cialo siedzi w metodzie
// GENEROWANEJ dla domkniecia BeginInvoke - dokladnie pulapka opisana w
// pomiar_567.cs.  Ten program NIE ocenia, tylko wypisuje fakty.

using System;
using System.IO;
using System.Reflection;

class Gdzie568 {

static bool Wola(MethodBase mb, string sNazwa) {
    if (mb == null) return false;
    MethodBody body = null;
    try { body = mb.GetMethodBody(); } catch { return false; }
    if (body == null) return false;
    byte[] aIl = body.GetILAsByteArray();
    if (aIl == null) return false;
    Module mod = mb.Module;
    for (int i = 0; i + 4 < aIl.Length; i++) {
        if (aIl[i] != 0x28 && aIl[i] != 0x6F && aIl[i] != 0x73) continue;
        int iToken = BitConverter.ToInt32(aIl, i + 1);
        MemberInfo mi = null;
        try { mi = mod.ResolveMember(iToken); } catch { continue; }
        if (mi != null && mi.Name == sNazwa) return true;
    }
    return false;
}

static void Main(string[] aArgs) {
    string sBin = (aArgs.Length > 0) ? aArgs[0] : "EdSharpNG.exe";
    Assembly asm = Assembly.LoadFile(Path.GetFullPath(sBin));
    Console.WriteLine("Binarka: " + asm.Location);
    Console.WriteLine();
    Console.WriteLine("--- metody wolajace Focus, z nazwa zawierajaca 'Focus' albo 'b__' ---");
    foreach (Type t in asm.GetTypes()) {
        foreach (MethodInfo mi in t.GetMethods(BindingFlags.Instance | BindingFlags.Static
                                             | BindingFlags.Public | BindingFlags.NonPublic
                                             | BindingFlags.DeclaredOnly)) {
            if (!Wola(mi, "Focus")) continue;
            if (mi.Name.IndexOf("Focus") < 0 && mi.Name.IndexOf("b__") < 0) continue;
            Console.WriteLine("  " + t.FullName + " :: " + mi.Name);
        }
    }
    Console.WriteLine();
    Console.WriteLine("--- metody MdiFrame o nazwie z 'FocusChild' ---");
    foreach (Type t in asm.GetTypes()) {
        if (t.Name != "MdiFrame") continue;
        foreach (MethodInfo mi in t.GetMethods(BindingFlags.Instance | BindingFlags.Static
                                             | BindingFlags.Public | BindingFlags.NonPublic
                                             | BindingFlags.DeclaredOnly)) {
            if (mi.Name.IndexOf("FocusChild") < 0 && mi.Name.IndexOf("b__") < 0) continue;
            Console.WriteLine("  " + mi.Name + "  (wola Focus: " + Wola(mi, "Focus") + ")");
        }
    }
}

}
