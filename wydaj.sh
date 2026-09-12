#!/bin/bash
# Wydaje gotowa paczke EdSharpNG: commit, push, wydanie na GitHubie, kopia u
# Michala - i ZAWSZE z suma SHA-256 w opisie wydania.
#
# PO CO TEN PLIK ISTNIEJE.  Sam program przy aktualizacji (F11) liczy sume
# pobranego instalatora i porownuje ja z suma podana w opisie wydania.  Gdy sumy
# w opisie NIE MA, program nie moze nic sprawdzic i pyta uzytkownika "Release
# vX does not publish a checksum... Run it anyway?".
#
# Tak sie stalo przy 5.0.82 i 5.0.83: sume dopisywalem RECZNIE w poleceniu
# wydania i po prostu jej nie dopisalem.  Michal zobaczyl to pytanie przy
# instalacji.  Zabezpieczenie, ktore zalezy od tego, czy ktos pamietal wkleic
# 64 znaki, nie jest zabezpieczeniem - dlatego suma jest tu liczona z PLIKU,
# ktory naprawde idzie do wydania, i wstawiana bez udzialu czlowieka.
#
# Skrypt ODMAWIA wydania, gdy po opublikowaniu sumy nie widac w opisie - lepiej
# brak wydania niz wydanie, ktore u uzytkownika wyskoczy ostrzezeniem.
#
# UZYCIE:  bash wydaj.sh 5.0.84 "opis zmian dla commita i wydania"
set -u

REPO="/home/michal/projekty/edsharp"
WERSJA="${1:-}"
OPIS="${2:-}"

if [ -z "$WERSJA" ] || [ -z "$OPIS" ]; then
    echo "BLAD: uzycie: bash wydaj.sh 5.0.84 \"opis zmian\""
    exit 2
fi

PACZKA="$REPO/dist/EdSharpNG_Setup_$WERSJA.exe"
if [ ! -f "$PACZKA" ]; then
    echo "BLAD: nie ma paczki $PACZKA - najpierw: bash zbuduj.sh $WERSJA"
    exit 3
fi

# Numer w programie musi zgadzac sie z numerem paczki - ta sama kontrola co w
# zbuduj.sh, bo wydac mozna takze bez swiezego budowania.
if ! grep -q "VersionString = \"$WERSJA\"" "$REPO/EdSharp.cs"; then
    echo "BLAD: EdSharp.cs nie ma VersionString = \"$WERSJA\"."
    exit 4
fi

SUMA=$(sha256sum "$PACZKA" | cut -d' ' -f1 | tr 'a-f' 'A-F')
if [ ${#SUMA} -ne 64 ]; then
    echo "BLAD: nie policzylem sumy paczki."
    exit 5
fi

cd "$REPO" || exit 6

echo "== 1/4 commit i push"
git add -A
git commit -q -m "$WERSJA: $OPIS" || echo "   (nic nowego do zapisania - jade dalej)"
git push -q origin master || exit 7

echo "== 2/4 wydanie v$WERSJA na GitHubie"
# Suma w osobnej, ostatniej linii.  Program szuka w opisie 64 znakow szesnastkowych
# (regex \b[0-9a-fA-F]{64}\b), wiec wazne jest tylko to, zeby tam BYLA.
NOTATKA="$OPIS

SHA-256 instalatora: $SUMA"
if gh release view "v$WERSJA" >/dev/null 2>&1; then
    echo "   wydanie juz istnieje - poprawiam opis i podmieniam plik"
    gh release edit "v$WERSJA" --notes "$NOTATKA" >/dev/null || exit 8
    gh release upload "v$WERSJA" "$PACZKA" --clobber >/dev/null || exit 8
else
    gh release create "v$WERSJA" "$PACZKA" --title "EdSharpNG $WERSJA" --notes "$NOTATKA" >/dev/null || exit 8
fi

echo "== 3/4 sprawdzam, czy suma NAPRAWDE jest w opisie wydania"
# To jest cala wartosc tego skryptu: nie wierzymy, ze sie udalo - patrzymy.
WIDZIANA=$(gh release view "v$WERSJA" --json body -q .body | grep -ioE '\b[0-9a-fA-F]{64}\b' | head -1 | tr 'a-f' 'A-F')
if [ "$WIDZIANA" != "$SUMA" ]; then
    echo "BLAD: w opisie wydania nie ma poprawnej sumy."
    echo "      paczka:  $SUMA"
    echo "      wydanie: ${WIDZIANA:-brak}"
    echo "      Program pokazalby uzytkownikowi ostrzezenie przy aktualizacji."
    exit 9
fi
echo "   OK: $SUMA"

echo "== 4/4 kopia u Michala i sprawdzenie na jego komputerze"
# Sciezka docelowa ma spacje, a scp na taka sciezke zawodzi przy kazdym sposobie
# cytowania - dlatego plik ladzie najpierw w C:\ pod nazwa bez spacji, a potem
# Windows przenosi go na miejsce.
scp -q "$PACZKA" michal-glowny:"C:/Temp_EdSharp_$WERSJA.exe" || exit 10
ssh michal-glowny "powershell -NoProfile -Command \"Move-Item -Force C:\\Temp_EdSharp_$WERSJA.exe \\\"D:\\Projekty Codex\\Hermes\\EdSharpNG_Setup_$WERSJA.exe\\\"; \$f=\\\"D:\\Projekty Codex\\Hermes\\EdSharpNG_Setup_$WERSJA.exe\\\"; (Get-Item \$f).Length; (Get-FileHash \$f -Algorithm SHA256).Hash; (Get-Item \$f).VersionInfo.FileVersion; (Get-Item \$f).VersionInfo.CompanyName\"" || exit 11

echo "WYDANE: v$WERSJA"
echo "D:\\Projekty Codex\\Hermes\\EdSharpNG_Setup_$WERSJA.exe"
