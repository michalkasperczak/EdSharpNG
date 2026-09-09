#!/usr/bin/env bash
# KONTROLA NEGATYWNA naprawy "jeden ruch, jeden glos" w siatce (5.0.40).
#
# Pyta o jedna rzecz: czy sonda mowy ROZROZNIA kod sprzed naprawy od kodu po
# naprawie.  Bez tego "14/14 na zywym NVDA" nie dowodzi naprawy - moglo by
# znaczyc, ze sonda przechodzi zawsze.
#
# Buduje binarke SPRZED zmiany z JAWNIE podanej rewizji (domyslnie 77d55df =
# stan 5.0.39), odpala na niej sonde i wymaga, zeby objaw zgloszony przez
# uzytkownika sie ODTWORZYL: powtorzona tresc komorki i slowo "zaznaczony".
# Potem PRZYWRACA nowy kod i pilnuje, ze w repo lezy NOWA binarka.
#
# PULAPKI, ktore ten skrypt respektuje (zmierzone w tym projekcie):
#  - rewizja odniesienia MUSI byc jawna, nie HEAD: po zacommitowaniu zmiany
#    HEAD porownuje kod z samym soba i daje falszywe "0 asercji pada",
#  - swiezosc binarki rozstrzyga TIMESTAMP, nie kod wyjscia cmd.exe: interop
#    WSL potrafi zwrocic 0 i nie wykonac buildu,
#  - na koniec sha256 MUSI sie zgadzac z binarka nowa, inaczej wyslalibysmy
#    paczke ze starym kodem.
set -u

REPO=/mnt/d/projekty/edsharp-pr
REV="${1:-77d55df}"
SONDA=testy/narzedzia_nvda/zmierz_dublowanie_w_siatce.py
cd "$REPO" || exit 3

echo "=== kontrola negatywna 5.0.40: rewizja odniesienia $REV ==="

if ! git rev-parse --verify "$REV" >/dev/null 2>&1; then
    echo "BLAD: rewizja $REV nie istnieje"; exit 3
fi

cp EdSharpNG.exe /tmp/EdSharpNG_nowy_540.exe || exit 3
SHA_NOWA=$(sha256sum EdSharpNG.exe | cut -d' ' -f1)
echo "sha256 nowej binarki: $SHA_NOWA"

buduj() {
    for proba in 1 2 3; do
        sleep 5
        /mnt/c/Windows/System32/cmd.exe /c "cd /d D:\\projekty\\edsharp-pr && BuildEdSharp.cmd" \
            > /tmp/kn540_build_$proba.log 2>&1
        if [ EdSharpNG.exe -nt Lbc.cs ] && [ EdSharpNG.exe -nt EdSharp.cs ]; then
            echo "  build doszedl do skutku (proba $proba)"; return 0
        fi
        echo "  UWAGA: binarka nie jest swiezsza od zrodel, ponawiam (proba $proba)"
    done
    return 1
}

przywroc() {
    git checkout -- Lbc.cs EdSharp.cs 2>/dev/null
    cp /tmp/EdSharpNG_nowy_540.exe EdSharpNG.exe
    SHA_PO=$(sha256sum EdSharpNG.exe | cut -d' ' -f1)
    if [ "$SHA_PO" != "$SHA_NOWA" ]; then
        echo "AWARIA: w repo NIE lezy nowa binarka. NIE WYSYLAJ PACZKI."; exit 3
    fi
    echo "przywrocono nowa binarke, sha256 zgodne"
}

# --- stary kod ---
git stash push -- Lbc.cs EdSharp.cs > /dev/null 2>&1
git checkout "$REV" -- Lbc.cs EdSharp.cs || { echo "BLAD checkoutu"; exit 3; }
if ! buduj; then
    echo "BLAD: nie zbudowalem binarki sprzed zmiany - pomiar NIEROZSTRZYGAJACY"
    git checkout -- Lbc.cs EdSharp.cs; git stash pop > /dev/null 2>&1; przywroc; exit 3
fi

echo ""
echo "=== sonda na kodzie SPRZED naprawy (objaw MUSI sie odtworzyc) ==="
timeout 420 python3 "$SONDA" > /tmp/kn540_stary.txt 2>&1
grep -E "^zaliczone" /tmp/kn540_stary.txt
ZALICZONE_STARY=$(grep -oP '^zaliczone: \K[0-9]+' /tmp/kn540_stary.txt || echo "-1")
# Objaw zgloszenia: slowo "zaznaczony" w wypowiedzi czytnika.
ILE_ZAZNACZ=$(grep -ci "zaznaczony" /tmp/kn540_stary.txt || true)
echo "wystapien 'zaznaczony' w logu starego kodu: $ILE_ZAZNACZ"

# --- nowy kod ---
git checkout -- Lbc.cs EdSharp.cs
git stash pop > /dev/null 2>&1
if ! buduj; then
    echo "BLAD: nie zbudowalem binarki po naprawie"; przywroc; exit 3
fi

echo ""
echo "=== sonda na kodzie PO naprawie ==="
timeout 420 python3 "$SONDA" > /tmp/kn540_nowy.txt 2>&1
grep -E "^zaliczone" /tmp/kn540_nowy.txt
ZALICZONE_NOWY=$(grep -oP '^zaliczone: \K[0-9]+' /tmp/kn540_nowy.txt || echo "-1")
ILE_ZAZNACZ_NOWY=$(grep -ci "zaznaczony" /tmp/kn540_nowy.txt || true)

SHA_KONCOWA=$(sha256sum EdSharpNG.exe | cut -d' ' -f1)
echo ""
echo "=== WERDYKT ==="
echo "stary kod: zaliczone=$ZALICZONE_STARY, wystapien 'zaznaczony'=$ILE_ZAZNACZ"
echo "nowy kod:  zaliczone=$ZALICZONE_NOWY, wystapien 'zaznaczony'=$ILE_ZAZNACZ_NOWY"
echo "sha256 binarki w repo po przebiegu: $SHA_KONCOWA"

if [ "$ILE_ZAZNACZ" -lt 1 ]; then
    echo "SONDA GLUCHA: na starym kodzie NIE odtworzyla slowa 'zaznaczony'."
    echo "Wynik na nowym kodzie NIC NIE ZNACZY."
    exit 2
fi
if [ "$ZALICZONE_STARY" -ge "$ZALICZONE_NOWY" ]; then
    echo "SONDA NIE ROZROZNIA: stary kod wypadl nie gorzej niz nowy."
    exit 2
fi
if [ "$SHA_KONCOWA" = "$SHA_NOWA" ]; then
    echo "UWAGA: binarka identyczna jak przed przebiegiem - sprawdz, czy build sie odbyl."
fi
echo "OK: objaw odtworzony na starym kodzie, znika na nowym - sonda ROZROZNIA."
exit 0
