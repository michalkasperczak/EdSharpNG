#!/usr/bin/env python3
"""Sprawdza PDF zlozony przez PdfExport: tresc zrodlowa i strukture otagowania.

Argument: sciezka do PDF (ta z wiersza "PDF-DO-SPRAWDZENIA" harnessu).
Wymaga pypdf.  Kod wyjscia 0 gdy wszystko sie zgadza.
"""
import sys
from pypdf import PdfReader

OCZEKIWANY_TEKST = [
    "Zazolc gesla jazn",              # naglowek H1
    "odnosnikiem testowym",           # etykieta linku
    "ąćęłńóśźż",                      # polskie znaki z H2
    "Pierwszy punkt listy",
    "Drugi punkt listy",
]
# Typy struktur, ktore musi miec PDF otagowany z naszego HTML.
OCZEKIWANE_TAGI = {"H1", "H2", "P", "L", "LI", "Link"}


def zbierz_typy(obiekt, zbior, glebokosc=0):
    if glebokosc > 60:
        return
    try:
        obiekt = obiekt.get_object()
    except Exception:
        return
    if isinstance(obiekt, list):
        for e in obiekt:
            zbierz_typy(e, zbior, glebokosc + 1)
        return
    if not hasattr(obiekt, "get"):
        return
    t = obiekt.get("/S")
    if t is not None:
        zbior.add(str(t).lstrip("/"))
    dzieci = obiekt.get("/K")
    if dzieci is not None:
        zbierz_typy(dzieci, zbior, glebokosc + 1)


def main():
    if len(sys.argv) < 2:
        print("uzycie: sprawdz_pdf.py <plik.pdf>")
        return 2
    sciezka = sys.argv[1]
    bledy = []

    with open(sciezka, "rb") as f:
        naglowek = f.read(5)
    if naglowek != b"%PDF-":
        bledy.append("plik nie zaczyna sie od %PDF-")

    reader = PdfReader(sciezka)
    print("stron: %d" % len(reader.pages))

    tekst = "\n".join(p.extract_text() or "" for p in reader.pages)
    for frag in OCZEKIWANY_TEKST:
        if frag in tekst:
            print("OK   tekst zrodlowy obecny: %r" % frag)
        else:
            bledy.append("brak tekstu %r" % frag)
            print("BLAD brak tekstu: %r" % frag)

    # OTAGOWANIE: /MarkInfo /Marked true plus drzewo struktur z naszymi typami.
    katalog = reader.trailer["/Root"]
    mark = katalog.get("/MarkInfo")
    marked = bool(mark.get("/Marked")) if mark else False
    print("OK   /MarkInfo /Marked = %s" % marked if marked else "BLAD PDF nie jest oznaczony jako tagged")
    if not marked:
        bledy.append("/MarkInfo /Marked nie jest true")

    root = katalog.get("/StructTreeRoot")
    if root is None:
        bledy.append("brak /StructTreeRoot")
        print("BLAD brak drzewa struktur /StructTreeRoot")
        typy = set()
    else:
        print("OK   /StructTreeRoot obecny")
        typy = set()
        zbierz_typy(root.get_object().get("/K"), typy)
        print("typy struktur: %s" % ", ".join(sorted(typy)))

    for tag in sorted(OCZEKIWANE_TAGI):
        if tag in typy:
            print("OK   struktura %s obecna" % tag)
        else:
            bledy.append("brak struktury %s" % tag)
            print("BLAD brak struktury %s" % tag)

    # Link musi zachowac adres, inaczej czytnik przeczyta sam tekst.
    adresy = []
    for p in reader.pages:
        for a in p.get("/Annots") or []:
            a = a.get_object()
            akcja = a.get("/A")
            if akcja and akcja.get_object().get("/URI"):
                adresy.append(str(akcja.get_object()["/URI"]))
    print("adresy linkow: %s" % adresy)
    if any("example.org/sciezka" in u for u in adresy):
        print("OK   odnosnik zachowal adres")
    else:
        bledy.append("link nie zachowal adresu")
        print("BLAD link nie zachowal adresu")

    print("---")
    if bledy:
        print("BLEDOW: %d" % len(bledy))
        return 1
    print("wszystko sie zgadza")
    return 0


if __name__ == "__main__":
    sys.exit(main())
