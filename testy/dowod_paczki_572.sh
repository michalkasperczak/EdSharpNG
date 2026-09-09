#!/usr/bin/env bash
# DOWOD PACZKI 5.0.72: czy instalator zawiera DOKLADNIE te binarke, ktora
# zmierzyla sonda, i czy zmienione napisy naprawde w niej sa.
#
# Zielony build i "Successful compile" NIE dowodza, ze user dostanie zmiane:
# staging moze zawierac starsza binarke, a numer wersji instalatora moze zostac
# z poprzedniego wydania.
set -uo pipefail

REPO="/mnt/d/projekty/edsharp-pr"
WERSJA="${1:-5.0.72}"
STAGE="/mnt/c/EdSharp"
PACZKA="$REPO/dist/EdSharpNG_Setup_${WERSJA}.exe"
LOG_ISCC="/tmp/edsharp-iscc-${WERSJA}.log"

cd "$REPO"
OK=0; ZLE=0
spr() { if [[ "$1" == "0" ]]; then echo "OK   $2"; OK=$((OK+1)); else echo "ZLE  $2"; ZLE=$((ZLE+1)); fi; }

echo "== 1. TOZSAMOSC BINARKI: staging == repo =="
MD_REPO=$(md5sum "$REPO/EdSharpNG.exe" | cut -d' ' -f1)
MD_STAGE=$(md5sum "$STAGE/EdSharpNG.exe" 2>/dev/null | cut -d' ' -f1)
[[ -n "$MD_STAGE" && "$MD_REPO" == "$MD_STAGE" ]]; spr $? "binarka w stagingu == binarka w repo (md5 $MD_REPO)"

echo
echo "== 2. NUMER WERSJI W INSTALATORZE =="
grep -aq "AppVerName=EdSharpNG ${WERSJA} (beta)" "$STAGE/EdSharp_Setup.iss"; spr $? "AppVerName=EdSharpNG ${WERSJA} (beta)"
grep -aq "AppVersion=${WERSJA}" "$STAGE/EdSharp_Setup.iss"; spr $? "AppVersion=${WERSJA}"

echo
echo "== 3. NAPISY W BINARCE (oba kodowania) =="
# Literale programu siedza w strumieniu #US jako UTF-16 LE, nazwy metod w
# #Strings jako UTF-8 - grep po samym ASCII nie widzi NICZEGO.
python3 - "$STAGE/EdSharpNG.exe" "$WERSJA" <<'PY'
import sys
sciezka, wersja = sys.argv[1], sys.argv[2]
dane = open(sciezka, 'rb').read()

def ile(napis):
    return dane.count(napis.encode('utf-8')) + dane.count(napis.encode('utf-16-le'))

bledy = 0
def spr(warunek, opis):
    global bledy
    print(("OK   " if warunek else "ZLE  ") + opis)
    if not warunek:
        bledy += 1

# Kontrola sondy: napis, ktorego nie ma, w ZADNYM kodowaniu.
spr(ile("napis ktorego tu nigdy nie bylo 12345") == 0,
    "KONTROLA SONDY: napisu nieistniejacego nie widzi w obu kodowaniach")

spr(ile("EdSharpNG %s (beta)" % wersja) >= 1,
    "okno About mowi 'EdSharpNG %s (beta)'" % wersja)
spr(ile("EdSharpNG 5.0.71 (beta)") == 0,
    "napis poprzedniej wersji 'EdSharpNG 5.0.71 (beta)' zniknal")
spr(ile("PickFileRemoveSelection") >= 1,
    "nosnik zdejmowania calego zaznaczenia jest w binarce (nazwa metody w #Strings)")
spr(ile("entries from list") >= 1,
    "komunikat z LICZBA zdjetych wpisow jest w binarce")
spr(ile("removing works on one entry at a time") == 0,
    "ROZLACZNOSC: stara odmowa 'removing works on one entry at a time' zniknela")
spr(ile("deleting from disk works on one file at a time") >= 1,
    "odmowa kasowania Z DYSKU przy kilku zaznaczonych ZOSTAJE")
spr(ile("Removed from list") >= 1,
    "komunikat dla jednej pozycji nadal jest")

sys.exit(1 if bledy else 0)
PY
spr $? "napisy w binarce zgodne (szczegoly powyzej)"

echo
echo "== 4. OPISY MOWIONE =="
# LICZNIKI BEZ '|| echo 0': grep -c zwraca 0 przy braku trafien i kod 1, wiec
# '|| echo 0' DOPISYWALO druga linie i test [[ ]] dostawal "0\n0" - blad skladni
# udajacy oblana asercje (zmierzone przy 5.0.71).
for f in Hotkeys.ini hotkeys.txt EdSharp.md; do
    N=$(grep -ac "Delete takes every selected entry off the list" "$STAGE/$f" 2>/dev/null; true)
    N=${N:-0}
    echo "  $f: nowy opis Delete=$N"
    [[ "$N" -ge 1 ]]; spr $? "$f mowi, ze Delete bierze cale zaznaczenie"
done
# PULAPKA PREFIKSU (zmierzona przy 5.0.70): stara obietnica byla PODCIAGIEM
# nowego zdania, wiec pytamy o CALE stare zdanie, nie o jego czlon.
S=$(grep -ac "Delete and Shift+Delete refuse on several selected items" "$STAGE/EdSharp.md" 2>/dev/null; true)
S=${S:-0}
echo "  EdSharp.md: stare zdanie o odmowie OBU klawiszy=$S"
[[ "$S" -eq 0 ]]; spr $? "EdSharp.md nie obiecuje juz, ze Delete odmawia przy kilku zaznaczonych"
# ROZLACZNOSC W PROZIE: kasowanie z dysku nadal ma byc opisane jako jednoplikowe.
grep -aq "Deleting from disk stays deliberately single-file" "$STAGE/EdSharp.md"; spr $? "EdSharp.md nadal mowi, ze kasowanie z dysku jest jednoplikowe"
# Hotkeys.ini i hotkeys.txt musza mowic to samo co do znaku (poza naglowkiem).
diff <(sed 's/\r$//' "$STAGE/Hotkeys.ini" | grep '=') <(sed 's/\r$//' "$STAGE/hotkeys.txt" | grep '=') >/dev/null
spr $? "Hotkeys.ini i hotkeys.txt zgodne co do znaku w liniach opisow"

echo
echo "== 5. LOG INSTALATORA I SWIEZOSC PACZKI =="
if [[ -s "$LOG_ISCC" ]]; then
    LICZ=$(grep -ac "Compressing" "$LOG_ISCC" || true)
    echo "  linii Compressing: $LICZ"
    [[ "$LICZ" -gt 100 ]]; spr $? "log ISCC ma sensowna liczbe pakowanych plikow"
    grep -aq "Successful compile" "$LOG_ISCC"; spr $? "log ISCC konczy sie 'Successful compile'"
else
    echo "  UWAGA: log $LOG_ISCC pusty albo brak (interop WSL padl na przebiegu, ktory sie udal)"
fi
[[ -s "$PACZKA" ]]; spr $? "paczka istnieje: $PACZKA"
[[ "$PACZKA" -nt "$REPO/EdSharpNG.exe" ]]; spr $? "paczka NOWSZA niz binarka"

echo
echo "PODSUMOWANIE DOWODU PACZKI: $OK OK / $ZLE ZLE"
[[ $((OK+ZLE)) -gt 0 ]] || { echo "AWARIA POMIARU: zero sprawdzen" >&2; exit 5; }
exit $(( ZLE == 0 ? 0 : 1 ))
