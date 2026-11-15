# EdSharpNG - zadania zgloszone 11.09.2026 (po wersji 5.0.78)

Zrodlo: Michal Kasperczak, wiadomosc z 11.09.2026 wieczorem.
Kolejnosc zapisu NIE jest priorytetem - priorytety do ustalenia.

## Synonimy — prywatne 5.0.123, etap odebrany

Michał zatwierdził całość propozycji, łącznie z przygotowaniem dalszych form.
To zlecenie wdrożenia prywatnego, nie publikacji ani instalacji na głównym PC.

Kontrakt: Shift+F7 otwiera płaską listę; góra/dół wybiera; Enter wstawia
wyświetlony zamiennik i zamyka wybór; Escape anuluje. Prawa strzałka pokazuje
źródłową definicję/przykład dokładnego znaczenia; Escape wraca do tej samej
pozycji. Shift+Enter pokazuje poświadczone odmiany zaznaczonego słowa;
Enter wstawia wybraną formę i zamyka oba okna; Escape wraca do synonimów.
Bez drzewa i bez szukania kolejnych synonimów synonimu. Szczegóły niczego
nie wstawiają. F1/pomoc i przyciski opisują te same działania. Opcje aplikacji
zawierają zapamiętywany wybór Polski / Angielski / Oba, niezależny od pisowni
oraz interfejsu. Brak form lub awaria zasobu mają jawny komunikat; nie zgadujemy.

Etapy wykonania i odbioru:
- [x] Czysta logika odmian PL powiązanych z konkretnym lematem, synsetem i POS;
  EN z poświadczonych wyjątków WordNet (nie pełna regularna gramatyka).
- [x] Prawa strzałka, Shift+Enter, powrót, przyciski i F1 w realnym dialogu.
- [x] Lista wyboru języka słownika w istniejącym oknie Opcji.
- [x] Pełny prywatny build, testy danych i UI, żywy NVDA, instalator i ZIP.
- [x] Odbiór i instrukcja testowania z jawnymi ograniczeniami.
- [x] Niezależne potwierdzenie poprawek po przeglądzie interfejsu.

Powtórny przegląd potwierdził poprawki zapisu i normalizacji trybu; bez nowego
blokera w tym zakresie. Sprawdził kod oraz logi, nie wykonywał ponownie GUI.
Zatwierdzony prywatny przyrost jest gotowy. Publikacja i instalacja na głównym
komputerze pozostają poza tym zleceniem.

Pierwszy przegląd odtworzył błędne potwierdzenie zapisu INI i rozbieżność `EN`.
Poprawiono oba problemy; sprawdzono RED/GREEN oraz żywy odczyt NVDA. Obsłużono
też niepowodzenie przywracania domyślnych. Cztery realne warianty formularza
z zapisywalnym / tylko-do-odczytu INI przeszły. Nowy build ma numer 5.0.123,
aby nie zmieniać zawartości wcześniejszej paczki 5.0.122.

Raport: `SYNONIMY-ODBIOR-5.0.123.md`; instrukcja:
`SYNONIMY-PELNY-WYBOR-QA.md`. Python: 35 testów. Odmiany: 69 asercji C#,
37 polskich wyborów bez błędu, 32 z dostępnymi formami pierwszego synonimu.
NVDA potwierdził m.in. `dom → lokum → lokach` i `DEPART → GO → WENT`.
Instalacja próbna: kod 0; rejestr i skróty odtworzone bajtowo. Zweryfikowano
1083 pliki instalacji; ZIP ma 1084 pliki razem z manifestem. Nie jest to pełna
gramatyka angielska. Następnie na osobne polecenie Michała dostarczono instalator,
ZIP i krótką instrukcję do `D:\Projekty Codex\Hermes` na komputerze `MICHAL`.
Docelowe rozmiary i SHA-256 potwierdzone, staging usunięty. Bez publikacji
ani uruchamiania instalatora przez wykonawcę. Zmianę istniejącej instalacji
oraz PID w trakcie transferu odnotowano w `main-delivery-verified.json`;
nie deklarujemy niezmienności tych elementów.

## Angielski słownik synonimów — przygotowana prywatna wersja 5.0.121

Michał zlecił sprawdzenie oryginalnego pakietu i przygotowanie angielskiego
słownika także w polskim EdSharpNG. Język słownika ma być niezależny od języka
interfejsu; rozważone korzystanie z obu języków jednocześnie.

Przyjęty zakres: wybór Polski / English / oba, domyślnie bez zmiany polskiego
słownika. W wariancie obu wyniki muszą wskazywać język; nie łączymy znaczeń
przez identyczny napis. Shift+F7, płaska lista i opcjonalne F4 pozostają.
Bez Worda, runtime Python/NLTK ani pobierania przy pierwszym użyciu.

- [x] Oryginalny kod i bieżący commit sprawdzone: `wordNet.py` używa angielskiego
  Princeton WordNet 3.0 przez NLTK. `en_US.aff/dic` to osobne dane pisowni.
- [x] Pełne archiwum danych pobrane, CRC i SHA sprawdzone; licencja Princeton
  dopuszcza redystrybucję z zachowaniem pełnego tekstu i zastrzeżeń.
- [x] Nowy moduł routingu C# przeszedł RED/GREEN na fixture'ach: osobne języki,
  homonimy, szczegóły, źródłowe nieregularne formy i izolacja uszkodzeń.
- [x] Generator i gotowe zasoby: 60 874 lematy (65 636 par lemma/POS), 45 043
  synsety z definicjami, 13 332 przykłady i 3 485 poświadczonych wyjątków.
  Sprawdzone hashe, licencja bajtowo zgodna z archiwum; brak markerów (a)/(p)/(ip).
- [x] Dostępny wybór słownika w Misc → Thesaurus Dictionary i palecie, zapis
  `SynonymDictionary`, integracja Shift+F7/F4. Prywatny pełny build 5.0.121 działa.
- [x] Regresja polskiego: pięć programów testowych C# bez błędów. Python: 33 testy.
  Końcowe zasoby, rzeczywisty jawny zestaw 100 angielskich wyrazów: 97 trafień,
  mediana 150 ms, maksimum 162 ms. `ablaze` działa bez źródłowych markerów.
- [x] Żywy NVDA: polski/angielski/oba dla `list` (4/9/13 pozycji), oznaczenia
  języka, odrębne opisy PL/EN, F4, powrót do tego samego wiersza, Enter i Escape.
  Zapis: `testy/wyniki/english-synonyms/ui-integration.json` i podgląd mowy obok.
- [x] Końcowe dane po poprawce markerów i parsera opisów: przykłady nie są
  błędnie etykietowane jako definicje; zachowana atrybucja i późniejsze definicje.
- [x] Niezależny review: zależności dodane do indeksu; brak pliku wyjątków EN
  ma teraz ostrzeżenie (RED/GREEN i odczyt NVDA). Ryzyko czasu ładowania zmierzone:
  20 wywołań na tryb, PL maks. 375 ms, EN 169 ms, oba 577 ms; bez refaktoru cache.
- [x] Instalator i ZIP z identycznym EXE; rzeczywista instalacja próbna na Hermesie
  zakończona kodem 0, wszystkie 1082 pliki zgodne bajtowo. Provider z gotowej paczki
  przeszedł test z zaporą blokującą ruch wychodzący własnego procesu testowego.
- [x] Końcowy NVDA na spakowanej binarce: `ablaze` → `inflamed`, F4 i powrót,
  Enter oraz ostrzeżenie o brakujących formach nieregularnych; środowisko QA sprzątnięte.
  Odbiór i hashe: `SYNONIMY-ODBIOR-5.0.121.md`.
- [ ] Publikacja lub podmiana głównej instalacji — nie wykonywano; nie rozszerzać
  zlecenia przygotowania słownika na automatyczną publikację/instalację.

Publiczne 5.0.120 i główna instalacja niezmienione. Nowy moduł jest podłączony
do interfejsu w prywatnym buildzie 5.0.121, ale nie został jeszcze opublikowany.

## Synonimy — opisy znaczeń na żądanie (5.0.120, wydano 10.11.2026)

Zakres potwierdzony przez Michała: kontynuować lokalne synonimy; bez Worda,
bez drzewa ani dodatkowego obowiązkowego etapu. F4 na pozycji listy otwiera
opis i przykład z istniejącego eksportu plWordNet 5.0, wyłącznie gdy źródło
je zawiera. Wybór pozostaje płaski: strzałki, Enter, Escape.

- [x] Test RED/GREEN odczytu opisów i izolacji homonimów; brak pliku oraz
  uszkodzenie opisu nie wyłączają podstawowej listy synonimów.
- [x] Generator i rzeczywisty zasób: 21278 definicji, 16834 przykłady, bez domysłów.
- [x] Pełny build, 20 testów Python i 5 zestawów C#, odbiór F4 żywym NVDA.
- [x] Niezależny przegląd; notice skorygowane. Instalator/ZIP, kod instalacji 0,
  wszystkie 1076 plików zgodne ze stagingiem i instalacją próbną na Hermesie.
- [x] GitHub/latest, pobrania i SHA-256 obu plików, F11 oraz dostawa do Nowe.

Wydanie: https://github.com/michalkasperczak/EdSharpNG/releases/tag/v5.0.120
Commit źródeł/tagu: `7898280d093c5db650d00553a7af0cf5dcded4fb`.
Na MICHAL: `D:\Projekty Codex\Hermes\Nowe\EdSharpNG_Setup_5.0.120.exe`,
ZIP i opis nowości; sumy celu zgodne z publicznym pobraniem. Stary instalator
i własny opis 5.0.119 usunięte dopiero po potwierdzeniu zastąpienia i hashy.
F11 rzeczywistej 5.0.119 wykryło 5.0.120; Escape anulował bez instalacji.
Zainstalowane bajty i działający proces na MICHAL zachowane. Raport:
`SYNONIMY-ODBIOR-5.0.120.md`; kwity `../testy/wyniki/synonyms120/`.

**Dostawa bez instalacji:** Michał sam zaktualizuje program. Nie zmieniać
jego bieżącej instalacji ani procesu. plWordNet 5.0 i poświadczona odmiana
są już wydane w 5.0.119; dawne zdania o nieukończonej migracji dotyczą historii.

## Synonimy — dalsza rozbudowa (09.11.2026, wydano 5.0.119)

Plan i wyniki: `SYNONIMY-ROZBUDOWA-2026-11.md`.

- [x] Pełny plWordNet 5.0: zakresy HTTP, SHA-256 i CRC gzip sprawdzone;
  parser nie wykonuje SQL, leksykon wybiera z metadanych.
- [x] Większy indeks roboczy i jawny fallback OMW dla brakujących haseł;
  pomiar 153/190 wobec 75/190, bez pięciu wcześniejszych utrat.
- [x] Licencja wskazana w odpowiedzi autorów: pełny tekst zachowany bez
  przerabiania copyrightu. Raport źródeł fleksji i probe Morfeusza2 zachowane.
- [x] Oficjalne API Worda rozpoznane. Nie wdrażać providera jako gotowego bez
  testu działającego Worda i sprawdzenia konkretnej licencji Office;
  Shift+F7 pozostaje lokalny, bez uruchamiania Worda.
- [x] Gotowy indeks poświadczonych form EDSYNF02 i test rzeczywistych danych
  w .NET; pełna równoważność wszystkich 805849 rekordów względem EDSYNF01.
- [x] Regresje i pomiar: 259/290 poświadczonych form w zestawie kontrolnym;
  działający pełny program, Enter/Escape, wielkość liter, komunikaty braku
  i uszkodzenia danych oraz powrót po naprawie sprawdzone żywym NVDA.
- [x] Instalator i ZIP 5.0.119: rzeczywista instalacja na Hermesie, kod 0;
  wszystkie 453 pliki identyczne ze stagingiem i ZIP-em, weryfikacja SHA-256.
- [x] Jedno spójne wydanie 5.0.119: instalator, ZIP, GitHub/latest i F11.
  Niezależny przegląd zakończony; przejściowy brak plików w indeksie usunięto.
  Tag `v5.0.119` wskazuje `d1f42c10eb7a096b0a3a641cd01390b3ee252d47`.
  Oba publiczne załączniki pobrano i porównano ich SHA-256. F11 z testowej
  instalacji 5.0.118 wykryło 5.0.119; pytanie anulowano bez instalacji.
  Instalator, ZIP i opis są w Nowe na komputerze MICHAL, hashe celu zgodne.
  Zachowano binarkę i działający proces głównej instalacji 5.0.118.
  Własne stare paczki usunięto po sprawdzeniu archiwum; Temp posprzątano.
  Wynik odbioru: `SYNONIMY-ODBIOR-5.0.119.md`. Brak `sprawdziłem` i brak providera Worda
  pozostają jawnymi ograniczeniami, nie zakończonymi funkcjami.

## Synonimy — publikacja odebranego etapu (09.11.2026)

Michał wyjaśnił, że pobrał aktualizację z GitHuba, i zlecił uporządkowanie
wydania. Publiczne `/releases/latest` wskazywało 5.0.115, podczas gdy lokalny
słownik był dotąd tylko w prywatnych instalatorach. Zakres: wydać gotowe
synonimy i krótszą listę jako 5.0.118, bez pełnego plWordNet 5.0 i bez innych
odłożonych funkcji. Nie wymuszać aktualizacji w aktywnej sesji na głównym.

- [x] Zamrożone źródła 960e5c9 i ten sam instalator co w dostawie; osobny
  worktree `edsharp-release-5.0.118`, bez ponownej kompilacji zmieniającej SHA.
- [x] Próba instalacji dokładnego instalatora na Hermesie; kod 0, wszystkie
  pliki paczki identyczne, Shift+F7/Enter/Escape zmierzone żywym NVDA.
- [x] Instalator i ZIP z tego samego stagingu; licencja i notice w obu.
- [x] Opublikowano i pobrano oba załączniki; `/releases/latest` wskazuje
  v5.0.118. Aktualizator wybrał właściwy instalator i pierwszą sumę SHA-256
  z opisu; F11 na próbnej instalacji potwierdziło wersję 5.0.118.
- [x] W `Nowe` na głównym są instalator, ZIP i opis 5.0.118. Stary własny
  instalator 5.0.115 z folderu Hermes usunięto po kontroli tożsamości,
  zachowaniu lokalnej kopii i potwierdzeniu publicznego archiwum.
  Proces użytkownika i zainstalowane bajty pozostały bez zmian — to nadal
  5.0.115 do czasu samodzielnej aktualizacji, nie meldować instalacji u niego.

Wydanie: https://github.com/michalkasperczak/EdSharpNG/releases/tag/v5.0.118
ID 438621260, tag wskazuje 960e5c9501efeff7bb29e98de13e560f00a322d2.
Weryfikacja: `testy/wyniki/release5118/`, opis: `RELEASE-NOTES-5.0.118.md`.

## Synonimy — odbiór pierwszej wersji i krótsza lista (09.11.2026)

Źródło: odesłana instrukcja testu `EdSharp INSTRUKCJA-TEST-SYNONIMY-5.0.116.md`.
Michał potwierdził działanie pierwszej wersji: Word i przeglądarka nie otwierają
się, Enter, Escape, strzałki i fokus działają. Nie przypisywać zgłoszeniu
wersji 5.0.117: nazwa dokumentu wskazuje 5.0.116, wersja uruchomionej binarki
nie została tym zgłoszeniem osobno potwierdzona.

- [x] Wyraz przed częścią mowy; usunąć zwrot „znaczenie 1”.
- [x] Numeracja pozycji na całej płaskiej liście, nie numer grupy znaczeniowej.
  Format: `chałupa, rzeczownik, 1 z 5`. Wersja robocza 5.0.118.
- [x] Testy formatu, loadera i indeksu; żywy NVDA: pierwsza, druga i ostatnia
  pozycja, Enter wstawia sam wyraz, Escape nie zmienia dokumentu.
- [ ] Pełny plWordNet 5.0: pobrać i zweryfikować kompletne źródło; nie zastępować
  działającego OMW niesprawdzonym pobraniem i nie nazywać go indeksem 5.0.
- [ ] W kolejnym etapie rozważyć krótkie opisy znaczeń z danych źródłowych.
  Bez automatycznie wymyślonych definicji, bez drzewa i numerowanych grup
  narzuconych użytkownikowi. Obecny indeks nie zawiera definicji.

Szczegóły i zapis mowy: `SYNONIMY-ODBIOR-I-KROTSZA-LISTA-5.0.118.md`.

Zgłoszenie po dostawie: Shift+F7 uruchamia Worda. Odczyt na głównym komputerze
wykazał działającą i zainstalowaną wersję **5.0.115**, nie 5.0.118; dołączone
źródło wciąż wywołuje WordAccess/CheckSynonyms. Instalator 5.0.118 w `Nowe`
ma zgodny hash dostawy. Nie ustalono przyczyny powrotu starszej wersji.
Nie zamykano programu ani nie instalowano aktualizacji w aktywnej sesji.
- [ ] Bezpieczna aktualizacja głównego komputera po decyzji Michała i odbiór
  lokalnego słownika na rzeczywiście uruchomionej binarce.
Diagnoza: `DIAGNOZA-SYNONIMY-WORD-PO-DOSTAWIE-5.0.118.md`.

## Zgłoszenie 05.10.2026 — DOCX nie trafia do ostatnich, ścieżka importu

Potwierdzono w binarce 5.0.114 identycznej z używaną na głównym komputerze:
po otwarciu i zamknięciu artykułu Quill Lite importowany DOCX nie trafia do
Alt+R (3/3 próby), podczas gdy kontrolny Markdown trafia (3/3, kursor przesunięty).
Import zachowuje pełną ścieżkę w OriginalDocument.Path, ale historia i komenda
Path to Clipboard używają roboczego child.File bez katalogu. Kopiowanie ścieżki
jest pod Alt+Shift+P; podany w zgłoszeniu Ctrl+Shift+P to Path List.

- [x] Naprawić użycie tożsamości oryginału w historii i kopiowaniu ścieżki
  importowanych dokumentów, bez podmieniania roboczego child.File na DOCX.
- [x] Sprawdzić wynik zapisu do schowka i komunikat błędu; fizycznego pustego
  schowka u Michała jeszcze nie odtworzono, nie utożsamiać go z błędną ścieżką.

ZLECONE przez Michała 05.10.2026: wdrożyć poprawkę i nowy instalator;
sprawdzić także inne formaty, nie tylko zgłoszony DOCX. AMC Python zachowuje
pierwszeństwo przy kolizji zasobów lub odbioru; poprawka może być po tej wersji.
To ograniczone wznowienie EdSharpa, bez pozostałych odłożonych funkcji.

Zakres odbioru: historia Alt+R po zamknięciu i ponownym otwarciu, fizyczne
kopiowanie pełnej ścieżki Alt+Shift+P, DOCX/EPUB/HTML/RTF oraz pozostałe
obsługiwane drogi importu (inwentarz z kodu/konfiguracji); kontrolne MD/TXT,
brak naruszenia oryginału, zapis Markdowna i odłączenie oryginału, odzysk sesji,
nazwy polskie/spacje. Niedostępne konwertery i nieobsługiwane formaty nazwać
jawnie, nie zaliczać jako sprawdzone. Zachować obecną funkcję Ctrl+Shift+P.

Po testach: nowy numer, instalator i ZIP, próbna instalacja na Hermesie,
żywy NVDA dla zmienionej drogi, publikacja i zweryfikowana dostawa na główny.
Instalacja nie może przerwać nagrywania ani utracić otwartych dokumentów.
Doprecyzowanie Michała 05.10: dopuszcza instalację, ale edytor na głównym jest
uruchomiony i akceptuje samo dostarczenie instalatora oraz późniejszą własną
aktualizację przez program. Wybrana bezpieczna dostawa: po pełnym odbiorze
opublikować wydanie dostępne aktualizatorowi i zapisać sprawdzony instalator w
`D:\Projekty Codex\Hermes` na głównym, bez wymuszania zamknięcia edytora ani
uruchamiania instalatora w jego aktywnej sesji. Próbna instalacja na Hermesie
pozostaje obowiązkowa. Nie meldować tej dostawy jako instalacji u Michała.
Status: ZAMKNIĘTE wydaniem 5.0.115. Commit/tag/master GitHuba:
`de01f686df8d6c4a56571ee50f7c88bb09d7c109`. Publiczne wydanie ID403307255:
https://github.com/michalkasperczak/EdSharpNG/releases/tag/v5.0.115
Instalator i ZIP: uploaded, pobrane z publicznych URL i porównane SHA/rozmiar.
`/releases/latest` wskazuje 5.0.115; kanał `prerelease=false` zachowany dla F11.

Odebrane: historia i pełna ścieżka importu, odzysk importów bez prawa zapisu
wstecz, ochrona celu zapisu po anulowanym zamknięciu, odczyt EPUB 3 oraz odmowa
surowego otwarcia spakowanego pliku po błędzie konwertera. Ryzyka roboczych
kandydatów nie są automatycznie diagnozą publicznej 5.0.114.
Na finalnej binarce testy 125/98/9/107/74 bez błędów. Trzy pominięcia pierwszej
sondy obejmują brak próbki DOC, brak importu ODT oraz celowe wyłączenie EPUB 3
z grupy jednokierunkowej — EPUB 3 przeszedł osobno także zapis wstecz.
Żywy NVDA: DOCX/PDF Alt+Shift+P, prawdziwy schowek i wklejenie, Alt+R i powrót
właściwej treści, jeden komunikat busy, odmowa uszkodzonego DOCX. Nie jest to
pełny audyt wszystkich formatów; DOC niezmierzony, ODT bez wpisu importu.

Hermes: próbna instalacja kod0, rejestr5.0.115, własny log uruchomienia;
SHA testowanego/pakowanego/zainstalowanego EXE zgodny:
86f59310f5f23fe132c361321b9165b2ba8be2c09269cbe4ce1eb6c69316a96e.
Główny: dostarczone `D:\Projekty Codex\Hermes\EdSharpNG_Setup_5.0.115.exe`
i `EdSharpNG_5.0.115_zmiany.txt`, pełne SHA odczytane i zgodne. Instalatora NIE
uruchamiano. Ten sam EdSharp PID24512 i EXE5.0.114 zachowane; Michał wybiera
moment aktualizacji. Jeden starszy instalator114 przeniesiony do
`kosz-EdSharpNG-5.0.115`, brak własnych tymczasowych plików na głównym.

Dokumenty: `WYDANIE-5.0.115.txt`, `WERYFIKACJA-5.0.115.md`.
Kwity: `../testy/wyniki/release5115/{parent-local-acceptance,public-release-verified,main-delivery-verified}.json`.
Rezerwacja EdSharpa oddana; brak dalszego zlecenia w tym zakresie.
Szczegóły diagnozy: `DIAGNOZA-QUILL-IMPORT-HISTORIA-2026-10-05.md`.

## Do obserwacji — Quin / trypsynth i czytanie z Paperback (05.10.2026)

Michał wskazał repozytoria Quina Gillespie (trypsynth), w tym wxWidgets,
ship-shape i profil Packages. Śledzić także inne jego projekty jako źródła
rozwiązań dla naszych aplikacji. Rozważyć czytanie z Paperback w EdSharpNG,
ale tylko przy rzeczywistej korzyści, bez zlecenia wdrożenia teraz.

Doprecyzowanie Michała: przede wszystkim chodzi o PODGLĄD/CZYTANIE pod
Escape, nie tylko wczytywanie plików. Import to drugi, powiązany kierunek.
Poprzednie skupienie na importerze było interpretacją asystenta, nie
ograniczeniem zakresu ustalonym przez Michała.

- [ ] Wykonać jedno wspólne zadanie: **przegląd parsowania, importu i podglądu
  EdSharpNG inspirowany Paperback**, zapisane w
  `ZADANIE-PRZEGLAD-PARSOWANIA-EDSHARP-INSPIROWANY-PAPERBACK.md`. Najpierw
  sprawdzić własny kod i własne dokumenty; Paperback jest źródłem wzorców,
  nie listą jego błędów do przeniesienia ani diagnozą EdSharpNG. Zadanie
  obejmuje pozycje źródło↔render↔kontrolka, długie RTF, komunikaty po menu,
  DOCX, ewentualny WebView2, dostępność NVDA i licencje. Przekazać ten plik
  przyszłemu wykonawcy działającemu na repozytorium Michała przez pakiet AI
  Michała Dziwisza.

Doprecyzowanie Michała: **Quill i Paperback razem do wykorzystania w EdSharp.**
Oba są źródłami rozwiązań dla naszego edytora — przede wszystkim podglądu
oraz otwierania i zapisywania dokumentów. Nie chodzi o porównanie Quill
z Paperback ani wybór jednego zamiast drugiego. To większa praca na później;
teraz kontynuujemy interfejs AMC.

Źródła i ograniczenia: `QUIN-TRYPSYNTH-ZRODLA-2026-10-05.md`.
To obserwacja i późniejsza ocena, nie wznowienie całego rozwoju EdSharpa
ani zmiana priorytetu AMC. Bez zmian kodu produktu i bez instalacji.

### Wstępny werdykt 24.10.2026 — nie przebudowywać importu ani parsera

Odczyt aktualnego EdSharpNG i materiałów Quill/Paperback nie daje podstawy
ani do wymiany WinForms, ani do portowania ich parserów czy bibliotek. EdSharpNG
ma własny import przez Pandoc, renderowany podgląd Escape z mapami
źródło↔widok oraz zabezpieczoną w 5.0.115 tożsamość importowanych dokumentów.
Najpierw wykonać opisany wyżej mały przegląd na własnych dokumentach i żywym
NVDA: Unicode/emoji, pozycje, linki, zakładki, komunikaty menu oraz rzeczywiste
formaty. Dopiero odtworzony błąd jest podstawą małej poprawki. Najbardziej
obiecującym kierunkiem pozostaje odbiór i nawigacja podglądu Escape, nie
przebudowa importu. Szczegółowe uzasadnienie i stan:
`PODSUMOWANIE-EDSHARPNG-2026-10-24.md`.

## Na później — PlanCake: notatki przypisane do treści Markdown

Michał wskazał https://plan-cake.oire.dev jako źródło ciekawych rozwiązań
notatek do treści Markdown do ewentualnego wykorzystania w EdSharpie.
Dopisać do późniejszego rozpoznania obok Quilla i Paperback; nie jest to
zlecenie wdrożenia teraz ani zatwierdzenie całego sposobu zapisu notatek.
Repozytorium wskazane na stronie: https://github.com/Oire/plan-cake.

Według odczytanej strony autora PlanCake pozwala dodawać notatki do akapitu,
pozycji listy, nagłówka, wiersza tabeli lub bloku kodu z poziomu czytanego
podglądu. Notatka trafia bezpośrednio do tego samego pliku za danym blokiem,
domyślnie między `[usernote]` i `[/usernote]`. Autor opisuje też listę notatek,
nawigację F9/Shift+F9, cofanie, zachowanie miejsca po zmianie zewnętrznej,
ochronę przed nadpisaniem niewidzianych zmian i odczyt notatek wraz z kontekstem
przez CLI/JSON. Są to deklaracje autora, nie wyniki własnego testu.

- [ ] Zbadać w kodzie i praktyce dodawanie notatki przy czytaniu podglądu,
  powiązanie z miejscem w źródle oraz wygodną nawigację klawiaturą/NVDA.
- [ ] Porównać z obecnymi komentarzami i przypisami EdSharpa; nie dublować
  tych funkcji ani nie narzucać nowych znaczników bez oceny kompatybilności.
- [ ] Ocenić zapis w tym samym Markdownie, listę notatek z kontekstem,
  cofanie i współpracę z zewnętrzną edycją/asystentem AI. Zachować niezapisany
  tekst użytkownika i pozycję w dokumencie.
- [ ] Przed adaptacją kodu zweryfikować jego licencję i zależności;
  strona deklaruje Apache-2.0. Nie wykonano instalacji ani prób GUI.

## Na później — Quill: rozbudować nasze snippets, nie tworzyć drugiej biblioteki

Michał wskazał połączenie pomysłów Snippets/Abbreviations z Quill Lite z już
istniejącymi funkcjami EdSharpa: Alt+S Save Snippet, Alt+V Invoke Snippet oraz
Alt+Shift+V View Snippet. Zachować pliki użytkownika w `Snippets\Default`,
ich format i dotychczasowe skróty. To kierunek późniejszej rozbudowy,
nie zlecenie implementacji teraz ani zmiany technologii edytora.

- [ ] Rozważyć listę z podglądem, wyszukiwaniem i najczęściej używanymi wpisami
  na bazie istniejącego wyboru, bez osobnego magazynu treści.
- [ ] Dodać możliwość przypisania skrótu tekstowego do istniejącego snippetu
  i automatyczne rozwijanie podczas pisania. Michał potwierdził tę funkcję
  jednoznacznie („na pewno”); nie jest już tylko kandydatem do rozważenia.
  Możliwość wyłączenia pozostaje właściwością funkcji, nie niepewnością zakresu.
  Zachować cofanie i rozróżnienie wpisywania od wklejania; szablon wymagający
  pytań nie może nieoczekiwanie uruchamiać okien w trakcie pisania.
- [ ] Osobno rozpoznać dodatek do NVDA rozwijający skróty globalnie, poza
  EdSharpem — Michał widzi go jako dodatkowy lub alternatywny sposób użycia.
  To pomysł do zbadania, nie zatwierdzona implementacja ani gwarancja działania
  we wszystkich aplikacjach. Michał już próbował dodatków do NVDA typu
  „Expansion” (dokładna nazwa nieustalona): były dla niego niesatysfakcjonujące,
  według jego relacji raczej nie rozwijały skrótów podczas pisania. Quill jest
  wskazanym przez niego działającym wzorcem. Nie kierować go ponownie do
  ogólnego przeglądu tych dodatków jako rozwiązania potrzeby. Sedno: wpisanie
  skrótu i zwykłego separatora ma wywołać zamianę bez listy i bez dodatkowej
  komendy. Przy rozpoznaniu skupić się na wykonalności takiego zachowania
  globalnie oraz wspólnej bibliotece, bez dublowania treści. Uwzględnić wyłączenia
  dla pól haseł/bezpiecznych pulpitów, pauzę, zachowanie schowka oraz zapobieganie
  podwójnemu rozwijaniu w EdSharpie. Rozbudowane szablony edytora są oddzielną
  kwestią; nie zakładać ich pełnej obsługi globalnej bez próby.
- [ ] Uwzględnić istniejące Replace Tokens zamiast dublować mechanizm:
  `EdSharp.cs: ReplaceTokens` odczytuje `[Tokens]`, zastępuje `%Nazwa%`
  tekstem lub zawartością wskazanego pliku snippetu. Obsługa polecenia potrafi
  także wybrać snippet po dopasowaniu wpisanego tekstu do nazwy pliku.
- [ ] Zachować bogatszą składnię obecnych plików: `InvokeSnippet` obsługuje
  nagłówki `text`/`html`, `form` (data, czas, nazwa użytkownika, pola
  `%Nazwa=wartość%`, własne tokeny) i `caret` (pozycja `^^`).

Powyższe istniejące mechanizmy potwierdzono odczytem kodu, bez testu GUI tej
funkcji w tej rozmowie. Pomysł Pythona/wxPython pozostaje luźnym rozważaniem
Michała; nie ma decyzji o przepisaniu EdSharpa. Samo dodanie tych funkcji
nie wymaga zmiany C#/WinForms.

## Na później — Sygnalista, formularz bez GitHuba (05.10.2026)

Michał zlecił rozpoznanie systemu Michała Dziwisza i późniejsze wdrożenie do
EdSharpNG oraz AMC. Nie wdrażać teraz i nie dołączać do obecnej naprawy importów.
W EdSharpie wrócić przy późniejszym wznowieniu prac, bez terminu. W AMC kolejność:
Python → decyzja technologii/dystrybucji i poprawki → próba Apple Music →
porządki menu/interfejsu oraz zgłoszenia. Nie zmienia to pierwszeństwa AMC Python.

Źródło odczytane: https://github.com/michaldziwisz/sygnalista,
commit `504be74f6be9f56bcfa75b23269ef7fedb07bfff`, MIT. Kod Workera potwierdza
JSON `POST /v1/report` → issue we wskazanym repo GitHub, bez konta zgłaszającego
ani otwierania przeglądarki. Wymaga własnej instancji Cloudflare i uprawnień
GitHuba przechowywanych wyłącznie po stronie usługi. Nie jest zależne od
Pythona: dostarczony klient jest pythonowy, lecz EdSharp może wywoływać HTTP z C#.
To analiza kodu, nie wykonana wysyłka ani uruchomiona usługa.

- [ ] Prosty dostępny formularz: temat, opis, opcjonalny e-mail, Wyślij/Anuluj.
  Bez kategorii ani wyboru programu; app.id/wersja i wymagane API kind=bug
  ustawiane przez aplikację. Lokalna kopia i zachowanie treści przy błędzie.
- [ ] Dostosować istniejące `EdSharp.cs: ReportProblem(...)` i `Web.cs`, nie
  tworzyć drugiego formularza. Obecny ReportUrl wysyła product/version/kind/
  subject/email/body/environment, NIE kontrakt Sygnalisty app/title/description.
  Samo wstawienie URL nie wystarczy. Sprawdzić HTTP201 + ok/reportId/issue,
  a nie dowolne2xx. Zastąpić domyślne otwieranie GitHuba bezpośrednią wysyłką;
  brak sieci nie może kasować tekstu ani udawać sukcesu.
- [ ] Wspólna własna usługa z AMC, oddzielny app.id i cel
  michalkasperczak/EdSharpNG; prywatne intake na dodatkowe dane, sekret GitHuba
  nie trafia do aplikacji. Zachować MIT, ustalić retencję danych przy wdrożeniu.
- [ ] Przed wdrożeniem zmienić domyślną prywatność upstreamu: e-mail oraz
  diagnostyka są obecnie dodawane do issue, a jego widoczność wynika z repo.
  Rekomendacja: e-mail tylko prywatnie; logi wyłącznie na świadome życzenie,
  domyślnie wyłączone. Nie wysyłać dokumentów, sekretów ani prywatnych ścieżek
  bez potrzebnego zakresu i jasnej informacji. Usunięcie e-maila z treści issue
  nie usuwa go samo z opisów/logów wpisanych przez użytkownika.
- [ ] Testy: prawdziwe zgłoszenie kontrolne i odczyt właściwego issue, bez
  GitHuba/przeglądarki po stronie zgłaszającego; potwierdzenie przyjęcia,
  błąd sieci/429/5xx i ponowienie bez utraty tekstu lub niejawnego duplikowania,
  obsługa okna awarii, klawiatura/żywy NVDA. Ochrona odbiornika jak w planie AMC.

Szczegółowy wspólny plan: `../../AMC/PLAN-16-09-2026.md`, sekcja
„Na później — Sygnalista, zgłoszenia bez konta GitHub (05.10.2026)”.
Status: wyłącznie rozpoznanie i zapis kolejki; bez zmian produktu/instalacji,
bez utworzenia usług lub wysyłania jakichkolwiek zgłoszeń.

## Na później — Python do skryptów, import PDF i polski słownik synonimów

Ustalenie Michała z 04.10.2026: „To na potem Python do skryptów i jako import
PDF. Słownik po polsku musimy sprawdzić. Zapisz sobie do prac na potem.”

- [ ] Python do uruchamiania skryptów w EdSharpNG. Rozważyć niewielką,
  opcjonalną funkcję z dostępnym wynikiem i możliwością przerwania; nie oznacza
  to przywrócenia całego dawnego środowiska programistycznego.
- [ ] Python jako zaplecze lepszego importu PDF. Porównać na rzeczywistych
  dokumentach odzyskiwanie nagłówków, list i tabel z obecnym importem tekstu.
  Rozwiązanie autora jest tropem do zbadania, nie potwierdzoną poprawą jakości.
- [ ] Sprawdzić słownik synonimów dla języka polskiego: rzeczywiste pokrycie,
  jakość wyników, źródło danych, warunki wykorzystania i dostępność obsługi.
  WordNet wspomniany w rozmowie nie jest potwierdzonym polskim rozwiązaniem.

Sposób dostarczenia Pythona ustalić przy powrocie. Propozycją, a nie osobno
zatwierdzonym projektem, jest środowisko dodatków EdSharpa bez samowolnego
aktualizowania Pythona używanego przez inne programy.

Status: zapisane do późniejszych prac, bez terminu. Nie rozpoczynać teraz
badań ani wdrożenia i nie traktować tego jako wznowienia całego rozwoju.

## Sprawdzone 04.10.2026 — poprawki Pandoca i Pythona z oryginału

Na prośbę Michała porównano nasze rozwiązanie z ostatnimi poprawkami Jamala.
Nie ma potrzeby przenosić tych konkretnych zmian: NG ma własny mechanizm
wykrywania Pandoca w instalacji i profilu, a aktualizatora Pythona autora
w ogóle nie używa. Pomiar binarki 5.0.114 na Hermesie: 10 sprawdzeń zgodnych,
0 błędów, w tym rzeczywiste uruchomienie Pandoca 3.11 i konwersja Markdown
na HTML. Nie zmieniono kodu produktu, nie instalowano niczego i nie wznowiono
pozostałych prac. Szczegóły i ograniczenia: `PANDOC-PYTHON-UPSTREAM-2026-10-04.md`.

## Na później — porządkowanie menu i interfejsu AMC oraz EdSharpa

Michał potwierdził odłożenie tematu: „Tak, zapisz na potem — porządkowanie menu
i interfejsu AMC i EdSharpa”. To zadanie na późniejszy powrót, nie polecenie
wdrożenia teraz ani wznowienia całego rozwoju EdSharpa. Obejmuje również
opisane niżej krótkie pytania i potwierdzenia. Szczegóły pozostałych porządków
menu ustalić przy powrocie; bez samowolnego usuwania funkcji lub zmiany skrótów.


Michał wskazał aktualizator FastPlay jako wzór zwykłego okna Tak/Nie i zgłosił,
że pola tekstowe w krótkich potwierdzeniach EdSharpa, zwłaszcza przy zapisie,
są nietypowe. To korekta preferencji względem dawnego „pole tekstowe wszędzie”.
Zlecił sprawdzenie kodu, nie zbiorową przebudowę ani wznowienie całego projektu.

Sprawdzony FastPlay używa zwykłego wxMessageBox Tak/Nie, a potem osobnego okna
postępu. Dla krótkich pytań rozważyć standardowy dialog i normalne przyciski;
pole tylko do czytania zostawić dla długich raportów/szczegółów. Potwierdzić
realną mowę NVDA oraz Enter/Escape/Tab. Przy niezapisanym pliku Escape musi
anulować zamknięcie, nie wybierać „Nie zapisuj”.

Badanie i źródła: `/home/michal/projekty/amc_pomoc/ui-inspiration-sara-fastplay/update-dialog-review/WNIOSKI.md`.
Nie instalowano FastPlay, nie zmieniono kodu/ustawień EdSharpa. Poniższa przerwa
rozwoju pozostaje w mocy do konkretnego zlecenia.

## Stan na 18.09.2026 — przerwa w pracach po wydaniu 5.0.114

Michał potwierdził w wiadomości z 18.09.2026, że zaktualizował i zainstalował
EdSharpNG u siebie. O Control+Shift+C napisał: „super zadziałało”. To jest
potwierdzenie użytkownika z praktyki, osobne od wcześniejszych testów technicznych.

Plik z opisem nowości ocenił jako czytelny i sensowny.

Pozostałych funkcji nie sprawdził jeszcze w praktyce. Komunikaty, śledzenie
zmian i inne elementy sprawdzi później. Nie oznaczać ich jako zaakceptowanych
przez użytkownika tylko dlatego, że przeszły pomiary techniczne.

Decyzja: przez dłuższy czas nie rozwijamy edytora. Nie wyznaczono terminu
powrotu. Nie rozpoczynać kolejnego etapu ani nowego wydania bez nowego
zlecenia Michała; po powrocie najpierw zebrać jego wyniki i uwagi.

Do późniejszego powrotu pozostają nierozstrzygnięte uwagi z
`UWAGI-MK-18.09.2026.md` oraz dalsze etapy z
`SLEDZENIE-ZMIAN-I-WSPOLNA-PRACA.md`, w tym krok 1c — ukrywanie znaczników.
Nie są one częścią wydania 5.0.114 ani zadaniami do wykonywania teraz.

Opis wydania: `WYDANIE-5.0.114.txt`. Zakres pomiarów i ich ograniczenia:
`WERYFIKACJA-5.0.114.md`. Potwierdzenie publikacji i dostawy:
`../testy/wyniki/delivery-5114.json`.

## Zamknięte zlecenie z 18.09.2026: formatowanie i zapis

Źródło uwag: `docs/UWAGI-MK-18.09.2026.md`, zwłaszcza końcowy opis
kopiowania całego zaznaczenia i zapisu pliku po imporcie z Worda.

Zakres uzgodniony: EdSharp, bez dalszych prac nad AMC w tym wątku.

Koordynacja historyczna: to samo zlecenie wpłynęło w dwóch tematach Telegrama.
Zakończone wdrożenie prowadziła sesja `20260918_002328_08637004` (temat `33010`).
Sesja z tematu `32113` zatrzymała swoje zdublowane zadania po potwierdzeniu
równoległych zmian w kodzie. Nie cofać pracy drugiej sesji. Nie budować dwóch
różnych instalatorów pod numerem 5.0.114. Nowa kopia robocza
`edsharp-rich-copy` powstała w zdublowanym zadaniu; nie scalać jej automatycznie.

- Wydane w 5.0.114: Control+Shift+C dla całego mieszanego zaznaczenia Markdown.
  Bogaty schowek obejmuje nagłówki, odsyłacze, listy i wyróżnienia;
  format do ponownego wklejenia Markdown w EdSharp pozostaje.
- Wydane w 5.0.114: Control+Shift+S z rzeczywistą konwersją do DOCX, EPUB, PDF,
  HTML, RTF oraz zapisem Markdown i tekstu. Błąd konwersji nie może uszkodzić
  poprzedniego pliku ani oznaczyć niezapisanych zmian jako zapisanych.
- Wykonane przed wydaniem: pomiary schowka i plików, obsługi okna zapisu
  żywym NVDA oraz próbna instalacja na Hermesie. Szczegóły w raporcie weryfikacji.
- Dostarczone: wydanie v5.0.114 na GitHubie z instalatorem i ZIP-em,
  kopia instalatora i opis zmian na głównym komputerze Michała.
- Poprawki checklist i drobnych komend uwzględniono w wydaniu oraz
  kontroli regresji; nie czekają już na publikację.

### Zatwierdzona opcja: Control+S zapisuje do formatu źródłowego

MK zatwierdził widoczny przełącznik w normalnym oknie ustawień: opcjonalny zapis
zaimportowanego DOCX, EPUB i innych wspieranych dokumentów z powrotem w formacie
otwartego oryginału. Opcja domyślnie wyłączona; bez niej działa dotychczasowy zapis MD.
Użytkownik świadomie akceptuje utratę czcionek i innych cech wyglądu, bo celem
jest redakcja treści i struktury do prostej publikacji, np. WordPressa.

Wydane w 5.0.114: powiązanie importu z oryginalnym
plikiem, Control+S, kopie poprzednich wersji, wykrywanie zewnętrznych zmian,
odzysk powiązania w sesji, natychmiastowe wyłączenie opcji i odłączenie po zapisie
roboczego Markdowna. Sprawdzone dla DOCX, EPUB, HTML i RTF (nie obiecujemy ODT,
PDF ani starego DOC bez osobnej obsługi). 84 sprawdzenia integracyjne bez błędu;
żywy NVDA potwierdził nazwę i stan checkboxa oraz zapis do DOCX po Control+S.
Dowody: `testy/wyniki/original-integration-5114.log`,
`testy/wyniki/original-live-nvda-5114.json`.
Przegląd domknięty: naprawiono izolację okien odzysku i zachowanie nieodczytanych
kopii (5 kontroli przed poprawką zawodziło), zapis snippetu i stan po zapisie
do oryginału. Rozszerzony zestaw integracji: 107/107. Wydano v5.0.114 na GitHubie
(instalator i ZIP), instalator i opis zmian są na głównym komputerze w
D:\Projekty Codex\Hermes. Starsze 5.0.112 i 5.0.113 przeniesiono do
kosz-2026-09-18. Próba instalacji i uruchomienia wykonana na Hermesie;
instalatora na głównym komputerze nie uruchamiał agent. Michał później
potwierdził, że sam zaktualizował i zainstalował program.
Nie obiecywać bezstratnej edycji dowolnego DOCX.

Próba Pandoca zachowała tekst i nagłówek, lecz utraciła kolor, rozmiar czcionki
i wyrównanie. Wynik: `testy/wyniki/roundtrip-docx-decyzja.json`.

Pozostałe uwagi (m.in. sporadyczny skok podglądu i komunikaty ustawień) nie są
jeszcze oznaczone jako naprawione. Nie rozszerzamy wydania na nowe funkcje
śledzenia zmian ani nie zmieniamy kolejnych skrótów bez osobnego ustalenia.


## Historyczna lista z 11.09.2026

Poniższe wpisy zachowują treść i statusy z wcześniejszych etapów. Nie są
bieżącą kolejką do automatycznego wykonania. Przy powrocie należy sprawdzić
wybrany punkt w aktualnym kodzie i późniejszych decyzjach. Aktualny stan
wydania i decyzja o przerwie znajdują się na początku tego dokumentu.

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
