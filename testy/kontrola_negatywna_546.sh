#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.46 (przypisy z rodziny F6 do rodziny K).
#
# Ta zmiana to PRZENIESIENIE, nie dodanie ani usuniecie. Dlatego kazdy klawisz
# ma tu DWIE asercje: ze STARY chord zniknal I ze NOWY jest na miejscu. Sama
# obecnosc nowego nie odroznialaby przeniesienia od dopisania DRUGIEJ komendy na
# ten sam klawisz - a to jest realne ryzyko, bo duplikat chordu nie psuje
# buildu, tylko po cichu robi jeden z dwoch klawiszy nieosiagalnym.
#
# Rewizja odniesienia jest ARGUMENTEM, nie HEAD-em: skrypt biorący HEAD po
# commicie porownuje kod z samym soba i przechodzi zawsze.
# Skrypt SAM PILNUJE, ze w repo lezy NOWA binarka (sha256).
set -u
REPO="/mnt/d/projekty/edsharp-pr"
REV="${1:-84b1775}"          # stan 5.0.45
cd "$REPO" || exit 3

NOWA_SUMA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
echo "== rewizja odniesienia: $REV"
echo "== sha256 NOWEJ binarki: ${NOWA_SUMA:0:24}"

PASS=0; FAIL=0
ok()  { echo "PASS $1"; PASS=$((PASS+1)); }
bad() { echo "FAIL $1"; FAIL=$((FAIL+1)); }

git show "$REV:EdSharp.cs"  > /tmp/stary_546.cs     2>/dev/null || {
    echo "BLAD: nie moge wyciagnac EdSharp.cs z $REV"; exit 3; }
git show "$REV:Hotkeys.ini" > /tmp/stary_546_hk.ini 2>/dev/null || {
    echo "BLAD: nie moge wyciagnac Hotkeys.ini z $REV"; exit 3; }
git show "$REV:EdSharp.md"  > /tmp/stary_546.md     2>/dev/null || {
    echo "BLAD: nie moge wyciagnac EdSharp.md z $REV"; exit 3; }

# ---------- KONTROLE POZYTYWNE SONDY ----------
# Bez nich kazde "nie znalazlem" nizej znaczy tylko "czytam pusty plik".
LICZ_MENU=$(grep -c 'CreateMenuItem(' /tmp/stary_546.cs)
if [ "$LICZ_MENU" -gt 180 ]; then ok "SONDA: stare zrodlo ma $LICZ_MENU pozycji menu"
else bad "SONDA GLUCHA: tylko $LICZ_MENU pozycji menu w starym zrodle"; exit 2; fi
LICZ_HK=$(grep -c '=' /tmp/stary_546_hk.ini)
if [ "$LICZ_HK" -gt 200 ]; then ok "SONDA: stary Hotkeys.ini ma $LICZ_HK wpisow"
else bad "SONDA GLUCHA: tylko $LICZ_HK wpisow"; exit 2; fi

# ---------- CZESC 1: OBJAW ODTWORZONY NA STARYM KODZIE ----------
# Najpierw dowod, ze 5.0.45 NAPRAWDE trzymalo przypisy na F6. Bez tego
# "nie widze Control+F6" znaczy tylko, ze sonda patrzy w zle miejsce.
if grep -qF 'menuMiscInsertFootnote = CreateMenuItem("Insert Footnote ...", "Control+F6"' /tmp/stary_546.cs; then
    ok "objaw: 5.0.45 wstawialo przypis na Control+F6"
else bad "objaw NIE odtworzony: w 5.0.45 nie bylo Insert Footnote na Control+F6"; fi
if grep -qF 'menuMiscFootnoteList = CreateMenuItem("Footnote List ...", "Control+Shift+F6"' /tmp/stary_546.cs; then
    ok "objaw: 5.0.45 mialo liste przypisow na Control+Shift+F6"
else bad "objaw NIE odtworzony: listy przypisow nie bylo na Control+Shift+F6"; fi
if grep -qF 'Insert Footnote=Control+F6,' /tmp/stary_546_hk.ini; then
    ok "objaw: stary opis mowiony podawal Control+F6"
else bad "objaw NIE odtworzony w opisach mowionych"; fi

# ---------- CZESC 2: STARE CHORDY ZNIKLY ----------
for WZ in 'CreateMenuItem("Insert Footnote ...", "Control+F6"' \
          'CreateMenuItem("Footnote List ...", "Control+Shift+F6"' \
          'Insert Footnote=Control+F6,' \
          'Footnote List=Control+Shift+F6,'; do
    N=0
    for P in EdSharp.cs Hotkeys.ini hotkeys.txt EdSharp.md; do
        # UWAGA: grep -c przy braku pliku pisze na stderr i zwraca puste, a
        # "N + <puste>" wywala arytmetyke i CICHO pomijalo te asercje.
        C=$(grep -cF "$WZ" "$P" 2>/dev/null); C=${C:-0}
        N=$((N + C))
    done
    if [ "$N" -eq 0 ]; then ok "stary zapis zniknal: $WZ"
    else bad "stary zapis ZOSTAL ($N razy): $WZ"; fi
done

# ---------- CZESC 3: NOWE CHORDY SA NA MIEJSCU ----------
if grep -qF 'menuMiscInsertFootnote = CreateMenuItem("Insert Footnote ...", "Control+Shift+K"' EdSharp.cs; then
    ok "nowy chord: wstawianie przypisu na Control+Shift+K"
else bad "BRAK nowego chordu wstawiania przypisu"; fi
if grep -qF 'menuMiscFootnoteList = CreateMenuItem("Footnote List ...", "Alt+K"' EdSharp.cs; then
    ok "nowy chord: lista przypisow na Alt+K"
else bad "BRAK nowego chordu listy przypisow"; fi
for WZ in 'Insert Footnote=Control+Shift+K,' 'Footnote List=Alt+K,'; do
    for P in Hotkeys.ini hotkeys.txt EdSharp.md; do
        if grep -qF "$WZ" "$P"; then ok "opis mowiony w $P: $WZ"
        else bad "opis mowiony w $P NIE podaje: $WZ"; fi
    done
done

# ---------- CZESC 4: KONTROLE, ZE NIE ZEPSULEM DZIALAJACEGO ----------
# SPROSTOWANIE 30.08.2026 (5.0.47): ta asercja pilnowala, ze kontekstowy skok
# ZOSTAJE na Alt+F6 - i bylo to sluszne, dopoki losu tego klawisza nie
# rozstrzygnal.  Rozstrzygnal go tego samego dnia o 10:01: "Skok przypis tekst
# tekst przypis robimy Alt-CTRL-ka (...) Tak z ego F6 w przypisach bysmy
# rezygnowali."  Sonda pytajaca o Alt+F6 zaczela wiec KLAMAC: pokazywala FAIL
# przy poprawnym kodzie i zaciemniala prawdziwe regresje w tym samym przebiegu.
# Pilnuje tego, co pilnowala naprawde: ze komenda skoku ISTNIEJE i nie zginela
# przy porzadkach - tylko na klawiszu, ktory on wybral.
if grep -qF 'menuMiscGoToFootnote = CreateMenuItem("Go to Footnote", "Control+Alt+K"' EdSharp.cs; then
    ok "KONTROLA: kontekstowy skok do przypisu istnieje, na Control+Alt+K (jego wybor z 30.08)"
else bad "KONTROLA PADLA: zgubilem Go to Footnote"; fi
# Rodzina komentarzy (5.0.43) i rodzina zakladek/linku (5.0.45) NIETKNIETE.
for PARA in 'Insert Comment|Alt+F9' 'Next Comment|Control+Shift+F9' \
            'Prior Comment|Alt+Shift+F9' 'Comment List|Control+Alt+F9' \
            'Set Bookmark|Control+B' 'Go to Bookmark|Alt+B' 'Insert Link|Control+K'; do
    NAZ="${PARA%%|*}"; CH="${PARA##*|}"
    if grep -qF "$NAZ=$CH," Hotkeys.ini; then ok "KONTROLA: $NAZ nadal na $CH"
    else bad "KONTROLA PADLA: $NAZ nie jest na $CH"; fi
done
# Control+F6 dostal LISTE LINKOW w 5.0.53 (ustalenia edsharpng-37 i 47). Ta
# asercja pilnowala wczesniej, ze klawisz jest WOLNY - i od 5.0.53 klamalaby,
# bo zwolnilismy go WLASNIE pod te liste. Pilnujemy teraz tego, co jest
# prawdziwym wymogiem: na Control+F6 stoi lista linkow, a nie skok przypisu.
if grep -qF 'Link List=Control+F6,' Hotkeys.ini; then
    ok "Control+F6 to LISTA LINKOW (klawisz zwolniony wlasnie pod nia)"
else bad "Control+F6 nie ma opisu listy linkow"; fi
if [ "$(grep -c '^Go to Footnote=Control+F6,' Hotkeys.ini)" -eq 0 ]; then
    ok "skok przypisu NIE wrocil na Control+F6"
else bad "skok przypisu znowu siedzi na Control+F6"; fi
# Zero duplikatow chordu - przeniesienie nie moglo zostawic dwoch komend.
# ZAWEZONE DO KODU, i to jest poprawka MOJEJ WLASNEJ SONDY, nie kodu programu.
# Pierwsza wersja liczyla duplikaty po calym Hotkeys.ini i zglosila Control+I
# oraz Control+Shift+I. Sprawdzone: to NIE kolizja. Wpisy "Insert Script Path"
# i "Insert All Users Path" opisuja klawisze dzialajace W OKIENKU Otworz/Zapisz
# (wstawianie sciezek skryptow JAWS), a "Next Indent" dziala w edytorze - inny
# kontekst, wiec ten sam chord nie jest konfliktem. Do tego oba wpisy siedzialy
# tam juz w 5.0.45, wiec nie sa niczyja regresja. Zrodlem prawdy o skrotach
# EDYTORA jest CreateMenuItem w kodzie, i po nim liczymy.
DUP=$(grep -oP 'CreateMenuItem\("[^"]*",\s*"\K[A-Za-z0-9+]+(?=")' EdSharp.cs \
      | grep -v '^$' | sort | uniq -d | tr '\n' ' ')
if [ -z "$DUP" ]; then ok "zero duplikatow chordu wsrod komend edytora"
else bad "DUPLIKATY CHORDU: $DUP"; fi

# ---------- CZESC 5: POMIAR NA BINARCE, nie tylko na zrodle ----------
# Grep ASCII po binarce .NET daje zero i wyglada jak brak komendy - to znana
# pulapka tego repo. Napisy sa w kodowaniu szerokim.
szuk() {
    if grep -qa "$1" EdSharpNG.exe; then return 0; fi
    python3 - "$1" <<'PY'
import sys
igla=sys.argv[1].encode("utf-16-le")
sys.exit(0 if igla in open("/mnt/d/projekty/edsharp-pr/EdSharpNG.exe","rb").read() else 1)
PY
}
if szuk "Insert Footnote"; then ok "BINARKA: napis Insert Footnote obecny"
else bad "BINARKA: brak napisu Insert Footnote"; fi
if szuk "Footnote List"; then ok "BINARKA: napis Footnote List obecny"
else bad "BINARKA: brak napisu Footnote List"; fi

# ---------- BRAMKA: w repo lezy NOWA binarka ----------
PO="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
if [ "$PO" = "$NOWA_SUMA" ]; then ok "BRAMKA: binarka nietknieta w trakcie pomiaru"
else bad "BRAMKA: binarka zmieniona w trakcie pomiaru"; fi

echo "----"
echo "PASS=$PASS FAIL=$FAIL"
[ "$FAIL" -eq 0 ] || exit 1
