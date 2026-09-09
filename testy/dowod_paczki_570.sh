#!/usr/bin/env bash
# DOWOD, ZE PACZKA 5.0.70 NIESIE ZMIANE.
#
# Paczka Inno jest SKOMPRESOWANA (LZMA), wiec grep po Setup.exe daje falszywe
# zero takze przy poprawnej zawartosci.  Dowod prowadzimy przez STAGING (z niego
# ISCC pakuje), przez sumy kontrolne i przez sonde czytajaca OBA kodowania
# napisow w binarce .NET.
set -u
cd /mnt/d/projekty/edsharp-pr
STAGE=/mnt/c/EdSharp
PACZKA=dist/EdSharpNG_Setup_5.0.70.exe

echo "=== 1. binarka w paczce to TA, ktora mierzylem ==="
A=$(md5sum < EdSharpNG.exe | cut -d' ' -f1)
B=$(md5sum < "$STAGE/EdSharpNG.exe" | cut -d' ' -f1)
echo "  repo:    $A"
echo "  staging: $B"
[ "$A" = "$B" ] && echo "  ZGODNE" || { echo "  ROZNE - NIE WYSYLAJ"; exit 1; }

echo "=== 2. nowa fraza w KAZDYM pliku opisow stagingu ==="
for f in EdSharp.md Hotkeys.ini hotkeys.txt; do
  N=$(grep -ac 'copies the files themselves\|copies the file ITSELF' "$STAGE/$f")
  echo "  $f: $N"
  [ "$N" -ge 1 ] || { echo "  BRAK nowej frazy w $f"; exit 1; }
done

echo "=== 3. KONTROLA NEGATYWNA: stara fraza zero wystapien ==="
# Stary opis mowil, ze Control+C kopiuje SCIEZKI - i wlasnie ta obietnica byla
# powodem zgloszenia, bo sciezki w schowku nie sa plikiem.
for f in EdSharp.md Hotkeys.ini hotkeys.txt; do
  N=$(grep -ac 'Control+C copies the full paths of everything selected\|Control+C copies its full path,' "$STAGE/$f")
  echo "  $f: $N (ma byc 0)"
  [ "$N" -eq 0 ] || { echo "  STARA fraza nadal w $f"; exit 1; }
done

echo "=== 4. wersja w .iss stagingu ==="
grep -aE '^(AppVersion|AppVerName)=' "$STAGE/EdSharp_Setup.iss"
grep -aqE '^AppVerName=EdSharpNG 5\.0\.70 \(beta\)' "$STAGE/EdSharp_Setup.iss" || { echo "  ZLA WERSJA"; exit 1; }

echo "=== 5. nowy kod w binarce stagingu ==="
python3 testy/napisy_w_binarce_570.py || exit 1

echo "=== 6. czasy: paczka nowsza od binarki ==="
[ "$PACZKA" -nt EdSharpNG.exe ] && echo "  paczka NOWSZA od binarki" || { echo "  paczka STARSZA"; exit 1; }

echo "=== 7. rozmiar i suma paczki ==="
ls -l "$PACZKA" | awk '{print "  "$5" B"}'
md5sum "$PACZKA"
