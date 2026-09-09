#!/usr/bin/env python3
"""Pomiar na ZYWYM NVDA: kursor na PUNKTORZE pozycji listy w podgladzie.

ZGLOSZENIE (Kasperczak 27.08.2026 21:33): "Esc podglad, NVDA nawiguje na liscie
i element listy, ale kursor jest na punktorze, a wewnetrzny link daje sie
uaktywnic dopiero, kiedy przesuniemy sie o 2 znaki".

CO MIERZYMY (nie kod, tylko realne zachowanie):
1. Po utworzeniu spisu (Alt+Shift+T) i wejsciu w podglad (Escape), Enter na
   pozycji spisu MA od razu skoczyc do rozdzialu - bez przesuwania kursora.
2. To samo dla nawigacji po pozycjach listy klawiszem I w podgladzie.
3. KONTROLA NEGATYWNA: na pozycji listy BEZ odsylacza Enter ma powiedziec
   swoje ("no link at cursor"), a NIE skoczyc gdziekolwiek.

DLACZEGO POMIAR NA ZYWO: sam kod moze wygladac poprawnie, a liczy sie to,
gdzie realnie staje kursor czytnika. Harness logiki jest osobno
(testy/pomiar_kursora_na_punktorze.cs).

PULAPKI odziedziczone z narzedzia zmierz_mowe_spisu_tresci.py:
- SetForegroundWindow NIE daje fokusu klawiatury: aktywacja przez klik.ps1.
- Pauza po kazdym klawiszu obowiazkowa.
- BRAK MOWY to UWAGA, nie OK.
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
LOG_MOWY = "/mnt/c/Users/g/AppData/Local/Temp/nvda_mowa.log"
PS = "/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe"
KLIK = r"C:\Users\g\AppData\Local\Temp\klik.ps1"
EXE_WIN = r"D:\projekty\edsharp-pr\EdSharpNG.exe"
PROBKA = r"D:\projekty\edsharp-pr\testy\spis_probka_punktor.md"


def akcja(nazwa, **params):
    body = json.dumps({"action": nazwa, "params": params}).encode()
    req = urllib.request.Request(
        f"{BASE}/action", data=body,
        headers={"Content-Type": "application/json",
                 "X-NVDA-Bridge-Token": TOKEN})
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


def mowa_od(poz, pauza=1.2):
    time.sleep(pauza)
    with io.open(LOG_MOWY, encoding="utf-8", errors="replace") as f:
        f.seek(poz)
        linie = [l.rstrip("\n") for l in f if l.strip()]
    out = []
    for l in linie:
        cz = l.split("\t", 1)
        t = cz[1] if len(cz) > 1 else l
        if t.startswith("SPEAK: "):
            out.append(t[7:])
    return out


def norm(s):
    s = s.lower()
    for a, b in (("\u0105", "a"), ("\u0107", "c"), ("\u0119", "e"),
                 ("\u0142", "l"), ("\u0144", "n"), ("\u00f3", "o"),
                 ("\u015b", "s"), ("\u017a", "z"), ("\u017c", "z")):
        s = s.replace(a, b)
    return s


def klaw(k, pauza=0.6):
    akcja("send_keys", keys=k)
    time.sleep(pauza)


def linia_biezaca():
    d = akcja("read_current_line")
    res = d.get("result") or d
    if isinstance(res, str):
        try:
            res = json.loads(res)
        except ValueError:
            return res
    if isinstance(res, dict):
        res = res.get("result", res)
    if isinstance(res, dict):
        for k in ("line", "text", "content"):
            if k in res:
                return str(res[k])
    return str(res)


def aktywuj(proby=4):
    for i in range(proby):
        r = subprocess.run([PS, "-NoProfile", "-ExecutionPolicy", "Bypass",
                            "-File", KLIK], capture_output=True, text=True,
                           timeout=120)
        time.sleep(0.9)
        if "EDSHARP_AKTYWNY=True" in r.stdout:
            return True
        print(f"   (aktywacja: proba {i + 1} nieudana, ponawiam)")
        time.sleep(1.6)
    return False


def uruchom_edsharp(plik_win):
    subprocess.run([PS, "-NoProfile", "-Command",
                    f"Start-Process -FilePath '{EXE_WIN}' -ArgumentList '{plik_win}'"],
                   capture_output=True, text=True, timeout=60)
    time.sleep(6)


def zamknij_edsharp():
    subprocess.run([PS, "-NoProfile", "-Command",
                    "Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force"],
                   capture_output=True, text=True, timeout=60)
    time.sleep(2.5)


def w_edytorze():
    f = fokus()
    kl = str(f.get("windowClassName") or "").upper()
    return "RICHEDIT" in kl or "EDIT" in kl or f.get("role") == 51


def main():
    wyniki = []

    def zapisz(opis, ok, detal):
        wyniki.append((opis, ok, detal))
        print(f"  -> [{'OK   ' if ok else 'UWAGA'}] {detal}")

    print("=== przygotowanie: dokument probny ze spisem i lista bez linkow ===")
    zamknij_edsharp()
    uruchom_edsharp(PROBKA)
    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa")
        return 2
    if not w_edytorze():
        print("BLAD: fokus nie jest w polu edycji")
        return 2

    print("\n=== krok 1: tworzymy spis tresci (Alt+Shift+T) ===")
    klaw("control+home", 0.8)
    klaw("alt+shift+t", 1.6)
    print(f"   pierwszy wiersz: {linia_biezaca()!r}")

    print("\n=== krok 2: wchodzimy w podglad (Escape) ===")
    klaw("control+home", 0.8)
    klaw("escape", 1.6)
    f = fokus()
    print(f"   klasa okna po Escape: {f.get('windowClassName')!r}")

    print("\n=== TEST A: nawigacja po pozycjach listy (I) i Enter od razu ===")
    # I = nastepna pozycja listy w podgladzie.  Spis tresci jest lista, wiec
    # to jest dokladnie droga, ktora Kasperczak opisal.
    klaw("i", 1.2)
    linia_pozycji = linia_biezaca()
    print(f"   wiersz pozycji spisu: {linia_pozycji!r}")
    z = znacznik()
    klaw("enter", 1.6)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    po_enter = linia_biezaca()
    print(f"   wiersz PO Enter: {po_enter!r}")
    if not m:
        zapisz("A Enter na pozycji spisu", False, "BRAK MOWY - nie mierzalne")
    else:
        skoczyl = re.search(r"heading\s*\d", caly) is not None
        zapisz("A Enter na pozycji spisu skacze do rozdzialu BEZ przesuwania",
               skoczyl, f"fragment mowy: {caly[:140]!r}")
        zapisz("A kontrola waznosci: NIE slychac 'no link at cursor'",
               "no link" not in caly,
               "brak odmowy - odsylacz zostal znaleziony pod kursorem"
               if "no link" not in caly else
               "slychac odmowe - kursor nadal nie stoi na odsylaczu")

    print("\n=== TEST B: KONTROLA NEGATYWNA - pozycja listy BEZ odsylacza ===")
    # Wracamy do podgladu i szukamy pozycji zwyklej listy (bez linku).
    klaw("control+home", 0.9)
    znaleziona = False
    for _ in range(12):
        klaw("i", 0.8)
        l = norm(linia_biezaca())
        if "pozycja bez odsylacza" in l:
            znaleziona = True
            print(f"   stoje na: {linia_biezaca()!r}")
            break
    if not znaleziona:
        zapisz("B kontrola negatywna", False,
               "nie znalazlem pozycji listy bez odsylacza - pomiar nierozstrzygajacy")
    else:
        przed = linia_biezaca()
        z = znacznik()
        klaw("enter", 1.6)
        m = mowa_od(z)
        for x in m:
            print("   MOWA:", x[:160])
        caly = norm(" ".join(m))
        po = linia_biezaca()
        print(f"   wiersz PO Enter: {po!r}")
        zapisz("B na pozycji bez odsylacza Enter mowi swoje, nie skacze",
               "no link" in caly,
               f"'no link at cursor' w mowie: {'no link' in caly}; fragment: {caly[:120]!r}")
        zapisz("B kontrola waznosci: kursor NIE opuscil wiersza",
               norm(przed)[:30] == norm(po)[:30],
               f"przed={przed[:40]!r} po={po[:40]!r}")

    print("\n" + "=" * 62)
    print("PODSUMOWANIE POMIARU: kursor na punktorze")
    print("=" * 62)
    for opis, ok, detal in wyniki:
        print(f"  [{'OK   ' if ok else 'UWAGA'}] {opis}")
        print(f"          {detal}")
    zle = [w for w in wyniki if not w[1]]
    print(f"\nzaliczone: {len(wyniki) - len(zle)} z {len(wyniki)}")
    zamknij_edsharp()
    return 0 if not zle else 1


if __name__ == "__main__":
    sys.exit(main())
