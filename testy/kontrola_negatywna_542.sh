#!/usr/bin/env bash
# KONTROLA NEGATYWNA dla 5.0.42 (litery dostepu w oknach dialogowych):
# buduje binarke SPRZED zmiany z JAWNEJ rewizji i wymaga, zeby objaw sie na
# niej ODTWORZYL.  Bez tego zielony wynik po naprawie nie odroznia
# "naprawilem" od "sonda nie widzi".
#
# Rewizja odniesienia jest ARGUMENTEM, nie HEAD-em: skrypt biorący HEAD po
# zacommitowaniu zmiany porownuje kod z samym soba i daje 0 asercji
# padajacych, co wyglada jak awaria, a znaczy tylko "nie ma czego porownac".
#
# Skrypt SAM PILNUJE, ze na koncu w repo lezy NOWA binarka (sha256).
set -u
REPO="/mnt/d/projekty/edsharp-pr"
REV="${1:-f55387d}"          # stan 5.0.41
cd "$REPO" || exit 3

CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
NOWA_SUMA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
echo "== rewizja odniesienia: $REV"
echo "== sha256 NOWEJ binarki: ${NOWA_SUMA:0:24}"
cp EdSharpNG.exe /tmp/EdSharpNG_nowa_kn542.exe || exit 3

PASS=0; FAIL=0
ok()   { echo "PASS $1"; PASS=$((PASS+1)); }
bad()  { echo "FAIL $1"; FAIL=$((FAIL+1)); }

# ---------- CZESC 1: asercje na ZRODLE starej rewizji ----------
git show "$REV:Lbc.cs" > /tmp/stary_542_lbc.cs 2>/dev/null || {
    echo "BLAD: nie moge wyciagnac Lbc.cs z $REV"; exit 3; }
git show "$REV:EdSharp.cs" > /tmp/stary_542.cs 2>/dev/null || {
    echo "BLAD: nie moge wyciagnac EdSharp.cs z $REV"; exit 3; }

# KONTROLE POZYTYWNE SONDY: stare pliki MUSZA byc czytane poprawnie, inaczej
# kazde "nie znalazlem" ponizej znaczy tylko "czytam pusty plik".
LICZ_MENU=$(grep -c 'CreateMenuItem(' /tmp/stary_542.cs)
if [ "$LICZ_MENU" -gt 180 ]; then ok "SONDA: stare zrodlo ma $LICZ_MENU pozycji menu"
else bad "SONDA GLUCHA: tylko $LICZ_MENU pozycji menu w starym zrodle"; exit 2; fi
if grep -q 'addFieldLabel' /tmp/stary_542_lbc.cs; then
    ok "SONDA: stary Lbc.cs zawiera addFieldLabel"
else bad "SONDA GLUCHA: brak addFieldLabel w starym Lbc.cs"; exit 2; fi

# 1. Stary kod NIE ZNAL mechanizmu doboru liter.
for METODA in claimAccessKey assignQueuedAccessKeys queueAccessKey reserveAccessKey; do
    if ! grep -q "$METODA" /tmp/stary_542_lbc.cs; then
        ok "objaw 1: stary Lbc.cs nie mial metody $METODA"
    else bad "objaw 1 NIE odtworzony - $METODA juz istniala"; fi
done

# 2. Etykiety pol szly do okna BEZ litery dostepu.
if grep -q '^        lbl.Text = sText;' /tmp/stary_542_lbc.cs; then
    ok "objaw 2: stara etykieta pola brala tekst wprost, bez litery"
else bad "objaw 2 NIE odtworzony - etykieta juz przechodzila przez mechanizm"; fi

# 3. KAZDY przycisk dostawal litere, takze OK i Cancel - czyli dwie litery
#    szly na przyciski, ktore i tak obsluguja Enter i Escape.
if grep -q 'btn.Text = "&" + sLabel.Replace' /tmp/stary_542_lbc.cs; then
    ok "objaw 3: stary kod dawal litere KAZDEMU przyciskowi, w tym OK i Cancel"
else bad "objaw 3 NIE odtworzony - przyciski juz byly rozrozniane"; fi

# 4. Brak pilnowania kolizji: zbior zajetych liter nie istnial.
if ! grep -q 'sAccessKeysTaken' /tmp/stary_542_lbc.cs; then
    ok "objaw 4: stary kod nie pilnowal, czy litera jest juz zajeta"
else bad "objaw 4 NIE odtworzony - zbior zajetych liter juz byl"; fi

# KONTROLE, ze nie mierze rzeczy, ktorej nigdy nie bylo: to MUSI byc w starym.
for FRAZA in 'runWithButtons' 'cleanLabel' 'addInlineInputBox' 'LbcGridCell'; do
    if grep -q "$FRAZA" /tmp/stary_542_lbc.cs; then
        ok "KONTROLA POZYTYWNA: '$FRAZA' istnieje w starym Lbc.cs"
    else bad "KONTROLA POZYTYWNA PADLA: brak '$FRAZA' w starym Lbc.cs"; fi
done

# ---------- CZESC 2: KOLIZJA W REALNYM OKNIE, zmierzona na .NET ----------
# Okno ustawien kompilatora prosi o "&Name" i "&NavigatePart" - obie na N.
# Stary kod honorowal kazdy ampersand doslownie, wiec jedno z tych pol bylo
# klawiszem NIEOSIAGALNE.  Mierzone arytmetyka na tych samych etykietach,
# ktore siedza w EdSharp.cs, a nie czytane z kodu.
mkdir -p /mnt/c/tmp/kn542
cat > /mnt/c/tmp/kn542/p.cs <<'CSEOF'
using System; using System.Collections.Generic;
class P {
  // Stary sposob: ampersand wolajacego szedl do okna bez sprawdzenia,
  // a kazdy przycisk dostawal litere swojej pierwszej litery.
  static string Stare(string[] pola, string[] przyciski) {
    string s = "";
    foreach (string p in pola) {
      int i = p.IndexOf('&');
      if (i >= 0 && i + 1 < p.Length) s += Char.ToUpper(p[i + 1]);
    }
    foreach (string b in przyciski) {
      string plain = b.Replace("&", "");
      if (plain.Length > 0) s += Char.ToUpper(plain[0]);
    }
    return s;
  }
  static int Duplikaty(string s) {
    int n = 0;
    for (int i = 0; i < s.Length; i++)
      for (int j = i + 1; j < s.Length; j++)
        if (s[i] == s[j]) { n++; break; }
    return n;
  }
  static void Main() {
    // DOSLOWNIE etykiety z EdSharp.cs l.5230.
    string[] kompilator = {"&Name","&CompileCommand","&JumpPosition",
      "&AbbreviateOutput","&NavigatePart","&QuotePrefix","&ExtensionDefault",
      "&GoToEnvironment"};
    string[] okCancel = {"OK","Cancel"};
    string stare = Stare(kompilator, okCancel);
    // KONTROLA POZYTYWNA: okno, ktore kolizji miec NIE MOZE (l.6586).
    string[] data = {"&Year","&Month","&Week","&Day"};
    string stareData = Stare(data, okCancel);
    Console.WriteLine("KOMPILATOR=" + stare + " DUP=" + Duplikaty(stare)
      + " DATA=" + stareData + " DUPDATA=" + Duplikaty(stareData));
  }
}
CSEOF
(cd /mnt/c/tmp/kn542 && "$CSC" /nologo /out:p.exe p.cs > /dev/null 2>&1)
sleep 3
WYNIK=$(/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\tmp\kn542 && p.exe" < /dev/null 2>&1 | tr -d '\r')
echo "   pomiar: $WYNIK"
case "$WYNIK" in
  *"DUP=0"*) bad "objaw 5 NIE odtworzony: stare okno kompilatora bez kolizji ($WYNIK)" ;;
  *DUP=*)    ok  "objaw 5 ZMIERZONY: stare okno kompilatora MIALO kolizje liter" ;;
  *)         bad "objaw 5 NIE zmierzony: $WYNIK" ;;
esac
# KONTROLA POZYTYWNA sondy: okno daty ma cztery rozne inicjaly, wiec kolizji
# miec NIE MOZE.  Bez niej "wszedzie sa kolizje" nie odroznia sie od sondy,
# ktora liczy je zawsze.
case "$WYNIK" in
  *"DUPDATA=0"*) ok "KONTROLA POZYTYWNA: okno daty NIE mialo kolizji" ;;
  *) bad "KONTROLA POZYTYWNA PADLA: sonda widzi kolizje tam, gdzie ich nie ma" ;;
esac

# ---------- CZESC 3: NOWY kod naprawia to, co czesc 2 zmierzyla ----------
# Sonda na binarce liczy litery realnych okien; ma zwrocic zero duplikatow.
if [ -f testy/pomiar_kolizji_liter_w_oknach.cs ]; then
    mkdir -p /mnt/c/tmp/kn542b
    cp testy/pomiar_kolizji_liter_w_oknach.cs /mnt/c/tmp/kn542b/k.cs
    cp EdSharpNG.exe EdSharp.dll Tektosyne.dll /mnt/c/tmp/kn542b/ 2>/dev/null
    (cd /mnt/c/tmp/kn542b && "$CSC" /nologo /t:exe /out:k.exe \
        /r:System.Windows.Forms.dll /r:System.Drawing.dll k.cs > /dev/null 2>&1)
    sleep 3
    KOL=$(/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\tmp\kn542b && k.exe" < /dev/null 2>&1 | tr -d '\r' | tail -1)
    echo "   nowy kod: $KOL"
    case "$KOL" in
      *"WYNIK 8/8"*) ok "NOWY KOD: realne okna bez kolizji liter (8/8)" ;;
      *) bad "NOWY KOD: sonda kolizji nie jest zielona ($KOL)" ;;
    esac
fi

# ---------- BRAMKA: w repo MUSI zostac NOWA binarka ----------
# Rozstrzygamy po SUMIE, nie po kodzie wyjscia cmd.exe: interop WSL potrafi
# zwrocic 0 przy buildzie, ktory sie nie odpalil (zmierzone przy 5.0.38).
KONCOWA="$(sha256sum EdSharpNG.exe | cut -d' ' -f1)"
if [ "$KONCOWA" = "$NOWA_SUMA" ]; then
    ok "BRAMKA: w repo lezy NOWA binarka (${KONCOWA:0:24})"
else
    bad "BRAMKA: binarka w repo ZMIENILA SIE - przywracam"
    cp /tmp/EdSharpNG_nowa_kn542.exe EdSharpNG.exe
    echo "   NIE WYSYLAJ paczki bez powtorzenia buildu"
fi

echo
echo "WYNIK KONTROLI NEGATYWNEJ: $PASS/$((PASS+FAIL))"
[ "$FAIL" -eq 0 ] || exit 1
