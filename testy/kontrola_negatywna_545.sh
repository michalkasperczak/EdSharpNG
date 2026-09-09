#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.45 (porzadkowanie skrotow: bloki kodu, zakladki,
# link, rodzina F4, Ctrl+T -- zlecenie 1788041000816-5).
#
# Ta zmiana jest w polowie USUWANIEM, a przy usuwaniu "nie widze komendy X" nie
# dowodzi niczego, dopoki ta sama sonda nie ZOBACZY jej w kodzie sprzed zmiany.
# W drugiej polowie jest PRZENOSZENIEM, wiec kazdy przeniesiony klawisz ma tu
# DWIE asercje: ze STARY chord zniknal I ze NOWY jest na miejscu.  Sama
# obecnosc nowego nie odrozniloby przeniesienia od dopisania drugiej komendy.
#
# Rewizja odniesienia jest ARGUMENTEM, nie HEAD-em: skrypt biorący HEAD po
# commicie porownuje kod z samym soba.
# Skrypt SAM PILNUJE, ze na koncu w repo lezy NOWA binarka (sha256).
set -u
REPO="/mnt/d/projekty/edsharp-pr"
REV="${1:-ae113fd}"          # stan 5.0.44
cd "$REPO" || exit 3

NOWA_SUMA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
echo "== rewizja odniesienia: $REV"
echo "== sha256 NOWEJ binarki: ${NOWA_SUMA:0:24}"
cp EdSharpNG.exe /tmp/EdSharpNG_nowa_kn545.exe || exit 3

PASS=0; FAIL=0
ok()   { echo "PASS $1"; PASS=$((PASS+1)); }
bad()  { echo "FAIL $1"; FAIL=$((FAIL+1)); }

git show "$REV:EdSharp.cs"   > /tmp/stary_545.cs     2>/dev/null || {
    echo "BLAD: nie moge wyciagnac EdSharp.cs z $REV"; exit 3; }
git show "$REV:Hotkeys.ini"  > /tmp/stary_545_hk.ini 2>/dev/null || {
    echo "BLAD: nie moge wyciagnac Hotkeys.ini z $REV"; exit 3; }
git show "$REV:EdSharp.md"   > /tmp/stary_545.md     2>/dev/null || {
    echo "BLAD: nie moge wyciagnac EdSharp.md z $REV"; exit 3; }

# ---------- KONTROLE POZYTYWNE SONDY ----------
# Bez nich kazde "nie znalazlem" nizej znaczy tylko "czytam pusty plik".
LICZ_MENU=$(grep -c 'CreateMenuItem(' /tmp/stary_545.cs)
if [ "$LICZ_MENU" -gt 180 ]; then ok "SONDA: stare zrodlo ma $LICZ_MENU pozycji menu"
else bad "SONDA GLUCHA: tylko $LICZ_MENU pozycji menu w starym zrodle"; exit 2; fi
LICZ_HK=$(grep -c '=' /tmp/stary_545_hk.ini)
if [ "$LICZ_HK" -gt 200 ]; then ok "SONDA: stary Hotkeys.ini ma $LICZ_HK wpisow"
else bad "SONDA GLUCHA: tylko $LICZ_HK wpisow w starym Hotkeys.ini"; exit 2; fi
LICZ_MD=$(wc -l < /tmp/stary_545.md)
if [ "$LICZ_MD" -gt 700 ]; then ok "SONDA: stary podrecznik ma $LICZ_MD linii"
else bad "SONDA GLUCHA: tylko $LICZ_MD linii w starym podreczniku"; exit 2; fi

# ---------- CZESC 1: USUNIETE KOMENDY, kazda na czterech warstwach ----------
# UWAGA POMIAROWA z tresci zlecenia: komenda nazywa sie menuNavigateNextBlock,
# BEZ slowa "Code".  Wzorzec "menuNavigateNextCodeBlock" daje zero trafien i
# wyglada jak "juz usuniete" - falszywe zero z wzorca, ktory nie mogl trafic.
# Dlatego kazda para nazwa|pole jest tu wypisana DOSLOWNIE.
for PARA in "Next Block|menuNavigateNextBlock" \
            "Prior Block|menuNavigatePriorBlock" \
            "Block|menuQueryBlock" \
            "Windows Open|menuQueryWindowsOpen" \
            "&Text Convert|menuMiscTextConvert"; do
    NAZWA="${PARA%%|*}"; POLE="${PARA##*|}"
    if grep -qF "CreateMenuItem(\"$NAZWA\"" /tmp/stary_545.cs; then
        ok "objaw: 5.0.44 MIALO pozycje menu $NAZWA"
    else bad "objaw NIE odtworzony: $NAZWA nie bylo w 5.0.44 (nie ma czego usuwac)"; fi
    if grep -qF "menuItem == $POLE" /tmp/stary_545.cs; then
        ok "objaw: 5.0.44 MIALO handler $POLE"
    else bad "objaw NIE odtworzony: brak handlera $POLE w 5.0.44"; fi
    if ! grep -qF "$POLE" EdSharp.cs; then
        ok "po zmianie: $POLE zniknelo z kodu w calosci"
    else bad "NIEDOKONCZONE: $POLE nadal w kodzie"; fi
    if ! grep -qF "CreateMenuItem(\"$NAZWA\"" EdSharp.cs; then
        ok "po zmianie: pozycja menu $NAZWA zniknela"
    else bad "NIEDOKONCZONE: pozycja menu $NAZWA nadal jest"; fi
done

# Opisy mowione (Ctrl+F1 czyta je niewidomemu).  Nazwa klucza to menuItem.Name,
# czyli tekst BEZ '&' - dlatego "Text Convert", nie "&Text Convert".
for NAZWA in "Next Block" "Prior Block" "Say Block" "Windows Open" "Text Convert"; do
    if grep -q "^$NAZWA=" /tmp/stary_545_hk.ini; then
        ok "objaw: 5.0.44 MIALO opis mowiony $NAZWA"
    else bad "objaw NIE odtworzony: brak opisu $NAZWA w 5.0.44"; fi
    if ! grep -q "^$NAZWA=" Hotkeys.ini && ! grep -q "^$NAZWA=" hotkeys.txt && ! grep -q "^$NAZWA=" EdSharp.md; then
        ok "po zmianie: opis mowiony $NAZWA zniknal ze WSZYSTKICH TRZECH plikow"
    else bad "MARTWY OPIS: $NAZWA nadal w spisie skrotow"; fi
done

# Metoda WindowsOpen: jedyny jej konsument zniknal, wiec ma pojsc razem z
# komenda.  To warstwa piata z lekcji 5.0.44 (osierocony helper).
if grep -q 'public void WindowsOpen()' /tmp/stary_545.cs; then
    ok "objaw: 5.0.44 mialo metode WindowsOpen"
else bad "objaw NIE odtworzony: brak metody WindowsOpen w 5.0.44"; fi
if ! grep -q 'WindowsOpen' EdSharp.cs; then
    ok "po zmianie: metoda WindowsOpen nie zostala osierocona"
else bad "OSIEROCONA METODA: WindowsOpen nadal w kodzie bez wolajacego"; fi

# ---------- CZESC 2: PRZENIESIENIA - stary chord znika, nowy jest ----------
# Kazde przeniesienie ma DWIE asercje.  Sam nowy chord nie odroznia
# przeniesienia od dopisania drugiej komendy pod tym samym imieniem.
for TROJKA in "Set Bookmar&k|Control+K|Control+B" \
              "Go to Bookmark|Alt+K|Alt+B" \
              "Close All but Current Window|Control+Shift+F4|Control+Shift+W" \
              "Go to Folder|Control+D0|Shift+F4" \
              "Go to Special Folder|Control+Alt+D0|Control+Shift+F4"; do
    NAZWA="$(echo "$TROJKA" | cut -d'|' -f1)"
    STARY="$(echo "$TROJKA" | cut -d'|' -f2)"
    NOWY="$(echo  "$TROJKA" | cut -d'|' -f3)"
    if grep -qF "CreateMenuItem(\"$NAZWA\", \"$STARY\"" /tmp/stary_545.cs; then
        ok "objaw: 5.0.44 mialo $NAZWA na $STARY"
    else bad "objaw NIE odtworzony: $NAZWA nie bylo na $STARY w 5.0.44"; fi
    if ! grep -qF "CreateMenuItem(\"$NAZWA\", \"$STARY\"" EdSharp.cs; then
        ok "po zmianie: $NAZWA NIE jest juz na $STARY"
    else bad "NIEPRZENIESIONE: $NAZWA nadal na $STARY"; fi
    if grep -qF "CreateMenuItem(\"$NAZWA\", \"$NOWY\"" EdSharp.cs; then
        ok "po zmianie: $NAZWA jest na $NOWY"
    else bad "BRAK CELU: $NAZWA nie ma chorda $NOWY"; fi
done

# Clear Bookmark schodzi do MENU-ONLY (pusty chord), bo usuwanie zakladki ma
# isc przez Delete na liscie.  To nie to samo co usuniecie komendy.
if grep -qF 'CreateMenuItem("Clear Bookmark", "Control+Shift+K"' /tmp/stary_545.cs; then
    ok "objaw: 5.0.44 mialo Clear Bookmark na Control+Shift+K"
else bad "objaw NIE odtworzony: Clear Bookmark nie bylo na Control+Shift+K"; fi
if grep -qF 'CreateMenuItem("Clear Bookmark", ""' EdSharp.cs; then
    ok "po zmianie: Clear Bookmark jest menu-only (bez chorda)"
else bad "Clear Bookmark nie zeszlo do menu-only"; fi
if grep -qF 'menuItem == menuNavigateClearBookmark' EdSharp.cs; then
    ok "KONTROLA: Clear Bookmark nadal DZIALA (handler jest), tylko bez klawisza"
else bad "REGRESJA: zniknal handler Clear Bookmark"; fi

# ---------- CZESC 3: NOWA KOMENDA - wstawianie linku ----------
if ! grep -q 'menuMiscInsertLink' /tmp/stary_545.cs; then
    ok "objaw: 5.0.44 NIE mialo wstawiania linku (jest nowe)"
else bad "objaw NIE odtworzony: menuMiscInsertLink juz bylo w 5.0.44"; fi
if grep -qF 'CreateMenuItem("Insert &Link ...", "Control+K"' EdSharp.cs; then
    ok "po zmianie: Insert Link jest na Control+K"
else bad "BRAK: Insert Link nie ma chorda Control+K"; fi
if grep -q 'menuItem == menuMiscInsertLink' EdSharp.cs; then
    ok "po zmianie: Insert Link ma handler"
else bad "BRAK: Insert Link bez handlera (martwa pozycja menu)"; fi
if grep -q 'private void InsertMarkdownLink' EdSharp.cs; then
    ok "po zmianie: metoda InsertMarkdownLink istnieje"
else bad "BRAK: metody InsertMarkdownLink"; fi
# KOTWICE Z TEGO SAMEGO ZRODLA CO SPIS TRESCI.  To jest sedno punktu 3
# zlecenia: wlasna implementacja specyfikacji dalaby link martwy po eksporcie.
if sed -n '/private void InsertMarkdownLink/,/InsertMarkdownLink method/p' EdSharp.cs | grep -q 'BuildMarkdownAnchors'; then
    ok "KLUCZOWE: link wewnetrzny liczy kotwice przez BuildMarkdownAnchors (to samo co spis tresci)"
else bad "KLUCZOWE PADLO: link wewnetrzny NIE uzywa BuildMarkdownAnchors"; fi
# Bramka na blok kodu - ta sama zasada, co przy przypisie i komentarzu.
if sed -n '/private void InsertMarkdownLink/,/InsertMarkdownLink method/p' EdSharp.cs | grep -q 'IsMarkdownIndexInFence'; then
    ok "Insert Link ma bramke na blok kodu"
else bad "BRAK BRAMKI: link da sie wstawic w blok kodu, gdzie jest martwy"; fi
# Obrazek bez opisu = niewidzialny dla czytnika; komenda ma odmowic.
if grep -qF 'An image needs a description for screen reader users!' EdSharp.cs; then
    ok "Insert Link odmawia wstawienia obrazka bez opisu"
else bad "BRAK: obrazek bez tekstu alternatywnego przechodzi po cichu"; fi

# ---------- CZESC 4: KONTROLE, ZE NIE ZNIKNELO NIC WIECEJ ----------
# Najgrozniejsze sa rzeczy sasiadujace z usunietymi w tym samym zdaniu kodu.
for PARA in "Next Indent|Control+I" \
            "Prior Indent|Control+Shift+I" \
            "Indentation|Alt+I" \
            "Right Brace|Control+Shift+OemCloseBrackets" \
            "Left Brace|Control+Shift+OemOpenBrackets" \
            "Braces|Alt+Shift+OemCloseBrackets" \
            "Text Combine|" \
            "Current Windows ...|F4" \
            "&Close Window|Control+F4" \
            "Next Bookmark|Shift+PageDown" \
            "Prior Bookmark|Shift+PageUp" \
            "Insert Table ...|Control+Shift+T" \
            "Insert Footnote ...|Control+Shift+K" \
            "Insert Comment ...|Alt+F9" \
            "Table of Contents|Alt+Shift+T"; do
    NAZWA="${PARA%%|*}"; CHORD="${PARA##*|}"
    if grep -qF "CreateMenuItem(\"$NAZWA\", \"$CHORD\"" EdSharp.cs; then
        ok "KONTROLA (zostaje): $NAZWA nadal na \"$CHORD\""
    else bad "REGRESJA: $NAZWA stracila chord \"$CHORD\""; fi
done
# Text Combine dzieli handler z usunieta Text Convert - najlatwiejsza tu
# regresja to wyciecie obu naraz przy zielonym buildzie.
if grep -q 'menuItem == menuMiscTextCombine' EdSharp.cs; then
    ok "KONTROLA (zostaje): Text Combine ma nadal swoj handler"
else bad "REGRESJA: usunalem Text Combine razem z Text Convert"; fi
# Lista zakladek z Delete - to ona zastepuje usuniety klawisz czyszczenia.
if grep -q 'public static string PickBookmark' EdSharp.cs; then
    ok "KONTROLA (zostaje): lista zakladek z Delete istnieje"
else bad "REGRESJA: zniknela lista zakladek, a to ona zastepuje Control+Shift+K"; fi
# Skrot Control+Shift+9 (szkielet linku) to INNA droga niz nowe okno.
if grep -q 'InsertMarkdownLinkShortcut' EdSharp.cs; then
    ok "KONTROLA (zostaje): Control+Shift+9 (szkielet linku) nietkniety"
else bad "REGRESJA: zniknal szkielet linku spod Control+Shift+9"; fi
# Obsluga JAWS na golym F9 siedzi POZA spisem skrotow.
if grep -qF 'keyData == Keys.F9' EdSharp.cs && grep -qF 'SayAllTempFile' EdSharp.cs; then
    ok "KONTROLA (nietkniete): czytanie do konca (JAWS) na golym F9 dziala"
else bad "REGRESJA: zabralem F9 obsludze JAWS"; fi
# Rzeczy, ktorych ta zmiana dotykac NIE MOZE (poprzednie wersje).
for FRAZA in 'RunMarkdownTableWizard' 'InsertOrEditMarkdownComment' 'InsertMarkdownFootnote' \
             'BuildMarkdownContentsText' 'HandleCloseWindowKey' 'HandleFileSlotKey'; do
    if grep -q "$FRAZA" EdSharp.cs; then ok "KONTROLA (nietkniete): $FRAZA"
    else bad "REGRESJA: zniknelo $FRAZA"; fi
done

# ---------- CZESC 5: ZADNEJ KOLIZJI CHORDOW ----------
# CreateMenuItem przy kolizji pokazuje modalny Alert NA STARCIE APKI i drugiego
# chorda NIE rejestruje - czyli komenda po cichu przestaje dzialac.
DUP=$(grep -n 'CreateMenuItem' EdSharp.cs | grep -v '^\s*[0-9]*:\s*//' \
      | sed 's/.*CreateMenuItem("[^"]*", \("[^"]*"\).*/\1/' \
      | grep -E '^"[^"]+"$' | sort | uniq -d)
if [ -z "$DUP" ]; then ok "BRAK KOLIZJI: zaden chord nie jest przypisany dwa razy"
else bad "KOLIZJA CHORDOW: $DUP"; fi
# Zwolnione chordy NIE MOGA naleze do zadnej komendy.
for WOLNY in '"Control+Shift+B"' '"Control+Shift+K"' '"Alt+K"' '"Control+D0"' '"Control+Alt+D0"'; do
    if ! grep -E 'CreateMenuItem\("[^"]*", '"$WOLNY" EdSharp.cs | grep -qv '^\s*//'; then
        ok "ZWOLNIONY: $WOLNY nie nalezy do zadnej komendy"
    else bad "$WOLNY nadal zajety"; fi
done

# ---------- CZESC 6: PODRECZNIK nie moze KLAMAC ----------
if grep -qF 'Press Control+B to go to the next code block' /tmp/stary_545.md; then
    ok "objaw: 5.0.44 obiecywalo w prozie skoki po blokach kodu"
else bad "objaw NIE odtworzony: brak tej obietnicy w 5.0.44"; fi
if ! grep -qF 'Press Control+B to go to the next code block' EdSharp.md; then
    ok "po zmianie: proza nie obiecuje skokow po blokach kodu"
else bad "PODRECZNIK KLAMIE: nadal obiecuje skoki po blokach"; fi
if grep -qF 'Press Shift+F4 to hear the titles of open document windows' /tmp/stary_545.md; then
    ok "objaw: 5.0.44 obiecywalo mowienie tytulow okien pod Shift+F4"
else bad "objaw NIE odtworzony: brak tej obietnicy w 5.0.44"; fi
if ! grep -qF 'Press Shift+F4 to hear the titles of open document windows' EdSharp.md; then
    ok "po zmianie: proza nie obiecuje tytulow okien pod Shift+F4"
else bad "PODRECZNIK KLAMIE: nadal obiecuje tytuly okien"; fi
if grep -qF 'Press Control+K to set a bookmark' /tmp/stary_545.md; then
    ok "objaw: 5.0.44 obiecywalo zakladke pod Control+K"
else bad "objaw NIE odtworzony: brak tej obietnicy w 5.0.44"; fi
if grep -qF 'Press Control+B to set a bookmark' EdSharp.md && ! grep -qF 'Press Control+K to set a bookmark' EdSharp.md; then
    ok "po zmianie: proza mowi o zakladce pod Control+B, nie pod Control+K"
else bad "PODRECZNIK KLAMIE o zakladkach"; fi
if grep -qF 'Press Control+K to insert a link' EdSharp.md; then
    ok "po zmianie: proza opisuje wstawianie linku pod Control+K"
else bad "BRAK OPISU: podrecznik milczy o nowej komendzie"; fi
if grep -qF 'Control+Shift+F4 to close all windows except the current one' /tmp/stary_545.md; then
    ok "objaw: 5.0.44 obiecywalo zamykanie okien pod Control+Shift+F4"
else bad "objaw NIE odtworzony: brak tej obietnicy w 5.0.44"; fi
if grep -qF 'Control+Shift+W to close all windows except the current one' EdSharp.md; then
    ok "po zmianie: proza mowi o Control+Shift+W"
else bad "PODRECZNIK KLAMIE o zamykaniu okien"; fi
if grep -qF 'Use the Text Convert command' EdSharp.md; then
    bad "PODRECZNIK KLAMIE: nadal kaze uzywac usunietej Text Convert"
else ok "po zmianie: proza nie odsyla do usunietej Text Convert"; fi
# KONTROLA POZYTYWNA prozy: nie wycialem za duzo.
for FRAZA in 'Press Control+I to go to the next change in indentation' \
             'Press F4 to activate an editing window' \
             'Use the Text Combine command'; do
    if grep -qF "$FRAZA" EdSharp.md; then ok "KONTROLA (zostaje w prozie): $FRAZA"
    else bad "REGRESJA prozy: zniknelo \"$FRAZA\""; fi
done

# ---------- CZESC 7: CRLF we wszystkich ruszonych plikach ----------
for PLIK in EdSharp.cs Hotkeys.ini hotkeys.txt EdSharp.md; do
    LINIE=$(wc -l < "$PLIK")
    CRLF=$(grep -c $'\r$' "$PLIK")
    if [ "$LINIE" = "$CRLF" ]; then ok "CRLF: $PLIK $CRLF/$LINIE"
    else bad "CRLF ZEPSUTE: $PLIK $CRLF/$LINIE"; fi
done

# ---------- CZESC 8: dowod na BINARCE, nie na zrodle ----------
# Napisy .NET sa w kodowaniu SZEROKIM, wiec grep ASCII po binarce daje zero i
# wyglada jak brak komendy.  Szukamy w OBU kodowaniach.
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
if [ -f /tmp/EdSharpNG_przed_545.exe ]; then
    for FRAZA in "Next Block" "Prior Block" "Windows Open" "Text Convert"; do
        if szukaj_w_binarce /tmp/EdSharpNG_przed_545.exe "$FRAZA"; then
            ok "BINARKA 5.0.44 MIALA napis $FRAZA (kontrola pozytywna sondy binarnej)"
        else bad "SONDA BINARNA GLUCHA: nie widzi $FRAZA nawet w starej binarce"; fi
        if ! szukaj_w_binarce EdSharpNG.exe "$FRAZA"; then
            ok "BINARKA po zmianie: napis $FRAZA zniknal"
        else bad "BINARKA po zmianie: napis $FRAZA NADAL JEST"; fi
    done
    # Nowa komenda: w starej binarce jej NIE MA, w nowej JEST.  Bez pierwszej
    # polowy "jest" nie odrozniloby sie od napisu, ktory tam lezal od zawsze.
    for FRAZA in "Insert Link" "Internal link inserted"; do
        if ! szukaj_w_binarce /tmp/EdSharpNG_przed_545.exe "$FRAZA"; then
            ok "BINARKA 5.0.44 NIE miala napisu $FRAZA"
        else bad "kontrola rozniczujaca padla: $FRAZA bylo juz w 5.0.44"; fi
        if szukaj_w_binarce EdSharpNG.exe "$FRAZA"; then
            ok "BINARKA po zmianie: napis $FRAZA jest"
        else bad "BINARKA po zmianie: brak napisu $FRAZA"; fi
    done
    # KONTROLA POZYTYWNA na NOWEJ binarce: napisy komend POZOSTAWIONYCH.
    for FRAZA in "Text Combine" "Insert Table" "Set Bookmar" "Clear Bookmark"; do
        if szukaj_w_binarce EdSharpNG.exe "$FRAZA"; then
            ok "KONTROLA POZYTYWNA: nowa binarka MA napis $FRAZA"
        else bad "KONTROLA POZYTYWNA PADLA: nowa binarka nie ma $FRAZA"; fi
    done
else
    echo "   (pominieto pomiar binarny: brak /tmp/EdSharpNG_przed_545.exe)"
fi

# ---------- BRAMKA: w repo MUSI zostac NOWA binarka ----------
KONCOWA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
if [ "$KONCOWA" = "$NOWA_SUMA" ]; then
    ok "BRAMKA: w repo lezy NOWA binarka (${KONCOWA:0:24})"
else
    bad "BRAMKA: binarka w repo ZMIENILA SIE - przywracam"
    cp /tmp/EdSharpNG_nowa_kn545.exe EdSharpNG.exe
    echo "   NIE WYSYLAJ paczki bez powtorzenia buildu"
fi

echo
echo "WYNIK: $PASS/$((PASS+FAIL))"
[ "$FAIL" = 0 ] || exit 1
