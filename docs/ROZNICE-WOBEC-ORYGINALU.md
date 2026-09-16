# EdSharpNG kontra oryginalny EdSharp — co się zmieniło

Stan na wersję 5.0.111 (16.09.2026). Ten plik jest przeznaczony do
**pokazywania ludziom**: użytkownikom, którzy znają EdSharpa Jamala Mazrui
i chcą wiedzieć, co dostają u nas, oraz osobom piszącym o programie.

Wszystkie liczby niżej są zmierzone w kodzie w dniu powstania wpisu, nie
przepisane z pamięci. Sposób pomiaru podany jest przy każdej liczbie, żeby
dało się ją powtórzyć.

Pisane listami, nie tabelami — ten plik czyta się czytnikiem ekranu.

---

## 1. Punkt odniesienia

Porównanie idzie do **EdSharp 5.0 beta Jamala Mazrui**, czyli do stanu kodu
zapisanego w naszym repozytorium jako pierwszy commit `62f07a5` („EdSharpNG
5.0.73 — punkt startowy"). To zrzut kodu upstreamu z naszymi pierwszymi
poprawkami budowania, nie klon repozytorium autora — dlatego historia gita
nie idzie głębiej.

Oryginał: <https://github.com/JamalMazrui/EdSharp>, licencja LGPL v3.
Nasza gałąź: <https://github.com/michalkasperczak/EdSharpNG>.
Nasza wersja **nie jest wydawana ani wspierana przez autora oryginału**;
zgłoszenia błędów idą do naszego repozytorium.

Zmierzone różnice w rozmiarze (git, `wc -l`):

- pliki w repozytorium: 1252 w punkcie startowym, 1154 teraz (mniej, bo
  wyleciało 168 plików balastu: stare kopie kodu, obce biblioteki, prywatna
  korespondencja w plikach .txt),
- `EdSharp.cs`: 21 802 wiersze w punkcie startowym, 24 140 teraz,
- cały program: 12 plików C#, 30 792 wiersze (`wc -l` na liście plików
  podanej w `BuildEdSharp.cmd`),
- zmiana łączna od punktu startowego: 264 pliki, 15 023 wiersze dodane,
  200 389 usuniętych (`git diff --stat 62f07a5 HEAD`).

---

## 2. Kierunek: edytor tekstu, nie środowisko programisty

To jedna różnica, z której wynika większość pozostałych. Oryginalny EdSharp
był jednocześnie edytorem tekstu **i** zestawem narzędzi programisty z lat
2000. EdSharpNG ma być czytelnym edytorem tekstu i Markdowna dla osoby
niewidomej.

Skutek policzalny (porównanie `Hotkeys.ini` między `62f07a5` i stanem
obecnym): **230 poleceń w oryginale, 217 u nas. Usunięto 18, dodano 5,
dwa straciły stary skrót.**

### 2.1. Usunięte polecenia (18)

Narzędzia programisty:

- `Compile` (Ctrl+F5), `Pick Compiler` (Ctrl+Shift+F5),
  `Review Output` (Alt+Shift+F5), `Say Compiler` (Ctrl+F9) — kompilowanie
  kodu z edytora, razem z całym mechanizmem plików `<Kompilator>.ini`,
- `Run` (menu File) i `Run at Cursor` (Shift+F5) — uruchamianie bieżącego
  pliku jako programu,
- `PyDent` (Alt+[) i `PyBrace` (Alt+Shift+[) — przerabianie kodu Pythona
  z wcięć na nawiasy i odwrotnie. Powstały, gdy czytniki ekranu nie mówiły
  poziomu wcięcia; dziś mówią, a EdSharpNG ma do tego własne `Alt+I`.

Ustawiacze bogatego formatowania (spadek po RTF, w Markdownie nie ma jak
tego zapisać):

- `Justify`, `Style`, `Baseline`, `Set Selection Font`, `Say Font`,
  `Set Default Font and Color`.

Narzędzia sieciowe z menu Miscellaneous:

- `Web Download` (Alt+Shift+W), `Web Client Utilities` (Alt+Shift+Spacja).
  Zostało z nich wewnętrzne pobieranie pliku — służy teraz autoaktualizacji.

Pozostałe:

- `Open Other Format` — jedno otwieranie (Ctrl+O) rozpoznaje formaty samo,
- `Text Combine` — łączenie wielu formatów w jedno okno.

### 2.2. Usunięta warstwa skryptów JScript .NET (5.0.111)

Największa pojedyncza zmiana architektury. Oryginał woził osobną bibliotekę
`EdSharp.dll`, zbudowaną kompilatorem `jsc.exe` ze źródła `EdSharp.js`,
i wczytywał ją w czasie działania przez refleksję. Służyła do uruchamiania
skryptów w JScript .NET — języku, który Microsoft porzucił.

Co z tym zniknęło:

- plik `EdSharp.dll` z instalatora,
- plik źródłowy `EdSharp.js` z repozytorium i z paczki,
- krok `jsc.exe` z kompilacji,
- późne wiązanie przez `Assembly.LoadFrom`, czyli klasa błędów, których
  kompilator nie wyłapie.

Co zostało zachowane: **liczenie wyrażeń** (polecenie `Evaluate Expression`)
i **rozwijanie sekwencji z odwrotnym ukośnikiem** — oba chodziły przez
JScript. Zastąpił je nowy plik `Wyrazenia.cs` (432 wiersze): własny parser
czterech działań, potęgi, reszty z dzielenia, nawiasów, procentu i garści
funkcji matematycznych. Kalkulator, nie język programowania — nie umie
zmiennych, przypisań, pętli ani wywołań systemowych i ma nie umieć.
Przyjmuje przecinek **i** kropkę jako separator dziesiętny, bo w polskim
układzie klawiatury naturalny jest przecinek; wynik wypisuje zawsze z kropką.

`Invoke Snippet` został jako czyste wklejanie tekstu.

### 2.3. Usunięte i zmienione opcje ustawień (5.0.111)

Oryginał miał 27 opcji w sekcji `[Options]`, część z nich martwych albo
związanych wyłącznie z kompilowaniem kodu. Usunięte:

- `CompileCommand`, `JumpPosition`, `AbbreviateOutput` — razem z poleceniami
  budowania kodu,
- `KeepBackup` — kopia `.bak` nadpisywanego pliku,
- `HardPageAddress` wraz z `GetPageAddress` — numer strony liczony ze znaków
  wysuwu strony. Uwaga historyczna, warta powiedzenia ludziom: do 13.09.2026
  pod tą opcją wisiały **omyłkowo także** komunikaty o pustym wierszu,
  tabulatorze i znaku wysuwu strony, a opcja była fabrycznie wyłączona —
  więc program o pustym wierszu w ogóle nie mówił. Zależność zdjęta,
  komunikaty są zawsze,
- `SectionBreak`,
- pozycja „do znaku wysuwu strony" w `LimitItem`.

Zmienione domyślne: `MaximizeWindow` fabrycznie **włączone** (w oryginale
wyłączone). Lista plików numerowanych przeniesiona na Alt+0, cyfry 1–9
wybierają plik z klawiatury.

Pełny spis wszystkich opcji z opisem, co każda robi: `docs/OPCJE-USTAWIEN.md`.

---

## 3. Nowe funkcje, których w oryginale nie ma

Pięć nowych poleceń w `Hotkeys.ini` i kilka mechanizmów bez własnego klawisza.

### 3.1. Paleta poleceń (Ctrl+Shift+F1), 5.0.79

Lista wszystkich poleceń z wyszukiwaniem po nazwie. Czyta nazwę polecenia
przed nazwą menu i mówi skrót po ludzku („Control plus Shift plus F1"),
a skrót bierze **z kodu**, nie z osobnej listy, żeby nie mógł się rozjechać.

Do 5.0.111 paleta siedziała na Ctrl+Shift+X; w 5.0.112 oddała ten klawisz
listom zadań, a samouczek zszedł z Ctrl+Shift+F1 na Ctrl+Alt+F1.

### 3.1a. Listy zadań, czyli checklisty (5.0.112)

Trzeci rodzaj listy obok punktowanej i numerowanej, w składni, którą rozumie
GitHub i większość edytorów Markdown:

    - [ ] jeszcze niezrobione
    - [x] zrobione

Cztery klawisze:

- **Ctrl+Shift+X** — przełącza pozycję zrobione/niezrobione i mówi nowy stan.
  Na kilku zaznaczonych wierszach ustawia je wszystkie jednakowo (według
  pierwszej pozycji), żeby zaznaczenie nie kończyło się mieszanką.
- **Ctrl+Shift+F2** — zamienia wiersze w listę zadań, a na gotowej liście
  zdejmuje pola i zostawia czysty tekst.
- **Ctrl+Shift+F7** — okno „Task List": wszystkie zadania z dokumentu, spacja
  przełącza stan **bez wychodzenia z okna**, Enter skacze do zadania w tekście.
  Tytuł okna niesie postęp, więc czytnik podaje go przy wejściu.
- **Alt+Shift+F2** — mówi postęp („3 of 12 done, 25 percent").

Pola wyboru rozumieją też funkcje, które istniały wcześniej: Enter kontynuuje
listę zadań (nowa pozycja jest **zawsze niezrobiona**, nawet po Enterze na
odhaczonej), Ctrl+L i Ctrl+Shift+L zdejmują pole razem ze znacznikiem, a nie
zostawiają w tekście gołego `[ ]`, kopiowanie do Worda i nazwy sekcji też.

W eksporcie do HTML powstaje **prawdziwe pole wyboru** (`<input
type="checkbox" disabled>`) z etykietą powiązaną przez `for`, więc czytnik
czyta „pole wyboru zaznaczone, zapłacić rachunek", a nie nawiasy. Pole jest
wyłączone do klikania, bo strona nie ma gdzie zapisać zmiany.

Dlaczego klawisze funkcyjne, a nie Ctrl+Alt+X: **X ma polski odpowiednik pod
prawym Altem**, a na takim klawiszu układ klawiatury wytwarza literę, zanim
skrót dojdzie do programu — komenda po prostu by nie zadziałała.

### 3.2. Ciągłość pracy (5.0.93–5.0.94)

Przywracanie sesji po zamknięciu programu i autozapis kopii ratunkowych.
Nowy plik `Sesja.cs` (349 wierszy) i osobne pliki danych `Sesja.ini` oraz
`Odzysk\*.odzysk` — celowo nie w `EdSharp.ini`, bo tamten użytkownik
otwiera i edytuje ręcznie. Własne okno przełączników w menu Misc
(`Work Continuity`), zamiast grzebania w pliku ustawień.

### 3.3. Polskie sprawdzanie pisowni bez Worda (5.0.84, przebudowa 5.0.89)

Nowy plik `Pisownia.cs` (229 wierszy): sprawdzaczem wbudowanym w Windows
(Windows Spell Checking API), słownik pl-PL, dodawanie wyrazów do słownika
systemowego. Zmierzone 89 ms na 16 tysięcy znaków. Oryginał wymagał
zainstalowanego Microsoft Worda.

Przebudowa okna po zgłoszeniu Michała: **najpierw lista wszystkich błędów
z otoczeniem w tekście**, dopiero Enter wchodzi w poprawianie; w oknie
poprawiania fokus jest na liście podpowiedzi (wcześniej kursor startował
w polu Replace i użytkownik słyszał tylko jedną propozycję, nie wiedząc
o pozostałych). „Pomiń raz" i „ignoruj wszędzie" to dwa osobne przyciski.
Menu pisowni na wyrazie pod klawiszem Aplikacje (`Word Spelling Menu`).

### 3.4. Stare polskie kodowania (5.0.82)

Mazovia (strona 667), Latin II (852), Windows-1250: rozpoznawanie z bajtów
pliku, komunikat przy otwarciu, konwersja do UTF-8 przy zapisie. Bez tego
polskie pliki z lat dziewięćdziesiątych otwierały się jako śmieci.

### 3.5. CSV jako dostępna tabela (5.0.87)

Nowy plik `Csv.cs` (257 wierszy), czytnik i zapisywacz wg RFC 4180 bez
zewnętrznych bibliotek. Nazwy kolumn brane z pierwszego wiersza, więc
czytnik mówi „ludność: 800653" zamiast kazać liczyć przecinki.
Rozpoznaje separator (przecinek, średnik polskiego Excela, tabulator,
kreskę). Ctrl+Enter zapisuje do pliku, poprzednia wersja zostaje w `.bak`.

### 3.6. Okno ustawień z polami wyboru (5.0.96)

Nowy plik `Ustawienia.cs` (267 wierszy). W oryginale ustawienia wpisywało
się z klawiatury jako tekst; teraz wybiera się je z pól wyboru i list.
Ręczna edycja pliku ustawień została jako osobne polecenie.

### 3.7. Samoaktualizacja i dociąganie narzędzi (5.0.81, 5.0.85)

- sprawdzanie nowej wersji przy starcie, raz na dobę, po cichu, oraz na
  żądanie pod F11; instalator w trybie cichym; program sam zamyka się przed
  aktualizacją, ale niezapisane pliki dalej pytają o zapis, a anulowanie
  przerywa aktualizację,
- suma SHA-256 paczki liczona automatycznie i sprawdzana po pobraniu,
- nowy plik `Skladniki.cs` (455 wierszy): brakujące narzędzia konwersji
  (pandoc, tidy, xpdf, liblouis, astyle) dociągane w tle, bez pytań,
  raz na dobę. W oryginale pobierał je tylko skrypt budowania, więc
  użytkownik ich nigdy nie miał i konwersje po cichu padały.

### 3.8. Zgłaszanie błędów z programu (`Report a Problem`) i samouczek (`Tutorial`)

Dwa nowe polecenia: pierwsze otwiera nasze repozytorium ze zgłoszeniami,
drugie — samouczek dla początkujących.

---

## 4. Poprawki dostępności, które zmieniają codzienną pracę

Te rzeczy nie są nowymi funkcjami, ale są tym, co użytkownik czytnika
ekranu odczuwa najmocniej.

- **Okna komunikatów czytane we właściwej kolejności** (5.0.88). Zgłoszenie:
  F11 czytał „OK" przed treścią „EdSharpNG jest aktualny". Przyczyna:
  w systemowym `MessageBox` fokus startuje na przycisku. Poprawka: własne
  okno, w którym treść jest polem tylko do czytania i to ono ma fokus —
  kolejność wynika z budowy okna, nie z opóźnienia. Treść da się przeczytać
  ponownie strzałkami i skopiować. Wzorzec zastosowany w całym programie.
- **Kolejność tabulacji przycisków** (5.0.90). Tab idzie w kolejności
  podanej w kodzie (Yes, No, Cancel, Help), nie odwrotnie. Dotyczyło
  każdego okna w programie.
- **Alt+F4 zamyka okno, nie program** (5.0.79). W oryginale Alt+F4 był
  zarejestrowany jako „Exit EdSharp" i działał tak na każdej liście
  i w każdym oknie. Windows standardowo zamyka tym oknem.
- **Escape zamyka każde okno** (5.0.91).
- **Zakładki czytają treść wiersza**, nie numer linii (5.0.80).
- **Karetka i zaznaczenie zostają widoczne po Alt+Tab** (5.0.83). Przyczyna
  zmierzona: `RichTextBox` ma `HideSelection` domyślnie włączone, a EdSharp
  nie ustawiał tego nigdzie. Usunięte też czekanie 100 ms po każdym ruchu
  kursora.
- **Puste wiersze: program milczy, głos oddany czytnikowi** (5.0.107–5.0.109).
- **Ctrl+C mówi „Copied"**, a na listach plików Ctrl+C kopiuje nazwę,
  Ctrl+Shift+C ścieżkę (5.0.75, 5.0.95).
- **Podsumowanie skrótów generowane ze źródła** (5.0.95). Plik
  `EdSharp_Hotkeys.txt` (Alt+Shift+H) był pisany rękami obok `Hotkeys.ini`
  i rozjechał się doszczętnie: zero opisów komentarzy, stare skróty
  zakładek, Ctrl+T dla polecenia, które dawno ma inny klawisz. Użytkownik
  niewidomy nie ma jak tego wychwycić — lista wygląda normalnie, tylko mówi
  nieprawdę. Teraz powstaje przy każdym budowaniu.
- **Nawigacja po przypisach i komentarzach przeniesiona do menu Navigate**
  (5.0.95), skoki po komentarzach na Alt+Shift+PageUp/PageDown (5.0.97).
- **Dodatek NVDA ze sprawdzaniem pisowni** jedzie w paczce instalatora.

---

## 5. Zmiany w budowaniu i wydawaniu

Nie widzi ich użytkownik, ale decydują o tym, czy wydanie jest wiarygodne.

- Kompilacja jednym `csc`, x64/ARM64 (AnyCPU), bez MSBuild i bez menedżera
  pakietów. Po 5.0.111 **bez kroku `jsc.exe`** — jeden kompilator, nie dwa.
- Instalator Inno Setup zamiast starego skryptu: bez wykrywania Javy i bez
  32-bitowych bibliotek wsparcia (JsSupport, VbSupport, saapi32,
  nvdaControllerClient32).
- `zbuduj.sh` odmawia pracy, gdy: numer wersji w kodzie nie zgadza się
  z numerem paczki, plik `.ini` ma znacznik BOM (cicha awaria — Windows nie
  widzi wtedy pierwszej sekcji), binarka jest starsza niż źródło, paczka
  starsza niż binarka, albo binarka woła `Ude`, a biblioteki nie ma w repo.
- `wydaj.sh` liczy sumę SHA-256 z pliku, wstawia ją do opisu wydania,
  a potem **odczytuje opis z GitHuba i sprawdza, czy suma tam naprawdę
  jest**. Zabezpieczenie, które zależy od tego, czy ktoś pamiętał wkleić
  64 znaki, nie jest zabezpieczeniem.
- 122 pliki pomiarów w `testy/` — każda poprawka ma dowód uruchamialny
  ponownie, nie deklarację.

Szczegóły: `docs/BUDOWANIE.md`, `docs/POMIARY.md`.

---

## 6. Autorstwo i porządki w repozytorium

- Wydawcą paczki jest Michał Kasperczak, prawa do zmian EdSharpNG 2026
  Michał Kasperczak, przy zachowaniu informacji o oryginale („based on
  EdSharp, copyright 2006–2026 by Jamal Mazrui"). Adresy w programie
  i w instalatorze wskazują nasze repozytorium.
- Usunięty balast odziedziczony po zrzucie kodu: 21 starych kopii samego
  programu, obce biblioteki (textile, brltex, detect, latexaccess,
  markdown.net), obcy panel PHP bez związku z edytorem, skrypty Pythona.
- **Usunięte dane osobowe.** W 338 plikach `.txt` odziedziczonych po
  oryginale było 156 prawdziwych adresów e-mail z prywatnej korespondencji
  (zgłoszenia błędów z podpisami, numery do połączeń konferencyjnych).
  Repozytorium zostało upublicznione dopiero po ich usunięciu.

---

## 7. Czego celowo nie ruszamy

Żeby porównanie było uczciwe, to też trzeba powiedzieć:

- **Interfejs zostaje angielski** do czasu pełnego tłumaczenia (decyzja
  właściciela projektu, 18.08.2026). Mieszanie języków w jednym oknie jest
  gorsze niż konsekwentnie jedno — czytnik czyta wtedy polskie słowa
  angielską wymową. Polska wersja interfejsu jest ostatnim punktem mapy
  drogowej.
- **Domyślna czcionka i jej rozmiar zostają.** Wyleciały ustawiacze
  formatowania RTF, ale nie to: dla osoby słabowidzącej czcionka jest rzeczą
  praktyczną, nie ozdobą.
- **Odczyt plików .rtf zostaje** — to nie formatowanie, to możliwość
  otwarcia cudzego pliku. Zapisu bogatego formatowania nie ma.
- **Komentarze zostają obok zakładek nazwanych**, choć wyglądają podobnie.
  Komentarz jest przywiązany do treści, zakładka do numeru wiersza: po
  dopisaniu akapitu w środku dokumentu zakładka zostaje na starym numerze,
  komentarz idzie z tekstem. Osobno: pliki .docx noszą komentarze recenzenta
  i EdSharpNG umie je wczytać.
- **Skoki po komentarzach zostają.** Zdjęte zostały tylko stare skróty
  z rodziny F9.
- **Prompt Command (Alt+F5) i Go to Environment zostają** — to zwykłe
  „otwórz mi wiersz poleceń tutaj", przydatne niezależnie od kompilowania.

Pełna analiza z decyzjami wers po wersie: `docs/CO-USUWAMY.md`.

---

## 8. Jak powtórzyć te pomiary

```bash
cd ~/projekty/edsharp
# rozmiar zmiany wobec punktu startowego
git diff --stat 62f07a5 HEAD | tail -3
# liczba wierszy programu
wc -l EdSharp.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs \
      Pisownia.cs Skladniki.cs Csv.cs Sesja.cs Ustawienia.cs Wyrazenia.cs
# różnica w poleceniach: porównanie Hotkeys.ini z punktem startowym
git show 62f07a5:Hotkeys.ini > /tmp/hk_base.ini
```

Porównanie poleceń zrobione skryptem czytającym oba pliki `Hotkeys.ini`
i różnicującym zbiory kluczy — wynik z 16.09.2026 jest w rozdziale 2.
