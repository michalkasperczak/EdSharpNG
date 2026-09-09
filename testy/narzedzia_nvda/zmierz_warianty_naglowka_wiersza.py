#!/usr/bin/env python3
"""Mierzy na ZYWYM NVDA trzy warianty wyciszenia naglowka wiersza w siatce.

Powod istnienia: build jest tu BEZ WARTOSCI jako dowod.  Poprzednia proba
(RowTemplate) kompilowala sie i wywalala okno dopiero przy dokladaniu wiersza
w dzialajacym oknie.  Mierzymy wiec MOWE i STABILNOSC, nie kompilacje.

Warianty (harness /mnt/c/tmp/siatka/h.exe):
  a - jak dzis w EdSharpie; to KONTROLA POZYTYWNA calego pomiaru: jesli tu NIE
      slychac "Wiersz 0", to sonda jest glucha i zaden zielony wynik z b/c
      niczego nie dowodzi,
  b - RowHeadersVisible = false,
  c - RowTemplate z wlasnym wierszem, ktorego Clone tworzy PUSTY wiersz,
  d - RowTemplate z wlasnym wierszem, ktorego Clone wola base.Clone()
      (DataGridViewRow.Clone tworzy instancje NASZEGO typu i kopiuje komorki).

Dla kazdego wariantu: czy okno przezylo wypelnienie i DOKLADANIE wiersza
(harness wypisuje to na stdout), oraz co czytnik mowil przy ruchu strzalkami.
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
EXE = r"C:\tmp\siatka\h.exe"

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


def klaw(k, pauza=0.9):
    akcja("send_keys", keys=k)
    time.sleep(pauza)


def zabij():
    subprocess.run([PS, "-NoProfile", "-Command",
                    "Get-Process h -ErrorAction SilentlyContinue | Stop-Process -Force"],
                   capture_output=True, text=True, timeout=60)
    time.sleep(1.5)


def zmierz(wariant):
    print(f"\n{'=' * 62}\nWARIANT {wariant}\n{'=' * 62}")
    zabij()
    # Uruchamiamy tak, zeby ZLAPAC stdout harnessu - to on mowi, czy okno
    # przezylo dokladanie wiersza.
    # Harness sam pisze swoj log do pliku.  Przekierowanie przez
    # Start-Process -RedirectStandardOutput ZAWIESZA sie na aplikacji okienkowej
    # (zmierzone: timeout 60 s), wiec nie uzywamy go.
    subprocess.run([PS, "-NoProfile", "-Command",
                    f"Start-Process -FilePath '{EXE}' -ArgumentList '{wariant}'"],
                   capture_output=True, text=True, timeout=60)
    time.sleep(5.5)

    z = znacznik()
    # KOLEJNOSC MA ZNACZENIE: "Wiersz 0" slychac na wierszu NAGLOWKA, wiec
    # trzeba na nim STANAC.  Timer harnessu doklada wiersz i przestawia kursor
    # w dol, wiec idziemy w gore DWA razy z zapasem - pierwsza wersja tej sondy
    # konczyla w wierszu tresci i sama zglosila sie jako glucha (i miala racje).
    klaw("upArrow", 1.2)
    klaw("upArrow", 1.2)
    klaw("upArrow", 1.5)
    m1 = mowa_od(z)
    z = znacznik()
    klaw("downArrow", 1.5)
    m2 = mowa_od(z)
    z = znacznik()
    klaw("upArrow", 1.5)
    m3 = mowa_od(z)
    z = znacznik()
    klaw("rightArrow", 1.5)
    m4 = mowa_od(z)

    zabij()
    try:
        with io.open(f"/mnt/c/tmp/siatka/out_{wariant}.txt",
                     encoding="utf-8", errors="replace") as f:
            stdout = f.read().strip()
    except OSError:
        stdout = "(brak pliku wyjscia)"

    wszystko = m1 + m2 + m3 + m4
    print("  stdout harnessu:")
    for l in stdout.splitlines():
        print("    ", l)
    print("  mowa:")
    for x in wszystko:
        print("    ", x[:170])

    return {
        "wariant": wariant,
        "stabilne": ("WYPELNIONE OK" in stdout and "DOKLADANIE WIERSZA OK" in stdout
                     and "AWARIA" not in stdout),
        "mowa": wszystko,
        "zera": RE_ZERO.findall(" ".join(wszystko)),
        "max_wierszy": (max((len(RE_WIERSZ.findall(x)) for x in wszystko), default=0)),
        "stdout": stdout,
    }


def main():
    wyniki = [zmierz(w) for w in ("a", "d", "e")]

    print("\n" + "=" * 62)
    print("PODSUMOWANIE")
    print("=" * 62)
    for r in wyniki:
        print(f"  wariant {r['wariant']}: stabilne={r['stabilne']} "
              f"mowa_wypowiedzi={len(r['mowa'])} "
              f"'wiersz 0'={r['zera']} max_slow_o_wierszu={r['max_wierszy']}")

    a = wyniki[0]
    print("\nKONTROLA POZYTYWNA (wariant a MUSI odtworzyc objaw):")
    if not a["mowa"]:
        print("  SONDA GLUCHA - czytnik nic nie mowil w wariancie a. "
              "Wyniki b i c NIC NIE ZNACZA.")
        return 2
    if not a["zera"]:
        print("  SONDA GLUCHA - wariant a NIE odtworzyl 'wiersz 0'. "
              "Wyniki b i c NIC NIE ZNACZA.")
        return 2
    print(f"  OK, objaw odtworzony: {a['zera']}")

    print("\nWERDYKT:")
    for r in wyniki[1:]:
        dobry = r["stabilne"] and not r["zera"] and bool(r["mowa"])
        print(f"  wariant {r['wariant']}: "
              f"{'NADAJE SIE' if dobry else 'ODPADA'} "
              f"(stabilne={r['stabilne']}, zera={r['zera']}, "
              f"mowa={'jest' if r['mowa'] else 'BRAK'})")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:
        print(f"AWARIA POMIARU: {type(e).__name__}: {e}")
        sys.exit(2)
