# Synonimy — prywatny odbiór 5.0.123 po przeglądzie

Stan: obie uwagi przeglądu odtworzone, poprawione i sprawdzone wykonawczo.
Pełny build, instalator, ZIP oraz pomiar żywego NVDA zakończyły się pomyślnie.
Niezależny przegląd potwierdził obie poprawki i nie znalazł nowego blokera
w badanym zakresie. Odbiór prywatnego etapu zakończony. Przegląd opierał się
na kodzie i zachowanych logach; wykonawcze testy oraz NVDA przeprowadził
wykonawca etapu, nie recenzent. Wynik: `../testy/wyniki/synonyms123/review-followup.md`.
Nie jest to publikacja ani zgoda na instalację na głównym komputerze.

## Funkcje

Obowiązuje zatwierdzony układ: Shift+F7 — płaska lista; Enter — wstawienie;
prawa strzałka / F4 — źródłowe znaczenie; Shift+Enter — poświadczone odmiany;
Escape z dziecka — powrót do wybranego synonimu. F1 i przyciski pozostają
alternatywnymi drogami. W Opcjach jest jeden zapamiętywany wybór Polski,
Angielski, Oba, niezależny od interfejsu oraz sprawdzania pisowni.

Dane i ograniczenia pozostają takie jak w [odbiorze 5.0.122](SYNONIMY-ODBIOR-5.0.122.md).
W szczególności EN zawiera tylko poświadczone wyjątki, nie pełny paradygmat;
PL wymaga zgodnego lematu, synsetu i POS. Nie generujemy ani nie zgadujemy form.

## Rozstrzygnięcie uwag niezależnego przeglądu

**P1: fałszywe potwierdzenie zapisu.** Rzeczywisty formularz Ustawień z plikiem
INI tylko do odczytu ogłaszał `One setting saved`, choć wybór pozostawał polski.
Analogiczna próba Restore defaults zwracała fałszywe potwierdzenie przywrócenia.
Teraz liczymy wyłącznie udane `App.WriteOption`; błędy są ogłaszane globalnie
z listą niezapisanych ustawień, oddzielnie od liczby udanych zapisów.
Nie deklarujemy transakcji: przy częściowym powodzeniu udane zapisy zostają.

Wykonano cztery warianty prawdziwego formularza i natywnego API INI:
OK oraz Restore defaults, każdy dla pliku zapisywalnego i tylko do odczytu.
Wszystkie zakończyły się PASS. Kalibracja potwierdza odmowę rzeczywistego
`App.WriteOption`, bez podstawiania wyniku. RED i GREEN są zachowane.[^settings]

**P2: `EN` w Opcjach, ale polski runtime.** Loader normalizuje teraz spacje,
zewnętrzne cudzysłowy i wielkość liter tak samo jak wybór na liście Opcji.
Sześć wariantów EN/BOTH najpierw wykazało 6 FAIL, po poprawce 0 FAIL.
Tryb nierozpoznany nadal bezpiecznie wybiera polski; samo otwarcie Opcji nie
usuwa nierozpoznanej własnej wartości. Test routingu: 31 asercji PASS.[^modes]

Przegląd nie wskazał blokera routingu Right, Shift+Enter, Enter ani Escape.
Pomoc F1 jest przypięta do listy; przyciski mają nazwy dostępności i działają
z klawiatury, bez osobnych focus tipów.

## Testy końcowe

- Pełny `BuildEdSharp.cmd`: kod 0. Kanoniczne pakowanie przez
  `build_installer_garfield.sh 5.0.123`: kod 0.
- Python: 35 testów, OK.[^python]
- Backend odmian: 69 PASS, 0 FAIL. 37 rzeczywistych polskich wyborów bez błędu,
  32 z niepustymi formami pierwszego wybranego synonimu; 12 angielskich wejść
  wyjątkowych. To nie jest pomiar pokrycia całego języka.[^forms]
- Rzeczywiste zainstalowane zasoby: 268 PASS; jawny zestaw 100 angielskich
  wejść, 97 z wynikami. Sprawdzone także rozdzielenie języków, szczegóły źródłowe
  i polskie `domami → chałupami`. Bez błędów zasobów.[^real]

Żywy NVDA w końcowej binarce:[^nvda]

- Ctrl+przecinek, cztery Tab, wybór Angielski i Enter przy INI tylko do odczytu:
  słyszalne „Nie zapisano ustawień: Słownik synonimów. Sprawdź dostęp do pliku
  ustawień.” Zachowano wcześniejsze Oba, bez fałszywego potwierdzenia.
- Po przywróceniu zapisywalności wpisano istniejące `SynonymDictionary= EN `.
  Shift+F7 dla `depart` zwrócił angielską listę, zamiast polskiego fallbacku.
- Prawa strzałka otworzyła definicję/przykład/źródło WordNet dla `go`.
  Escape wrócił do `go`; Shift+Enter otworzył `gone` / `went`.
  Dół i Enter wstawiły dokładnie `went` i zamknęły obie listy.

Zachowano 7 snapshotów (w tym punkt początkowy); ciągłość podglądu mowy i stan
interfejsu sprawdzono programowo. Pomiar 5.0.122 zawiera szersze przypadki
polskie, F1, przyciski, F4, kapitalizację i trwałość wyboru. Porównanie kodu
`PickSynonyms` i `PickSynonymForm` potwierdza, że nie zmieniły się między
spakowanymi wersjami. Nie przedstawiamy starych prób jako powtórzonych w całości.

Harness ładuje rzeczywisty `EdSharpNG.exe` i jego klasy okien, ale omija
`App.Main` i używa prywatnego profilu. To nie jest ponowny test pierwszego
uruchomienia aplikacji ani instalacji dodatku czytnika. NVDA oraz istniejący
podgląd mowy pozostały uruchomione; zamykano wyłącznie własne procesy próbne.

## Dostawa na główny komputer

Na osobne polecenie Michała dostarczono trzy pliki do komputera `MICHAL`,
folder `D:\Projekty Codex\Hermes`: instalator i ZIP 5.0.123 oraz
`EdSharpNG_5.0.123-Instrukcja.txt`. Pełne SHA-256 i rozmiary wszystkich
trzech docelowych plików porównano z lokalnymi oryginałami. Prywatny staging
transferu usunięto. Dowód: `../testy/wyniki/synonyms123/main-delivery-verified.json`.

To dostawa plików, nie wykonanie instalacji. Transfer nie wydawał poleceń
zamykania ani uruchamiania edytora. Zestaw procesów i binarka już zainstalowanego
programu zmieniły się w czasie dostawy; nie oznaczamy ich jako niezmienionych
ani nie przypisujemy tej zmiany naszemu transferowi. Nie podmieniano instalacji
w celu uzyskania zgodnego porównania.

## Paczki i instalacja

Prawdziwa prywatna instalacja na Hermesie do `C:\EdSharp123UxInstalled`:
`INSTALL_EXIT=0`. Zmiany kluczy rejestru i skrótów odtworzono i porównano
bajtowo: `RESTORED_BYTES_EQUAL`. Główny komputer nie był celem tej próby.[^install]

Manifest obejmuje 1083 pliki: zgodne między deklaracjami instalatora,
stagingiem i instalacją. ZIP ma 1084 wpisy plikowe: ten sam zbiór oraz manifest.
Każdy rozmiar i SHA-256 sprawdzono, bez duplikatów i niebezpiecznych ścieżek.[^package]

Instalator: `dist/EdSharpNG_Setup_5.0.123.exe`, 113999216 bajtów.
SHA-256: `e189ff2fc19e44466be74fdd8da7f0ddee3422d58d8563c0488a8821f3389b20`.

ZIP: `dist/EdSharpNG_5.0.123.zip`, 148421861 bajtów.
SHA-256: `da6021e8597e160592c4fe744d0f38355599287090d829d8dda2719032584a67`.

EXE testowany przez harness, kompilacji, stagingu i instalacji jest bajtowo ten sam:
`24b3c927d9b117443ccdb4e6c41453979927721bcbe2339d7b9c5be246fa4b2e`.
Wersja aplikacji: `App.VersionString=5.0.123`; automatyczny zasób Windows
FileVersion: `5.0.9810.24574`. To różne pola, nie dowód starszego programu.

Nie ponawiano blokady zaporą całego GUI; wcześniejszy pomiar offline routingu
pozostaje w odbiorze 5.0.121. Runtime nowych funkcji korzysta wyłącznie z lokalnych
plików, bez Pythona, NLTK, SQL, Worda i generowania tekstu przez model.
Publicznej wersji nie zmieniono, źródeł nie wypchnięto i programu nie instalowano
na głównym komputerze. 5.0.122 zachowano jako wcześniejszy prywatny artefakt,
nie podmieniano go inną treścią pod tym samym numerem.

[^settings]: `../testy/wyniki/synonyms123/settings-write-red.log`,
    `settings-defaults-red.log`, `settings-write-green.log`,
    `settings-defaults-green.log`, `settings-write-success.log`,
    `settings-defaults-success.log`; test: `../testy/test_settings_write_failure.cs`.
[^modes]: `../testy/wyniki/synonyms123/modes-red.log`, `modes-green.log`.
[^python]: `../testy/wyniki/synonyms123/python-green.log`.
[^forms]: `../testy/wyniki/synonyms123/forms-green.log`.
[^real]: `../testy/wyniki/synonyms123/real-green.log`.
[^nvda]: `../testy/wyniki/synonyms123/nvda-ui-events.json`, `nvda-speech.txt`,
    `integration-verified.json`; surowe gesty mostka także w historii sesji.
[^install]: `../testy/wyniki/synonyms123/installer-transaction.log`;
    pełny log Inno: `C:\EdSharpUxQA\install123-inno.log`.
[^package]: `../testy/wyniki/synonyms123/package-manifest.json`,
    `package-verified.json`.
