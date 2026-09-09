#!/usr/bin/env bash
# KONTROLA NEGATYWNA 5.0.73: sonda pomiar_573.cs na binarce SPRZED zmiany
# (rewizja 4a3a644, wersja 5.0.72) MUSI dac wynik ROZNY - inaczej zielone 23/0
# na nowej binarce nie odroznia naprawy od sondy, ktora nic nie mierzy.
#
# Prog: co najmniej 3 oblane asercje, czyli tyle, ile jest RODZIN zmiany
# (opcje pozycji menu Toggle/Clear Favorite, nowy komunikat Clear Favorite,
# numer wersji).  Prog na >= 1 dopuszczalby stan, w ktorym sonda rozroznia
# tylko jedna rodzine, a pozostale mierzy glucho.
#
# CO MA PRZEJSC NA OBU BINARKACH (nie asercje zmiany, a wymagania zachowane):
#   - obie KONTROLE SONDY (czytanie opcji dziala i rozroznia speak od silent),
#   - komunikaty 'Added to favorites' i 'Removed from favorites' (byly juz w
#     5.0.72 - to przelacznik z 31.08),
#   - nosniki z 5.0.70 i 5.0.72 (regresja sasiedztwa).
# Gdyby ktorekolwiek z tych oblalo tutaj, znaczylo by to, ze mierzymy zla
# rewizje albo ze sonda patrzy w zly typ.
#
# KATALOG ROBOCZY NA DYSKU WINDOWS: dla .NET /tmp jest lokalizacja siecowa
# (\\wsl.localhost), Assembly.LoadFile rzuca NotSupportedException i sonda nie
# startuje wcale (falszywe "0 OK / 0 ZLE" zmierzone przy 5.0.70).
set -uo pipefail

REPO="/mnt/d/projekty/edsharp-pr"
REWIZJA="${1:-4a3a644}"
CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
PRACA="/mnt/c/edsharp-kn573"
PRACA_WIN='C:\edsharp-kn573'

cd "$REPO"
rm -rf "$PRACA"; mkdir -p "$PRACA"

echo "== KONTROLA ZRODLA: czy rewizja $REWIZJA naprawde ma STARY ksztalt =="
# ZRODLO WYCIAGAMY DO PLIKU, nie do potoku z grep -q: grep -q zamyka potok po
# pierwszym trafieniu, git show dostaje SIGPIPE i konczy kodem 141, a przy
# pipefail caly potok jest porazka (falszywe "BRAK" zmierzone przy 5.0.71).
git show "$REWIZJA:EdSharp.cs" > "$PRACA/stara_EdSharp.cs" \
    || { echo "BLAD: nie moge odczytac EdSharp.cs z $REWIZJA" >&2; exit 4; }
STARE="$PRACA/stara_EdSharp.cs"

WARUNKI=0
grep -q 'CreateMenuItem("Toggle Favorite", "Alt+Shift+L", menuItem_Click, "child speak")' "$STARE" \
    && { echo "  OK  Toggle Favorite ma tam STARE opcje 'child speak'"; WARUNKI=$((WARUNKI+1)); } \
    || echo "  BRAK starych opcji 'child speak' przy Toggle Favorite - to NIE jest rewizja sprzed zmiany"
grep -q 'Not in favorites' "$STARE" \
    && echo "  UWAGA: nowy komunikat 'Not in favorites' JUZ tam jest" \
    || { echo "  OK  braku nowego komunikatu 'Not in favorites'"; WARUNKI=$((WARUNKI+1)); }
grep -q 'Added to favorites' "$STARE" \
    && { echo "  OK  komunikat 'Added to favorites' jest (ma zostac po zmianie)"; WARUNKI=$((WARUNKI+1)); } \
    || echo "  BRAK komunikatu 'Added to favorites'"
grep -q 'VersionString = "5.0.72"' "$STARE" \
    && { echo "  OK  stara stala wersji 5.0.72"; WARUNKI=$((WARUNKI+1)); } \
    || echo "  BRAK starej stalej wersji 5.0.72"

if [[ "$WARUNKI" -lt 4 ]]; then
    echo "AWARIA KONTROLI: rewizja $REWIZJA nie ma starego ksztaltu ($WARUNKI/4)." >&2
    echo "Podaj wlasciwa rewizje odniesienia - inaczej wynik nic nie znaczy." >&2
    exit 4
fi

mkdir -p "$PRACA/src"
# Pliki zrodlowe programu z tamtej rewizji.  Unicode.cs NIE wchodzi do binarki
# (BuildEdSharp.cmd go nie kompiluje), wiec nie bierzemy go tutaj.
for f in EdSharp.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs; do
    git show "$REWIZJA:$f" > "$PRACA/src/$f" || { echo "BLAD: brak $f w $REWIZJA" >&2; exit 4; }
done
for d in Tektosyne.dll Ude.dll; do
    [[ -e "$REPO/$d" ]] && cp -a "$REPO/$d" "$PRACA/src/"
done

echo
echo "== BUDOWA BINARKI Z REWIZJI $REWIZJA =="
# REFERENCJE JAK W BuildEdSharp.cmd, nie zgadywane: plik budowy pisze
# '/reference:', nie '/r:' - regex po '/r:' dawal PUSTA liste i build padal na
# 24 bledy CS0234/CS0246 przy NIEZMIENIONYM zrodle (falszywe "nie da sie
# zbudowac odniesienia", zmierzone przy 5.0.71).
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
cp -a testy/pomiar_573.cs "$PRACA/pomiar.cs"
"$CSC" /nologo /target:exe "/out:${PRACA_WIN}\\sonda.exe" \
    "/r:${PRACA_WIN}\\stara.exe" "${PRACA_WIN}\\pomiar.cs" >"$PRACA/sonda.log" 2>&1
if [[ ! -s "$PRACA/sonda.exe" ]]; then
    echo "BLAD: nie skompilowalem sondy przeciw starej binarce. Log:" >&2
    tail -25 "$PRACA/sonda.log" >&2
    exit 4
fi

WYNIK=""
for proba in 1 2 3; do
    WYNIK=$(/mnt/c/Windows/System32/cmd.exe /c "${PRACA_WIN}\\sonda.exe ${PRACA_WIN}\\stara.exe" 2>&1)
    printf '%s\n' "$WYNIK" | grep -q 'WYNIK:' && break
    echo "  proba $proba: brak linii podsumowania (interop?), ponawiam"
    sleep 20
done
echo "$WYNIK"

# LICZNIK LAPIACY OBA FORMATY ASERCJI ('OK ' i 'OK:') - lekcja z 5.0.72.
LICZ_OK=$(printf '%s\n' "$WYNIK" | grep -cE '^OK[ :]' || true)
LICZ_ZLE=$(printf '%s\n' "$WYNIK" | grep -cE '^ZLE[ :]' || true)
echo
echo "STARA BINARKA: $LICZ_OK OK / $LICZ_ZLE ZLE"

# BRAMKA: zero asercji RAZEM to awaria pomiaru, nie wynik.
if [[ $((LICZ_OK + LICZ_ZLE)) -eq 0 ]]; then
    echo "AWARIA POMIARU: sonda nie wykonala ANI JEDNEJ asercji (nie wystartowala)." >&2
    exit 5
fi

# KONTROLE SONDY MUSZA PRZEJSC TEZ TUTAJ: czytanie opcji pozycji menu dziala w
# obu wersjach, wiec ich oblanie znaczylo by, ze sonda patrzy w zly typ.
printf '%s\n' "$WYNIK" | grep -q "^OK   KONTROLA SONDY: czytanie opcji dziala" \
    || { echo "AWARIA POMIARU: kontrola sondy (New=speak) oblala na starej binarce." >&2; exit 5; }
printf '%s\n' "$WYNIK" | grep -q "^OK   KONTROLA SONDY: czytanie opcji rozroznia" \
    || { echo "AWARIA POMIARU: kontrola sondy (Recent=silent) oblala na starej binarce." >&2; exit 5; }
# ROZLACZNOSC: komunikaty przelacznika z 31.08 byly juz w 5.0.72.
printf '%s\n' "$WYNIK" | grep -q "^OK   komunikat 'Added to favorites' jest" \
    || { echo "AWARIA POMIARU: 'Added to favorites' oblalo na STAREJ binarce - zla rewizja." >&2; exit 5; }
# REGRESJA SASIEDZTWA: nosniki z 5.0.70 i 5.0.72 sa w obu wersjach.
printf '%s\n' "$WYNIK" | grep -q "^OK   REGRESJA: nosnik z 5.0.72" \
    || { echo "AWARIA POMIARU: nosnik z 5.0.72 nie widziany na starej binarce - zla rewizja." >&2; exit 5; }

# ROZKLAD OBLANYCH NA RODZINY: prog liczbowy sam nie mowi, ze wszystkie trzy
# rodziny sa rozrozniane - trzy oblania w JEDNEJ rodzinie tez daja 3.
RODZINY=0
printf '%s\n' "$WYNIK" | grep -q "^ZLE  Toggle Favorite ma opcje 'silent'" \
    && { echo "  rodzina OPCJE MENU: oblana (jak trzeba)"; RODZINY=$((RODZINY+1)); } \
    || echo "  rodzina OPCJE MENU: NIE oblana - sonda jej nie rozroznia"
printf '%s\n' "$WYNIK" | grep -q "^ZLE  NOWY komunikat 'Not in favorites'" \
    && { echo "  rodzina MOWA CLEAR FAVORITE: oblana (jak trzeba)"; RODZINY=$((RODZINY+1)); } \
    || echo "  rodzina MOWA CLEAR FAVORITE: NIE oblana - sonda jej nie rozroznia"
printf '%s\n' "$WYNIK" | grep -q "^ZLE  App.VersionString to 5.0.73" \
    && { echo "  rodzina WERSJA: oblana (jak trzeba)"; RODZINY=$((RODZINY+1)); } \
    || echo "  rodzina WERSJA: NIE oblana - sonda jej nie rozroznia"

echo
if [[ "$LICZ_ZLE" -ge 3 && "$RODZINY" -ge 3 ]]; then
    echo "KONTROLA NEGATYWNA PASS: sonda rozroznia stan przed i po ($LICZ_ZLE oblanych, $RODZINY/3 rodzin)."
    exit 0
fi
echo "KONTROLA NEGATYWNA FAIL: $LICZ_ZLE oblanych, $RODZINY/3 rodzin rozroznianych (prog 3 i 3)." >&2
exit 1
