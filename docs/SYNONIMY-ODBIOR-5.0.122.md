# Synonimy — odbiór prywatnego przyrostu 5.0.122

Stan: historyczny prywatny build z odebranymi testami danych i interfejsu.
Niezależny przegląd wykrył potem błędne potwierdzenie zapisu ustawień oraz
rozbieżność wartości `EN`. Poprawki i końcowy odbiór są w
`SYNONIMY-ODBIOR-5.0.123.md`. Nie traktować 5.0.122 jako kandydata do publikacji.

## Zachowanie

- Shift+F7: płaska lista synonimów. Enter wstawia wyświetlony wyraz.
- Prawa strzałka: źródłowe znaczenie wybranej pozycji. F4 pozostaje aliasem.
- Shift+Enter: poświadczone odmiany dokładnie wybranego słowa, części mowy
  i znaczenia. Enter na odmianie wstawia ją i zamyka obie listy.
- Escape ze szczegółów, odmian i pomocy wraca do tego samego synonimu.
- F1: pomoc. Przyciski Znaczenie i Odmiany wykonują te same działania.
- Opcje, Ctrl+przecinek: Słownik synonimów, Polski / Angielski / Oba.
  Misc → Thesaurus Dictionary oraz paleta otwierają to samo pole Opcji.

## Wykonane testy

Pełny `BuildEdSharp.cmd`: kod 0. Następnie kanoniczny
`build_installer_garfield.sh 5.0.122` z prywatnym stagingiem: kod 0.
Binarka testowana końcowo przez harness ma ten sam SHA-256 co binarka
z rzeczywistej instalacji i stagingu.[^package]

Python: 35 testów. Ustawienie C#: jedna lista, poprawne wartości i etykiety,
wartość domyślna oraz round-trip — OK.

Backend odmian C#: 69 asercji PASS, 0 FAIL. Fixture obejmuje dokładny synset/POS,
homonimy, już odmienione wybrane słowo, brak i uszkodzenie zasobu, izolację
zwykłego lookupu, odmowę form obcego znaczenia oraz brak wymyślonego `goed`.
Osobny RED/GREEN wykrył gubienie przypadków skompresowanych jako `nom.acc.voc`;
końcowy kod tłumaczy wszystkie źródłowe wartości na polskie nazwy.[^forms]

Jawny zestaw polski: 37 obsłużonych wyborów bez błędu, z czego 32 z niepustymi
formami pierwszego wybranego synonimu. Pozostałe 5 to brak poświadczonych form,
nie dowód awarii ani pełnego pokrycia polszczyzny. Sprawdzono także 12 wejść
angielskich opartych na wyjątkach; test ten nie mierzy kompletnej gramatyki.
Istniejące regresje C#: 43 oraz 268 asercji PASS, bez FAIL.[^forms]

Żywy NVDA, działający czytnik i rzeczywisty podgląd mowy:[^nvda]

- `dom`: lista 12 pozycji, przejście do `lokum`, prawa strzałka, tekst tylko do
  odczytu z definicją/przykładem/źródłem, Escape do tej samej pozycji z fokusem.
- Shift+Enter na `lokum`: lista form. Escape nie zmienia dokumentu.
  Wybranie `lokach` i Enter wstawia dokładnie `lokach`, zamyka obie listy.
- F1, powrót z pomocy, przyciski osiągnięte Tabem i wykonane Enterem.
  F4 na `epistoła` nadal otwiera źródłowe szczegóły.
- Zmiana Opcji na Angielski działa od następnego wywołania bez restartu.
  Paleta kieruje od razu na to samo pole. Anulowanie zmiany na Oba zachowuje EN.
- `depart → go`: dostępne `gone` i `went`. `go away`: jawny brak odmian,
  po powrocie zwykły Enter nadal wstawia `go away`.
- `DEPART`: wybór odmiany wstawia `WENT`, zachowując kapitalizację.
- Zapis Oba przetrwał restart prywatnego procesu i profilu. `list`: 13 pozycji,
  4 polskie i 9 angielskich, z oznaczeniem języka.
- Końcowa spakowana binarka: opis `kąt` zachowuje źródłowe „mianownik, biernik”.
  Tymczasowy brak prywatnego indeksu PL daje czytelne ostrzeżenie; indeks
  odtworzono ze zgodnym SHA. Zwykłe Enter nadal działa, Ctrl+Z przywraca `dom`.
- Zakończono wyłącznie własne procesy próbne. NVDA pozostał uruchomiony;
  istniejącego wcześniej podglądu mowy ani ustawień czytnika nie zmieniano.

Pomiar interfejsu używa osobnego harnessu `testy/probe_synonyms_ux.cs`, który
ładuje rzeczywisty `EdSharpNG.exe` i jego klasy okien, ale omija `App.Main`
i korzysta z prywatnego profilu. Nie jest to test pierwszego startu App.Main
ani instalowania dodatku NVDA. Wykonane gesty i mowę oddzielono od kontroli
samego kodu źródłowego. W zapisie powrotu ze szczegółów pojawia się czasem
powtórzenie nazwy tej samej pozycji; zachowanie wyboru i fokusu jest poprawne.

## Instalator i ZIP

Instalator uruchomiono faktycznie na Hermesie do
`C:\EdSharp122UxInstalled`: `INSTALL_EXIT=0`. Całą transakcję wykonano
z uprawnieniami pozwalającymi także odtworzyć stan. Klucze rejestru i skróty
odtworzono i porównano bajtowo: `RESTORED_BYTES_EQUAL`.[^install]

Manifest obejmuje **1083 pliki** zgodne między deklaracjami instalatora,
stagingiem i faktyczną instalacją. ZIP ma **1084 wpisy plikowe**: te same
1083 pliki oraz manifest. Wszystkie rozmiary i SHA-256 zostały sprawdzone,
bez duplikatów i niebezpiecznych ścieżek. Komponenty odinstalowania nie są
zawartością przenośnego ZIP-a.[^package]

Instalator: `dist/EdSharpNG_Setup_5.0.122.exe`, 114000740 bajtów.
SHA-256: `00ba46241b4712b923796dde5a1e8f59c97f281ecd659f3adb0d2f212e9b490b`.

ZIP: `dist/EdSharpNG_5.0.122.zip`, 148421451 bajtów.
SHA-256: `2a47a6a0a436cd94a79e77142495ffa5fd3793fede5eaa21036c03ce02e8067c`.

EXE: `9bb0b55317c1c3aed90e797818a2e01cd7d41c9d1f911a9b61cc4add29dcbd64`.
Semantyczna wersja aplikacji to `App.VersionString=5.0.122`; automatycznie
numerowany zasób Windows FileVersion ma `5.0.9810.23459`. Nie utożsamiać tych pól.

## Ograniczenia

Nie generujemy końcówek. PL jest ograniczone do zgodnych wpisów źródłowego
indeksu; OMW nie pożycza form plWordNet po samym napisie. Angielski udostępnia
tylko wyjątki WordNet, nie pełny paradygmat regularny ani opis czasu gramatycznego,
którego te dane nie dostarczają. Opisy i przykłady znaczeń nie są wymyślane.

Lista może zawierać ten sam zapis formy z różnymi poświadczonymi opisami,
np. miejscownik i wołacz. Deduplikowane są identyczne pary zapis/opis.
Zestawy kontrolne nie są statystyczną oceną pokrycia całego języka.

Nowy moduł korzysta wyłącznie z lokalnych plików. Nie powtarzano blokady
zaporą dla całego nowego GUI; wcześniejsze wykonawcze QA offline routingu
pozostaje w raporcie 5.0.121. Nie wymagamy runtime Pythona, NLTK, SQL ani Worda.
Nie publikowano, nie wypychano źródeł i nie podmieniano głównej instalacji.

[^forms]: `../testy/wyniki/forms-qa/parent-compressed-red.log.utf8`,
    `parent-compressed-green.log.utf8`, `existing-csharp.log`,
    `existing-real-csharp-complete.log.utf8`.
[^nvda]: `../testy/wyniki/synonyms122/nvda-ui-events.json` i `nvda-speech.txt`;
    surowe odczyty mostka są także w historii sesji. Poprawność ciągłości zapisu
    podglądu mowy i stanów interfejsu sprawdzono programowo.
[^install]: `../testy/wyniki/synonyms122/installer-transaction.log`;
    pełny dziennik Inno: `C:\EdSharpUxQA\install122-inno.log`.
[^package]: `../testy/wyniki/synonyms122/package-manifest.json` i
    `package-verified.json`.
