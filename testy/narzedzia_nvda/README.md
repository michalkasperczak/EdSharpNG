# Narzędzia do pomiaru dostępności EdSharpNG przez żywe NVDA

Do 18.08.2026 lista testów mówiła „nie mam czym zmierzyć, co wymówi czytnik".
Od 19.08.2026 mam czym. Tu leżą narzędzia, żeby kolejna sesja nie budowała
tego od zera.

## Co jest potrzebne

- Windows z NVDA (sprawdzone na 2026.1.1) dostępny z WSL przez `/mnt/c`.
- Mostek NVDA MCP (dodatek `nvdaMcpBridge`) słuchający na `127.0.0.1:8765`.
  Token domyślny: `nvda-bridge-secret-change-me`, nagłówek `X-NVDA-Bridge-Token`.

## Kolejność uruchamiania

```bash
# 1. warunek wstępny: NVDA żyje, EdSharp NIE jest uruchomiony, nikt nie pracuje
/mnt/c/Windows/System32/tasklist.exe /FI "IMAGENAME eq nvda.exe"

# 2. wgraj dodatek podsłuchu mowy i zrestartuj NVDA przez mostek
cp -r podsluchMowy /mnt/c/Users/<user>/AppData/Roaming/nvda/addons/
#    potem: nvda_restart_nvda przez MCP

# 3. uruchom EdSharpa z plikiem testowym i zmierz
python3 zmierz_drzewo_f6.py     # struktura drzewa F6 (testy 1.9, kompletność)
python3 zmierz_mowe_nvda.py     # co NVDA REALNIE wymawia (testy 1.2, 1.5)
```

Log mowy: `%TEMP%\nvda_mowa.log` (znacznik czasu + treść każdej wypowiedzi).

## Pułapki, które kosztowały pół sesji

1. **`SetForegroundWindow` i `AppActivate` NIE DAJĄ FOKUSU KLAWIATURY** z procesu
   w tle. Oba zwracają sukces, okno idzie na wierzch, a klawisze lecą dalej do
   konsoli WSL, czyli do sesji użytkownika. Nie pomaga ani `ForegroundLockTimeout=0`,
   ani trik z ALT, ani `AttachThreadInput`. Jedyne, co działa: syntetyczne
   kliknięcie w obszar tekstu (`aktywuj_okno.ps1`, przywraca pozycję myszy).
   **„Okno na wierzchu" i „okno ma fokus klawiatury" to dwie różne rzeczy — sprawdzaj obie.**

2. **NVDA zapisuje `nvda.ini` przy ZAMYKANIU, ze stanu w pamięci.** Edycja pliku
   przy działającym NVDA jest bezcelowa: restart nadpisze plik i wpis zniknie bez
   śladu. Dlatego podsłuch jest DODATKIEM (`addons/`), a nie wtyczką w scratchpadzie
   (ta wymaga `enableScratchpadDir` w ini).

3. **Monkey-patch `speech.speak` nie łapie nic** w NVDA 2026 — moduły trzymają
   własne referencje. Działa `speech.extensions.pre_speechQueued` oraz łatka na
   `speech.speech.speak` (pełna ścieżka modułu, nie pakiet).

4. **UIA widzi drzewo WinForms jako płaski `Pane`** bez struktury — NVDA czyta je
   przez MSAA. Do poziomów nagłówków używaj mowy NVDA albo `get_current_focus`
   po klawiszach, nie `TreeWalker`.

5. **Pauza ~0,45 s po każdym klawiszu jest obowiązkowa.** Bez niej czytasz fokus,
   zanim drzewo się przestawi — pierwszy przebieg zebrał 1 pozycję zamiast 7
   i wyglądało to jak defekt programu.

6. **`tasklist.exe` i `powershell.exe` nie są w PATH z WSL.** Bez pełnej ścieżki
   dostajesz „command not found", co łatwo odczytać jako „proces nie istnieje".

## Czego tą drogą NIE zmierzysz

Testów 3.1–3.5 (zdania, słowa, akapity). Klawisze wysłane przez `send_keys`
mostka przesuwają kursor (widać to w `read_current_line`), ale nie wywołują
u NVDA reakcji mowy na ruch karetki. **Kontrola pozytywna to rozstrzyga:** zwykła
strzałka w dół, która MUSI mówić w każdym edytorze, też milczy. Więc to
ograniczenie pomiaru, nie wada EdSharpa — nie zgłaszaj tego jako defektu.
Do zrobienia kiedyś: wysyłanie klawiszy przez systemowy `SendInput` zamiast
przez NVDA.
