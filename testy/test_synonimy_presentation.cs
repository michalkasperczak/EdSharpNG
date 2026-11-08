using System;
using System.Reflection;

public static class TestSynonimyPresentation {
static int iFailures;
static void Check(string sWord, string sPart, string sExpected) {
MethodInfo method = typeof(Synonimy).GetMethod("FormatEntry", new Type[] { typeof(string), typeof(string), typeof(int), typeof(int) });
string sActual = method == null ? "[missing FormatEntry]" : (string) method.Invoke(null, new object[] { sWord, sPart, 1, 3 });
if (sActual != sExpected) {
Console.WriteLine("FAIL expected [{0}], got [{1}]", sExpected, sActual);
iFailures++;
}
else Console.WriteLine("PASS " + sExpected);
}
public static int Main() {
Check("chałupa", "n", "chałupa, rzeczownik, 1 z 3");
Check("istotny", "a", "istotny, przymiotnik, 1 z 3");
Check("działać", "v", "działać, czasownik, 1 z 3");
Check("szybko", "r", "szybko, przysłówek, 1 z 3");
Check("inny", "x", "inny, inna część mowy, 1 z 3");
Console.WriteLine("FAILURES=" + iFailures);
return iFailures == 0 ? 0 : 1;
}
}
