# Pomiary i testy

Zasada obowiązująca w tym projekcie: **zdanie „to działa" bez wyniku uruchomienia
jest hipotezą, nie faktem.** Każda zmiana, która da się zmierzyć, jest mierzona
przed wydaniem.

## Dlaczego akurat tak, a nie zwykły framework testowy

Program kompiluje się jednym wywołaniem `csc`, bez menedżera pakietów (patrz
`DECYZJE.md`). Dodanie NUnit czy xUnit oznaczałoby wprowadzenie pierwszej
zależności zewnętrznej i utratę tego, że projekt buduje się na czystym Windows
bez sieci.

Zamiast tego każdy pomiar to samodzielny plik C# z własną metodą `Main`,
kompilowany **razem z całym programem** i podmieniający punkt wejścia:

```
csc /main:PomiarSesja ... EdSharp.cs Lbc.cs Say.cs ... Sesja.cs pomiar_sesja.cs
```

Zysk: pomiar widzi prawdziwy kod produkcyjny, nie kopię. Koszt: kompilacja
całości przy każdym uruchomieniu (kilkanaście sekund).

### Pułapka: odwołania UIA

`EdSharp.cs` używa `System.Windows.Automation`, więc każdy pomiar dotykający tego
pliku musi mieć w wywołaniu kompilatora odwołania
`UIAutomationProvider.dll` i `UIAutomationTypes.dll`. Bez nich leci `CS0234`.

Wzorzec jest w `testy/uruchamiacze/pomiar_sesja.cmd` — przy nowym pomiarze
skopiuj ten plik i zmień nazwy.

## Uruchamianie

```bash
cd /mnt/c/EdSharpBuild && cmd.exe /c pomiar_sesja.cmd
```

Uruchamiacze `.cmd` są wersjonowane w `testy/uruchamiacze/`; `zbuduj.sh` kopiuje
je do katalogu wymiany razem ze źródłami. Wynik na końcu wyjścia:

```
WYNIK: 74 OK, 0 BLAD
```

## Spis pomiarów

Około 60 plików `pomiar_*.cs` w `testy/`. Najistotniejsze:

| Plik | Co mierzy | Przypadki |
|---|---|---|
| `pomiar_sesja.cs` | Ciągłość pracy: zapis i odczyt sesji, kopie ratunkowe, pliki uszkodzone | 74 |
| `pomiar_slownik_menu.cs` | Sprawdzanie pisowni, pozycje menu | 33 |
| `pomiar_nazwy_skrotow.cs` | Nazwy klawiszy w opisach poleceń | 31 |
| `pomiar_csv.cs` | Czytnik CSV wg RFC 4180 | — |
| `pomiar_ini_bom.cs` | Zachowanie plików `.ini` ze znacznikiem BOM | — |
| `pomiar_kolizji_altgr.cs` | Skróty zjadające polskie znaki przez AltGr | — |
| `pomiar_kodowania_polskie.cs`, `pomiar_mazovia.cs` | Strony kodowe: Mazovia, Latin II, Windows-1250 | — |
| `pomiar_koncow_wiersza.cs` | Zamiana końców wiersza Windows / Mac / Linux | — |
| `pomiar_okienka_kolejnosc.cs`, `pomiar_tab_przyciskow.cs` | Kolejność tabulacji w oknach | — |
| `pomiar_liter_dostepu.cs` | Litery dostępu w menu i oknach | — |
| `pomiar_spisu_tresci.cs`, `pomiar_przypisow.cs` | Spis treści i przypisy Markdown | — |

Poza tym `kontrola_negatywna_*.sh` — sprawdzenia, że coś, co ma **nie** działać,
faktycznie nie działa. Ta klasa testów bywa pomijana, a wyłapuje najwięcej:
funkcja, która „działa zawsze", zwykle nie sprawdza warunku wcale.

## Co warto mierzyć

Te klasy przypadków złapały realne błędy w tym projekcie:

- **Wartość dłuższa niż bufor** — 260 znaków dla `GetPrivateProfileString`.
  Ucięcie jest ciche.
- **Plik ucięty w połowie i plik ze śmieciami** — program nie może się na tym
  wywalić ani wstać w połowie stanu.
- **Ścieżki z polskimi znakami i spacjami** — osobno, bo to dwa różne problemy.
- **Powtarzalność skrótu między uruchomieniami programu** —
  `String.GetHashCode` jej nie gwarantuje.
- **Liczba ujemna i tekst nieliczbowy** tam, gdzie kod oczekuje indeksu.
- **Brzmienie zdania, które usłyszy czytnik ekranu** — komunikat da się
  sprawdzić tekstowo i warto to robić, bo to jedyne wyjście programu, które
  użytkownik odbiera.
- **Znacznik BOM** na początku każdego pliku, który czyta Windows.

## Czego nie robić

- **Nie odpisywać padającego testu jako „losowy".** Zanim uznasz test za
  niestabilny, udowodnij, że przechodzi czysto — w tym projekcie „losowe"
  padnięcie okazało się raz prawdziwą regresją w kodzie, a raz testem
  unieważniającym zlecenie zegarkiem zamiast wprost.
- **Nie mierzyć kopii kodu.** Pomiar ma kompilować się z prawdziwym plikiem
  źródłowym, inaczej mierzy własne założenia.
- **Nie uznawać zera błędów narzędzia automatycznego za dowód dostępności.**
  Narzędzia sprawdzają to, co da się sprawdzić maszynowo; sensowność
  komunikatu i kolejność czytania — nie.
