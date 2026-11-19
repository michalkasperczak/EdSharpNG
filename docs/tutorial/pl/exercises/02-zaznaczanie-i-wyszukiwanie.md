# Ćwiczenie 02 — zaznaczanie i wyszukiwanie

**Cel.** Zaznaczyć zakres bez myszy i znaleźć tekst bez zmiany dokumentu.

## Kroki

W nowym pliku wpisz:

```text
alfa
beta
alfa
gamma
```

Stań przed `beta`, naciśnij `F8`, przejdź strzałką w dół na koniec drugiego `alfa` i naciśnij `Shift+F8`. Wpisz `alfa` po `Ctrl+F`, potem użyj `F3` i `Shift+F3`.

**Sprawdź:** zaznaczenie obejmuje `beta` i drugi wiersz `alfa`; wyszukiwanie przechodzi między dwoma wystąpieniami. Po ostatnim trafieniu następne `F3` komunikuje `Not found!` i nie zmienia tekstu.

**Bezpieczny powrót:** `Ctrl+Shift+F8` odtwarza ostatni zakres, a `Alt+Shift+F8` wraca do pierwotnego punktu `F8`; po zaznaczaniu w górę może on być dolnym końcem zakresu. Zmian tekstu nie było.

[Wróć do podręcznika](../podrecznik.md).