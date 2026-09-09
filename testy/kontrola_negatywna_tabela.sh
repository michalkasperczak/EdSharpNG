#!/usr/bin/env bash
# KONTROLA NEGATYWNA nowych asercji rozdzialu 18: na kodzie SPRZED kreatora
# tabeli musza PADAC. Zielony wynik na biezacym kodzie sam z siebie nie
# dowodzi niczego - asercja moglaby byc glucha.
set -uo pipefail
cd /mnt/d/projekty/edsharp-pr

STARY=$(git rev-parse HEAD)
TMP=/tmp/kontrola_neg_tabela
rm -rf "$TMP" && mkdir -p "$TMP"

# Kopie plikow SPRZED zmiany, czytane Z DYSKU (git show do zmiennej Pythona
# potrafi dac falszywe None - zmierzone 28.08.2026).
git show "$STARY:EdSharp.cs" > "$TMP/EdSharp.cs"
git show "$STARY:Lbc.cs" > "$TMP/Lbc.cs"
git show "$STARY:Hotkeys.ini" > "$TMP/Hotkeys.ini"

python3 - "$TMP" <<'PY'
import re, sys, pathlib
d = pathlib.Path(sys.argv[1])
CS = (d / "EdSharp.cs").read_text(encoding="utf-8", errors="replace")
LBC = (d / "Lbc.cs").read_text(encoding="utf-8", errors="replace")
HOT = (d / "Hotkeys.ini").read_text(encoding="utf-8", errors="replace")
CS_KOD = "\n".join(l for l in CS.split("\n") if not l.strip().startswith("//"))

MENU = dict(re.findall(r'CreateMenuItem\("([^"]*)",\s*"([^"]*)"', CS_KOD))

# KONTROLA POZYTYWNA sondy: na starym kodzie te rzeczy MUSZA byc widoczne,
# inaczej badam pusty plik, a nie stary kod.
poz = [
    ("stary kod ma pozycje menu (>=200)", len(MENU) >= 200),
    ("stary kod ma Document Navigation pod F6", MENU.get("Document Navigation ...") == "F6"),
    ("stary kod ma Text Combine", "Text Combine" in MENU),
    ("stary Lbc.cs ma addTreeView", "addTreeView" in LBC),
]
for n, ok in poz:
    print(("POZ OK   " if ok else "POZ BLAD ") + n)
if not all(ok for _, ok in poz):
    print("\nSONDA GLUCHA - kontrola pozytywna padla, wynik nizej jest bez wartosci")
    sys.exit(2)

body_tab = CS[CS.find("private void InsertMarkdownTable("):]
body_tab = body_tab[:body_tab.find("} // InsertMarkdownTable")]

nowe = [
    ("Insert Table pod Control+Shift+T", MENU.get("Insert Table ...") == "Control+Shift+T"),
    ("Text Combine menu-only", MENU.get("Text Combine") == ""),
    ("Hotkeys.ini: Text Combine menu only",
     re.search(r"^Text Combine=, .*menu only", HOT, re.M) is not None),
    ("Hotkeys.ini: Insert Table=Control+Shift+T",
     re.search(r"^Insert Table=Control\+Shift\+T,", HOT, re.M) is not None),
    ("komunikat bramki na typ pliku", '"Tables work only on Markdown files!"' in CS_KOD),
    ("komunikat bramki na blok kodu", '"Cannot put a table inside a code block!"' in CS_KOD),
    ("tabela cofalna przez ReplaceRange", "ReplaceRange" in body_tab),
    ("Escape pyta o potwierdzenie", "Close the table without inserting it?" in CS),
    ("puste kolumny ucinane", "GetMarkdownTableGridExtent" in CS),
    ("kreska pionowa zabezpieczana", "EscapeMarkdownTableCell" in CS),
    ("siatka w bibliotece okien", "addTableGrid" in LBC and "class LbcGrid" in LBC),
]

padly = 0
for n, ok in nowe:
    # Na STARYM kodzie asercja MUSI byc falszywa.
    print(("PADA (dobrze)  " if not ok else "PRZESZLA (ZLE) ") + n)
    if not ok:
        padly += 1

print()
print("WYNIK KONTROLI NEGATYWNEJ: %d/%d nowych asercji PADA na starym kodzie"
      % (padly, len(nowe)))
sys.exit(0 if padly == len(nowe) else 1)
PY
