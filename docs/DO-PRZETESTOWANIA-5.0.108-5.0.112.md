# Do przetestowania - nowosci z 15-17.09.2026 (wersje 5.0.108 do 5.0.112)

Stan na 17.09.2026, godzina 1:00. Punkt odniesienia: wersja 5.0.107 z 13.09.2026.

Najnowsza wydana wersja to **5.0.112**.
Instalator: `D:\Projekty Codex\Hermes\EdSharpNG_Setup_5.0.112.exe` (jesli go tam nie ma, powiedz - dowioze).
Suma SHA-256 paczki 5.0.112: e3720ac50af60bb11c8dcb7330ca8060e95ba7307e1171c6abdf3e2e39e652e8

UWAGA WSTEPNA: na Twoim glownym komputerze widze tylko instalator 5.0.108 i wpis
"EdSharp 4.0" w rejestrze. Nie umiem stad potwierdzic, ktora wersje faktycznie
masz uruchomiona. Zacznij od punktu 0.

---

## 0. Ktora wersje masz

- F11 (Elevate Version) - program mowi swoja wersje i czy jest aktualna.
- Jesli mowi mniej niz 5.0.112 - zainstaluj 5.0.112, bo cala reszta tej listy
  dotyczy nowszych wersji.

---

## 1. Listy zadan, czyli checklisty Markdown (nowosc 5.0.112)

To jest cala nowa rodzina, wczesniej nie bylo jej wcale. Skladnia w pliku:
`- [ ] tresc` dla niezrobionego, `- [x] tresc` dla zrobionego.

### 1.1. Control+Shift+F2 - zrob z wierszy checkliste (i cofnij)

- Napisz trzy zwykle wiersze, zaznacz je, nacisnij Control+Shift+F2.
  Oczekiwane: kazdy wiersz zaczyna sie od `- [ ] `.
- Nacisnij to samo raz jeszcze na tych samych wierszach.
  Oczekiwane: pola wyboru znikaja, zostaje czysty tekst.
- To samo na jednym wierszu, bez zaznaczenia.

### 1.2. Control+Shift+X - przelacz zrobione / niezrobione

- Kursor na pozycji checklisty, Control+Shift+X.
  Oczekiwane: `[ ]` zmienia sie na `[x]`, program MOWI nowy stan (czytnik nie
  widzi zmiany znaku w wierszu, ktory sie nie przesunal).
- Ponownie Control+Shift+X - wraca `[ ]` i tez to slychac.
- Zaznacz kilka pozycji o roznych stanach i nacisnij raz.
  Oczekiwane: WSZYSTKIE dostaja ten sam stan, ustalony wedlug PIERWSZEJ pozycji
  w zaznaczeniu.
- Kursor na zwyklym wierszu (bez pola wyboru), Control+Shift+X.
  Oczekiwane: sensowny komunikat, a nie cisza i nie zepsuty wiersz.

### 1.3. Control+Shift+F7 - okno listy zadan

- Otwiera liste wszystkich zadan z dokumentu.
- Kazda pozycja ma byc czytana ze stanem: "to do: ..." albo "done: ...".
- SPACJA przelacza stan BEZ zamykania okna - sprawdz, czy po spacji lista mowi
  nowy stan i czy zmiana faktycznie trafia do tekstu.
- Enter skacze do zadania w dokumencie.
- Tytul okna niesie postep (ile z ilu zrobione) - sprawdz, czy czytnik go poda
  po wejsciu do okna.
- Escape wychodzi bez zmian dodatkowych.

### 1.4. Alt+Shift+F2 - powiedz postep

- Bez zaznaczenia: liczy zadania w calym dokumencie.
- Z zaznaczonymi wierszami: liczy TYLKO zaznaczone.

### 1.5. Enter kontynuujacy liste zadan

- Stan kursora na koncu pozycji `- [x] gotowe`, nacisnij Enter i pisz.
  Oczekiwane: nowa pozycja jest NIEZROBIONA (`- [ ]`), nie dziedziczy iksa.
  To bylo osobno poprawiane - warto sprawdzic.

### 1.6. Checklista wobec zwyklej listy punktowanej

Pozycja checklisty pasuje takze do wzorca zwyklego punktora, wiec w szesciu
miejscach zmienialem kolejnosc rozpoznawania. Do sprawdzenia:

- Control+L (lista punktowana) na wierszach, ktore SA checklista - czy nie zjada
  pol wyboru.
- Control+Shift+L (lista numerowana) na checkliscie - to samo.
- Kopiowanie checklisty do Worda.
- Nazwa sekcji (Alt+T) gdy pierwszy wiersz sekcji jest checklista.
- Podglad Markdown (Escape) na dokumencie z checklista - pole ma byc czytane
  jako slowo, nie jako cisza.
- Eksport do HTML - w pliku HTML maja byc prawdziwe pola wyboru
  (`<input type="checkbox" disabled>` z etykieta), nie nawiasy w tekscie.
  Warto otworzyc taki plik w przegladarce i przejsc po nim czytnikiem.

### 1.7. Dwa skroty, ktore MUSIALY sie przeprowadzic

Checklisty zajely Control+Shift+X, wiec:

- **Paleta polecen: Control+Shift+F1** (bylo Control+Shift+X).
- **Samouczek: Control+Alt+F1** (bylo Control+Shift+F1).

Sprawdz oba nowe skroty i sprawdz, czy paleta polecen podaje przy tych
poleceniach WLASNIE te nowe skroty.

Odrzucilem Twoja propozycje Control+Alt+X i Control+Alt+Shift+X: X ma polski
odpowiednik pod prawym Altem, a na chordzie dzielonym z polska litera wygrywa
pisanie litery - komenda nie odpalilaby sie wcale. Stad klawisze funkcyjne.

---

## 2. Pusty wiersz - program ma MILCZEC (5.0.109)

- Przejdz strzalkami w gore i w dol przez pusty wiersz.
  Oczekiwane: slyszysz TYLKO NVDA. Zadnego "Empty line" od programu, w zadnym
  kierunku, ani raz, ani wielokrotnie.
- Ale komunikat ZOSTAJE tam, gdzie program czyta wskazany wiersz na zadanie i
  czytnik go sam nie przeczyta - sprawdz, czy tam nadal jest:
  - lista zakladek (Alt+B) i nazwanych zakladek (Alt+Shift+B), gdy wskazany
    wiersz jest pusty,
  - lista przypisow (Alt+K),
  - przeglad wiersza.

To byla czwarta proba tej samej rzeczy - trzy poprzednie zwezaly czestosc
komunikatu, a problemem bylo jego istnienie.

---

## 3. Polskie litery przy nawigacji po slowach w PODGLADZIE (5.0.108)

- Otworz plik z tekstem typu "Michala Sledzinskiego", "Wszystkich swietych" -
  koniecznie z prawdziwymi ogonkami.
- Wejdz w podglad (Escape) i przejdz Control+strzalka w prawo po slowach.
  Oczekiwane: slowo zaczynajace sie polska litera jest OSOBNYM slowem, nie
  przykleja sie do poprzedniego.
- Przyczyna byla w tym, ze okno podgladu powstawalo z innej odmiany kontrolki
  tekstowej niz okno edycji. Teraz uzywa tej samej.
- Przy okazji: podglad ma zachowywac pozycje kursora przy przerysowaniu.
  Sprawdz, czy nie wraca na poczatek pliku po zmianie tekstu.

To jest ten punkt, przy ktorym pisales "ogolnie po dzisiejszych wersjach kursor
mniej stabilny, preview czesto nie podaza, wraca do poczatku pliku". Jesli to
nadal wystepuje - powiedz, przy jakiej czynnosci, bo to bede mierzyl osobno.

---

## 4. Okno startuje na caly ekran (5.0.111)

- Uruchom program.
  Oczekiwane: okno wypelnia ekran od razu, bez ustawiania czegokolwiek.
- W ustawieniach (Control+przecinek) opcja "Start with the window maximized"
  ma byc ZAZNACZONA fabrycznie.

---

## 5. Lista plikow numerowanych na Alt+0 (5.0.111)

- **Alt+0** otwiera LISTE plikow numerowanych (bylo Alt+Shift+F2, ktore poszlo
  do postepu zadan).
- Alt+1 do Alt+9 otwieraja pliki przypisane do cyfr - to bez zmian.
- **Uwaga, swiadomy skutek uboczny:** dziesiatego slotu NIE da sie juz otworzyc
  z klawiatury, bo Alt+0 zajmuje lista. Przypisanie na Alt+Shift+0 tez zniklo.
  Jesli uzywasz dziesiatego slotu - powiedz, wymyslimy inaczej.

---

## 6. Kalkulator bez silnika skryptow (5.0.111)

Polecenie zostaje: **Control+rowna sie** (Evaluate Expression). Ale liczy je
teraz wlasny kod, nie JScript .NET.

- Napisz `2+2*3`, kursor w wierszu, Control+rowna sie. Wynik ma sie dopisac.
- Sprawdz PRZECINEK jako separator dziesietny: `1,5+2,5` ma dac 4.
  Kropka tez ma dzialac: `1.5+2.5`.
- Nawiasy: `(2+3)*4`.
- Procent postfiksowy: `20%` to 0,2. Reszta z dzielenia: `7 % 3` to 1.
- Potega: `2^10`.
- Funkcje: sqrt, abs, round, floor, ceil, trunc, min, max, pow, log, log10, ln,
  exp, sin, cos, tan, oraz stale pi i e. Np. `sqrt(16)`, `round(2,5)`.
- Blad ma dac zrozumialy komunikat, nie okno awarii: sprobuj `2+`, `foo(3)`,
  `1/0`.
- Czego kalkulator NIE umie i umiec nie ma: zmiennych, przypisan, kodu.

Rowniez z tej zmiany:
- **Control+Shift+rowna sie** (Replace Tokens) - tokeny z pliku ustawien
  wstawiaja sie teraz DOSLOWNIE, jako czysty tekst. Rozwijane sa tylko sekwencje
  z odwrotnym ukosnikiem, czyli `\n` i `\t`. Jesli masz w ustawieniach token,
  ktory kiedys byl kawalkiem programu - dzis wstawi sie jako tekst. Sprawdz swoje
  tokeny.
- Sciezki typu `C:\dane` w pliku ustawien maja zostawac NIETKNIETE. Warto
  sprawdzic, czy nic w ustawieniach sie nie sypie.

---

## 7. Co zniklo z programu (5.0.111) - sprawdz, czy nie brakuje Ci tego

Kazda pozycja: skrot, ktory od teraz nic nie robi. Jesli ktoregos uzywales,
powiedz - przywrocenie jest tanie.

- Compile (Control+F5), Pick Compiler (Control+Shift+F5), Review Output
  (Alt+Shift+F5), Say Compiler (Control+F9) - polecenia budowania kodu razem
  z mechanizmem osobnych plikow ustawien na kompilator.
- Run (bylo w menu File, bez skrotu), Run at Cursor (Shift+F5).
- Text Combine (menu Misc, bez skrotu).
- PyDent (Alt+lewy nawias kwadratowy), PyBrace (Alt+Shift+lewy nawias).
- Kopia .bak nadpisywanego pliku - opcja KeepBackup zniknela. Byla fabrycznie
  wylaczona, wiec dotyczy to tylko kogos, kto ja wlaczyl.
- Adres stronowy: Alt+A mowi juz tylko procent, nie numer strony liczony
  z wysuwow strony (opcja HardPageAddress zniknela).
- Z listy "przy porownywaniu jeden element to" zniknela pozycja "do wysuwu
  strony". Kto ma ja w pliku ustawien, temu dziala dalej.
- Opcje CompileCommand, JumpPosition, AbbreviateOutput.
- Cala warstwa skryptow JScript .NET (pliki EdSharp.dll i EdSharp.js nie jedza
  juz w paczce; z poprzedniej instalacji znikna przy odinstalowaniu).

Sprawdz tez, czy w menu Misc nie zostaly PUSTE pozycje albo separatory po tych
usunieciach, i czy paleta polecen (Control+Shift+F1) nie wymienia niczego, co
juz nie istnieje.

---

## 8. Jeden plik ustawien

Nie ma juz ustawien osobnych na kompilator, wiec jest JEDEN plik ustawien.
- Control+przecinek - okno ustawien; Enter ma potwierdzic zapis na glos, a bez
  zmian powiedziec "No settings changed".
- Alt+Shift+M (Manual Options) - otwiera ten plik do recznej edycji; przycisk
  "Edit file" w oknie ustawien ma prowadzic w to samo miejsce.

---

## 9. Rzeczy, ktore prosze zglos od razu, jesli sie zdarza

- Kursor "zamarza" po Alt+Tab albo po wyjsciu z podgladu.
- Podglad wraca na poczatek pliku.
- Cokolwiek czytane dwa razy (program plus NVDA na to samo).
- Skrot, ktory nic nie robi, ale jest wymieniony w palecie polecen albo
  w podsumowaniu skrotow (Alt+Shift+H).

---

## Sciagawka: nowe i przeniesione klawisze

- Control+Shift+X - przelacz zadanie zrobione / niezrobione (NOWE)
- Control+Shift+F2 - zrob z wierszy checkliste i z powrotem (NOWE)
- Control+Shift+F7 - okno listy zadan (NOWE)
- Alt+Shift+F2 - powiedz postep zadan (NOWE, przejete od listy plikow)
- Control+Shift+F1 - paleta polecen (PRZENIESIONE z Control+Shift+X)
- Control+Alt+F1 - samouczek (PRZENIESIONE z Control+Shift+F1)
- Alt+0 - lista plikow numerowanych (PRZENIESIONE z Alt+Shift+F2)
- Control+rowna sie - kalkulator, teraz wlasny (bez zmiany klawisza)
- Control+F5, Control+Shift+F5, Alt+Shift+F5, Control+F9, Shift+F5,
  Alt+lewy nawias, Alt+Shift+lewy nawias - USUNIETE, nie robia nic
