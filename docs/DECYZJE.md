# Rozstrzygnięcia projektowe

Każdy wpis: **co ustalono**, **dlaczego**, **kiedy**. Uzasadnienie jest tu
ważniejsze od samego ustalenia — bez niego następna osoba (albo ten sam
człowiek pół roku później) podważy decyzję i przejdzie tę samą drogę od zera.

Decyzje właściciela projektu są oznaczone jego nazwiskiem. Tych nie zmienia się
z własnej inicjatywy.

---

## Dostępność i interfejs

### Kontrolka niosąca swój stan zamiast przełącznika i komunikatu

Pole wyboru (`CheckBox`) czytnik ekranu odczytuje razem ze stanem
(„zaznaczone" / „niezaznaczone"). Pozycja menu działająca jak przełącznik stanu
**nie niesie** — wymaga osobnego komunikatu mówionego po każdym użyciu, a i tak
nie da się jej sprawdzić bez naciśnięcia.

Wniosek: gdy ustawienie ma stan, ma być polem wyboru w oknie, nie przełącznikiem
w menu.

*12.09.2026*

### Nowe ustawienie dostaje własne okno, nie klucz w pliku

Kasperczak, po zobaczeniu wersji 5.0.93: *„Ja w ogóle nie chciałbym żeby
cokolwiek tam trzeba było robić w pliku ustawień. Chciałbym żeby do tych
ustawień normalnie był dostęp."*

Uwaga istotna: **okno Configuration Options też się nie liczy**. To lista
około 30 pól tekstowych z jednowyrazowymi angielskimi nazwami — czyli ten sam
plik ustawień, tylko wyświetlony. „Normalny dostęp" oznacza okno z polami
wyboru i opisami pełnymi zdaniami.

Wzorzec do naśladowania (menu Misc → Work Continuity, wersja 5.0.94):

```csharp
LbcDialog dlg = new LbcDialog("Work Continuity", this);
dlg.addLabel("EdSharp can bring your work back after a restart, a crash, or a power cut.");
CheckBox cb = dlg.addCheckBox("Restore &open files and cursor positions on startup", stan, podpowiedz);
NumericUpDown nud = dlg.addNumericUpDown("&Seconds between auto saves", teraz, 5, 3600, podpowiedz);
if (!dlg.runOkCancel()) { dlg.Dispose(); return; }
```

*12.09.2026*

### Pole liczbowe z granicami zamiast pola tekstowego

`NumericUpDown` ma granice wbudowane, więc nie istnieje wartość, którą program
musiałby po cichu poprawiać. Ciche poprawianie wpisanej liczby jest zawsze
niespodzianką, a przy czytniku ekranu — niespodzianką niewidoczną.

*12.09.2026*

### Skróty Ctrl+Alt+litera są zakazane

Prawy Alt (AltGr) na klawiaturze polskiej to fizycznie Ctrl+Alt. Skrót
`Ctrl+Alt+litera` **zjada polskie znaki diakrytyczne** — użytkownik traci
możliwość napisania „ą" albo „ł".

Dotyczy każdej aplikacji pisanej dla tego użytkownika, nie tylko EdSharpa.
Kolizje skrótów sprawdza się u źródła (w kodzie **i** w `Hotkeys.ini`), osobno
w każdym miejscu czytającym modyfikatory.

*ustalenie stałe*

### Escape zamyka każde okno

Globalna obsługa w `Lbc.cs`, niezależna od nazwy przycisku:

```csharp
KeyDown += (sender, e) => { if (e.KeyCode == Keys.Escape) frm.Close(); };
```

Wcześniej Escape działał tylko tam, gdzie ktoś pamiętał o przypisaniu go do
przycisku Anuluj. Okno bez tego jest pułapką: użytkownik nie ma jak wyjść bez
szukania przycisku.

*12.09.2026, Kasperczak: „na stałe, wszędzie"*

### Komunikaty programu zostają po angielsku aż do pełnego tłumaczenia

Mieszanie języków w interfejsie jest gorsze niż konsekwentnie jedno albo drugie.
Polska wersja interfejsu jest ostatnim punktem mapy drogowej.

*18.08.2026, decyzja Kasperczaka*

### Strzałka w lewo na liście zakładek czyta samą treść wiersza

Bez numeru linii. Pusty wiersz odczytywany jako „Empty line".

*12.09.2026*

---

## Ciągłość pracy (wersja 5.0.93–5.0.94)

### Obie funkcje domyślnie wyłączone

Kasperczak: *„trzeba będzie jakoś włączyć i wyłączyć bo nie każdy może sobie
czegoś takiego życzyć, tak samo jak auto zapisu"*.

Klucze w `[Options]`: `RestoreSession="N"`, `AutoSaveSeconds="0"`.

*12.09.2026*

### Autozapis nie dotyka pliku użytkownika

Kopia idzie do `<DataDir>\Odzysk\*.odzysk`. Autozapis piszący do pliku
źródłowego odebrałby możliwość porzucenia zmian przez „nie zapisuj" — czyli
odwołania całej pracy nad dokumentem. Tak samo robi Word.

### Jedna wartość liczbowa zamiast włącznika i częstotliwości osobno

`AutoSaveSeconds="0"` znaczy „wyłączone". Dwa osobne klucze pozwalałyby na stan
sprzeczny: „włączone, co 0 sekund".

### Zapis stanu atomowy

Plik tymczasowy, potem `File.Move`. Plik sesji uszkodzony w połowie jest
**gorszy od braku pliku**: program wstaje z połową okien i bez ostrzeżenia, więc
użytkownik nie wie, że czegoś brakuje.

### Własny skrót do nazw kopii, nie String.GetHashCode

`String.GetHashCode` w .NET **nie gwarantuje tego samego wyniku między
procesami**. Kopie mnożyłyby się przy każdym uruchomieniu programu. Użyto
FNV-1a liczonego własnym kodem.

### Jeden zegar na cały program

`timerCiaglosciPracy` w `MdiFrame`, nie po jednym na okno dokumentu. Dwadzieścia
otwartych dokumentów to dwadzieścia tyknięć jednego zegara, nie dwadzieścia
zegarów. `Windows.Forms.Timer`, czyli wątek interfejsu — świadomie, bo obsługa
czyta treść kontrolek.

### Zmiana ustawień działa natychmiast

`UstawCiaglosc()` przekłada zegar od razu po zatwierdzeniu okna. Bez tego
włączenie funkcji zadziałałoby dopiero po restarcie programu, czego nikt się nie
domyśli.

### Wyłączenie funkcji usuwa sesję i wszystkie kopie

Trzymanie fragmentów dokumentów po wyłączeniu byłoby przechowywaniem tekstu
użytkownika bez jego zgody.

### Pytanie przy starcie tylko wtedy, gdy jest co odzyskiwać

Gdy wszystkie pliki istnieją i nic nie było zmienione — program otwiera je bez
pytania, bo pytanie byłoby ceremonią bez wyboru. Treść pytania podaje **ile
plików i z kiedy**; przy czytniku ekranu to jedyne źródło tej wiedzy.

### Puste okno „NoName" powstaje na końcu i warunkowo

Tylko gdy `MdiChildren.Length == 0`. Do wersji 5.0.92 powstawało bezwarunkowo
jako pierwsza rzecz przy starcie, więc po przywróceniu sesji użytkownik
dostawał swoją pracę **oraz** puste okno do zamknięcia.

---

## Techniczne

### Jeden `csc`, zero zewnętrznych bibliotek

Program kompiluje się jednym wywołaniem kompilatora, bez menedżera pakietów.
Dlatego `Csv.cs` ma własny czytnik CSV wg RFC 4180, a `Pisownia.cs` woła
Windows Spell Checking API przez COM zamiast używać gotowej biblioteki.

Koszt: więcej kodu do napisania. Zysk: program buduje się na czystej instalacji
Windows, bez sieci i bez przygotowania środowiska.

### Suma kontrolna w opisie każdego wydania

Program czyta sumę SHA-256 z opisu wydania na GitHubie i bez niej nie potwierdzi,
że pobrana paczka jest prawdziwa. Przy wersjach 5.0.82 i 5.0.83 suma nie została
dopisana, więc instalator pytał użytkownika *„Release does not publish
a checksum… Run it anyway?"*.

Naprawione na stałe: skrypt `wydaj.sh` **sam liczy sumę z pliku** i sprawdza, że
naprawdę znalazła się w opisie wydania.

*12.09.2026*

### Autorstwo oryginału zostaje

Licencja LGPL tego wymaga. Adres <jamal@EmpowermentZone.com> jest publiczny
i podany celowo — nie usuwać.

Osobno: w repozytorium przy upublicznianiu znalazło się 338 odziedziczonych
plików `.txt` zawierających 156 prawdziwych adresów e-mail z prywatnej
korespondencji. Zostały usunięte. Przed każdym upublicznieniem **sprawdza się
zawartość**, nie tylko kod.

*11.09.2026*

### Sprawdzanie oryginału odbywa się po naszej stronie

Zadanie śledzenia zmian w oryginale Jamala realizuje zadanie cykliczne co
niedzielę o 10:00 na maszynie roboczej. Nie wymaga niczego od użytkownika
i milczy, gdy nie ma zmian.

*12.09.2026, Kasperczak*

### Quill for All jest osobnym projektem

`Community-Access/quill` — nie jest gałęzią Jamala. Do obserwacji pod kątem
pomysłów, nie do przenoszenia kodu.
