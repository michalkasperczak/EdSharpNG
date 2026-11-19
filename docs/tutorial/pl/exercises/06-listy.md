# Ćwiczenie 06 — listy

**Cel.** Sprawdzić operacje na zaznaczeniu i rozróżnić porównanie dwóch list.

## Kroki

W nowym pliku wpisz:

```text
Ala
ala
Ola
Ala
```

Zaznacz tylko te cztery wiersze i użyj `Alt+Shift+K`. Cofnij `Ctrl+Z`, potem użyj `Alt+Shift+O`, cofnij i użyj `Alt+Shift+Z`.

**Sprawdź:** usuwanie duplikatów pozostawia pierwsze `Ala`, `Ola`; nie rozróżnia `Ala` i `ala`. Sortowanie też ignoruje wielkość liter, lecz zachowuje oryginalny zapis elementów. Każdy test wykonujesz na zaznaczeniu.

Zastąp tekst tymi dwoma listami i ustaw kursor na początku `Ala` w drugiej części:

```text
Ala
ala
Ola
Ala
Ala
ola
```

`Alt+Shift+G` tworzy nowe okno z `ala`, `Ola`; `Alt+Shift+Q` tworzy nowe okno z `Ala`, `Ala`. Pierwsza lista jest nad bieżącym wierszem, druga od bieżącego wiersza do końca. Duplikaty pierwszej listy zostają, a porównanie jest czułe na wielkość liter.

**Bezpieczny powrót:** wynik jest w nowym oknie; zamknij je bez zapisu, aby nie tworzyć pliku. Plik ćwiczenia pozostaje bez zmian przez samo porównanie.

[Wróć do podręcznika](../podrecznik.md).