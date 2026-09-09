#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.70: sonda pomiar_570.cs na binarce SPRZED zmiany
# (5.0.69, rewizja podana argumentem) MUSI oblac asercje w KAZDEJ rodzinie.
# Bez tego zielony wynik nie odroznia naprawy od gluchej sondy.
#
# Uzycie:
#   testy/kontrola_negatywna_570.sh 3d79c9a
#
# PROG: >= 3 oblane asercje, czyli tyle, ile jest RODZIN zmiany (nosnik
# schowka plikow, wolanie nosnika przez liste plikow, mowa o skutku).  Prog
# ">= 1" moglby znaczyc, ze sonda rozroznia tylko jedna rodzine, a dwie
# pozostale mierzy glucho.
set -u
ROOT=/mnt/d/projekty/edsharp-pr
REW="${1:-}"
if [[ -z "$REW" ]]; then
  echo "Uzycie: $0 <rewizja 5.0.69, np. 3d79c9a>" >&2
  exit 2
fi
cd "$ROOT"

# KATALOG MUSI LEZEC NA DYSKU WINDOWS.  /tmp z WSL widziany jest przez .NET jako
# \\wsl.localhost, czyli LOKALIZACJA SIECIOWA - Assembly.LoadFile rzuca wtedy
# NotSupportedException i sonda w ogole nie startuje.  Wynik wygladal jak
# "0 OK, 0 ZLE", czyli jak sonda gluche, a nie jak brak dostepu do pliku.
KAT=/mnt/c/edsharp_probe/kn570
rm -rf "$KAT"; mkdir -p "$KAT"

echo "=== 0. KONTROLA ZRODLA: rewizja odniesienia MUSI miec STARY ksztalt ==="
# Bez tego "nie widze zmiany" moze znaczyc "patrze w zla rewizje".
SRC=$(git -C "$ROOT" show "$REW:EdSharp.cs")
BLEDY=0
sprawdz_zrodlo() {
  local opis="$1"; local wzor="$2"; local ile_ma_byc="$3"
  local n
  n=$(printf '%s' "$SRC" | grep -cE "$wzor" || true)
  echo "  $opis: $n (ma byc $ile_ma_byc)"
  [[ "$n" == "$ile_ma_byc" ]] || { echo "  ZLA REWIZJA ODNIESIENIA"; BLEDY=$((BLEDY+1)); }
}
sprawdz_zrodlo "SetClipboardFileDrop nie istnieje" 'SetClipboardFileDrop' 0
sprawdz_zrodlo "SetFileDropList nie istnieje" 'SetFileDropList' 0
sprawdz_zrodlo "PickFileCopySelection JEST (lista plikow kopiuje)" 'private static void PickFileCopySelection' 1
sprawdz_zrodlo "SetClipboardData JEST (droga bogatego schowka istniala)" 'public static bool SetClipboardData' 1
if [[ "$BLEDY" -gt 0 ]]; then
  echo "PRZERWANE: rewizja $REW nie ma starego ksztaltu, pomiar nic nie dowodzi." >&2
  exit 3
fi

echo
echo "=== 1. Binarka rewizji odniesienia ==="
# Binarki nie ma w gicie, wiec bierzemy ta, ktora zostala po tamtym buildzie:
# instalator 5.0.69 niesie DOKLADNIE ja, a jej md5 jest zapisane w nocie.
STARA=/mnt/c/edsharp_probe/EdSharpNG_569.exe
if [[ ! -s "$STARA" ]]; then
  echo "BRAK $STARA - kontroli nie da sie przeprowadzic uczciwie." >&2
  exit 4
fi
echo -n "  md5 binarki 5.0.69: "; md5sum < "$STARA" | cut -d' ' -f1
echo "  (nota 5.0.69 podaje 614788939f0237af5dd8483a626aa13f)"

echo
echo "=== 2. Sonda 570 na STAREJ binarce ==="
cp -f "$STARA" "$KAT/EdSharpNG.exe"
WYNIK="$KAT/wynik.txt"
/mnt/c/Windows/System32/cmd.exe /c "$(wslpath -w "$ROOT/testy/out_570.exe") $(wslpath -w "$KAT/EdSharpNG.exe")" >"$WYNIK" 2>&1 || true
sed 's/\r$//' "$WYNIK"

OK=$(grep -c '^OK   ' "$WYNIK" || true)
ZLE=$(grep -c '^ZLE  ' "$WYNIK" || true)
# SONDA, KTORA SIE NIE URUCHOMILA, DAJE 0 OK i 0 ZLE - czyli wyglada dokladnie
# jak sonda gluche.  Zero asercji RAZEM to zawsze awaria pomiaru, nie wynik.
if [[ "$OK" -eq 0 && "$ZLE" -eq 0 ]]; then
  echo "PRZERWANE: sonda nie wypisala ZADNEJ asercji - pomiar sie nie odbyl." >&2
  exit 5
fi
echo
echo "  OK=$OK  ZLE=$ZLE"

echo
echo "=== 3. Oblane asercje MUSZA lezec w KAZDEJ z trzech rodzin ==="
r1=$(grep '^ZLE  ' "$WYNIK" | grep -c 'SetClipboardFileDrop\|CF_HDROP\|DropEffect\|SetFileDropList' || true)
r2=$(grep '^ZLE  ' "$WYNIK" | grep -c 'WOLA schowek plikow\|ISTNIEJE na dysku' || true)
r3=$(grep '^ZLE  ' "$WYNIK" | grep -c 'MOWI\|as text\|PLIK' || true)
echo "  rodzina 1 (nosnik schowka plikow): $r1"
echo "  rodzina 2 (lista plikow wola nosnik): $r2"
echo "  rodzina 3 (mowa o skutku): $r3"

echo
if [[ "$ZLE" -ge 3 && "$r1" -ge 1 && "$r2" -ge 1 && "$r3" -ge 1 ]]; then
  echo "WYNIK: sonda ROZROZNIA stan przed i po ($ZLE oblanych, wszystkie trzy rodziny)."
  exit 0
fi
echo "WYNIK: sonda NIE rozroznia stanu przed i po - popraw sonde, nie wynik." >&2
exit 1
