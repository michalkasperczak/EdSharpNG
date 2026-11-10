# EdSharpNG 5.0.120 — odbiór szczegółów znaczeń

## Zakres

F4 na płaskiej liście Shift+F7 otwiera opcjonalne szczegóły wybranego znaczenia.
Definicja i przykład pochodzą wyłącznie z istniejącego eksportu plWordNet 5.0.
Escape wraca do tej samej pozycji. Enter na liście nadal od razu wstawia wyraz.
Nie dodano Worda, drzewa znaczeń ani obowiązkowego etapu wyboru.

Dane: 21396 z 30360 synsetów ma definicję lub przykład (70,474%). Jest 21278
definicji i 16834 przykłady. To pokrycie grup obecnych w indeksie, nie całego
języka. Brak opisu jest jawny. Placeholder „brak danych” nie jest definicją.

Preferowany jest opis synsetu. Gdy go brakuje, cytujemy polską jednostkę tego
samego synsetu i jawnie podajemy jej nazwę przez etykietę „Jednostka”. Przykład
jednostki wskazuje źródłowy wyraz. Nie łączymy znaczeń przez podobieństwo lematu.
OMW pozostaje odizolowany; nie otrzymuje opisów 5.0. Licencje bez zmian.

## Wykonane sprawdzenia

- TDD C#: nowy test szczegółów miał 7 rzeczywistych porażek przed implementacją,
  po zmianie 12 sprawdzeń zakończyło się `FAILURES=0`.
- Cztery dotychczasowe programy testowe C#: `TESTS=4 FAILED=0`.
  Pierwsze uruchomienie nie miało zasobu w katalogu sondy; po skopiowaniu
  rzeczywistego indeksu test słownika wykonano poprawnie. Nie była to wada produktu.
- Python: `python3 -m unittest discover -s testy -p 'test_*.py' -v`:
  20 testów, OK. Nowe testy placeholderów i fallbacku wykazały RED przed zmianą.
- Rzeczywisty eksport: `details-build.json`; strict UTF-8, trzy kolumny,
  unikalne posortowane identyfikatory, źródłowe SHA-256 i liczby zgodne.
- Pełny build .NET zakończony kodem 0. Wersja 5.0.120.
- Instalator końcowy na Hermesie zakończył się kodem 0; wszystkie 1076 plików
  porównano byte-for-byte z ZIP-em. Odbiór wykonano ponownie po korekcie notice.
  Zainstalowany EXE i indeks mają te same hashe co w próbie żywym NVDA.
- Rzeczywisty program i żywy NVDA: `native-qa.json`, dziewięć punktów.
  `agent`: pierwsza pozycja „tajniak”, ostatnia „ajent”; różne źródłowe definicje,
  Escape zachowuje pozycję, Enter wstawia wyłącznie „ajent”.
  `DOMAMI`: pozycja „kątami”, F4 podaje opis jednostki „chata”; oba Escape
  zachowują odpowiednio pozycję listy oraz oryginalny tekst „DOMAMI”.
- `f4-native-read.json` zawiera rzeczywisty odczyt NVDA „tajny funkcjonariusz…”.
  Nie utożsamiamy samego ValuePattern z wypowiedzią czytnika. Próba szybkiego
  odczytu drugiego opisu nie potwierdziła jego wypowiedzenia; wartości i fokus
  są zapisane oddzielnie. Przypadkową pojedynczą zmianę tempa NVDA cofnięto
  do poprzedniego 80; potwierdzenie w `speech-restored.json`.

## Niezależny przegląd i rozstrzygnięcie

Przegląd tylko do odczytu: `deleg_f19e06ba`, 8/8 małych testów Python, brak
blokerów funkcjonalnych, semantycznych i pakowania. Nie wykonywał GUI ani
pełnego eksportu; te pomiary wykonał rodzic. Sprawdzone hashe w
`review-source-snapshot.json` nadal odpowiadają kodowi i zasobowi.

Jedyny bloker: stare notice wymieniało tylko tabele synsetu. Poprawiono je,
aby jawnie obejmowało `tbl_sense_attributes`, `tbl_sense_examples`, dokładne
powiązanie synsetu, główny polski leksykon i etykietę jednostki. Zmiana notice
nie zmienia kodu ani indeksu ocenionych przez recenzenta.

Końcowy pusty przykład w trzykolumnowym TSV oznacza końcowy tabulator.
Nie usuwamy go dla kosmetycznego `diff --check`, bo zmieniłoby to format.
Sprawdzamy trzy kolumny parserem, a pozostałe pliki zwykłym `git diff --check`.

Standardowy build uzupełnił brakujące narzędzia Convert, stąd większy rozmiar
paczki niż 5.0.119. Wszystkie wspólne pliki Convert pozostały byte-for-byte
bez zmian; dodane pliki pochodzą ze standardowego FetchTools. Nie ogłaszamy
nowych funkcji konwersji ani pełnego regresyjnego odbioru tych narzędzi.

## Publikacja i dostawa

Wykonano pełny łańcuch. Publiczne wydanie ID439815211 i `/releases/latest`
wskazują `v5.0.120`; tag odpowiada źródłom
`7898280d093c5db650d00553a7af0cf5dcded4fb`. Oba publiczne załączniki pobrano
w całości i porównano rozmiary i SHA-256 z `candidate-artifacts.json`.
Pierwsza suma w opisie jest sumą instalatora, zgodnie z parserem aktualizatora.

Rzeczywista 5.0.119 na Hermesie po F11 pokazała „Installed: 5.0.119,
Available: 5.0.120”. Podgląd mowy zawiera odczyt tego pytania przez NVDA
(`f11-final.json`). Escape anulował bez uruchomienia instalatora. Własną
aplikację QA i podgląd mowy zamknięto; NVDA pozostał uruchomiony.

Na MICHAL dostarczono instalator, ZIP i opis nowości do
`D:\Projekty Codex\Hermes\Nowe`. Pełne SHA-256 plików celu zgodne.
Zainstalowane bajty i PID/czas startu EdSharpa są identyczne przed i po
dostawie. Nie wymuszano zamknięcia ani aktualizacji. Własny instalator i opis
5.0.119 usunięto po potwierdzeniu hashy, dostępności publicznego odpowiednika
i nieużywania. Inne dokumenty użytkownika pozostawiono.

Dostawa zakończona, nie jest to instalacja na głównym komputerze. Michał
sam wybierze moment aktualizacji. Pozostałe odłożone funkcje nie są wznowione.
