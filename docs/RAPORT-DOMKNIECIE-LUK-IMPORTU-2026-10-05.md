# Domknięcie luk tożsamości importu: odzysk sesji, anulowane zamknięcie, czytnik epub3

Korekta na commicie `2c1edf7` (`fix/import-history-path`). Poprzednia zmiana dała
zaimportowanemu dokumentowi **tożsamość** (`ImportedFrom` / `IdentityPath`), ale
odzysk sesji tej tożsamości nie rozumiał. Tu domykam trzy konkretne luki, z
których dwie groziły **nadpisaniem oryginału**, a trzecia czytaniem archiwum ZIP.

## 1. Co było zepsute (zmierzone, nie wywnioskowane)

Pomiar RED: nowa sonda na **niezmienionym produkcie z commitu rodzica**
(`C:\EdSharpRedProof`, zbudowana z `git archive HEAD`) — **108 PASS / 10 FAIL**,
log `testy/wyniki/tozsamosc-importu/RED-luki-domkniecie.log`.

### Luka 1: odzysk niezmienionego importu meldował błąd mimo otwartego okna

`PrzywrocSesje` szukał przywróconego okna przez `Util.Equiv(candidate.File, sFile)`.
Po imporcie `candidate.File` to **nazwa robocza** (`Książka.md`), a `sFile` to pełna
ścieżka źródła (`...\Książka.pdf`) — warunek nie trafiał **nigdy**, więc odzysk
rzucał `IOException("The document could not be opened.")` przy poprawnie otwartym
oknie.

    FAIL F1 odzysk niezmienionego importu bez linku melduje sukces

### Luka 2: odzysk zmienionego importu kierował zapis na binarny oryginał

Zmieniony import bez `OriginalDocument` wpadał do gałęzi
`else if (hasFile) { restored.File = sFile; }`. Bufor jest Markdownem/czystym
tekstem, a `sFile` to `*.pdf` / `*.doc` — po odzysku **cel zapisu wskazywał
binarny oryginał**. Jeden Ctrl+S i artykuł użytkownika zostaje zastąpiony surowym
tekstem. Nic nie pytało o zgodę: to zwykły zapis do „własnego" pliku.

    FAIL F2 child.File po odzysku NIE jest pdf (ochrona przed nadpisaniem surowym tekstem)
    FAIL F2 rozszerzenie celu zapisu nie jest .pdf

### Luka 3: anulowane zamknięcie podmieniało cel zapisu na stałe

Handler `MdiChild.Closing` zapisywał tożsamość do historii przypisaniem
`sFile = this.IdentityPath`. `sFile` to **pole prywatne** klasy, to samo, które
stoi za `child.File` — czyli za celem zapisu. Gdy użytkownik **anulował**
zamknięcie (pytanie „zapisać zmiany?" → Anuluj), okno zostawało z celem
przestawionym na źródło importu. Pomiar złapał to dosłownie:

    FAIL K cel zapisu NIETKNIETY przez anulowane zamkniecie
         (bylo: Anulowane.md, jest: C:\EdSharpRedProof\QA tożsamość ŻÓŁĆ\Anulowane.docx)

### Luka 4: czytnik epub3 nie istnieje, a program czytał wtedy archiwum

Sprawdzone u źródła, na Pandocu z katalogu narzędzi:

    pandoc.exe --list-input-formats  | grep -x epub3   -> brak
    pandoc.exe "book.epub3" -f epub3 ... -> Unknown input format 'epub3'   (exit 21)
    pandoc.exe "book.epub3" -f epub  ... -> exit 0, poprawny Markdown

Pandoc ma **zapis** do `epub3`, ale **czytnika `epub3` nie ma** — jest tylko
`epub`, który czyta oba. Sześć wpisów `epub32*` w `[Import]` wołało `-f epub3`,
więc import książki padał, a `OpenOrActivateWindow` wracał do otwarcia
**surowego**: czytnik ekranu dostawał bajty archiwum ZIP (`PK`, `mimetype`,
`META-INF`).

    FAIL I epub3 NIE zostaje otwarty jako surowe archiwum ZIP

## 2. Naprawy

| plik | zmiana |
|---|---|
| `Sesja.cs` | `SesjaOkno.Robocza` — nazwa robocza obok źródła; `JestImportem` to `Robocza.Length > 0`. Puste = „to nie import", więc **stara sesja bez tego klucza wczytuje się jak dotąd**. Klucz dopisany do zapisu i odczytu. |
| `EdSharp.cs` `ZbierzSesje` | import bez linku zapisuje `Plik = ImportedFrom` **oraz** `Robocza = sFile`. |
| `EdSharp.cs` `PrzywrocSesje` | dopasowanie okna po `IdentityPath` dla importu; odzysk ustawia `File = Robocza` (nazwa robocza) i `ImportedFrom = źródło` — **tożsamość tak, prawo zapisu nie**; otwarcie przez `PreferredImportKey`, nie surowo. |
| `EdSharp.cs` `Closing` | tożsamość w **zmiennej lokalnej** `sIdent`, pole `sFile` nietknięte. Historia dostaje wpis, cel zapisu zostaje. |
| `ZapisFormatow.cs` `NormalizujPolecenie` | `-f/--from/-r/--read epub3` → `epub`. **Tylko wejście**; `-t epub3` nietknięte (eksport `md2epub3` działa dalej). W normalizacji, nie w samym INI, bo **profil użytkownika ma pierwszeństwo nad `EdSharp.ini`** i stary profil dalej by się psuł. |
| `EdSharp.ini` | te same 6 wpisów poprawione u źródła (`-f epub3` → `-f epub`); 3 wpisy `-t epub3` nietknięte. |
| `EdSharp.cs` droga konwersji | gdy narzędzie **uruchomiło się i padło**, a format jest spakowany (`FormatSpakowany`), program **odmawia otwarcia** zamiast mówić „opening file as is". Pozostałe gałęzie pytały o to już wcześniej, ta jedna nie. Dla formatów tekstowych (rtf, html, pdf) surowa treść nadal jest lepsza niż nic. |

`SetRecent` — **sprawdzone, bez zmian.** Ciało jest poprawne: strażnik
`!sFile.Contains(@"\")` przepuszcza `IdentityPath` (pełna ścieżka źródła), wpis
dostaje pozycję kursora, flagę strażnika i zawijanie (`EdSharp.cs` 8980–8996).
Przypisanie `sFile` w ostatnich wierszach dotyczy **parametru lokalnego**, nie
pola — nie ma tu drugiego wariantu tego samego błędu co w `Closing`.
Zakładki nietknięte: przebudowy zakładek nie ruszam.

## 3. Pomiary

Sonda: `testy/harness_tozsamosc_importu.cs` (ładuje prawdziwą binarkę refleksją),
uruchamiana przez `testy/zmierz_tozsamosc_importu.sh`.

| przebieg | produkt | wynik | log |
|---|---|---|---|
| RED | commit `2c1edf7` bez poprawek, sonda nowa | **108 PASS / 10 FAIL / 3 SKIP** | `RED-luki-domkniecie.log` |
| GREEN | kandydat z poprawkami | **125 PASS / 0 FAIL / 3 SKIP** | `GREEN-luki-domkniecie.log` |

Ta sama sonda na obu wersjach, więc 10 FAIL → 0 FAIL to różnica w produkcie, nie
w kryterium.

### Nowe sekcje sondy

- **F** — odzysk importu bez `OriginalDocument`: niezmieniony (F1) i zmieniony z
  kopią odzysku (F2). F2 kończy **próbą zapisu wstecz i porównaniem bajtów pdf**.
- **G** — odzysk, gdy źródło importu zniknęło: treść wraca z kopii, cel zapisu nie
  wskazuje nieobecnego pliku.
- **H** — **rzeczywista próba zapisu** docx przy opcji `N` i przy `Y`: przy `N`
  droga nie przyjmuje zadania i bajty stoją, przy `Y` bajty **naprawdę się
  zmieniają**. Poprzednia wersja ustawiała opcję globalnie na `Y` i mierzyła
  tylko jeden wariant.
- **I** — epub3 od strony produktu: nie wolno otworzyć surowego ZIP-a, treść musi
  być czytelna, plus **zapis wstecz do epub3** (bo `OriginalFormatSupported`
  obiecuje go obok docx — przy zepsutym czytniku ta obietnica była martwa).
- **J** — kontrakt normalizacji: `-f epub3` zamienione, `-t epub3` nietknięte,
  inne formaty wejścia nietknięte (kontrola różnicująca).
- **K** — anulowane zamknięcie nie rusza celu zapisu, a historia wpis dostaje.

### Naprawione błędy pomiaru (poprzednia wersja sondy)

1. **A5 i B liczyły odcisk PO akcji.** W sekcji B `beforeHash` powstawał *po*
   `TrySaveOriginalDocument`, więc asercja porównywała plik **sam ze sobą** i
   przechodziła także wtedy, gdy zapis wstecz doszedł do skutku. Odcisk liczony
   teraz przed rzeczywistą operacją (B) i przed importem (A5).
2. **`zmierz_tozsamosc_importu.sh` gubił kody wyjścia.** `cmd /c BuildEdSharp.cmd | tail -8`
   oddawał kod `tail`-a (zawsze 0), a końcowe `echo "KOD: $?"` zjadało kod pomiaru
   — błąd kompilacji i czerwony pomiar mogły wyjść jako sukces. Teraz kod budowania
   czytany przez `PIPESTATUS`, kod pomiaru zapamiętany i zwrócony przez `exit`.
   Sprawdzone: RED dał kod 1, GREEN kod 0.
3. **Sekcja B twierdziła, że epub3 jest jednokierunkowy.** Lista była wpisana z
   ręki, a `OriginalFormatSupported` (`EdSharp.cs` 3309) wymienia `epub3` obok
   `docx`/`odt`/`rtf`. Dopóki import epub3 był zepsuty, nikt tego nie zauważył.
   Sekcja B pyta teraz **produkt**, który format ma drogę powrotną, i oddaje epub3
   do sekcji H/I. To jedyny stary test, który odwracam — kodował dawne, zepsute
   zachowanie.

## 4. Ograniczenia pomiaru

- **`.doc` niezmierzony.** Produkt **umie czytać** `.doc` (`Convert/doc2txt.cmd` →
  `WdVert.exe`, z odwrotem na `GetText.exe`), ale ten pomiar nie umie *wytworzyć*
  prawdziwej próbki: nie ma wpisu `md2doc`, a instalowanie Worda ani LibreOffice
  nie wchodziło w zakres. Nie znalazłem licencjonowanej próbki `.doc` o znanej
  treści w istniejących testach ani konwerterach. Sonda melduje to jako `LIMIT`,
  nie jako zaliczenie. **Ścieżka `.doc` jest niezmierzona, nie zepsuta** — i nie
  blokuje wydania.
- **`.odt`** nie ma żadnego wpisu w `[Import]`, więc EdSharp go nie importuje.
  Sonda pilnuje, żeby późniejsze dodanie `odt2*` nie przeszło bez sprawdzenia
  tożsamości. Nowych formatów nie dodaję.
- **Ctrl+S nie był wciskany w SaveAs na oryginale.** Mierzę drogę zapisu przez
  `TrySaveOriginalDocument` i przez wartość `child.File`, z porównaniem bajtów
  oryginału. Nie wybierałem oryginału w okienku SaveAs — przy odmowie zapisu
  dialog należałoby obsłużyć ręcznie, a pomiar ma być powtarzalny.
- **SHA binarki nie dowodzi źródeł.** Kompilacja nie jest deterministyczna
  (`AssemblyVersion("5.0.*")` + znacznik czasu): dwa budowania tych samych źródeł
  dały różne SHA. SHA identyfikuje **artefakt**, nie wersję kodu.
- Testy chodziły na **prywatnym pulpicie**, bez widocznego GUI, bez NVDA i bez
  schowka systemowego — jak u poprzednika.

## 5. Stan wydania

- Wersja **nie bumpowana** — zostaje `5.0.114` (do ustalenia przez rodzica).
- **Nic nie publikowane, nic nie instalowane, `main` nietknięty.** Bez `push`.
- Katalog roboczy: `C:\EdSharpImportIdentity` (kandydat). `C:\EdSharpBuild`
  nietknięty jako odniesienie. `C:\EdSharpRedProof` to jednorazowe stanowisko RED.

## 6. Kontynuacja

```bash
cd /home/michal/projekty/edsharp-import-identity

# pełny pomiar (buduje kandydata w C:\EdSharpImportIdentity i mierzy)
bash testy/zmierz_tozsamosc_importu.sh ; echo "kod: $?"   # 0 = zielony

# powtórzenie RED na commicie rodzica (czyste stanowisko)
R=/mnt/c/EdSharpRedProof; rm -rf $R; mkdir -p $R
git archive HEAD | tar -x -C $R
cp testy/harness_tozsamosc_importu.cs $R/
cp -r /mnt/c/EdSharpImportIdentity/Convert $R/
cd $R && /mnt/c/Windows/System32/cmd.exe /c BuildEdSharp.cmd \
      && /mnt/c/Windows/System32/cmd.exe /c zmierz_tozsamosc_importu.cmd
```

### Co zostaje otwarte

1. **`.doc`** — próbka do zmierzenia ścieżki importu (patrz Ograniczenia).
2. **Pozostałe formaty spakowane** — użytkownik prosił o sprawdzenie innych
   formatów. Sprawdziłem epub3 do końca; `[Import]` ma też wpisy dla innych
   formatów, których nie przeszedłem po kolei pod kątem „czy błąd konwersji nie
   pozwala otworzyć surowo". Odmowa otwarcia jest już wspólna dla wszystkich
   `FormatSpakowany`, więc to pomiar pokrycia, nie nowa naprawa.
3. **SaveAs na oryginale** — ręczne potwierdzenie, że odmowa zapisu jest
   bezpieczna i słyszalna.
