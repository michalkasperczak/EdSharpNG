# Zadania testowe EdSharpNG, wersje od 5.0 do 5.0.21

Lista jest po polsku, ale komunikaty programu podaję po angielsku, bo interfejs jest angielski i tak je usłyszysz.

Czego ta lista NIE jest: potwierdzeniem, że wszystko działa. Sprawdzam u siebie logikę w kodzie, czyli to, jaki tekst program wyliczy i gdzie postawi kursor.

AKTUALIZACJA 20.08.2026: testy o mowie rozstrzygam już SAM, także te z rozdziału 3. Na maszynie z Windowsem stoi NVDA 2026.1.1 z mostkiem MCP, do pomiaru wymowy służy dodatek "podsluchMowy" (zapisuje każdą wypowiedź czytnika), a klawisze wysyłam przez systemowy SendInput, więc docierają do programu jak z prawdziwej klawiatury. Wyniki oznaczone są niżej jako "ZMIERZONE".

Jedna uwaga o metodzie, bo zmieniła wnioski: sama mowa czytnika to za mało jako miara. NVDA nie zawsze ogłasza ruch kursora przy wejściu programowym, a wtedy cisza wygląda jak "komenda nie zadziałała". Dlatego każdy test rozdziału 3 ma DWIE miary: skutek w programie (pozycja kursora czytana wprost z pola edycji) oraz mowę czytnika. Skutek mówi, czy komenda się wykonała, mowa - czy brzmi poprawnie. Gdzie mowy nie było, piszę to wprost, zamiast zgadywać.

Czego nadal NIE potrafię sprawdzić: JAWS-a nie mam, więc wszystko, co dotyczy jego zachowania, zostaje dla Ciebie.

Jak zgłaszać: wystarczy numer testu i jedno zdanie, co usłyszałeś albo co się stało. Nie musisz przechodzić listy po kolei ani całej. Testy oznaczone jako pilne to te, których u siebie w ogóle nie sprawdziłem.

Do testów przyda się jeden plik z nagłówkami Markdown i kilkoma akapitami. Ten plik, który właśnie czytasz, sam się do tego nadaje: ma nagłówki na kilku poziomach i blok z przykładem kodu.

## 1. Nawigacja po dokumencie pod F6 (najnowsze, wersja 5.0.21) - PILNE

1.1. Otwórz plik z nagłówkami i naciśnij F6. Oczekiwane: otwiera się okno "Document Navigation" z drzewem nagłówków, kursor w drzewie stoi na nagłówku, w którym byłeś w tekście.

ZMIERZONE 19.08.2026 (NVDA 2026.1.1): okno się otwiera, NVDA ogłasza "Document Navigation", potem "Enter goes to the heading, drzewo". Kursor w drzewie stanął na nagłówku, w którym byłem w tekście.

1.2. W drzewie chodź strzałkami w górę i w dół. Oczekiwane: czytnik wymawia tytuł nagłówka, POZIOM NAGŁÓWKA W POSTACI "heading 2" na końcu nazwy, oraz - przy nagłówku, który ma podnagłówki - czy gałąź jest zwinięta czy rozwinięta. Przykład tego, co ma być słyszalne: "2. Sekcje, czyli nagłówki Markdown, heading 2".

ZMIENIONE 26.08.2026 (wersja 5.0.23, na Twoje zlecenie): poziom nagłówka jest teraz DOPISANY do nazwy pozycji, więc nie zależy od tego, co czytnik wyliczy z drzewa. Uwaga na dwie liczby, które mówią o czym innym: "poziom 1", które słyszysz od czytnika, to GŁĘBOKOŚĆ GAŁĘZI (jak głęboko pozycja wisi w drzewie), a "heading 2" na końcu nazwy to PRAWDZIWY poziom nagłówka Markdown. Głębokości gałęzi nie da się wyciszyć po naszej stronie.

ZMIERZONE 19.08.2026, przed tą zmianą: stan gałęzi słyszalny - przy strzałce w lewo na nagłówku z podnagłówkami "zwinięte", przy strzałce w prawo "rozwinięte, 3 elementy". Poziomu czytnik nie powtarzał przy przechodzeniu MIĘDZY nagłówkami tego samego poziomu; po zmianie z 26.08 poziom nagłówka jest częścią nazwy, więc słyszysz go przy każdej pozycji.

1.3. Strzałka w prawo na nagłówku z podnagłówkami. Oczekiwane: gałąź się rozwija. Strzałka w lewo: zwija ją, a na już zwiniętej przenosi na nagłówek nadrzędny.

1.4. Wpisz literę. Oczekiwane: przeskok do najbliższego nagłówka zaczynającego się od tej litery.

1.5. Ustaw się na wybranym nagłówku i naciśnij Enter. Oczekiwane: okno się zamyka, kursor w tekście stoi na tym nagłówku, a program mówi tytuł i poziom, na przykład "Installation, heading 3" - dokładnie tak, jak przy chodzeniu Controlem ze strzałkami. Sprawdź, czy ten komunikat nie ginie przy zamykaniu okna.

ZMIERZONE 19.08.2026: komunikat NIE GINIE. Po Enter NVDA mówi "Plik testowy EdSharpNG 5.0.21, heading 1", a dopiero potem tytuł okna i "pole edycji, wielowierszowe". Kursor stanął na wybranym nagłówku.

1.6. Otwórz F6, przejdź na inny nagłówek i naciśnij Escape. Oczekiwane: okno się zamyka, kursor w tekście NIE rusza się.

1.7. Po teście 1.6 naciśnij F6 ponownie. Oczekiwane: drzewo otwiera się na tym nagłówku, na którym byłeś przed Escapem, a nie na początku.

1.8. Naciśnij F6 w dokumencie bez żadnego nagłówka (na przykład w zwykłej notatce). Oczekiwane: sam komunikat "No headings!", okno się NIE otwiera.

1.9. W dokumencie z blokiem kodu (trzy odwrotne apostrofy, a w środku linia zaczynająca się od kratki) naciśnij F6. Oczekiwane: linia z kratki WEWNĄTRZ bloku kodu nie jest nagłówkiem i nie ma jej w drzewie. To była pomyłka programu, którą znalazłem przy okazji - dotyczyła też testów z rozdziału 2, więc warto sprawdzić oba.

ZMIERZONE 19.08.2026: PRZECHODZI. Zebrałem całe drzewo (7 pozycji) i linii "# to jest komentarz w kodzie" z wnętrza bloku ``` w nim NIE MA. Wszystkie 7 realnych nagłówków pliku jest obecnych, więc filtr nie odsiał niczego za dużo.

1.10. Control+F6, Alt+F6 i Control+Shift+F6 należały od 5.0.28 do PRZYPISÓW, opisanych w rozdziale 16; od 5.0.46 przypisy przeszły na rodzinę K (Control+Shift+K wstawia, Alt+K to lista), kontekstowy skok został na Alt+F6, a Control+F6 czeka na listę linków - rozdział 29. Stare "szukanie tematu" zostało usunięte - sam zgłosiłeś je jako niedziałające w tym punkcie, a przyczyną było to, że szukało podziałów kreskami, których w Markdownie nie ma. Shift+F6 ma od 5.0.25 nowe zadanie: skacze między spisem treści i rozdziałem, opisane w rozdziale 15.

1.11. Stara komenda "Go to Section" (kiedyś pod F6, potem pod Control+Shift+F12) została USUNIĘTA z EdSharpNG 26.08.2026 na Twoje polecenie. Oczekiwane: w menu Navigate nie ma pozycji "Go to Section", a Control+Shift+F12 nic nie robi.

## 2. Sekcje, czyli nagłówki Markdown

2.1. Ustaw kursor w środku sekcji i naciśnij Control+Enter. Oczekiwane: program wstawia nowy nagłówek na tym samym poziomie, co nagłówek, w którym jesteś, i czeka, aż wpiszesz tytuł. Przed pierwszym nagłówkiem pliku wstawia nagłówek poziomu 1.

2.2. Control+PageDown i Control+PageUp. Oczekiwane: przeskok do następnego i poprzedniego nagłówka KAŻDEGO poziomu, po skoku słyszysz tytuł i poziom, na przykład "Instalacja, heading 3". Bez słowa "level" i bez czytania tekstu pod nagłówkiem.

2.3. Control+Shift+PageDown i Control+Shift+PageUp. Oczekiwane: przeskok tylko po nagłówkach TEGO SAMEGO poziomu, podnagłówki są pomijane.

2.4. Dojedź do ostatniego nagłówka i naciśnij Control+PageDown jeszcze raz. Oczekiwane: kursor STOI, komunikat "Last heading!". Na początku: "First heading!". Przy wariantach z Shiftem: "Last heading at this level!" i "First heading at this level!".

2.5. Control+Alt+strzałka w górę i w dół. Oczekiwane: cała sekcja razem z podsekcjami przenosi się przed poprzednią albo za następną, a program mówi "Above" albo "Below" i tytuł tej drugiej sekcji.

2.6. Naciśnij Control+PageDown w dokumencie z blokiem kodu, w którym jest linia z kratką. Oczekiwane: nawigacja NIE zatrzymuje się na takiej linii (poprawione w 5.0.21).

2.7. Control+Shift+Enter to nadal Trim Blanks, tak jak chciałeś. Sprawdź, że Control+Enter go nie przejął.

## 3. Nawigacja po tekście i czytanie - tu było najwięcej poprawek

3.1. PILNE, wersja 5.0.20: Alt+strzałka w górę i w dół, czyli zdania. Oczekiwane: zdanie jest czytane RAZ. Wcześniej słyszałeś je dwa razy, a czasem raz - i o to "czasem" właśnie chodziło: program i czytnik wyścigowali się o głos. Teraz program milczy i oddaje mowę czytnikowi, a tekst zdania trafia na pasek stanu, więc da się go dopytać.

ZMIERZONE 20.08.2026, PRZECHODZI. Na akapicie z czterech zdań Alt+strzałka w dół przesuwa kursor dokładnie o jedno zdanie (ze znaku 0 na 35, potem o kolejne 50), a zdanie jest wypowiadane jeden raz, bez powtórzenia. Sprawdź proszę u siebie tylko jedno: czy tak samo brzmi to na JAWS-ie.

3.2. Control+strzałka w lewo i w prawo, czyli słowa, na tekście z polskimi literami (na przykład "Parafia Wszystkich Świętych oraz żarówka"). Oczekiwane: każde słowo czytane raz i CAŁE. Wcześniej program łamał słowo na polskiej literze ("wszystkich", potem samo "ś", potem "więtych") i doklejał poprzednie słowo.

ZMIERZONE 20.08.2026, PRZECHODZI - to najmocniejszy wynik z całego rozdziału. Na zdaniu "Parafia Wszystkich Świętych oraz żarówka" czytnik wymówił kolejno: "Wszystkich", "Świętych", "oraz", "żarówka" - słowa całe, z ogonkami, ani jednej samotnej litery. Kursor idzie równo do przodu (znaki 179, 190, 199, 204). Dawny defekt łamał "wszystkich" na "ś" i "więtych" i już go nie ma.

3.3. Control+strzałka w górę i w dół, czyli akapity. Oczekiwane: słyszysz CAŁY akapit, a nie sam pierwszy wiersz, i nic nie jest powtórzone dwa razy. Sprawdź to w dwóch układach: akapit będący jednym długim zdaniem z zawijaniem wierszy, oraz akapit złożony z kilku krótkich linii przełamanych Enterem.

ZMIERZONE 20.08.2026, PRZECHODZI dla akapitu z zawijaniem: kursor skacze o cały akapit (171 znaków), a czytnik podaje jego treść w całości, raz. Drugiego układu (kilka krótkich linii przełamanych Enterem) NIE zmierzyłem - to nadal prośba do Ciebie.

3.4. Alt+strzałka w lewo i w prawo, czyli fragmenty. Oczekiwane: bez zmian, program mówi tu sam, bo tych klawiszy czytnik nie zajmuje. Nawigacji po częściach (Alt+PageUp i Alt+PageDown) już nie sprawdzamy: została usunięta 27.08.2026 na Twoje polecenie, razem z Alt+Shift+G, bo działała tylko z wzorcem ustawianym pod język programowania. Te trzy skróty są teraz wolne i nic nie robią.

3.5. Zwykłe strzałki w górę i w dół. Oczekiwane: wiersz czytany raz, jak zawsze. To test kontrolny - jeśli tu też słyszysz dwa razy, przyczyna jest w ustawieniach czytnika, nie w programie.

ZMIERZONE 20.08.2026, PRZECHODZI. Kursor schodzi o jeden wiersz i czytnik podaje go raz. Ten test był u mnie ważny podwójnie: służył jako kontrola samego pomiaru. Gdy zwykła strzałka - która MUSI mówić w każdym edytorze - milczała, wiedziałem, że wina jest w moim sposobie mierzenia, a nie w programie. Dzięki temu nie zgłosiłem Ci fałszywego defektu.

3.6. Control+F1 (Key Describer), a potem Control+strzałka w prawo i Alt+strzałka w dół. Oczekiwane: opis klawisza NIE obiecuje już, że "EdSharp przeczyta" słowo albo zdanie, bo teraz robi to czytnik. Przy akapitach opis mówi, że czytnik czyta, a program dodaje dalsze wiersze akapitu.

3.7. Alt+Shift+H otwiera pełną listę skrótów w osobnym oknie. Oczekiwane: plik się otwiera i skróty w nim zgadzają się z tym, co realnie robią klawisze - szczególnie te przeniesione, wypisane w rozdziale 6.

## 4. Zakładki

4.1. Control+K stawia zakładkę, Control+Shift+K ją usuwa, Alt+K skacze. Oczekiwane: przy jednej zakładce Alt+K skacze od razu, przy wielu otwiera listę "Bookmarks" z domyślnym wyborem najbliższej za kursorem.

4.2. Na liście Alt+K naciśnij Delete albo Backspace. Oczekiwane: zakładka znika, komunikat "Bookmark removed", lista zostaje otwarta, a kursor na tej samej pozycji listy. Po usunięciu ostatniej lista sama się zamyka.

4.3. Shift+PageDown i Shift+PageUp. Oczekiwane: skok do następnej i poprzedniej zakładki BEZ listy, po skoku czytany cały wiersz.

4.4. Naciśnij Shift+PageDown na ostatniej zakładce. Oczekiwane: kursor stoi, komunikat "Last bookmark!". Na pierwszej w drugą stronę: "First bookmark!". Bez zawijania na drugi koniec pliku.

4.5. Test kontrolny: Shift+PageUp i Shift+PageDown to normalnie systemowe zaznaczanie strony tekstu. Sprawdź, czy w EdSharpNG nic ci przypadkiem nie zaznacza.

4.6. W pliku bez zakładek naciśnij Alt+K. Oczekiwane: "No bookmark!".

## 5. Pliki numerowane i okna

5.1. Alt+Shift+cyfra przypisuje otwarty plik do numeru. Oczekiwane: "Numbered file 3 is nazwa.md". Sprawdź WSZYSTKIE dziesięć: 1 do 9 oraz 0, które jest numerem dziesiątym. Numer 6 też ma się dać przypisać - komenda Baseline, która wcześniej zajmowała Alt+Shift+6, jest teraz pod Alt+Shift+F6.

5.2. Alt+cyfra idzie do przypisanego pliku. Oczekiwane: jeśli plik jest już otwarty, słyszysz samą nazwę pliku; jeśli zamknięty, "Opening" i nazwę. Bez słowa "returning".

5.3. Alt+cyfra na nieprzypisanym numerze. Oczekiwane: "Numbered file 3 is empty!". Gdy przypisany plik zniknął z dysku: "Numbered file 3 not found!".

5.4. Alt+Shift+F2 otwiera listę przypisanych plików. Oczekiwane: każda pozycja mówi numer, nazwę i ścieżkę, Enter otwiera. Gdy nic nie jest przypisane: "No numbered files are assigned!".

5.5. Zamknij i uruchom program ponownie. Oczekiwane: przypisania pod Alt+cyfra przetrwały.

5.6. Alt+Shift+cyfra w nowym, niezapisanym oknie. Oczekiwane: "No disk file is open for this command!".

5.7. Control+1 do Control+9 chodzi po OTWARTYCH oknach w kolejności otwierania, nie ostatniego użycia. Oczekiwane: Control+1 to pierwszy otwarty plik, program mówi jego nazwę. Sprawdź szczególnie Control+4 i Control+6 - do wersji 5.0.13 były zajęte.

5.8. Control+7, gdy otwarte są trzy okna. Oczekiwane: kursor stoi, komunikat mówi, ile okien jest otwartych.

5.9. Control+W zamyka okno, tak samo jak Control+F4. Control+Shift+F4 zamyka wszystkie poza bieżącym.

## 6. Skróty przeniesione - czy stare komendy nadal działają

Każdą z tych komend przeniosłem na Twoją zgodę, żeby zwolnić klawisz na nową funkcję. Test jest jeden i ten sam: komenda ma działać na nowym klawiszu I mówić (na nieudanym skrócie komenda potrafi się wykonać, ale zamilknąć).

6.1. Append from Clipboard: Alt+Shift+C, czyli tam, gdzie sam chciałeś (5.0.33). Alt+F9, o którym mówiła wcześniejsza wersja tej listy, było tylko naszym tymczasowym zastępnikiem.
6.2. Compiler: Control+F9 (było Alt+0).
6.3. Word Wrap i Unwrap: OD 5.0.34 BEZ SKRÓTÓW, tylko w menu Miscellaneous - tak zdecydowałeś ("oba do menu"). Oczekiwane: obie pozycje są w menu i działają, komunikaty "Word wrap" i "Unwrap", a Control+F12 oraz Control+Shift+W nic już nie robią. Control+W nadal zamyka okno. Sprawdź też Control+F1 na tych pozycjach w menu: program nie powinien obiecywać żadnego klawisza.
6.4. Baseline: Alt+Shift+F6 (było Alt+Shift+6).
6.5. Reset Configuration: Alt+Shift+F10 (było Alt+Shift+0).
6.6. Format Code: USUNIĘTA z EdSharpNG 26.08.2026 na Twoje polecenie. Oczekiwane: pozycji "Format Code" nie ma w menu Miscellaneous. Zwolniony Control+Shift+F6 dostał w 5.0.28 listę przypisów, ale od 5.0.46 lista przeniosła się na Alt+K, więc ten klawisz jest znowu wolny - patrz rozdział 29.
6.7. Next Baseline: Control+F2, Prior Baseline: Control+Shift+F2 (były Control+6 i Control+Alt+6).
6.8. Go to Special Folder: Control+Alt+0.
6.9. Go to Section, Go to Contents i Text Contents w starej formie: USUNIĘTE 26.08.2026 na Twoje polecenie. Control+Shift+F12 pozostaje wolny i nic nie robi. Shift+F6 i Alt+Shift+T dostały 27.08.2026 nowe komendy markdownowego spisu treści, opisane w rozdziale 15.
6.10. Control+F1 na każdym z tych klawiszy. Oczekiwane: opis podaje NOWY skrót. Błędny opis jest gorszy niż brak opisu, więc to warto sprawdzić wyrywkowo.

## 7. Formatowanie Markdown

7.1. Control+Shift+1 do Control+Shift+6 na linii z tekstem. Oczekiwane: linia staje się nagłówkiem tego poziomu, komunikat "Heading 3". Sprawdź szczególnie 6 - kiedyś był połykany.

7.2. Control+Shift+7 i Control+Shift+8. Oczekiwane: przełączanie listy wypunktowanej i numerowanej.

7.3. Enter na końcu pozycji listy. Oczekiwane: program sam zaczyna następną pozycję. Enter na PUSTEJ pozycji kończy listę.

7.4. Control+Shift+9. Oczekiwane: wstawia link Markdown.

7.5. Control+Shift+0. Oczekiwane: czyści formatowanie, komunikat "Clear formatting".

7.6. Control+Shift+C na linku Markdown, potem wklej do Worda. Oczekiwane: wkleja się prawdziwy odsyłacz, nie surowy tekst z nawiasami.

7.7. Control+Shift+C na liście Markdown, potem wklej do Worda. Oczekiwane: wkleja się prawdziwa lista Worda (styl "Akapit z listą"), a nie ręczne punktory. Komunikat "List copied".

7.8. Control+H. Oczekiwane: NIC się nie dzieje i w menu Misc nie ma pozycji "HTML Format" - ta komenda została usunięta na Twoją prośbę. Konwersja do HTML jest tylko przez Zapisz jako.

## 8. Podgląd Markdown

8.1. Escape w pliku .md. Oczekiwane: wejście w podgląd, komunikat "Preview", kursor w podglądzie w tym samym miejscu tekstu.

8.2. Escape w podglądzie. Oczekiwane: powrót do edycji, komunikat "Editing". Escape jest JEDYNYM wyjściem, niezależnie od tego, którym klawiszem wszedłeś.

8.3. Shift+Escape z edycji. Oczekiwane: wejście w podgląd odczepiony, komunikat "Preview detached", a ruch po podglądzie NIE przesuwa kursora w tekście.

8.4. Shift+Escape będąc już w podglądzie. Oczekiwane: przełącza samą synchronizację w miejscu, komunikaty "Synced" i "Detached", i NIE wychodzi z podglądu.

8.5. Sprawdź, czy komunikaty nie są odwrócone, to znaczy czy wchodząc w podgląd nie słyszysz "Editing". To był realny błąd wyścigu mowy i został naprawiony.

8.6. F7 w podglądzie. Oczekiwane: okno listy elementów - najpierw wybór typu (All, Headings, Links, Lists, Tables z licznikami), potem lista elementów w kolejności dokumentu, Enter skacze.

8.7. W podglądzie litery H, L, I, K, T i te same z Shiftem. Oczekiwane: skok do następnego i poprzedniego nagłówka, listy, pozycji listy, linku, tabeli.

8.8. Enter na linku w podglądzie. Oczekiwane: link się otwiera. Gdy kursor nie jest na linku: "No link at cursor".

8.9. F7 poza podglądem. Oczekiwane: to nadal Spell Check, sprawdzanie pisowni - lista elementów działa tylko w podglądzie.

## 9. Listy ostatnich i ulubionych plików

9.1. Alt+R i Alt+L. Oczekiwane: czytnik od razu mówi nazwę pliku, na którym stoisz. NIE ma czytać najpierw wyliczanki klawiszy - to była Twoja uwaga z 16 sierpnia i zostało poprawione.

9.2. Na liście naciśnij F1. Oczekiwane: pomoc podaje pełną listę klawiszy tej listy (Shift+F1 wymawia ją na żądanie).

9.3. Strzałka w prawo na pozycji listy. Oczekiwane: otwiera się systemowe okno "Otwórz za pomocą" - to ten nowoczesny dialog, w którym JEST opcja "Zawsze używaj tej aplikacji".

9.4. Strzałka w lewo. Oczekiwane: program mówi pełną ścieżkę pliku.

9.5. Control+Enter. Oczekiwane: plik zostaje pokazany w Eksploratorze Windows. Control+C: pełna ścieżka trafia do schowka.

9.6. Delete albo Backspace na pozycji listy. Oczekiwane: wpis znika Z LISTY, a plik zostaje na dysku. Komunikat "Removed from list" - dawniej mówił samo "Removed", czego nie dało się odróżnić od usunięcia z dysku. Po usunięciu ostatniego wpisu: "List is now empty".

9.7. Shift+Delete na pozycji listy. Oczekiwane: pytanie o potwierdzenie, że plik ma zniknąć Z DYSKU, a nie tylko z listy. Po potwierdzeniu "Deleted from disk".

9.8. Shift+Delete na pliku, który jest otwarty w programie i ma niezapisane zmiany, i ODMÓW zapisu przy pytaniu o zamknięcie. Oczekiwane: plik NIE zostaje usunięty, komunikat "Delete canceled". To zabezpieczenie przed utratą danych.

9.9. Shift+Delete na pozycji, która jest folderem. Oczekiwane: "This is a folder, not a file".

9.10. Shift+Delete na wpisie, którego pliku już nie ma na dysku. Oczekiwane: pytanie, czy usunąć sam wpis z listy.

9.11. Kolejność Tabem w tym okienku. Oczekiwane: najpierw lista, potem akcje na pliku, a OK i Anuluj na końcu.

## 10. Polskie znaki w nazwach plików i zabezpieczenie dokumentu

10.1. PILNE, wersja 5.0.15: dodaj do ulubionych (Control+L) plik o nazwie z polskimi literami, na przykład "ogłoszenia-parafialne.md". Zamknij program, uruchom ponownie i otwórz Alt+L. Oczekiwane: plik JEST na liście. Wcześniej znikał sam z ulubionych i z ostatnich, bo program czytał plik ustawień złym kodowaniem i uznawał ścieżkę za nieistniejącą.

10.2. Uwaga do 10.1: wpisy, które zginęły PRZED aktualizacją, są skasowane na trwałe. Trzeba je dodać do ulubionych jeszcze raz - poprawka chroni od teraz, nie odzyskuje starych.

10.3. Wejdź w podgląd (Escape) i będąc w nim naciśnij Control+K albo Control+L. Wyjdź i otwórz plik ponownie. Oczekiwane: plik NIE jest zabezpieczony do odczytu. Wcześniej podgląd włączał zabezpieczenie na stałe, choć sam tego nie robiłeś.

10.4. Control+Shift+F7 (No Guard) na pliku, który jest w ulubionych. Zamknij i otwórz go ponownie. Oczekiwane: zabezpieczenie NIE wraca. Wcześniej wracało przy każdym otwarciu.

10.5. Uwaga do 10.4: jeśli plik miał zapisane zabezpieczenie sprzed aktualizacji, raz musisz je zdjąć ręcznie.

10.6. Control+F7 włącza zabezpieczenie. Oczekiwane: komunikat "Guard" i nie da się pisać.

## 11. Okna, dodatek NVDA i sprawy ogólne

11.1. Przejdź Alt+Tabem do innego programu i wróć do EdSharpNG. Oczekiwane: kursor jest od razu w polu edycji, czytnik reaguje normalnie i NIE musisz naciskać dwa razy Escape ani wchodzić ponownie w okno.

11.2. Sprawdź w NVDA, w zarządzaniu dodatkami, czy jest dodatek zgłaszający błędy pisowni w EdSharpNG. Oczekiwane: dodatek jest zainstalowany (instalator proponuje go na końcu) i przy przechodzeniu po tekście z błędem czytnik zgłasza błąd pisowni. Osobno jest dodatek przepuszczający klawisze, od autora oryginału.

11.3. Sprawdź, czy plik programu nazywa się EdSharpNG.exe. Oczekiwane: tak - i to jest warunek działania dodatku z punktu 11.2, bo NVDA dopasowuje dodatek po nazwie procesu.

11.4. Sprawdź, czy skrót na pulpicie NIE ma przypisanego skrótu klawiszowego Alt+Control+E. Oczekiwane: nie ma. Taki skrót globalny połykałby literę "ę" w całym systemie.

11.5. Zajrzyj do menu Pomoc i otwórz Documentation (F1). Oczekiwane: podręcznik się otwiera i opisy nowych funkcji są w nim obecne.

## 12. Testy poglądowe na stare podstawy

Te rzeczy działały przed naszymi zmianami. Chodzi tylko o sprawdzenie, czy czegoś nie popsuliśmy po drodze - wystarczy przejść je szybko.

12.1. Nowy plik, wpisanie tekstu, Control+S, zamknięcie, ponowne otwarcie. Tekst na miejscu.

12.2. Otwarcie pliku .txt, .md i .rtf. Każdy się otwiera i czyta poprawnie.

12.3. Zapisz jako, w tym zapis do HTML. Plik powstaje.

12.4. Szukanie i szukanie ponownie. Znajduje i mówi znalezione miejsce.

12.5. Cofanie i ponawianie: Control+Z cofa, a ponawiaja DWA klawisze, Control+Shift+Z oraz Control+Y. Oba maja robic dokladnie to samo, razem z mowa. Historia tego punktu, zebys nie zglaszal bledu, ktorego nie ma: do 5.0.56 lista podawala Control+Y jako ponawianie, a program mial tam Repeat Line - to byl blad NASZEJ listy. Od 5.0.58 Control+Y ponawia, bo tak poprosiles, a Repeat Line zeszlo do menu bez skrotu.

12.6. Kopiowanie, wycinanie, wklejanie, w tym Alt+C (Copy Append) i Alt+X (Cut Append).

12.7. Control+Space (Select Chunk) i Shift+Backspace (Chunk). Uwaga: Shift+Backspace jest w czytniku zajęty jako usuwanie znaku, ale u nas komenda tylko pyta i nie rusza kursora. Jeśli usłyszysz coś dziwnego, napisz - to skrót odziedziczony po oryginale, więc nie zmieniam go bez Twojej decyzji.

12.8. Alt+Home i Alt+End (znak na początku i końcu wiersza).

12.9. Alt+T (Topic) ma nadal mówić temat sekcji. Alt+Shift+T od 27.08.2026 tworzy markdownowy spis treści, opisany w rozdziale 15.

12.10. F2 (Special Character), F4 (lista okien), Shift+F4 (Windows Open).

12.11. Menu główne przechodzone Altem i strzałkami. Każda pozycja czytana, skróty przy pozycjach zgodne z tym, co realnie działa.

## 13. Przykład bloku kodu do testów 1.9 i 2.6

Poniżej jest blok kodu. Linia z kratką wewnątrz niego NIE jest nagłówkiem i nie powinna pojawić się ani w drzewie F6, ani w nawigacji Controlem z PageUp i PageDown.

```
# to nie jest nagłówek, to komentarz w przykładzie
echo "test"
## ten też nie
```

Na tym kończy się blok kodu. Następny nagłówek jest już prawdziwy.

## 14. Czego jeszcze nie ma i nie ma sensu testować

14.1. NIEAKTUALNE. Przypisy już są - rozdział 16 tej listy. Zapis z tego punktu pochodzi z wersji 5.0.21 i dotyczył stanu sprzed ich napisania.

14.2. Spis treści WPISYWANY do dokumentu to osobna sprawa od drzewa pod F6. Drzewo służy do skakania, nie zostawia niczego w pliku. Czekam na Twoją decyzję, czy chcesz też wersję wpisywaną do tekstu.

14.3. Polskie komunikaty i polski interfejs są świadomie odłożone na koniec projektu. Dlatego wszystko powyżej cytuję po angielsku.

## 15. Spis treści (nowe w 5.0.25)

Ten rozdział jest w całości nowy: spis treści po markdownowemu, w miejsce starego, opartego na podziałach z oryginalnego EdSharpa. Klawisze zostały te same, funkcja jest inna.

15.1. Otwórz plik z rozszerzeniem .md, w którym są nagłówki, i naciśnij Alt+Shift+T. Oczekiwane: na samej górze dokumentu pojawia się nagłówek "Contents" i lista wszystkich nagłówków dokumentu, po jednym w wierszu, z wcięciami pokazującymi poziomy. Program mówi "Contents" i liczbę pozycji, na przykład "Contents, 15 items". Kursor stoi na początku spisu.

15.2. Naciśnij Alt+Shift+T po raz drugi. Oczekiwane: NIE powstaje drugi spis. Ten, który jest, zostaje przepisany od nowa, a program mówi "Contents updated" i liczbę pozycji. Tak samo należy odświeżyć spis po dopisaniu albo przemianowaniu rozdziału.

15.3. Naciśnij Control+Z zaraz po Alt+Shift+T. Oczekiwane: spis znika, dokument wraca do stanu sprzed komendy. To jest ważne po tym, co zrobiła Ci komenda Format Code: żadna komenda przepisująca dokument nie może być nieodwracalna.

15.4. Naciśnij Alt+Shift+T na pliku, który NIE jest Markdownem, na przykład na .txt. Oczekiwane: program nic nie zmienia i mówi "Table of Contents works only on Markdown files!".

15.5. Naciśnij Alt+Shift+T na dokumencie .md bez ani jednego nagłówka. Oczekiwane: program nic nie wpisuje i mówi "No headings!".

15.6. Ustaw kursor gdzieś w treści rozdziału, w dokumencie ze spisem, i naciśnij Shift+F6. Oczekiwane: kursor przeskakuje na górę, na pozycję spisu odpowiadającą temu rozdziałowi, a program czyta tekst tej pozycji i słowo "contents".

15.7. Stojąc na pozycji spisu, naciśnij Shift+F6 jeszcze raz. Oczekiwane: kursor wraca w dół, do tego właśnie rozdziału, a program czyta tytuł i poziom, tak samo jak przy Control+PageDown, na przykład "Instalacja, heading 2". To jest ta kontekstowość w obie strony, o którą prosiłeś.

15.8. Naciśnij Shift+F6 w dokumencie, w którym są nagłówki, ale spisu jeszcze nie ma. Oczekiwane: program mówi "No contents yet, press Alt+Shift+T to create it!", czyli podpowiada klawisz tworzenia spisu. W dokumencie bez nagłówków ten sam klawisz mówi krótko "No headings!".

15.9. Eksport do Worda: zrób spis, potem Control+T (Text Convert) na docx, i otwórz wynik w Wordzie. Oczekiwane: pozycje spisu są klikalnymi odsyłaczami i prowadzą do właściwych rozdziałów. U mnie zmierzone jest to, co idzie na wejściu: nasze zakotwiczenie nagłówków zgadza się co do znaku z tym, jak liczy je pandoc, a pandoc robi z tego zakładki Worda. Zachowania samego Worda nie sprawdziłem, bo go tu nie ma.

15.10. Podgląd pod Escape: włącz podgląd na dokumencie ze spisem, ustaw kursor na pozycji spisu i naciśnij Enter. Oczekiwane: kursor przechodzi do rozdziału w tym samym dokumencie, a program czyta tytuł i poziom. NIE otwiera się przeglądarka. Enter na zwykłym odsyłaczu do strony internetowej ma nadal otwierać przeglądarkę, jak dotąd.

15.11. Nagłówek złożony z samych znaków przestankowych, na przykład wiersz "## !!!". Oczekiwane: taka pozycja jest w spisie, ale BEZ odsyłacza, samym tekstem. To nie jest przeoczenie: pandoc nie tworzy dla takiego nagłówka żadnego celu, więc odsyłacz prowadziłby w nicość, a pozycja bez odsyłacza jest uczciwsza niż odsyłacz, który nie działa. Pozostałe pozycje spisu w tym samym dokumencie mają odsyłacze normalnie.


## 16. Przypisy (nowe, po Twoich ustaleniach z 27 sierpnia)

Rozdział w całości nowy. Przypisów w programie nie było wcale, więc to nie poprawka, a nowa funkcja. Wstawianie i skok NIE MAJĄ jeszcze klawiszy - wykonasz je z menu Misc albo z Menu Alternatywnego - bo Ctrl+F6 i Alt+F6, które wybrałeś, trzyma jeszcze stara para komend szukania tematu. Pytanie o nie masz osobno. Lista przypisów działa od razu z Ctrl+Shift+F6.

16.1. Otwórz plik .md, ustaw kursor w środku zdania i wywołaj Insert Footnote z menu Misc. Oczekiwane: otwiera się okienko z jednym polem na treść przypisu. Wpisz coś i naciśnij Enter. W zdaniu, dokładnie tam gdzie stał kursor, pojawia się znacznik w postaci nawiasu kwadratowego z daszkiem i numerem. Treść przypisu ląduje na samym końcu dokumentu. Kursor zostaje w zdaniu, za znacznikiem - nie skacze na koniec. Program mówi "Footnote 1 inserted".

16.2. Wstaw drugi przypis w innym zdaniu. Oczekiwane: dostaje numer 2, a jego treść dopisuje się pod pierwszą, nie zamiast niej.

16.3. Wstaw przypis w zdaniu, które stoi PRZED tymi dwoma. Oczekiwane: dostaje numer 3, nie 1, i nic nie jest przenumerowywane. To jest zamierzone: Markdown numeruje przypisy w kolejności, w jakiej znaczniki stoją w tekście, więc w gotowym dokumencie i po eksporcie zobaczysz je jako 1, 2, 3 od góry, niezależnie od tego, jakie mają etykiety w pliku. Jeśli jednak wolisz, żeby liczby w samym pliku też szły po kolei, powiedz - da się dorobić przenumerowanie.

16.4. Naciśnij Control+Z zaraz po wstawieniu przypisu. Oczekiwane: cofa się i znacznik, i treść na końcu. Może to wymagać dwóch cofnięć, bo są to dwie zmiany w dokumencie - jeśli tak, napisz, czy przeszkadza.

16.5. Wywołaj okienko i zatwierdź PUSTE pole albo naciśnij Escape. Oczekiwane: nic się nie wstawia, program mówi "No footnote text, nothing inserted!".

16.6. Ustaw kursor w zdaniu ze znacznikiem i wywołaj Go to Footnote. Oczekiwane: kursor przechodzi na koniec dokumentu, na treść tego przypisu, a program czyta treść i słowo "footnote" z numerem.

16.7. Stojąc na treści przypisu, wywołaj Go to Footnote jeszcze raz. Oczekiwane: kursor wraca do zdania, na znacznik, a program czyta ten wiersz. To ta sama kontekstowość w obie strony, którą wybrałeś dla spisu treści.

16.8. Sprawdź to samo, stojąc na SAMYM KOŃCU wiersza ze znacznikiem, za kropką. Oczekiwane: skok działa. Pytam osobno, bo tu program wcześniej mylił się o jeden wiersz - poprawiłem to i, przy okazji, ta sama pomyłka dotyczyła spisu treści, więc warto sprawdzić też Shift+F6 z kursorem na końcu pozycji spisu.

16.9. Wywołaj Go to Footnote w zdaniu, w którym żadnego znacznika nie ma. Oczekiwane: program nie zgaduje i mówi, żebyś ustawił kursor w wierszu ze znacznikiem albo na treści przypisu.

16.10. Wywołaj Go to Footnote w dokumencie bez ani jednego przypisu. Oczekiwane: krótkie "No footnotes!".

16.11. Naciśnij Alt+K (do 5.0.45 było to Control+Shift+F6). Oczekiwane: otwiera się okno "Footnotes" z listą przypisów - każda pozycja to numer i treść. Enter idzie do znacznika w zdaniu, bo tam się pisze. Escape zamyka bez ruszania kursora.

16.12. Usuń ręcznie znacznik ze zdania, zostawiając treść na końcu, i otwórz listę. Oczekiwane: przypis jest na liście z dopiskiem, że nie ma treści na końcu dokumentu albo że nie ma znacznika - zależnie od tego, czego brakuje. Program ma o tym powiedzieć, a nie milczeć.

16.13. Wywołaj którąkolwiek z trzech komend na pliku .txt. Oczekiwane: "Footnotes work only on Markdown files!" i nic się nie zmienia.

16.14. Wywołaj Insert Footnote na dokumencie zabezpieczonym (Control+F7). Oczekiwane: "Document is guarded!" i nic się nie wstawia. Skok i lista mają na zabezpieczonym pliku działać normalnie - one tylko czytają.

16.15. Wstaw przypis, potem wyeksportuj dokument do Worda (Control+T) i otwórz wynik. Oczekiwane: przypis jest PRAWDZIWYM przypisem Worda, czyli takim, który Word pokazuje u dołu strony i który czytnik zgłasza jako przypis - a nie surowym tekstem z nawiasem w środku zdania. U siebie zmierzyłem to na pliku wynikowym: przypis wchodzi w mechanizm przypisów Worda, a surowa składnia nie zostaje w treści akapitu. Zachowania samego Worda i tego, jak zapowie to czytnik, nie sprawdziłem - Worda tu nie ma.

16.16. ZROBIONE, decyzja podjęta - zostawiam punkt, żeby było widać, co się zmieniło. Pytałem tu, jak ma brzmieć surowy znacznik w podglądzie, a Ty odpowiedziałeś lepiej, niż pytałem: co ma się dać na nim ZROBIĆ. Znacznik brzmi teraz "footnote 1" i Enter na nim działa - szczegóły w punktach od 16.20. Tak samo wiersz z treścią na końcu dokumentu: zamiast nawiasu z daszkiem czytnik mówi "footnote 1" i dalej treść.

16.17. Postaw kursor w wierszu wewnątrz bloku kodu i naciśnij Control+Shift+K. Oczekiwane: program ODMAWIA, mówi "Cannot put a footnote inside a code block!", okienko na treść wcale się nie otwiera i nic się nie wstawia. Powód: sprawdziłem naszym własnym eksportem, że znacznik postawiony w bloku kodu jest martwy - do Worda nie wchodzi żaden przypis, a w gotowym dokumencie zostaje goły nawias z daszkiem. Wcześniej program mówił w tej sytuacji "Footnote 1 inserted", czyli potwierdzał coś, czego nie zrobił. Sprawdź, czy ta odmowa nie przeszkadza Ci w sposobie, w jaki używasz bloków kodu.

16.18. Postaw kursor w wierszu z treścią przypisu na końcu dokumentu (najłatwiej: skocz tam Alt+F6) i naciśnij Control+Shift+K. Oczekiwane: program odmawia i mówi "Cannot put a footnote inside the text of footnote 1!", z numerem tego przypisu, w którego treści stoisz. Powód jest poważniejszy niż w bloku kodu: zmierzyłem, że treść przypisu wstawionego w treści innego przypisu ZNIKA z eksportu do Worda całkowicie - ani jako przypis, ani jako tekst. Czyli straciłbyś to, co napisałeś, bez żadnego ostrzeżenia.

16.19. To kontrola do dwóch poprzednich punktów, żeby sprawdzić, że odmowy nie zepsuły zwykłego wstawiania. W tym samym dokumencie, w którym program właśnie odmówił, postaw kursor w normalnym zdaniu i naciśnij Control+Shift+K. Oczekiwane: okienko otwiera się jak zawsze, przypis wchodzi, słychać "Footnote N inserted". Zmierzyłem to u siebie na żywym czytniku, ale to Ty poczujesz, czy w codziennym pisaniu odmowy trafiają tylko tam, gdzie powinny. Jeśli program odmówi Ci w miejscu, w którym przypis wydaje się sensowny, napisz - to znaczy, że bramka jest za szeroka.

16.20. Włącz podgląd pod Escape na dokumencie z przypisem i przejdź na zdanie ze znacznikiem. Oczekiwane: czytnik NIE mówi już nawiasu ani daszka - w miejscu znacznika słyszysz "footnote 1". Tak samo na końcu dokumentu: wiersz z treścią zaczyna się od "footnote 1" i dalej idzie treść. To jest odpowiedź na Twoją uwagę z punktu 16.16.

16.21. W podglądzie, stojąc w zdaniu ze znacznikiem, naciśnij Enter. Oczekiwane: kursor PRZENOSI się na treść przypisu na końcu dokumentu, a czytnik czyta tę treść razem z numerem. Przeniesienie kursora to Twoja decyzja - napisałeś "chyba jednak bardziej naturalnie przenieść". Wystarczy stać gdziekolwiek w wierszu ze znacznikiem, także za kropką.

16.22. Stojąc na treści przypisu, naciśnij Enter jeszcze raz. Oczekiwane: wracasz do zdania, z którego przyszedłeś, i słyszysz je razem z informacją, że to znacznik w tekście. Czyli w podglądzie Enter działa tak samo kontekstowo w obie strony, jak Alt+F6 w edytorze i Shift+F6 w spisie treści.

16.23. Sprawdź, czy Enter nie zrobił się nadgorliwy: w podglądzie stań w zwykłym wierszu, bez znacznika i bez odsyłacza, i naciśnij Enter. Oczekiwane: nic nie skacze, a program mówi "No link at cursor". Enter w podglądzie nadal ma robić trzy rzeczy zależnie od miejsca: skakać po przypisie, skakać po pozycji spisu treści i otwierać zwykły odsyłacz. Jeśli któraś z tych trzech przestała działać, to najważniejsza rzecz do zgłoszenia z tej wersji.

16.24. W podglądzie wejdź w blok kodu, w którym stoi przykładowy zapis z nawiasem i daszkiem. Oczekiwane: tam znacznik zostaje SUROWY, dokładnie tak jak go napisałeś, i Enter na nim nie skacze. Tak ma być: w przykładzie kodu to zwykły tekst, nie przypis - i tak samo traktuje go nasz eksport.

16.25. Przejdź strzałkami przez zdanie ze znacznikiem w podglądzie i wróć do edytora pod Escape. Oczekiwane: kursor w edytorze stoi w tym samym miejscu zdania, w którym byłeś w podglądzie, a nie na początku dokumentu. Pytam osobno, bo napis "footnote 1" jest dłuższy niż zapis, który zastępuje, i właśnie tam program mógł pogubić pozycję - naprawiłem to i chcę wiedzieć, czy u Ciebie synchronizacja jest w porządku.

## 17. Wersje od 5.0.31 do 5.0.34

Ten rozdział zbiera rzeczy, które weszły po tym, jak dostałeś pierwszą wersję tej listy. Wysyłałem Ci je w osobnych wiadomościach; tu są razem, żeby nic nie zginęło.

17.1. Ostatni folder przy otwieraniu. Otwórz Ctrl+O plik z odległego folderu, zamknij okno i naciśnij Ctrl+O jeszcze raz. Oczekiwane: okno startuje w tym samym folderze, a nie tam, gdzie ostatnio zapisywałeś.

17.2. Kontrola do 17.1: zapisz coś Ctrl+Shift+S w zupełnie innym miejscu, potem otwórz plik ze starego folderu. Oczekiwane: kolejne Ctrl+O startuje z folderu OSTATNIO OTWARTEGO pliku, czyli zapis nie przejmuje pierwszeństwa.

17.3. Pliki brajlowskie. Otwórz plik .brl przez Ctrl+Shift+O, czyli otwórz w innym formacie. Oczekiwane: zamiast znaków brajlowskich widzisz normalny tekst. Wcześniej dostawałeś pusty dokument albo błąd.

17.4. To samo z plikiem .brf. Oczekiwane: to samo, czytelny tekst.

17.5. Uwaga do 17.3 i 17.4: domyślnie ustawiony jest brajl angielski, bo taki był w oryginale. Jeśli Twój plik jest po polsku, tekst wróci pokiereszowany i to nie będzie usterka, tylko zła tablica. Napisz, jeśli tak wyjdzie - polską tablicę mam sprawdzoną i dołożę wybór języka w normalnych ustawieniach.

17.6. Pliki Python. Otwórz plik .py z błędem, na przykład odwołaniem do nieistniejącej zmiennej, i naciśnij Ctrl+F5. Oczekiwane: program się uruchamia i słyszysz numer wiersza oraz treść błędu, a nie reklamę Sklepu Microsoft.

17.7. To samo, ale zwróć uwagę, CO słyszysz najpierw. Oczekiwane: mowa zaczyna się od numeru wiersza, a nie od czytania Twojej własnej ścieżki do pliku.

17.8. Błąd wcięcia w pliku .py. Oczekiwane: kursor staje na POCZĄTKU tego wiersza, gotowy do poprawki, a nie na końcu, gdzie znacznik stawia Python.

17.9. Jeśli masz własny kompilator dla Pythona ustawiony przez Ctrl+Shift+F5, sprawdź, że nadal używany jest Twój, a nie nasz domyślny.

17.10. Dziwne znaki przy otwieraniu. Otwórz kilka zwykłych, krótkich plików tekstowych bez polskich znaków. Oczekiwane: normalny tekst. Program czasem uznawał taki plik za zapisany dwubajtowo i sklejał litery parami w znaki dalekowschodnie.

17.11. Kontrola do 17.10, ważniejsza: jeśli masz plik naprawdę zapisany jako Unicode dwubajtowy, otwórz go i sprawdź, że nadal czyta się poprawnie. Chodzi o to, czy naprawiając jedno nie zepsuliśmy drugiego.

17.12. Ustawienia programu są od 5.0.33 pod Control z przecinkiem, tak jak w większości dzisiejszych programów. Oczekiwane: Ctrl+przecinek otwiera okno ustawień. Zwolniony Alt+Shift+C dostał dopisywanie do schowka, patrz punkt 6.1.

17.13. Kursor na pozycji listy w podglądzie (5.0.34). Zrób spis treści Alt+Shift+T, wejdź w podgląd Escape, przejdź na pozycję spisu i naciśnij Enter BEZ przesuwania kursora w prawo. Oczekiwane: skok działa od razu, a program czyta tytuł rozdziału z poziomem. Wcześniej kursor stał na punktorze i słyszałeś "No link at cursor".

17.14. Kontrola do 17.13: to samo na pozycji zwykłej listy, która nie ma odsyłacza. Oczekiwane: słyszysz "No link at cursor" i kursor zostaje w wierszu, czyli nic nie skacze w losowe miejsce.

## 18. Kreator tabeli, Ctrl+Shift+T (5.0.35)

To punkt drugi mapy drogowej i pierwsza wersja tabel. Wszystko tutaj powstało z Twojego opisu, więc te punkty sprawdzają przede wszystkim, czy zrozumiałem Cię dobrze.

18.1. Otwórz plik .md i naciśnij Ctrl+Shift+T. Oczekiwane: otwiera się okno „Insert Table" z jedną komórką, bez żadnego pytania o liczbę wierszy i kolumn.

18.2. Zwróć uwagę, co czytnik mówi na starcie. Oczekiwane: słyszysz, że jesteś w wierszu „Header", czyli w wierszu nagłówka tabeli. Pierwszy wiersz jest nagłówkiem, bo tabela Markdown bez nagłówka nie jest tabelą dla żadnego konwertera — nie da się tego wyłączyć, ale chcę wiedzieć, czy zapowiedź jest zrozumiała.

18.3. Napisz coś w komórce, nie naciskając wcześniej niczego. Oczekiwane: tekst wchodzi od razu, tak jak mówiłeś.

18.4. Stojąc na komórce z tekstem, napisz coś innego. Oczekiwane: nowy tekst NADPISUJE stary, znowu jak w Excelu.

18.5. Teraz F2 na komórce, która ma już treść. Oczekiwane: wchodzisz w edycję tego, co tam jest, możesz dopisać albo poprawić, a Enter zatwierdza.

18.6. Strzałka w prawo z ostatniej kolumny. Oczekiwane: dokłada się nowa kolumna i kursor od razu w niej stoi. Tabela ma rosnąć w miarę pisania.

18.7. Strzałka w dół z ostatniego wiersza. Oczekiwane: dokłada się nowy wiersz, a czytnik mówi „Row 1", „Row 2" i tak dalej.

18.8. Zbuduj tabelę dwa na dwa, na przykład Imię i Wiek w nagłówku, a niżej Ala i 7. Naciśnij Ctrl+Enter. Oczekiwane: okno zamyka się, tabela ląduje w dokumencie w miejscu kursora, a program mówi ile ma wierszy i kolumn.

18.9. Popatrz na wstawiony tekst. Oczekiwane: pod wierszem nagłówka jest wiersz z kreskami. Wygląda dziwnie, ale bez niego to nie jest tabela — po tym wierszu konwertery ją rozpoznają.

18.10. Naciśnij Ctrl+Z. Oczekiwane: cała tabela znika jednym cofnięciem. To ta sama zasada, którą wprowadziliśmy po utracie Twojego pliku: nic nie jest zapisywane na dysk i wszystko da się cofnąć.

18.11. Otwórz Ctrl+Shift+T jeszcze raz, wpisz cokolwiek w jedną komórkę i naciśnij Escape. Oczekiwane: program PYTA, czy zamknąć bez wstawiania, ostrzegając, że stracisz to, co wpisałeś. Jeśli odpowiesz „nie", zostajesz w oknie.

18.12. Kontrola do 18.11: otwórz kreator i naciśnij Escape od razu, nic nie wpisując. Oczekiwane: okno zamyka się bez pytania, bo nie ma czego stracić.

18.13. Escape w trakcie pisania w komórce. Wejdź w komórkę pod F2, zmień treść i naciśnij Escape. Oczekiwane: Escape należy najpierw do komórki — cofa zmianę w niej, a nie zamyka całego okna. Dopiero drugie Escape zamyka.

18.14. Nadmiarowa kolumna. Zbuduj tabelę, dołóż strzałką jedną kolumnę więcej i nic w niej nie pisz, potem Ctrl+Enter. Oczekiwane: puste kolumny i wiersze z końca NIE trafiają do dokumentu. Dokłada się je jednym klawiszem, więc każdemu się to zdarzy, a inaczej nie byłoby jak takiej kolumny cofnąć.

18.15. Pusty kreator. Otwórz Ctrl+Shift+T i naciśnij Ctrl+Enter nic nie wpisując. Oczekiwane: program mówi, że tabela jest pusta i nic nie wstawia.

18.16. Kreska pionowa w treści. Wpisz w komórkę coś z kreską pionową, na przykład „a|b". Oczekiwane: tabela nie rozjeżdża się, a w tekście widzisz kreskę z lewym ukośnikiem przed nią. Kreska kończy komórkę, więc Twoja własna musi być zabezpieczona.

18.17. Eksport, czyli najważniejszy sprawdzian tej funkcji. Zrób tabelę, zapisz plik i przekonwertuj go do HTML naszym narzędziem. Oczekiwane: dostajesz prawdziwą tabelę z komórkami nagłówka, czyli czytnik zapowiada nazwę kolumny przy przechodzeniu po komórkach. U mnie zmierzone i działa; chcę potwierdzenie z Twojej maszyny.

18.18. To samo przez podgląd Escape. Oczekiwane: podgląd rozpoznaje tabelę, czyta „Table:" z treścią wiersza, a klawisz T skacze po tabelach. To działało już wcześniej, więc pytam kontrolnie, czy nowa tabela też jest tak widziana.

18.19. Bramka na typ pliku. Otwórz zwykły plik .txt i naciśnij Ctrl+Shift+T. Oczekiwane: program mówi, że tabele działają tylko na plikach Markdown, i nic nie otwiera.

18.20. Bramka na podgląd. Wejdź w podgląd pod Escape i naciśnij Ctrl+Shift+T. Oczekiwane: słyszysz, że najpierw trzeba zamknąć podgląd.

18.21. Bramka na blok kodu. Stań wewnątrz bloku ograniczonego trzema odwrotnymi apostrofami i naciśnij Ctrl+Shift+T. Oczekiwane: program mówi, że w bloku kodu tabeli wstawić nie można. W przykładzie kodu tabela byłaby martwa, a komunikat o wstawieniu byłby nieprawdą.

18.22. Bramka na dokument zabezpieczony. Włącz Ctrl+F7 i spróbuj Ctrl+Shift+T. Oczekiwane: program mówi, że dokument jest zabezpieczony.

18.23. Zmiana, o której trzeba wiedzieć: Ctrl+Shift+T było wcześniej łączeniem plików innych formatów. Ta komenda NIE zniknęła — jest w menu Miscellaneous jako „Text Combine", tylko bez skrótu. Zgodnie z tym, co napisałeś, łączenie plików i wyodrębnianie rozdziałów wróci później jako jedna przemyślana funkcja, najpewniej pod Ctrl+Shift z klawiszem funkcyjnym.

18.24. Sprawdź jeszcze Ctrl+F1 na Ctrl+Shift+T. Oczekiwane: program opisuje kreator tabeli, a nie łączenie plików. Zły opis mówiony jest gorszy niż jego brak, więc jeśli usłyszysz starą treść, to usterka.

## 19. Polskie litery w plikach ANSI (5.0.36)

To pierwsza część trzeciego punktu naszej mapy drogowej. Chodzi o pliki tekstowe
zapisane po polsku „po staremu", czyli w windowsowej stronie kodowej, bez
znacznika na początku pliku. Takie pliki robi Notatnik ze starszych Windowsów,
wiele programów urzędowych i większość eksportów z DOS-a.

Do 5.0.35 program czytał taki plik jako Unicode i każda polska litera zamieniała
się w znak zapytania. To nie było tylko brzydkie: znak był już podmieniony w
pamięci, więc zapisanie pliku utrwalało uszkodzenie na dysku i nie było czego
odzyskać. Zmierzyłem to i naprawiłem, ale odczyt na Twojej maszynie zależy od
ustawień regionalnych Windows, więc potrzebuję potwierdzenia od Ciebie.

19.1. Weź albo zrób plik z polskimi literami zapisany w starym Notatniku jako
ANSI (w oknie zapisu jest pole kodowania). Otwórz go w programie. Oczekiwane:
widzisz „Zażółć gęślą jaźń" poprawnie, a czytnik wymawia polskie słowa, a nie
literuje znaków zapytania.

19.2. Ten sam plik po otwarciu zapisz Ctrl+S i otwórz ponownie. Oczekiwane:
polskie litery nadal są na miejscu. UWAGA, od 5.0.37 zmieniło się to, czym plik
zostaje zapisany: stary plik ANSI po zapisaniu jest już plikiem UTF-8. To Twoja
decyzja z 28 sierpnia i sprawdzamy ją osobno w punkcie 20.7.

19.3. Sprawdź, co program mówi o kodowaniu: naciśnij Alt+Z dwa razy. Oczekiwane:
przy pliku ANSI słyszysz nazwę środkowoeuropejskiej strony kodowej z numerem
1250, a nie Unicode. Jeśli usłyszysz Unicode, to znaczy że plik został wzięty za
UTF-8 i punkt 19.1 też powinien paść.

19.4. Kontrola, ważniejsza od poprzednich: otwórz zwykły plik Markdown zapisany
jako UTF-8, na przykład któryś z naszych. Oczekiwane: nadal czyta się poprawnie.
Naprawa dotyczy tylko plików, które UTF-8 nie są, ale chcę wiedzieć, czy przy
okazji nie zepsuliśmy tego, co działało.

19.5. Druga kontrola: plik zawierający wyłącznie znaki angielskie, bez ogonków.
Oczekiwane: otwiera się normalnie i po zapisaniu nadal jest zwykłym plikiem
tekstowym.

19.6. Czego ta naprawa NIE robi, żebyś nie testował rzeczy, której nie ma:
program nie ma osobnej komendy „zmień kodowanie pliku i zapisz". Alt+Shift+Y
zmienia interpretację tekstu już otwartego, a Alt+Shift+E eksportuje do wybranej
strony kodowej. Jeśli chcesz komendę, która wprost przełącza kodowanie bieżącego
dokumentu i zapisuje, powiedz — to jest do dorobienia w tym samym punkcie mapy.

19.7. Końce wiersza Windows, Mac i Linux to druga połowa tego punktu i jest
zrobiona w 5.0.37 według Twojej decyzji: program otwiera każdy rodzaj, a zapisuje
zawsze w Windows. Testy są w rozdziale 20.

## 20. Tabele czytane inaczej, końce wiersza i kodowanie zapisu (5.0.37)

To wersja z Twoich czterech uwag z 28 sierpnia. Najważniejsze do sprawdzenia jest
20.1 i 20.2, bo tam zmieniło się to, co słyszysz.

20.1. Otwórz kreator tabeli (Ctrl+Shift+T), wpisz coś w kilka komórek i chodź po
nich strzałkami w lewo i w prawo. Oczekiwane: najpierw słyszysz treść komórki, a
dopiero potem gdzie jesteś, i to w kolejności kolumna, potem wiersz. Czyli na
przykład "trzydzieści, kolumna 2, wiersz 1".

20.2. To samo, ale chodź strzałkami w górę i w dół. Oczekiwane: znowu najpierw
treść, ale współrzędne w odwrotnej kolejności, czyli wiersz, potem kolumna. Tak jak
prosiłeś: liczy się ta współrzędna, która właśnie się zmieniła.

20.3. Stań na pierwszym wierszu tabeli. Oczekiwane: zamiast numeru wiersza
słyszysz, że to wiersz nagłówka. Pierwszy wiersz jest nagłówkiem tabeli na sztywno,
bo bez niego żaden konwerter nie uzna tekstu za tabelę.

20.4. Stań na pustej komórce. Oczekiwane: słyszysz, że jest pusta, a nie ciszę.
Cisza jest nie do odróżnienia od klawisza, który nic nie zrobił.

20.5. Odpowiedź na Twoje pytanie o usuwanie danych: stań na komórce z treścią i
naciśnij Delete. Oczekiwane: treść znika i słyszysz "Cleared". Na pustej komórce
Delete mówi, że już jest pusta. W trakcie pisania albo po F2 Delete należy do
tekstu w komórce i usuwa jeden znak, bo inaczej poprawianie literówki kasowałoby
całą komórkę.

20.6. Rzecz, którą znalazłem przy pomiarach, a której nie zgłaszałeś: wpisz coś w
komórkę i BEZ zatwierdzania naciśnij strzałkę w lewo. Oczekiwane: komórka zostaje
zatwierdzona i przechodzisz w lewo, jak w arkuszu. Wcześniej tekst wpadał w środek
tego, co już było w komórce, i powstawały sklejki. Po F2 ta sama strzałka nadal
przesuwa kursor w tekście, bo wtedy poprawiasz treść.

20.7. Kodowanie, Twoja decyzja "czyli UTF-8": otwórz stary plik z polskimi
literami zapisany w ANSI, zapisz Ctrl+S, a potem naciśnij Alt+Z dwa razy.
Oczekiwane: po zapisaniu program mówi już o UTF-8, a nie o stronie 1250. Plik raz
otwarty u nas przestaje być pułapką na polskie litery.

20.8. Kontrola do poprzedniego punktu, ważna: otwórz plik zapisany jako UTF-16
(czyli Unicode) i zapisz go. Oczekiwane: NADAL jest UTF-16. Szerokich kodowań
świadomie nie przerabiamy, bo ktoś je wybrał celowo, a zamiana zmieniłaby rozmiar
pliku.

20.9. Końce wiersza, Twoja decyzja "jednak bym był za tym, żeby zapisywał w
Windows": weź plik z końcami wiersza Linuxa, otwórz, naciśnij Alt+Z dwa razy.
Oczekiwane: słyszysz kodowanie, a po nim rodzaj końców wiersza, które plik miał na
dysku, czyli Unix.

20.10. Ten sam plik zapisz i sprawdź go czymkolwiek, co pokazuje końce wiersza.
Oczekiwane: po zapisaniu ma końce wiersza Windows. To było prawdą już wcześniej,
więc tu pilnujemy tylko, że nic się nie zepsuło.

20.11. Plik sklejony z dwóch źródeł, czyli mający i jedne, i drugie końce wiersza.
Oczekiwane: Alt+Z dwa razy mówi "mixed". Uznałem, że milczenie o takim pliku byłoby
myleniem Cię, bo taki plik istnieje realnie.

20.12. Czego tu NIE ma, żebyś nie szukał: nie ma osobnej komendy "zamień końce
wiersza w otwartym dokumencie", bo przy Twojej decyzji nie jest potrzebna - zapis i
tak zawsze robi Windows. Zapis z końcami Linuxa albo Maca na życzenie jest tam,
gdzie był, czyli w eksporcie pod Alt+Shift+E. Jeśli po użyciu uznasz, że chcesz to
mieć bliżej, na przykład w oknie zapisywania, powiedz.

20.13. Znane ograniczenie, które przy okazji zmierzyłem i wolę zgłosić sam: gdy
stary plik ANSI ma dużo zwykłego tekstu angielskiego, a polskich liter mało,
biblioteka rozpoznająca kodowanie potrafi wskazać stronę zachodnią zamiast
środkowoeuropejskiej, i wtedy ogonki nadal się psują. To nie jest nowe w tej
wersji. Obejście oznaczałoby nadpisywanie wyniku tej biblioteki własnym
zgadywaniem, co uderzyłoby w pliki niepolskie, więc czekam na Twoje zdanie, czy
warto.

## 21. Edycja gotowej tabeli i tabela jednokolumnowa (5.0.38)

Ten rozdział domyka drugi punkt mapy drogowej. Do tej pory Ctrl+Shift+T umiał
tylko wstawić nową tabelę, a gotową trzeba było poprawiać ręcznie w tekście,
kreska po kresce. Teraz ten sam skrót rozpoznaje, gdzie stoi kursor.

21.1. Otwórz plik Markdown z gotową tabelą, stań kursorem w jej środku i naciśnij
Ctrl+Shift+T. Oczekiwane: otwiera się okno o tytule "Edit Table", a w siatce jest
Twoja tabela z pliku, z treścią w komórkach.

21.2. To samo, ale stań kursorem poza tabelą, na przykład w zwykłym akapicie.
Oczekiwane: okno ma tytuł "Insert Table" i jedną pustą komórkę, czyli działa jak
dotąd. Tytuł okna jest jedynym miejscem, w którym słyszysz, w którym trybie
jesteś, więc powiedz, jeśli to za mało.

21.3. W oknie edycji popraw jedną komórkę i naciśnij Ctrl+Enter. Oczekiwane:
słyszysz "Table saved" wraz z liczbą wierszy i kolumn, a tabela w dokumencie jest
podmieniona na miejscu. Wokół niej nie pojawiają się żadne nowe puste linie.

21.4. Zaraz po tym naciśnij Ctrl+Z. Oczekiwane: tabela wraca do stanu sprzed
poprawki. Podmiana jest cofalna, bo to ta sama droga, którą wstawiamy nową.

21.5. Wejdź w edycję gotowej tabeli i od razu naciśnij Escape, nic nie zmieniając.
Oczekiwane: okno zamyka się BEZ pytania o potwierdzenie, a program mówi "Table not
changed". Pytanie przy każdym wyjściu byłoby męczące, więc pytamy tylko wtedy, gdy
naprawdę coś zmieniłeś.

21.6. Teraz wejdź, zmień jedną komórkę i naciśnij Escape. Oczekiwane: TERAZ pyta,
czy zamknąć bez zapisania zmian. Odpowiedz "nie" i sprawdź, że wracasz do siatki z
Twoją zmianą, a nie do dokumentu.

21.7. W oknie edycji dołóż kolumnę strzałką w prawo z ostatniej kolumny, wpisz coś
i zapisz. Oczekiwane: tabela w dokumencie ma nową kolumnę, także w wierszu
nagłówka i w wierszu kresek.

21.8. Tabela z wyrównaniem kolumn, czyli z dwukropkami w wierszu kresek. Wejdź w
edycję, zmień jakąś komórkę i zapisz. Oczekiwane: wyrównania są nadal na swoim
miejscu. To jest rzecz, którą łatwo przeoczyć, więc sprawdź ją osobno.

21.9. Komórka, w której treści jest kreska pionowa, na przykład "cena | netto".
Wejdź w edycję i zapisz bez zmian. Oczekiwane: komórka nadal ma jedną kreskę w
treści, a tabela nie rozpada się na więcej kolumn.

21.10. Tabela wewnątrz bloku kodu, czyli między liniami z trzema odwrotnymi
apostrofami. Stań w niej i naciśnij Ctrl+Shift+T. Oczekiwane: program NIE otwiera
jej do edycji, bo tam tabela jest tylko tekstem do pokazania. Powinien zgłosić, że
tabeli nie da się umieścić w bloku kodu.

21.11. Zwykły akapit z kreskami pionowymi, ale bez wiersza kresek pod pierwszą
linią. Stań w nim i naciśnij Ctrl+Shift+T. Oczekiwane: otwiera się PUSTE okno
wstawiania nowej tabeli, a Twój akapit zostaje nietknięty. To ważna kontrola:
program nie ma prawa uznać dowolnego tekstu z kreskami za tabelę.

21.12. Naprawiony błąd, którego nie zgłaszałeś, a znalazłem go przy tej pracy:
tabela z JEDNĄ kolumną, czyli "| Nazwa |" i pod tym "| --- |", nie była dla
programu tabelą. Zrób taką tabelę i naciśnij Escape, żeby wejść w podgląd.
Oczekiwane: podgląd mówi o niej "Table", a klawisz T po niej skacze.

21.13. Ta sama tabela jednokolumnowa w liście elementów pod F7 w podglądzie.
Oczekiwane: jest w kategorii tabel i liczy się do licznika.

21.14. Ta sama tabela jednokolumnowa: stań w niej i naciśnij Ctrl+Shift+T.
Oczekiwane: otwiera się do edycji, a nie jako nowa pusta.

21.15. Kontrola, że przy tej naprawie nic się nie zepsuło: dokument z poziomą
linią, czyli linią z trzema myślnikami. Oczekiwane: pozioma linia NADAL jest
poziomą linią, a nie tabelą bez treści. Sprawdziłem to u siebie, ale to jest
dokładnie ta rzecz, która mogłaby wyjść dopiero na Twoich plikach.

21.16. Czego tu NIE ma, żebyś nie szukał: w samym edytorze nadal nie ma
przechodzenia po komórkach tabeli strzałkami, tak jak w Wordzie. Tabelę czytasz w
tekście albo otwierasz kreatorem. Jeśli po użyciu uznasz, że chodzenie po
komórkach wprost w tekście jest potrzebne, powiedz, bo to osobna robota.

## 22. Wiersz zero w kreatorze tabeli i zapis dla Linuksa oraz Maca (5.0.39)

22.1. To Twoje zgłoszenie: numeracja od zera. Naciśnij Ctrl+Shift+T, wpisz coś i
pochodź strzałkami w górę i w dół po komórkach. Oczekiwane: NIGDZIE nie słychać
"wiersz zero". Pierwszy wiersz to nagłówek i mówi o sobie słowem, a wiersze pod
nim liczą się od jedynki.

22.2. Ta sama rzecz, ale policz głosy: jeden ruch strzałką ma dawać JEDNĄ
wypowiedź. Wcześniej czytnik mówił najpierw numer wiersza od zera, a potem
komórkę, która ten sam wiersz podawała drugi raz i od jedynki. Oczekiwane: słychać
tylko treść komórki i jej położenie.

22.3. Kontrola, że nie zepsułem tego, co ustaliliśmy poprzednio: strzałka w prawo
ma czytać treść, potem kolumnę, potem wiersz. Strzałka w dół: treść, potem wiersz,
potem kolumnę. Oczekiwane: bez zmian względem wersji, którą już znasz.

22.4. Kontrola: pusta komórka nadal mówi "blank", a nie milczy.

22.5. Kontrola: Delete na komórce nadal ją czyści i mówi "Cleared".

22.6. Kontrola: F2 nadal wchodzi w poprawianie tego, co w komórce jest, a samo
pisanie nadal zastępuje treść.

22.7. Kontrola, że okno kreatora jest stabilne: dokładaj wiersze strzałką w dół
kilka razy pod rząd, potem kolumny strzałką w prawo. Oczekiwane: okno działa
normalnie. Piszę o tym wprost, bo pierwsze podejście do tej poprawki wywalało
okno komunikatem "Unexpected Event" i tego właśnie się tu boję.

22.8. Druga rzecz z Twojej wiadomości: zapis z końcami wiersza dla Linuksa i
Maca. Ta możliwość jest w Alt+Shift+E, wybierasz z listy "unx" albo "mac".
Zrób plik z polskimi literami i wyeksportuj go jako "unx". Oczekiwane: plik
powstaje i polskie litery są w nim poprawne.

22.9. Sprawdzenie tego, co naprawiłem przy okazji: otwórz ten wyeksportowany plik
z powrotem w EdSharpNG. Oczekiwane: polskie litery są całe. Wcześniej taki plik
zapisywał się w starej stronie kodowej Windows, więc na Linuksie, dla którego był
przeznaczony, polskie litery były zepsute.

22.10. To samo dla "mac". Oczekiwane: polskie litery poprawne.

22.11. Kontrola, że nie zepsułem pozycji "asc" na tej samej liście. Oczekiwane:
tak jak dawniej, polskie litery są zamieniane na podstawowe odpowiedniki, czyli
zamiast ą jest a. Tak ma być, bo ASCII nie zna ogonków.

22.12. Czego tu NIE ma: nie dorobiłem osobnego okna wyboru końców wiersza przy
zwykłym zapisie. Napisałeś "to chyba gdzieś tam przy zapisie tylko", a ta droga
już istnieje pod Alt+Shift+E. Powiedz, jeśli chcesz to mieć również w oknie
zapisywania pod nazwą, bo to osobna zmiana.

## 23. Kreator tabeli: jeden ruch, jeden komunikat (wersja 5.0.40)

To poprawka wprost z Twojego zgłoszenia. Przysłałeś zapis tego, co słyszysz:
"gh, column 1, row 2 | gh | zaznaczony", czyli na jedno naciśnięcie strzałki
leciała treść komórki, potem ta sama treść drugi raz, a na koniec słowo
"zaznaczony". Przy pustej komórce zamiast powtórki było "(zerowy)".

23.1. Otwórz kreator tabeli (Control+Shift+T), wpisz cokolwiek w kilka komórek i
chodź po nich strzałkami w lewo i w prawo. Oczekiwane: na każdy ruch słyszysz
JEDNĄ wypowiedź, na przykład "Ala, column 1, row 1". Treść ma być powiedziana
raz, bez powtórki.

23.2. To samo strzałkami w górę i w dół. Oczekiwane: jedna wypowiedź, treść
pierwsza, potem wiersz, potem kolumna. To jest ta kolejność, którą sam ustaliłeś.

23.3. Najważniejsze w tej wersji: słowa "zaznaczony" ani "niezaznaczony" nie ma
już nigdzie przy chodzeniu po komórkach. Oczekiwane: cisza w tym miejscu, bo
zaznaczenie i tak znaczyło tylko tyle, że tam właśnie stoisz.

23.4. Pusta komórka. Stań na komórce, w której nic nie ma. Oczekiwane: słyszysz
"blank" i współrzędne, bez żadnego "zerowy" i bez powtórki.

23.5. Kontrola, że nadal wiesz, gdzie jesteś: wiersz nagłówków ma być ogłaszany
słowem "header row", a wiersze danych numerami od jedynki. Nigdzie nie ma być
zera.

23.6. Kontrola, że nie zepsułem czytania treści: w każdej niepustej komórce
słyszysz to, co w niej wpisałeś. Gdyby program zamilkł zupełnie, to byłby
poważniejszy błąd niż ten, który zgłosiłeś.

23.7. Kontrola stabilności okna: dokładaj wiersze strzałką w dół kilka razy pod
rząd i kolumny strzałką w prawo. Oczekiwane: okno działa normalnie. Uwaga
uczciwa: przy dokładaniu wiersza usłyszysz dwie wypowiedzi, ale to dwie RÓŻNE
komórki, czyli potwierdzenie zapisanej komórki i ogłoszenie nowej. Zmierzyłem to
osobno i tak ma być.

23.8. Kontrola, że edycja gotowej tabeli i wstawianie nowej nadal działają: stań
kursorem w istniejącej tabeli, naciśnij Control+Shift+T i sprawdź, czy okno
nazywa się "Edit Table", a Twoje komórki są w siatce.

23.9. Ctrl+T było wtedy odłożone i JUŻ NIE JEST aktualne w tym rozdziale:
funkcja została zdjęta z klawisza w wersji 5.0.41. Sprawdzasz to w punktach 24.10
do 24.13, nie tutaj.

## 24. Wersja 5.0.41: przypisy w rozdziale, F2 w tabeli, Ctrl+T

24.1. Przypis na końcu rozdziału, czyli sedno tego, o co prosiłeś. Otwórz plik
Markdown z kilkoma rozdziałami, stań kursorem w środku drugiego rozdziału i
naciśnij Control+Shift+K. Oczekiwane: w okienku są teraz DWA pola. Pierwsze to treść
przypisu, drugie to wybór, gdzie ta treść ma wylądować: koniec dokumentu albo
koniec bieżącego rozdziału. Wybierz koniec rozdziału i zatwierdź.

24.2. Sprawdź skutek 24.1 w tekście: treść przypisu ma leżeć na końcu DRUGIEGO
rozdziału, czyli przed nagłówkiem trzeciego, a nie na samym dole pliku. To jest
ta rzecz, o którą chodziło: po podzieleniu pliku na rozdziały przypis zostaje
przy swoim.

24.3. Program potwierdza wybór głosem. Oczekiwane: przy wyborze rozdziału
usłyszysz, że przypis wstawiono na końcu rozdziału, a przy końcu dokumentu samo
potwierdzenie wstawienia. Chodzi o to, żebyś nie musiał sprawdzać wzrokiem,
którą drogę wybrałeś.

24.4. Kontrola, że stara droga działa jak dotąd: wstaw przypis, zostawiając
domyślny wybór, czyli koniec dokumentu. Ma być dokładnie tak jak wcześniej.

24.5. Dokument BEZ nagłówków. Otwórz plik bez żadnego nagłówka i naciśnij
Control+Shift+K. Oczekiwane: w okienku jest tylko jedna możliwość, koniec dokumentu.
Świadomie nie pokazuję martwej pozycji, bo wybranie jej nie dałoby żadnego
skutku, a Ty czekałbyś na coś, co się nie stanie.

24.6. Kursor PRZED pierwszym nagłówkiem, czyli we wstępie. To samo co 24.5:
rozdziału jeszcze nie ma, więc zostaje koniec dokumentu.

24.7. F2 w tabeli, Twoje zgłoszenie o słowie "edytowano". Otwórz kreator tabeli
przez Control+Shift+T, wpisz coś do komórki, zatwierdź, wróć na nią i naciśnij
F2. Oczekiwane: słyszysz współrzędne komórki i rodzaj pola, bez słowa o
edytowaniu. U mnie na żywym NVDA czytnik mówił przedtem najpierw formant edytowania, a
potem pole edycji; teraz mówi współrzędne komórki i pole edycji.

24.8. Kontrola, że F2 nadal ma sens: rodzaj pola ZOSTAJE wymawiany celowo, bo to
jedyna informacja, która mówi, że możesz teraz pisać. Gdyby F2 zamilkł zupełnie,
to byłby gorszy błąd niż ten, który zgłosiłeś.

24.9. Kontrola, że poprawianie treści nadal działa: po F2 strzałki mają przesuwać
kursor W TEKŚCIE komórki, a nie przechodzić do sąsiedniej. Podczas zwykłego
pisania, bez F2, strzałka nadal zatwierdza komórkę i idzie dalej.

24.10. Ctrl+T, czyli funkcja, która mówiła Ci "Unexpected Event". Naciśnij Ctrl+T
w dowolnym dokumencie. Oczekiwane: NIC się nie dzieje, klawisz jest wolny.
Zgodnie z tym, co ustaliliśmy, może kiedyś pójść na tłumacza.

24.11. Sama funkcja nie zniknęła, tylko zeszła do menu, dokładnie jak Text
Combine. Znajdź pozycję Text Convert w menu Miscellaneous. Oczekiwane: pozycja jest.

24.12. Znalazłem przy okazji przyczynę tego błędu i ona jest ciekawsza niż sam
błąd. Ta funkcja czyta każdy wiersz dokumentu jako ścieżkę pliku, a Windows
odrzuca jako ścieżkę między innymi zdanie z cudzysłowem ORAZ wiersz tabeli
Markdown, czyli taki z kreskami pionowymi wokół nazw kolumn. Program przewracał się więc na dokumencie,
który sami uczymy Cię tworzyć kreatorem tabeli. Uruchom teraz Text Convert z menu
na zwykłym tekście: oczekiwane jest spokojne "No files found!", bez okna błędu.

24.13. Kontrola, że nie zepsułem Text Combine, którego chciałeś zachować: on
dzielił z Ctrl+T ten sam kod, więc miał ten sam błąd. Uruchom go z menu na
zwykłym tekście. Oczekiwane: też spokojny komunikat, bez okna błędu.

24.14. Kontrola, że kreator tabeli nie został ruszony: Control+Shift+T ma
otwierać okno tak jak w wersjach 5.0.35 do 5.0.40. To sąsiedni klawisz, więc
sprawdzam go celowo.

## 25. Litery dostepu w okienkach (Alt z litera) - wersja 5.0.42

Twoja decyzja z 29 sierpnia: "Skroty alt-litera w oknach dialogowych
wprowadzamy. Enter zatwierdza, Esc anuluuje, czyli jak u niego", a potem
uscislenie, ze maja byc we wszystkich okienkach. Zrobione naraz w calym programie,
nie tylko w nowych.

25.1. Nacisnij Control+Shift+K, czyli wstawianie przypisu. Oczekiwane: czytnik przy
polu tresci mowi nazwe pola, a po niej litere dostepu jako Alt i litera. To jest
najwazniejszy punkt tego rozdzialu: litera, ktorej nie slyszysz, jest dla Ciebie
niewidzialna.

25.2. W tym samym okienku nacisnij Tab, zeby przejsc na wybor miejsca, a potem
Alt z litera pola tresci. Oczekiwane: wracasz od razu na pole tresci, bez
tabowania.

25.3. Kontrola, ze klawisz nie robi czegokolwiek: nacisnij Alt z litera, ktorej
w tym okienku nie ma, na przyklad Alt+Z. Oczekiwane: nic sie nie dzieje, fokus
stoi tam, gdzie byl.

25.4. Nacisnij Control+Przecinek, czyli ustawienia. Oczekiwane: kazde pole ma
swoja litere i kazda jest INNA. Jesli usluszysz dwa pola z ta sama litera,
zglos to - dokladnie temu ta zmiana ma zapobiegac.

25.5. Enter zatwierdza okienko z dowolnego pola, Escape anuluje. Oczekiwane: tak
jak dotad, bez zmiany. To Twoja wlasna zasada z tej rozmowy.

25.6. Przyciski OK i Anuluj CELOWO nie maja litery. Powod: Enter i Escape juz je
obsluguja, a dwie litery zajete na darmo to dwie litery mniej dla pol i dla
przyciskow, ktore cos robia. Jesli uwazasz, ze wolisz miec je z literami, napisz
- to jedna linia zmiany.

25.7. Okienko z przyciskami akcji, na przyklad Alt+Shift+M (Manual Options).
Oczekiwane: kazdy przycisk ma swoja litere i dziala Alt z nia. Przyciski maja
pierwszenstwo przed polami, bo ich litery sa dobrane swiadomie.

25.8. Kontrola, ze pole moze zostac BEZ litery i to jest w porzadku: w oknie,
gdzie wszystkie pierwsze litery slow sa juz zajete, ostatnie pole nie dostanie
zadnej. Doszlismy do wniosku, ze pole bez litery jest lepsze niz dwa pola
odpowiadajace na ten sam klawisz. Powiedz, jesli w praktyce bedzie inaczej.

25.9. Kontrola, ze litera siedzi na POCZATKU slowa, nie w srodku. Oczekiwane:
slyszysz na przyklad litere z drugiego slowa nazwy pola, a nie trzecia litere
pierwszego slowa.

25.10. Kontrola, ze nie zepsulem list wyboru: Alt+R (ostatnie pliki) i Alt+L
(ulubione) maja dzialac jak dotad, z czytaniem nazwy pliku.

25.11. Kontrola, ze nie zepsulem kreatora tabeli: Control+Shift+T ma otwierac
okno z siatka tak jak w 5.0.35 do 5.0.41, a mowa w siatce ma byc bez zmian.

25.12. Kontrola pol wyboru: w oknie z polem zaznaczanym Alt z jego litera ma je
PRZELACZAC, a nie tylko przenosic fokus. Tak dziala Windows i tak ma dzialac u nas.

25.13. Skrypty JAWS, o ktore pytalem. Napisales, ze nie testujesz z JAWS-em i na
razie nie potrzebujesz ich w instalatorze, dopiero przy upublicznieniu. Nic wiec
w tej wersji nie zmienialem - zapisane na pozniej. Nie ma tu czego testowac,
punkt jest tylko po to, zebys wiedzial, ze temat nie zaginal.

## 26. Komentarze wewnetrzne - wersja 5.0.43

Twoje zlecenie z 28 sierpnia: "A komentarze, nie da sie robic Markdown
komentarzy zgodnych potem z Wordem? Jakas tu ich sensowna obsluga?", a potem
Twoje rozstrzygniecie: "To na razie komentarze wewnetrzne, a pozniej komentarze
dla Worda, ale to juz mocno pozniej". To jest punkt czwarty mapy drogowej.

Komentarz wewnetrzny to uwaga robocza dla Ciebie samego. Zostaje w pliku
Markdown, a przy zamianie na Worda, strone albo czysty tekst wylatuje. To NIE
jest komentarz Worda na marginesie - tego sam odlozyles na pozniej i tu go nie
ma.

Klawisze sa w rodzinie F9, bo sam ja na to przeznaczyles slowami "szkoda mi tego
F9, wolalbym na komentarze albo inne funkcje typu Tlumaczenie zostawic".

GOLY F9 I SHIFT+F9 SA WOLNE OD 5.0.63, na Twoja decyzje z 03.09.2026. Do 5.0.62
obslugiwaly czytanie tekstu do konca przez skrypt JAWS - dlatego wygladaly na
wolne, a naprawde byly zajete, i dlatego kiedys nie chcialem ich zabierac bez
Twojego slowa. Napisales, ze z tego klawisza i skryptu rezygnujemy, wiec obsluga
zeszla z programu. Same pliki skryptow autora zostaja w paczce.

Uklad, ktory dostajesz: Alt+F9 wstawia i poprawia, Control+Shift+F9 idzie do
nastepnego, Alt+Shift+F9 do poprzedniego, Control+Alt+F9 pokazuje liste.
Control+F9, czyli mowienie kompilatora, zostaje nietkniety.

26.1. Otworz plik z rozszerzeniem md, stan w zdaniu i nacisnij Alt+F9.
Oczekiwane: otwiera sie okienko o tytule Insert Comment z jednym polem na tresc.
Wpisz uwage i zatwierdz Enterem. Oczekiwane: program mowi, ze komentarz
wstawiony, a kursor stoi za nim, wiec mozesz pisac dalej.

26.2. Zostan tam, gdzie jestes po punkcie 26.1, i nacisnij Alt+F9 jeszcze raz.
Oczekiwane: tytul okienka to teraz Edit Comment, a w polu jest Twoja poprzednia
tresc gotowa do poprawki. To ten sam klawisz robiacy dwie rzeczy, tak jak
Control+Shift+T przy tabelach - tytul okna jest miejscem, gdzie slyszysz, ktory
tryb dostales.

26.3. W okienku poprawki wyczysc cale pole i zatwierdz. Oczekiwane: komentarz
znika z dokumentu, a program mowi, ze zostal usuniety. Zglos, jesli wolisz, zeby
usuwanie mialo wlasny klawisz - swiadomie go nie dolozylem, bo to rzecz robiona
rzadko.

26.4. Wstaw drugi komentarz nizej w dokumencie, wroc kursorem nad pierwszy i
nacisnij Control+Shift+F9. Oczekiwane: kursor skacze na pierwszy komentarz, a
czytnik mowi najpierw TRESC uwagi, a potem slowo comment. Nacisnij
Control+Shift+F9 jeszcze raz: kursor idzie na drugi.

26.5. Nacisnij Control+Shift+F9 stojac na ostatnim komentarzu. Oczekiwane: kursor
NIE wraca na poczatek, zostaje w miejscu, a program mowi, ze to ostatni
komentarz. Tak samo Alt+Shift+F9 na pierwszym. Zrobione celowo: ciche zawijanie
kazaloby Ci szukac uwagi, ktora juz przeczytales.

26.6. Alt+Shift+F9 idzie do komentarza POPRZEDNIEGO. Oczekiwane: to samo co 26.4,
tylko w druga strone.

26.7. Nacisnij Control+Alt+F9. Oczekiwane: lista wszystkich komentarzy, kazdy
z numerem wiersza, w ktorym siedzi. Enter idzie na wybrany. Numer wiersza jest
tam zamiast numeru kolejnego, bo szukasz MIEJSCA w dokumencie, a nie tego,
ktory komentarz napisales jako trzeci.

26.8. NAJWAZNIEJSZY PUNKT TEGO ROZDZIALU. Wstaw komentarz, zapisz plik i zrob
podglad pod Escape. Oczekiwane: uwagi NIE WIDAC na stronie podgladu. Potem
wyeksportuj ten plik do Worda i sprawdz, czy tresci uwagi w dokumencie nie ma.
To jest cala istota tej funkcji i to zmierzylem naszymi wlasnymi narzedziami
przed wyslaniem, ale Twoja maszyna rozstrzyga.

26.9. Wpisz w tresc komentarza dwa myslniki i nawias zamykajacy, czyli dokladnie
to, co konczy komentarz. Oczekiwane: reszta uwagi NIE wysypuje sie do widocznego
dokumentu - program zabezpiecza taka tresc przy wstawianiu. Jesli zobaczysz
polowe uwagi w tekscie, to blad i chce o nim wiedziec.

26.10. Nacisnij Control+Z po wstawieniu komentarza. Oczekiwane: cofa sie jednym
ruchem, jak kazda inna zmiana. Na dysku nic nie jest zapisywane bez Twojej
komendy.

26.11. KONTROLA, ze nie ruszylem niczego dzialajacego: Control+F9 ma dalej mowic
kompilator i katalog, dokladnie jak dotad. Tego klawisza nie tknalem. Goly F9 i
Shift+F9 nie robia od 5.0.63 NICZEGO i tak ma byc: sam z nich zrezygnowales.
Czytanie calego tekstu wlasna mowa programu zostaje na Alt+F8, bez zmian.

26.12. KONTROLA na bloku kodu: wstaw w dokument blok kodu ogrodzony trzema
odwrotnymi apostrofami, a w nim wpisz recznie tekst komentarza HTML. Oczekiwane:
Control+Shift+F9 tego NIE widzi jako komentarza, a Alt+F9 postawiony w bloku
odmawia wstawienia nowego. Powod: w bloku kodu to przyklad, ktory podglad
pokazuje jako zwykly widoczny tekst, wiec skakanie po nim wprowadzaloby w blad.

26.13. KONTROLA na innym typie pliku: otworz plik txt i nacisnij Alt+F9.
Oczekiwane: program mowi, ze komentarze dzialaja tylko w plikach Markdown. Ta
sama bramka co przy przypisach i tabelach - w txt komentarz bylby widocznym
smieciem.

26.14. KONTROLA w podgladzie: nacisnij Alt+F9 przy otwartym podgladzie.
Oczekiwane: program prosi o zamkniecie podgladu. W podgladzie nie ma gdzie pisac.

26.15. KONTROLA, ze nie zepsulem przypisow ani tabel: Control+Shift+K ma dalej
wstawiac przypis z wyborem miejsca, a Control+Shift+T otwierac kreator tabeli.
Te dwie rzeczy dziela z komentarzami ten sam kod rozpoznawania blokow kodu, wiec
to najwazniejsza kontrola tej wersji.
## 27. Porzadkowanie skrotow: wersja 5.0.44 (Twoja mini korekta)

To jest wykonanie Twojej mini korekty z 29.08. Usunelismy dziewiec komend, ktore
sluzyly bogatemu formatowaniu. Rozdzial jest krotki, bo przy USUWANIU sprawdza sie
dwie rzeczy: czy to, co mialo zniknac, zniknelo, i czy nie zniknelo nic wiecej.
Wieksza czesc punktow to KONTROLE tego drugiego.

27.1. Otworz menu Navigate i przejrzyj je strzalkami do konca. Oczekiwane: NIE MA
tam pozycji Next Alignment, Prior Alignment, Next Style, Prior Style, Next
Baseline, Prior Baseline, Next Font ani Prior Font. Osiem pozycji mniej.

27.2. Otworz menu Misc i przejrzyj je do konca. Oczekiwane: nie ma pozycji
Calculate Date.

27.3. Nacisnij po kolei kazdy ze zwolnionych klawiszy w oknie edycji: Control z
prawym nawiasem kwadratowym, Control z lewym nawiasem kwadratowym, Control z
ukosnikiem, Control+Shift z ukosnikiem, Control+F2, Control+Shift+F2, Control z
myslnikiem, Control+Shift z myslnikiem oraz Control+Shift ze srednikiem.
Oczekiwane: NIC sie nie dzieje i nic nie jest mowione. Zaden z nich nie ma juz
przypisanej komendy. Jesli ktorykolwiek cos robi, chce o tym wiedziec.

27.4. Nacisnij Control+F1 i posluchaj spisu skrotow do konca albo poszukaj w nim
slow "Next Font" i "Calculate Date". Oczekiwane: nie ma ich tam. To wazne osobno
od punktu 27.1, bo spis skrotow czyta Ci opisy, ktore da sie zostawic martwe -
opis obiecujacy klawisz, po ktorym nic sie nie dzieje, jest gorszy niz jego brak.

27.5. KONTROLA, ze zostalo to, co miesci sie w Twojej decyzji. Sprawdz, ze
DZIALAJA nadal: Alt+Shift+J (okno wyrownania), Alt+Shift z ukosnikiem (okno stylu,
czyli pogrubienie i kursywa), Alt+Shift+F6 (okno indeksu gornego i dolnego) oraz
Alt+Shift z myslnikiem (okno kroju pisma).
Wyjasniam, dlaczego ich NIE usunalem, mimo ze dotycza formatowania: Ty kazales
usunac SKOKI po zmianach formatowania, bo w Markdownie nie ma po czym skakac.
Te cztery okna formatowanie USTAWIAJA, a program nadal otwiera i zapisuje pliki
RTF, w ktorych to ma sens. Jesli uwazasz, ze maja zniknac razem z reszta, powiedz
i zdejmiemy je nastepnym razem - to jedna decyzja, nie nowa funkcja.

27.6. KONTROLA: Alt z ukosnikiem ma dalej mowic styl i wyrownanie pod kursorem, a
Alt z myslnikiem krój pisma i kolor. Te dwie komendy nie ruszaja kursora, tylko
odpowiadaja na pytanie "co tu jest", wiec zostaly.

27.7. KONTROLA: Alt+Shift ze srednikiem ma dalej wstawiac biezaca date i godzine.
Usunieta zostala tylko ta druga komenda, ktora pytala o rok, miesiac, tydzien i
dzien, i liczyla date z kalendarza.

27.8. KONTROLA, ze nie zepsulem ostatnich wersji: Control+Shift+T ma otwierac
kreator tabeli, Control+Shift+K wstawiac przypis, a Alt+F9 komentarz. Wersje 5.0.41 do
5.0.43 ruszaly kod wspolny wielu okien, wiec przy kazdym porzadkowaniu to jest
pierwsze miejsce, w ktorym moglbym cos zlamac.

27.9. KONTROLA na oknach: otworz dowolne okno dialogowe, na przyklad Control z
przecinkiem (ustawienia), i sprawdz, ze litery dostepu Alt z litera dzialaja jak w
5.0.42. Ta wersja nie dotykala tego kodu, ale pytam, bo to Twoja maszyna
rozstrzyga.

27.10. Jesli masz w ustawieniach programu zapisane wartosci obliczania daty (rok,
miesiac, tydzien, dzien), to po tej wersji przestaja byc zapisywane i czytane.
Nie musisz nic robic - zglaszam to tylko, zeby nie bylo niespodzianki.

## 28. Porzadkowanie skrotow: wersja 5.0.45 (zakladki, link, rodzina F4)

To jest wykonanie Twoich decyzji z rozmow 28 i 29 sierpnia. Zmienilo sie piec
rodzin klawiszy naraz, wiec punkty ida po rodzinach, nie po wersjach.

Najwazniejsze trzy punkty, jesli masz malo czasu: 28.3 (zakladka pod nowym
klawiszem), 28.6 (link wewnetrzny do rozdzialu) i 28.12 (kontrola, ze nic
innego nie przestalo dzialac).

### Bloki kodu - znikaja

28.1. Nacisnij Control+B w dowolnym miejscu tekstu. Program NIE ma juz skakac po
blokach kodu - Control+B wstawia teraz zakladke, o czym jest punkt 28.3.

28.2. Nacisnij Alt+B. NIE ma juz mowic o biezacym bloku kodu, tylko pokazywac
liste zakladek. Sprawdz tez menu Nawigacja: pozycji "Next Block" i "Prior Block"
ma tam nie byc, a w menu Zapytania nie ma juz pozycji "Block".

### Zakladki - na klawisz B

28.3. Ustaw kursor gdzies w tekscie zapisanego pliku i nacisnij Control+B. To ma
wstawic zakladke, dokladnie tak jak wczesniej robil to Control+K. Potem nacisnij
Alt+B: przy jednej zakladce program ma od razu do niej skoczyc, a przy kilku
pokazac liste.

28.4. Na liscie zakladek nacisnij Delete na wybranej pozycji. Ma ja usunac i
powiedziec o tym. To jest teraz jedyna droga na co dzien - osobnego klawisza do
usuwania zakladki juz nie ma, bo sam napisales, ze Delete na liscie wystarcza.
Sama komenda "Clear Bookmark" nadal jest w menu Nawigacja, gdyby byla potrzebna.

28.5. KONTROLA: Control+K NIE ma juz wstawiac zakladki, a Control+Shift+K nie ma
robic nic. Oba klawisze zmienily wlasciciela albo sa wolne.

### Wstawianie linku pod Control+K

28.6. W pliku Markdown, ktory ma kilka rozdzialow, nacisnij Control+K. Otworzy
sie okienko z polami: tresc linku, adres i rodzaj. Wybierz rodzaj "Place in this
document", a nizej pojawi sie lista naglowkow tego dokumentu. Wybierz rozdzial i
zatwierdz. W tekscie ma wyladowac odsylacz do tego rozdzialu.
To jest ten punkt, ktory prosze sprawdzic dokladnie: eksportuj potem plik do
Worda albo do HTML i sprobuj kliknac ten odsylacz. Ma prowadzic do rozdzialu, a
nie w nicosc. Kotwice liczymy dokladnie tak samo jak w spisie tresci, wiec jesli
spis dziala, to i ten link powinien.

28.7. Zaznacz slowo w tekscie i nacisnij Control+K. Zaznaczone slowo ma sie
pojawic w polu tresci jako propozycja - wystarczy dopisac adres.

28.8. Wybierz rodzaj "Image" i zatwierdz z PUSTYM polem tresci. Program ma
odmowic i powiedziec, ze obrazek potrzebuje opisu. To celowe: obrazek bez opisu
jest dla czytnika ekranu niewidzialny.

28.9. Wstaw link zwykly bez wpisywania tresci, za to z adresem. Program ma
uzyc adresu jako tresci linku, zamiast wstawiac odsylacz, ktory nic nie mowi.

28.10. KONTROLA: sprobuj Control+K w srodku bloku kodu (miedzy liniami z
trzema grawisami). Program ma odmowic i powiedziec dlaczego - w bloku kodu
odsylacz i tak bylby wypisany jako zwykly tekst.

### Rodzina F4

28.11. Sprawdz cala rodzine po kolei:
F4 - lista otwartych okien, jak dotad.
Shift+F4 - lista folderow Twoich ostatnich i ulubionych plikow. WCZESNIEJ ten
klawisz wyliczal tytuly okien; przestal, bo liste okien daje F4.
Control+Shift+F4 - lista folderow systemowych Windows.
Control+W - zamyka biezacy plik, jak dotad.
Control+Shift+W - zamyka wszystkie okna poza biezacym. Wczesniej bylo to pod
Control+Shift+F4.

28.12. KONTROLA, ze nic nie zginelo po drodze: Control+F4 ma nadal zamykac
biezace okno, a Control+1 do Control+9 przechodzic miedzy oknami. Stare
Control+0 i Control+Alt+0, ktore trzymaly foldery, maja teraz nie robic nic.

28.13. PYTANIE, ktorego nie rozstrzygalem sam. W Twoim ukladzie F4 pisales tez
"Control+F4 zmiana folderu". Control+F4 to dzis ZAMKNIJ OKNO i tak jest w
kazdym programie wielookienkowym Windows. Oddanie go folderom odebraloby
standardowy klawisz zamykania, wiec tego nie zrobilem. Powiedz jednym slowem,
czy mimo to ma tam pojsc zmiana folderu, czy zostawiamy zamykanie.

### Control+T

28.14. Otworz menu Rozne. Pozycji "Text Convert" ma tam juz NIE BYC - poprzednio
zdjalem tylko klawisz, teraz zniknela cala funkcja, zgodnie z Twoim "zglasza blad
Unexpected Event i jest zbedna". Control+T jest wolny.

28.15. KONTROLA: pozycja "Text Combine" ma w tym samym menu ZOSTAC i dzialac.
Obie komendy siedzialy w jednym kawalku kodu, wiec to jest miejsce, w ktorym
najlatwiej bylo mi wyciac za duzo.

### Kontrole ogolne

28.16. KONTROLA: Control+I i Control+Shift+I maja nadal skakac po zmianach
wciecia, a Alt+I mowic o wcieciu. Zostawilem je swiadomie - wciecie ma
znaczenie takze w zwyklym tekscie, a Ty rozstrzygales tylko o blokach kodu.

28.17. KONTROLA rzeczy z poprzednich wersji: Control+Shift+T ma otwierac kreator
tabeli, Alt+F9 komentarz, a Alt+Shift+T pisac spis tresci. Przypisy przeniosly
sie w tej paczce na rodzine K - o nich jest rozdzial 29. Ta wersja ich nie dotykala, ale przesunela piec rodzin klawiszy naraz,
wiec pytam.

28.18. KONTROLA: Control+F1 na kazdym ze zmienionych klawiszy ma mowic to, co
klawisz REALNIE robi. Jesli uslyszysz stary opis, to jest blad i prosze o
zgloszenie - blednie podpowiedziany skrot jest gorszy niz brak podpowiedzi.

## 29. Przypisy przeprowadzone na rodzinę K (5.0.46)

Ta wersja robi jedną rzecz i wynika wprost z Twojego układu z wczorajszego
wieczora. Sam sformułowałeś zasadę: "Alt literki to były pewne listy a z Ctrl
się wstawiała reszta". Przypisy stały wbrew niej w rodzinie F6, więc przeszły
tam, gdzie siedzi ich rodzeństwo, czyli link pod Control+K.

Powód jest jednak konkretniejszy niż porządek. Powiedziałeś "to CTRL-F6 linki
fajne", czyli Control+F6 ma pokazywać listę linków w dokumencie. Nie mógł jej
dostać, dopóki wstawiał przypis. Ta paczka zwalnia mu miejsce; sama lista linków
to następny krok.

Nowy układ:
Control+Shift+K - wstawia przypis (wcześniej Control+F6).
Alt+K - lista przypisów (wcześniej Control+Shift+F6).
Alt+F6 - skok między znacznikiem w zdaniu i treścią przypisu, BEZ ZMIAN.

29.1. Otwórz plik z rozszerzeniem md, postaw kursor na końcu jakiegoś zdania i
naciśnij Control+Shift+K. Oczekiwane: otwiera się to samo okienko co dotąd,
wpisujesz treść, Enter, a czytnik mówi "Footnote 1 inserted".

29.2. Naciśnij Alt+K. Oczekiwane: otwiera się okno "Footnotes" z listą
przypisów, Enter idzie do wybranego, Escape zamyka bez ruszania kursora.

29.3. To najważniejszy punkt tego rozdziału. Naciśnij STARE klawisze, czyli
Control+F6 i Control+Shift+F6. Oczekiwane: nie dzieje się NIC - żadnego okienka,
żadnego komunikatu, żadnego dźwięku błędu. Gdyby któryś z nich nadal coś robił,
to znaczy, że stara komenda została obok nowej, a wtedy jeden z klawiszy będzie
nieosiągalny. Zgłoś to od razu.

29.4. KONTROLA, że kontekstowy skok nadal działa, ale POD NOWYM KLAWISZEM.
Odpowiedziałeś na pytanie 29.5 z tej listy, więc skok przeniosłem - szczegóły
i punkty do sprawdzenia są w rozdziale 30. Tutaj zostaje tylko tyle: stary
Alt+F6 ma już NIC nie robić.

29.5. PYTANIE ROZSTRZYGNIĘTE - to już nie jest pytanie. Odpowiedziałeś
30.08.2026: "Skok przypis tekst tekst przypis robimy Alt-CTRL-ka to będzie
lepsza komenda chyba co? (...) Tak z ego F6 w przypisach byśmy rezygnowali".
Zrobione w 5.0.47, cała rodzina F6 wyszła z przypisów. Rozdział 30.

29.6. KONTROLA, że nie zepsułem rodzin, które zmieniły się dzień wcześniej.
Control+B ma wstawiać zakładkę, Alt+B pokazywać jej listę, Control+K wstawiać
link. Wszystkie trzy przyszły w 5.0.45 i ta paczka ich nie dotykała, ale ruszała
kod tuż obok.

29.7. KONTROLA komentarzy z 5.0.43: Alt+F9 wstawia komentarz, Control+Shift+F9
idzie do następnego, Alt+Shift+F9 do poprzedniego, Control+Alt+F9 daje listę.
Rodzina F9 nie była tu ruszana.

29.8. KONTROLA opisów mówionych. Naciśnij Control+F1 i sprawdź, co program mówi
o wstawianiu przypisu i o liście przypisów. Oczekiwane: podaje NOWE klawisze.
Jeśli usłyszysz stary Control+F6, to jest błąd - podpowiedź, która kłamie, jest
gorsza niż jej brak.

29.9. Czego u siebie NIE sprawdzę: Twojego czytnika i Twoich ustawień mowy.
Wstawianie przypisu i listę zmierzyłem na żywym NVDA u nas, razem z kontrolą, że
stare klawisze naprawdę milczą, ale to Twoja maszyna rozstrzyga.

## 30. Skok przypisu na Control+Alt+K i koniec nagrywania płyt (5.0.47)

Dwie rzeczy z Twoich wiadomości z 30 sierpnia rano. Obie były jednoznaczne, więc
nie dopytywałem.

Skok między znacznikiem przypisu w zdaniu i jego treścią przeszedł z Alt+F6 na
Control+Alt+K, czyli tam, gdzie proponowałeś. Rodzina F6 wychodzi z przypisów
zupełnie - zgodnie z Twoim "z ego F6 w przypisach byśmy rezygnowali" - i jest
wolna pod listę linków.

Przy tej okazji zmierzyłem coś, o co nie pytałeś, a co przesądziło o kształcie
tej zmiany. Program ma w środku zasadę, że milczy, gdy trzymasz Control razem
z Altem. Powstała dawno i sama w sobie ma sens, ale znaczyła, że komenda
postawiona na Control+Alt+K wykonałaby skok W CISZY. Kursor by się przeniósł,
a Ty nie usłyszałbyś ani treści przypisu, ani powodu odmowy. To jest dokładnie
to, co przy czytniku wygląda jak zepsuty klawisz. Wszystkie wypowiedzi tej
komendy chodzą teraz obok tej zasady, tak samo jak przy przesuwaniu rozdziałów
na Control+Alt ze strzałką.

Wyszło z tego jeszcze jedno, czego nie zgłaszałeś: tryb opisywania klawiszy pod
Control+F1 MILCZAŁ dla wszystkich komend z Control i Altem naraz. Czyli funkcja
do nauki skrótów nie działała przy skrótach najtrudniejszych do zapamiętania.
Poprawione dla całej tej rodziny, nie tylko dla przypisów.

Nagrywanie płyt pod Alt+Shift+B usunięte. Zwolniony klawisz ma już przeznaczenie:
to w Twoim układzie lista zakładek z nazwą. Przy okazji policzyłem, że ta funkcja
i tak była martwa - instalator nigdy nie pakował programu, który ją wykonywał,
więc u każdego, kto zainstalował EdSharpNG z naszej paczki, pytała o rozszerzenia
i nie robiła nic.

30.1. Otwórz plik md, w którym masz przypis, postaw kursor w zdaniu ze
znacznikiem i naciśnij Control+Alt+K. Oczekiwane: kursor ląduje na treści
przypisu na końcu dokumentu, a czytnik mówi tę treść i numer.

30.2. Naciśnij Control+Alt+K jeszcze raz, stojąc na treści. Oczekiwane: wracasz
do zdania ze znacznikiem, czytnik mówi zdanie i "footnote 1 in text". Jeden
klawisz w obie strony, bez dwóch osobnych komend.

30.3. To najważniejszy punkt tego rozdziału, bo dotyczy tego, co zmierzyłem
u siebie, a co rozstrzyga Twoja maszyna: czy przy Control+Alt+K NAPRAWDĘ
SŁYSZYSZ mowę. Naciśnij ten skrót kilka razy, raz szybko, a raz TRZYMAJĄC
klawisze dłużej, tak jak wychodzi w normalnym pisaniu. Oczekiwane: za każdym
razem coś słyszysz. Cisza przy dłuższym przytrzymaniu jest błędem i proszę,
zgłoś ją, bo to jedyny objaw, którego moja sonda mogła nie odtworzyć - program
testujący puszcza klawisze szybciej niż ręka.

30.4. Otwórz plik md BEZ przypisów i naciśnij Control+Alt+K. Oczekiwane: słyszysz
"No footnotes!". To ten sam sprawdzian co 30.3, tylko od strony odmowy - i to
właśnie ten komunikat milczał, dopóki nie poprawiłem mowy.

30.5. Naciśnij Control+Alt+K w pliku txt. Oczekiwane: słyszysz, że przypisy
działają tylko w plikach Markdown, i nic się nie wstawia.

30.6. Naciśnij STARY klawisz Alt+F6. Oczekiwane: nie dzieje się NIC. Gdyby nadal
skakał, znaczyłoby to, że stara komenda została obok nowej.

30.7. KONTROLA, że nie zabrałem Ci nic z przypisów: Control+Shift+K nadal wstawia
przypis, Alt+K nadal pokazuje ich listę. Oba przyszły w 5.0.46 i tu ich nie
ruszałem.

30.8. Naciśnij Control+F1, a potem Control+Alt+K. Oczekiwane: program MÓWI, co
ten skrót robi. Wcześniej w tym trybie każda komenda z Control i Altem milczała,
więc jeśli usłyszysz opis, to znaczy, że poprawka weszła. Sprawdź przy okazji
Control+Alt+F9, czyli listę komentarzy - to ta sama rodzina.

30.9. Sprawdź, że nagrywanie płyt zniknęło: w menu Misc nie ma pozycji "Burn to
CD", a Alt+Shift+B nic nie robi.

30.10. KONTROLA, że przy usuwaniu nie zabrałem sąsiada: Control+Shift+P, czyli
Path List, ma nadal zbierać ścieżki plików z folderu do dokumentu. Ta funkcja
korzystała ze wspólnego kodu z nagrywaniem płyt i to jest miejsce, w którym
mogłem zepsuć coś działającego.

30.11. KONTROLA, że Alt+Shift+W nadal pobiera pliki ze strony. Ta pozycja
siedziała w menu dokładnie obok usuniętej.

30.12. KONTROLA rodzin zmienionych w poprzednich paczkach: Control+B wstawia
zakładkę, Alt+B daje jej listę, Control+K wstawia link, Alt+F9 wstawia komentarz,
Control+Shift+T otwiera kreator tabeli.

30.13. Czego u siebie NIE sprawdzę: Twojego czytnika i Twoich ustawień mowy.
Mowę przy Control+Alt+K zmierzyłem na żywym NVDA u nas, 13 punktów na 13, razem
z kontrolą, że stary Alt+F6 naprawdę milczy. Zmierzyłem też, że bez poprawki
mowy odmowa "No footnotes!" była CISZĄ - więc poprawka realnie coś zmienia,
a nie jest ostrożnością na zapas. Ale to Twoja maszyna rozstrzyga, zwłaszcza
w punkcie 30.3.

## 31. Strażnik pisania: skrót nie może zabrać polskiej litery (5.0.48)

To odpowiedź na Twoje pytanie z 30 sierpnia: "Mam nadzieję, że tu się nie
dubluje, bo Alt+Ctrl to też to samo co prawy Alt, a prawy Alt to polska
literka, ale można to jakoś tak zrobić, żeby działało prawidłowo."

Miałeś rację co do klawiatury i to warto powiedzieć wprost. Na polskim
układzie prawy Alt naprawdę jest tym samym co Control z Altem, a dziewięć
polskich liter powstaje właśnie tak: ą na klawiszu A, ć na C, ę na E, ł na L,
ń na N, ó na O, ś na S, ź na X i ż na Z. Zmierzyłem to, pytając sam Windows,
a nie z pamięci.

Nie dublowało się jednak w tym konkretnym miejscu: K, F9 ani strzałki nie
tworzą żadnej polskiej litery, więc Control+Alt+K, lista komentarzy pod
Control+Alt+F9 i przesuwanie sekcji strzałkami nie zabierają Ci pisania.
Sprawdziłem to również na żywym programie: wpisałem wszystkie dziewięć liter
przez prawy Alt i wszystkie znalazły się w dokumencie, a Control+Alt+K w tym
samym przebiegu nadal skakał do przypisu.

Twoje "można to jakoś tak zrobić, żeby działało prawidłowo" wzięliśmy jednak
poważniej niż jako pytanie o jeden klawisz. Program dostał strażnika: przy
starcie sam pyta układ klawiatury, które chordy wpisują znak, i odmawia
postawienia na takim chordzie komendy. Powód jest praktyczny. Gdyby ktoś
kiedyś przypisał komendę na przykład do Control+Alt+E, litera ę zniknęłaby po
cichu: program by się zbudował, menu wyglądałoby dobrze, żaden alarm o
duplikacie by nie padł, a Ty dowiedziałbyś się o tym, dopiero nie mogąc napisać
słowa. Sprawdziliśmy to nie teoretycznie: zbudowaliśmy taką wersję celowo i ę
faktycznie przestało wchodzić, a po włączeniu strażnika ta sama wersja mówi
przy starcie, że nie przypisze skrótu, bo ten chord wpisuje znak.

Strażnik pyta układ, a nie ma listy liter w kodzie. Na klawiaturze bez polskich
znaków po prostu nigdy się nie odezwie.

31.1. Napisz ą, ć, ę, ł, ń, ó, ś, ź i ż prawym Altem w zwykłym dokumencie.
Wszystkie powinny wejść normalnie, bez żadnego komunikatu.

31.2. Wielkie litery: prawy Alt z Shiftem, czyli Ą, Ć, Ę, Ł, Ń, Ó, Ś, Ź, Ż.
Też powinny wchodzić.

31.3. Control+Alt+K w dokumencie z przypisem: ma nadal skakać między
znacznikiem i treścią, i ma mówić. To ten sam punkt, o który prosiłem
w rozdziale 30, więc jeśli już go sprawdziłeś, wystarczy poprzednia odpowiedź.

31.4. Kontrola, że nic nie zniknęło: Control+Alt+F9 nadal pokazuje listę
komentarzy, a Control+Alt ze strzałką w górę i w dół nadal przesuwa sekcję.

31.5. Kontrola, że strażnik nie jest przewrażliwiony: przy starcie programu NIE
powinien pojawić się żaden komunikat o skrótach. Jeśli zobaczysz okienko
mówiące, że program nie może przypisać jakiegoś skrótu, powiedz jakiego -
to znaczyłoby, że strażnik złapał coś, czego nie powinien.

31.6. Zwykłe skróty z samym Control albo samym Altem mają działać jak wcześniej:
Control+S zapis, Alt+K lista przypisów, Control+Shift+K wstawienie przypisu.
Strażnik dotyczy tylko chordów z Control i Altem naraz.

31.7. Czego u siebie NIE sprawdzę: Twojego układu klawiatury i Twoich ustawień.
Mierzyliśmy na układzie Polish (Programmers), bo taki jest u nas ustawiony.
Jeśli używasz innego, na przykład maszynistki, litery mogą siedzieć na innych
klawiszach - strażnik i tak zapyta Twój układ, ale to Twoja maszyna
rozstrzyga, czy pisanie działa bez zmian.

## 32. Nawigacja po wyróżnieniach i listach (5.0.49)

Rozdział sprawdza cztery nowe skróty, które weszły po Twoim rozstrzygnięciu
z 30 sierpnia: Control z ukośnikiem i Control+Shift z ukośnikiem chodzą po
pogrubieniach i pochyleniach, Control z myślnikiem i Control+Shift z myślnikiem
po listach. Do testów potrzebny plik .md z pogrubieniem, kursywą, listą
wypunktowaną i numerowaną.

32.1. Stań na początku dokumentu i naciśnij Control z ukośnikiem. Kursor ma
trafić do pierwszego pogrubienia albo pochylenia w tekście, a czytnik ma
powiedzieć jego treść, a po niej rodzaj: bold, italic albo bold italic.

32.2. Sprawdź, czy słyszysz SAMĄ treść, bez gwiazdek. Prosiłeś o „sam pogrubiony
fragment", więc kursor staje na pierwszej literze tekstu, nie na gwiazdce.

32.3. Control+Shift z ukośnikiem ma cofnąć do poprzedniego wyróżnienia.
Na pierwszym w dokumencie ma powiedzieć „First bold or italic text" i zostać
w miejscu, bez zawijania na koniec pliku.

32.4. Rodzaj wyróżnienia ma zależeć od liczby znaczników: jedna gwiazdka to
italic, dwie to bold, trzy to bold italic. Podkreślnik ma działać tak samo jak
gwiazdka, bo taki jest Markdown.

32.5. KONTROLA, że komenda nie skacze na śmieci: wpisz w dokumencie wiersz listy
zaczynający się gwiazdką („* pozycja"), zdanie z mnożeniem („2 * 3 = 6") oraz
nazwę z podkreślnikami („nazwa_z_podkresleniem"). Control z ukośnikiem NIE
POWINIEN zatrzymywać się na żadnym z nich - to nie są wyróżnienia.

32.6. KONTROLA bloku kodu: gwiazdki wewnątrz bloku kodu (trzy grawisy) mają być
pomijane, tak samo jak przy nagłówkach i przypisach.

32.7. W dokumencie bez pogrubień i kursywy Control z ukośnikiem ma powiedzieć
„No bold or italic text" i nic nie zrobić - nie ma milczeć.

32.8. Control z myślnikiem ma przejść na POCZĄTEK następnej listy, nie na
kolejną pozycję. Czytnik ma powiedzieć treść pierwszej pozycji i ile pozycji ma
ta lista.

32.9. Sprawdź, czy w dokumencie z dwiema listami (wypunktowaną i numerowaną)
komenda robi DWA przystanki, nie tyle, ile jest pozycji razem. Wewnątrz listy
pracujesz strzałką w dół - tak to ustaliliśmy.

32.10. Control+Shift z myślnikiem ma cofnąć do początku poprzedniej listy,
a na pierwszej powiedzieć „First list".

32.11. W dokumencie bez list Control z myślnikiem ma powiedzieć „No lists".

32.12. KONTROLA, że nic nie zniknęło: Alt z ukośnikiem nadal mówi styl pod
kursorem, Alt z myślnikiem nadal mówi czcionkę, a Alt+Shift z tymi klawiszami
nadal otwiera okna ustawiania formatowania. Nowe skróty mają Control, nie Alt.

32.13. KONTROLA, że skoki po wcięciach nadal chodzą na Control+I
i Control+Shift+I, a przypisy na Control+Alt+K i Alt+K.

32.14. Control+F6 - SPROSTOWANIE, nie test. Napisaliśmy Ci wcześniej, że to
systemowy skrót Windows „wygrywa" z naszym przypisaniem listy linków. To była
nasza pomyłka: sprawdziliśmy kod i listy linków tam po prostu NIE MA, nigdy nie
została napisana. Twój objaw był poprawny, diagnoza nasza - błędna. Nic tu nie
testuj; klawisz zostaje zarezerwowany, a przy pisaniu listy odbierzemy go
systemowi.

32.15. Czego u siebie NIE sprawdzę: czy mowa brzmi naturalnie przy dłuższych
fragmentach pogrubionego tekstu i czy liczba pozycji listy zgadza się w Twoich
prawdziwych dokumentach, gdzie listy bywają zagnieżdżone. Sonda mierzy parser
i binarkę, Twoje ucho rozstrzyga.

## 33. Lista linków pod Control+F6 (5.0.53)

To wykonanie trzech Twoich ustaleń naraz: Control+F6 ma być listą linków
analogicznie do F6 dla nagłówków, na liście Control+C kopiuje link jako
Markdown, Control+Shift+C jako link z formatowaniem, a F2 otwiera okienko
edycji tytułu i adresu. Klawisz odebraliśmy systemowemu przełączaniu okien
w poprzedniej paczce właśnie pod tę funkcję, więc dopiero teraz miała ona
gdzie zamieszkać.

33.1. Control+F6 w dokumencie z linkami ma otworzyć okno „Links" i przeczytać
pierwszą pozycję. Każdy wiersz to numer wiersza w dokumencie, potem treść
linku, potem adres, a na końcu słowo „link" albo „image".

33.2. Enter na wybranej pozycji ma przenieść kursor do tego linku i przeczytać
go tak samo. Kursor ma stanąć na TREŚCI linku, nie na nawiasie kwadratowym -
czytnik ma powiedzieć słowa, nie znaki składni.

33.3. Escape ma zamknąć listę i nie ruszyć kursora z miejsca.

33.4. W dokumencie bez linków Control+F6 ma powiedzieć „No links" i NIE
otwierać pustego okna.

33.5. Control+C na pozycji listy ma skopiować sam link w postaci Markdown, czyli
dokładnie to, co stoi w pliku, i powiedzieć „Copied as Markdown". Sprawdź przez
wklejenie do dokumentu: ma się wkleić link, a nie widoczny wiersz listy razem
z numerem wiersza.

33.6. Control+Shift+C ma skopiować link z formatowaniem i powiedzieć „Copied as
formatted link". To wersja do wklejenia w Wordzie: ma tam powstać prawdziwy,
klikalny odsyłacz z Twoim tytułem, nie sam adres.

33.7. Control+Shift+C na linku wewnętrznym, czyli takim, który skacze do
nagłówka w tym samym dokumencie, ma odmówić i powiedzieć, że ten link nie ma
adresu internetowego do sformatowania. To celowe: taki link poza dokumentem
nigdzie nie prowadzi, więc wklejenie go do Worda dałoby odsyłacz w nikąd.

33.8. F2 na pozycji listy ma otworzyć okienko „Edit Link" z dwoma polami:
tekstem linku i adresem, oba wypełnione tym, co jest w dokumencie. Po zmianie
i zatwierdzeniu link w tekście ma się przepisać, a lista ma się otworzyć
ponownie z nową treścią.

33.9. Escape w okienku edycji ma nie zmienić niczego w dokumencie.

33.10. Control+Z po edycji ma cofnąć zmianę linku - to zwykła edycja tekstu,
nic nie jest zapisywane na dysk.

33.11. F2 na obrazku z wyczyszczonym opisem ma ODMÓWIĆ i powiedzieć, że obrazek
potrzebuje opisu dla czytnika ekranu. Obrazek bez opisu jest dla Ciebie
niewidzialny, więc nie pozwalamy go takim zrobić.

33.12. Lista ma pokazywać także gołe adresy wpisane wprost w tekst, czyli
zaczynające się od http, od www, albo wzięte w nawiasy ostre. Sprawdź, że
kropka kończąca zdanie NIE wchodzi do adresu.

33.13. Adres wpisany w blok kodu ma NIE trafić na listę - tam adres jest
przykładem, a nie odsyłaczem. To ta sama zasada, co przy przypisach,
komentarzach i podglądzie.

33.14. Lista działa też w zwykłym pliku .txt, nie tylko w .md, i to jest
świadoma różnica wobec wstawiania linku pod Control+K. Tamta komenda PISZE
składnię Markdown, która w pliku tekstowym byłaby śmieciem; ta tylko CZYTA,
a goły adres w .txt jest prawdziwym linkiem. Powiedz, gdybyś wolał, żeby lista
też była tylko dla plików Markdown.

33.15. W otwartym podglądzie Control+F6 ma powiedzieć „Close the preview first".
W podglądzie linki daje lista elementów pod F7, z własnym filtrem, a
przesunięcia z pliku źródłowego nie mają tam pokrycia.

33.16. KONTROLA, że nic nie zniknęło: F6 nadal otwiera drzewo nagłówków,
Shift+F6 nadal skacze po spisie treści, Control+K nadal wstawia link, Alt+K
nadal pokazuje listę przypisów, a Control+Alt+F9 listę komentarzy.

33.17. KONTROLA rodziny Control+F6: Control+Shift+F6 ma nadal NIC nie robić
(to on wywoływał ten „Accessible Selection"), a przełączanie okien ma dalej
działać na Control+Tab, z listą okien pod F4.

33.18. Czego u siebie NIE sprawdzę: jak Twój czytnik czyta długie wiersze tej
listy, gdy tytuł linku i adres są oba długie, i czy kolejność „treść, adres,
rodzaj" jest dla Ciebie wygodna, czy wolałbyś sam tytuł, a adres dopiero po
naciśnięciu strzałki. Sonda mierzy parser i binarkę, Twoje ucho rozstrzyga.


## 34. Zakładki z nazwą pod Control+Shift+B i Alt+Shift+B (5.0.56)

34.1. Ustaw kursor w dowolnym wierszu pliku i naciśnij Control+Shift+B. Ma
otworzyć się okno o tytule „Set Named Bookmark" z jednym polem „Bookmark name",
a w polu ma już być treść bieżącego wiersza jako propozycja nazwy. Enter
zatwierdza, program mówi „Named bookmark set".

34.2. Naciśnij Alt+Shift+B. Ma otworzyć się lista o tytule „Named Bookmarks", a
na niej pozycja w postaci „nazwa, line N". Treść przed współrzędną, tak jak
wszędzie w tym programie. Enter ma skoczyć do tego wiersza i przeczytać go cały.

34.3. Ustaw dwie kolejne zakładki z nazwą: jedną wyżej, jedną niżej w
dokumencie. Na liście mają być w kolejności WIERSZY, nie w kolejności, w której
je zapisałeś. Zaznaczona ma być pozycja najbliższa kursorowi.

34.4. Stań w wierszu, który JUŻ ma zakładkę z nazwą, i naciśnij Control+Shift+B
ponownie. Tytuł okna ma brzmieć „Rename Bookmark" (a nie „Set"), a w polu ma być
dotychczasowa nazwa. Po zmianie program mówi „Bookmark renamed", a na liście ma
być nadal JEDNA pozycja w tym wierszu.

34.5. W tym samym oknie wyczyść pole i zatwierdź. Program ma powiedzieć „Named
bookmark removed", a zakładka ma zniknąć z listy. To ta sama zasada, którą mają
komentarze na Alt+F9.

34.6. Na NOWEJ zakładce wyczyść propozycję nazwy i zatwierdź. Program ma
powiedzieć „No name, no bookmark set!" i NIC nie zapisać. Zakładka bez nazwy nie
różniłaby się niczym od zwykłej, a pusta pozycja na liście jest dla czytnika
bezużyteczna.

34.7. Na liście Alt+Shift+B naciśnij Delete na wybranej pozycji. Ma zniknąć od
razu, bez wychodzenia z okna, z komunikatem „Named bookmark removed". Po
usunięciu ostatniej pozycji okno ma się zamknąć z „No named bookmark!".

34.8. NAJWAŻNIEJSZY PUNKT, bo to jest to, o co pytałeś: zakładki z nazwą to
DRUGA, OSOBNA lista. Ustaw w tym samym pliku zakładkę zwykłą (Control+B) i
zakładkę z nazwą (Control+Shift+B). Alt+B ma pokazać TYLKO zwykłą, Alt+Shift+B
TYLKO nazwaną. Żadne okno nie ma pytać, którego rodzaju chcesz.

34.9. KONTROLA MAGAZYNU, która dotyczy Twojego pytania o ulubione: zdejmij plik
z ulubionych (Alt+Shift+L w pliku, który jest ulubiony). Zakładki ZWYKŁE znikną,
bo dzielą sekcję z ulubionymi i program mówi o tym wprost. Zakładki Z NAZWĄ mają
ZOSTAĆ, bo trzymamy je w osobnej sekcji pliku ustawień.

34.10. Zamknij i otwórz program ponownie. Zakładki z nazwą mają być nadal na
liście, bo są zapisywane na dysk od razu po naciśnięciu klawisza.

34.11. Dopisz akapit WYŻEJ niż zakładka z nazwą i sprawdź listę. Numer wiersza
się przesunie, bo tekst się przesunął, ale dopisanie słowa W ŚRODKU akapitu nie
ma jej ruszać. Trzymamy numer wiersza, nie pozycję znaku, bo to odporniejsze na
edycję.

34.12. KONTROLA, że nic nie znikło: Control+B nadal wstawia zakładkę zwykłą,
Alt+B nadal pokazuje jej listę, Shift+PageDown i Shift+PageUp nadal chodzą po
zakładkach zwykłych, a Control+Shift+Backspace nadal usuwa do początku wiersza.
Ten ostatni punkt nie jest kaprysem: zapis „Control+Shift+B" jest częścią zapisu
„Control+Shift+Backspace" i to pomyliło naszą pierwszą sondę.

34.13. Czego u siebie NIE sprawdzę: jak Twój czytnik czyta wiersz listy „nazwa,
line N" przy długiej nazwie, i czy propozycja nazwy z treści wiersza jest dla
Ciebie wygodna, czy wolisz puste pole. Sonda mierzy magazyn i binarkę, Twoje
ucho rozstrzyga.

## 35. Twoje wyniki z 5.0.56 i 5.0.57 oraz rodzina Control (5.0.58)

Ten rozdzial jest odpowiedzia na trzy Twoje wiadomosci z 1 wrzesnia: wyniki
testow zakladek i listy linkow, decyzje o nawigacji przypisow, oraz plik
"Do usunięcia.txt" z literami pod Control.

35.1. Otworz liste linkow pod Control+F6. Kazdy wiersz ma sie teraz zaczynac od
TRESCI odsylacza, a numer wiersza ma byc na KONCU, po przecinku. Zglaszales, ze
"dalej mowi Line, Line" - to byl skutek kolejnosci, bo pozycja zaczynala sie
slowem Line.

35.2. Na tej samej liscie sprobuj nawigacji pierwsza litera: napisz litere, ktora
zaczyna sie tytul jednego z odsylaczy. Ma przeskoczyc na ten odsylacz. Wczesniej
kazda pozycja zaczynala sie ta sama litera L, wiec nawigacja literami byla
martwa.

35.3. Otworz liste zakladek (Alt+B) i posluchaj, co program mowi PRZY OTWARCIU.
NIE ma juz byc slychac "Delete removes the bookmark". Ma byc od razu tresc
wiersza z zakladka.

35.4. Na tej liscie nacisnij Shift+F1. Teraz ma przeczytac klawisze, w tym
Delete. Ta sama informacja jest pod F1 w okienku pomocy. Chodzilo o to, zeby
podpowiedz byla NA ZADANIE, a nie za kazdym otwarciem.

35.5. KONTROLA, ze nic nie znikło: na liscie zakladek nacisnij Delete. Zakladka
ma zniknac tak jak dotad i ma byc slychac "Bookmark removed".

35.6. Na liscie zakladek nacisnij STRZALKE W LEWO. Ma powiedziec numer wiersza,
z ktorego pochodzi zakladka. Na tej liscie widzisz tresc, wiec brakujaca
informacja jest wspolrzedna.

35.7. Na liscie zakladek Z NAZWA (Alt+Shift+B) nacisnij STRZALKE W LEWO. Tu ma
byc odwrotnie: ma przeczytac TRESC wiersza, na ktorym stoi zakladka, bo nazwe i
numer wiersza juz slyszysz w pozycji listy. O to prosiles wprost.

35.8. Na liscie przypisow (Alt+K) nacisnij STRZALKE W LEWO. Ma przeczytac wiersz
tekstu, w ktorym stoi znacznik tego przypisu, BEZ samego znacznika. Tresc
przypisu slyszysz w pozycji listy, wiec brakuje zdania, do ktorego przypis
nalezy.

35.9. NAWIGACJA PRZYPISOW ROZDZIELONA, tak jak zdecydowales. Stan w zdaniu ze
znacznikiem i nacisnij Control+Alt+K. Ma zabrac do tresci przypisu. Nacisnij
znowu: ma wrocic do zdania. I NIC WIECEJ - ten klawisz nie skacze juz na
kolejny przypis.

35.10. Stan w miejscu, gdzie NIE MA zadnego przypisu, i nacisnij Control+Alt+K.
Ma powiedziec, gdzie postawic kursor, i wymienic Control+Alt+PageDown. Kursor
ma STAC.

35.11. Nacisnij Control+Alt+PageDown. Ma przejsc do nastepnego znacznika
przypisu w tekscie. Control+Alt+PageUp ma isc do poprzedniego. Na ostatnim i
pierwszym ma powiedziec "Last footnote!" albo "First footnote!" i NIE zawijac
na drugi koniec dokumentu.

35.12. KONTROLA, ze nowe klawisze nikomu nic nie zabraly: Shift+PageUp i
Shift+PageDown maja nadal chodzic po zakladkach, a Control+PageUp i
Control+PageDown po sekcjach. Control+Alt+Shift+K, ktory w 5.0.57 szedl do
poprzedniego przypisu, jest teraz WOLNY i ma nie robic nic.

35.13. Control+Y ma PONAWIAC, o co prosiles pierwszym zdaniem tamtego pliku.
Cofnij zmiane przez Control+Z, potem nacisnij Control+Y: zmiana ma wrocic, tak
samo jak po Control+Shift+Z. Oba klawisze maja mowic to samo.

35.14. Repeat Line, ktore siedzialo na Control+Y, jest teraz w menu Misc BEZ
skrotu. Sprawdz, ze pozycja tam JEST i dziala. Nie usunalem jej, bo roznica
wobec Control+C jest realna: Control+C kopiuje wiersz do schowka, a Repeat Line
pisze jego kopie od razu w dokumencie.

35.15. Cztery pozostale komendy zwolnily klawisze, ale ZOSTALY w menu Misc:
Environment Variables (bylo Control+E), Extract with Regular Expression (bylo
Control+Shift+E), Go to Environment (bylo Control+Shift+G) i Extra Speech
Toggle (bylo Control+Shift+X). Sprawdz, ze zadna z nich nie odpowiada juz na
swoj dawny skrot, i ze kazda da sie wywolac z menu.

35.16. Yield with Regular Expression ZOSTAJE na Control+Shift+Y, bo napisales
"To chyba warto". Sprawdz, ze klawisz dziala.

35.17. Control+U i Control+Shift+U (wielkie i male litery) oraz Control+I i
Control+Shift+I (skok po wcieciach) NIE ZOSTALY ruszone. Przy pierwszej parze
napisales "To chyba bysmy zostawili?", a przy drugiej "Nie wiem" - to pytanie,
nie decyzja, wiec nie zgaduje. Odpowiedz na nie jest w opisie paczki.

35.18. Czego u siebie NIE sprawdze: jak Twoj czytnik czyta nowe wiersze listy
linkow, czy Shift+F1 na listach zakladek mowi u Ciebie klawisze, i czy
Control+Alt+PageUp oraz PageDown nie sa u Ciebie przechwycone przez cos poza
EdSharpem. Klawisze nawigacyjne z Control+Alt sa na polskim ukladzie bezpieczne
(prawy Alt robi diakrytyki tylko na literach), ale to Twoja maszyna
rozstrzyga.

## 36. Wersja 5.0.59: domyslne rozszerzenie md i poczta, ktora przestaje milczec

36.1. Nacisnij Control+N, napisz cokolwiek i nacisnij Control+S. W okienku
zapisu NIE dopisuj rozszerzenia, podaj sama nazwe. Oczekiwane: plik zapisuje
sie jako .md, nie .rtf. To byla przyczyna szerszego klopotu niz sama nazwa:
caly Markdown (spis tresci, przypisy, komentarze, listy, wstawianie odsylacza)
ma bramke na plik .md, wiec w nowym dokumencie te komendy ODMAWIALY dzialania.

36.2. W tym samym, swiezo zapisanym pliku sprawdz Alt+Shift+T (spis tresci)
i Control+Shift+K (przypis). Oczekiwane: dzialaja, bo plik jest juz .md.
Przedtem mowily "works only on Markdown files".

36.3. Zajrzyj w ustawienia (Control+przecinek) i znajdz ExtensionDefault.
Oczekiwane: md. Jesli KIEDYS ustawiles tam wlasne rozszerzenie inne niz rtf
(cs, py, txt), MA ZOSTAC Twoje - migracja rusza wylacznie wartosc rtf i tylko
raz. Gdyby przestawila Ci wlasny wybor, to blad i chce o nim wiedziec.

36.4. Ustaw ExtensionDefault recznie na rtf, zamknij program i uruchom go
ponownie. Oczekiwane: zostaje rtf. Migracja jest JEDNORAZOWA, wiec po niej
Twoja decyzja jest wazniejsza od naszej domyslnej wartosci.

36.5. Otworz zapisany plik, zmien w nim cos i NIE zapisuj. Nacisnij
Control+Shift+M (poczta z zalacznikiem). Oczekiwane: program pyta, czy zapisac
zmiany, bo zalacznik bierze plik Z DYSKU. Przedtem po cichu wysylal wersje bez
Twoich zmian. To odpowiedz na Twoje "jedna funkcja dziala, druga nie": tresc
listu (Control+M) bierze tekst Z OKNA, a zalacznik plik z dysku.

36.6. Nacisnij Control+Shift+M i sprobuj doprowadzic do bledu, na przyklad
anulujac okno klienta poczty. Oczekiwane: program MOWI, ze klient odmowil, i
proponuje wyslanie tresci jako zwyklej wiadomosci. Przedtem w tym miejscu byl
pusty catch, czyli komenda nie mowila NIC i nie mialo jak byc wiadomo, czy list
poszedl.

36.7. Przy udanym wyslaniu zalacznika program mowi teraz, ze przekazal list
klientowi poczty. Przedtem milczal takze przy powodzeniu.

36.8. Control+M i Control+Shift+M ZOSTAJA na swoich klawiszach, bo napisales
"tu akurat bym oba zostawil". Zadnego skrotu w tej wersji nie ruszalem.

36.9. Czego u siebie NIE sprawdze: czy Twoj klient poczty faktycznie przyjmie
list z zalacznikiem, i co powie NVDA na nowe komunikaty. Na tej maszynie nie ma
skonfigurowanego klienta poczty MAPI, wiec droge udana widze tylko w kodzie.

## 37. Wersja 5.0.60: wklejanie w EdSharpie odzyskuje skladnie, schowek nie wywraca programu, instalator prostszy

37.1. Otworz plik .md, napisz odsylacz w skladni Markdown, na przyklad
[Strona NVDA](https://www.nvaccess.org/). Ustaw kursor na tym odsylaczu i
nacisnij Control+Shift+C. Potem przejdz w inne miejsce tego samego dokumentu i
nacisnij Control+V. Oczekiwane: wklejony CALY odsylacz ze skladnia i adresem,
program mowi "Markdown pasted". Przedtem wklejal sam tytul, bez adresu - to jest
dokladnie to, co zglosiles.

37.2. To samo, ale wklej do WORDA (albo LibreOffice) zamiast do EdSharpa.
Oczekiwane: BEZ ZMIAN wobec 5.0.59, czyli klikalny odsylacz z tytulem jako
tekstem. Ta czesc nie miala sie zmienic i o to prosi ten punkt: gdyby Word
zaczal wklejac surowa skladnie z nawiasami, byloby to zepsucie tego, co dzialalo.

37.3. Zaznacz kilka wierszy z naglowkiem i lista, nacisnij Control+Shift+C,
potem Control+V w innym miejscu dokumentu. Oczekiwane: wraca skladnia Markdown,
czyli krzyzyki naglowka i punktory listy, a nie tekst bez znacznikow.

37.4. Ta sama para klawiszy na liscie odsylaczy (Control+F6, potem
Control+Shift+C na pozycji) i Control+V w dokumencie. Oczekiwane: pelna skladnia
odsylacza.

37.5. Skopiuj cokolwiek z INNEGO programu (na przyklad z przegladarki) i
nacisnij Control+V w EdSharpie. Oczekiwane: zwykle wklejanie dziala jak zawsze.
Ten punkt pilnuje, ze nowa droga nie przechwytuje obcego schowka.

37.6. W pliku .rtf skopiuj fragment i wklej go w innym miejscu. Oczekiwane:
wklejenie zachowuje sie jak dotad, bo w RTF formatowanie jest prawdziwe i
skladni Markdown tam nie wstawiamy.

37.7. WLACZ Ditto (albo inny menedzer schowka) i pokopiuj Control+Shift+C
kilkanascie razy pod rzad, tez szybko. Oczekiwane: albo kopiowanie dziala, albo
program MOWI "Clipboard is busy" i nazwe tego, czego nie skopiowal. Czego byc NIE
POWINNO: okna "Unexpected Event" ze sladem stosu. Zmierzona przyczyna tego okna:
zapis do schowka przy schowku zajetym przez inny proces rzucal wyjatek
0x800401d0, a siedem naszych sciezek kopiowania nie mialo zadnej oslony.

37.8. Zainstaluj te wersje i dojdz do ostatniej strony instalatora. Oczekiwane:
strona konczaca jest PUSTA, bez pol wyboru o skryptach JAWS i dodatkach NVDA -
o to prosiles. Pliki zostaja w katalogu programu, wiec gdybys kiedys chcial
zainstalowac dodatek NVDA, otwierasz EdSharpNG-spellcheck.nvda-addon z katalogu
programu, a skrypty JAWS wgrywa polecenie EdSharpNG.exe --install-jaws-settings.

37.9. Sprawdz Control+F1 na Control+V. Oczekiwane: opis wspomina, ze tekst
skopiowany z formatowaniem wewnatrz EdSharpNG wraca jako Markdown.

37.10. Czego u siebie NIE sprawdze: jak Twoj NVDA przeczyta komunikat
"Markdown pasted" i komunikaty o zajetym schowku, oraz czy przy Twoim Ditto
kolizja mija w oknie dziesieciu ponowien. Sprawdzilem, ze blokada trwajaca
cwierc sekundy zostaje przeczekana.

## 38. Wersja 5.0.61: plik .rtf przestaje gubic tresc, lista eksportu rozroznia pozycje

38.1. SPROSTOWANIE DO TWOJEJ WIADOMOSCI, przeczytaj przed testami. Napisales:
"plik rtf jest i tak przetwarzany na markdown". Zmierzone w kodzie: NIE BYL.
Otwarcie pliku .rtf przez Control+O pytalo "Treat as rich text?" i przy
odpowiedzi TAK pokazywalo dokument jako tekst bogaty, a przy NIE jako surowe
znaczniki. Zadna z tych drog nie szla przez pandoca ani przez Markdown - do
tekstu RTF konwertowal WORD przez COM, wiec bez zainstalowanego Worda ta droga
w ogole nie dzialala. Od tej wersji jest to, o czym myslales: patrz 38.5.

38.2. Otworz jakis plik .rtf przez Control+O i odpowiedz NIE na pytanie o tekst
bogaty (czyli zobacz surowe znaczniki). Zmien cokolwiek i nacisnij Control+S.
Oczekiwane: program MOWI "Saving as plain text, not rich text", a plik zapisuje
sie tak, jak go widzisz. Co bylo PRZED ta wersja: zapis przepuszczal tresc przez
zapis bogaty, ktory ESCAPOWAL kazdy znacznik - plik po ponownym otwarciu
pokazywal znaczniki zamiast dokumentu i tracil formatowanie bezpowrotnie.
To odpowiedz na Twoje pytanie "zapisze ctrl+s, to co on wtedy tez sie zapisze w
tym rtfie?".

38.3. Otworz ten sam plik .rtf i odpowiedz TAK na pytanie o tekst bogaty. Zmien
cokolwiek, Control+S. Oczekiwane: zapisuje sie jak dotad, jako prawdziwy RTF, i
komunikatu o tekscie NIE MA. To jest kontrola, ze poprawka nie zabrala niczego
plikom naprawde bogatym.

38.4. Nacisnij Control+N, wklej cokolwiek i zapisz jako plik z rozszerzeniem
.rtf. Oczekiwane: program mowi, ze zapisuje tekstem, i plik jest tekstem. Twoje
pytanie brzmialo, czy nowy dokument powinien byc rtf-em - sam odpowiedziales, ze
nie, i tak zostalo: domyslne rozszerzenie to nadal md.

38.5. Otworz plik .rtf z Menedzera plikow Windows albo z listy ostatnio uzywanych
(nie przez Control+O). Oczekiwane: pojawia sie lista formatow importu, a na niej
pozycja "md". Wybierz ja. Oczekiwane: dokument otwiera sie jako Markdown -
naglowki jako gwiazdki albo kratki, odsylacze w nawiasach, listy z punktorami.
To jest ta droga, o ktorej pisales, i idzie przez pandoca z naszej paczki, nie
przez Worda.

38.6. Na tej samej liscie formatow zobacz pozycje "rtf" i "txt". Oczekiwane: sa
rozne i nie powtarzaja sie.

38.7. W pliku .md nacisnij Alt+Shift+E (Export Format) i przejdz strzalkami cala
liste. Oczekiwane: nie ma DWOCH pozycji o tej samej nazwie. Tam, gdzie dawniej
byly dwie pozycje "rtf" i dwie "htm", stoi teraz "rtf (converted)" i
"rtf (as shown)" oraz to samo dla "htm". Co to znaczy: "(converted)" przepuszcza
dokument przez pandoca, czyli daje prawdziwy plik RTF z formatowaniem;
"(as shown)" zapisuje to, co widzisz w oknie. Przed ta wersja obie pozycje
nazywaly sie identycznie, wiec czytnik czytal je tak samo, a robily rozne rzeczy.

38.8. Sprawdz Alt+Shift+S (Save Copy) na dowolnym pliku i podaj nazwe kopii z
rozszerzeniem .rtf. Oczekiwane: zachowuje sie jak zwykly zapis z punktu 38.2
albo 38.3, zaleznie od tego, czym dokument jest.

38.9. Czego u siebie NIE sprawdze: jak Twoj NVDA przeczyta komunikat "Saving as
plain text, not rich text" oraz nazwy pozycji z przyrostkami "(converted)" i
"(as shown)" - u mnie nie ma czytnika. Nie sprawdze tez, jak pandoc poradzi
sobie z Twoimi prawdziwymi plikami .rtf z Worda; na probce z odsylaczem,
pogrubieniem, pochyleniem, naglowkami, lista i polskimi znakami konwersja
oddala wszystko poprawnie.

## 39. Wersja 5.0.62: skok po odsylaczach i komentarzach, lista zakladek nie ucieka

39.1. Otworz plik .md, w ktorym jest kilka odsylaczy, i nacisnij Alt+PageDown.
Oczekiwane: kursor staje na TRESCI odsylacza (nie na nawiasie kwadratowym), a
czytnik mowi tresc, potem adres, potem slowo "link" albo "image". Naciskaj dalej.
Oczekiwane: przechodzisz po kolei po wszystkich odsylaczach dokumentu.

39.2. Dojedz Alt+PageDown do ostatniego odsylacza i nacisnij jeszcze raz.
Oczekiwane: program mowi "Last link!" i kursor NIE wraca na poczatek dokumentu.
Alt+PageUp analogicznie w drugą strone, z komunikatem "First link!". Skok nie
zawija sie swiadomie: nie widzac tego, szukalbys odsylacza, ktory juz czytales.

39.3. Sprawdz, czy skok i lista pod Control+F6 pokazuja TE SAME odsylacze.
Oczekiwane: tak, bo obie komendy czytaja jeden parser. Jesli zobaczysz roznice,
to jest blad i chcialbym o nim wiedziec.

39.4. Nacisnij Alt+PageDown w pliku .txt, w ktorym jest goly adres (na przyklad
http albo www). Oczekiwane: skok DZIALA, bo goly adres w zwyklym pliku jest
prawdziwym odsylaczem. To swiadoma roznica wobec Control+K, ktory pisze skladnie
Markdown i dziala tylko w plikach .md.

39.5. Otworz podglad Markdown (Escape) i nacisnij Alt+PageDown. Oczekiwane:
program mowi "Close the preview first!" i nic sie nie rusza. W podgladzie kursor
chodzi po tekscie przetworzonym, wiec przesuniecia z pliku zrodlowego nie maja
tam pokrycia.

39.6. KOMENTARZE ZMIENILY KLAWISZE, zgodnie z Twoja decyzja z 30.08: skok do
nastepnego komentarza jest teraz na Alt+Shift+PageDown, a do poprzedniego na
Alt+Shift+PageUp. Sprawdz oba. Oczekiwane: dzialaja jak dotad, tylko z innych
klawiszy.

39.7. Sprawdz, ze STARE klawisze komentarzy nic juz nie robia:
Control+Shift+F9 i Alt+Shift+F9. Oczekiwane: cisza, zadnej komendy.

39.8. Wstawianie komentarza ZOSTAJE na Alt+F9, a lista komentarzy na
Control+Alt+F9 - te dwie rzeczy nie sa skokami, wiec zostaly w rodzinie F9.
Sprawdz, czy nadal dzialaja.

39.9. Sprawdz, ze goly F9 i Shift+F9 nadal naleza do skryptu JAWS (czytanie do
konca). Tych dwoch nie ruszamy bez Twojego slowa.

39.10. TWOJE ZGLOSZENIE O LISCIE ZAKLADEK. Ustaw kilka zakladek w pliku, otworz
liste (Alt+B) i usuwaj je Delete jedna po drugiej, do konca, szybko. Oczekiwane:
po usunieciu OSTATNIEJ zakladki okno listy ZOSTAJE otwarte i puste, a czytnik
mowi "No bookmarks, press Escape to close the list". Wychodzisz Escape. Przed ta
wersja okno zamykalo sie samo i fokus wracal do TEKSTU dokumentu, wiec kolejne
nacisniecie Delete kasowalo znak w dokumencie zamiast zakladki.

39.11. To samo na liscie zakladek Z NAZWA (Alt+Shift+B). Oczekiwane: identyczne
zachowanie. Zglosiles jedna liste, ale wada byla w obu, wiec naprawione sa obie.

39.12. Sprawdz, ze usuwanie zakladki przy NIEPUSTEJ liscie dziala jak dotad:
pozycja znika, program mowi "Bookmark removed", a fokus zostaje na liscie.

39.13. Czego u siebie NIE sprawdze: jak Twoj NVDA przeczyta komunikat pustej
listy i czy wystarcza on, zeby sie zorientowac, ze lista jest juz pusta - u mnie
nie ma czytnika. Nie sprawdze tez, czy Alt+PageDown i Alt+PageUp nie koliduja z
niczym na Twoim ukladzie klawiatury; u mnie zaden inny skrot programu ich nie
uzywa, co zmierzylem na pelnej mapie klawiszy.

## 40. Zwolniony klawisz F9 (5.0.63)

Twoja decyzja z 03.09.2026: "z tego klawisza F dziewiec i skryptu raczej
rezygnujemy i tak nie bede go testowac z programem". Obsluga zeszla z programu.

Co tam bylo, zeby bylo jasne, czego juz nie ma: goly F9 zrzucal tekst od kursora
do konca dokumentu do pliku tymczasowego i wolal funkcje skryptu JAWS, ktora ten
tekst czytala; Shift+F9 stawial potem kursor tam, gdzie mowa ucichla. Bez
zainstalowanych skryptow JAWS oba klawisze nie robily NIC, i to jest wlasnie
powod, dla ktorego lepiej je zwolnic, niz zostawic: cichy klawisz jest gorszy od
wolnego, bo przy wolnym slyszysz, ze nic sie nie stalo.

40.1. Nacisnij goly F9 w otwartym pliku. Oczekiwane: nic sie nie dzieje, zadnego
komunikatu ani ruchu kursora. Tak samo Shift+F9.

40.2. KONTROLA, ze nie zabralem czytania calego tekstu. Nacisnij Alt+F8.
Oczekiwane: program czyta caly tekst dokumentu wlasna mowa, bez ruszania
kursora. Ta komenda nie ma z JAWS-em nic wspolnego i dziala tez pod NVDA -
zostaje nietknieta.

40.3. KONTROLA rodziny F9, ktorej nie ruszalem: Alt+F9 wstawia i poprawia
komentarz, Control+Alt+F9 pokazuje ich liste, Control+F9 mowi kompilator i
katalog. Wszystkie trzy jak dotad.

40.4. Czego u siebie NIE sprawdze: samego nacisniecia klawisza na zywym
programie z czytnikiem - u mnie nie ma ani NVDA, ani JAWS-a. Zmierzylem, ze
obsluga zniknela ze zbudowanej binarki (nie tylko ze zrodla), a wersja sprzed
zmiany ta sama sonda daje wynik inny. Twoje nacisniecie rozstrzyga.
