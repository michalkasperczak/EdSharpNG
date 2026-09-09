#!/usr/bin/env bash
# KONTROLA NEGATYWNA 5.0.66 - asercje na ZRODLE, nie na binarce.
#
# CO PILNUJE (dwie decyzje Kasperczaka):
#   A. GUARD TO JEDEN PRZELACZNIK na Control+F7, Control+Shift+F7 WOLNY.
#      Jego slowa 03.09.2026: "Ja bym dal wspolny skrot Ctrl-F7 wlacz/wylacz
#      zabezpieczenie i tyle, a z shiftem bedzie wolny".
#   B. Control+O NA .RTF DAJE LISTE WARIANTOW zamiast pytania tak-nie
#      (ustalenie edsharpng-106, wariant B).
#
# KONTROLA WAZNOSCI: ten sam skrypt na rewizji 0ad6a2b (5.0.65) MUSI oblac.
# Uruchomienie: bash testy/kontrola_negatywna_566.sh [katalog repo] [rewizja]
#
# PULAPKA, KTORA TEN SKRYPT OMIJA (czwarty raz w tym projekcie): asercja o
# BRAKU napisu trafia w KOMENTARZ opisujacy usuniecie i oblewa przy dobrym
# kodzie.  Dlatego kazde pytanie "czegos NIE MA" idzie do CS_KOD - zrodla bez
# komentarzy liniowych - a filtr komentarzy ma wlasna kontrole waznosci.
#
# DRUGA PULAPKA, ISTOTNA WLASNIE TU: przy USUWANIU komendy polowa asercji musi
# pilnowac, ze nie znikneło NIC WIECEJ.  Guard to nie tylko dwie pozycje menu:
# to takze ApplyGuard, SaveGuardFlag i GetUserGuard, ktore obsluguja OTWIERANIE
# plikow.  Usuniecie ich razem z komenda dalo by zielony build i pliki
# otwierajace sie bez zapamietanej ochrony.

set -uo pipefail
REPO="${1:-/mnt/d/projekty/edsharp-pr}"
REW="${2:-}"
if [ -n "$REW" ]; then
  # Mierzenie WSKAZANEJ rewizji: pliki wydajemy przez git show do katalogu
  # tymczasowego.  Po cd git wymaga -C "$REPO", bo nie jest juz w repozytorium.
  MIERZ="/tmp/kn566_mierzona"
  rm -rf "$MIERZ"; mkdir -p "$MIERZ"
  for f in EdSharp.cs EdSharp.ini Hotkeys.ini hotkeys.txt EdSharp.md; do
    git -C "$REPO" show "$REW:$f" > "$MIERZ/$f" 2>/dev/null \
      || { echo "BLAD: nie moge wydac $f z rewizji $REW" >&2; exit 2; }
  done
  echo "UWAGA: mierze rewizje $REW, nie drzewo robocze."
  REPO="$MIERZ"
else
  echo "UWAGA: mierze DRZEWO ROBOCZE $REPO."
fi
CS="$REPO/EdSharp.cs"
INI="$REPO/EdSharp.ini"
HOTINI="$REPO/Hotkeys.ini"
HOTTXT="$REPO/hotkeys.txt"
MD="$REPO/EdSharp.md"
OK=0
ZLE=0

KOD="$(mktemp)"
sed 's://.*::' "$CS" > "$KOD"

ok()  { OK=$((OK+1));  echo "OK: $1"; }
zle() { ZLE=$((ZLE+1)); echo "ZLE: $1"; }
ma()      { if grep -qF -- "$2" "$1"; then ok "$3"; else zle "$3"; fi; }
niema()   { if grep -qF -- "$2" "$1"; then zle "$3"; else ok "$3"; fi; }
ile()     { C=$(grep -cF -- "$2" "$1" 2>/dev/null); C=${C:-0}; if [ "$C" -eq "$3" ]; then ok "$4 (zmierzone: $C)"; else zle "$4 (zmierzone: $C, oczekiwano $3)"; fi; }

echo "--- KONTROLA WAZNOSCI FILTRA KOMENTARZY"
# Nazwa usunietej komendy MUSI zostac w komentarzu (komentarz dokumentuje
# decyzje), a NIE w kodzie.  Ten przypadek dowodzi, ze filtr cokolwiek robi.
if grep -qF 'No Guard' "$CS" && ! grep -qF 'No Guard' "$KOD"; then
  ok "filtr komentarzy dziala: nazwa usunietej komendy jest w komentarzu, nie w kodzie"
else
  zle "filtr komentarzy NIE zostal potwierdzony (brak przypadku kontrolnego)"
fi

echo
echo "--- A. GUARD: JEDEN PRZELACZNIK, DRUGI CHORD ZWOLNIONY"
# WSZYSTKIE WARSTWY USUNIECIA KOMENDY, po kolei (lekcja z 5.0.44).
niema "$KOD" 'menuMiscNoGuard' "pole klasy, AddRange i handler komendy No Guard zniknely"
niema "$KOD" 'CreateMenuItem("No Guard"' "pozycja menu No Guard zniknela"
niema "$KOD" '"Control+Shift+F7"' "chord Control+Shift+F7 nie nalezy do zadnej komendy"
niema "$HOTINI" 'No Guard=' "Hotkeys.ini: opis mowiony zniknal"
niema "$HOTTXT" 'No Guard=' "hotkeys.txt: opis mowiony zniknal"
niema "$MD" 'No Guard=' "EdSharp.md: tabela skrotow bez No Guard"
niema "$MD" 'Control+Shift+F7 drops this protection' "EdSharp.md: proza nie obiecuje zdjetego chordu"
# WARSTWA, KTORA NAJLATWIEJ PRZEGAPIC: martwy wpis w sekcji [Keys] pliku
# ustawien.  Lekcja z 5.0.44 (osierocony NavigatePart) - usuniecie komendy nie
# usuwa jej wpisu w konfiguracji.
niema "$INI" 'No Guard=' "EdSharp.ini sekcja Keys: martwy wpis zniknal"

# KOMENDA ZOSTAJE I PRZELACZA.
ma "$KOD" 'CreateMenuItem("Guard Document", "Control+F7"' "Guard Document nadal na Control+F7"
ma "$KOD" 'bool bWasGuarded = GetUserGuard(child);' "stan brany z GetUserGuard, nie z rtb.ReadOnly"
ma "$KOD" 'rtb.SetGuard(!bWasGuarded);' "SetGuard wolany z ZANEGOWANYM stanem, czyli przelacza"
ma "$KOD" 'SaveGuardFlag(child.File, !bWasGuarded);' "zapisywany jest stan NOWY, nie staly"
ma "$KOD" 'AddMessage(bWasGuarded ? "Guard off" : "Guard on");' "mowa podaje SKUTEK w obu kierunkach"
# BRAMKA PODGLADU: bez niej przelacznik zdejmowal by ochrone, ktorej user nie
# wlaczal (podglad Markdown wlacza ja sam), i utrwalal to w pliku ustawien.
ma "$KOD" 'if (child.MarkdownReviewMode) {' "bramka trybu podgladu jest w handlerze"
ma "$KOD" 'AddMessage("Close the preview first!");' "bramka mowi, dlaczego nie zadzialala"
# KOMUNIKATY DWOCH KOMEND JEDNOKIERUNKOWYCH MUSZA ZNIKNAC.
niema "$KOD" 'Already guarded' "komunikat komendy jednokierunkowej zniknal (Already guarded)"
niema "$KOD" 'Not guarded' "komunikat komendy jednokierunkowej zniknal (Not guarded)"
ma "$HOTINI" 'Guard Document=Control+F7, Turn read-only protection on or off' "Hotkeys.ini: opis mowi o przelaczaniu"
ma "$HOTTXT" 'Guard Document=Control+F7, Turn read-only protection on or off' "hotkeys.txt: opis mowi o przelaczaniu"
ma "$MD" 'to turn read-only protection on or off' "EdSharp.md: proza mowi o przelaczaniu"

echo
echo "--- A2. NIC WIECEJ NIE ZNIKNELO (polowa asercji na kontrole)"
# Te trzy helpery obsluguja OTWIERANIE plikow, nie komende.  Usuniecie ich
# razem z No Guard dalo by zielony build i pliki bez zapamietanej ochrony.
ma "$CS" 'bool ApplyGuard(string sSection, string sFile) {' "ApplyGuard (odczyt ochrony przy otwarciu) nietkniety"
ma "$CS" 'public void SaveGuardFlag(string sFile, bool bGuard) {' "SaveGuardFlag (trwalosc flagi) nietkniety"
ma "$CS" 'public bool GetUserGuard(MdiChild child) {' "GetUserGuard (zrodlo prawdy o stanie) nietkniety"
ma "$CS" 'if (!ApplyGuard("Favorites", sFile)) ApplyGuard("Recent", sFile)' "kolejnosc odczytu Favorites przed Recent nietknieta"
ma "$CS" 'public bool SetGuard(bool bGuard) {' "HomerRichTextBox.SetGuard nadal istnieje"
ma "$CS" 'bool bOldGuard = this.ReadOnly;' "SetGuard nadal zwraca stan POPRZEDNI (na tym stoi przelacznik)"
ma "$CS" 'rtb.SetGuard(child.MarkdownReviewOldGuard);' "wyjscie z podgladu nadal przywraca stan uzytkownika"
# SASIEDZI RODZINY F7: kontrola, ze nie zdjelismy calej rodziny.
ma "$KOD" '"Shift+F7"' "sasiad Shift+F7 (Thesaurus) nietkniety"
ma "$KOD" '"Alt+F7"' "sasiad Alt+F7 (Lookup Term) nietkniety"
ma "$KOD" '"Alt+Shift+F7"' "sasiad Alt+Shift+F7 (Translate Language) nietkniety"

echo
echo "--- B. CONTROL+O NA .RTF: LISTA WARIANTOW"
ma "$KOD" 'Dialog.Pick("Open RTF File As"' "lista wariantow zamiast MessageBoxa"
ma "$KOD" '"Convert to Markdown"' "wariant konwersji do Markdown"
ma "$KOD" '"Open as rich text, keeping formatting"' "wariant otwarcia jako rich text"
ma "$KOD" '"Open as plain text, showing RTF source"' "wariant otwarcia jako zwykly tekst"
ma "$KOD" '"Other conversion ..."' "wariant przejscia do tabeli Import"
ma "$KOD" 'OpenOrActivateWindow(sFile, 2, "", "", "rtf2md");' "wariant Markdown podaje klucz Import WPROST"
ma "$KOD" 'public const int iOpenRichText = -2;' "stala otwarcia rich text bez konwersji istnieje"
ma "$KOD" 'if (iConvert == 0 || iConvert == iOpenRichText) sText = "";' "stala omija sciezke konwersji"
# KLUCZ MUSI DOJSC DO KONWERTERA, inaczej jest martwym argumentem.
ma "$KOD" 'public static string ConvertFile2String(string sSource, ref int iConvert, ref string sTargetExt, bool bTextOnly, string sForceImport) {' "konwerter przyjmuje klucz Import"
ma "$KOD" 'else if (sForceImport.Length > 0) sResult = sForceImport;' "podany klucz POMIJA pytanie o format"
ma "$KOD" 'sText = COM.ConvertFile2String(sFile, ref iConvert, ref sTargetExt, false, sForceImport);' "klucz realnie przekazany z otwarcia do konwertera"
# PLIK USTAWIEN: bez tego klucza wariant Markdown wskazuje na nic.
ma "$INI" 'rtf2md=' "tabela Import ma klucz rtf2md"
ma "$INI" '-f rtf -t gfm' "rtf2md idzie przez pandoc do gfm"
# ZMIANA WEZLOWA, NIE HURTOWA: pytanie tak-nie zostaje tam, gdzie go nie tykal.
ma "$KOD" 'if (Path.GetExtension(sFile).ToLower() == ".rtf") sChoice = Dialog.Confirm("Confirm", "Treat as rich text?", "Y");' "Paste File nadal pyta tak-nie (jego decyzja tego nie dotyczyla)"
ile "$KOD" 'Dialog.Confirm("Confirm", "Treat as rich text?"' 1 "pytanie tak-nie zostalo DOKLADNIE w jednym miejscu"
# STARY OVERLOAD MUSI ZOSTAC: wolaja go trzy inne miejsca w kodzie.
ma "$KOD" 'public void OpenOrActivateWindow(string sFile, int iConvert, string sLine, string sColumn) {' "czteroargumentowa wersja otwarcia ZOSTAJE"
ma "$KOD" 'OpenOrActivateWindow(sFile, iConvert, sLine, sColumn, "");' "stara wersja deleguje do nowej z pustym kluczem"

echo
echo "--- C. REGRESJA WERSJI POPRZEDNICH"
ma "$CS" 'public static void ClearExtraSpeechOption() {' "5.0.65: czyszczenie wpisu przelacznika mowy nietkniete"
# PULAPKA NAZWY (znana z 5.0.44): pozycja menu ma na koncu " ...", wiec
# pytanie o goly "Regular Expression Tool" z nawiasem CreateMenuItem dawalo
# FALSZYWY ALARM przy poprawnym kodzie.  Pytamy o pelny napis pozycji.
ma "$KOD" 'CreateMenuItem("Regular Expression Tool ...", "Control+Shift+Y"' "5.0.65: polaczona komenda wyrazen regularnych nietknieta"
niema "$KOD" '"Control+Shift+H"' "5.0.65: Control+Shift+H nadal zwolniony"
ma "$CS" 'public static void MigrateBookmarksOutOfFavorites() {' "5.0.64: migracja zakladek nietknieta"
ma "$CS" 'ReadData("BookmarksSplit"' "5.0.64: znacznik migracji nietkniety"
niema "$KOD" 'SayAllTempFile' "5.0.63: wolanie skryptu JAWS nadal nie istnieje"
ma "$KOD" '"Control+Alt+F9"' "5.0.63: lista komentarzy nadal na swoim chordzie"
ma "$CS" 'Saving as plain text, not rich text' "5.0.62: ostrzezenie o zapisie tekstem w pliku .rtf nietkniete"
ma "$CS" 'public bool IsRichTextDocument = false;' "5.0.62: prowieniencja dokumentu nietknieta"
ma "$CS" 'MigrateDefaultExtensionToMarkdown' "5.0.59: migracja rozszerzenia nietknieta"
ma "$CS" 'NamedBookmarks' "5.0.55: zakladki z nazwa maja wlasna sekcje"

rm -f "$KOD"
echo
echo "== $OK OK, $ZLE ZLE =="
[ "$ZLE" -eq 0 ] || exit 1
