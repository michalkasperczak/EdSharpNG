// JezykInterfejsu.cs -- niezmienny na czas procesu wybor języka interfejsu.
using System;
using System.Collections.Generic;
using System.Globalization;

public static class JezykInterfejsu
{
    static bool bZainicjowany;
    static bool bPolski;
    static Dictionary<string, string> dPolski = ZbudujPolski();
    static Dictionary<string, string> dAngielski = ZbudujAngielski();

    public static bool Polski { get { return bPolski; } }

    // value accepts the persisted values system|pl|en, including hand-written
    // surrounding spaces and quotes.  Unknown values deliberately use the same
    // system fallback as the UI list.
    public static void Initialize(string value, CultureInfo systemCulture)
    {
        if (bZainicjowany) return;
        string s = (value ?? "").Trim().Trim('"').Trim().ToLowerInvariant();
        if (s == "pl") bPolski = true;
        else if (s == "en") bPolski = false;
        else {
            string sSystem = systemCulture == null ? "" : systemCulture.TwoLetterISOLanguageName;
            bPolski = string.Equals(sSystem, "pl", StringComparison.OrdinalIgnoreCase);
        }
        bZainicjowany = true;
    } // Initialize method

    public static string Wybierz(string en, string pl)
    {
        return bPolski ? pl : en;
    } // Wybierz method

    public static string LiczbaZnakow(int count)
    {
        if (!bPolski) return count + (count == 1 ? " character" : " characters");
        int units = count % 10, lastTwo = count % 100;
        return count + (count == 1 ? " znak" : units >= 2 && units <= 4 && (lastTwo < 12 || lastTwo > 14) ? " znaki" : " znaków");
    }

    // Translate only exact, registered UI keys.  User data and document text
    // fall through unchanged, even when they happen to look like UI wording.
    public static string Tekst(string source)
    {
        if (source == null) return "";
        string s;
        if (bPolski && dPolski.TryGetValue(source, out s)) return s;
        if (!bPolski && dAngielski.TryGetValue(source, out s)) return s;
        return source;
    } // Tekst method

    static Dictionary<string, string> ZbudujPolski()
    {
        Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.Ordinal);
        Dodaj(d, "Settings", "Ustawienia");
        Dodaj(d, "Settings apply to the whole program.  Changes take effect when you press OK; some of them apply to windows opened afterwards.", "Ustawienia dotyczą całego programu. Zmiany zostaną zapisane po wybraniu OK; część z nich dotyczy okien otwieranych później.");
        Dodaj(d, "Interface language", "Język interfejsu");
        Dodaj(d, "Choose the language for the user interface. The choice is saved now and is used after the next start. It does not change document text, spelling or the synonym dictionary.", "Wybierz język interfejsu. Wybór zostanie zapisany teraz i zastosowany po następnym uruchomieniu. Nie zmienia treści dokumentów, pisowni ani słownika synonimów.");
        Dodaj(d, "Match system language", "Zgodny z językiem systemu");
        Dodaj(d, "&Wrap long lines in new windows", "Z&awijaj długie wiersze w nowych oknach");
        Dodaj(d, "A long line is shown broken across the screen instead of running off to the right.  Alt+W changes it for the window you are in.", "Długi wiersz jest wyświetlany w kilku wierszach ekranu zamiast wychodzić poza prawą krawędź. Alt+W zmienia to w bieżącym oknie.");
        Dodaj(d, "Start with the window ma&ximized", "Uruchamiaj z oknem zma&ksymalizowanym");
        Dodaj(d, "The EdSharp window fills the screen when the program starts.", "Okno EdSharpa wypełnia ekran podczas uruchamiania programu.");
        Dodaj(d, "Turn &indent mode on in new windows", "Włączaj tryb &wcięć w nowych oknach");
        Dodaj(d, "A new line keeps the indent of the line above it.  Alt+Shift+I changes it for the window you are in.", "Nowy wiersz zachowuje wcięcie wiersza powyżej. Alt+Shift+I zmienia to w bieżącym oknie.");
        Dodaj(d, "One step of &indent is", "Jeden krok &wcięcia to");
        Dodaj(d, "What one press of Tab adds, and what counts as one level when reading indent.", "Co dodaje jedno naciśnięcie Tab i co jest jednym poziomem podczas odczytu wcięć.");
        Dodaj(d, "Tab character", "znak tabulatora"); Dodaj(d, "Two spaces", "dwie spacje"); Dodaj(d, "Three spaces", "trzy spacje"); Dodaj(d, "Four spaces", "cztery spacje"); Dodaj(d, "Eight spaces", "osiem spacji");
        Dodaj(d, "New files are saved as", "Nowe pliki zapisuj jako");
        Dodaj(d, "The extension added when you save a new document without typing one.", "Rozszerzenie dodawane przy zapisie nowego dokumentu bez podanego rozszerzenia.");
        Dodaj(d, "Plain text (txt)", "zwykły tekst (txt)");
        Dodaj(d, "Control+S saves imported documents in their original format", "Control+S zapisuje zaimportowane dokumenty w ich oryginalnym formacie");
        Dodaj(d, "Off: keep working and saving in Markdown. On: rebuild imported DOCX, EPUB, HTML or RTF documents from Markdown when saving. Typography and unsupported document features may be lost. Previous versions are kept in .edsharp-backups next to the original. External changes block the save.", "Wyłączone: pracuj i zapisuj w Markdownie. Włączone: podczas zapisu odtwarzaj zaimportowane dokumenty DOCX, EPUB, HTML lub RTF z Markdowna. Typografia i nieobsługiwane elementy dokumentu mogą zostać utracone. Poprzednie wersje są zachowywane w .edsharp-backups obok oryginału. Zmiany zewnętrzne blokują zapis.");
        Dodaj(d, "&Snippet library folder", "Folder biblioteki &fragmentów");
        Dodaj(d, "Leave empty for the existing Snippets\\Default folder in your EdSharp data folder. Browse chooses an existing library folder. The folder is used for saving, inserting, editing, tokens and manual snippet expansion.", "Pozostaw puste, aby użyć istniejącego folderu Snippets\\Default w katalogu danych EdSharpa. Przeglądaj wybiera istniejący folder biblioteki. Folder jest używany do zapisywania, wstawiania, edycji, tokenów i ręcznego rozwijania fragmentów.");
        Dodaj(d, "Open the files from the &previous session on startup", "Przy uruchomieniu otwieraj pliki z &poprzedniej sesji");
        Dodaj(d, "Files that were open when you last left are opened again.  Work Continuity, in this same menu, also brings back the cursor positions.", "Pliki otwarte przy poprzednim zakończeniu są otwierane ponownie. Ciągłość pracy w tym samym menu przywraca także pozycje kursora.");
        Dodaj(d, "Automatically install a verified update at the next normal start", "Automatycznie instaluj sprawdzoną aktualizację przy następnym zwykłym uruchomieniu");
        Dodaj(d, "Off by default. When on, EdSharp may download and verify a newer installer in the background. It starts that verified package only at a later normal start, before a document is opened. Recovery and files opened from the command line never start it automatically.", "Domyślnie wyłączone. Po włączeniu EdSharp może pobrać i sprawdzić nowszy instalator w tle. Uruchomi sprawdzoną paczkę dopiero przy późniejszym zwykłym starcie, przed otwarciem dokumentu. Odzyskiwanie i pliki otwierane z wiersza polecenia nigdy nie uruchamiają jej automatycznie.");
        Dodaj(d, "How many files the &recent list keeps", "Ile plików zachowuje lista &ostatnich");
        Dodaj(d, "The length of the list on Alt+R.  Older entries drop off the end.", "Długość listy pod Alt+R. Starsze pozycje są usuwane z końca.");
        Dodaj(d, "Read files that have no byte order mark as", "Odczytuj pliki bez znacznika kolejności bajtów jako");
        Dodaj(d, "Only files that do not say what they are.  Detect looks at the content: it is right for Unicode files and for Polish legacy text.  Choose a code page only if your old files are read wrongly.", "Tylko pliki, które nie określają własnego kodowania. Wykrywanie analizuje treść: działa dla plików Unicode i starszego polskiego tekstu. Wybierz stronę kodową tylko wtedy, gdy stare pliki są odczytywane błędnie.");
        Dodaj(d, "Detect from content (recommended)", "wykrywaj z treści (zalecane)"); Dodaj(d, "UTF-8 with byte order mark", "UTF-8 ze znacznikiem kolejności bajtów"); Dodaj(d, "UTF-8 without byte order mark", "UTF-8 bez znacznika kolejności bajtów"); Dodaj(d, "Windows-1250 Central European", "Windows-1250 środkowoeuropejskie"); Dodaj(d, "Latin 2 DOS (852)", "Latin 2 DOS (852)"); Dodaj(d, "Mazovia (Polish DOS)", "Mazovia (polski DOS)"); Dodaj(d, "Windows system default", "domyślne systemu Windows");
        Dodaj(d, "Date is written as", "Datę zapisuj jako"); Dodaj(d, "The form used by Insert Date and Time and by the %Date% token.", "Format używany przez Wstaw datę i czas oraz token %Date%.");
        Dodaj(d, "Long, as Windows writes it", "długą, jak zapisuje Windows"); Dodaj(d, "Short, as Windows writes it", "krótką, jak zapisuje Windows"); Dodaj(d, "Year-month-day (2026-09-13)", "rok-miesiąc-dzień (2026-09-13)"); Dodaj(d, "Day.month.year (13.09.2026)", "dzień.miesiąc.rok (13.09.2026)"); Dodaj(d, "Do not insert a date", "nie wstawiaj daty");
        Dodaj(d, "Time is written as", "Czas zapisuj jako"); Dodaj(d, "The form used by Insert Date and Time and by the %Time% token.", "Format używany przez Wstaw datę i czas oraz token %Time%."); Dodaj(d, "With seconds (14:35:02)", "z sekundami (14:35:02)"); Dodaj(d, "24-hour without seconds (14:35)", "24-godzinny bez sekund (14:35)"); Dodaj(d, "12-hour with am or pm", "12-godzinny z am lub pm"); Dodaj(d, "Do not insert a time", "nie wstawiaj czasu");
        Dodaj(d, "Quoted lines start with", "Cytowane wiersze zaczynaj od"); Dodaj(d, "What Quote Text puts in front of every line of a reply.", "Co Cytuj tekst dodaje na początku każdego wiersza odpowiedzi."); Dodaj(d, "Greater-than and a space (> )", "znak większości i spacja (> )"); Dodaj(d, "Greater-than (>)", "znak większości (>)"); Dodaj(d, "Two greater-thans (>> )", "dwa znaki większości (>> )"); Dodaj(d, "Vertical bar and a space (| )", "pionowa kreska i spacja (| )");
        Dodaj(d, "When comparing or sorting, one item is", "Podczas porównywania lub sortowania jeden element to"); Dodaj(d, "What the item commands treat as a single item: a line, a paragraph, or a section.", "Co polecenia elementów traktują jako pojedynczy element: wiersz, akapit lub sekcję."); Dodaj(d, "One line", "jeden wiersz"); Dodaj(d, "One paragraph (blank line between)", "jeden akapit (pusty wiersz między nimi)");
        Dodaj(d, "For building and running code", "Do budowania i uruchamiania kodu"); Dodaj(d, "Matching brackets to jump between", "Nawiasy do przechodzenia między parą"); Dodaj(d, "Which pair the Match Brace command jumps between.", "Między którą parą przechodzi polecenie Dopasuj nawias."); Dodaj(d, "Curly braces { }", "nawiasy klamrowe { }"); Dodaj(d, "Round brackets ( )", "nawiasy okrągłe ( )"); Dodaj(d, "Square brackets [ ]", "nawiasy kwadratowe [ ]"); Dodaj(d, "Angle brackets < >", "nawiasy ostre < >");
        Dodaj(d, "Command run by Pro&mpt Command", "Polecenie uruchamiane przez &Polecenie z monitem"); Dodaj(d, "The non-interactive program started by Prompt Command. Standard input is closed; output opens in a readable, copyable result window.", "Program nieinteraktywny uruchamiany przez Polecenie z monitem. Standardowe wejście jest zamknięte; wynik otwiera się w czytelnym oknie, z którego można kopiować.");
        Dodaj(d, "Command for Run Configured &Tool", "Polecenie dla Uruchom skonfigurowane &narzędzie"); Dodaj(d, "Command line run by Run Configured Tool.  Set it to the tool you use; it can include arguments and %Source%.", "Wiersz polecenia uruchamiany przez Uruchom skonfigurowane narzędzie. Ustaw używane narzędzie; może zawierać argumenty i %Source%.");
        Dodaj(d, "Formats to open converted or raw", "Formaty otwierane po konwersji lub bezpośrednio"); Dodaj(d, "Pairs such as docx:0 rst:1, separated by spaces.  Zero opens that kind of file as it is stored, one converts it to text.", "Pary takie jak docx:0 rst:1 rozdzielone spacjami. Zero otwiera dany typ pliku w zapisanej postaci, jeden konwertuje go do tekstu.");
        Dodaj(d, "Kept from the configuration file: ", "Zachowano z pliku konfiguracji: ");
        Dodaj(d, "Choose snippet library folder", "Wybierz folder biblioteki fragmentów");
        Dodaj(d, "&Browse...", "&Przeglądaj...");
        Dodaj(d, "Browse snippet library folder", "Przeglądaj folder biblioteki fragmentów");
        Dodaj(d, "OK", "OK"); Dodaj(d, "Cancel", "Anuluj"); Dodaj(d, "&Restore defaults", "&Przywróć domyślne"); Dodaj(d, "&Edit file", "&Edytuj plik");
        Dodaj(d, "Confirm", "Potwierdź"); Dodaj(d, "Restore every setting in this window to its default?", "Przywrócić dla wszystkich ustawień tego okna wartości domyślne?");
        Dodaj(d, "Editing the configuration file; save it and restart EdSharp for changes to apply", "Edytujesz plik konfiguracji; zapisz go i uruchom ponownie EdSharpa, aby zastosować zmiany");
        Dodaj(d, "Every setting was already at its default", "Każde ustawienie już miało wartość domyślną"); Dodaj(d, " settings restored to defaults", " ustawień przywrócono do wartości domyślnych");
        Dodaj(d, "No settings changed", "Nie zmieniono ustawień"); Dodaj(d, "One setting saved", "Zapisano jedno ustawienie"); Dodaj(d, " settings saved", " ustawień zapisano");
        Dodaj(d, "Interface language saved. It will change after the next start.", "Zapisano język interfejsu. Zmieni się po następnym uruchomieniu.");
        Dodaj(d, "Settings saved, but these were not saved: ", "Zapisano ustawienia, ale nie zapisano: ");
        Dodaj(d, "Could not save: ", "Nie można zapisać: ");
        return d;
    } // ZbudujPolski method

    static Dictionary<string, string> ZbudujAngielski()
    {
        Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.Ordinal);
        Dodaj(d, "Słownik synonimów", "Synonym dictionary"); Dodaj(d, "Słownik pod Shift+F7: polski, angielski albo oba. Wybór nie zmienia języka interfejsu ani sprawdzania pisowni. Działa bez Internetu.", "Dictionary under Shift+F7: Polish, English or both. The choice does not change the interface language or spell checking. It works without the Internet.");
        Dodaj(d, "Polski", "Polish"); Dodaj(d, "Angielski", "English"); Dodaj(d, "Oba", "Both");
        Dodaj(d, "&Dziennik diagnostyczny", "&Diagnostic log"); Dodaj(d, "Zapisuje ograniczony, bezpieczny dziennik techniczny bez nazw dokumentów, ścieżek, treści i Speech.log. Wyłączenie zatrzymuje wszystkie wpisy.", "Writes a limited, safe technical log without document names, paths, content or Speech.log. Turning it off stops all entries.");
        Dodaj(d, "Dołączaj bezpieczny dziennik do zgłoszenia problemu", "Attach safe log to problem report"); Dodaj(d, "Domyślnie wyłączone. Przy zgłoszeniu można dołączyć tylko zredagowany bezpieczny dziennik diagnostyczny, nigdy Speech.log.", "Off by default. A problem report can attach only the redacted safe diagnostic log, never Speech.log.");
        return d;
    } // ZbudujAngielski method

    static void Dodaj(Dictionary<string, string> d, string source, string translated)
    {
        d[source] = translated;
    } // Dodaj method
} // JezykInterfejsu class
