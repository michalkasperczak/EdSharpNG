#!/bin/bash
# Kompiluje kandydata i uruchamia pomiar tozsamosci zaimportowanego dokumentu.
#
# PO CO OSOBNY STAGING.  C:\EdSharp jest ZAJETE (stare drzewo EdSharpa), a
# C:\EdSharpBuild trzyma binarke 5.0.114, ktora sluzy za punkt odniesienia dla
# pomiaru RED.  Nadpisanie jej zabraloby mozliwosc pokazania, ze pomiar naprawde
# rozroznia stara wersje od nowej.  Dlatego kandydat ma wlasny katalog NTFS.
#
# KOMPILATORA NIE MA W WSL, a sciezek \\wsl.localhost\ Windows nie obsluguje -
# wiec zrodla trzeba skopiowac na dysk C i tam uruchomic csc przez cmd.exe
# (ten sam powod, co w zbuduj.sh i uruchom_pomiar.sh).
#
# UZYCIE:
#   bash testy/zmierz_tozsamosc_importu.sh           # kandydat (nowy kod)
#   bash testy/zmierz_tozsamosc_importu.sh --stara    # odniesienie 5.0.114
set -u

REPO="/home/michal/projekty/edsharp-import-identity"
STAGE="/mnt/c/EdSharpImportIdentity"
ODNIESIENIE="/mnt/c/EdSharpBuild/EdSharpNG.exe"
TRYB="${1:-nowa}"

mkdir -p "$STAGE" || exit 2

if [ "$TRYB" = "--stara" ]; then
    # POMIAR ODNIESIENIA: ta sama sonda, STARA binarka.  Punkty dla formatow
    # importowanych maja tu FAILowac - inaczej sonda nie roznicuje wersji i nie
    # jest dowodem niczego.
    #
    # UWAGA NA KOLEJNOSC: ten tryb NADPISUJE EdSharpNG.exe w stagingu binarka
    # odniesienia.  Kto uruchomi "--stara" po zbudowaniu kandydata, zostawi w
    # katalogu STARY plik pod nazwa kandydata - i nastepny pomiar, i pakowanie,
    # poszlyby nie z tego, co wlasnie zmierzono.  Dlatego kandydata zawsze
    # buduj PO pomiarze odniesienia, a SHA sprawdzaj w logu (wypisujemy je
    # w obu trybach).
    if [ ! -f "$ODNIESIENIE" ]; then
        echo "BLAD: nie ma binarki odniesienia $ODNIESIENIE"
        exit 3
    fi
    cp "$ODNIESIENIE" "$STAGE/EdSharpNG.exe" || exit 4
    echo "== TRYB: ODNIESIENIE (stara binarka)"
    echo "== SHA mierzonej binarki: $(sha256sum "$STAGE/EdSharpNG.exe" | cut -c1-64)"
else
    echo "== TRYB: KANDYDAT (nowy kod)"
    echo "== 1/3 kopiuje zrodla kandydata do $STAGE"
    cp "$REPO"/*.cs "$STAGE"/ || exit 4
    cp "$REPO"/BuildEdSharp.cmd "$STAGE"/ || exit 4
    # Manifest i ikona: BuildEdSharp.cmd BEZ manifestu przerywa prace, a ikona
    # jest opcjonalna.  Kopiujemy oba, zeby kandydat szedl ta sama komenda
    # kompilacji co produkt - inaczej mierzylibysmy inny plik niz ten, ktory
    # rodzic potem zapakuje.
    cp "$REPO"/EdSharp.manifest "$STAGE"/ || exit 4
    cp "$REPO"/EdSharp.ico "$STAGE"/ 2>/dev/null
    # Biblioteki runtime i referencyjne bierzemy z dzialajacego katalogu
    # budowania: Tektosyne i Ude sa /reference w komendzie csc, wiec bez nich
    # kompilacja padnie albo (dla Ude) cicho zgubi rozpoznawanie kodowania.
    for d in Tektosyne.dll Ude.dll nvdaControllerClient.dll; do
        cp "/mnt/c/EdSharpBuild/$d" "$STAGE"/ 2>/dev/null
    done
    # Pobieraczy narzedzi NIE kopiujemy: sciagalyby kilkadziesiat megabajtow do
    # stagingu, a Convert i tak dowiazujemy nizej do katalogu produktu.
    echo "== 2/3 kompiluje kandydata"
    cd "$STAGE" || exit 5
    # KOD WYJSCIA KOMPILACJI MUSI PRZEZYC POTOK.  `... | tail -8` oddaje kod
    # TAIL-a, czyli zawsze 0 - blad kompilacji przechodzil tu niezauwazony az
    # do kontroli ponizej.  Log idzie do pliku, a potem pokazujemy ogon.
    LOG_KOMPILACJI="$STAGE/kompilacja_kandydata.log"
    /mnt/c/Windows/System32/cmd.exe /c BuildEdSharp.cmd > "$LOG_KOMPILACJI" 2>&1
    KOD_KOMPILACJI=$?
    tail -8 "$LOG_KOMPILACJI"
    if grep -aqE "error CS[0-9]+" "$LOG_KOMPILACJI"; then
        echo "BLAD: kompilator zglosil bledy CS (patrz $LOG_KOMPILACJI)"
        exit 6
    fi
    if [ "$KOD_KOMPILACJI" -ne 0 ]; then
        echo "BLAD: kompilacja zwrocila kod $KOD_KOMPILACJI (patrz $LOG_KOMPILACJI)"
        exit 6
    fi
    if [ ! -f "$STAGE/EdSharpNG.exe" ]; then
        echo "BLAD: nie ma $STAGE/EdSharpNG.exe - kompilacja nie doszla do konca."
        exit 6
    fi
    if [ "$STAGE/EdSharpNG.exe" -ot "$STAGE/EdSharp.cs" ]; then
        echo "BLAD: EdSharpNG.exe STARSZA niz EdSharp.cs - kompilacja cicho padla."
        exit 7
    fi
    echo "   OK: $(stat -c '%s B, %y' "$STAGE/EdSharpNG.exe")"
    echo "   SHA: $(sha256sum "$STAGE/EdSharpNG.exe" | cut -c1-64)"
fi

# Konwertery: sonda buduje PRAWDZIWE pliki zrodlowe Pandokiem, wiec katalog
# Convert musi byc na miejscu.  Dowiazanie zamiast kopii - Pandoc to kilkadziesiat
# megabajtow, a dysk C ma malo wolnego miejsca.
if [ ! -e "$STAGE/Convert" ]; then
    /mnt/c/Windows/System32/cmd.exe /c "mklink /J C:\\EdSharpImportIdentity\\Convert C:\\EdSharpBuild\\Convert" 2>&1 | tail -2
fi

cp "$REPO/EdSharp.ini" "$STAGE"/ || exit 8
cp "$REPO/Hotkeys.ini" "$STAGE"/ 2>/dev/null
cp "$REPO/testy/harness_tozsamosc_importu.cs" "$STAGE"/ || exit 8
cp "$REPO/testy/uruchamiacze/zmierz_tozsamosc_importu.cmd" "$STAGE"/ || exit 8

echo "== 3/3 uruchamiam pomiar"
cd "$STAGE" || exit 9
/mnt/c/Windows/System32/cmd.exe /c zmierz_tozsamosc_importu.cmd 2>&1
KOD_POMIARU=$?
# KOD WYJSCIA POMIARU JEST WYNIKIEM, nie ozdoba logu.  Samo `echo "...: $?"`
# ustawialo kod skryptu na kod ECHA (0), wiec oblany pomiar konczyl sie
# cichym PASS dla kazdego, kto patrzy na `$?` albo na `set -e`.
echo "KOD WYJSCIA POMIARU: $KOD_POMIARU"
if [ "$KOD_POMIARU" -ne 0 ]; then
    echo "WYNIK: POMIAR OBLANY (kod $KOD_POMIARU)"
else
    echo "WYNIK: POMIAR ZIELONY"
fi
exit "$KOD_POMIARU"
