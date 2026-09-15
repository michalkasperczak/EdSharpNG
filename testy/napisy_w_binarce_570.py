import sys

sciezka = "/mnt/c/EdSharp/EdSharpNG.exe"
dane = open(sciezka, "rb").read()

# DWA ROZNE KODOWANIA W JEDNYM PLIKU, i pomylka tu daje FALSZYWE ZERO
# (nadzialem sie na to w pierwszym biegu dowodu 5.0.69):
# - LITERALE tekstowe programu siedza w strumieniu #US jako UTF-16 LE,
# - NAZWY metod, typow i pol siedza w heapie #Strings jako UTF-8.
def ile_literal(napis):
    return dane.count(napis.encode("utf-16-le"))

def ile_nazwa(napis):
    return dane.count(napis.encode("utf-8"))

# "Preferred DropEffect" to nazwa formatu schowka, wiec w programie jest
# LITERALEM, nie nazwa metody - bez niej czesc powlok PRZENIOSLABY plik.
# UWAGA NA BRZMIENIE KOMUNIKATU.  Szukalismy tu kiedys "File copied", ale
# 11.09.2026 Michal kazal skrocic ten komunikat do samego "Copied" ("troche
# mylaco mowi Copied file (...) powinien mowic Copied po prostu") - slowo
# "file" nazywalo FORMAT schowka, a nie to, co sie stalo.  Test zostal ze starym
# napisem i od tamtej pory swiecil na czerwono przy POPRAWNYM kodzie.
# Sprawdzamy wiec to, co program naprawde mowi.
nowe_literaly = ["Preferred DropEffect", "Copied", "missing as text only"]
nowe_nazwy = ["SetClipboardFileDrop", "SetFileDropList"]
# STARY KOMUNIKAT MUSI ZNIKNAC.  Sama obecnosc nowych napisow przeszlaby takze
# wtedy, gdyby galaz plikowa zostala dopisana OBOK starej i nigdy nie wykonana.
#
# PULAPKA PREFIKSU, zlapana w tym biegu: pytanie o "Path copied" dalo 1 przy
# POPRAWNYM kodzie, bo ten napis jest PODCIAGIEM nowego komunikatu "Path copied
# as text, file not found".  Literale w strumieniu #US leza ciasno obok siebie,
# wiec zwykle wyszukanie podciagu trafia ZA DUZO.  Rozstrzyga dopasowanie
# DOKLADNE: przed literalem stoi prefiks dlugosci w bajtach (dla napisow do 63
# znakow jeden bajt o wartosci 2*dlugosc+1).
stare_dokladne = ["Path copied"]

def ile_dokladny(napis):
    aU = napis.encode("utf-16-le")
    if len(aU) + 1 > 0x7F:
        return dane.count(aU)
    return dane.count(bytes([len(aU) + 1]) + aU)

stare_literaly = []
kontrola_literal = ["ZupelnieNieMaTakiegoNapisu"]
kontrola_nazwa = ["ZupelnieNieMaTakiejMetody"]

bledy = 0
print("=== NOWE literale programu, UTF-16 (maja byc > 0) ===")
for n in nowe_literaly:
    k = ile_literal(n)
    print(f"  {n!r}: {k}")
    if k == 0:
        bledy += 1
print("=== NOWE nazwy metod, UTF-8 (maja byc > 0) ===")
for n in nowe_nazwy:
    k = ile_nazwa(n)
    print(f"  {n!r}: {k}")
    if k == 0:
        bledy += 1
print("=== STARE literale zdjete, dopasowanie DOKLADNE (maja byc 0) ===")
for n in stare_dokladne:
    k = ile_dokladny(n)
    print(f"  {n!r} dokladnie: {k}")
    if k != 0:
        bledy += 1
    # Dla porownania: samo wyszukanie podciagu trafia w nowszym, dluzszym
    # komunikacie - dlatego ta liczba NIE jest asercja.
    print(f"    (podciag, tylko informacyjnie: {ile_literal(n)})")
print("=== KONTROLA SONDY, oba kodowania (ma byc 0) ===")
for n in kontrola_literal:
    k = ile_literal(n)
    print(f"  literal {n!r}: {k}")
    if k != 0:
        bledy += 1
for n in kontrola_nazwa:
    k = ile_nazwa(n)
    print(f"  nazwa {n!r}: {k}")
    if k != 0:
        bledy += 1
# KONTROLA POZYTYWNA OBU KODOWAN: napis i nazwa, ktore w binarce SA od dawna.
# Bez niej "0 trafien" nie odroznia braku zmiany od zlego kodowania w sondzie.
print("=== KONTROLA POZYTYWNA (maja byc > 0) ===")
for n in ["Clipboard is busy"]:
    k = ile_literal(n)
    print(f"  literal {n!r}: {k}")
    if k == 0:
        bledy += 1
for n in ["PickFileCopySelection"]:
    k = ile_nazwa(n)
    print(f"  nazwa {n!r}: {k}")
    if k == 0:
        bledy += 1
# Kontrola dopasowania DOKLADNEGO: napis, ktory w binarce stoi samodzielnie,
# musi byc widoczny takze z prefiksem dlugosci.  Bez tego "0 dokladnych" nie
# odroznia porzadku od sondy liczacej zawsze zero.
for n in ["No item!"]:
    k = ile_dokladny(n)
    print(f"  dokladnie {n!r}: {k}")
    if k == 0:
        bledy += 1

print("WYNIK:", "OK" if bledy == 0 else f"{bledy} niezgodnosci")
# Pelne rozroznienie wersji mierzy testy/kontrola_negatywna_570.sh na binarce
# 5.0.69, plus harness_schowek_plikow_570.cs, ktory czyta schowek Z POWROTEM.
sys.exit(1 if bledy else 0)
