#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.48.
#
# ZMIANA: STRAZNIK PISANIA.  Chord, ktorym uzytkownik WPISUJE znak na biezacym
# ukladzie klawiatury, nie moze zostac skrotem komendy.
#
# SKAD: pytanie Kasperczaka z 30.08.2026 11:45 o nasz Control+Alt+K: "Mam
# nadzieje, ze tu sie nie dubluje, bo Alt+Ctrl to tez to samo co prawy Alt, a
# prawy Alt to polska literka, ale mozna to jakos tak zrobic, zeby dzialalo
# prawidlowo."
#
# DLACZEGO TEN SKRYPT ISTNIEJE: "widze straznik w kodzie" nie dowodzi niczego,
# dopoki ta sama sonda nie ZOBACZY, ze w kodzie SPRZED zmiany go NIE BYLO.  Bez
# tego mierzy sie glucha sonde.  Rewizja odniesienia jest podana JAWNIE w
# argumencie, bo skrypt bioracy HEAD po commicie porownuje kod z samym soba.
#
# NAJWAZNIEJSZA CZESC to KONTROLE POZYTYWNE: straznik zbyt szeroki bylby GORSZY
# niz jego brak, bo odebral by nam dzialajace skroty.  Osobne asercje pilnuja,
# ze Control+Alt+K, Control+Alt+F9 i Control+Alt+strzalki NADAL sa przypisane,
# oraz ze straznik NIE dotyka chordow z samym Control albo samym Alt.
set -u
REPO="/mnt/d/projekty/edsharp-pr"
REV="${1:-2460a49}"
cd "$REPO" || exit 2

PASS=0; FAIL=0
ok()   { PASS=$((PASS+1)); printf 'PASS  %s\n' "$1"; }
bad()  { FAIL=$((FAIL+1)); printf 'FAIL  %s\n' "$1"; }
chk()  { if [ "$2" -eq "$3" ]; then ok "$1 (=$3)"; else bad "$1 (jest $2, oczekiwano $3)"; fi; }
ge()   { if [ "$2" -ge "$3" ]; then ok "$1 (=$2, co najmniej $3)"; else bad "$1 (jest $2, oczekiwano co najmniej $3)"; fi; }

STARY_CS="$(mktemp)";  git show "$REV:EdSharp.cs"  > "$STARY_CS"  2>/dev/null
STARY_MD="$(mktemp)";  git show "$REV:EdSharp.md"  > "$STARY_MD"  2>/dev/null
if [ ! -s "$STARY_CS" ]; then echo "BLAD: nie mam rewizji $REV"; exit 2; fi

echo "=== rewizja odniesienia: $REV ==="
echo
c() { grep -c "$1" "$2" 2>/dev/null || true; }

echo "--- CZESC 1: STARY kod NIE MIAL straznika (dowod, ze sonda nie jest glucha) ---"
chk "stary EdSharp.cs bez metody IsTypingChord"        "$(c 'IsTypingChord' "$STARY_CS")" 0
chk "stary EdSharp.cs bez importu VkKeyScanEx"         "$(c 'VkKeyScanEx' "$STARY_CS")" 0
chk "stary EdSharp.cs bez importu GetKeyboardLayout"   "$(c 'GetKeyboardLayout' "$STARY_CS")" 0
chk "stary EdSharp.cs bez komunikatu o pisaniu znaku"  "$(c 'types a character' "$STARY_CS")" 0
chk "stary podrecznik bez akapitu o strazniku"         "$(c 'types a character' "$STARY_MD")" 0
# Ta asercja jest wazna osobno: stary kod MIAL alarm o duplikacie, wiec sonda
# szukajaca samego slowa "Cannot assign" dala by trafienie i wygladalo by to jak
# obecny straznik.  Rozroznienie idzie po TRESCI komunikatu.
ge  "stary kod MIAL juz alarm o duplikacie (kontrola rozroznienia)" "$(c 'Cannot assign' "$STARY_CS")" 1
echo

echo "--- CZESC 2: NOWY kod MA straznika, na kazdej warstwie ---"
chk "metoda Util.IsTypingChord istnieje"               "$(c 'public static bool IsTypingChord' EdSharp.cs)" 1
chk "import VkKeyScanEx w Win32"                       "$(c 'public static extern short VkKeyScanEx' EdSharp.cs)" 1
chk "import GetKeyboardLayout w Win32"                 "$(c 'public static extern IntPtr GetKeyboardLayout' EdSharp.cs)" 1
chk "CreateMenuItem WOLA straznika"                    "$(c 'else if (Util.IsTypingChord(keyData))' EdSharp.cs)" 1
chk "komunikat mowi o PISANIU znaku, nie o duplikacie" "$(c 'since this chord types a character' EdSharp.cs)" 1
# Straznik MUSI stac PO sprawdzeniu duplikatu i PRZED przypisaniem chordu:
# postawiony po hashKey.Add przepuscil by chord, ktory chce zablokowac.
LINIA_DUP=$(grep -n 'since already assigned to' EdSharp.cs | head -1 | cut -d: -f1)
LINIA_STR=$(grep -n 'else if (Util.IsTypingChord(keyData))' EdSharp.cs | head -1 | cut -d: -f1)
LINIA_ADD=$(grep -n 'hashKey.Add(keyData, menuItem);' EdSharp.cs | head -1 | cut -d: -f1)
if [ -n "$LINIA_DUP" ] && [ -n "$LINIA_STR" ] && [ -n "$LINIA_ADD" ] \
   && [ "$LINIA_DUP" -lt "$LINIA_STR" ] && [ "$LINIA_STR" -lt "$LINIA_ADD" ]; then
  ok "straznik stoi PO alarmie o duplikacie i PRZED hashKey.Add (linie $LINIA_DUP < $LINIA_STR < $LINIA_ADD)"
else
  bad "zla kolejnosc: duplikat $LINIA_DUP, straznik $LINIA_STR, przypisanie $LINIA_ADD"
fi
echo

echo "--- CZESC 3: KONTROLE POZYTYWNE - straznik zbyt szeroki byl by GORSZY niz brak ---"
# Gdyby straznik lapal wszystko, te komendy stracily by skroty przy zielonym
# buildzie, a alarm zobaczylby dopiero Kasperczak przy starcie programu.
chk "Control+Alt+K nadal trzyma skok przypisu"         "$(c 'CreateMenuItem("Go to Footnote", "Control+Alt+K"' EdSharp.cs)" 1
chk "Control+Alt+F9 nadal trzyma liste komentarzy"     "$(c 'CreateMenuItem("Comment List ...", "Control+Alt+F9"' EdSharp.cs)" 1
chk "Alt+K nadal trzyma liste przypisow"               "$(c 'CreateMenuItem("Footnote List ...", "Alt+K"' EdSharp.cs)" 1
chk "przesuwanie sekcji nadal na Control+Alt+strzalki" "$(c 'Markdown section move (Control+Alt+Up/Down)' EdSharp.cs)" 1
# Straznik pyta o modyfikatory JAWNIE: bez tego warunku lapal by tez zwykle
# skroty z samym Control (Control+S) albo samym Alt (Alt+K) i program wstal by
# bez polowy klawiszy.
chk "straznik ogranicza sie do Control+Alt (i z Shiftem)" "$(c 'if (mods != (Keys.Control | Keys.Alt) \&\& mods != (Keys.Control | Keys.Alt | Keys.Shift)) return false;' EdSharp.cs)" 1
chk "straznik odrzuca Keys.None (komendy tylko z menu)"   "$(c 'if (keyData == Keys.None) return false;' EdSharp.cs)" 1
# Uklad, nie lista liter na sztywno: inaczej straznik klamal by na kazdej
# klawiaturze innej niz polska.
POLSKIE=$(grep -c 'IsTypingChord' EdSharp.cs || true)
ge "metoda IsTypingChord wymieniona w kodzie (definicja + wywolanie + komentarze)" "$POLSKIE" 2
echo

echo "--- CZESC 4: POMIAR NA BINARCE (nie tylko na zrodle) ---"
# Grep ASCII po binarce .NET daje zero dla napisow interfejsu i wyglada jak brak
# funkcji - to znana pulapka tego repo.  Napisy uzytkownika sa SZEROKIE (UTF-16),
# a nazwy metod i importow ASCII.  Sprawdzam OBA kodowania.
if [ ! -s EdSharpNG.exe ]; then bad "brak zbudowanej binarki EdSharpNG.exe"; else
python3 - <<'PY'
import sys
d = open("/mnt/d/projekty/edsharp-pr/EdSharpNG.exe", "rb").read()
def licz(s):
    return d.count(s.encode("ascii")), d.count(s.encode("utf-16-le"))
wyn = []
for s, oczA, oczW in [("IsTypingChord", 1, 0), ("VkKeyScanEx", 1, 0),
                      ("GetKeyboardLayout", 1, 0),
                      ("types a character", 0, 1),
                      ("already assigned", 0, 1),
                      ("Go to Footnote", 0, 1)]:
    a, w = licz(s)
    dobrze = (a >= oczA and w >= oczW)
    wyn.append(dobrze)
    print("%s  binarka: %-20s ascii=%d szeroko=%d (oczekiwano co najmniej %d/%d)"
          % ("PASS " if dobrze else "FAIL ", s, a, w, oczA, oczW))
sys.exit(0 if all(wyn) else 1)
PY
if [ $? -eq 0 ]; then ok "binarka zawiera straznika i nie stracila istniejacych komend"; else bad "pomiar na binarce nie przeszedl"; fi
fi
echo

echo "--- CZESC 5: BINARKA SPRZED ZMIANY NIE MA STRAZNIKA (kontrola roznicujaca) ---"
STARA_EXE="/mnt/c/tmp/altgr/EdSharpNG_prawidlowy.exe"
if [ ! -s "$STARA_EXE" ]; then
  echo "POMIN  brak kopii binarki 5.0.47 pod $STARA_EXE"
else
python3 - <<'PY'
import sys
d = open("/mnt/c/tmp/altgr/EdSharpNG_prawidlowy.exe", "rb").read()
zle = []
for s in ("IsTypingChord", "VkKeyScanEx", "types a character"):
    a = d.count(s.encode("ascii")); w = d.count(s.encode("utf-16-le"))
    if a or w:
        zle.append(s)
    print("  stara binarka: %-20s ascii=%d szeroko=%d" % (s, a, w))
# Kontrola pozytywna na TEJ SAMEJ binarce: musi w niej byc komenda, ktora
# istniala i wtedy.  Bez tego "zero trafien" moglo by znaczyc, ze czytam zly
# albo pusty plik.
a = d.count("Go to Footnote".encode("utf-16-le"))
print("  stara binarka: Go to Footnote (kontrola pozytywna) szeroko=%d" % a)
sys.exit(0 if (not zle and a >= 1) else 1)
PY
if [ $? -eq 0 ]; then ok "binarka 5.0.47 NIE miala straznika, a miala skok przypisu"; else bad "kontrola roznicujaca na starej binarce nie przeszla"; fi
fi
echo

echo "--- CZESC 6: CRLF (edycja Pythonem cicho zamienia konce wiersza) ---"
for f in EdSharp.cs EdSharp.md; do
  LINII=$(wc -l < "$f")
  CR=$(grep -c $'\r$' "$f" || true)
  chk "$f ma CRLF w kazdym wierszu" "$CR" "$LINII"
done
echo

echo "================================"
echo "WYNIK: $PASS PASS, $FAIL FAIL"
rm -f "$STARY_CS" "$STARY_MD"
[ "$FAIL" -eq 0 ] || exit 1
