#!/usr/bin/env bash
# KONTROLA NEGATYWNA 5.0.72: sonda pomiar_572.cs na binarce SPRZED zmiany
# (rewizja e4bf903, wersja 5.0.71) MUSI dac wynik ROZNY - inaczej zielone 28/0
# na nowej binarce nie odroznia naprawy od sondy, ktora nic nie mierzy.
#
# Prog: co najmniej 4 oblane asercje, czyli tyle, ile jest RODZIN zmiany
# (nosnik zdejmowania, czytanie zaznaczenia, wolanie z listy plikow + zniknieta
# stara odmowa, mowa z liczba).  Prog na >= 1 dopuszczalby stan, w ktorym sonda
# rozroznia tylko jedna rodzine, a pozostale mierzy glucho.
#
# RODZINA 5 (Shift+Delete zostaje jednopozycyjny) MA przejsc na obu binarkach -
# to nie asercja zmiany, a wymaganie zachowane.  Gdyby oblewala tutaj, znaczylo
# by to, ze mierzymy zla rewizje.
#
# KATALOG ROBOCZY NA DYSKU WINDOWS: dla .NET /tmp jest lokalizacja siecowa
# (\\wsl.localhost), Assembly.LoadFile rzuca NotSupportedException i sonda nie
# startuje wcale (falszywe "0 OK / 0 ZLE" zmierzone przy 5.0.70).
set -uo pipefail

REPO="/mnt/d/projekty/edsharp-pr"
REWIZJA="${1:-e4bf903}"
CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
PRACA="/mnt/c/edsharp-kn572"
PRACA_WIN='C:\edsharp-kn572'

cd "$REPO"
rm -rf "$PRACA"; mkdir -p "$PRACA"

echo "== KONTROLA ZRODLA: czy rewizja $REWIZJA naprawde ma STARY ksztalt =="
# Bez tego "nie widze zmiany" moglo by znaczyc "patrze w zla rewizje".
#
# ZRODLO WYCIAGAMY DO PLIKU, nie do potoku z grep -q: grep -q zamyka potok po
# pierwszym trafieniu, git show dostaje SIGPIPE i konczy kodem 141, a przy
# pipefail caly potok jest porazka (falszywe "BRAK" zmierzone przy 5.0.71).
git show "$REWIZJA:EdSharp.cs" > "$PRACA/stara_EdSharp.cs" \
    || { echo "BLAD: nie moge odczytac EdSharp.cs z $REWIZJA" >&2; exit 4; }
STARE="$PRACA/stara_EdSharp.cs"

WARUNKI=0
grep -q 'removing works on one entry at a time' "$STARE" \
    && { echo "  OK  stara odmowa przy kilku zaznaczonych jest w tej rewizji"; WARUNKI=$((WARUNKI+1)); } \
    || echo "  BRAK starej odmowy - to NIE jest rewizja sprzed zmiany"
if grep -q 'PickFileRemoveSelection' "$STARE"; then
    echo "  UWAGA: nowy nosnik JUZ tam jest"
else
    echo "  OK  braku nowego nosnika PickFileRemoveSelection"; WARUNKI=$((WARUNKI+1))
fi
grep -q 'deleting from disk works on one file at a time' "$STARE" \
    && { echo "  OK  odmowa kasowania z dysku jest (ma zostac po zmianie)"; WARUNKI=$((WARUNKI+1)); } \
    || echo "  BRAK odmowy kasowania z dysku"
grep -q 'VersionString = "5.0.71"' "$STARE" \
    && { echo "  OK  stara stala wersji 5.0.71"; WARUNKI=$((WARUNKI+1)); } \
    || echo "  BRAK starej stalej wersji 5.0.71"

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
cp -a testy/pomiar_572.cs "$PRACA/pomiar.cs"
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

# KONTROLA SONDY MUSI PRZEJSC TEZ TUTAJ: stary nosnik kopiowania istnieje w obu
# wersjach, wiec jego brak znaczylby, ze sonda patrzy w zly typ.
printf '%s\n' "$WYNIK" | grep -q '^OK   KONTROLA SONDY' \
    || { echo "AWARIA POMIARU: kontrola sondy oblala na starej binarce - sonda patrzy w zly typ." >&2; exit 5; }
# ROZLACZNOSC: rodzina 5 (Shift+Delete jednopozycyjny) ma byc zielona na OBU.
printf '%s\n' "$WYNIK" | grep -q '^OK   Shift+Delete NADAL odmawia' \
    || { echo "AWARIA POMIARU: odmowa kasowania z dysku oblala na STAREJ binarce - zla rewizja." >&2; exit 5; }

echo
echo "== HARNESS ZACHOWANIA NA STAREJ BINARCE =="
# Ten sam harness na 5.0.71 nie ma czego wolac - to jest dowod, ze mierzy DROGE,
# ktora powstala teraz, a nie cos, co bylo od dawna.
cp -a testy/harness_usuwanie_listy_572.cs "$PRACA/harness.cs"
"$CSC" /nologo /target:exe "/out:${PRACA_WIN}\\harness.exe" \
    "/r:${PRACA_WIN}\\stara.exe" /r:System.Windows.Forms.dll /r:System.dll \
    "${PRACA_WIN}\\harness.cs" >"$PRACA/harness.log" 2>&1
if [[ -s "$PRACA/harness.exe" ]]; then
    WYNIK_H=$(/mnt/c/Windows/System32/cmd.exe /c "${PRACA_WIN}\\harness.exe ${PRACA_WIN}\\stara.exe" 2>&1)
    echo "$WYNIK_H"
    printf '%s\n' "$WYNIK_H" | grep -q 'brak Dialog.PickFileRemoveSelection' \
        && echo "  HARNESS PASS: na 5.0.71 tej drogi NIE MA (jak trzeba)" \
        || { echo "AWARIA: harness na starej binarce zachowal sie inaczej niz oczekiwano." >&2; exit 5; }
else
    echo "  UWAGA: harness nie skompilowal sie przeciw starej binarce (log $PRACA/harness.log)"
fi

echo
if [[ "$LICZ_ZLE" -ge 4 ]]; then
    echo "KONTROLA NEGATYWNA PASS: sonda rozroznia stan przed i po ($LICZ_ZLE oblanych, prog 4)."
    exit 0
fi
echo "KONTROLA NEGATYWNA FAIL: tylko $LICZ_ZLE oblanych, prog to 4 (liczba rodzin zmiany)." >&2
exit 1
