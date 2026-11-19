# EdSharp: podręcznik offline — DRAFT nowego menu PL/EN

> **DRAFT nieopublikowanej wersji.** Ten podręcznik opisuje docelowy, zamrożony model menu PL/EN, a nie bieżące wydanie zainstalowane u Michała. Nie oznacza wykonania ćwiczeń w GUI ani pomiaru żywego NVDA. Pracuj na nowym pliku lub kopii ćwiczenia, nigdy na własnym dokumencie.

<!-- lesson: document-save -->
## 1. Dokument i zapis

**Cel.** Utworzyć mały dokument Markdown i zachować go pod własną nazwą.

1. Naciśnij `Ctrl+N`, wpisz tytuł i dwa zdania.
2. Naciśnij `Ctrl+Shift+S`, wybierz folder ćwiczeń i nazwę `notatka.md`.
3. Dopisz trzecie zdanie i naciśnij `Ctrl+S`.

**Oczekiwany wynik.** Jest osobny plik `notatka.md` z trzema zdaniami. `Ctrl+S` zapisuje bieżący dokument, a `Ctrl+Shift+S` pyta o nową nazwę.

**Bezpieczny powrót.** `Ctrl+Z` cofa ostatnią zmianę; zamknięcie bez zapisu pozwala odrzucić niezapisane zmiany. To nie zastępuje własnej kopii pliku.

Ćwiczenie: [01 — dokument i zapis](exercises/01-dokument-i-zapis.md).

<!-- lesson: selection-search -->
## 2. Zaznaczanie i wyszukiwanie

**Cel.** Zaznaczyć dwa wiersze bez myszy i znaleźć powtarzające się słowo.

1. Ustaw kursor przed pierwszym wierszem i naciśnij `F8`.
2. Przejdź na koniec zakresu i naciśnij `Shift+F8`.
3. Użyj `Ctrl+F`, a potem `F3` lub `Shift+F3`.

**Oczekiwany wynik.** Zaznaczenie biegnie od punktu `F8` do kursora; wyszukiwanie nie zmienia tekstu. `Alt+F3` szuka tekstu pod kursorem albo zaznaczenia.

**Bezpieczny powrót.** `Ctrl+Shift+F8` odtwarza poprzedni zakres, a `Alt+Shift+F8` wraca do pierwotnego punktu `F8`; po zaznaczaniu w górę może on być dolnym końcem zakresu.

Ćwiczenie: [02 — zaznaczanie i wyszukiwanie](exercises/02-zaznaczanie-i-wyszukiwanie.md).

<!-- lesson: markdown-preview -->
## 3. Markdown i podgląd

**Cel.** Zapisać nagłówek, listę i łącze, a potem sprawdzić podgląd.

1. W pliku `.md` wpisz `# Spotkanie`, pusty wiersz i `- kawa`.
2. Użyj `Escape`, aby przełączyć tekstowy podgląd Markdown; drugi `Escape` wraca do edycji.
3. Po zapisie użyj `F5` albo w docelowym menu **Widok** wybierz **Podgląd Markdown w przeglądarce**.

**Oczekiwany wynik.** `Escape` pokazuje tekstowy `MarkdownReviewTextBox`, nie WebView. `F5` otwiera zapisany dokument w domyślnej przeglądarce. W **Widok** są też **Zawijanie wierszy** i **Wyłącz zawijanie wierszy**.

**Bezpieczny powrót.** `Escape` opuszcza podgląd; `Ctrl+Z` po powrocie cofa tylko zmianę tekstu.

Ćwiczenie: [03 — Markdown i podgląd](exercises/03-markdown-i-podglad.md).

<!-- lesson: tasks-tables -->
## 4. Zadania i tabele

**Cel.** Utworzyć checklistę i małą tabelę w nowym pliku.

1. Użyj `Ctrl+Shift+F2`, aby utworzyć listę zadań.
2. Naciśnij `Ctrl+Shift+X`, aby zmienić stan zadania; `Alt+Shift+F2` podaje postęp.
3. Użyj `Ctrl+Shift+T`, podaj dane kreatora i zatwierdź `Ctrl+Enter`.

**Oczekiwany wynik.** W docelowym menu **Przegląd > Zadania** dynamiczna pozycja mówi **Oznacz jako wykonane** albo **Oznacz jako niewykonane**. Pomoc i paleta nazywają tę samą komendę neutralnie: **Przełącz wykonanie zadania**. Po przełączeniu aplikacja komunikuje skutek: **Wykonane** albo **Niewykonane**. Tabela jest w bieżącym dokumencie; w nowym menu polecenie to **Wstawianie > Wstaw tabelę…**.

**Bezpieczny powrót.** `Ctrl+Z` cofa ostatnią operację tylko w pliku ćwiczenia.

Ćwiczenie: [04 — zadania i tabela](exercises/04-zadania-i-tabela.md).

<!-- lesson: snippets -->
## 5. Snippety: trwały folder, ZIP i rozwijanie

**Cel.** Wybrać jeden trwały folder biblioteki, zapisać snippet, wstawić go, a potem bezpiecznie wyeksportować lub zaimportować ZIP.

1. Naciśnij `Ctrl+,`. W docelowym **Ustawienia > Opcje konfiguracji…** wybierz **Folder biblioteki fragmentów** i przyciskiem **Przeglądaj…** wskaż istniejący trwały folder, np. własny folder dokumentów. Zatwierdź zmianę.
2. Zaznacz tekst testowy i użyj `Alt+S` (**Zapisz Snippet…**). Wstaw go przez `Alt+V` (**Wstaw Snippet…**), a otwórz do edycji przez `Alt+Shift+V` (**Edytuj Snippet…**).
3. Wpisz fragment nazwy bezpośrednio przed kursorem i użyj `Ctrl+Shift+Space` (**Rozwiń Snippet**).
4. W **Narzędzia > Snippety** wybierz **Eksportuj bibliotekę Snippetów…**, zapisz ZIP poza biblioteką, a następnie wybierz **Importuj bibliotekę Snippetów…** dla kontrolnego ZIP-a.

**Oczekiwany wynik.** Ten sam folder służy do zapisu, wstawiania, edycji, tokenów i ręcznego rozwijania. Pusty wybór używa dotychczasowego `Snippets\Default`; wskazany, niedostępny folder zgłasza błąd, nie przełącza się po cichu. Rozwijanie preferuje pełną nazwę, potem prefiks, a na końcu fragment; kilka trafień wymaga wyboru. Import ZIP pyta o kolizje: pomiń, zachowaj oba albo zastąp z kopią zapasową. Eksport tworzy ZIP, nie nowe drzewo bibliotek.

**Bezpieczny powrót.** `Ctrl+Z` cofa wstawienie w bieżącym dokumencie. Przed importem zachowaj własną kopię folderu; po anulowaniu dialogu pliki nie powinny być zmieniane. `Ctrl+Space` nadal zaznacza ciąg bez spacji — nie rozwija snippetu.

Ćwiczenie: [05 — snippety](exercises/05-snippety.md).

<!-- lesson: lists -->
## 6. Operacje listowe

**Cel.** Pracować na zaznaczonych wierszach i porównać dwie listy.

1. Zaznacz zakres; użyj `Alt+Shift+O`, `Alt+Shift+Z` albo `Alt+Shift+K`.
2. Dla porównania ustaw kursor: pierwsza lista jest nad bieżącym wierszem, druga od niego do końca.
3. Użyj `Alt+Shift+G` albo `Alt+Shift+Q`.

**Oczekiwany wynik.** Sortowanie i usuwanie duplikatów ignorują wielkość liter i zostawiają pierwszą pisownię. Porównania dwóch list zachowują kolejność i duplikaty pierwszej listy, lecz są czułe na wielkość liter. Nowe menu umieszcza je w **Narzędzia > Przekształcenia tekstu**.

**Bezpieczny powrót.** Zrób ćwiczenie w kopii; `Ctrl+Z` cofa ostatnią zmianę tekstu.

Ćwiczenie: [06 — listy](exercises/06-listy.md).

<!-- lesson: transform-files -->
## 7. Przekształć pliki: najpierw kopie i Test

**Cel.** Uruchomić zadanie znajdowania i zastępowania wyłącznie na kopiach plików.

1. Przygotuj kopie w osobnym folderze oraz dokument z ich pełnymi ścieżkami, po jednej w wierszu.
2. Użyj `Alt+=` albo **Narzędzia > Przekształcenia tekstu > Przekształć pliki** i wybierz plik `.inix`.
3. Najpierw wybierz **Test**, potem dopiero **Run** lub **Verbose**.

**Oczekiwany wynik.** **Test** liczy dopasowania bez zapisu. **Run** zapisuje pliki z dopasowaniem. **Verbose** także zapisuje pliki i dodatkowo wypisuje sekcje oraz liczbę dopasowań. `Extract=true` zbiera dopasowania do schowka bez modyfikacji.

**Bezpieczny powrót.** Nie ma obiecanego pełnego Undo ani transakcji rollbacku wielu plików. Zachowaj własną kopię przed `Run` lub `Verbose`; w razie potrzeby usuń tylko folder kopii i odtwórz go z backupu.

Ćwiczenie: [07 — Transform Files](exercises/07-transform-files.md).

## Granice tego DRAFT-u

Opis opiera się na zamrożonym modelu menu 210 poleceń. Nie dowodzi finalnej integracji, instalatora, routingu skrótów w żywym GUI ani odczytu NVDA. Usunięte polecenia — Styles, Copy All, Alternate Menu, Clear Favorite, Topic/Alt+T, Read All/Alt+F8, Read Clipboard/Alt+apostrophe, Env Vars editor i Speech Log — nie są tu przywracane ani dokumentowane.
