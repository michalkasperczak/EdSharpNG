# Tożsamość zaimportowanego dokumentu: historia ostatnich i kopiowanie ścieżki

Gałąź `fix/import-history-path`, worktree `/home/michal/projekty/edsharp-import-identity`.
Pomiary z 05.10.2026. Raport dla rodzica — kontynuacja live/instalator na końcu.

## 1. Co było zepsute

Zgłoszenie Michała: otworzył przygotowany artykuł `.docx`, po zamknięciu **nie
znalazł go na Alt+R**; polecenie kopiowania ścieżki (Alt+Shift+P) też nie dało
ścieżki. Diagnoza: `docs/DIAGNOZA-QUILL-IMPORT-HISTORIA-2026-10-05.md`.

Przyczyna, zmierzona a nie zgadnięta: po imporcie `child.File` trzyma **samą
nazwę roboczą Markdowna bez katalogu** (`Artykul.md`), bo bufor nie jest plikiem
na dysku. A wszystkie trzy miejsca, które *mówią* o pliku — handler zamknięcia
`MdiChild`, `SetRecent` i `menuMiscPathToClipboard` — pytały właśnie o
`child.File`. `SetRecent` i handler zamknięcia odrzucają napis bez ukośnika
(`if (!sFile.Contains(@"\")) return;`), czyli odpadały **milczkiem**.

## 2. Rozwiązanie: jedna właściwość, nie nowa architektura

Rozdzielone zostały dwie rzeczy, które do tej pory jechały na jednym polu:

| | znaczenie | kto dostaje |
|---|---|---|
| `OriginalDocument` | *wolno zapisać wstecz* | tylko Markdown z formatu obsługiwanego przez zapis wstecz, przy włączonym `SaveImportedOriginalFormat` |
| `ImportedFrom` (nowe) | *tak nazywa się dokument, który czytam* | **każdy** import, także PDF/`.doc`/konwersje do czystego tekstu |
| `IdentityPath` (nowe, pochodne) | tożsamość dla poleceń mówiących o pliku | `OriginalDocument.Path` → `ImportedFrom` → `child.File` |

`IdentityPath` dla pliku otwieranego surowo zwraca dokładnie `child.File`, więc
droga nieimportowana **nic nie traci** (punkty C w pomiarze).

**Czego ta naprawa świadomie NIE robi:** `child.File` zostaje nazwą roboczą.
Przypisanie tam ścieżki DOCX-a oznaczałoby, że Control+S nadpisuje dokument
Worda surowym tekstem. `IdentityPath` nie jest i nie może być celem zapisu —
zapis idzie przez `child.File` i `TrySaveOriginalDocument`. Utrwalenie
tożsamości **nie nadaje nowego prawa zapisu wstecz** (punkty B w pomiarze
sprawdzają to osobno dla każdego formatu bez drogi powrotnej).

### Zmienione miejsca (`EdSharp.cs`, +95/−8, jeden plik)

1. `MdiChild` — nowe pole `ImportedFrom`, nowa właściwość `IdentityPath`;
   setter `File` czyści `ImportedFrom` (żeby „Zapisz jako Markdown" naprawdę
   odczepiało tożsamość, a nie pozornie).
2. `MdiChild.Closing` — `sFile = this.IdentityPath` zamiast `this.File`.
3. `menuMiscWordWrap` / `menuMiscUnwrap` — `SetRecent(child.IdentityPath)`.
4. `menuMiscPathToClipboard` — kopiuje `child.IdentityPath`; **plus** wynik
   `Util.SetClipboardText` przestał być ignorowany. Dotąd komenda mówiła ścieżkę
   nawet wtedy, gdy schowek trzymał inny proces — czyli brzmiała identycznie
   przy pustym schowku. Dla niewidomego najgorszy wariant: dowiaduje się dopiero
   przy wklejaniu. Komunikat w tej samej postaci, co 11 pozostałych ścieżek
   kopiowania w pliku (`"Clipboard is busy, path not copied!"`).
5. `ZbierzSesje` — `okno.Plik` bierze `ImportedFrom`, ale `OriginalFormatFile`
   **tylko** gdy jest `OriginalDocument`; odzysk nie nadaje prawa zapisu.
6. `OpenOrActivateWindow` — pętla „już otwarte?" pyta o `IdentityPath` (bez tego
   Alt+R robiłby drugie okno z tym samym artykułem i dwiema kopiami zmian);
   gałąź importu ustawia `ImportedFrom = sFile` **po** przypisaniu `child.File`
   (kolejność istotna) i woła `SetRecent(sFile)` — wpis powstaje **przy
   imporcie**, nie przy zamknięciu, bo handler zamknięcia wraca też przy
   pozycji kursora 0.

Klawisze **nietknięte**: Alt+Shift+P = PathToClipboard, Ctrl+Shift+P = PathList.
Numer wersji **nie bumpowany** — zostaje `5.0.114` (do ustalenia przez rodzica).

## 3. Pomiary

Sonda: `testy/harness_tozsamosc_importu.cs`, uruchamiana przez
`testy/zmierz_tozsamosc_importu.sh` (+ `testy/uruchamiacze/zmierz_tozsamosc_importu.cmd`).
Ładuje **prawdziwą binarkę** przez refleksję i chodzi rzeczywistą drogą
`MdiFrame.OpenOrActivateWindow` z kluczem `[Import]` — żadnej atrapy konwertera.
Pliki źródłowe są budowane naprawdę, Pandokiem, przez `MdiFrame.KonwertujDoPliku`.

Sonda czyta `IdentityPath` przez refleksję i gdy właściwości nie ma (stara
wersja) spada na `child.File` — czyli dokładnie na to, czym stara wersja karmiła
historię i schowek. Dzięki temu **obie wersje ocenia to samo kryterium**.

| przebieg | binarka | SHA256 | wynik |
|---|---|---|---|
| ODNIESIENIE (RED) | 5.0.114 zainstalowana | `0a3e8b9db37c2deb41c0bfd2b9f2bd9a0dcb5fab99723ee5b1d958607eb714c6` | **56 PASS / 23 FAIL / 3 SKIP** |
| KANDYDAT (GREEN) | ten commit | `cb794a9834faa79944be65128b3f29f53ab38bd4a9fb3bbf12ee240c37e274d9` | **79 PASS / 0 FAIL / 3 SKIP** |

**SHA kandydata jest ZMIENNY między przebudowami — sprawdzone, nie założone.**
`BuildEdSharp.cmd` nie podaje `csc /deterministic`, więc dwie kompilacje
identycznych źródeł dały `ea4cfb7470…` i `f7182924fa…`. SHA powyżej identyfikuje
**ten konkretny plik, który dał ten wynik** (sonda wypisuje go do logu), a nie
źródła. Tożsamość kodu dowodzi commit, nie hash exe. Jeśli rodzic chce
powtarzalnego SHA, trzeba dodać `/deterministic` do `BuildEdSharp.cmd` —
to osobna decyzja, poza zakresem tej naprawy.

Logi: `testy/wyniki/tozsamosc-importu/RED-5.0.114.log`,
`testy/wyniki/tozsamosc-importu/GREEN-kandydat.log`.

Pomiar **nietrywialnie różnicuje** wersje: 23 punkty czerwone na starej binarce
(brak wpisu w historii i zła wartość ścieżki dla docx/epub/html/rtf/pdf/rst/tex,
plus zgubiona tożsamość po odzysku sesji), a punkty kontrolne MD/TXT,
bezpieczeństwo oryginału i odczepienie po „Zapisz jako" przechodzą na **obu**.

### Zakres: skąd wzięta lista formatów

Nie z pamięci — z tabeli `[Import]` w `EdSharp.ini`, którą sonda wypisuje do
logu (20 źródłowych rozszerzeń). Sprawdzone realnie:

| format | droga | wynik |
|---|---|---|
| `.docx` | `docx2md`, zapis wstecz | PASS (12 punktów) |
| `.epub` | `epub2md`, zapis wstecz | PASS (12 punktów) |
| `.html` | `html2md`, zapis wstecz | PASS (12 punktów) |
| `.rtf` | `rtf2md`, zapis wstecz | PASS (12 punktów) |
| `.pdf` | `pdf2txt`, **bez** zapisu wstecz | PASS (5 punktów) |
| `.rst` | `rst2md`, bez zapisu wstecz | PASS (5 punktów) |
| `.tex` | `tex2md`, bez zapisu wstecz | PASS (5 punktów) |
| `.md`, `.txt` | kontrola, droga surowa | PASS (8 punktów) |

### Ograniczenia — jawnie, bez udawania

- **`.odt` NIE jest formatem importu EdSharpa.** Tabela `[Import]` nie ma
  żadnego wpisu `odt2*`. To stan produktu, nie luka pomiaru; sonda pilnuje tego
  asercją, żeby ewentualne dodanie `odt2md` nie przeszło bez sprawdzenia
  tożsamości. Fikstury też nie da się zbudować (brak `md2odt`).
- **`.doc` pominięty — brak konwertera do zbudowania fikstury** (`md2doc` nie
  istnieje; `doc2txt` czyta, ale nie mam czym *wytworzyć* prawdziwego `.doc`).
  Droga `doc2txt` to ta sama gałąź kodu co zmierzony `pdf2txt`, ale **nie
  twierdzę, że ją zmierzyłem.**
- **`.epub3` — defekt SPRZED tej zmiany, nie regresja.** Wpisy `epub32*` wołają
  `pandoc -f epub3`, a Pandoc 3.11 nie ma *czytnika* `epub3` (tylko zapis).
  Konwersja pada, program cicho wraca do otwarcia surowego i użytkownik dostaje
  bajty ZIP-a. Potwierdzone osobną sondą na **obu** binarkach. Sonda melduje to
  jako SKIP z powodem, a nie mierzy drogi surowej pod nazwą importu.
  **Do osobnego zgłoszenia** (poprawka jednoliniowa w `EdSharp.ini`:
  `epub32* → -f epub`) — poza zakresem tej naprawy.
- **Schowek NIE był dotknięty.** Osobny prywatny pulpit dzieli schowek z sesją
  użytkownika, więc fizycznego `SetClipboardText`/wklejenia nie da się zmierzyć
  bez ingerencji w schowek Michała. Zmierzona jest **wartość, którą komenda
  bierze** (`child.IdentityPath`) — i nie nazywam tego pomiarem schowka.
  Etap GUI należy do rodzica (punkt 5 niżej).
- **Fizyczne „pusto" użytkownika nie odtworzone** (zgodnie z diagnozą).
- Nazwy polskie i spacje sprawdzone: katalog `QA tożsamość Żółć`, plik
  `Artykuł z spacją.<ext>`.

### Brak regresji w istniejących testach

`testy/uruchamiacze/verify_original_option.cmd` na kandydacie, exit 0:
`98 OK / 0 BŁĄD` (transakcja zapisu oryginału) + `9 PASS / 0 FAIL` (opcja
formatu źródłowego) + `107 PASS / 0 FAIL` (integracja oryginału, w tym
„failed original save preserves external bytes" dla docx/epub/html/rtf) +
`74 OK / 0 BŁĄD` (sesja).
Log: `testy/wyniki/tozsamosc-importu/REGRESJA-zapis-oryginalu.log`.

## 4. Artefakty

- Kandydat: `C:\EdSharpImportIdentity\EdSharpNG.exe`, 535 552 B,
  SHA256 `cb794a9834faa79944be65128b3f29f53ab38bd4a9fb3bbf12ee240c37e274d9`
  (patrz uwaga o niedeterminizmie csc w punkcie 3 — hash identyfikuje plik,
  nie źródła).
- **Staging świadomie NIE jest `C:\EdSharp`** (zajęte starym drzewem) ani
  `C:\EdSharpBuild` (trzyma binarkę odniesienia 5.0.114; nadpisanie zabrałoby
  możliwość pokazania, że pomiar różnicuje wersje). `Convert\` jest dowiązaniem
  (junction) do `C:\EdSharpBuild\Convert` — dysk C ma ~8 GB wolnego.
- **Nic nie zainstalowane, nic nie opublikowane, pulpit użytkownika nietknięty**
  (sondy biegną bez `SwitchDesktop`, okna ramki tylko `Show()` w procesie sondy).

## 5. Co rodzic musi zrobić, żeby ciągnąć dalej

```bash
# A. Powtórzenie pomiaru (odniesienie PIERWSZE, kandydat OSTATNI — tryb
#    --stara nadpisuje EdSharpNG.exe w stagingu binarką 5.0.114):
cd /home/michal/projekty/edsharp-import-identity
bash testy/zmierz_tozsamosc_importu.sh --stara   # oczekiwane 23 FAIL
bash testy/zmierz_tozsamosc_importu.sh           # oczekiwane 0 FAIL

# B. Regresja zapisu oryginału:
cd /mnt/c/EdSharpImportIdentity && cmd.exe /c verify_original_option.cmd
```

**Etap live GUI/NVDA (mój zakres tego nie obejmował):**
1. Uruchomić kandydata na widocznym pulpicie, otworzyć prawdziwy `.docx`
   Michała, nacisnąć **Alt+Shift+P**, wkleić gdziekolwiek — sprawdzić, że
   w schowku jest pełna ścieżka DOCX-a, a nie `Artykul.md`.
2. Zamknąć dokument, **Alt+R** — sprawdzić, że DOCX jest na liście i że
   wybranie go otwiera **jedno** okno z treścią.
3. Sprawdzić, co NVDA czyta przy nowym komunikacie
   `"Clipboard is busy, path not copied!"` (do tej pory komenda mówiła ścieżkę
   nawet przy porażce).
4. Zweryfikować „fizyczne pusto" zgłoszone przez Michała — niereprodukowane
   ani w diagnozie, ani tutaj.

**Wydanie/instalator:** numer wersji nie bumpowany, Inno nie uruchamiany,
`git push` nie wykonany — wszystko po stronie rodzica.
