# Co usuwamy z EdSharpa — analiza

Stan na wersję 5.0.108 (16.09.2026). Plik powstał na polecenie Michała
Kasperczaka z 15.09.2026: „Przeanalizuj, co usuwamy z EdSharpa. Możesz to
wypisać do osobnego pliku w folderze."

Zasada, po której to jest posortowane: EdSharpNG ma być **czytelnym edytorem
tekstu i Markdowna dla osoby niewidomej**, a nie środowiskiem programisty
z 2005 roku. Oryginalny EdSharp Jamala Mazrui był jednym i drugim. Wszystko,
co niżej, wynika z tej jednej różnicy kierunku.

Liczby: 227 poleceń w `Hotkeys.ini`, 27 opcji konfiguracyjnych. To dużo za
dużo na paletę poleceń, którą realnie da się przejrzeć.

---

## 1. DO USUNIĘCIA — decyzja Michała już podjęta, jeszcze nie wykonana

To jest lista z sekcji „Do zrobienia" pliku zadań, pozycje wciąż otwarte.

### 1.1. Narzędzia sieciowe z menu Miscellaneous

Michał: „Usunięcie z Misc narzędzi typu Web Download (Alt+Shift+W), Web Client
Utilities (Alt+Shift+Space)".

**Stan faktyczny sprawdzony w kodzie 16.09.2026: tych dwóch poleceń w EdSharpNG
JUŻ NIE MA.** Ani w `EdSharp.cs`, ani w `Hotkeys.ini`. Zostały usunięte
wcześniej. Pozostała po nich tylko wewnętrzna metoda `DownloadFile`
(linia ~20635), która jest teraz używana do czegoś pożytecznego — pobierania
instalatora nowej wersji przy autoaktualizacji. Ta zostaje.

Do zrobienia: nic. Pozycję można odhaczyć.

### 1.2. Ustawiacze bogatego formatowania (spadek po RTF)

Michał: „Usunięcie Default Fonts i innych opcji z RTF bogato formatowanych."

Co jeszcze siedzi w kodzie i dotyczy formatowania, którego w Markdownie nie ma
jak zapisać:

- `FontDefault` — domyślna czcionka, własne okno pod Alt+Shift+Równa się.
  **Uwaga: tego bym NIE usuwał.** Czcionka i jej rozmiar to dla osoby
  słabowidzącej rzecz praktyczna, a nie ozdoba. To nie jest „opcja z RTF".
- `GetBaselineText` (linia 2721) — czyta indeks górny/dolny. Wywoływana już
  tylko z jednego miejsca (linia 6003, polecenie „Styles" pod Alt+ukośnik).
- Ustawiacze w menu Edit: Justify, Style, Baseline, Set Selection Font.
  Skoki po nich zostały usunięte 29.08.2026, same ustawiacze zostały —
  komentarz w kodzie (linia 1591) mówi wprost, że „o ich losie Kasperczak nie
  rozstrzygał". Teraz rozstrzygnął: idą.

**Propozycja:** usunąć Justify, Style, Baseline, Set Selection Font oraz
polecenie „Styles" (Alt+ukośnik). Zostawić `FontDefault` i jego okno.
Zostawić SAM ODCZYT plików .rtf — to nie formatowanie, to możliwość otwarcia
cudzego pliku.

### 1.3. Komentarze na starych klawiszach z rodziny F9

Michał: „Komentarze mają stare skróty klawiszowe. Te jeszcze z klawiszem
funkcyjnym F9. To trzeba usunąć, bo już są inne skróty."

Stan: `Alt+F9` (wstaw komentarz) i `Control+Alt+F9` (lista) zostały już zdjęte
13.09.2026, komentarz w kodzie linia 2041 to potwierdza. Skoki po komentarzach
zostały CELOWO — Michał wtedy powiedział wprost: „nie mówiłem, żebyś usuwał
nawigację".

Do zrobienia: nic. Pozycję można odhaczyć.

### 1.4. Rozstrzygnięcie: komentarze wewnętrzne kontra zakładki z nazwami

Michał: „Ustaliliśmy, że te komentarze to są inaczej mówiąc zakładki
z nazwami, chyba że to są komentarze, które mogą być odczytywane
z importowanego pliku z Worda. Możesz to dokładnie sprawdzić i mi powiedzieć.
To wtedy byśmy zostawili."

**Sprawdzone. Odpowiedź: to NIE to samo i komentarze trzeba zostawić.**

Powód: komentarze w EdSharpNG mają zakotwiczenie w TREŚCI (przywiązane do
fragmentu tekstu), a zakładki z nazwami mają zakotwiczenie w POZYCJI (numer
wiersza). Po dopisaniu akapitu w środku dokumentu zakładka nazwana zostaje na
starym numerze wiersza, komentarz idzie z tekstem.

Osobno: pliki .docx Worda naprawdę noszą komentarze recenzenta i EdSharpNG umie
je wczytać. Zlikwidowanie komentarzy zabrałoby możliwość przeczytania cudzej
recenzji dokumentu — a to jedna z rzeczy, po które sięga się w pracy
redakcyjnej.

**Propozycja: zostawić oba mechanizmy.** Warto natomiast poprawić nazwy w menu,
żeby było widać różnicę: „Comment" mówi w tej chwili tyle samo co „Named
Bookmark".

---

## 2. DO USUNIĘCIA — moja propozycja, decyzji jeszcze nie było

Wszystko w tym rozdziale to **propozycja do rozstrzygnięcia**, nie coś, co
zrobiłem.

### 2.1. Narzędzia programistyczne Pythona

- `PyDent` (Alt+lewy nawias) — przerabia kod Pythona z formatu nawiasowego na
  wcięciowy.
- `PyBrace` (Alt+Shift+lewy nawias) — to samo w drugą stronę.

To są narzędzia z czasów, gdy niewidomy programista miał problem z odczytem
wcięć. Dziś czytniki ekranu mówią poziom wcięcia same, a EdSharpNG ma do tego
własne polecenie (`Alt+I`, Indentation). Trzy klawisze i dwa dość duże kawałki
kodu na funkcję, której nikt tu nie używa.

`Infer Indent` (Alt+prawy nawias) natomiast **zostawiłbym** — rozpoznaje krok
wcięcia dokumentu i ustawia go, co jest przydatne przy cudzym pliku tekstowym,
nie tylko w kodzie.

### 2.2. Warstwa skryptów JScript .NET

Osobna biblioteka `EdSharp.dll` istnieje wyłącznie po to, by uruchamiać skrypty
w JScript .NET (polecenie „Run at Cursor", Shift+F5, oraz „Invoke Snippet"
w trybie wykonania). JScript .NET to język, który Microsoft porzucił lata temu.

Koszt: dodatkowy plik w instalatorze, dodatkowy krok w kompilacji, późne
wiązanie przez refleksję (linia ~20700), czyli klasa błędów, których kompilator
nie wyłapie.

Zysk: możliwość napisania skryptu w martwym języku.

**Propozycja: usunąć warstwę JScript, zostawić „Invoke Snippet" jako czyste
wklejanie tekstu.** To by usunęło jeden plik z paczki i cały mechanizm
refleksji.

Zastrzeżenie: jeśli Michał ma gdzieś własne skrypty .js do EdSharpa, to
zostaje — wtedy trzeba je wcześniej znaleźć.

### 2.3. Polecenia budowania kodu

- `Compile` (Control+F5), `Pick Compiler` (Control+Shift+F5),
  `Review Output` (Alt+Shift+F5), `Say Compiler` (Control+F9)
- opcje `CompileCommand`, `JumpPosition`, `AbbreviateOutput`
- cały mechanizm plików `<Kompilator>.ini` z osobną sekcją `[Options]`

To jest największy pojedynczy blok kodu, który nie służy edytowaniu tekstu.
Mechanizm per-kompilator komplikuje też okno ustawień (patrz „TRZECIA PUŁAPKA"
w `Ustawienia.cs`).

**Propozycja: usunąć.** Kto koduje, ten używa Visual Studio albo Codexa.

Zastrzeżenie: `Prompt Command` (Alt+F5) i `Go to Environment`
**zostawiłbym** — to zwykłe „otwórz mi wiersz poleceń tutaj", przydatne
niezależnie od kompilowania.

### 2.4. Rzeczy o niejasnym przeznaczeniu

- `Run` (menu File, bez skrótu) — uruchamia bieżący plik jako program. Przy
  pliku .md nie robi nic sensownego.
- `Text Combine` (menu, bez skrótu) — stracił chord 13.09.2026 i został
  w menu „na życzenie". Warto sprawdzić po miesiącu, czy był użyty choć raz.
- `Evaluate Expression` (Control+plus) — liczy wyrażenie pod kursorem. To
  kalkulator. Może zostać, jest tani.
- `Transform Files` (Alt+plus) — masowe przetwarzanie plików wyrażeniem
  regularnym. Narzędzie mocne i ryzykowne; jeśli zostaje, to potrzebuje
  pytania potwierdzającego, którego chyba nie ma.

### 2.5. Numerowane pliki

`Numbered Files` (Alt+Shift+F2) plus Alt+cyfra otwierające plik zapamiętany pod
cyfrą. To duplikat listy ulubionych (Alt+L) i listy ostatnich (Alt+R), tylko
z gorszym interfejsem — cyfrę trzeba pamiętać.

**Propozycja: usunąć, zwolnione Alt+cyfra przydadzą się na coś lepszego.**

---

## 3. NIE USUWAĆ — wygląda na zbędne, ale nie jest

Zapisuję to osobno, żeby przy następnym porządkowaniu nie skreślić tego
pochopnie.

- **Odczyt plików .rtf, .docx, .odt** — to nie „bogate formatowanie", to
  możliwość otwarcia cudzego dokumentu. Zostaje.
- **`Copy Rich Text` (Control+Shift+C)** — wygląda na relikt RTF, a jest
  odwrotnie: to jest droga, którą Markdown wkleja się do Worda z zachowanymi
  nagłówkami i listami. Kluczowe dla pracy z redakcją.
  **Uwaga: Michał chce ten chord na kopiowanie pełnej ścieżki** (pozycja
  z sekcji „## Edsharp" pliku zadań). Kolizja do rozstrzygnięcia osobno —
  `Copy Rich Text` musi wtedy dostać inny klawisz, nie zniknąć.
- **`FontDefault`** — patrz 1.2.
- **`Extra Speech Log` (Alt+Shift+X)** — sam przełącznik „extra speech" został
  wycofany 03.09.2026, ale dziennik mowy ZOSTAJE. To narzędzie diagnostyczne:
  bez niego nie da się sprawdzić, co program naprawdę powiedział.
- **`NavigatePart` w pliku ustawień** — martwy klucz, żadne polecenie go nie
  czyta. Ale usunięcie go z pliku zepsułoby zgodność z oryginalnym EdSharpem.
  Zostaje w pliku, nie ma go w oknie.
- **Skoki po komentarzach i przypisach** — patrz 1.3, Michał wprost odmówił
  ich usunięcia.
- **`Guard Document` (Control+F7)** — ochrona przed przypadkową edycją. Do
  5.0.65 były dwa polecenia jednokierunkowe, teraz jedno przełączające. Tanie
  i sensowne.

---

## 4. Porządki, które nie są usuwaniem

Trzy pozycje z listy Michała to nie usuwanie funkcji, tylko naprawa tego, jak
są podane. Wpisuję je tu, żeby nie zginęły.

1. **Opcje nawigacji po zakładkach, komentarzach i przypisach siedzą w menu
   Miscellaneous, a powinny w Navigate.** Stan: skoki po przypisach
   i komentarzach są JUŻ w menu Navigate (linia 2112) — ale te same pozycje
   pokazują się dodatkowo w Miscellaneous (linia 2106), bo lista Misc dostaje
   te same obiekty. Do zrobienia: zdjąć duplikaty z Misc.
2. **Menu Miscellaneous ma 56 pozycji.** To nie menu, to spis treści. Nawet po
   usunięciu wszystkiego z rozdziału 2 zostanie około 40. Wymaga podziału na
   podmenu albo rozrzucenia po menu tematycznych.
3. **Work Continuity ma być grupą w Ustawieniach, nie osobną pozycją menu.**
   Pozycja z listy Michała, wciąż otwarta.

---

## Co z tego wynika w liczbach

Gdyby przyjąć wszystkie propozycje z rozdziału 2, ubyłoby:

- około 12 poleceń z palety i menu (PyDent, PyBrace, Compile, Pick Compiler,
  Review Output, Say Compiler, Run, Run at Cursor, Numbered Files, Justify,
  Style, Baseline, Set Selection Font, Styles)
- 3 opcje konfiguracyjne (CompileCommand, JumpPosition, AbbreviateOutput)
- 1 plik z instalatora (EdSharp.dll)
- 1 mechanizm późnego wiązania przez refleksję
- cały mechanizm plików per-kompilator

Nic z tego nie zostało zrobione. Rozdział 2 czeka na decyzję.
