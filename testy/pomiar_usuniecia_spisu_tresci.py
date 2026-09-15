#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Pomiar usunięcia trzech komend starego modelu sekcji ze ZBUDOWANEGO EdSharpNG.exe.

PO CO: Kasperczak polecił 26.08.2026 (zlecenie 1787700780170-8) usunąć
"spis treści w obecnej formie". Chodzi o trzy komendy oparte na znaku
Form Feed, którego w jego plikach Markdown nie ma ani jednego:
  Go to Section    (Control+Shift+F12)
  Go to Contents   (Shift+F6)
  Text Contents    (Alt+Shift+T) - dodatkowo PISAŁA do dokumentu
Rolę spisu treści pełni drzewo nagłówków pod F6.
Samo "usunąłem w kodzie" nic nie znaczy, dopóki nie widać, że w binarce,
którą on dostanie, tych komend NIE MA.

CO MIERZY (na pliku EdSharpNG.exe, nie na źródle):
  1. Brak pól menuNavigateGoToSection, menuNavigateGoToContents,
     menuMiscTextContents w typie EdSharp.MdiFrame.
  2. Brak napisów "Go to Section", "Go to Contents", "Text Contents".
  3. Skróty Control+Shift+F12 i Alt+Shift+T nie występują w binarce,
     czyli są wolne. Shift+F6 sprawdzamy osobno: sam ciąg "Shift+F6"
     zawiera się w "Alt+Shift+F6" i "Control+Shift+F6", więc szukamy
     przypisania dokładnego.

CZEGO USUNIĘCIE BYŁOBY BŁĘDEM (kontrola: usunąć za dużo jest tu groźniejsze
niż za mało - te komendy Kasperczak zaliczył w testach i używają tej samej
stałej Form Feed):
  Section Break          Control+Enter
  Topic                  Alt+T
Każda MUSI nadal być w binarce - jako pole i jako napis.

UWAGA NA DATY: Search for Topic (Control+F6) i Search for Topic Again (Alt+F6)
były tu wymagane, ale 27.08.2026 Michał zgodził się je usunąć - należały do
starego modelu Form Feed. Podobnie Alt+Shift+T i Shift+F6 zostały tego dnia
przypisane do NOWYCH komend (Table of Contents, kontekstowy skok do spisu).
Test aktualizowano 16.09.2026, żeby mierzył stan po tych decyzjach.

KONTROLE NEGATYWNE (bez nich zielony wynik nic nie dowodzi - sonda, która
niczego nie znajduje, może być po prostu zepsuta):
  A. Pole menuMiscRepeatLine MUSI się znaleźć - sonda czyta pola.
  B. Napis "Repeat Line" MUSI się znaleźć - sonda czyta napisy.
  C. Skrót "Control+Y" MUSI się znaleźć - sonda czyta skróty.
  D. Ta sama sonda puszczona na binarkę SPRZED zmiany (jeśli podana jako
     drugi argument) MUSI znaleźć wszystkie trzy usuwane komendy.

Uruchomienie:
  python3 testy/pomiar_usuniecia_spisu_tresci.py EdSharpNG.exe [stary.exe]

PUŁAPKA: starą binarkę do kontroli D trzeba skopiować do katalogu projektu.
Podanie ścieżki w /tmp kończy się "PermissionError: Permission denied" -
sonda jest programem Windows i nie wykona pliku z systemu plików WSL.

DRUGA PUŁAPKA: kontrola D wymaga binarki SPRZED 26.08.2026, czyli takiej,
która te komendy jeszcze MA. Podanie dowolnej nowszej wersji (np. 5.0.107)
daje cztery czerwone linie, a to nie jest usterka - w tej wersji komendy są już
usunięte, dokładnie jak być powinno. Bez takiej archiwalnej binarki po prostu
nie podawaj drugiego argumentu.
"""

import subprocess
import sys
import os

CSC = "/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"

SONDA_CS = r'''
using System;
using System.Reflection;

public class SondaPol2 {
public static int Main(string[] a) {
Assembly asm = Assembly.LoadFrom(a[0]);
Type t = asm.GetType("EdSharp.MdiFrame");
if (t == null) {Console.WriteLine("BLAD: brak typu EdSharp.MdiFrame"); return 2;}
foreach (FieldInfo fi in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
Console.WriteLine(fi.Name);
return 0;
}
}
'''

# UWAGA: "Go to Contents" WROCILO 27.08.2026 jako komenda kontekstowa pod
# Shift+F6 - inna niz usunieta, oparta na spisie Markdown, a nie na Form Feed.
# Nie ma jej wiec na liscie usuwanych, mimo tej samej nazwy.
USUWANE_POLA = ["menuNavigateGoToSection", "menuMiscTextContents"]
USUWANE_NAPISY = ["Go to Section", "Text Contents"]
# Search for Topic i Search for Topic Again ZESZLY z tej listy 27.08.2026 -
# Michal zgodzil sie je usunac ("Tak. Mozna podmienic ten klawisz.").
ZOSTAJA_POLA = ["menuMiscSectionBreak", "menuQueryTopic"]
ZOSTAJA_NAPISY = ["Section Break"]


def napisy_w_binarce(sciezka):
    """Napisy .NET siedzą w heapie jako UTF-16LE, ale nie muszą zaczynać się
    na parzystym bajcie pliku. Dekodowanie od jednego offsetu gubi połowę
    napisów - i wtedy sonda milczy nie dlatego, że napisu nie ma, a dlatego,
    że patrzy w złe miejsce (pułapka z pomiaru Format Code, 26.08.2026).
    Dlatego dekodujemy oba wyrównania oraz Latin-1."""
    dane = open(sciezka, "rb").read()
    return (dane.decode("utf-16-le", errors="ignore")
            + "\n" + dane[1:].decode("utf-16-le", errors="ignore")
            + "\n" + dane.decode("latin-1", errors="ignore"))


def pola_typu(exe):
    kat = os.path.dirname(os.path.abspath(exe)) or "."
    cs = os.path.join(kat, "_sonda_pol2.cs")
    ex = os.path.join(kat, "_sonda_pol2.exe")
    open(cs, "w").write(SONDA_CS)
    try:
        # csc.exe to program Windows: nie rozumie ścieżek /mnt/..., więc
        # wołamy go z katalogu roboczego i podajemy same nazwy plików.
        r = subprocess.run([CSC, "/nologo", "/out:_sonda_pol2.exe",
                            "_sonda_pol2.cs"],
                           capture_output=True, text=True, cwd=kat)
        if r.returncode != 0:
            raise RuntimeError("nie zbudowano sondy: " + r.stdout + r.stderr)
        r = subprocess.run(["./_sonda_pol2.exe", os.path.basename(exe)],
                           capture_output=True, text=True, cwd=kat)
        if r.returncode != 0:
            raise RuntimeError("sonda padla: " + r.stdout + r.stderr)
        return set(r.stdout.split())
    finally:
        for p in (cs, ex):
            if os.path.exists(p):
                os.remove(p)


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    exe = sys.argv[1]
    stary = sys.argv[2] if len(sys.argv) > 2 else None

    zle = 0

    def spr(opis, ok, detal=""):
        nonlocal zle
        print(("  [OK ] " if ok else "  [ZLE] ") + opis + (": " + detal if detal else ""))
        if not ok:
            zle += 1

    print("Mierzona binarka: " + os.path.abspath(exe))
    pola = pola_typu(exe)
    tekst = napisy_w_binarce(exe)
    print("Pol w typie MdiFrame: %d, znakow tekstu w binarce: %d"
          % (len(pola), len(tekst)))

    print("\nKONTROLE NEGATYWNE (sonda musi cokolwiek znajdowac):")
    spr("A. pole menuMiscRepeatLine jest w binarce", "menuMiscRepeatLine" in pola)
    spr("B. napis 'Repeat Line' jest w binarce", "Repeat Line" in tekst)
    # Control+Y przestal byc osobnym skrotem (Redo to Control+Shift+Z, a
    # Control+Y robi to samo przez obsluge kontrolki) - stara kontrola swiecila
    # na czerwono przy poprawnej binarce.
    spr("C. skrot 'Control+Shift+Z' jest w binarce", "Control+Shift+Z" in tekst)

    print("\nWLASCIWY POMIAR - trzy komendy maja NIE ISTNIEC:")
    for p in USUWANE_POLA:
        spr("brak pola " + p, p not in pola)
    for n in USUWANE_NAPISY:
        spr("brak napisu '%s'" % n, n not in tekst)
    spr("skrot Control+Shift+F12 wolny", "Control+Shift+F12" not in tekst)
    # ALT+SHIFT+T JUZ NIE JEST WOLNY - I TAK MA BYC.  Skrot zostal zwolniony
    # 26.08.2026 po usunieciu "Text Contents", a 27.08.2026 Michal przypisal do
    # niego NOWA komende "Table of Contents" (spis tresci Markdown z linkami).
    # Test pilnowal wiec stanu z jednego dnia i od 27.08 klamal.
    spr("Alt+Shift+T zajety przez NOWA komende Table of Contents",
        "Alt+Shift+T" in tekst and "Table of Contents" in tekst)
    # SHIFT+F6 WROCILO 27.08.2026 I TO NIE JEST USTERKA.  Michal sam wskazal
    # ten skrot i chcial go kontekstowego w obie strony ("Shift-F6 kontekstowo
    # w obie strony to dobre rozwiazanie"): z tresci skacze do pozycji spisu,
    # z pozycji spisu do rozdzialu.  Usuniete 26.08 "Go to Contents" oparte na
    # znaku Form Feed to BYLA INNA komenda o tej samej nazwie.  Sprawdzamy wiec,
    # ze skrot jest przypisany do komendy nawigacyjnej.
    spr("Shift+F6 przypisany do kontekstowego skoku do spisu tresci",
        "Shift+F6" in tekst and "Go to Contents" in tekst)

    print("\nKONTROLA, ZE NIE USUNIETO ZA DUZO (te komendy Kasperczak zaliczyl):")
    for p in ZOSTAJA_POLA:
        spr("pole %s NADAL jest" % p, p in pola)
    for n in ZOSTAJA_NAPISY:
        spr("napis '%s' NADAL jest" % n, n in tekst)
    spr("skrot Control+Enter (Section Break) NADAL jest", "Control+Enter" in tekst)
    spr("skrot Alt+T (Topic) NADAL jest", "Alt+T" in tekst)
    # SEARCH FOR TOPIC I SEARCH FOR TOPIC AGAIN ZOSTALY USUNIETE 27.08.2026
    # na wyrazna zgode Michala ("Tak. Mozna podmienic ten klawisz.").  Nalezaly
    # do oryginalnego modelu tekstu strukturalnego i szukaly wzorca ze znakiem
    # Form Feed, ktorego w plikach Markdown nie ma ani jednego.  Test pisany
    # 26.08 wymagal ich obecnosci - dzien pozniej stal sie nieprawdziwy.
    # Zamiast tego pilnujemy, zeby NIE WROCILY przy scalaniu z upstreamem.
    spr("Search for Topic usuniete (27.08.2026)",
        "menuNavigateSearchForTopic" not in pola)
    spr("komunikat 'No topic to search for again!' usuniety",
        "No topic to search for again!" not in tekst)

    if stary:
        print("\nKONTROLA D - ta sama sonda na binarce SPRZED zmiany (%s):" % stary)
        pola_st = pola_typu(stary)
        tekst_st = napisy_w_binarce(stary)
        for p in USUWANE_POLA:
            spr("D. stara binarka MA pole " + p, p in pola_st)
        for n in USUWANE_NAPISY:
            spr("D. stara binarka MA napis '%s'" % n, n in tekst_st)
    else:
        print("\nKONTROLA D pominieta - nie podano starej binarki.")

    print("\nWynik: %s (bledow: %d)" % ("ZDANE" if zle == 0 else "NIEZDANE", zle))
    return 0 if zle == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
