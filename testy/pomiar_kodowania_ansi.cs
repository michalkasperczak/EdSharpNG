using System;
using System.Reflection;
using System.Text;
public class P {
  static int iPass = 0, iFail = 0;
  public static void Main() {
    Assembly asm = Assembly.LoadFrom(@"C:\tmp\enc\EdSharpNG.exe");
    Type tUtil = asm.GetType("EdSharp.Util");
    if (tUtil == null) { Console.WriteLine("SONDA GLUCHA: brak typu EdSharp.Util"); Environment.Exit(2); }
    MethodInfo miGet = tUtil.GetMethod("GetFileEncoding", BindingFlags.Public|BindingFlags.Static, null, new Type[]{typeof(string)}, null);
    MethodInfo miStrict = tUtil.GetMethod("IsStrictUtf8", BindingFlags.Public|BindingFlags.Static);
    if (miGet == null) { Console.WriteLine("SONDA GLUCHA: brak GetFileEncoding(string)"); Environment.Exit(2); }
    Console.WriteLine("KONTROLA POZYTYWNA SONDY: GetFileEncoding obecne, IsStrictUtf8 " + (miStrict==null?"BRAK (stara binarka)":"obecne"));
    Console.WriteLine();

    // 1-4: polskie litery przezywaja odczyt
    Check("pl_cp1250.txt", miGet, "Zażółć gęślą jaźń.", 1250);
    Check("pl_utf8_nobom.txt", miGet, "Zażółć gęślą jaźń.", 65001);
    Check("pl_utf8_bom.txt", miGet, "Zażółć gęślą jaźń.", 65001);
    // KONTROLA: rosyjski cp1251 - Ude go rozpoznaje, wiec nasza bramka NIE moze go tknac
    CheckCodePageNot("ru_cp1251.txt", miGet, 1250, "rosyjski nie moze wpasc na polskie ANSI");
    // KONTROLA: niemiecki cp1252 - Ude rozpoznaje, zostaje 1252
    Check("de_cp1252.txt", miGet, "Die Straße war schön", 1252);
    // KONTROLA: czysty ASCII musi zostac UTF-8 (nie ANSI)
    System.IO.File.WriteAllText(@"C:\tmp\enc\ascii.txt", "Plain ASCII only, no accents at all.\r\n", new UTF8Encoding(false));
    Check("ascii.txt", miGet, "Plain ASCII only", 65001);

    if (miStrict != null) {
      Assert((bool)miStrict.Invoke(null, new object[]{ Encoding.UTF8.GetBytes("Zażółć") }), "IsStrictUtf8: poprawny UTF-8 = true");
      Assert(!(bool)miStrict.Invoke(null, new object[]{ Encoding.GetEncoding(1250).GetBytes("Zażółć") }), "IsStrictUtf8: bajty cp1250 = false");
      Assert((bool)miStrict.Invoke(null, new object[]{ new byte[0] }), "IsStrictUtf8: pusty = true");
      Assert((bool)miStrict.Invoke(null, new object[]{ Encoding.ASCII.GetBytes("abc") }), "IsStrictUtf8: ASCII = true");
    }
    Console.WriteLine();
    Console.WriteLine("WYNIK: " + iPass + "/" + (iPass+iFail) + " PASS");
    Environment.Exit(iFail == 0 ? 0 : 1);
  }
  static void Check(string sFile, MethodInfo miGet, string sExpect, int iCp) {
    string path = @"C:\tmp\enc\" + sFile;
    try {
      Encoding en = (Encoding) miGet.Invoke(null, new object[]{path});
      string s = System.IO.File.ReadAllText(path, en);
      Assert(s.IndexOf(sExpect) >= 0, sFile + ": tekst \"" + sExpect + "\" odczytany bez utraty znakow (cp" + en.CodePage + ")");
      Assert(en.CodePage == iCp, sFile + ": strona kodowa " + iCp + " (jest " + en.CodePage + ")");
      Assert(s.IndexOf('\uFFFD') < 0, sFile + ": ZERO znakow zastepczych U+FFFD");
    } catch (Exception ex) { Assert(false, sFile + ": WYJATEK " + ex.GetBaseException().Message); }
  }
  static void CheckCodePageNot(string sFile, MethodInfo miGet, int iForbidden, string sWhy) {
    string path = @"C:\tmp\enc\" + sFile;
    Encoding en = (Encoding) miGet.Invoke(null, new object[]{path});
    Assert(en.CodePage != iForbidden, sFile + ": " + sWhy + " (dostal cp" + en.CodePage + ")");
  }
  static void Assert(bool b, string s) {
    Console.WriteLine((b ? "  PASS  " : "  FAIL  ") + s);
    if (b) iPass++; else iFail++;
  }
}
