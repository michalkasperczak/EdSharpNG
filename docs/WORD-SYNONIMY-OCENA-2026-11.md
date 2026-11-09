# Microsoft Word jako opcjonalny provider polskich synonimów — rozpoznanie

## Wniosek

**Technicznie jest to wykonalne jako lokalny, interaktywny provider COM**, ale tylko warunkowo: program łączy się z już uruchomionym Wordem na tym samym pulpicie użytkownika i pyta `Application.SynonymInfo` o słowo oraz `wdPolish = 1045`.[^1][^2][^3] Nie ma podstaw, by deklarować, że słownik Worda jest bogatszy od obecnego indeksu plWordNet — tego nie zmierzono na docelowej instalacji.

**Nie ma tu jednoznacznej, ogólnej zgody licencyjnej Microsoftu dla planowanego scenariusza.** Oficjalne materiały potwierdzają automatyzację Worda z aplikacji klienckiej oraz zakaz/ryzyko automatyzacji nieinteraktywnej i serwerowej, lecz nie znalazłem warunku licencyjnego, który wprost zezwalałby niezależnej aplikacji na odczyt i prezentację wyników tezaurusa w każdej edycji Office.[^4][^5] Dlatego można przygotować rozwiązanie o małym ryzyku redystrybucji, ale wydanie powinno być zależne od sprawdzenia **konkretnych** warunków licencji Office użytkownika (szczególnie licencji firmowej/volume) przez jej posiadacza lub prawnika. To nie jest opinia prawna.

## Co potwierdzają dokumenty Microsoftu

1. `SynonymInfo` reprezentuje informacje o synonimach, antonimach, wyrazach i wyrażeniach pokrewnych dla podanego słowa albo zakresu. Gdy obiekt pochodzi z Worda, przekazuje się słowo/frazę i identyfikator języka sprawdzania.[^1]
2. `WdLanguageID.wdPolish` ma wartość **1045**.[^2][^3]
3. `Found` nie znaczy „znaleziono synonimy”: może być prawdziwe także wyłącznie dla antonimów albo wyrazów/wyrażeń pokrewnych. Trzeba odczytać `MeaningCount` i konkretne `SynonymList`, a pusty wynik obsłużyć jako brak synonimów.[^1][^6]
4. `MeaningList` zwraca tablicę tekstów znaczeń; `SynonymList(meaning)` zwraca tablicę synonimów danego znaczenia. `PartOfSpeechList` odpowiada pozycjami elementom `MeaningList`.[^7][^8][^9]
5. Microsoft dokumentuje uzyskanie działającej instancji COM z ROT przez `Marshal.GetActiveObject("Word.Application")`; przykład wyraźnie kończy się wyjątkiem, gdy aplikacja nie działa. To pozwala nie uruchamiać Worda.[^10]
6. Word jest produktem dla interaktywnego pulpitu i profilu użytkownika. Microsoft nie rekomenduje ani nie wspiera automatyzacji nieinteraktywnej/serwerowej (m.in. ASP, usługi, DCOM); wskazuje również ograniczenia STA i współbieżności.[^4][^5]

## Zalecana propozycja produktu

### Zachowanie poleceń

- **`Shift+F7` pozostaje bezwarunkowo lokalny.** Bieżący kod `EdSharp.cs:8073–8131` czyta wyłącznie `Synonyms\\plwordnet-synonyms.txt`; nie wywołuje w tej metodzie COM ani Worda. Nie należy zmieniać tego kontraktu.
- Dodać osobne, domyślnie bez skrótu polecenie, np. **„Synonimy z otwartego Microsoft Worda…”**. Nie uruchamia się ono po zwykłym `Shift+F7` ani jako fallback po braku lokalnego wyniku.
- Provider wykonuje wyłącznie `Marshal.GetActiveObject("Word.Application")`. Jeżeli Word nie działa, komunikat brzmi: **„Microsoft Word nie jest otwarty; użyto tylko lokalnego słownika.”** Nie używać `CreateObject`, `Activator.CreateInstance`, istniejącego `COM.WordAccess` ani żadnej ścieżki „get or create”.
- Nie tworzyć dokumentu, nie aktywować okna Worda, nie pisać do zaznaczenia, nie zmieniać `Visible`, `DisplayAlerts` ani nie zamykać procesu. Provider ma być zapytaniem tylko do odczytu wobec działającej instancji.
- Opcja powinna być wyraźnie opisana: „Wymaga uruchomionego, licencjonowanego Microsoft Worda oraz dostępnych narzędzi języka polskiego. Wynik zależy od instalacji Worda.” W ustawieniach ma być domyślnie wyłączona.

### Kontrakt techniczny `.NET Framework 4.8 / WinForms`

Osobny interfejs, niezależny od obecnego parsera danych, może mieć postać konceptualną:

```text
ISynonymProvider.TryLookup(word) -> ProviderResult
LocalPlWordNetProvider             -> obecne Shift+F7
RunningWordProvider                -> jawne polecenie użytkownika
```

`RunningWordProvider` powinien:

1. pracować na wątku STA WinForms i serializować wywołania; Office jest automatyzacją STA i nie jest serwerem wieloklientowym,[^4]
2. uzyskać wyłącznie działającą instancję `Word.Application` z ROT,
3. wywołać `SynonymInfo(word, 1045)`, sprawdzić `MeaningCount`, pobrać `MeaningList`, `PartOfSpeechList` i dla każdego indeksu `SynonymList(i)`,
4. znormalizować tylko bieżący wynik do modeli w pamięci, usunąć duplikaty i wynik identyczny ze słowem wejściowym,
5. zachować obecną dostępnościową kolejność płaskiej listy: **„synonim, część mowy, pozycja z liczby”**. Grupowanie według znaczeń służy wyłącznie pobraniu danych; etykiet „znaczenie 1” nie trzeba dodawać do każdej pozycji,
6. zawsze zwolnić własne referencje COM i nie wywoływać `Quit` — instancja należy do użytkownika.

Do wdrożenia preferowany jest **late binding** (`object`/IDispatch), aby EdSharpNG nie dystrybuował `Microsoft.Office.Interop.Word.dll` ani nie uzależniał uruchomienia aplikacji od PIA. Jest to decyzja architektoniczna, nie stwierdzenie o licencji PIA. Wariant early binding można rozważyć dopiero po osobnej weryfikacji wymaganych artefaktów i ich redystrybucji.

### Granice danych i licencji

Projekt minimalizujący ryzyko powinien spełniać wszystkie poniższe warunki:

- Word jest **zainstalowany, aktywowany i licencjonowany dla tego użytkownika/urządzenia**; EdSharpNG go nie instaluje, nie aktywuje i nie dostarcza żadnej części Office.
- Nie kopiować, nie eksportować, nie cache’ować na dysku, nie indeksować i nie wysyłać baz, plików językowych ani masowych odpowiedzi Worda. Odpowiedź jest ulotna i służy tylko temu jednemu, jawnemu zapytaniu użytkownika.
- Nie udostępniać providerem Worda innym urządzeniom, kontom, procesom serwerowym ani użytkownikom bez własnego Office. Oficjalny artykuł wskazuje, że licencjonowanie nie pokrywa użycia Office na serwerze do obsługi żądań klientów bez licencjonowanych kopii Office.[^5]
- Nie stosować automatyzacji w tle, harmonogramie z kontem SYSTEM, usłudze Windows, RDP jako wspólnego silnika ani przy wylogowanym użytkowniku.[^4][^5]
- Do dokumentacji funkcji dodać, że jest integracją z **lokalnie posiadaną kopią Worda**, nie dostarcza słownika Worda i nie jest substytutem licencji Office.

Aktualne warunki Office 2024 Retail są tylko przykładem kanału licencyjnego: same wskazują, że klient volume podlega umowie jego pracodawcy, a warunki dodatkowe mogą mieć zastosowanie.[^11] Nie należy przenosić wniosku z tego dokumentu na Microsoft 365, OEM lub licencję zbiorczą bez ich sprawdzenia.

## Wersje i warunki wsparcia

**Proponowana deklaracja na dziś:** „Eksperymentalny provider dla uruchomionego desktopowego Microsoft Word dla Windows, który w preflight zwraca `SynonymInfo` dla polskiego (`1045`).” Nie deklarować jeszcze listy wersji Worda jako wspieranych.

Oficjalne strony `SynonymInfo`/PIA udokumentowały API, ale nie dają tabeli kompatybilności konkretnych wydań Worda; rekord PIA mówi tylko „Latest”.[^1][^7] W artykule o automatyzacji serwerowej wymieniono Word 2010, 2013, 2016 i Office 365 w zakresie, którego dotyczy ostrzeżenie — nie jest to certyfikat funkcji dla tezaurusa.[^5]

Po implementacji wspierać konkretną wersję dopiero po teście na jej realnej instalacji, z polskimi zasobami językowymi, co najmniej na słowach `dom`, `ważny`, `szybki`, słowie nieznanym i słowie wieloznacznym. Kryterium nie jest sama obecność Worda, lecz brak uruchomienia nowego procesu, brak modyfikacji dokumentu Worda, poprawne obsłużenie braku danych oraz dostępna obsługa listy przez NVDA. Nie testowano tu żadnej wersji Worda ani pokrycia polskiego słownika.

## Stan sprawdzony lokalnie i ograniczenia badania

- W kopii repozytorium `/home/michal/projekty/edsharp` bieżąca metoda `Thesaurus()` czyta lokalny indeks; nie ma w niej wywołania Word COM. Inne fragmenty programu nadal używają `COM.WordAccess`, m.in. sprawdzanie pisowni, więc nowy provider nie może współdzielić helpera, który tworzy Worda.
- Na tej maszynie roboczej odczyty rejestru `HKCR\\Word.Application`, `HKLM\\SOFTWARE\\Microsoft\\Office` (także WOW6432Node) oraz typowych ścieżek `WINWORD.EXE` nie wykazały Worda. Nie uruchamiałem COM Word, nie instalowałem ani nie zmieniałem Office i nie badałem komputera głównego.
- Nie zweryfikowano: licencji docelowego Worda, aktywacji, obecności polskiego tezaurusa, rzeczywistych wyników dla polskich słów, zachowania w danej wersji/bitowości Office, ani tego, czy dostawca danych słownika w konkretnej edycji ma odrębne warunki.

## Źródła

[^1]: [Microsoft Learn — SynonymInfo object (Word)](https://learn.microsoft.com/en-us/office/vba/api/word.synonyminfo)
[^2]: [Microsoft Learn — WdLanguageID enumeration (Word)](https://learn.microsoft.com/en-us/office/vba/api/word.wdlanguageid)
[^3]: [Microsoft Learn — WdLanguageID Enum (Microsoft.Office.Interop.Word)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.office.interop.word.wdlanguageid?view=word-pia)
[^4]: [Microsoft Learn — Considerations for unattended automation of Office in Microsoft 365 for unattended RPA](https://learn.microsoft.com/en-us/office/client-developer/integration/considerations-unattended-automation-office-microsoft-365-for-unattended-rpa)
[^5]: [Microsoft Support — Considerations for server-side Automation of Office](https://support.microsoft.com/en-us/topic/considerations-for-server-side-automation-of-office-48bcfe93-8a89-47f1-0bce-017433ad79e2)
[^6]: [Microsoft Learn — SynonymInfo.Found property (PIA)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.office.interop.word.synonyminfo.found?view=word-pia)
[^7]: [Microsoft Learn — SynonymInfo.MeaningList property (PIA)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.office.interop.word.synonyminfo.meaninglist?view=word-pia)
[^8]: [Microsoft Learn — SynonymInfo.SynonymList property (Word)](https://learn.microsoft.com/en-us/office/vba/api/word.synonyminfo.synonymlist)
[^9]: [Microsoft Learn — SynonymInfo.PartOfSpeechList property (PIA)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.office.interop.word.synonyminfo.partofspeechlist?view=word-pia)
[^10]: [Microsoft Learn — Marshal.GetActiveObject(String)](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.getactiveobject?view=netframework-4.8.1)
[^11]: [Microsoft — Microsoft Software License Terms: Office 2024 Desktop (Retail, English PDF)](https://www.microsoft.com/content/dam/microsoft/usetm/documents/office/2024-word/retail-(packaged)/Microsoft%20License%20Terms%20Office%202024%20-%20English.pdf)
