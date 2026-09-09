#!/usr/bin/env bash
# DOWOD PACZKI 5.0.71: czy instalator zawiera DOKLADNIE te binarke, ktora
# zmierzyla sonda, i czy zmienione napisy naprawde w niej sa.
#
# Zielony build i "Successful compile" NIE dowodza, ze user dostanie zmiane:
# staging moze zawierac starsza binarke, a numer wersji instalatora moze zostac
# z poprzedniego wydania.
set -uo pipefail

REPO="/mnt/d/projekty/edsharp-pr"
WERSJA="${1:-5.0.71}"
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
spr(ile("EdSharpNG 5.0.1 (beta)") == 0,
    "STARY napis 'EdSharpNG 5.0.1 (beta)' zniknal")
spr(ile("August 5, 2026") == 0,
    "STARA stala data 'August 5, 2026' zniknela")
spr(ile("michaldziwisz/EdSharp") >= 1,
    "aktualizacja siega do naszego repozytorium")
spr(ile("JamalMazrui/EdSharp") == 0,
    "ROZLACZNOSC: adres repozytorium upstreamu zniknal z binarki")
spr(ile("EdSharpNG_Setup.exe") >= 1,
    "pobierany artefakt to EdSharpNG_Setup.exe")
spr(ile("No release has been published") >= 1,
    "komunikat o BRAKU WYDANIA jest w binarce")
spr(ile("GetProgramBuildDate") >= 1,
    "nosnik daty wydania jest w binarce (nazwa metody w #Strings)")

sys.exit(1 if bledy else 0)
PY
spr $? "napisy w binarce zgodne (szczegoly powyzej)"

echo
echo "== 4. OPISY MOWIONE =="
# LICZNIKI BEZ '|| echo 0': grep -c zwraca 0 przy braku trafien i kod 1, wiec
# '|| echo 0' DOPISYWALO druga linie i test [[ ]] dostawal "0\n0" - blad skladni
# udajacy oblana asercje. Liczymy przez grep -c z wymuszonym kodem 0.
for f in Hotkeys.ini hotkeys.txt EdSharp.md; do
    N=$(grep -ac "Download latest EdSharpNG version" "$STAGE/$f" 2>/dev/null; true)
    S_DOKL=$(grep -ac "latest EdSharp version" "$STAGE/$f" 2>/dev/null; true)
    N=${N:-0}; S_DOKL=${S_DOKL:-0}
    echo "  $f: nowa=$N stara_dokladnie=$S_DOKL"
    [[ "$N" -ge 1 && "$S_DOKL" -eq 0 ]]; spr $? "$f ma nowy opis komendy F11 i nie ma starego"
done
grep -aq "michaldziwisz/EdSharp/releases" "$STAGE/EdSharp.md"; spr $? "EdSharp.md wskazuje NASZA strone wydan"

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
