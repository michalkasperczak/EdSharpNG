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

### 5.0.112 - 17.09.2026

Listy zadan (checklisty Markdown) - zlecenie MK z 16.09.2026: "Przed
komentarzami musimy zrobic obsluge Checklisty markdown. To nie jest trudne,
zaproponuj jak."

    dist/EdSharpNG_Setup_5.0.112.exe
    3 513 738 B
    sha256 e3720ac50af60bb11c8dcb7330ca8060e95ba7307e1171c6abdf3e2e39e652e8

PULAPKA PAKOWANIA (znowu, jak przy 5.0.111): build_installer_garfield.sh robi
staging z listy `git ls-files`, wiec NOWY plik zrodlowy niedodany jeszcze do
gita wypada z paczki PO CICHU - krok [4/6] go nie zglasza, bo sprawdza tylko
pliki wymienione w .iss z flaga bez skipifsourcedoesntexist. Zadania.cs trafil
do instalatora dopiero po `git add`. Kolejnosc jest wiec: commit, POTEM
instalator, a nie odwrotnie.

Nowy plik Zadania.cs (funkcje czyste: rozpoznanie skladni GFM "- [ ]" / "- [x]",
przelaczanie stanu, tworzenie i zdejmowanie pola, postep, podpisy dla czytnika).
Dopisany do listy zrodel w BuildEdSharp.cmd.

Cztery komendy: Control+Shift+X przelacza zrobione/niezrobione, Control+Shift+F2
robi z wierszy checkliste i z powrotem, Control+Shift+F7 otwiera okno listy
zadan (spacja przelacza stan bez wychodzenia), Alt+Shift+F2 mowi postep.

RELOKACJE SKROTOW wymuszone tym wydaniem:
- paleta polecen: Control+Shift+X -> Control+Shift+F1 (decyzja MK "Paleta
  CTRL-Shift-F1"),
- samouczek: Control+Shift+F1 -> Control+Alt+F1 (zwalnia miejsce palecie).

ODRZUCONA PROPOZYCJA MK: Control+Alt+X i Control+Alt+Shift+X na pozostale dwie
komendy. X ma na polskim ukladzie odpowiednik pod prawym Altem, a zmierzone
13.09.2026 (spor_ctrl_alt_s.ps1) - na chordzie dzielonym z polska litera wygrywa
PISANIE, wiec skrot bylby bez skutku. Stad klawisze funkcyjne, ktore wariantu z
ogonkiem nie maja.

SZESC ISTNIEJACYCH MIEJSC POPRAWIONYCH, bo kazda pozycja checklisty pasuje
TAKZE do wzorca zwyklego punktora (pytac trzeba NAJPIERW o checkliste):
- Enter kontynuujacy liste: dopisuje "- [ ] ", nowa pozycja zawsze niezrobiona
  (nie dziedziczy [x]),
- Control+L i Control+Shift+L: zdejmuja pole razem ze znacznikiem, inaczej w
  tekscie zostawalby goly "[ ] tresc",
- kopiowanie do Worda: to samo,
- podglad Markdown: pole zamienione na SLOWO "[done]"/"[to do]", nie usuniete w
  cisze (podglad jest kontrolka tekstowa, prawdziwego pola wyboru tam nie ma),
- nazwa sekcji: pole zdejmowane przed punktorem,
- eksport HTML: prawdziwe <input type="checkbox" disabled> z <label for> i
  wlasnym id, zamiast nawiasow w tresci.

POMIARY:
- testy/pomiar_zadania_611.cs - 64/64 OK (funkcje czyste, goly csc, bez Windows),
- testy/pomiar_zadania_zywe.ps1 - 6/6 OK na zywym programie: brak alarmu
  kolizji klawiszy, Control+Shift+X zmienia PLIK na dysku, drugie nacisniecie
  wraca (nie jest komenda jednokierunkowa), kontrola: na wierszu bez zadania
  plik sie NIE zmienia,
- testy/pomiar_zadania_enter_html.ps1 - 3/3 OK: Enter dopisuje pozycje
  niezrobiona, nie dziedziczy stanu, kontrola: zwykle punktory dzialaja dalej,
- testy/pomiar_html_checklisty.ps1 - 6/6 OK (refleksja na binarce, bez GUI),
- zywy NVDA przez mostek MCP: okno czyta "to do: kupic chleb", tytul niesie
  "Task List - 1 of 3 done, 33 percent", po spacji "done: kupic chleb", zmiana
  zapisana do pliku.

PULAPKA SOND KLAWIATUROWYCH (kosztowala trzy falszywe wyniki w tej sesji):
- SendKeys idzie do okna AKTYWNEGO, a program uruchomiony z WSL nim nie jest;
  samo SetForegroundWindow NIE WYSTARCZA - trzeba AttachThreadInput, jak w
  testy/na_wierzch.ps1. Dlatego kazda taka sonda ma teraz KONTROLE POZYTYWNA
  (wpisz znak, sprawdz, ze wszedl) i przerywa, gdy klawisze nie dochodza -
  bez niej sonda pokazywala "komenda nie dziala" przy dzialajacym kodzie,
- Control+End laduje na PUSTYM wierszu za tekstem, nie na ostatnim wierszu z
  trescia. Dwie asercje mierzyly przez to pusty wiersz i falszywie oskarzyly
  program o zepsute listy.

### 5.0.111 - 16.09.2026

Wykonanie decyzji z docs/CO-USUWAMY.md i docs/OPCJE-USTAWIEN.md.

    dist/EdSharpNG_Setup_5.0.111.exe
    3 500 492 B (rozmiar zalacznika wydania odczytany przez gh)

Zmiana architektury: warstwa skryptow JScript .NET USUNIETA. Znikly EdSharp.dll,
EdSharp.js, krok jsc.exe w BuildEdSharp.cmd i late binding przez
Assembly.LoadFrom. Liczenie wyrazen i rozwijanie sekwencji z backslashem
przeniesione do nowego pliku Wyrazenia.cs (432 wiersze, wlasny parser).

PULAPKI ZMIERZONE PRZY TEJ ZMIANIE (bez nich build padal cicho):
- build_installer_garfield.sh mial TRZY miejsca z EdSharp.dll/EdSharp.js:
  warunek swiezosci binarki (-nt EdSharp.js), kontrola [[ -s EdSharp.dll ]]
  i cp do stagingu. Kazde z nich przerywalo pakowanie PO udanej kompilacji,
  komunikatem mowiacym o czyms innym ("build nie utworzyl EdSharpNG.exe"),
- zbuduj.sh kopiowal EdSharp.dll do repo bezwarunkowo (exit 8),
- EdSharp_Setup.iss pakowal EdSharp.js z flaga ignoreversion (bez
  skipifsourcedoesntexist), czyli brak pliku = blad Inno Setup. Wpis w
  [UninstallDelete] dla EdSharp.dll ZOSTAJE celowo, zeby plik z poprzednich
  instalacji zniknal przy odinstalowaniu.
- do .iss dopisane brakujace zrodla: Sesja.cs, Ustawienia.cs, Wyrazenia.cs
  (paczka wozi zrodla, zeby program dal sie przekompilowac u uzytkownika).

### 5.0.110 - numer PRZESKOCZONY

Numer 5.0.110 nie zostal wydany. Dwie sesje (Telegram i BlindPilot) pracowaly
rownolegle w tym samym repozytorium: jedna podniosla wersje na 5.0.110, druga
w tym samym czasie na 5.0.111 i to ona doszla do wydania. LEKCJA: przed
podniesieniem numeru wersji sprawdzic, czy w repo nie pracuje druga sesja
(ps -eo cmd | grep zbuduj.sh oraz git log -1).

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
