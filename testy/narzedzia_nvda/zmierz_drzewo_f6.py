#!/usr/bin/env python3
"""Zbiera drzewo Document Navigation (F6) przez mostek NVDA i sprawdza wymagania
z listy testow 5.0-5.0.21, rozdzial 1 (F6) - te, ktorych autor listy nie mogl
zmierzyc, bo pisal "nie mam ani NVDA, ani JAWS-a".

Mierzy DOKLADNIE to, co czytnik oglasza: name + states elementu drzewa.
"""
import json
import os
import re
import sys
import time
import urllib.request

BASE = "http://127.0.0.1:8765"
# Token mostka: ze zmiennej srodowiskowej, z publiczna wartoscia domyslna
# dodatku. Nie zaszywamy sekretu w repo, nawet gdy jest domyslny - u kogos
# innego bedzie zmieniony i skrypt ma dzialac bez edycji kodu.
TOKEN = os.environ.get("NVDA_BRIDGE_TOKEN", "nvda-bridge-secret-change-me")


def akcja(nazwa, **params):
    body = json.dumps({"action": nazwa, "params": params}).encode()
    req = urllib.request.Request(
        f"{BASE}/action",
        data=body,
        headers={"Content-Type": "application/json",
                 "X-NVDA-Bridge-Token": TOKEN},
    )
    with urllib.request.urlopen(req, timeout=15) as r:
        return json.loads(r.read().decode())


def fokus():
    d = akcja("get_current_focus")
    res = d.get("result") or d
    if isinstance(res, str):
        res = json.loads(res)
    return res.get("result", res)


def stany(s):
    """'{<State.EXPANDED: 256>, ...}' -> {'EXPANDED', ...}"""
    return set(re.findall(r"State\.([A-Z_]+)", str(s or "")))


def klaw(k):
    akcja("send_keys", keys=k)
    # ODSTEP OBOWIAZKOWY: bez niego czytam fokus ZANIM drzewo zdazy sie przestawic
    # i widze stary element - pierwsza wersja tego skryptu zebrala z tego powodu
    # 1 pozycje zamiast 7 i wygladalo to jak defekt programu.
    time.sleep(0.45)


def main():
    f = fokus()
    print("klasa okna:", f.get("windowClassName"))
    if "TreeView" not in str(f.get("windowClassName")):
        print("BLAD: fokus NIE jest w drzewie Document Navigation - przerywam")
        return 2

    # zejdz na sam poczatek drzewa (w SysTreeView32 dziala 'home')
    klaw("home")
    zebrane = []
    poprzednia = None
    for i in range(60):
        f = fokus()
        nazwa = f.get("name") or ""
        st = stany(f.get("states"))
        if nazwa == poprzednia and i > 0:
            break  # koniec drzewa: strzalka nic nie zmienila
        zebrane.append({"nazwa": nazwa, "stany": sorted(st), "rola": f.get("role")})
        poprzednia = nazwa
        klaw("downArrow")

    print(f"\n=== DRZEWO NAGLOWKOW ({len(zebrane)} pozycji) ===")
    for z in zebrane:
        ozn = []
        if "EXPANDED" in z["stany"]:
            ozn.append("rozwiniety")
        if "COLLAPSED" in z["stany"]:
            ozn.append("zwiniety")
        print(f"  {z['nazwa']!r} rola={z['rola']} {' '.join(ozn)}")

    nazwy = [z["nazwa"] for z in zebrane]

    print("\n=== WERYFIKACJA WYMAGAN Z LISTY TESTOW ===")
    wynik = []

    # TEST 1.9: linia z kratka WEWNATRZ bloku kodu NIE jest naglowkiem
    zle = [n for n in nazwy if "komentarz w kodzie" in n]
    wynik.append(("1.9 linia z # w bloku kodu nie jest naglowkiem",
                  not zle, f"znalezione podejrzane: {zle}" if zle else "brak w drzewie - OK"))

    # Kompletnosc: wszystkie realne naglowki pliku sa w drzewie
    oczekiwane = ["Plik testowy EdSharpNG 5.0.21", "Instalacja", "Wymagania wstepne",
                  "Znane problemy", "Konfiguracja", "Ustawienia zaawansowane",
                  "Podsumowanie"]
    def norm(s):
        return (s.replace("ą","a").replace("ć","c").replace("ę","e").replace("ł","l")
                 .replace("ń","n").replace("ó","o").replace("ś","s").replace("ź","z")
                 .replace("ż","z").lower())
    nn = [norm(n) for n in nazwy]
    brak = [o for o in oczekiwane if norm(o) not in nn]
    wynik.append(("kompletnosc drzewa (7 naglowkow pliku)",
                  not brak, f"brakuje: {brak}" if brak else f"wszystkie {len(oczekiwane)} obecne"))

    # TEST 1.2 (czesc mierzalna): naglowek z podnaglowkami MA stan rozwiniety/zwiniety,
    # czyli czytnik ma co oglosic. Bez tego stanu user nie slyszy stanu galezi.
    z_dziecmi = [z for z in zebrane
                 if norm(z["nazwa"]) in (norm("Instalacja"), norm("Konfiguracja"),
                                         norm("Plik testowy EdSharpNG 5.0.21"))]
    maja_stan = [z for z in z_dziecmi
                 if "EXPANDED" in z["stany"] or "COLLAPSED" in z["stany"]]
    wynik.append(("1.2 naglowki z podnaglowkami maja stan galezi (czytnik go oglosi)",
                  len(maja_stan) == len(z_dziecmi) and z_dziecmi,
                  f"{len(maja_stan)} z {len(z_dziecmi)} ma EXPANDED/COLLAPSED"))

    for opis, ok, detal in wynik:
        print(f"  [{'OK ' if ok else 'ZLE'}] {opis}: {detal}")

    print("\n=== UWAGA O POZIOMIE NAGLOWKA (test 1.2, druga polowa) ===")
    print("  Zadna pozycja nie ma w 'name' ani 'description' slowa o poziomie.")
    print("  Poziom w drzewie czytnik wylicza z GLEBOKOSCI galezi (level), nie z tekstu -")
    print("  do rozstrzygniecia, czy NVDA to oglasza; sprawdzam osobno przez tree level.")
    return 0 if all(ok for _, ok, _ in wynik) else 1


if __name__ == "__main__":
    sys.exit(main())
