#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.37: konce wiersza, kodowanie zapisu i mowa siatki.
#
# Dowod "przechodzi" nic nie znaczy sam z siebie - test moglby przechodzic tez
# PRZED naprawa.  Ten skrypt buduje binarke SPRZED zmiany, odpala na niej te same
# sondy i sprawdza, ze one PADAJA, a potem przywraca binarke nowa.
#
# Sam sprawdza swoja KONTROLE POZYTYWNA i konczy kodem 2, gdyby sonda byla glucha
# (np. czytala pusty plik albo nie znalazla typu w binarce) - wtedy wynik "pada"
# nie dowodzil by niczego.
set -u
cd /mnt/d/projekty/edsharp-pr || exit 2

CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
CMD="/mnt/c/Windows/System32/cmd.exe"
HARNESS=/mnt/c/tmp/kn537
NOWA=/tmp/EdSharpNG_nowy_537.exe

echo "=== 0. zachowuje NOWA binarke (bez tego przebieg zostawia stary kod w repo) ==="
cp EdSharpNG.exe "$NOWA" || exit 2
sha_nowa=$(sha256sum EdSharpNG.exe | cut -d' ' -f1)
echo "    sha256 nowej: $sha_nowa"

mkdir -p "$HARNESS"
cp testy/pomiar_koncow_wiersza.cs "$HARNESS/t.cs"
cp EdSharp.dll Tektosyne.dll Ude.dll "$HARNESS/" 2>/dev/null
(cd "$HARNESS" && "$CSC" /nologo /out:t.exe /t:exe t.cs > /dev/null 2>&1) || { echo "BLAD: harness sie nie kompiluje"; exit 2; }

echo
echo "=== 1. sonda na NOWEJ binarce - MUSI przejsc (kontrola pozytywna) ==="
cp "$NOWA" "$HARNESS/EdSharpNG.exe"
out_nowa=$("$CMD" /c "cd /d C:\\tmp\\kn537 && t.exe EdSharpNG.exe" < /dev/null 2>&1)
echo "$out_nowa" | tail -3
zal_nowa=$(echo "$out_nowa" | grep -o 'zaliczone: [0-9]* z [0-9]*' | tail -1)
echo "    -> $zal_nowa"
if ! echo "$out_nowa" | grep -q "DetectLineBreakKind istnieje" ; then
  :
fi
if echo "$out_nowa" | grep -q "\[FAIL \]" ; then
  echo "BLAD: sonda nie przechodzi na NOWEJ binarce - nie ma czego dowodzic"
  exit 2
fi

echo
echo "=== 2. buduje binarke SPRZED zmiany (git stash) ==="
git stash push -- EdSharp.cs Lbc.cs > /tmp/kn537_stash.log 2>&1
cat /tmp/kn537_stash.log
"$CMD" /c "cd /d D:\\projekty\\edsharp-pr && BuildEdSharp.cmd" < /dev/null > /tmp/kn537_build_stare.log 2>&1
if ! grep -q "Build complete" /tmp/kn537_build_stare.log ; then
  echo "BLAD: stara wersja sie nie zbudowala - przywracam i koncze"
  git stash pop
  cp "$NOWA" EdSharpNG.exe
  exit 2
fi
echo "    stara binarka zbudowana"

echo
echo "=== 3. sonda na STAREJ binarce - MUSZA padac nowe asercje ==="
cp EdSharpNG.exe "$HARNESS/EdSharpNG.exe"
out_stara=$("$CMD" /c "cd /d C:\\tmp\\kn537 && t.exe EdSharpNG.exe" < /dev/null 2>&1)
echo "$out_stara"
zal_stara=$(echo "$out_stara" | grep -o 'zaliczone: [0-9]* z [0-9]*' | tail -1)

echo
echo "=== 4. przywracam nowy kod i binarke ==="
git stash pop
cp "$NOWA" EdSharpNG.exe
sha_po=$(sha256sum EdSharpNG.exe | cut -d' ' -f1)
echo "    sha256 po przywroceniu: $sha_po"
if [ "$sha_po" != "$sha_nowa" ]; then
  echo "BLAD: w repo NIE lezy nowa binarka!"
  exit 2
fi

echo
echo "============================================================"
echo "WYNIK KONTROLI NEGATYWNEJ"
echo "  nowa binarka : $zal_nowa"
echo "  stara binarka: $zal_stara"
# Na starej binarce MUSI brakowac obu nowych metod.
brak1=$(echo "$out_stara" | grep -c "DetectLineBreakKind istnieje w binarce.*BRAK METODY")
brak2=$(echo "$out_stara" | grep -c "GetSaveEncoding istnieje w binarce.*BRAK METODY")
# KONTROLA POZYTYWNA: stare helpery konwersji MUSZA byc widoczne takze na starej
# binarce - inaczej sonda po prostu nie widzi zawartosci pliku.
kp=$(echo "$out_stara" | grep -c "Convert2WinLineBreak z mieszanki daje same CRLF")
echo "  na starej brakuje DetectLineBreakKind: $brak1 (ma byc 1)"
echo "  na starej brakuje GetSaveEncoding    : $brak2 (ma byc 1)"
echo "  KONTROLA POZYTYWNA sondy na starej   : $kp (ma byc 1)"
if [ "$kp" != "1" ]; then
  echo "SONDA GLUCHA: na starej binarce nie widze nawet starych helperow."
  echo "Wynik 'pada' nic nie dowodzi. Popraw sonde."
  exit 2
fi
if [ "$brak1" = "1" ] && [ "$brak2" = "1" ]; then
  echo "OK: obie nowe rzeczy sa NIEOBECNE sprzed zmiany, a obecne po zmianie."
  rm -rf "$HARNESS"
  exit 0
fi
echo "UWAGA: asercje NIE padly na starej binarce - sprawdz, czy mierzysz to co trzeba."
exit 1
