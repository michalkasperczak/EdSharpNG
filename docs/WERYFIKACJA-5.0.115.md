# Weryfikacja 5.0.115 — fakty i granice

Dokument opisuje **co faktycznie zmierzono** przed wydaniem 5.0.115 i czego
**nie** zmierzono. Nie zawiera ocen jakościowych bez pokrycia w logu.

Gałąź: `fix/import-history-path`, commit poprawki `07470eb`.
Odbiór lokalny: 2026-10-05, Hermes (WSL → Windows, sesja użytkownika).

## Tożsamość binarki

`AssemblyVersion` to `5.0.*`, więc każda przebudowa daje inny plik. Binarka
została **zamrożona po kompilacji**; ten sam plik testowano, pakowano
i instalowano. Po QA nie było przebudowy.

```
86f59310f5f23fe132c361321b9165b2ba8be2c09269cbe4ce1eb6c69316a96e
  = EdSharpNG.exe po buildzie i po całym QA
  = EdSharpNG/EdSharpNG.exe w ZIP
  = C:\Program Files\EdSharpNG\EdSharpNG.exe po instalacji
```

Pełne hashe artefaktów i źródeł: `testy/wyniki/release5115/MANIFEST-5.0.115.txt`.

## Testy automatyczne na finalnej binarce 115

Uruchomione **na tej samej binarce**, która trafiła do paczki:

| Harness | Wynik |
|---|---|
| `harness_tozsamosc_importu` | 125 PASS / 0 FAIL / 3 SKIP |
| zapis oryginału (`verify_original_option`, cz. 1) | 9 PASS / 0 FAIL |
| integracja oryginału (cz. 2) | 107 PASS / 0 FAIL |
| sesja/odzysk (cz. 3) | 74 OK / 0 BŁĄD |

Surowe logi: `testy/wyniki/release5115/01-…`, `02-…`.

3 SKIP w harnessie tożsamości to: DOC (brak pliku testowego), ODT (brak
ścieżki importu w tej gałęzi), EPUB 3 dwukierunkowy (pokryty osobnym
sprawdzeniem jednokierunkowym).

## Żywy odbiór: prawdziwy NVDA i prawdziwy schowek

Testy automatyczne czytają *wartość w pamięci*. Poniższe sprawdzenia szły
przez **fizyczne klawisze wysłane przez NVDA** do okna produktu, a mowę
czytano z **prawdziwego Speech Viewera** NVDA — nie z `speak_text` i nie
z samej roli obiektu. Surowy zapis: `testy/wyniki/release5115/04-zywy-nvda-schowek.log`.

Potwierdzone:

- **Alt+Shift+P po imporcie DOCX** — w schowku Windows (format UnicodeText)
  pełna ścieżka źródła z polskimi znakami i spacjami; odczyt z powrotem
  do własnego bufora zgodny znak w znak (5/5 asercji).
- **Drugi import bez linku (PDF)** — to samo, ścieżka pliku PDF, nie pliku
  roboczego `.txt`.
- **Alt+R po zamknięciu okien** — lista „Recent Files” pokazuje
  `Odzysk bez linku.pdf` i `Artykuł z spacją.docx`, czyli nazwy źródeł.
  Wybór wpisu DOCX otworzył właściwą treść, w **jednym** oknie.
- **Historia po zamknięciu programu** — w profilu zapisane **pełne ścieżki**
  źródeł (`testy/wyniki/release5115/05-historia-po-zamknieciu.log`).
- **Nowy komunikat przy zajętym schowku** — przy udowodnionym czasowo
  przetrzymaniu schowka obcym procesem (lock 01:32:28Z, klawisz 01:32:52Z,
  lock nadal trzymany) produkt powiedział komunikat o niewykonanym
  kopiowaniu ścieżki **dokładnie raz**; po zwolnieniu ten sam skrót znów
  skopiował poprawną pełną ścieżkę. Blokada była zdejmowana we własnym
  `finally`, schowek po teście wolny.
- **Odrzucenie uszkodzonego pakietu** — DOCX z celowo zepsutym katalogiem
  centralnym ZIP (ale poprawnym nagłówkiem `PK`) został odrzucony
  komunikatem; **nie** otwarło się nowe okno i **nie** pojawiła się surowa
  treść ZIP. Plik nie trafił też do historii.

Testowano wyłącznie na własnych plikach syntetycznych; żaden prywatny
dokument użytkownika nie był otwierany ani logowany.

## Instalacja próbna

```
EdSharpNG_Setup_5.0.115.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG=…
```

- kod wyjścia **0**, ścieżka lokalna (nie UNC), sesja zalogowana;
- rejestr: `DisplayName = EdSharpNG 5.0.115 (beta)`, `DisplayVersion = 5.0.115`,
  `InstallLocation = C:\Program Files\EdSharpNG\`;
- log Inno 204 838 B, bez `Exception`/`Aborted`;
- okno uruchomione **z zainstalowanej ścieżki**, druga instancja zakończyła
  się sama (single instance);
- wersja potwierdzona **odczytem**, nie istnieniem procesu: własny log
  produktu zawiera `2026-10-05 03:50:03  start  EdSharpNG 5.0.115 uruchomiony`.

## Skład paczki

Lista plików ZIP 5.0.115 porównana 1:1 z przyjętą paczką 5.0.114:
**0 braków, 0 nadwyżek, 444 pliki w obu**. Obecne `Ude.dll`,
`nvdaControllerClient.dll`, `Tektosyne.dll`, `EdSharpNG-spellcheck.nvda-addon`.
Konwertery jak w 5.0.114 — tylko `Convert/any2txt.cmd`; silników zewnętrznych
nie pakowano i nie pobierano. Źródła (24 pliki) i `lgpl.txt` zachowane.
W paczce brak profili testowych, logów i katalogu `testy/` (sprawdzone
programowo).

## Czego NIE zmierzono — granice tego odbioru

- **Import DOC** (stary binarny Word) — **niezmierzony**, brak pliku
  testowego. Kod nie był w tej gałęzi zmieniany, ale to nie jest dowód
  poprawności. Pozostaje jawnie otwarte.
- **ODT** — brak ścieżki importu w tej gałęzi, nic nie zmierzono.
- **EPUB 3** — sprawdzony jednokierunkowo (normalizacja przy czytaniu
  + odmowa uszkodzonego pakietu); trasa dwukierunkowa nie była testowana.
- **Pozostałe formaty z puli ~20** — nie przechodzono ich jeden po drugim;
  ten odbiór celował w zakres poprawki, nie w pełny audyt formatów.
- **Wydajność i długie sesje** — nie badano.
- Instalację sprawdzono tylko na Hermesie, aktualizacją z 5.0.114.
  Instalacja na czystym systemie nie była testowana.

## Zmiany w narzędziach (nie w produkcie)

- `build_installer_garfield.sh` — katalog stagingu sparametryzowany
  (`STAGE=…`). Wcześniej miał zaszyte `C:\EdSharp` i robił na nim `rm -rf`;
  na Hermesie w tym katalogu stoi inne drzewo robocze (177 plików), które
  zostałoby zniszczone. `SourceDir`/`OutputDir` w `.iss` są teraz ustawiane
  w stagingu, a nie zakładane. Dodano też odporność na brak logu buildu,
  gdy binarka jest celowo zamrożona i tylko pakowana.
- `testy/spakuj_zip.py` — katalog stagingu jako drugi argument.
- `testy/schowek_zywy_115.cs`, `testy/podglad_mowy_115.cs` — nowe narzędzia
  pomiarowe (schowek Windows, odczyt Speech Viewera NVDA).

`C:\EdSharp` i `C:\EdSharpBuild` (referencja 5.0.114) pozostały nietknięte.
Profil użytkownika `%APPDATA%\EdSharp` i schowek przywrócono bajt w bajt
z kopii. NVDA i AMC nie były restartowane. Główny komputer nie był dotykany.
