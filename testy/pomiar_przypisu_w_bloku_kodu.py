#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Pomiar HIPOTEZY O DEFEKCIE (zlecenie 1787832545532-3, iteracja 2).

HIPOTEZA: Ctrl+F6 nie sprawdza, GDZIE stoi kursor, wiec znacznik da sie
wstawic do bloku kodu albo do tresci innego przypisu. Nasz WLASNY odczyt
znacznikow blok kodu POMIJA (zmierzone w pomiar_przypisow.cs), wiec program
powiedzialby "Footnote N inserted", a Alt+F6 zaraz potem "nie ma znacznika".
Czyli komunikat sukcesu klamie.

Zanim cokolwiek zmienimy w programie, pytamy NARZEDZI, ktore realnie jada
w paczce u Kasperczaka (Convert/2htm = nasz podglad, Convert/Pandoc = eksport),
co one robia z takim dokumentem. Nie zgadujemy, jak "chyba dziala Markdown".

MIERZYMY TRZY DOKUMENTY:
  A. znacznik w bloku kodu (```) + tresc na koncu,
  B. znacznik W TRESCI innego przypisu (przypis w przypisie),
  C. KONTROLA: ten sam znacznik w zwyklym zdaniu - MUSI dac zywy przypis,
     inaczej sonda jest gluchа i wyniki A i B nic nie znacza.
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
WORK_WIN = r"C:\tmp\fn_blok"
WORK = "/mnt/c/tmp/fn_blok"

A_BLOK = (
    "# Dokument\n"
    "\n"
    "```\n"
    "przyklad[^1] w kodzie\n"
    "```\n"
    "\n"
    "[^1]: Tresc przypisu z bloku kodu.\n"
)
B_NESTED = (
    "# Dokument\n"
    "\n"
    "Zdanie z przypisem[^1].\n"
    "\n"
    "[^1]: Pierwsza tresc[^2] z odnosnikiem.\n"
    "[^2]: Druga tresc.\n"
)
C_KONTROLA = (
    "# Dokument\n"
    "\n"
    "przyklad[^1] w zdaniu\n"
    "\n"
    "[^1]: Tresc przypisu ze zdania.\n"
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
    if not os.path.exists(sciezka):
        return None, None, ""
    with zipfile.ZipFile(sciezka) as z:
        nazwy = z.namelist()
        doc = z.read("word/document.xml").decode("utf-8", "replace")
        if "word/footnotes.xml" not in nazwy:
            return 0, 0, doc
        fn = z.read("word/footnotes.xml").decode("utf-8", "replace")
    ids = [int(x) for x in re.findall(r'<w:footnote[^>]*w:id="(-?\d+)"', fn)]
    tresciowe = [i for i in ids if i > 0]
    odwolania = re.findall(r"<w:footnoteReference", doc)
    return len(tresciowe), len(odwolania), doc


def przygotuj():
    if os.path.isdir(WORK):
        shutil.rmtree(WORK)
    os.makedirs(WORK)
    for nazwa, tresc in (("a_blok.md", A_BLOK), ("b_nested.md", B_NESTED), ("c_kontrola.md", C_KONTROLA)):
        with open(os.path.join(WORK, nazwa), "w", encoding="utf-8", newline="\r\n") as f:
            f.write(tresc)
    shutil.copy(TOHTM, WORK)
    shutil.copy(PANDOC, WORK)


def htm_dla(nazwa):
    cmd('cd /d {} && 2htm.exe {}'.format(WORK_WIN, nazwa))
    baza = os.path.splitext(nazwa)[0]
    for rozsz in (".htm", ".html"):
        p = os.path.join(WORK, baza + rozsz)
        if os.path.exists(p):
            return open(p, encoding="utf-8", errors="replace").read()
    return ""


def docx_dla(nazwa):
    baza = os.path.splitext(nazwa)[0]
    cmd('cd /d {} && pandoc.exe -f gfm+footnotes -t docx -o {}.docx {}'.format(WORK_WIN, baza, nazwa))
    return docx_przypisy(os.path.join(WORK, baza + ".docx"))


def main():
    for p in (PANDOC, TOHTM):
        if not os.path.exists(p):
            print("BRAK " + p)
            return 3
    przygotuj()

    # ---------- C. KONTROLA POZYTYWNA (najpierw, bo bez niej reszta nic nie znaczy) ----------
    htm_c = htm_dla("c_kontrola.md")
    c_fn, c_ref, c_doc = docx_dla("c_kontrola.md")
    ok('class="footnote-ref"' in htm_c or 'href="#fn:1"' in htm_c,
       "KONTROLA: znacznik w ZWYKLYM zdaniu daje odsylacz w podgladzie", "brak")
    ok(c_fn == 1 and c_ref == 1,
       "KONTROLA: znacznik w ZWYKLYM zdaniu daje PRAWDZIWY przypis Worda",
       "przypisy=" + str(c_fn) + " odwolania=" + str(c_ref))

    # ---------- A. ZNACZNIK W BLOKU KODU ----------
    htm_a = htm_dla("a_blok.md")
    a_fn, a_ref, a_doc = docx_dla("a_blok.md")
    print("      [dane] podglad bloku kodu zawiera odsylacz przypisu: "
          + str('class="footnote-ref"' in htm_a or 'href="#fn:1"' in htm_a))
    print("      [dane] docx z bloku kodu: przypisy=" + str(a_fn) + " odwolania=" + str(a_ref))
    ok(a_ref == 0,
       "BLOK KODU: znacznik NIE staje sie przypisem w Wordzie (znacznik martwy)",
       "odwolania=" + str(a_ref))
    ok("[^1]" in a_doc or "^1" in a_doc,
       "BLOK KODU: znacznik zostaje w dokumencie golym tekstem",
       "nie znaleziono surowego znacznika")

    # ---------- B. PRZYPIS W TRESCI PRZYPISU ----------
    htm_b = htm_dla("b_nested.md")
    b_fn, b_ref, b_doc = docx_dla("b_nested.md")
    with zipfile.ZipFile(os.path.join(WORK, "b_nested.docx")) as z:
        b_fnxml = z.read("word/footnotes.xml").decode("utf-8", "replace")
    print("      [dane] docx z przypisu w przypisie: przypisy=" + str(b_fn) + " odwolania=" + str(b_ref))
    print("      [dane] surowy [^2] zostal w tresci dokumentu: " + str("[^2]" in b_doc))
    ok(b_fn is not None, "PRZYPIS W PRZYPISIE: pandoc w ogole zrobil plik", "brak pliku")
    # NAJWAZNIEJSZE: tresc zagniezdzonego przypisu NIE dociera do Worda ani
    # jako przypis, ani jako tekst - po prostu jej nie ma.  To utrata tresci.
    ok("Druga tresc" not in b_fnxml and "Druga tresc" not in b_doc,
       "PRZYPIS W PRZYPISIE: tresc zagniezdzonego przypisu ZNIKA z docx (utrata tresci)",
       "znaleziono ja w pliku, wiec hipoteza o utracie jest falszywa")
    ok(b_fn == 1,
       "PRZYPIS W PRZYPISIE: w Wordzie zostaje TYLKO jeden przypis z dwoch",
       "przypisy=" + str(b_fn))

    # ---------- D. ZNACZNIK W NAGLOWKU ----------
    # Kursor w naglowku to u niego czesty przypadek, bo po Shift+F6 (spis
    # tresci) tam wlasnie ladujesz.  Sprawdzamy, czy przypis w naglowku zyje
    # i czy nie psuje samego naglowka.
    with open(os.path.join(WORK, "d_naglowek.md"), "w", encoding="utf-8", newline="\r\n") as f:
        f.write("# Tytul dokumentu\n\n## Rozdzial z przypisem[^1]\n\nAkapit.\n\n[^1]: Tresc przypisu z naglowka.\n")
    htm_d = htm_dla("d_naglowek.md")
    d_fn, d_ref, d_doc = docx_dla("d_naglowek.md")
    print("      [dane] docx z naglowka: przypisy=" + str(d_fn) + " odwolania=" + str(d_ref))
    print("      [dane] surowy [^1] zostal w tresci: " + str("[^1]" in d_doc))
    ok(d_fn == 1 and d_ref == 1,
       "NAGLOWEK: przypis w naglowku ZYJE (prawdziwy przypis Worda)",
       "przypisy=" + str(d_fn) + " odwolania=" + str(d_ref))
    ok("Rozdzial z przypisem" in d_doc,
       "NAGLOWEK: tekst naglowka zostal nietkniety", "brak tekstu naglowka")

    print()
    print("PASS: {}   FAIL: {}".format(iPass, iFail))
    return 0 if iFail == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
