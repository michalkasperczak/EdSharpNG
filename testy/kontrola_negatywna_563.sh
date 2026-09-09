#!/usr/bin/env bash
# Kontrola negatywna dla 5.0.63 - zwolnienie golego F9 i Shift+F9.
#
# CO PILNUJE:
#   1. Obsluga golego F9 i Shift+F9 zeszla CALA z ProcessCmdKey_Helper.
#   2. Nie zostal osierocony helper COM.JFWRunFunction (lekcja z 5.0.44).
#   3. ROZLACZNOSC: nie zabralismy przy okazji NICZEGO, co dziala - mowienia
#      przez JAWS-a, czytania calego tekstu na Alt+F8, rodziny komentarzy.
#   4. Podrecznik nie klamie o klawiszach, ktore sie zmienily.
#
# Uzycie:
#   ./kontrola_negatywna_563.sh [rewizja_odniesienia] [rewizja_mierzona]
# Bez drugiego argumentu mierzone jest DRZEWO ROBOCZE (i skrypt to mowi).
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ODNIESIENIE="${1:-434b6e1}"
MIERZONA="${2:-}"

OK=0; ZLE=0
ok() { OK=$((OK+1)); echo "OK: $1"; }
zle() { ZLE=$((ZLE+1)); echo "ZLE: $1"; }
sprawdz() { if [[ "$1" == "tak" ]]; then ok "$2"; else zle "$2"; fi }

if [[ -n "$MIERZONA" ]]; then
    KAT="/tmp/kn563_mierzona"
    rm -rf "$KAT"; mkdir -p "$KAT"
    for plik in EdSharp.cs Hotkeys.ini hotkeys.txt EdSharp.md; do
        git -C "$ROOT" show "$MIERZONA:$plik" > "$KAT/$plik" || exit 1
    done
    cd "$KAT"
    echo "MIERZONA REWIZJA: $MIERZONA"
else
    cd "$ROOT"
    echo "UWAGA: mierze DRZEWO ROBOCZE (rewizja odniesienia: $ODNIESIENIE)"
fi
echo "REWIZJA ODNIESIENIA: $ODNIESIENIE"
echo

STARE_CS="/tmp/kn563_stare_EdSharp.cs"
git -C "$ROOT" show "$ODNIESIENIE:EdSharp.cs" > "$STARE_CS" || exit 1
STARE_MD="/tmp/kn563_stare_EdSharp.md"
git -C "$ROOT" show "$ODNIESIENIE:EdSharp.md" > "$STARE_MD" || exit 1

# Kod BEZ komentarzy liniowych.  BEZ TEGO KAZDE PYTANIE O BRAK CZEGOS KLAMIE:
# komentarz opisujacy usuniecie zawiera usuniete nazwy z definicji, wiec trafia
# w siebie samego.  Zmierzone 03.09.2026 na asercji o COM.JFWRunFunction, trzeci
# taki falszywy alarm w tym projekcie.
KOD="/tmp/kn563_kod_aktywny.cs"
sed 's://.*$::' EdSharp.cs > "$KOD"
# KONTROLA WAZNOSCI SAMEGO FILTRA: gdyby wycinal za duzo, kazde pytanie o brak
# bylo by zielone zawsze.
sprawdz "$(grep -q 'menuMiscCommentList = CreateMenuItem("Comment List ...", "Control+Alt+F9"' "$KOD" && echo tak)" \
    "kontrola waznosci filtra komentarzy: plik kodu aktywnego nadal widzi kod"

echo
echo "--- 1. OBSLUGA GOLEGO F9 I SHIFT+F9 ZESZLA CALA"
sprawdz "$( [[ $(grep -c 'keyData == Keys.F9' "$KOD") -eq 0 ]] && echo tak )" \
    "warunek golego F9 nie istnieje w kodzie aktywnym"
sprawdz "$( [[ $(grep -c 'keyData == (Keys.Shift | Keys.F9)' "$KOD") -eq 0 ]] && echo tak )" \
    "warunek Shift+F9 nie istnieje w kodzie aktywnym"
sprawdz "$( [[ $(grep -c 'SayAllTempFile' "$KOD") -eq 0 ]] && echo tak )" \
    "wolanie funkcji skryptu JAWS SayAllTempFile zniknelo z kodu aktywnego"
# KONTROLA WAZNOSCI: w rewizji odniesienia to WSZYSTKO istnialo.
sprawdz "$( [[ $(grep -c 'keyData == Keys.F9' "$STARE_CS") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE goly F9 BYL obslugiwany"
sprawdz "$( [[ $(grep -c 'keyData == (Keys.Shift | Keys.F9)' "$STARE_CS") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE Shift+F9 BYL obslugiwany"

echo
echo "--- 2. HELPER NIE ZOSTAL SIEROTA (lekcja z 5.0.44)"
# Osierocony helper przezyl kiedys usuniecie calej rodziny komend i mylil przy
# nastepnej pracy: wygladal na dzialajaca droge, ktorej juz nikt nie wolal.
sprawdz "$( [[ $(grep -c 'COM.JFWRunFunction' "$KOD") -eq 0 ]] && echo tak )" \
    "nie ma wywolania COM.JFWRunFunction"
sprawdz "$( [[ $(grep -c 'JFWRunFunction(sText, ref App.JAWS)' "$KOD") -eq 0 ]] && echo tak )" \
    "nie ma DEFINICJI COM.JFWRunFunction (sam brak wywolania nie wystarcza)"
sprawdz "$( [[ $(grep -c 'JFWRunFunction(sText, ref App.JAWS)' "$STARE_CS") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE ta definicja BYLA"

echo
echo "--- 3. ROZLACZNOSC: nic dzialajacego nie zniknelo przy okazji"
# Bez tych asercji "naprawa" polegajaca na wywaleniu calej obslugi JAWS-a albo
# calej rodziny komentarzy tez byla by zielona.
sprawdz "$(grep -q 'public static bool JFWSay(string sText, ref object oJFW)' "$KOD" && echo tak)" \
    "mowienie przez JAWS-a (COM.JFWSay) ZOSTAJE - to zywa droga mowy, nie sierota po F9"
sprawdz "$(grep -q 'menuQueryReadAll = CreateMenuItem("Read All", "Alt+F8"' "$KOD" && echo tak)" \
    "czytanie calego tekstu na Alt+F8 nietkniete (wlasna mowa programu, bez JAWS-a)"
sprawdz "$(grep -q '^Read All=Alt+F8,' Hotkeys.ini && echo tak)" \
    "opis mowiony Read All nietkniety"
for para in 'Insert Comment ...|Alt+F9' 'Comment List ...|Control+Alt+F9' \
            'Next Comment|Alt+Shift+PageDown' 'Prior Comment|Alt+Shift+PageUp'; do
    nazwa="${para%%|*}"; chord="${para##*|}"
    sprawdz "$(grep -q "CreateMenuItem(\"$nazwa\", \"$chord\"" "$KOD" && echo tak)" \
        "rodzina komentarzy nietknieta: $nazwa nadal na $chord"
done
# Zwolniony chord MUSI byc naprawde wolny: gdyby ktos dopisal komende na F9 bez
# jego slowa, ta asercja to zlapie.
sprawdz "$( [[ $(grep -c 'CreateMenuItem("[^"]*", "F9"' "$KOD") -eq 0 ]] && echo tak )" \
    "zwolniony goly F9 nie zostal od razu zajety inna komenda"
sprawdz "$( [[ $(grep -c 'CreateMenuItem("[^"]*", "Shift+F9"' "$KOD") -eq 0 ]] && echo tak )" \
    "zwolniony Shift+F9 nie zostal od razu zajety inna komenda"
sprawdz "$(grep -q '^Say Compiler=Control+F9,' Hotkeys.ini && echo tak)" \
    "Control+F9 (mowienie kompilatora) nietkniety - to inny klawisz tej rodziny"

echo
echo "--- 4. PODRECZNIK NIE KLAMIE o zwolnionych klawiszach"
sprawdz "$( [[ $(grep -c 'the bare F9 and Shift+F9 already drive the JAWS read-to-end script' EdSharp.md) -eq 0 ]] && echo tak )" \
    "podrecznik NIE twierdzi juz, ze goly F9 obsluguje czytanie do konca"
sprawdz "$(grep -q 'the bare F9 and Shift+F9, which used to drive a JAWS read-to-end script' EdSharp.md && echo tak)" \
    "podrecznik mowi, ze te chordy sa teraz wolne"
sprawdz "$( [[ $(grep -c 'the bare F9 and Shift+F9 already drive the JAWS read-to-end script' "$STARE_MD") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE podrecznik TWIERDZIL, ze klawisze sa zajete"

echo
echo "--- 5. PLIKI SKRYPTOW JAWS ZOSTAJA W REPOZYTORIUM"
# Zdjeta jest obsluga W PROGRAMIE, a nie pliki autora.  Skasowanie plikow byloby
# decyzja szersza niz jego zdanie i trudniejsza do cofniecia.
for plik in scripts/EdSharp.JSS scripts/edsharp.jkm; do
    sprawdz "$( [[ -f "$ROOT/$plik" ]] && echo tak )" \
        "plik $plik nadal jest w repozytorium"
done
sprawdz "$(grep -q 'SayAllTempFile' "$ROOT/scripts/EdSharp.JSS" && echo tak)" \
    "funkcja SayAllTempFile nadal jest w skrypcie JAWS autora (zdjeta obsluga, nie plik)"

echo
echo "--- 6. NIC Z POPRZEDNICH WERSJI NIE ZNIKNELO"
sprawdz "$(grep -q 'menuNavigateNextLink = CreateMenuItem("Next Link", "Alt+PageDown"' "$KOD" && echo tak)" \
    "5.0.62 nietknieta: skok po odsylaczach nadal na Alt+PageDown"
sprawdz "$(grep -q 'No bookmarks, press Escape to close the list' "$KOD" && echo tak)" \
    "5.0.62 nietknieta: pusta lista zakladek nadal mowi o Escape"
sprawdz "$(grep -q 'public bool IsRichTextDocument' "$KOD" && echo tak)" \
    "5.0.61 nietknieta: pole proweniencji dokumentu RTF nadal jest"
sprawdz "$(grep -q 'menuMiscNextFootnote = CreateMenuItem("Next Footnote", "Control+Alt+PageDown"' "$KOD" && echo tak)" \
    "5.0.58 nietknieta: skok po przypisach nadal na Control+Alt+PageDown"

echo
echo "PODSUMOWANIE: $OK OK, $ZLE ZLE"
[[ $ZLE -eq 0 ]] || exit 1
