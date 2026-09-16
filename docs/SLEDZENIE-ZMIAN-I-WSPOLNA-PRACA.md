# Sledzenie zmian i wspolna praca na dwoch EdSharpach - propozycja

Dokument roboczy, 17.09.2026. Nic z tego jeszcze nie jest zrobione. To jest
projekt do Twojej decyzji, nie opis dzialajacej funkcji.

Zlecenie: "jak mozna by zrobic sledzenie zmian i wspolna prace na dwoch
EdSharpach".

---

## 1. To sa DWA rozne zadania, nie jedno

Warto je rozdzielic, bo maja inne rozwiazania i inna trudnosc.

**Zadanie A: sledzenie zmian.** Widac, kto co dopisal, skasowal i zmienil,
i mozna to przyjac albo odrzucic. Odpowiednik "Recenzja" z Worda. Dotyczy
JEDNEGO pliku i moze dzialac nawet u jednej osoby (wersja z wczoraj wobec
dzisiejszej).

**Zadanie B: wspolna praca dwoch osob.** Dwa komputery, dwa EdSharpy, jeden
tekst. Trzeba jakos wymieniac zmiany i radzic sobie z sytuacja, gdy dwie osoby
zmienily to samo miejsce.

Zadanie A da sie zrobic w calosci wewnatrz EdSharpa. Zadanie B wymaga czegos
na zewnatrz - jakiegos posrednika, przez ktory pliki przechodza.

---

## 2. Zadanie A - sledzenie zmian

### 2.1. Czym zapisac zmiane w pliku Markdown

Sprawdzone u zrodla 17.09.2026: istnieje gotowa, ustalona skladnia do tego -
**CriticMarkup**. Piec znacznikow, wszystkie da sie wymowic i wszystkie zostaja
w zwyklym pliku tekstowym:

- dopisane: `{++ nowy tekst ++}`
- skasowane: `{-- stary tekst --}`
- zamienione: `{~~ stary ~> nowy ~~}`
- podswietlone: `{== fragment ==}`
- komentarz: `{>> uwaga recenzenta <<}`

Dlaczego to, a nie wlasny wymysl: jest opisane, ma implementacje w innych
edytorach (m.in. wtyczka do Obsidiana) i istnieja narzedzia, ktore to czytaja.
Czyli plik z Twoimi poprawkami otworzy sie u kogos innego.

CZEGO NIE WIEM I NIE UDAJE, ZE WIEM: Pandoc **nie obsluguje CriticMarkup
natywnie** - to jest otwarte zgloszenie w Pandocu od lat, sprawdzone u zrodla
(jgm/pandoc, zgloszenia 2873 i 5430). Znaczy to trzy rzeczy:
- eksport do Worda ze sledzeniem zmian Worda NIE wyjdzie sam z siebie,
- do konwersji potrzebny bylby dodatkowy krok (istnieja obce narzedzia
  "pancritic" i "pandiff", oba pythonowe albo nodowe - do sprawdzenia, czy warto
  je ciagnac, czy taniej napisac przeliczanie samemu),
- surowa konwersja pliku z CriticMarkup przez Pandoc rozjedzie znaczniki
  w tekscie. To trzeba obsluzyc PRZED konwersja, inaczej bedzie brzydko.

Mysl techniczna: skoro znaczniki sa proste, przeliczenie "przyjmij wszystko" i
"odrzuc wszystko" to kilkadziesiat linii wlasnego kodu, bez zadnej obcej
biblioteki. To bym zrobil sam. Tlumaczenie na sledzenie zmian Worda to osobna,
duzo wieksza sprawa - i moim zdaniem na potem.

### 2.2. Jak to ma brzmiec i dzialac dla czytnika

To jest wazniejsze od skladni. Propozycja:

- **Osobne okno "Zmiany"** (jak dzisiejsze okno listy zadan): lista wszystkich
  zmian w dokumencie, kazda czytana jako zdanie, np.
  "dopisane: nowy tekst, wiersz 12" albo
  "zamienione: bylo stary, jest nowy, wiersz 40".
  W tym oknie: Enter skacze do miejsca, jedna litera przyjmuje, druga odrzuca,
  spacja czyta pelna tresc. Wzor z okna zadan sie sprawdzil.
- **Skoki po zmianach** w dokumencie, para klawiszy w jedna i w druga strone,
  z odczytem rodzaju zmiany.
- **Przyjmij / odrzuc w miejscu kursora** - dwa polecenia.
- **Przyjmij wszystko / odrzuc wszystko** - z pytaniem, ile zmian to dotyczy.
- **Czytanie w trakcie pisania:** znaczniki w tekscie beda czytane przez NVDA
  jako nawiasy i tyldy, co jest halasliwe. Do rozwazenia tryb "czytaj tekst
  po przyjeciu zmian" - dokument brzmi czysto, a zmiany sa dostepne tylko przez
  okno i skoki. To trzeba przemyslec razem, bo to najdelikatniejszy punkt calej
  funkcji.

### 2.3. Skad biora sie zmiany do sledzenia

Trzy zrodla, kazde sensowne osobno:

1. **Ja wpisuje jako recenzent** - polecenia "dopisz jako zmiana", "skasuj jako
   zmiana", "dodaj komentarz recenzenta". Najprostsze do zrobienia.
2. **Porownanie dwoch plikow** - "porownaj ten plik z innym i pokaz roznice jako
   zmiany do przyjecia". To jest w praktyce najuzyteczniejsze i tu tez wlasny
   kod wystarczy (porownanie wiersz do wiersza, w drugim kroku po slowach).
3. **Z importu z Worda** - sprawdzone 13.09.2026 i zapisane: komentarze
   recenzenta z .docx przy zwyklym imporcie GINA calkowicie, a z opcja
   sledzenia zmian wychodza w formacie, ktorego dzisiejszy EdSharp nie
   rozpoznaje. Czyli to jest do zrobienia od zera, gdy zdecydujesz.

### 2.4. Ile to jest pracy - moja ocena

- Skladnia, rozpoznawanie, przyjmij/odrzuc, okno zmian, skoki: jedno wydanie,
  podobnej wielkosci jak listy zadan.
- Porownanie dwoch plikow jako zmiany: drugie wydanie.
- Cokolwiek z Wordem: trzecie i najtrudniejsze.

---

## 3. Zadanie B - wspolna praca dwoch EdSharpow

### 3.1. Warstwa, ktora juz istnieje i dziala

Sledzenie wersji plikow tekstowych jest rozwiazane od dawna i nazywa sie git.
Nasze repozytorium juz na tym stoi. Roznica jest w tym, ze git jest narzedziem
dla programisty i przez wiersz polecen jest dla Ciebie nie do uzycia.

Wniosek: nie budowac wlasnego systemu wymiany plikow. Zbudowac PROSTE, mowione
okno nad gotowym mechanizmem.

### 3.2. Trzy poziomy, od najtanszego

**Poziom 1: historia mojego wlasnego pliku (bez drugiej osoby).**
Program po cichu zapisuje kolejne wersje pliku obok niego (mechanizm autozapisu
z Ciaglosci Pracy juz to prawie robi). Nowe polecenie: "historia tego pliku" -
lista wersji z data, Enter otwiera do porownania, i wtedy dziala okno zmian
z punktu 2. To jest tanie i uzyteczne od pierwszego dnia, nawet gdy nikt inny
nie wspolpracuje.

**Poziom 2: dwie osoby, jeden folder wspolny.**
Folder w chmurze (OneDrive, Dropbox, dysk sieciowy) - cokolwiek, co synchronizuje
pliki samo. EdSharp dokladalby:
- "wyslij moja wersje" - kopia z moim podpisem i data,
- "sciagnij wersje drugiej osoby",
- automatyczne porownanie: to, co ona zmienila, wchodzi jako ZMIANY DO
  PRZYJECIA (punkt 2), nie nadpisuje mojego tekstu.
To jest moim zdaniem sciezka wlasciwa: prosta, nie wymaga serwera, a caly
trudny fragment (przyjmowanie zmian) to jest ta sama funkcja co w zadaniu A.

**Poziom 3: repozytorium na GitHubie, ale mowione.**
Program robi git za kulisami, a widzisz tylko: "pobierz zmiany", "wyslij moje
zmiany", "kto zmienil ten wiersz". Trudny fragment to konflikt, czyli oba
komputery zmienily to samo miejsce - i tu tez odpowiedz jest ta sama:
pokaz konflikt jako liste zmian do przyjecia, nigdy jako znaczniki `<<<<<<<`
wklejone w tekst, bo tego nie da sie czytac czytnikiem.

Sprawdzone u zrodla 17.09.2026: biblioteka do gita dla .NET (LibGit2Sharp)
w dzisiejszych wersjach wymaga .NET Framework 4.7.2, a my kompilujemy pod 4.8 -
czyli PASUJE. Starsze wydania szly nawet od 4.6. Trzeba by dowiezc dwa pliki
z paczka. Alternatywa bez zadnej biblioteki: wywolywac zwykly program git,
jesli jest na komputerze - mniej zaleznosci, ale wymaga, zeby uzytkownik mial
gita zainstalowanego.

### 3.3. Czego to NIE bedzie

Nie bedzie pisania w tym samym pliku jednoczesnie, na zywo, jak w Dokumentach
Google. To wymaga serwera i stalego polaczenia, a przy czytniku ekranu tekst
zmieniajacy sie pod kursorem, gdy sam nic nie robisz, jest nie do czytania.
Model "ja pracuje u siebie, potem wymieniamy sie zmianami" jest tu nie tylko
tanszy, ale po prostu lepszy.

---

## 4. Co proponuje zrobic w jakiej kolejnosci

1. Znaczniki zmian w pliku plus okno "Zmiany" plus skoki plus przyjmij/odrzuc.
   Uzyteczne od razu, samo w sobie, bez zadnej wspolpracy.
2. Porownanie dwoch plikow jako zmiany do przyjecia.
3. Historia wlasnego pliku (poziom 1).
4. Folder wspolny dla dwoch osob (poziom 2).
5. Git za kulisami (poziom 3) - tylko jesli poprzednie punkty okaza sie za male.

---

## 5. Pytania, na ktore potrzebuje Twojej odpowiedzi, zanim zaczne

1. Czy sledzenie zmian ma Ci sluzyc do PRACY Z KIMS (recenzja artykulu do Tyflo
   Swiata na przyklad), czy raczej do WLASNEJ historii pliku?
2. Czy znaczniki maja byc widoczne w tekscie podczas pisania, czy dokument ma
   brzmiec czysto, a zmiany byc tylko w osobnym oknie?
3. Czy druga strona wspolpracy to tez EdSharp, czy ktos w Wordzie? Jesli Word,
   to eksport ze sledzeniem zmian Worda staje sie najwazniejszym punktem, a nie
   ostatnim.
4. Czy wymiana plikow ma isc przez folder w chmurze, ktory juz masz, czy przez
   GitHuba?
