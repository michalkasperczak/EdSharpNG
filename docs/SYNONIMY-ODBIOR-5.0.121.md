# EdSharpNG 5.0.121 — odbiór prywatnej wersji PL/EN

## Wynik

Przygotowana i odebrana prywatna wersja, bez publikacji i bez instalowania na
głównym komputerze Michała. Synonimy działają z wyborem Polski, English lub oba,
niezależnym od języka interfejsu i pisowni. Wybór w Misc → Thesaurus Dictionary
oraz palecie poleceń zapisuje się w konfiguracji. Shift+F7, Enter, Escape i F4
zachowują dotychczasowe zadania. W trybie obu wyniki wskazują język; identyczny
napis nie łączy polskich i angielskich znaczeń.

## Dane i ograniczenia

Angielski zasób to Princeton WordNet 3.0 z 2006 roku, pochodzący z tego samego
korpusu, którego oryginalny EdSharp używa przez NLTK. Gotowe pliki runtime nie
potrzebują Pythona, NLTK, Worda ani pobierania danych. Pełna licencja jest bajtowo
zgodna z archiwum źródłowym. Szczegóły i bibliografia: `SYNONIMY-ANGIELSKI-2026-11.md`.

Indeks obejmuje 60 874 różne lematy, 65 636 par lemat/część mowy i 45 043
synsety z definicjami. Są 13 332 pierwsze źródłowe przykłady i 3 485 rekordów
poświadczonych wyjątków fleksyjnych. Definicje/przykłady pozostają angielskie.
Formy takie jak `went` kierują do poświadczonego `go`, ale wstawiane synonimy
angielskie pozostają formami podstawowymi. Nie obiecujemy pełnej odmiany EN.

## Weryfikacja

- Pełna kompilacja Windows .NET Framework: kod 0.
- Pełne 33 testy Python: OK. Generator WordNet i snapshot zasobów: 10 testów,
  w tym RED/GREEN dla
  technicznych markerów przymiotnika, późniejszych definicji, pustych cytatów,
  atrybucji i nieprzenoszenia kolejnych przykładów pod etykietę definicji.
- Runtime PL/EN/oba: fixture C# bez błędów. Zgłoszony w review brak ostrzeżenia
  przy usuniętym pliku wyjątków: najpierw FAILURES=2, po poprawce FAILURES=0.
- Pięć programów regresji polskiego słownika C#: bez błędów. Rzeczywiste `domami`
  nadal daje poświadczone `chałupami`; polskie zasoby są identyczne jak w HEAD.
- Jawny zestaw 100 wyrazów EN: 97 trafień; nie jest to losowa próba ani miara
  pokrycia całego języka. Na końcowej binarce mediana 150 ms, maksimum 159 ms.
- Osobny proces z faktyczną blokadą ruchu wychodzącego zapory Windows również
  przeszedł test gotowego providera i danych. Wszystkie profile zapory były
  włączone; regułę dodano tylko dla własnego testu i po pomiarze usunięto.
- Żywy NVDA: polski/angielski/oba dla `list` dały odpowiednio 4/9/13 pozycji.
  Odczytane etykiety języka, nawigacja, szczegóły F4 i powrót do tej samej pozycji,
  wstawienie wybranej frazy Enterem oraz brak zmiany tekstu po Escape.
- Na ostatecznej binarce NVDA odczytał `inflamed, przymiotnik, 1 z 9, English`
  dla `ablaze`, powrót po F4 i `Wstawiono: inflamed`. W polu szczegółów była
  dokładna definicja i przykład właściwego synsetu WordNet, bez markerów `(p)`.
- Usunięcie pliku wyjątków w prywatnej kopii i wyszukanie `went` dało odczytany
  przez NVDA komunikat o niedostępnych formach nieregularnych. Plik przywrócono.
- Instalator uruchomiono na Hermesie w odrębnym katalogu: kod 0. Wszystkie 1082
  pliki z ZIP-a są bajtowo zgodne z instalacją, w tym moduł, dane EN i licencje.
  Odtworzono i porównano bajtowo wcześniejsze wpisy rejestru i skrót stanowiska.

## Rozstrzygnięcie niezależnego review

Review `deleg_c6a86d77` zgłosił brak nowych plików w indeksie Git oraz ciche
pomijanie nieobecnego pliku wyjątków EN. Zależności są teraz śledzone, a ich
faktyczną obecność potwierdziły staging, ZIP i instalacja; drugą usterkę poprawiono
minimalnie, z testem RED/GREEN i odczytem NVDA. Review nie znalazł mieszania
języków, błędnego ID dla F4, POS ani zmiany semantyki Enter/Escape.

Ryzyko powtarzanego synchronicznego parsowania zmierzono zamiast optymalizować
w ciemno: po 20 wywołań, mediana/maksimum PL 348/375 ms, EN 150/169 ms,
oba 521/577 ms. Nie dodawano cache w tym zadaniu; koszt pozostaje jawny.

## Artefakty lokalne — nieopublikowane

- `dist/EdSharpNG_Setup_5.0.121.exe`, 113 986 763 bajty.
  SHA-256: `6b0d48cc0afe3d5f7842ca1e2f4977c634eabde7faea5a846046806d6868d0cf`.
- `dist/EdSharpNG_5.0.121.zip`, 148 379 077 bajtów, 1082 pliki.
  SHA-256: `b886bca8901a8d9fb708e54928e702fae77e0a75b2eb7968b98562074e01b351`.
- EXE testowany i spakowany:
  `ff027c0669fd857302e6cf3d3462bcb027e77ee26eb66d9df3fcde4972ef94c4`.

Pełny manifest: `../testy/wyniki/english-synonyms/package-verified.json`.
Rozstrzygnięcia i tożsamość końcowego kodu:
`../testy/wyniki/english-synonyms/acceptance-final.json`.
Logi testów, zapory i podglądu mowy są w tym samym katalogu.

Publiczne wydanie pozostaje 5.0.120. Nie wykonano push, publikacji, dostawy na
główny komputer ani podmiany jego działającej instalacji.
