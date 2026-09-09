#!/usr/bin/env bash
# Kontrola negatywna 5.0.56: ZAKLADKI Z NAZWA na Control+Shift+B (wstawienie,
# zmiana nazwy, usuniecie pusta nazwa) i Alt+Shift+B (osobna lista).
# Zlecenie Kasperczaka 1788204906696-3, czesc nie podjeta w iteracji 1.
#
# PO CO: "widze nowa komende" nie dowodzi niczego, dopoki ta sama sonda nie
# ZOBACZY, ze w kodzie sprzed zmiany jej NIE BYLO.  Rewizja odniesienia jest
# podana JAWNIE - skrypt biorący HEAD po commicie porownywalby kod z samym soba.
#
# Uzycie: testy/kontrola_negatywna_556.sh [rewizja_odniesienia]
set -uo pipefail
# UWAGA na pipefail: `grep ... | grep -q ...` zwraca blad przez SIGPIPE.
# Kazdy taki lancuch idzie przez plik tymczasowy, nie przez potok.

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
ODNIESIENIE="${1:-b67c6ef}"   # 5.0.55, stan PRZED zakladkami z nazwa
PRZED="/tmp/kn556_przed_EdSharp.cs"
PRZED_MD="/tmp/kn556_przed_EdSharp.md"
PRZED_INI="/tmp/kn556_przed_Hotkeys.ini"
PRZED_TXT="/tmp/kn556_przed_hotkeys.txt"
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
nie_bylo() {
    local plik="${3:-$PRZED}"
    if grep -qF -- "$1" "$plik"; then oblej "$2 (bylo juz w $ODNIESIENIE: $1)"; else zdaj "$2"; fi
}
bylo() {
    local plik="${3:-$PRZED}"
    if grep -qF -- "$1" "$plik"; then zdaj "$2"; else oblej "$2 (sonda glucha: nie widzi $1 w $ODNIESIENIE)"; fi
}

echo "== Rewizja odniesienia: $ODNIESIENIE =="
echo
echo "-- 1. WSTAWIANIE ZAKLADKI Z NAZWA na czterech warstwach --"
jest_teraz 'menuNavigateSetNamedBookmark = CreateMenuItem("Set &Named Bookmark ...", "Control+Shift+B", menuItem_Click, "child silent");' "pozycja menu z chordem Control+Shift+B"
jest_teraz "menuNavigateSetNamedBookmark, menuNavigateNamedBookmarkList, menuNavigateDocumentNavigation" "pola klasy zadeklarowane"
jest_teraz "menuNavigateSetNamedBookmark, menuNavigateNamedBookmarkList, menuNavigateHomeCharacter" "pozycje WSTAWIONE do menu przez AddRange"
jest_teraz "if (menuItem == menuNavigateSetNamedBookmark) {" "handler w menuItem_Click"

echo
echo "-- 2. LISTA ZAKLADEK Z NAZWA na czterech warstwach --"
jest_teraz 'menuNavigateNamedBookmarkList = CreateMenuItem("Named Bookmark &List ...", "Alt+Shift+B", menuItem_Click, "child silent");' "pozycja menu z chordem Alt+Shift+B"
jest_teraz "if (menuItem == menuNavigateNamedBookmarkList) {" "handler w menuItem_Click"
jest_teraz "Dialog.PickNamedBookmark(\"Named Bookmarks\"" "handler WOLA wlasne okno listy"
jest_teraz "public static string PickNamedBookmark(string sTitle, string[] aValue, string[] aDisplay, int iIndex, string sFile) {" "metoda okna listy"

echo
echo "-- 3. MAGAZYN: METODY POMOCNICZE (osierocony helper to lekcja z 5.0.44) --"
jest_teraz "public static string NamedBookmarkKey(string sFile, int iRow) {" "klucz zakladki"
jest_teraz "private static string ReadNamedBookmark(string sFile, int iRow) {" "odczyt jednej"
jest_teraz "private static void WriteNamedBookmark(string sFile, int iRow, string sName) {" "zapis"
jest_teraz "private static void DeleteNamedBookmark(string sFile, int iRow) {" "usuniecie"
jest_teraz "private static void ReadNamedBookmarks(string sFile, List<int> listRows, List<string> listNames) {" "odczyt calej listy"
# KAZDA metoda pomocnicza musi byc WOLANA, nie tylko istniec.
jest_teraz "ReadNamedBookmarks(sFile, listRows, listNames);" "odczyt listy jest WOLANY z handlera"
jest_teraz "WriteNamedBookmark(sFile, iRowNamed, sNamed);" "zapis jest WOLANY z handlera"
jest_teraz "DeleteNamedBookmark(sFile, iRowNamed);" "usuniecie jest WOLANE z handlera"
jest_teraz 'App.DeleteKey("NamedBookmarks", MdiFrame.NamedBookmarkKey(sFile, iRowGone));' "Delete na liscie usuwa z magazynu"

echo
echo "-- 4. TEGO NIE BYLO w $ODNIESIENIE (kontrola roznicujaca) --"
nie_bylo "menuNavigateSetNamedBookmark" "zadnej wzmianki o zakladce z nazwa"
nie_bylo "menuNavigateNamedBookmarkList" "zadnej wzmianki o liscie zakladek z nazwa"
nie_bylo "NamedBookmarks" "sekcji ustawien NamedBookmarks nie bylo"
nie_bylo "PickNamedBookmark" "okna listy nie bylo"
nie_bylo '"Control+Shift+B"' "chord Control+Shift+B nie byl przypisany"
nie_bylo '"Alt+Shift+B"' "chord Alt+Shift+B nie byl przypisany"
nie_bylo "Named bookmark set" "komunikatu potwierdzenia nie bylo"
nie_bylo "No named bookmark!" "komunikatu pustej listy nie bylo"

echo
echo "-- 5. SONDA NIE JEST GLUCHA: to w $ODNIESIENIE JUZ BYLO --"
bylo 'menuNavigateSetBookmark = CreateMenuItem("Set Bookmar&k", "Control+B"' "zakladka zwykla istniala przed zmiana"
bylo 'menuNavigateGoToBookmark = CreateMenuItem("Go to Bookmark", "Alt+B"' "lista zakladek zwyklych istniala przed zmiana"
bylo "public static string PickBookmark(string sTitle" "okno listy zakladek zwyklych istnialo przed zmiana"
bylo "Set Bookmark=Control+B," "opis mowiony zakladki zwyklej istnial przed zmiana" "$PRZED_INI"
# Chord byl WOLNY, ale komentarz o jego zwolnieniu w kodzie BYL - to nie to samo.
bylo "Control+Shift+B zostaje wolny" "komentarz o zwolnieniu chordu byl juz w kodzie"

echo
echo "-- 6. DWIE OSOBNE LISTY (ustalenie edsharpng-40), nie jedna z wyborem --"
jest_teraz "public static string PickBookmark(string sTitle" "lista zakladek zwyklych ZOSTAJE"
jest_teraz 'Dialog.PickBookmark("Bookmarks"' "i jest nadal WOLANA z Alt+B"
# Gdyby powstalo jedno okno z wyborem rodzaju, byloby w nim pytanie o rodzaj.
if grep -qE 'addRadio|Bookmark (kind|type)' EdSharp.cs; then
    oblej "nie ma okna z wyborem rodzaju zakladki"
else
    zdaj "nie ma okna z wyborem rodzaju zakladki"
fi

echo
echo "-- 7. MAGAZYN JEST OSOBNY OD ULUBIONYCH --"
# Zakladki ZWYKLE nadal dziela sekcje Favorites (o tym on wie, czeka na decyzje),
# ale zakladki Z NAZWA maja wlasna sekcje - inaczej zdjecie z ulubionych
# czyscilo by tez nazwane.
jest_teraz 'App.ReadValue("NamedBookmarks"' "odczyt idzie do sekcji NamedBookmarks"
jest_teraz 'App.WriteValue("NamedBookmarks"' "zapis idzie do sekcji NamedBookmarks"
# ASERCJA ZAKTUALIZOWANA 03.09.2026 (5.0.64).  Pytala o to, ze zakladki ZWYKLE
# nadal czytaja sekcje Favorites - i slusznie, dopoki dzielily z ulubionymi jeden
# magazyn.  Od 5.0.64 to jest JUZ NIEPRAWDA: on zdecydowal "Rozdzielic. Zakladki
# a ulubione pliki nie sa powiazane w jedna i druga strone", wiec zakladki zwykle
# maja wlasna sekcje Bookmarks.  Intencja tej asercji byla inna niz jej litera:
# chodzilo o to, ze nazwane NIE dziela magazynu ze zwyklymi.  Ta intencja
# zostaje, zmienil sie tylko nosnik zwyklych zakladek.
jest_teraz 'sText = App.ReadValue("Bookmarks", sFile, "");' "KONTROLA: zakladki zwykle maja OSOBNY magazyn (sekcja Bookmarks, od 5.0.64)"
if grep -n 'App.WriteValue("Favorites"' EdSharp.cs > /tmp/kn556_fav.txt 2>/dev/null && [[ -s /tmp/kn556_fav.txt ]]; then
    zdaj "KONTROLA: zapis do Favorites nadal istnieje (ulubione nie zostaly wyciete)"
else
    oblej "KONTROLA: zapis do Favorites zniknal"
fi

echo
echo "-- 8. OPISY MOWIONE w TRZECH plikach (Ctrl+F1 czyta je niewidomemu) --"
for plik in Hotkeys.ini hotkeys.txt EdSharp.md; do
    jest_teraz "Set Named Bookmark=Control+Shift+B," "opis wstawiania w $plik" "$plik"
    jest_teraz "Named Bookmark List=Alt+Shift+B," "opis listy w $plik" "$plik"
done
nie_bylo "Set Named Bookmark=" "opisu wstawiania nie bylo w Hotkeys.ini" "$PRZED_INI"
nie_bylo "Named Bookmark List=" "opisu listy nie bylo w hotkeys.txt" "$PRZED_TXT"
# ZALEGLY ROZJAZD ZNALEZIONY PRZY OKAZJI: hotkeys.txt nie mial WCALE opisow
# zakladek sekwencyjnych z 5.0.4, choc Hotkeys.ini je mial.
jest_teraz "Next Bookmark=Shift+PageDown," "opis Next Bookmark dopisany do hotkeys.txt" "hotkeys.txt"
jest_teraz "Prior Bookmark=Shift+PageUp," "opis Prior Bookmark dopisany do hotkeys.txt" "hotkeys.txt"
nie_bylo "Next Bookmark=" "tego opisu w hotkeys.txt BRAKOWALO (zalegly rozjazd)" "$PRZED_TXT"

echo
echo "-- 9. PROZA PODRECZNIKA (grep po CHORDZIE, nie po nazwie) --"
jest_teraz "on Control+Shift+B, and it has its own list on Alt+Shift+B" "proza opisuje oba klawisze" "EdSharp.md"
jest_teraz "Two lists rather than one box asking which kind" "proza nazywa decyzje o dwoch listach" "EdSharp.md"
jest_teraz "in their own section" "proza mowi o osobnym magazynie" "EdSharp.md"
# PULAPKA PODCIAGU, zlapana na tej sondzie: "Alt+Shift+B" jest CZESCIA
# "Alt+Shift+Backspace" (Delete Up, komenda obecna od zawsze), wiec goly grep
# dawal falszywe "ten chord juz byl w prozie".  Ta sama klasa bledu wyszla w
# sondzie na binarce.  Wzorzec MUSI wykluczac dluzszy chord o tym poczatku.
if grep -qE 'Alt\+Shift\+B([^a-zA-Z]|$)' "$PRZED_MD"; then
    oblej "proza nie wspominala Alt+Shift+B przed zmiana (bylo juz w $ODNIESIENIE)"
else
    zdaj "proza nie wspominala Alt+Shift+B przed zmiana"
fi
# KONTROLA TEGO WZORCA: musi WIDZIEC dluzszy chord, ktory tam jest.
if grep -qE 'Alt\+Shift\+Backspace' "$PRZED_MD"; then
    zdaj "KONTROLA wzorca: Alt+Shift+Backspace w tamtej prozie jest widziany"
else
    oblej "KONTROLA wzorca: sonda nie widzi nawet Alt+Shift+Backspace"
fi
# KONTROLA POZYTYWNA PROZY: nie wycialem opisu, ktory ma zostac.
jest_teraz "Press Control+B to set a bookmark at the cursor position." "opis zakladki zwyklej NIETKNIETY" "EdSharp.md"
jest_teraz "EdSharp tracks and restores the bookmark, word wrap, and guard settings" "koniec tamtego akapitu NIETKNIETY" "EdSharp.md"

echo
echo "-- 10. TRZY PLIKI OPISOW SA ZE SOBA ZGODNE --"
# Hotkeys.ini i hotkeys.txt roznia sie tylko naglowkiem (BOM+[Hotkeys] kontra
# dwa wiersze tytulu) - reszta MUSI byc identyczna, inaczej Ctrl+F1 i podrecznik
# mowia rozne rzeczy.
if diff <(sed 's/\r//' Hotkeys.ini | tail -n +2) <(sed 's/\r//' hotkeys.txt | tail -n +4) >/dev/null; then
    zdaj "Hotkeys.ini i hotkeys.txt maja identyczna tresc opisow"
else
    oblej "Hotkeys.ini i hotkeys.txt sie ROZJECHALY"
fi

echo
echo "== $OK OK, $ZLE ZLE =="
[[ $ZLE -eq 0 ]] || exit 1
exit 0
