#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.47.
#
# Dwie zmiany na jego polecenia z 30.08.2026:
#   A) skok przypisu znacznik-tresc-znacznik: Alt+F6 -> Control+Alt+K
#      ("Skok przypis tekst tekst przypis robimy Alt-CTRL-ka (...) Tak z ego F6
#      w przypisach bysmy rezygnowali"),
#   B) nagrywanie plyt (Alt+Shift+B) USUNIETE
#      ("ALT+SHIFT+B nagrywanie plikow to na pewno usun").
#
# DLACZEGO TEN SKRYPT ISTNIEJE: "nie widze komendy X" nie dowodzi niczego,
# dopoki ta sama sonda nie ZOBACZY jej w kodzie SPRZED zmiany.  Bez tego mierzy
# sie glucha sonde.  Rewizja odniesienia jest podana JAWNIE w argumencie, bo
# skrypt bioracy HEAD po commicie porownuje kod z samym soba i wszystko "pada".
#
# NAJWAZNIEJSZA CZESC to jednak nie same chordy, a MOWA: Util.Say ma twardy
# warunek tlumiacy kazda wypowiedz przy trzymanym Control+Alt (l.17711).
# Komenda przeniesiona na Control+Alt+K bez trybu globalnego WYKONALA BY skok,
# ale niewidomy nie uslyszalby NICZEGO.  Zielony build takiej wady NIE odrzuci,
# wiec pilnuja jej osobne asercje.
set -u
REPO="/mnt/d/projekty/edsharp-pr"
REV="${1:-8e5ac17}"
cd "$REPO" || exit 2

PASS=0; FAIL=0
ok()   { PASS=$((PASS+1)); printf 'PASS  %s\n' "$1"; }
bad()  { FAIL=$((FAIL+1)); printf 'FAIL  %s\n' "$1"; }
chk()  { if [ "$2" -eq "$3" ]; then ok "$1 (=$3)"; else bad "$1 (jest $2, oczekiwano $3)"; fi; }

STARY_CS="$(mktemp)";  git show "$REV:EdSharp.cs"  > "$STARY_CS"  2>/dev/null
STARY_INI="$(mktemp)"; git show "$REV:Hotkeys.ini" > "$STARY_INI" 2>/dev/null
STARY_TXT="$(mktemp)"; git show "$REV:hotkeys.txt" > "$STARY_TXT" 2>/dev/null
STARY_MD="$(mktemp)";  git show "$REV:EdSharp.md"  > "$STARY_MD"  2>/dev/null
if [ ! -s "$STARY_CS" ]; then echo "BLAD: nie mam rewizji $REV"; exit 2; fi

echo "=== rewizja odniesienia: $REV ==="
echo
echo "--- CZESC 1: STARY kod MIAL to, co usuwamy (dowod, ze sonda widzi) ---"
c() { grep -c "$1" "$2" 2>/dev/null || true; }

# A) skok przypisu byl na Alt+F6
chk "stary kod: skok przypisu na Alt+F6 w tablicy skrotow" \
    "$(c '"Go to Footnote", "Alt+F6"' "$STARY_CS")" 1
chk "stary opis mowiony (Hotkeys.ini) podawal Alt+F6" \
    "$(c '^Go to Footnote=Alt+F6,' "$STARY_INI")" 1
chk "stary opis mowiony (hotkeys.txt) podawal Alt+F6" \
    "$(c '^Go to Footnote=Alt+F6,' "$STARY_TXT")" 1
chk "stary podrecznik podawal Alt+F6 w spisie skrotow" \
    "$(c '^Go to Footnote=Alt+F6,' "$STARY_MD")" 1
chk "stary kod NIE mial jeszcze Control+Alt+K" \
    "$(c '"Control+Alt+K"' "$STARY_CS")" 0

# B) nagrywanie plyt bylo na czterech warstwach + opisach
chk "stary kod: pole klasy menuMiscBurnToCD" \
    "$(grep -c 'menuMiscCommandPrompt, menuMiscBurnToCD, menuMiscWebDownload' "$STARY_CS")" 2
chk "stary kod: pozycja menu Burn to CD z chordem Alt+Shift+B" \
    "$(c '"Burn to CD", "Alt+Shift+B"' "$STARY_CS")" 1
chk "stary kod: handler if (menuItem == menuMiscBurnToCD)" \
    "$(c 'menuItem == menuMiscBurnToCD' "$STARY_CS")" 1
chk "stary kod: metoda BurnToCD" \
    "$(c 'public void BurnToCD()' "$STARY_CS")" 1
chk "stary kod: helper GetPathsFromDocument" \
    "$(c 'public string\[\] GetPathsFromDocument()' "$STARY_CS")" 1
chk "stary opis mowiony (Hotkeys.ini) obiecywal Burn to CD" \
    "$(c '^Burn to CD=Alt+Shift+B,' "$STARY_INI")" 1
chk "stary opis mowiony (hotkeys.txt) obiecywal Burn to CD" \
    "$(c '^Burn to CD=Alt+Shift+B,' "$STARY_TXT")" 1
chk "stary podrecznik mial PROZE o nagrywaniu plyt" \
    "$(c 'The Burn to CD command, Alt+Shift+B, operates on a path list' "$STARY_MD")" 1
chk "stary podrecznik mial zdanie o DVD" \
    "$(c 'At this time, DVDs do not work' "$STARY_MD")" 1

# MOWA: stary kod wolal tryb ZWYKLY, bo chord nie mial Control+Alt
# DRUGA PULAPKA TEJ SAMEJ SONDY: komunikat "No footnotes!" wystepuje w kodzie
# DWA razy, bo maja go DWIE komendy - skok kontekstowy i lista przypisow.
# Asercja na 1 padala przy dobrym kodzie.  Wykorzystuje to jako KONTROLE
# ROZNICUJACA: po zmianie jeden z nich MUSI miec tryb globalny (skok, bo ma
# Control+Alt), a drugi MUSI zostac bez niego (lista na Alt+K).  Gdyby oba
# dostaly tryb globalny, zdjalbym po cichu sprawdzenie aktywnosci okna komendzie,
# ktora go potrzebuje.
chk "stary kod: OBA komunikaty 'No footnotes!' szly trybem zwyklym" \
    "$(c 'AddMessage("No footnotes!");' "$STARY_CS")" 2
chk "stary kod: podpowiedz Ctrl+F1 szla przez SetMessage bez trybu globalnego" \
    "$(c 'SetMessage(aSummary\[0\]);' "$STARY_CS")" 1

echo
echo "--- CZESC 2: NOWY kod tego NIE MA (a ma to, co ma miec) ---"
chk "skok przypisu jest na Control+Alt+K" \
    "$(c '"Go to Footnote", "Control+Alt+K"' EdSharp.cs)" 1
chk "chord Alt+F6 nie nalezy do ZADNEJ komendy" \
    "$(grep -c '"Alt+F6"' EdSharp.cs)" 0
chk "opis mowiony (Hotkeys.ini) podaje Control+Alt+K" \
    "$(c '^Go to Footnote=Control+Alt+K,' Hotkeys.ini)" 1
chk "opis mowiony (hotkeys.txt) podaje Control+Alt+K" \
    "$(c '^Go to Footnote=Control+Alt+K,' hotkeys.txt)" 1
chk "podrecznik podaje Control+Alt+K w spisie skrotow" \
    "$(c '^Go to Footnote=Control+Alt+K,' EdSharp.md)" 1

chk "pole klasy menuMiscBurnToCD zniknelo" \
    "$(c 'menuMiscBurnToCD' EdSharp.cs)" 0
chk "pozycja menu Burn to CD zniknela" \
    "$(c '"Burn to CD"' EdSharp.cs)" 0
chk "metoda BurnToCD zniknela" \
    "$(c 'public void BurnToCD()' EdSharp.cs)" 0
chk "helper GetPathsFromDocument zniknal (mial JEDNEGO wolajacego)" \
    "$(c 'public string\[\] GetPathsFromDocument()' EdSharp.cs)" 0
chk "opis mowiony Burn to CD zniknal z Hotkeys.ini" \
    "$(c '^Burn to CD=' Hotkeys.ini)" 0
chk "opis mowiony Burn to CD zniknal z hotkeys.txt" \
    "$(c '^Burn to CD=' hotkeys.txt)" 0
chk "podrecznik nie obiecuje juz nagrywania plyt" \
    "$(c 'operates on a path list' EdSharp.md)" 0
chk "podrecznik nie ma juz zdania o DVD" \
    "$(c 'At this time, DVDs do not work' EdSharp.md)" 0
chk "chord Alt+Shift+B nie nalezy do ZADNEJ komendy" \
    "$(grep -c '"Alt+Shift+B"' EdSharp.cs)" 0

echo
echo "--- CZESC 3: MOWA przy Control+Alt (tego build NIE odrzuci) ---"
chk "skok przypisu: 'No footnotes!' idzie trybem GLOBALNYM" \
    "$(c 'AddMessage("No footnotes!", true);' EdSharp.cs)" 1
chk "KONTROLA ROZNICUJACA: lista przypisow ZOSTAJE bez trybu globalnego" \
    "$(c 'AddMessage("No footnotes!");' EdSharp.cs)" 1
chk "skok przypisu: brak znacznika idzie trybem GLOBALNYM" \
    "$(c 'has no marker in the text!", true);' EdSharp.cs)" 1
chk "skok przypisu: brak tresci idzie trybem GLOBALNYM" \
    "$(c 'has no text at the end of the document!", true);' EdSharp.cs)" 1
chk "skok przypisu: podpowiedz o ustawieniu kursora idzie trybem GLOBALNYM" \
    "$(c 'or on the footnote text!", true);' EdSharp.cs)" 1
chk "skok przypisu: tresc przypisu wymawiana trybem GLOBALNYM" \
    "$(c 'Util.Say(defs\[iDef\].Text + ", footnote " + defs\[iDef\].Label, true);' EdSharp.cs)" 1
chk "bramki typu pliku i podgladu wybieraja tryb po WOLAJACYM chordzie" \
    "$(c 'bool bGlobalSpeech = (menuItem == menuMiscGoToFootnote);' EdSharp.cs)" 1
chk "podpowiedz Ctrl+F1 wlacza tryb globalny dla komend Control+Alt" \
    "$(c 'if ((keysDescribe & Keys.Control) != 0 && (keysDescribe & Keys.Alt) != 0) bDescribeGlobal = true;' EdSharp.cs)" 1

echo
echo "--- CZESC 4: KONTROLE, ze NIE zniknelo nic wiecej ---"
# Rodzenstwo przypisow zostaje NIETKNIETE.
chk "KONTROLA: wstawianie przypisu nadal na Control+Shift+K" \
    "$(c '"Insert Footnote ...", "Control+Shift+K"' EdSharp.cs)" 1
chk "KONTROLA: lista przypisow nadal na Alt+K" \
    "$(c '"Footnote List ...", "Alt+K"' EdSharp.cs)" 1
chk "KONTROLA: lista przypisow mowi trybem ZWYKLYM (nie ma Control+Alt)" \
    "$(c 'GoToMarkdownFootnoteRef(rtb, sText, refs\[iRef\], false);' EdSharp.cs)" 1
# Helpery WSPOLNE - najgrozniejsza klasa bledu przy usuwaniu.
chk "KONTROLA: Util.GetExtensions ma nadal innych klientow" \
    "$(c 'Util.GetExtensions' EdSharp.cs)" 4
chk "KONTROLA: Util.GetPathsWithExtensions ma nadal innych klientow" \
    "$(c 'Util.GetPathsWithExtensions' EdSharp.cs)" 4
chk "KONTROLA: Path List (Control+Shift+P) nadal istnieje" \
    "$(c '"Path List", "Control+Shift+P"' EdSharp.cs)" 1
chk "KONTROLA: Web Download nadal na Alt+Shift+W" \
    "$(c '"Web Download", "Alt+Shift+W"' EdSharp.cs)" 1
chk "KONTROLA: podrecznik nadal opisuje Path List" \
    "$([ "$(c 'Control+Shift+P' EdSharp.md)" -ge 1 ] && echo 1 || echo 0)" 1
# Poprzednie wersje nietkniete.
chk "KONTROLA: zakladki nadal Ctrl+B / Alt+B (5.0.45)" \
    "$(c '"Set Bookmar&k", "Control+B"' EdSharp.cs)" 1
chk "KONTROLA: wstawianie linku nadal Control+K (5.0.45)" \
    "$(c '"Insert &Link ...", "Control+K"' EdSharp.cs)" 1
chk "KONTROLA: komentarze nadal w rodzinie F9 (5.0.43)" \
    "$(c '"Insert Comment ...", "Alt+F9"' EdSharp.cs)" 1
chk "KONTROLA: lista komentarzy nadal Control+Alt+F9" \
    "$(c '"Comment List ...", "Control+Alt+F9"' EdSharp.cs)" 1
chk "KONTROLA: kreator tabeli nadal Control+Shift+T (5.0.35)" \
    "$(c '"Control+Shift+T"' EdSharp.cs)" 1
# PULAPKA MOJEJ WLASNEJ SONDY, zlapana na tym przebiegu (30.08.2026): pierwsza
# wersja tych trzech asercji liczyla goly grep po NAZWIE, wiec zliczala takze
# KOMENTARZE i linie zakomentowane.  Dawaly 4, 3 i 3 zamiast oczekiwanych 2, 1
# i 2 - i wygladalo to jak regresja, choc kod byl w porzadku (dopisalem wlasne
# komentarze wyjasniajace, w ktorych te nazwy wystepuja).  Nie oslabiam sondy:
# mierze KONKRETNE linie KODU, ktore musza istniec, zamiast liczyc wystapienia
# nazwy w calym pliku.  To ta sama klasa bledu co "Say Styles" w 5.0.44.
chk "KONTROLA: litery dostepu - kolejka liter jest nadal WOLANA (5.0.42)" \
    "$(grep -c '^\s*assignQueuedAccessKeys();' Lbc.cs)" 1
chk "KONTROLA: litery dostepu - metoda rozdajaca litery nadal ISTNIEJE" \
    "$(grep -c 'private void assignQueuedAccessKeys()' Lbc.cs)" 1
chk "KONTROLA: obsluga JAWS na golym F9 - AKTYWNE wywolanie nietkniete" \
    "$(grep -c '^COM.JFWRunFunction("SayAllTempFile");' EdSharp.cs)" 1
chk "KONTROLA: skok przypisu w PODGLADZIE - metoda nadal ISTNIEJE" \
    "$(c 'private bool TryGoToFootnoteInReview(MdiChild child)' EdSharp.cs)" 1
chk "KONTROLA: skok przypisu w PODGLADZIE - nadal WOLANY z obslugi Enter" \
    "$(c 'if (TryGoToFootnoteInReview(child)) return true;' EdSharp.cs)" 1

echo
echo "--- CZESC 5: BINARKA (grep ASCII po .NET daje zero - pulapka tego repo) ---"
BIN="EdSharpNG.exe"
if [ ! -f "$BIN" ]; then bad "brak binarki $BIN"; else
  if [ "$BIN" -nt EdSharp.cs ]; then ok "binarka jest NOWSZA niz zrodlo"; else bad "binarka STARSZA niz zrodlo - build sie nie odpalil"; fi
  w() { python3 -c "
import sys
raw=open('$BIN','rb').read()
print(raw.count(sys.argv[1].encode('utf-16le')))
" "$1"; }
  chk "binarka NIE ma napisu 'Burn to CD'"        "$(w 'Burn to CD')" 0
  chk "binarka NIE ma napisu 'Burn2CD'"           "$(w 'Burn2CD')" 0
  chk "KONTROLA POZYTYWNA: binarka MA 'Go to Footnote'" "$(w 'Go to Footnote')" 1
  chk "KONTROLA POZYTYWNA: binarka MA 'Web Download'"   "$(w 'Web Download')" 1
  chk "KONTROLA POZYTYWNA: binarka MA 'Path List'"      "$(w 'Path List')" 1
fi

echo
echo "--- CZESC 6: konce wiersza (CRLF w .cs/.ini/.md, LF w .py/.sh) ---"
for f in EdSharp.cs Hotkeys.ini hotkeys.txt EdSharp.md; do
  L=$(wc -l < "$f"); C=$(grep -c $'\r$' "$f"); G=$(python3 -c "
import re;print(len(re.findall(rb'\r(?!\n)',open('$f','rb').read())))")
  if [ "$L" -eq "$C" ] && [ "$G" -eq 0 ]; then ok "$f: CRLF $C/$L, zero golych CR"
  else bad "$f: linie $L, CRLF $C, gole CR $G"; fi
done

rm -f "$STARY_CS" "$STARY_INI" "$STARY_TXT" "$STARY_MD"
echo
echo "WYNIK: $PASS/$((PASS+FAIL))"
[ "$FAIL" -eq 0 ] || exit 1
