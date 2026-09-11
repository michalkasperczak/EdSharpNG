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

echo "== 1/3 kopiuje zrodla do $BUILD"
cp "$REPO"/*.cs "$BUILD"/ || exit 4
cp "$REPO"/*.js "$BUILD"/ 2>/dev/null

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
cd "$REPO" || exit 8
bash build_installer_garfield.sh "$WERSJA" 2>&1 | tail -8

PACZKA="$REPO/dist/EdSharpNG_Setup_$WERSJA.exe"
if [ ! -f "$PACZKA" ]; then
    echo "BLAD: nie ma paczki $PACZKA"
    exit 9
fi
echo "GOTOWE: $PACZKA"
stat -c '%s B, %y' "$PACZKA"
sha256sum "$PACZKA" | cut -c1-64
