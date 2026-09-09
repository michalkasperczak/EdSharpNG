#!/bin/bash
# KONTROLA NEGATYWNA dla 5.0.38: edycja gotowej tabeli + wiersz kreskek
# jednokolumnowy.  Buduje binarke SPRZED zmiany, odtwarza na niej objaw,
# przywraca nowa i pilnuje, ze w repo zostala NOWA (porownanie sha256).
#
# Rewizja odniesienia w argumencie, JAWNIE - skrypt biorący HEAD po commicie
# porownywalby kod z samym soba i dawal 0 padnietych asercji.
set -u
REPO=/mnt/d/projekty/edsharp-pr
REV="${1:-614d6e6}"
CSC=/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe
POM=/mnt/c/tmp/tabedit
POM2=/mnt/c/tmp/tab1col

cd "$REPO" || exit 3
SHA_NOWA=$(sha256sum EdSharpNG.exe | cut -d' ' -f1)
cp EdSharpNG.exe /tmp/EdSharpNG_nowy_538.exe

echo "== KONTROLA POZYTYWNA: nowa binarka musi PRZECHODZIC =="
cp EdSharpNG.exe "$POM/" && cp EdSharpNG.exe "$POM2/"
/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\\tmp\\tabedit && t.exe EdSharpNG.exe" > /tmp/nowa_t.log 2>&1
NOWA_T=$(grep -c '^PASS' /tmp/nowa_t.log)
/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\\tmp\\tab1col && h.exe EdSharpNG.exe" > /tmp/nowa_h.log 2>&1
NOWA_H=$?
echo "  nowa binarka: sonda edycji $NOWA_T PASS, eksport HTML kod $NOWA_H (0 = jednokolumnowa jest tabela)"
if [ "$NOWA_T" -lt 31 ] || [ "$NOWA_H" -ne 0 ]; then
  echo "SONDA GLUCHA: nowa binarka nie przechodzi wlasnych asercji - wynik nic nie znaczy."
  exit 2
fi

echo
echo "== BUDUJE BINARKE SPRZED ZMIANY (rewizja $REV) =="
git show "$REV:EdSharp.cs" > /tmp/EdSharp_stary.cs || exit 3
cp EdSharp.cs /tmp/EdSharp_biezacy.cs
cp /tmp/EdSharp_stary.cs EdSharp.cs
/mnt/c/Windows/System32/cmd.exe /c "cd /d D:\\projekty\\edsharp-pr && BuildEdSharp.cmd" > /tmp/build_stary_538.log 2>&1
if ! grep -q "Build complete" /tmp/build_stary_538.log; then
  echo "Build starego kodu PADL - przywracam i koncze."
  cp /tmp/EdSharp_biezacy.cs EdSharp.cs
  cp /tmp/EdSharpNG_nowy_538.exe EdSharpNG.exe
  exit 3
fi

cp EdSharpNG.exe "$POM/" && cp EdSharpNG.exe "$POM2/"
/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\\tmp\\tabedit && t.exe EdSharpNG.exe" > /tmp/stara_t.log 2>&1
STARA_T=$(grep -c '^PASS' /tmp/stara_t.log)
STARA_F=$(grep -c '^FAIL' /tmp/stara_t.log)
BRAK=$(grep -c 'BRAK metody\|BRAK Find\|Unhandled' /tmp/stara_t.log)
/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\\tmp\\tab1col && h.exe EdSharpNG.exe" > /tmp/stara_h.log 2>&1
STARA_H=$?
echo "  stara binarka: sonda edycji $STARA_T PASS / $STARA_F FAIL (wyjatek o braku metody: $BRAK)"
echo "  stara binarka: eksport HTML kod $STARA_H (1 = jednokolumnowa NIE byla tabela, czyli objaw odtworzony)"
grep -E 'JEDNOKOLUMNOWA|DWUKOLUMNOWA' /tmp/stara_h.log | sed 's/^/    /'

echo
echo "== PRZYWRACAM NOWY KOD I NOWA BINARKE =="
cp /tmp/EdSharp_biezacy.cs EdSharp.cs
# PULAPKA ZMIERZONA 28.08.2026: przywracajacy build potrafi NIE dojsc do skutku
# (WSL zgubil wywolanie cmd.exe, log pusty), a w repo zostaje wtedy binarka ze
# STAREGO kodu - czyli paczka bez naprawy.  Dlatego probujemy do trzech razy i
# rozstrzygamy po TIMESTAMPIE, nie po kodzie wyjscia.
for i in 1 2 3; do
  # Pauza przed buildem: interop WSL do cmd.exe zawodzi po serii wywolan
  # ("accept4 failed 110", pusty log), a wtedy build w ogole sie nie odpala.
  sleep 5
  /mnt/c/Windows/System32/cmd.exe /c "cd /d D:\\projekty\\edsharp-pr && BuildEdSharp.cmd" > /tmp/build_nowy_538.log 2>&1
  if [ EdSharpNG.exe -nt EdSharp.cs ]; then break; fi
  echo "  proba $i: binarka nie jest mlodsza od zrodla, powtarzam build"
done
if [ ! EdSharpNG.exe -nt EdSharp.cs ]; then
  echo "AWARIA PRZYWRACANIA: w repo lezy binarka STARSZA niz zrodlo."
  echo "NIE WYSYLAJ paczki - zbuduj recznie BuildEdSharp.cmd i sprawdz sonde."
  exit 3
fi
SHA_PO=$(sha256sum EdSharpNG.exe | cut -d' ' -f1)
cp EdSharpNG.exe "$POM/" && cp EdSharpNG.exe "$POM2/"
if [ "$SHA_PO" != "$SHA_NOWA" ]; then
  echo "UWAGA: przebudowana binarka ma inny sha256 niz wyjsciowa."
  echo "  bylo: $SHA_NOWA"
  echo "  jest: $SHA_PO"
  echo "  (rozny sha przy identycznym zrodle jest normalny - .NET stempluje MVID.)"
fi
/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\\tmp\\tabedit && t.exe EdSharpNG.exe" > /tmp/powrot_t.log 2>&1
POWROT=$(grep -c '^PASS' /tmp/powrot_t.log)
echo "  po przywroceniu: $POWROT PASS (musi byc 31)"

echo
if [ "$STARA_H" -eq 1 ] && [ "$POWROT" -ge 31 ]; then
  echo "WYNIK: objaw ODTWORZONY na kodzie sprzed zmiany, naprawa dziala na biezacym."
  exit 0
fi
echo "WYNIK: kontrola NIE rozstrzyga - przeczytaj logi /tmp/stara_*.log."
exit 1
