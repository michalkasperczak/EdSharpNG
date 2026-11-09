# Rozbudowa lokalnego słownika synonimów

## Zakres przyjęty od Michała

Michał odebrał obsługę Shift+F7 i skrócony odczyt listy z wersji 5.0.118. Kolejne prace obejmują większą bazę, formy odmienione i możliwe ulepszenia słownika. Osobno należy sprawdzić oficjalny, opcjonalny dostęp do słownika zainstalowanego Worda; to nie jest zgoda na kopiowanie jego bazy ani zastąpienie trybu offline obowiązkowym Wordem.

Zachowujemy płaską listę `synonim, część mowy, N z M`, Enter wstawiający tylko wyraz, Escape bez zmiany dokumentu oraz zachowanie wielkości liter. Nie zmieniamy instalacji na głównym komputerze.

## Etapy wykonania

1. **Większa baza:** bezpieczny odczyt oficjalnego eksportu SQL plWordNet 5.0 wyłącznie podczas budowy. Wybór polskiego leksykonu z metadanych, synonimia wyłącznie ze wspólnego synsetu, sprawdzenie licencji, raport porównawczy z OMW 2.0. Nie wykonujemy SQL i nie publikujemy całego zrzutu ani tabel audytowych.
2. **Odmiana:** wykorzystanie poświadczonych form i znaczników gramatycznych. Najpierw pomiar faktycznego pokrycia; później indeks do odczytu przez samą aplikację .NET. Rozpoznawanie form oraz odmiana propozycji to oddzielne kryteria. Nie zastępujemy automatycznie wyrazu formą z wymyśloną końcówką. Wieloznaczności bez kontekstu nie przedstawiamy jako jednoznacznego rozpoznania.
3. **Word jako opcja:** sprawdzenie dokumentacji Microsoft i warunków korzystania z lokalnie zainstalowanego produktu. Zwykłe Shift+F7 nie może uruchamiać Worda. Brak Worda nie może wyłączyć podstawowej funkcji. Nie twierdzimy, że jego polski słownik jest bogatszy, dopóki nie ma porównania.
4. **Odbiór:** co najmniej 100 częstych słów, osobno lemy i formy odmienione; regresje parsera, wieloznaczności, braku danych, Enter/Escape i wielkości liter. Zmieniona droga użytkownika wymaga pomiaru żywym NVDA. Zachowujemy treść dokumentu przy braku wyniku lub błędzie.
5. **Dostawa:** działający przyrost, aktualne instrukcje i licencje, jeden zestaw artefaktów dla instalatora, ZIP-u, GitHuba i F11. Publikacja dopiero po odbiorze artefaktu; żadnej samoczynnej instalacji u Michała.

## Zweryfikowany postęp

**Aktualny stan kandydata 5.0.119:** pełny program .NET, instalator i ZIP
zostały zbudowane i uruchomione. Próba instalacji na Hermesie zakończyła się
kodem 0; wszystkie 453 pliki paczki są identyczne. Żywy NVDA potwierdził
odmianę, płaski odczyt z licznikiem, strzałki, Enter, Escape, wielkość liter,
komunikaty braku wyniku i uszkodzenia indeksu oraz działanie po jego
odtworzeniu. Pomiar wykonania: 153/190 bezpośrednich haseł, 156/190 z
rozpoznawaniem form, 259/290 jawnie wybranych poświadczonych form. To zestaw
kontrolny, nie statystyczna próba języka. Szczegóły i ograniczenia:
`SYNONIMY-ODBIOR-5.0.119.md`. Po niezależnym przeglądzie opublikowano
5.0.119: GitHub/latest, oba pobrane załączniki i F11 z instalacji 5.0.118
zweryfikowano. Instalator, ZIP i opis dostarczono do Nowe na komputerze
Michała z kontrolą SHA-256; głównej instalacji ani procesu nie zmieniono.
Poniższe akapity zachowują kolejność prac,
więc ich wcześniejsze zastrzeżenia „na razie” nie opisują końcowego odbioru.

Nie przenosimy odmiany między OMW i 5.0 po samym napisie lematu: nie ma
potwierdzonego mapowania znaczeń. Starszy fallback działa dla bezpośredniego
hasła. Dopasowanie stopnia dodano również dla przysłówków (`szybciej` →
`prędzej`). `sprawdziłem` nie występuje w użytej żywej tabeli morfologii,
więc nie dopisujemy go heurystycznie; `sprawdził` działa.

Pełny plik `wordnet_without_extra_lexicons-2026_02_09.sql.gz` pobrano 09.11.2026 przez natywny klient Windows z publicznego udziału CLARIN.[^dump] Zweryfikowano wszystkie 32 zakresy HTTP (położenie, długość, ETag), scalony rozmiar, SHA-256 i pełną kontrolę CRC gzip. Warunki źródła sprawdzamy osobno przed dystrybucją indeksu.[^license]

- Plik skompresowany: 482280284 bajty.
- Rozmiar po dekompresji: 2293208850 bajtów.
- SHA-256: `fcae73be610944582079dedbd1919878fc626cbed4bfda94bd5e9c0a96ad9ab9`.
- Metadane `tbl_lexicon` potwierdzają leksykon `plWN`, język `pl`, wersję `5.0`.
- Eksport zawiera `tbl_morphology`: powiązanie jednostki znaczeniowej, formę wyrazu i znacznik morfologiczny. Sama obecność tabeli nie dowodzi jeszcze pokrycia użytecznych form.
- UUID-y są zapisane w literałach `_binary`; nie wolno dekodować całego SQL jako UTF-8. Tekstowe pola należy dekodować osobno i ściśle, a identyfikatory traktować jako bajty.

Źródło zachowano trwale poza publicznym repozytorium, w katalogu roboczym danych `~/projekty/edsharp-data/plwordnet-5/`, wraz z manifestem zakresów i schematem. Nie zmieniono jeszcze indeksu dostarczanego użytkownikowi; wydanie 5.0.118 nadal zawiera OMW/plWordNet 2.0.

Bezpieczny generator `tools/build_synonyms_plwordnet.py` odczytał eksport bez uruchamiania SQL. Testy parsera bajtowego i izolacji polskiego leksykonu przeszły po wykazaniu braku implementacji. Wygenerowany indeks ma 52622 hasła z synonimami, 72992 rekordy i 4713496 bajtów. Jest na razie artefaktem roboczym obok źródła, nie podmienioną bazą wydania.

W jawnym zestawie 190 codziennych słów liczba trafień wzrosła z 75 (OMW 2.0) do 148 (plWordNet 5.0). To zestaw kontrolny, a nie statystycznie reprezentatywna próba. Wykryto także pięć utraconych trafień: `radość`, `strach`, `nadzieja`, `prosty`, `głośny`. Przed decyzją o podmianie trzeba wyjaśnić różnice danych; nie wolno uznać samej większej liczby haseł za brak regresji. Wynik i listy braków są w `testy/wyniki/synonyms119/coverage-lemmas.json`.

Przed podmianą przeanalizowano te pięć braków w źródle: hasła występują w 5.0, lecz ich odpowiednie synsety nie mają innych jednowyrazowych członków (dla rzeczownikowego `prosty` jest wyrażenie `cios prosty`). Nie dodano dowolnych relacji semantycznych jako rzekomych synonimów.

Dodano kontrolowany wariant łączony: główne dane 5.0 mają pierwszeństwo, a starsze OMW 2.0 uzupełnia wyłącznie hasła całkowicie pozbawione synonimów w nowym indeksie. Każdy taki rekord ma jawny identyfikator `omw-2.0:...`; nie przedstawiamy go jako danych 5.0. Wariant roboczy ma 53750 haseł, w tym 1128 ze starszego źródła. Pokrycie zestawu wynosi 153 z 190, bez utraty wcześniejszych trafień. Test pierwszeństwa źródeł przeszedł po uprzednim czerwonym wyniku.

Powstał odczyt binarnego indeksu odmiany z wyszukiwaniem na dysku, bez ładowania wszystkich form do pamięci. Test C# na fixture potwierdza `domami` → `chałupami`, odrzucenie propozycji bez poświadczonej formy i zachowanie pierwszeństwa bezpośredniego hasła. Dla rzeczowników dopasowujemy liczbę i przypadek (różny rodzaj synonimu jest dozwolony), dla przymiotników także rodzaj i stopień, dla czasowników poświadczony znacznik z aspektem. To na razie testy małych danych, **nie odbiór gotowej odmiany na pełnej bazie ani w GUI**. Trwa osobna budowa indeksu rzeczywistych form.

Pliki pomocnicze dodano jednocześnie do kompilacji i instalatora. Runner `testy/run_synonyms.ps1` wykonał cztery programy testowe, obejmując nową większą bazę oraz regresje dotychczasowego parsera i formatowania; wszystkie przeszły. Wydanie ani główna instalacja nie są zmienione.

## Źródła, licencja i Word

Badanie Morfeusza2/SGJP (10 zaliczonych asercji na rzeczywistym narzędziu) i jego źródła są w `MORFOLOGIA-ZRODLA-OCENA-2026-11.md`; wynik probe zapisano w `testy/wyniki/synonyms119/probe-morfeusz2.json`. To narzędzie pomocnicze build-time, nie nowa zależność runtime. Dla `mam` potwierdzono niejednoznaczność `mama`, `mieć`, `mamić`; nie wolno przedstawiać wyniku jako ustalonego znaczenia w zdaniu. `sprawdziłem` wymaga uwagi na złożone tagi form czasu przeszłego.

Ponownie odczytano odpowiedź zespołu Słowosieci z 29.10.2026 (wiadomość 92566): autorzy wskazali pobrany eksport i stronę `about#license` jako aktualną licencję dla zamierzonego lokalnego indeksu. Pełny tekst sekcji licencyjnej pobrano natywnym klientem Windows 09.11.2026. Zachowano go w `Synonyms/LICENSE-plWordNet5.txt`, **bez poprawiania oryginalnego copyrightu 4.2 / 2020 na rzekomy tekst 5.0**. Starsza licencja OMW zostaje osobno. Notice wydania musi podać oba źródła oraz cytowania wymagane przez autorów; nie publikujemy treści korespondencji ani SQL.

Raport `WORD-SYNONIMY-OCENA-2026-11.md` potwierdza oficjalne API Microsoft i język polski 1045. Brak pomiaru na działającym Wordzie i brak potwierdzenia warunków konkretnej licencji Office nie pozwalają ogłosić gotowego providera. Nie podłączono go do aplikacji. Projekt ewentualnej opcji: osobna, jawna komenda, wyłącznie już otwarty Word, wyniki w pamięci jednego zapytania, bez kopiowania bazy i bez uruchamiania Worda przez Shift+F7. Nie ustalono, że słownik Worda jest bogatszy.

## Źródła

[^dump]: [Publiczny eksport CLARIN](https://nc.clarin.e-science.pl/index.php/s/cEbfsZZX5r4sKNW).

[^license]: [Warunki używania Słowosieci](https://slowosiec.pl/pl/about#license).
