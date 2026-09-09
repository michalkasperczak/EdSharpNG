#!/usr/bin/env bash
# Kontrola negatywna 5.0.50: odebranie rodzinie Control+F6 systemowego cyklu
# okien MDI (zgloszenie Kasperczaka o "Accessible Selection" pod Ctrl+Shift+F6).
#
# DLACZEGO TA KONTROLA JEST TU WAZNIEJSZA NIZ ZWYKLE: naprawa polega na tym, ze
# klawisz PRZESTAJE cos robic. "Nie slysze niczego" nie dowodzi niczego, dopoki
# ta sama sonda nie ZOBACZY, ze przed zmiana kodu blokady NIE BYLO. Rewizja
# odniesienia jest podana JAWNIE - skrypt biorący HEAD po commicie porownywalby
# kod z samym soba i zawsze bylby zielony.
#
# Uzycie: testy/kontrola_negatywna_550.sh [rewizja_odniesienia]
set -uo pipefail
# UWAGA na pipefail: `grep ... | grep -q ...` zwraca blad, bo grep -q zamyka
# strumien i pierwszy grep dostaje SIGPIPE. Zmierzone 31.08.2026 - dwie sondy
# ponizej falszywie oblaly. Dlatego kazdy taki lancuch idzie przez plik
# tymczasowy, nie przez potok.

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
ODNIESIENIE="${1:-d403c92}"   # 5.0.49, stan PRZED odebraniem cyklu okien MDI
PRZED="/tmp/kn550_przed_EdSharp.cs"
OK=0
ZLE=0

git show "${ODNIESIENIE}:EdSharp.cs" > "$PRZED" 2>/dev/null || {
    echo "BLAD: nie moge pobrac EdSharp.cs z rewizji $ODNIESIENIE" >&2
    exit 2
}

zdaj()  { OK=$((OK+1));  echo "OK   $1"; }
oblej() { ZLE=$((ZLE+1)); echo "ZLE  $1"; }

# jest_teraz WZORZEC OPIS - wzorzec MUSI byc w dzisiejszym kodzie
jest_teraz() {
    if grep -qF -- "$1" EdSharp.cs; then zdaj "$2"; else oblej "$2 (brak w HEAD: $1)"; fi
}
# nie_bylo WZORZEC OPIS - wzorca NIE MOZE byc w kodzie sprzed zmiany
nie_bylo() {
    if grep -qF -- "$1" "$PRZED"; then oblej "$2 (bylo juz w $ODNIESIENIE: $1)"; else zdaj "$2"; fi
}
# bylo WZORZEC OPIS - wzorzec MUSIAL byc przed zmiana (dowod, ze sonda widzi)
bylo() {
    if grep -qF -- "$1" "$PRZED"; then zdaj "$2"; else oblej "$2 (sonda gluche: nie widzi $1 w $ODNIESIENIE)"; fi
}

echo "== Rewizja odniesienia: $ODNIESIENIE =="
echo
echo "-- 1. Blokada rodziny Control+F6 ISTNIEJE dzis --"
jest_teraz "private bool HandleMdiWindowCycleKey(Keys keyData) {" "metoda blokady jest w kodzie"
jest_teraz "if (HandleMdiWindowCycleKey(keyData)) return true;" "blokada jest WYWOLANA w ProcessCmdKey_Helper"
jest_teraz "keyData != (Keys.Control | Keys.F6) && keyData != (Keys.Control | Keys.Shift | Keys.F6)" "blokada obejmuje OBA chordy rodziny"
jest_teraz "if (hashKey.ContainsKey(keyData)) return false;" "blokada USTEPUJE wlasnej komendzie z tablicy skrotow"

echo
echo "-- 2. Kontrola negatywna: przed zmiana blokady NIE BYLO --"
nie_bylo "HandleMdiWindowCycleKey" "metody blokady nie bylo w $ODNIESIENIE"

echo
echo "-- 3. Sonda NIE JEST gluche: widzi rzeczy, ktore w $ODNIESIENIE byly --"
bylo "private bool HandleCloseWindowKey(Keys keyData) {" "widzi sasiednia metode HandleCloseWindowKey"
bylo "if (HandleCloseWindowKey(keyData)) return true;" "widzi wywolanie HandleCloseWindowKey"
bylo "menuNavigateDocumentNavigation = CreateMenuItem(\"Document Navigation ...\", \"F6\"" "widzi drzewo naglowkow pod F6"

echo
echo "-- 4. Nie zniknelo nic, co ma zostac (kontrole pozytywne) --"
jest_teraz "menuWindowNext = CreateMenuItem(\"Next Window\", \"Control+Tab\"" "zmiana okna Control+Tab ZOSTAJE"
jest_teraz "menuWindowPrior = CreateMenuItem(\"Prior Window\", \"Control+Shift+Tab\"" "zmiana okna Control+Shift+Tab ZOSTAJE"
jest_teraz "menuNavigateDocumentNavigation = CreateMenuItem(\"Document Navigation ...\", \"F6\"" "drzewo naglowkow pod F6 ZOSTAJE"
jest_teraz "menuNavigateGoToContents = CreateMenuItem(\"Go to Contents\", \"Shift+F6\"" "skok po spisie tresci Shift+F6 ZOSTAJE"
jest_teraz "menuEditBaseline = CreateMenuItem(\"Baseline ...\", \"Alt+Shift+F6\"" "okno Baseline Alt+Shift+F6 ZOSTAJE"
jest_teraz "menuMiscFootnoteList = CreateMenuItem(\"Footnote List ...\", \"Alt+K\"" "lista przypisow ZOSTAJE na Alt+K"
jest_teraz "private bool HandleCloseWindowKey(Keys keyData) {" "zamykanie okna Control+W ZOSTAJE"
jest_teraz "private bool HandleWindowNumberKey(Keys keyData) {" "okna pod cyframi ZOSTAJA"

echo
echo "-- 5. Blokada MDI wpuszcza NASZA komende, a Control+Shift+F6 zostaje wolny --"
MENU="$(mktemp)"
grep -F 'CreateMenuItem' EdSharp.cs > "$MENU" || true
# ZMIENIONE W 5.0.53: ta asercja pilnowala wczesniej, ze OBA chordy rodziny nie
# maja komendy - i po napisaniu listy linkow zaczela KLAMAC, bo klawisz
# odebralismy systemowi WLASNIE pod nia (ustalenie edsharpng-47). Prawdziwym
# wymogiem jest: Control+F6 ma NASZA komende, Control+Shift+F6 nadal wolny, a
# blokada MDI ustepuje tablicy skrotow.
if grep -qE '"Control\+F6"' "$MENU"; then
    zdaj "Control+F6 ma nasza komende (lista linkow z 5.0.53)"
else
    oblej "Control+F6 stracil komende - blokada MDI blokuje teraz wszystko"
fi
if grep -qE '"Control\+Shift\+F6"' "$MENU"; then
    oblej "Control+Shift+F6 komus przypisany, a ma zostac wolny"
else
    zdaj "Control+Shift+F6 nadal bez komendy, wiec blokada MDI go trzyma"
fi
if grep -qF 'if (hashKey.ContainsKey(keyData)) return false;' EdSharp.cs; then
    zdaj "blokada MDI USTEPUJE komendzie z tablicy skrotow"
else
    oblej "blokada MDI NIE ustepuje - lista linkow nie mialaby jak sie otworzyc"
fi
# ta sama sonda MUSI widziec chord, ktory w kodzie JEST - inaczej jej zero klamie
if grep -qE '"Shift\+F6"' "$MENU"; then
    zdaj "sonda chordow nie jest gluche (widzi Shift+F6 w menu)"
else
    oblej "sonda chordow GLUCHE: nie widzi nawet Shift+F6"
fi
rm -f "$MENU"

echo
echo "-- 6. Napisu \"Accessible Selection\" nie ma w opisach ani w kodzie wykonywanym --"
# EdSharp.cs jest sprawdzany INACZEJ niz reszta: fraza wystepuje w nim tylko w
# KOMENTARZU opisujacym to zgloszenie, nie w napisie programu. Naiwne szukanie
# w cudzyslowach TEZ oblewa, bo cytuje ja w komentarzu wlasnie w cudzyslowach
# (zmierzone). Wiec odsiewamy linie komentarza i pytamy o reszte; twardym
# dowodem i tak jest punkt 7 na ZBUDOWANEJ binarce.
CS_KOD="$(mktemp)"
grep -vE '^[[:space:]]*//' EdSharp.cs > "$CS_KOD" || true
if grep -qiF "accessible selection" "$CS_KOD"; then
    oblej "EdSharp.cs ma \"Accessible Selection\" poza komentarzem"
else
    zdaj "EdSharp.cs ma te fraze WYLACZNIE w komentarzu, nie w kodzie"
fi
rm -f "$CS_KOD"
jest_teraz "Accessible Selection" "diagnoza tego zgloszenia jest utrwalona w komentarzu"
for PLIK in Hotkeys.ini hotkeys.txt EdSharp.md; do
    if grep -qiF "accessible selection" "$PLIK" 2>/dev/null; then
        oblej "$PLIK zawiera \"Accessible Selection\""
    else
        zdaj "$PLIK nie zawiera \"Accessible Selection\""
    fi
done
# kontrola, ze wzorzec dziala: to samo szukanie w pliku, gdzie fraza JEST
TMPK="$(mktemp)"; printf 'Accessible Selection\n' > "$TMPK"
if grep -qiF "accessible selection" "$TMPK"; then zdaj "wzorzec frazy dziala (kontrola pozytywna)"; else oblej "wzorzec frazy NIE dziala"; fi
rm -f "$TMPK"

echo
echo "-- 7. Binarka: napisu \"Accessible Selection\" nie ma w OBU kodowaniach --"
if [[ -s EdSharpNG.exe ]]; then
    python3 - <<'PY'
import re, sys
d = open('EdSharpNG.exe','rb').read()
zle = 0
for nazwa, wzor in (('ASCII', b'Accessible Selection'),
                    ('szerokie', 'Accessible Selection'.encode('utf-16-le'))):
    n = len(re.findall(re.escape(wzor), d))
    print(("OK   " if n == 0 else "ZLE  ") + f'binarka: 0 wystapien "Accessible Selection" ({nazwa}), zmierzono {n}')
    if n: zle += 1
# kontrola, ze sonda po binarce NIE JEST gluche: napis, ktory tam BYC MUSI
for nazwa, wzor in (('szerokie', 'Document Navigation'.encode('utf-16-le')),):
    n = len(re.findall(re.escape(wzor), d))
    print(("OK   " if n > 0 else "ZLE  ") + f'sonda binarki widzi "Document Navigation" ({nazwa}), zmierzono {n}')
    if n == 0: zle += 1
sys.exit(1 if zle else 0)
PY
    if [[ $? -eq 0 ]]; then OK=$((OK+3)); else ZLE=$((ZLE+1)); fi
else
    oblej "brak zbudowanej binarki EdSharpNG.exe"
fi

echo
echo "===================================="
echo "ZDANE: $OK   OBLANE: $ZLE"
[[ $ZLE -eq 0 ]] || exit 1
