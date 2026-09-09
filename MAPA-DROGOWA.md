# Mapa drogowa EdSharpNG

Kolejność prac ustalona przez Michała Kasperczaka (Telegram, 28.08.2026 00:30),
potwierdzona przez niego zdaniem "Wszystko OK" o 00:59. To jego projekt i jego
kolejność — nie zmieniamy jej z własnej inicjatywy, nie wyprzedzamy punktów.

Zapisujemy to w repozytorium, a nie tylko w rozmowie, bo poprzednie ustalenia
kolejności ginęły między przebiegami i wracały do niego jako to samo pytanie.

## Kolejność

1. **Pozostałości z kolejki.** Domknięcie tego, co już leży: wyniki jego testów,
   nieaktualne punkty listy testów, rzeczy zbudowane a nieopisane. Dopiero potem
   cokolwiek nowego.
2. **Tabele.** Dostępne tworzenie i edytowanie tabel Markdown.
3. **Kodowanie znaków.** Polskie litery, strony kodowe, oraz zamiana końców
   wiersza między Windows, Mac i Linux.
4. **Komentarze wewnętrzne** w dokumencie.
5. **Konfiguracja i uporządkowanie programu**, wraz z usunięciem zbędnych funkcji.
6. **Praca grupowa**, na tyle, na ile się da: Etherpad i Google Docs.
7. **Polska wersja interfejsu.** Świadomie na końcu — do tego czasu wszystkie
   komunikaty programu zostają po angielsku, bo mieszanie języków jest gorsze
   niż jedno albo drugie konsekwentnie (jego decyzja, 18.08.2026).

## Stan punktu 1 na 28.08.2026

Zamknięte i wysłane: markdownowy spis treści (5.0.25–5.0.27), przypisy razem
z podglądem (5.0.28–5.0.30), przeniesienia z wersji autora i pliki brajlowskie
(5.0.31–5.0.32), ustawienia na Control z przecinkiem (5.0.33), kursor za
punktorem oraz zawijanie i rozwijanie w menu (5.0.34).

Otwarte w punkcie 1:
- Jego wyniki testów wersji 5.0–5.0.21 obejmują 19 punktów z 103. Reszta czeka na
  jego przebieg, nie na naszą pracę.
- Lista testów doprowadzona do stanu 5.0.34: rozdział 17 opisuje wersje od 5.0.31,
  a punkty 1.10, 6.1, 6.3 i 6.6 mówiły o skrótach, które już nie obowiązują.

## Stan punktu 2 na 28.08.2026 (5.0.35, commit bcbb808)

**Kreator tabeli zrobiony w pierwszej wersji i wysłany.** Sposób pracy ustalony
z nim w rozmowie 28.08.2026 17:00–17:06, każdy element jest jego decyzją:

- Control+Shift+T otwiera okno z **jedną komórką**, bez pytania o wymiary — jego
  słowa: „pytanie o wymiary z góry jest sztuczne, bo przy pisaniu rzadko się to
  wie";
- strzałka w prawo z ostatniej kolumny dokłada kolumnę, w dół z ostatniego
  wiersza dokłada wiersz, więc tabela rośnie w miarę pisania;
- pisanie na komórce od razu zastępuje jej treść, **F2** otwiera do poprawki to,
  co w niej już jest, Enter zatwierdza („tak jak w Excelu");
- **Control+Enter** wstawia gotową tabelę do dokumentu — spójne z Control+Enter
  jako wstawianiem nagłówka;
- **Escape** zamyka bez wstawiania, ale pyta o potwierdzenie, gdy coś jest
  wpisane.

Skrót jest jego wyborem: sam o niego zapytał, dostał jawne zgłoszenie kolizji
i odpowiedział „Control shift t to dobry pomysł". **Text Combine nie zniknął** —
oddał chord i zszedł do menu, bo łączenie plików i wyodrębnianie rozdziałów
wróci osobno, jako jedna przemyślana funkcja z jego pomysłem, najpewniej pod
Control+Shift z klawiszem funkcyjnym. To **nowy temat po jego stronie**, nie
punkt tej mapy.

Pierwszy wiersz siatki jest nagłówkiem tabeli i nie da się tego wyłączyć: tabela
Markdown bez wiersza nagłówka i wiersza kresek nie jest tabelą dla żadnego
konwertera. Nagłówki wierszy mówią „Header", „Row 1", „Row 2".

Zmierzone przed wysyłką: logika na zbudowanej binarce przez refleksję (20/20),
nasz konwerter na próbce robi prawdziwą tabelę HTML z komórkami nagłówka przy
kontroli negatywnej, sonda na binarce 16/16, 11 nowych asercji pada na kodzie
sprzed zmiany, lista testów 188/188. **Niezmierzone: wymowa czytnika w samej
siatce** — to wymaga jego maszyny i o to został poproszony wprost.

### Edycja gotowej tabeli: ZROBIONE 28.08.2026 (5.0.38)

Ten sam skrót Control+Shift+T rozpoznaje teraz, gdzie stoi kursor: na wierszu
gotowej tabeli otwiera **tę** tabelę do poprawek (tytuł okna „Edit Table"),
gdziekolwiek indziej zaczyna nową („Insert Table"). Tytuł okna jest jedynym
miejscem, w którym niewidomy słyszy tryb, więc rozróżnia je wprost.

Decyzje, które wynikły z pomiaru, a nie z dyskusji:

- edycja podmienia **dokładnie te wiersze**, które tabela zajmowała, więc wokół
  niej nie narastają puste linie przy każdym przejściu przez kreator;
- Escape pyta o potwierdzenie tylko wtedy, gdy treść **różni się** od tej z
  pliku. Przy edycji siatka jest pełna od początku, więc pytanie „czy porzucić
  zmiany" przy każdym wyjściu byłoby męczące i nieprawdziwe;
- wyrównania kolumn z wiersza kresek są **zachowywane** — ktoś może je mieć
  wpisane, a ich zgubienie po przejściu przez kreator byłoby cichą utratą pracy;
- kreska pionowa wpisana w treść komórki wraca do siatki jako **treść**, nie
  jako koniec komórki. Parser komórek jest ścisłą odwrotnością zabezpieczania.

**Naprawiony przy okazji błąd, którego nikt nie zgłaszał:** tabela z jedną
kolumną nie była tabelą dla podglądu, dla listy elementów pod F7 ani dla naszego
eksportu HTML — choć nasz własny konwerter `2htm` robił z niej prawidłową tabelę
z komórkami nagłówka. Wyrażenie rozpoznające wiersz kresek wymagało co najmniej
dwóch kolumn zawsze. Teraz z wiodącą kreską pionową wystarcza jedna, a bez niej
nadal potrzebne są dwie, żeby pozioma linia „---" nie stała się tabelą.

Zmierzone przed wysyłką: 31 asercji na zbudowanej binarce przez refleksję,
11 kontroli, że pozioma linia, punktor i blok kodu nadal nie są tabelą, objaw
jednokolumnowy odtworzony na binarce sprzed zmiany, round-trip przez nasz
konwerter na próbce z kontrolą negatywną, lista testów 238/238.
**Niezmierzone: praca w oknie na jego czytniku** — o to został poproszony.

Do zrobienia w punkcie 2 zostaje jedno: **nawigacja po komórkach tabeli wprost
w edytorze**, czyli chodzenie strzałkami po komórkach w tekście dokumentu, jak w
Wordzie. Świadomie nie zaczynamy tego bez jego słowa, bo dziś tabelę czyta się
w tekście albo otwiera kreatorem, i może to wystarczać. Wzorce warte
podpatrzenia to nadal AccessiMD Nathana i QUILL „Table Studio", które
dodatkowo wczytuje CSV i TSV wprost do siatki.

## Stan punktu 3 na 28.08.2026 (5.0.36, commit 021f14f)

**Polskie litery w plikach ANSI: naprawione i wysłane.** To pierwsza z dwóch
połówek tego punktu.

Zmierzony defekt: detektor kodowania (biblioteka Ude) nie rozpoznaje polskiego
windows-1250 — na próbce z ogonkami zwraca brak rozstrzygnięcia z pewnością
zero. Kod spadał wtedy na UTF-8 i czytał plik jako UTF-8, zamieniając każdą
polską literę na znak zastępczy. Utrata była bezpowrotna, bo zapis utrwalał
uszkodzenie na dysku. Takie pliki produkuje starszy Notatnik, programy urzędowe
i wszystko z epoki DOS-a, więc trafiłby na to przy pierwszym starym dokumencie.

Naprawa nie zgaduje, a liczy: bajt niedozwolony w UTF-8 dowodzi, że plik UTF-8
nie jest. Bez rozstrzygnięcia detektora bierzemy systemową stronę kodową ANSI —
to zresztą zachowanie, które podręcznik opisywał dla pustego ustawienia
YieldEncoding. Poprawny UTF-8 i czysty ASCII idą jak dotąd na UTF-8 z BOM.

Dowody, których nie ma sensu powtarzać: `testy/pomiar_kodowania_ansi.cs` przez
refleksję na zbudowanej binarce 20/20, z kontrolami pozytywnymi na pliku
rosyjskim, niemieckim i czysto angielskim; `testy/kontrola_negatywna_kodowanie.sh`
buduje binarkę sprzed zmiany, odtwarza na niej objaw (13/16) i przywraca nową;
`weryfikuj_liste_testow.py` 197/197. Rozdział 19 listy testów, 7 punktów.
**Niezmierzone: odczyt na jego maszynie** — wynik zależy od ustawień
regionalnych Windows, więc został o to poproszony wprost.

### Druga połowa punktu 3: końce wiersza — CZEKA NA JEGO DECYZJĘ

ZROBIONE 28.08.2026 w 5.0.37, po jego decyzji: „jednak bym był za tym, żeby
zapisywał w Windows".

Pomiar zmienił zakres tej roboty i warto to pamiętać: zapis **już** zamieniał
końce wiersza na Windows, a samo pole edycyjne normalizuje CRLF i CR do jednego
LF przy wczytaniu pliku. Czyli zamówiona funkcja działała, zanim o nią poprosił,
a „edycja pliku z wierszami Linuxa" w sensie zachowania ich w trakcie pracy jest
niemożliwa bez przepisania kontrolki. To rozstrzygnęło też jego własne wahanie,
czy taką możliwość dawać — odpowiedź przyszła z maszyny, nie z dyskusji.

Dołożone zostało to, o co prosił osobno: Alt+Z naciśnięty dwa razy mówi rodzaj
końców wiersza, jaki plik miał na dysku (Windows, Unix, Macintosh albo mixed).
Rodzaj trzeba zapamiętać w momencie odczytu z dysku, bo po przypisaniu tekstu do
kontrolki jest bezpowrotnie stracony.

Osobnej komendy zamiany **nie ma i nie jest potrzebna** przy jego decyzji: zapis
i tak zawsze robi Windows. Zapis na życzenie z końcami Linuxa albo Maca zostaje
w eksporcie pod Alt+Shift+E.

Kodowanie zapisu, jego decyzja „Dobrze, czyli UTF-8": plik w starej
jednobajtowej stronie kodowej po zapisaniu jest UTF-8, więc raz otwarty tutaj
przestaje być pułapką na polskie litery. UTF-16 i UTF-32 zostają swoje, bo
konwersja jest bezpieczna tylko w jedną stronę, a zamiana szerokich zmieniłaby
rozmiar pliku, który ktoś wybrał świadomie.

Do rozważenia **po** jego odpowiedzi, jeśli sam o to poprosi: komenda wprost
przełączająca kodowanie bieżącego dokumentu i zapisująca plik. Dziś Alt+Shift+Y
zmienia interpretację otwartego tekstu, a Alt+Shift+E eksportuje do nowego
pliku — obie rzeczy istnieją, ale żadna nie robi tego w jednym kroku.

## Czego brakowało przed punktem 2 (zamknięte)

Tabele nie były z nim omówione na tyle, by je kodować: sposób wpisywania, sposób
chodzenia po gotowej tabeli i to, co ma mówić czytnik. Wszystko trzy ustalone
w rozmowie 28.08.2026 — patrz sekcja wyżej.

## Stan punktu 2 na 29.08.2026 (5.0.39, wiersz zero)

**Zgłoszenie Kasperczaka z 28.08 o 23:53, które nie miało pozycji w kolejce:**
„czy on dalej to liczy od wiersza zero? Bo to tak chyba nie powinno być, że wiersz
zero." Miał rację i jest to domknięcie tego, co przy 5.0.37 zgłosiliśmy mu sami
jako rzecz nieudaną.

Pomiar na żywym czytniku pokazał, że objaw był gorszy, niż opisywaliśmy: każdy ruch
strzałką dawał DWA głosy — najpierw obiekt wiersza mówił „Wiersz 0", potem komórka
podawała ten sam wiersz drugi raz i już od jedynki. Czyli jeden ruch, dwie
sprzeczne numeracje.

Przyczyna poprzedniej porażki była w NASZYM kodzie, nie w ograniczeniu WinForms:
własne `Clone()` budowało wiersz bez komórek, stąd wywalanie okna. Poprawka to
`return base.Clone()`. Wyciszamy `Name`, `Value`, `Description` oraz podmieniamy
rolę na `Pane` — przy roli wiersza czytnik ogłasza samo słowo „wiersz", choć nazwa
jest pusta. Wszystko zmierzone, nie wywnioskowane: cztery warianty na żywym NVDA,
w tym ukrycie nagłówków wierszy, które NIE pomaga.

Ustalenie na przyszłość: `RowTemplate` siedzi w `addTableGrid`, czyli w jedynym
miejscu tworzącym siatkę, więc każde następne okno z siatką dostaje to za darmo.

**Druga rzecz z tej samej wiadomości** („możliwość zapisu i wyboru innych wierszy
typu Mac i Linux powinna być, ale to chyba gdzieś tam przy zapisie tylko"): ta
droga już istnieje pod Alt+Shift+E i to ją mu wskazaliśmy. Ale zapisywała plik
w jednobajtowej stronie kodowej Windows, nie w UTF-8 — czyli kopia robiona DLA
Linuksa miała polskie litery w kodowaniu, którego nikt poza polskim Windowsem nie
odczyta. Program przeczył sam sobie i swojej własnej decyzji o UTF-8 z tego samego
dnia. Naprawione; pozycja „asc" zostaje jednobajtowa świadomie, bo i tak zdejmuje
ogonki.

Z punktu 2 zostaje nadal JEDNA rzecz: chodzenie strzałkami po komórkach tabeli
wprost w tekście dokumentu. Świadomie nie zaczynamy bez jego słowa — zapytany
o to dwa razy, ostatnio przy tej wysyłce.


## Jeden ruch, jeden glos w siatce (5.0.40, 29.08.2026)

Kasperczak przyslal WLASNY zapis tego, co slyszy: "gh, column 1, row 2 / gh /
zaznaczony". Czyli na jedno nacisniecie strzalki lecialy trzy rzeczy - nasza
wypowiedz, tresc komorki drugi raz i stan. Przy pustej komorce zamiast powtorki
bylo "(zerowy)".

Przyczyna: nadpisalismy NAZWE obiektu dostepnosciowego komorki i uznali temat za
zamkniety. Czytnik sklada wypowiedz takze z WARTOSCI, OPISU i STANU. Tresc
wracala jako wartosc, a "zaznaczony" bralo sie ze stanu.

Rozstrzygniete WARIANTAMI na zywym NVDA, nie zgadywaniem (harness h2.cs, szesc
uruchomien obok siebie). Wynik, ktorego nie dawalo sie przewidziec z kodu:
zdjecie samego zaznaczenia (Selected) daje GORZEJ - czytnik zaczyna mowic
"niezaznaczony". Slowo znika dopiero, gdy komorka przestaje deklarowac
ZAZNACZALNOSC (Selectable). W siatce z jedna komorka biezaca ta informacja i tak
nic nie niesie.

Osobno zmierzone i JAWNIE zgloszone mu jako "tak ma byc": ruch DOKLADAJACY
wiersz daje dwie wypowiedzi, ale to dwie ROZNE komorki (potwierdzenie zapisanej
i ogloszenie nowej). Zmierzone dwoma wariantami: przepisywanie naglowkow wierszy
nie ma z tym nic wspolnego.

Poprawiony podrecznik: obiecywal naglowki wierszy mowiace "Header, Row 1,
Row 2", a te sa CICHE od 5.0.39 - mowi komorka. Ten fragment klamal przez jedna
wersje.

Zapisane na pozniej z jego wlasnej decyzji ("nie musi byc w tej chwili"):
usuniecie funkcji spod Ctrl+T (Text Convert), ktora mowi "Unexpected Event".
Klawisz ma zostac wolny, byc moze pod tlumacza.

## Przypisy w rozdziale, F2 w siatce, Ctrl+T (5.0.41, 29.08.2026)

Trzy ustalenia z jego nocnej rozmowy 29.08 01:39-02:04. Żadne z nich NIE MIAŁO
pozycji w kolejce — to jego decyzje, które poprzednia iteracja zapisała jako
„ustalenia na później". Wykonane, bo żadna nie zależy od otwartego pytania
o układ klawiszy.

**Przypis może wylądować na końcu rozdziału, nie tylko dokumentu.** Jego powód,
własnymi słowami: plik z wieloma rozdziałami będzie potem dzielony na osobne
pliki, więc treść przypisu musi zostać przy swoim rozdziale. Wybór jest w tym
samym okienku co treść, bo tak poprosił. Koniec rozdziału liczymy tym samym
helperem, którego używa przestawianie sekcji, żeby oba rozumiały rozdział
identycznie.

Pozycja „koniec rozdziału" nie pojawia się, gdy dokument nie ma nagłówków. To
decyzja podjęta bez pytania go i zgłoszona mu wprost: martwa pozycja jest dla
niewidomego gorsza niż jej brak, bo wybiera ją i czeka na skutek, który nie
nadejdzie.

**F2 w siatce nie mówi już o edytowaniu.** Zmierzone na żywym NVDA: czytnik
mówił „Formant edytowania, pole edycji". To była NAZWA kontrolki edycyjnej
tworzonej przez WinForms — nie widać jej w kodzie, bo nigdzie jej nie
ustawialiśmy. Teraz kontrolka dostaje nazwę ze współrzędnych komórki. Rodzaj pola
zostaje celowo: to jedyna informacja mówiąca niewidomemu, że może zacząć pisać.

**Ctrl+T zwolniony**, funkcja zeszła do menu jak Text Combine. Przy okazji
znaleziona przyczyna jego „Unexpected Event": komenda czytała każdy wiersz
dokumentu jako ścieżkę pliku, a zdanie z cudzysłowem oraz wiersz tabeli Markdown
wywalają Path.GetDirectoryName. Program przewracał się na dokumencie, który sami
uczymy go tworzyć kreatorem tabeli. Ten sam kod obsługuje Text Combine, który
miał zostać, więc zabezpieczone są oba wejścia, nie tylko zdjęty klawisz.

### Co zostaje otwarte

**Układ klawiszy przypisów.** Świadomie nie przestawiany. Ustalenie z nocy
(Alt+F6 wstawia, Ctrl+F6 i Ctrl+Shift+F6 nawigują) wyklucza się z jego własnym
pomysłem na pokrętło, w którym przypisy stają się jedną z pozycji i nie
potrzebują własnej pary klawiszy. Zapytany, którą drogą idziemy, z radą na
zostawienie klawiszy do czasu pokrętła — inaczej przestawialibyśmy je dwa razy.

**Pokrętło nawigacyjne** (Alt z Page Up i Page Down chodzi po elementach, osobny
klawisz przestawia rodzaj) to jego pomysł i osobny temat, nie punkt tej mapy.
Wchodzi dopiero po jego decyzji, bo przestawia klawisze kilku istniejących
funkcji naraz.

## Litery dostępu we wszystkich okienkach (5.0.42, 29.08.2026)

Jego decyzja z 12:00:31 ("Skróty alt-litera w oknach dialogowych wprowadzamy.
Enter zatwierdza, Esc anuluuje, czyli jak u niego") plus dwa uściślenia z 12:05
("Tak." i "We wszystkich."). Zrobione w całym programie naraz.

Skala okazała się inna, niż wyglądała z zewnątrz: wszystkie okna programu (75
wywołań przez Dialog.Input, MultiInput, Choose, Pick i pozostałe) budują pola
przez dwa wspólne helpery w Lbc.cs. Zmiana objęła więc te dwa miejsca, a nie
siedemdziesiąt kilka okien.

**Błąd niezgłoszony, który wyszedł przy pomiarze.** Okno ustawień kompilatora
prosiło o "&Name" i "&NavigatePart" — obie litery to N, więc jedno z tych pól
było klawiszem nieosiągalne. W oknie szukania plików pięć z sześciu pól zaczyna
się na F. Zmierzone na żywym WinForms: przy duplikacie fokus ląduje na kontrolce,
której użytkownik nie wskazał. Dlatego jawny ampersand wołającego jest teraz
PREFERENCJĄ, nie licencją na duplikat.

**Zasada, która z tego zostaje.** Litera dostępu ma jedno źródło prawdy na okno
(zbiór zajętych liter), a litery pól rozdaje się PO przyciskach — litera
przycisku jest wyborem świadomym, litera pola automatycznym, więc świadomy musi
wygrać. Pole bez litery jest lepsze niż dwa pola na jednym klawiszu.

**Czego nie da się przenieść z upstreamu.** Autor przemianował pola w swoim
okienku pisowni ("Correction", "In context"). My takiego okienka nie mamy —
pisownia pod F7 idzie przez Worda i jego własne okno. Nie ma czego przenosić.

**Skrypty JAWS** świadomie nietknięte: powiedział wprost, że nie testuje z
JAWS-em i nie potrzebuje ich w instalatorze przed upublicznieniem.

## Punkt 4: komentarze wewnętrzne (5.0.43, 29.08.2026)

Jego zlecenie z 28.08.2026 00:20 („A komentarze, nie da się robić Markdown
komentarzy zgodnych potem z Wordem? Jakaś tu ich sensowna obsługa?") i jego
własne rozstrzygnięcie z 00:24: „To na razie komentarze wewnętrzne, a później
komentarze dla Worda, ale to już mocno później".

Komentarz wewnętrzny to uwaga robocza dla autora. Zostaje w pliku Markdown,
a przy zamianie na Worda, stronę albo czysty tekst wylatuje.

**Składnia wybrana pomiarem, nie upodobaniem.** Markdown nie ma własnej składni
komentarza, więc kandydatów było trzech i każdy przeszedł przez nasze własne
konwertery, te z jego paczki:

- komentarz HTML zostaje komentarzem strony w podglądzie, a jego treści nie ma
  ani w pliku Worda, ani w wyjściu tekstowym — czyli dokładnie to, o co prosił;
- CriticMarkup **odpada**: i nasz podgląd, i Pandoc zostawiają go jako widoczny
  tekst, więc uwaga robocza wylądowałaby w gotowym dokumencie u czytelnika;
- komentarz odsyłaczowy znika z obu wyjść, ale działa tylko na początku wiersza,
  więc nie da się go wstawić w środek zdania.

**Jeden klawisz na dwie rzeczy**, jak w kreatorze tabeli: kursor stoi w
komentarzu, więc go poprawiamy; stoi gdziekolwiek indziej, więc wstawiamy nowy.
Tytuł okna mówi który tryb. Wyczyszczenie treści przy poprawce usuwa komentarz
i program to ogłasza. Skoki nie zawijają się na koniec dokumentu — jak zakładki.

**Kolizja klawiszy, której nie było widać w spisie skrótów.** On sam przeznaczył
na komentarze F9 („szkoda mi tego F9, wolałbym na komentarze albo inne funkcje
typu Tłumaczenie zostawić"), więc tak to najpierw zrobiliśmy. Pomiar na żywym
NVDA pokazał **ciszę**: klawisz nie robił nic przy zielonym buildzie. Goły F9
i Shift+F9 nie mają wpisu w `Hotkeys.ini`, ale są przechwytywane wprost w
`ProcessCmdKey_Helper` i obsługują skrypt JAWS czytający tekst do końca.
Nie zabieramy ich bez jego słowa. Układ: Alt+F9 wstawia i poprawia,
Control+Shift+F9 następny, Alt+Shift+F9 poprzedni, Control+Alt+F9 lista.

Zasada, która z tego zostaje: **wolny chord sprawdza się w dwóch miejscach** —
w spisie skrótów ORAZ w jawnych warunkach przechwytujących klawisze przed
tablicą menu. Sam spis skrótów pokazuje tylko część prawdy.

Zmierzone przed wysyłką: 43 asercje na zbudowanej binarce przez refleksję
(z sześcioma kontrolami negatywnymi tego, co komentarzem być nie może), 38
asercji kontroli negatywnej na kodzie sprzed zmiany, żywy NVDA 8/8 z kontrolą
negatywną na pliku nie-Markdown, lista testów 352/352. **Niezmierzone: praca
w jego dokumentach i eksport do Worda na jego maszynie** — o to został
poproszony wprost, punkt 26.8 listy testów.

Komentarze zgodne z Wordem zostają na później, jak sam ustalił.
## Stan punktu 5 na 29.08.2026 (5.0.44) - porzadkowanie skrotow ruszylo

Punkt 5 mapy zaczal sie od jego wlasnej **mini korekty naszej listy skrotow do
wyrzucenia** (Telegram 29.08.2026 13:24-14:27). Kierunek podal jednym zdaniem:
„usuwamy te wszystkie wyrownania, wciecia rozumiane jako bogate formatowanie
strony i tak dalej. W ten sposob edytor stanie sie czysto tekstowy i nie ma sensu
zeby skakac do czcionki, stylu i tak dalej".

**Usuniete w 5.0.44** (dziewiec komend, kazda na czterech warstwach kodu):

- skoki po zmianie wyrownania (Control z nawiasami kwadratowymi),
- skoki po zmianie stylu, czyli pogrubienia i kursywy (Control z ukosnikiem),
- skoki po indeksie gornym i dolnym (Control+F2 i Control+Shift+F2),
- skoki po zmianie kroju pisma (Control z myslnikiem),
- obliczanie daty z kalendarza (Control+Shift ze srednikiem), razem z metoda
  i z czterema polami, ktore zapisywala do pliku ustawien uzytkownika.

**Granica, ktora postawilismy sami i zglosili mu wprost:** zostaja OKNA, ktore
formatowanie USTAWIAJA (wyrownanie, styl, indeks, krój pisma) oraz dwie komendy
PYTAJACE, co jest pod kursorem. On kazal usunac SKOKI, bo w Markdownie nie ma po
czym skakac; ustawiacze dotykaja plikow RTF, ktore program nadal otwiera i
zapisuje. Zapytany w punkcie 27.5 listy testow, czy maja pojsc razem z reszta.

**Sprostowanie wobec wlasnej wczesniejszej wypowiedzi:** powiedzielismy mu, ze
program „nie ma szans wspierac bogatego formatowania". To bylo nieprecyzyjne i
zostalo sprostowane w tej samej rozmowie: program formatowanie UMIE, bo obsluguje
RTF. Usuwamy te komendy jako NIEPOTRZEBNE dla kierunku Markdown, nie jako
niemozliwe. Warto tego pilnowac przy pisaniu opisow w menu.

**Zostaje w tym punkcie, wprost z jego korekty:** kalkulator pod Control ze
znakiem rowna sie („Zostawiamy."), szablony pod Control+Shift ze znakiem rowna
sie (wycofal wlasne „wyrzucamy" po wyjasnieniu, czym sa - plus dlug: nauczyc go
tego osobno), szukanie pasujacego nawiasu oraz skoki po znacznikach HTML („jakby
ktos robil cos w HTML, to by sie przydalo").

**Wieksza rzecz, ktora z tej rozmowy wynika i NIE jest porzadkowaniem:** chce
nawigacji po elementach Markdown jako osobnych komend, zeby zwykle szukanie
zostalo wolne na jego frazy. Rozstrzygnal tez spor o model: „jezeli moga byc
skroty klawiszowe i na nie starczy miejsca no to wlasciwie dlaczego by z tych
skrotow nie skorzystac" - czyli OSOBNE KLAWISZE, a pokretlo albo podmenu dopiero
wtedy, gdy miejsca zabraknie. To rozstrzyga rowniez otwarty spor o uklad klawiszy
przypisow. Klawisze zwolnione ta wersja sa naturalnym miejscem na te komendy;
sam to zauwazyl slowami „czesc z nich to bardzo dobre miejsca na przyszle rzeczy".

**Dalsze jego decyzje z tej samej rozmowy, jeszcze NIE wykonane** (nie mialy
pozycji w kolejce, bo miala pelna): przeniesienie listy folderow na Shift+F4
(ustepuje mowienie tytulow okien, bo liste okien daje F4), folderow systemowych
na Control+Shift+F4, zamykania wszystkich okien poza biezacym na Control+Shift+W,
zakladek z Control+K na Control+B z lista pod Alt+B, a Control+K na wstawianie
linku. Usuwanie powtarzajacych sie linii ZOSTAJE, klawisz nierozstrzygniety.

Zmierzone przed wyslaniem: kontrola negatywna 107 asercji na kodzie i binarce
sprzed zmiany (rewizja bc3fe19 podana jawnie, z pomiarem napisow w binarce w obu
kodowaniach), lista testow 405/405, audyt skrotow 204 komendy i zero
rozbieznosci, oraz szesc sond wczesniejszych wersji nietknietych.
**Niezmierzone: jego maszyna i jego czytnik.**

## Punkt 5 dalej: zakladki, link, rodzina F4, Ctrl+T (5.0.45, 30.08.2026)

Wykonanie zlecenia hurtowego wlasciciela 1788041000816-5, czyli dwunastu decyzji
Kasperczaka z rozmow 28-29.08, ktore mial w rejestrze ustalen, ale nie mialy jak
trafic do wykonania (kolejka byla pelna). Piec rodzin klawiszy naraz.

**Usuniete calkowicie** (kazda na wszystkich warstwach: pole klasy, pozycja menu,
lista AddRange, handler, metoda pomocnicza, opisy mowione w trzech plikach):

- nawigacja po blokach kodu, Next Block i Prior Block (ustalenie edsharpng-4),
- pytanie o biezacy blok kodu, komenda „Block" pod Alt+B (edsharpng-4, -12),
- mowienie tytulow otwartych okien, „Windows Open" pod Shift+F4 (edsharpng-6),
  razem z metoda WindowsOpen, ktorej jedyny wolajacy zniknal,
- Text Convert (edsharpng-1) - etap pierwszy w 5.0.44 zdjal tylko klawisz, a on
  kazal usunac funkcje: „zglasza blad Unexpected Event i jest zbedna". Text
  Combine, ktora dzielila z nia handler, ZOSTAJE w menu na jego zyczenie.

**Przeniesione** (kazde z asercja na zniknieciu starego chorda ORAZ na obecnosci
nowego - sam nowy chord nie odroznia przeniesienia od dopisania drugiej komendy):

- zakladka: Control+K na Control+B, lista zakladek Alt+K na Alt+B (edsharpng-4,
  -12). Powod jego wyboru: B jak bookmark, a K bylo potrzebne na link,
- zamykanie wszystkich okien poza biezacym: Control+Shift+F4 na Control+Shift+W
  (edsharpng-7), „jak w wielu programach",
- lista folderow: Control+0 na Shift+F4; foldery systemowe: Control+Alt+0 na
  Control+Shift+F4 (edsharpng-5, -8).

**Usuwanie zakladki bez wlasnego klawisza** (edsharpng-12): komenda Clear Bookmark
zeszla do MENU-ONLY, a nie zniknela. Jego decyzja brzmiala „usuwanie zakladki z
poziomu tekstu niepotrzebne, Delete na liscie wystarcza" - Delete na liscie
dzialal juz od 13.08, wiec klawisza nie trzymamy, ale droga w menu musi zostac dla
kogos, kto listy nie otwiera.

**Dodane: wstawianie linku pod Control+K** (edsharpng-9). Jedno okno, trzy rodzaje:
zwykly, graficzny, wewnetrzny. Przy wewnetrznym NIE pytamy o adres, tylko dajemy
liste naglowkow dokumentu, a kotwice liczy **BuildMarkdownAnchors, czyli ta sama
metoda, ktora wpisuje odsylacze do spisu tresci**. To nie jest wygoda, a wymog:
kotwica policzona wlasna implementacja specyfikacji prowadzilaby po eksporcie do
Worda albo HTML w nicosc, czego niewidomy nie ma jak zobaczyc. Naglowek, ktoremu
pandoc nie daje celu (same znaki przestankowe), nie wchodzi na liste. Obrazek bez
opisu jest ODRZUCANY - dla czytnika ekranu byl by niewidzialny.

**Granica postawiona samodzielnie i zgloszona mu jako pytanie:** ustalenie
edsharpng-5 wymienia takze „Control+F4 zmiana folderu". Control+F4 to dzis ZAMKNIJ
OKNO i tak jest w kazdym programie wielookienkowym Windows, nie tylko u nas.
Oddanie go folderom odebraloby standardowy klawisz zamykania, wiec tego NIE
zrobilismy; pytanie w punkcie 28.13 listy testow.

**ROZSTRZYGNIETE 30.08.2026 (5.0.47).** Ponizsza kolizja juz nie istnieje: on
sam kazal zdjac nagrywanie plyt („ALT+SHIFT+B nagrywanie plikow to na pewno
usun”), wiec Alt+Shift+B jest wolny pod liste zakladek z nazwa. Zostaje jego
wlasna uwaga, ze zakladki z nazwa moga byc TYM SAMYM co komentarze - to nadal
pytanie do niego. Zapis historyczny:

**Nierozstrzygnieta kolizja, ktorej celowo nie rozstrzygamy sami:** on proponowal
Alt+Shift+B na „zakladki z nazwa", a ten chord trzyma Burn to CD. Do tego sam
zauwazyl, ze zakladki z nazwa moga byc TYM SAMYM co komentarze, ktore dostal w
5.0.43. Dublowanie funkcji jest realnym ryzykiem, wiec to pytanie do niego, nie
nasza decyzja.

**Zwolnione tym razem:** Control+Shift+B, Control+Shift+K, Alt+K, Control+0,
Control+Alt+0, Control+T. Wszystkie zmierzone jako nienalezace do zadnej komendy
(chord „zwolniony", ktory nadal siedzi w CreateMenuItem, dawalby modalny alert na
starcie i po cichu zabijal komende).

Zmierzone przed wyslaniem: kontrola negatywna 125 asercji na kodzie, opisach i
BINARCE sprzed zmiany (rewizja ae113fd podana jawnie, napisy w obu kodowaniach),
weryfikator listy testow 485/485, audyt skrotow 200 komend i zero rozbieznosci,
nowa sonda linku 25/25 z kontrola roznicujaca na starej binarce (24/25 tam, bo
metody jeszcze nie bylo), piec sond wczesniejszych wersji nietknietych.
**Niezmierzone: jego maszyna i jego czytnik.**


## 5.0.47 (30.08.2026): skok przypisu na Control+Alt+K, koniec nagrywania plyt

Dwa jego polecenia z rana, oba jednoznaczne, wiec bez dopytywania.

**Skok kontekstowy przypisu: Alt+F6 -> Control+Alt+K.** To odpowiedz na nasze
pytanie z punktu 29.5 listy testow, doslownie: „Skok przypis tekst tekst przypis
robimy Alt-CTRL-ka to bedzie lepsza komenda chyba co? (...) Tak z ego F6
w przypisach bysmy rezygnowali". Cala rodzina F6 wychodzi wiec z przypisow i jest
wolna pod LISTE LINKOW (ustalenie edsharpng-37).

**PULAPKA, KTORA PRZESADZILA O KSZTALCIE TEJ ZMIANY - zmierzona, nie zgadnieta.**
`Util.Say` ma twardy warunek tlumiacy KAZDA mowe, gdy trzymane sa Control i Alt
naraz. Przeniesienie samego chordu dalo by komende, ktora WYKONUJE skok
W CISZY - kursor sie przenosi, a niewidomy nie slyszy ani tresci przypisu, ani
powodu odmowy. Zielony build tego NIE odrzuca. Zmierzone harnessem na zywych
modyfikatorach (`/mnt/c/tmp/ctrlaltk/h.cs`, 6/6, z kontrola ze sam Alt nie
tlumi), a potem na zywym NVDA. Wszystkie wypowiedzi tej komendy - tresc,
powrot ORAZ WSZYSTKIE ODMOWY - ida trybem globalnym, tak samo jak przesuwanie
sekcji na Control+Alt+strzalka.

**BLAD NIEZGLOSZONY, znaleziony tym samym pomiarem:** tryb opisywania klawiszy
pod Control+F1 MILCZAL dla KAZDEJ komendy z Control i Altem (skok przypisu,
lista komentarzy Control+Alt+F9, foldery systemowe Control+Alt+0). Czyli funkcja
sluzaca do NAUKI skrotow nie dzialala dokladnie przy skrotach najtrudniejszych
do zapamietania. Poprawione dla calej rodziny, nie tylko dla przypisow.

**Nagrywanie plyt (Alt+Shift+B) usuniete** - jego „to na pewno usun". Zwolniony
chord ma juz przeznaczenie: lista zakladek z nazwa. Zdjete na SZESCIU warstwach:
pole klasy, pozycja menu, lista AddRange, handler, metoda `BurnToCD` ORAZ helper
`GetPathsFromDocument`, ktory mial DOKLADNIE JEDNEGO wolajacego i zostalby
martwym kodem pytajacym uzytkownika o filtr rozszerzen. Plus opisy mowione
w trzech plikach i cztery miejsca w podreczniku.

**POMIAR, ktorego nie zamawial:** ta funkcja i tak byla MARTWA u kazdego, kto
zainstalowal EdSharpNG z naszej paczki - `EdSharp_Setup.iss` NIGDY nie pakowal
`Burn2CD.exe`, a komenda uruchamiala go z katalogu programu. Usuwamy wiec
funkcje, ktora nie dzialala, i tak to jest mu przedstawione.

**Granica postawiona samodzielnie:** `Util.GetExtensions` i
`Util.GetPathsWithExtensions` ZOSTAJA - maja innych klientow, miedzy innymi Path
List pod Control+Shift+P. Zdjecie ich razem z komenda dalo by zielony build
i ciche zepsucie tamtej funkcji. Zmierzone licznikiem wystapien, nie zalozone.
Pliki `Burn2CD.exe` i `Burn2CD.dll` zostaja w repo: nie sa przez nic wolane,
a czyszczenie historii to osobna decyzja.

Zmierzone przed wyslaniem: kontrola negatywna 547 66/66 (rewizja 8e5ac17 jawnie,
napisy w binarce w obu kodowaniach), ZYWY NVDA 13/13 z kontrola negatywna
Alt+F6 i pozytywna Alt+K w tym samym przebiegu, KONTROLA ROZNICUJACA na binarce
sprzed poprawki mowy (12/13 - odmowa „No footnotes!" byla tam CISZA),
weryfikator listy testow 515/515, audyt skrotow 199 komend i zero rozbieznosci
(bylo 200, czyli dokladnie jedna mniej), sondy wczesniejszych wersji nietkniete:
litery dostepu 14/14, kolizje liter 8/8, eksport kodowania 13/13, przypis
w rozdziale 18/18.

**OGRANICZENIE DOWODU, zgloszone mu wprost:** sonda NVDA puszcza klawisze
szybciej niz ludzka reka, wiec przy przebiegu bez poprawki zamilkla tylko odmowa,
a wypowiedzi po udanym skoku przeszly. U czlowieka, ktory trzyma klawisze dluzej,
zagrozone sa TAKZE one - dlatego tryb globalny zalozylem na wszystkie, a nie
tylko na te, ktore sonda odtworzyla jako cisze. Punkt 30.3 listy testow prosi go
o sprawdzenie wlasnie dluzszego przytrzymania.

**Niezmierzone: jego maszyna i jego czytnik.**

## 5.0.48 (30.08.2026): straznik pisania - skrot nie zabierze polskiej litery

Odpowiedz na jego pytanie z 11:45, doslownie: „Mam nadzieje, ze tu sie nie
dubluje, bo Alt+Ctrl to tez to samo co prawy Alt, a prawy Alt to polska
literka, ale mozna to jakos tak zrobic, zeby dzialalo prawidlowo."

**MIAL RACJE CO DO KLAWIATURY i to trzeba powiedziec wprost.** Na ukladzie
Polish (Programmers) prawy Alt JEST tym samym co Control z Altem, a dziewiec
polskich liter powstaje wlasnie tak. Zmierzone przez `VkKeyScanEx`, nie z
pamieci: a z ogonkiem na klawiszu A, c z kreska na C, e z ogonkiem na E,
l z kreska na L, n z kreska na N, o z kreska na O, s z kreska na S, z z kreska
na X (nie na Z, jak podpowiada intuicja) i z z kropka na Z.

**W TYM MIEJSCU KOLIZJI JEDNAK NIE MA.** K, F9 ani strzalki nie tworzy zadnej
polskiej litery, wiec Control+Alt+K, lista komentarzy pod Control+Alt+F9
i przesuwanie sekcji strzalkami nie zabieraja mu pisania. Potwierdzone TAKZE na
zywym programie, nie tylko na ukladzie: wszystkie dziewiec liter wpisanych
prawym Altem trafilo do dokumentu, a Control+Alt+K w tym samym przebiegu nadal
skakal do przypisu i mowil.

**JEGO „mozna to jakos tak zrobic, zeby dzialalo prawidlowo" wziete szerzej niz
jako pytanie o jeden klawisz.** `CreateMenuItem` dostal STRAZNIKA
(`Util.IsTypingChord`): przy budowie menu pyta uklad klawiatury, czy dany chord
WPISUJE znak, i odmawia postawienia na nim komendy z jawnym komunikatem.

DLACZEGO KOD, A NIE SAMA OSTROZNOSC PRZY WYBORZE KLAWISZA - to jest sedno.
Zmierzone roznicujaco: zbudowalem CELOWO wersje z komenda na Control+Alt+E
i litera e z ogonkiem PRZESTALA wchodzic do dokumentu (osiem liter z dziewieciu,
brakowalo dokladnie tej jednej). Build byl zielony, menu wygladalo dobrze,
istniejacy alarm o duplikacie NIE padl, bo to nie kolizja z inna komenda. Taka
wada wychodzi dopiero wtedy, gdy niewidomy nie moze napisac slowa. Po wlaczeniu
straznika ta sama wersja mowi przy starcie „Cannot assign Control+Alt+E to
Footnote List" - zmierzone na zywym NVDA.

**Straznik pyta UKLAD, a nie ma listy liter w kodzie.** Lista bylaby prawdziwa
tylko dla polskiej klawiatury. Kontrola pozytywna sondy na ukladzie czeskim:
zero zajetych chordow, czyli straznik nie odezwie sie tam, gdzie nie ma o co.

**GRANICA, postawiona swiadomie:** straznik obejmuje TYLKO Control+Alt (z
Shiftem i bez). Chordy z samym Control albo samym Alt nie wpisuja znakow,
a rozszerzenie na nie odebralo by nam wszystkie normalne skroty. Straznik zbyt
szeroki bylby GORSZY niz jego brak: zdjalby dzialajace klawisze przy zielonym
buildzie, a alarm zobaczylby dopiero on przy starcie. Osobne asercje pilnuja,
ze Control+Alt+K, Control+Alt+F9, Alt+K i Control+S nadal zyja, oraz ze straznik
stoi PO alarmie o duplikacie i PRZED `hashKey.Add` (postawiony pozniej
przepuscilby chord, ktory ma blokowac).

**DWIE WLASNE WADY SOND, rozstrzygniete pomiarem, nie odruchem.** Pierwsza
wersja sondy pytala „co daje ten klawisz przy AltGr" przez `ToUnicodeEx`
i zwracala wyniki WEWNETRZNIE SPRZECZNE (AltGr+L jako „B" przy poprawnym
AltGr+O). Nie zgadywalem, ktory zwrot jest prawdziwy - odwrocilem kierunek
pytania na `VkKeyScanEx`, ktory dla ZNAKU zwraca klawisz i modyfikatory. Druga:
dwie asercje weryfikatora listy testow padaly, bo szukane frazy sa w tekscie
ZLAMANE zawinieciem wiersza - ta sama klasa bledu co „Burn to CD" w 5.0.47.
Poprawione SONDY, nie tekst.

**SPROSTOWANE TRZY PRZESTARZALE ASERCJE, ktore zaczely KLAMAC.** Kontrola 546
pilnowala, ze skok przypisu ZOSTAJE na Alt+F6, a kontrole 544 i 545, ze
wstawianie przypisu siedzi na Control+F6, Windows Open na Shift+F4 i zamykanie
pozostalych okien na Control+Shift+F4. Wszystkie te klawisze przeniosl ON SAM
w 5.0.45 i 5.0.46, wiec sondy dawaly FAIL przy poprawnym kodzie i zaciemnialy
prawdziwe regresje w tym samym przebiegu. Teraz 546 daje 30/30, 545 125/125,
544 107/107.

Zmierzone przed wyslaniem: kontrola negatywna 548 23/23 (rewizja 2460a49 jawnie,
z kontrola roznicujaca na binarce 5.0.47, ktora straznika NIE MA, i kontrola
pozytywna, ze ta sama binarka ma skok przypisu), pomiar na zywym programie 13/13
(dziewiec liter + kontrola negatywna, ze Control+Alt+K nic nie wpisuje +
kontrola pozytywna, ze nadal skacze), sonda ukladu 8/8 z kontrola negatywna
(znak chinski zwraca -1, czyli sonda umie odpowiedziec NIE), weryfikator listy
testow 527/527 (bylo 515, doszlo 12 asercji), audyt skrotow 199 komend i zero
rozbieznosci, sondy wczesniejszych wersji na nowej binarce: komentarze 43/43,
edycja tabeli 31/31, eksport kodowania 13/13, kolizje liter 8/8, przypis
w rozdziale 18/18, bramki przypisu 13/13, podglad przypisow 33/33.

**Niezmierzone: jego uklad klawiatury i jego czytnik.** Mierzylismy na Polish
(Programmers), bo taki jest ustawiony u nas. Jesli uzywa ukladu maszynistki,
litery siedza na innych klawiszach - straznik i tak zapyta jego uklad, ale to
jego maszyna rozstrzyga. Punkt 31.5 listy testow prosi go wprost o zgloszenie,
gdyby przy starcie zobaczyl komunikat o skrotach, bo to znaczylo by, ze straznik
zlapal za duzo.

## 5.0.49 (30.08.2026): nawigacja po wyroznieniach i listach Markdown

Zamkniete wahanie, ktore iteracja 14 slusznie zostawila jako pytanie A/B.
Odpowiedzial 15:41 jednym zdaniem: „Control/pogrubienie i podkreslenie razem,
control myslnik, skakanie po listach", a minute pozniej dolozyl, gdzie skok ma
stawac: „Na poczatek kazdej nowej listy, bo po elementach mozna chodzic strzalka
w dol". To ustalenia edsharpng-45 i edsharpng-46.

Wybral wariant A, ktory rekomendowalismy, i powod jest jego wlasny: w Markdownie
ukosnik nie ma zadnej funkcji, wiec nic mu nie koliduje znaczeniowo. Wariant B
(Control+Shift z myslnikiem) odpadl dlatego, ze myslnik jest u niego zajety pod
listy, a dwie rodziny nie zmieszcza sie na jednym klawiszu.

CO WESZLO

- Control z ukosnikiem i Control+Shift z ukosnikiem: nastepne i poprzednie
  WYROZNIENIE. Jedna komenda na pogrubienie ORAZ pochylenie, bo ustalenie
  edsharpng-42 mowi, ze osobnej komendy dla kursywy nie chce.
- Control z myslnikiem i Control+Shift z myslnikiem: POCZATEK nastepnej i
  poprzedniej listy. Nie kazda pozycja, tylko poczatek.

TRZY RZECZY, KTORE PARSER MUSI ODSIEWAC, kazda zmierzona osobno na binarce
(sonda /mnt/c/tmp/emfazy/h.cs, refleksja, 27 asercji, z tego szesc negatywnych):
punktor listy („* pozycja" zaczyna sie gwiazdka, ktora NIE otwiera wyroznienia),
mnozenie w zdaniu (2 * 3) oraz podkreslnik w srodku slowa (nazwa_z_podkresleniem
to jedno slowo, nie kursywa). Bloki kodu pomija ten sam parser ogrodzen, co
naglowki i przypisy. Bez tych trzech bramek komenda skakalaby na smieci przy
zielonym buildzie i wygladalaby na dzialajaca.

KURSOR STAJE NA TRESCI, NIE NA GWIAZDCE. To jego wymog z tej samej rozmowy
(„sam pogrubiony fragment"). Mowa czyta tresc, a POTEM nazywa rodzaj: bold,
italic albo bold italic - liczba znacznikow rozstrzyga, tak jak w Markdownie.
Osobne pola Start i TextStart w parserze pilnuja tej roznicy, a asercja sondy
sprawdza, ze kod stawia kursor na TextStart. Postawienie go na Start dalo by
komende, ktora dziala, a czytnik czytalby niewidomemu gwiazdki.

LICZBA POZYCJI W MOWIE LISTY jest po to, zeby skok cos znaczyl: sam punktor nie
powiedzialby mu, gdzie stanal. Poczatki list bierzemy z TEGO SAMEGO parsera,
ktory zasila liste elementow w podgladzie (MarkdownReview_ParseText), wiec obie
funkcje nie moga sie rozjechac. Wziecie tam pola `items` zamiast `lists` dalo by
komende chodzaca po kazdym punkcie, czyli dokladnie to, czego nie chcial -
osobna asercja kontroli 549 tego pilnuje.

## SPROSTOWANIE: lista linkow pod Control+F6 NIGDY NIE ISTNIALA W KODZIE

Zglosil 15:54, ze „dziwnie zachowuje sie Control+F6, tak jakby mi przechodzil
z jednego pliku do drugiego". W odpowiedzi dostal diagnoze, ze to systemowy
skrot Windows przechodzenia miedzy okienkami dokumentow „wygrywa z naszym
przypisaniem".

**TA DIAGNOZA BYLA FALSZYWA i zostala mu sprostowana.** Zmierzone: w EdSharp.cs
nie ma ANI JEDNEJ komendy przypisanej do Control+F6 - zero trafien w
`CreateMenuItem`, zero w handlerach, zero w Hotkeys.ini. Nie ma tez zadnej
metody listy linkow (`grep -i linkList` daje wylacznie zmienne lokalne modulu
Web, nic z menu). Czyli nie doszlo do przechwycenia naszego skrotu przez system:
funkcji po prostu jeszcze nie napisalismy, a Windows uzywa wolnego klawisza
zgodnie ze swoim zwyczajem. Jego objaw byl w 100 procentach poprawny, blad byl
w NASZEJ diagnozie.

Podrecznik utrwalal ten sam blad w dwoch miejscach („Control+F6 is now the list
of links"), wiec oba akapity poprawione. Lista linkow zostaje na Control+F6 jako
REZERWACJA (ustalenia edsharpng-37 i edsharpng-47) i przy jej pisaniu trzeba
bedzie odebrac klawisz systemowi - to prawdziwa robota, ktora dopiero przed nami,
a nie naprawa czegos zepsutego.

**Niezmierzone: jego maszyna i jego czytnik.** Sonda mierzy parser i binarke;
czy mowa brzmi u niego naturalnie i czy skok trafia tam, gdzie sie go spodziewa
w prawdziwym tekscie, rozstrzyga on.


## 5.0.51 (31.08.2026): rodzina Control+F6 odebrana systemowemu cyklowi okien

Kasperczak zapytal, skad w programie wzial sie tryb "Accessible Selection" pod
Control+Shift+F6, ktory "wlacza wylacza tryb", i dopisal, ze wczesniej tego nie
bylo, wiec musialo pojawic sie niedawno.

**Zadnego takiego trybu w programie nie ma i nigdy nie bylo.** Napisu
"Accessible Selection" nie ma w naszym kodzie, w zbudowanej binarce (obie
kodowania, zero trafien), w samym NVDA ani w jego dodatkach - sprawdzone u
zrodla, nie z pamieci. To nazwa, ktora czytnik zlozyl sam, gdy fokus wskoczyl do
drugiego okna dokumentu.

Prawdziwy sprawca to WinForms. Kontrolka MdiClient obsluguje Control+F6 i
Control+Shift+F6 jako WBUDOWANY cykl okien potomnych i bierze te klawisze
ZANIM zobaczy je nasza tablica skrotow. Zmierzone na zywym NVDA: przy jednym
otwartym pliku klawisz nie robi nic (stad wrazenie, ze "tego nie bylo"), a przy
dwoch cicho przelacza dokument - identyfikator pola edycji zmienil sie
9767026 na 8980688. Nic sie zatem nie zepsulo ostatnio: zachowanie siedzialo tam
od zawsze i ujawnialo sie tylko przy wiecej niz jednym otwartym pliku.

Jego ustalenie edsharpng-47 mowi wprost, ze systemowe przechodzenie miedzy
okienkami dokumentow na tym klawiszu nalezy odebrac, bo Control+F6 ma dostac
liste linkow. Odebrane teraz, przed pisaniem samej listy: oba chordy rodziny sa
przechwytywane i nie robia nic. Blokada USTEPUJE naszej wlasnej komendzie -
gdy chord trafi do tablicy skrotow, obsluga wraca do menu. Bez tego kroku lista
linkow po prostu by nie zadzialala, bo klawisz zabieralby ja system.

Do zmiany okna sluzy Control+Tab i Control+Shift+Tab, a lista okien jest pod F4
i to sie nie zmienia. Zmierzone po naprawie: oba chordy rodziny Control+F6 nie
ruszaja okna (identyfikator staly 10094706 przez oba nacisniecia), a Control+Tab
nadal je zmienia (10094706 na 8653004).

**Niezmierzone: jego maszyna i jego czytnik.** U nas klawisz milczy i okno stoi;
czy u niego zniknal takze komunikat czytnika, rozstrzyga on.


## 5.0.53 (31.08.2026): LISTA LINKOW pod Control+F6

Wykonanie trzech jego ustalen naraz, ktore od 30.08 czekaly bez pozycji
w kolejce: edsharpng-37 (Control+F6 to lista linkow, analogicznie do F6 dla
naglowkow, listy tabel nie robimy), edsharpng-38 (na liscie Control+C kopiuje
link jako Markdown, Control+Shift+C jako link z formatowaniem, F2 otwiera
okienko edycji tytulu i adresu) oraz edsharpng-47 (systemowe przechodzenie
miedzy okienkami dokumentow na tym klawiszu nalezy odebrac).

KOLEJNOSC MIALA ZNACZENIE. Klawisz odebralismy WinFormsowi w 5.0.52, przed
napisaniem samej listy - i to nie byla nadgorliwosc: MdiClient bral oba chordy
rodziny ZANIM zobaczyla je nasza tablica skrotow, wiec lista otwarta na tym
klawiszu po prostu by sie nie pokazala. Teraz blokada USTEPUJE naszej komendzie
(warunek `hashKey.ContainsKey`), wiec Control+F6 idzie do menu, a
Control+Shift+F6 nadal jest przez nia trzymany i nic nie robi.

CO WESZLO

- Nowa komenda `Link List ...` w menu Nawigacja, na Control+F6.
- Parser `GetMarkdownLinks`: postac `[tresc](adres)` razem z obrazkami oraz gole
  adresy w trzech postaciach (`<adres>`, `http...`, `www...`). Zrodlem prawdy sa
  TE SAME dwa pomiary, ktore zasilaja liste elementow w podgladzie
  (`MarkdownReview_FindInlineLinks` i trzy wyrazenia regularne) - gdyby lista
  miala wlasny parser, po pierwszej poprawce obie funkcje by sie rozjechaly.
- Trzy klawisze na liscie: Control+C (postac Markdown), Control+Shift+C (RTF
  z polem HYPERLINK, czyli klikalny odsylacz po wklejeniu w Wordzie), F2
  (okienko edycji tytulu i adresu, po zmianie lista wraca z aktualnymi
  przesunieciami).

CZTERY RZECZY, KTORE MUSIALY BYC ZROBIONE, INACZEJ KOMENDA BYLA BY ZIELONA
I ZLA

1. **Lbc musi ODDAC nam Control+C i Control+Shift+C.** Okienko `LbcDialog` ma
   wlasna obsluge kopiowania z listy i skopiowalo by WIDOCZNY wiersz, czyli
   "Line 12. tresc, adres, link", zamiast samego odsylacza. Znacznik
   `edsharp-linklist` dziala tak samo jak `edsharp-filelist` przy listach plikow.
   Zaden build tego nie wylapie: kopiowanie by dzialalo, tylko kopiowalo by
   niewlasciwa rzecz.
2. **Kursor staje na TRESCI (`TextStart`), nie na nawiasie.** Postawienie go na
   `Start` dalo by komende, ktora dziala, a czytnik czytal by niewidomemu
   nawias kwadratowy.
3. **Wykrzyknik obrazka nalezy do zakresu odsylacza.** Bez tego F2 przepisalo by
   `[opis](plik.png)`, a wykrzyknik zostal by w tekscie osobno i obrazek zamienil
   by sie w zwykly link.
4. **Kropka konczaca zdanie nie jest czescia golego adresu.** Zakres bierzemy
   z `MarkdownReview_CleanUrl`, inaczej edycja zjadla by interpunkcje zdania.

GRANICA POSTAWIONA SWIADOMIE I ZGLOSZONA MU JAKO PYTANIE (punkt 33.14 listy
testow): lista NIE MA bramki "tylko pliki Markdown", choc taka bramka jest przy
wstawianiu linku, tabeli, przypisie i komentarzu. Powod: tamte komendy PISZA
skladnie Markdown, ktora w zwyklym pliku bylaby smieciem, a ta tylko CZYTA, a
goly adres w pliku .txt jest prawdziwym odsylaczem. Bramka podgladu ZOSTAJE, bo
w podgladzie kursor chodzi po tekscie przetworzonym i przesuniecia z pliku
zrodlowego nie maja tam pokrycia; linki daje tam lista elementow pod F7.

DOWODY

- `testy/pomiar_listy_linkow.cs` (refleksja na ZBUDOWANEJ binarce): 37/37, z tego
  szesc asercji NEGATYWNYCH (tekst bez adresu, adres w bloku kodu, odsylacz
  w bloku kodu, znacznik przypisu, nawias bez adresu, pusty tekst) i trzy
  POZYTYWNE, ktore dowodza, ze parser nie jest gluchy (adresy przed i po bloku
  kodu widoczne, adres pocztowy liczony, adres w nawiasach ostrych liczony RAZ,
  a nie dwa razy dwoma wyrazeniami).
- KONTROLA ROZNICUJACA na binarce: ta sama sonda odpalona na EdSharpNG.exe
  wersji 5.0.52 (staging `C:\EdSharp`, sha256 eff4701b4c5bbb3e...) konczy sie
  wyjatkiem "brak GetMarkdownLinks". Zielony wynik na nowej binarce dopiero
  wtedy cos znaczy.
- `testy/kontrola_negatywna_553.sh`: 59/59, rewizja odniesienia e07ede4 podana
  JAWNIE. Cztery warstwy komendy, brak kazdej z nich przed zmiana, trzy kontrole
  pozytywne (sonda widzi rzeczy z 5.0.45, 5.0.49 i 5.0.52), trzynascie kontrol,
  ze nic innego nie znikneło, oraz kontrola waznosci sondy bramki: ta sama sonda
  MUSI zobaczyc bramke tylko-Markdown przy wstawianiu linku.
- PELNE ZBIORY CHORDOW przed i po: 204 kontra 205, `comm -23` puste (nic nie
  zniklo), `comm -13` to dokladnie `Control+F6`, `uniq -d` puste (zero
  duplikatow). Sama liczebnosc by nie wystarczyla.
- `testy/audyt_skrotow_vs_opisy.py`: 204 komendy z chordem, zero rozbieznosci,
  kontrola negatywna sondy PASS. Bylo 203 - dokladnie jedna wiecej.
- `testy/weryfikuj_liste_testow.py`: 567/567 (bylo 544, doszlo 23 asercje).
- Sondy poprzednich wersji na NOWEJ binarce: komentarze 43/43, edycja tabeli
  31/31, litery dostepu 14/14, kolizje liter 8/8, eksport kodowania 13/13,
  przypis w rozdziale 18/18, bramki przypisu PASS, podglad przypisow 33/33,
  wyroznienia i listy 27/27, wiersz kreskek 11/11, spis tresci 56/56.
- Kontrole negatywne poprzednich wersji: 544 107/107, 545 125/125, 546 31/31,
  547 66/66, 548 23/23, 549 69/69, 550 29/29.

TRZY PRZESTARZALE ASERCJE, KTORE ZACZELY KLAMAC, i to jest wazniejsze niz sam
zielony wynik. Kontrole 546 i 550 oraz weryfikator listy testow pilnowaly, ze
Control+F6 jest WOLNY - bo tak bylo do wczoraj. Po napisaniu listy dawaly FAIL
przy POPRAWNYM kodzie, zaciemniajac prawdziwe regresje w tym samym przebiegu.
Wszystkie trzy przepisane na to, czego naprawde chcemy: na Control+F6 stoi lista
linkow, przypis tam NIE wrocil, Control+Shift+F6 nadal wolny, a blokada MDI
ustepuje tablicy skrotow.

DWIE WLASNE WADY SOND, rozstrzygniete pomiarem, nie odruchem. Zakres `awk` po
ciele handlera nie domykal sie na wzorcu `/^\}$/`, bo w tym repo KAZDY wiersz
konczy sie znakiem CR - sonda lecialaby do konca pliku i lapala bramke z innej
komendy; dolozona asercja waznosci na dlugosc ciala. Druga: asercja szukala
`PickFile` w Lbc.cs, a ta metoda mieszka w EdSharp.cs. Poprawione SONDY, nie kod.

Niezmierzone: jego maszyna i jego czytnik. U nas parser i binarka sa zielone,
ale czy dlugi wiersz listy (tytul plus adres) czyta sie u niego wygodnie i czy
kolejnosc "tresc, adres, rodzaj" mu odpowiada, rozstrzyga on. Zapytany wprost
w punkcie 33.18.

## Punkt 5 (porzadkowanie): F9 zwolniony 03.09.2026 (5.0.63)

Jego decyzja: "z tego klawisza F dziewiec i skryptu raczej rezygnujemy i tak nie
bede go testowac z programem". Goly F9 i Shift+F9 nie naleza od tej wersji do
zadnej komendy.

Co ta zmiana domyka, a nie tylko usuwa: te dwa chordy byly przez cala historie
projektu PULAPKA POMIAROWA. Nie stalo ich w Hotkeys.ini ani w tablicy skrotow
menu, bo warunek lapal je WPROST w ProcessCmdKey_Helper, PRZED tablica. Sonda
pytajaca o spis skrotow pokazywala je wiec jako wolne, a proba zajecia ich
komentarzami dala klawisz, ktory nie robil NIC przy zielonym buildzie (lekcja
z 5.0.43). Po zdjeciu warunku ta rozbieznosc znika: zajecie F9 jest odtad
zwyklym dopisaniem pozycji menu.

Zdjeta jest OBSLUGA W PROGRAMIE, nie pliki. scripts/EdSharp.JSS z funkcja
SayAllTempFile i mapa klawiszy edsharp.jkm zostaja w repozytorium i w paczce -
to pliki autora oryginalu, ktore wracaja do rozmowy przy upublicznieniu
programu (ustalenie edsharpng-27: skrypty JAWS na razie nie w instalatorze).
Skasowanie ich byloby decyzja szersza niz jego zdanie i trudniejsza do cofniecia.

Razem z warunkiem zszedl helper COM.JFWRunFunction, ktorego JEDYNYM konsumentem
w calym programie bylo to czytanie. Zostawienie go byloby powtorzeniem bledu
z 5.0.44: osierocony helper przezyl tam usuniecie rodziny komend i przy nastepnej
pracy wygladal na dzialajaca droge. COM.JFWSay ZOSTAJE swiadomie - to nie sierota
po tej zmianie, a zywy kanal mowy dla uzytkownikow JAWS-a.

Zmierzone: sonda testy/pomiar_563.cs 16/16 na binarce przebudowanej od nowa,
kontrola negatywna tej samej sondy na binarce 5.0.62 konczy sie kodem 3.
kontrola_negatywna_563.sh 29/29, a na rewizji 434b6e1 oblewa na 7 asercjach.
Lista testow 599/599. Trzy podstawione bledy, i to jest najwazniejszy wynik tego
przebiegu: TRZECI Z NICH SONDA PRZEPUSCILA. Pytanie o ISTNIENIE POLA klasy nie
odroznia komendy zywej od zabranej, bo pole jest deklarowane osobno od
przypisania - podmiana CreateMenuItem na null dala zielone 12/12. Rozstrzyga
NAPIS CHORDU w binarce; po dolozeniu takich asercji ten sam blad jest lapany.

Niezmierzone: samo nacisniecie klawisza na zywym programie z czytnikiem, bo tu
nie ma ani NVDA, ani JAWS-a. Rozstrzyga jego maszyna.
