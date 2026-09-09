#!/usr/bin/env bash
# Kontrola negatywna 5.0.58: wyniki testow Kasperczaka z 5.0.56/5.0.57 plus
# rodzina Control (zlecenia 1788278589371-3, 1788278778162-4, 1788279917207-5).
#
# PO CO: "widze zmiane" nie dowodzi niczego, dopoki ta sama sonda nie ZOBACZY,
# ze w kodzie sprzed zmiany bylo INACZEJ.  Rewizja odniesienia jest JAWNA -
# skrypt biorący HEAD po commicie porownywalby kod z samym soba.
#
# Uzycie: testy/kontrola_negatywna_558.sh [rewizja_odniesienia]
set -uo pipefail
# UWAGA na pipefail: `grep ... | grep -q ...` zwraca blad przez SIGPIPE.
# Kazdy taki lancuch idzie przez plik tymczasowy, nie przez potok.

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
ODNIESIENIE="${1:-4f06c08}"   # 5.0.57, stan PRZED tymi zmianami
PRZED="/tmp/kn558_przed_EdSharp.cs"
PRZED_MD="/tmp/kn558_przed_EdSharp.md"
PRZED_INI="/tmp/kn558_przed_Hotkeys.ini"
PRZED_TXT="/tmp/kn558_przed_hotkeys.txt"
OK=0
ZLE=0

for para in "EdSharp.cs:$PRZED" "EdSharp.md:$PRZED_MD" "Hotkeys.ini:$PRZED_INI" "hotkeys.txt:$PRZED_TXT"; do
    plik="${para%%:*}"
    cel="${para##*:}"
    git show "${ODNIESIENIE}:${plik}" > "$cel" 2>/dev/null || {
        echo "BLAD: nie moge pobrac $plik z rewizji $ODNIESIENIE" >&2
        exit 2
    }
done

zdaj()  { OK=$((OK+1));  echo "OK   $1"; }
oblej() { ZLE=$((ZLE+1)); echo "ZLE  $1"; }

jest_teraz() {
    local plik="${3:-EdSharp.cs}"
    if grep -qF -- "$1" "$plik"; then zdaj "$2"; else oblej "$2 (brak w HEAD: $1)"; fi
}
nie_ma_teraz() {
    local plik="${3:-EdSharp.cs}"
    if grep -qF -- "$1" "$plik"; then oblej "$2 (nadal jest w HEAD: $1)"; else zdaj "$2"; fi
}
nie_bylo() {
    local plik="${3:-$PRZED}"
    if grep -qF -- "$1" "$plik"; then oblej "$2 (bylo juz w $ODNIESIENIE: $1)"; else zdaj "$2"; fi
}
bylo() {
    local plik="${3:-$PRZED}"
    if grep -qF -- "$1" "$plik"; then zdaj "$2"; else oblej "$2 (sonda glucha: nie widzi $1 w $ODNIESIENIE)"; fi
}
ile() {
    local plik="${4:-EdSharp.cs}"
    local n
    n=$(grep -cF -- "$1" "$plik" || true)
    if [ "$n" = "$3" ]; then zdaj "$2 (=$3)"; else oblej "$2 (jest $n, ma byc $3)"; fi
}

echo "== Rewizja odniesienia: $ODNIESIENIE =="
echo
echo "-- 1. SKOK PO PRZYPISACH: NOWA PARA KLAWISZY, STARY CHORD ZWOLNIONY --"
jest_teraz 'menuMiscNextFootnote = CreateMenuItem("Next Footnote", "Control+Alt+PageDown"' "Next Footnote na Control+Alt+PageDown"
nie_bylo   'menuMiscNextFootnote' "komendy Next Footnote nie bylo w odniesieniu"
jest_teraz 'menuMiscPriorFootnote = CreateMenuItem("Prior Footnote", "Control+Alt+PageUp"' "Prior Footnote na Control+Alt+PageUp"
bylo       'menuMiscPriorFootnote = CreateMenuItem("Prior Footnote", "Control+Alt+Shift+K"' "w odniesieniu Prior Footnote byl na Control+Alt+Shift+K"
# CHORD, NIE SLOWO: "Control+Alt+Shift+K" wystepuje nadal w KOMENTARZACH, ktore
# opisuja, skad ten skok zszedl - i tak ma byc.  Pytamy o chord w POZYCJI
# ARGUMENTU CreateMenuItem, bo tylko to znaczy "przypisany do komendy".
# Bez tego zawezenia sonda dawala falszywy alarm przy poprawnym kodzie.
n=$(grep -oP 'CreateMenuItem\("[^"]*", *"Control\+Alt\+Shift\+K"' EdSharp.cs | wc -l)
if [ "$n" = "0" ]; then zdaj "chord Control+Alt+Shift+K nie nalezy do zadnej komendy"; else oblej "chord Control+Alt+Shift+K nadal przypisany ($n)"; fi
# KONTROLA POZYTYWNA: ten sam wzorzec musi WIDZIEC chord, ktory tam jest.
n=$(grep -oP 'CreateMenuItem\("[^"]*", *"Control\+Alt\+K"' EdSharp.cs | wc -l)
if [ "$n" = "1" ]; then zdaj "KONTROLA WZORCA: Control+Alt+K widziany jako przypisany"; else oblej "KONTROLA WZORCA zawiodla (Control+Alt+K: $n)"; fi
# WSZYSTKIE WARSTWY, nie tylko CreateMenuItem: pole klasy, AddRange, handler.
# Osierocone pole albo pozycja poza AddRange to lekcja z 5.0.44.
# SZESC WARSTW, wyliczonych PO KOLEI - liczba sama byla by slepa na to, ktorej
# warstwy brakuje.  Deklaracja pola, CreateMenuItem, AddRange, warunek rodziny,
# bramka mowy globalnej, handler.
ile 'menuMiscNextFootnote' "menuMiscNextFootnote wystepuje w szesciu warstwach" 6
jest_teraz 'else if (menuItem == menuMiscNextFootnote) GoToMarkdownFootnoteMarker(rtb, true);' "handler Next Footnote wola skok w przod"
jest_teraz 'menuItem == menuMiscNextFootnote || menuItem == menuMiscPriorFootnote);' "Next Footnote objety trybem globalnym mowy"
jest_teraz 'menuItem == menuMiscNextFootnote || menuItem == menuMiscPriorFootnote || menuItem == menuMiscFootnoteList' "Next Footnote objety bramkami rodziny przypisow"

echo
echo "-- 2. Control+Alt+K PRZESTAL SKAKAC PO PRZYPISACH --"
jest_teraz 'Put the cursor in a line with a footnote marker, or use Control+Alt+PageDown to find one!' "poza przypisem komunikat nazywa DRUGI klawisz"
nie_bylo   'Put the cursor in a line with a footnote marker, or use Control+Alt+PageDown' "tego komunikatu nie bylo w odniesieniu"
bylo       'int iNext = FindMarkdownFootnoteMarkerIndex(refs, iCursor, true);' "w odniesieniu Control+Alt+K szedl do nastepnego znacznika"
# Sciezka "idz do nastepnego" ma zniknac TYLKO z Control+Alt+K.  Helper
# FindMarkdownFootnoteMarkerIndex ZOSTAJE, bo z niego zyje nowa para klawiszy -
# gdyby zniknal, nowe komendy bylyby martwe przy zielonym buildzie.
jest_teraz 'FindMarkdownFootnoteMarkerIndex' "helper wyboru znacznika ZOSTAJE (uzywa go nowa para)"
jest_teraz 'private void GoToMarkdownFootnoteMarker(HomerRichTextBox rtb, bool bForward) {' "metoda skoku po znacznikach zostaje"

echo
echo "-- 3. LISTA LINKOW: TRESC PRZED WSPOLRZEDNA --"
# ASERCJA PRZESTARZALA JEGO DECYZJA Z 03.09.2026 (edsharpng-107: wiersz listy
# ma niesc SAM TYTUL, adres dopowiada strzalka w lewo).  INTENCJA ZOSTAJE
# NIETKNIETA: tresc odsylacza stoi PRZED wspolrzedna, czyli czytnik mowi
# najpierw to, po czym uzytkownik wybiera.  Zmienil sie NOSNIC - wiersz liczy
# teraz GetMarkdownLinkListLine (sam tytul) zamiast GetMarkdownLinkSpeech
# (tytul z adresem).  Kontrola waznosci: na rewizji a78a631 ta asercja oblewa,
# bo tam kolejnosc byla odwrotna.
jest_teraz 'lsShow.Add(GetMarkdownLinkListLine(link) + ", line " + iLine);' "wiersz listy linkow: tresc, potem wiersz"
jest_teraz 'GetMarkdownLinkListLine' "wiersz listy niesie SAM TYTUL, bez adresu (edsharpng-107)"
bylo       'lsShow.Add("Line " + iLine + ". " + GetMarkdownLinkSpeech(link));' "w odniesieniu wiersz zaczynal sie od slowa Line"
nie_ma_teraz 'lsShow.Add("Line " + iLine' "stary format wiersza listy linkow zniknal"

echo
echo "-- 4. LISTY ZAKLADEK: PASEK STANU CICHY, KLAWISZE POD F1 --"
nie_ma_teraz 'addListBox(lDisp, "", "Delete removes the bookmark")' "podpowiedz zeszla z paska stanu listy zakladek z nazwa"
nie_ma_teraz 'addListBox(lDisp, "", "Delete or Backspace removes the bookmark")' "podpowiedz zeszla z paska stanu listy zakladek zwyklych"
bylo       'addListBox(lDisp, "", "Delete removes the bookmark")' "w odniesieniu podpowiedz BYLA w pasku stanu"
jest_teraz 'setHelpDetail(lst, "Keys: Delete or Backspace removes the bookmark' "klawisze zakladek zwyklych sa w pomocy F1"
jest_teraz 'setHelpDetail(lst, "Keys: Delete or Backspace removes the named bookmark' "klawisze zakladek z nazwa sa w pomocy F1"
# KLAWISZ MUSI DZIALAC DALEJ: zdjecie podpowiedzi nie moze zdjac obslugi.
jest_teraz 'App.Frame.AddMessage("Bookmark removed");' "Delete na liscie zakladek nadal usuwa"
jest_teraz 'App.Frame.AddMessage("Named bookmark removed");' "Delete na liscie zakladek z nazwa nadal usuwa"

echo
echo "-- 5. STRZALKA W LEWO NA TRZECH LISTACH --"
jest_teraz 'Say.sayForced("Line " + (rtbHere.GetLineFromCharIndex(iCharAt) + 1));' "lista zakladek zwyklych: strzalka w lewo mowi numer wiersza"
jest_teraz 'setHelpDetail(lstFoot, "Keys: Left Arrow reads the sentence line' "lista przypisow: strzalka w lewo udokumentowana"
jest_teraz 'LbcDialog dlgFoot = new LbcDialog("Footnotes", this);' "lista przypisow ma wlasne okno (Dialog.Pick nie przyjmuje klawiszy)"
bylo       'string sPicked = Dialog.Pick("Footnotes", lsLabel.ToArray(), lsShow.ToArray(), false, 0);' "w odniesieniu lista przypisow szla przez Dialog.Pick"
nie_ma_teraz 'Dialog.Pick("Footnotes"' "lista przypisow nie idzie juz przez Dialog.Pick"
# Wybor z listy MUSI dalej dzialac: samo okno bez odczytu wyboru byloby
# regresja, ktora zielony build przepuszcza.
jest_teraz 'bool bFootOk = dlgFoot.runOkCancel();' "lista przypisow czyta wybor uzytkownika"
jest_teraz 'string sPicked = aFootValues[iFootPicked];' "wybor z listy przypisow trafia do dawnej sciezki"

echo
echo "-- 6. RODZINA CONTROL: CHORDY ZWOLNIONE, KOMENDY ZOSTAJA --"
# DWIE KOMENDY WYSZLY Z TEJ LISTY 03.09.2026 NA JEGO DECYZJE, i to nie jest
# ignorowanie asercji, a zmiana pytania na zgodne z uzgodnionym stanem:
#   - Extra Speech Toggle (edsharpng-104, "Tak.  Usunac.  To bylo glownie pod
#     Jaws") - komenda USUNIETA calkiem, nie zostala menu-only.
#   - Extract with Regular Expression (edsharpng-105) - POLACZONA z Yield w
#     jedna komende Regular Expression Tool, wiec pod ta nazwa jej nie ma.
# Oba przypadki maja WLASNE asercje w sekcji 6b, ktora sprawdza, ze komendy
# NAPRAWDE zniknely, a nie ze "sonda przestala patrzec".
for para in "Environment Variables:Control+E" \
            "Go to Environment:Control+Shift+G" \
            "Repeat Line:Control+Y"; do
    nazwa="${para%%:*}"
    chord="${para##*:}"
    # AMPERSAND W NAZWIE MENU: pozycja to "&Environment Variables ...", wiec
    # pytanie o "Environment Variables" nie mialo jak trafic - falszywe zero,
    # ta sama pulapka co w weryfikatorze listy testow 01.09.2026.  Szukamy wiec
    # nazwy Z DOWOLNYM ampersandem miedzy literami: wzorzec z opcjonalnym "&"
    # przed kazdym znakiem.
    wzor=$(printf '%s' "$nazwa" | sed 's/./\&?&/g')
    # Wzorzec bierze CALA linie CreateMenuItem z pustym chordem - inaczej
    # "menu-only" nie da sie odroznic od komendy z innym skrotem.
    if grep -qP "CreateMenuItem\(\"$wzor" EdSharp.cs; then
        if grep -P "CreateMenuItem\(\"$wzor" EdSharp.cs > /tmp/kn558_linia.txt 2>/dev/null && grep -qF '", "", menuItem_Click' /tmp/kn558_linia.txt; then
            zdaj "$nazwa jest komenda menu-only (pusty chord)"
        else
            oblej "$nazwa nie ma pustego chordu: $(cat /tmp/kn558_linia.txt)"
        fi
    else
        oblej "$nazwa ZNIKNELA z menu (mialo zostac, zwalniamy tylko chord)"
    fi
    # W odniesieniu ta komenda MIALA ten chord - dowod, ze sonda nie jest glucha.
    if grep -qP "CreateMenuItem\(\"$wzor" "$PRZED"; then zdaj "w odniesieniu komenda $nazwa istniala"; else oblej "sonda glucha: nie widzi komendy $nazwa w $ODNIESIENIE"; fi
done
# Chordy zwolnione: zaden nie moze wystepowac jako PELNY chord w CreateMenuItem.
for chord in "Control+E" "Control+Shift+E" "Control+Shift+G" "Control+Shift+X" "Control+Y"; do
    n=$(grep -oP "CreateMenuItem\(\"[^\"]*\", *\"\Q$chord\E\"" EdSharp.cs | wc -l)
    if [ "$n" = "0" ]; then zdaj "chord $chord nie nalezy do zadnej komendy"; else oblej "chord $chord nadal przypisany ($n)"; fi
done
# KONTROLA POZYTYWNA TEGO WZORCA: chord, ktory JEST przypisany, musi byc widziany.
n=$(grep -oP 'CreateMenuItem\("[^"]*", *"Control\+Shift\+Z"' EdSharp.cs | wc -l)
if [ "$n" = "1" ]; then zdaj "KONTROLA WZORCA: Control+Shift+Z widziany jako przypisany"; else oblej "KONTROLA WZORCA zawiodla (Control+Shift+Z: $n)"; fi

echo
echo "-- 6b. DWIE KOMENDY USUNIETE JEGO DECYZJA 03.09.2026 --"
# Ta sekcja istnieje, zeby wyjscie dwoch komend z petli w sekcji 6 NIE bylo
# zamieceniem asercji pod dywan.  Tam pytalismy "czy komenda zostala jako
# menu-only"; tu pytamy "czy naprawde zniknela", co jest mocniejszym
# twierdzeniem i tak samo sprawdzalnym.
if grep -qF 'menuMiscExtraSpeechToggle' EdSharp.cs; then
    oblej "Extra Speech Toggle NADAL jest w kodzie (edsharpng-104 kazal usunac)"
else
    zdaj "Extra Speech Toggle usuniety z kodu calkiem (edsharpng-104)"
fi
# WARUNEK BEZPIECZENSTWA TEGO USUNIECIA: ustawienie zapisuje sie do pliku, wiec
# bez czyszczenia klucza kto ma mowe wylaczona, zostalby z cisza BEZ wlacznika.
if grep -qF 'ClearExtraSpeechOption' EdSharp.cs; then
    zdaj "klucz ExtraSpeech jest czyszczony przy starcie (nikt nie zostaje z cisza)"
else
    oblej "BRAK czyszczenia klucza ExtraSpeech: usuniecie przelacznika moze zostawic cisze na stale"
fi
if grep -qF 'Extract with Regular Expression ...' EdSharp.cs; then
    oblej "Extract with Regular Expression NADAL osobna komenda (edsharpng-105 kazal polaczyc)"
else
    zdaj "Extract with Regular Expression polaczona z Yield (edsharpng-105)"
fi
# POLACZENIE MA MIEC NOSNIK: sama nieobecnosc obu nazw byla by tez skutkiem
# skasowania funkcji, wiec pytamy o komende, ktora je ZASTAPILA.
if grep -qF 'menuMiscRegExpTool = CreateMenuItem("Regular Expression Tool ...", "Control+Shift+Y"' EdSharp.cs; then
    zdaj "narzedzie wyrazen regularnych istnieje i trzyma Control+Shift+Y"
else
    oblej "BRAK komendy Regular Expression Tool na Control+Shift+Y: funkcja nie ma nosnika"
fi
# SONDA NIE JEST GLUCHA: w odniesieniu OBIE usuniete komendy istnialy.
if grep -qF 'menuMiscExtraSpeechToggle' "$PRZED"; then zdaj "w odniesieniu Extra Speech Toggle istnial"; else oblej "sonda glucha: nie widzi Extra Speech Toggle w $ODNIESIENIE"; fi
if grep -qF 'Extract with Regular Expression ...' "$PRZED"; then zdaj "w odniesieniu Extract with RegExp istnial"; else oblej "sonda glucha: nie widzi Extract with RegExp w $ODNIESIENIE"; fi

echo
echo "-- 7. Control+Y PONAWIA --"
jest_teraz 'private bool HandleRedoAliasKey(Keys keyData) {' "metoda drugiego klawisza ponawiania istnieje"
nie_bylo   'HandleRedoAliasKey' "tej metody nie bylo w odniesieniu"
jest_teraz 'if (HandleRedoAliasKey(keyData)) return true;' "metoda jest WOLANA w lancuchu klawiszy"
jest_teraz 'if (keyData != (Keys.Control | Keys.Y)) return false;' "pyta dokladnie o Control+Y"
jest_teraz 'menuEditRedo.PerformClick();' "wola te sama pozycje menu co Control+Shift+Z"
# KOLEJNOSC JEST CZESCIA ZACHOWANIA: alias musi stac PRZED tablica skrotow,
# inaczej hashKey przechwycilby klawisz pierwszy (gdyby kiedys tam wrocil).
linia_alias=$(grep -n 'if (HandleRedoAliasKey(keyData)) return true;' EdSharp.cs | head -1 | cut -d: -f1)
linia_tablica=$(grep -n 'else if (hashKey.TryGetValue(keyData, out menuItem)) {' EdSharp.cs | head -1 | cut -d: -f1)
if [ -n "$linia_alias" ] && [ -n "$linia_tablica" ] && [ "$linia_alias" -lt "$linia_tablica" ]; then
    zdaj "alias Control+Y stoi PRZED tablica skrotow ($linia_alias < $linia_tablica)"
else
    oblej "alias Control+Y nie stoi przed tablica skrotow ($linia_alias vs $linia_tablica)"
fi
# Redo ZOSTAJE na swoim klawiszu (ustalenie edsharpng-72).
jest_teraz 'menuEditRedo = CreateMenuItem("Redo", "Control+Shift+Z"' "Redo nadal na Control+Shift+Z"

echo
echo "-- 8. OPISY MOWIONE: OBA PLIKI CO DO ZNAKU ZGODNE --"
# DWA WPISY WYSZLY Z TEJ LISTY 03.09.2026: opisy Extra Speech Toggle i Extract
# with Regular Expression zeszly RAZEM z komendami (edsharpng-104 i -105).
# Opis mowiony komendy, ktorej nie ma, byl by KLAMSTWEM w podreczniku, wiec
# ich brak to wynik pozytywny.  Sprawdza to sekcja 8b ponizej - inaczej
# usuniecie wpisu z tej petli tuszowaloby zapomniany opis.
for wpis in "Next Footnote=Control+Alt+PageDown" "Prior Footnote=Control+Alt+PageUp" \
            "Repeat Line=, Copy current line below it" "Environment Variables=, Change Windows" \
            "Go to Environment=, Go to interactive"; do
    jest_teraz "$wpis" "Hotkeys.ini: $wpis" "Hotkeys.ini"
    jest_teraz "$wpis" "hotkeys.txt: $wpis" "hotkeys.txt"
    jest_teraz "$wpis" "EdSharp.md: $wpis" "EdSharp.md"
done

echo
echo "-- 8b. OPISY USUNIETYCH KOMEND ZESZLY Z TRZECH PLIKOW --"
# Trzy pliki, dwie nazwy: opis komendy, ktorej nie ma, wprowadza w blad
# niewidomego, ktory szuka funkcji po podreczniku.  Lekcja z 5.0.44: przy
# usuwaniu komendy trzeba przejsc WSZYSTKIE warstwy, a opisy mowione sa
# warstwa, o ktorej najlatwiej zapomniec, bo build o nich nie mowi.
for nazwa in "Extra Speech Toggle=" "Extract with Regular Expression="; do
    for f in Hotkeys.ini hotkeys.txt EdSharp.md; do
        if grep -qF "$nazwa" "$f"; then
            oblej "$f NADAL opisuje usunieta komende: $nazwa"
        else
            zdaj "$f nie opisuje juz usunietej komendy: $nazwa"
        fi
    done
done
# NOWA KOMENDA MA MIEC OPIS we wszystkich trzech plikach - to druga strona tej
# samej zasady: usunieta nie moze zostac, a nowa nie moze byc bez opisu.
for f in Hotkeys.ini hotkeys.txt EdSharp.md; do
    if grep -qF "Regular Expression Tool=" "$f"; then
        zdaj "$f opisuje nowa komende Regular Expression Tool"
    else
        oblej "$f NIE opisuje nowej komendy Regular Expression Tool"
    fi
done
nie_ma_teraz "Prior Footnote=Control+Alt+Shift+K" "Hotkeys.ini nie mowi juz starego chordu" "Hotkeys.ini"
nie_ma_teraz "Prior Footnote=Control+Alt+Shift+K" "hotkeys.txt nie mowi juz starego chordu" "hotkeys.txt"
nie_ma_teraz "Prior Footnote=Control+Alt+Shift+K" "EdSharp.md nie mowi juz starego chordu" "EdSharp.md"
# Oba pliki opisow musza byc zgodne co do znaku - rozjazd zmierzony 01.09.2026
# (hotkeys.txt nie mial wcale opisow Next/Prior Bookmark).
roz=$(diff <(grep -c '=' Hotkeys.ini) <(grep -c '=' hotkeys.txt) >/dev/null && echo rowne || echo rozne)
if [ "$roz" = "rowne" ]; then zdaj "Hotkeys.ini i hotkeys.txt maja tyle samo wpisow"; else oblej "Hotkeys.ini i hotkeys.txt maja ROZNA liczbe wpisow"; fi

echo
echo "-- 9. NIENARUSZALNE --"
# CRLF MIERZYMY NA SAMYM HEAD, nie przez porownanie z git show: git normalizuje
# konce wiersza przy wydawaniu tresci (core.autocrlf), wiec odniesienie ma zero
# znakow CR i porownanie zawsze oblewalo przy poprawnym pliku.  Pytanie brzmi
# wiec: czy KAZDA linia pliku na dysku konczy sie CRLF.
wszystkie=$(wc -l < EdSharp.cs)
zcr=$(grep -c $'\r$' EdSharp.cs || true)
if [ "$wszystkie" = "$zcr" ]; then zdaj "CRLF w KAZDEJ z $wszystkie linii EdSharp.cs"; else oblej "CRLF tylko w $zcr z $wszystkie linii - narzedzie edycji wstawilo LF"; fi
# ZERO SUROWYCH TABULATOROW W LITERALACH W MOICH LINIACH (pulapka patch/write_file
# z 5.0.57: narzedzie edycji zamienia literal z odwrotnym ukosnikiem, np. \tab
# albo \tx720, na REALNY bajt tabulacji).
# ASERCJA MUSI PYTAC O TABULATOR W SRODKU LINII, NIE O WCIECIE WIODACE.
# Poprzednia wersja pytala o '^\+\t', czyli o KAZDA dodana linie zaczynajaca sie
# tabulatorem - a caly rejon metod Markdown jest wciety tabulatorami i plik ma
# ich 4359 od dawna (tyle samo w rewizji odniesienia).  Oblewala wiec przy
# POPRAWNEJ zmianie w tym rejonie: to byla wina asercji, nie kodu.  Zmierzone
# 01.09.2026 przy 5.0.60 (97 dodanych linii komentarza z wcieciem, ZERO
# literalow z tabulacja).  Prawdziwy defekt daje tabulator PO wcieciu wiodacym.
git diff "$ODNIESIENIE" -- EdSharp.cs > /tmp/kn558_diff.txt 2>/dev/null || true
if python3 -c '
import sys
zle = []
for linia in open("/tmp/kn558_diff.txt", encoding="utf-8", errors="replace"):
    if not linia.startswith("+") or linia.startswith("+++"):
        continue
    tresc = linia[1:].rstrip("\n")
    if "\t" in tresc.lstrip("\t"):
        zle.append(tresc)
if zle:
    print("   PRZYKLAD: " + repr(zle[0][:120]))
sys.exit(1 if zle else 0)
'; then
    zdaj "zero surowych tabulatorow W LITERALACH dodanych linii"
else
    oblej "w DODANYCH liniach EdSharp.cs jest surowy tabulator W SRODKU linii (rozwalony literal)"
fi
# KONTROLA POZYTYWNA TEJ ASERCJI: na podstawionej linii z tabulacja w srodku
# musi ona ZAREAGOWAC.  Bez tego zielony wynik nie odroznia poprawnego pliku od
# asercji, ktora nie potrafi trafic.
if python3 -c '
import sys
tresc = "\t\t\tstring s = @\"\\" + "\t" + "ab\";"
sys.exit(0 if "\t" in tresc.lstrip("\t") else 1)
'; then
    zdaj "kontrola pozytywna: asercja widzi tabulator w srodku linii"
else
    oblej "kontrola pozytywna OBLALA - asercja nie potrafi trafic w rozwalony literal"
fi
# KONTROLA POZYTYWNA: sonda musi widziec, ze diff w ogole ma dodane linie.
if grep -qP '^\+[^+]' /tmp/kn558_diff.txt; then zdaj "KONTROLA SONDY: diff wobec $ODNIESIENIE ma dodane linie"; else oblej "KONTROLA SONDY: diff pusty - nie mierze niczego"; fi
jest_teraz 'menuNavigateNextBookmark = CreateMenuItem("Next Bookmark", "Shift+PageDown"' "zakladki nadal na Shift+PageDown"
jest_teraz 'menuNavigateNextSection= CreateMenuItem("Next Section", "Control+PageDown"' "sekcje nadal na Control+PageDown"
jest_teraz 'menuMiscBulletList = CreateMenuItem("Bulleted List", "Control+L"' "rodzina L z 5.0.55 nietknieta"
jest_teraz 'menuNavigateSetNamedBookmark' "zakladki z nazwa z 5.0.56 nietkniete"
jest_teraz 'TryCopyMarkdownSelection' "kopiowanie zaznaczenia z 5.0.57 nietkniete"

echo
echo "== $OK OK / $ZLE ZLE =="
[ "$ZLE" = "0" ] || exit 1
