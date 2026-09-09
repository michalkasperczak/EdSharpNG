#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Pomiar: czy PRZYPISY przezywaja eksport (zlecenie 1787832545532-3).

Kasperczak postawil to jako wymaganie funkcjonalne, nie kosmetyke: przypisy
maja przezyc eksport tak samo jak linki spisu tresci. Mierzymy WLASNYMI
narzedziami z katalogu Convert - tymi, ktore realnie jada w paczce u niego,
a nie dowolnym pandokiem z systemu.

CO MIERZYMY:
  1. 2htm.exe (Markdig, nasz podglad w przegladarce pod F5): znacznik w
     zdaniu musi stac sie odsylaczem do tresci, a tresc odsylaczem POWROTNYM.
  2. pandoc.exe -> docx: przypis musi wejsc w PRAWDZIWY mechanizm przypisow
     Worda (footnotes.xml + odwolanie w document.xml), a nie zostac golym
     tekstem "[^1]" w akapicie.
  3. pandoc.exe -> html: to samo w wersji HTML.

KONTROLA WAZNOSCI (bez niej zielony wynik nic nie dowodzi): plik kontrolny
BEZ przypisow musi dac ZERO przypisow w docx. Gdyby sonda liczyla cokolwiek
i tam, jej trafienia na pliku z przypisami nie znaczylyby nic.
"""
import os
import re
import shutil
import subprocess
import sys
import zipfile

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PANDOC = os.path.join(REPO, "Convert", "Pandoc", "pandoc.exe")
TOHTM = os.path.join(REPO, "Convert", "2htm", "2htm.exe")
# cmd.exe nie wchodzi na sciezki UNC z /tmp WSL, wiec pracujemy na dysku C:
WORK_WIN = r"C:\tmp\fn_pomiar"
WORK = "/mnt/c/tmp/fn_pomiar"

DOC = (
    "# Dokument probny\n"
    "\n"
    "Zdanie z przypisem[^1] oraz drugie zdanie[^2].\n"
    "\n"
    "## Rozdzial\n"
    "\n"
    "Akapit bez przypisu.\n"
    "\n"
    "[^1]: Pierwsza tresc przypisu.\n"
    "[^2]: Druga tresc przypisu.\n"
)
DOC_KONTROLNY = (
    "# Dokument probny\n"
    "\n"
    "Zdanie bez zadnego przypisu.\n"
    "\n"
    "## Rozdzial\n"
    "\n"
    "Akapit takze bez przypisu.\n"
)

iPass = 0
iFail = 0


def ok(warunek, opis, got=""):
    global iPass, iFail
    if warunek:
        iPass += 1
        print("PASS  " + opis)
    else:
        iFail += 1
        print("FAIL  " + opis + ("  -> " + str(got) if got else ""))


def cmd(polecenie):
    return subprocess.run(
        ["/mnt/c/Windows/System32/cmd.exe", "/c", polecenie],
        capture_output=True, text=True, stdin=subprocess.DEVNULL, timeout=180,
    )


def docx_przypisy(sciezka):
    """Ile przypisow Worda jest w pliku i ile odwolan do nich w tresci."""
    if not os.path.exists(sciezka):
        return None, None, ""
    with zipfile.ZipFile(sciezka) as z:
        nazwy = z.namelist()
        if "word/footnotes.xml" not in nazwy:
            return 0, 0, "brak word/footnotes.xml"
        fn = z.read("word/footnotes.xml").decode("utf-8", "replace")
        doc = z.read("word/document.xml").decode("utf-8", "replace")
    # Word trzyma dwa przypisy techniczne o id -1 i 0 (separatory) - te nie sa
    # trescia dokumentu i nie wolno ich liczyc.
    ids = [int(x) for x in re.findall(r'<w:footnote[^>]*w:id="(-?\d+)"', fn)]
    tresciowe = [i for i in ids if i > 0]
    odwolania = re.findall(r"<w:footnoteReference", doc)
    return len(tresciowe), len(odwolania), fn


def main():
    if not os.path.exists(PANDOC):
        print("BRAK " + PANDOC)
        return 3
    if not os.path.exists(TOHTM):
        print("BRAK " + TOHTM)
        return 3

    if os.path.isdir(WORK):
        shutil.rmtree(WORK)
    os.makedirs(WORK)
    for nazwa, tresc in (("probka.md", DOC), ("kontrolna.md", DOC_KONTROLNY)):
        with open(os.path.join(WORK, nazwa), "w", encoding="utf-8", newline="\r\n") as f:
            f.write(tresc)
    shutil.copy(TOHTM, WORK)
    shutil.copy(PANDOC, WORK)

    # ---------- 1. NASZ PODGLAD W PRZEGLADARCE (2htm / Markdig) ----------
    r = cmd('cd /d {} && 2htm.exe probka.md'.format(WORK_WIN))
    htm = os.path.join(WORK, "probka.htm")
    if not os.path.exists(htm):
        htm = os.path.join(WORK, "probka.html")
    tresc_htm = ""
    if os.path.exists(htm):
        tresc_htm = open(htm, encoding="utf-8", errors="replace").read()
    ok(bool(tresc_htm), "2htm.exe wyprodukowal plik", r.stdout + r.stderr)
    ok('class="footnote-ref"' in tresc_htm or 'href="#fn:1"' in tresc_htm,
       "2htm: znacznik w zdaniu jest ODSYLACZEM do tresci",
       "brak odsylacza w tresci htm")
    ok('class="footnotes"' in tresc_htm or 'id="fn:1"' in tresc_htm,
       "2htm: tresci przypisow w osobnej sekcji na koncu", "brak sekcji")
    ok("&#8617;" in tresc_htm or 'class="footnote-back-ref"' in tresc_htm,
       "2htm: z tresci prowadzi odsylacz POWROTNY do zdania", "brak powrotu")
    ok("[^1]" not in tresc_htm,
       "2htm: surowa skladnia [^1] NIE zostala w wyniku", "zostala golym tekstem")

    # ---------- 2. EKSPORT DO WORDA ----------
    r = cmd('cd /d {} && pandoc.exe -f gfm+footnotes -t docx -o probka.docx probka.md'.format(WORK_WIN))
    docx = os.path.join(WORK, "probka.docx")
    ok(os.path.exists(docx), "pandoc zrobil plik docx", r.stdout + r.stderr)
    n_fn, n_ref, dump = docx_przypisy(docx)
    ok(n_fn == 2, "docx: dwa PRAWDZIWE przypisy Worda", "policzono " + str(n_fn) + " (" + str(dump)[:120] + ")")
    ok(n_ref == 2, "docx: dwa odwolania do przypisow w tresci", "policzono " + str(n_ref))
    ok("Pierwsza tresc przypisu." in (dump or ""), "docx: tresc pierwszego przypisu na miejscu", "brak")
    ok("Druga tresc przypisu." in (dump or ""), "docx: tresc drugiego przypisu na miejscu", "brak")
    # Najwazniejsze dla niego: przypis NIE moze zostac golym tekstem w akapicie.
    with zipfile.ZipFile(docx) as z:
        doc_xml = z.read("word/document.xml").decode("utf-8", "replace")
    ok("[^1]" not in doc_xml and "[^2]" not in doc_xml,
       "docx: surowa skladnia [^1] NIE wyladowala w tresci akapitu", "wyladowala")

    # KONTROLA WAZNOSCI: bez przypisow ma byc ZERO.
    r = cmd('cd /d {} && pandoc.exe -f gfm+footnotes -t docx -o kontrolna.docx kontrolna.md'.format(WORK_WIN))
    k_fn, k_ref, _ = docx_przypisy(os.path.join(WORK, "kontrolna.docx"))
    ok(k_fn == 0 and k_ref == 0,
       "KONTROLA: dokument bez przypisow daje ZERO przypisow w docx",
       "przypisy=" + str(k_fn) + " odwolania=" + str(k_ref))

    # ---------- 3. EKSPORT DO HTML ----------
    r = cmd('cd /d {} && pandoc.exe -f gfm+footnotes -t html -o probka_pandoc.html probka.md'.format(WORK_WIN))
    ph = os.path.join(WORK, "probka_pandoc.html")
    tresc_ph = open(ph, encoding="utf-8", errors="replace").read() if os.path.exists(ph) else ""
    ok(bool(tresc_ph), "pandoc zrobil plik html", r.stdout + r.stderr)
    ok('href="#fn1"' in tresc_ph or 'class="footnote-ref"' in tresc_ph,
       "html: znacznik jest odsylaczem do tresci", "brak")
    ok('id="fn1"' in tresc_ph or 'class="footnotes"' in tresc_ph,
       "html: tresci przypisow na koncu", "brak")
    ok('href="#fnref1"' in tresc_ph or "footnote-back" in tresc_ph,
       "html: odsylacz powrotny do zdania", "brak")

    print()
    print("PASS: {}   FAIL: {}".format(iPass, iFail))
    if iFail == 0:
        print("WYNIK: PRZYPISY PRZEZYWAJA EKSPORT (podglad, Word, html)")
    return 0 if iFail == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
