# Ude — wykrywanie kodowania tekstu

EdSharpNG używa niezmienionej biblioteki Ude.dll z pakietu NuGet UDE.CSharp 1.1.0. Numer assembly tej biblioteki to 0.1.0.0, niezależnie od numeru pakietu NuGet.

Biblioteka jest portem Mozilla Universal Charset Detector autorstwa Rudiego Pettazziego; nagłówki źródeł zachowują prawa Netscape Communications Corporation oraz atrybucje pozostałych autorów.

Oryginalne źródła umożliwiają wybór MPL 1.1, GPL 2 lub późniejszej albo LGPL 2.1 lub późniejszej. Dla tej dystrybucji wybieramy LGPL 3 lub późniejszą. Jej pełny tekst jest w głównym pliku lgpl.txt; zachowujemy również wszystkie trzy oryginalne teksty w upstream/license oraz oryginalne nagłówki źródeł. Biblioteka jest dostarczana bez gwarancji.

Komplet źródeł biblioteki oraz jej upstreamowych skryptów budowania z oficjalnego archiwum Google Code jest w upstream. Pominięto jedynie przykładowy program i dane testowe. Plik provenance.json przypina adresy i SHA-256 pakietu, biblioteki oraz archiwum źródeł. Nie twierdzimy, że ponowny build odtwarza te same bajty binarium NuGet.

Biblioteka jest ładowana dynamicznie jako osobny Ude.dll. Można ją zastąpić zgodną interfejsowo zmodyfikowaną wersją i ponownie zbudować EdSharpNG z udostępnionych źródeł. Nie nakładamy dodatkowych ograniczeń modyfikacji ani analizy wymaganej do debugowania zmian tej biblioteki.
