#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Wyprodukuj liste kotwic naglowkow ZMIERZONA PANDOKIEM z katalogu Convert.

Po co: nasz spis tresci wpisuje linki wewnetrzne [tytul](#kotwica). Jesli nasza
kotwica rozni sie od tej, ktora liczy pandoc, link po eksporcie do Worda albo
HTML prowadzi w nicosc - a Kasperczak chce klikac pozycje spisu w
WYEKSPORTOWANYM dokumencie. Dlatego prawdy o kotwicach nie zgadujemy: bierzemy
ja od tego samego pandoca, ktory siedzi w naszej paczce.

Wynik: testy/kotwice_pandoc.txt, wiersze "tytul<TAB>kotwica", czytane przez
testy/pomiar_spisu_tresci.cs.
"""
import re
import subprocess
import sys
from pathlib import Path

BAZA = Path('/mnt/d/projekty/edsharp-pr')
PANDOC = BAZA / 'Convert' / 'Pandoc' / 'pandoc.exe'

TYTULY = [
    "Instalacja",
    "Pierwsze kroki",
    "Zażółć gęślą jaźń",
    "A, B: C!",
    "Rozdział 1 (wstęp)",
    "100 lat",
    "-- kreski --",
    "Wersja 5.0.25",
    "proste_z_podkresleniem",
    "Ala & Ola",
    "Koniec",
    "Tytuł z myślnikiem - i dalej",
    "Slash / backslash pipe",
    "Kropka. Przecinek, Srednik;",
    "MiXeD CaSe",
    "1. Punkt",
    "Ą",
    "Test kod i pogrubienie",
]


def main():
    if not PANDOC.exists():
        print("BRAK pandoca:", PANDOC)
        return 3

    # kazdy tytul jako osobny naglowek h2; numeracja w komentarzu, zeby dopasowac
    md = "\n\n".join("## %s" % t for t in TYTULY) + "\n"
    zrodlo = BAZA / 'testy' / '_kotwice_zrodlo.md'
    zrodlo.write_text(md, encoding='utf-8')

    wynik = subprocess.run([str(PANDOC), zrodlo.name, '-f', 'gfm', '-t', 'html'],
                           cwd=str(BAZA / 'testy'), capture_output=True)
    if wynik.returncode != 0:
        print("pandoc zwrocil", wynik.returncode, wynik.stderr.decode('utf-8', 'replace'))
        return 3
    html = wynik.stdout.decode('utf-8')

    idy = re.findall(r'<h2 id="([^"]*)"', html)
    if len(idy) != len(TYTULY):
        print("NIEZGODNOSC: %d tytulow, %d kotwic z pandoca" % (len(TYTULY), len(idy)))
        print(html)
        return 3

    linie = ["# Kotwice naglowkow ZMIERZONE pandokiem %s" % (
        subprocess.run([str(PANDOC), '--version'], capture_output=True).stdout
        .decode('utf-8', 'replace').splitlines()[0])]
    linie.append("# tytul<TAB>kotwica")
    for t, a in zip(TYTULY, idy):
        linie.append("%s\t%s" % (t, a))

    (BAZA / 'testy' / 'kotwice_pandoc.txt').write_text("\n".join(linie) + "\n", encoding='utf-8')
    zrodlo.unlink()
    print("Zapisane: testy/kotwice_pandoc.txt, %d kotwic" % len(idy))
    for t, a in zip(TYTULY, idy):
        print("  %-32s -> %s" % (t, a))
    return 0


if __name__ == '__main__':
    sys.exit(main())
