# Pytania do rozstrzygnięcia, EdSharpNG

Stan na 3 września 2026, wersja poprawiona. Siedem spraw czeka na Twoją decyzję.
Każda ma warianty z literami i moją rekomendację, więc odpowiedź może być krótka,
choćby samymi literami przy numerach albo jednym „wszystkie jak proponujesz".

Ten plik zastępuje ten, który dostałeś nad ranem. Tam było dziesięć pytań, ale
trzy z nich już rozstrzygnąłeś wcześniej i nie miałem prawa pytać drugi raz.
Zniknęły stąd: zamiana wielkości liter (Twoje „to chyba byśmy zostawili?"),
nazwa nowej zakładki (Twoje „ze słowa na którym jest kursor") oraz zakładki
kontra ulubione (Twoje „Rozdzielić"). Dwie ostatnie są już wdrożone w 5.0.64.

Kolejność jest od najprostszych do rozstrzygnięcia. Numery są z rejestru ustaleń
w repozytorium, żeby dało się do nich wracać po nazwie.

---

## 1. Szerokość łamania wierszy (edsharpng-89)

Control+Shift+H otwiera pytanie o szerokość, po której program twardo łamie
wiersze. Sam zapytałeś „chcemy to?" i nie wróciliśmy do tego.

- **A. Zostaje na skrócie.** Rekomendacja: działa na tekście, niczego nie psuje,
  a przy pisaniu pod wąską szerokość jest jedyną drogą.
- B. Schodzi do menu bez skrótu i zwalnia literę H.

---

## 2. Przełącznik dodatkowych komunikatów mowy (edsharpng-83)

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

## 3. Dwie komendy od wyrażeń regularnych (edsharpng-82)

Extract with Regular Expression i Yield with Regular Expression robią rzeczy
podobne. Twoje słowa: „nie wiem czy to chcemy czy jakoś chcemy to połączyć".

- **A. Zostają osobno,** jak dziś: Yield na Control+Shift+Y, Extract w menu bez
  skrótu. Rekomendacja jako mniejsza zmiana.
- B. Łączę w JEDNĄ komendę, a w okienku wybierasz, co ma zrobić.

---

## 4. Otwieranie pliku .rtf (edsharpng-98)

Dziś Control+O na pliku .rtf pyta „Treat as rich text?" i idziesz albo w tekst
bogaty, albo w surowe znaczniki.

- **A. Zostaje jak dziś.** Rekomendacja: pytanie jest krótkie i znane, a
  konwersja do Markdown jest już dostępna przy otwieraniu pliku spoza edytora.
- B. Control+O od razu proponuje konwersję do Markdown razem z pozostałymi
  wariantami, czyli jedna lista zamiast pytania tak-nie.

---

## 5. Wiersz na liście odsyłaczy (edsharpng-50)

Lista pod Control+F6 pokazuje dziś: treść odsyłacza, adres, rodzaj.

- **A. Zostaje jak jest.** Rekomendacja tylko dlatego, że nie wiem, jak długi
  wiersz czyta się u Ciebie wygodnie, a to Ty tego słuchasz.
- B. W wierszu sam tytuł, a adres dopowiada strzałka w lewo (tak jak na innych
  listach dopowiadamy to, czego w wierszu nie ma).

---

## 6. Lista odsyłaczy w plikach nie-Markdown (edsharpng-49)

Lista pod Control+F6 działa też w plikach .txt, bo goły adres w zwykłym pliku
jest prawdziwym odsyłaczem. To świadoma różnica wobec Control+K, który PISZE
składnię Markdown i ma bramkę tylko na .md.

- **A. Zostaje bez bramki,** czyli działa wszędzie. Rekomendacja: czytanie
  odsyłaczy niczego nie psuje, a bramka odebrałaby funkcję w plikach .txt.
- B. Zawężam do plików .md, dla spójności z Control+K.

---

## 7. Aktualizacja z poziomu programu (edsharpng-93)

Czy program ma sam sprawdzać nową wersję i ją pobierać. Sam napisałeś „może to już
czas, ale pewnie nie, to na koniec zrobimy, bo będziemy dużo kombinować".

- **A. Na koniec prac.** Rekomendacja, bo to Twoje własne zdanie, a przy tak
  częstych wersjach testowych mechanizm aktualizacji trzeba by nadążać z każdą
  paczką i nic to nie oszczędza.
- B. Robimy teraz.
