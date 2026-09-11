#!/bin/bash
# KROK 2: usuwa te same pliki .txt z CALEJ HISTORII repozytorium.
#
# Po co osobny krok.  Krok 1 (wyczysc_stare_txt.sh) usunal pliki z biezacej
# wersji, ale kazdy stary commit nadal je zawiera - a publiczne repozytorium
# udostepnia rowniez historie.  Bez tego kroku upublicznienie WCIAZ wystawialoby
# 156 adresow e-mail obcych ludzi.
#
# Kopia repozytorium przed czyszczeniem: ~/projekty/edsharp_kopia_przed_czyszczeniem
#
# UZYCIE: bash /home/michal/projekty/edsharp/wyczysc_historie.sh
set -u
REPO="/home/michal/projekty/edsharp"
LISTA="/tmp/txt_do_usuniecia.txt"
cd "$REPO" || exit 2

# Lista bierze sie z historii, nie z dysku: pliki sa juz usuniete z biezacej
# wersji, wiec git ls-files ich nie pokaze.  Ograniczamy do katalogu glownego
# (bez ukosnika), bo .txt w podkatalogach sa uzywane przez testy i dodatek NVDA.
git log --all --diff-filter=D --name-only --pretty=format: -- '*.txt' \
    | grep -v '/' | grep -E '\.txt$' | sort -u > "$LISTA"

# Te zostaja - wymienione w skrypcie instalatora albo uzywane przez kod.
for keep in HotKeys.txt History.txt Hotkeys.txt history.txt lgpl.txt LGPL.txt temp.txt EdSharp_Hotkeys.txt; do
    grep -vxF "$keep" "$LISTA" > "$LISTA.tmp" && mv "$LISTA.tmp" "$LISTA"
done

echo "Plikow do wyciecia z historii: $(wc -l < "$LISTA")"
[ -s "$LISTA" ] || { echo "BLAD: pusta lista - przerywam."; exit 3; }

git filter-repo --force --invert-paths --paths-from-file "$LISTA" 2>&1 | tail -5
