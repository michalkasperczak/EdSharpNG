#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.49.
#
# ZMIANA: NAWIGACJA PO WYROZNIENIACH I LISTACH MARKDOWN.
#   Control z ukosnikiem       - nastepne pogrubienie ALBO pochylenie,
#   Control+Shift z ukosnikiem - to samo w tyl,
#   Control z myslnikiem       - poczatek nastepnej listy,
#   Control+Shift z myslnikiem - poczatek poprzedniej listy.
#
# SKAD: rozstrzygniecie Kasperczaka z 30.08.2026 15:41 i 15:42, doslownie
# "Control/pogrubienie i podkreslenie razem, control myslnik, skakanie po
# listach" oraz "Na poczatek kazdej nowej listy, bo po elementach mozna chodzic
# strzalka w dol" (ustalenia edsharpng-45 i edsharpng-46).  Zamyka wahanie,
# ktore poprzednia iteracja slusznie zostawila jako pytanie A/B.
#
# DLACZEGO TEN SKRYPT ISTNIEJE: "widze komende w kodzie" nie dowodzi niczego,
# dopoki ta sama sonda nie ZOBACZY, ze w kodzie SPRZED zmiany jej NIE BYLO.
# Rewizja odniesienia podana JAWNIE, bo skrypt bioracy HEAD po commicie
# porownuje kod z samym soba.
#
# NAJWAZNIEJSZE CZESCI to kontrole POZYTYWNE i kontrola na BINARCE: nowa komenda
# nie moze zabrac ani jednego istniejacego skrotu, a napisow interfejsu .NET
# grep ASCII po pliku .exe NIE WIDZI (znana pulapka tego repo, dlatego kazdy
# pomiar binarki idzie w OBU kodowaniach).
set -u
REPO="/mnt/d/projekty/edsharp-pr"
REV="${1:-ca32eca}"
cd "$REPO" || exit 2

PASS=0; FAIL=0
ok()   { PASS=$((PASS+1)); printf 'PASS  %s\n' "$1"; }
bad()  { FAIL=$((FAIL+1)); printf 'FAIL  %s\n' "$1"; }
chk()  { if [ "$2" -eq "$3" ]; then ok "$1 (=$3)"; else bad "$1 (jest $2, oczekiwano $3)"; fi; }
ge()   { if [ "$2" -ge "$3" ]; then ok "$1 (=$2, co najmniej $3)"; else bad "$1 (jest $2, oczekiwano co najmniej $3)"; fi; }

STARY_CS="$(mktemp)";  git show "$REV:EdSharp.cs"   > "$STARY_CS"  2>/dev/null
STARY_HK="$(mktemp)";  git show "$REV:Hotkeys.ini"  > "$STARY_HK"  2>/dev/null
STARY_MD="$(mktemp)";  git show "$REV:EdSharp.md"   > "$STARY_MD"  2>/dev/null
if [ ! -s "$STARY_CS" ]; then echo "BLAD: nie mam rewizji $REV"; exit 2; fi

echo "=== rewizja odniesienia: $REV ==="
echo
c() { grep -c "$1" "$2" 2>/dev/null || true; }

echo "--- CZESC 1: STARY kod NIE MIAL tych komend (dowod, ze sonda nie jest glucha) ---"
chk "STARY kod: brak pola menuNavigateNextEmphasis"     "$(c 'menuNavigateNextEmphasis' "$STARY_CS")" 0
chk "STARY kod: brak pola menuNavigateNextList"         "$(c 'menuNavigateNextList' "$STARY_CS")" 0
chk "STARY kod: brak metody GoToMarkdownEmphasis"       "$(c 'GoToMarkdownEmphasis' "$STARY_CS")" 0
chk "STARY kod: brak metody GoToMarkdownList"           "$(c 'private void GoToMarkdownList' "$STARY_CS")" 0
chk "STARY kod: brak parsera GetMarkdownEmphases"       "$(c 'GetMarkdownEmphases' "$STARY_CS")" 0
chk "STARY kod: brak GetMarkdownListStarts"             "$(c 'GetMarkdownListStarts' "$STARY_CS")" 0
chk "STARY kod: brak licznika CountMarkdownListItemsFrom" "$(c 'CountMarkdownListItemsFrom' "$STARY_CS")" 0
chk "STARY kod: chord Control+OemQuestion NIEPRZYPISANY"       "$(c '"Control+OemQuestion"' "$STARY_CS")" 0
chk "STARY kod: chord Control+Shift+OemQuestion NIEPRZYPISANY" "$(c '"Control+Shift+OemQuestion"' "$STARY_CS")" 0
chk "STARY kod: chord Control+OemMinus NIEPRZYPISANY"          "$(c '"Control+OemMinus"' "$STARY_CS")" 0
chk "STARY kod: chord Control+Shift+OemMinus NIEPRZYPISANY"    "$(c '"Control+Shift+OemMinus"' "$STARY_CS")" 0
chk "STARY Hotkeys.ini: brak opisu Next Emphasis"       "$(c '^Next Emphasis=' "$STARY_HK")" 0
chk "STARY Hotkeys.ini: brak opisu Next List"           "$(c '^Next List=' "$STARY_HK")" 0
echo

echo "--- CZESC 2: NOWY kod ma komendy na KAZDEJ warstwie ---"
# Kazda warstwa mierzona OSOBNO: pole, pozycja menu, AddRange, handler, metoda.
# Jedna sonda po nazwie komendy dala by zielone i przegapila brak handlera -
# to dokladnie ten defekt, ktory dal raz martwy klawisz przy zielonym buildzie.
for N in NextEmphasis PriorEmphasis NextList PriorList; do
  ge  "NOWY kod: pole menuNavigate$N zadeklarowane"  "$(c "menuNavigate$N" EdSharp.cs)" 3
done
chk "NOWY kod: pozycja menu Next Emphasis"    "$(c 'CreateMenuItem("Next Emphasis", "Control+OemQuestion"' EdSharp.cs)" 1
chk "NOWY kod: pozycja menu Prior Emphasis"   "$(c 'CreateMenuItem("Prior Emphasis", "Control+Shift+OemQuestion"' EdSharp.cs)" 1
chk "NOWY kod: pozycja menu Next List"        "$(c 'CreateMenuItem("Next List", "Control+OemMinus"' EdSharp.cs)" 1
chk "NOWY kod: pozycja menu Prior List"       "$(c 'CreateMenuItem("Prior List", "Control+Shift+OemMinus"' EdSharp.cs)" 1
chk "NOWY kod: AddRange menu Navigate zawiera wszystkie cztery" \
    "$(grep -c 'menuNavigateNextEmphasis, menuNavigatePriorEmphasis, menuNavigateNextList, menuNavigatePriorList' EdSharp.cs)" 2
chk "NOWY kod: handler wyroznien wola GoToMarkdownEmphasis" "$(c 'GoToMarkdownEmphasis(rtb, menuItem == menuNavigateNextEmphasis)' EdSharp.cs)" 1
chk "NOWY kod: handler list wola GoToMarkdownList"          "$(c 'GoToMarkdownList(rtb, menuItem == menuNavigateNextList)' EdSharp.cs)" 1
chk "NOWY kod: metoda GoToMarkdownEmphasis istnieje"        "$(c 'private void GoToMarkdownEmphasis' EdSharp.cs)" 1
chk "NOWY kod: metoda GoToMarkdownList istnieje"            "$(c 'private void GoToMarkdownList' EdSharp.cs)" 1
echo

echo "--- CZESC 3: ZACHOWANIE, ktorego build NIE ODRZUCI ---"
# 3a. Kursor na TRESCI, nie na gwiazdce - jego wymog z 15:41 ("sam pogrubiony
#     fragment").  Gdyby kod stawial kursor na Start, komenda dzialalaby, a
#     czytnik przeczytalby gwiazdki.
chk "kursor wyroznienia staje na TextStart (tresc), nie na Start (gwiazdka)" "$(c 'rtb.Index = found.TextStart;' EdSharp.cs)" 1
# 3b. Wyroznienia i listy to JEDNA komenda na oba rodzaje - ustalenie
#     edsharpng-42 mowi, ze osobnej komendy dla pochylenia NIE CHCE.
chk "wyroznienie rozpoznaje POCHYLENIE (poziom 1)"    "$(c 'return "italic";' EdSharp.cs)" 1
chk "wyroznienie rozpoznaje POGRUBIENIE (poziom 2)"   "$(c 'return "bold";' EdSharp.cs)" 1
chk "wyroznienie rozpoznaje OBA NARAZ (poziom 3)"     "$(c 'return "bold italic";' EdSharp.cs)" 1
# 3c. Listy staja na POCZATKU listy, nie na kazdej pozycji (edsharpng-46).
#     Parser podgladu zbiera osobno 'lists' (poczatki) i 'items' (pozycje);
#     wziecie 'items' dalo by komende chodzaca po kazdym punkcie, czyli
#     dokladnie to, czego on NIE chcial.
chk "skok po listach bierze POCZATKI (lists), nie pozycje (items)" "$(grep -c 'if (lists != null) starts.AddRange(lists);' EdSharp.cs)" 1
chk "KONTROLA ROZNICUJACA: GetMarkdownListStarts NIE dodaje items" "$(grep -c 'starts.AddRange(items)' EdSharp.cs)" 0
# 3d. Cztery odmowy MUSZA byc slyszalne - bez nich niewidomy nie wie, czemu nic
#     sie nie stalo.  Chordy sa BEZ Control+Alt, wiec tryb zwykly wystarcza
#     (Util.Say tlumi mowe tylko przy Control i Alt naraz).
chk "odmowa: brak wyroznien w dokumencie"  "$(c '"No bold or italic text!"' EdSharp.cs)" 1
chk "odmowa: ostatnie wyroznienie"         "$(c '"Last bold or italic text!"' EdSharp.cs)" 1
chk "odmowa: pierwsze wyroznienie"         "$(c '"First bold or italic text!"' EdSharp.cs)" 1
chk "odmowa: brak list"                    "$(c '"No lists!"' EdSharp.cs)" 1
chk "odmowa: ostatnia lista"               "$(c '"Last list!"' EdSharp.cs)" 1
chk "odmowa: pierwsza lista"               "$(c '"First list!"' EdSharp.cs)" 1
# 3e. Trzy pulapki parsera, kazda mierzona sonda h.cs, tu pilnowane w kodzie.
chk "parser pomija BLOKI KODU (ten sam helper co naglowki i przypisy)" "$(grep -c 'if (IsMarkdownIndexInFence(fences, i)) {i++; continue;}' EdSharp.cs)" 1
chk "parser odrzuca znacznik z bialym znakiem po nim (punktor listy)"  "$(grep -c 'if (Char.IsWhiteSpace(sText\[iTextStart\])) {i = iTextStart; continue;}' EdSharp.cs)" 1
chk "parser wymaga granicy slowa dla podkreslnika (nazwa_z_podkresleniem)" "$(grep -c 'if (Char.IsLetterOrDigit(cBefore)) {i = iTextStart; continue;}' EdSharp.cs)" 1
echo

echo "--- CZESC 4: KONTROLE POZYTYWNE - nic istniejacego nie zabrane ---"
# Nowa komenda na zajetym chordzie dala by ZIELONY BUILD i alarm o duplikacie
# widoczny tylko U NIEGO przy starcie.  Te asercje pilnuja, ze cztery chordy
# nadal naleza do JEDNEJ komendy kazdy, oraz ze rodziny Alt (pytania o format
# i okna ustawien) sa NIETKNIETE - one siedza na tych samych klawiszach z Altem.
chk "Alt z ukosnikiem nadal pyta o styl (Say Styles)"          "$(c 'CreateMenuItem("Styles", "Alt+OemQuestion"' EdSharp.cs)" 1
chk "Alt z myslnikiem nadal pyta o czcionke (Say Font)"        "$(c 'CreateMenuItem("Font", "Alt+OemMinus"' EdSharp.cs)" 1
chk "Alt+Shift z ukosnikiem nadal USTAWIA styl"                "$(c 'CreateMenuItem("Style ...", "Alt+Shift+OemQuestion"' EdSharp.cs)" 1
chk "Alt+Shift z myslnikiem nadal USTAWIA czcionke"            "$(c 'CreateMenuItem("Set Selection Font ...", "Alt+Shift+OemMinus"' EdSharp.cs)" 1
chk "skok przypisu nadal na Control+Alt+K (5.0.47)"            "$(c '"Control+Alt+K"' EdSharp.cs)" 1
chk "lista przypisow nadal na Alt+K"                           "$(c 'CreateMenuItem("Footnote List ...", "Alt+K"' EdSharp.cs)" 1
chk "straznik pisania nadal w CreateMenuItem (5.0.48)"         "$(c 'Util.IsTypingChord(keyData)' EdSharp.cs)" 1
chk "drzewo naglowkow nadal na F6"                             "$(c 'CreateMenuItem("Document Navigation ...", "F6"' EdSharp.cs)" 1
chk "skoki po wcieciach nadal na Control+I"                    "$(c 'CreateMenuItem("Next Indent", "Control+I"' EdSharp.cs)" 1
chk "parser podgladu Markdown NIETKNIETY (MarkdownReview_ParseText)" "$(c 'private static void MarkdownReview_ParseText' EdSharp.cs)" 1
# Zaden chord nie moze byc przypisany DWA RAZY - to jedyna wada, ktorej sam
# program nie zlapie przed startem u niego.
# PUSTY chord jest WYKLUCZONY swiadomie: pusty klawisz znaczy "komenda tylko w
# menu" (patrz Util.String2Key) i takich pozycji jest w programie pieciu.
# Pierwsza wersja tej asercji ich nie odsiewala i dawala FAIL przy POPRAWNYM
# kodzie - poprawiona zostala SONDA, nie kod.
DUP="$(grep -o 'CreateMenuItem("[^"]*", "[^"]*"' EdSharp.cs | sed 's/.*, "//' | grep -v '^"$' | sort | uniq -d | wc -l)"
chk "ZERO chordow przypisanych dwa razy w calym menu" "$DUP" 0
# KONTROLA POZYTYWNA SONDY: bez niej "zero duplikatow" nie odroznia sie od
# wzorca, ktory nie moze niczego trafic.  Podstawiamy duplikat w KOPII pliku.
TMPDUP="$(mktemp)"
{ cat EdSharp.cs; printf 'menuTest = CreateMenuItem("Test Duplikat", "Control+I", menuItem_Click, "child silent");\r\n'; } > "$TMPDUP"
DUP2="$(grep -o 'CreateMenuItem("[^"]*", "[^"]*"' "$TMPDUP" | sed 's/.*, "//' | grep -v '^"$' | sort | uniq -d | wc -l)"
chk "KONTROLA POZYTYWNA SONDY: podstawiony duplikat Control+I ZOSTAJE ZLAPANY" "$DUP2" 1
rm -f "$TMPDUP"
echo

echo "--- CZESC 5: BINARKA (zielony build nie dowodzi, ze komenda tam jest) ---"
# Pomiar w OBU kodowaniach: napisy interfejsu .NET siedza w binarce jako UTF-16,
# wiec grep ASCII po .exe daje ZERO i wyglada jak brak komendy.
if [ -s EdSharpNG.exe ]; then
python3 - <<'PY'
d=open('/mnt/d/projekty/edsharp-pr/EdSharpNG.exe','rb').read()
def hit(s):
    return d.count(s.encode('ascii')), d.count(s.encode('utf-16-le'))
res=[]
for s in ["Next Emphasis","Prior Emphasis","Next List","Prior List",
          "No bold or italic text!","No lists!","bold italic",
          "Go to Footnote","IsTypingChord","Document Navigation"]:
    a,w=hit(s)
    res.append((s,a,w,(a>0 or w>0)))
for s,a,w,good in res:
    print(("PASS  " if good else "FAIL  ")+f"BINARKA: napis {s!r} obecny (ascii={a}, szeroko={w})")
PY
  # KONTROLA ROZNICUJACA na binarce SPRZED zmiany, jesli lezy w stagingu.
  if [ -s /mnt/c/EdSharp/EdSharpNG.exe ]; then
python3 - <<'PY'
d=open('/mnt/c/EdSharp/EdSharpNG.exe','rb').read()
def hit(s):
    return d.count(s.encode('ascii')), d.count(s.encode('utf-16-le'))
for s in ["Next Emphasis","Next List","No bold or italic text!"]:
    a,w=hit(s)
    print(("PASS  " if (a==0 and w==0) else "FAIL  ")+f"KONTROLA ROZNICUJACA (staging, stara binarka): {s!r} NIE MA (ascii={a}, szeroko={w})")
# Bez tej pozytywnej asercji "zero trafien" moglo by znaczyc, ze czytam pusty
# albo zly plik, a nie ze napisu tam nie ma.
for s in ["Go to Footnote","IsTypingChord"]:
    a,w=hit(s)
    print(("PASS  " if (a>0 or w>0) else "FAIL  ")+f"KONTROLA POZYTYWNA na TEJ SAMEJ starej binarce: {s!r} JEST (ascii={a}, szeroko={w})")
PY
  fi
else
  bad "BINARKA: brak EdSharpNG.exe w repo"
fi
echo

echo "--- CZESC 6: OPISY MOWIONE (Ctrl+F1 czyta je niewidomemu) ---"
for P in Hotkeys.ini hotkeys.txt; do
  chk "$P: opis Next Emphasis z chordem Control+Slash"        "$(c '^Next Emphasis=Control+Slash,' "$P")" 1
  chk "$P: opis Prior Emphasis z chordem Control+Shift+Slash" "$(c '^Prior Emphasis=Control+Shift+Slash,' "$P")" 1
  chk "$P: opis Next List z chordem Control+Dash"             "$(c '^Next List=Control+Dash,' "$P")" 1
  chk "$P: opis Prior List z chordem Control+Shift+Dash"      "$(c '^Prior List=Control+Shift+Dash,' "$P")" 1
done
echo

echo "--- CZESC 7: CRLF (edycja Pythonem cicho zamienia konce wiersza) ---"
for P in EdSharp.cs Hotkeys.ini hotkeys.txt EdSharp.md; do
  L="$(wc -l < "$P" | tr -d ' ')"
  R="$(grep -c $'\r$' "$P" || true)"
  GOLE="$(python3 -c "
import sys
d=open('$P','rb').read()
print(sum(1 for i,b in enumerate(d) if b==13 and (i+1>=len(d) or d[i+1]!=10)))
")"
  chk "$P ma CRLF w kazdym wierszu" "$R" "$L"
  chk "$P nie ma golych CR" "$GOLE" 0
done
echo

rm -f "$STARY_CS" "$STARY_HK" "$STARY_MD"
echo "================================"
# Sumujemy takze wiersze PASS/FAIL wypisane przez wstawki Pythona.
echo "WYNIK: $PASS PASS, $FAIL FAIL (bez czesci binarnej - jej wiersze wyzej)"
[ "$FAIL" -eq 0 ] || exit 1
