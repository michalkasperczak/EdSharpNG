#!/usr/bin/env python3
"""Pomiar MOWY NVDA dla skoku przypisu przeniesionego na Control+Alt+K (5.0.47).

DLACZEGO TEN POMIAR JEST ROZSTRZYGAJACY, a nie ozdobny:
Kasperczak poprosil (30.08.2026 10:01) o skok znacznik-tresc-znacznik na
Control+Alt+K.  Util.Say ma twardy warunek (EdSharp.cs l.17711), ktory TLUMI
KAZDA MOWE, gdy uzytkownik trzyma Control+Alt.  Czyli komenda przeniesiona tam
bez dalszej zmiany WYKONALA BY skok, a niewidomy NIE USLYSZALBY nic.  Dla niego
"kursor gdzies poszedl w ciszy" jest nie do odroznienia od "klawisz nie dziala".
Harness /mnt/c/tmp/ctrlaltk/h.cs zmierzyl sam warunek (6/6); TUTAJ mierzymy
skutek koncowy: czy CZYTNIK REALNIE MOWI po nacisnieciu skrotu.

CO MIERZY:
  1. start programu po podmianie chordu NIE daje alarmu "Cannot assign"
     (kolizji nie widac przez refleksje - KeyMap wypelnia sie przy budowie menu),
  2. Control+Alt+K ze zdania -> kursor na TRESCI przypisu ORAZ mowa z trescia,
  3. Control+Alt+K z tresci -> powrot do zdania ORAZ mowa "in text",
  4. Control+Alt+K w dokumencie BEZ przypisow -> slyszalne "No footnotes!"
     (to najczulszy punkt: odmowy tez szly przez tlumiona sciezke),
  5. Control+Alt+K w pliku .txt -> slyszalna odmowa o Markdownie.

KONTROLA NEGATYWNA W TYM SAMYM PRZEBIEGU: stary chord Alt+F6 NIE MOZE juz nic
robic.  Bez niej "Control+Alt+K dziala" nie odroznia sie od "oba dzialaja", a
Alt+F6 ma byc wolne pod liste linkow.
KONTROLA POZYTYWNA: Alt+K (lista przypisow) MUSI nadal mowic - ona chodzi bez
Control, wiec dowodzi, ze mierzymy bramke Control+Alt, a nie ogolna cisze.

CZEGO TA SONDA NIE UDOWADNIA - ZMIERZONE, WAZNE PRZY CZYTANIU WYNIKU:
Przebieg na binarce SPRZED naprawy mowy (chord juz przeniesiony, tryb globalny
jeszcze nie) dal 12/13: zamilkl WYLACZNIE punkt 5, czyli "No footnotes!".
Punkty 1 i 2 mowily normalnie.  Przyczyna nie jest przypadkowa: komunikat
odmowy leci NATYCHMIAST w handlerze, gdy modyfikatory sa jeszcze wcisniete, a
wypowiedzi po udanym skoku ida PO przesunieciu kursora (rtb.Index), czyli o
kilka milisekund pozniej - a syntetyczny klawisz z mostka NVDA zwalnia
Control+Alt niemal natychmiast.  CZLOWIEK TRZYMA KLAWISZE DLUZEJ NIZ SONDA,
wiec u niego zagrozone sa TAKZE punkty 1 i 2.  Dlatego tryb globalny zalozylem
na WSZYSTKIE wypowiedzi tej komendy, nie tylko na te, ktore sonda odtworzyla
jako cisze.  Nie czytaj wiec 13/13 jako "bez naprawy dzialaloby prawie tak
samo" - sonda MIERZY MNIEJ, niz robi realna reka na klawiaturze.

PULAPKI ODZIEDZICZONE (nie powtarzaj): nazwy gestow to "downArrow"/"upArrow",
nie "down"; pauza po kazdym klawiszu obowiazkowa; BRAK MOWY to UWAGA, nie OK.
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

ROBOCZY = "/mnt/c/tmp/fn_ctrlaltk"
ROBOCZY_WIN = r"C:\tmp\fn_ctrlaltk"

wyniki = []


def zapisz(opis, ok, detal):
    wyniki.append((opis, ok, detal))
    print("  -> [{}] {}".format("OK   " if ok else "UWAGA", detal))


def przygotuj():
    if os.path.isdir(ROBOCZY):
        shutil.rmtree(ROBOCZY)
    os.makedirs(ROBOCZY)
    gotowy = ("# Dokument ze przypisem\r\n"
              "\r\n"
              "Zdanie z przypisem[^1] w tresci.\r\n"
              "\r\n"
              "Inne zdanie bez niczego.\r\n"
              "\r\n"
              "[^1]: Tresc pierwszego przypisu.\r\n")
    with io.open(os.path.join(ROBOCZY, "gotowe.md"), "w",
                 encoding="utf-8", newline="") as f:
        f.write(gotowy)
    bez = ("# Dokument bez przypisow\r\n"
           "\r\n"
           "Pierwsze zdanie dokumentu.\r\n")
    with io.open(os.path.join(ROBOCZY, "bez.md"), "w",
                 encoding="utf-8", newline="") as f:
        f.write(bez)
    with io.open(os.path.join(ROBOCZY, "zwykly.txt"), "w",
                 encoding="utf-8", newline="") as f:
        f.write("Zwykly plik tekstowy.\r\nDrugi wiersz.\r\n")


def start(nazwa):
    zamknij_edsharp()
    uruchom_edsharp(ROBOCZY_WIN + "\\" + nazwa)
    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa ({})".format(nazwa))
        return False
    return True


def main():
    przygotuj()

    # ===== 0. START: podmiana chordu nie dala alarmu =====
    print("=== TEST 0: start po przeniesieniu skoku na Control+Alt+K ===")
    z = znacznik()
    if not start("gotowe.md"):
        return 2
    m = mowa_od(z, 1.5)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    zapisz("0. brak alarmu 'Cannot assign' przy starcie",
           "cannot assign" not in caly and "already assigned" not in caly,
           "mowa startowa bez alarmu o zajetym skrocie")
    w_polu = w_edytorze()
    zapisz("0. fokus w POLU EDYCJI, nie w okienku alarmu",
           w_polu, "fokus w edytorze: {}".format(w_polu))
    if not w_polu:
        print("BLAD: fokus nie w polu edycji - dalszy pomiar bylby nieuczciwy")
        return 2

    # ===== 1. ZE ZDANIA DO TRESCI - kursor ORAZ mowa =====
    print("\n=== TEST 1: Control+Alt+K ze zdania do tresci przypisu ===")
    klaw("control+home", 0.8)
    klaw("downArrow", 0.5)
    klaw("downArrow", 0.6)
    w_start = linia_biezaca()
    print("   wiersz startowy:", repr(w_start))
    zapisz("1. kursor faktycznie stoi w zdaniu ze znacznikiem",
           "[^1]" in w_start,
           "wiersz: {!r}".format(w_start))
    z = znacznik()
    klaw("control+alt+k", 1.6)
    m = mowa_od(z, 1.4)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    w_cel = linia_biezaca()
    print("   wiersz po skoku:", repr(w_cel))
    zapisz("1. kursor PRZESKOCZYL na tresc przypisu",
           w_cel.strip().startswith("[^1]:"),
           "wiersz docelowy: {!r}".format(w_cel))
    # TO JEST TA ASERCJA, O KTORA CALY POMIAR CHODZI.
    zapisz("1. CZYTNIK REALNIE MOWI (bramka Control+Alt ominieta)",
           len([x for x in m if x.strip()]) > 0,
           "wypowiedzi: {}".format([x[:70] for x in m][:3] or "CISZA - BRAMKA TLUMI"))
    zapisz("1. w mowie jest tresc przypisu i slowo footnote",
           "footnote" in caly and "tresc" in caly,
           "mowa: {}".format([x for x in m if "ootnote" in x][:2] or "BRAK"))

    # ===== 2. Z TRESCI WRACAMY DO ZDANIA =====
    print("\n=== TEST 2: Control+Alt+K z tresci wraca do zdania ===")
    z = znacznik()
    klaw("control+alt+k", 1.6)
    m = mowa_od(z, 1.4)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    w_pow = linia_biezaca()
    print("   wiersz po powrocie:", repr(w_pow))
    zapisz("2. kursor WROCIL do zdania ze znacznikiem",
           "[^1]" in w_pow and not w_pow.strip().startswith("[^1]:"),
           "wiersz: {!r}".format(w_pow))
    zapisz("2. czytnik mowi przy powrocie",
           len([x for x in m if x.strip()]) > 0,
           "wypowiedzi: {}".format([x[:70] for x in m][:3] or "CISZA"))
    zapisz("2. w mowie jest 'in text'",
           "in text" in caly,
           "mowa: {}".format([x for x in m if "in text" in norm(x)][:2] or "BRAK"))

    # ===== 3. KONTROLA NEGATYWNA: stary Alt+F6 NIE MOZE nic robic =====
    print("\n=== TEST 3: KONTROLA NEGATYWNA - stary Alt+F6 jest wolny ===")
    klaw("control+home", 0.8)
    klaw("downArrow", 0.5)
    klaw("downArrow", 0.6)
    w_przed = linia_biezaca()
    z = znacznik()
    klaw("alt+f6", 1.5)
    m = mowa_od(z, 1.2)
    for x in m:
        print("   MOWA:", x[:160])
    w_po = linia_biezaca()
    print("   wiersz przed:", repr(w_przed), " po:", repr(w_po))
    zapisz("3. Alt+F6 NIE przenosi juz kursora do przypisu",
           not w_po.strip().startswith("[^1]:"),
           "wiersz po Alt+F6: {!r}".format(w_po))

    # ===== 4. KONTROLA POZYTYWNA: Alt+K (bez Control) nadal mowi =====
    print("\n=== TEST 4: KONTROLA POZYTYWNA - Alt+K nadal otwiera liste ===")
    z = znacznik()
    klaw("alt+k", 1.8)
    m = mowa_od(z, 1.2)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    f = fokus()
    zapisz("4. lista przypisow nadal dziala i mowi",
           "footnote" in caly or "footnote" in norm(str(f.get("name") or "")),
           "mowa/fokus: {} | {}".format([x[:60] for x in m][:2] or "BRAK",
                                        str(f.get("name"))[:50]))
    klaw("escape", 1.2)

    # ===== 5. ODMOWA: dokument BEZ przypisow MUSI byc SLYSZALNA =====
    # Najczulszy punkt: odmowy tez szly przez tlumiona sciezke mowy.
    print("\n=== TEST 5: Control+Alt+K bez przypisow - odmowa SLYSZALNA ===")
    if not start("bez.md"):
        return 2
    klaw("control+home", 0.8)
    z = znacznik()
    klaw("control+alt+k", 1.6)
    m = mowa_od(z, 1.4)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    zapisz("5. slychac 'No footnotes!' (nie cisza)",
           "no footnotes" in caly,
           "komunikat: {}".format([x[:70] for x in m][:2] or "CISZA - ODMOWA STLUMIONA"))

    # ===== 6. ODMOWA na pliku .txt =====
    print("\n=== TEST 6: Control+Alt+K na pliku .txt - odmowa SLYSZALNA ===")
    if not start("zwykly.txt"):
        return 2
    klaw("control+home", 0.8)
    z = znacznik()
    klaw("control+alt+k", 1.6)
    m = mowa_od(z, 1.4)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    zapisz("6. slychac odmowe 'work only on Markdown files'",
           "only on markdown files" in caly,
           "komunikat: {}".format([x[:70] for x in m][:2] or "CISZA - ODMOWA STLUMIONA"))

    zamknij_edsharp()

    print("\n=== PODSUMOWANIE ===")
    ile_ok = sum(1 for _, ok, _ in wyniki if ok)
    for opis, ok, detal in wyniki:
        print("[{}] {}".format("OK   " if ok else "UWAGA", opis))
    print("\nWYNIK: {}/{}".format(ile_ok, len(wyniki)))
    return 0 if ile_ok == len(wyniki) else 1


if __name__ == "__main__":
    sys.exit(main())
