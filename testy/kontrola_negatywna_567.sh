#!/usr/bin/env bash
# Kontrola negatywna dla poprawki 5.0.67 (numer wiersza schodzi z pozycji
# listy odsylaczy do dopowiedzenia strzalka w lewo).
#
# Buduje binarke z rewizji SPRZED zmiany i uruchamia na niej TE SAMA sonde
# pomiar_567. Wynik MUSI byc rozny od 12/0 - inaczej sonda nie rozroznia
# naprawy od stanu poprzedniego i zielony wynik nie dowodzi niczego.
#
# Uzycie: bash testy/kontrola_negatywna_567.sh [rewizja_odniesienia]
#
# PULAPKA ZMIERZONA 04.09.2026 (pierwsza wersja tego skryptu): lista plikow
# zrodlowych byla przepisana Z PAMIECI i brakowalo w niej Web.cs oraz
# wszystkich /reference do bibliotek. csc zwrocil blad, a rownolegle w logu
# stala linia o awarii interopu WSL - wygladalo to wiec na przejsciowa awarie
# srodowiska, choc bylo brakiem pliku w MOIM wywolaniu. REGULA: liste plikow i
# referencji bierz Z BuildEdSharp.cmd, nie z pamieci, i nie zwalaj na interop,
# dopoki nie sprawdzisz wlasnego polecenia.
set -u
ROOT=/mnt/d/projekty/edsharp-pr
REF=${1:-791da51}
CSC=/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe
export PATH="$PATH:/mnt/c/Windows/System32"

TMP=/mnt/c/EdSharp/kn567
TMPWIN='C:\EdSharp\kn567'
rm -rf "$TMP"; mkdir -p "$TMP"

echo "=== KONTROLA NEGATYWNA 567, rewizja odniesienia: $REF ==="

# Pliki zrodlowe DOKLADNIE te, ktore wymienia BuildEdSharp.cmd (linia 106).
PLIKI="EdSharp.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs"
for f in $PLIKI; do
  if ! git -C "$ROOT" show "$REF:$f" > "$TMP/$f" 2>/dev/null; then
    echo "PRZERWANE: nie ma $f w rewizji $REF"
    exit 2
  fi
  echo "  pobrany $f ($(wc -c < "$TMP/$f") B)"
done

# Biblioteki i manifest z drzewa roboczego - nie sa czescia mierzonej zmiany.
for f in Tektosyne.dll Ude.dll EdSharp.manifest; do
  [ -f "$ROOT/$f" ] && cp "$ROOT/$f" "$TMP/" && echo "  skopiowany $f"
done

# KONTROLA ZRODLA: rewizja odniesienia MUSI zawierac stary ksztalt wiersza.
# Bez tego "nie widze zmiany" znaczy tylko "patrze w zle miejsce".
STARY=$(grep -c ', line " + iLine' "$TMP/EdSharp.cs" 2>/dev/null); STARY=${STARY:-0}
echo "  stary ksztalt (', line \" + iLine') w rewizji $REF: $STARY wystapien"
if [ "$STARY" -lt 1 ]; then
  echo "PRZERWANE: rewizja odniesienia nie zawiera starego ksztaltu - zla rewizja."
  exit 2
fi

# REFERENCJE: DOKLADNIE te, ktore podaje BuildEdSharp.cmd (linie 57-65 i 106).
# UI Automation jest OBOWIAZKOWA - Say.cs, Lbc.cs, Inix.cs i EdSharp.cs maja
# "using System.Windows.Automation.Provider", wiec bez tych dwoch bibliotek
# build sypie kilkunastoma CS0234/CS0246. Pierwsza wersja tego skryptu ich nie
# podawala i wygladalo to na awarie interopu. Sciezki jak w BuildEdSharp.cmd:
# najpierw Reference Assemblies .NET 4.8, w razie braku GAC.
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
echo "  UIAutomationProvider: $UIAPROV"
echo "  UIAutomationTypes: $UIATYPES"

REFS="/reference:Microsoft.VisualBasic.dll /reference:Microsoft.CSharp.dll"
REFS="$REFS /reference:$UIAPROV /reference:$UIATYPES"
[ -f "$TMP/Tektosyne.dll" ] && REFS="$REFS /reference:$TMPWIN\\Tektosyne.dll"
# Ude.dll wymaga TEZ symbolu HAVEUDE - inaczej kod warunkowy sie nie skompiluje.
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
    > /tmp/kn567_csc.txt 2>&1
  [ -f "$BINWSL" ] && { echo "  zbudowana (proba $p, $(wc -c < "$BINWSL") B)"; break; }
  sleep 20
done
if [ ! -f "$BINWSL" ]; then
  echo "PRZERWANE: binarka odniesienia nie powstala. Ogon logu csc:"
  grep -av "UtilAcceptVsock" /tmp/kn567_csc.txt | tail -15
  exit 3
fi

# Sonda na binarce odniesienia. Czekamy na linie PODSUMOWANIE - jej brak
# znaczy, ze sonda w ogole nie wystartowala (martwy kanal = falszywe zero).
WYN=""
for p in $(seq 1 12); do
  WYN=$(cd "$ROOT" && cmd.exe /c "testy\\out_567.exe $TMPWIN\\EdSharpNG_$REF.exe" 2>&1)
  echo "$WYN" | grep -q PODSUMOWANIE && break
  sleep 20
done

echo
echo "=== WYNIK NA BINARCE ODNIESIENIA ($REF) ==="
echo "$WYN"
echo

if ! echo "$WYN" | grep -q PODSUMOWANIE; then
  echo "NIEROZSTRZYGNIETE: sonda nie wystartowala (brak linii PODSUMOWANIE)."
  exit 4
fi

ZLE=$(echo "$WYN" | grep -c "^ZLE")
echo "Asercji oblanych na wersji sprzed zmiany: $ZLE"
if [ "$ZLE" -ge 1 ]; then
  echo "KONTROLA WAZNA: sonda ROZROZNIA wersje przed i po zmianie."
else
  echo "KONTROLA NIEWAZNA: sonda przechodzi TAKZE na starej wersji -"
  echo "zielony wynik na nowej binarce nie dowodzi niczego. Popraw sonde."
  exit 5
fi
