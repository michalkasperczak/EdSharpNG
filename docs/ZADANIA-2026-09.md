# EdSharpNG - zadania zgloszone 11.09.2026 (po wersji 5.0.78)

Zrodlo: Michal Kasperczak, wiadomosc z 11.09.2026 wieczorem.
Kolejnosc zapisu NIE jest priorytetem - priorytety do ustalenia.

## Bieżące zlecenie MK — 18.09.2026: formatowanie i zapis

Źródło uwag: `docs/UWAGI-MK-18.09.2026.md`, zwłaszcza końcowy opis
kopiowania całego zaznaczenia i zapisu pliku po imporcie z Worda.

Zakres uzgodniony: EdSharp, bez dalszych prac nad AMC w tym wątku.

Koordynacja: to samo zlecenie wpłynęło w dwóch tematach Telegrama. Aktywne
wdrożenie prowadzi sesja `20260918_002328_08637004` (temat `33010`).
Sesja z tematu `32113` zatrzymała swoje zdublowane zadania po potwierdzeniu
równoległych zmian w kodzie. Nie cofać pracy drugiej sesji. Nie budować dwóch
różnych instalatorów pod numerem 5.0.114. Nowa kopia robocza
`edsharp-rich-copy` powstała w zdublowanym zadaniu; nie scalać jej automatycznie.

- W trakcie: Control+Shift+C dla całego mieszanego zaznaczenia Markdown.
  Nagłówki, odsyłacze, listy i wyróżnienia mają zachować znaczenie w bogatym
  schowku; format do ponownego wklejenia Markdown w EdSharp pozostaje.
- W trakcie: Control+Shift+S z rzeczywistą konwersją do DOCX, EPUB, PDF,
  HTML, RTF oraz zapisem Markdown i tekstu. Błąd konwersji nie może uszkodzić
  poprzedniego pliku ani oznaczyć niezapisanych zmian jako zapisanych.
- Do weryfikacji przed wydaniem: pomiary wyników schowka i plików,
  obsługa nowego okna zapisu żywym NVDA, próba instalacji.
- Do dostarczenia: nowy numer wersji, wydanie GitHub z instalatorem i ZIP,
  kopia instalatora w folderze Michała oraz krótka instrukcja testu.
- Poprawki checklist i drobnych komend są już zapisane lokalnie w commicie
  `57a9919`; przed wydaniem wymagają uwzględnienia w kontroli regresji.

### Zatwierdzona opcja: Control+S zapisuje do formatu źródłowego

MK zatwierdził widoczny przełącznik w normalnym oknie ustawień: opcjonalny zapis
zaimportowanego DOCX, EPUB i innych wspieranych dokumentów z powrotem w formacie
otwartego oryginału. Opcja domyślnie wyłączona; bez niej działa dotychczasowy zapis MD.
Użytkownik świadomie akceptuje utratę czcionek i innych cech wyglądu, bo celem
jest redakcja treści i struktury do prostej publikacji, np. WordPressa.

Wdrożone w kandydacie, jeszcze bez wydania: powiązanie importu z oryginalnym
plikiem, Control+S, kopie poprzednich wersji, wykrywanie zewnętrznych zmian,
odzysk powiązania w sesji, natychmiastowe wyłączenie opcji i odłączenie po zapisie
roboczego Markdowna. Sprawdzone dla DOCX, EPUB, HTML i RTF (nie obiecujemy ODT,
PDF ani starego DOC bez osobnej obsługi). 84 sprawdzenia integracyjne bez błędu;
żywy NVDA potwierdził nazwę i stan checkboxa oraz zapis do DOCX po Control+S.
Dowody: `testy/wyniki/original-integration-5114.log`,
`testy/wyniki/original-live-nvda-5114.json`.
Przegląd domknięty: naprawiono izolację okien odzysku i zachowanie nieodczytanych
kopii (5 kontroli przed poprawką zawodziło), zapis snippetu i stan po zapisie
do oryginału. Rozszerzony zestaw integracji: 107/107. Trwa końcowy build wydania.
Nie obiecywać bezstratnej edycji dowolnego DOCX.

Próba Pandoca zachowała tekst i nagłówek, lecz utraciła kolor, rozmiar czcionki
i wyrównanie. Wynik: `testy/wyniki/roundtrip-docx-decyzja.json`.

Pozostałe uwagi (m.in. sporadyczny skok podglądu i komunikaty ustawień) nie są
jeszcze oznaczone jako naprawione. Nie rozszerzamy wydania na nowe funkcje
śledzenia zmian ani nie zmieniamy kolejnych skrótów bez osobnego ustalenia.


## 1. Alt+F4 zamyka caly program (BLAD, potwierdzony w kodzie)

PRZYCZYNA ZNALEZIONA: EdSharp.cs linia 1165
    menuFileExit = CreateMenuItem("&E&xit EdSharp", "Alt+F4", ...)
Alt+F4 jest ZAREJESTROWANY jako "Exit EdSharp" - wiec dziala tak, jak
zaprogramowano, na kazdej liscie i w kazdym oknie. To nie przypadek.
Windows standardowo: Alt+F4 zamyka OKNO, nie aplikacje. Ctrl+F4 (zamknij
biezace okno) juz istnieje i zostaje nietkniety.
DO USTALENIA z Michalem: czy Alt+F4 ma zamykac tylko biezace okno/liste,
a wyjscie z programu przeniesc na inny skrot.

## 2. Cotygodniowy przeglad oryginalnego EdSharp 5 i projektu Quill

Zadanie REGULARNE, raz w tygodniu:
- co nowego w EdSharp 5 Jamala Mazrui (upstream)
- co wnosi Quill - inspiracje, nie kopiowanie
Do zrobienia jako zadanie cykliczne (cron + raport na Telegram).

## 3. Zakladki - podglad tresci wiersza w menu

Alt+B = zakladki, Alt+Shift+B = zakladki nazwane.
TERAZ: strzalka w lewo czyta NUMER LINII z zakladka.
MA BYC: czyta TRESC wiersza, w ktorym jest zakladka, BEZ wychodzenia
z menu (podglad na miejscu).
Kod: sekcja Bookmarks w EdSharp.cs (MigrateBookmarksOutOfFavorites,
odczyt App.ReadValue("Bookmarks", ...) okolice linii 745).

## 4. Polski slownik - nowy format slownikow w EdSharp 5 [ZROBIONE w 5.0.84 - bez Worda, sprawdzaczem Windows, ZMIERZONE]

W oryginalnym EdSharp 5 pojawil sie nowy format/system slownikow.
NAJPIERW ZBADAC, jak to zrobione u Jamala, potem decyzja.

## 5. Zamarzanie kursora po Alt+Tab [ZROBIONE w 5.0.83 - przyczyna ZMIERZONA]

Objaw zglaszany przez Michala: po przejsciu na okno przez Alt+Tab kursor
jakby zamarza, okno znika; czasem przy wyszukiwaniu, Esc pomaga.
NIEPOTWIERDZONE - najpierw pomiar, nie poprawka na wyczucie.
Narzedzie: testy z zywym NVDA przez mostek MCP (mam dostep).

## 6. Instalator w trybie cichym -- ZROBIONE w 5.0.81

Instalacja bez pytan (Inno Setup /SILENT lub /VERYSILENT).
Potrzebne do zadania 7.

## 7. Sprawdzanie nowej wersji przy uruchomieniu programu -- ZROBIONE w 5.0.81

Teraz tylko recznie przez F11. Ma sprawdzac sam przy starcie i pytac
o instalacje. Uwaga: nie zamulac startu ani nie gadac, gdy nic nowego
(zasada z 5.0.77 - pytanie tylko gdy jest realny wybor).

## 8. Aktualizacja komponentow zewnetrznych, bezobslugowo [ZROBIONE w 5.0.85 - ZMIERZONE]

Sprawdzanie i instalacja potrzebnych komponentow (pandoc itd.) mozliwie
bez udzialu uzytkownika. Jesli konieczny restart programu: pytanie
o zapis otwartych plikow i automatyczny restart.

ZROBIONE: nowy plik Skladniki.cs. Przy starcie program po cichu w tle
sprawdza, czy sa narzedzia Convert (pandoc, tidy, xpdf, liblouis, astyle)
i dociaga BRAKUJACE z ich oryginalnych zrodel - bez okien i bez pytan.
Nie aktualizuje przy okazji tego, co juz dziala (to by zmienialo dzialajace
narzedzia bez wiedzy uzytkownika). Raz na dobe, wylacznik w ini:
CheckComponentsOnStartup=N. Restart NIE jest potrzebny - narzedzia sa
osobnymi programami, wolanymi dopiero przy konwersji.
Aktualizowanie wszystkiego na zadanie: menu Help, "Update Components".
Gdy konwersja padnie z powodu braku narzedzia, komunikat podaje jego NAZWE
i proponuje pobranie (do 5.0.84 pokazywal sam wiersz polecenia).

## 9. Stare polskie kodowania - wczytywanie i konwersja do UTF-8 [ZROBIONE w 5.0.82]

Mazovia, Latin II (CP852), Windows-1250. Wczytanie + automatyczna
konwersja do UTF-8. W kodzie jest juz 15 wzmianek o 1250/Latin -
sprawdzic, co dziala, zanim dopisze cokolwiek.

## 10. Pliki CSV wczytywane jako tabela w kreatorze  [ZROBIONE w 5.0.87 - ZMIERZONE]

CSV ma sie otwierac jako tabela w naszym systemie-kreatorze tabel.
Dopytanie 12.09.2026: "i edycje chyba tez w takiej formie" - jedno okno do
przegladania I poprawiania.

JAK ZROBIONE
- Nowy plik Csv.cs: wlasny czytnik/zapisywacz wg RFC 4180 (bez bibliotek).
- EditCsvAsTable() w oknie glownym: ta sama dostepna siatka co kreator tabel.
- Nazwy kolumn Z PIERWSZEGO WIERSZA PLIKU - czytnik mowi "ludnosc: 800653".
- Separator rozpoznawany sam: przecinek, SREDNIK (polski Excel), tabulator,
  kreska pionowa.  Przy zapisie wraca ten sam separator, koniec wiersza i
  kodowanie, ktore mial plik.
- Ctrl+Enter zapisuje do pliku; kopia poprzedniej wersji jako .bak.
- Przy otwieraniu .csv program pyta, ale tylko gdy tresc wyglada na tabele.
- Menu Misc: "Edit CSV as Table ..." dla pliku otwartego juz jako tekst.

ZMIERZONE: testy/pomiar_csv.cs 35/35 zgodnych (20 000 wierszy w 0,02 s),
testy/pomiar_csv_w_programie.cs 21/21 zgodnych na wydanej binarce.

## 11. Paleta polecen (jak w AMC)

W kodzie NIE MA jeszcze zadnej palety (sprawdzone: 0 wzmianek).
SKROT - PROPOZYCJA: Ctrl+Shift+P.
Uzasadnienie: standard w edytorach (VS Code, Sublime), a w EdSharpNG
Ctrl+Shift+P jest WOLNY - zajete sa tylko Ctrl+Shift+A, C, F, F6.
Alternatywa Ctrl+Shift+Spacja - tez wolna, ale mniej oczywista.

## 12. Stan programu po instalacji i autozapis  [ZROBIONE 5.0.93, okno ustawien 5.0.94]

Zgloszenie Michala (trzy osobne sprawy, jedna kategoria - ciagosc pracy):
1. Po instalacji nowej wersji program ma wstac w stanie, w jakim byl ostatnio
   (otwarte pliki, pozycje kursora, zakladki).
2. Autozapis - zeby nie tracic tekstu.
3. NIE otwierac pustego dokumentu "noname", gdy jest co przywrocic.

ZROBIONE. Nowy plik Sesja.cs; wszystkie trzy punkty:
1. Sesja zapisywana W TRAKCIE pracy (nie tylko przy wyjsciu, jak stara sekcja
   [Previous]), wiec przezywa zanik pradu i zabicie procesu. Wraca sciezka,
   pozycja kursora i zakladki.
2. Autozapis co N sekund do OSOBNEJ kopii w katalogu danych - plik uzytkownika
   nietkniety. Po awarii program pyta, czy przywrocic, i mowi ile plikow i
   z kiedy.
3. Puste okno NoName powstaje na koncu startu i tylko gdy nic innego sie nie
   otwarlo.

USTAWIENIA: menu Misc -> Work Continuity. Wlasne okno z dwoma polami wyboru i
liczba sekund; OBIE funkcje domyslnie WYLACZONE (warunek Michala: "nie kazdy
moze sobie czegos takiego zyczyc"). Pierwsza wersja (5.0.93) miala to jako
klucze w pliku ustawien i przelaczniki w menu - odrzucone, bo "chcialbym zeby
do tych ustawien normalnie byl dostep" (12.09.2026).

POMIAR: testy/pomiar_sesja.cs, 74 przypadki, 74/74 OK.
