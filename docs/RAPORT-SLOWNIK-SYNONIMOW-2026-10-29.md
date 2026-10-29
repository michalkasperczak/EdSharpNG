# Raport: polski słownik synonimów dla EdSharpNG

Data rozpoznania: 29 października 2026.

## Wniosek

Da się wdrożyć lokalny, działający bez Internetu słownik synonimów dla języka polskiego.

Najlepszym kandydatem jest **Słowosieć / plWordNet** Politechniki Wrocławskiej. Nie należy jednak kopiować bez zmian rozwiązania z nowego, oryginalnego EdSharpa Jamala: jego nowy tezaurus obsługuje WordNet angielski, a nie polski.

Rekomenduję osobną funkcję `Synonimy` w EdSharpNG, pozostawienie `Shift+F7` jako jej skrótu oraz wykonanie najpierw małego prototypu na ograniczonej, lokalnej paczce danych plWordNet. Prototyp ma zmierzyć pokrycie zwykłych polskich tekstów i sposób odczytu listy przez NVDA, zanim do instalatora trafi większy zasób.

## Stan obecnego EdSharpNG

W naszej gałęzi `master` komenda `Shift+F7` istnieje, lecz uruchamia automatyzację Microsoft Word przez COM i metodę `CheckSynonyms`.

Działa to tylko wtedy, gdy Word jest zainstalowany oraz ma odpowiedni słownik synonimów. Kod przekazuje do Worda cały aktualny fragment albo zaznaczenie, pokazuje jego okno tezaurusa i następnie przepisuje ewentualnie zmieniony tekst do dokumentu. To nie jest własny, dostępny wybór synonimu w EdSharpNG.

Sprawdzanie pisowni od wersji 5.0.84 jest już niezależne od Worda, bo używa mechanizmu Windows. Windows Spell Checking API nie dostarcza jednak polskiego tezaurusa, więc nie jest źródłem dla tej funkcji.

## Co zmienił Jamal Mazrui

Bieżący oryginalny EdSharp deklaruje tezaurus bez Microsoft Office: `Shift+F7` wywołuje WordNet, pokazuje synonimy pogrupowane według znaczeń oraz krótką definicję.[^1]

Sprawdzony kod potwierdza tę deklarację. `Thesaurus()` woła skrypt `configs/convert/wordNet.py`; skrypt korzysta z `nltk.corpus.wordnet`, pobiera dane `wordnet` i `omw-1.4`, a potem wywołuje `wordnet.synsets(sWord)` bez wskazania języka.[^2] [^3]

To jest istotne: domyślne wyszukiwanie tej biblioteki jest angielskie. Kod nie odczytuje Słowosieci, nie przekazuje `lang="pol"` i nie zawiera polskiego analizatora odmiany. Zatem jest dobrym wzorem interfejsu i działania bez Worda, ale nie gotową implementacją polskiego słownika.

Jamal nadal zostawił opcję powrotu do Worda (`Thesaurus=Word`) dla osób, które go mają.[^2]

## Źródła polskich danych

### 1. Słowosieć / plWordNet — rekomendowane źródło

Słowosieć to polski wordnet: semantyczna sieć znaczeń i relacji, połączona także z angielskim Princeton WordNetem.[^5]

Licencja zezwala na używanie, kopiowanie, modyfikowanie i rozpowszechnianie bez opłat, także komercyjnie, ale wymaga zachowania informacji licencyjnej i zastrzeżeń odpowiedzialności.[^5] [^8]

Oficjalna, nowsza paczka plWordNet 4.5 jest duża: repozytorium CLARIN-PL opisuje archiwum o rozmiarze 444 MB. Nie powinna być pierwszym zasobem dodawanym do lekkiego instalatora EdSharpNG.[^7]

Aktualna strona Słowosieci nie udostępnia tej bazy od ręki: prosi o zgłoszenie do helpdesku `plwordnet@clarin-pl.eu`, podanie danych kontaktowych oraz celu użycia.[^6] Nie wysłano takiego zgłoszenia.

### 2. Kompaktowy wariant Open Multilingual Wordnet — dobry do prototypu

Projekt Open Multilingual Wordnet publikuje paczkę `omw-pl:2.0` dla plWordNet.[^9]

Pobrana do pomiaru paczka ma 1 119 884 B po kompresji i 13 922 646 B jako XML. Zawiera 45 140 różnych lemów, 52 378 połączeń leksem–znaczenie oraz 33 826 zbiorów synonimów. Jej dołączona licencja wskazuje dane plWordNet 2.3 z 2015 r., więc jest wyraźnie starsza i znacznie mniejsza od aktualnego zasobu.[^8]

Próbne wyniki z tej paczki są obiecujące, lecz nie pełne:

- `dom` → między innymi: „chata”, „chałupa”, „dach nad głową”, „lokum”, „własny kąt”;
- `szybki` → „prędki”, „szparki”, „ścigły”;
- `ważny` → „istotny”, „niebagatelny”, „ważki”, „znaczący”;
- `pomoc` → „wsparcie”, „datek”, „zapomoga”.

Są też realne ograniczenia: w tym wariancie nie znaleziono haseł `ładny`, `mówić`, `robić`, `sprawdzić` ani `synonim`. XML nie zawiera polskich definicji znaczeń — 0 elementów definicji na 33 826 synsetów. Sama lista synonimów bez objaśnienia znaczenia będzie słabsza niż interfejs angielskiego WordNetu Jamala.

Ten wariant nadaje się więc do sprawdzenia ergonomii i pokrycia, ale nie powinien być ogłaszany jako pełny słownik języka polskiego.

### 3. Usługa sieciowa Słowosieci — nie jako pierwsza wersja

Słowosieć deklaruje usługę sieciową i API.[^5]

Nie zweryfikowano w tej pracy dostępności, limitów, sposobu autoryzacji ani stabilności obecnego API. Zależność od Internetu jest też gorsza dla edytora używanego offline. Można ją rozważyć później jako opcjonalne rozszerzenie pełniejszego słownika, nie jako podstawę funkcji.

### Odrzucony kandydat: PolNet

PolNet jest osobnym polskim wordnetem, ale licencja wskazana przez jego stronę to CC BY-NC-ND 3.0, więc wyklucza zastosowanie komercyjne i nie pasuje do swobodnie dystrybuowanego EdSharpNG.[^10] Nie należy na nim budować funkcji.

## Proponowane wdrożenie

### Etap 1 — prototyp lokalny, bez Pythona i bez Worda

1. Dodać do EdSharpNG mały moduł C# odpowiedzialny wyłącznie za odczyt indeksu synonimów.

2. Do paczki testowej dodać kompaktowy XML OMW albo wygenerowany z niego indeks tylko do odczytu oraz pełny plik licencji plWordNet.

3. Zachować `Shift+F7`, ale zmienić zachowanie:

   - bez zaznaczenia: pobierany jest wyraz pod kursorem;
   - z zaznaczeniem: akceptowany jest jeden wyraz; dla wielu wyrazów program jasno mówi, że słownik synonimów działa dla pojedynczego wyrazu;
   - wynik otwiera dostępne okno listy;
   - Enter zastępuje tylko wybrany wyraz;
   - Escape i Anuluj nie zmieniają dokumentu;
   - brak wyniku jest komunikowany krótko, bez pustego okna.

4. Lista ma najpierw podawać słowo, potem część mowy i numer grupy znaczenia, a następnie kandydatów. W prototypie numer grupy nie będzie definicją; ograniczenie należy wypowiedzieć wprost, nie udawać dokładnego rozróżnienia znaczeń.

5. Word pozostawić wyłącznie jako ręczny wariant awaryjny w ustawieniu, nie jako ukrytą zależność podstawowego polecenia.

### Etap 2 — odmiana polska

Słowosieć indeksuje przede wszystkim lemy, podczas gdy w edytorze użytkownik stoi zwykle na formie odmienionej, na przykład „sprawdziłem” lub „ładnego”. Proste porównanie tekstu nie wystarczy.

Prototyp ma najpierw zmierzyć, jak wiele realnych wyrazów działa bez lematyzacji. Dopiero potem trzeba dobrać lokalny analizator morfologiczny albo uzgodnić z dostawcą pełnego plWordNetu dostęp do danych form fleksyjnych. Nie należy udawać, że sama baza synonimów rozwiązuje odmianę.

### Etap 3 — decyzja o pełnym słowniku

Jeśli prototyp okaże się użyteczny, są dwa realistyczne kierunki:

- uzyskać oficjalny aktualny plWordNet od zespołu Słowosieci i przygotować z niego mniejszy indeks dystrybucyjny z zachowaniem licencji;
- utrzymać kompaktowy wariant jako podstawowy oraz zaoferować pełniejszy pakiet jako osobne, dobrowolne pobranie.

Nie rekomenduję wprowadzania Python/NLTK tylko po to, by czytać polskie dane. Własny indeks C# będzie prostszy w instalatorze, szybszy przy uruchomieniu i nie powiąże podstawowej funkcji edytora z osobnym środowiskiem uruchomieniowym.

## Format potrzebny EdSharpNG

EdSharpNG **nie potrzebuje całego formatu źródłowego plWordNet w czasie działania**. Pełna Słowosieć jest dostarczana w specjalizowanym XML `plwnxml`; jej parser musi obsłużyć jednostki leksykalne, synsety i wiele typów relacji. To dobry format wejściowy dla narzędzia przygotowującego dane, lecz zły format dla edytora: pełny odczyt XML przy każdym starcie byłby duży i wolny.[^11]

Kompaktowy OMW użyty do pomiaru ma prostszy XML Lexical Markup Framework: `LexicalEntry` zawiera lemat oraz odwołanie `Sense` do identyfikatora synsetu, a `Synset` grupuje należące do niego znaczenia. Pomiar potwierdza tę strukturę na pobranym pliku. To wystarcza, aby na etapie budowania wyliczyć listy synonimów.

Do dystrybucji rekomenduję jeden własny plik tylko do odczytu, np. `synonyms-pl.v1.bin`, generowany podczas budowania z XML. Ma zawierać:

- nagłówek: wersję indeksu, wersję i nazwę źródła oraz wymagane informacje licencyjne;
- klucz: polski lemat po bezpiecznym ujednoliceniu wielkości liter;
- jedną lub więcej grup: część mowy, identyfikator znaczenia oraz lista innych lemów z tej samej grupy;
- osobny, jawny stan „brak definicji”, gdy źródło nie dostarcza objaśnienia.

Ten format nie wymaga Pythona, Worda, Internetu, bazy danych ani dodatkowej natywnej biblioteki SQLite w instalatorze. C# wczyta go do słownika w pamięci albo odczyta indeksowo; narzędzie budujące może być oddzielnym programem deweloperskim. Dla pierwszego prototypu prostszy, poprawny wariant to UTF-8 JSON lub plik tekstowy z identycznym modelem danych; przed wydaniem należy zmierzyć czas startu i rozmiar, a dopiero potem utrwalać format binarny.

Niezależnie od formatu samego słownika potrzebny jest drugi etap dla polskiej odmiany. Indeks synonimów odpowie na `ważny`, ale nie automatycznie na `ważnego` czy `sprawdziłem`. Najpierw mierzymy to bez lematyzacji. Dopiero wynik wskaże, czy dołączyć lokalny analizator odmiany albo zbudować dodatkowy indeks form fleksyjnych. Nie należy podmieniać form na zasadzie prostego obcinania końcówek.

## Czy wystarczy dostępny polski zasób

**Tak, można już teraz użyć legalnego polskiego zasobu do prototypu.** Pobrany OMW/plWordNet jest lokalny, niewielki i wystarczający, aby stworzyć oraz przetestować całą funkcję. Nie wymaga starania się o dostęp.

**Dla docelowego, sensownego słownika warto wystąpić o aktualny pełny plWordNet.** Oficjalny zasób jest zdecydowanie bogatszy: opis CLARIN podaje 178 000 jednostek słownikowych i 259 000 znaczeń; licencja dopuszcza użycie także komercyjne.[^12] Aktualna strona źródeł nie daje jednak gotowego pobrania bez kontaktu, więc potrzebne jest krótkie zgłoszenie do zespołu Słowosieci, opisujące EdSharpNG i planowane użycie offline. To nie jest prośba o płatną licencję ani zgoda użytkownika końcowego — to procedura uzyskania aktualnego pliku źródłowego.

Wniosek praktyczny: nie trzeba czekać, aby wykonać prototyp i test NVDA. Warto równolegle przygotować zgłoszenie o pełny zasób, ale nie wysyłać go bez decyzji Michała co do podania danych kontaktowych i celu projektu.

## Warunki odbioru

Przed wydaniem należy wykonać trzy rodzaje pomiaru:

1. **Dane:** zestaw co najmniej 100 częstych polskich słów z tekstów Michała; osobno wynik dla form podstawowych i odmienionych; liczba trafień, braków oraz błędnych grup.

2. **Kod:** testy bez interfejsu dla wyszukiwania, usuwania własnego słowa z listy, polskich znaków, wieloznaczności, braku wyniku i zachowania wielkości pierwszej litery przy podmianie.

3. **Dostępność:** żywy NVDA — wyraz pod kursorem, tytuł i pierwsza pozycja listy, ruch strzałkami, Enter, Escape, brak wyniku oraz zachowanie kursora po anulowaniu i po zastąpieniu.

## Odpowiedź zespołu Słowosieci i decyzja licencyjna

29 października 2026 Bartłomiej Alberski z Zespołu Lingwistów Słowosieci wskazał oficjalny plik aktualnej bazy (`wordnet_without_extra_lexicons-2026_02_09.sql.gz`) oraz stronę aktualnej licencji.[^13] [^14] Odpowiedź wymaga cytowania zasobu plWordNet 5.0 oraz całej infrastruktury CLARIN-PL.[^13]

Oficjalna strona licencji mówi, że Słowosieć jest dostępna bezpłatnie także do zastosowań komercyjnych. Zezwala na użycie, kopiowanie, modyfikowanie, rozpowszechnianie i publiczne udostępnianie bez opłat, jeżeli informacje o prawach autorskich, licencja i zastrzeżenie braku gwarancji pozostaną we wszystkich kopiach oraz modyfikacjach.[^14] Nie wolno używać nazwy Politechniki Wrocławskiej w reklamie lub promocji dystrybucji bez odrębnej zgody.[^14]

Wniosek: EdSharpNG może tworzyć i dołączać własny indeks tylko do odczytu, lecz instalator musi zawierać pełny tekst właściwej licencji, informację o źródle oraz wymagane cytowania. Do czasu pełnego zweryfikowania pobrania 5.0 pierwsza kompilacja funkcji używa małego indeksu OMW/plWordNet 2.0 i jego własnej licencji. Nie należy przedstawiać go jako danych plWordNet 5.0.

Wymagane cytowania dla indeksu z plWordNet 5.0:

- Rudnicka, Ewa, Bartłomiej Alberski, and Maciej Piasecki. “plWordNet 5.0–challenges of a life-long wordnet development process.” *GWC 2025* (2025): 162–170.
- Piasecki, Maciej, Agnieszka Dziob, Arkadiusz Janz, Jan Kocoń, Tomasz Naskręt, Marcin Oleksy, Ewa Rudnicka, Tomasz Walkowiak, Jan Wieczorek, and Krzysztof Hwaszcz. 2025. “CLARIN-PL: A user centred language technology infrastructure.” *Language Resources & Evaluation* 59: 4493–4528. https://doi.org/10.1007/s10579-025-09839-y

## Pierwsze wdrożenie beta

Wdrożono lokalny moduł C# `Synonimy.cs` oraz indeks UTF-8 `Synonyms/plwordnet-synonyms.txt`, generowany podczas budowania. `Shift+F7` nie uruchamia już Word COM. Działa tak:

- pobiera jedno słowo spod kursora albo z zaznaczenia;
- odmawia dla wielu słów komunikatem „Zaznacz jedno słowo.”;
- otwiera standardowe dostępne okno listy EdSharpNG;
- każda pozycja zawiera część mowy, numer znaczenia i synonim;
- Enter zastępuje tylko wskazane słowo, a Escape nie zmienia dokumentu;
- brak wyniku jest komunikowany bez otwierania pustego okna.

Paczka beta zawiera `Synonyms/LICENSE-plWordNet.txt` i `Synonyms/NOTICE-plWordNet.txt`. Pierwszy indeks ma 17 675 lemów i 20 412 grup synonimów po odrzuceniu wyrażeń wielowyrazowych, których obecne polecenie nie zastępuje bezpiecznie. Pełny plWordNet 5.0 będzie następnym źródłem do regeneracji indeksu po zakończeniu pobrania i audycie schematu SQL.

## Decyzja na teraz

Nie ma już zależności `Shift+F7` od Microsoft Worda. Wersja beta ma lokalny, licencjonowany indeks OMW/plWordNet i wszystkie wymagane pliki informacyjne w instalatorze.

Przed publicznym wydaniem pozostają: pomiar pokrycia na 100 słowach, żywy test NVDA oraz aktualizacja danych do zweryfikowanego plWordNet 5.0.

## Źródła

[^1]: [Jamal Mazrui — EdSharp, opis funkcji](https://github.com/jamalmazrui/EdSharp)

[^2]: [Jamal Mazrui — EdSharp.cs, implementacja tezaurusa](https://raw.githubusercontent.com/jamalmazrui/EdSharp/master/EdSharp.cs)

[^3]: [Jamal Mazrui — wordNet.py, pomocniczy skrypt Pythona](https://raw.githubusercontent.com/jamalmazrui/EdSharp/master/configs/convert/wordNet.py)

[^4]: [Jamal Mazrui — History.md, opis przejścia na WordNet](https://raw.githubusercontent.com/jamalmazrui/EdSharp/master/help/History.md)

[^5]: [Słowosieć — opis projektu i licencja](https://slowosiec.pl/about/project)

[^6]: [Słowosieć — zasady uzyskania dostępu do zasobów](https://slowosiec.pl/about/resources)

[^7]: [CLARIN-PL — plWordNet 4.5](https://clarin-pl.eu/dspace/handle/11321/834?show=full)

[^8]: [Open Multilingual Wordnet — licencja polskich danych](https://github.com/omwn/omw-data/blob/main/wns/pol/LICENSE)

[^9]: [Wn — indeks pakietów Open Multilingual Wordnet](https://github.com/goodmami/wn/blob/main/wn/index.toml)

[^10]: [PolNet — opis i licencja zasobu](http://ltc.amu.edu.pl/polnet/)

[^11]: [plwordnet — dokumentacja formatu plwnxml](https://github.com/maxadamski/plwordnet)

[^12]: [CLARIN ERIC — plWordNet: skala zasobu i licencja](https://clarin.eu/tags/plwordnet)

[^13]: Odpowiedź Bartłomieja Alberskiego z Zespołu Lingwistów Słowosieci, 29 października 2026, zachowana w lokalnej skrzynce odbiorczej Michała jako wiadomość `92566`.

[^14]: [Słowosieć — oficjalna licencja](https://slowosiec.pl/pl/about#license)
