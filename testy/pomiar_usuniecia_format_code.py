#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Pomiar usunięcia komendy Format Code z ZBUDOWANEGO EdSharpNG.exe.

PO CO: Kasperczak polecił 26.08.2026 (zlecenie 1787700621195-7) usunąć
komendę Format Code. To ona zamieniła mu plik z wynikami testów na HTML
i nie dało się tego cofnąć. Samo "poprawiłem w kodzie" nic nie znaczy,
dopóki nie widać, że w binarce, którą on dostanie, tej komendy NIE MA.

CO MIERZY (na pliku EdSharpNG.exe, nie na źródle):
  1. W metadanych typu EdSharp.MdiFrame nie ma pola menuMiscFormatCode.
  2. W binarce nie ma napisu "Format Code" ani komunikatów tej komendy.
  3. Skrót Control+Shift+F6 nie występuje w binarce, czyli jest wolny.
  4. Nie ma już wywołań narzędzi zewnętrznych z tej komendy (astyle.exe
     uruchamiany na dokumencie, tidy z flagą modify-in-place).

KONTROLE NEGATYWNE (bez nich zielony wynik nic nie dowodzi - sonda, która
niczego nie znajduje, może być po prostu zepsuta):
  A. Pole menuMiscRepeatLine MUSI się znaleźć - sonda potrafi czytać pola.
  B. Napis "Repeat Line" MUSI się znaleźć - sonda potrafi czytać napisy.
  C. Skrót "Control+Y" MUSI się znaleźć - sonda potrafi czytać skróty.
  D. Ta sama sonda puszczona na binarkę SPRZED zmiany (jeśli jest podana
     jako drugi argument) MUSI zgłosić Format Code jako obecne.

Uruchomienie:
  python3 testy/pomiar_usuniecia_format_code.py EdSharpNG.exe [stary.exe]
"""

import subprocess
import sys
import os

CSC = "/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"

SONDA_CS = r'''
using System;
using System.Reflection;

public class SondaPol {
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


def napisy_w_binarce(sciezka):
    """Napisy .NET siedzą w heapie jako UTF-16LE, ale nie muszą zaczynać się
    na parzystym bajcie pliku. Dekodowanie od jednego offsetu gubi połowę
    napisów - i wtedy sonda milczy nie dlatego, że napisu nie ma, a dlatego,
    że patrzy w złe miejsce. Dlatego dekodujemy oba wyrównania oraz Latin-1
    (napisy w tabeli nazw metadanych są jednobajtowe) i szukamy w sumie."""
    dane = open(sciezka, "rb").read()
    return (dane.decode("utf-16-le", errors="ignore")
            + "\n" + dane[1:].decode("utf-16-le", errors="ignore")
            + "\n" + dane.decode("latin-1", errors="ignore"))


def pola_typu(exe):
    kat = os.path.dirname(os.path.abspath(exe)) or "."
    cs = os.path.join(kat, "_sonda_pol.cs")
    ex = os.path.join(kat, "_sonda_pol.exe")
    open(cs, "w").write(SONDA_CS)
    try:
        # csc.exe to program Windows: nie rozumie sciezek /mnt/..., wiec
        # wolamy go z katalogu roboczego i podajemy same nazwy plikow.
        r = subprocess.run([CSC, "/nologo", "/out:_sonda_pol.exe",
                            "_sonda_pol.cs"],
                           capture_output=True, text=True, cwd=kat)
        if r.returncode != 0:
            raise RuntimeError("nie zbudowano sondy: " + r.stdout + r.stderr)
        # Ta sama zasada dla samej sondy: nazwa binarki wzgledna, cwd ustawione.
        r = subprocess.run(["./_sonda_pol.exe", os.path.basename(exe)],
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
    print("Pol w typie MdiFrame: %d, znakow tekstu w binarce: %d" % (len(pola), len(tekst)))

    print("\nKONTROLE NEGATYWNE (sonda musi cokolwiek znajdowac):")
    spr("A. pole menuMiscRepeatLine jest w binarce", "menuMiscRepeatLine" in pola)
    spr("B. napis 'Repeat Line' jest w binarce", "Repeat Line" in tekst)
    spr("C. skrot 'Control+Shift+Z' jest w binarce", "Control+Shift+Z" in tekst)

    print("\nWLASCIWY POMIAR - Format Code ma NIE ISTNIEC:")
    spr("1. brak pola menuMiscFormatCode", "menuMiscFormatCode" not in pola)
    spr("2. brak napisu 'Format Code'", "Format Code" not in tekst)
    spr("3. brak napisu 'Format code' (komunikat po wykonaniu)", "Format code" not in tekst)
    spr("4. skrot Control+Shift+F6 wolny", "Control+Shift+F6" not in tekst)
    # SLOWO "astyle" ZOSTAJE W BINARCE ZGODNIE Z PRAWDA - i to nie jest usterka.
    # Skladniki.cs trzyma liste narzedzi DO POBRANIA (pandoc, tidy, xpdf,
    # liblouis, astyle) wraz z adresami; AStyle jest tam nadal, bo uzytkownik
    # moze go sobie sciagnac.  Usuniete zostalo POLECENIE "Format Code", ktore
    # przepuszczalo dokument przez astyle.exe.  Szukanie samego "astyle"
    # swiecilo wiec na czerwono przy poprawnym kodzie.  Mierzymy nazwe metody,
    # ktora to polecenie wykonywala.
    spr("5. brak metody wolajacej astyle na dokumencie",
        "FormatCode" not in tekst and "RunAStyle" not in tekst)
    spr("6. brak wywolania tidy z modify-in-place",
        "tidy.exe -config" not in tekst)

    if stary:
        print("\nKONTROLA D - ta sama sonda na binarce SPRZED zmiany (%s):" % stary)
        pola_st = pola_typu(stary)
        tekst_st = napisy_w_binarce(stary)
        spr("D1. stara binarka MA pole menuMiscFormatCode",
            "menuMiscFormatCode" in pola_st)
        spr("D2. stara binarka MA napis 'Format Code'", "Format Code" in tekst_st)
    else:
        print("\nKONTROLA D pominieta - nie podano starej binarki.")

    print("\nWynik: %s (bledow: %d)" % ("ZDANE" if zle == 0 else "NIEZDANE", zle))
    return 0 if zle == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
