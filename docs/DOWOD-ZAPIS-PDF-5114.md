# Niezależna weryfikacja paczki zapis + PDF — dowód złożony

Data pomiaru: 18.09.2026, 17:04–17:17 CEST
Wykonawca: sesja weryfikująca (osobna od sesji integrującej)
Staging: `C:\EdSharpRegressionTest` (izolowany; **nie** `C:\EdSharpBuild`,
**nie** `C:\EdSharpSaveTest`)

## Czego ten dokument NIE dowodzi

Żaden z tych pomiarów nie dotyka GUI ani czytnika ekranu. **Mowa NVDA/JAWS nie
była tu mierzona.** Zmierzona jest logika zapisu, realne pliki na dysku i
struktura otagowania PDF. Weryfikacja komunikatów żywym czytnikiem pozostaje do
zrobienia po stronie sesji, która ma desktop.

## Mierzony artefakt

| co | wartość |
|---|---|
| commit | `bb7bf2823a8dd631ae34871214b1abea7bdc28cc` (18.09.2026 16:55:36) |
| tytuł | Transakcja zapisu do formatu zrodlowego: OriginalFormatSave + harness |
| exe | `C:\EdSharpBuild\EdSharpNG.exe` |
| md5 przebiegu finalnego | `f4d8709eb5bbf9bf746d69d264405f8d` (mtime 17:16:01) |
| csc | `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (.NET Framework 4.8) |
| Edge | `C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe` |
| Pandoc | 3.11 (kopia z `C:\EdSharpBuild\Convert\Pandoc\pandoc.exe`) |
| pypdf | 6.19.0 (`/tmp/pdfvenv/bin/python`) |

UWAGA O RUCHOMYM CELU: binarka w `C:\EdSharpBuild` zmieniła się **trzy razy** w
trakcie tej weryfikacji (sesja równoległa buduje):
`cb2e6f3b…` (16:52) → `8280c9a8…` (17:05:53) → `f4d8709e…` (17:16:01).
Zmierzone zostały **dwie pierwsze i ostatnia**; wszystkie trzy dały ten sam
wynik. Wersje plików: 5.0.9757.28588, 5.0.9757.28976.

## Wynik zbiorczy — 159 asercji harnessów, 0 FAIL (185 z kontrolą artefaktów)

Liczby są policzone z logów finalnego przebiegu (`grep -c` po `OK`/`PASS`),
nie przepisane z pamięci.

| # | pomiar | asercje | wynik | log |
|---|---|---|---|---|
| 1 | `tests/PdfExportHarness.cs` — PdfExport przez realny Edge | 25 | **25 OK / 0 BLAD** | `logi/pdf-harness.log` |
| 2 | `tests/sprawdz_pdf.py` — treść + tagi PDF z harnessu (pypdf) | 14 | **14 OK / 0 BLAD** | `logi/pdf-pypdf.log` |
| 3 | `testy/harness_zapis_formatow.cs` — konwersje realnym Pandokiem | 89 | **89 PASS / 0 FAIL** | `logi/formats-harness.log` |
| 4 | `testy/harness_zapis_bezpieczny_5114.cs` — odporność transakcji | 8 | **8 PASS / 0 FAIL** | `logi/hardening-harness.log` |
| 5 | `testy/harness_integracja_zapisu_5114.cs` — zapis przez GOTOWY exe | 11 | **11 PASS / 0 FAIL** | `logi/integracja-harness.log` |
| 6 | PDF **złożony przez exe** — tagi/H1/link/polskie litery (pypdf) | 12 | **12 OK / 0 BLAD** | `logi/pdf-z-exe-pypdf.log` |
| 7 | niezależna kontrola artefaktów na dysku (poniżej) | 26 | **26 PASS / 0 FAIL** | w tym dokumencie |

Razem 185 asercji z kontrolą artefaktów; 159 z samych harnessów + sond PDF.
Wszystkie logi: `C:\EdSharpRegressionTest\logi\`

## Dokładne polecenia (odtwarzalne)

```bash
CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"

# 1+2. PdfExport przez Edge, potem treść i otagowanie PDF
cd /mnt/c/EdSharpRegressionTest/pdf
"$CSC" /nologo /target:exe /platform:anycpu /langversion:5 \
  /out:PdfExportHarness.exe PdfExport.cs PdfExportHarness.cs
./PdfExportHarness.exe 'C:\EdSharpRegressionTest\pdf\wyniki'
/tmp/pdfvenv/bin/python ~/projekty/edsharp/tests/sprawdz_pdf.py \
  /mnt/c/EdSharpRegressionTest/pdf/wyniki/harness-<id>.pdf

# 3. konwersje formatów realnym Pandokiem
cd /mnt/c/EdSharpRegressionTest/formats
"$CSC" /nologo /target:exe /platform:anycpu /langversion:5 \
  /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll \
  /out:harness_formatow.exe harness_zapis_formatow.cs ZapisFormatow.cs
./harness_formatow.exe

# 4. odporność transakcji zapisu
cd /mnt/c/EdSharpRegressionTest/hardening
"$CSC" /nologo /target:exe /platform:anycpu /langversion:5 \
  /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll \
  /out:SaveSafety.exe harness_zapis_bezpieczny_5114.cs ZapisFormatow.cs
./SaveSafety.exe

# 5. zapis przez GOTOWY EdSharpNG.exe (refleksja na MdiFrame.KonwertujDoPliku)
cd /mnt/c/EdSharpRegressionTest/build
"$CSC" /nologo /target:exe /platform:anycpu /langversion:5 \
  /out:SaveIntegration.exe harness_integracja_zapisu_5114.cs
./SaveIntegration.exe 'C:\EdSharpRegressionTest\build\EdSharpNG.exe' \
                      'C:\EdSharpRegressionTest\build\EdSharp.ini'

# 6. PDF złożony przez exe
/tmp/pdfvenv/bin/python /mnt/c/EdSharpRegressionTest/logi/sprawdz_pdf_z_exe.py \
  "/mnt/c/EdSharpRegressionTest/build/QA formats Żółty/wynik Żółty raport.pdf"
```

Wszystko razem jednym poleceniem: `testy/verify_save_bundle.sh`
(opcjonalnie `EXE=/mnt/c/gdzies/EdSharpNG.exe testy/verify_save_bundle.sh`).

## PDF Z EXE: ODPOWIEDŹ NA PYTANIE, KTÓRE BYŁO OTWARTE

Ścieżka PDF **jest** w gotowej binarce i **działa**. Nie wnioskuję tego z
zielonego builda — zmierzone na pliku, który wyprodukował `EdSharpNG.exe`:

```
plik: /mnt/c/EdSharpRegressionTest/build/QA formats Żółty/wynik Żółty raport.pdf
stron: 1
OK   tekst obecny: 'Żółty nagłówek'
OK   tekst obecny: 'Zażółć gęślą jaźń'
OK   tekst obecny: 'chleb'
OK   tekst obecny: 'mleko'
OK   /MarkInfo /Marked = True
OK   /StructTreeRoot obecny
typy struktur: Document, H1, L, LI, Lbl, Link, NonStruct, P
OK   struktura H1 obecna
OK   struktura L / LI / Link / P obecne
adresy linkow: ['https://example.org/']
OK   odnosnik zachowal adres
jezyk dokumentu /Lang: 'pl-PL'
OK   jezyk dokumentu jest polski
```

Nagłówek jest realnym `H1` w drzewie struktur (a nie wizualnie pogrubionym
akapitem), link zachował adres jako `/URI`, polskie znaki wyszły poprawnie,
a `/Lang` = `pl-PL` — czyli czytnik przeczyta dokument polskim głosem.

## Kontrola negatywna sondy PDF (bez tego punkt 6 nic nie znaczy)

Zielony wynik sondy jest bezwartościowy, jeśli sonda nie umie zgłosić błędu.
Wziąłem PDF z exe, usunąłem `/StructTreeRoot`, `/MarkInfo` i `/Lang`, i puściłem
**tę samą** sondę:

```
BLAD PDF nie jest tagged
BLAD brak /StructTreeRoot
BLAD brak struktury H1 / L / LI / Link / P
BLEDOW: 7    exit=1
```

Sonda różnicuje. Dodatkowo `logi/formats-harness.log` zawiera własne kontrole
negatywne harnessu (punkty 0, 10, 11: stary `.docx` bez nagłówka ZIP, surowy
wpis `md2rtf` dający urywek, `md2docx` z brakującym wzorcem).

## Niezależna kontrola artefaktów na dysku (poza asercjami harnessu)

Harness integracyjny sprawdza tylko flagę `Udane`. Otworzyłem pliki wyjściowe
własnym kodem — wszystkie formaty, 0 FAIL:

| plik | zmierzone |
|---|---|
| `wynik Żółty raport.docx` (10891 B) | PK; polskie litery w `word/document.xml`; nagłówek jako styl **Heading1**; `hyperlink`; lista `numPr`; **brak** surowego Markdowna |
| `legacy Żółty.docx` (10892 B) | to samo — stare INI użytkownika działa przez realny czytnik Ini |
| `wynik Żółty raport.epub` (5566 B) | PK; polskie litery; `<h1>`; `href="https://example.org…"`; `<li>` |
| `wynik Żółty raport.html` (4196 B) | polskie litery; `<h1>`; link; kompletny `<html>` + charset; brak surowego md |
| `wynik Żółty raport.rtf` (959 B) | zaczyna się od `{\rtf`; ma `fonttbl`; brak surowego md |
| `wynik Żółty raport.pdf` (42227 B) | podpis `%PDF-`; otagowany (wyżej) |

## Znalezione problemy (nie w produkcie — w oprzyrządowaniu testowym)

1. **Instrukcja uruchomienia w nagłówku `testy/harness_zapis_formatow.cs` jest
   martwa.** Podane tam polecenie
   `csc.exe /nologo /out:harness.exe harness_zapis_formatow.cs ZapisFormatow.cs`
   **nie kompiluje się** od czasu, gdy `ZapisFormatow.cs` zaczął używać
   `System.IO.Compression`:
   ```
   ZapisFormatow.cs(538,8): error CS0246: ... "ZipArchive" ...
   ZapisFormatow.cs(538,25): error CS0103: ... "ZipFile" ...
   ZapisFormatow.cs(540,1): error CS0246: ... "ZipArchiveEntry" ...
   ```
   Trzeba dodać `/reference:System.IO.Compression.dll` **i**
   `/reference:System.IO.Compression.FileSystem.dll` (tak jak robi
   `BuildEdSharp.cmd`). Do rozważenia: poprawić komentarz w harnessie.

2. **Harnessy mają zaszyte `C:\EdSharpSaveTest`** (`harness_zapis_formatow.cs`
   linie 38 i 360, `harness_zapis_bezpieczny_5114.cs` linie 8, 20, 25). Przy
   dwóch sesjach pracujących równolegle dwa przebiegi wchodzą sobie w ten sam
   katalog. `verify_save_bundle.sh` podmienia te ścieżki w **kopiach**
   w stagingu i przerywa, jeśli podmiana się nie uda.

3. **`tests/sprawdz_pdf.py` nie nadaje się do PDF-a z exe.** Asercjuje próbkę
   `PdfExportHarness` (nagłówek `H2`, adres `example.org/sciezka`), których
   fixture harnessu integracyjnego nie zawiera — użyta tam daje **fałszywy
   błąd** (7 rzekomych błędów przy w pełni poprawnym PDF). Punkt 6 ma własną
   sondę dopasowaną do fixture'u integracyjnego.

## Czystość produkcji

- W repo **nie zmieniłem żadnego pliku produkcyjnego.** Jedyny dodany plik to
  `testy/verify_save_bundle.sh` (nowy, nieśledzony, bez commita).
- `C:\EdSharpBuild` — tylko czytany (kopiowanie exe, dll, ini, pandoc.exe).
  `md5` binarki zmieniała wyłącznie sesja budująca.
- Jeden wyciek, naprawiony: pierwsza wersja `verify_save_bundle.sh` miała błąd
  cytowania w `sed`, więc podmiana ścieżek cicho nie zadziałała i przebieg
  o 17:15 zapisał katalogi robocze (`praca_harness`, `hardening`,
  `harness_zapis_formatow.txt`) do wspólnego `C:\EdSharpSaveTest`. To katalogi
  scratch, które harness i tak czyści przy każdym uruchomieniu — żadnych plików
  produkcyjnych ani niczego w `C:\EdSharpBuild`. `sed` zastąpiony Pythonem
  z asercją; ostatni przebieg potwierdzony jako w pełni izolowany
  (0 wystąpień `EdSharpSaveTest` w kopiach, brak zapisów do wspólnego katalogu).

## Do zrobienia po tej weryfikacji

- Mowa czytnika dla komunikatów zapisu/eksportu — **niezmierzona** (brak GUI w tej sesji).
- Sesja integrująca powinna przepuścić `testy/verify_save_bundle.sh` na
  finalnej binarce po zakończeniu pracy nad `OriginalDocument` — binarka
  ruszała się w trakcie tego pomiaru.
