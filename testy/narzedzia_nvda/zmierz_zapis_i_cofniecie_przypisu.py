#!/usr/bin/env python3
"""Kontrola po wstawieniu przypisu: ZERO zapisu na dysk i pelne cofniecie.

Powod, dla ktorego to jest osobny pomiar: Format Code zamienil plik .md
Kasperczaka na HTML bezpowrotnie, bo pisal po pliku NA DYSKU i czyscil historie
cofania. Kazda nasza komenda przepisujaca dokument musi udowodnic dwie rzeczy
osobno, a poprzedni skrypt mierzyl tylko wiersz pod kursorem:

  1. plik na dysku jest NIETKNIETY dopoki user nie zapisze (md5 przed i po),
  2. cofniecie przywraca CALA tresc w oknie, nie tylko usuwa znacznik.

KONTROLA WAZNOSCI: po Control+S md5 MUSI sie zmienic. Bez tej kontroli
"md5 identyczne" moglo znaczyc, ze mierzymy zly plik albo ze program wcale
nie doszedl do wstawiania.
"""
import hashlib
import io
import os
import shutil
import sys
import time

sys.path.insert(0, "/mnt/d/projekty/edsharp-pr/testy/narzedzia_nvda")
from zmierz_mowe_spisu_tresci import (akcja, aktywuj, klaw, linia_biezaca,
                                      mowa_od, norm, uruchom_edsharp,
                                      w_edytorze, zamknij_edsharp, znacznik)

ROBOCZY = "/mnt/c/tmp/fn_zapis"
ROBOCZY_WIN = r"C:\tmp\fn_zapis"
PLIK = "cofanie.md"
DOC = ("# Dokument probny\r\n"
       "\r\n"
       "Pierwsze zdanie dokumentu.\r\n"
       "\r\n"
       "Drugie zdanie dokumentu.\r\n")

wyniki = []


def zapisz(opis, ok, detal):
    wyniki.append((opis, ok, detal))
    print("  -> [{}] {}".format("OK   " if ok else "UWAGA", detal))


def md5():
    with open(os.path.join(ROBOCZY, PLIK), "rb") as f:
        return hashlib.md5(f.read()).hexdigest()


def tresc_okna():
    """Cala tresc dokumentu W OKNIE: Ctrl+Home, potem czytamy wiersz po wierszu.

    Czytamy z okna, nie z dysku - o to wlasnie chodzi, bo na dysku ma NIC nie
    byc zmienione.
    """
    klaw("control+home", 0.7)
    linie = []
    ostatnia = None
    for _ in range(30):
        w = linia_biezaca()
        linie.append(w.rstrip("\r\n"))
        klaw("downArrow", 0.35)
        nowa = linia_biezaca()
        if nowa == w and ostatnia == w:
            break
        ostatnia = w
    return "\n".join(linie)


def main():
    if os.path.isdir(ROBOCZY):
        shutil.rmtree(ROBOCZY)
    os.makedirs(ROBOCZY)
    with io.open(os.path.join(ROBOCZY, PLIK), "w", encoding="utf-8", newline="") as f:
        f.write(DOC)
    md5_start = md5()
    print("md5 przed uruchomieniem:", md5_start)

    zamknij_edsharp()
    uruchom_edsharp(ROBOCZY_WIN + "\\" + PLIK)
    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa")
        return 2
    if not w_edytorze():
        print("BLAD: fokus nie w polu edycji")
        return 2

    tresc_przed = tresc_okna()
    print("tresc w oknie przed:", repr(tresc_przed[:120]))

    # --- wstawiamy przypis ---
    print("\n=== wstawiam przypis (Control+F6) ===")
    klaw("control+home", 0.7)
    klaw("downArrow", 0.4)
    klaw("downArrow", 0.5)
    klaw("end", 0.6)
    z = znacznik()
    klaw("control+f6", 1.6)
    for ch in "abc":
        akcja("send_keys", keys=ch)
        time.sleep(0.3)
    klaw("enter", 2.0)
    m = mowa_od(z, 1.2)
    for x in m:
        print("   MOWA:", x[:120])
    zapisz("przypis wstawiony (slychac potwierdzenie)",
           "inserted" in norm(" ".join(m)),
           "komunikat: {}".format([x for x in m if "nserted" in x][:1] or "BRAK"))

    md5_po_wstawieniu = md5()
    print("md5 po wstawieniu:      ", md5_po_wstawieniu)
    zapisz("PLIK NA DYSKU NIETKNIETY po wstawieniu przypisu",
           md5_po_wstawieniu == md5_start,
           "md5 identyczne: {}".format(md5_po_wstawieniu == md5_start))

    # --- cofamy ---
    print("\n=== cofam (Control+Z) ===")
    for i in range(4):
        klaw("control+z", 0.9)
    tresc_po = tresc_okna()
    print("tresc w oknie po cofnieciu:", repr(tresc_po[:120]))
    zapisz("cofniecie przywrocilo CALA tresc dokumentu w oknie",
           norm(tresc_po).strip() == norm(tresc_przed).strip(),
           "tresc identyczna" if norm(tresc_po).strip() == norm(tresc_przed).strip()
           else "ROZNI SIE:\n  przed: {!r}\n  po:    {!r}".format(tresc_przed, tresc_po))
    zapisz("po cofnieciu nie ma ani znacznika, ani tresci przypisu",
           "[^1]" not in tresc_po,
           "brak sladu przypisu w oknie")

    # --- KONTROLA WAZNOSCI: zapis MUSI zmienic md5 ---
    print("\n=== kontrola waznosci: Control+S ma zmienic plik ===")
    klaw("control+end", 0.6)
    akcja("send_keys", keys="x")
    time.sleep(0.5)
    klaw("control+s", 2.0)
    md5_po_zapisie = md5()
    print("md5 po zapisie:         ", md5_po_zapisie)
    zapisz("KONTROLA WAZNOSCI: po Control+S md5 SIE ZMIENIA",
           md5_po_zapisie != md5_start,
           "md5 rozne: {} (bez tego 'nietkniete' wyzej nic nie dowodzi)".format(
               md5_po_zapisie != md5_start))

    zamknij_edsharp()

    print("\n" + "=" * 60)
    ok = sum(1 for _, w, _ in wyniki if w)
    print("WYNIK: {}/{} OK".format(ok, len(wyniki)))
    for opis, w, detal in wyniki:
        if not w:
            print("  UWAGA: {} -> {}".format(opis, detal))
    return 0 if ok == len(wyniki) else 1


if __name__ == "__main__":
    sys.exit(main())
