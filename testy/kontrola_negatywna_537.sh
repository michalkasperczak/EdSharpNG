#!/usr/bin/env bash
# KONTROLA NEGATYWNA nowych asercji 5.0.37 (mowa w siatce, konce wiersza,
# kodowanie zapisu, Delete w komorce): na kodzie SPRZED tych zmian MUSZA padac.
#
# Punkt odniesienia to commit 60b12a4 = stan 5.0.36, czyli PRZED ta iteracja.
# Uwaga na pulapke z poprzedniego przebiegu: skrypt biorący HEAD dowodzi
# niczego, gdy zmiana jest juz zacommitowana - dlatego rewizja jest tu JAWNA.
set -uo pipefail
cd /mnt/d/projekty/edsharp-pr || exit 2

PRZED=${1:-60b12a4}
TMP=/tmp/kontrola_neg_537
rm -rf "$TMP" && mkdir -p "$TMP"

# Pliki SPRZED zmiany czytane Z DYSKU (git show wprost do zmiennej Pythona
# potrafi dac falszywe None - zmierzone 28.08.2026).
git show "$PRZED:EdSharp.cs" > "$TMP/EdSharp.cs" || exit 2
git show "$PRZED:Lbc.cs" > "$TMP/Lbc.cs" || exit 2
cp EdSharp.cs "$TMP/EdSharp_nowy.cs"
cp Lbc.cs "$TMP/Lbc_nowy.cs"

python3 - "$TMP" <<'PY'
import re, sys, pathlib
d = pathlib.Path(sys.argv[1])
def czytaj(n):
    return (d / n).read_text(encoding="utf-8", errors="replace")
CS, LBC = czytaj("EdSharp.cs"), czytaj("Lbc.cs")
CSN, LBCN = czytaj("EdSharp_nowy.cs"), czytaj("Lbc_nowy.cs")
bez_kom = lambda s: "\n".join(l for l in s.split("\n") if not l.strip().startswith("//"))
CS_KOD, LBC_KOD = bez_kom(CS), bez_kom(LBC)
CSN_KOD, LBCN_KOD = bez_kom(CSN), bez_kom(LBCN)

MENU = dict(re.findall(r'CreateMenuItem\("([^"]*)",\s*"([^"]*)"', CS_KOD))

# KONTROLA POZYTYWNA SONDY: na starym kodzie te rzeczy MUSZA byc widoczne,
# inaczej czytam pusty plik i kazde "pada" jest bez wartosci.
poz = [
    ("stary kod ma pozycje menu (>=200)", len(MENU) >= 200),
    ("stary kod ma Document Navigation pod F6", MENU.get("Document Navigation ...") == "F6"),
    ("stary kod ma Insert Table pod Control+Shift+T", MENU.get("Insert Table ...") == "Control+Shift+T"),
    ("stary Lbc.cs ma klase LbcGrid", "class LbcGrid" in LBC_KOD),
    ("stary kod ma Alt+Z jako Status", MENU.get("Status") == "Alt+Z"),
    ("stary kod ma helper Convert2WinLineBreak", "Convert2WinLineBreak" in CS_KOD),
]
for n, ok in poz:
    print(("POZ OK    " if ok else "POZ BLAD  ") + n)
if not all(ok for _, ok in poz):
    print("\nSONDA GLUCHA - kontrola pozytywna padla, wynik nizej nic nie znaczy")
    sys.exit(2)

# Nowe asercje: (opis, czy jest w STARYM, czy jest w NOWYM)
nowe = [
    ("komorka siatki podaje tresc przed pozycja (LbcGridCell)",
     "class LbcGridCell" in LBC_KOD, "class LbcGridCell" in LBCN_KOD),
    ("opis komorki sklada tresc i wspolrzedne (DescribeCell)",
     "DescribeCell" in LBC_KOD, "DescribeCell" in LBCN_KOD),
    ("kolejnosc wspolrzednych zalezy od kierunku ruchu (LastMoveWasVertical)",
     "LastMoveWasVertical" in LBC_KOD, "LastMoveWasVertical" in LBCN_KOD),
    ("kolumny siatki dodawane metoda pilnujaca dostepnosci (addGridColumn)",
     "addGridColumn" in LBC_KOD, "addGridColumn" in LBCN_KOD),
    ("edycja po F2 odrozniana od pisania (EditStartedByF2)",
     "EditStartedByF2" in LBC_KOD, "EditStartedByF2" in LBCN_KOD),
    ("Delete czysci tresc komorki (komunikat Cleared)",
     '"Cleared"' in CS_KOD, '"Cleared"' in CSN_KOD),
    ("Delete na pustej komorce mowi swoje",
     '"Cell is already empty"' in CS_KOD, '"Cell is already empty"' in CSN_KOD),
    ("rodzaj koncow wiersza rozpoznawany (DetectLineBreakKind)",
     "DetectLineBreakKind" in CS_KOD, "DetectLineBreakKind" in CSN_KOD),
    ("rodzaj koncow wiersza pamietany per okno (FileLineBreakKind)",
     "FileLineBreakKind" in CS_KOD, "FileLineBreakKind" in CSN_KOD),
    ("Alt+Z mowi rodzaj koncow wiersza (line breaks)",
     '"line breaks ' in CS_KOD or '", line breaks ' in CS_KOD,
     '", line breaks "' in CSN_KOD or 'line breaks ' in CSN_KOD),
    ("stara strona kodowa zapisywana jako UTF-8 (GetSaveEncoding)",
     "GetSaveEncoding" in CS_KOD, "GetSaveEncoding" in CSN_KOD),
    ("naglowki wierszy siatki nie dubluja numeru",
     '"Row " + i' not in CS_KOD, '"Row " + i' not in CSN_KOD),
]
padly = 0
for opis, w_starym, w_nowym in nowe:
    if not w_starym and w_nowym:
        padly += 1
        print("PADA (dobrze)   " + opis)
    elif w_starym and w_nowym:
        print("PRZESZLA (ZLE)  " + opis + "  <- bylo JUZ przed zmiana")
    else:
        print("BRAK W NOWYM    " + opis + "  <- asercja nie ma pokrycia w kodzie")

print("\nWYNIK KONTROLI NEGATYWNEJ: %d/%d nowych asercji PADA na kodzie sprzed zmiany"
      % (padly, len(nowe)))
sys.exit(0 if padly == len(nowe) else 1)
PY
rc=$?
rm -rf "$TMP"
exit $rc
