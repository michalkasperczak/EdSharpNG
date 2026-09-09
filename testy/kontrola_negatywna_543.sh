#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.43 (komentarze wewnetrzne, punkt 4 mapy):
# buduje binarke SPRZED zmiany z JAWNEJ rewizji i wymaga, zeby objaw sie na
# niej ODTWORZYL.  Bez tego zielony wynik po zmianie nie odroznia "dolozylem
# funkcje" od "sonda nie widzi".
#
# Rewizja odniesienia jest ARGUMENTEM, nie HEAD-em: skrypt biorący HEAD po
# zacommitowaniu zmiany porownuje kod z samym soba i daje 0 asercji
# padajacych, co wyglada jak awaria, a znaczy tylko "nie ma czego porownac".
#
# Skrypt SAM PILNUJE, ze na koncu w repo lezy NOWA binarka (sha256).
set -u
REPO="/mnt/d/projekty/edsharp-pr"
REV="${1:-345ff47}"          # stan 5.0.42
cd "$REPO" || exit 3

CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
NOWA_SUMA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
echo "== rewizja odniesienia: $REV"
echo "== sha256 NOWEJ binarki: ${NOWA_SUMA:0:24}"
cp EdSharpNG.exe /tmp/EdSharpNG_nowa_kn543.exe || exit 3

PASS=0; FAIL=0
ok()   { echo "PASS $1"; PASS=$((PASS+1)); }
bad()  { echo "FAIL $1"; FAIL=$((FAIL+1)); }

# ---------- CZESC 1: asercje na ZRODLE starej rewizji ----------
git show "$REV:EdSharp.cs" > /tmp/stary_543.cs 2>/dev/null || {
    echo "BLAD: nie moge wyciagnac EdSharp.cs z $REV"; exit 3; }
git show "$REV:Hotkeys.ini" > /tmp/stary_543_hk.ini 2>/dev/null || {
    echo "BLAD: nie moge wyciagnac Hotkeys.ini z $REV"; exit 3; }

# KONTROLE POZYTYWNE SONDY: stare pliki MUSZA byc czytane poprawnie, inaczej
# kazde "nie znalazlem" ponizej znaczy tylko "czytam pusty plik".
LICZ_MENU=$(grep -c 'CreateMenuItem(' /tmp/stary_543.cs)
if [ "$LICZ_MENU" -gt 180 ]; then ok "SONDA: stare zrodlo ma $LICZ_MENU pozycji menu"
else bad "SONDA GLUCHA: tylko $LICZ_MENU pozycji menu w starym zrodle"; exit 2; fi
LICZ_HK=$(grep -c '=' /tmp/stary_543_hk.ini)
if [ "$LICZ_HK" -gt 200 ]; then ok "SONDA: stary Hotkeys.ini ma $LICZ_HK wpisow"
else bad "SONDA GLUCHA: tylko $LICZ_HK wpisow w starym Hotkeys.ini"; exit 2; fi

# 1. Stary kod NIE ZNAL zadnej z metod obslugi komentarzy.
for METODA in GetMarkdownComments InsertOrEditMarkdownComment GoToMarkdownComment \
              ShowMarkdownCommentList SanitizeMarkdownCommentBody \
              FindMarkdownCommentAtIndex GetMarkdownCommentSpeech \
              GetTextLineNumberAtIndex MarkdownCommentRegex; do
    if ! grep -q "$METODA" /tmp/stary_543.cs; then
        ok "objaw 1: stary kod nie mial $METODA"
    else bad "objaw 1 NIE odtworzony - $METODA juz istniala"; fi
done

# 2. Klasa opisujaca komentarz nie istniala.
if ! grep -q 'class MarkdownComment' /tmp/stary_543.cs; then
    ok "objaw 2: stary kod nie mial klasy MarkdownComment"
else bad "objaw 2 NIE odtworzony - klasa juz byla"; fi

# 3. Pozycje menu nie istnialy, wiec funkcji nie bylo skad wywolac.
for POZ in menuMiscInsertComment menuMiscNextComment menuMiscPriorComment menuMiscCommentList; do
    if ! grep -q "$POZ" /tmp/stary_543.cs; then
        ok "objaw 3: stare menu nie mialo pozycji $POZ"
    else bad "objaw 3 NIE odtworzony - $POZ juz byla w menu"; fi
done

# 4. Chordy MUSZA byc naprawde wolne - i tu nauczka z tego przebiegu: sam
#    Hotkeys.ini NIE WYSTARCZY.  Goly F9 i Shift+F9 nie maja tam wpisu, a sa
#    przechwytywane WPROST w ProcessCmdKey_Helper (skrypt JAWS SayAllTempFile).
#    Pierwsza wersja tej zmiany zajela je komentarzom i klawisz byl MARTWY przy
#    zielonym buildzie.  Dlatego sprawdzamy OBA miejsca.
for CHORD in '=Alt+F9,' '=Control+Shift+F9,' '=Alt+Shift+F9,' '=Control+Alt+F9,'; do
    if ! grep -q -- "$CHORD" /tmp/stary_543_hk.ini; then
        ok "objaw 4: chord $CHORD byl wolny w spisie skrotow 5.0.42"
    else bad "KOLIZJA: chord $CHORD byl JUZ ZAJETY - zabralem dzialajaca komende"; fi
done
# Drugie miejsce: jawne warunki w ProcessCmdKey_Helper, ktore biora zdarzenie
# PRZED tablica skrotow menu.  Zbior chordow komentarzy nie moze ich dotykac.
for WAR in 'keyData == (Keys.Alt | Keys.F9)' \
           'keyData == (Keys.Control | Keys.Shift | Keys.F9)' \
           'keyData == (Keys.Alt | Keys.Shift | Keys.F9)' \
           'keyData == (Keys.Control | Keys.Alt | Keys.F9)'; do
    if ! grep -qF "$WAR" /tmp/stary_543.cs; then
        ok "objaw 4b: brak jawnego przechwycenia [$WAR] w starym kodzie"
    else bad "KOLIZJA W KODZIE: [$WAR] byl przechwytywany wprost"; fi
done
# KONTROLA POZYTYWNA tej sondy, i zarazem dowod na pulapke: goly F9 i Shift+F9
# BYLY przechwytywane w kodzie, mimo ze w Hotkeys.ini ich nie ma.  Bez tej
# kontroli "wszystkie chordy F9 sa wolne" nie odroznia sie od sondy, ktora
# patrzy w zle miejsce - a wlasnie to zdarzylo sie w tym przebiegu.
if grep -qF 'keyData == Keys.F9' /tmp/stary_543.cs; then
    ok "KONTROLA POZYTYWNA: goly F9 BYL przechwytywany w kodzie (sonda widzi drugie miejsce)"
else bad "KONTROLA POZYTYWNA PADLA: sonda nie widzi przechwycenia F9 w kodzie"; fi
if grep -qF 'keyData == (Keys.Shift | Keys.F9)' /tmp/stary_543.cs; then
    ok "KONTROLA POZYTYWNA: Shift+F9 BYL przechwytywany w kodzie"
else bad "KONTROLA POZYTYWNA PADLA: sonda nie widzi przechwycenia Shift+F9"; fi
# I te dwa MUSZA zostac nietkniete po zmianie - naleza do obslugi JAWS.
if grep -qF 'keyData == Keys.F9' EdSharp.cs && grep -qF 'SayAllTempFile' EdSharp.cs; then
    ok "KONTROLA: czytanie do konca (JAWS) na golym F9 NADAL dziala po zmianie"
else bad "REGRESJA: zabralem F9 obsludze JAWS"; fi
if grep -qF 'keyData == (Keys.Shift | Keys.F9)' EdSharp.cs; then
    ok "KONTROLA: Shift+F9 NADAL nalezy do tego samego skryptu"
else bad "REGRESJA: zabralem Shift+F9 obsludze JAWS"; fi
# KONTROLA POZYTYWNA na Hotkeys.ini: Control+F9 BYL zajety i ma zostac.
if grep -q -- '=Control+F9,' /tmp/stary_543_hk.ini; then
    ok "KONTROLA POZYTYWNA: Control+F9 BYL zajety (Say Compiler) - sonda widzi zajete wpisy"
else bad "KONTROLA POZYTYWNA PADLA: sonda nie widzi nawet zajetego Control+F9"; fi
# I ten sam chord MUSI byc zajety NADAL, czyli niczego mu nie odebralem.
if grep -q -- '=Control+F9,' Hotkeys.ini; then
    ok "KONTROLA: Control+F9 (Say Compiler) NADAL zajety po zmianie"
else bad "REGRESJA: Control+F9 zniknal z Hotkeys.ini"; fi

# ---------- CZESC 2: SKLADNIA ZMIERZONA NASZYMI KONWERTERAMI ----------
# To jest wlasciwy dowod, ze wybrana skladnia jest ta, o ktora prosil:
# komentarz ma ZOSTAC w Markdown, a WYLECIEC z Worda, HTML-a i tekstu.
# Mierzone NASZYMI narzedziami z katalogu Convert, tymi z jego paczki - nie
# czytaniem dokumentacji Pandoca.
mkdir -p /mnt/c/tmp/kn543
printf 'Akapit.\r\n\r\n<!-- UWAGA-ROBOCZA-HTML -->\r\n\r\n{>> UWAGA-CRITIC <<}\r\n\r\nKoniec.\r\n' \
    > /mnt/c/tmp/kn543/p.md
cp Convert/2htm/2htm.exe /mnt/c/tmp/kn543/ 2>/dev/null
cp Convert/Pandoc/pandoc.exe /mnt/c/tmp/kn543/ 2>/dev/null
/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\tmp\kn543 && 2htm.exe p.md && pandoc.exe -f markdown -t docx p.md -o p.docx && pandoc.exe -f markdown -t plain p.md -o p.txt" < /dev/null > /dev/null 2>&1
sleep 2

# Komentarz HTML: w podgladzie MUSI byc komentarzem (nie widocznym tekstem).
if [ -f /mnt/c/tmp/kn543/p.htm ]; then
    if grep -q '<!-- UWAGA-ROBOCZA-HTML -->' /mnt/c/tmp/kn543/p.htm; then
        ok "2htm: komentarz HTML zostaje KOMENTARZEM strony, nie widocznym tekstem"
    else bad "2htm: komentarz HTML nie wyszedl jako komentarz"; fi
    # KONTROLA ROZNICUJACA: CriticMarkup w tym samym pliku wychodzi jako
    # WIDOCZNY TEKST.  To rozstrzyga wybor skladni pomiarem, a nie gustem.
    if grep -q 'UWAGA-CRITIC' /mnt/c/tmp/kn543/p.htm; then
        ok "KONTROLA ROZNICUJACA: CriticMarkup wychodzi jako WIDOCZNY tekst (dlatego odrzucony)"
    else bad "KONTROLA ROZNICUJACA PADLA: CriticMarkup tez zniknal, wiec pomiar nic nie rozstrzyga"; fi
else bad "2htm: nie powstal plik wyjsciowy - nie zmierzono niczego"; fi

# Word: tresci komentarza HTML w dokumencie byc NIE MOZE.
if [ -f /mnt/c/tmp/kn543/p.docx ]; then
    if python3 - <<'PYEOF'
import zipfile,sys
x=zipfile.ZipFile('/mnt/c/tmp/kn543/p.docx').read('word/document.xml').decode('utf-8')
sys.exit(0 if 'UWAGA-ROBOCZA-HTML' not in x else 1)
PYEOF
    then ok "Word: tresc komentarza HTML NIE WCHODZI do dokumentu"
    else bad "Word: tresc komentarza HTML WYLADOWALA w dokumencie"; fi
    # KONTROLA ROZNICUJACA na tym samym pliku docx.
    if python3 - <<'PYEOF'
import zipfile,sys
x=zipfile.ZipFile('/mnt/c/tmp/kn543/p.docx').read('word/document.xml').decode('utf-8')
sys.exit(0 if 'UWAGA-CRITIC' in x else 1)
PYEOF
    then ok "KONTROLA ROZNICUJACA: CriticMarkup WCHODZI do Worda jako tekst (dlatego odrzucony)"
    else bad "KONTROLA ROZNICUJACA PADLA w docx"; fi
else bad "Word: nie powstal docx - nie zmierzono niczego"; fi

# Tekst (ta droga zasila eksport brajlowski).
if [ -f /mnt/c/tmp/kn543/p.txt ]; then
    if ! grep -q 'UWAGA-ROBOCZA-HTML' /mnt/c/tmp/kn543/p.txt; then
        ok "Tekst: komentarz HTML wylatuje tez z wyjscia tekstowego"
    else bad "Tekst: komentarz HTML zostal w tekscie"; fi
else bad "Tekst: nie powstal plik txt"; fi

# ---------- CZESC 3: NOWY kod - sonda na binarce ----------
if [ -f testy/pomiar_komentarzy.cs ]; then
    mkdir -p /mnt/c/tmp/kn543b
    cp testy/pomiar_komentarzy.cs /mnt/c/tmp/kn543b/k.cs
    cp EdSharpNG.exe EdSharp.dll Tektosyne.dll /mnt/c/tmp/kn543b/ 2>/dev/null
    (cd /mnt/c/tmp/kn543b && "$CSC" /nologo /t:exe /platform:x64 /out:k.exe k.cs > /dev/null 2>&1)
    sleep 3
    KOM=$(/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\tmp\kn543b && k.exe EdSharpNG.exe" < /dev/null 2>&1 | tr -d '\r' | tail -1)
    echo "   nowy kod: $KOM"
    case "$KOM" in
      "WYNIK: 43/43") ok "NOWY KOD: sonda komentarzy zielona (43/43)" ;;
      *) bad "NOWY KOD: sonda komentarzy nie jest zielona ($KOM)" ;;
    esac
    # KONTROLA: ta sama sonda na STAREJ binarce MUSI PADAC.  Bez tego 43/43 nie
    # dowodzi, ze mierzy cokolwiek nowego.  Uzywamy binarki z paczki 5.0.42,
    # jesli lezy w dist - budowanie starego zrodla od nowa nadpisalo by binarke
    # w repo, co przy 5.0.38 realnie sie zdarzylo.
    if [ -f /tmp/EdSharpNG_stara_kn543.exe ]; then
        cp /tmp/EdSharpNG_stara_kn543.exe /mnt/c/tmp/kn543b/stara.exe
        STARY=$(/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\tmp\kn543b && k.exe stara.exe" < /dev/null 2>&1 | tr -d '\r' | tail -1)
        echo "   stara binarka: $STARY"
        case "$STARY" in
          "WYNIK: 43/43") bad "KONTROLA PADLA: stara binarka tez daje 43/43 - sonda nic nie mierzy" ;;
          *) ok "KONTROLA: stara binarka NIE przechodzi sondy ($STARY)" ;;
        esac
    else
        echo "   (pominieto porownanie ze stara binarka: brak /tmp/EdSharpNG_stara_kn543.exe)"
    fi
fi

# ---------- BRAMKA: w repo MUSI zostac NOWA binarka ----------
# Rozstrzygamy po SUMIE, nie po kodzie wyjscia cmd.exe: interop WSL potrafi
# zwrocic 0 przy buildzie, ktory sie nie odpalil (zmierzone przy 5.0.38).
KONCOWA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
if [ "$KONCOWA" = "$NOWA_SUMA" ]; then
    ok "BRAMKA: w repo lezy NOWA binarka (${KONCOWA:0:24})"
else
    bad "BRAMKA: binarka w repo ZMIENILA SIE - przywracam"
    cp /tmp/EdSharpNG_nowa_kn543.exe EdSharpNG.exe
    echo "   NIE WYSYLAJ paczki bez powtorzenia buildu"
fi

echo
echo "WYNIK KONTROLI NEGATYWNEJ: $PASS/$((PASS+FAIL))"
[ "$FAIL" -eq 0 ] || exit 1
