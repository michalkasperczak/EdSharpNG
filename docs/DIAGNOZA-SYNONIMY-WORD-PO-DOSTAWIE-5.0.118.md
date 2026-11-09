# Shift+F7 uruchamia Worda — diagnoza po dostawie 5.0.118

## Zgłoszenie

Michał zgłosił, że Shift+F7 ponownie otwiera Worda i jego słownik synonimów. Nie negować tego wynikiem testu na odrębnej kopii aplikacji.

## Pomiar na głównym komputerze

Odczyt przez SSH `michal-glowny`, komputer `MICHAL`, bez ingerowania w fokus, zamykania aplikacji i instalowania czegokolwiek.

- Działa `C:\Program Files\EdSharpNG\EdSharpNG.exe`.
- Stała `EdSharp.App.VersionString` odczytana bez wykonywania aplikacji przez ReflectionOnly: **5.0.115**, nie 5.0.118.
- SHA-256 zainstalowanej binarki: `86f59310f5f23fe132c361321b9165b2ba8be2c09269cbe4ce1eb6c69316a96e`.
- Rejestr deinstalacji wskazuje tę samą lokalizację i wersję 5.0.115.
- Dołączone zainstalowane źródło `EdSharp.cs` ma w `Thesaurus()` wywołania `COM.WordAccess`, aktywację `WinWord` i `CheckSynonyms`.
- Plik indeksu `Synonyms\plwordnet-synonyms.txt` jest obecny. Sama obecność danych nie dowodzi integracji z uruchomioną binarką.
- W folderze `D:\Projekty Codex\Hermes\Nowe` nadal jest instalator 5.0.118. SHA-256: `a169520818d9a64a8bf7ef537c1e4ee8db05ada17ac8534a55f21d7bc7a308ee`, zgodny z dostawą.
- W chwili odczytu nie było procesu WINWORD. Nie oznacza to, że Word nie uruchamiał się wcześniej; nie odtwarzano gestu w aktywnej sesji Michała.

## Wniosek i ograniczenie

Bieżąca instalacja jest starsza niż wersje z lokalnymi synonimami. Aktualnie uruchomiona kopia nie jest kopią 5.0.118 testowaną z NVDA na Hermesie. Zatem wcześniejszy test nie potwierdza działania instalacji na głównym komputerze. Nie ustalono, co ani kiedy przywróciło wersję 5.0.115; nie przypisywać tego użytkownikowi, instalatorowi ani aktualizatorowi bez dowodu.

Do rozwiązania pozostaje bezpieczna instalacja 5.0.118 i ponowny odbiór na tej kopii. Nie zamykano otwartego edytora, nie podmieniano plików programu i nie uruchamiano instalatora. Przed taką ingerencją potrzebna jest decyzja Michała oraz zabezpieczenie niezapisanej pracy. Nie ogłaszać zgłoszenia jako naprawionego.

## Wyjaśnienie i uporządkowanie publicznego wydania

Michał następnie wyjaśnił, że pobrał aktualizację z GitHuba zamiast czekać na instalator w folderze odbioru. Odczyt publicznego `/releases/latest` potwierdził, że wskazywał on 5.0.115. Na jego zlecenie opublikowano 5.0.118 jako najnowsze wydanie, nie prerelease; endpoint aktualizatora, wybór instalatora i suma z opisu zostały sprawdzone. Usunięto starą własną paczkę 5.0.115 z folderu odbioru po zachowaniu zweryfikowanej kopii na Hermesie i potwierdzeniu publicznego archiwum. Na głównym komputerze nadal działa wcześniejszy proces i binarka 5.0.115, bez ingerencji w sesję. Dostawa i kanał aktualizacji są uporządkowane; aktualizacji u użytkownika nie wykonywano.

Wydanie: https://github.com/michalkasperczak/EdSharpNG/releases/tag/v5.0.118

Dowody publikacji i dostawy: `../testy/wyniki/release5118/`.
