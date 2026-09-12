# Dziennik techniczny Hermesa - EdSharpNG

Tu ida szczegoly, ktore Michala nie interesuja w rozmowie: sciezki, sumy
kontrolne, numery wersji, przyczyny bledow. W Telegramie - tylko efekty.

Zalozony 12.09.2026 na prosbe Michala ("logi co gdzie do pliku tylko u siebie").

## Ustawienia Hermesa

Podglad polecen WYCISZONY na stale (12.09.2026), po trzech prosbach Michala:

    display.tool_progress: false        (bylo: all)
    display.tool_preview_length: 0
    display.tool_progress_command: false

Zmienione przez `hermes config set display.tool_progress off`, zweryfikowane
odczytem. Nie wlaczac z powrotem.

## Wydania

### 5.0.86 - 12.09.2026

Program sam zamyka sie przed aktualizacja.

    dist/EdSharpNG_Setup_5.0.86.exe
    3 401 172 B
    SHA-256: 2C1E5BAD9D55D3F0B096D872C682571C19AD4E303391D9B336394CCD6A709934

Zgloszenie: instalator pokazywal "Setup has detected that EdSharpNG is currently
running. Please close all instances of it now."

Przyczyna - dwa ROZNE mechanizmy Inno Setup, latwo pomylic:
- `AppMutex` sprawdzany na samym POCZATKU, przed pierwszym oknem instalatora;
  to on daje ten komunikat,
- `CloseApplications=force` dziala DUZO pozniej, na etapie "Preparing to install".
Skoro mutex `EdSharpNG_Running_Mutex` zyl przy starcie instalatora, pytanie
MUSIALO paść. Do 5.0.85 `ElevateVersion` odpalal instalator i program dzialal dalej.

Poprawka w `ElevateVersion` (EdSharp.cs): instalator uruchamiany przez cmd.exe
z opoznieniem, program natychmiast wychodzi przez `ExitApp()`:

    cmd.exe /c ping -n 5 127.0.0.1 >nul & start "" "<instalator>"

`ping`, nie `timeout.exe` - timeout wymaga prawdziwej konsoli i w tle potrafi paść.
Opoznienie ~4 s to zapas na zwolnienie mutexu i zapis ustawien; bez niego wyscig.
Kolejnosc celowa: najpierw `ExitApp` (pyta o zapis niezapisanych plikow), a gdy
uzytkownik anuluje - aktualizacja przerwana, program zostaje, komunikat mowi
gdzie lezy instalator. Zapasowa droga bez opoznienia, gdy cmd.exe zawiedzie.

UWAGA: przejscie 5.0.85 -> 5.0.86 jeszcze pokaze stare pytanie (poprawka jest
w nowej wersji). Dopiero kolejne aktualizacje sa gladkie.

### 5.0.85 - 12.09.2026

Zadanie 8: automatyczne dociaganie skladnikow Convert.

    dist/EdSharpNG_Setup_5.0.85.exe
    3 398 028 B
    SHA-256: 6C53E1FB316894B0CB470BF3619B10BDC9D9B7829288DAC3E05D91B22209E5B3

Nowy plik `Skladniki.cs`. Narzedzia Convert (Pandoc 42 MB, Tidy, Xpdf, Liblouis,
Astyle) nie sa w instalatorze - to obce programy, razem ponad 60 MB. Do 5.0.84
pobieral je TYLKO skrypt budowania, wiec uzytkownik ich nigdy nie mial i
konwersje po cichu padaly.

- start: `CheckComponentsOnStartup()` w watku tla, tylko BRAKUJACE, raz na dobe,
  bez okien; wylacznik w ini `CheckComponentsOnStartup=N`,
- nie aktualizuje tego, co dziala (zmiana dzialajacego narzedzia bez wiedzy
  uzytkownika = zle),
- na zadanie: menu Help > "Update Components" (bez skrotu, zeby nic nie kolidowalo),
- komunikat nieudanej konwersji podaje NAZWE brakujacego narzedzia; do 5.0.84
  pokazywal sam wiersz polecenia,
- restart programu niepotrzebny - narzedzia to osobne exe.

Pomiary: `testy/pomiar_skladniki.cs`, `testy/pomiar_skladniki_w_programie.cs`,
`testy/pomiar_skladniki_brzegowe.cs`. Pandoc 42 MB pobrany w 6,2 s i uruchamia sie
(pandoc 3.11); bez internetu program milczy 10,2 s i nie psuje istniejacego Tidy.

### 5.0.84 - 12.09.2026

    3 391 717 B
    SHA-256: A0BB2934E69C85CE71106B72ED0779B78496CE34F076554DB9C3AD14351D8AF0

Polski slownik bez Worda (Windows Spell Checking API, pl-PL); czytnik czyta tresc
okienek MessageBox; suma SHA-256 zawsze w opisie wydania.

### Starsze

    5.0.83  3 387 249 B  84475E47A36593E1555871E0930D7F320AF456EEDDB98903E21D95A0DC9DDAB9
    5.0.82  3 384 288 B  C433FB347C2E8EA7DCED7DA306E1F92E8D7383AFBCDED9AAA59F2F9607538704
    5.0.81  3 381 613 B  A7D544D42B5D16E4EE0087655FA18B49BA054C68F23C0C9BF84B78DB8423446C

## Pulapki warte zapamietania

**TLS 1.2 jawnie.** Przed kazdym pobraniem w .NET Framework 4.x:
`ServicePointManager.SecurityProtocol = Tls12 | Tls11 | Tls;` plus TLS 1.3 liczba
`(SecurityProtocolType) 12288` (nazwa nie istnieje w tej wersji .NET). Bez tego
pobieranie pada w 0,2 s, a objaw jest nieodrozninalny od braku internetu.

**App.ProgramDir bywa puste.** Ustawiane w trakcie startu; kod z watku tla dostanie
pusty string, `Path.Combine` rzuci wyjatek, ten poleci do `catch` - awaria po cichu.
Zapas: `Assembly.GetExecutingAssembly().Location`, potem `Directory.GetCurrentDirectory()`.

**zbuduj.sh meldowal falszywy sukces.** `build_installer_garfield.sh` odmawia
nadpisania paczki o istniejacym numerze, ale konczy kodem 0; skrypt sprawdzal tylko
istnienie pliku, wiec pisal GOTOWE z suma STAREJ paczki. Naprawione: `rm` starej
paczki przed pakowaniem plus `[ "$PACZKA" -ot "$BUILD/EdSharpNG.exe" ]` -> exit 10.
Zasada: przy przebudowie tej samej wersji sprawdzaj DATE i SUME, nie istnienie pliku.

## Niezalatwione

- Kopie 5.0.85 i 5.0.86 NIE wgrane do `D:\Projekty Codex\Hermes` - SSH na
  michal-glowny nie odpowiada (port 22 zamkniety, sshd zatrzymana). Wgrac, gdy wroci.
- Zadanie 2: przeglad nowosci u Jamala (EmpowermentZone/EdSharp). Tarball sie urywal;
  klonowanie do /tmp/up5/orig.
- Zadanie 10: pliki CSV jako tabela - w toku.
