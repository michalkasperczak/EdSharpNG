#!/usr/bin/env bash
# KONTROLA NEGATYWNA 5.0.71: sonda pomiar_571.cs na binarce SPRZED zmiany
# (rewizja 83f1b52, wersja 5.0.70) MUSI dac wynik ROZNY - inaczej zielone 32/0
# na nowej binarce nie odroznia naprawy od sondy, ktora nic nie mierzy.
#
# Prog: co najmniej 4 oblane asercje, czyli tyle, ile jest RODZIN zmiany
# (wersja jako stala, okno About, nosnik daty, repozytorium aktualizacji,
# rozdzielenie braku wydania od awarii sieci).  Prog na >= 1 dopuszczalby stan,
# w ktorym sonda rozroznia tylko jedna rodzine, a pozostale mierzy glucho.
#
# KATALOG ROBOCZY NA DYSKU WINDOWS: dla .NET /tmp jest lokalizacja siecowa
# (\\wsl.localhost), Assembly.LoadFile rzuca NotSupportedException i sonda nie
# startuje wcale - to bylo falszywe "0 OK / 0 ZLE" zmierzone przy 5.0.70.
set -uo pipefail

REPO="/mnt/d/projekty/edsharp-pr"
REWIZJA="${1:-83f1b52}"
CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
PRACA="/mnt/c/edsharp-kn571"
PRACA_WIN='C:\edsharp-kn571'

cd "$REPO"
rm -rf "$PRACA"; mkdir -p "$PRACA"

echo "== KONTROLA ZRODLA: czy rewizja $REWIZJA naprawde ma STARY ksztalt =="
# Bez tego "nie widze zmiany" moglo by znaczyc "patrze w zla rewizje".
#
# ZRODLO WYCIAGAMY DO PLIKU, nie do potoku z grep -q.  Pulapka zmierzona w tym
# biegu: grep -q zamyka potok po pierwszym trafieniu, git show dostaje SIGPIPE
# i konczy kodem 141, a przy set -o pipefail CALY potok jest porazka - wiec
# WSZYSTKIE trzy warunki raportowaly "BRAK" przy rewizji, ktora te napisy MA.
# To bylo falszywe zero z narzedzia, nie z rewizji.
git show "$REWIZJA:EdSharp.cs" > "$PRACA/stara_EdSharp.cs" \
    || { echo "BLAD: nie moge odczytac EdSharp.cs z $REWIZJA" >&2; exit 4; }
STARE="$PRACA/stara_EdSharp.cs"

WARUNKI=0
grep -q 'VersionString = "5.0.0"' "$STARE" \
    && { echo "  OK  stara stala wersji 5.0.0"; WARUNKI=$((WARUNKI+1)); } \
    || echo "  BRAK starej stalej 5.0.0"
grep -q 'EdSharpNG 5.0.1 (beta)' "$STARE" \
    && { echo "  OK  stary literal okna About"; WARUNKI=$((WARUNKI+1)); } \
    || echo "  BRAK starego literalu okna About"
grep -q 'sOwnerRepo = "JamalMazrui/EdSharp"' "$STARE" \
    && { echo "  OK  stare repozytorium aktualizacji (upstream)"; WARUNKI=$((WARUNKI+1)); } \
    || echo "  BRAK starego repozytorium aktualizacji"
if grep -q 'GetProgramBuildDate' "$STARE"; then
    echo "  UWAGA: nosnik daty JUZ tam jest"
else
    echo "  OK  braku nosnika daty (jeszcze go nie ma)"; WARUNKI=$((WARUNKI+1))
fi

if [[ "$WARUNKI" -lt 4 ]]; then
    echo "AWARIA KONTROLI: rewizja $REWIZJA nie ma starego ksztaltu wszystkich rodzin ($WARUNKI/4)." >&2
    echo "Podaj wlasciwa rewizje odniesienia - inaczej wynik nic nie znaczy." >&2
    exit 4
fi

mkdir -p "$PRACA/src"
# Pliki zrodlowe programu z tamtej rewizji.  Unicode.cs NIE wchodzi do binarki
# (BuildEdSharp.cmd go nie kompiluje), wiec nie bierzemy go tutaj.
for f in EdSharp.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs; do
    git show "$REWIZJA:$f" > "$PRACA/src/$f" || { echo "BLAD: brak $f w $REWIZJA" >&2; exit 4; }
done
# Biblioteki obok zrodel - csc szuka ich wzgledem katalogu wywolania.
for d in Tektosyne.dll Ude.dll; do
    [[ -e "$REPO/$d" ]] && cp -a "$REPO/$d" "$PRACA/src/"
done

echo
echo "== BUDOWA BINARKI Z REWIZJI $REWIZJA =="
# REFERENCJE JAK W BuildEdSharp.cmd, nie zgadywane.  Pierwsza wersja tego
# skryptu wyciagala je regexem po '/r:' - a plik budowy pisze '/reference:',
# wiec lista wychodzila PUSTA i build padal na 24 bledy CS0234/CS0246 przy
# NIEZMIENIONYM zrodle.  To bylo falszywe "nie da sie zbudowac odniesienia".
UIA_PROV=$(ls /mnt/c/Windows/Microsoft.NET/assembly/GAC_MSIL/UIAutomationProvider/*/UIAutomationProvider.dll 2>/dev/null | head -1)
UIA_TYPES=$(ls /mnt/c/Windows/Microsoft.NET/assembly/GAC_MSIL/UIAutomationTypes/*/UIAutomationTypes.dll 2>/dev/null | head -1)
if [[ -z "$UIA_PROV" || -z "$UIA_TYPES" ]]; then
    echo "BLAD: nie znalazlem UIAutomationProvider/Types w GAC - build odniesienia nie ma szans." >&2
    exit 4
fi
UIA_PROV_WIN=$(printf '%s' "$UIA_PROV" | sed 's|^/mnt/c|C:|; s|/|\\|g')
UIA_TYPES_WIN=$(printf '%s' "$UIA_TYPES" | sed 's|^/mnt/c|C:|; s|/|\\|g')

UDE_DEF=""
UDE_REF=""
if [[ -e "$PRACA/src/Ude.dll" ]]; then
    UDE_DEF="/define:HAVEUDE"
    UDE_REF="/reference:Ude.dll"
fi

( cd "$PRACA/src" && "$CSC" /nologo /target:winexe /platform:anycpu /optimize+ \
    $UDE_DEF \
    /reference:"Tektosyne.dll" /reference:"Microsoft.VisualBasic.dll" \
    /reference:"Microsoft.CSharp.dll" \
    /reference:"$UIA_PROV_WIN" /reference:"$UIA_TYPES_WIN" \
    $UDE_REF \
    "/out:${PRACA_WIN}\\stara.exe" \
    EdSharp.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs ) >"$PRACA/build.log" 2>&1
if [[ ! -s "$PRACA/stara.exe" ]]; then
    echo "BLAD: nie zbudowalem binarki odniesienia. Log:" >&2
    tail -25 "$PRACA/build.log" >&2
    exit 4
fi

echo "== SONDA NA STAREJ BINARCE =="
cp -a testy/pomiar_571.cs "$PRACA/pomiar.cs"
"$CSC" /nologo /target:exe "/out:${PRACA_WIN}\\sonda.exe" \
    "/r:${PRACA_WIN}\\stara.exe" "${PRACA_WIN}\\pomiar.cs" >"$PRACA/sonda.log" 2>&1
if [[ ! -s "$PRACA/sonda.exe" ]]; then
    echo "BLAD: nie skompilowalem sondy przeciw starej binarce. Log:" >&2
    tail -25 "$PRACA/sonda.log" >&2
    exit 4
fi

WYNIK=$(/mnt/c/Windows/System32/cmd.exe /c "${PRACA_WIN}\\sonda.exe ${PRACA_WIN}\\stara.exe" 2>&1)
echo "$WYNIK"

LICZ_OK=$(printf '%s\n' "$WYNIK" | grep -c '^OK ' || true)
LICZ_ZLE=$(printf '%s\n' "$WYNIK" | grep -c '^ZLE ' || true)
echo
echo "STARA BINARKA: $LICZ_OK OK / $LICZ_ZLE ZLE"

# BRAMKA: zero asercji RAZEM to awaria pomiaru, nie wynik.
if [[ $((LICZ_OK + LICZ_ZLE)) -eq 0 ]]; then
    echo "AWARIA POMIARU: sonda nie wykonala ANI JEDNEJ asercji (nie wystartowala)." >&2
    exit 5
fi

if [[ "$LICZ_ZLE" -ge 4 ]]; then
    echo "KONTROLA NEGATYWNA PASS: sonda rozroznia stan przed i po ($LICZ_ZLE oblanych, prog 4)."
    exit 0
fi
echo "KONTROLA NEGATYWNA FAIL: tylko $LICZ_ZLE oblanych, prog to 4 (liczba rodzin zmiany)." >&2
exit 1
