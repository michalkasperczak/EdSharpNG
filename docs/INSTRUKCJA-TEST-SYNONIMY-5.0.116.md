# Test lokalnego słownika synonimów — EdSharpNG 5.0.116 beta

## Cel

Ten test sprawdza nowe działanie `Shift+F7`. Słownik ma pracować lokalnie, bez
uruchamiania Microsoft Worda i bez Internetu.

## Przed rozpoczęciem

1. Uruchom EdSharpNG 5.0.116 beta z otrzymanego instalatora.
2. Otwórz nowy, pusty dokument.
3. Nie musisz wyłączać NVDA ani zmieniać jego ustawień.
4. Microsoft Word może pozostać zainstalowany, ale nie powinien się otworzyć.

## Test 1 — znalezienie i lista wyników

1. Wpisz `dom`.
2. Ustaw kursor w słowie i naciśnij `Shift+F7`.
3. Oczekiwany rezultat: otwiera się okno zatytułowane „Synonimy dla: dom”.
4. Pierwsza pozycja listy powinna zawierać część mowy, numer znaczenia i wyraz,
   na przykład „Rzeczownik, znaczenie 1: chałupa”.
5. Strzałką w dół przejdź do następnej pozycji. NVDA ma odczytywać całą pozycję.
6. Naciśnij Escape.
7. Oczekiwany rezultat: wracasz do dokumentu, a słowo `dom` nie zmienia się.

## Test 2 — zastąpienie słowa

1. Nadal stojąc na `dom`, naciśnij `Shift+F7`.
2. Wybierz strzałką dowolny synonim.
3. Naciśnij Enter.
4. Oczekiwany rezultat: okno znika, a tylko słowo `dom` zostaje zastąpione
   wybranym synonimem. Fokus wraca do pola tekstowego.

## Test 3 — brak wyniku

1. W nowym dokumencie wpisz `xyz`.
2. Naciśnij `Shift+F7`.
3. Oczekiwany rezultat: nie otwiera się pusta lista. Program komunikuje
   „Nie znaleziono synonimów dla: xyz”.

## Test 4 — wiele słów

1. Zaznacz tekst `bardzo ważny`.
2. Naciśnij `Shift+F7`.
3. Oczekiwany rezultat: program komunikuje „Zaznacz jedno słowo.” i nie zmienia
   zaznaczenia ani dokumentu.

## Co zgłosić

Po każdym teście podaj krótko:

- PASS albo FAIL;
- dokładny komunikat NVDA, jeśli różni się od oczekiwanego;
- czy Word lub przeglądarka samoczynnie się otworzyły;
- czy Enter, Escape, strzałki i fokus działały poprawnie;
- słowo, dla którego wynik był błędny lub którego brakowało.

## Znane ograniczenie wersji beta

Obecny indeks korzysta z kompaktowego eksportu OMW/plWordNet. Szuka głównie
form podstawowych, więc może nie znaleźć form odmienionych, takich jak
`ważnego` lub `sprawdziłem`. Pełniejszy indeks z plWordNet 5.0 jest w osobnym
procesie przygotowania i będzie wymagał ponownego pomiaru.
