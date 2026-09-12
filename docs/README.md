# Dokumentacja EdSharpNG

Katalog `docs/` zbiera wiedzę o tym projekcie w jednym miejscu, w repozytorium,
żeby przetrwała niezależnie od czyjejś pamięci i niezależnie od maszyny, na
której akurat trwa praca.

## Co tu leży

| Plik | Co zawiera | Kto pisze |
|---|---|---|
| [ARCHITEKTURA.md](ARCHITEKTURA.md) | Budowa programu: pliki źródłowe, co za co odpowiada, jak to się kompiluje | ręcznie |
| [DECYZJE.md](DECYZJE.md) | Rozstrzygnięcia projektowe wraz z uzasadnieniem i datą | ręcznie |
| [BUDOWANIE.md](BUDOWANIE.md) | Jak zbudować program i wydać wersję, z pułapkami | ręcznie |
| [POMIARY.md](POMIARY.md) | Zasady testowania i spis testów automatycznych | ręcznie |
| [PAMIEC-PROJEKTU.md](PAMIEC-PROJEKTU.md) | **Materiał źródłowy**: surowy zapis ustaleń z całej pracy | automat, nie edytować |

Dalej w `docs/`, materiał roboczy:

- `MAPA-DROGOWA.md` — kolejność prac ustalona przez właściciela projektu,
- `ZADANIA-2026-09.md` — lista zadań z września 2026 wraz ze stanem,
- `DZIENNIK-TECHNICZNY.md` — sumy kontrolne, ścieżki, przyczyny błędów wydanie
  po wydaniu.

W katalogu głównym:

- `EdSharp.md` — podręcznik użytkownika, angielski (odziedziczony po Jamalu
  Mazrui, rozszerzany o nasze funkcje),
- `Tutorial.md` — samouczek dla początkujących, angielski,
- `Developer.md` — uwagi autora oryginału o stylu kodu,
- `BUILD_NOTES.md` — dziennik zmian z czasów wersji 5.0 baseline.

## W jakim języku co jest pisane

Docelowo oba języki. Podział nie jest przypadkowy i wynika z tego, kto co czyta:

| Materiał | Język | Dlaczego |
|---|---|---|
| Dokumentacja techniczna w `docs/` | polski | Powstaje w polskiej rozmowie i opisuje decyzje uzasadniane po polsku. Tłumaczenie jej na angielski gubiłoby niuanse, na których te decyzje stoją |
| Podręcznik `EdSharp.md`, `Tutorial.md` | angielski | Interfejs programu jest angielski, więc podręcznik musi cytować dokładnie te słowa, które użytkownik usłyszy z czytnika. Poza tym to materiał odziedziczony po autorze oryginału |
| Komentarze w kodzie | polski w plikach dodanych, angielski w odziedziczonych | Komentarz stoi obok kodu, który opisuje; przepisywanie cudzych komentarzy na polski niszczyłoby porównywalność z oryginałem |
| Nazwy poleceń, menu, komunikaty programu | angielski | Do czasu pełnego tłumaczenia interfejsu — mieszanie języków w jednym oknie jest gorsze niż konsekwentnie jedno (decyzja właściciela, 18.08.2026) |

**Kiedy powstanie polska wersja interfejsu** (ostatni punkt mapy drogowej),
podręcznik dostanie polskie tłumaczenie jako `EdSharp-pl.md` — osobny plik, nie
wersja mieszana. Dokumentacja techniczna dostanie wtedy skrót angielski
(`docs/en/`), żeby ktoś z zewnątrz mógł się w projekcie odnaleźć.

Do tego czasu obowiązuje zasada: **jeden plik, jeden język.** Akapit polski
wstawiony w angielski tekst zmusza czytnik ekranu do czytania polskich słów
angielską wymową i odwrotnie — to nie kwestia estetyki, tylko zrozumiałości.

## Skąd bierze się PAMIEC-PROJEKTU.md

Podczas pracy nad projektem ustalenia trafiają do pamięci wektorowej
(HyperspaceDB) na maszynie roboczej. Ta pamięć żyje w kontenerze Dockera na
jednym komputerze — czyli dokładnie tak długo, jak długo stoi ten komputer.

Skrypt `~/.hermes/scripts/eksport_pamieci_edsharp.py` przenosi z niej **dosłowną
treść** wszystkich wpisów dotyczących EdSharpa do `docs/PAMIEC-PROJEKTU.md`.
Dosłowną, nie streszczoną — streszczenie na tym etapie byłoby utratą materiału,
z którego dopiero ma powstać dokumentacja i podręcznik.

Uruchomienie:

```bash
~/.hermes/hermes-agent/venv/bin/python ~/.hermes/scripts/eksport_pamieci_edsharp.py
```

Plik jest nadpisywany w całości. Nie wpisuj do niego niczego ręcznie.

## Jak z tego zrobić dokumentację i podręcznik

Kolejność ma znaczenie — materiał źródłowy najpierw, wnioski potem:

1. `PAMIEC-PROJEKTU.md` to zapis surowy: ustalenia, pomiary, pomyłki i ich
   przyczyny, w kolejności, w jakiej powstawały. Zawiera też wpisy oznaczone
   jako **NIEAKTUALNE** — zostają, bo pokazują, co się zmieniło i dlaczego.
2. Z niego wyciągane są rzeczy trwałe do `DECYZJE.md`, `ARCHITEKTURA.md`
   i `POMIARY.md` — już redagowane, bez kroniki zdarzeń.
3. Podręcznik użytkownika (`EdSharp.md`) opisuje wyłącznie to, co widzi
   i robi człowiek przy programie. Żadnych nazw plików źródłowych, żadnych
   numerów wersji w treści objaśnień.

Rozdzielenie tych trzech poziomów jest celowe. Wcześniejsze próby trzymania
wszystkiego w jednym pliku kończyły się tym, że instrukcja dla użytkownika
tonęła w szczegółach kompilacji.

## Zasady pisania

- **Uzasadnienie jest ważniejsze od samego ustalenia.** Bez „dlaczego" następna
  osoba podważy decyzję i przejdzie tę samą drogę od zera.
- **Podawaj, co zostało zmierzone, a co jest założeniem.** Zdanie „to działa"
  bez wyniku uruchomienia jest hipotezą.
- **Bez tabel ASCII i bez rysunków ze znaków** tam, gdzie treść ma być czytana
  czytnikiem ekranu. Tabela Markdown jest w porządku, „ramka" z kresek nie.
- **Nazwy plików i ścieżki w osobnej linii**, nie wplecione w zdanie.
- **Bez kroniki zdarzeń** („najpierw zrobiłem A, potem B"). Wnioski, nie przebieg.
- **Data przy decyzji**, bo bez niej nie widać, która wersja ustalenia jest
  nowsza.
