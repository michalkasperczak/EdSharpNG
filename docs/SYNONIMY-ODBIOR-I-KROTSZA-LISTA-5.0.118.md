# Synonimy — odbiór i krótsza lista w EdSharpNG 5.0.118

## Odbiór pierwszej wersji

Michał potwierdził działanie pierwszej wersji w odesłanej instrukcji oznaczonej 5.0.116: Word i przeglądarka nie otwierają się; Enter, Escape, strzałki i fokus działają. Dokument nie stanowi osobnego pomiaru wersji uruchomionej binarki.

Zażądał wyrazu na początku pozycji, następnie części mowy i numeru pozycji na całej liście. Zwrot „znaczenie 1” wydłuża odczyt i ma zniknąć.

## Zmiana w tym etapie

Wyniki pozostają prostą płaską listą, bez zagnieżdżeń. Nowy format:

- `chałupa, rzeczownik, 1 z 5`;
- `chata, rzeczownik, 2 z 5`;
- `lokum, rzeczownik, 5 z 5`.

Licznik dotyczy wszystkich wyników w oknie, również gdy pochodzą z kilku grup słownikowych. Nie oznacza numeru znaczenia. Enter wstawia tylko synonim, bez części mowy i licznika. Shift+F7, Escape i zachowanie wielkich liter pozostają bez zmian.

## Weryfikacja na Hermesie

Test formatu najpierw zakończył się błędem, ponieważ brakowało implementacji nowego formatu. Po zmianie wszystkie przypadki formatu, wcześniejszy test loadera i test rzeczywistego indeksu zakończyły się `FAILURES=0`.

Żywy NVDA, aplikacja uruchomiona z `C:\EdSharpSynonymsQA118\EdSharpNG.exe`:

- Shift+F7 na `dom`: fokus na `chałupa, rzeczownik, 1 z 5`;
- strzałka w dół: `chata, rzeczownik, 2 z 5`;
- Enter: dokument zawiera `chata`, fokus wraca do edytora;
- po cofnięciu zamiany ponowne Shift+F7 i End: `lokum, rzeczownik, 5 z 5`;
- Escape: fokus w edytorze, dokument nadal zawiera `dom`.

Rzeczywisty podgląd mowy zawierał kolejno:

```text
Thesaurus
Synonimy dla: dom
lista
chałupa, rzeczownik, 1 z 5
chata, rzeczownik, 2 z 5
```

Bez dopisanego licznika NVDA w bieżącej konfiguracji czytał sam wyraz i część mowy, bez pozycji. Dlatego licznik jest częścią etykiety. W zmierzonej konfiguracji został wypowiedziany raz. Przy innych ustawieniach czytnika należy sprawdzić, czy nie dochodzi drugi, natywny odczyt pozycji.

## Kolejny etap — propozycja, nie wdrożona funkcja

Samo „znaczenie 1” nie wyjaśnia różnicy semantycznej. Po uzyskaniu pełniejszego źródła proponujemy krótkie opisy znaczeń albo dodatkowe szczegóły dostępne na żądanie. Opisy muszą pochodzić z danych słownika, nie z domysłów programu. Nie wprowadzamy teraz drzewa ani kolejnego obowiązkowego kroku przed wyborem synonimu.

Ten etap nie zmienia źródła danych ani licencji. Nadal używany jest indeks OMW/plWordNet, bez definicji i pełnej obsługi odmiany. Pełny plWordNet 5.0 pozostaje osobnym, niezakończonym zadaniem. Nie jest potrzebny Word, Python ani połączenie z Internetem podczas korzystania ze słownika.

## Krótki test dla Michała

1. Wpisz `dom` i naciśnij Shift+F7.
2. Sprawdź, czy najpierw słyszysz wyraz, potem część mowy i pozycję.
3. Przejdź strzałką do następnego wyniku i sprawdź zmianę licznika.
4. Enter ma wstawić tylko wybrany wyraz.
5. Ponów próbę i zamknij listę przez Escape — tekst ma pozostać bez zmian.
