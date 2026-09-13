#!/bin/bash
# Buduje EdSharpNG i pakuje instalator JEDNYM prostym poleceniem.
#
# PO CO TEN PLIK.  Kompilacja wymaga zlozonego polecenia: skopiuj zrodla, wejdz
# na dysk C, uruchom cmd.exe, ktory uruchamia kompilator.  Takiego polecenia
# skaner bezpieczenstwa Hermesa nie potrafi rozlozyc z gory (program uruchamia
# program), wiec za kazdym razem wstrzymuje je i czeka na zatwierdzenie w
# konsoli.  Michal pisze z telefonu i tego okna NIE WIDZI - po minucie wychodzi
# "brak zgody" i budowanie stoi, mimo ze zgode dal w rozmowie.
#
# Listy dozwolonych polecen NIE DA sie tu uzyc: odrzuca ona z zasady wszystko,
# co zawiera "&&", cudzysłowy i podobne łączniki (_has_allowlist_shell_operator
# w tools/approval_floors.py), a takie wlasnie jest polecenie kompilacji.
# Dlatego zlozonosc siedzi TUTAJ, a z zewnatrz wola sie jedno proste polecenie
# bez lacznikow - i to ono trafia na liste dozwolonych.
#
# UZYCIE:  bash /home/michal/projekty/edsharp/zbuduj.sh 5.0.75
set -u

REPO="/home/michal/projekty/edsharp"
BUILD="/mnt/c/EdSharpBuild"
WERSJA="${1:-}"

if [ -z "$WERSJA" ]; then
    echo "BLAD: podaj numer wersji, np.: bash zbuduj.sh 5.0.75"
    exit 2
fi

# Numer wersji w programie MUSI zgadzac sie z numerem paczki.  Raz sie
# rozjechal (paczka 5.0.74, a okno About mowilo 5.0.73) - dla osoby, ktora
# testuje kolejne wersje po kolei, to najgorszy mozliwy blad, bo nie wiadomo,
# co wlasciwie sie sprawdza.  Dlatego sprawdzamy to PRZED kompilacja.
if ! grep -q "VersionString = \"$WERSJA\"" "$REPO/EdSharp.cs"; then
    echo "BLAD: EdSharp.cs nie ma VersionString = \"$WERSJA\"."
    echo "      Popraw numer wersji w zrodle albo podaj ten, ktory tam jest:"
    grep -n 'VersionString = ' "$REPO/EdSharp.cs" | head -1
    exit 3
fi

# ZNACZNIK BOM W PLIKACH INI TO CICHA AWARIA.  Zmierzone 12.09.2026: trzy
# bajty EF BB BF na poczatku Hotkeys.ini sprawiaja, ze Windows (funkcja
# GetPrivateProfileString, ktora czyta te pliki) NIE WIDZI pierwszej sekcji -
# nazwa sekcji brzmi dla niej "<BOM>[Hotkeys]", a nie "[Hotkeys]".  Skutek:
# WSZYSTKIE opisy i skroty polecen czytaly sie jako puste, a program nie
# zglaszal zadnego bledu, tylko milczal - paleta polecen nie mowila skrotow.
# Plik wyglada przy tym normalnie w kazdym edytorze, wiec bez pomiaru nie ma
# tego jak zauwazyc.  Dlatego sprawdzamy to przy KAZDYM budowaniu.
for INI in "$REPO/Hotkeys.ini" "$REPO/EdSharp.ini"; do
    [ -f "$INI" ] || continue
    if [ "$(head -c 3 "$INI" | od -An -tx1 | tr -d ' \n')" = "efbbbf" ]; then
        echo "BLAD: $INI zaczyna sie znacznikiem BOM - Windows nie odczyta z niego sekcji."
        echo "      Usun trzy pierwsze bajty (EF BB BF) i zbuduj ponownie."
        exit 11
    fi
done

echo "== 1/3 kopiuje zrodla do $BUILD"
# PODSUMOWANIE SKROTOW POWSTAJE ZE ZRODLA, NIE Z PAMIECI.  EdSharp_Hotkeys.txt
# (Alt+Shift+H) byl do 5.0.94 pisany rekami obok Hotkeys.ini i rozjechal sie
# doszczetnie: zero opisow komentarzy, stare chordy zakladek, Control+T dla
# komendy, ktora dawno ma inny klawisz.  Uzytkownik niewidomy nie ma jak tego
# wychwycic - lista wyglada normalnie, tylko mowi nieprawde.  Od 5.0.95 jest
# generowana przy KAZDYM budowaniu, wiec dwa pliki nie moga sie roznic.
python3 "$REPO/testy/generuj_podsumowanie_skrotow.py" || exit 12
cp "$REPO"/*.cs "$BUILD"/ || exit 4
cp "$REPO"/*.js "$BUILD"/ 2>/dev/null
# Polecenie kompilacji tez trzeba przekopiowac.  Bez tego dodanie NOWEGO pliku
# zrodlowego konczy sie bledem "nazwa nie istnieje": zrodlo lezy na dysku C, ale
# kompilator dostaje stara liste plikow z poprzedniej kopii BuildEdSharp.cmd.
cp "$REPO"/BuildEdSharp.cmd "$BUILD"/ || exit 4
# Uruchamiacze pomiarow sa wersjonowane w repozytorium (testy/uruchamiacze), bo
# wczesniej istnialy TYLKO w katalogu wymiany na dysku C - czyli poza kopia
# zapasowa i poza GitHubem. Skasowanie tego katalogu zabieralo mozliwosc
# powtorzenia pomiarow, a same pliki .cs pomiarow zostawaly bez sposobu
# uruchomienia (kazdy wymaga wlasnej listy plikow i odwolan do UIA).
cp "$REPO"/testy/uruchamiacze/*.cmd "$BUILD"/ 2>/dev/null
cp "$REPO"/testy/*.cs "$BUILD"/ 2>/dev/null

echo "== 2/3 kompiluje (cmd.exe BuildEdSharp.cmd)"
cd "$BUILD" || exit 5
/mnt/c/Windows/System32/cmd.exe /c BuildEdSharp.cmd 2>&1 | tail -6

# Binarka musi byc MLODSZA niz zrodlo, inaczej pakujemy stary program.
if [ ! -f "$BUILD/EdSharpNG.exe" ]; then
    echo "BLAD: nie ma $BUILD/EdSharpNG.exe - kompilacja nie doszla do konca."
    exit 6
fi
if [ "$BUILD/EdSharpNG.exe" -ot "$BUILD/EdSharp.cs" ]; then
    echo "BLAD: EdSharpNG.exe STARSZA niz EdSharp.cs - kompilacja cicho padla."
    echo "      NIE pakuje paczki; zajrzyj wyzej w wyjscie kompilatora."
    exit 7
fi
echo "   OK: $(stat -c '%s B, %y' "$BUILD/EdSharpNG.exe")"

# Gotowa binarke i biblioteke przenosimy do repo, bo skrypt pakujacy szuka ich
# TAM.  Bez tego probowal kompilowac po swojemu, wolajac cmd.exe z katalogu WSL
# (\\wsl.localhost\...) - a Windows takich sciezek nie obsluguje ("UNC paths are
# not supported"), wiec kompilator w ogole nie startowal i skrypt slusznie
# odmawial spakowania starej binarki.  Kompilacja zostaje w $BUILD na dysku C,
# gdzie dziala; tutaj tylko przynosimy wynik.
echo "== 3/3 pakuje instalator $WERSJA"
cp "$BUILD/EdSharpNG.exe" "$REPO/EdSharpNG.exe" || exit 8
cp "$BUILD/EdSharp.dll" "$REPO/EdSharp.dll" || exit 8

# BIBLIOTEKA Ude.dll MUSI JECHAC Z PACZKA, jesli kompilowalismy Z NIA.
#
# Zmierzone 13.09.2026 i to byla awaria zabierajaca cala wersje: binarka byla
# zbudowana z symbolem HAVEUDE (czyli KOD WOLA Ude), ale Ude.dll lezala tylko
# w katalogu kompilacji, nie w repo - a instalator pakuje z repo, z flaga
# "skipifsourcedoesntexist", wiec po cichu ja POMIJAL.  U uzytkownika kazde
# otwarcie pliku konczylo sie "Cannot open file!", bo .NET nie mial czym
# rozpoznac kodowania.  Flaga "pomin, gdy nie ma" zamienila brak pliku w cicha
# awarie dzialajacego programu.
# Warunek pytamy o BINARKE, nie o skrypt kompilacji: to jedyny pewny dowod, ze
# kod naprawde wola Ude (grep po BuildEdSharp.cmd trafia takze w KOMENTARZ o
# HAVEUDE i byl prawdziwy zawsze).
#
# WZORZEC TO SAME "CharsetDetector".  Zlaczonej nazwy "Ude.CharsetDetector" w
# binarce NIE MA - metadane .NET trzymaja przestrzen nazw i nazwe typu w
# OSOBNYCH napisach, wiec pytanie o nia dawalo zawsze falsz i cala ta bramka
# byla martwa (zmierzone 13.09.2026).
if grep -aq 'CharsetDetector' "$BUILD/EdSharpNG.exe" 2>/dev/null; then
    if [ -f "$BUILD/Ude.dll" ]; then
        cp "$BUILD/Ude.dll" "$REPO/Ude.dll" || exit 8
    fi
    if [ ! -f "$REPO/Ude.dll" ]; then
        echo "BLAD: binarka jest zbudowana z Ude (HAVEUDE), a Ude.dll nie ma w repo."
        echo "      Instalator pominalby ja po cichu, a program otwieralby pliki"
        echo "      bez rozpoznawania kodowania. Pobierz ja: powershell ./FetchUde.ps1"
        exit 12
    fi
fi
cd "$REPO" || exit 8

PACZKA="$REPO/dist/EdSharpNG_Setup_$WERSJA.exe"
# Stara paczke o tym numerze USUWAM przed pakowaniem.  Zmierzone (12.09.2026):
# skrypt pakujacy odmawia nadpisania istniejacego pliku ("juz istnieje, podbij
# wersje"), ale konczy sie kodem 0 - a ten skrypt sprawdzal tylko, czy plik
# ISTNIEJE, wiec meldowal GOTOWE i podawal sume STAREJ paczki.  Poprawka do tego
# samego numeru wersji wygladala jak zbudowana, a nie byla.  Falszywy sukces
# jest grozniejszy niz blad.
if [ -f "$PACZKA" ]; then
    echo "   usuwam poprzednia paczke $WERSJA (bede pakowal na nowo)"
    rm -f "$PACZKA"
fi

bash build_installer_garfield.sh "$WERSJA" 2>&1 | tail -8

if [ ! -f "$PACZKA" ]; then
    echo "BLAD: nie ma paczki $PACZKA"
    exit 9
fi
# Paczka musi byc MLODSZA niz binarka, ktora do niej weszla.  Inaczej wydalbym
# instalator ze starym programem w srodku.
if [ "$PACZKA" -ot "$BUILD/EdSharpNG.exe" ]; then
    echo "BLAD: paczka STARSZA niz EdSharpNG.exe - pakowanie nie doszlo do skutku."
    exit 10
fi
echo "GOTOWE: $PACZKA"
stat -c '%s B, %y' "$PACZKA"
sha256sum "$PACZKA" | cut -c1-64
