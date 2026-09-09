#!/usr/bin/env python3
# Mapa chord -> nazwa komendy z EdSharp.cs, dla DOWOLNEJ rewizji git.
#
# PO CO OSOBNE NARZEDZIE: sam ZBIOR chordow jest slepy na przeprowadzke
# (zmierzone 31.08.2026 przy rodzinie klawisza L: Control+Shift+L byl w obu
# zbiorach, tylko zmienil wlasciciela).  Porownywac trzeba SLOWNIK.
#
# Uzycie:
#   python3 testy/mapa_chordow.py <rewizja|HEAD|WORKTREE>
#   python3 testy/mapa_chordow.py <rewizja_a> <rewizja_b>   # rozne wpisy
#
# Wzorzec zawezony do KODU AKTYWNEGO (linia zaczyna sie od menuXxx =), zeby
# zakomentowane linie nie dawaly falszywych duplikatow - druga pulapka tej
# samej sondy, zmierzona 31.08.2026.

import re
import subprocess
import sys
import os

KATALOG = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WZORZEC = re.compile(r'^\s*(menu[A-Za-z]+)\s*=\s*CreateMenuItem\("([^"]*)",\s*"([^"]*)"', re.M)


def zrodlo(rewizja):
    if rewizja.upper() in ("WORKTREE", "HEAD-WORKTREE"):
        with open(os.path.join(KATALOG, "EdSharp.cs"), encoding="utf-8", errors="replace") as fh:
            return fh.read()
    out = subprocess.run(["git", "show", "%s:EdSharp.cs" % rewizja],
                         cwd=KATALOG, capture_output=True)
    if out.returncode != 0:
        sys.exit("BLAD: nie moge pobrac EdSharp.cs z rewizji %s" % rewizja)
    return out.stdout.decode("utf-8", "replace")


def mapa(tekst):
    """chord -> nazwa komendy. Komendy BEZ chordu (menu-only) osobno."""
    wynik = {}
    bez = set()
    duple = []
    for m in WZORZEC.finditer(tekst):
        nazwa = m.group(2).replace("&", "").replace("...", "").strip()
        chord = m.group(3).replace("&", "").strip()
        if chord == "":
            bez.add(nazwa)
            continue
        if chord in wynik:
            duple.append((chord, wynik[chord], nazwa))
        wynik[chord] = nazwa
    return wynik, bez, duple


def main():
    if len(sys.argv) == 2:
        m, bez, duple = mapa(zrodlo(sys.argv[1]))
        for chord in sorted(m):
            print("%s\t%s" % (chord, m[chord]))
        print("# komend z chordem: %d, menu-only: %d" % (len(m), len(bez)))
        for d in duple:
            print("# DUPLIKAT chordu %s: %s oraz %s" % d)
        return 0

    if len(sys.argv) != 3:
        sys.exit(__doc__ or "Uzycie: mapa_chordow.py <rew_a> [rew_b]")

    a, ba, dupa = mapa(zrodlo(sys.argv[1]))
    b, bb, dupb = mapa(zrodlo(sys.argv[2]))

    print("== %s -> %s ==" % (sys.argv[1], sys.argv[2]))
    for chord in sorted(set(a) | set(b)):
        wa = a.get(chord)
        wb = b.get(chord)
        if wa == wb:
            continue
        if wa is None:
            print("DOSZEDL   %s -> %s" % (chord, wb))
        elif wb is None:
            print("ZWOLNIONY %s (byl: %s)" % (chord, wa))
        else:
            print("WLASCICIEL %s: %s -> %s" % (chord, wa, wb))

    # Komenda, ktora STRACILA albo DOSTALA chord - druga strona tej samej
    # relacji.  Bez tego przeprowadzka komendy do menu przechodzi niezauwazona,
    # gdy jej chord przejmuje kto inny.
    inv_a = {}
    for c, n in a.items():
        inv_a.setdefault(n, set()).add(c)
    inv_b = {}
    for c, n in b.items():
        inv_b.setdefault(n, set()).add(c)
    for nazwa in sorted(set(inv_a) | set(inv_b) | ba | bb):
        ca = inv_a.get(nazwa, set())
        cb = inv_b.get(nazwa, set())
        if ca != cb:
            print("KOMENDA %s: %s -> %s" % (nazwa,
                                            ",".join(sorted(ca)) or "(brak)",
                                            ",".join(sorted(cb)) or "(brak)"))
    for d in dupb:
        print("DUPLIKAT w %s: chord %s ma %s oraz %s" % (sys.argv[2], d[0], d[1], d[2]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
