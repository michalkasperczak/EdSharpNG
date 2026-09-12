# Architektura EdSharpNG

Stan na wersję 5.0.94 (12.09.2026). Liczby wierszy zmierzone `wc -l`, nie
przepisane z pamięci.

## Skąd to pochodzi

EdSharpNG jest kontynuacją edytora **EdSharp autorstwa Jamala Mazrui**
(<jamal@EmpowermentZone.com>, <https://github.com/JamalMazrui/EdSharp>), na
licencji LGPL v3. Większość kodu i cały projekt interfejsu pochodzą od niego.
Historia gita zaczyna się od wersji 5.0.73 — wcześniejsza historia nie została
przeniesiona, bo punktem startowym był zrzut kodu, nie klon repozytorium.

Nasza gałąź nie jest wydawana ani wspierana przez autora oryginału. Zgłoszenia
błędów tej wersji idą do <https://github.com/michalkasperczak/EdSharpNG/issues>,
nie do niego.

## Pliki źródłowe

Cały program to 11 plików C#, razem około 30 100 wierszy. Kompiluje się jednym
wywołaniem `csc`, bez menedżera pakietów i bez żadnej zewnętrznej biblioteki
poza .NET Framework. To ograniczenie jest świadome — patrz `DECYZJE.md`.

### Rdzeń odziedziczony

| Plik | Wiersze | Za co odpowiada |
|---|---|---|
| `EdSharp.cs` | 24 207 | Prawie cały program: klasy `MdiFrame` (okno główne), `MdiChild` (okno dokumentu), `App` (stan globalny), menu, obsługa plików, konwersje, skróty klawiszowe |
| `Lbc.cs` | 2 876 | Okna dialogowe budowane kodem: `LbcDialog`, `LbcForm`, `LbcTextBox`, `HelpDialog`. Tu powstają wszystkie okna z polami |
| `Say.cs` | 764 | Mowa: JAWS przez COM, NVDA przez bibliotekę sterującą, w ostateczności powiadomienie UIA |
| `Inix.cs` | 505 | Czytanie i zapisywanie plików `.ini` z zachowaniem kolejności wpisów |
| `KeyMap.cs` | 147 | Jedna tablica: kontekst, nazwa polecenia, opis, skrót. Czytają z niej menu, menu alternatywne (Alt+F10), opisywacz klawiszy (Ctrl+F1) i paleta poleceń |
| `Unicode.cs` | 80 | Narzędzie pomocnicze do rozpoznawania kodowania (nie część programu) |

Pliki `Lbc.cs`, `Say.cs`, `Inix.cs`, `KeyMap.cs` i `Web.cs` należą do wspólnego
zestawu narzędzi w przestrzeni nazw `Homer` — mają być przenośne między
projektami przez skopiowanie pliku.

### Dodane w tej gałęzi

| Plik | Wiersze | Za co odpowiada | Wersja |
|---|---|---|---|
| `Web.cs` | 360 | Klient HTTP z nowoczesnym uzgadnianiem TLS i sensownym nagłówkiem User-Agent | 5.0.7x |
| `Skladniki.cs` | 356 | Dociąganie narzędzi zewnętrznych (pandoc, tidy, xpdf, liblouis, astyle) w tle, bez pytań | 5.0.85 |
| `Sesja.cs` | 349 | Ciągłość pracy: sesja robocza i autozapis kopii ratunkowych | 5.0.93 |
| `Csv.cs` | 257 | Czytnik i zapisywacz CSV wg RFC 4180, bez zewnętrznych bibliotek | 5.0.87 |
| `Pisownia.cs` | 229 | Sprawdzanie pisowni przez Windows Spell Checking API, bez Worda | 5.0.84 |

Każdy z tych plików zaczyna się komentarzem, który podaje: numer zadania,
cytat zgłoszenia, co było wcześniej i dlaczego nie wystarczało. To celowo — po
kilku miesiącach powód zmiany jest trudniejszy do odzyskania niż sam kod.

## Kluczowe klasy i ich zależności

```
App                       stan globalny, ścieżki, wersja, ustawienia
 └─ MdiFrame              okno główne (jedna instancja)
     ├─ MdiChild          okno dokumentu (wiele), każde z kontrolką RTB
     ├─ timerCiaglosciPracy   jeden zegar na cały program
     └─ menu*             pozycje menu jako pola klasy

Homer.Say                 mowa (statyczna)
Homer.LbcDialog           okna z polami; wymaga tylko Say
Homer.KeyMap              tablica poleceń; czyta ją menu i paleta
Homer.InixCodec           pliki .ini
Sesja                     statyczna; czyta i zapisuje Sesja.ini
```

### Pułapka: `new MdiChild(frame)` nie buduje okna

Konstruktor jednoargumentowy wywołuje wewnątrz siebie `MdiChild(frame, tytuł)`
i **ta druga instancja** dostaje kontrolkę edycyjną oraz `Show()`. Obiekt
zwrócony przez `new MdiChild(this)` jest pustą skorupą z `RTB == null`.

Poprawnie:

```csharp
new MdiChild(this, GetNoNameTitle());
MdiChild nowe = this.Child;      // TO jest okno, które powstało
if (nowe == null || nowe.RTB == null) return;
```

## Pliki danych

Katalog danych programu (`App.DataDir`):

| Plik | Zawartość | Kto pisze |
|---|---|---|
| `EdSharp.ini` | Ustawienia użytkownika, sekcja `[Options]` | program i użytkownik (Manual Options) |
| `Hotkeys.ini` | Przypisania skrótów klawiszowych | użytkownik |
| `Sesja.ini` | Stan sesji roboczej: otwarte pliki, pozycje kursora, zakładki | tylko program, co N sekund |
| `Odzysk\*.odzysk` | Kopie ratunkowe niezapisanych zmian | tylko program |

**Sesja i kopie ratunkowe idą do osobnych plików, nie do `EdSharp.ini`.** Ten
ostatni użytkownik otwiera i edytuje ręcznie (funkcja Manual Options robi
dokładnie to) — nie może być nadpisywany co kilka sekund pod czytającym.

### Pliki .ini nie mogą mieć znacznika BOM

`GetPrivateProfileString` przy obecnym BOM (bajty `EF BB BF`) **nie widzi
pierwszej sekcji** i nie zgłasza błędu — po prostu zwraca pustą wartość. To
awaria cicha: program działa, tylko bez skrótów klawiszowych. Skrypt budowania
`zbuduj.sh` ma wbudowaną kontrolę i zatrzymuje się, gdy BOM wróci do pliku .ini.

Dokumenty użytkownika w UTF-8 BOM mieć **mają**. Pliki .ini i skrypty — nie.

### Limit 260 znaków przy czytaniu .ini

`Ini.ReadValue` opiera się na `GetPrivateProfileString`, który ma bufor 260
znaków i **ucina wartość po cichu**. Lista zakładek w dużym dokumencie bywa
dłuższa. Dlatego `Sesja.cs` czyta swój plik własnym kodem przez
`File.ReadAllLines`, a nie przez `Ini.ReadValue`.

## Kompilacja

```
zbuduj.sh <wersja>
  ├─ kontrola BOM w plikach .ini
  ├─ kopiowanie źródeł do /mnt/c/EdSharpBuild   (kompilator jest po stronie Windows)
  ├─ cmd.exe BuildEdSharp.cmd                  (csc z JAWNĄ listą plików .cs)
  └─ Inno Setup → dist/EdSharpNG_Setup_<wersja>.exe
```

**Nowy plik `.cs` trzeba ręcznie dopisać do listy w `BuildEdSharp.cmd`.**
`zbuduj.sh` kopiuje wszystkie `*.cs`, ale kompilator dostaje wyliczoną listę.
Pominięcie kończy się błędem `CS0246` o nieznanym typie.

Szczegóły w `BUDOWANIE.md`.
