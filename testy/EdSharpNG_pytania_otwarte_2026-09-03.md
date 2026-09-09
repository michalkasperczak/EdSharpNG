# Pytania do rozstrzygnięcia, EdSharpNG

Stan na 3 września 2026. Dziesięć spraw czeka na Twoją decyzję. Każda ma warianty
z literami i moją rekomendację, więc odpowiedź może być krótka, choćby samymi
literami przy numerach albo jednym „wszystkie jak proponujesz".

Kolejność jest od najprostszych do rozstrzygnięcia. Numery są z rejestru ustaleń
w repozytorium, żeby dało się do nich wracać po nazwie.

---

## 1. Zamiana wielkości liter zostaje? (edsharpng-68)

Control+U robi WIELKIE LITERY, Control+Shift+U małe, Alt+U Pierwszą Wielką,
Alt+Shift+U odwraca. Wypłynęło przy porządkowaniu skrótów, gdzie zbierałem
kandydatów do zdjęcia.

- **A. Wszystkie cztery zostają.** Rekomendacja: to praca na tekście, a nie na
  wyglądzie strony, i w każdym edytorze się przydaje.
- B. Zostaje para Control+U i Control+Shift+U, a Alt+U i Alt+Shift+U schodzą do
  menu i zwalniają dwa skróty.
- C. Cała rodzina do menu.

---

## 2. Szerokość łamania wierszy (edsharpng-89)

Control+Shift+H otwiera pytanie o szerokość, po której program twardo łamie
wiersze. Sam zapytałeś „chcemy to?" i nie wróciliśmy do tego.

- **A. Zostaje na skrócie.** Rekomendacja: działa na tekście, niczego nie psuje,
  a przy pisaniu pod wąską szerokość jest jedyną drogą.
- B. Schodzi do menu bez skrótu i zwalnia literę H.

---

## 3. Przełącznik dodatkowych komunikatów mowy (edsharpng-83)

Extra Speech Toggle. Napisałeś przy nim „do usunięcia", ale skrót już zdjąłem, a
samej komendy nie ruszyłem i chcę to potwierdzić, zanim zniknie.

Powód, dla którego się wstrzymałem: to ustawienie zapisuje się do pliku, więc
jeśli komenda zniknie w stanie „wyłączone", dodatkowe komunikaty mowy przestaną
działać NA STAŁE i nie będzie czym ich wrócić.

- **A. Komenda zostaje w menu, bez skrótu.** Rekomendacja: skrót już zwolniony,
  czyli to, o co Ci chodziło, a droga powrotu zostaje.
- B. Usuwamy całkiem, razem z ustawieniem w pliku (wtedy dopisuję kod, który
  czyści ten wpis, żeby nie został zapisany stan bez włącznika).

---

## 4. Dwie komendy od wyrażeń regularnych (edsharpng-82)

Extract with Regular Expression i Yield with Regular Expression robią rzeczy
podobne. Twoje słowa: „nie wiem czy to chcemy czy jakoś chcemy to połączyć".

- **A. Zostają osobno,** jak dziś: Yield na Control+Shift+Y, Extract w menu bez
  skrótu. Rekomendacja jako mniejsza zmiana.
- B. Łączę w JEDNĄ komendę, a w okienku wybierasz, co ma zrobić.

---

## 5. Nazwa nowej zakładki (edsharpng-63)

Control+Shift+B zakłada zakładkę z nazwą. Dziś w polu nazwy leży propozycja: treść
wiersza, w którym stoisz.

- **A. Propozycja z wiersza zostaje.** Rekomendacja: mniej pisania, a jeśli nie
  pasuje, kasujesz i wpisujesz swoje.
- B. Puste pole, nazwę zawsze wpisujesz sam.

---

## 6. Otwieranie pliku .rtf (edsharpng-98)

Dziś Control+O na pliku .rtf pyta „Treat as rich text?" i idziesz albo w tekst
bogaty, albo w surowe znaczniki.

- **A. Zostaje jak dziś.** Rekomendacja: pytanie jest krótkie i znane, a
  konwersja do Markdown jest już dostępna przy otwieraniu pliku spoza edytora.
- B. Control+O od razu proponuje konwersję do Markdown razem z pozostałymi
  wariantami, czyli jedna lista zamiast pytania tak-nie.

---

## 7. Wiersz na liście odsyłaczy (edsharpng-50)

Lista pod Control+F6 pokazuje dziś: treść odsyłacza, adres, rodzaj.

- **A. Zostaje jak jest.** Rekomendacja tylko dlatego, że nie wiem, jak długi
  wiersz czyta się u Ciebie wygodnie, a to Ty tego słuchasz.
- B. W wierszu sam tytuł, a adres dopowiada strzałka w lewo (tak jak na innych
  listach dopowiadamy to, czego w wierszu nie ma).

---

## 8. Lista odsyłaczy w plikach nie-Markdown (edsharpng-49)

Lista pod Control+F6 działa też w plikach .txt, bo goły adres w zwykłym pliku
jest prawdziwym odsyłaczem. To świadoma różnica wobec Control+K, który PISZE
składnię Markdown i ma bramkę tylko na .md.

- **A. Zostaje bez bramki,** czyli działa wszędzie. Rekomendacja: czytanie
  odsyłaczy niczego nie psuje, a bramka odebrałaby funkcję w plikach .txt.
- B. Zawężam do plików .md, dla spójności z Control+K.

---

## 9. Zakładki i ulubione dzielą jedną sekcję pliku ustawień (edsharpng-60)

To jest sprawa techniczna z widocznym skutkiem. Zakładki zwykłe i lista ulubionych
plików trzymają się w JEDNEJ sekcji Twojego pliku ustawień. Skutek: samo
postawienie zakładki czyni plik ulubionym, a zdjęcie pliku z ulubionych CZYŚCI
jego zakładki.

- A. Rozdzielam obie listy na osobne sekcje. Zaleta: zachowanie przestaje
  zaskakiwać. Koszt: zmiana formatu pliku ustawień, który masz na dysku, więc
  musiałbym napisać migrację i uważać, żeby nie zgubić tego, co już tam jest.
- **B. Zostaje jak jest, ale program MÓWI,** co się właśnie stało („removing from
  favorites also clears its bookmarks"). Rekomendacja: to zachowanie odziedziczone
  po autorze i niekoniecznie złe, a niewidomy potrzebuje przede wszystkim
  wiedzieć, że coś zniknęło.

Uwaga na marginesie: zakładki Z NAZWĄ mają własną sekcję i ich to NIE dotyczy.

---

## 10. Aktualizacja z poziomu programu (edsharpng-93)

Czy program ma sam sprawdzać nową wersję i ją pobierać. Sam napisałeś „może to już
czas, ale pewnie nie, to na koniec zrobimy, bo będziemy dużo kombinować".

- **A. Na koniec prac.** Rekomendacja, bo to Twoje własne zdanie, a przy tak
  częstych wersjach testowych mechanizm aktualizacji trzeba by nadążać z każdą
  paczką i nic to nie oszczędza.
- B. Robimy teraz.
