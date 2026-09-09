#!/usr/bin/env bash
# Kontrola negatywna dla poprawek 5.0.68 (fokus po zamknieciu menu, ponawianie
# zapisu do schowka na kazdej drodze, wielokrotny wybor Shiftem na listach).
#
# Buduje binarke z rewizji SPRZED zmiany i uruchamia na niej TE SAMA sonde
# pomiar_568.  Wynik MUSI byc rozny od 30/0 - inaczej sonda nie rozroznia
# naprawy od stanu poprzedniego i zielony wynik nie dowodzi niczego.
#
# Uzycie: bash testy/kontrola_negatywna_568.sh [rewizja_odniesienia]
#
# PULAPKI ODZIEDZICZONE (zmierzone przy 5.0.67, nie powtarzaj ich):
# - liste plikow zrodlowych i referencji bierz Z BuildEdSharp.cmd, nie z
#   pamieci; brak Web.cs albo referencji do UI Automation daje blad csc, ktory
#   przy rownoleglej awarii interopu WSL wyglada na awarie srodowiska,
# - Ude.dll wymaga TEZ symbolu HAVEUDE,
# - brak linii PODSUMOWANIE = sonda nie wystartowala (martwy kanal, nie zero).
set -u
ROOT=/mnt/d/projekty/edsharp-pr
REF=${1:-0ab8a5e}
CSC=/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe
export PATH="$PATH:/mnt/c/Windows/System32"

TMP=/mnt/c/EdSharp/kn568
TMPWIN='C:\EdSharp\kn568'
rm -rf "$TMP"; mkdir -p "$TMP"

echo "=== KONTROLA NEGATYWNA 568, rewizja odniesienia: $REF ==="

PLIKI="EdSharp.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs"
for f in $PLIKI; do
  if ! git -C "$ROOT" show "$REF:$f" > "$TMP/$f" 2>/dev/null; then
    echo "PRZERWANE: nie ma $f w rewizji $REF"
    exit 2
  fi
  echo "  pobrany $f ($(wc -c < "$TMP/$f") B)"
done

for f in Tektosyne.dll Ude.dll EdSharp.manifest; do
  [ -f "$ROOT/$f" ] && cp "$ROOT/$f" "$TMP/" && echo "  skopiowany $f"
done

# KONTROLA ZRODLA: rewizja odniesienia MUSI zawierac STARY ksztalt wszystkich
# trzech rodzin.  Bez tego "nie widze zmiany" znaczy tylko "patrze w zle
# miejsce", a nie "zmiana zadzialala".
STARY_FOKUS=$(grep -c 'this.Activated += delegate' "$TMP/EdSharp.cs" 2>/dev/null); STARY_FOKUS=${STARY_FOKUS:-0}
NOWY_FOKUS=$(grep -c 'FocusChildEditControl' "$TMP/EdSharp.cs" 2>/dev/null); NOWY_FOKUS=${NOWY_FOKUS:-0}
STARY_SCHOWEK=$(grep -c 'public static void SetClipboardText' "$TMP/EdSharp.cs" 2>/dev/null); STARY_SCHOWEK=${STARY_SCHOWEK:-0}
NOWY_TRYB=$(grep -c 'SelectionMode.MultiExtended' "$TMP/Lbc.cs" 2>/dev/null); NOWY_TRYB=${NOWY_TRYB:-0}
echo "  rewizja $REF: Activated jako domkniecie=$STARY_FOKUS, FocusChildEditControl=$NOWY_FOKUS,"
echo "               SetClipboardText jako void=$STARY_SCHOWEK, MultiExtended=$NOWY_TRYB"
if [ "$STARY_FOKUS" -lt 1 ] || [ "$NOWY_FOKUS" -ne 0 ] || [ "$STARY_SCHOWEK" -lt 1 ] || [ "$NOWY_TRYB" -ne 0 ]; then
  echo "PRZERWANE: rewizja odniesienia nie jest stanem SPRZED zmiany - zla rewizja."
  exit 2
fi

GAC='C:\Windows\Microsoft.NET\assembly\GAC_MSIL'
GACWSL=/mnt/c/Windows/Microsoft.NET/assembly/GAC_MSIL
UIAPROV=""; UIATYPES=""
REFDIRWSL="/mnt/c/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8"
REFDIRWIN='C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'
if [ -f "$REFDIRWSL/UIAutomationProvider.dll" ]; then
  UIAPROV="$REFDIRWIN\\UIAutomationProvider.dll"
elif [ -f "$GACWSL/UIAutomationProvider/v4.0_4.0.0.0__31bf3856ad364e35/UIAutomationProvider.dll" ]; then
  UIAPROV="$GAC\\UIAutomationProvider\\v4.0_4.0.0.0__31bf3856ad364e35\\UIAutomationProvider.dll"
fi
if [ -f "$REFDIRWSL/UIAutomationTypes.dll" ]; then
  UIATYPES="$REFDIRWIN\\UIAutomationTypes.dll"
elif [ -f "$GACWSL/UIAutomationTypes/v4.0_4.0.0.0__31bf3856ad364e35/UIAutomationTypes.dll" ]; then
  UIATYPES="$GAC\\UIAutomationTypes\\v4.0_4.0.0.0__31bf3856ad364e35\\UIAutomationTypes.dll"
fi
if [ -z "$UIAPROV" ] || [ -z "$UIATYPES" ]; then
  echo "PRZERWANE: brak UIAutomationProvider.dll albo UIAutomationTypes.dll."
  exit 2
fi

REFS="/reference:Microsoft.VisualBasic.dll /reference:Microsoft.CSharp.dll"
REFS="$REFS /reference:$UIAPROV /reference:$UIATYPES"
[ -f "$TMP/Tektosyne.dll" ] && REFS="$REFS /reference:$TMPWIN\\Tektosyne.dll"
DEFS=""
if [ -f "$TMP/Ude.dll" ]; then
  REFS="$REFS /reference:$TMPWIN\\Ude.dll"
  DEFS="/define:HAVEUDE"
fi

SRC=""
for f in $PLIKI; do SRC="$SRC $TMPWIN\\$f"; done
BINWIN="$TMPWIN\\EdSharpNG_$REF.exe"
BINWSL="$TMP/EdSharpNG_$REF.exe"

echo "  buduje binarke odniesienia..."
for p in $(seq 1 12); do
  # shellcheck disable=SC2086
  "$CSC" /nologo /target:winexe /platform:anycpu $DEFS "/out:$BINWIN" $REFS $SRC \
    > /tmp/kn568_csc.txt 2>&1
  [ -f "$BINWSL" ] && { echo "  zbudowana (proba $p, $(wc -c < "$BINWSL") B)"; break; }
  sleep 20
done
if [ ! -f "$BINWSL" ]; then
  echo "PRZERWANE: binarka odniesienia nie powstala. Ogon logu csc:"
  grep -av "UtilAcceptVsock" /tmp/kn568_csc.txt | tail -15
  exit 3
fi

WYN=""
for p in $(seq 1 12); do
  WYN=$(cd "$ROOT" && cmd.exe /c "testy\\out_568.exe $TMPWIN\\EdSharpNG_$REF.exe" 2>&1)
  echo "$WYN" | grep -q PODSUMOWANIE && break
  sleep 20
done

echo
echo "=== WYNIK NA BINARCE ODNIESIENIA ($REF) ==="
echo "$WYN" | grep -av UtilAcceptVsock
echo

if ! echo "$WYN" | grep -q PODSUMOWANIE; then
  echo "NIEROZSTRZYGNIETE: sonda nie wystartowala (brak linii PODSUMOWANIE)."
  exit 4
fi

ZLE=$(echo "$WYN" | grep -c "^ZLE")
echo "Asercji oblanych na wersji sprzed zmiany: $ZLE"
# Trzy rodziny zmian, wiec zadamy WIECEJ niz jednej oblanej asercji: jedna
# oblana moglaby znaczyc, ze sonda rozroznia tylko jedna z trzech rzeczy, a
# dwie pozostale mierzy glucho.
if [ "$ZLE" -ge 3 ]; then
  echo "KONTROLA WAZNA: sonda ROZROZNIA wersje przed i po zmianie we wszystkich trzech rodzinach."
else
  echo "KONTROLA SLABA albo NIEWAZNA: na starej wersji oblalo tylko $ZLE asercji."
  echo "Sprawdz, ktora rodzina zmian nie jest rozrozniana, i popraw sonde."
  exit 5
fi
