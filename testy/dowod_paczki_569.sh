#!/usr/bin/env bash
# DOWOD, ZE PACZKA NIESIE ZMIANE 5.0.69.
#
# Paczka Inno jest SKOMPRESOWANA (LZMA), wiec grep po Setup.exe za nowa fraza
# daje falszywe zero takze przy poprawnej zawartosci.  Dowod prowadzimy przez
# STAGING - to z niego ISCC pakuje - oraz przez czasy i sumy kontrolne.
set -u
cd /mnt/d/projekty/edsharp-pr
STAGE=/mnt/c/EdSharp
PACZKA=dist/EdSharpNG_Setup_5.0.69.exe

echo "=== 1. binarka w paczce to TA, ktora mierzylem ==="
md5sum EdSharpNG.exe "$STAGE/EdSharpNG.exe"
A=$(md5sum < EdSharpNG.exe | cut -d' ' -f1)
B=$(md5sum < "$STAGE/EdSharpNG.exe" | cut -d' ' -f1)
[ "$A" = "$B" ] && echo "  ZGODNE" || { echo "  ROZNE - NIE WYSYLAJ"; exit 1; }

echo "=== 2. nowa fraza w KAZDYM pliku opisow stagingu ==="
for f in EdSharp.md Hotkeys.ini hotkeys.txt; do
  N=$(grep -ac 'Control+Shift+C copies its name alone\|Control+Shift+C just the file names' "$STAGE/$f")
  echo "  $f: $N"
  [ "$N" -ge 1 ] || { echo "  BRAK nowej frazy w $f"; exit 1; }
done

echo "=== 3. KONTROLA NEGATYWNA: stara fraza zero wystapien ==="
for f in EdSharp.md Hotkeys.ini hotkeys.txt; do
  N=$(grep -ac 'Control+C copies its path, Delete removes' "$STAGE/$f")
  echo "  $f: $N (ma byc 0)"
  [ "$N" -eq 0 ] || { echo "  STARA fraza nadal w $f"; exit 1; }
done

echo "=== 4. wersja w .iss stagingu ==="
grep -aE '^(AppVersion|AppVerName)=' "$STAGE/EdSharp_Setup.iss"

echo "=== 5. nowy kod w binarce stagingu (napisy) ==="
# Literale .NET siedza w binarce jako UTF-16 LE, a nazwy metod jako UTF-8 w
# heapie metadanych - grep po ASCII daje na obu FALSZYWE ZERO.  Rozstrzyga
# osobna sonda, ktora czyta oba kodowania i ma wlasna kontrole.
python3 testy/napisy_w_binarce_569.py || exit 1
echo -n "  SelectionMode.One na liscie plikow w zrodle: "; grep -c 'lst.SelectionMode = SelectionMode.One' EdSharp.cs || true

echo "=== 6. czasy: paczka nowsza od binarki i od opisow ==="
ls -la --time-style=full-iso "$PACZKA" EdSharpNG.exe "$STAGE/EdSharp.md" "$STAGE/Hotkeys.ini" "$STAGE/hotkeys.txt" | awk '{print "  "$6" "$7" "$9}'
[ "$PACZKA" -nt EdSharpNG.exe ] && echo "  paczka NOWSZA od binarki" || { echo "  paczka STARSZA od binarki"; exit 1; }

echo "=== 7. rozmiar i suma paczki ==="
ls -l "$PACZKA" | awk '{print "  "$5" B"}'
md5sum "$PACZKA"
