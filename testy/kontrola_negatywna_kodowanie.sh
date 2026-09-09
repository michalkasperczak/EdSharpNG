#!/usr/bin/env bash
# Kontrola negatywna naprawy kodowania ANSI (punkt 3 mapy drogowej, 5.0.36).
#
# Sama sonda pomiar_kodowania_ansi.cs, przechodzaca na NOWEJ binarce, nie
# dowodzi niczego: przechodzilaby takze, gdyby mierzyla nie to, co trzeba.
# Rozstrzyga PRZEBIEG NA BINARCE SPRZED NAPRAWY - tam objaw zgloszenia MUSI
# sie odtworzyc. Zmierzone 28.08.2026: stara binarka daje 13/16 (trzy asercje
# o polskim pliku ANSI padaja), nowa 20/20.
#
# UWAGA: po tym skrypcie w repo lezalaby binarka ze STAREGO kodu, dlatego
# nowa jest zachowywana i przywracana na koncu. Przed wysylka paczki sprawdz
# sha256 EdSharpNG.exe wobec logu buildu instalatora.
set -u
REPO=/mnt/d/projekty/edsharp-pr
PROBA=/mnt/c/tmp/enc
CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"

cd "$REPO" || exit 2

echo "== przygotowanie probek =="
mkdir -p "$PROBA"
cp Ude.dll "$PROBA/" || exit 2
python3 - <<'PY' || exit 2
import os
d = "/mnt/c/tmp/enc"
t = ("Zazolc gesla jazn.\n" .replace("Zazolc gesla jazn",
     "Za\u017c\u00f3\u0142\u0107 g\u0119\u015bl\u0105 ja\u017a\u0144"))
t += ("\u0141\u00f3d\u017a, \u0107ma, \u0144.\nDluzszy polski tekst, zeby detektor "
      "mial na czym pracowac: \u0105\u0107\u0119\u0142\u0144\u00f3\u015b\u017a\u017c "
      "\u0104\u0106\u0118\u0141\u0143\u00d3\u015a\u0179\u017b.\n")
w = t.replace("\n", "\r\n")
open(os.path.join(d, "pl_cp1250.txt"), "wb").write(w.encode("cp1250"))
open(os.path.join(d, "pl_utf8_nobom.txt"), "wb").write(w.encode("utf-8"))
open(os.path.join(d, "pl_utf8_bom.txt"), "wb").write(b"\xef\xbb\xbf" + w.encode("utf-8"))
ru = "\u042d\u0442\u043e \u0440\u0443\u0441\u0441\u043a\u0438\u0439 \u0442\u0435\u043a\u0441\u0442, \u043d\u0430\u043f\u0438\u0441\u0430\u043d\u043d\u044b\u0439 \u0434\u043b\u044f \u043f\u0440\u043e\u0432\u0435\u0440\u043a\u0438 \u043e\u043f\u0440\u0435\u0434\u0435\u043b\u0435\u043d\u0438\u044f \u043a\u043e\u0434\u0438\u0440\u043e\u0432\u043a\u0438.\r\n"
open(os.path.join(d, "ru_cp1251.txt"), "wb").write(ru.encode("cp1251"))
de = "Die Stra\u00dfe war sch\u00f6n, die B\u00e4ume gr\u00fcn. Caf\u00e9, na\u00efve, \u0153uvre, \u00c5ngstr\u00f6m.\r\n"
open(os.path.join(d, "de_cp1252.txt"), "wb").write(de.encode("cp1252"))
print("probki gotowe")
PY

cp testy/pomiar_kodowania_ansi.cs "$PROBA/pomiar.cs" || exit 2
# PULAPKA: csc.exe nie rozumie sciezek /mnt/... (fatal error CS2007).
# Kompiluj Z KATALOGU probki, podajac SAME NAZWY plikow.
( cd "$PROBA" && "$CSC" /nologo /t:exe /out:pomiar.exe pomiar.cs ) > /dev/null || exit 2

echo
echo "== 1. NOWA binarka: sonda MUSI przejsc =="
cp EdSharpNG.exe /tmp/EdSharpNG_nowy.exe || exit 2
cp EdSharpNG.exe "$PROBA/" || exit 2
/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\\tmp\\enc && chcp 65001 >nul && pomiar.exe" < /dev/null 2>&1 | tail -3
NOWA_OK=$?

echo
echo "== 2. STARA binarka: objaw MUSI sie odtworzyc =="
git stash push -- EdSharp.cs > /dev/null || { echo "SONDA GLUCHA: nie udalo sie odlozyc zmiany"; exit 2; }
/mnt/c/Windows/System32/cmd.exe /c "cd /d D:\\projekty\\edsharp-pr && BuildEdSharp.cmd" < /dev/null > /dev/null 2>&1
cp EdSharpNG.exe "$PROBA/" || exit 2
WYNIK_STARY=$(/mnt/c/Windows/System32/cmd.exe /c "cd /d C:\\tmp\\enc && chcp 65001 >nul && pomiar.exe" < /dev/null 2>&1)
echo "$WYNIK_STARY" | grep -E "FAIL|WYNIK|KONTROLA POZYTYWNA"

echo
echo "== 3. przywrocenie stanu =="
git stash pop > /dev/null || echo "UWAGA: przywroc EdSharp.cs recznie z git stash"
cp /tmp/EdSharpNG_nowy.exe EdSharpNG.exe || exit 2
cp EdSharpNG.exe "$PROBA/" || exit 2
echo "sha256 przywroconej binarki: $(sha256sum EdSharpNG.exe | cut -d' ' -f1)"

echo
if echo "$WYNIK_STARY" | grep -q "FAIL  pl_cp1250.txt"; then
  echo "WYNIK: kontrola negatywna WAZNA - stara binarka gubi polskie litery w pliku ANSI."
  exit 0
else
  echo "WYNIK: SONDA GLUCHA - stara binarka NIE odtworzyla objawu, wiec nowy wynik nic nie dowodzi."
  exit 2
fi
