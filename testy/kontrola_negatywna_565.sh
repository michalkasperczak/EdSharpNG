#!/usr/bin/env bash
# KONTROLA NEGATYWNA 5.0.65 - asercje na ZRODLE, nie na binarce.
#
# CO PILNUJE (piec decyzji Kasperczaka z 03.09.2026, jego plik odpowiedzi):
#   A. Control+Shift+H schodzi do menu, litera H wolna.
#   B. Extra Speech Toggle usuniete CALKIEM + wpis w pliku ustawien wyczyszczony.
#   C. Extract i Yield with Regular Expression polaczone w jedna komende.
#   D. Wiersz listy odsylaczy bez adresu, adres po strzalce w lewo.
#   E. Adresy e-mail jako odsylacze, tez w plikach nie-Markdown.
#
# KONTROLA WAZNOSCI: ten sam skrypt na rewizji 8a33f4f (5.0.64) MUSI oblac.
# Uruchomienie: bash testy/kontrola_negatywna_565.sh [katalog repo]
#
# PULAPKA, KTORA TEN SKRYPT OMIJA (trzeci raz w tym projekcie): asercja o BRAKU
# napisu trafiala w KOMENTARZ opisujacy usuniecie i oblewala przy dobrym kodzie.
# Dlatego wszystko, co pyta "czegos NIE MA", pyta o CS_KOD - zrodlo bez
# komentarzy - a filtr komentarzy ma na koncu wlasna kontrole waznosci.

set -uo pipefail
REPO="${1:-/mnt/d/projekty/edsharp-pr}"
CS="$REPO/EdSharp.cs"
HOTINI="$REPO/Hotkeys.ini"
HOTTXT="$REPO/hotkeys.txt"
MD="$REPO/EdSharp.md"
OK=0
ZLE=0

# Zrodlo BEZ komentarzy liniowych. Pytania o brak napisu ida tutaj.
KOD="$(mktemp)"
sed 's://.*::' "$CS" > "$KOD"

ok()  { OK=$((OK+1));  echo "OK: $1"; }
zle() { ZLE=$((ZLE+1)); echo "ZLE: $1"; }
ma()      { if grep -qF -- "$2" "$1"; then ok "$3"; else zle "$3"; fi; }
niema()   { if grep -qF -- "$2" "$1"; then zle "$3"; else ok "$3"; fi; }

echo "--- KONTROLA WAZNOSCI FILTRA KOMENTARZY"
if grep -qF 'Extra Speech Toggle' "$CS" && ! grep -qF 'Extra Speech Toggle' "$KOD"; then
  ok "filtr komentarzy dziala: nazwa usunietej komendy jest w komentarzu, nie w kodzie"
else
  # Nie jest to blad kodu - to znaczy, ze filtr nie ma czego pokazac.  Mowimy
  # o tym wprost, zeby zielony wynik nie ukryl gluchego filtra.
  zle "filtr komentarzy NIE zostal potwierdzony (brak przypadku kontrolnego)"
fi

echo
echo "--- A. HARD LINE BREAK: CHORD ZDJETY, KOMENDA ZOSTAJE"
ma "$KOD" 'CreateMenuItem("Hard Line Break ...", ""' "pozycja menu Hard Line Break bez chordu"
niema "$KOD" '"Control+Shift+H"' "chord Control+Shift+H nie nalezy do zadnej komendy"
ma "$CS" 'HardLineBreak();' "sama funkcja HardLineBreak nadal wolana"
ma "$HOTINI" 'Hard Line Break=, ' "Hotkeys.ini: opis mowiony bez chordu"
ma "$HOTTXT" 'Hard Line Break=, ' "hotkeys.txt: opis mowiony bez chordu"
niema "$MD" 'Hard Line Break, Control+Shift+H' "EdSharp.md: proza nie obiecuje zdjetego chordu"

echo
echo "--- B. EXTRA SPEECH TOGGLE USUNIETE CALKIEM"
niema "$KOD" 'menuMiscExtraSpeechToggle' "pole klasy i obsluga zniknely (wszystkie warstwy)"
niema "$KOD" 'CreateMenuItem("Extra Speech Toggle"' "pozycja menu zniknela"
niema "$HOTINI" 'Extra Speech Toggle=' "Hotkeys.ini: opis mowiony zniknal"
niema "$HOTTXT" 'Extra Speech Toggle=' "hotkeys.txt: opis mowiony zniknal"
ma "$CS" 'public static void ClearExtraSpeechOption() {' "metoda czyszczaca wpis istnieje"
ma "$CS" 'ClearExtraSpeechOption();' "metoda czyszczaca jest WOLANA przy starcie"
ma "$CS" 'ReadData("ExtraSpeechDropped"' "czyszczenie jest jednorazowe (znacznik)"
# TA ASERCJA PILNUJE RZECZY, O KTORA NIKT NIE PROSIL, A KTORA ZGINELABY SLEPYM
# DeleteKey: ten sam klucz niesie wyciszenie zmian wciecia.
ma "$CS" 'if (sValue.Contains("-")) WriteOption("E&xtraSpeech", "-");' "czyszczenie ZACHOWUJE segment wyciszenia zmian wciecia"
ma "$CS" 'App.IndentChange = App.ReadOption("E&xtraSpeech", "Y").Contains("-") ? false : true;' "odczyt IndentChange nietkniety"
ma "$KOD" 'CreateMenuItem("Extra Speech Log", "Alt+Shift+X"' "dziennik mowy ZOSTAJE (osobna funkcja)"
ma "$CS" 'if (!App.ExtraSpeech) {' "przekierowanie do Speech.log nadal dziala"

echo
echo "--- C. DWIE KOMENDY REGEXP POLACZONE W JEDNA"
ma "$KOD" 'CreateMenuItem("Regular Expression Tool ...", "Control+Shift+Y"' "nowa jedna pozycja menu z chordem"
niema "$KOD" 'menuMiscYieldWithRegExp' "stare pole Yield zniknelo (wszystkie warstwy)"
niema "$KOD" 'menuMiscExtractWithRegExp' "stare pole Extract zniknelo (wszystkie warstwy)"
ma "$CS" '"Count matches"' "wybor liczenia w okienku"
ma "$CS" '"Extract matches to a new window"' "wybor wypisania w okienku"
ma "$CS" 'Util.RegExpCountCase(sText, sResult)' "dzialanie liczenia NIE zginelo"
ma "$CS" 'Util.RegExpExtractCase(sText, sResult)' "dzialanie wypisywania NIE zginelo"
ma "$HOTINI" 'Regular Expression Tool=Control+Shift+Y' "Hotkeys.ini: nowy opis mowiony"
ma "$HOTTXT" 'Regular Expression Tool=Control+Shift+Y' "hotkeys.txt: nowy opis mowiony"
ma "$MD" 'Regular Expression Tool=Control+Shift+Y' "EdSharp.md: tabela skrotow zaktualizowana"
niema "$HOTINI" 'Yield with Regular Expression=' "Hotkeys.ini: stary opis Yield zszedl"
niema "$HOTINI" 'Extract with Regular Expression=' "Hotkeys.ini: stary opis Extract zszedl"
ma "$KOD" '"Control+Shift+R"' "sasiadka Replace with Regular Expression nietknieta"

echo
echo "--- D. LISTA ODSYLACZY: WIERSZ BEZ ADRESU"
ma "$CS" 'lsShow.Add(GetMarkdownLinkListLine(link) + ", line " + iLine);' "wiersz listy budowany nowa metoda"
ma "$CS" 'private static string GetMarkdownLinkListLine(MarkdownLink link) {' "metoda wiersza istnieje"
ma "$CS" 'private static string GetMarkdownLinkAddressSpeech(MarkdownLink link) {' "metoda dopowiedzenia adresu istnieje"
ma "$CS" 'Say.sayForced(GetMarkdownLinkAddressSpeech(link));' "strzalka w lewo WOLA dopowiedzenie"
ma "$CS" 'if (ev.KeyData == Keys.Left) {' "obsluga strzalki w lewo na liscie odsylaczy"
ma "$CS" 'Left Arrow reads the web address' "podpowiedz okna mowi o strzalce w lewo"
# Mowa po skoku ZOSTAJE pelna: tam adres jest potrzebny, bo user wybral pozycje
# i chce wiedziec, gdzie trafil.  Bez tej asercji "uproszczenie" mogloby zabrac
# adres takze po skoku.
ma "$CS" 'Util.Say(GetMarkdownLinkSpeech(pick));' "mowa PO SKOKU nadal pelna (adres potrzebny)"
ma "$MD" 'Left Arrow reads the address' "EdSharp.md: opis listy odsylaczy zaktualizowany"

echo
echo "--- E. ADRESY E-MAIL JAKO ODSYLACZE"
ma "$CS" 'MarkdownMailAddressRegex' "wzorzec adresu pocztowego istnieje"
ma "$CS" 'MarkdownWwwUrlRegex, MarkdownMailAddressRegex}' "wzorzec DOLOZONY do tablicy parsera (nie tylko zadeklarowany)"
ma "$KOD" 'sUrl = "mailto:" + sUrl;' "kopiowanie z formatowaniem dopisuje schemat mailto"
ma "$CS" 'char cBefore = sLine[m.Index - 1];' "adres z mailto: nie wchodzi na liste dwa razy (odsiew po znaku przed)"
ma "$CS" 'MarkdownAutoLinkRegex' "trzy stare wzorce nietkniete: <http>"
ma "$CS" 'MarkdownBareUrlRegex' "trzy stare wzorce nietkniete: http"
ma "$CS" 'MarkdownWwwUrlRegex' "trzy stare wzorce nietkniete: www"

echo
echo "--- F. REGRESJA WERSJI POPRZEDNICH"
ma "$CS" 'public static void MigrateBookmarksOutOfFavorites() {' "5.0.64: migracja zakladek nietknieta"
ma "$CS" 'ReadData("BookmarksSplit"' "5.0.64: znacznik migracji nietkniety"
niema "$KOD" 'SayAllTempFile' "5.0.63: wolanie skryptu JAWS nadal nie istnieje"
ma "$KOD" '"Control+Alt+F9"' "5.0.63: lista komentarzy nadal na swoim chordzie"
ma "$CS" 'MigrateDefaultExtensionToMarkdown' "5.0.59: migracja rozszerzenia nietknieta"
ma "$CS" 'NamedBookmarks' "5.0.55: zakladki z nazwa maja wlasna sekcje"
ma "$CS" 'EdSharpMarkdownFormat' "5.0.60: wlasny format schowka nietkniety"

rm -f "$KOD"
echo
echo "== $OK OK, $ZLE ZLE =="
[ "$ZLE" -eq 0 ] || exit 1
