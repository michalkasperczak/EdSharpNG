// Ustawienia.cs -- OKNO USTAWIEN Z POLAMI WYBORU ZAMIAST POL TEKSTOWYCH.
//
// POLECENIE MICHALA KASPERCZAKA (13.09.2026, zadanie 7 z jego listy):
// "Ctrl, przecinek, ustawienia.  Tam wszystkie ustawienia maja wartosci do
// wpisywania w polach tekstowych.  Chodzi mi o to, zeby to byly normalne opcje
// w formie checkbox, pola kombi, takie jak we wszystkich aplikacjach.  Czyli
// trzeba przelowic te wartosci numeryczne na normalne pola edycyjne z wyborem,
// nie edycyjne tylko pola z wyborem okreslonych wartosci.  Jezeli masz
// watpliwosci co i jak robic, to mozesz wrocic do programu AMC."
//
// Wzor z AMC (SettingsWindow.xaml) zmierzony przed napisaniem tego pliku:
// CheckBox z Content i podkreslnikiem klawisza dostepu, listy wyboru z
// gotowymi pozycjami, opcje podrzedne wciete pod swoim przelacznikiem.
// Tutaj to samo, tylko w WinForms przez LbcDialog, bo EdSharp nie ma XAML.
//
// CO BYLO PRZED TA ZMIANA: Dialog.MultiInput dawal 27 POL TEKSTOWYCH, po
// jednym na kazdy klucz sekcji [Options], z nazwa klucza jako etykieta.
// Czlowiek mial wpisac recznie "Y", "N", "\t", "\n----------\n\f\n" albo numer
// strony kodowej -- czyli musial znac zapis z pliku ini na pamiec.  Zadnej
// podpowiedzi, zadnej kontroli, literowka w "Y" cicho wylaczala funkcje.
//
// ZASADA TEGO PLIKU: KAZDE ustawienie, ktore ma skonczony zbior sensownych
// wartosci, dostaje przelacznik albo liste wyboru.  Pole tekstowe zostaje
// TYLKO tam, gdzie wartosc jest naprawde dowolnym tekstem (polecenie
// kompilatora, wzorzec wyrazenia regularnego, format daty) -- i nawet tam
// obok stoi lista gotowych wzorcow, gdy takie istnieja.
//
// PULAPKA, KTORA ROZSTRZYGA KSZTALT KODU: wartosci w pliku ini sa
// NIEJEDNORODNE.  Wlaczone to raz "Y", raz "y", raz "Yes"; wciecie to "\t"
// zapisane DWOMA znakami (backslash i t), a nie tabulator; kodowanie to raz
// nazwa ("cp1250"), raz numer ("1250").  Dlatego czytanie idzie przez
// funkcje tolerancyjne (CzyWlaczone, PozycjaDlaWartosci), a zapis ZAWSZE
// pisze postac kanoniczna.  Nie wolno tu "poprawiac" pliku na wejsciu: kto ma
// na dysku "yes", ten ma dzialajace ustawienie i nie moze go stracic przez to,
// ze otworzyl okno i nacisnal Anuluj.
//
// DRUGA PULAPKA: klucz E&xtraSpeech w [Options] ma ampersand w NAZWIE (to
// pozostalosc po znaczniku klawisza dostepu w starym oknie).  Lista kluczy
// czytana z pliku poda go wlasnie tak.  Ten klucz zostal wycofany, wiec go
// pomijamy -- ale nazwy kluczy porownujemy po zdjeciu ampersandu, zeby zaden
// inny klucz nie wpadl w te sama pulapke w przyszlosci.
//
// TRZECIA PULAPKA: opcje sa TAKZE per-kompilator (sekcja [Options] w
// <Kompilator>.ini).  Stare okno pisalo zawsze do glownego pliku i tak
// zostaje: to okno jest o ustawieniach programu.  Manual Options (Alt+Shift+M)
// dalej otwiera plik do recznej edycji i jest jedyna droga do rzeczy, ktorych
// tu nie ma -- dlatego przycisk "Edit file" w tym oknie prowadzi wprost tam,
// zamiast kazac szukac osobnej komendy w menu.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

// Jedno ustawienie w oknie: jak sie nazywa w pliku, jak brzmi po ludzku,
// jakiego jest rodzaju i jakie ma dopuszczalne wartosci.
public class Opcja
{
    public string Klucz;          // nazwa w pliku ini
    public string Etykieta;       // z & przed litera klawisza dostepu
    public string Podpowiedz;     // czytana przy wejsciu w pole
    public string Rodzaj;         // "przelacznik", "lista", "liczba", "tekst"
    public string[] Nazwy;        // pozycje listy widziane przez czlowieka
    public string[] Wartosci;     // co idzie do pliku dla kazdej pozycji
    public int Min, Max;          // granice dla rodzaju "liczba"
    public string Domyslna;       // wartosc fabryczna
    public bool Zaawansowana;     // idzie na koniec, pod nagłowek

    public Opcja(string sKlucz, string sEtykieta, string sRodzaj, string sPodpowiedz)
    {
        Klucz = sKlucz; Etykieta = sEtykieta; Rodzaj = sRodzaj; Podpowiedz = sPodpowiedz;
        Min = 0; Max = 1000; Domyslna = ""; Zaawansowana = false;
    }
}

public class Ustawienia
{
    // ---------- czytanie wartosci z pliku, tolerancyjnie ----------

    // Wlaczone: "Y", "y", "yes", "Tak", "1", "on".  Cokolwiek innego (w tym
    // pusta wartosc i literowka) znaczy wylaczone -- tak samo jak czyta to
    // reszta programu, np. ReadOption("WordWrap", "Y") ... == "Y".
    public static bool CzyWlaczone(string sWartosc)
    {
        if (sWartosc == null) return false;
        string s = sWartosc.Trim().Trim('"').ToLower();
        if (s.Length == 0) return false;
        return s.StartsWith("y") || s.StartsWith("t") || s == "1" || s == "on";
    }

    public static string Kanoniczne(bool bWlaczone) { return bWlaczone ? "Y" : "N"; }

    // Ktora pozycja listy odpowiada wartosci z pliku.  Porownanie bez
    // wielkosci liter i bez cudzyslowow, bo plik bywa pisany recznie.
    // -1 = wartosc nieznana temu oknu (czlowiek wpisal cos wlasnego).
    public static int PozycjaDlaWartosci(Opcja o, string sWartosc)
    {
        if (o.Wartosci == null) return -1;
        string s = (sWartosc ?? "").Trim().Trim('"');
        for (int i = 0; i < o.Wartosci.Length; i++)
            if (string.Equals(s, o.Wartosci[i], StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    // Nazwa klucza bez ampersandu klawisza dostepu (patrz DRUGA PULAPKA).
    public static string CzystyKlucz(string sKlucz)
    {
        return (sKlucz ?? "").Replace("&", "").Trim();
    }

    // ---------- spis ustawien ----------
    //
    // Kolejnosc w tej liscie to kolejnosc w oknie, a wiec i kolejnosc
    // tabulatora.  Rzeczy uzywane najczesciej ida na gore; ustawienia dla
    // programistow (kompilator, wyrazenia regularne) na dol, pod nagłowek.
    public static List<Opcja> Spis()
    {
        List<Opcja> l = new List<Opcja>();
        Opcja o;

        // --- okno i pisanie ---
        o = new Opcja("WordWrap", "&Wrap long lines in new windows", "przelacznik",
            "A long line is shown broken across the screen instead of running off to the right.  Alt+W changes it for the window you are in.");
        o.Domyslna = "Y"; l.Add(o);

        o = new Opcja("MaximizeWindow", "Start with the window ma&ximized", "przelacznik",
            "The EdSharp window fills the screen when the program starts.");
        o.Domyslna = "N"; l.Add(o);

        o = new Opcja("UseIndentModeDefault", "Turn &indent mode on in new windows", "przelacznik",
            "A new line keeps the indent of the line above it.  Alt+Shift+I changes it for the window you are in.");
        o.Domyslna = "N"; l.Add(o);

        o = new Opcja("IndentUnit", "One step of &indent is", "lista",
            "What one press of Tab adds, and what counts as one level when reading indent.");
        o.Nazwy = new string[] { "Tab character", "Two spaces", "Three spaces", "Four spaces", "Eight spaces" };
        o.Wartosci = new string[] { "\\t", "  ", "   ", "    ", "        " };
        o.Domyslna = "\\t"; l.Add(o);

        // --- pliki ---
        o = new Opcja("ExtensionDefault", "New files are saved as", "lista",
            "The extension added when you save a new document without typing one.");
        o.Nazwy = new string[] { "Markdown (md)", "Plain text (txt)", "Rich text (rtf)", "HTML (html)" };
        o.Wartosci = new string[] { "md", "txt", "rtf", "html" };
        o.Domyslna = "md"; l.Add(o);

        o = new Opcja("KeepBackup", "&Keep a backup copy of the file being overwritten", "przelacznik",
            "The previous content is kept next to your file with .bak added to the name.");
        o.Domyslna = "N"; l.Add(o);

        o = new Opcja("OpenPrevious", "Open the files from the &previous session on startup", "przelacznik",
            "Files that were open when you last left are opened again.  Work Continuity, in this same menu, also brings back the cursor positions.");
        o.Domyslna = "N"; l.Add(o);

        o = new Opcja("RecentFiles", "How many files the &recent list keeps", "liczba",
            "The length of the list on Alt+R.  Older entries drop off the end.");
        o.Min = 5; o.Max = 500; o.Domyslna = "100"; l.Add(o);

        o = new Opcja("YieldEncoding", "Read files that have no byte order mark as", "lista",
            "Only files that do not say what they are.  Detect looks at the content: it is right for Unicode files and for Polish legacy text.  Choose a code page only if your old files are read wrongly.");
        o.Nazwy = new string[] { "Detect from content (recommended)", "UTF-8 with byte order mark", "UTF-8 without byte order mark",
            "UTF-16", "Windows-1250 Central European", "Latin 2 DOS (852)", "Mazovia (Polish DOS)", "Windows system default" };
        o.Wartosci = new string[] { "", "utf8b", "utf8n", "utf16", "cp1250", "latin2", "mazovia", "ansi" };
        o.Domyslna = ""; l.Add(o);

        // --- czytanie i mowa ---
        o = new Opcja("HardPageAddress", "Position command says the page num&ber", "przelacznik",
            "Alt+A says a page number counted from page break characters instead of a percentage of the document.  Pressing Alt+A twice gives the other kind either way.");
        o.Domyslna = "N"; l.Add(o);

        o = new Opcja("DateFormat", "Date is written as", "lista",
            "The form used by Insert Date and Time and by the %Date% token.");
        o.Nazwy = new string[] { "Long, as Windows writes it", "Short, as Windows writes it", "Year-month-day (2026-09-13)",
            "Day.month.year (13.09.2026)", "Do not insert a date" };
        o.Wartosci = new string[] { "", "d", "yyyy-MM-dd", "dd.MM.yyyy", "0" };
        o.Domyslna = ""; l.Add(o);

        o = new Opcja("TimeFormat", "Time is written as", "lista",
            "The form used by Insert Date and Time and by the %Time% token.");
        o.Nazwy = new string[] { "Short, as Windows writes it", "With seconds (14:35:02)", "24-hour without seconds (14:35)",
            "12-hour with am or pm", "Do not insert a time" };
        o.Wartosci = new string[] { "", "HH:mm:ss", "HH:mm", "h:mm tt", "0" };
        o.Domyslna = ""; l.Add(o);

        o = new Opcja("QuotePrefix", "Quoted lines start with", "lista",
            "What Quote Text puts in front of every line of a reply.");
        o.Nazwy = new string[] { "Greater-than and a space (> )", "Greater-than (>)", "Two greater-thans (>> )", "Vertical bar and a space (| )" };
        o.Wartosci = new string[] { "> ", ">", ">> ", "| " };
        o.Domyslna = "> "; l.Add(o);

        // --- porownywanie i sortowanie list ---
        o = new Opcja("LimitItem", "When comparing or sorting, one item is", "lista",
            "What the item commands treat as a single item: a line, a paragraph, or a section.");
        o.Nazwy = new string[] { "One line", "One paragraph (blank line between)", "One section (up to a page break)" };
        o.Wartosci = new string[] { "\\n", "\\n\\n", "\\f" };
        o.Domyslna = "\\n"; l.Add(o);

        // --- zaawansowane: kompilator i wzorce ---
        o = new Opcja("BraceMatch", "Matching brackets to jump between", "lista",
            "Which pair the Match Brace command jumps between.");
        o.Nazwy = new string[] { "Curly braces { }", "Round brackets ( )", "Square brackets [ ]", "Angle brackets < >" };
        o.Wartosci = new string[] { "{}", "()", "[]", "<>" };
        o.Domyslna = "{}"; o.Zaawansowana = true; l.Add(o);

        o = new Opcja("CompileCommand", "&Command that builds the current file", "tekst",
            "Left empty, C# and Python files still build with the compiler found on this machine.  %File% stands for the file being built.");
        o.Domyslna = ""; o.Zaawansowana = true; l.Add(o);

        o = new Opcja("PromptCommand", "Command run by Pro&mpt Command", "tekst",
            "The program started by the Prompt Command item in the Miscellaneous menu.");
        o.Domyslna = ""; o.Zaawansowana = true; l.Add(o);

        o = new Opcja("GoToEnVironment", "Language of the Go To En&vironment command", "tekst",
            "The interpreter opened by Go To Environment, for example python.");
        o.Domyslna = "python"; o.Zaawansowana = true; l.Add(o);

        o = new Opcja("JumpPosition", "Pattern that finds an error position in build output", "tekst",
            "A regular expression.  Left empty, the pattern built into EdSharp is used, which reads the output of the usual compilers.");
        o.Domyslna = ""; o.Zaawansowana = true; l.Add(o);

        o = new Opcja("AbbreviateOutput", "Pattern that shortens build output", "tekst",
            "A regular expression.  What it matches is dropped from the message, so the part that matters is said first.");
        o.Domyslna = "\\r"; o.Zaawansowana = true; l.Add(o);

        o = new Opcja("ViewLevels", "Formats to open converted or raw", "tekst",
            "Pairs such as docx:0 rst:1, separated by spaces.  Zero opens that kind of file as it is stored, one converts it to text.");
        o.Domyslna = ""; o.Zaawansowana = true; l.Add(o);

        o = new Opcja("SectionBreak", "Text inserted as a section break", "tekst",
            "Written with backslash n for a line break and backslash f for a page break, as in the configuration file.");
        o.Domyslna = "\\n----------\\n\\f\\n"; o.Zaawansowana = true; l.Add(o);

        return l;
    }

    // Klucze, ktorych to okno swiadomie NIE pokazuje, z powodem.
    // Porownanie po CzystyKlucz, wiec E&xtraSpeech tez tu wpada.
    public static bool Pomijany(string sKlucz)
    {
        string s = CzystyKlucz(sKlucz);
        // ExtraSpeech: przelacznik wycofany 03.09.2026, klucz schodzi z pliku.
        if (string.Equals(s, "ExtraSpeech", StringComparison.OrdinalIgnoreCase)) return true;
        // FontDefault: ma wlasne okno na Alt+Shift+Equals, z podgladem czcionki
        // i koloru; lista nazw czcionek w polu kombi byla by jego gorsza kopia.
        if (string.Equals(s, "FontDefault", StringComparison.OrdinalIgnoreCase)) return true;
        // NavigatePart: podrecznik mowi wprost, ze zadna komenda EdSharpNG go
        // nie czyta -- zostaje w pliku dla zgodnosci z oryginalem.
        if (string.Equals(s, "NavigatePart", StringComparison.OrdinalIgnoreCase)) return true;
        // RestoreSession i AutoSaveSeconds maja wlasne okno Work Continuity,
        // gdzie stoja razem i gdzie liczba sekund ma granice.
        if (string.Equals(s, "RestoreSession", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(s, "AutoSaveSeconds", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
} // Ustawienia class
