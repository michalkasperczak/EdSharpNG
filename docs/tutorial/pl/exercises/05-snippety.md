# Ćwiczenie 05 — trwała biblioteka snippetów i ZIP

> **DRAFT docelowego menu PL/EN.** Używaj nowego pliku oraz testowego folderu i ZIP-a, nie własnej biblioteki produkcyjnej.

**Cel.** Ustawić trwały folder, zapisać i rozwinąć snippet oraz poznać jawny eksport/import ZIP.

## Kroki

1. Naciśnij `Ctrl+,`. W **Ustawienia > Opcje konfiguracji…** znajdź **Folder biblioteki fragmentów**, wybierz **Przeglądaj…** i wskaż istniejący folder testowy. Zatwierdź.
2. W nowym pliku wpisz `Pozdrawiam,`, zaznacz tekst i użyj `Alt+S` (**Zapisz Snippet…**); nazwij go `podpis`.
3. W nowym miejscu użyj `Alt+V` (**Wstaw Snippet…**). `Alt+Shift+V` (**Edytuj Snippet…**) otwiera plik do odczytu lub edycji.
4. Usuń wstawienie, wpisz `podpis` przed kursorem i naciśnij `Ctrl+Shift+Space` (**Rozwiń Snippet**).
5. W **Narzędzia > Snippety** wybierz **Eksportuj bibliotekę Snippetów…**, zapisz testowy ZIP poza folderem biblioteki. Dla testowego importu wybierz **Importuj bibliotekę Snippetów…** i świadomie wskaż politykę kolizji.

## Oczekiwany wynik

Jeden folder jest używany przez zapis, wstawianie, edycję, tokeny i ręczne rozwijanie. Pełna nazwa ma pierwszeństwo, potem prefiks, na końcu fragment; wiele trafień wymaga wyboru. Import ZIP pyta: pomiń, zachowaj oba albo zastąp z kopią zapasową. Pusty wybór folderu oznacza dotychczasowy `Snippets\Default`; jawnie niedostępny folder zgłasza błąd bez cichego fallbacku.

## Bezpieczny powrót

`Ctrl+Z` cofa wstawienie w bieżącym dokumencie. Przed importem zachowaj własną kopię folderu i anuluj zamiast zgadywać politykę kolizji. `Ctrl+Space` tylko zaznacza ciąg bez spacji — nie rozwija snippetu.

[Wróć do podręcznika](../podrecznik.md).
