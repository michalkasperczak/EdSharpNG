#!/usr/bin/env bash
# Kontrola negatywna dla 5.0.59: domyslne rozszerzenie md oraz poczta,
# ktora przestaje milczec.  Mierzy ZRODLA wobec JAWNEJ rewizji odniesienia.
#
#   ./kontrola_negatywna_559.sh [rewizja_odniesienia] [rewizja_mierzona]
#
# Bez drugiego argumentu mierzy DRZEWO ROBOCZE (i mowi o tym wprost) - lekcja
# z 5.0.58: skrypt bez tego argumentu pilnowal zachowan, ktore user PO jego
# napisaniu kazal zmienic, i oblanie bylo POPRAWNYM wynikiem sondy.
set -uo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

REF="${1:-a78a631}"
CEL="${2:-}"
OK=0; ZLE=0

if [[ -n "$CEL" ]]; then
    NOWY=$(git show "$CEL:EdSharp.cs")
    echo "Mierze rewizje $CEL wobec odniesienia $REF"
else
    NOWY=$(cat EdSharp.cs)
    echo "UWAGA: mierze DRZEWO ROBOCZE wobec odniesienia $REF (podaj 2. argument, zeby przypiac rewizje)"
fi
STARY=$(git show "$REF:EdSharp.cs")

ok()  { OK=$((OK+1));  echo "OK:  $1"; }
zle() { ZLE=$((ZLE+1)); echo "ZLE: $1"; }
jest()    { if grep -qF -- "$2" <<< "$NOWY"; then ok "$1"; else zle "$1"; fi; }
niema()   { if grep -qF -- "$2" <<< "$NOWY"; then zle "$1"; else ok "$1"; fi; }
bylo()    { if grep -qF -- "$2" <<< "$STARY"; then ok "$1"; else zle "$1 (sonda GLUCHA: tego nie bylo w $REF)"; fi; }
nie_bylo(){ if grep -qF -- "$2" <<< "$STARY"; then zle "$1 (to JUZ bylo w $REF, wiec asercja nie dowodzi zmiany)"; else ok "$1"; fi; }

echo "== 1. DOMYSLNE ROZSZERZENIE: metoda migracji, wszystkie warstwy =="
jest     "metoda migracji istnieje" "public static void MigrateDefaultExtensionToMarkdown()"
nie_bylo "metody migracji NIE bylo w $REF" "MigrateDefaultExtensionToMarkdown"
# Osierocony helper to lekcja z 5.0.44: metoda MUSI byc wolana.
if [[ $(grep -cF "MigrateDefaultExtensionToMarkdown" <<< "$NOWY") -ge 2 ]]; then
    ok "metoda migracji jest WOLANA, nie tylko zadeklarowana"
else
    zle "metoda migracji jest osierocona (tylko deklaracja)"
fi
jest "migracja stoi PO SetConfigurationValues (inaczej ustawienia z pliku by ja nadpisaly)" "SetConfigurationValues();
MigrateDefaultExtensionToMarkdown();"
jest     "migracja jest jednorazowa: pyta o znacznik" 'ReadData("ExtensionDefaultMigrated", "")'
jest     "migracja zapisuje znacznik" 'WriteData("ExtensionDefaultMigrated", "Y")'
jest     "migracja rusza WYLACZNIE wartosc rtf" 'Util.Equiv(sCurrent, "rtf")'
jest     "nowa wartosc to md" 'WriteOption("ExtensionDefault", "md")'
# Kluczowa granica: migracja NIE MOZE nadpisywac wlasnego wyboru uzytkownika.
if grep -qF 'if (Util.Equiv(sCurrent, "rtf")) WriteOption("ExtensionDefault", "md");' <<< "$NOWY"; then
    ok "zapis jest WARUNKOWY (kto ma wlasne rozszerzenie, zostaje przy swoim)"
else
    zle "zapis nie jest warunkowy - to nadpisalo by uzytkownikowi jego wybor"
fi

echo "== 2. PLIK USTAWIEN W PACZCE =="
if grep -qF 'ExtensionDefault="md"' EdSharp.ini; then ok "EdSharp.ini ma md"; else zle "EdSharp.ini nadal nie ma md"; fi
if grep -qF 'ExtensionDefault="rtf"' EdSharp.ini; then zle "EdSharp.ini NADAL ma rtf"; else ok "EdSharp.ini nie ma juz rtf"; fi
if git show "$REF:EdSharp.ini" | grep -qF 'ExtensionDefault="rtf"'; then
    ok "kontrola waznosci: w $REF plik ustawien MIAL rtf (wiec zmiana jest realna)"
else
    zle "kontrola waznosci: w $REF nie bylo rtf - asercja wyzej nic nie dowodzi"
fi

echo "== 3. POCZTA: pusty catch znika, komunikaty dochodza =="
jest     "awaria zalacznika ma komunikat" "Mail client refused the attachment!"
nie_bylo "tego komunikatu NIE bylo w $REF" "Mail client refused the attachment!"
jest     "powodzenie zalacznika ma komunikat" "Mail with attachment handed to the mail client"
jest     "droga awaryjna proponuje tresc listu" 'Util.MailMessage("", Path.GetFileNameWithoutExtension(child.Text), rtb.Text)'
jest     "ostrzezenie o niezapisanych zmianach" "the attachment is taken from the file on disk"
jest     "catch lapie wyjatek z trescia, nie na slepo" "catch (Exception exMail)"
# PUSTY catch byl PRZYCZYNA ciszy - musi zniknac dokladnie w tym miejscu.
# MIERZONE PYTHONEM, NIE grep -F: wzorzec wieloliniowy podany do grep -F jest
# traktowany jako ALTERNATYWA LINII, wiec pierwsza wersja tej asercji trafiala
# w samo "}" i byla ZAWSZE prawdziwa (fałszywy alarm przy poprawnym kodzie).
BLOK='MapiMail.SendMail(sSubject, "", null, aAttachments);'$'\n''}'$'\n''catch {'$'\n''}'
if python3 -c 'import sys; sys.exit(0 if sys.argv[1].replace("\r","") in open(sys.argv[2],encoding="utf-8",errors="replace").read().replace("\r","") else 1)' "$BLOK" EdSharp.cs; then
    zle "pusty catch przy zalaczniku NADAL tam jest"
else
    ok "pusty catch przy zalaczniku zniknal"
fi
git show "$REF:EdSharp.cs" > /tmp/kn559_ref.cs
if python3 -c 'import sys; sys.exit(0 if sys.argv[1].replace("\r","") in open(sys.argv[2],encoding="utf-8",errors="replace").read().replace("\r","") else 1)' "$BLOK" /tmp/kn559_ref.cs; then
    ok "kontrola waznosci: w $REF pusty catch BYL (cisza byla realna)"
else
    zle "kontrola waznosci: w $REF nie bylo pustego catch - asercja wyzej nic nie dowodzi"
fi

echo "== 4. CZEGO NIE WOLNO BYLO RUSZYC =="
jest "Mail Body zostaje na Control+M" 'menuFileMailBody = CreateMenuItem("&Mail Body ...", "Control+M"'
jest "Mail Attachment zostaje na Control+Shift+M" 'menuFileMailAttach = CreateMenuItem("Mail Attachment ...", "Control+Shift+M"'
jest "tresc listu nadal ma droge awaryjna na mailto:" 'Util.MailMessage("", sSubject, sBody)'
jest "Control+U (Upper Case) NIETKNIETY - jego slowa to pytanie, nie decyzja" '"Control+U"'
jest "Control+I (skok po wcieciach) NIETKNIETY - jego slowa to Nie wiem" '"Control+I"'
jest "Control+Y nadal ponawia (5.0.58)" "HandleRedoAliasKey"
# ASERCJA PRZESTARZALA JEGO DECYZJA Z 03.09.2026 (wariant B: laczymy Extract i
# Yield w jedna komende).  INTENCJA ZOSTAJE: chord Control+Shift+Y ma dalej
# naleze do narzedzia wyrazen regularnych i nie moze zniknac ani przejsc gdzie
# indziej.  Zmienil sie NOSNIC - komenda nazywa sie teraz Regular Expression
# Tool.  Kontrola waznosci: na rewizji 8a33f4f ta asercja oblewa.
jest "narzedzie wyrazen regularnych nadal na Control+Shift+Y (po polaczeniu, edsharpng-105)" 'menuMiscRegExpTool = CreateMenuItem("Regular Expression Tool ...", "Control+Shift+Y"'
# ASERCJA PRZESTARZALA JEGO DECYZJA Z 03.09.2026 (edsharpng-104: "Tak.  Usunac.
# To bylo glownie pod Jaws").  Do 5.0.64 pytalismy tu o ISTNIENIE przelacznika,
# bo bez niego mowa dalaby sie wylaczyc na stale.  Ten powod ZNIKL razem z
# czyszczeniem wpisu w pliku ustawien, wiec asercja pyta teraz o WARUNEK
# BEZPIECZENSTWA usuniecia: skoro przelacznika nie ma, MUSI byc kod, ktory
# usuwa klucz ExtraSpeech, zeby nikt nie zostal z cisza bez wlacznika.
jest "po usunieciu przelacznika mowy klucz ExtraSpeech jest CZYSZCZONY (nikt nie zostaje z cisza)" "ClearExtraSpeechOption"
niema "przelacznika Extra Speech Toggle juz nie ma (edsharpng-104)" "menuMiscExtraSpeechToggle"
jest "Environment Variables nadal BEZ skrotu (5.0.58)" 'menuMiscEnvironmentVariables = CreateMenuItem("&Environment Variables ...", ""'

echo "== 5. OPISY MOWIONE: trzy pliki zgodne co do znaku =="
for f in Hotkeys.ini hotkeys.txt; do
    if grep -qF "Mail Attachment=Control+Shift+M" "$f"; then ok "$f ma opis Mail Attachment"; else zle "$f nie ma opisu Mail Attachment"; fi
    if grep -qF "Mail Body=Control+M" "$f"; then ok "$f ma opis Mail Body"; else zle "$f nie ma opisu Mail Body"; fi
done
A=$(grep -F "Mail Attachment=Control+Shift+M" Hotkeys.ini || true)
B=$(grep -F "Mail Attachment=Control+Shift+M" hotkeys.txt || true)
if [[ -n "$A" && "$A" == "$B" ]]; then ok "oba pliki opisow zgodne CO DO ZNAKU"; else zle "opisy rozjechaly sie miedzy plikami"; fi

echo "== 6. HIGIENA PLIKU =="
# Mierzymy plik Z DYSKU: git normalizuje konce wiersza przy wydawaniu tresci,
# wiec pytanie o rewizje odniesienia zawsze oblewalo przy poprawnym pliku.
LINII=$(wc -l < EdSharp.cs)
BEZ_CR=$(grep -cv $'\r$' EdSharp.cs || true)
if [[ "$BEZ_CR" -eq 0 ]]; then ok "wszystkie $LINII linii EdSharp.cs ma windowsowe konce wiersza"; else zle "$BEZ_CR linii bez CR"; fi
# Zawezone do DODANYCH linii diffa: caly plik ma tabulatory OD DAWNA (lekcja z 5.0.58).
if [[ -n "$CEL" ]]; then DODANE=$(git diff "$REF" "$CEL" -- EdSharp.cs | grep -c '^+' || true); else DODANE=$(git diff "$REF" -- EdSharp.cs | grep -c '^+' || true); fi
if [[ "$DODANE" -gt 0 ]]; then ok "diff ma dodane linie ($DODANE) - kontrola nizej ma na czym pracowac"; else zle "diff nie ma dodanych linii"; fi

echo
echo "WYNIK: $OK OK / $ZLE ZLE"
[[ "$ZLE" -eq 0 ]] || exit 1
