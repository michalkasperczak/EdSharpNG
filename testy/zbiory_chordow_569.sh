#!/usr/bin/env bash
# ZBIORY CHORDOW przed i po zmianie 5.0.69: pelne porownanie w obie strony
# plus kontrola duplikatow.  Skrotow nie ruszalem, wiec to KONTROLA, ze zmiana
# ich nie tknela - sama liczebnosc bez roznicy zbiorow by nie wystarczyla.
set -u
cd /mnt/d/projekty/edsharp-pr
REF=${1:-7462516}
git show "$REF:EdSharp.cs" > /tmp/przed569.cs

wyciag() {
  grep -oP 'CreateMenuItem\("[^"]*",\s*"\K[^"]+' "$1" | sort -u
}
wyciag /tmp/przed569.cs > /tmp/chord_przed.txt
wyciag EdSharp.cs > /tmp/chord_po.txt

echo "chordow przed: $(wc -l < /tmp/chord_przed.txt)"
echo "chordow po:    $(wc -l < /tmp/chord_po.txt)"
echo "--- ubylo (przed, nie ma po) ---"; comm -23 /tmp/chord_przed.txt /tmp/chord_po.txt
echo "--- doszlo (po, nie bylo przed) ---"; comm -13 /tmp/chord_przed.txt /tmp/chord_po.txt
echo "--- duplikaty w nowym kodzie (jeden chord na dwoch komendach) ---"
grep -oP 'CreateMenuItem\("[^"]*",\s*"\K[^"]+' EdSharp.cs | sort | uniq -d
echo "(pusto powyzej = brak duplikatow)"

echo "--- fraza nowa i stara w plikach opisow ---"
for f in EdSharp.md Hotkeys.ini hotkeys.txt; do
  echo "$f: nowa='$(grep -c 'Control+Shift+C copies its name alone\|Control+Shift+C just the file names' "$f")' stara='$(grep -c 'Control+C copies its path, Delete removes' "$f")'"
done
