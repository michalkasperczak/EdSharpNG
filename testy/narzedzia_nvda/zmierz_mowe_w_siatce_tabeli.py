#!/usr/bin/env python3
"""Pomiar na ZYWYM NVDA: co czytnik mowi przy ruchu po komorkach kreatora tabeli.

ZGLOSZENIE (Kasperczak 28.08.2026): "lepiej zeby on po nawigacji strzalkami po
komorkach tabeli wpierw czytal zawartosc komorki, a potem numer (...) Przy czym
jak strzalka lewo-prawo to wpierw po tresci czyta kolumna numer-wiersz-numer,
a kiedy ide po wierszach to czyta tresc i potem wiersz, numer wiersza kolumna,
numer kolumny."

CO MIERZYMY (mowa, nie kod):
1. Strzalka w PRAWO: pierwsze slowo wypowiedzi to TRESC komorki, potem kolumna,
   potem wiersz.
2. Strzalka w DOL: pierwsze slowo to TRESC, potem wiersz, potem kolumna.
3. Delete na komorce z trescia: tresc znika (kontrola: pusta komorka).
KONTROLA POZYTYWNA POMIARU: czytnik musi w ogole cokolwiek powiedziec przy
ruchu po siatce - bez tego wynik "tresc nie jest pierwsza" nic nie znaczy.

Pulapki odziedziczone z README w tym katalogu: aktywacja przez klik.ps1,
pauza po kazdym klawiszu, brak mowy = UWAGA a nie OK.
"""
import io
import json
import os
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


def klaw(k, pauza=0.7):
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
        print("BLAD: nie aktywowalem EdSharpa")
        return 2

    print("\n=== krok 1: otwieramy kreator tabeli (Control+Shift+T) ===")
    z = znacznik()
    klaw("control+shift+t", 2.2)
    m_otw = mowa_od(z)
    for x in m_otw:
        print("   MOWA:", x[:160])
    f = fokus()
    print(f"   fokus po otwarciu: rola={f.get('role')} klasa={f.get('windowClassName')!r} nazwa={f.get('name')!r}")
    if not m_otw:
        zapisz("KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna", False,
               "BRAK MOWY - pomiar NIEROZSTRZYGAJACY, nie wyciagaj wnioskow")
        for opis, ok, detal in wyniki:
            print(f"  [{'OK   ' if ok else 'UWAGA'}] {opis}\n          {detal}")
        zamknij_edsharp()
        return 2
    zapisz("KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna", True,
           f"pierwsza wypowiedz: {m_otw[0][:90]!r}")

    print("\n=== krok 2: wpisujemy tresc w dwie kolumny i dwa wiersze ===")
    pisz("Imie")
    klaw("rightArrow", 1.0)      # doklada kolumne 2
    pisz("Wiek")
    klaw("downArrow", 1.0)       # doklada wiersz 2
    pisz("30")
    klaw("leftArrow", 1.0)
    pisz("Ala")

    print("\n=== TEST A: strzalka w PRAWO - tresc, potem kolumna, potem wiersz ===")
    # Staje w wierszu 1 kolumna 1 (Ala), potem w prawo na 30 - ruch POZIOMY.
    klaw("leftArrow", 1.0)
    z = znacznik()
    klaw("rightArrow", 1.6)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:200])
    caly = " ".join(m)
    low = caly.lower()
    i_tresc = low.find("30")
    i_kol = low.find("column")
    i_wier = low.find("row")
    print(f"   pozycje w mowie: tresc(30)={i_tresc} column={i_kol} row={i_wier}")
    if not m:
        zapisz("A ruch w prawo cokolwiek mowi", False, "BRAK MOWY")
    else:
        zapisz("A tresc komorki jest PIERWSZA po ruchu w prawo",
               i_tresc >= 0 and (i_kol < 0 or i_tresc < i_kol) and (i_wier < 0 or i_tresc < i_wier),
               f"mowa: {caly[:180]!r}")
        zapisz("A po tresci kolumna PRZED wierszem",
               i_kol >= 0 and i_wier >= 0 and i_kol < i_wier,
               f"column={i_kol} row={i_wier}")

    print("\n=== TEST B: strzalka w GORE - tresc, potem wiersz, potem kolumna ===")
    z = znacznik()
    klaw("upArrow", 1.4)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:200])
    caly = " ".join(m)
    low = caly.lower()
    i_tresc = low.find("wiek")
    i_kol = low.find("column")
    i_wier = low.find("row")
    print(f"   pozycje w mowie: tresc(Wiek)={i_tresc} column={i_kol} row={i_wier}")
    if not m:
        zapisz("B ruch w gore cokolwiek mowi", False, "BRAK MOWY")
    else:
        zapisz("B tresc komorki jest PIERWSZA po ruchu w gore",
               i_tresc >= 0 and (i_kol < 0 or i_tresc < i_kol) and (i_wier < 0 or i_tresc < i_wier),
               f"mowa: {caly[:180]!r}")
        zapisz("B po tresci wiersz PRZED kolumna",
               i_wier >= 0 and i_kol >= 0 and i_wier < i_kol,
               f"row={i_wier} column={i_kol}")

    print("\n=== TEST C: Delete czysci tresc komorki ===")
    z = znacznik()
    klaw("delete", 1.4)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:200])
    caly = " ".join(m)
    zapisz("C Delete cokolwiek mowi (potwierdzenie czyszczenia)",
           bool(m), f"mowa: {caly[:180]!r}")
    z = znacznik()
    klaw("downArrow", 1.2)
    klaw("upArrow", 1.2)
    m2 = mowa_od(z)
    for x in m2:
        print("   MOWA po powrocie:", x[:200])
    caly2 = " ".join(m2).lower()
    zapisz("C po Delete komorka jest PUSTA (nie slychac starej tresci)",
           "wiek" not in caly2, f"mowa po powrocie: {caly2[:180]!r}")

    print("\n=== sprzatanie: Escape (bez wstawiania) ===")
    klaw("escape", 1.2)
    klaw("y", 1.0)

    print("\n" + "=" * 62)
    print("PODSUMOWANIE: mowa czytnika w siatce kreatora tabeli")
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
