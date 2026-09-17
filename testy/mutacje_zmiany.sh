#!/bin/bash
# mutacje_zmiany.sh - SPRAWDZA, CZY POMIAR 1a W OGOLE COKOLWIEK LAPIE.
#
# PO CO: pomiar, ktory przechodzi zawsze, nie mierzy niczego. Psujemy Zmiany.cs
# na piec sposobow - kazdy odpowiada JEDNEJ decyzji projektowej - i zadamy, zeby
# pomiar za kazdym razem KRZYKNAL. Mutacja, ktora przechodzi, znaczy ze tej
# decyzji nikt nie pilnuje.
#
# UZYCIE: bash testy/mutacje_zmiany.sh
set -u
REPO="/home/michal/projekty/edsharp"
TMP="/tmp/mutacje_zmiany"
mkdir -p "$TMP"
cd "$REPO" || exit 2

# opis|sed-owe podmienienie
MUTACJE=(
	"zachlanny wzorzec (pierwsza zmiana zjada plik do ostatniego domkniecia)|s/\.\*?/.*/g"
	"podmiana przyjeta zostawia STARE zamiast nowego|s/case RodzajZmiany.Podmiana: return z.Nowe;/case RodzajZmiany.Podmiana: return z.Stare;/"
	"kursor na koncu zmiany uznany za bedacy w niej|s/iPozycja < z.Koniec) return z;/iPozycja <= z.Koniec) return z;/"
	"skok nastepna nie pomija zmiany, w ktorej stoimy|s/if (z.Start > iPozycja) return z;/if (z.Koniec > iPozycja) return z;/"
	"komentarz recenzenta zostaje w tresci dokumentu|s/case RodzajZmiany.Komentarz: return \"\";/case RodzajZmiany.Komentarz: return z.Nowe;/"
)

ILE=0
ZLAPANE=0
for WPIS in "${MUTACJE[@]}"; do
	OPIS="${WPIS%%|*}"
	SED="${WPIS#*|}"
	ILE=$((ILE + 1))
	cp Zmiany.cs "$TMP/Zmiany.cs" || exit 3
	sed -i "$SED" "$TMP/Zmiany.cs" || exit 4
	if cmp -s Zmiany.cs "$TMP/Zmiany.cs"; then
		echo "NIE ZADZIALALA PODMIANA: $OPIS  (wzorzec sed nie trafil - popraw skrypt)"
		continue
	fi
	WYNIK=$(bash uruchom_pomiar.sh testy/pomiar_zmiany_1a.cs "$TMP/Zmiany.cs" 2>&1 | tr -d '\r' | grep "=== WYNIK")
	if echo "$WYNIK" | grep -q " 0 BLAD"; then
		echo "PRZESZLA MIMO USZKODZENIA: $OPIS"
		echo "   $WYNIK"
	else
		ZLAPANE=$((ZLAPANE + 1))
		echo "zlapana: $OPIS"
		echo "   $WYNIK"
	fi
done

echo
echo "=== MUTACJE: $ZLAPANE z $ILE zlapanych ==="
[ "$ZLAPANE" -eq "$ILE" ] || exit 1
