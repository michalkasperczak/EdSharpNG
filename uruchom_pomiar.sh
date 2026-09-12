#!/bin/bash
# Uruchamia pojedynczy pomiar (plik .cs z metoda Main) przez kompilator csc z
# Windows, tak samo jak robi to zbuduj.sh.
#
# PO CO: kompilatora nie ma w WSL, a sciezek \\wsl.localhost\ Windows nie
# obsluguje - wiec pomiar trzeba skopiowac na dysk C i tam skompilowac.
# Bez tego skryptu kazdy pomiar wymagalby recznie pisanego, dlugiego polecenia
# z lacznikami, ktore skaner bezpieczenstwa wstrzymuje i czeka na zgode w
# konsoli, ktorej Michal nie widzi (ten sam powod, co w zbuduj.sh).
#
# UZYCIE:  bash uruchom_pomiar.sh testy/pomiar_mazovia.cs [dodatkowy.cs ...]
set -u

REPO="/home/michal/projekty/edsharp"
BUILD="/mnt/c/EdSharpBuild/pomiar"
mkdir -p "$BUILD"

if [ $# -lt 1 ]; then
    echo "BLAD: podaj plik pomiaru, np.: bash uruchom_pomiar.sh testy/pomiar_mazovia.cs"
    exit 2
fi

NAZWY=""
for PLIK in "$@"; do
    SCIEZKA="$PLIK"
    [ -f "$SCIEZKA" ] || SCIEZKA="$REPO/$PLIK"
    if [ ! -f "$SCIEZKA" ]; then
        echo "BLAD: nie ma pliku $PLIK"
        exit 3
    fi
    cp "$SCIEZKA" "$BUILD/" || exit 4
    NAZWY="$NAZWY $(basename "$SCIEZKA")"
done

EXE="pomiar_$(date +%s).exe"
cd "$BUILD" || exit 5
/mnt/c/Windows/System32/cmd.exe /c "csc_wrapper.cmd $EXE$NAZWY" 2>&1
