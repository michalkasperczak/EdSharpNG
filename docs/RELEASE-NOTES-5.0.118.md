# EdSharpNG 5.0.118 — lokalny słownik synonimów

## Synonimy bez Worda

Shift+F7 wyszukuje teraz polskie synonimy lokalnie. Nie uruchamia Microsoft Worda ani przeglądarki i nie wymaga Internetu podczas korzystania ze słownika.

Wyniki są prostą listą obsługiwaną strzałkami. Enter wstawia wybrany wyraz, Escape zamyka listę bez zmiany tekstu. Zachowywana jest wielka pierwsza litera oraz zapis wersalikami. Zaznaczenia wielowyrazowe są odrzucane bez naruszania dokumentu.

## Krótszy odczyt listy

Najpierw podawany jest synonim, potem część mowy i pozycja, np. „chałupa, rzeczownik, 1 z 5”. Numer dotyczy całej listy wyników, nie numeru znaczenia. Usunięto zwrot „znaczenie 1”.

## Jedno publiczne wydanie

Poprzednie publiczne wydanie 5.0.115 nadal korzystało ze słownika Worda. Wersje 5.0.116 i 5.0.117 były dostawami testowymi, nie najnowszym wydaniem na GitHubie. Wersja 5.0.118 udostępnia lokalny słownik również przez GitHub i aktualizację F11.

Jeżeli po aktualizacji nadal otwiera się Word, sprawdź numer wersji w oknie informacji o programie. Powinna to być wersja 5.0.118, nie 5.0.115.

## Co sprawdzono

Testy parsera, indeksu i formatu listy zakończyły się bez błędów. Instalator zainstalowano próbnie na komputerze Hermes; zainstalowane pliki porównano z paczką. Na tej instalacji żywy NVDA potwierdził pierwszą i drugą pozycję, Enter, Escape oraz powrót fokusu do edytora. Shift+F7 nie uruchomił procesu Worda. Wcześniejszy zapis podglądu mowy pochodzi z tej samej binarki o identycznym SHA-256.

Na głównym komputerze nie wymuszano zamknięcia edytora ani instalacji. Aktualizację można wykonać samodzielnie przez F11 albo uruchomić instalator z folderu Nowe.

## Ograniczenia i licencja

To przejściowy indeks OMW/plWordNet, nie pełny plWordNet 5.0. Wyszukiwanie dotyczy głównie form podstawowych; nie ma definicji ani pełnej obsługi odmiany i wyrażeń wielowyrazowych. Pełniejsza baza pozostaje osobnym etapem.

Licencja danych, informacje o prawach autorskich, zastrzeżenie braku gwarancji i wymagane cytowanie są w dołączonym katalogu Synonyms. Nie są zmieniane warunki licencji programu.

## Pliki wydania

**EdSharpNG_Setup_5.0.118.exe** — zalecany instalator.

Rozmiar: 3777021 bajtów.

SHA-256: A169520818D9A64A8BF7EF537C1E4EE8DB05ADA17AC8534A55F21D7BC7A308EE

**EdSharpNG_5.0.118.zip** — zapasowa paczka plików programu, nie zawiera konwerterów niewchodzących do instalatora.

Rozmiar: 2425401 bajtów.

SHA-256: 3CCCB12704CD287637EB662CA141E18E647FDBC60AFB390D96C186EEC5101569
