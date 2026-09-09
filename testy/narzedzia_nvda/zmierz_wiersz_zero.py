#!/usr/bin/env python3
"""Pomiar na ZYWYM NVDA: czy przed trescia komorki slychac numer wiersza LICZONY
OD ZERA - i czy naglowek wiersza dubluje wspolrzedna, ktora komorka podaje sama.

ZGLOSZENIE (Kasperczak 28.08.2026 23:53, wiadomosc bez pozycji w kolejce):
  "Przy okazji tabele, czy on dalej to liczy od wiersza zero? Bo to tak chyba
   nie powinno byc, ze wiersz zero."

CO MIERZYMY (mowa czytnika, nie kod):
1. Przy ruchu po komorkach NIE slychac "wiersz 0" ani "row 0" w ZADNEJ postaci.
   To jest jego zgloszenie doslownie.
2. Naglowek wiersza nie dubluje numeru: liczba wystapien slowa "row"/"wiersz"
   w jednej wypowiedzi ma byc 1, nie 2. Dublowanie bylo drugim objawem
   tego samego defektu.
3. Wiersze tresci sa numerowane OD JEDYNKI (pierwszy wiersz pod naglowkiem
   mowi "row 1"), a wiersz naglowkowy mowi slowem, nie numerem.

KONTROLE POZYTYWNE POMIARU (bez nich "nie slyszalem zera" nie znaczy nic,
bo rownie dobrze moze znaczyc "czytnik milczal albo log sie nie pisze"):
P1. czytnik mowi cokolwiek przy otwarciu okna kreatora,
P2. czytnik mowi TRESC komorki (slychac wpisany tekst) - czyli mierzymy
    faktycznie komorki, a nie samo okno,
P3. czytnik mowi slowo "column" - czyli wspolrzedne sa oglaszane i brak slowa
    o wierszu bylby realnym brakiem, a nie cisza calego pomiaru.

Pulapki odziedziczone z README w tym katalogu: aktywacja przez klik.ps1,
pauza po kazdym klawiszu, brak mowy = UWAGA a nie OK.
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

# "wiersz 0" w kazdej postaci, jaka moze wyjsc z czytnika: po angielsku i po
# polsku, ze spacja i bez.  Zamiast szukac jednego napisu, szukamy WZORCA -
# inaczej pomiar przechodzi tylko dlatego, ze zgadlem jezyk syntezatora.
RE_ZERO = re.compile(r"\b(?:row|wiersz)\s*0\b", re.IGNORECASE)
RE_WIERSZ = re.compile(r"\b(?:row|wiersz)\b", re.IGNORECASE)


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


def mowa_od(poz, pauza=1.3):
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
        print(f"  -> [{'OK   ' if ok else 'UWAGA'}] {detal}")

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
        print("   MOWA:", x[:200])
    if not m_otw:
        zapisz("P1 KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna", False,
               "BRAK MOWY - pomiar NIEROZSTRZYGAJACY, nie wyciagaj wnioskow")
        zamknij_edsharp()
        return 2
    zapisz("P1 KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna", True,
           f"pierwsza wypowiedz: {m_otw[0][:90]!r}")

    # Zbieramy tabele dwa na dwa, zeby byl i wiersz naglowka, i wiersz tresci.
    print("\n=== krok 2: wpisujemy naglowki i dane ===")
    pisz("Imie")
    klaw("rightArrow", 1.0)
    pisz("Wiek")
    klaw("downArrow", 1.0)
    pisz("30")
    klaw("leftArrow", 1.0)
    pisz("Ala")

    zebrane = []

    print("\n=== TEST 1: ruch w GORE na wiersz naglowka ===")
    z = znacznik()
    klaw("upArrow", 1.6)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:200])
    zebrane.extend(m)
    caly = " ".join(m)
    zapisz("1 na wierszu naglowka NIE slychac 'wiersz 0' / 'row 0'",
           bool(m) and not RE_ZERO.search(caly),
           f"mowa: {caly[:200]!r}")
    if m:
        ile = max(len(RE_WIERSZ.findall(x)) for x in m)
        zapisz("1b numer wiersza nie jest DUBLOWANY w jednej wypowiedzi",
               ile <= 1, f"najwiecej wystapien slowa wiersz/row w jednej wypowiedzi: {ile}")

    print("\n=== TEST 2: ruch w DOL na pierwszy wiersz tresci ===")
    z = znacznik()
    klaw("downArrow", 1.6)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:200])
    zebrane.extend(m)
    caly = " ".join(m)
    low = caly.lower()
    zapisz("2 na wierszu tresci NIE slychac 'wiersz 0' / 'row 0'",
           bool(m) and not RE_ZERO.search(caly),
           f"mowa: {caly[:200]!r}")
    zapisz("2b pierwszy wiersz tresci jest numerowany OD JEDYNKI",
           bool(re.search(r"\b(?:row|wiersz)\s*1\b", low)),
           f"mowa: {caly[:200]!r}")
    zapisz("P2 KONTROLA POZYTYWNA: czytnik mowi TRESC komorki",
           "ala" in low, f"szukam 'Ala' w: {caly[:200]!r}")

    print("\n=== TEST 3: ruch w PRAWO w tym samym wierszu ===")
    z = znacznik()
    klaw("rightArrow", 1.6)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:200])
    zebrane.extend(m)
    caly = " ".join(m)
    low = caly.lower()
    zapisz("3 przy ruchu poziomym NIE slychac 'wiersz 0' / 'row 0'",
           bool(m) and not RE_ZERO.search(caly),
           f"mowa: {caly[:200]!r}")
    zapisz("P3 KONTROLA POZYTYWNA: czytnik oglasza kolumne",
           "column" in low or "kolumna" in low,
           f"mowa: {caly[:200]!r}")
    if m:
        ile = max(len(RE_WIERSZ.findall(x)) for x in m)
        zapisz("3b numer wiersza nie jest DUBLOWANY przy ruchu poziomym",
               ile <= 1, f"najwiecej wystapien slowa wiersz/row: {ile}")

    print("\n=== TEST 4: caly przebieg razem - ani jedno 'wiersz 0' ===")
    caly_przebieg = " ".join(zebrane)
    trafienia = RE_ZERO.findall(caly_przebieg)
    zapisz("4 w CALYM przebiegu po siatce zero wystapien 'wiersz 0'",
           not trafienia, f"trafienia: {trafienia!r}")

    print("\n=== sprzatanie: Escape (bez wstawiania) ===")
    klaw("escape", 1.2)
    klaw("y", 1.0)

    print("\n" + "=" * 62)
    print("PODSUMOWANIE: numeracja wierszy w siatce kreatora tabeli")
    print("=" * 62)
    for opis, ok, detal in wyniki:
        print(f"  [{'OK   ' if ok else 'UWAGA'}] {opis}")
        print(f"          {detal}")
    zle = [w for w in wyniki if not w[1]]
    print(f"\nzaliczone: {len(wyniki) - len(zle)} z {len(wyniki)}")
    zamknij_edsharp()
    return 0 if not zle else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:
        print(f"AWARIA POMIARU: {type(e).__name__}: {e}")
        sys.exit(2)
