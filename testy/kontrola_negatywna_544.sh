#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.44 (mini korekta skrotow Kasperczaka, punkt 5 mapy:
# porzadkowanie programu).  Sprawdza to, co przy USUWANIU jest trudniejsze niz
# przy dodawaniu: czy usunieta rzecz REALNIE TAM BYLA i czy razem z nia nie
# poszlo nic, co ma zostac.
#
# "Nie widze komendy X" po usunieciu nie dowodzi niczego, dopoki ta sama sonda
# nie ZOBACZY jej w kodzie sprzed zmiany.  Rewizja odniesienia jest ARGUMENTEM,
# nie HEAD-em: skrypt biorący HEAD po commicie porownuje kod z samym soba.
#
# Skrypt SAM PILNUJE, ze na koncu w repo lezy NOWA binarka (sha256).
set -u
REPO="/mnt/d/projekty/edsharp-pr"
REV="${1:-bc3fe19}"          # stan 5.0.43
cd "$REPO" || exit 3

NOWA_SUMA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
echo "== rewizja odniesienia: $REV"
echo "== sha256 NOWEJ binarki: ${NOWA_SUMA:0:24}"
cp EdSharpNG.exe /tmp/EdSharpNG_nowa_kn544.exe || exit 3

PASS=0; FAIL=0
ok()   { echo "PASS $1"; PASS=$((PASS+1)); }
bad()  { echo "FAIL $1"; FAIL=$((FAIL+1)); }

git show "$REV:EdSharp.cs"   > /tmp/stary_544.cs    2>/dev/null || {
    echo "BLAD: nie moge wyciagnac EdSharp.cs z $REV"; exit 3; }
git show "$REV:Hotkeys.ini"  > /tmp/stary_544_hk.ini 2>/dev/null || {
    echo "BLAD: nie moge wyciagnac Hotkeys.ini z $REV"; exit 3; }
git show "$REV:EdSharp.md"   > /tmp/stary_544.md    2>/dev/null || {
    echo "BLAD: nie moge wyciagnac EdSharp.md z $REV"; exit 3; }

# ---------- KONTROLE POZYTYWNE SONDY ----------
# Bez nich kazde "nie znalazlem" nizej znaczy tylko "czytam pusty plik".
LICZ_MENU=$(grep -c 'CreateMenuItem(' /tmp/stary_544.cs)
if [ "$LICZ_MENU" -gt 180 ]; then ok "SONDA: stare zrodlo ma $LICZ_MENU pozycji menu"
else bad "SONDA GLUCHA: tylko $LICZ_MENU pozycji menu w starym zrodle"; exit 2; fi
LICZ_HK=$(grep -c '=' /tmp/stary_544_hk.ini)
if [ "$LICZ_HK" -gt 200 ]; then ok "SONDA: stary Hotkeys.ini ma $LICZ_HK wpisow"
else bad "SONDA GLUCHA: tylko $LICZ_HK wpisow w starym Hotkeys.ini"; exit 2; fi
LICZ_MD=$(wc -l < /tmp/stary_544.md)
if [ "$LICZ_MD" -gt 700 ]; then ok "SONDA: stary podrecznik ma $LICZ_MD linii"
else bad "SONDA GLUCHA: tylko $LICZ_MD linii w starym podreczniku"; exit 2; fi

# ---------- CZESC 1: objaw MUSI sie odtworzyc na starym kodzie ----------
# Kazda z dziewieciu komend byla w kodzie na CZTERECH warstwach.  Mierzymy
# wszystkie osobno, bo jedna sonda po nazwie daje zielone i przegapia slad
# (lekcja z osieroconego NavigatePart, ktory zlapal sam Kasperczak).
for PARA in "Next Alignment|menuNavigateNextJustify" \
            "Prior Alignment|menuNavigatePriorJustify" \
            "Next Style|menuNavigateNextStyle" \
            "Prior Style|menuNavigatePriorStyle" \
            "Next Baseline|menuNavigateNextBaseline" \
            "Prior Baseline|menuNavigatePriorBaseline" \
            "Next Font|menuNavigateNextFont" \
            "Prior Font|menuNavigatePriorFont" \
            "Calculate Date|menuMiscCalculateDate"; do
    NAZWA="${PARA%%|*}"; POLE="${PARA##*|}"
    # A. stary kod MIAL pozycje menu
    if grep -qF "CreateMenuItem(\"$NAZWA" /tmp/stary_544.cs; then
        ok "objaw: 5.0.43 MIALO pozycje menu $NAZWA"
    else bad "objaw NIE odtworzony: $NAZWA nie bylo w 5.0.43 (nie ma czego usuwac)"; fi
    # B. stary kod MIAL handler
    if grep -qF "menuItem == $POLE" /tmp/stary_544.cs; then
        ok "objaw: 5.0.43 MIALO handler $POLE"
    else bad "objaw NIE odtworzony: brak handlera $POLE w 5.0.43"; fi
    # C. NOWY kod nie ma NICZEGO z tej komendy
    if ! grep -qF "$POLE" EdSharp.cs; then
        ok "po zmianie: $POLE zniknelo z kodu w calosci"
    else bad "NIEDOKONCZONE: $POLE nadal w kodzie"; fi
    if ! grep -qF "CreateMenuItem(\"$NAZWA" EdSharp.cs; then
        ok "po zmianie: pozycja menu $NAZWA zniknela"
    else bad "NIEDOKONCZONE: pozycja menu $NAZWA nadal jest"; fi
    # D. opis mowiony (Ctrl+F1 czyta go niewidomemu) tez musi zniknac
    if grep -q "^$NAZWA=" /tmp/stary_544_hk.ini; then
        ok "objaw: 5.0.43 MIALO opis mowiony $NAZWA"
    else bad "objaw NIE odtworzony: brak opisu $NAZWA w 5.0.43"; fi
    if ! grep -q "^$NAZWA=" Hotkeys.ini && ! grep -q "^$NAZWA=" hotkeys.txt; then
        ok "po zmianie: opis mowiony $NAZWA zniknal z OBU plikow"
    else bad "MARTWY OPIS: $NAZWA nadal w spisie skrotow"; fi
done

# Metoda CalculateDate i jej pola konfiguracji - warstwa, ktora najlatwiej
# osierocic.  Pola Year/Month/Week/Day byly ZAPISYWANE do pliku ustawien.
if grep -q 'public void CalculateDate()' /tmp/stary_544.cs; then
    ok "objaw: 5.0.43 mialo metode CalculateDate"
else bad "objaw NIE odtworzony: brak metody CalculateDate w 5.0.43"; fi
if ! grep -q 'CalculateDate' EdSharp.cs; then
    ok "po zmianie: brak jakiegokolwiek sladu CalculateDate w kodzie"
else
    # dopuszczamy WYLACZNIE komentarz z powodem usuniecia
    ILE=$(grep -c 'CalculateDate' EdSharp.cs)
    ILE_KOM=$(grep 'CalculateDate' EdSharp.cs | grep -c '^\s*//')
    if [ "$ILE" = "$ILE_KOM" ]; then
        ok "po zmianie: CalculateDate wystepuje tylko w komentarzu z powodem ($ILE)"
    else bad "NIEDOKONCZONE: CalculateDate nadal w KODZIE ($ILE wystapien, $ILE_KOM w komentarzach)"; fi
fi
for POLE_D in '"Year"' '"Month"' '"Week"' '"Day"'; do
    if grep -qF "$POLE_D" /tmp/stary_544.cs; then
        ok "objaw: 5.0.43 zapisywalo pole ustawien $POLE_D"
    else bad "objaw NIE odtworzony: brak pola $POLE_D w 5.0.43"; fi
    if ! grep -qF "$POLE_D" EdSharp.cs; then
        ok "po zmianie: pole ustawien $POLE_D nie zostalo osierocone"
    else bad "OSIEROCONE POLE: $POLE_D nadal w kodzie po usunieciu komendy"; fi
done

# ---------- CZESC 2: KONTROLE, ZE NIE ZEPSULEM DZIALAJACEGO ----------
# On kazal usunac SKOKI po formatowaniu.  Nie kazal usuwac okien, ktore
# formatowanie USTAWIAJA (program nadal otwiera pliki RTF), ani komend, ktore
# PYTAJA o format pod kursorem, ani wstawiania biezacej daty.
for PARA in "Justify ...|Alt+Shift+J" \
            "Style ...|Alt+Shift+OemQuestion" \
            "Baseline ...|Alt+Shift+F6" \
            "Set Selection Font ...|Alt+Shift+OemMinus" \
            "Insert Time|Alt+Shift+OemSemicolon" \
            "Styles|Alt+OemQuestion" \
            "Font|Alt+OemMinus"; do
    NAZWA="${PARA%%|*}"; CHORD="${PARA##*|}"
    if grep -qF "CreateMenuItem(\"$NAZWA\", \"$CHORD\"" EdSharp.cs; then
        ok "KONTROLA (zostaje): $NAZWA nadal na $CHORD"
    else bad "REGRESJA: $NAZWA stracila chord $CHORD"; fi
done
# Helpery wspolne dla usunietych skokow i dla POZOSTAWIONYCH okien ustawien:
# gdyby poszly razem ze skokami, okna zamilkly by przy zielonym buildzie.
for H in GetJustifyText GetStyleText GetBaselineText GetFontText; do
    ILE=$(grep -c "$H" EdSharp.cs)
    if [ "$ILE" -ge 2 ]; then ok "KONTROLA (zostaje): helper $H nadal uzywany ($ILE)"
    else bad "REGRESJA: helper $H osierocony albo usuniety ($ILE)"; fi
done
# Month2Num i Day2Num byly uzywane TAKZE poza Calculate Date (w Util).
for H in Month2Num Day2Num; do
    ILE=$(grep -c "$H" EdSharp.cs)
    if [ "$ILE" -ge 2 ]; then ok "KONTROLA (zostaje): $H nie usuniety razem z Calculate Date ($ILE)"
    else bad "REGRESJA: $H zniknal razem z Calculate Date ($ILE)"; fi
done
# Rzeczy z ostatnich wersji, ktorych ta zmiana dotykac NIE MOZE.
# SPROSTOWANIE 30.08.2026: trzy pary ponizej byly aktualne w 5.0.44, ale ON SAM
# przeniosl te klawisze pozniej, wiec sonda zaczela KLAMAC - dawala FAIL przy
# poprawnym kodzie i zaciemniala prawdziwe regresje w tym samym przebiegu.
# Wstawianie przypisu: Control+F6 -> Control+Shift+K (rodzina K, 5.0.46).
# Windows Open (mowila tytuly okien): USUNIETA w 5.0.45, bo liste okien daje F4,
#   a Shift+F4 dostal foldery ostatnich plikow - wiec pilnuje jej BRAKU.
# Zamykanie pozostalych okien: Control+Shift+F4 -> Control+Shift+W (5.0.45).
for PARA in "Insert Table ...|Control+Shift+T" \
            "Insert Footnote ...|Control+Shift+K" \
            "Insert Comment ...|Alt+F9" \
            "Current Windows ...|F4" \
            "Close All but Current Window|Control+Shift+W"; do
    NAZWA="${PARA%%|*}"; CHORD="${PARA##*|}"
    if grep -qF "CreateMenuItem(\"$NAZWA\", \"$CHORD\"" EdSharp.cs; then
        ok "KONTROLA (nietkniete): $NAZWA nadal na $CHORD"
    else bad "REGRESJA: $NAZWA stracila chord $CHORD"; fi
done
# Windows Open ma NIE ISTNIEC - usunieta w 5.0.45 na jego polecenie.
if grep -qF 'CreateMenuItem("Windows Open"' EdSharp.cs; then
    bad "REGRESJA: Windows Open wrocila, a miala zniknac w 5.0.45"
else ok "KONTROLA (usuniete w 5.0.45): Windows Open nie ma w kodzie"; fi
# Obsluga JAWS na golym F9 i Shift+F9 siedzi POZA spisem skrotow.
if grep -qF 'keyData == Keys.F9' EdSharp.cs && grep -qF 'SayAllTempFile' EdSharp.cs; then
    ok "KONTROLA (nietkniete): czytanie do konca (JAWS) na golym F9 dziala"
else bad "REGRESJA: zabralem F9 obsludze JAWS"; fi

# ---------- CZESC 3: PODRECZNIK nie moze KLAMAC ----------
# Proza obiecywala skoki po formatowaniu i okno obliczania daty.  Podrecznik,
# ktory obiecuje niewidomemu klawisz, po ktorym nic sie nie dzieje, jest gorszy
# niz brak opisu.
if grep -qF 'Control+RightBracket goes to the next justification change' /tmp/stary_544.md; then
    ok "objaw: 5.0.43 obiecywalo w prozie skoki po formatowaniu"
else bad "objaw NIE odtworzony: brak tej obietnicy w 5.0.43"; fi
if ! grep -qF 'Control+RightBracket goes to the next justification change' EdSharp.md; then
    ok "po zmianie: proza nie obiecuje skokow po formatowaniu"
else bad "PODRECZNIK KLAMIE: nadal obiecuje skoki po formatowaniu"; fi
if grep -qF 'Press Control+Shift+Semicolon to calculate and insert a date' /tmp/stary_544.md; then
    ok "objaw: 5.0.43 obiecywalo okno obliczania daty"
else bad "objaw NIE odtworzony: brak tej obietnicy w 5.0.43"; fi
if ! grep -qF 'Press Control+Shift+Semicolon to calculate and insert a date' EdSharp.md; then
    ok "po zmianie: proza nie obiecuje obliczania daty"
else bad "PODRECZNIK KLAMIE: nadal obiecuje obliczanie daty"; fi
# Zdanie o Control+1..9, ktore mowilo "Next Baseline teraz na Control+F2".
if ! grep -qF 'now lives on Control+F2' EdSharp.md; then
    ok "po zmianie: zdanie o Control+1..9 nie odsyla do usunietego Control+F2"
else bad "PODRECZNIK KLAMIE: nadal odsyla do Control+F2"; fi
# KONTROLA POZYTYWNA prozy: opis wstawiania BIEZACEJ daty MUSI zostac.
if grep -qF 'Press Alt+Shift+Semicolon to insert the current time and date' EdSharp.md; then
    ok "KONTROLA (zostaje): proza nadal opisuje wstawianie biezacej daty"
else bad "REGRESJA: wycialem z prozy za duzo - zniknal opis Insert Time"; fi

# ---------- CZESC 4: CRLF we wszystkich ruszonych plikach ----------
# Edycja Pythonem zamienia CRLF na LF, a git diff to MASKUJE (autocrlf).
for PLIK in EdSharp.cs Hotkeys.ini hotkeys.txt EdSharp.md; do
    LINIE=$(wc -l < "$PLIK")
    CRLF=$(grep -c $'\r$' "$PLIK")
    if [ "$LINIE" = "$CRLF" ]; then ok "CRLF: $PLIK $CRLF/$LINIE"
    else bad "CRLF ZEPSUTE: $PLIK $CRLF/$LINIE"; fi
done

# ---------- CZESC 5: dowod na BINARCE, nie na zrodle ----------
# Zrodlo moze byc czyste, a paczka zawierac stara binarke - to sie realnie
# zdarzylo przy 5.0.38.  Napisy .NET sa w kodowaniu SZEROKIM, wiec grep ASCII
# daje zero i wyglada jak brak komendy.  Szukamy w OBU kodowaniach.
szukaj_w_binarce() {   # $1=plik, $2=fraza
    if grep -qF "$2" "$1" 2>/dev/null; then return 0; fi
    if python3 - "$1" "$2" <<'PYEOF'
import sys
dane = open(sys.argv[1], 'rb').read()
sys.exit(0 if sys.argv[2].encode('utf-16-le') in dane else 1)
PYEOF
    then return 0; fi
    return 1
}
if [ -f /tmp/EdSharpNG_przed_544.exe ]; then
    for FRAZA in "Next Alignment" "Prior Font" "Calculate Date"; do
        if szukaj_w_binarce /tmp/EdSharpNG_przed_544.exe "$FRAZA"; then
            ok "BINARKA 5.0.43 MIALA napis $FRAZA (kontrola pozytywna sondy binarnej)"
        else bad "SONDA BINARNA GLUCHA: nie widzi $FRAZA nawet w starej binarce"; fi
        if ! szukaj_w_binarce EdSharpNG.exe "$FRAZA"; then
            ok "BINARKA po zmianie: napis $FRAZA zniknal"
        else bad "BINARKA po zmianie: napis $FRAZA NADAL JEST"; fi
    done
    # KONTROLA POZYTYWNA na NOWEJ binarce: napisy komend POZOSTAWIONYCH musza
    # byc obecne, inaczej "nie widze" znaczy tylko "sonda patrzy w zle miejsce".
    for FRAZA in "Insert Time" "Insert Table" "Baseline"; do
        if szukaj_w_binarce EdSharpNG.exe "$FRAZA"; then
            ok "KONTROLA POZYTYWNA: nowa binarka MA napis $FRAZA"
        else bad "KONTROLA POZYTYWNA PADLA: nowa binarka nie ma $FRAZA"; fi
    done
else
    echo "   (pominieto pomiar binarny: brak /tmp/EdSharpNG_przed_544.exe)"
fi

# ---------- BRAMKA: w repo MUSI zostac NOWA binarka ----------
KONCOWA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
if [ "$KONCOWA" = "$NOWA_SUMA" ]; then
    ok "BRAMKA: w repo lezy NOWA binarka (${KONCOWA:0:24})"
else
    bad "BRAMKA: binarka w repo ZMIENILA SIE - przywracam"
    cp /tmp/EdSharpNG_nowa_kn544.exe EdSharpNG.exe
    echo "   NIE WYSYLAJ paczki bez powtorzenia buildu"
fi

echo
echo "WYNIK: $PASS/$((PASS+FAIL))"
[ "$FAIL" = 0 ] || exit 1
