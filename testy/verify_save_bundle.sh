#!/usr/bin/env bash
# WERYFIKACJA NIEZALEZNA PACZKI ZAPISU/PDF (harnessy save + PDF razem).
#
# Uruchamia NA CZYSTO, w izolowanym stagingu C:\EdSharpRegressionTest:
#   1. tests/PdfExportHarness.cs          - PdfExport przez zainstalowany Edge
#   2. tests/sprawdz_pdf.py               - tresc i OTAGOWANIE PDF z harnessu (pypdf)
#   3. testy/harness_zapis_formatow.cs    - konwersje przez PRAWDZIWY Pandoc
#   4. testy/harness_zapis_bezpieczny_5114.cs - odpornosc transakcji zapisu
#   5. testy/harness_integracja_zapisu_5114.cs - zapis przez GOTOWY EdSharpNG.exe
#   6. kontrola PDF ZLOZONEGO PRZEZ EXE   - tagi/H1/link/polskie litery (pypdf)
#
# PULAPKI, KTORE TO OBCHODZI (zmierzone 18.09.2026):
#  - /tmp WSL jest dla Windows pod UNC (\\wsl.localhost\...) - csc.exe i wlasne
#    binarki NIE ruszaja z takiej sciezki; caly staging idzie na /mnt/c,
#  - staging jest WLASNY, nie C:\EdSharpBuild (tam pracuje build instalatora)
#    ani C:\EdSharpSaveTest (stary katalog z zaszytymi sciezkami),
#  - ZapisFormatow.cs uzywa System.IO.Compression (ZipArchive) - BEZ dwoch
#    /reference:System.IO.Compression*.dll harness NIE SKOMPILUJE SIE
#    (komentarz z naglowka harness_zapis_formatow.cs jest w tym punkcie martwy),
#  - harnessy maja zaszyte C:\EdSharpSaveTest - kopie w stagingu dostaja
#    podmienione WYLACZNIE sciezki, zadnej logiki.
#
# NIE dotyka GUI ani czytnika ekranu.  Otagowania PDF csc nie umie sprawdzic,
# dlatego robi to Python z pypdf.
#
# Uzycie:
#   testy/verify_save_bundle.sh                       # exe z C:\EdSharpBuild
#   EXE=/mnt/c/gdzies/EdSharpNG.exe testy/verify_save_bundle.sh
set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
R_WSL=/mnt/c/EdSharpRegressionTest
R_WIN='C:\EdSharpRegressionTest'
CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
PY="${PDF_VENV_PYTHON:-/tmp/pdfvenv/bin/python}"
EXE="${EXE:-/mnt/c/EdSharpBuild/EdSharpNG.exe}"
BUILD_DIR="$(dirname "$EXE")"
REFS='/reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll'

rc=0
fail() { echo "BLAD: $*"; rc=1; }

[ -x "$CSC" ]  || { echo "BLAD: brak $CSC"; exit 1; }
[ -f "$EXE" ]  || { echo "BLAD: brak $EXE"; exit 1; }
"$PY" -c 'import pypdf' 2>/dev/null || {
  echo "BLAD: brak Pythona z pypdf ($PY)"
  echo "  utworz: uv venv /tmp/pdfvenv && uv pip install --python /tmp/pdfvenv/bin/python pypdf"
  exit 1
}

rm -rf "$R_WSL"
mkdir -p "$R_WSL"/{pdf,formats,hardening,build,logi}
L="$R_WSL/logi"

echo "== TOZSAMOSC MIERZONEGO ARTEFAKTU =="
( cd "$REPO" && git log -1 --format='commit: %H %ci%n  %s' )
echo "exe:    $EXE"
echo "md5:    $(md5sum "$EXE" | cut -d' ' -f1)"
echo "mtime:  $(date -r "$EXE" '+%F %T')"
# Binarka STARSZA niz zrodlo nie mierzy tego, co jest w repo.
nowsze=$(find "$REPO" -maxdepth 1 -name '*.cs' -newer "$EXE" -printf '%p ')
[ -n "$nowsze" ] && fail "zrodla NOWSZE niz exe (binarka nieaktualna): $nowsze"
echo

# --- staging: zrodla pomocnicze + Pandoc + exe z zasobami --------------------
cp "$REPO/PdfExport.cs" "$REPO/tests/PdfExportHarness.cs"                 "$R_WSL/pdf/"
cp "$REPO/ZapisFormatow.cs" "$REPO/testy/harness_zapis_formatow.cs"       "$R_WSL/formats/"
cp "$REPO/EdSharp.ini"                                                    "$R_WSL/formats/EdSharp.ini"
cp "$REPO/ZapisFormatow.cs" "$REPO/testy/harness_zapis_bezpieczny_5114.cs" "$R_WSL/hardening/"
cp "$REPO/testy/harness_integracja_zapisu_5114.cs"                        "$R_WSL/build/"
cp "$EXE" "$BUILD_DIR"/*.dll "$BUILD_DIR/EdSharp.ini"                     "$R_WSL/build/"
for d in formats hardening build; do
  mkdir -p "$R_WSL/$d/Convert/Pandoc"
  cp "$BUILD_DIR/Convert/Pandoc/pandoc.exe" "$R_WSL/$d/Convert/Pandoc/"
done
# Zaszyte C:\EdSharpSaveTest -> staging.  TYLKO sciezki, zadnej logiki.
# NIE sed: wzorce ze zdwojonymi backslashami przechodza przez trzy poziomy
# cytowania i CICHO nie trafiaja - wtedy harness pisze do WSPOLNEGO
# C:\EdSharpSaveTest i pomiar NIE JEST izolowany.  Zmierzone 18.09.2026.
"$PY" - "$R_WSL" <<'PYEOF'
import io, sys
R = sys.argv[1]
def podmien(path, pary):
    with io.open(path, "r", encoding="utf-8") as f:
        t = f.read()
    for a, b in pary:
        t = t.replace(a, b)
    with io.open(path, "w", encoding="utf-8", newline="") as f:
        f.write(t)
    assert "EdSharpSaveTest" not in t, "ZOSTALA stara sciezka w " + path

podmien(R + "/formats/harness_zapis_formatow.cs", [
    (r"C:\EdSharpSaveTest\EdSharp.ini", r"C:\EdSharpRegressionTest\formats\EdSharp.ini"),
    (r"C:\EdSharpSaveTest",             r"C:\EdSharpRegressionTest\formats"),
    ("/mnt/c/EdSharpSaveTest",          "/mnt/c/EdSharpRegressionTest/formats"),
])
podmien(R + "/hardening/harness_zapis_bezpieczny_5114.cs", [
    (r"C:\EdSharpSaveTest\hardening",     r"C:\EdSharpRegressionTest\hardening\praca"),
    (r"C:\EdSharpSaveTest\praca_harness", r"C:\EdSharpRegressionTest\formats\praca_harness"),
    (r"C:\\EdSharpSaveTest",              r"C:\\EdSharpRegressionTest\\hardening"),
    (r"C:\EdSharpSaveTest",               r"C:\EdSharpRegressionTest\hardening"),
])
print("sciezki podmienione na staging (zero wystapien EdSharpSaveTest)")
PYEOF
[ $? -eq 0 ] || { fail "podmiana sciezek na staging NIE UDALA SIE - przerywam, zeby nie pisac do wspolnego katalogu"; echo "CALOSC: FAIL"; exit 1; }

# --- 1+2. PdfExport przez Edge, potem tresc i tagi PDF -----------------------
echo "== 1. PdfExportHarness (Edge) =="
( cd "$R_WSL/pdf" && "$CSC" /nologo /target:exe /platform:anycpu /langversion:5 \
    /out:PdfExportHarness.exe PdfExport.cs PdfExportHarness.cs ) > "$L/pdf-compile.log" 2>&1 \
  || { cat "$L/pdf-compile.log"; fail "kompilacja PdfExportHarness"; }
if [ -x "$R_WSL/pdf/PdfExportHarness.exe" ]; then
  ( cd "$R_WSL/pdf" && ./PdfExportHarness.exe "$R_WIN\\pdf\\wyniki" ) > "$L/pdf-harness.log" 2>&1 \
    || fail "PdfExportHarness zglosil bledy"
  tail -3 "$L/pdf-harness.log"
  PDF=$(sed -n 's/^PDF-DO-SPRAWDZENIA: //p' "$L/pdf-harness.log" | tr -d '\r' | head -1)
  if [ -n "$PDF" ]; then
    PDF_WSL="/mnt/c/${PDF#C:\\}"; PDF_WSL="${PDF_WSL//\\//}"
    echo "== 2. tresc i tagi PDF z harnessu (pypdf) =="
    "$PY" "$REPO/tests/sprawdz_pdf.py" "$PDF_WSL" > "$L/pdf-pypdf.log" 2>&1 \
      || fail "sprawdz_pdf.py zglosil bledy"
    tail -2 "$L/pdf-pypdf.log"
  else fail "harness nie podal sciezki PDF-DO-SPRAWDZENIA"; fi
fi
echo

# --- 3. konwersje formatow prawdziwym Pandokiem ------------------------------
echo "== 3. harness_zapis_formatow (Pandoc) =="
( cd "$R_WSL/formats" && "$CSC" /nologo /target:exe /platform:anycpu /langversion:5 $REFS \
    /out:harness_formatow.exe harness_zapis_formatow.cs ZapisFormatow.cs ) > "$L/formats-compile.log" 2>&1 \
  || { cat "$L/formats-compile.log"; fail "kompilacja harness_zapis_formatow"; }
if [ -x "$R_WSL/formats/harness_formatow.exe" ]; then
  ( cd "$R_WSL/formats" && ./harness_formatow.exe ) > "$L/formats-harness.log" 2>&1 \
    || fail "harness_zapis_formatow zglosil FAIL"
  grep -E '^PASS: [0-9]+' "$L/formats-harness.log" | tail -1
fi
echo

# --- 4. odpornosc transakcji zapisu -----------------------------------------
echo "== 4. harness_zapis_bezpieczny_5114 =="
mkdir -p "$R_WSL/hardening/praca"
( cd "$R_WSL/hardening" && "$CSC" /nologo /target:exe /platform:anycpu /langversion:5 $REFS \
    /out:SaveSafety.exe harness_zapis_bezpieczny_5114.cs ZapisFormatow.cs ) > "$L/hardening-compile.log" 2>&1 \
  || { cat "$L/hardening-compile.log"; fail "kompilacja harness_zapis_bezpieczny"; }
if [ -x "$R_WSL/hardening/SaveSafety.exe" ]; then
  ( cd "$R_WSL/hardening" && ./SaveSafety.exe ) > "$L/hardening-harness.log" 2>&1 \
    || fail "harness_zapis_bezpieczny zglosil FAIL"
  tail -1 "$L/hardening-harness.log"
fi
echo

# --- 5. zapis przez GOTOWY EdSharpNG.exe ------------------------------------
echo "== 5. harness_integracja_zapisu_5114 (gotowy exe) =="
( cd "$R_WSL/build" && "$CSC" /nologo /target:exe /platform:anycpu /langversion:5 \
    /out:SaveIntegration.exe harness_integracja_zapisu_5114.cs ) > "$L/integracja-compile.log" 2>&1 \
  || { cat "$L/integracja-compile.log"; fail "kompilacja harness_integracja"; }
if [ -x "$R_WSL/build/SaveIntegration.exe" ]; then
  ( cd "$R_WSL/build" && ./SaveIntegration.exe "$R_WIN\\build\\EdSharpNG.exe" "$R_WIN\\build\\EdSharp.ini" ) \
    > "$L/integracja-harness.log" 2>&1 || fail "harness_integracja zglosil FAIL"
  tail -1 "$L/integracja-harness.log"
fi
echo

# --- 6. PDF ZLOZONY PRZEZ EXE: tagi, naglowek, link, polskie litery ----------
# WLASNA sonda, NIE tests/sprawdz_pdf.py: tamta asercjuje probke PdfExportHarness
# (naglowek H2, adres example.org/sciezka), ktorej fixture harnessu integracyjnego
# NIE MA - uzyta tutaj dawalaby FALSZYWY BLAD.  Zmierzone 18.09.2026.
cat > "$L/sprawdz_pdf_z_exe.py" <<'PYEOF'
import sys
from pypdf import PdfReader
TEKST = ["Żółty nagłówek", "Zażółć gęślą jaźń", "chleb", "mleko"]
TAGI = {"H1", "P", "L", "LI", "Link"}
def zbierz(o, zb, d=0):
    if d > 60: return
    try: o = o.get_object()
    except Exception: return
    if isinstance(o, list):
        for e in o: zbierz(e, zb, d + 1)
        return
    if not hasattr(o, "get"): return
    t = o.get("/S")
    if t is not None: zb.add(str(t).lstrip("/"))
    k = o.get("/K")
    if k is not None: zbierz(k, zb, d + 1)
p = sys.argv[1]; bledy = []
with open(p, "rb") as f:
    if f.read(5) != b"%PDF-": bledy.append("brak podpisu %PDF-")
r = PdfReader(p)
print("plik: %s" % p); print("stron: %d" % len(r.pages))
tekst = "\n".join(pg.extract_text() or "" for pg in r.pages)
for frag in TEKST:
    if frag in tekst: print("OK   tekst obecny: %r" % frag)
    else: bledy.append("brak tekstu %r" % frag); print("BLAD brak tekstu: %r" % frag)
kat = r.trailer["/Root"]; mi = kat.get("/MarkInfo")
marked = bool(mi.get("/Marked")) if mi else False
print("OK   /MarkInfo /Marked = True" if marked else "BLAD PDF nie jest tagged")
if not marked: bledy.append("/Marked nie jest true")
root = kat.get("/StructTreeRoot"); typy = set()
if root is None: bledy.append("brak /StructTreeRoot"); print("BLAD brak /StructTreeRoot")
else:
    print("OK   /StructTreeRoot obecny")
    zbierz(root.get_object().get("/K"), typy)
    print("typy struktur: %s" % ", ".join(sorted(typy)))
for t in sorted(TAGI):
    if t in typy: print("OK   struktura %s obecna" % t)
    else: bledy.append("brak struktury %s" % t); print("BLAD brak struktury %s" % t)
adresy = []
for pg in r.pages:
    for a in pg.get("/Annots") or []:
        a = a.get_object(); akcja = a.get("/A")
        if akcja and akcja.get_object().get("/URI"): adresy.append(str(akcja.get_object()["/URI"]))
print("adresy linkow: %s" % adresy)
if any("example.org" in u for u in adresy): print("OK   odnosnik zachowal adres")
else: bledy.append("link nie zachowal adresu"); print("BLAD link nie zachowal adresu")
lang = kat.get("/Lang"); print("jezyk dokumentu /Lang: %r" % (str(lang) if lang else None))
if not (lang and str(lang).lower().startswith("pl")):
    print("UWAGA /Lang nie jest polski (czytnik moze czytac obcym glosem)")
print("---")
if bledy:
    print("BLEDOW: %d" % len(bledy))
    for b in bledy: print("  - %s" % b)
    sys.exit(1)
print("wszystko sie zgadza")
PYEOF
echo "== 6. PDF z gotowego exe (pypdf) =="
EXE_PDF=$(find "$R_WSL/build" -name '*.pdf' | head -1)
if [ -n "$EXE_PDF" ]; then
  "$PY" "$L/sprawdz_pdf_z_exe.py" "$EXE_PDF" > "$L/pdf-z-exe-pypdf.log" 2>&1 \
    || fail "PDF z exe: sonda zglosila bledy (patrz $L/pdf-z-exe-pypdf.log)"
  grep -E '^(OK|BLAD) +(struktura|/MarkInfo|/StructTreeRoot|odnosnik|tekst)' "$L/pdf-z-exe-pypdf.log" || true
else fail "exe nie zostawil zadnego PDF do sprawdzenia"; fi
echo

echo "=================================================="
if [ $rc -eq 0 ]; then echo "CALOSC: PASS.  Logi: $R_WSL/logi/"
else echo "CALOSC: FAIL - czytaj logi w $R_WSL/logi/"; fi
echo "Mowa czytnika ekranu NIE byla tu mierzona (harnessy nie dotykaja GUI)."
echo "=================================================="
exit $rc
