# Morfologia do offline'owego indeksu EdSharpNG — wynik probe

## Werdykt

**Jako źródło produkcyjne wybierz `tbl_morphology` z plWordNet 5.0 razem z jego identyfikatorami `sense_id`; Morfeusz2 traktuj tylko jako opcjonalny, odtwarzalny generator/walidator build-time.** Dzięki temu alias `forma → sens` nie wymaga zgadywania, a uruchomiona aplikacja .NET ładuje wyłącznie własny indeks — bez Pythona, Javy, Worda i sieci.

To jest lepsze niż budowanie indeksu semantycznego przez dopasowanie lemma tekstowego z Morfeusza: Morfeusz nie zwraca identyfikatora sensu i dla `mam` rzeczywiście zwraca trzy różne analizy. Publikacja zespołu plWordNet opisuje właśnie połączenie form SGJP z jednostkami plWordNet oraz ręczne rozstrzyganie niejednoznacznych wzorców fleksji.[^plwn-morph]

Nie wybieram Morfologik/PoliMorf jako głównej bazy dla tej funkcji. PoliMorf jest prawnie wygodny (BSD-2), ale oficjalny opis uprzedza o słabiej dopracowanej informacji gramatycznej w części pochodzącej z Morfologik; nie ma też bezpośredniego klucza do sensu plWordNet.[^polimorf]

## Zweryfikowany Morfeusz2

Zainstalowano i uruchomiono wyłącznie w prywatnym venv:

- wheel: `/home/michal/.hermes/profiles/edsharp/cache/scratch/morfologia119/wheels/morfeusz2-1.99.15-20260601-cp310-abi3-manylinux_2_28_x86_64.whl`
  - 9 210 531 B; SHA-256 `089a83ab03a137a57e23d9d42028d80b8858d1a4de78f1db86a18ba504400a98`
- Python użyty w probe: 3.11.16. Wheel oznaczony `cp310-abi3` działa w tym Pythonie 3.11; bieżący Python 3.14 nie był do niego potrzebny.
- moduł API: `/home/michal/.hermes/profiles/edsharp/cache/scratch/morfologia119/venv/lib/python3.11/site-packages/morfeusz2.py`
- biblioteka z **wbudowanym** słownikiem SGJP: `/home/michal/.hermes/profiles/edsharp/cache/scratch/morfologia119/venv/lib/python3.11/site-packages/libmorfeusz2.so` (19 939 480 B).
  Nie ma osobnego pliku `.dict` w katalogu pakietu; `libmorfeusz2.so` zawiera identyfikator `pl.sgjp.sgjp-2026.06.01`.
- identyfikator tagsetu zwrócony przez API: `pl.sgjp.morfeusz-0.8.0`; liczba tagów: 65 536.

API Python zweryfikowane przez `inspect` i uruchomienie:

```python
import morfeusz2
m = morfeusz2.Morfeusz(praet="composite")
analiza = m.analyse("sprawdziłem")  # (startNode, endNode, (orth, lemma, tag, names, labels))
resolver = m._morfeusz_obj.getIdResolver()
tag_id = resolver.getTagId("praet:sg:m1.m2.m3:pri:perf")
formy = m.generate("sprawdzić", tag_id)
```

Sygnatury z zainstalowanego pakietu: `Morfeusz(dict_name=None, dict_path=None, analyse=True, generate=True, expand_dag=False, expand_tags=False, expand_dot=True, expand_underscore=True, aggl=None, praet=None, separate_numbering=True, case_handling=100, whitespace=301)`, `analyse(text)`, `generate(lemma, tag_id=None)`. Oficjalna dokumentacja potwierdza ten model: analiza zwraca DAG interpretacji, generator przyjmuje lemat i opcjonalny identyfikator tagu, a `praet="composite"` scala formy czasu przeszłego z końcówką osobową.[^morfeusz-doc]

### Wyniki wymaganych przykładów

| Tekst / generowanie | Wynik rzeczywiście zwrócony |
|---|---|
| `ważny` | lemma `ważny`; `adj:sg:acc:m3:pos` **oraz** `adj:sg:nom.voc:m1.m2.m3:pos` |
| `ważnego` | lemma `ważny`; `adj:sg:acc:m1.m2:pos` **oraz** `adj:sg:gen:m1.m2.m3.n:pos` |
| `wazny`, `waznego` | odpowiednio lemma własna i tag `ign`; Morfeusz nie przywraca polskich znaków |
| `domami` | `dom`, `subst:pl:inst:m3` |
| `generate("dom", subst:pl:inst:m3)` | dokładnie `domami` (tagId 690) |
| `sprawdziłem`, ustawienie domyślne `praet=split` | dwa segmenty: `sprawdził / sprawdzić / praet:sg:m1.m2.m3:perf` + `em / być / aglt:sg:pri:imperf:wok` |
| `sprawdziłem`, `praet="composite"` | jeden segment: `sprawdziłem / sprawdzić / praet:sg:m1.m2.m3:pri:perf` |
| `generate("sprawdzić", praet:sg:m1.m2.m3:perf)` w trybie split | tylko `sprawdził` (tagId 549) |
| `generate("sprawdzić", praet:sg:m1.m2.m3:pri:perf)` w trybie composite | dokładnie `sprawdziłem` (tagId 554) |
| `sprawdzilem`, `sprawdzic` | tag `ign`; nie ma transliteracji `ł`/`ć` |
| `mam` | trzy kandydatury: `mama / subst:pl:gen:f`, `mieć / fin:sg:pri:imperf`, `mamić / impt:sg:sec:imperf` |
| `generate("mieć", fin:sg:pri:imperf)` | dokładnie `mam` (tagId 162) |

Wszystkie 10 asercji probe przeszły. Pełny, maszynowy wynik jest w `probe-morfeusz2.json`.

## Tag matching i brak zgadywania

1. **Podstawowy indeks buduj bez Morfeusza:** z każdego rekordu `tbl_morphology` zapisz `word_form → (sense_id, tag)`. Połącz `sense_id → synset_id` przez `tbl_sense`; synonimy bierz wyłącznie z tego samego synsetu/sensu według relacji już istniejących w dumpie.
2. Zachowaj wszystkie kandydatury. `mam` dowodzi, że `string → lemma` nie jest funkcją; bez kontekstu nie wolno automatycznie wybrać `mieć` zamiast `mama` albo `mamić`.
3. Dla **odmienionych synonimów**: dla każdego kandydata źródłowego pobierz jego dokładny `morphological_tag`; dla każdego synonimicznego `sense_id` wybierz tylko formy z tym samym tagiem. Nie ma zgodnej formy — nie wymyślaj jej; pokaż formę podstawową albo pomiń wynik zależnie od opcji UI.
4. Nie należy na ślepo mieszać tagów plWordNet i Morfeusza. Dopóki parser dumpu nie potwierdzi dla konkretnej wersji zgodności słownika tagów, warunkiem jest równość pełnego napisu tagu w `tbl_morphology`. Dopiero potem można świadomie dodać wersjonowany adapter tagsetu; nie opierać go na skracaniu końcówek.
5. Klucz podstawowy: UTF-8 po normalizacji Unicode Form C i porównaniu `StringComparer.Ordinal`, z zachowaniem oryginalnej formy. Ewentualny pomocniczy indeks bez diakrytyków może wskazywać **wyłącznie istniejące** wpisy i musi zwracać wszystkie warianty jako wynik nieprecyzyjny — nie jest analizą fleksji. Probe pokazuje, że `wazny` i `sprawdzilem` są dla Morfeusza `ign`.
6. Jeżeli kiedykolwiek generowany jest alias z Morfeusza, użyj `praet="composite"`; domyślne `split` rozbija `sprawdziłem`, więc nie nadaje się do indeksu form pełnych bez dodatkowego, ryzykownego składania segmentów.

To daje deterministyczną, testowalną ścieżkę: przy buildzie powstają np. `form → lista kandydatów`, `sense → formy według tagu`, `sense → synset` i `synset → senses`; runtime czyta tylko te własne struktury .NET.

## Licencje i redystrybucja

- **Morfeusz2 + dane fleksyjne SGJP/PoliMorf:** oficjalna licencja mówi wprost, że program i zawarte dane fleksyjne są udostępnione na BSD-2; wolno redystrybuować oraz używać w kodzie źródłowym i binarnym, pod warunkiem zachowania informacji copyright, warunków i disclaimeru.[^morfeusz-license] Zainstalowany słownik zwrócił ten sam tekst BSD-2 przez `dict_copyright()`. Ten wariant jest zatem potwierdzonym źródłem dopuszczającym redystrybucję własnego indeksu po dołączeniu noty.
- **PoliMorf:** strona IPI PAN deklaruje dla danych źródłowych i wynikowego zasobu BSD-2.[^polimorf]
- **plWordNet 5.0 wraz z morfologią:** aktualna strona Słowosieci opisuje wersję 5.0, udostępnianie pełnych dumpów po weryfikacji zgłoszenia oraz zalecane cytowanie.[^slowosiec-experts] Oficjalna strona projektu deklaruje publiczne, także komercyjne użycie i publikuje licencję wzorowaną na Princeton WordNet. Jej tekst przyznaje prawo użycia, kopiowania, modyfikacji, dystrybucji i publicznego udostępniania bazy, z obowiązkiem zachowania copyrightu, licencji i disclaimeru.[^slowosiec-license]

**Ważne ograniczenie prawne:** tekst licencji aktualnie eksponowany przez oficjalną stronę nadal oznacza copyright jako `plWordNet 4.2 © 2020`, zaś strona 5.0 nie publikuje odrębnego tekstu licencji konkretnego dumpu. Plik SQL odczytany na początku nie ma nagłówka licencyjnego. Dlatego przed wydaniem EdSharpNG trzeba zachować dołączony/otrzymany wraz z dumpem tekst licencji 5.0 lub potwierdzenie od PWr, a następnie umieścić go w `THIRD-PARTY-NOTICES`. Fakty z 2021 r. potwierdzają, że morfologia SGJP została zintegrowana z plWordNet, ale nie zastępują licencji wydania 5.0.[^plwn-morph] Na podstawie zebranych źródeł nie ma podstawy, aby twierdzić, że osobny, ukryty warunek wyłącza `tbl_morphology`; jest jednak konieczne zachowanie licencji dokładnie tego dumpu.

Dodatkowa obserwacja z początku SQL: dump zawiera `REVINFO` z danymi audytowymi. Generator powinien mieć whitelistę wyłącznie wymaganych tabel i nigdy nie umieszczać tabel audytowych ani danych użytkowników w produkcyjnym indeksie.

## Pliki utworzone

- `/home/michal/.hermes/profiles/edsharp/cache/scratch/morfologia119/probe_morfeusz.py` — odtwarzalny, tylko-odczytowy probe.
- `/home/michal/.hermes/profiles/edsharp/cache/scratch/morfologia119/probe-morfeusz2.json` — wynik 20 392 B, poprawny JSON; 10/10 asercji.
- `/home/michal/.hermes/profiles/edsharp/cache/scratch/morfologia119/raport-morfologia.md` — ten raport.

Nie zmieniono repozytorium produkcyjnego ani plików na głównym komputerze.

[^morfeusz-license]: [Morfeusz 2 — warunki udostępnienia](https://morfeusz.sgjp.pl/doc/license/)
[^morfeusz-doc]: [Morfeusz 2 — dokumentacja techniczna i użytkowa, 22.01.2026](https://download.sgjp.pl/morfeusz/Morfeusz2.pdf)
[^polimorf]: [IPI PAN — PoliMorf](https://zil.ipipan.waw.pl/PoliMorf)
[^slowosiec-experts]: [Słowosieć — dla badaczy i zaawansowanych użytkowników](https://slowosiec.pl/pl/experts)
[^slowosiec-license]: [Słowosieć — o projekcie i licencja](https://slowosiec.pl/pl/about)
[^plwn-morph]: [Dziob, Walentynowicz, *Enriching plWordNet with morphology*, GWC 2021](https://aclanthology.org/2021.gwc-1.20.pdf)
