# Ćwiczenie 07 — Transform Files

**Cel.** Uruchomić bezpieczny Test na kopiach oraz rozpoznać zapis przez Run i Verbose.

## Kroki

Utwórz folder `transform-kopie`. Zrób w nim kopie dwóch małych plików `.md`; nie wskazuj własnych dokumentów. W nowym dokumencie wpisz pełne ścieżki do obu kopii, po jednej w wierszu.

Przygotuj plik `usun-spacje.inix`:

```ini
[Trim trailing whitespace]
Options=multiline
Find=[ \t]+$
Replace=
```

Naciśnij `Alt+=`, wybierz ten plik, a następnie **Test**.

**Sprawdź:** słyszysz nazwy kopii, nazwę sekcji i liczbę dopasowań; zawartość obu kopii pozostaje identyczna jak przed testem. Wybierz **Run** dopiero po własnym porównaniu kopii. Gdy ma być więcej komunikatów, wybierz **Verbose**: ono też stosuje zmianę, nie jest testem.

Dopisz osobną sekcję z `Extract=true`, jeśli chcesz zbierać dopasowania. Taka sekcja pisze do schowka, a nie do plików.

**Bezpieczny powrót:** `Run` może zapisać każdą kopię z dopasowaniem, również gdy `Replace` jest puste. Nie ma zbiorczego Undo ani obiecanego rollbacku. Usuń folder kopii i odtwórz go z własnej kopii bezpieczeństwa, zamiast uruchamiać zadanie na pracy użytkownika.

[Wróć do podręcznika](../podrecznik.md).