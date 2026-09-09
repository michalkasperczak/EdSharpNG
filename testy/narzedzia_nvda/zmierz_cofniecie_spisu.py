#!/usr/bin/env python3
"""Czysty pomiar punktu 15.3: JEDNO Alt+Shift+T, JEDNO Control+Z.

CZEMU OSOBNO: w pelnym biegu zmierz_mowe_spisu_tresci.py punkt 15.3 lezy PO
tescie 15.2, czyli po DWOCH nacisnieciach Alt+Shift+T - i faktycznie
trzeba tam bylo dwoch cofniec. To poprawne (dwie komendy = dwa cofniecia),
ale NIE jest tym, co mowi lista testow: ona mowi o Control+Z zaraz po
JEDNYM Alt+Shift+T. Ten skrypt mierzy dokladnie ten przypadek, zeby
zaliczenie 15.3 nie opieralo sie na innym scenariuszu niz zapisany.
"""
import sys
sys.path.insert(0, "/mnt/d/projekty/edsharp-pr/testy/narzedzia_nvda")
from zmierz_mowe_spisu_tresci import (aktywuj, klaw, linia_biezaca, norm,
                                      uruchom_edsharp, w_edytorze,
                                      zamknij_edsharp)

zamknij_edsharp()
uruchom_edsharp(r"D:\projekty\edsharp-pr\testy\spis_probka.md")
if not aktywuj():
    print("BLAD: nie aktywowalem EdSharpa")
    sys.exit(2)
if not w_edytorze():
    print("BLAD: fokus nie w polu edycji")
    sys.exit(2)

klaw("control+home", 0.8)
przed = linia_biezaca()
print(f"pierwszy wiersz na starcie:        {przed!r}")

klaw("alt+shift+t", 1.5)
klaw("control+home", 0.8)
po_spisie = linia_biezaca()
print(f"pierwszy wiersz po Alt+Shift+T:    {po_spisie!r}")

klaw("control+z", 1.4)
klaw("control+home", 0.8)
po_cofnieciu = linia_biezaca()
print(f"pierwszy wiersz po JEDNYM Ctrl+Z:  {po_cofnieciu!r}")

wyniki = []
wyniki.append(("15.3 spis powstal (warunek wstepny pomiaru)",
               "contents" in norm(po_spisie)))
wyniki.append(("15.3 JEDNO Control+Z przywraca oryginalny pierwszy wiersz",
               norm(po_cofnieciu).strip() == norm(przed).strip()))
wyniki.append(("15.3 kontrola waznosci: stan po spisie ROZNI sie od oryginalu",
               norm(po_spisie).strip() != norm(przed).strip()))

print()
for opis, ok in wyniki:
    print(f"  [{'OK   ' if ok else 'UWAGA'}] {opis}")
zle = [w for w in wyniki if not w[1]]
print(f"\nzaliczone: {len(wyniki) - len(zle)} z {len(wyniki)}")
zamknij_edsharp()
sys.exit(0 if not zle else 1)
