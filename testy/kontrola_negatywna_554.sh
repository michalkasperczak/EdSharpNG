#!/usr/bin/env bash
# Kontrola negatywna 5.0.54: NAWIGACJA PO PRZYPISACH (Control+Alt+Shift+K oraz
# Control+Alt+K poza przypisem) i EKSPORT PRZYPISOW w dwoch wariantach.
# Zlecenia Kasperczaka 1788204326709-1 i 1788204113403-0.
#
# PO CO TA KONTROLA: "widze nowa komende" nie dowodzi niczego, dopoki ta sama
# sonda nie ZOBACZY, ze w kodzie sprzed zmiany jej NIE BYLO. Rewizja odniesienia
# jest podana JAWNIE - skrypt biorący HEAD po commicie porownywalby kod z samym
# soba i zawsze bylby zielony.
#
# Uzycie: testy/kontrola_negatywna_554.sh [rewizja_odniesienia] [rewizja_mierzona]
#
# TA KONTROLA PILNUJE WERSJI HISTORYCZNEJ.  Czesc jej asercji opisuje zachowanie,
# ktore Kasperczak POZNIEJ kazal zmienic - wtedy oblanie na HEAD jest POPRAWNYM
# wynikiem sondy, a nie regresja programu.  Zeby kontrola dalej dawala sygnal,
# podaj DRUGI argument: rewizje, ktorej ma dotyczyc pomiar.  Bez niego mierzone
# jest drzewo robocze i skrypt wypisuje ostrzezenie.
set -uo pipefail
# UWAGA na pipefail: `grep ... | grep -q ...` zwraca blad, bo grep -q zamyka
# strumien i pierwszy grep dostaje SIGPIPE. Kazdy taki lancuch idzie przez plik
# tymczasowy, nie przez potok.

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
ODNIESIENIE="${1:-13e24cc}"   # 5.0.53, stan PRZED nawigacja i eksportem przypisow
PRZED="/tmp/kn554_przed_EdSharp.cs"
PRZED_MD="/tmp/kn554_przed_EdSharp.md"
PRZED_INI="/tmp/kn554_przed_Hotkeys.ini"
PRZED_TXT="/tmp/kn554_przed_hotkeys.txt"
OK=0
ZLE=0

for para in "EdSharp.cs:$PRZED" "EdSharp.md:$PRZED_MD" "Hotkeys.ini:$PRZED_INI" "hotkeys.txt:$PRZED_TXT"; do
    plik="${para%%:*}"
    cel="${para##*:}"
    git -C "$ROOT" show "${ODNIESIENIE}:${plik}" > "$cel" 2>/dev/null || {
        echo "BLAD: nie moge pobrac $plik z rewizji $ODNIESIENIE" >&2
        exit 2
    }
done

# REWIZJA MIERZONA: bez drugiego argumentu mierzymy drzewo robocze, z nim -
# podana rewizje (wtedy pliki ida do katalogu tymczasowego i tam pracujemy).
# git wolamy z -C "$ROOT", bo po zmianie katalogu nie bylby w repozytorium.
MIERZONA="${2:-}"
if [ -n "$MIERZONA" ]; then
    ROBOCZY="/tmp/kn554_mierzona"
    rm -rf "$ROBOCZY"; mkdir -p "$ROBOCZY"
    for plik in EdSharp.cs EdSharp.md Hotkeys.ini hotkeys.txt; do
        git -C "$ROOT" show "${MIERZONA}:${plik}" > "$ROBOCZY/$plik" 2>/dev/null || {
            echo "BLAD: nie moge pobrac $plik z rewizji mierzonej $MIERZONA" >&2
            exit 2
        }
    done
    cd "$ROBOCZY"
    echo "== Mierzona rewizja: $MIERZONA =="
else
    echo "UWAGA: mierze DRZEWO ROBOCZE.  Ta kontrola pilnuje wersji 5.0.54;"
    echo "       jesli pozniejsza decyzja Kasperczaka zmienila ktores z tych"
    echo "       zachowan, podaj rewizje mierzona jako drugi argument."
fi

zdaj()  { OK=$((OK+1));  echo "OK   $1"; }
oblej() { ZLE=$((ZLE+1)); echo "ZLE  $1"; }

jest_teraz() {
    local plik="${3:-EdSharp.cs}"
    if grep -qF -- "$1" "$plik"; then zdaj "$2"; else oblej "$2 (brak w HEAD: $1)"; fi
}
nie_bylo() {
    local plik="${3:-$PRZED}"
    if grep -qF -- "$1" "$plik"; then oblej "$2 (bylo juz w $ODNIESIENIE: $1)"; else zdaj "$2"; fi
}
bylo() {
    local plik="${3:-$PRZED}"
    if grep -qF -- "$1" "$plik"; then zdaj "$2"; else oblej "$2 (sonda glucha: nie widzi $1 w $ODNIESIENIE)"; fi
}
nie_ma() {
    local plik="${3:-EdSharp.cs}"
    if grep -qF -- "$1" "$plik"; then oblej "$2 (jest w HEAD: $1)"; else zdaj "$2"; fi
}

echo "== Rewizja odniesienia: $ODNIESIENIE =="
echo
echo "-- 1. SKOK DO POPRZEDNIEGO PRZYPISU na czterech warstwach kodu --"
jest_teraz 'menuMiscPriorFootnote = CreateMenuItem("Prior Footnote", "Control+Alt+Shift+K", menuItem_Click, "child silent");' "pozycja menu z chordem Control+Alt+Shift+K"
jest_teraz "menuMiscGoToFootnote, menuMiscPriorFootnote, menuMiscFootnoteList, menuMiscExportFootnotes," "pola klasy zadeklarowane"
jest_teraz "menuMiscGoToFootnote, menuMiscPriorFootnote, menuMiscFootnoteList, menuMiscExportFootnotes, menuMiscInsertComment" "pozycje WSTAWIONE do menu przez AddRange"
jest_teraz "menuItem == menuMiscPriorFootnote" "handler w menuItem_Click"
jest_teraz "else if (menuItem == menuMiscPriorFootnote) GoToMarkdownFootnoteMarker(rtb, false);" "handler WOLA metode skoku"

echo
echo "-- 2. EKSPORT PRZYPISOW na czterech warstwach kodu --"
jest_teraz 'menuMiscExportFootnotes = CreateMenuItem("Export Footnotes ...", "", menuItem_Click, "child silent");' "pozycja menu BEZ chordu (menu only)"
jest_teraz "menuItem == menuMiscExportFootnotes" "handler w menuItem_Click"
jest_teraz "else if (menuItem == menuMiscExportFootnotes) ExportMarkdownFootnotes(rtb);" "handler WOLA metode eksportu"
jest_teraz "private void ExportMarkdownFootnotes(HomerRichTextBox rtb) {" "metoda okna eksportu"
jest_teraz "private static string BuildMarkdownFootnoteExport(string sText, bool bWithSentences) {" "metoda budujaca tresc eksportu"
jest_teraz "private static string GetMarkdownSentenceAt(string sText, int iIndex) {" "metoda granic zdania"
jest_teraz "private static int FindMarkdownFootnoteMarkerIndex(List<MarkdownFootnote> refs, int iCursor, bool bForward) {" "wspolny wybor nastepnego znacznika"

echo
echo "-- 3. TEGO NIE BYLO w $ODNIESIENIE (kontrola roznicujaca) --"
nie_bylo "menuMiscPriorFootnote" "zadnej wzmianki o skoku do poprzedniego przypisu"
nie_bylo "menuMiscExportFootnotes" "zadnej wzmianki o eksporcie przypisow"
nie_bylo "Control+Alt+Shift+K" "chord Control+Alt+Shift+K nie byl uzywany"
nie_bylo "GoToMarkdownFootnoteMarker" "metody skoku po znacznikach nie bylo"
nie_bylo "BuildMarkdownFootnoteExport" "metody tresci eksportu nie bylo"
nie_bylo "GetMarkdownSentenceAt" "metody granic zdania nie bylo"
nie_bylo "FindMarkdownFootnoteMarkerIndex" "wspolnego wyboru znacznika nie bylo"
nie_bylo "Footnotes with their sentences" "opcji ze zdaniami nie bylo"

echo
echo "-- 4. SONDA NIE JEST GLUCHA: to w $ODNIESIENIE JUZ BYLO --"
bylo "menuMiscGoToFootnote = CreateMenuItem(\"Go to Footnote\", \"Control+Alt+K\"" "skok kontekstowy istnial przed zmiana"
bylo "menuMiscFootnoteList = CreateMenuItem(\"Footnote List ...\", \"Alt+K\"" "lista przypisow istniala przed zmiana"
bylo "private void InsertMarkdownFootnote(HomerRichTextBox rtb) {" "wstawianie przypisu istnialo przed zmiana"
bylo "Footnote List=Alt+K," "opis mowiony listy istnial przed zmiana" "$PRZED_INI"

echo
echo "-- 5. STARY SLEPY ZAULEK: byl przed, NIE MA go dzis --"
bylo "Put the cursor in a line with a footnote marker" "komunikat odmowy byl w $ODNIESIENIE"
nie_ma "Put the cursor in a line with a footnote marker" "komunikat odmowy zniknal z kodu"
jest_teraz "int iNext = FindMarkdownFootnoteMarkerIndex(refs, iCursor, true);" "w jego miejscu jest skok do nastepnego przypisu"

echo
echo "-- 6. OPISY MOWIONE w TRZECH plikach (Ctrl+F1 czyta je niewidomemu) --"
for plik in Hotkeys.ini hotkeys.txt EdSharp.md; do
    jest_teraz "Prior Footnote=Control+Alt+Shift+K," "opis skoku do poprzedniego w $plik" "$plik"
    jest_teraz "Export Footnotes=, Put all footnotes" "opis eksportu w $plik" "$plik"
done
# Opis eksportu MUSI byc oznaczony jako menu only, inaczej audyt chordow go zle policzy.
jest_teraz "(menu only, no shortcut)" "eksport oznaczony jako menu only" "Hotkeys.ini"
# Opis skoku kontekstowego musi mowic o nowym zachowaniu, a nie o starym.
jest_teraz "away from any footnote it goes to the next marker" "opis Control+Alt+K uwzglednia nowe zachowanie" "Hotkeys.ini"
nie_bylo "Prior Footnote=" "opisu skoku do poprzedniego nie bylo w Hotkeys.ini" "$PRZED_INI"
nie_bylo "Export Footnotes=" "opisu eksportu nie bylo w hotkeys.txt" "$PRZED_TXT"

echo
echo "-- 7. PROZA PODRECZNIKA (grep po CHORDZIE, nie po nazwie) --"
jest_teraz "Control+Alt+Shift+K goes to the previous one" "proza opisuje skok w tyl" "EdSharp.md"
jest_teraz "Export Footnotes, in the menu with no shortcut of its own" "proza opisuje eksport" "EdSharp.md"
jest_teraz "Control+Alt+K and Control+Alt+Shift+K to move between footnotes" "lista komend strukturalnych zaktualizowana" "EdSharp.md"
jest_teraz "K for the footnote jumps, in both directions" "sekcja o strazniku pisania zaktualizowana" "EdSharp.md"
nie_bylo "Control+Alt+Shift+K" "proza nie wspominala tego chordu przed zmiana" "$PRZED_MD"
# KONTROLA POZYTYWNA PROZY: nie wycialem opisu, ktory ma zostac.
jest_teraz "A footnote keeps a remark out of the sentence that needed it." "wstep sekcji przypisow NIETKNIETY" "EdSharp.md"
jest_teraz "Insert Footnote opens a box with two fields" "opis wstawiania NIETKNIETY" "EdSharp.md"

echo
echo "-- 8. CO SWIADOMIE ZOSTAJE (nie usunalem przy okazji) --"
jest_teraz "private void GoToMarkdownFootnoteOrBack(HomerRichTextBox rtb) {" "skok kontekstowy nadal istnieje"
jest_teraz "private void ShowMarkdownFootnoteList(HomerRichTextBox rtb) {" "lista przypisow nadal istnieje"
jest_teraz "private bool TryGoToFootnoteInReview(MdiChild child) {" "przypisy w podgladzie nadal dzialaja"
jest_teraz 'AddMessage("Footnotes work only on Markdown files!", bGlobalSpeech);' "bramka tylko-Markdown nadal obowiazuje"
jest_teraz "bool bGlobalSpeech = (menuItem == menuMiscGoToFootnote || menuItem == menuMiscPriorFootnote);" "tryb globalny mowy objal TEZ nowy chord z Control+Alt"

echo
echo "== $OK OK, $ZLE ZLE =="
[[ $ZLE -eq 0 ]] || exit 1
exit 0
