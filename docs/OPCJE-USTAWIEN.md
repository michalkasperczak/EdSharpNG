# Opcje ustawień EdSharpNG — co robi każda z nich

Stan na wersję 5.0.108 (16.09.2026). Plik powstał na polecenie Michała
Kasperczaka z 15.09.2026: „Co robią poszczególne opcje w ustawieniach? Też to
daj do pliku. Te z pliku konfiguracyjnego w GUI też już są?"

Odpowiedź na drugie pytanie stoi na końcu, w rozdziale „Pokrycie pliku
konfiguracyjnego".

Gdzie to jest w programie:

- Okno ustawień: Control+przecinek (menu Options, „Configuration Options").
- Ręczna edycja pliku: Alt+Shift+M („Manual Options").
- Plik na dysku: `EdSharp.ini`, sekcja `[Options]`, w katalogu programu.
- Przywrócenie ustawień fabrycznych: Alt+Shift+F10.

Wartości w pliku zapisywane są w cudzysłowach. Włączone to „Y", wyłączone „N".
Okno ustawień czyta także „y", „yes", „Tak", „1" i „on" jako włączone, bo pliki
bywały pisane ręcznie — ale samo zapisuje zawsze „Y" albo „N".

---

## Okno i pisanie

### WordWrap — Zawijaj długie wiersze w nowych oknach

Długi wiersz jest pokazywany przełamany na kolejne linie ekranu, zamiast
uciekać w prawo. Dotyczy okien otwieranych PO zmianie; dla okna, w którym
właśnie jesteś, przełącza to Alt+W.

Fabrycznie: włączone.

### MaximizeWindow — Startuj z oknem zmaksymalizowanym

Okno EdSharpa zajmuje cały ekran zaraz po uruchomieniu programu.

Fabrycznie: wyłączone.

### UseIndentModeDefault — Włączaj tryb wcięć w nowych oknach

Nowy wiersz zachowuje wcięcie wiersza poprzedniego. Dla bieżącego okna
przełącza to Alt+Shift+I.

Fabrycznie: wyłączone.

### IndentUnit — Jeden krok wcięcia to

Co dokładnie wstawia jedno naciśnięcie Tab i co liczy się jako jeden poziom
przy odczytywaniu wcięcia (Alt+I).

Do wyboru: znak tabulacji, dwie, trzy, cztery albo osiem spacji.

Fabrycznie: znak tabulacji. W pliku zapisany jako dwa znaki: backslash i `t`.

---

## Pliki

### ExtensionDefault — Nowe pliki zapisuj jako

Rozszerzenie dodawane, gdy zapisujesz nowy dokument bez wpisania rozszerzenia.

Do wyboru: Markdown (md), zwykły tekst (txt), HTML (html).

Fabrycznie: md. RTF świadomie zdjęty z tej listy 13.09.2026 — patrz plik
`docs/CO-USUWAMY.md`.

### KeepBackup — Zachowuj kopię nadpisywanego pliku

Poprzednia treść zostaje obok twojego pliku, z `.bak` dodanym do nazwy.

Fabrycznie: wyłączone.

### OpenPrevious — Otwieraj pliki z poprzedniej sesji przy starcie

Pliki otwarte przy ostatnim wyjściu otwierają się ponownie. Uwaga: same pliki,
bez pozycji kursora. Pozycje kursora przywraca osobne okno „Work Continuity"
(w tym samym menu).

Fabrycznie: wyłączone.

### RecentFiles — Ile plików pamięta lista ostatnich

Długość listy pod Alt+R. Starsze wpisy wypadają z końca.

Zakres 5–500, fabrycznie 100.

### YieldEncoding — Pliki bez znacznika kolejności bajtów czytaj jako

Dotyczy WYŁĄCZNIE plików, które same nie mówią, w jakim są kodowaniu.
Rozpoznawanie z treści jest właściwym wyborem i dla plików Unicode, i dla
polskich plików z dawnych lat. Konkretną stronę kodową wybieraj tylko wtedy,
gdy twoje stare pliki czytają się błędnie.

Do wyboru: rozpoznanie z treści, UTF-8 ze znacznikiem, UTF-8 bez znacznika,
UTF-16, Windows-1250, Latin 2 DOS (852), Mazovia, domyślne kodowanie systemu.

Fabrycznie: rozpoznanie z treści (wartość pusta).

Doraźnie, dla jednego pliku, kodowanie zmienia Alt+Shift+Y.

---

## Czytanie i mowa

### HardPageAddress — Polecenie pozycji podaje numer strony

Alt+A mówi numer strony liczony od znaków wysuwu strony, zamiast procentu
dokumentu. Dwukrotne naciśnięcie Alt+A daje drugi rodzaj informacji tak czy
inaczej.

Fabrycznie: wyłączone.

Uwaga historyczna: do 13.09.2026 pod tą opcją wisiały omyłkowo TAKŻE komunikaty
o pustym wierszu, tabulatorze i znaku wysuwu strony — przy domyślnym „N"
program o pustym wierszu w ogóle nie mówił. Ta zależność została zdjęta;
komunikaty o pustym wierszu są zawsze.

### DateFormat — Data zapisywana jako

Postać używana przez „Insert Date and Time" i przez znacznik `%Date%`.

Do wyboru: długa jak w Windows, krótka jak w Windows, rok-miesiąc-dzień
(2026-09-13), dzień.miesiąc.rok (13.09.2026), nie wstawiaj daty.

Fabrycznie: długa jak w Windows (wartość pusta).

### TimeFormat — Godzina zapisywana jako

Postać używana przez „Insert Date and Time" i przez znacznik `%Time%`.

Do wyboru: krótka jak w Windows, z sekundami (14:35:02), 24-godzinna bez
sekund (14:35), 12-godzinna z am/pm, nie wstawiaj godziny.

Fabrycznie: krótka jak w Windows (wartość pusta).

### QuotePrefix — Cytowane wiersze zaczynają się od

Co polecenie „Quote" (Control+Q) stawia przed każdym wierszem odpowiedzi.

Do wyboru: `> `, `>`, `>> `, `| `.

Fabrycznie: `> `.

---

## Porównywanie i sortowanie list

### LimitItem — Przy porównywaniu i sortowaniu jednym elementem jest

Co polecenia listowe (Alt+Shift+K, N, O, Z, G, Q) traktują jako jeden element:
wiersz, akapit (rozdzielone pustym wierszem) albo sekcję (do znaku wysuwu
strony).

Fabrycznie: jeden wiersz.

---

## Zaawansowane — kompilator i wzorce

Te opcje stoją w oknie na końcu, pod osobnym nagłówkiem. Są pozostałością po
roli EdSharpa jako edytora kodu i większość użytkowników nigdy ich nie rusza.

### BraceMatch — Pasujące nawiasy do przeskakiwania

Która para nawiasów obsługuje polecenia skoku między nawiasami.

Do wyboru: `{}`, `()`, `[]`, `<>`. Fabrycznie: `{}`.

### CompileCommand — Polecenie budujące bieżący plik

Zostawione puste — pliki C# i Python nadal budują się kompilatorem znalezionym
na tej maszynie. `%File%` oznacza budowany plik.

Fabrycznie: puste.

### PromptCommand — Polecenie uruchamiane przez „Prompt Command"

Program startowany pozycją „Prompt Command" (Alt+F5) w menu Miscellaneous.

Fabrycznie: puste.

### GoToEnVironment — Język polecenia „Go to Environment"

Interpreter otwierany poleceniem „Go to Environment".

Fabrycznie: `python`.

Uwaga: nazwa klucza w pliku ma wielką literę „V" w środku (`GoToEnVironment`) —
tak jest w oryginalnym EdSharpie i tak musi zostać, żeby stare pliki
ustawień dalej działały.

### JumpPosition — Wzorzec znajdujący pozycję błędu w wyniku budowania

Wyrażenie regularne. Puste — używany jest wzorzec wbudowany w EdSharpa, który
czyta wynik typowych kompilatorów.

Fabrycznie: puste.

### AbbreviateOutput — Wzorzec skracający wynik budowania

Wyrażenie regularne. To, co pasuje, jest usuwane z komunikatu, żeby to co ważne
zostało powiedziane od razu.

Fabrycznie: `\r`.

### ViewLevels — Formaty otwierane przekonwertowane albo surowe

Pary w postaci `docx:0 rst:1`, rozdzielone spacjami. Zero otwiera taki plik
w postaci, w jakiej jest zapisany; jeden konwertuje go do tekstu.

Fabrycznie: puste.

### SectionBreak — Tekst wstawiany jako podział sekcji

Wstawiany przez Control+Enter. Pisany z backslash-n dla przejścia do nowego
wiersza i backslash-f dla wysuwu strony, tak jak w pliku konfiguracyjnym.

Fabrycznie: `\n----------\n\f\n`.

---

## Pokrycie pliku konfiguracyjnego

Odpowiedź na pytanie „te z pliku konfiguracyjnego w GUI też już są?" —
sprawdzone programowo na wersji 5.0.108, przez porównanie kluczy sekcji
`[Options]` w `EdSharp.ini` z listą `Ustawienia.Spis()`:

- kluczy w pliku konfiguracyjnym: 27
- w oknie Control+przecinek: 22
- świadomie pominiętych w oknie: 5
- kluczy w pliku, których okno NIE zna i nie pomija świadomie: 0
- pozycji w oknie, których nie ma w domyślnym pliku: 0

Czyli tak: wszystkie. Pięć pominiętych ma powód i każde z nich jest dostępne
inaczej:

1. **FontDefault** — ma własne okno pod Alt+Shift+Równa się, z podglądem
   czcionki i koloru. Lista nazw czcionek w polu kombi byłaby jego gorszą
   kopią.
2. **RestoreSession** i **AutoSaveSeconds** — mają wspólne okno „Work
   Continuity" (menu Options), gdzie stoją razem i gdzie liczba sekund ma
   sensowne granice.
3. **NavigatePart** — podręcznik oryginalnego EdSharpa mówi wprost, że żadne
   polecenie tego klucza nie czyta. Zostaje w pliku tylko dla zgodności.
4. **E&xtraSpeech** — przełącznik wycofany 03.09.2026, klucz schodzi z pliku.
   Ma w nazwie ampersand (pozostałość po znaczniku klawisza dostępu w starym
   oknie), więc porównania nazw kluczy robione są po zdjęciu ampersandu.

Do rzeczy, których w oknie nie ma, prowadzi przycisk „Edit file" w tym samym
oknie — otwiera plik ustawień w edytorze, bez szukania osobnego polecenia
w menu.

## Czego w tym oknie nie ma i mieć nie będzie

Ustawienia PER KOMPILATOR (sekcja `[Options]` w pliku `<Kompilator>.ini`).
To okno jest o ustawieniach programu; tamte edytuje się ręcznie przez
Alt+Shift+M.
