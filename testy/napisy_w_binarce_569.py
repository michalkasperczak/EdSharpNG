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

nowe_literaly = ["at a time", "Clipboard is busy, nothing copied!"]
nowe_nazwy = ["PickFileCopySelection"]
stare_literaly = ["Path copied", "Clipboard is busy, path not copied!"]
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
print("=== STARE literale zdjete (maja byc 0) ===")
for n in stare_literaly:
    k = ile_literal(n)
    print(f"  {n!r}: {k}")
    if k != 0:
        bledy += 1
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

print("WYNIK:", "OK" if bledy == 0 else f"{bledy} niezgodnosci")
# CO TU ROZROZNIA STAN PRZED I PO: para warunkow, nie sam nowy napis.
# Nowe literale > 0 przeszlyby takze wtedy, gdyby dopisac je OBOK starej drogi
# kopiowania; zerowa liczba starych ("Path copied", "Clipboard is busy, path not
# copied!") mowi, ze stara droga zostala ZASTAPIONA, a nie uzupelniona.
# Pelne rozroznienie wersji mierzy testy/kontrola_negatywna_569.sh na binarce
# odniesienia zbudowanej z rewizji 5.0.68.
sys.exit(1 if bledy else 0)
