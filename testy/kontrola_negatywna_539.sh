#!/usr/bin/env bash
# KONTROLA NEGATYWNA naprawy "Wiersz 0" w siatce kreatora tabeli (5.0.39).
#
# Pyta o jedna rzecz: czy sonda mowy ROZROZNIA kod sprzed naprawy od kodu po
# naprawie.  Bez tego "10/10 na zywym NVDA" nie dowodzi naprawy - moglo by
# znaczyc, ze sonda przechodzi zawsze.
#
# Buduje binarke SPRZED zmiany z JAWNIE podanej rewizji (domyslnie ce9edd4 =
# stan 5.0.38), odpala na niej sonde mowy i wymaga, zeby objaw sie ODTWORZYL.
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
REV="${1:-ce9edd4}"
SONDA=testy/narzedzia_nvda/zmierz_wiersz_zero.py
cd "$REPO" || exit 3

echo "=== kontrola negatywna: rewizja odniesienia $REV ==="

if ! git rev-parse --verify "$REV" >/dev/null 2>&1; then
    echo "BLAD: rewizja $REV nie istnieje"; exit 3
fi

# Zachowujemy NOWA binarke i jej sume - to jest to, co musi wrocic.
cp EdSharpNG.exe /tmp/EdSharpNG_nowy_539.exe || exit 3
SHA_NOWA=$(sha256sum EdSharpNG.exe | cut -d' ' -f1)
echo "sha256 nowej binarki: $SHA_NOWA"

buduj() {
    for proba in 1 2 3; do
        sleep 5
        /mnt/c/Windows/System32/cmd.exe /c "cd /d D:\\projekty\\edsharp-pr && BuildEdSharp.cmd" \
            > /tmp/kn539_build_$proba.log 2>&1
        if [ EdSharpNG.exe -nt Lbc.cs ] && [ EdSharpNG.exe -nt EdSharp.cs ]; then
            echo "  build doszedl do skutku (proba $proba)"; return 0
        fi
        echo "  UWAGA: binarka nie jest swiezsza od zrodel, ponawiam (proba $proba)"
    done
    return 1
}

przywroc() {
    git checkout -- Lbc.cs EdSharp.cs 2>/dev/null
    cp /tmp/EdSharpNG_nowy_539.exe EdSharpNG.exe
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
echo "=== sonda mowy na kodzie SPRZED naprawy (objaw MUSI sie odtworzyc) ==="
timeout 420 python3 "$SONDA" > /tmp/kn539_stary.txt 2>&1
KOD_STARY=$?
grep -E "^zaliczone|Wiersz 0" /tmp/kn539_stary.txt | head -6
ZALICZONE_STARY=$(grep -oP '^zaliczone: \K[0-9]+' /tmp/kn539_stary.txt || echo "-1")
CZY_ZERO_STARY=$(grep -c "Wiersz 0" /tmp/kn539_stary.txt || true)

# --- nowy kod ---
git checkout -- Lbc.cs EdSharp.cs
git stash pop > /dev/null 2>&1
if ! buduj; then
    echo "BLAD: nie zbudowalem binarki po naprawie"; przywroc; exit 3
fi

echo ""
echo "=== sonda mowy na kodzie PO naprawie ==="
timeout 420 python3 "$SONDA" > /tmp/kn539_nowy.txt 2>&1
grep -E "^zaliczone" /tmp/kn539_nowy.txt
ZALICZONE_NOWY=$(grep -oP '^zaliczone: \K[0-9]+' /tmp/kn539_nowy.txt || echo "-1")

# W repo musi zostac binarka z NOWEGO kodu - tu jest wlasnie zbudowana,
# ale liczymy sume, zeby to potwierdzic, a nie zalozyc.
SHA_KONCOWA=$(sha256sum EdSharpNG.exe | cut -d' ' -f1)
echo ""
echo "=== WERDYKT ==="
echo "stary kod: zaliczone=$ZALICZONE_STARY, wystapien 'Wiersz 0' w logu=$CZY_ZERO_STARY"
echo "nowy kod:  zaliczone=$ZALICZONE_NOWY"
echo "sha256 binarki w repo po przebiegu: $SHA_KONCOWA"

if [ "$CZY_ZERO_STARY" -lt 1 ]; then
    echo "SONDA GLUCHA: na starym kodzie NIE odtworzyla 'Wiersz 0'."
    echo "Wynik na nowym kodzie NIC NIE ZNACZY."
    exit 2
fi
if [ "$ZALICZONE_STARY" -ge "$ZALICZONE_NOWY" ]; then
    echo "SONDA NIE ROZROZNIA: stary kod wypadl nie gorzej niz nowy."
    exit 2
fi
echo "OK: objaw odtworzony na starym kodzie, znika na nowym - sonda ROZROZNIA."
exit 0
