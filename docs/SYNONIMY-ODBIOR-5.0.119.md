# EdSharpNG 5.0.119 — odbiór lokalnego słownika i odmiany

## Wynik

Kandydat został zbudowany pełnym kompilatorem .NET Framework i rzeczywiście zainstalowany na Hermesie. Wszystkie 453 pliki instalacji są identyczne ze stagingiem i ZIP-em. Odbiór żywym NVDA przeszedł, włącznie z komunikatem uszkodzonego indeksu i odzyskaniem działania w tym samym procesie.

**Stan publikacji: wydano 5.0.119 i dostarczono pliki, bez aktualizacji głównej instalacji.** Niezależny przegląd zakończono; wskazany przejściowy brak nowych plików w indeksie Git został usunięty przed udaną budową i instalacją. Rozstrzygnięcie: `testy/wyniki/synonyms119/independent-review-resolution.md`.

GitHub Release i `/releases/latest` wskazują [v5.0.119](https://github.com/michalkasperczak/EdSharpNG/releases/tag/v5.0.119), tag prowadzi do `d1f42c10eb7a096b0a3a641cd01390b3ee252d47`. Oba załączniki pobrano z publicznego wydania i porównano pełne SHA-256 z odebraną paczką. Rzeczywiste F11 w testowej instalacji 5.0.118 pokazało dostępne 5.0.119; Escape anulował pytanie bez instalacji. Osobne wywołanie skompilowanego `FetchLatestRelease` zwróciło HTTP 200, poprawny adres instalatora i poprawną pierwszą sumę SHA-256 z opisu.

Na komputerze MICHAL do `D:\Projekty Codex\Hermes\Nowe` dostarczono instalator, ZIP i `EdSharpNG_5.0.119_Nowosci.md`. Hashe obu paczek odczytano z dysku celu; nie zmieniono zainstalowanej binarki 5.0.118 ani procesu użytkownika (ten sam PID i czas startu). Usunięto wyłącznie zastąpione, nieużywane paczki 5.0.118 po pobraniu i sprawdzeniu ich odpowiedników z archiwalnego wydania. Dokumenty użytkownika zachowano; usunięto własną niedokończoną kopię z Temp. Raporty: `testy/wyniki/release5119/public-release-verified.json`, `public-downloads-verified.json`, `installed118-updater-latest119-verified.json`, `main-delivery-verified.json` i `main-cleanup-verified.json`.

## Dane i pokrycie

- Główny indeks: plWordNet 5.0; łącznie ze starszym uzupełnieniem 53750 haseł, w tym 1128 fallbacków OMW.
- Bezpośrednie trafienia na jawnym zestawie 190 słów: OMW 2.0 — 75; nowe dane 5.0 — 148; połączony indeks — 153.
- Rzeczywisty runtime z rozpoznawaniem form: 156/190. Trzy dodatkowe trafienia pochodzą z analizy form; nie nazywamy ich nowymi dosłownymi hasłami.
- Test osobnych form: 259/290. Zapytania dobrano jawnie i deterministycznie z poświadczonych rekordów dla listy kontrolnej; nie są statystyczną próbą polszczyzny. Lista i braki są w `testy/wyniki/synonyms119/coverage-forms.tsv` oraz `coverage-runtime.json`.
- EDSYNF02: 805849 kluczy, z osobno kompresowanymi blokami i dostępem losowym. Porównano wszystkie klucze i wartości z EDSYNF01, nie tylko wybrane przykłady.
- Indeks runtime ma 134750740 bajtów; zasób gzip w repozytorium ma 86586121 bajtów. Instalator i ZIP zawierają gotowy indeks, a nie wymagają dekompresji przez użytkownika.
- Pomiar czasu tej serii: mediana 1 ms, najwolniejsze zapytanie 40 ms, szczytowy working set procesu pomiarowego 95596544 bajty. To jedna seria w środowisku testowym, nie niezależny benchmark zimnego cache ani gwarancja dla innych komputerów.

## Sprawdzenia kodu i danych

- 13 testów Python: parser bajtowego SQL, polski leksykon, UUID, synsety homonimów, dwukrotny odczyt źródła, katalog binarny, kompresja i przygotowanie zasobu; 0 błędów i 0 pominięć.
- Cztery programy testowe C# uruchomione przez `testy/run_synonyms.ps1`: wszystkie zaliczone. Dodatkowo wykonano .NET na rzeczywistym pełnym indeksie EDSYNF02 i zestaw pokrycia.
- Kontrole rzeczywistych przykładów: `domami → chałupami`, `ważnego → istotnego`, `sprawdził → skontrolował`, `mam → posiadam`, `zamkiem → suwakiem`, `szybciej → prędzej`.
- Czerwony test wykazał niedozwolone łączenie starego OMW z odmianą nowego źródła po samym lemie. Po usunięciu obejścia wymagane są zgodny synset i część mowy. Bez potwierdzonego mapowania OMW zachowuje tylko wyszukiwanie bezpośredniego hasła.
- Bezpośredni lemat ma pierwszeństwo. Brak poświadczonej formy celu nie jest uzupełniany zgadywaniem końcówki.
- Wszystkie 19 plików C# faktycznie przekazanych kompilatorowi porównano bajt po bajcie z katalogiem roboczym i stagingiem instalatora. Nie twierdzimy, że ponowny build jest bitowo deterministyczny.

## Żywy NVDA i pełna aplikacja

Zapis mowy zbierano z otwartego Podglądu mowy w trakcie prawdziwych gestów mostka NVDA, nie z opisów kontrolek po fakcie. Surowe zapisy: `testy/wyniki/release5119/nvda-*`; automatyczne asercje odbioru: `nvda-verdict.json`.

- Shift+F7 dla `domami`: „kątami, rzeczownik, 1 z 10”; strzałka: „chatami, rzeczownik, 2 z 10”. Nie ma poprzedzającego „znaczenie 1”.
- Enter wstawił samo `chatami`, bez części mowy ani licznika.
- `ważnego`: odczyt „istotnego, przymiotnik, 1 z 3”; Escape nie zmienił bufora dokumentu.
- `DOMAMI`: Enter wstawił `CHATAMI`.
- `sprawdziłem` i nieistniejące słowo: prawidłowy komunikat braku wyniku, bez zmiany tekstu.
- Własny indeks instalacji QA zabezpieczono i zastąpiono uszkodzonym nagłówkiem. NVDA odczytał „Nie można odczytać indeksu odmiany słownika synonimów”, zamiast zwykłego komunikatu braku synonimów. Przywrócono oryginał i jego SHA-256; kolejne Shift+F7 zadziałało bez restartu procesu.
- Ctrl+S zapisał wyłącznie własny plik pomiarowy; zawartość odczytano z dysku i porównano z buforem.
- Po próbie ponownie sprawdzono wszystkie 453 hashe instalacji. Word nie został uruchomiony. Zakończono wyłącznie własny proces QA i Podgląd mowy otwarty do tej próby; NVDA pozostał uruchomiony.

## Ograniczenia i zakres niewydawany

Nie rozstrzygamy znaczenia homonimu z kontekstu zdania. `mam` może pochodzić od różnych leksemów. Nie dodajemy definicji ani pełnego zbioru wyrażeń wielowyrazowych.

`sprawdziłem` nie występuje w użytej żywej tabeli `tbl_morphology`, choć forma `sprawdził` działa. Pomocniczy Morfeusz2 potrafi ją analizować, lecz nie jest źródłem tego indeksu ani zależnością runtime. Nie nazywamy obsługi odmiany kompletną.

Provider Worda nie został dodany. Potwierdzono oficjalne API Microsoft, ale nie wykonano testu z działającym Wordem ani nie potwierdzono warunków konkretnej licencji Office. Zwykłe Shift+F7 pozostaje lokalne i nie uruchamia Worda.[^word]

## Źródła i licencje

Pełny eksport pozostaje poza repozytorium. Publiczny pakiet zawiera wyłącznie indeksy, pełne warunki obu źródeł oraz notice z cytowaniami. Copyright 4.2 / 2020 w licencji wskazanej dla plWordNet 5.0 zachowano bez samodzielnej poprawki.[^license] Dokumentacja pochodzenia: `SYNONIMY-ROZBUDOWA-2026-11.md` i `Synonyms/NOTICE-plWordNet.txt`.

[^word]: [Oficjalne API Microsoft Word — SynonymInfo](https://learn.microsoft.com/en-us/office/vba/api/word.synonyminfo). Ocena lokalnej integracji: `WORD-SYNONIMY-OCENA-2026-11.md`.

[^license]: [Warunki używania Słowosieci](https://slowosiec.pl/pl/about#license). Dokładny tekst: `Synonyms/LICENSE-plWordNet5.txt`; starsze uzupełnienie: `Synonyms/LICENSE-plWordNet.txt`.
