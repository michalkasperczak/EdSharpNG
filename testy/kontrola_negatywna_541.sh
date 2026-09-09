#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.41: buduje binarke SPRZED zmiany z JAWNEJ rewizji
# i wymaga, zeby objawy sie na niej ODTWORZYLY.  Bez tego zielony wynik po
# naprawie nie odroznia "naprawilem" od "sonda nie widzi".
#
# Rewizja odniesienia jest ARGUMENTEM, nie HEAD-em: skrypt biorący HEAD po
# zacommitowaniu zmiany porownuje kod z samym soba i daje 0 asercji padajacych,
# co wyglada jak awaria, a znaczy tylko tyle, ze nie ma czego porownywac.
#
# Skrypt SAM PILNUJE, ze na koncu w repo lezy NOWA binarka (porownanie sha256).
# Bez tej bramki przebieg zostawia binarke ze STAREGO kodu i wysyla sie paczke
# BEZ naprawy - to sie realnie zdarzylo przy 5.0.38.
set -u
REPO="/mnt/d/projekty/edsharp-pr"
REV="${1:-a56ab08}"          # stan 5.0.40
cd "$REPO" || exit 3

CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
NOWA_SUMA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
echo "== rewizja odniesienia: $REV"
echo "== sha256 NOWEJ binarki: ${NOWA_SUMA:0:24}"
cp EdSharpNG.exe /tmp/EdSharpNG_nowa_kn541.exe || exit 3

PASS=0; FAIL=0
ok()   { echo "PASS $1"; PASS=$((PASS+1)); }
bad()  { echo "FAIL $1"; FAIL=$((FAIL+1)); }

# ---------- CZESC 1: asercje na ZRODLE starej rewizji ----------
git show "$REV:EdSharp.cs" > /tmp/stary_541.cs 2>/dev/null || {
    echo "BLAD: nie moge wyciagnac EdSharp.cs z $REV"; exit 3; }
git show "$REV:Lbc.cs" > /tmp/stary_541_lbc.cs 2>/dev/null || {
    echo "BLAD: nie moge wyciagnac Lbc.cs z $REV"; exit 3; }

# KONTROLE POZYTYWNE SONDY: stary plik MUSI byc czytany poprawnie, inaczej
# kazde "nie znalazlem" ponizej znaczy tylko "czytam pusty plik".
LICZ_MENU=$(grep -c 'CreateMenuItem(' /tmp/stary_541.cs)
if [ "$LICZ_MENU" -gt 180 ]; then ok "SONDA: stare zrodlo ma $LICZ_MENU pozycji menu"
else bad "SONDA GLUCHA: tylko $LICZ_MENU pozycji menu w starym zrodle"; exit 2; fi
if grep -q 'LbcGridCellAccessibleObject' /tmp/stary_541_lbc.cs; then
    ok "SONDA: stary Lbc.cs zawiera obiekt dostepnosciowy komorki"
else bad "SONDA GLUCHA: brak LbcGridCellAccessibleObject w starym Lbc.cs"; exit 2; fi

# 1. Control+T BYL zajety przez Text Convert.
if grep -q '"&Text Convert", "Control+T"' /tmp/stary_541.cs; then
    ok "objaw 1: stary kod trzymal Text Convert na Control+T"
else bad "objaw 1 NIE odtworzony - Control+T nie byl tam zajety"; fi

# 2. Petla list plikow NIE MIALA bramki na tresc, ktora nie jest sciezka.
if grep -q '^sTempDir = Path.GetDirectoryName(s);' /tmp/stary_541.cs; then
    ok "objaw 2: stary kod wolal GetDirectoryName BEZ zabezpieczenia"
else bad "objaw 2 NIE odtworzony - bramka juz tam byla"; fi

# 3. Brak wyboru miejsca tresci przypisu.
if ! grep -q 'GetMarkdownFootnoteChapterEnd' /tmp/stary_541.cs; then
    ok "objaw 3: stary kod NIE ZNAL konca rozdzialu dla przypisu"
else bad "objaw 3 NIE odtworzony - metoda juz istniala"; fi
if grep -q 'Dialog.Input("Insert Footnote", "Footnote text"' /tmp/stary_541.cs; then
    ok "objaw 3b: stary kod pytal TYLKO o tresc, bez miejsca"
else bad "objaw 3b NIE odtworzony"; fi

# 4. Brak nazwy dla kontrolki edycyjnej komorki (zrodlo "Formant edytowania").
if ! grep -q 'EditingControlShowing' /tmp/stary_541_lbc.cs; then
    ok "objaw 4: stary Lbc.cs NIE nadawal nazwy kontrolce edycyjnej"
else bad "objaw 4 NIE odtworzony - zdarzenie juz bylo obsluzone"; fi

# KONTROLE, ze nie mierze rzeczy, ktorej nigdy nie bylo: to MUSI byc w starym.
for FRAZA in 'LbcGridCell' 'Insert Table' 'DescribeCell'; do
    if grep -q "$FRAZA" /tmp/stary_541.cs /tmp/stary_541_lbc.cs; then
        ok "KONTROLA POZYTYWNA: '$FRAZA' istnieje w starym kodzie"
    else bad "KONTROLA POZYTYWNA PADLA: brak '$FRAZA' w starym kodzie"; fi
done

# ---------- CZESC 2: objaw Ctrl+T na URUCHOMIONYM kodzie ----------
# Path.GetDirectoryName na tresci dokumentu MUSI rzucac - to jest przyczyna
# "Unexpected Event", ktore zglosil.  Mierzone na .NET, nie czytane z kodu.
mkdir -p /mnt/c/tmp/kn541
cat > /mnt/c/tmp/kn541/p.cs <<'CSEOF'
using System; using System.IO;
class P { static void Main() {
  string[] probki = {"On powiedzial \"tak\" i wyszedl.", "| Imie | Wiek |"};
  int rzucilo = 0;
  foreach (string s in probki) {
    try { Path.GetDirectoryName(s); }
    catch (ArgumentException) { rzucilo++; }
  }
  // KONTROLA POZYTYWNA: prawdziwa sciezka NIE moze rzucac.
  int sciezkaOk = 0;
  try { Path.GetDirectoryName(@"C:\tmp\plik.txt"); sciezkaOk = 1; } catch {}
  Console.WriteLine("RZUCILO=" + rzucilo + " SCIEZKA_OK=" + sciezkaOk);
} }
CSEOF
(cd /mnt/c/tmp/kn541 && "$CSC" /nologo /out:p.exe p.cs > /dev/null 2>&1)
sleep 3
WYNIK=$(/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\tmp\kn541 && p.exe" < /dev/null 2>&1 | tr -d '\r')
echo "   pomiar: $WYNIK"
case "$WYNIK" in
  *"RZUCILO=2"*) ok "objaw 2 ZMIERZONY: oba realne wiersze dokumentu wywalaja GetDirectoryName" ;;
  *) bad "objaw 2 NIE zmierzony: $WYNIK" ;;
esac
case "$WYNIK" in
  *"SCIEZKA_OK=1"*) ok "KONTROLA POZYTYWNA: prawdziwa sciezka NIE rzuca" ;;
  *) bad "KONTROLA POZYTYWNA PADLA: sciezka tez rzuca, sonda bez wartosci" ;;
esac

# ---------- BRAMKA: w repo MUSI zostac NOWA binarka ----------
# Rozstrzygamy po SUMIE, nie po kodzie wyjscia cmd.exe: interop WSL potrafi
# zwrocic 0 przy buildzie, ktory sie nie odpalil (zmierzone przy 5.0.38).
KONCOWA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
if [ "$KONCOWA" = "$NOWA_SUMA" ]; then
    ok "BRAMKA: w repo lezy NOWA binarka (${KONCOWA:0:24})"
else
    bad "BRAMKA: binarka w repo ZMIENILA SIE - przywracam"
    cp /tmp/EdSharpNG_nowa_kn541.exe EdSharpNG.exe
    echo "   NIE WYSYLAJ paczki bez powtorzenia buildu"
fi

echo
echo "WYNIK KONTROLI NEGATYWNEJ: $PASS/$((PASS+FAIL))"
[ "$FAIL" -eq 0 ] || exit 1
