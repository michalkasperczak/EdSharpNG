# Sledzenie zmian i wspolna praca na dwoch EdSharpach - propozycja

## Stan na 18.09.2026 — dalsze prace wstrzymane

Kroki 1a i 1b są już wdrożone; opis układu F9 i wcześniejszych pomiarów jest
w rozdziale 6. Stwierdzenie „kodu jeszcze nie ma” dotyczyło pierwotnej propozycji,
nie obecnej wersji programu.

Michał potwierdził instalację 5.0.114 i praktyczne działanie Control+Shift+C.
Śledzenia zmian i komunikatów jeszcze nie sprawdził w praktyce — wróci do nich
później. Nie traktować potwierdzenia kopiowania jako zatwierdzenia śledzenia zmian.

Na jego prośbę dalszy rozwój edytora jest wstrzymany na dłuższą przerwę,
bez terminu wznowienia. Krok 1c (ukrywanie znaczników) i następne etapy pozostają
planem na przyszłość, nie zleceniem do rozpoczęcia teraz. Bieżący zapis stanu:
`ZADANIA-2026-09.md`.

Poniżej zachowano propozycję z 17.09.2026, uzgodnienia i późniejszy opis kroków.
Cele planu nie oznaczają, że wszystkie opisane funkcje są już wdrożone.

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

### 3.3. Czego to NIE bedzie TERAZ

Nie bedzie pisania w tym samym pliku jednoczesnie, na zywo, jak w Dokumentach
Google. To wymaga serwera i stalego polaczenia, a przy czytniku ekranu tekst
zmieniajacy sie pod kursorem, gdy sam nic nie robisz, jest nie do czytania.
Model "ja pracuje u siebie, potem wymieniamy sie zmianami" jest tu nie tylko
tanszy, ale po prostu lepszy.

### 3.4. Praca na zywo - kierunek na pozniej (decyzja MK 17.09.2026)

Michal: praca grupowa w czasie rzeczywistym ma byc "gdzies z tylu glowy" -
nie teraz, ale tak, zeby nie zamurowac sobie drogi. I nie wlasnym protokolem,
a w oparciu o istniejacy standard (wymienil Etherpada, pozniej Dokumenty
Google).

Co to znaczy praktycznie DZIS - jedna zasada projektowa, zero dodatkowej pracy:
zmiane zapisywac jako OPERACJE na tekscie (w tym miejscu wstawiono to, w tym
usunieto tyle znakow), a nie tylko jako gotowy wynik. Wszystkie mechanizmy
pracy na zywo tak licza. Jesli okno "Zmiany" bedzie od poczatku stalo na
liscie operacji, podlaczenie zywego zrodla to podmiana dostawcy tych operacji,
nie przepisywanie funkcji.

ZMIERZONE U ZRODLA 17.09.2026, dwie drogi, obie realne:

1. Etherpad (serwer, ktory mozna postawic u siebie): ma HTTP API z metodami
   getText(padID, [rev]) i setText(padID, text) - czyli EdSharp moglby czytac
   i zapisywac tresc pada zwyklym zapytaniem HTTP, bez zadnej biblioteki.
   To najtansze wejscie: "otworz pad" i "wyslij moja wersje do pada", a inni
   siedza w przegladarce. Nie jest to jeszcze pisanie na zywo, ale jest to
   wspolny dokument na standardzie.
2. Yjs (biblioteka, na ktorej stoi wiekszosc dzisiejszej pracy na zywo) ma
   oficjalny port na .NET: github.com/yjs/ycs, licencja MIT, cel kompilacji
   netstandard2.0 i 2.1. Netstandard2.0 daje sie uzywac z .NET Framework 4.8,
   czyli z naszego programu - PASUJE. Zastrzezenia uczciwie: obsluguje
   Y.Array, Y.Map, Y.Text, nie obsluguje typow Y.Xml; ostatni commit z 2023
   roku, czyli projekt jest w praktyce zamrozony; wymaga Newtonsoft.Json,
   a my kompilujemy jednym csc bez menedzera pakietow - trzeba by dowiezc
   dwa pliki z paczka.

Dokumenty Google to trzecia sprawa i najdalsza: nie ma tam pisania na zywo
z zewnetrznego programu przez API - API pozwala czytac i zapisywac dokument,
nie siedziec w sesji edycji. Realne jest wiec "wez tekst z Dokumentow / odloz
tekst do Dokumentow", nie wspolny kursor.

---

## 4. Co proponuje zrobic w jakiej kolejnosci

Kolejnosc PO odpowiedziach Michala z 17.09.2026 (rozdzial 5). Rdzeniem jest
okno "Zmiany" i lista zmian - wszystko inne tylko ja zasila.

1. **Rdzen: lista zmian, okno "Zmiany", skoki, przyjmij/odrzuc.**
   Dokument czyta sie czysto (znaczniki ukryte), opcja pokazania surowych
   znacznikow fabrycznie wylaczona. Lista trzymana jako OPERACJE na tekscie
   (patrz 3.4), nie jako gotowy wynik.
2. **Wpisywanie poprawek jako recenzent** - dopisz jako zmiana, skasuj jako
   zmiana, komentarz recenzenta. To zasila liste z punktu 1.
3. **Porownanie dwoch plikow jako zmiany do przyjecia** - drugi dostawca tej
   samej listy. Od tego momentu i recenzja, i historia dzialaja tym samym
   oknem.
4. **Historia wlasnego pliku (poziom 1)** - na autozapisie z Ciaglosci Pracy,
   otwierana przez porownanie z punktu 3.
5. **Folder wspolny dla dwoch osob (poziom 2)** - pierwszy dostawca wymiany
   plikow, pisany za interfejsem, ktory da sie podmienic.
6. **Git za kulisami (poziom 3)** - drugi dostawca wymiany, ten sam interfejs.
   Zaplanowany cel, nie warunkowy dodatek.
7. **Cokolwiek z Wordem** - na koniec, "jesli sie uda".

### 4.1. Punkt 1 podzielony na male kroki (na zyczenie MK 17.09.2026)

MK: "I robisz to stopniowo?" - TAK. Punkt 1 z listy wyzej to nie jedno duze
wydanie, a cztery kroki. Kazdy krok konczy sie DZIALAJACA i sprawdzalna
funkcja, kazdy da sie przetestowac osobno, i po kazdym mozna sie zatrzymac
albo zawrocic.

**Krok 1a: samo rozpoznawanie zmian, nic nie ukrywamy.**
Nowy plik Zmiany.cs z funkcjami czystymi: rozpoznaj znaczniki w tekscie,
zwroc liste zmian (rodzaj, miejsce, stara tresc, nowa tresc), przelicz tekst
po przyjeciu wszystkiego i po odrzuceniu wszystkiego. Zero zmian w interfejsie.
Do sprawdzenia pomiarem na golym kompilatorze, bez uruchamiania programu.
Wzor: tak samo zaczynalismy Zadania.cs przy listach zadan i to sie sprawdzilo.

**Krok 1b: okno "Zmiany" i skoki, znaczniki JESZCZE widoczne. — ZROBIONE 18.09.2026**
Okno wzorowane na oknie listy zadan: lista zmian czytana zdaniami, Enter
skacze. Plus para klawiszy do skakania po zmianach
w dokumencie. Plus przyjmij i odrzuc na pozycji kursora oraz przyjmij/odrzuc
wszystko. Na tym etapie znaczniki sa w tekscie widoczne i slyszalne - halasliwe,
ale wszystko inne mozna juz przetestowac zywym NVDA.
Klawisze: rozdzial 6. Pomiar: `testy/pomiar_zmiany_1b.ps1`, 10/10 na zywym
programie. SPACJA W OKNIE NIC NIE PRZELACZA (inaczej niz w liscie zadan):
przyjecie zmiany nie jest odwracalne tym samym klawiszem, bo po przyjeciu
znacznika juz nie ma. Przyjmowanie zostaje przy kursorze w dokumencie, gdzie
czlowiek slyszy kontekst zdania.

**Krok 1c: ukrywanie znacznikow.**
Ten najdrozszy fragment osobno, dopiero
gdy 1b dziala: tresc w edytorze bez znacznikow, pozycje zmian trzymane osobno
i przeliczane przy edycji. Opcja w ustawieniach do pokazania surowych
znacznikow - fabrycznie wylaczona, czyli po tym kroku dokument brzmi czysto.
Gdyby ten krok okazal sie za trudny, 1b i tak zostaje uzyteczne - dlatego jest
przed nim, a nie po.

**Krok 1d: zapis i odczyt pliku ze zmianami.**
Zeby plik ze zmianami przezyl zamkniecie programu i dal sie komus przeslac:
zapis znacznikow do pliku, wczytanie z powrotem, kopia .bak przed nadpisaniem.

Dopiero po 1d ma sens punkt 2 z listy (wpisywanie poprawek jako recenzent).

---

## 5. Pytania i odpowiedzi Michala (17.09.2026)

3. **Druga strona: tez EdSharp.** ODPOWIEDZIANE. Michal: zgodnosc z Wordem
   "jezeli da sie" - dobrze, ale realnie raczej nie wyjdzie, wiec zakladamy,
   ze druga osoba tez ma EdSharpa. SKUTEK DLA PLANU: eksport do sledzenia
   zmian Worda ZOSTAJE na koncu jako "jesli sie uda", nie jest warunkiem.
   Nasza wlasna skladnia znacznikow (punkt 2.1) wystarcza, bo obie strony
   czytaja ja tym samym programem. Ma to tez zalete: pliki zostaja czystym
   Markdownem, bez zaleznosci od Worda i bez Pandoca, ktory tego i tak
   nie umie.

1. **Do czego ma sluzyc: DO OBU RZECZY.** ODPOWIEDZIANE. Michal: "oba warianty,
   recenzji redagowania i historii zmian, bylyby wskazane". SKUTEK DLA PLANU:
   nie wybieramy jednego zrodla zmian. Rdzeniem jest lista zmian z okna
   "Zmiany", a recenzja i historia sa dwoma DOSTAWCAMI tej samej listy. Robimy
   je po kolei, nie zamiast siebie.

2. **Znaczniki w tekscie: DOMYSLNIE NIEWIDOCZNE, opcja do wlaczenia.**
   ODPOWIEDZIANE. Michal: "raczej nie wyobrazam sobie, zeby te nawiasy byly
   widoczne w tresci, ale opcjonalnie mozna by to wlaczyc". SKUTEK DLA PLANU:
   dokument brzmi CZYSTO (tak, jakby zmiany byly przyjete), a zmiany dostepne
   sa przez okno "Zmiany" i przez skoki. Opcja w ustawieniach do pokazania
   surowych znacznikow - fabrycznie WYLACZONA.
   To jest ta sama zasada, ktora Michal postawil przy Ciaglosci Pracy
   i autozapisie (12.09.2026): funkcja, ktorej nie kazdy sobie zyczy,
   ma wlacznik i startuje wylaczona.
   UWAGA TECHNICZNA: to jest najdrozsza decyzja w calej funkcji. Znaczniki
   SA w pliku, ale NIE moga byc w tym, co czyta czytnik - czyli tresc w oknie
   edytora to tekst po ukryciu znacznikow, a pozycje zmian trzymamy osobno
   i przeliczamy przy kazdej edycji. Nie da sie tego zrobic "potem" - musi byc
   tak zaprojektowane od pierwszej linii kodu.

4. **Wymiana plikow: CHMURA teraz, GIT docelowo.** ODPOWIEDZIANE. Michal:
   "nie mam doswiadczenia, no ale docelowo pewnie taki git to moglaby byc tez
   ciekawa opcja. Nie mowie, ze od razu to wszystko musimy zrobic."
   SKUTEK DLA PLANU: kolejnosc z rozdzialu 4 zostaje bez zmian - folder wspolny
   (poziom 2) przed gitem (poziom 3) - ale git NIE jest juz warunkowy
   ("tylko jesli poprzednie okaza sie za male"), jest zaplanowanym celem.
   Warunek projektowy: wymiana plikow ma byc DOSTAWCA, ktorego mozna podmienic.
   Ten sam mechanizm "sciagnij cudza wersje i pokaz jako zmiany do przyjecia"
   musi dzialac i dla folderu w chmurze, i dla gita - inaczej poziom 3
   oznacza pisanie wszystkiego od nowa.

## 6. Klawisze kroku 1b - UKLAD ZATWIERDZONY PRZEZ MK 18.09.2026

MK 17.09.2026: "Dobrac liste i zaproponowac."  Propozycja z 17.09 (rozdzial 6.2
nizej, zachowany dla historii) NIE zostala przyjeta bez zmian - MK ja ODWROCIL
18.09.2026 i to jego uklad siedzi w kodzie:

- **F9** - nastepna zmiana, mowi rodzaj, tresc i numer wiersza
- **Shift+F9** - poprzednia zmiana, to samo
- **Alt+F9** - przyjmij zmiane pod kursorem
- **Alt+Shift+F9** - odrzuc zmiane pod kursorem
- **Control+F9** - okno "Zmiany" z lista wszystkich

Jego slowa: "F dziewiec shift F dziewiec nastepna poprzednia zmiana, alt F
dziewiec przyjmij, alt shift F dziewiec odrzuc, kontrol F dziewiec lista".
Potem doprecyzowal na pytanie: "Tak, na Alt F9, a Alt Shift F9 nieprzyjmowanie."

DLACZEGO JEGO UKLAD JEST LEPSZY OD MOJEJ PROPOZYCJI - dwie rzeczy, obie
merytoryczne, nie kurtuazja:

Pierwsza: skoki robi sie NAJCZESCIEJ.  Przez recenzje przechodzi sie zmiana po
zmianie, a przyjmuje tylko czesc - wiec najkrotszy klawisz nalezy sie skokom, nie
przyjmowaniu.  Moja propozycja stawiala skoki na trzyklawiszowych chordach.

Druga, wazniejsza: ODRZUCENIE KASUJE CZYJAS PRACE, a u MK wymaga DWOCH
modyfikatorow.  W mojej propozycji siedzialo na samym Shift+F9 i sam zglaszalem
to jako ryzyko przypadkowego naciecia (rozdzial 6.2, akapit "RYZYKO").  Uklad MK
to ryzyko znosi ukladem klawiszy, bez zadnej dodatkowej ostroznosci w kodzie.

Control+F9 na okno bylo wolne, bo "Say Compiler" zostalo usuniete 16.09.2026
razem z kompilowaniem (EdSharp.cs 5905).

### 6.0. Wolnosc chordow zmierzona przed przypisaniem (18.09.2026)

Powtorzone tuz przed wpisaniem do kodu, w trzech miejscach:

1. `EdSharp_Hotkeys.txt` - zero wystapien "F9".
2. `Hotkeys.ini` - zero wystapien "F9".
3. `grep Keys.F9` po wszystkich plikach `.cs` - zero warunkow, same komentarze
   historyczne.

### 6.05. Pomiar kroku 1b na ZYWYM programie

`testy/pomiar_zmiany_1b.ps1` - **10 asercji, 10 OK, 0 ZLE** (18.09.2026, na
binarce zbudowanej z tej zmiany).

Krok 1a byl mierzony golym kompilatorem (90/90 na czystych funkcjach z
Zmiany.cs) i tego ten pomiar NIE powtarza.  Mierzy dokladnie to, czego tamten
nie mogl dotknac: czy klawisze dochodza do komend w zywym oknie i czy zapisany
plik wyglada tak, jak powinien.

Zmierzone: przyjecie i odrzucenie dopisania, usuniecia i podmiany (szesc
wariantow, bo dla kazdego rodzaju przyjecie i odrzucenie daja INNY wynik),
drugi skok F9, powrot Shift+F9, zniknienie komentarza recenzenta.

Co czyni ten pomiar roznicujacym: kazdy wariant ma KONTROLE POZYTYWNA
KLAWIATURY (wpisanie litery i zapis przed wlasciwym pomiarem) - bez niej "plik
sie nie zmienil" znaczylo by to samo przy dzialajacej komendzie i przy
klawiszach, ktore w ogole nie dochodza do okna.  Osobno mierzony jest przypadek
NEGATYWNY: Alt+F9 na pliku `.txt` nie ma prawa nic zrobic i nie robi.

### 6.1. Jak sprawdzilem, ze te chordy sa wolne (zapis z 17.09.2026)

Sprawdzone W TRZECH miejscach, bo sam spis skrotow pokazuje tylko czesc prawdy
(lekcja 5.0.43 z golego F9, MAPA-DROGOWA rozdzial o kolizji "ktorej nie bylo
widac w spisie"):

1. EdSharp_Hotkeys.txt - 269 wierszy, rodzina F9 ma ZERO wystapien.
2. EdSharp.cs i KeyMap.cs - grep po Keys.F9 nie zwraca ani jednego warunku,
   same komentarze historyczne.
3. Historia decyzji: Alt+F9 i Control+Alt+F9 zdjete 13.09.2026 (CO-USUWAMY 1.3),
   Control+Shift+F9 i Alt+Shift+F9 zwolnione tym samym ruchem (EdSharp.cs 2058),
   goly F9 i Shift+F9 zwolnione 03.09.2026 wraz z warstwa skryptow JAWS
   (EdSharp.cs 2369, jego slowa: "z tego klawisza F dziewiec i skryptu raczej
   rezygnujemy").

Zadna z liter w propozycji nie tworzy polskiego znaku pod prawym Altem, ale to
i tak NIE jest argument, na ktorym opieram bezpieczenstwo: straznik
Util.IsTypingChord pyta uklad klawiatury przy budowie menu i odmawia postawienia
komendy na chordzie, ktory WPISUJE znak.  Propozycja trzyma sie rodziny F9
wlasnie dlatego, ze klawisz funkcyjny zadnego znaku nie wpisuje.

### 6.2. Propozycja z 17.09.2026 - NIE PRZYJETA, zachowana dla historii

Rodzina F9 w calosci na sledzenie zmian.  Jeden klawisz, jedna rodzina, jedno
skojarzenie - tak jak F9 w Wordzie nie ma nic wspolnego ze zmianami, ale tu MK
sam te rodzine zwolnil i jest pusta.

- Control+Shift+F9 - nastepna zmiana, mowi rodzaj, tresc i numer wiersza
- Alt+Shift+F9 - poprzednia zmiana, to samo
- Control+Alt+F9 - okno "Zmiany" z lista wszystkich zmian w dokumencie
- Alt+F9 - przyjmij zmiane pod kursorem
- Shift+F9 - odrzuc zmiane pod kursorem

DLACZEGO TAK, a nie inaczej:

Skoki na Control+Shift+F9 i Alt+Shift+F9 to DOKLADNIE ten uklad, ktory
komentarze mialy przed przeniesieniem (EdSharp.cs 2058) - palec juz go zna.

Okno na Control+Alt+F9 - bo wszystkie okna list w EdSharpie siedza na
trzyklawiszowych chordach, a Control+Alt+F9 juz raz bylo oknem listy
(komentarzy), wiec skojarzenie "trzy klawisze plus F9 rowna sie okno" zostaje.

Przyjmij na Alt+F9 i odrzuc na Shift+F9 - te dwa sa NAJKROTSZE celowo, bo przy
przegladaniu recenzji naciska sie je najczesciej: skok, przyjmij, skok,
przyjmij.  Alt+F9 to tez dawny "wstaw komentarz", czyli chord, ktory JUZ pisal
do dokumentu - a przyjmij tez pisze do dokumentu.

RYZYKO, ktore widze i zglaszam wprost: Shift+F9 jest krotkie, a odrzucenie
zmiany USUWA czyjas prace.  Dlatego odrzucenie ma MOWIC co zrobilo ("odrzucone:
dopisanie, taka tresc") i ma sie cofac zwyklym Control+Z jak kazda inna zmiana
tekstu.  Jesli MK uzna to za zbyt lekkie, odrzucenie moze zejsc na chord
trzyklawiszowy - to jedna linia roznicy.

### 6.3. Czego w tej liscie NIE MA i dlaczego

Przyjmij wszystkie i odrzuc wszystkie - bez klawisza, tylko w palecie polecen
i w menu.  To operacja na calym dokumencie, robi sie ja raz na koniec pracy,
a kazdy klawisz przy niej to ryzyko przypadkowego naciecia.

Wstawianie zmiany jako recenzent (zapisz swoja poprawke jako zmiane do
przyjecia) - to krok 1d, nie 1b.  Na razie zmiany powstaja z porownania dwoch
wersji pliku, nie z pisania.

Wlacznik widocznosci znacznikow - krok 1c, i to raczej pozycja w ustawieniach
niz klawisz, bo przelacza sie go raz.
