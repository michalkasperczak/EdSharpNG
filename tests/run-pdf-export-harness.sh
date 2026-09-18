#!/usr/bin/env bash
# Kompiluje i uruchamia program testowy PdfExport frameworkowym csc.
#
# DWIE PULAPKI, KTORE TO OBCHODZI:
#  - pliki w /tmp WSL sa dla Windows pod UNC (\\wsl.localhost\...), a csc.exe i
#    wlasna binarka nie ruszaja z takiej sciezki; caly staging idzie na /mnt/c,
#  - staging jest WLASNY (C:\EdSharpPdfTest), nie C:\EdSharpBuild uzywany przez
#    build instalatora.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STAGE_WIN='C:\EdSharpPdfTest\harness'
STAGE=/mnt/c/EdSharpPdfTest/harness
CSC="/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"

[ -x "$CSC" ] || { echo "BLAD: brak $CSC"; exit 1; }

rm -rf "$STAGE"
mkdir -p "$STAGE"
cp "$REPO/PdfExport.cs" "$REPO/tests/PdfExportHarness.cs" "$STAGE/"

cd "$STAGE"
"$CSC" /nologo /target:exe /platform:anycpu /langversion:5 /out:PdfExportHarness.exe \
  PdfExport.cs PdfExportHarness.cs
rc=0
./PdfExportHarness.exe 'C:\EdSharpPdfTest' > "$STAGE/wynik.txt" 2>&1 || rc=$?
cat "$STAGE/wynik.txt"
echo "kod wyjscia harnessu: $rc"

# TRESC I OTAGOWANIE PDF sprawdza Python (pypdf) - csc tego nie umie.
# Sciezke gotowego PDF podaje sam harness.
PDF=$(sed -n 's/^PDF-DO-SPRAWDZENIA: //p' "$STAGE/wynik.txt" | tr -d '\r' | head -1)
if [ -n "$PDF" ]; then
  PDF_WSL="/mnt/c/${PDF#C:\\}"
  PDF_WSL="${PDF_WSL//\\//}"
  PY="${PDF_VENV_PYTHON:-/tmp/pdfvenv/bin/python}"
  if [ -x "$PY" ] && "$PY" -c 'import pypdf' 2>/dev/null; then
    "$PY" "$REPO/tests/sprawdz_pdf.py" "$PDF_WSL" || rc=1
  else
    echo "POMINIETE sprawdzenie tresci PDF: brak Pythona z pypdf ($PY)"
    echo "  utworz: uv venv /tmp/pdfvenv && uv pip install --python /tmp/pdfvenv/bin/python pypdf"
    rc=1
  fi
  rm -f "$PDF_WSL"
fi
exit $rc
