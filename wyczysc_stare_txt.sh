#!/bin/bash
# Usuwa z repozytorium EdSharpNG odziedziczone pliki .txt po oryginalnym
# EdSharpie (korespondencja, notatki, wycinki stron) - RAZEM Z HISTORIA, zeby
# repozytorium dalo sie bezpiecznie upublicznic.
#
# PO CO.  W repo lezalo 338 plikow .txt przejetych po EdSharpie Jamala Mazrui,
# a w nich 156 PRAWDZIWYCH adresow e-mail obcych ludzi wraz z zachowana
# korespondencja (zgloszenia bledow z podpisami, instrukcje wejscia na
# telekonferencje).  Upublicznienie repozytorium jest NIEODWRACALNE: samo
# usuniecie plikow nie wystarcza, bo zostaja w historii commitow i w cudzych
# kopiach.  Dlatego czyscimy tez historie.
#
# CZEGO NIE RUSZAMY (ustalone czytaniem .iss i kodu C#, nie na wyczucie):
#   HotKeys.txt, History.txt, lgpl.txt - wymienione w skrypcie instalatora
#   temp.txt - uzywany przez kod
# Pliki .txt w katalogach testowych tez zostaja (uzywane przez testy kodowan).
#
# UZYCIE:  bash /home/michal/projekty/edsharp/wyczysc_stare_txt.sh lista
#          bash /home/michal/projekty/edsharp/wyczysc_stare_txt.sh usun
set -u

REPO="/home/michal/projekty/edsharp"
TRYB="${1:-lista}"
cd "$REPO" || exit 2

# Pliki .txt w KATALOGU GLOWNYM repo - tam siedzi odziedziczony balast.
# Podkatalogi (testy, NVDAAddon) pomijamy, bo tam .txt sa uzywane.
mapfile -t WSZYSTKIE < <(git ls-files -- '*.txt' | grep -v '/')

ZOSTAWIAMY="HotKeys.txt History.txt Hotkeys.txt history.txt lgpl.txt LGPL.txt temp.txt EdSharp_Hotkeys.txt"

DO_USUNIECIA=()
for f in "${WSZYSTKIE[@]}"; do
    zachowaj=0
    for keep in $ZOSTAWIAMY; do
        if [ "$f" = "$keep" ]; then zachowaj=1; break; fi
    done
    [ "$zachowaj" -eq 0 ] && DO_USUNIECIA+=("$f")
done

echo "Plikow .txt w katalogu glownym: ${#WSZYSTKIE[@]}"
echo "Do usuniecia: ${#DO_USUNIECIA[@]}"
echo "Zostawiamy (potrzebne instalatorowi/kodowi):"
for keep in $ZOSTAWIAMY; do
    [ -f "$keep" ] && echo "   $keep"
done

if [ "$TRYB" = "lista" ]; then
    echo ""
    echo "--- PELNA LISTA DO USUNIECIA ---"
    printf '%s\n' "${DO_USUNIECIA[@]}"
    echo ""
    echo "Nic nie usunieto. Aby usunac:  bash wyczysc_stare_txt.sh usun"
    exit 0
fi

if [ "$TRYB" != "usun" ]; then
    echo "BLAD: tryb to 'lista' albo 'usun'"
    exit 2
fi

echo ""
echo "== usuwam z biezacej wersji"
printf '%s\0' "${DO_USUNIECIA[@]}" | xargs -0 git rm -q --cached --
printf '%s\0' "${DO_USUNIECIA[@]}" | xargs -0 rm -f --
git commit -q -m "Usuniecie odziedziczonych plikow .txt po oryginalnym EdSharpie (korespondencja i notatki osob trzecich) - przygotowanie do upublicznienia"
echo "   OK, commit: $(git log --oneline -1)"
echo ""
echo "UWAGA: pliki zniknely z biezacej wersji, ale SA JESZCZE W HISTORII."
echo "Czyszczenie historii to osobny krok (git filter-repo) - patrz opis wyzej."
