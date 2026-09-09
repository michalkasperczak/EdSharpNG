#!/usr/bin/env python3
"""Pomiar zlecenia Kasperczaka z 26.08.2026: w drzewie F6 ma byc slyszalny
PRAWDZIWY poziom naglowka Markdown, a nie tylko glebokosc galezi, ktora czytnik
wylicza sam ze struktury drzewa.

CO MIERZY: dla kazdej pozycji drzewa - to, co czytnik dostaje w polu 'name'
(a wiec to, co wymowi) - i sprawdza, ze konczy sie fraza "heading N", gdzie N
zgadza sie z LICZBA KRATEK w pliku, nie z glebokoscia galezi.

KONTROLA, KTORA ROZSTRZYGA POMIAR: w probce sa naglowki drugiego poziomu wiszace
na PIERWSZEJ glebokosci galezi. Dla nich czytnik oglasza "poziom 1", a nasza
etykieta musi mowic "heading 2". Jesli pomiar tego nie rozroznia, to znaczy, ze
mierzy glebokosc, a nie nasz dopisek - i wynik nic nie dowodzi.

Uruchomienie: EdSharpNG z otwarta probka, fokus w edytorze, potem
  python3 zmierz_poziomy_w_drzewie.py <sciezka do tej samej probki .md>
"""
import io
import json
import os
import re
import subprocess
import sys
import time
import urllib.request

BASE = "http://127.0.0.1:8765"
TOKEN = os.environ.get("NVDA_BRIDGE_TOKEN", "nvda-bridge-secret-change-me")
PS = "/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe"
KLIK = r"C:\Users\g\AppData\Local\Temp\klik.ps1"


def akcja(nazwa, **params):
    body = json.dumps({"action": nazwa, "params": params}).encode()
    req = urllib.request.Request(
        f"{BASE}/action", data=body,
        headers={"Content-Type": "application/json", "X-NVDA-Bridge-Token": TOKEN})
    with urllib.request.urlopen(req, timeout=20) as r:
        return json.loads(r.read().decode())


def fokus():
    d = akcja("get_current_focus")
    res = d.get("result") or d
    if isinstance(res, str):
        res = json.loads(res)
    return res.get("result", res)


def klaw(k, pauza=0.5):
    akcja("send_keys", keys=k)
    time.sleep(pauza)


def aktywuj():
    r = subprocess.run([PS, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", KLIK],
                       capture_output=True, text=True, timeout=120)
    time.sleep(0.8)
    return "EDSHARP_AKTYWNY=True" in r.stdout


def naglowki_z_pliku(sciezka):
    """Naglowki Markdown POZA blokami kodu - tak samo jak program."""
    tekst = io.open(sciezka, encoding="utf-8", errors="replace").read()
    wynik = []
    w_bloku = False
    for linia in tekst.splitlines():
        if linia.strip().startswith("```"):
            w_bloku = not w_bloku
            continue
        if w_bloku:
            continue
        m = re.match(r"^(#{1,6})\s+(.*)$", linia)
        if m:
            wynik.append((len(m.group(1)), m.group(2).strip()))
    return wynik


def main():
    if len(sys.argv) < 2:
        print("Podaj sciezke do tej samej probki .md, ktora jest otwarta w EdSharpie.")
        return 2
    oczekiwane = naglowki_z_pliku(sys.argv[1])
    print(f"naglowkow w pliku (poza blokami kodu): {len(oczekiwane)}")

    if not aktywuj():
        print("BLAD: nie aktywowalem okna EdSharpa")
        return 2
    klaw("control+home", 0.8)
    klaw("f6", 1.8)
    f = fokus()
    if "TreeView" not in str(f.get("windowClassName")):
        print("BLAD: fokus nie wszedl do drzewa Document Navigation:", f.get("windowClassName"))
        return 2

    klaw("home", 0.8)
    zebrane = []
    poprzednia = None
    for i in range(80):
        f = fokus()
        nazwa = f.get("name") or ""
        if nazwa == poprzednia and i > 0:
            break
        zebrane.append(nazwa)
        poprzednia = nazwa
        klaw("downArrow", 0.45)
    klaw("escape", 1.0)

    print(f"\n=== POZYCJE DRZEWA, JAK JE DOSTAJE CZYTNIK ({len(zebrane)}) ===")
    for n in zebrane:
        print("  " + repr(n))

    print("\n=== WERYFIKACJA ===")
    wyniki = []

    # 1. kazda pozycja konczy sie "heading N"
    bez = [n for n in zebrane if not re.search(r", heading [1-6]$", n)]
    wyniki.append(("kazda pozycja konczy sie 'heading N'", not bez,
                   f"bez dopisku: {bez[:3]}" if bez else f"wszystkie {len(zebrane)}"))

    # 2. poziomy zgadzaja sie z liczba kratek w pliku, po kolei
    poziomy_drzewo = [int(re.search(r", heading ([1-6])$", n).group(1))
                      for n in zebrane if re.search(r", heading ([1-6])$", n)]
    poziomy_plik = [p for p, _ in oczekiwane][:len(poziomy_drzewo)]
    zgodne = poziomy_drzewo == poziomy_plik
    wyniki.append(("poziomy = liczba kratek w pliku (kolejnosc zachowana)", zgodne,
                   f"drzewo {poziomy_drzewo[:12]} vs plik {poziomy_plik[:12]}"))

    # 3. KONTROLA ROZSTRZYGAJACA: czy pomiar w ogole rozroznia nasz dopisek od
    #    glebokosci galezi. Naglowek poziomu 2 na pierwszej glebokosci galezi
    #    MUSI miec w etykiecie "heading 2", nie "heading 1".
    drugie = [n for n in zebrane if re.search(r", heading 2$", n)]
    wyniki.append(("kontrola: sa pozycje 'heading 2' (glebokosc galezi = 1)",
                   bool(drugie),
                   f"{len(drugie)} pozycji, np. {drugie[0]!r}" if drugie
                   else "BRAK - pomiar nie odroznia poziomu naglowka od glebokosci galezi"))

    # 4. tytul zostal nietkniety przed przecinkiem (nawigacja literowa)
    tytuly_drzewo = [re.sub(r", heading [1-6]$", "", n) for n in zebrane]
    tytuly_plik = [t for _, t in oczekiwane][:len(tytuly_drzewo)]
    def norm(s):
        for a, b in (("ą","a"),("ć","c"),("ę","e"),("ł","l"),("ń","n"),
                     ("ó","o"),("ś","s"),("ź","z"),("ż","z")):
            s = s.replace(a, b)
        return s.lower().strip()
    rozne = [(a, b) for a, b in zip(tytuly_drzewo, tytuly_plik) if norm(a) != norm(b)]
    wyniki.append(("tytul przed przecinkiem = tytul z pliku (litera nadal skacze)",
                   not rozne, f"rozbieznosci: {rozne[:2]}" if rozne else "wszystkie zgodne"))

    for opis, ok, detal in wyniki:
        print(f"  [{'OK ' if ok else 'ZLE'}] {opis}: {detal}")
    zle = [w for w in wyniki if not w[1]]
    print(f"\nzaliczone: {len(wyniki) - len(zle)} z {len(wyniki)}")
    return 0 if not zle else 1


if __name__ == "__main__":
    sys.exit(main())
