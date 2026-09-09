#!/usr/bin/env bash
# Kontrola negatywna 5.0.53: LISTA LINKOW pod Control+F6 (ustalenia Kasperczaka
# edsharpng-37, edsharpng-38 i edsharpng-47).
#
# PO CO TA KONTROLA: "widze liste linkow" nie dowodzi niczego, dopoki ta sama
# sonda nie ZOBACZY, ze w kodzie sprzed zmiany jej NIE BYLO. Rewizja odniesienia
# jest podana JAWNIE - skrypt biorący HEAD po commicie porownywalby kod z samym
# soba i zawsze bylby zielony.
#
# Uzycie: testy/kontrola_negatywna_553.sh [rewizja_odniesienia]
set -uo pipefail
# UWAGA na pipefail: `grep ... | grep -q ...` zwraca blad, bo grep -q zamyka
# strumien i pierwszy grep dostaje SIGPIPE (zmierzone 31.08.2026). Kazdy taki
# lancuch idzie przez plik tymczasowy, nie przez potok.

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
ODNIESIENIE="${1:-e07ede4}"   # 5.0.52, stan PRZED napisaniem listy linkow
PRZED="/tmp/kn553_przed_EdSharp.cs"
PRZED_LBC="/tmp/kn553_przed_Lbc.cs"
PRZED_MD="/tmp/kn553_przed_EdSharp.md"
PRZED_INI="/tmp/kn553_przed_Hotkeys.ini"
OK=0
ZLE=0

for para in "EdSharp.cs:$PRZED" "Lbc.cs:$PRZED_LBC" "EdSharp.md:$PRZED_MD" "Hotkeys.ini:$PRZED_INI"; do
    plik="${para%%:*}"
    cel="${para##*:}"
    git show "${ODNIESIENIE}:${plik}" > "$cel" 2>/dev/null || {
        echo "BLAD: nie moge pobrac $plik z rewizji $ODNIESIENIE" >&2
        exit 2
    }
done

zdaj()  { OK=$((OK+1));  echo "OK   $1"; }
oblej() { ZLE=$((ZLE+1)); echo "ZLE  $1"; }

# jest_teraz WZORZEC OPIS [PLIK] - wzorzec MUSI byc w dzisiejszym kodzie
jest_teraz() {
    local plik="${3:-EdSharp.cs}"
    if grep -qF -- "$1" "$plik"; then zdaj "$2"; else oblej "$2 (brak w HEAD: $1)"; fi
}
# nie_bylo WZORZEC OPIS [PLIK_PRZED] - wzorca NIE MOZE byc w kodzie sprzed zmiany
nie_bylo() {
    local plik="${3:-$PRZED}"
    if grep -qF -- "$1" "$plik"; then oblej "$2 (bylo juz w $ODNIESIENIE: $1)"; else zdaj "$2"; fi
}
# bylo WZORZEC OPIS [PLIK_PRZED] - wzorzec MUSIAL byc przed zmiana (sonda widzi)
bylo() {
    local plik="${3:-$PRZED}"
    if grep -qF -- "$1" "$plik"; then zdaj "$2"; else oblej "$2 (sonda gluche: nie widzi $1 w $ODNIESIENIE)"; fi
}
# nie_ma WZORZEC OPIS [PLIK] - wzorca nie moze byc dzis
nie_ma() {
    local plik="${3:-EdSharp.cs}"
    if grep -qF -- "$1" "$plik"; then oblej "$2 (jest w HEAD: $1)"; else zdaj "$2"; fi
}

echo "== Rewizja odniesienia: $ODNIESIENIE =="
echo
echo "-- 1. KOMENDA na CZTERECH warstwach kodu --"
jest_teraz "menuNavigateLinkList = CreateMenuItem(\"Link List ...\", \"Control+F6\", menuItem_Click, \"child silent\");" "pozycja menu z chordem Control+F6"
# ZAWEZONE W 5.0.62.  Obie asercje pilnowaly doslownego SASIEDZTWA pozycji na
# koncu listy pol i na koncu AddRange, a nie tego, ze pozycja tam JEST.  Kazde
# dopisanie nowej komendy na koncu listy (w 5.0.62: Next Link i Prior Link)
# lamalo je przy POPRAWNYM kodzie, zaciemniajac prawdziwe regresje - ta sama
# pulapka, co przestarzale asercje Control+F6 opisane nizej.  Mierzymy teraz
# OBECNOSC pozycji w obu warstwach, bo to jest rzecz, ktora ma znaczenie.
jest_teraz "menuNavigatePriorList, menuNavigateLinkList" "pole klasy zadeklarowane"
jest_teraz "menuNavigateNextList, menuNavigatePriorList, menuNavigateLinkList," "pozycja WSTAWIONA do menu przez AddRange"
jest_teraz "if (menuItem == menuNavigateLinkList) {" "handler w menuItem_Click"
jest_teraz "ShowMarkdownLinkList(rtb);" "handler WOLA metode listy"

echo
echo "-- 2. Tych czterech warstw NIE BYLO w $ODNIESIENIE (kontrola roznicujaca) --"
nie_bylo "menuNavigateLinkList" "zadnej wzmianki o komendzie w kodzie sprzed zmiany"
nie_bylo "ShowMarkdownLinkList" "metody listy nie bylo"
nie_bylo "GetMarkdownLinks" "parsera listy nie bylo"
nie_bylo "class MarkdownLink {" "modelu danych nie bylo"

echo
echo "-- 3. SONDA WIDZI (kontrola pozytywna: te wzorce byly juz wczesniej) --"
bylo "menuNavigateNextList = CreateMenuItem(\"Next List\"" "skok po listach z 5.0.49 byl juz w kodzie"
bylo "private bool HandleMdiWindowCycleKey(Keys keyData) {" "blokada cyklu okien MDI z 5.0.52 byla juz w kodzie"
bylo "private void InsertMarkdownLink(HomerRichTextBox rtb) {" "wstawianie linku z 5.0.45 bylo juz w kodzie"

echo
echo "-- 4. TRZY KLAWISZE NA LISCIE (ustalenie edsharpng-38) --"
jest_teraz "if (ev.KeyData == (Keys.Control | Keys.C)) {" "Control+C obsluzony"
jest_teraz "AddMessage(\"Copied as Markdown\");" "Control+C melduje kopiowanie jako Markdown"
jest_teraz "else if (ev.KeyData == (Keys.Control | Keys.Shift | Keys.C)) {" "Control+Shift+C obsluzony"
jest_teraz "AddMessage(\"Copied as formatted link\");" "Control+Shift+C melduje link z formatowaniem"
jest_teraz "else if (ev.KeyData == Keys.F2) {" "F2 obsluzony"
jest_teraz "private bool EditMarkdownLink(HomerRichTextBox rtb, MarkdownLink link) {" "F2 ma metode edycji"
jest_teraz "LbcDialog dlg = new LbcDialog(\"Edit Link\", this);" "okienko edycji z tytulem Edit Link"
jest_teraz "TextBox txtLabel = dlg.addInputBox(\"Link &text\", link.Title," "pole tytulu w okienku edycji"
jest_teraz "TextBox txtAddress = dlg.addInputBox(\"&Address\", link.Url, \"\");" "pole adresu w okienku edycji"

echo
echo "-- 5. Lbc ODDAJE nam Control+C i Control+Shift+C na tej liscie --"
# Bez tego okienko Lbc skopiowaloby WIDOCZNY wiersz ("Line 12. tresc, adres,
# link") zamiast samego odsylacza - i zielony build by tego nie pokazal.
jest_teraz "lst.Tag = \"edsharp-linklist\";" "lista oznaczona znacznikiem" "EdSharp.cs"
jest_teraz "(string)lb.Tag == \"edsharp-linklist\"" "Lbc rozpoznaje znacznik" "Lbc.cs"
jest_teraz "|| evArgs.KeyData == (Keys.Control | Keys.Shift | Keys.C))) return;" "Lbc oddaje OBA chordy kopiowania" "Lbc.cs"
nie_bylo "edsharp-linklist" "znacznika NIE bylo w Lbc sprzed zmiany" "$PRZED_LBC"
bylo "(string)lb.Tag == \"edsharp-filelist\"" "sonda widzi znacznik list plikow, ktory byl wczesniej" "$PRZED_LBC"

echo
echo "-- 6. KURSOR NA TRESCI, nie na nawiasie (wymog niewidomego) --"
jest_teraz "int iAt = pick.TextStart;" "skok idzie na TextStart, nie na Start"
nie_ma "int iAt = pick.Start;" "kursor NIE staje na nawiasie otwierajacym"

echo
echo "-- 7. BRAMKI: podglad TAK, plik Markdown NIE (swiadoma roznica) --"
jest_teraz "AddMessage(\"Close the preview first!\");" "bramka podgladu istnieje"
jest_teraz "// CZYTA i goly adres w pliku .txt jest prawdziwym odsylaczem." "powod braku bramki .md zapisany w kodzie"
# Kontrola, ze NIE dolozylem bramki .md do tej komendy: badamy CIALO handlera.
# UWAGA na CRLF: wzorzec konca `/^\}$/` NIE trafia w tym repo, bo kazdy wiersz
# konczy sie znakiem CR, wiec zakres awk lecialby do konca pliku i lapal bramke
# z zupelnie innej komendy. Zmierzone - pierwsza wersja tej sondy tak oblala.
awk '/if \(menuItem == menuNavigateLinkList\) \{/,/^\}\r?$/' EdSharp.cs > /tmp/kn553_handler.txt
LICZ_H=$(wc -l < /tmp/kn553_handler.txt)
if [[ "$LICZ_H" -gt 40 ]]; then
    oblej "SONDA NIEWAZNA: cialo handlera ma $LICZ_H wierszy, czyli zakres sie nie domknal"
else
    zdaj "zakres ciala handlera domkniety ($LICZ_H wierszy)"
fi
if grep -qF "only on Markdown files" /tmp/kn553_handler.txt; then
    oblej "handler NIE MA bramki tylko-Markdown (jest, a nie powinno byc)"
else
    zdaj "handler NIE MA bramki tylko-Markdown - lista dziala tez w .txt"
fi
# Kontrola waznosci tej sondy: taka bramka JEST przy wstawianiu linku.
awk '/if \(menuItem == menuMiscInsertLink\) \{/,/^\}\r?$/' EdSharp.cs > /tmp/kn553_handler2.txt
if grep -qF "Links work only on Markdown files!" /tmp/kn553_handler2.txt; then
    zdaj "KONTROLA WAZNOSCI: ta sama sonda WIDZI bramke przy wstawianiu linku"
else
    oblej "KONTROLA WAZNOSCI PADLA: sonda nie widzi bramki tam, gdzie ona jest"
fi

echo
echo "-- 8. BLOKI KODU pomijane tym samym parserem, co reszta --"
jest_teraz "if (MarkdownReview_IsFenceLine(sLine)) bInFence = !bInFence;" "parser listy uzywa TEGO SAMEGO wykrywacza ogrodzen"
jest_teraz "List<int[]> inlineLinks = MarkdownReview_FindInlineLinks(sText);" "postac nawiasowa z TEGO SAMEGO pomiaru, co podglad"

echo
echo "-- 9. OPISY MOWIONE (Ctrl+F1) w trzech plikach --"
for plik in Hotkeys.ini hotkeys.txt EdSharp.md; do
    jest_teraz "Link List=Control+F6, Show the list of links in the document" "opis mowiony w $plik" "$plik"
done
nie_bylo "Link List=Control+F6" "opisu NIE bylo w Hotkeys.ini sprzed zmiany" "$PRZED_INI"
bylo "Next List=Control+Dash" "sonda widzi opis skoku po listach, ktory byl wczesniej" "$PRZED_INI"

echo
echo "-- 10. PODRECZNIK: skasowane zdanie, ze listy NIE MA --"
nie_ma "HAS NOT BEEN WRITTEN YET" "podrecznik NIE mowi juz, ze listy nie napisano" "EdSharp.md"
bylo "HAS NOT BEEN WRITTEN YET" "sonda widzi to zdanie w podreczniku sprzed zmiany" "$PRZED_MD"
jest_teraz "Control+F6 now opens the Link List" "podrecznik mowi, ze klawisz otwiera liste" "EdSharp.md"
jest_teraz "Control+F6 opens the Link List: every link of the document" "proza o liscie linkow w rozdziale o linkach" "EdSharp.md"

echo
echo "-- 11. NIC INNEGO NIE ZNIKNELO (kontrole na rzeczach z poprzednich wersji) --"
jest_teraz "menuNavigateDocumentNavigation = CreateMenuItem(\"Document Navigation ...\", \"F6\"" "F6 nadal drzewo naglowkow"
jest_teraz "menuNavigateGoToContents = CreateMenuItem(\"Go to Contents\", \"Shift+F6\"" "Shift+F6 nadal spis tresci"
jest_teraz "menuMiscInsertLink = CreateMenuItem(\"Insert &Link ...\", \"Control+K\"" "Control+K nadal wstawianie linku"
jest_teraz "menuMiscFootnoteList = CreateMenuItem(\"Footnote List ...\", \"Alt+K\"" "Alt+K nadal lista przypisow"
jest_teraz "menuMiscCommentList = CreateMenuItem(\"Comment List ...\", \"Control+Alt+F9\"" "Control+Alt+F9 nadal lista komentarzy"
jest_teraz "menuNavigateNextEmphasis = CreateMenuItem(\"Next Emphasis\", \"Control+OemQuestion\"" "skok po wyroznieniach nietkniety"
jest_teraz "private void RunMarkdownTableWizard(HomerRichTextBox rtb) {" "kreator tabeli nietkniety"
jest_teraz "private void ShowMarkdownFootnoteList(HomerRichTextBox rtb) {" "lista przypisow nietknieta"
jest_teraz "private void ShowMarkdownCommentList(HomerRichTextBox rtb) {" "lista komentarzy nietknieta"
jest_teraz "if (HandleMdiWindowCycleKey(keyData)) return true;" "blokada cyklu okien MDI nadal wywolana"
# Blokada MUSI ustepowac naszej komendzie - inaczej lista by sie nie otworzyla.
jest_teraz "if (hashKey.ContainsKey(keyData)) return false;" "blokada MDI USTEPUJE komendzie z tablicy skrotow"
jest_teraz "public static string PickFile(string sTitle, string[] aValue, string[] aDisplay, bool bSort, int iIndex, string sSection) {" "lista plikow nietknieta"
jest_teraz "public static string PickBookmark(string sTitle, string[] aValue, string[] aDisplay, int iIndex, string sFile) {" "lista zakladek nietknieta"

echo
echo "-- 12. RODZINA Control+F6: chord zajety RAZ, przez nas --"
LICZ=$(grep -c "\"Control+F6\"" EdSharp.cs)
if [[ "$LICZ" == "1" ]]; then zdaj "chord Control+F6 wystepuje DOKLADNIE raz (jest $LICZ)"; else oblej "chord Control+F6 wystepuje $LICZ razy, ma byc 1"; fi
LICZS=$(grep -c "\"Control+Shift+F6\"" EdSharp.cs)
if [[ "$LICZS" == "0" ]]; then zdaj "Control+Shift+F6 nadal WOLNY (jest $LICZS przypisan)"; else oblej "Control+Shift+F6 komus przypisany ($LICZS)"; fi

echo
echo "======================================"
echo "RAZEM: $OK OK / $ZLE ZLE"
[[ "$ZLE" == "0" ]] && exit 0 || exit 1
