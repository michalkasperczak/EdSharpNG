#!/usr/bin/env python3
"""Pomiar na ZYWYM NVDA: co czytnik mowi po nacisnieciu F2 w siatce tabeli.

ZGLOSZENIE (Kasperczak 29.08.2026 01:59, wiadomosc 393, doslownie):

    "W trybie tabel jak nacisne F2, to on mi wystarczy jak powie pole edycyjne
     i ewentualnie wspolrzedne komorki wiersza. Nie musi mowic jakiegos edit,
     edytowano albo cos takiego."

CO MIERZYMY po nacisnieciu F2 na komorce z trescia:
1. KONTROLA POZYTYWNA: czytnik w ogole cokolwiek mowi (bez tego "nie slychac
   edytowano" jest nie do odroznienia od gluchej sondy).
2. Czy leci slowo "edytowano" / "edit" / "editing" - to ma ZNIKNAC.
3. Czy slychac "pole edycyjne" / "edit field" - to ma ZOSTAC.
4. ILE wypowiedzi leci na samo F2.

Sonda NIE ZAKLADA wyniku: wypisuje pelna mowe, zeby dalo sie rozstrzygnac, czy
objaw w ogole istnieje, zanim cokolwiek w kodzie ruszymy.
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
PROBKA = r"D:\projekty\edsharp-pr\testy\probka_pusta_tabela.md"

RE_EDYT = re.compile(r"\b(?:edytowan\w*|edycja|editing|edited)\b", re.IGNORECASE)
RE_POLE = re.compile(r"(?:pole edy\w+|pole tekstowe|edit(?:able)? (?:field|text))", re.IGNORECASE)


def akcja(nazwa, **params):
    body = json.dumps({"action": nazwa, "params": params}).encode()
    req = urllib.request.Request(
        f"{BASE}/action", data=body,
        headers={"Content-Type": "application/json",
                 "X-NVDA-Bridge-Token": TOKEN})
    with urllib.request.urlopen(req, timeout=20) as r:
        return json.loads(r.read().decode())


def znacznik():
    try:
        return os.path.getsize(LOG_MOWY)
    except OSError:
        return 0


def mowa_od(poz, pauza=1.8):
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


def klaw(k, pauza=0.9):
    akcja("send_keys", keys=k)
    time.sleep(pauza)


def pisz(tekst, pauza=1.0):
    for ch in tekst:
        akcja("send_keys", keys=ch)
        time.sleep(0.12)
    time.sleep(pauza)


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


def main():
    wyniki = []

    def zapisz(opis, ok, detal):
        wyniki.append((opis, ok, detal))
        print(f"  -> [{'OK   ' if ok else 'UWAGA'}] {opis}\n        {detal}")

    print("=== przygotowanie ===")
    zamknij_edsharp()
    uruchom_edsharp(PROBKA)
    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa - pomiar NIEROZSTRZYGAJACY")
        return 2

    print("\n=== krok 1: kreator tabeli (Control+Shift+T) ===")
    z = znacznik()
    klaw("control+shift+t", 2.6)
    m_otw = mowa_od(z)
    for x in m_otw:
        print("   MOWA:", x[:170])
    if not m_otw:
        zapisz("KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna", False,
               "BRAK MOWY - pomiar NIEROZSTRZYGAJACY")
        zamknij_edsharp()
        return 2
    zapisz("KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna", True,
           f"pierwsza wypowiedz: {m_otw[0][:90]!r}")

    print("\n=== krok 2: wpisuje tresc do komorki i zatwierdzam ===")
    pisz("abc")
    klaw("enter", 1.4)

    print("\n=== krok 3: dokladam wiersz i wracam na komorke z trescia ===")
    # Strzalka w DOL z ostatniego wiersza doklada wiersz (tak dziala kreator),
    # wiec dopiero teraz ruch w gore ma gdzie pojsc.  Bez tego kroku sonda
    # mierzyla BRAK RUCHU, a nie brak mowy.
    klaw("downArrow", 1.4)
    z = znacznik()
    klaw("upArrow", 1.4)
    m_ruch = mowa_od(z)
    for x in m_ruch:
        print("   MOWA(ruch):", x[:170])
    zapisz("KONTROLA POZYTYWNA: ruch po komorce cos mowi", bool(m_ruch),
           f"{len(m_ruch)} wypowiedzi")

    print("\n=== krok 4: F2 - TO JEST MIERZONA RZECZ ===")
    z = znacznik()
    klaw("f2", 2.0)
    m_f2 = mowa_od(z)
    print(f"   liczba wypowiedzi na F2: {len(m_f2)}")
    for x in m_f2:
        print("   MOWA(F2):", repr(x[:170]))

    zapisz("KONTROLA POZYTYWNA: F2 w ogole cos mowi", bool(m_f2),
           f"{len(m_f2)} wypowiedzi"
           + ("" if m_f2 else " - BEZ TEGO CALY POMIAR JEST NIEROZSTRZYGAJACY"))

    if not m_f2:
        zamknij_edsharp()
        print("\nPOMIAR NIEROZSTRZYGAJACY - czytnik milczy na F2")
        return 2

    caly = " | ".join(m_f2)
    trafienia_edyt = RE_EDYT.findall(caly)
    zapisz("F2 NIE mowi 'edytowano' ani 'edit'", not trafienia_edyt,
           f"znalezione: {trafienia_edyt}" if trafienia_edyt
           else "brak slowa o edytowaniu")

    zapisz("F2 mowi o polu edycyjnym (to ma ZOSTAC)",
           bool(RE_POLE.search(caly)),
           f"pelna mowa: {caly[:220]!r}")

    zamknij_edsharp()

    print("\n=== PODSUMOWANIE ===")
    ok = sum(1 for _, o, _ in wyniki if o)
    for opis, o, detal in wyniki:
        print(f"[{'OK   ' if o else 'UWAGA'}] {opis}")
    print(f"\nWYNIK: {ok}/{len(wyniki)}")
    return 0 if ok == len(wyniki) else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:
        import traceback
        traceback.print_exc()
        sys.exit(3)
