using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// POMIAR PRZEZ REFLEKSJE na ZBUDOWANEJ binarce EdSharpNG.exe: WSTAWIANIE LINKU
// pod Control+K (ustalenie edsharpng-9, zlecenie 1788041000816-5).
// Mierzymy PRAWDZIWE metody z .exe, nie kopie logiki.
// PULAPKA: typ to EdSharp.MdiFrame, bo namespace EdSharp.
//
// Czego ta sonda NIE mierzy: samego okna i mowy czytnika.  Okno buduje sie na
// wspolnym LbcDialog, ktory ma wlasne sondy (litery dostepu 14/14, kolizje
// 8/8), a mowe rozstrzyga maszyna Kasperczaka.  Tu mierzymy rzecz, ktorej
// zielony build NIE wylapie: czy kotwica linku wewnetrznego jest LICZONA TAK
// SAMO jak w spisie tresci.  Gdyby sie rozjechala, link po eksporcie do Worda
// prowadzilby w nicosc, a niewidomy nie ma jak tego zobaczyc.
class T {
    static int pass = 0, fail = 0;
    static Type tFrame;
    static void A(string co, bool ok) {
        Console.WriteLine((ok ? "PASS " : "FAIL ") + co);
        if (ok) pass++; else fail++;
    }
    static MethodInfo M(string nazwa) {
        return tFrame.GetMethod(nazwa,
            BindingFlags.NonPublic | BindingFlags.Public
            | BindingFlags.Static | BindingFlags.Instance);
    }
    // Kotwice policzone TA SAMA droga, ktorej uzywa nasze okno linku: naglowki
    // z dokumentu -> BuildMarkdownAnchors.
    static List<string> Kotwice(string txt) {
        object headings = M("GetMarkdownSectionHeadings").Invoke(null, new object[] {txt});
        object anchors = M("BuildMarkdownAnchors").Invoke(null, new object[] {headings});
        List<string> wynik = new List<string>();
        foreach (object o in (IEnumerable) anchors) wynik.Add((string) o);
        return wynik;
    }
    static string Spis(string txt) {
        object headings = M("GetMarkdownSectionHeadings").Invoke(null, new object[] {txt});
        object anchors = M("BuildMarkdownAnchors").Invoke(null, new object[] {headings});
        return (string) M("BuildMarkdownContentsText").Invoke(null, new object[] {headings, anchors});
    }

    static void Main(string[] args) {
        string sExe = (args.Length > 0) ? args[0] : @"D:\projekty\edsharp-pr\EdSharpNG.exe";
        Assembly asm = Assembly.LoadFrom(sExe);
        tFrame = asm.GetType("EdSharp.MdiFrame");
        A("SONDA: typ EdSharp.MdiFrame istnieje", tFrame != null);
        if (tFrame == null) {Console.WriteLine("WYNIK: 0/1 sonda glucha"); return;}

        // 2-5: metody, na ktorych okno linku stoi, MUSZA byc w binarce.  Sonda
        // patrzaca tylko na zrodlo nie odroznilaby zbudowanej binarki od starej.
        A("SONDA: InsertMarkdownLink istnieje w binarce", M("InsertMarkdownLink") != null);
        A("SONDA: BuildMarkdownAnchors istnieje w binarce", M("BuildMarkdownAnchors") != null);
        A("SONDA: GetMarkdownSectionHeadings istnieje w binarce", M("GetMarkdownSectionHeadings") != null);
        A("SONDA: GetMarkdownPlainText istnieje w binarce", M("GetMarkdownPlainText") != null);

        string doc = "# Instalacja\n\nTresc.\n\n## Pierwsze kroki\n\nTresc.\n\n## Instalacja\n\nTresc.\n\n## !!!\n\nTresc.\n";

        // 6-9: KOTWICE.  To jest sedno: nasze okno bierze je z tej samej metody,
        // co spis tresci, wraz z sufiksem powtorki.
        List<string> k = Kotwice(doc);
        A("cztery naglowki daja cztery kotwice", k.Count == 4);
        A("kotwica pierwszego naglowka to instalacja", k[0] == "instalacja");
        A("kotwica z dwoch slow ma myslnik", k[1] == "pierwsze-kroki");
        A("POWTORZONY tytul dostaje sufiks -1 (jak u pandoca)", k[2] == "instalacja-1");

        // 10-11: naglowek BEZ kotwicy.  Pandoc nie daje mu celu, wiec nasze okno
        // NIE MOZE go pokazac na liscie - link do niego bylby martwy.
        A("naglowek z samych znakow przestankowych ma PUSTA kotwice", k[3] == "");
        int ileZKotwica = 0;
        for (int i = 0; i < k.Count; i++) if (k[i].Length > 0) ileZKotwica++;
        A("KONTROLA: na liste linku wewnetrznego wchodza 3 z 4 naglowkow", ileZKotwica == 3);

        // 12-14: ZGODNOSC ZE SPISEM TRESCI - kontrola, ktora rozstrzyga o tym,
        // czy link zadziala po eksporcie.  Bez niej "kotwica jest" nie znaczy
        // "kotwica ta sama, ktorej uzywa spis".
        string spis = Spis(doc);
        A("spis tresci uzywa kotwicy instalacja", spis.Contains("(#instalacja)"));
        A("spis tresci uzywa kotwicy instalacja-1", spis.Contains("(#instalacja-1)"));
        A("spis tresci NIE wpisuje linku do naglowka bez kotwicy", !spis.Contains("(#)"));

        // 15-16: KONTROLA ROZNICUJACA sondy.  Gdyby BuildMarkdownAnchors zwracalo
        // stala albo pusta liste, wszystko wyzej tez by "przeszlo".
        List<string> kInny = Kotwice("# Zupelnie inny tytul\n\nTresc.\n");
        A("KONTROLA ROZNICUJACA: inny dokument daje INNA kotwice",
          kInny.Count == 1 && kInny[0] == "zupelnie-inny-tytul" && kInny[0] != k[0]);
        A("KONTROLA NEG.: dokument bez naglowkow daje ZERO kotwic",
          Kotwice("Zwykly tekst bez naglowkow.\nDrugi wiersz.\n").Count == 0);

        // 17-19: kotwica z polskich liter i z naglowka ze skladnia w tytule.
        List<string> kPl = Kotwice("# Zazolc gesla jazn\n\n## Kod i **pogrubienie**\n\n## 100 lat\n");
        A("polskie slowa bez ogonkow daja kotwice ze slowami", kPl[0] == "zazolc-gesla-jazn");
        A("znaczniki Markdown NIE wchodza do kotwicy", kPl[1] == "kod-i-pogrubienie");
        A("cyfry zostaja w kotwicy", kPl[2] == "100-lat");

        // 20-22: etykieta pozycji na liscie naglowkow czyta sie jak zdanie, a nie
        // jak kod - to ta sama metoda, ktorej uzywa spis tresci.
        MethodInfo plain = M("GetMarkdownPlainText");
        A("etykieta bez gwiazdek", ((string) plain.Invoke(null, new object[] {"Kod i **pogrubienie**"})) == "Kod i pogrubienie");
        A("etykieta bez grawisow", ((string) plain.Invoke(null, new object[] {"Tekst z `kodem`"})) == "Tekst z kodem");
        A("etykieta linku w tytule zostawia sam tekst", ((string) plain.Invoke(null, new object[] {"Zobacz [strone](http://x)"})) == "Zobacz strone");

        // 23-25: BRAMKA NA BLOK KODU.  Znacznik linku w bloku kodu jest MARTWY -
        // konwerter wypisze go jako widoczny tekst przykladu.
        MethodInfo fence = M("MarkdownReview_FindFenceRanges");
        MethodInfo inFence = M("IsMarkdownIndexInFence");
        A("SONDA: bramka na blok kodu jest w binarce", fence != null && inFence != null);
        string docKod = "Tekst.\n\n```\nkod w bloku\n```\n\nDalszy tekst.\n";
        object zakresy = fence.Invoke(null, new object[] {docKod});
        int iWKodzie = docKod.IndexOf("kod w bloku");
        int iPozaKodem = docKod.IndexOf("Dalszy tekst");
        A("kursor W BLOKU KODU jest rozpoznany",
          (bool) inFence.Invoke(null, new object[] {zakresy, iWKodzie}));
        A("KONTROLA: kursor POZA blokiem NIE jest rozpoznany jako blok",
          !((bool) inFence.Invoke(null, new object[] {zakresy, iPozaKodem})));

        Console.WriteLine();
        Console.WriteLine("WYNIK: " + pass + "/" + (pass + fail));
        Environment.Exit(fail == 0 ? 0 : 1);
    }
}
