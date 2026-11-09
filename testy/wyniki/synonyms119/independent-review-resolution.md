# Niezależny przegląd kandydata 5.0.119

Przegląd w oddzielnym kontekście: deleg_f0fd937c. Wykonawca nie zmieniał kodu. Sprawdzony zestaw źródeł: `review-source-snapshot.json`; rodzic ponownie porównał wszystkie jego hashe z plikami po zakończeniu przeglądu.

Wykonawca wskazał jeden bloker procesu budowania: nowe pliki nie były jeszcze w indeksie Git w momencie jego odczytu, a skrypt tworzy staging z `git ls-files`.

**Rozstrzygnięcie rodzica:** bloker został usunięty przed faktyczną budową kandydata, podczas równoległej pracy. `git ls-files --error-unmatch` potwierdza obecność `SynonimyOdmiany.cs`, `PrepareSynonyms.ps1`, `Synonyms/forms-resource.json` i `Synonyms/plwordnet-forms.bin.gz`. Instalator został następnie rzeczywiście zbudowany i zainstalowany: kod 0, wszystkie 453 pliki zgodne ze stagingiem i ZIP-em. Manifest, nowy moduł C# i przygotowanie zasobu są w zainstalowanym pakiecie. Nie ma powodu zmieniać zaakceptowanych bajtów paczki z powodu już usuniętego stanu indeksu.

Wykonawca niezależnie potwierdził izolację synsetów/POS, pierwszeństwo lematu, brak przenoszenia morfologii do OMW, ścisłe UTF-8 i ograniczenia długości bloków, brak runtime'owych zależności od SQL/Pythona/sieci oraz 13 testów Python i cztery programy C#. Rodzic ma własne wykonania tych testów i odbiór pełnego programu z żywym NVDA.

Granice: przegląd nie był fuzzingiem całego zasobu ani niezależnym testem GUI czy instalacji. Te ostatnie wykonał i zapisał rodzic w `../release5119/`. Nie nazywamy tego niezależnym audytem bezpieczeństwa ani akceptacją użytkownika.
