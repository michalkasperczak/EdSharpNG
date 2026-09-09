#!/usr/bin/env bash
# Kontrola negatywna 5.0.57: KOPIOWANIE ZAZNACZENIA Z FORMATOWANIEM pod
# Control+Shift+C (zlecenie Kasperczaka 1788204748663-2, czesc o "kopiowaniu
# formatowania", w iteracjach 1 i 2 swiadomie nie podjeta).
#
# PO CO: "widze nowa metode" nie dowodzi niczego, dopoki ta sama sonda nie
# ZOBACZY, ze w kodzie sprzed zmiany jej NIE BYLO.  Rewizja odniesienia jest
# podana JAWNIE - skrypt biorący HEAD po commicie porownywalby kod z samym soba.
#
# Uzycie: testy/kontrola_negatywna_557.sh [rewizja_odniesienia] [rewizja_mierzona]
#
# TA KONTROLA PILNUJE WERSJI HISTORYCZNEJ.  Czesc jej asercji opisuje zachowanie,
# ktore Kasperczak POZNIEJ kazal zmienic - wtedy oblanie na HEAD jest POPRAWNYM
# wynikiem sondy, a nie regresja programu.  Zeby kontrola dalej dawala sygnal,
# podaj DRUGI argument: rewizje, ktorej ma dotyczyc pomiar.  Bez niego mierzone
# jest drzewo robocze i skrypt wypisuje ostrzezenie.
set -uo pipefail
# UWAGA na pipefail: `grep ... | grep -q ...` zwraca blad przez SIGPIPE.
# Kazdy taki lancuch idzie przez plik tymczasowy, nie przez potok.

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
ODNIESIENIE="${1:-c104d26}"   # 5.0.56, stan PRZED kopiowaniem zaznaczenia
PRZED="/tmp/kn557_przed_EdSharp.cs"
PRZED_MD="/tmp/kn557_przed_EdSharp.md"
PRZED_INI="/tmp/kn557_przed_Hotkeys.ini"
PRZED_TXT="/tmp/kn557_przed_hotkeys.txt"
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
    ROBOCZY="/tmp/kn557_mierzona"
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
    echo "UWAGA: mierze DRZEWO ROBOCZE.  Ta kontrola pilnuje wersji 5.0.57;"
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
ile() {
    # ile: <wzorzec> <opis> <oczekiwana liczba> [plik]
    local plik="${4:-EdSharp.cs}"
    local n
    n=$(grep -cF -- "$1" "$plik" || true)
    if [ "$n" = "$3" ]; then zdaj "$2 (=$3)"; else oblej "$2 (jest $n, ma byc $3)"; fi
}

echo "== Rewizja odniesienia: $ODNIESIENIE =="
echo
echo "-- 1. NOWE METODY SA, I W ODNIESIENIU ICH NIE BYLO --"
# Pytamy o PELNY NAGLOWEK metody, nie o slowo "private": asercja, ktora nie
# moze oblac, nie jest dowodem (zlapane przy pisaniu tej kontroli).
naglowek() {
    # naglowek: <wzorzec naglowka> <nazwa metody>
    jest_teraz "$1" "metoda $2 zdefiniowana w HEAD"
    nie_bylo "$1" "  definicji $2 NIE bylo w $ODNIESIENIE"
}
naglowek "private bool TryCopyMarkdownSelection(MdiChild child) {" "TryCopyMarkdownSelection"
naglowek "private static string BuildRtfFromMarkdownLines(string[] aLines) {" "BuildRtfFromMarkdownLines"
naglowek "private static string RtfEncodeMarkdownInline(string sText) {" "RtfEncodeMarkdownInline"
naglowek "private static string RtfEncodeMarkdownSpan(string sText) {" "RtfEncodeMarkdownSpan"
naglowek "private static bool SelectionHasMarkdownMarkup(string[] aLines) {" "SelectionHasMarkdownMarkup"
naglowek "private static string[] SplitTextLines(string sText) {" "SplitTextLines"
# KONTROLA, ZE 'nie_bylo' NIE JEST GLUCHE: metoda, ktora w odniesieniu BYLA.
bylo "private bool TryCopyMarkdownRichLine(MdiChild child) {" "KONTROLA: sonda widzi w $ODNIESIENIE metode, ktora tam byla"

echo
echo "-- 2. KAZDA METODA JEST WOLANA (osierocony helper to lekcja z 5.0.44) --"
# Sama definicja nie wystarcza: metoda bez wolajacego to martwy kod, ktory
# zielony build przepuszcza.
jest_teraz "if (!TryCopyMarkdownSelection(this.Child)" "TryCopyMarkdownSelection WOLANA z handlera Copy Rich Text"
jest_teraz "BuildRtfFromMarkdownLines(aLines)" "generator RTF WOLANY"
jest_teraz "SelectionHasMarkdownMarkup(aLines)" "bramka znacznikow WOLANA"
jest_teraz "SplitTextLines(sSelected)" "podzial na wiersze WOLANY"
jest_teraz "RtfEncodeMarkdownInline(sHeading)" "koder wiersza WOLANY dla naglowka"
jest_teraz "RtfEncodeMarkdownInline(sItem)" "koder wiersza WOLANY dla pozycji listy"
jest_teraz "RtfEncodeMarkdownSpan(sText.Substring(iPos))" "koder kawalka WOLANY z kodera wiersza"

echo
echo "-- 3. KOLEJNOSC SCIEZEK: nowa PRZED dotychczasowymi --"
# Gdyby nowa sciezka szla PO TryCopyMarkdownRichLine, to przy zaznaczeniu
# jednowierszowym nic by sie nie zmienilo, ale przy wielowierszowym rowniez nie:
# RichLine zwraca false, ale List moze przejac pierwszy wiersz listy i skopiowac
# TYLKO JA.  Dlatego kolejnosc jest czescia zachowania, nie kosmetyka.
LINIA=$(grep -n "if (!TryCopyMarkdownSelection" EdSharp.cs | head -1 | cut -d: -f1)
if [ -n "$LINIA" ]; then
    if sed -n "${LINIA}p" EdSharp.cs | grep -qF "!TryCopyMarkdownSelection(this.Child) && !TryCopyMarkdownLinkAsRichText"; then
        zdaj "nowa sciezka jest PIERWSZA w lancuchu warunkow"
    else
        oblej "nowa sciezka NIE jest pierwsza w lancuchu"
    fi
else
    oblej "nie znalazlem lancucha warunkow Copy Rich Text"
fi
bylo "if (!TryCopyMarkdownLinkAsRichText(this.Child) && !TryCopyMarkdownList(rtb)" "sonda widzi STARY lancuch w $ODNIESIENIE"

echo
echo "-- 4. BRAMKI, KTORE MAJA ZOSTAC --"
jest_teraz "if (IsRichTextFile(child)) return false;" "plik RTF idzie stara droga (formatowanie jest tam prawdziwe)"
jest_teraz "if (rtb.SelectionLength <= 0) return false;" "bez zaznaczenia nie przejmujemy"
jest_teraz "if (aLines.Length < 2) return false;" "jeden wiersz idzie stara droga"
jest_teraz "if (!SelectionHasMarkdownMarkup(aLines)) return false;" "czysty tekst idzie stara droga"

echo
echo "-- 5. DOTYCHCZASOWE SCIEZKI NIETKNIETE (brak regresji) --"
# Kasperczak uzywa Control+Shift+C od dawna na pojedynczym odsylaczu i liscie.
# Ta wersja NIE MOZE tego zmienic.
jest_teraz "private bool TryCopyMarkdownLinkAsRichText(MdiChild child) {" "kopiowanie pojedynczego odsylacza nietkniete"
jest_teraz "private static bool TryCopyMarkdownList(HomerRichTextBox rtb) {" "kopiowanie pojedynczej listy nietkniete"
jest_teraz "private bool TryCopyMarkdownRichLine(MdiChild child) {" "kopiowanie pojedynczego wiersza nietkniete"
jest_teraz 'AddMessage("Link copied")' "komunikat Link copied zostaje"
jest_teraz 'AddMessage("List copied")' "komunikat List copied zostaje"
jest_teraz 'AddMessage("Heading copied")' "komunikat Heading copied zostaje"
jest_teraz "if (rtb.SelectionLength != 0) return false;" "RichLine nadal odmawia przy zaznaczeniu"

echo
echo "-- 6. NOWY KOMUNIKAT: JEST, I NIE BYLO --"
jest_teraz 'AddMessage("Selection copied")' "nowy komunikat Selection copied"
nie_bylo 'Selection copied' "komunikatu Selection copied NIE bylo w $ODNIESIENIE"

echo
echo "-- 7. BLOK KODU DOSLOWNY, TABLICA LIST PELNA --"
jest_teraz 'sTrim.StartsWith("```") || sTrim.StartsWith("~~~")' "fence rozpoznawany w generatorze"
jest_teraz 'listtemplateid1' "definicja listy punktowanej w tablicy list"
jest_teraz 'listtemplateid2' "definicja listy numerowanej w tablicy list"
jest_teraz 'listoverride\listid2' "przypisanie \\ls2 dla listy numerowanej"

echo
echo "-- 8. ZADNA KOMENDA ANI CHORD NIE ZMIENIL SIE --"
# Ta wersja NIE dotyka przypisan klawiszy.  Sam zbior chordow bylby na to slepy
# (lekcja z 5.0.55), wiec porownujemy MAPE chord->komenda.
mapa() {
    local zrodlo="$1"
    grep -oE 'CreateMenuItem\("[^"]*", "[^"]*"' "$zrodlo" \
      | sed -E 's/CreateMenuItem\("([^"]*)", "([^"]*)"/\2|\1/' \
      | sed 's/&//g' | sort
}
mapa "$PRZED" > /tmp/kn557_mapa_przed.txt
mapa "EdSharp.cs" > /tmp/kn557_mapa_teraz.txt
if diff -q /tmp/kn557_mapa_przed.txt /tmp/kn557_mapa_teraz.txt > /dev/null; then
    zdaj "mapa chord->komenda IDENTYCZNA (ta wersja nie rusza skrotow)"
else
    oblej "mapa chord->komenda ZMIENILA SIE: $(diff /tmp/kn557_mapa_przed.txt /tmp/kn557_mapa_teraz.txt | head -4 | tr '\n' ' ')"
fi
# KONTROLA, ZE SONDA MAPY NIE JEST GLUCHA: musi widziec konkretna pare.
if grep -qF 'Control+Shift+C|Copy Rich Text' /tmp/kn557_mapa_teraz.txt; then
    zdaj "KONTROLA: sonda mapy widzi pare Control+Shift+C -> Copy Rich Text"
else
    oblej "sonda mapy GLUCHA: nie widzi Copy Rich Text"
fi
LICZ=$(wc -l < /tmp/kn557_mapa_teraz.txt)
if [ "$LICZ" -gt 200 ]; then zdaj "sonda mapy widzi $LICZ komend (>200)"; else oblej "sonda mapy widzi tylko $LICZ komend"; fi

echo
echo "-- 9. OPISY MOWIONE: OBA PLIKI ZGODNE CO DO ZNAKU --"
# Sekcja z 5.0.56: hotkeys.txt rozjechal sie z Hotkeys.ini na lata i nikt tego
# nie zauwazyl.  Ctrl+F1 czyta jeden z tych plikow, wiec musza byc rowne.
if diff <(sed 's/\r//' Hotkeys.ini | tail -n +2) <(sed 's/\r//' hotkeys.txt | tail -n +4) > /dev/null; then
    zdaj "Hotkeys.ini i hotkeys.txt zgodne co do znaku"
else
    oblej "ROZJAZD opisow: $(diff <(sed 's/\r//' Hotkeys.ini | tail -n +2) <(sed 's/\r//' hotkeys.txt | tail -n +4) | head -4 | tr '\n' ' ')"
fi
jest_teraz "Copy Rich Text=Control+Shift+C" "opis mowiony Copy Rich Text jest w Hotkeys.ini" "Hotkeys.ini"
jest_teraz "Copy Rich Text=Control+Shift+C" "opis mowiony Copy Rich Text jest w hotkeys.txt" "hotkeys.txt"

echo
echo "-- 10. KONCE WIERSZA CRLF NIETKNIETE --"
# Edycja pliku repo przez Pythona zamienia CRLF na LF w calym pliku, a git diff
# to MASKUJE przez autocrlf.  Ta wersja byla pisana wlasnie Pythonem.
#
# TEN POMIAR MA SENS TYLKO NA PLIKU Z DYSKU.  Gdy mierzymy rewizje (drugi
# argument), tresc pochodzi z `git show`, ktory sam normalizuje konce wiersza do
# LF - wiec asercja oblewala przy pliku, ktory na dysku byl poprawny.  To byla
# wina sondy, nie kodu: git nie oddaje bajtow z drzewa roboczego.
if [ -n "$MIERZONA" ]; then
    zdaj "CRLF pomijam: rewizja z git show jest znormalizowana do LF (nie ma czego mierzyc)"
else
    LINII=$(wc -l < EdSharp.cs)
    ZCR=$(grep -c $'\r$' EdSharp.cs || true)
    if [ "$LINII" = "$ZCR" ]; then zdaj "wszystkie $LINII linii EdSharp.cs maja CRLF"; else oblej "CRLF ZGUBIONE: $ZCR z $LINII linii"; fi
fi

echo
echo "WYNIK: $OK OK / $ZLE ZLE"
[ "$ZLE" -eq 0 ] || exit 1
