# Angielski słownik synonimów EdSharpNG — rozpoznanie i zakres

## Wniosek

Możemy korzystać z angielskiego Princeton WordNet 3.0, czyli tych samych danych,
które oryginalny EdSharp czyta przez NLTK. Nie potrzebujemy przejmować jego
środowiska Python ani pobierania danych podczas pracy. Licencja dopuszcza
kopiowanie, modyfikację i redystrybucję bez opłat z zachowaniem pełnego tekstu,
copyrightu i zastrzeżeń. Nie przedstawiamy WordNet 3.0 (2006) jako najnowszego
wydania językowej bazy.[^1] [^2]

## Co rzeczywiście jest w oryginale

Sprawdzony commit oryginalnego EdSharpa:
`ea0a4b8c25c61b9dc5c9ff5c9cd89c75b024caee`.
`configs/convert/wordNet.py` wywołuje `wordnet.synsets(sWord)` i `lemmas()` bez
innego języka. W razie braku biblioteki lub danych próbuje automatycznie
pobrać NLTK `wordnet` i `omw-1.4`. To nie jest samowystarczalny lokalny plik
tezaurusa w samym EXE.[^1]

W drzewie są także `data/dictionaries/en_US.aff` i `en_US.dic`: to inny typ
danych, do pisowni, nie zamiennik tezaurusa. Pełne drzewo API nie było ucięte;
zapis identyfikatora i ścieżek: `../testy/wyniki/english-synonyms/upstream-source.json`.

## Przygotowany kierunek obsługi

Język słownika nie wynika z języka interfejsu ani ustawienia sprawdzania pisowni.
Wybór: Polski, English lub oba. Domyślny polski pozostaje bez zmian.
Przy obu wyniki zachowują osobne znaczenia, wskazują język i nie są tłumaczeniami.
Wspólny napis, np. `list`, nie jest podstawą łączenia polskiego i angielskiego
synsetu. Enter wstawia tylko wyraz/wyrażenie. F4 nadal tylko objaśnia wybór.

Definicje i przykłady angielskie pozostają angielskie i źródłowe. Wielowyrazowe
synonimy mogą być wynikami; wejście pozostaje jednym wyrazem. Angielskie
nieregularne formy poświadczone w plikach `.exc` kierują do odpowiedniej części
mowy i formy podstawowej. Nie obiecujemy na tej podstawie pełnej angielskiej
fleksji ani automatycznego odmieniania wstawianego synonimu.

## Co sprawdzono lokalnie

Pobrane pełne archiwum NLTK WordNet 3.0: 10775600 bajtów. CRC ZIP poprawne.
SHA-256: `cbda5ea6eef7f36a97a43d4a75f85e07fccbb4f23657d27b4ccbc93e2646ab59`.
Pełny plik `wordnet/LICENSE` odczytany i porównany zakresem z warunkami autora.[^2] [^3]

Nowy moduł C# `SlownikiSynonimow.cs` i test `testy/test_slowniki_synonimow.cs`
wykonane na rzeczywistym .NET Framework Windows, w pionowych cyklach RED/GREEN.
Zielone sprawdzenia obejmują PL/EN/oba, `bank` (finanse/brzeg), `list` w dwóch
językach, izolację opisów, brak/uszkodzenie opcjonalnych szczegółów, błędne
rekordy bazowe, nieregularne `went → go` tylko jako czasownik, brak zgadywania
`goed` oraz zachowanie podstawy przy błędnym indeksie wyjątków. Surowy przebieg:
`../testy/wyniki/english-synonyms/runtime-tdd.log`.

Integracja wyboru z GUI i pełny prywatny build 5.0.121 są wykonane. W Misc →
Thesaurus Dictionary oraz w palecie (Ctrl+Shift+F1) dostępny jest zapisany wybór
Polski / English / oba. Żywy NVDA potwierdził dla `list` 4 polskie, 9 angielskich
oraz 13 rozdzielonych pozycji trybu obu; odczytał oznaczenia języka, źródłową
angielską definicję i przykład, powrót po F4 do tej samej pozycji oraz komunikat
wstawienia wybranej frazy. Escape zachował tekst dokumentu. Dowody:
`../testy/wyniki/english-synonyms/ui-integration.json` i transkrypcja podglądu mowy.

Końcowe zasoby: 60 874 różne lematy, 65 636 par lemma/część mowy, 91 128
wierszy indeksu, 45 043 synsety/definicje, 13 332 przykłady, 3 485 wyjątków.
Licencja jest bajtowo zgodna z archiwum źródłowym. Generator usuwa techniczne
markery pozycji przymiotników; `ablaze` jest wyszukiwane bez `(p)`. Parser
oddziela przykłady od definicji, pomija puste cytaty, zachowuje atrybucję źródła
i definicje znajdujące się po przykładzie. Pozostałe przykłady nie trafiają pod
etykietę definicji. Manifest: `../testy/wyniki/english-synonyms/build.json`.

Jawny zestaw kontrolny 100 angielskich wyrazów na końcowych zasobach dał 97
trafień, medianę 150 ms i maksimum 162 ms; nie jest to losowa próba ani miara
pokrycia całego języka. Log: `../testy/wyniki/english-synonyms/runtime-real-final-resources.log`.
Wykonano pięć programów regresji C# polskiego słownika (bez błędów) oraz pełne
33 testy Python. Niezależny review rozstrzygnięto, zależności spakowano, a brak
pliku wyjątków ma jawne ostrzeżenie potwierdzone przez NVDA. Instalator zakończył
próbę na Hermesie kodem 0; wszystkie 1082 pliki ZIP-a są zgodne bajtowo z instalacją.
Gotowy provider przeszedł również pomiar z zaporą blokującą ruch wychodzący
własnego procesu testowego. Końcowy odbiór, ograniczenia, pomiar czasu PL/EN/oba
i hashe: `SYNONIMY-ODBIOR-5.0.121.md`. Główna instalacja pozostaje bez zmian;
prywatna funkcja jest przygotowana, lecz nie została publicznie wydana.

## Źródła

[^1]: [Oryginalny wordNet.py, sprawdzony commit](https://github.com/jamalmazrui/EdSharp/blob/ea0a4b8c25c61b9dc5c9ff5c9cd89c75b024caee/configs/convert/wordNet.py).
[^2]: [Princeton: License and Commercial Use of WordNet](https://wordnet.princeton.edu/license-and-commercial-use).
[^3]: [Metadane pakietu NLTK WordNet 3.0](https://raw.githubusercontent.com/nltk/nltk_data/gh-pages/packages/corpora/wordnet.xml); [pobrane archiwum](https://raw.githubusercontent.com/nltk/nltk_data/gh-pages/packages/corpora/wordnet.zip).
