#!/usr/bin/env python3
"""Skad bierze sie DRUGI glos przy DOKLADANIU wiersza w siatce.

Pomiar po naprawie 5.0.40 pokazal, ze wszystkie ruchy po istniejacych komorkach
daja JEDNA wypowiedz, ale ruch dokladajacy nowy wiersz daje DWIE: najpierw
komorke, z ktorej wychodzimy, potem docelowa.  To nie jest to samo, co
zgloszona powtorka tresci - tu sa dwie ROZNE komorki - ale nadal jest to jeden
klawisz i dwa glosy.

Warianty (harness /mnt/c/tmp/siatka/h2.exe):
  g - dokladanie dokladnie jak w EdSharpie: Rows.Add + przepisanie naglowkow
      WSZYSTKICH wierszy (podejrzany o przebudowe siatki i drugie ogloszenie)
  h - to samo BEZ dotykania naglowkow wierszy

Jesli h daje jeden glos, a g dwa - winne jest przepisywanie naglowkow.
Jesli oba daja dwa - drugi glos nalezy do samego Rows.Add i zamiana kodu nic
nie da (wtedy nie ruszamy, bo objaw jest wlasnoscia kontrolki).
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
EXE = r"C:\tmp\siatka\h2.exe"


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


def klaw(k, pauza=1.0):
    akcja("send_keys", keys=k)
    time.sleep(pauza)


def zabij():
    subprocess.run([PS, "-NoProfile", "-Command",
                    "Get-Process h2 -ErrorAction SilentlyContinue | Stop-Process -Force"],
                   capture_output=True, text=True, timeout=60)
    time.sleep(1.5)


def zmierz(wariant):
    print(f"\n{'=' * 62}\nWARIANT {wariant}\n{'=' * 62}")
    zabij()
    subprocess.run([PS, "-NoProfile", "-Command",
                    f"Start-Process -FilePath '{EXE}' -ArgumentList '{wariant}'"],
                   capture_output=True, text=True, timeout=60)
    time.sleep(5.5)

    # Kursor startuje w wierszu 1 (ostatnim), wiec strzalka w dol DOKLADA wiersz.
    z = znacznik()
    klaw("downArrow", 2.0)
    m_dokl = mowa_od(z)
    # Kontrola: zwykly ruch w gore po istniejacych komorkach.
    z = znacznik()
    klaw("upArrow", 2.0)
    m_zwykly = mowa_od(z)

    zabij()
    try:
        with io.open(f"/mnt/c/tmp/siatka/out2_{wariant}.txt",
                     encoding="utf-8", errors="replace") as f:
            stdout = f.read().strip()
    except OSError:
        stdout = "(brak pliku wyjscia)"

    print("  stdout harnessu:")
    for l in stdout.splitlines():
        print("    ", l)
    print(f"  DOKLADANIE: {len(m_dokl)} wypowiedzi")
    for x in m_dokl:
        print("     ", x[:150])
    print(f"  ZWYKLY RUCH (kontrola): {len(m_zwykly)} wypowiedzi")
    for x in m_zwykly:
        print("     ", x[:150])

    return {"wariant": wariant, "dokladanie": len(m_dokl), "zwykly": len(m_zwykly),
            "zadzialalo": "DOKLADANIE PO KLAWISZU OK" in stdout,
            "awaria": "AWARIA" in stdout}


def main():
    wyniki = [zmierz(w) for w in ("g", "h")]

    print("\n" + "=" * 62)
    print("PODSUMOWANIE")
    print("=" * 62)
    for r in wyniki:
        print(f"  wariant {r['wariant']}: dokladanie={r['dokladanie']} glosow, "
              f"zwykly ruch={r['zwykly']} glosow, "
              f"klawisz zadzialal={r['zadzialalo']}, awaria={r['awaria']}")

    # KONTROLA POZYTYWNA: dokladanie MUSI sie w ogole odbyc, inaczej mierzymy nic.
    if not all(r["zadzialalo"] for r in wyniki):
        print("\n  SONDA GLUCHA - dokladanie wiersza po klawiszu sie nie odbylo. "
              "Wynik NIC NIE ZNACZY.")
        return 2
    if not all(r["zwykly"] >= 1 for r in wyniki):
        print("\n  SONDA GLUCHA - zwykly ruch nie dal mowy. Wynik NIC NIE ZNACZY.")
        return 2

    g, h = wyniki
    print("\nWERDYKT:")
    if h["dokladanie"] < g["dokladanie"]:
        print("  Winne PRZEPISYWANIE NAGLOWKOW wierszy - warto to zmienic w kodzie.")
    elif h["dokladanie"] == g["dokladanie"] and g["dokladanie"] > 1:
        print("  Drugi glos nalezy do samego dokladania wiersza w kontrolce, "
              "nie do przepisywania naglowkow. Zamiana kodu NIC NIE DA.")
    else:
        print("  Dokladanie daje jeden glos w obu wariantach.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:
        print(f"AWARIA POMIARU: {type(e).__name__}: {e}")
        sys.exit(2)
