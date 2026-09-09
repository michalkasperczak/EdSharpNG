// Pomiar na ZBUDOWANEJ binarce: co program realnie robi z koncami wiersza i z
// kodowaniem przy odczycie i zapisie pliku.
//
// POWOD: Kasperczak poprosil, zeby program "zamienial plik na prawdziwe wiersze
// i po zapisaniu tez powinny byc wiersze w Windows".  Zanim cokolwiek
// dolozymy, trzeba WIEDZIEC, co juz sie dzieje - inaczej dorobimy funkcje,
// ktora istnieje, i zepsujemy zachowanie, ktore bylo poprawne.
using System;
using System.IO;
using System.Reflection;
using System.Text;

class T {
static int ok = 0, zle = 0;
static void A(string opis, bool bOk, string detal) {
if (bOk) {ok++; Console.WriteLine("  [OK   ] " + opis + "  " + detal);}
else {zle++; Console.WriteLine("  [FAIL ] " + opis + "  " + detal);}
}

static string Widok(string s) {
StringBuilder sb = new StringBuilder();
foreach (char c in s) {
if (c == '\r') sb.Append("<CR>");
else if (c == '\n') sb.Append("<LF>");
else sb.Append(c);
}
return sb.ToString();
}

static Assembly asm;
static Type tUtil;

static string Konw(string sMetoda, string sArg) {
MethodInfo mi = tUtil.GetMethod(sMetoda, BindingFlags.Public | BindingFlags.Static,
    null, new Type[] {typeof(string)}, null);
if (mi == null) return null;
return (string) mi.Invoke(null, new object[] {sArg});
}

static void Main(string[] args) {
string sExe = args.Length > 0 ? args[0] : "EdSharpNG.exe";
asm = Assembly.LoadFrom(sExe);
tUtil = asm.GetType("EdSharp.Util");   // PULAPKA: typ jest w namespace EdSharp
if (tUtil == null) {Console.WriteLine("BLAD: brak typu EdSharp.Util"); Environment.Exit(2);}

Console.WriteLine("=== helpery konwersji (istnialy przed nami - kontrola pozytywna) ===");
string sMix = "a\r\nb\nc\rd";
A("Convert2WinLineBreak z mieszanki daje same CRLF",
  Konw("Convert2WinLineBreak", sMix) == "a\r\nb\r\nc\r\nd", Widok(Konw("Convert2WinLineBreak", sMix)));
A("Convert2UnixLineBreak daje same LF",
  Konw("Convert2UnixLineBreak", sMix) == "a\nb\nc\nd", Widok(Konw("Convert2UnixLineBreak", sMix)));
A("Convert2MacLineBreak daje same CR",
  Konw("Convert2MacLineBreak", sMix) == "a\rb\rc\rd", Widok(Konw("Convert2MacLineBreak", sMix)));

Console.WriteLine("\n=== NOWE: rozpoznanie rodzaju koncow wiersza w tekscie ===");
MethodInfo miKind = tUtil.GetMethod("DetectLineBreakKind",
    BindingFlags.Public | BindingFlags.Static, null, new Type[] {typeof(string)}, null);
if (miKind == null) {
A("Util.DetectLineBreakKind istnieje w binarce", false, "BRAK METODY");
} else {
Func<string,string> K = delegate(string s) {return (string) miKind.Invoke(null, new object[] {s});};
A("czysty CRLF rozpoznany jako Windows", K("a\r\nb\r\n") == "Windows", "-> " + K("a\r\nb\r\n"));
A("czysty LF rozpoznany jako Unix", K("a\nb\n") == "Unix", "-> " + K("a\nb\n"));
A("czysty CR rozpoznany jako Macintosh", K("a\rb\r") == "Macintosh", "-> " + K("a\rb\r"));
A("mieszanka rozpoznana jako mixed", K("a\r\nb\nc\rd") == "mixed", "-> " + K("a\r\nb\nc\rd"));
A("tekst bez zlamania nie ma rodzaju", K("abc") == "", "-> '" + K("abc") + "'");
A("KONTROLA: CRLF NIE jest raportowany jako Unix mimo obecnosci LF",
  K("a\r\nb\r\n") != "Unix", "-> " + K("a\r\nb\r\n"));
A("KONTROLA: pusty tekst nie rzuca wyjatku", K("") == "", "-> '" + K("") + "'");
A("KONTROLA: CRLF z jednym dodatkowym LF to mieszanka",
  K("a\r\nb\r\nc\nd") == "mixed", "-> " + K("a\r\nb\r\nc\nd"));
}

Console.WriteLine("\n=== NOWE: kodowanie docelowe zapisu (starocie ida na UTF-8) ===");
MethodInfo miSave = tUtil.GetMethod("GetSaveEncoding",
    BindingFlags.Public | BindingFlags.Static, null, new Type[] {typeof(Encoding)}, null);
if (miSave == null) {
A("Util.GetSaveEncoding istnieje w binarce", false, "BRAK METODY");
} else {
Func<Encoding,Encoding> S = delegate(Encoding e) {return (Encoding) miSave.Invoke(null, new object[] {e});};
Encoding en1250 = Encoding.GetEncoding(1250);
Encoding enOut = S(en1250);
A("plik w polskim ANSI (1250) zapisuje sie jako UTF-8",
  enOut != null && enOut.CodePage == 65001, "-> " + (enOut == null ? "null" : enOut.CodePage.ToString()));
Encoding en1252 = S(Encoding.GetEncoding(1252));
A("plik w zachodnim ANSI (1252) tez idzie na UTF-8",
  en1252 != null && en1252.CodePage == 65001, "-> " + en1252.CodePage);
// KONTROLE POZYTYWNE: czego NIE WOLNO ruszyc
Encoding enU16 = S(Encoding.Unicode);
A("KONTROLA: UTF-16 zostaje UTF-16 (nie przerabiamy szerokich)",
  enU16 != null && enU16.CodePage == 1200, "-> " + enU16.CodePage);
Encoding enU8 = S(new UTF8Encoding(true));
A("KONTROLA: UTF-8 zostaje UTF-8", enU8 != null && enU8.CodePage == 65001, "-> " + enU8.CodePage);
Encoding enU16be = S(Encoding.BigEndianUnicode);
A("KONTROLA: UTF-16BE zostaje UTF-16BE", enU16be != null && enU16be.CodePage == 1201, "-> " + enU16be.CodePage);
A("KONTROLA: null (brak informacji) nie wywala metody", S(null) == null || S(null) != null, "przeszlo bez wyjatku");
}

// UWAGA: odczytu pliku ANSI ta sonda NIE mierzy - robi to
// testy/pomiar_kodowania_ansi.cs (20/20) i tam nalezy dokladac asercje o
// kodowaniu.  ODKRYCIE UBOCZNE z probowania tego tutaj, warte zapisania:
// probka polska DLUZSZA, z domieszka zwyklego ASCII ("Zazolc gesla jazn.
// Ogloszenia parafialne na niedziele."), dostaje od Ude strone 1252, a nie
// 1250 - i wtedy polskie litery i tak sie psuja, bo 1252 ich nie ma.
// To zachowanie odziedziczone (decyduje Ude), NIE regresja naprawy z 5.0.36:
// tam probka bez zdania ASCII dostaje 1250 poprawnie.  Zglaszane jako znane
// ograniczenie, do decyzji Kasperczaka, bo obejscie znaczy nadpisywanie
// wyniku detektora zgadywaniem po znakach - a to trafia w pliki niepolskie.

Console.WriteLine("\nzaliczone: " + ok + " z " + (ok + zle));
Environment.Exit(zle == 0 ? 0 : 1);
}
}
