# Weryfikacja poprawek 5.0.114 — 18.09.2026

Status: opublikowano v5.0.114 z instalatorem i ZIP-em. Instalator i opis zmian
dostarczono na główny komputer. Sprawdzono sumy zgłoszone przez GitHub API
oraz ponowny odczyt instalatora u odbiorcy. Oba pliki pobrane z GitHuba mają
identyczne SHA-256 i rozmiary jak lokalne; ZIP przeszedł kontrolę CRC.
Nie instalowano na głównym komputerze.

## Kopiowanie całego zaznaczenia

Punkt odniesienia: 57a9919. Faktyczny schowek po Control+A, Control+Shift+C
zawierał RTF, UnicodeText i EdSharpNG.Markdown, ale nie HTML. Nagłówki RTF
były jedynie pogrubione i powiększone, bez poziomów konspektu.

Po poprawce: 18 asercji w pomiar_bogaty_schowek_5114.cs, trzy kolejne przebiegi
bez błędów. Badane są również surowe bajty CF_HTML odczytane przez Win32,
nie tylko ponowny odczyt przez .NET. Wykryło to utratę polskich liter na drodze
ANSI/UTF-8. Fragment HTML używa teraz referencji znakowych dla znaków spoza
ASCII, a pozycje fragmentu liczone są po serializacji.

Pomiar klawiszami żywego NVDA: Control+A, Control+Shift+C; odczyt schowka
w pomiar_bogaty_schowek_5114.ps1: przed poprawką 3 PASS / 2 FAIL, po dodaniu
HTML i semantycznych nagłówków 9 PASS / 0 FAIL.

Word na głównym komputerze: osobna, niewidoczna instancja otworzyła wyłącznie
pliki próbne, bez zmieniania schowka ani dokumentów użytkownika. RTF przed
poprawką: zero nagłówków. RTF po poprawce: poziomy 1 i 2, link i dwie pozycje
listy. HTML po poprawce: również dwa nagłówki, link i lista. Pełny odczyt:
`testy/wyniki/word-bogaty-schowek-5114.json`. Instancja testowa została zamknięta,
a zadania tymczasowe usunięte; potwierdzono brak pozostawionego procesu Worda.

Rzeczywiste wklejenie do contenteditable w Edge, na lokalnej stronie bez konta:
przed poprawką zero nagłówków, linków i list; po poprawce dwa nagłówki,
jeden link i jedna lista, pełna polska treść bez powtórzeń. Wczesny wariant HTML
ujawnił uszkodzone polskie litery, więc nie został uznany za wynik poprawny.
Odczyt DOM przed/po: `testy/wyniki/edge-schowek-5114.json`.

Nie badano konkretnej instalacji WordPressa ani Thunderbirda. Sprawdzono
standardowy bogaty edytor Chromium, MSHTML i odczyt formatów przez Worda;
wklejanie jako zwykły tekst z definicji nie zachowuje formatowania.

Dotychczasowy pomiar_kopiowania_zaznaczenia.cs: 54 PASS / 0 FAIL.

## Zapis dokumentów

ZapisFormatow.cs zintegrowany z Control+Shift+S. Niezależny przegląd i test
rzeczywistej binarki wykryły problemy, których pierwszy zestaw testów nie widział:

- niezerowy kod zakończenia konwertera przy niepustym wyjściu;
- niepusty, ale niepoprawny plik DOCX;
- ścieżki ze spacjami i polskimi znakami (fałszywy ekspander testowy sam dodawał
  cudzysłowy, których nie dodawała produkcja);
- starsze polecenia Pandoca w rzeczywistym pliku ustawień Michała;
- skasowanie celu przed przeniesieniem gotowego wyniku;
- zmienianie spacji wewnątrz ścieżek podczas usuwania brakującego szablonu.

Naprawione: kod zakończenia jest sprawdzany, wynik ma zweryfikowany format,
istniejący cel jest zastępowany przez File.Replace, a błędy zostawiają go
nietkniętym. Stare polecenia są normalizowane w pamięci, bez edycji pliku
ustawień użytkownika. Brakujący opcjonalny reference.docx z domyślnego wpisu
nie blokuje konwersji; istniejący szablon pozostaje.

Ważne rozróżnienie: domyślny wpis programu wymagał brakującego szablonu;
rzeczywisty wpis Michała go NIE wymaga, lecz używa markdown_github i zewnętrznej
pary cudzysłowów. Oba przypadki są objęte testami.

`harness_zapis_formatow.cs`: 89 PASS / 0 FAIL po przejściu delegata na kod
zakończenia i zastąpieniu ASCII w próbie polskich liter prawdziwymi ogonkami.

`harness_zapis_bezpieczny_5114.cs`: przed poprawkami 1 PASS / 6 FAIL,
po poprawkach 8 PASS / 0 FAIL (podzbiór zależny od udanej konwersji wykonuje
się dopiero po naprawie).

`harness_integracja_zapisu_5114.cs`, rzeczywista metoda z EdSharpNG.exe,
prawdziwy odczyt INI i ExpandCommandLine: przed cytowaniem ścieżek
4 PASS / 5 FAIL; po poprawce 9 PASS / 0 FAIL. DOCX, EPUB, HTML i RTF zapisane
w katalogu z polską nazwą i spacjami.

Żywe NVDA rozpoznało pole „Zapisz jako typ:” i pozycję „Word document (*.docx)”.
Enter utworzył DOCX ze stylami Heading1 i Heading2, linkiem i pełną treścią.
Osobna próba: dopisana, niezapisana litera trafiła do eksportu DOCX, ale źródło
MD pozostało nietknięte. Następne Control+S zapisało MD i NIE zmieniło sumy
kontrolnej DOCX. Bufor po eksporcie pozostał źródłem Markdown.

PDF: przechodzi przez zainstalowany Edge, bez wymagania LaTeX. Końcowy
`verify_save_bundle.sh` na kandydacie 5.0.114: 25 testów helpera PDF, 89 konwersji,
8 ochrony zapisu, 11 integracji gotowego EXE — wszystkie bez błędu.
Niezależny odczyt PDF z EXE potwierdził polski tekst, /Marked, StructTreeRoot,
H1, listy, link i URI. Próba pozbawiona tagów powoduje błąd sondy.
Nie jest to certyfikacja PDF/UA ani pełny audyt dostępności dowolnego PDF.
Logi: `testy/wyniki/save-bundle-5114/`.

## Opcjonalny zapis do formatu otwartego dokumentu

Widoczna opcja w Settings, domyślnie wyłączona:
„Control+S saves imported documents in their original format”.
Działa na plikach DOCX, EPUB, HTML i RTF zaimportowanych do Markdowna;
nie dotyczy PDF ani starszego DOC. Wyłączenie nie wymaga restartu.
Zapis oryginału tworzy poprzednią wersję w `.edsharp-backups` obok dokumentu.
Ochrona wykrywa zmianę oryginału i nie przepuszcza błędu do surowego zapisu.
Bufor zachowuje format Markdown. Jawny zapis jako MD odłącza oryginał.
Nie jest to bezstratna edycja Worda: wygląd i cechy niewspierane w Markdownzie
mogą zostać utracone, co użytkownik świadomie zaakceptował.

Sprawdzone na rzeczywistym programie: 84 asercje importu, Control+S, ponownego
zapisu, odczytu gotowego dokumentu, kopii, czystej sesji, odzysku lokalnych zmian,
konfliktu po zmianie źródła oraz odłączenia powiązania. Osobno 9 testów opcji
i metadanych, a poprzedni zestaw sesji nadal 74/74.

Transakcja plikowa: 98/98, powtórzone dziesięć razy. Nowe kontrole przed
poprawką dawały 4 błędy: nieudane zdobycie blokady nie mogło dopuszczać zapisu,
a plik należało ponownie sprawdzić po oczekiwaniu na inny zapis.
Odcisk nowej treści jest liczony przed podmianą, z wyniku konwersji.
Istnieje nadal wąskie okno wyścigu z programem niestosującym blokady EdSharpa;
kopią jest wersja faktycznie zastąpiona przez File.Replace.

Żywe NVDA: pole wyboru odczytane z nazwą i stanem, Space zmienił stan na
zaznaczone, Control+Enter utrwalił opcję. Tytuł okna wskazał DOCX (Markdown).
Po dopisaniu litery i Control+S DOCX zawierał nową treść, a kopia odpowiadała
poprzedniej sumie kontrolnej. Dziennik NVDA zawiera potwierdzenie zapisu.
Dowód: `testy/wyniki/original-live-nvda-5114.json`.

Końcowa regresja na kandydacie: schowek bogaty 18/18 (trzy przebiegi),
poprzednie kopiowanie zaznaczenia 54/54, zachowanie linków na listach 12/12.
Logi: `testy/wyniki/final-*-5114.log`.


## Checklisty i wcześniejsze drobne zmiany

Wcześniejsza interpretacja uwagi MK była błędna: napisał o USUWANIU nawiasów,
nie o ich pozostawaniu. Prób na „[-]” nie wolno przedstawiać jako odtworzenia
jego zgłoszenia.

Dokładny przypadek odtworzony żywym NVDA: dwie pozycje „- [ ] chleb” i
„- [x] mleko” po Control+Shift+L traciły pola. Po poprawce dają
„1. [ ] chleb” i „2. [x] mleko”. Control+L przywraca punktory z zachowaniem
obu pól. Kolejne Control+L wyłącza listę całkowicie, pozostawiając zwykły tekst.

Luźne zdejmowanie pól zostało ograniczone, aby nie kasować etykiet linków
ani odwołań. `pomiar_listy_bez_utraty_linkow_5114.cs`: 12 PASS / 0 FAIL.

Naprawiony opis nieprzypisanego skrótu Manual Options: puste pole zamiast
niepoprawnego „(menu only)”. Audyt skrótów: zero rozbieżności; generator
potwierdza zgodność podsumowania z Hotkeys.ini.

Nie uznajemy dawnych prób SendKeys za dowód przyczyny gubienia znaków. Brak
kontroli wejścia unieważnia pomiar; rozgrzewka nie dowodzi winy inicjalizacji.

## Rozstrzygnięcie końcowego przeglądu

Odtworzono dwa istotne problemy odzyskiwania sesji: błędne przypisanie tekstu
do wcześniejszego okna po nieudanym re-imporcie oraz usunięcie nieodczytanej
kopii. Nowa próba na dwóch dokumentach oraz blokowanych plikach dała przed
naprawą 5 błędów. Po naprawie odzysk czyta kopię bez zależności od konwertera,
każdy dokument dostaje własne okno, nieodczytane kopie i ich wpisy pozostają
na kolejną próbę również po zwykłym wyjściu. Nie liczymy okna, które nie powstało.

Zapis snippetu zaimportowanego dokumentu nie zmienia jego powiązania ani stanu
niezapisanych zmian. Edycja jako CSV wymaga osobnego, istniejącego pliku.
Pełny zapis jako ten sam oryginał przy włączonej opcji zeruje stan zmian.
Komunikat konfliktu wskazuje Control+Shift+S jako drogę zapisania pracy pod
nową nazwą. Treść komunikatu i zachowanie zewnętrznej wersji sprawdzono
żywym NVDA: `testy/wyniki/original-conflict-nvda-5114.json`.

Końcowy test integracyjny po tych zmianach: 107 PASS / 0 FAIL.
Dowody przed i po: `testy/wyniki/session-review-*-5114.log`,
`testy/wyniki/save-lifecycle-review-*-5114.log`.

Nie przyjęto postulatu zablokowania jawnego Zapisz jako / Eksportu przy
wyłączonym przełączniku: przełącznik dotyczy Control+S. Świadomy eksport
nadal jest dostępny; dla celu będącego oryginałem zachowuje kontrolę konfliktu
i kopię, niezależnie od przełącznika. Nie dodajemy automatycznego przyjmowania
cudzego odcisku po konflikcie, ponieważ omijałoby to ochronę cudzej pracy.

## Końcowy kandydat 5.0.114

Po przebudowie: transakcja 98/98, opcja 9/9, integracja 107/107, poprzednia
sesja 74/74, schowek bogaty 18/18 w trzech przebiegach, pozostałe kopiowanie
54/54, listy 12/12. Pełny zestaw zapisu i PDF także zakończył się bez błędu.
Dokładny log: `testy/wyniki/final-combined-5114.log`.

Końcowy instalator zainstalowano na Hermesie (kod 0). Rejestr odinstalowania
wskazał 5.0.114, zainstalowany EXE ma tę samą sumę co plik zbudowany.
Program uruchomił się z Program Files i otworzył rich-copy-fixture.md.
Paczka ZIP została sprawdzona pod kątem CRC i zgodności EXE, rozpakowana
w osobnym katalogu, uruchomiona i otworzyła ten sam dokument.

Sumy i rozmiary: `testy/wyniki/artifacts-5114.json`.
Publikacja i dostawa są potwierdzane osobno u źródła, nie przez sam build.

