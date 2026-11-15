# Niezależne potwierdzenie poprawek ustawień

Wynik: brak nowego blokera w badanym zakresie.
Delegacja: `deleg_f45dcdd0`, zadanie `sa-0-9655cdc5`.
Pełny zapis lokalny:
`/home/michal/.hermes/profiles/edsharp/cache/delegation/live/deleg_f45dcdd0/task-0.log`.

Recenzent sprawdził tylko kod i zachowane logi. Nie wykonywał sam testów,
GUI ani NVDA, nie zmieniał plików. Testy wykonawcze i pomiar czytnika są
własnymi pomiarami wykonawcy opisanymi w `docs/SYNONIMY-ODBIOR-5.0.123.md`.

## Rozstrzygnięcia

- P1: liczniki OK i Restore defaults zwiększają się tylko po udanym
  `App.WriteOption`. Nieudane zapisy mają listę etykiet i globalny komunikat.
  Recenzent potwierdził zmianę zarówno w kodzie, jak i porównaniu RED/GREEN.
- Fixture nie podstawia wyniku zapisu: prawdziwy formularz WinForms używa
  izolowanego INI tylko do odczytu. Kalibracja wykazuje odmowę natywnego
  `WritePrivateProfileString` przez rzeczywisty `App.WriteOption`.
- P2: runtime i lista Opcji są zgodne co do spacji, zewnętrznych cudzysłowów
  i wielkości liter dla identyfikatorów `pl`, `en`, `both`.
  Sześć dawniej czerwonych wariantów EN/BOTH jest zielonych.
- Nierozpoznany tryb pozostaje bezpiecznie polskim fallbackiem; istniejące
  własne wartości nie są usuwane przez samo otwarcie Opcji.

Pierwszy przegląd `deleg_527237b7` nie znalazł blokera routingu Right/F4,
Shift+Enter, Enter i Escape ani przywracania wyboru/fokusu po dziecku.
Zgłoszone wtedy dwie uwagi ustawień były powodem powyższego retestu.
Nie jest to audyt całego monolitu ani dowód gotowości publicznej publikacji.
