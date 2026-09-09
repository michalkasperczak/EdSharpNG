// KONTROLA, ze naprawa wiersza kreskek NIE ZEPSULA istniejacego zachowania.
// Wszystko przez refleksje na zbudowanym EdSharpNG.exe.
using System;
using System.Reflection;

public class T {
static int iPass = 0, iFail = 0;
static void Ok(bool b, string s, string sGot) {
if (b) {iPass++; Console.WriteLine("PASS " + s);}
else {iFail++; Console.WriteLine("FAIL " + s + "  -> " + sGot);}
}

public static int Main(string[] args) {
Assembly asm = Assembly.LoadFrom(args[0]);
Type t = asm.GetType("EdSharp.MdiFrame");
MethodInfo miHtml = null;
foreach (MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
if (m.Name == "MarkdownDocumentToHtml") {miHtml = m; break;}
}
string LF = "\n";

// 1-3. POZIOMA LINIA nadal jest pozioma linia, nie tabela.  To glowne ryzyko
// tej naprawy: gdyby "---" wpadlo na wiersz kreskek, kazdy dokument z
// pozioma linia dostal by tabele-widmo.
string[] aLinie = new string[] {"tekst" + LF + LF + "---" + LF + LF + "dalej" + LF,
                               "tekst" + LF + LF + "***" + LF + LF + "dalej" + LF,
                               "tekst" + LF + LF + "- - -" + LF + LF + "dalej" + LF};
string[] aNazwy = new string[] {"trzy myslniki", "trzy gwiazdki", "myslniki ze spacjami"};
for (int i = 0; i < aLinie.Length; i++) {
string h = (string) miHtml.Invoke(null, new object[] {aLinie[i], "t"});
Ok(!h.Contains("<table>"), (i + 1) + ". pozioma linia (" + aNazwy[i] + ") NIE jest tabela", "zawiera <table>");
}

// 4. Pozioma linia nadal daje <hr> - kontrola POZYTYWNA, ze wciaz jest
// rozpoznawana jako cokolwiek.
string hHr = (string) miHtml.Invoke(null, new object[] {"a" + LF + LF + "---" + LF + LF + "b" + LF, "t"});
Ok(hHr.Contains("<hr"), "4. pozioma linia nadal daje <hr> (kontrola pozytywna)", hHr.Length + " znakow bez <hr>");

// 5. Punktowana lista nie jest tabela
string hLi = (string) miHtml.Invoke(null, new object[] {"- jeden" + LF + "- dwa" + LF, "t"});
Ok(hLi.Contains("<li>") && !hLi.Contains("<table>"), "5. lista punktowana nadal lista, nie tabela", hLi.Contains("<table>") ? "tabela!" : "brak <li>");

// 6-7. Tabela dwukolumnowa dziala jak dotad (nagłówki th)
string h2 = (string) miHtml.Invoke(null, new object[] {"| A | B |" + LF + "| --- | --- |" + LF + "| 1 | 2 |" + LF, "t"});
Ok(h2.Contains("<table>"), "6. tabela dwukolumnowa nadal tabela", "brak");
Ok(h2.Contains("<th>A</th>") && h2.Contains("<td>1</td>"), "7. naglowki th i komorki td na swoich miejscach", "brak");

// 8. Wiersz kreskek NIE trafia do tresci tabeli jako wiersz danych
Ok(!h2.Contains("<td>---</td>") && !h2.Contains("<th>---</th>"), "8. wiersz kreskek nie stal sie wierszem danych", "kreski w komorce");

// 9. Wyrownania kolumn nadal rozpoznawane jako wiersz kreskek
string h3 = (string) miHtml.Invoke(null, new object[] {"| A | B |" + LF + "| :--- | ---: |" + LF + "| 1 | 2 |" + LF, "t"});
Ok(h3.Contains("<table>") && !h3.Contains(":---"), "9. wyrownania kolumn nadal ukrywane", "brak");

// 10. Tabela w bloku kodu nadal NIE jest tabela
string hFence = (string) miHtml.Invoke(null, new object[] {"```" + LF + "| A |" + LF + "| --- |" + LF + "```" + LF, "t"});
Ok(!hFence.Contains("<table>"), "10. tabela w bloku kodu nadal nietkniete zrodlo", "zrobil tabele");

// 11. Cztery kreski to nadal pozioma linia, nie tabela
string h4 = (string) miHtml.Invoke(null, new object[] {"a" + LF + LF + "----" + LF + LF + "b" + LF, "t"});
Ok(!h4.Contains("<table>"), "11. cztery myslniki nadal nie tabela", "tabela!");

Console.WriteLine();
Console.WriteLine("WYNIK: " + iPass + "/" + (iPass + iFail));
return iFail == 0 ? 0 : 1;
}
}
