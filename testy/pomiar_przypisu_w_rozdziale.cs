using System;
using System.Collections;
using System.IO;
using System.Reflection;

// POMIAR PRZEZ REFLEKSJE na ZBUDOWANEJ binarce EdSharpNG.exe: przypis na
// koncu ROZDZIALU (zlecenie Kasperczaka 29.08.2026 01:40).
// Mierzymy PRAWDZIWE metody z .exe, nie kopie logiki.
// PULAPKA: typ to EdSharp.Util / EdSharp.Frame, bo namespace EdSharp.
class T {
    static int pass = 0, fail = 0;
    static Type tFrame;
    static void A(string co, bool ok) {
        Console.WriteLine((ok ? "PASS " : "FAIL ") + co);
        if (ok) pass++; else fail++;
    }
    static MethodInfo M(string nazwa) {
        MethodInfo mi = tFrame.GetMethod(nazwa,
            BindingFlags.NonPublic | BindingFlags.Public
            | BindingFlags.Static | BindingFlags.Instance);
        return mi;
    }
    static int ChapterEnd(string txt, int cur) {
        return (int) M("GetMarkdownFootnoteChapterEnd").Invoke(null, new object[] {txt, cur});
    }
    static string DefAt(string txt, int at, string lab, string tresc) {
        return (string) M("BuildMarkdownFootnoteDefinitionAt").Invoke(null,
            new object[] {txt, at, lab, tresc});
    }

    static void Main() {
        Assembly asm = Assembly.LoadFrom(@"D:\projekty\edsharp-pr\EdSharpNG.exe");
        tFrame = asm.GetType("EdSharp.MdiFrame");
        A("SONDA: typ EdSharp.MdiFrame istnieje", tFrame != null);
        A("SONDA: GetMarkdownFootnoteChapterEnd istnieje w binarce", M("GetMarkdownFootnoteChapterEnd") != null);
        A("SONDA: BuildMarkdownFootnoteDefinitionAt istnieje w binarce", M("BuildMarkdownFootnoteDefinitionAt") != null);
        if (tFrame == null) {Console.WriteLine("WYNIK: 0/1 sonda glucha"); return;}

        // Dokument z trzema rozdzialami.  LF, bo kontrolka normalizuje konce.
        string doc = "# Pierwszy\n\nTresc pierwszego.\n\n# Drugi\n\nTresc drugiego.\n\n# Trzeci\n\nTresc trzeciego.\n";
        int iDrugi = doc.IndexOf("Tresc drugiego");
        int iPierwszy = doc.IndexOf("Tresc pierwszego");
        int iTrzeci = doc.IndexOf("Tresc trzeciego");

        // 4-6: KONIEC ROZDZIALU to koniec TEGO rozdzialu, nie dokumentu.
        int endDrugi = ChapterEnd(doc, iDrugi);
        A("koniec rozdzialu 2 nie jest koncem dokumentu", endDrugi != doc.Length && endDrugi > 0);
        A("koniec rozdzialu 2 lezy PRZED naglowkiem rozdzialu 3",
          endDrugi <= doc.IndexOf("# Trzeci"));
        A("koniec rozdzialu 2 lezy ZA trescia rozdzialu 2",
          endDrugi >= iDrugi + "Tresc drugiego.".Length);

        // 7-8: kazdy rozdzial daje INNY koniec - kontrola roznicujaca, bez niej
        // metoda zwracajaca stala tez by przeszla.
        int endPierwszy = ChapterEnd(doc, iPierwszy);
        int endTrzeci = ChapterEnd(doc, iTrzeci);
        A("KONTROLA ROZNICUJACA: trzy rozdzialy daja trzy rozne konce",
          endPierwszy != endDrugi && endDrugi != endTrzeci && endPierwszy < endDrugi);
        A("ostatni rozdzial konczy sie na koncu tresci dokumentu",
          endTrzeci >= iTrzeci && endTrzeci <= doc.Length);

        // 9-11: KONTROLA NEGATYWNA - gdy rozdzialu NIE MA, metoda musi zwrocic
        // -1, zeby wolajacy spadl na koniec dokumentu.  Bez tego wstawialaby
        // tresc w losowe miejsce dokumentu bez naglowkow.
        A("KONTROLA NEG.: dokument bez naglowkow zwraca -1",
          ChapterEnd("Zwykly tekst bez naglowkow.\nDrugi wiersz.\n", 5) == -1);
        A("KONTROLA NEG.: pusty dokument zwraca -1", ChapterEnd("", 0) == -1);
        A("KONTROLA NEG.: kursor PRZED pierwszym naglowkiem zwraca -1",
          ChapterEnd("Wstep przed naglowkiem.\n\n# Rozdzial\n\nTresc.\n", 3) == -1);

        // 12-15: ODSTEPY.  Tresc wstawiana w SRODEK musi miec puste wiersze z
        // obu stron, inaczej nie jest przypisem dla konwertera.
        string d1 = DefAt(doc, endDrugi, "1", "Tresc przypisu.");
        A("definicja zawiera znacznik i tresc", d1.Contains("[^1]: Tresc przypisu."));
        A("definicja ma odstep PRZED (nie sklei sie z akapitem)", d1.StartsWith("\n"));
        A("definicja ma pusty wiersz PO (nie sklei sie z naglowkiem)", d1.EndsWith("\n\n"));
        string dEnd = DefAt(doc, doc.Length, "2", "Na koncu.");
        A("KONTROLA: na koncu dokumentu wystarcza jeden konczacy konc wiersza",
          dEnd.EndsWith("\n") && !dEnd.EndsWith("\n\n"));

        // 16-17: ZLOZENIE calosci - po wstawieniu tresc MUSI wyladowac w swoim
        // rozdziale, czyli PRZED naglowkiem nastepnego.  To jest sedno jego
        // zlecenia: po podzieleniu pliku przypis zostaje przy rozdziale.
        string wynik = doc.Substring(0, endDrugi) + d1 + doc.Substring(endDrugi);
        int posDef = wynik.IndexOf("[^1]: Tresc przypisu.");
        int posTrzeci = wynik.IndexOf("# Trzeci");
        A("tresc przypisu ZOSTALA w swoim rozdziale (przed naglowkiem nastepnego)",
          posDef > 0 && posDef < posTrzeci);
        A("KONTROLA: rozdzialy nie zostaly uszkodzone",
          wynik.Contains("# Pierwszy") && wynik.Contains("# Drugi")
          && wynik.Contains("# Trzeci") && wynik.Contains("Tresc drugiego."));

        // 18: KONTROLA, ze wariant "koniec dokumentu" NADAL dziala - to jest
        // dotychczasowe, dzialajace zachowanie i nie wolno go zepsuc.
        MethodInfo miOld = M("BuildMarkdownFootnoteDefinition");
        string dOld = (string) miOld.Invoke(null, new object[] {doc, "9", "Stara droga."});
        A("KONTROLA: stara droga (koniec dokumentu) nadal buduje definicje",
          dOld.Contains("[^9]: Stara droga."));

        Console.WriteLine();
        Console.WriteLine("WYNIK: " + pass + "/" + (pass + fail));
    }
}
