#!/usr/bin/env bash
# Kontrola negatywna dla 5.0.62.
#
# CO PILNUJE:
#   1. Skok po odsylaczach (Alt+PageDown / Alt+PageUp) - ustalenie edsharpng-36.
#   2. Skok po komentarzach przeniesiony z rodziny F9 na Alt+Shift+PageUp/Down.
#   3. Lista zakladek po usunieciu OSTATNIEJ pozycji NIE zamyka okna.
#   4. Opisy mowione w TRZECH plikach zgodne z kodem.
#
# Uzycie:
#   ./kontrola_negatywna_562.sh [rewizja_odniesienia] [rewizja_mierzona]
# Bez drugiego argumentu mierzone jest DRZEWO ROBOCZE (i skrypt to mowi).
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ODNIESIENIE="${1:-393d68e}"
MIERZONA="${2:-}"

OK=0; ZLE=0
ok() { OK=$((OK+1)); echo "OK: $1"; }
zle() { ZLE=$((ZLE+1)); echo "ZLE: $1"; }
sprawdz() { if [[ "$1" == "tak" ]]; then ok "$2"; else zle "$2"; fi }

if [[ -n "$MIERZONA" ]]; then
    KAT="/tmp/kn562_mierzona"
    rm -rf "$KAT"; mkdir -p "$KAT"
    for plik in EdSharp.cs Hotkeys.ini hotkeys.txt EdSharp.md; do
        git -C "$ROOT" show "$MIERZONA:$plik" > "$KAT/$plik" || exit 1
    done
    cd "$KAT"
    echo "MIERZONA REWIZJA: $MIERZONA"
else
    cd "$ROOT"
    echo "UWAGA: mierze DRZEWO ROBOCZE (rewizja odniesienia: $ODNIESIENIE)"
fi
echo "REWIZJA ODNIESIENIA: $ODNIESIENIE"
echo

# Zrodlo rewizji odniesienia raz, do wielokrotnego uzytku.
STARE_CS="/tmp/kn562_stare_EdSharp.cs"
git -C "$ROOT" show "$ODNIESIENIE:EdSharp.cs" > "$STARE_CS" || exit 1
STARE_INI="/tmp/kn562_stare_Hotkeys.ini"
git -C "$ROOT" show "$ODNIESIENIE:Hotkeys.ini" > "$STARE_INI" || exit 1

echo "--- 1. SKOK PO ODSYLACZACH: wszystkie CZTERY warstwy komendy"
# Cztery warstwy, bo brak KTOREJKOLWIEK daje zielony build i komende, ktorej
# nie da sie uruchomic z klawiatury (lekcja z 5.0.44 i osieroconego helpera).
sprawdz "$( [[ $(grep -c 'menuNavigateNextLink, menuNavigatePriorLink;' EdSharp.cs) -eq 1 ]] && echo tak )" \
    "warstwa 1: pola menuNavigateNextLink i menuNavigatePriorLink zadeklarowane"
sprawdz "$(grep -q 'menuNavigateNextLink = CreateMenuItem("Next Link", "Alt+PageDown"' EdSharp.cs && echo tak)" \
    "warstwa 2: pozycja menu Next Link na Alt+PageDown"
sprawdz "$(grep -q 'menuNavigatePriorLink = CreateMenuItem("Prior Link", "Alt+PageUp"' EdSharp.cs && echo tak)" \
    "warstwa 2: pozycja menu Prior Link na Alt+PageUp"
sprawdz "$(grep -q 'menuNavigateLinkList, menuNavigateNextLink, menuNavigatePriorLink, menuNavigateGoToStartOfSelection});' EdSharp.cs && echo tak)" \
    "warstwa 3: obie pozycje w AddRange (bez tego nie ma ich w menu ANI w tablicy skrotow)"
sprawdz "$(grep -q 'if (menuItem == menuNavigateNextLink || menuItem == menuNavigatePriorLink)' EdSharp.cs && echo tak)" \
    "warstwa 4: handler rozpoznaje obie pozycje"
sprawdz "$( [[ $(grep -c 'private void GoToMarkdownLink' EdSharp.cs) -eq 1 ]] && echo tak )" \
    "warstwa 4: metoda GoToMarkdownLink istnieje dokladnie raz"

# KONTROLA WAZNOSCI: w rewizji odniesienia NIC Z TEGO nie istnialo.
sprawdz "$( [[ $(grep -c 'GoToMarkdownLink' "$STARE_CS") -eq 0 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE metody GoToMarkdownLink NIE BYLO"
sprawdz "$( [[ $(grep -c 'menuNavigateNextLink' "$STARE_CS") -eq 0 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE pola menuNavigateNextLink NIE BYLO"
sprawdz "$( [[ $(grep -c '"Alt+PageDown"' "$STARE_CS") -eq 0 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE chord Alt+PageDown nie nalezal do zadnej komendy"

echo
echo "--- 2. SKOK MA WSPOLNY PARSER Z LISTA, nie wlasny"
# Wlasny parser oznaczalby, ze po pierwszej poprawce skok i lista pokazuja INNE
# odsylacze w tym samym dokumencie - blad niewidoczny w zielonym buildzie.
CIALO=$(awk '/^private void GoToMarkdownLink/,/^} \/\/ GoToMarkdownLink method/' EdSharp.cs)
DL=$(echo "$CIALO" | wc -l)
sprawdz "$( [[ $DL -ge 20 && $DL -le 80 ]] && echo tak )" \
    "kontrola waznosci zakresu awk: cialo metody ma $DL wierszy (spodziewane 20-80)"
sprawdz "$(echo "$CIALO" | grep -q 'GetMarkdownLinks(sText)' && echo tak)" \
    "skok wola GetMarkdownLinks, czyli parser listy odsylaczow"
sprawdz "$(echo "$CIALO" | grep -q 'GetMarkdownLinkSpeech(found)' && echo tak)" \
    "skok mowi przez GetMarkdownLinkSpeech, czyli tak samo jak lista"
sprawdz "$(echo "$CIALO" | grep -q 'rtb.Index = found.TextStart;' && echo tak)" \
    "kursor staje na TRESCI (TextStart), nie na nawiasie kwadratowym"
# ASERCJA ROZLACZNA: postawienie kursora na Start byloby bledem dostepnosci -
# czytnik czytalby niewidomemu znaki skladni zamiast slow.
sprawdz "$( [[ $(echo "$CIALO" | grep -c 'rtb.Index = found.Start;') -eq 0 ]] && echo tak )" \
    "asercja rozlaczna: kursor NIE staje na found.Start (czytnik czytalby nawias)"
sprawdz "$(echo "$CIALO" | grep -q 'Last link!' && echo tak)" \
    "brak zawijania w przod: komunikat Last link!"
sprawdz "$(echo "$CIALO" | grep -q 'First link!' && echo tak)" \
    "brak zawijania w tyl: komunikat First link!"

echo
echo "--- 3. KOMENTARZE: skoki WYSZLY z rodziny F9, wstawianie i lista ZOSTALY"
sprawdz "$(grep -q 'menuMiscNextComment = CreateMenuItem("Next Comment", "Alt+Shift+PageDown"' EdSharp.cs && echo tak)" \
    "Next Comment na Alt+Shift+PageDown"
sprawdz "$(grep -q 'menuMiscPriorComment = CreateMenuItem("Prior Comment", "Alt+Shift+PageUp"' EdSharp.cs && echo tak)" \
    "Prior Comment na Alt+Shift+PageUp"
# Przeniesienie = stary chord ZNIKNAL ORAZ nowy JEST.  Sam nowy chord nie
# odroznia przeniesienia od dopisania DRUGIEJ komendy na tym klawiszu.
sprawdz "$( [[ $(grep -c '"Control+Shift+F9"' EdSharp.cs) -eq 0 ]] && echo tak )" \
    "stary chord Control+Shift+F9 nie nalezy do zadnej komendy"
sprawdz "$( [[ $(grep -c '"Alt+Shift+F9"' EdSharp.cs) -eq 0 ]] && echo tak )" \
    "stary chord Alt+Shift+F9 nie nalezy do zadnej komendy"
sprawdz "$( [[ $(grep -c '"Control+Shift+F9"' "$STARE_CS") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE Control+Shift+F9 BYL zajety (trafien: $(grep -c '"Control+Shift+F9"' "$STARE_CS"))"
sprawdz "$( [[ $(grep -c '"Alt+Shift+F9"' "$STARE_CS") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE Alt+Shift+F9 BYL zajety"
# ROZLACZNOSC: wstawianie i lista NIE ruszyly sie z rodziny F9.  Bez tego
# "przeniesienie" wywalajace cala rodzine tez byloby zielone.
sprawdz "$(grep -q 'menuMiscInsertComment = CreateMenuItem("Insert Comment ...", "Alt+F9"' EdSharp.cs && echo tak)" \
    "asercja rozlaczna: wstawianie komentarza ZOSTAJE na Alt+F9"
sprawdz "$(grep -q 'menuMiscCommentList = CreateMenuItem("Comment List ...", "Control+Alt+F9"' EdSharp.cs && echo tak)" \
    "asercja rozlaczna: lista komentarzy ZOSTAJE na Control+Alt+F9"
# ODWROCONA 03.09.2026 (5.0.63): do 5.0.62 pilnowalismy tu, ze golego F9 NIE
# zabralismy komentarzom, bo obslugiwal czytanie do konca przez skrypt JAWS.
# On sam z tego klawisza i skryptu zrezygnowal, wiec asercja w starej postaci
# oblewala na kodzie zrobionym zgodnie z jego decyzja.  Pilnujemy teraz, ze
# warunek zszedl CALY - a osobno, ze komentarze zwolnionych chordow NIE przejely
# (rodzina F9 zostaje taka, jaka jest, dopoki on nie powie inaczej).
sprawdz "$( [[ $(grep -c 'if (keyData == Keys.F9)' EdSharp.cs) -eq 0 ]] && echo tak )" \
    "goly F9 nie nalezy do zadnej komendy (obsluga skryptu JAWS zdjeta na jego decyzje)"
sprawdz "$( [[ $(grep -c 'if (keyData == Keys.F9)' "$STARE_CS") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE goly F9 BYL obslugiwany"
sprawdz "$(grep -q 'menuMiscInsertComment = CreateMenuItem("Insert Comment ...", "Alt+F9"' EdSharp.cs && echo tak)" \
    "asercja rozlaczna: komentarze NIE przejely zwolnionego F9, wstawianie zostaje na Alt+F9"

echo
echo "--- 4. LISTA ZAKLADEK: usuniecie OSTATNIEJ pozycji NIE zamyka okna"
# To jego zgloszenie z 02.09 01:41.  Mierzone na ZNIKNIECIU wywolania Close w
# obu obslugach Delete - sam nowy komunikat nie dowodzi, ze okno zostaje.
sprawdz "$(grep -q 'No bookmarks, press Escape to close the list' EdSharp.cs && echo tak)" \
    "nowy komunikat pustej listy zakladek mowi, czym sie wychodzi"
sprawdz "$(grep -q 'No named bookmarks, press Escape to close the list' EdSharp.cs && echo tak)" \
    "ta sama naprawa na liscie zakladek z nazwa (komenda blizniacza)"
LICZBA_CLOSE=$(grep -c 'if (frmHost != null) frmHost.Close();' EdSharp.cs)
sprawdz "$( [[ $LICZBA_CLOSE -eq 0 ]] && echo tak )" \
    "zadna obsluga Delete nie zamyka okna po oproznieniu listy (trafien: $LICZBA_CLOSE)"
STARE_CLOSE=$(grep -c 'if (frmHost != null) frmHost.Close();' "$STARE_CS")
sprawdz "$( [[ $STARE_CLOSE -eq 2 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE okno zamykalo sie w DWOCH miejscach (trafien: $STARE_CLOSE)"
# Mowa MUSI byc wymuszona: fokus nie drgnal, wiec czytnik sam nie ma z czego
# wywnioskowac, ze lista jest pusta.  AddMessage idzie na pasek stanu, ktorego
# czytnik w tym momencie nie oglasza.
sprawdz "$( [[ $(grep -c 'Say.sayForced("No bookmarks, press Escape' EdSharp.cs) -eq 1 ]] && echo tak )" \
    "komunikat idzie przez Say.sayForced, nie na pasek stanu"
# ROZLACZNOSC: usuwanie POJEDYNCZEJ zakladki nadal dziala i nadal potwierdza.
sprawdz "$(grep -q 'App.Frame.AddMessage("Bookmark removed");' EdSharp.cs && echo tak)" \
    "asercja rozlaczna: usuniecie zakladki przy niepustej liscie nadal potwierdza"
# ASERCJA ZAKTUALIZOWANA 03.09.2026 (5.0.64): zakladki zwykle zeszly z sekcji
# Favorites do wlasnej sekcji Bookmarks (jego decyzja "Rozdzielic").  Pytanie
# jest to samo co bylo - czy Delete na liscie usuwa zakladke NAPRAWDE, z pliku
# ustawien, a nie tylko z widoku - zmienila sie nazwa magazynu.
sprawdz "$(grep -qE 'App\.(WriteValue|DeleteKey)\("Bookmarks", sFile' EdSharp.cs && echo tak)" \
    "asercja rozlaczna: zakladka nadal jest USUWANA z pliku ustawien (sekcja Bookmarks), nie tylko z listy"

echo
echo "--- 5. OPISY MOWIONE W TRZECH PLIKACH (nie w jednym)"
for plik in Hotkeys.ini hotkeys.txt EdSharp.md; do
    sprawdz "$(grep -q '^Next Link=Alt+PageDown,' "$plik" && echo tak)" \
        "$plik: opis Next Link z chordem Alt+PageDown"
    sprawdz "$(grep -q '^Prior Link=Alt+PageUp,' "$plik" && echo tak)" \
        "$plik: opis Prior Link z chordem Alt+PageUp"
    sprawdz "$(grep -q '^Next Comment=Alt+Shift+PageDown,' "$plik" && echo tak)" \
        "$plik: opis Next Comment przepisany na nowy chord"
    sprawdz "$(grep -q '^Prior Comment=Alt+Shift+PageUp,' "$plik" && echo tak)" \
        "$plik: opis Prior Comment przepisany na nowy chord"
    sprawdz "$( [[ $(grep -c '^Next Comment=Control+Shift+F9,' "$plik") -eq 0 ]] && echo tak )" \
        "$plik: stary chord komentarza NIE zostal w opisie"
done
sprawdz "$( [[ $(grep -c '^Next Link=' "$STARE_INI") -eq 0 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE opisu Next Link NIE BYLO"

echo
echo "--- 6. PODRECZNIK NIE KLAMIE o klawiszach, ktore sie zmienily"
sprawdz "$(grep -q 'Alt+Shift+PageDown goes to the next comment' EdSharp.md && echo tak)" \
    "proza podrecznika mowi nowe klawisze komentarzy"
sprawdz "$( [[ $(grep -c 'Control+Shift+F9 goes to the next comment' EdSharp.md) -eq 0 ]] && echo tak )" \
    "proza podrecznika NIE mowi juz starych klawiszy komentarzy"
sprawdz "$(grep -q 'Alt+PageDown goes to the next link' EdSharp.md && echo tak)" \
    "proza podrecznika opisuje skok po odsylaczach"
# Akapit o NavigatePart mowil, ze Alt+PageUp/PageDown sa WOLNE po usunieciu Next
# Part.  Po tej wersji to nieprawda i trzeba to bylo sprostowac.
sprawdz "$(grep -q 'Alt+PageDown and Alt+PageUp now travel between links' EdSharp.md && echo tak)" \
    "akapit o NavigatePart sprostowany: te chordy nie sa juz wolne"
sprawdz "$(grep -q 'No bookmarks, press Escape to close the list' EdSharp.md && echo tak)" \
    "podrecznik opisuje nowe zachowanie pustej listy zakladek"

echo
echo "--- 7. NIC INNEGO NIE ZNIKNELO (kontrole na wczesniejszych wersjach)"
sprawdz "$(grep -q 'menuNavigateLinkList = CreateMenuItem("Link List ...", "Control+F6"' EdSharp.cs && echo tak)" \
    "5.0.53 nietknieta: lista odsylaczow nadal na Control+F6"
sprawdz "$(grep -q 'menuMiscNextFootnote = CreateMenuItem("Next Footnote", "Control+Alt+PageDown"' EdSharp.cs && echo tak)" \
    "5.0.58 nietknieta: skok po przypisach nadal na Control+Alt+PageDown"
sprawdz "$(grep -q 'menuNavigateNextBookmark = CreateMenuItem("Next Bookmark", "Shift+PageDown"' EdSharp.cs && echo tak)" \
    "skok po zakladkach nadal na Shift+PageDown (rodzina PageDown bez kolizji)"
sprawdz "$(grep -q 'menuNavigateNextSection= CreateMenuItem("Next Section", "Control+PageDown"' EdSharp.cs && echo tak)" \
    "skok po naglowkach nadal na Control+PageDown"
sprawdz "$(grep -q 'public bool IsRichTextDocument' EdSharp.cs && echo tak)" \
    "5.0.61 nietknieta: pole proweniencji dokumentu RTF nadal jest"
sprawdz "$(grep -q 'rtf2md' EdSharp.ini 2>/dev/null || grep -q 'rtf2md' "$ROOT/EdSharp.ini" && echo tak)" \
    "5.0.61 nietknieta: konwerter rtf2md nadal w sekcji Import"

echo
echo "PODSUMOWANIE: $OK OK, $ZLE ZLE"
[[ $ZLE -eq 0 ]] || exit 1
