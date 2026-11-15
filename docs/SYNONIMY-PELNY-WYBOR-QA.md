# Synonimy — jak testować pełny wybór i odmiany (prywatne 5.0.123)

Status: instrukcja retestu prywatnego przyrostu; ten dokument jest kontraktem,
nie listą zaliczonych prób. Rzeczywiste wyniki i granice pokrycia opisuje
`SYNONIMY-ODBIOR-5.0.123.md`.
Publiczne 5.0.120 i główna instalacja nie są zmieniane.

## Przygotowanie

Użyć prywatnego katalogu i profilu; obok EXE musi być cały katalog `Synonyms`.
NVDA pozostawić uruchomiony. Wyniki mowy i stan dokumentu zapisać oddzielnie.
Nie odczytywać ani nie przenosić ustawień głównego profilu użytkownika.

## Klawiatura i znaczenie

1. W dokumencie wpisać `dom`, zaznaczyć, nacisnąć Shift+F7.
   Fokus: pierwszy synonim; odczyt najpierw wyrazu, następnie części mowy i pozycji.
2. Wybrać inną pozycję strzałką w dół. Nacisnąć prawą strzałkę.
   Otwiera się tekst tylko do odczytu z definicją/przykładem/źródłem lub jawnym
   brakiem danych. To dokładne znaczenie wybranej pozycji, nie lookup samego napisu.
3. W tekście szczegółów prawa/lewa strzałka zachowują zwykłe czytanie tekstu.
   Ctrl+C może kopiować. Enter w polu nie wstawia nic do dokumentu.
4. Escape: wraca do tej samej pozycji, z fokusem na liście.
   F4 jako zgodny wstecznie alias otwiera te same szczegóły.
5. F1: pomoc wymienia prawą strzałkę, Shift+Enter, Enter, Escape oraz przyciski.
   Escape z pomocy nie zamyka listy synonimów.
6. Dotabować do Znaczenie i Odmiany: działają tak samo jak ich klawisze.

## Wybór odmiany

1. Na polskim synonimie mającym dane nacisnąć Shift+Enter.
   Nie może od razu wykonać domyślnego przycisku Wstaw.
2. Otwiera się lista form dokładnie wybranego słowa. NVDA czyta formę i czytelny
   opis gramatyczny, bez surowych tagów ani technicznych identyfikatorów.
3. Wybrać inną formę; Enter wstawia ją w pierwotny zakres dokumentu i zamyka
   także główną listę synonimów. Zachowane wielkie litery. Ctrl+Z cofa zamianę.
4. Ponownie otworzyć i anulować Escape: powrót do tego samego synonimu;
   następny Escape wraca do niezmienionego dokumentu.
5. Powtórzyć dla już odmienionego wejścia, np. `domami`, oraz słów
   wieloznacznych. Formy obcej części mowy i znaczenia nie mogą przeciekać.
6. Dla OMW bez potwierdzonego mapowania: jawny brak form, bez pożyczania
   odmiany plWordNet przez identyczny napis.
7. Dla angielskiego: dostępne są poświadczone wyjątki WordNet, nie pełna
   regularna gramatyka. Brak danych nie może tworzyć `goed` lub podobnych form.
8. Brak/uszkodzenie zasobu odmian: jawny komunikat; po Escape nadal działa
   zwykłe Enter na liście synonimów. Przywrócić tylko własny zasób QA i SHA.

## Opcje

1. Ctrl+przecinek: pole Słownik synonimów zawiera Polski / Angielski / Oba.
2. Zmiana i OK: następne Shift+F7 korzysta z wybranego słownika bez restartu.
3. Zmiana i Escape: wcześniejszy wybór pozostaje. Ponowne otwarcie i restart
   prywatnego profilu pokazują zapisany stan.
4. Misc → Thesaurus Dictionary oraz paleta otwierają to samo okno Opcji,
   od razu na tym polu; nie ma drugiego, niezależnego mechanizmu wyboru.
5. W trybie Oba wyniki wskazują język i nie łączą polskich i angielskich znaczeń.

## Odbiór

W raporcie odnotować osobno testy danych, prawdziwy build, paczkę,
żywy NVDA, odczyt mowy i granice pokrycia. Nie oznaczać planu powyżej jako
zaliczonego na podstawie samych testów sprawdzających tekst źródłowy.
