#!/usr/bin/env python3
# Audyt zgodnosci: chord z CreateMenuItem w EdSharp.cs kontra chord w MOWIONYM
# opisie (Hotkeys.ini / hotkeys.txt), ktory Ctrl+F1 czyta niewidomemu.
#
# PO CO: bledny opis jest dla niewidomego GORSZY niz brak opisu - Key Describer
# poda mu skrot, ktory nic nie robi. Skrot ma dwa niezalezne zrodla prawdy i
# przy relokacji latwo poprawic tylko jedno.
#
# URUCHOMIENIE (z katalogu repozytorium):
#   python3 testy/audyt_skrotow_vs_opisy.py
# Kod wyjscia 0 = zgodne, 1 = sa rozbieznosci.
#
# Skrypt sam robi KONTROLE NEGATYWNA: psuje jeden wpis w pamieci i sprawdza, ze
# audyt to zglasza. Bez tego zielony wynik nie odrozniaby "zgodne" od "sonda
# nic nie widzi" - taka glucha sonda zdarzyla sie juz w tym projekcie.

import os
import re
import sys

KATALOG = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ZRODLO = os.path.join(KATALOG, "EdSharp.cs")
# PLIKI OPISOW: bierzemy tylko te, ktore ISTNIEJA.  Do 5.0.94 obok Hotkeys.ini
# lezal pisany recznie hotkeys.txt; od 5.0.95 podsumowanie nazywa sie
# EdSharp_Hotkeys.txt i jest GENEROWANE przy kazdym budowaniu
# (testy/generuj_podsumowanie_skrotow.py), wiec nie moze sie rozjechac.
# Sonda wolala stara nazwe i przy kazdym uruchomieniu padala na
# FileNotFoundError - czyli audyt skrotow nie dzialal wcale, cicho, od zmiany
# nazwy.  Brak pliku NIE moze wywracac audytu pozostalych.
_KANDYDACI = [os.path.join(KATALOG, "Hotkeys.ini"),
              os.path.join(KATALOG, "hotkeys.txt"),
              os.path.join(KATALOG, "EdSharp_Hotkeys.txt")]
OPISY = [p for p in _KANDYDACI if os.path.isfile(p)]

# Nazwy klawiszy w kodzie to nazwy z System.Windows.Forms.Keys, a w opisach
# ludzkie. Bez tej normalizacji audyt zwraca kilkadziesiat falszywek.
ALIASY = {
    "OemMinus": "Dash", "Oemplus": "Equals", "OemQuestion": "Slash",
    "Oem5": "Backslash", "OemSemicolon": "Semicolon", "OemQuotes": "Apostrophe",
    "OemOpenBrackets": "LeftBracket", "OemCloseBrackets": "RightBracket",
    "Oemcomma": "Comma", "OemPeriod": "Period", "Back": "Backspace",
    "Right": "RightArrow", "Left": "LeftArrow", "Up": "UpArrow",
    "Down": "DownArrow", "Prior": "PageUp", "Next": "PageDown",
}


def normalizuj(chord):
    czesci = [ALIASY.get(c, c) for c in chord.split("+")]
    return "+".join(re.sub(r"^D(\d)$", r"\1", c) for c in czesci)


def chordy_z_kodu():
    tekst = open(ZRODLO, encoding="utf-8", errors="replace").read()
    mapa = {}
    for m in re.finditer(r'^(?!\s*//).*CreateMenuItem\("([^"]+)",\s*"([^"]*)"', tekst, re.M):
        nazwa = m.group(1).replace("&", "").replace("...", "").strip()
        mapa[nazwa] = normalizuj(m.group(2).replace("&", ""))
    return mapa


def rozbieznosci(mapa, linie_opisow):
    wynik = []
    for plik, linie in linie_opisow:
        for linia in linie:
            if "=" not in linia or "," not in linia:
                continue
            nazwa, reszta = linia.split("=", 1)
            nazwa = nazwa.strip()
            chord = reszta.split(",")[0].strip()
            if nazwa in mapa and mapa[nazwa] != normalizuj(chord):
                wynik.append((os.path.basename(plik), nazwa, mapa[nazwa], chord))
    return wynik


# OPISY MOWIONE BEZ KOMENDY W KODZIE - dziedziczone i celowe.
#
# PO CO TA LISTA ISTNIEJE (zmierzone 04.09.2026 testem gluchoty): audyt
# porownywal chordy TYLKO dla nazw, ktore ma kod, wiec opis komendy USUNIETEJ
# byl cicho pomijany.  Podstawiony blad "zostaw No Guard w hotkeys.txt po
# usunieciu komendy" przechodzil na zielono - a to jest dokladnie ta klasa
# bledu, przed ktora audyt ma chronic: Ctrl+F1 czyta niewidomemu skrot, ktory
# nic nie robi.
#
# Ponizsze nazwy NIE sa pozycjami menu i nigdy nimi nie byly: to skroty
# skryptow JAWS (Voice*, Toggle Punctuation, Say* z rodziny mowy czytnika),
# klawisze dzialajace w okienku Otworz/Zapisz (Insert Script Path) oraz nazwy
# opisowe rozjechane z nazwa komendy juz w kodzie upstreamu (Say Chunk kontra
# komenda Chunk).  Kazda NOWA sierota to blad do naprawy, nie wpis do dopisania
# tutaj bez zastanowienia.
ZNANE_SIEROTY = {
    "Assign Numbered File", "Detached Preview", "Go to Window",
    "Insert All Users Path", "Insert Script Path", "Launch EdSharp",
    "Next Word", "Open Numbered File", "Preview", "Prior Word",
    "Say Address", "Say Braces", "Say Chunk", "Say Clipboard",
    "Say Compiler", "Say Font", "Say Indentation", "Say Path",
    "Say Selected", "Say Status", "Say Styles", "Say Time", "Say Yield",
    "Toggle Indentation", "Toggle Punctuation",
    "Voice Faster", "Voice Louder", "Voice Slower", "Voice Softer",
}


def sieroty(mapa, linie_opisow):
    """Opisy mowione, ktorym nie odpowiada zadna komenda w kodzie."""
    wynik = []
    for plik, linie in linie_opisow:
        for linia in linie:
            if "=" not in linia or "," not in linia:
                continue
            nazwa = linia.split("=", 1)[0].strip()
            if nazwa in mapa or nazwa in ZNANE_SIEROTY:
                continue
            wynik.append((os.path.basename(plik), nazwa))
    return wynik


def main():
    mapa = chordy_z_kodu()
    linie_opisow = [(p, open(p, encoding="utf-8", errors="replace").read().splitlines()) for p in OPISY]

    # KONTROLA NEGATYWNA: audyt musi zauwazyc celowo zepsuty opis.
    proba = mapa.copy()
    nazwa_proby = next(iter(k for k, v in proba.items() if v), None)
    if nazwa_proby is None:
        print("BLAD: nie wyluskano ani jednego chorda z kodu - sonda jest glucha.")
        return 1
    proba[nazwa_proby] = proba[nazwa_proby] + "X"
    if not any(r[1] == nazwa_proby for r in rozbieznosci(proba, linie_opisow)):
        print("BLAD: kontrola negatywna nie zadzialala, wynik audytu nic nie dowodzi.")
        return 1

    zle = rozbieznosci(mapa, linie_opisow)
    # KONTROLA WAZNOSCI SONDY SIEROT: nazwa, ktorej w kodzie NIE MA i ktora nie
    # jest na liscie znanych, MUSI byc zgloszona.  Bez tej kontroli "0 sierot"
    # nie odroznialoby porzadku od gluchej sondy.
    proba_sierot = [(OPISY[0], ["Komenda Ktorej Nie Ma=Control+Shift+F12, opis probny"])]
    if not sieroty(mapa, proba_sierot):
        print("BLAD: kontrola sierot nie dziala, wynik nic nie dowodzi.")
        return 1
    osierocone = sieroty(mapa, linie_opisow)
    print("Komend z chordem w kodzie: %d" % len([v for v in mapa.values() if v]))
    print("Kontrola negatywna: PASS (sonda wykrywa podstawiony blad)")
    print("Kontrola sierot: PASS (sonda wykrywa opis bez komendy)")
    if osierocone:
        print("OSIEROCONE OPISY: %d - Ctrl+F1 przeczyta skrot, ktory nic nie robi" % len(osierocone))
        for plik, nazwa in osierocone:
            print("  %s: %s - opis jest, komendy w kodzie NIE MA" % (plik, nazwa))
    if not zle and not osierocone:
        print("Rozbieznosci: 0 - kazdy mowiony opis podaje ten sam skrot, co kod.")
        return 0
    print("Rozbieznosci: %d" % len(zle))
    for plik, nazwa, w_kodzie, w_opisie in zle:
        print("  %s: %s - kod mowi %s, opis mowi %s" % (plik, nazwa, w_kodzie, w_opisie))
    return 1


if __name__ == "__main__":
    sys.exit(main())
