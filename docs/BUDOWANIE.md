# Budowanie i wydawanie

Środowisko: WSL Ubuntu na Windows. Kompilator C# stoi po stronie Windows
(`Microsoft.NET\Framework64`), źródła leżą po stronie Linuksa — dlatego
budowanie przechodzi przez katalog wymiany `/mnt/c/EdSharpBuild`.

## Krótko

```bash
cd ~/projekty/edsharp
# 1. podnieś numer wersji w EdSharp.cs: VersionString = "5.0.95"
bash zbuduj.sh 5.0.95
bash wydaj.sh 5.0.95 "opis zmian jednym zdaniem"
```

To wszystko. Oba skrypty odmawiają pracy, gdy coś się nie zgadza — patrz niżej.

## Co robi zbuduj.sh

1. **Sprawdza, czy `VersionString` w `EdSharp.cs` zgadza się z numerem paczki.**
   Raz się rozjechało: paczka 5.0.74, a okno About mówiło 5.0.73. Dla kogoś, kto
   testuje kolejne wersje po kolei, to najgorszy możliwy błąd — nie wiadomo, co
   właściwie się sprawdza.
2. **Sprawdza znacznik BOM w plikach `.ini`.** Trzy bajty `EF BB BF` na początku
   `Hotkeys.ini` sprawiają, że `GetPrivateProfileString` nie widzi pierwszej
   sekcji: nazwa brzmi dla niej `<BOM>[Hotkeys]`. Skutek — wszystkie opisy
   i skróty poleceń czytają się jako puste, **a program nie zgłasza błędu**.
   Plik wygląda przy tym normalnie w każdym edytorze.
3. Kopiuje `*.cs`, `*.js` i `BuildEdSharp.cmd` do `/mnt/c/EdSharpBuild`.
4. Uruchamia `cmd.exe /c BuildEdSharp.cmd` — kompilacja.
5. **Sprawdza, czy binarka jest młodsza od źródła.** Kompilator potrafi paść
   cicho, zostawiając stary plik `.exe`; bez tej kontroli spakowałoby się starą
   wersję programu.
6. Pakuje instalator przez Inno Setup do `dist/EdSharpNG_Setup_<wersja>.exe`.

Kody wyjścia: `2` brak numeru wersji, `3` numer nie zgadza się ze źródłem,
`4`–`5` błąd kopiowania, `6` brak binarki, `7` binarka starsza od źródła,
`11` BOM w pliku `.ini`.

### Nowy plik źródłowy trzeba dopisać ręcznie

`zbuduj.sh` kopiuje wszystkie `*.cs`, ale kompilator dostaje **wyliczoną listę**
plików w `BuildEdSharp.cmd`. Dodanie nowego pliku bez dopisania go do tej listy
kończy się błędem `CS0246: nazwa typu nie istnieje`.

Zdarzyło się przy `Sesja.cs` (wersja 5.0.93) — pierwszy build padł na
`CS0246 SesjaOkno`.

## Co robi wydaj.sh

1. Sprawdza, czy paczka istnieje i czy numer wersji zgadza się ze źródłem.
2. Commit i push na `origin master`.
3. Tworzy albo poprawia wydanie na GitHubie, dołączając instalator.
4. **Liczy sumę SHA-256 z pliku i wstawia ją do opisu wydania**, potem
   **odczytuje opis z GitHuba i sprawdza, czy suma tam naprawdę jest.** Gdy nie —
   przerywa z kodem `9`.
5. Kopiuje instalator na komputer użytkownika (`michal-glowny`) i odczytuje
   z niego rozmiar, sumę i wersję zapisaną w pliku exe.

### Dlaczego suma jest liczona automatycznie

Program przy aktualizacji (F11) liczy sumę pobranego instalatora i porównuje ją
z sumą z opisu wydania. Bez sumy w opisie pyta użytkownika: *„Release vX does
not publish a checksum… Run it anyway?"*.

Tak stało się przy 5.0.82 i 5.0.83 — suma była wklejana ręcznie i po prostu jej
nie wkleiłem. **Zabezpieczenie, które zależy od tego, czy ktoś pamiętał wkleić
64 znaki, nie jest zabezpieczeniem.**

### Pułapka ścieżki ze spacją

`scp` na ścieżkę `D:\Projekty Codex\Hermes\` zawodzi przy każdym sposobie
cytowania. Dlatego plik ląduje najpierw w `C:\` pod nazwą bez spacji, a potem
PowerShell po stronie Windows przenosi go na miejsce.

## Uruchamianie testów

```bash
cd /mnt/c/EdSharpBuild && cmd.exe /c pomiar_<nazwa>.cmd
```

Skrypty testowe leżą w `testy/`, a ich uruchamiacze `.cmd` w katalogu wymiany.
Szczegóły w `POMIARY.md`.

## Wersjonowanie

Numer wersji musi być podniesiony w **dwóch miejscach naraz**:

- `App.VersionString` w `EdSharp.cs` — wpieczone w plik exe,
- `AppVersion` w `EdSharp_Setup.iss` — z tego powstaje znacznik wydania.

Gdy podniesiony jest tylko `.iss`, każde sprawdzenie aktualizacji na zawsze
zgłasza nowszą wersję. Gdy tylko stała w kodzie — wydanie jest niewidoczne dla
starszych instalacji. `zbuduj.sh` i `wydaj.sh` pilnują pierwszego z nich.

## Czego nie robić

- **Nie budować przez ręczne składanie polecenia kompilatora.** Cała złożoność
  siedzi w `zbuduj.sh` celowo, między innymi dlatego, że złożone polecenie
  uruchamiające inny program bywa wstrzymywane do zatwierdzenia — a właściciel
  projektu pisze z telefonu i tego okna nie widzi.
- **Nie dopisywać BOM do plików `.ini` ani do skryptów.** Dokumenty użytkownika
  w UTF-8 BOM mieć mają; pliki konfiguracyjne — nie.
- **Nie wydawać bez podniesienia numeru wersji.** Nadpisanie istniejącego
  wydania tą samą wersją sprawia, że aktualizacja u użytkownika nie widzi zmiany.
