#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Czy linki spisu tresci DZIALAJA po eksporcie - pomiar na docx i html.

Zlecenie 1787793592380-0, punkt 4 (Kasperczak): "on chce klikac pozycje spisu
w wyeksportowanym dokumencie i trafiac do rozdzialu". Kotwica zgodna z pandokiem
to warunek konieczny, ale nie dowod. Dowodem jest to, ze w WYNIKU eksportu
KAZDY odsylacz spisu ma po drugiej stronie cel o tej samej nazwie.

Co robi:
 1. bierze prawdziwy dokument (domyslnie liste testow Kasperczaka),
 2. wola testy/pomiar_eksportu_spisu.exe, ktory wpisuje spis metoda z
    ZBUDOWANEGO EdSharpNG.exe (nie z przepisanego kodu),
 3. konwertuje wynik pandokiem z katalogu Convert na docx i na html,
 4. sprawdza, ze kazdy odsylacz ma cel: w docx w:hyperlink w:anchor wobec
    w:bookmarkStart w:name, w html <a href="#..."> wobec id="...".

KONTROLA WAZNOSCI (bez niej zielony wynik nic nie dowodzi): ten sam pomiar
uruchomiony na dokumencie ze SPSEM ZEPSUTYM (kotwice zamienione na "#brak-...")
MUSI zglosic zerwane odsylacze. Sonda, ktora zawsze mowi "OK", nie mierzy nic.
"""
import re
import subprocess
import sys
import zipfile
from pathlib import Path

BAZA = Path('/mnt/d/projekty/edsharp-pr')
PANDOC = BAZA / 'Convert' / 'Pandoc' / 'pandoc.exe'
SONDA = BAZA / 'testy' / 'pomiar_eksportu_spisu.exe'
ROBOCZY = BAZA / 'testy' / 'pomiar_spisu'


def pandoc(zrodlo: Path, cel: Path, fmt: str):
    w = subprocess.run([str(PANDOC), zrodlo.name, '-f', 'gfm', '-t', fmt, '-o', cel.name],
                       cwd=str(zrodlo.parent), capture_output=True)
    if w.returncode != 0:
        raise RuntimeError("pandoc %s: %s" % (fmt, w.stderr.decode('utf-8', 'replace')))


def sprawdz_docx(plik: Path):
    with zipfile.ZipFile(plik) as z:
        xml = z.read('word/document.xml').decode('utf-8')
    kotwice = set(re.findall(r'w:anchor="([^"]*)"', xml))
    zakladki = set(re.findall(r'w:bookmarkStart[^>]*w:name="([^"]*)"', xml))
    return kotwice, zakladki


def sprawdz_html(plik: Path):
    html = plik.read_text(encoding='utf-8')
    kotwice = set(re.findall(r'href="#([^"]*)"', html))
    cele = set(re.findall(r'id="([^"]*)"', html))
    return kotwice, cele


def raport(nazwa, kotwice, cele):
    zerwane = sorted(k for k in kotwice if k not in cele)
    print("  %s: %d odsylaczy, %d celow, zerwanych: %d" % (nazwa, len(kotwice), len(cele), len(zerwane)))
    for z in zerwane[:8]:
        print("     ZERWANY -> #%s" % z)
    return zerwane


def main():
    zrodlo = Path(sys.argv[1]) if len(sys.argv) > 1 else BAZA / 'testy' / 'EdSharpNG_testy_5.0-5.0.21.md'
    if not SONDA.exists():
        print("BRAK sondy, zbuduj: csc.exe /r:EdSharpNG.exe /out:testy\\pomiar_eksportu_spisu.exe testy\\pomiar_eksportu_spisu.cs")
        return 3
    ROBOCZY.mkdir(exist_ok=True)

    ze_spisem = ROBOCZY / 'ze_spisem.md'
    # PULAPKA: sonda to program WINDOWS i nie rozumie sciezek /mnt/... - dostaje
    # wiec sciezki WZGLEDNE wobec katalogu projektu, ktory podajemy jako cwd.
    w = subprocess.run([str(SONDA), str(zrodlo.relative_to(BAZA)), str(ze_spisem.relative_to(BAZA))],
                       cwd=str(BAZA), capture_output=True)
    print(w.stdout.decode('utf-8', 'replace').strip())
    if w.returncode != 0:
        print(w.stderr.decode('utf-8', 'replace'))
        return 3

    bledy = 0

    print("POMIAR WLASCIWY (spis wpisany przez EdSharpNG):")
    docx = ROBOCZY / 'ze_spisem.docx'
    html = ROBOCZY / 'ze_spisem.html'
    pandoc(ze_spisem, docx, 'docx')
    pandoc(ze_spisem, html, 'html')
    if raport("docx (zakladki Worda)", *sprawdz_docx(docx)):
        bledy += 1
    if raport("html (kotwice)", *sprawdz_html(html)):
        bledy += 1

    print("KONTROLA WAZNOSCI (spis celowo zepsuty - MUSI zglosic zerwane):")
    zepsuty = ROBOCZY / 'zepsuty.md'
    tresc = ze_spisem.read_text(encoding='utf-8')
    tresc = re.sub(r'\]\(#', '](#brak-', tresc, count=0)
    zepsuty.write_text(tresc, encoding='utf-8')
    zdocx = ROBOCZY / 'zepsuty.docx'
    zhtml = ROBOCZY / 'zepsuty.html'
    pandoc(zepsuty, zdocx, 'docx')
    pandoc(zepsuty, zhtml, 'html')
    zerwane_d = raport("docx zepsuty", *sprawdz_docx(zdocx))
    zerwane_h = raport("html zepsuty", *sprawdz_html(zhtml))
    if not zerwane_d or not zerwane_h:
        print("  BLAD KONTROLI: zepsuty spis przeszedl, wiec pomiar jest gluchy")
        bledy += 1
    else:
        print("  kontrola OK: zepsuty spis zglosil zerwane odsylacze")

    print()
    print("WYNIK: " + ("WSZYSTKIE ODSYLACZE SPISU MAJA CEL" if bledy == 0 else "%d PROBLEMOW" % bledy))
    return 0 if bledy == 0 else 1


if __name__ == '__main__':
    sys.exit(main())
