#!/usr/bin/env python3
"""Pomiar na ZYWYM NVDA: ILE i CO czytnik mowi na JEDEN ruch po komorce siatki.

ZGLOSZENIE (Kasperczak 29.08.2026, wiadomosci 353 i 363 - jego wlasny zapis
tego, co slyszy):

    gh, column 1, row 2
    gh
    zaznaczony
    gh, row 2, column 1
    gh
    zaznaczony
    blank, row 3, column 1
    (zerowy)
    zaznaczony

Czyli JEDEN ruch strzalka daje TRZY wypowiedzi: nasza nazwa (poprawna), potem
tresc komorki DRUGI RAZ, potem slowo "zaznaczony".  Przy pustej komorce zamiast
powtorki tresci leci "(zerowy)".

CO MIERZYMY:
1. LICZBA wypowiedzi na jeden ruch (ma byc JEDNA).
2. Czy tresc komorki nie wraca DRUGI RAZ jako osobna wypowiedz.
3. Czy nie leci slowo "zaznaczony" / "selected".
4. Czy na pustej komorce nie leci "zerowy" / "0".
5. Czy tresc jest PIERWSZA w pionie (jego zgloszenie 357).

KONTROLA POZYTYWNA: czytnik musi cokolwiek powiedziec przy ruchu - bez tego
"nie slychac dublowania" jest nie do odroznienia od gluchej sondy.
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

RE_ZAZNACZ = re.compile(r"\b(?:zaznaczon\w*|selected)\b", re.IGNORECASE)
RE_ZERO = re.compile(r"\b(?:zerow\w*|zero)\b", re.IGNORECASE)


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


def mowa_od(poz, pauza=1.6):
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


def klaw(k, pauza=0.8):
    akcja("send_keys", keys=k)
    time.sleep(pauza)


def pisz(tekst, pauza=0.9):
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
    klaw("control+shift+t", 2.4)
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

    print("\n=== krok 2: tabela dwa na dwa ===")
    pisz("Imie")
    klaw("rightArrow", 1.0)
    pisz("Wiek")
    klaw("downArrow", 1.0)
    pisz("30")
    klaw("leftArrow", 1.0)
    pisz("Ala")

    # WAZNE DLA POMIARU: ruch zaraz PO PISANIU zawsze daje dwie wypowiedzi -
    # najpierw komorka, ktora wlasnie zatwierdzilismy, potem docelowa.  To NIE
    # jest zgloszona usterka (to dwie ROZNE komorki, a potwierdzenie zapisu
    # niesie informacje) i zmierzone osobno w
    # zmierz_glos_przy_dokladaniu_wiersza.py: samo dokladanie wiersza daje
    # JEDEN glos.  Dlatego przed testami robimy jeden ruch "na sucho", zeby
    # mierzyc czysty ruch po komorkach, a nie skutek konca edycji.
    klaw("upArrow", 1.6)
    klaw("downArrow", 1.6)

    # Ruch W DOL doklada wiersz (pusty) - to daje przypadek pustej komorki.
    def jeden_ruch(nazwa, klawisz, oczekiwana_tresc):
        print(f"\n=== RUCH {nazwa}: {klawisz} ===")
        z = znacznik()
        klaw(klawisz, 1.8)
        m = mowa_od(z)
        for i, x in enumerate(m, 1):
            print(f"   MOWA {i}: {x[:170]}")
        caly = " ".join(m)
        low = caly.lower()
        print(f"   liczba wypowiedzi: {len(m)}")
        zapisz(f"{nazwa} czytnik cokolwiek mowi (kontrola)", bool(m),
               f"{len(m)} wypowiedzi")
        zapisz(f"{nazwa} JEDNA wypowiedz na jeden ruch", len(m) == 1,
               f"wypowiedzi={len(m)}: {[x[:60] for x in m]}")
        zapisz(f"{nazwa} slowa 'zaznaczony/selected' NIE MA",
               not RE_ZAZNACZ.search(caly), f"mowa: {caly[:200]!r}")
        if oczekiwana_tresc == "blank":
            zapisz(f"{nazwa} pusta komorka NIE mowi 'zerowy'",
                   not RE_ZERO.search(caly), f"mowa: {caly[:200]!r}")
        else:
            ile = low.count(oczekiwana_tresc.lower())
            zapisz(f"{nazwa} tresc komorki mowiona DOKLADNIE RAZ",
                   ile == 1, f"'{oczekiwana_tresc}' wystapilo {ile} razy w: {caly[:200]!r}")
        return m

    # W dol: z wiersza 1 (Ala) na wiersz 2 - siatka doklada pusty wiersz.
    jeden_ruch("A w dol na PUSTA komorke", "downArrow", "blank")
    # W gore: wraca na "Ala" - ruch PIONOWY, tresc ma byc pierwsza.
    m = jeden_ruch("B w gore na tresc", "upArrow", "Ala")
    if m:
        low = " ".join(m).lower()
        i_tresc = low.find("ala")
        i_row = low.find("row")
        zapisz("B w PIONIE tresc jest PRZED slowem 'row' (jego zgloszenie 357)",
               i_tresc >= 0 and i_row >= 0 and i_tresc < i_row,
               f"tresc={i_tresc} row={i_row} w: {low[:200]!r}")
    # W prawo: ruch poziomy na "30".
    jeden_ruch("C w prawo na tresc", "rightArrow", "30")

    print("\n=== sprzatanie ===")
    klaw("escape", 1.4)
    klaw("y", 1.0)
    zamknij_edsharp()

    print("\n" + "=" * 62)
    print("PODSUMOWANIE: ile glosow na jeden ruch po komorce")
    print("=" * 62)
    for opis, ok, detal in wyniki:
        print(f"  [{'OK   ' if ok else 'UWAGA'}] {opis}")
        print(f"          {detal}")
    zle = [w for w in wyniki if not w[1]]
    print(f"\nzaliczone: {len(wyniki) - len(zle)} z {len(wyniki)}")
    return 0 if not zle else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:
        print(f"AWARIA POMIARU: {type(e).__name__}: {e}")
        sys.exit(2)
