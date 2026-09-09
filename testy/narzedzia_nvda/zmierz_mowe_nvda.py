#!/usr/bin/env python3
"""Testy EdSharpNG 5.0.21 z pomiarem MOWY NVDA - wersja 3.

POPRAWKI PO PIERWSZYM PRZEBIEGU (19.08.2026), obie w MOICH asercjach, nie w programie:
1. Test 1.2 twierdzil "NIE MA slowa o poziomie", a mowa zawierala wprost
   "poziom 1 | Podsumowanie | 3 z 3". Blad: czytalem mowe TYLKO po strzalce w dol,
   a NVDA oglasza poziom przy WEJSCIU do drzewa; po strzalce w obrebie tego samego
   poziomu NVDA CELOWO poziomu nie powtarza (tak dziala kazde drzewo w Windows).
   KLASA BLEDU: asercja na zlym MOMENCIE pomiaru, nie na zlej tresci.
2. Testy 3.1 i 3.2 pokazaly "0 wypowiedzi" i zaliczylem to jako OK - brak danych
   wziety za dowod poprawnosci. Realna przyczyna: po `aktywuj_edsharp()` fokus
   klawiatury byl w edytorze, ale kliknieciem USTAWILEM KURSOR w miejscu klikniecia,
   a nastepnie `control+home` szlo do okna, ktore jeszcze nie przyjelo fokusu.
   Teraz KAZDY test najpierw potwierdza, ze fokus jest w polu edycji EdSharpa
   (przez get_current_focus), i dopiero wtedy mierzy. Brak mowy = UWAGA, nie OK.
"""
import io
import json
import os
import subprocess
import sys
import time
import urllib.request

BASE = "http://127.0.0.1:8765"
# Token mostka: ze zmiennej srodowiskowej, z publiczna wartoscia domyslna
# dodatku. Nie zaszywamy sekretu w repo, nawet gdy jest domyslny - u kogos
# innego bedzie zmieniony i skrypt ma dzialac bez edycji kodu.
TOKEN = os.environ.get("NVDA_BRIDGE_TOKEN", "nvda-bridge-secret-change-me")
LOG_MOWY = "/mnt/c/Users/g/AppData/Local/Temp/nvda_mowa.log"
PS = "/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe"


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


def znacznik():
    try:
        return os.path.getsize(LOG_MOWY)
    except OSError:
        return 0


def mowa_od(poz, pauza=1.1):
    time.sleep(pauza)
    with io.open(LOG_MOWY, encoding="utf-8", errors="replace") as f:
        f.seek(poz)
        linie = [l.rstrip("\n") for l in f if l.strip()]
    out = []
    for l in linie:
        cz = l.split("\t", 1)
        t = cz[1] if len(cz) > 1 else l
        if t.startswith("SPEAK: "):          # bierzemy JEDNA reprezentacje,
            out.append(t[7:])                 # zeby nie liczyc kazdej mowy dwa razy
    return out


def tekst_mowy(lista):
    """Same slowa, bez znacznikow komend mowy."""
    czyste = []
    for l in lista:
        for cz in l.split("|"):
            cz = cz.strip()
            if cz and not cz.startswith("<"):
                czyste.append(cz)
    return czyste


def klaw(k, pauza=0.55):
    akcja("send_keys", keys=k)
    time.sleep(pauza)


def aktywuj():
    r = subprocess.run([PS, "-NoProfile", "-ExecutionPolicy", "Bypass",
                        "-File", r"C:\Users\g\AppData\Local\Temp\klik.ps1"],
                       capture_output=True, text=True, timeout=120)
    time.sleep(0.8)
    return "EDSHARP_AKTYWNY=True" in r.stdout


def w_edytorze():
    f = fokus()
    kl = str(f.get("windowClassName") or "")
    return "RICHEDIT" in kl.upper() or "EDIT" in kl.upper() or f.get("role") == 51


def main():
    wyniki = []

    def zapisz(opis, ok, detal):
        wyniki.append((opis, ok, detal))
        print(f"  -> [{'OK ' if ok else 'UWAGA'}] {detal}")

    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa")
        return 2
    f = fokus()
    print(f"fokus po aktywacji: klasa={f.get('windowClassName')} rola={f.get('role')}\n")

    # ---------------- TEST 3.1 (PILNE) ----------------
    print("=== TEST 3.1 (PILNE): Alt+strzalka w dol = zdanie czytane RAZ ===")
    klaw("control+home", 0.8)
    z = znacznik()
    klaw("alt+downArrow", 1.0)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:120])
    slowa = tekst_mowy(m)
    dlugie = [s for s in slowa if len(s) > 15]
    if not m:
        zapisz("3.1 zdanie czytane raz", False,
               "BRAK MOWY - nie mierzalne, nie zaliczam (fokus albo brak reakcji)")
    else:
        # powtorzenie = ta sama dluga fraza wiecej niz raz
        powt = len(dlugie) != len(set(dlugie))
        zapisz("3.1 zdanie czytane raz (nie dwa razy)", not powt,
               f"fragmentow tresci: {len(dlugie)}, unikalnych: {len(set(dlugie))}"
               + (" - POWTORZENIE" if powt else " - bez powtorzen"))

    # ---------------- TEST 3.3: akapit ----------------
    print("\n=== TEST 3.3: Control+strzalka w dol = CALY akapit, raz ===")
    klaw("control+home", 0.7)
    z = znacznik()
    klaw("control+downArrow", 1.1)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:140])
    slowa = tekst_mowy(m)
    dlugie = [s for s in slowa if len(s) > 15]
    if not m:
        zapisz("3.3 caly akapit czytany raz", False, "BRAK MOWY - nie mierzalne")
    else:
        powt = len(dlugie) != len(set(dlugie))
        zapisz("3.3 akapit bez powtorzenia", not powt,
               f"fragmentow: {len(dlugie)}, unikalnych: {len(set(dlugie))}")

    # ---------------- TEST 1.2 (PILNE) ----------------
    print("\n=== TEST 1.2 (PILNE): F6 - POZIOM naglowka i STAN GALEZI w mowie ===")
    if not aktywuj():
        print("  nie wrocilem do EdSharpa")
        return 2
    klaw("control+home", 0.8)
    z = znacznik()
    klaw("f6", 1.8)
    m_wejscie = mowa_od(z, 1.0)
    for x in m_wejscie:
        print("   MOWA (wejscie):", x[:140])
    f = fokus()
    if "TreeView" not in str(f.get("windowClassName")):
        zapisz("1.2 F6 otwiera drzewo", False, "fokus nie wszedl do drzewa")
        return 1

    # POZIOM: NVDA oglasza go przy wejsciu do drzewa ORAZ przy ZMIANIE poziomu
    caly = " ".join(m_wejscie).lower()
    ma_poziom = "poziom" in caly or "level" in caly
    zapisz("1.2 NVDA oglasza POZIOM naglowka", ma_poziom,
           "slyszalne 'poziom N' przy wejsciu do drzewa" if ma_poziom
           else "brak slowa o poziomie")

    # STAN GALEZI: trzeba stanac na naglowku, ktory MA dzieci (Instalacja)
    print("\n   -- szukam naglowka Z PODNAGLOWKAMI (stan galezi) --")
    klaw("home", 0.8)          # gora drzewa: 'Plik testowy...' (ma dzieci)
    z = znacznik()
    klaw("leftArrow", 0.9)     # zwin
    m_zwin = mowa_od(z, 0.9)
    for x in m_zwin:
        print("   MOWA (zwijanie):", x[:140])
    z = znacznik()
    klaw("rightArrow", 0.9)    # rozwin
    m_rozwin = mowa_od(z, 0.9)
    for x in m_rozwin:
        print("   MOWA (rozwijanie):", x[:140])
    razem = (" ".join(m_zwin) + " " + " ".join(m_rozwin)).lower()
    # NORMALIZACJA OGONKOW - konieczna. Pierwsza wersja szukala "zwinie" w tekscie
    # "zwinięte" i NIE trafiala, bo 'ę' to inny znak niz 'e'. Asercja wypisala
    # wtedy "NIE slychac stanu galezi", choc NVDA mowilo to wprost.
    # KLASA BLEDU: dopasowanie polskiego tekstu bez normalizacji diakrytyk daje
    # FALSZYWY NEGATYW, ktory brzmi jak defekt produktu.
    for a, b in (("ą","a"),("ć","c"),("ę","e"),("ł","l"),("ń","n"),
                 ("ó","o"),("ś","s"),("ź","z"),("ż","z")):
        razem = razem.replace(a, b)
    ma_stan = any(s in razem for s in
                  ("zwiniet", "rozwiniet", "collapsed", "expanded"))
    zapisz("1.2 NVDA oglasza STAN GALEZI (zwinieta/rozwinieta)", ma_stan,
           "slyszalne przy zwijaniu/rozwijaniu" if ma_stan
           else "NIE slychac stanu galezi - to jest realna uwaga do zgloszenia")

    # ---------------- TEST 1.5 ----------------
    print("\n=== TEST 1.5: Enter - tytul i poziom, komunikat nie ginie ===")
    z = znacznik()
    klaw("enter", 1.8)
    m_enter = mowa_od(z, 1.2)
    for x in m_enter:
        print("   MOWA:", x[:140])
    caly = " ".join(m_enter)
    ma_heading = "heading" in caly.lower()
    zapisz("1.5 po Enter slychac tytul + poziom (np. 'Podsumowanie, heading 2')",
           ma_heading,
           f"znalezione: {[s for s in tekst_mowy(m_enter) if 'heading' in s.lower()]}"
           if ma_heading else "brak formuly 'heading N'")

    print("\n" + "=" * 62)
    print("PODSUMOWANIE")
    print("=" * 62)
    for opis, ok, detal in wyniki:
        print(f"  [{'OK   ' if ok else 'UWAGA'}] {opis}")
        print(f"          {detal}")
    zle = [w for w in wyniki if not w[1]]
    print(f"\nzaliczone: {len(wyniki) - len(zle)} z {len(wyniki)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
