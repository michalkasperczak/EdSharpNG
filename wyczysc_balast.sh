#!/bin/bash
# Usuwa z repozytorium EdSharpNG "balast" odziedziczony po oryginalnym
# EdSharpie: stare kopie kodu, obce biblioteki i pliki nie majace zwiazku
# z programem.
#
# CO ZOSTAJE (sprawdzone, NIE usuwac):
#   - EdSharp.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs  -> te SZESC plikow
#     kompiluje BuildEdSharp.cmd.  Say.cs i KeyMap.cs wygladaja na stare
#     smieci z nazwy, ale sa uzywane.
#   - history.txt lgpl.txt HotKeys.txt EdSharp.ini Snippets/ Convert/
#     -> wymienione w EdSharp_Setup.iss (instalator je pakuje).
#
# DLACZEGO PONIZSZE MOZNA USUNAC (sprawdzone przed napisaniem skryptu):
#   - konwersje formatow ida przez pandoc (patrz EdSharp.ini), NIE przez
#     txt2tags.py ani html2text.py,
#   - zaden z tych katalogow nie jest wymieniony w sekcji Files instalatora,
#   - zaden nie jest wolany z szesciu kompilowanych plikow .cs.
#
# Uzycie:
#   bash wyczysc_balast.sh lista   <- tylko pokazuje, nic nie rusza
#   bash wyczysc_balast.sh usun    <- usuwa z biezacej wersji i commituje

set -u
cd "$(dirname "$0")" || exit 1

TRYB="${1:-lista}"

# Stare kopie glownego kodu - nie kompilowane, myla przy szukaniu.
PLIKI=(
  "old_edsharp.cs"
  "net4_edsharp.cs"
  "edsharp30g.cs"
  "corrupt_EdSharp.cs"
  "2011-07-02_EdSharp.cs"
  "EdSharp_01.cs"
  "EdSharp2017.cs"
  "EdSharp30e.cs"
  "EdSharp32.cs"
  "EdSharp33.cs"
  "EdSharp39a.cs"
  "EdSharp.cs.bak"
  "edsharp.cs.orig"
  "EdSharp.cs.orig"
  "EdSharp.md.cs"
  "EdSharp.ini.bak"
  "old_EdSharp.md"
  "htm2md_EdSharp.md"
  "admin_bans.php"
  "txt2tags.py"
  "txt2tags-2.6.tgz"
  "html2text.py"
)

# Obce biblioteki - nieuzywane, czesc z katalogami .svn z lat 2000-nych.
KATALOGI=(
  "textile"
  "brltex"
  "detect"
  "latexaccess"
  "markdown.net"
)

echo "=== PLIKI DO USUNIECIA ==="
ILE=0
DO_USUNIECIA=()
for f in "${PLIKI[@]}"; do
  if git ls-files --error-unmatch "$f" >/dev/null 2>&1; then
    echo "  $f"
    DO_USUNIECIA+=("$f")
    ILE=$((ILE+1))
  fi
done

echo "=== KATALOGI DO USUNIECIA ==="
for d in "${KATALOGI[@]}"; do
  n=$(git ls-files -- "$d" | wc -l)
  if [ "$n" -gt 0 ]; then
    echo "  $d/  ($n plikow)"
    DO_USUNIECIA+=("$d")
    ILE=$((ILE+n))
  fi
done

echo
echo "RAZEM: $ILE plikow"

if [ "$TRYB" != "usun" ]; then
  echo "(tryb podgladu - nic nie usunieto; uruchom z argumentem: usun)"
  exit 0
fi

echo
echo "=== USUWANIE ==="
for x in "${DO_USUNIECIA[@]}"; do
  git rm -r -q --ignore-unmatch -- "$x" 2>/dev/null
done

git commit -q -m "Porzadki: usuniecie starych kopii kodu i nieuzywanych obcych bibliotek

Usuniete stare wersje glownego pliku (old_edsharp.cs, edsharp30g.cs,
EdSharp2017.cs i podobne), kopie zapasowe oraz obce biblioteki nie
wolane przez program: textile, brltex, detect, latexaccess,
markdown.net, a takze admin_bans.php (fragment obcego panelu, bez
zwiazku z edytorem) i skrypty txt2tags/html2text - konwersje formatow
obsluguje pandoc.

Zachowane pliki kompilowane (EdSharp.cs, Lbc.cs, Say.cs, Inix.cs,
KeyMap.cs, Web.cs) oraz wszystko, co pakuje instalator." && echo "OK - usunieto i zacommitowano"

echo
echo "=== SPRAWDZENIE: czy szesc kompilowanych plikow nadal jest ==="
for f in EdSharp.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs; do
  if git ls-files --error-unmatch "$f" >/dev/null 2>&1; then echo "  OK  $f"; else echo "  BRAK!  $f"; fi
done
