#!/usr/bin/env bash
# Kontrola negatywna dla 5.0.64 - rozdzielenie zakladek od ulubionych oraz
# propozycja nazwy zakladki ze slowa pod kursorem.
#
# PO CO OSOBNY SKRYPT OBOK SONDY NA BINARCE: sonda pyta o NAPISY w .exe, a
# literaly sa w binarce DEDUPLIKOWANE - napis "Bookmarks" jest tam juz wtedy,
# gdy uzywa go SAMA MIGRACJA, wiec sonda NIE odroznia stanu "wszystkie odczyty
# zakladek przeniesione" od "przeniesiona tylko migracja, a Set Bookmark dalej
# pisze do Favorites".  To rozstrzyga wylacznie liczenie miejsc w KODZIE, i
# dlatego ten skrypt istnieje.  Zmierzone kontrola gluchoty w tej iteracji.
#
# CO PILNUJE:
#   1. Wszystkie odczyty i zapisy zakladek ZWYKLYCH ida do sekcji Bookmarks.
#   2. W sekcji Favorites zostaje TYLKO to, co dotyczy ulubionych, guard i
#      zawijania - czyli wlasnosci OTWARCIA pliku.
#   3. Program nie mowi juz, ze zdjecie z ulubionych kasuje zakladki.
#   4. Propozycja nazwy zakladki bierze slowo pod kursorem, nie caly wiersz.
#   5. ROZLACZNOSC: zakladki z nazwa, ulubione i rodziny z poprzednich wersji
#      sa nietkniete.
#   6. Opisy mowione (Hotkeys.ini, hotkeys.txt, EdSharp.md) nie klamia.
#
# Uzycie:
#   ./kontrola_negatywna_564.sh [rewizja_odniesienia] [rewizja_mierzona]
# Bez drugiego argumentu mierzone jest DRZEWO ROBOCZE (i skrypt to mowi).
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ODNIESIENIE="${1:-dd27546}"
MIERZONA="${2:-}"

OK=0; ZLE=0
ok() { OK=$((OK+1)); echo "OK: $1"; }
zle() { ZLE=$((ZLE+1)); echo "ZLE: $1"; }
sprawdz() { if [[ "$1" == "tak" ]]; then ok "$2"; else zle "$2"; fi }

if [[ -n "$MIERZONA" ]]; then
    KAT="/tmp/kn564_mierzona"
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

STARE_CS="/tmp/kn564_stare_EdSharp.cs"
git -C "$ROOT" show "$ODNIESIENIE:EdSharp.cs" > "$STARE_CS" || exit 1
STARE_MD="/tmp/kn564_stare_EdSharp.md"
git -C "$ROOT" show "$ODNIESIENIE:EdSharp.md" > "$STARE_MD" || exit 1

# Kod BEZ komentarzy liniowych.  BEZ TEGO KAZDE PYTANIE O BRAK CZEGOS KLAMIE:
# komentarz opisujacy zmiane zawiera przeniesione nazwy, wiec trafia w siebie
# samego.  To czwarty taki falszywy alarm w tym projekcie.
KOD="/tmp/kn564_kod_aktywny.cs"
sed 's://.*$::' EdSharp.cs > "$KOD"
# KONTROLA WAZNOSCI SAMEGO FILTRA: gdyby wycinal za duzo, kazde pytanie o brak
# byloby zielone zawsze.
sprawdz "$(grep -q 'App.WriteValue("Bookmarks", sFile, sText)' "$KOD" && echo tak)" \
    "kontrola waznosci filtra komentarzy: plik kodu aktywnego nadal widzi kod"

echo
echo "--- 1. ZAKLADKI ZWYKLE CZYTAJA I PISZA SEKCJE Bookmarks"
# ROZSTRZYGA LICZBA MIEJSC, nie samo istnienie napisu: gdyby przeniesiona byla
# tylko migracja, ta asercja oblewa.  Set Bookmark, Clear Bookmark (2x: zapis i
# usuniecie klucza), Go to Bookmark, Next/Prior Bookmark, Delete na liscie
# (2x), wznowienie pozycji w Shown, ApplyFileOptions, migracja (1 zapis).
LICZBA_BOOK=$(grep -c '"Bookmarks"' "$KOD")
sprawdz "$( [[ $LICZBA_BOOK -ge 10 ]] && echo tak )" \
    "sekcja Bookmarks uzyta w co najmniej 10 miejscach kodu aktywnego (jest $LICZBA_BOOK)"
# KONTROLA WAZNOSCI, POPRAWIONA PO FALSZYWYM ALARMIE: pytanie "czy napisu
# Bookmarks nie bylo wcale" oblewalo, bo w rewizji odniesienia ten napis JEST -
# jako TYTUL OKNA listy zakladek (Dialog.PickBookmark("Bookmarks", ...)), a nie
# jako nazwa sekcji INI.  Zmierzone: git show dd27546:EdSharp.cs | grep
# '"Bookmarks"' zwraca dokladnie jedna linie i jest to tytul okna.  Pytamy wiec
# o to, co naprawde rozstrzyga: czy istnial choc jeden ODCZYT albo ZAPIS
# ustawien do tej sekcji.  Bez tej poprawki asercja mierzyla nazwe okna.
STARE_BOOK_INI=$(sed 's://.*$::' "$STARE_CS" | grep -cE 'App\.(ReadValue|WriteValue|DeleteKey|ReadSectionKeys)\("Bookmarks"')
sprawdz "$( [[ $STARE_BOOK_INI -eq 0 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE NIE BYLO zadnego odczytu ani zapisu sekcji Bookmarks (jest $STARE_BOOK_INI)"
NOWE_BOOK_INI=$(grep -cE 'App\.(ReadValue|WriteValue|DeleteKey|ReadSectionKeys)\("Bookmarks"' "$KOD")
sprawdz "$( [[ $NOWE_BOOK_INI -ge 8 ]] && echo tak )" \
    "teraz odczytow i zapisow sekcji Bookmarks jest co najmniej 8 (jest $NOWE_BOOK_INI)"

for wzor in 'sText = App.ReadValue("Bookmarks", sFile, "")' \
            'App.WriteValue("Bookmarks", sFile, sText)' \
            'App.DeleteKey("Bookmarks", sFile)' \
            'string sStored = App.ReadValue("Bookmarks", sFile, "")'; do
    sprawdz "$(grep -qF "$wzor" "$KOD" && echo tak)" \
        "kod aktywny zawiera: $wzor"
done

echo
echo "--- 2. W SEKCJI Favorites ZOSTAJE TYLKO TO, CO DOTYCZY ULUBIONYCH"
# Po rozdzieleniu Favorites ma obslugiwac ulubione, guard i zawijanie - a NIE
# zakladki.  Liczba wystapien MUSI SPASC wobec rewizji odniesienia; gdyby nie
# spadla, znaczyloby, ze zaden odczyt zakladek nie zostal przeniesiony.
NOWE_FAV=$(grep -c '"Favorites"' "$KOD")
STARE_FAV=$(sed 's://.*$::' "$STARE_CS" | grep -c '"Favorites"')
echo "    (Favorites: $ODNIESIENIE=$STARE_FAV, mierzone=$NOWE_FAV)"
sprawdz "$( [[ $NOWE_FAV -lt $STARE_FAV ]] && echo tak )" \
    "liczba uzyc sekcji Favorites SPADLA wobec $ODNIESIENIE"
sprawdz "$( [[ $NOWE_FAV -gt 0 ]] && echo tak )" \
    "ulubione nadal istnieja - Favorites nie zostalo wyciete calkiem"
# Zaden handler zakladek nie moze juz pytac o Favorites.  Sprawdzamy TRZY
# najgrozniejsze wzorce, ktore tam stoly do 5.0.63.
sprawdz "$( [[ $(grep -c 'sText = rtb.Index + "|" + (GetUserGuard(child) ? "G" : "M")' "$KOD") -eq 0 ]] && echo tak )" \
    "Set Bookmark nie sklada juz wpisu z segmentami guard i zawijania (to byl wpis ulubionego)"
sprawdz "$( [[ $(grep -c 'sText = rtb.Index + "|" + (GetUserGuard(child) ? "G" : "M")' "$STARE_CS") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE Set Bookmark TAK skladal ten wpis"
# Guard i zawijanie ZOSTAJA na Favorites - to nie sa zakladki i ich nie ruszamy.
sprawdz "$(grep -q 'if (!ApplyGuard("Favorites", sFile)) ApplyGuard("Recent", sFile)' "$KOD" && echo tak)" \
    "odczyt guard nadal idzie przez Favorites (wlasnosc otwarcia pliku, nie zakladka)"
sprawdz "$(grep -q 'string sStored = App.ReadValue("Favorites", sFile, "")' "$KOD" && echo tak)" \
    "SaveGuardFlag nadal pisze flage guard do Favorites"

# KONTROLA GLUCHOTY WYKRYLA TU DZIURE (03.09.2026) i to jest najwazniejszy wynik
# tego przebiegu.  Podstawiony blad: JEDEN odczyt zakladek (Go to Bookmark)
# wrocil do sekcji Favorites, a migracja i cala reszta zostaly poprawne.  Skutek
# dla uzytkownika bylby cichy i grozny: postawiona zakladka lezy w Bookmarks, a
# Alt+B szuka jej w Favorites, czyli lista zakladek jest PUSTA mimo postawionych
# zakladek.  Sonda przepuscila to na zielono 44/0, bo pytala o PROGI ("co
# najmniej 10 uzyc", "liczba Favorites spadla") - a prog przezyje jedno miejsce
# zapomniane.  Prog mierzy kierunek zmiany, nie jej kompletnosc.
#
# ROZSTRZYGA DOKLADNA LICZBA operacji INI na sekcji Favorites, bo po
# rozdzieleniu ich zbior jest ZAMKNIETY i wyliczalny: Toggle Favorite (odczyt,
# usuniecie klucza, zapis), Clear Favorite (usuniecie), List Favorites (odczyt
# kluczy, usuniecie nieistniejacego pliku), Go to Folder (odczyt kluczy),
# SaveGuardFlag (odczyt i zapis flagi guard).  Razem DZIEWIEC i ani jednej
# wiecej - kazde dziesiate jest zakladka, ktora zostala w starym miejscu.
FAV_INI=$(grep -cE 'App\.(ReadValue|WriteValue|DeleteKey|ReadSectionKeys)\("Favorites"' "$KOD")
sprawdz "$( [[ $FAV_INI -eq 9 ]] && echo tak )" \
    "operacji na sekcji Favorites jest DOKLADNIE 9, czyli tyle, ile wymaga obsluga ulubionych i flagi guard (jest $FAV_INI)"
# Sam odczyt wartosci zawezony jeszcze mocniej: legalne sa DWA - przelacznik
# ulubionych i flaga guard.  Kazdy trzeci odczyt to zakladka pytajaca o zly
# magazyn.
FAV_READ=$(grep -cE 'App\.ReadValue\("Favorites"' "$KOD")
sprawdz "$( [[ $FAV_READ -eq 2 ]] && echo tak )" \
    "odczytow wartosci z Favorites jest DOKLADNIE 2 (przelacznik ulubionych i flaga guard), jest $FAV_READ"
# Kontrola waznosci obu progow w druga strone: w rewizji odniesienia te liczby
# byly INNE, wiec asercje naprawde rozdzielaja stany.
STARE_FAV_INI=$(sed 's://.*$::' "$STARE_CS" | grep -cE 'App\.(ReadValue|WriteValue|DeleteKey|ReadSectionKeys)\("Favorites"' )
sprawdz "$( [[ $STARE_FAV_INI -ne 9 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE operacji na Favorites bylo $STARE_FAV_INI, a nie 9"

echo
echo "--- 3. MIGRACJA JEST JEDNORAZOWA I NIE GUBI TEGO, CO USER MA NA DYSKU"
sprawdz "$(grep -q 'public static void MigrateBookmarksOutOfFavorites()' "$KOD" && echo tak)" \
    "metoda migracji istnieje"
sprawdz "$(grep -q 'MigrateBookmarksOutOfFavorites();' "$KOD" && echo tak)" \
    "migracja jest WOLANA przy starcie (sama definicja nic nie robi)"
sprawdz "$(grep -q 'if (ReadData("BookmarksSplit", "").Trim().Length > 0) return;' "$KOD" && echo tak)" \
    "migracja ma znacznik jednorazowosci - nie przepisuje ustawien przy kazdym starcie"
sprawdz "$(grep -q 'WriteData("BookmarksSplit", "Y");' "$KOD" && echo tak)" \
    "znacznik jest ZAPISYWANY (bez tego jednorazowosc jest pozorna)"
sprawdz "$(grep -q 'ReadSectionKeys("Favorites")' "$KOD" && echo tak)" \
    "migracja czyta ISTNIEJACE wpisy - przenosi zakladki, a nie tylko zaczyna pisac w nowym miejscu"
sprawdz "$(grep -q 'Util.Equiv(sSection, "Bookmarks")' "$KOD" && echo tak)" \
    "nowa sekcja idzie do pliku biezacego kompilatora, jak ulubione (Ini.RedirectFile)"

echo
echo "--- 4. PROGRAM NIE MOWI JUZ NIEPRAWDY O KASOWANIU ZAKLADEK"
sprawdz "$( [[ $(grep -c 'cleared too' "$KOD") -eq 0 ]] && echo tak )" \
    "komunikat o zakladkach skasowanych razem z ulubionym zniknal"
sprawdz "$( [[ $(grep -c 'cleared too' "$STARE_CS") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE ten komunikat BYL"
sprawdz "$(grep -q 'AddMessage("Removed from favorites");' "$KOD" && echo tak)" \
    "zdjecie z ulubionych nadal sie MELDUJE (komenda nie zamilkla)"

echo
echo "--- 5. NAZWA ZAKLADKI ZE SLOWA POD KURSOREM, NIE Z CALEGO WIERSZA"
sprawdz "$(grep -q 'object\[\] aChunk = GetChunk();' "$KOD" && echo tak)" \
    "propozycja nazwy bierze slowo pod kursorem (GetChunk, ta sama droga co slownik)"
sprawdz "$( [[ $(grep -c 'string sRowText = rtb.RowText;' "$KOD") -eq 0 ]] && echo tak )" \
    "caly wiersz NIE jest juz zrodlem propozycji nazwy"
sprawdz "$( [[ $(grep -c 'string sRowText = rtb.RowText;' "$STARE_CS") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE propozycja szla z CALEGO WIERSZA"
sprawdz "$(grep -q 'if (rtb.SelectionLength > 0) sSuggest = rtb.SelectedText;' "$KOD" && echo tak)" \
    "zaznaczenie ma pierwszenstwo nad slowem pod kursorem"
sprawdz "$(grep -q 'bEditNamed ? sExisting : sSuggest' "$KOD" && echo tak)" \
    "zmiana nazwy istniejacej zakladki nadal pokazuje STARA nazwe, nie propozycje"

echo
echo "--- 6. ROZLACZNOSC: nic dzialajacego nie zniknelo przy okazji"
sprawdz "$(grep -q 'App.ReadValue("NamedBookmarks", NamedBookmarkKey(sFile, iRow), "")' "$KOD" && echo tak)" \
    "zakladki Z NAZWA maja nadal wlasna sekcje NamedBookmarks (5.0.55 nietknieta)"
sprawdz "$(grep -q 'menuNavigateSetBookmark = CreateMenuItem("Set Bookmar&k", "Control+B"' "$KOD" && echo tak)" \
    "Control+B nadal stawia zakladke"
sprawdz "$(grep -q 'menuNavigateGoToBookmark = CreateMenuItem("Go to Bookmark", "Alt+B"' "$KOD" && echo tak)" \
    "Alt+B nadal pokazuje liste zakladek"
sprawdz "$(grep -q 'menuFileSetFavorite = CreateMenuItem("Toggle Favorite", "Alt+Shift+L"' "$KOD" && echo tak)" \
    "Alt+Shift+L nadal przelacza ulubione"
sprawdz "$(grep -q 'No bookmarks, press Escape to close the list' "$KOD" && echo tak)" \
    "5.0.62 nietknieta: pusta lista zakladek nadal mowi o Escape"
sprawdz "$(grep -q 'menuNavigateNextLink = CreateMenuItem("Next Link", "Alt+PageDown"' "$KOD" && echo tak)" \
    "5.0.62 nietknieta: skok po odsylaczach nadal na Alt+PageDown"
sprawdz "$( [[ $(grep -c 'COM.JFWRunFunction' "$KOD") -eq 0 ]] && echo tak )" \
    "5.0.63 nietknieta: helper po golym F9 nadal nie wrocil"
sprawdz "$(grep -q 'public static void MigrateDefaultExtensionToMarkdown()' "$KOD" && echo tak)" \
    "5.0.59 nietknieta: migracja domyslnego rozszerzenia nadal jest"

echo
echo "--- 7. OPISY MOWIONE NIE KLAMIA"
sprawdz "$(grep -q 'offering the word under the cursor as the name' Hotkeys.ini && echo tak)" \
    "Hotkeys.ini mowi o slowie pod kursorem"
sprawdz "$(grep -q 'offering the word under the cursor as the name' hotkeys.txt && echo tak)" \
    "hotkeys.txt mowi o slowie pod kursorem"
sprawdz "$( [[ $(grep -c 'so taking a file off the list of favorites clears the bookmarks set in it' EdSharp.md) -eq 0 ]] && echo tak )" \
    "podrecznik NIE twierdzi juz, ze zdjecie z ulubionych kasuje zakladki"
sprawdz "$( [[ $(grep -c 'so taking a file off the list of favorites clears the bookmarks set in it' "$STARE_MD") -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE podrecznik TWIERDZIL, ze kasuje"
sprawdz "$(grep -q 'Bookmarks are kept apart from favorites' EdSharp.md && echo tak)" \
    "podrecznik mowi, ze zakladki i ulubione sa osobne"
sprawdz "$( [[ $(grep -c 'since a bookmarked file is automatically considered a favorite' EdSharp.md) -eq 0 ]] && echo tak )" \
    "podrecznik NIE twierdzi juz, ze plik z zakladka staje sie ulubionym"
sprawdz "$( [[ $(grep -c 'The box offers the text of the current line as a starting name' EdSharp.md) -eq 0 ]] && echo tak )" \
    "podrecznik NIE twierdzi juz, ze propozycja nazwy to caly wiersz"
sprawdz "$(grep -q 'The box offers the word under the cursor as a starting name' EdSharp.md && echo tak)" \
    "podrecznik mowi o slowie pod kursorem"

echo
echo "PODSUMOWANIE: $OK OK, $ZLE ZLE"
[[ $ZLE -eq 0 ]] || exit 1
