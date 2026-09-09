#!/usr/bin/env python3
"""Pomiar na ZYWYM programie: czy Control+Alt+litera WPISUJE polska litere.

PYTANIE KASPERCZAKA (30.08.2026 11:45, zlecenie 1788083132549-4): "Mam
nadzieje, ze tu sie nie dubluje, bo Alt+Ctrl to tez to samo co prawy Alt, a
prawy Alt to polska literka, ale mozna to jakos tak zrobic, zeby dzialalo
prawidlowo."

DLACZEGO SAM POMIAR UKLADU NIE WYSTARCZA: sonda kolizje_altgr.exe pyta
WINDOWS, ktorym chordem powstaje polska litera (a z ogonkiem to Control+Alt+A
i tak dalej).  To dowodzi, ze KLAWIATURA tak dziala, ale NIE dowodzi, co robi
NASZ program: WinForms moze przechwycic chord w ProcessCmdKey PRZED tym, jak
znak trafi do pola edycji.  Dopiero wpisanie litery do dokumentu i odczytanie
jej z powrotem rozstrzyga, czy pisanie po polsku dziala.

CO MIERZY:
  1. Control+Alt+A, C, E, L, N, O, S, X, Z wpisane po kolei do pustego
     dokumentu daja DOKLADNIE dziewiec polskich liter w tresci.
  2. KONTROLA NEGATYWNA W TYM SAMYM PRZEBIEGU: Control+Alt+K (nasz skok
     przypisu) NIE MOZE wpisac zadnego znaku - gdyby wpisywal, znaczylo by, ze
     mierze samo pisanie i punkt 1 nic nie dowodzi.
  3. KONTROLA POZYTYWNA DZIALANIA KOMENDY: Control+Alt+K w dokumencie z
     przypisem nadal skacze (czyli nasz skrot dziala, a nie zostal zjedzony
     przez klawiature).

PULAPKI ODZIEDZICZONE: nazwy gestow to "downArrow"/"upArrow"; pauza po kazdym
klawiszu obowiazkowa; BRAK MOWY to UWAGA, nie OK.
"""
import io
import os
import shutil
import sys
import time

sys.path.insert(0, "/mnt/d/projekty/edsharp-pr/testy/narzedzia_nvda")
from zmierz_mowe_spisu_tresci import (aktywuj, fokus, klaw, linia_biezaca,
                                      mowa_od, norm, uruchom_edsharp,
                                      w_edytorze, zamknij_edsharp, znacznik)

ROBOCZY = "/mnt/c/tmp/altgr_zywy"
ROBOCZY_WIN = r"C:\tmp\altgr_zywy"

# Litera -> klawisz, ktorym powstaje (zmierzone przez VkKeyScanEx na ukladzie
# 00000415, nie zalozone: z z kreska jest na X, nie na Z).
PARY = [
    ("a z ogonkiem", "a", "\u0105"),
    ("c z kreska", "c", "\u0107"),
    ("e z ogonkiem", "e", "\u0119"),
    ("l z kreska", "l", "\u0142"),
    ("n z kreska", "n", "\u0144"),
    ("o z kreska", "o", "\u00f3"),
    ("s z kreska", "s", "\u015b"),
    ("z z kreska", "x", "\u017a"),
    ("z z kropka", "z", "\u017c"),
]

wyniki = []


def zapisz(opis, ok, detal):
    wyniki.append((opis, ok, detal))
    print("  -> [{}] {}".format("OK   " if ok else "UWAGA", detal))


def przygotuj():
    if os.path.isdir(ROBOCZY):
        shutil.rmtree(ROBOCZY)
    os.makedirs(ROBOCZY)
    with io.open(os.path.join(ROBOCZY, "pusty.md"), "w",
                 encoding="utf-8", newline="") as f:
        f.write("# Proba pisania\r\n\r\n")
    with io.open(os.path.join(ROBOCZY, "przypis.md"), "w",
                 encoding="utf-8", newline="") as f:
        f.write("# Dokument z przypisem\r\n\r\n"
                "Zdanie z przypisem[^1] w tresci.\r\n\r\n"
                "[^1]: Tresc przypisu.\r\n")


def start(nazwa):
    zamknij_edsharp()
    uruchom_edsharp(ROBOCZY_WIN + "\\" + nazwa)
    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa ({})".format(nazwa))
        return False
    return True


def zapisz_i_czytaj(nazwa):
    """Control+S zapisuje, a tresc czytamy Z DYSKU - nie z mowy czytnika.
    Mowa moglaby powiedziec 'a z ogonkiem' nawet gdyby program wstawil co
    innego; plik na dysku jest rozstrzygajacy."""
    klaw("control+s", 2.0)
    sciezka = os.path.join(ROBOCZY, nazwa)
    for _ in range(6):
        try:
            with io.open(sciezka, "r", encoding="utf-8") as f:
                return f.read()
        except Exception:
            time.sleep(0.5)
    return ""


def main():
    przygotuj()

    # ===== 1. POLSKIE LITERY PRZEZ CONTROL+ALT =====
    print("=== TEST 1: Control+Alt+litera wpisuje polska litere ===")
    if not start("pusty.md"):
        return 2
    if not w_edytorze():
        print("BLAD: fokus nie w polu edycji")
        return 2
    klaw("control+end", 0.8)
    for nazwa, klawisz, _znak in PARY:
        klaw("control+alt+" + klawisz, 0.7)
    tresc = zapisz_i_czytaj("pusty.md")
    print("   tresc pliku po probie: {!r}".format(tresc[-40:]))
    for nazwa, klawisz, znak in PARY:
        zapisz("1. {} (Control+Alt+{}) trafila do dokumentu".format(nazwa, klawisz.upper()),
               znak in tresc,
               "{}: {}".format(nazwa, "JEST" if znak in tresc else "BRAK"))

    # ===== 2. KONTROLA NEGATYWNA: nasz skrot NIE pisze =====
    print("\n=== TEST 2 (kontrola negatywna): Control+Alt+K nie wpisuje znaku ===")
    dlugosc_przed = len(tresc)
    klaw("control+end", 0.6)
    klaw("control+alt+k", 1.2)
    tresc2 = zapisz_i_czytaj("pusty.md")
    zapisz("2. Control+Alt+K NIE dopisal znaku do dokumentu",
           len(tresc2) <= dlugosc_przed + 2,
           "dlugosc przed {}, po {}".format(dlugosc_przed, len(tresc2)))

    # ===== 3. KONTROLA POZYTYWNA: skrot nadal dziala =====
    print("\n=== TEST 3 (kontrola pozytywna): Control+Alt+K nadal skacze ===")
    if not start("przypis.md"):
        return 2
    klaw("control+home", 0.8)
    klaw("downArrow", 0.5)
    klaw("downArrow", 0.6)
    w_start = linia_biezaca()
    zapisz("3. kursor stoi w zdaniu ze znacznikiem",
           "[^1]" in w_start, "wiersz: {!r}".format(w_start))
    z = znacznik()
    klaw("control+alt+k", 1.6)
    m = mowa_od(z, 1.4)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    w_cel = linia_biezaca()
    zapisz("3. Control+Alt+K PRZESKOCZYL na tresc przypisu",
           "tresc przypisu" in norm(w_cel),
           "wiersz po skoku: {!r}".format(w_cel))
    zapisz("3. program POWIEDZIAL tresc przypisu",
           "przypisu" in caly, "mowa: {!r}".format(caly[:120]))

    zamknij_edsharp()

    print("\n=== PODSUMOWANIE ===")
    ile_ok = sum(1 for _, ok, _ in wyniki if ok)
    for opis, ok, detal in wyniki:
        print("[{}] {}".format("OK   " if ok else "UWAGA", opis))
    print("\nWYNIK: {}/{}".format(ile_ok, len(wyniki)))
    return 0 if ile_ok == len(wyniki) else 1


if __name__ == "__main__":
    sys.exit(main())
