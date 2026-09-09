// Diagnostyka jednej oblanej asercji kontroli negatywnej harnessu 570:
// "droga 5.0.69 kladla sam TEKST ze sciezkami" dala ZLE.  Pytanie: czy
// Clipboard.SetText w ogole doszedl, czy tylko ODCZYT go nie widzi.
using System;
using System.IO;
using System.Windows.Forms;

public class Gdzie570 {
[STAThread]
public static int Main(string[] a) {
    string sKat = Path.Combine(Path.GetTempPath(), "edsharp_drop570");
    Directory.CreateDirectory(sKat);
    string sA = Path.Combine(sKat, "pierwszy.txt");
    File.WriteAllText(sA, "A");

    Console.WriteLine("PRZED: ContainsText=" + Clipboard.ContainsText());
    try { Clipboard.SetText(sA + "\r\n"); Console.WriteLine("SetText: bez wyjatku"); }
    catch (Exception ex) { Console.WriteLine("SetText RZUCIL: " + ex.GetType().Name + " " + ex.Message); }

    Console.WriteLine("PO: ContainsText=" + Clipboard.ContainsText());
    string sGot = null;
    try { sGot = Clipboard.GetText(); } catch (Exception ex) { Console.WriteLine("GetText RZUCIL: " + ex.GetType().Name); }
    Console.WriteLine("GetText dlugosc=" + ((sGot == null) ? -1 : sGot.Length));
    Console.WriteLine("GetText tresc=[" + sGot + "]");
    Console.WriteLine("zawiera pierwszy.txt: " + (sGot != null && sGot.Contains("pierwszy.txt")));

    // Czy schowek widzi PONOWIONY odczyt (jak w Util.GetClipboardText)?
    for (int i = 0; i < 10; i++) {
        try {
            if (Clipboard.ContainsText()) { Console.WriteLine("proba " + i + ": tekst JEST"); break; }
            Console.WriteLine("proba " + i + ": tekstu nie ma");
        } catch (Exception) { Console.WriteLine("proba " + i + ": wyjatek"); }
        System.Threading.Thread.Sleep(40);
    }
    return 0;
}
}
