#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Generuje EdSharp_Hotkeys.txt (Podsumowanie skrotow, Alt+Shift+H) z Hotkeys.ini.

PO CO TO ISTNIEJE
=================
Do 5.0.94 byly DWA niezalezne pliki opisujace te same skroty:

  Hotkeys.ini          - czytany przez program (Key Describer, paleta polecen)
  EdSharp_Hotkeys.txt  - pokazywany uzytkownikowi pod Alt+Shift+H

Drugi z nich nie byl aktualizowany od czasu upstreamu.  Zmierzone 13.09.2026:
nie mial ANI JEDNEGO opisu komentarzy, mial stare chordy zakladek, obiecywal
Control+T dla Text Convert (komenda dawno przeniesiona) i nie znal
Control+Shift+W.  Uzytkownik niewidomy, ktory otwiera podsumowanie skrotow,
dostawal wiec liste klamiaca o tym, co program robi - a to gorsze niz brak
listy, bo nie ma jak tego zauwazyc bez wciskania kazdego klawisza po kolei.

Do tego kod otwieral plik pod STARA nazwa ("HotKeys.txt"), ktorej w katalogu
programu nie ma od 5.0.73 - Alt+Shift+H otwieralo pusty, nowy dokument.

ROZWIAZANIE: jedno zrodlo prawdy.  Hotkeys.ini jest czytany przez program, wiec
on jest zrodlem; EdSharp_Hotkeys.txt POWSTAJE z niego przy kazdym budowaniu.
Rozjechac sie nie moga, bo drugi nie jest juz pisany rekami.

UZYCIE:  python3 testy/generuj_podsumowanie_skrotow.py [--sprawdz]
  bez opcji   - zapisuje EdSharp_Hotkeys.txt
  --sprawdz   - tylko sprawdza, czy plik na dysku zgadza sie ze zrodlem
                (kod wyjscia 1, gdy sie rozjechal); tego uzywa zbuduj.sh
"""
import os
import pathlib
import sys

REPO = pathlib.Path(os.environ.get("EDSHARP_REPO",
                                   pathlib.Path(__file__).resolve().parent.parent))
ZRODLO = REPO / "Hotkeys.ini"
CEL = REPO / "EdSharp_Hotkeys.txt"

NAGLOWEK = [
    "Hotkey Summary",
    "",
    "List of EdSharp commands, their keys and what they do.",
    "",
    "This file is GENERATED from Hotkeys.ini when the program is built.",
    "Do not edit it by hand - edit Hotkeys.ini instead, otherwise the two",
    "disagree and this summary starts announcing keys that have moved.",
    "",
]


def wiersze_zrodla(tekst):
    """Zwraca [(nazwa, skrot, opis)] z sekcji [Hotkeys]; puste wiersze jako None.

    Format wiersza w Hotkeys.ini:  Nazwa=Skrot, opis czego dotyczy
    Skrot moze byc PUSTY (polecenie bez klawisza) - wtedy mowimy o tym wprost,
    bo "" w kolumnie klawisza czyta sie jak usterka, a nie jak informacja.
    """
    wynik = []
    w_sekcji = False
    for linia in tekst.replace("\r\n", "\n").split("\n"):
        goly = linia.strip()
        if goly.startswith("[") and goly.endswith("]"):
            w_sekcji = (goly.lower() == "[hotkeys]")
            continue
        if not w_sekcji:
            continue
        if not goly:
            # Puste wiersze w zrodle dziela polecenia na grupy tematyczne.
            # Zachowujemy je: dla czytnika ekranu to jedyny podzial tej listy.
            if wynik and wynik[-1] is not None:
                wynik.append(None)
            continue
        if goly.startswith(";") or "=" not in goly:
            continue
        nazwa, reszta = goly.split("=", 1)
        if "," in reszta:
            skrot, opis = reszta.split(",", 1)
        else:
            skrot, opis = reszta, ""
        wynik.append((nazwa.strip(), skrot.strip(), opis.strip()))
    while wynik and wynik[-1] is None:
        wynik.pop()
    return wynik


def zbuduj_tekst():
    dane = wiersze_zrodla(ZRODLO.read_text(encoding="utf-8", errors="replace"))
    linie = list(NAGLOWEK)
    ile = 0
    for wpis in dane:
        if wpis is None:
            linie.append("")
            continue
        nazwa, skrot, opis = wpis
        klawisz = skrot if skrot else "(no key assigned)"
        linie.append(", ".join(x for x in (nazwa, klawisz, opis) if x))
        ile += 1
    linie.append("")
    linie.append("Commands listed: %d" % ile)
    linie.append("")
    # Konce wierszy CRLF: plik otwiera sie w Notatniku i w samym EdSharpie na
    # Windowsie.  Bez BOM - znacznik BOM lamie czytanie plikow ini przez
    # Windows i nie ma powodu wprowadzac go tutaj.
    return "\r\n".join(linie)


def main():
    if not ZRODLO.exists():
        print("BLAD: brak %s" % ZRODLO)
        return 2
    nowy = zbuduj_tekst()
    if "--sprawdz" in sys.argv:
        # ODCZYT BEZ TLUMACZENIA KONCOW WIERSZY.  read_text() zamienia CRLF na
        # LF, wiec porownanie z tekstem pisanym w CRLF zawsze wychodzilo rozne i
        # sprawdzenie krzyczalo "rozjechal sie" nawet zaraz po wygenerowaniu -
        # czyli mierzylo wlasny sposob czytania, a nie tresc pliku.
        if CEL.exists():
            with CEL.open("r", encoding="utf-8", errors="replace", newline="") as f:
                stary = f.read()
        else:
            stary = ""
        if stary != nowy:
            print("BLAD: %s rozjechal sie z Hotkeys.ini." % CEL.name)
            print("      Uruchom: python3 testy/generuj_podsumowanie_skrotow.py")
            return 1
        print("OK: podsumowanie skrotow zgadza sie z Hotkeys.ini")
        return 0
    CEL.write_text(nowy, encoding="utf-8", newline="")
    print("Zapisane: %s (%d znakow)" % (CEL, len(nowy)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
