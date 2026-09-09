#!/usr/bin/env bash
# DOWOD PACZKI 5.0.73: czy instalator zawiera DOKLADNIE te binarke, ktora
# zmierzyla sonda, i czy zmienione napisy naprawde w niej sa.
#
# Zielony build i "Successful compile" NIE dowodza, ze user dostanie zmiane:
# staging moze zawierac starsza binarke, a numer wersji instalatora moze zostac
# z poprzedniego wydania.
set -uo pipefail

REPO="/mnt/d/projekty/edsharp-pr"
WERSJA="${1:-5.0.73}"
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
spr(ile("EdSharpNG 5.0.72 (beta)") == 0,
    "napis poprzedniej wersji 'EdSharpNG 5.0.72 (beta)' zniknal")
spr(ile("Not in favorites") >= 1,
    "NOWY komunikat 'Not in favorites' jest w binarce (Clear Favorite juz nie milczy)")
spr(ile("Added to favorites") >= 1,
    "komunikat 'Added to favorites' jest (kierunek dodania)")
spr(ile("Removed from favorites") >= 1,
    "komunikat 'Removed from favorites' jest (kierunek zdjecia)")
# ROZLACZNOSC: praca z 5.0.70 i 5.0.72 stoi.
spr(ile("PickFileRemoveSelection") >= 1,
    "REGRESJA: nosnik z 5.0.72 (zdejmowanie calego zaznaczenia) nadal w binarce")
spr(ile("PickFileCopySelection") >= 1,
    "REGRESJA: nosnik z 5.0.70 (kopiowanie plikow) nadal w binarce")

sys.exit(1 if bledy else 0)
PY
spr $? "napisy w binarce zgodne (szczegoly powyzej)"

echo
echo "== 4. OPISY MOWIONE =="
# LICZNIKI BEZ '|| echo 0': grep -c zwraca 0 przy braku trafien i kod 1, wiec
# '|| echo 0' DOPISYWALO druga linie i test [[ ]] dostawal "0\n0" - blad skladni
# udajacy oblana asercje (zmierzone przy 5.0.71).
for f in Hotkeys.ini hotkeys.txt EdSharp.md; do
    N=$(grep -ac 'says only what happened' "$STAGE/$f" 2>/dev/null; true)
    N=${N:-0}
    echo "  $f: nowy opis mowy Toggle Favorite=$N"
    [[ "$N" -ge 1 ]]; spr $? "$f mowi, ze Toggle Favorite podaje SKUTEK, nie nazwe komendy"
    M=$(grep -ac 'Not in favorites' "$STAGE/$f" 2>/dev/null; true)
    M=${M:-0}
    echo "  $f: opis Clear Favorite z komunikatem=$M"
    [[ "$M" -ge 1 ]]; spr $? "$f opisuje nowy komunikat Clear Favorite"
done
# PULAPKA PREFIKSU (zmierzona przy 5.0.70): stara obietnica byla PODCIAGIEM
# nowego zdania, wiec asercja o zniknieciu musi pytac o CALE stare zdanie.
# Tutaj stary opis konczyl sie na "if it is already there" i nowy zaczyna sie
# tak samo, wiec pytamy o linie KONCZACA sie tym czlonem.
for f in Hotkeys.ini hotkeys.txt; do
    S=$(grep -ac 'off that list if it is already there\r*$' "$STAGE/$f" 2>/dev/null; true)
    S=${S:-0}
    echo "  $f: stary opis konczacy sie na 'already there'=$S"
    [[ "$S" -eq 0 ]]; spr $? "$f nie ma juz opisu URWANEGO na starym zdaniu"
done
# ROZLACZNOSC W PROZIE: opis pracy z 5.0.72 ma zostac.
grep -aq "Delete works across the whole selection as well" "$STAGE/EdSharp.md"; spr $? "EdSharp.md nadal opisuje Delete na calym zaznaczeniu (praca z 5.0.72)"
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
