#!/usr/bin/env python3
"""Punkt 15.4: Alt+Shift+T na pliku, ktory NIE jest Markdownem.

Oczekiwane wg listy testow: program NIC nie zmienia i mowi
"Table of Contents works only on Markdown files!".

Mierzymy DWIE rzeczy, bo sam komunikat to za malo: czy tresc dokumentu
pozostala nietknieta (pierwszy wiersz bez zmian) ORAZ czy komunikat padl.
Kontrola waznosci: ten sam klawisz na .md w tym samym biegu MUSI dac
inny wynik - inaczej pomiar nie rozroznia bramki od bezczynnosci programu.
"""
import sys
sys.path.insert(0, "/mnt/d/projekty/edsharp-pr/testy/narzedzia_nvda")
from zmierz_mowe_spisu_tresci import (aktywuj, klaw, linia_biezaca, mowa_od,
                                      norm, uruchom_edsharp, w_edytorze,
                                      zamknij_edsharp, znacznik)

wyniki = []


def zapisz(opis, ok, detal):
    wyniki.append((opis, ok, detal))
    print(f"  -> [{'OK   ' if ok else 'UWAGA'}] {detal}")


print("=== TEST 15.4: Alt+Shift+T na pliku .txt ===")
zamknij_edsharp()
uruchom_edsharp(r"D:\projekty\edsharp-pr\testy\spis_probka_niemarkdown.txt")
if not aktywuj():
    print("BLAD: nie aktywowalem EdSharpa")
    sys.exit(2)
if not w_edytorze():
    print("BLAD: fokus nie w polu edycji")
    sys.exit(2)

klaw("control+home", 0.8)
przed = linia_biezaca()
print(f"   pierwszy wiersz przed: {przed!r}")
z = znacznik()
klaw("alt+shift+t", 1.5)
m = mowa_od(z)
for x in m:
    print("   MOWA:", x[:160])
klaw("control+home", 0.8)
po = linia_biezaca()
print(f"   pierwszy wiersz po:    {po!r}")

caly = norm(" ".join(m))
zapisz("15.4 slychac odmowe 'works only on Markdown files'",
       "works only on markdown files" in caly,
       f"komunikat obecny: {'works only on markdown files' in caly}")
zapisz("15.4 dokument NIETKNIETY (pierwszy wiersz bez zmian)",
       norm(po).strip() == norm(przed).strip(),
       "tresc taka sama przed i po"
       if norm(po).strip() == norm(przed).strip() else
       f"TRESC ZMIENIONA: {przed!r} -> {po!r}")
zapisz("15.4 kontrola waznosci: NIE powstal naglowek Contents",
       "contents" not in norm(po),
       "brak wpisanego spisu w pliku nie-Markdown")

# --- KONTROLA DYSKRYMINACYJNA: ten sam klawisz na .md musi dzialac ---
print("\n=== kontrola dyskryminacyjna: ten sam klawisz na .md ===")
zamknij_edsharp()
uruchom_edsharp(r"D:\projekty\edsharp-pr\testy\spis_probka.md")
if not aktywuj():
    print("BLAD: nie aktywowalem EdSharpa (md)")
else:
    klaw("control+home", 0.8)
    z = znacznik()
    klaw("alt+shift+t", 1.5)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly_md = norm(" ".join(m))
    zapisz("kontrola: na .md TEN SAM klawisz tworzy spis (nie odmawia)",
           "works only on markdown files" not in caly_md and "contents" in caly_md,
           "bramka rozroznia typ pliku - odmowa dotyczy tylko nie-Markdown"
           if "works only on markdown files" not in caly_md
           else "odmowa takze na .md - bramka jest ZA SZEROKA")
    klaw("control+z", 1.2)   # sprzataj: nie zostawiaj spisu w pliku probnym

print()
for opis, ok, detal in wyniki:
    print(f"  [{'OK   ' if ok else 'UWAGA'}] {opis}")
    print(f"          {detal}")
zle = [w for w in wyniki if not w[1]]
print(f"\nzaliczone: {len(wyniki) - len(zle)} z {len(wyniki)}")
zamknij_edsharp()
sys.exit(0 if not zle else 1)
