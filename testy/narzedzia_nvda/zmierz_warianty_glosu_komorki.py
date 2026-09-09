#!/usr/bin/env python3
"""Mierzy na ZYWYM NVDA cztery warianty wyciszenia POWTORKI tresci i "zaznaczony".

Powod istnienia: zgloszenie Kasperczaka (29.08.2026) mowi, ze jeden ruch po
komorce daje trzy segmenty mowy - nasza nazwe, tresc DRUGI RAZ i slowo
"zaznaczony", a na pustej komorce "(zerowy)".  Build tego nie rozstrzyga; to
warstwa czytnika.

Warianty (harness /mnt/c/tmp/siatka/h2.exe):
  a - jak dzis w EdSharpie.  KONTROLA POZYTYWNA calego pomiaru: jesli tu NIE
      slychac powtorki i "zaznaczony", sonda jest glucha i reszta nic nie znaczy.
  b - wyciszona WARTOSC obiektu komorki (Value => "")
  e - b + stan bez Selected I bez Selectable + wyciszony Description
  f - b + stan bez Selectable (zaznaczenie zostaje) + wyciszony Description

Dla kazdego wariantu sprawdzamy TEZ, czy okno przezylo wypelnienie i dokladanie
wiersza - tam padala poprzednia proba przy wierszu siatki.
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
EXE = r"C:\tmp\siatka\h2.exe"

RE_ZAZNACZ = re.compile(r"\b(?:niezaznaczon\w*|zaznaczon\w*|selected)\b", re.IGNORECASE)
RE_ZERO = re.compile(r"zerow\w*", re.IGNORECASE)


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


def mowa_od(poz, pauza=1.5):
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

    # Kursor startuje w wierszu 1 kolumnie 1 ("Ala").  Ruchy: w prawo na "30",
    # w lewo z powrotem na "Ala", w dol na PUSTA komorke dokladanego wiersza.
    z = znacznik()
    klaw("rightArrow", 1.6)
    m_prawo = mowa_od(z)
    z = znacznik()
    klaw("leftArrow", 1.6)
    m_lewo = mowa_od(z)
    z = znacznik()
    klaw("downArrow", 1.6)
    m_dol = mowa_od(z)

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
    print("  mowa w prawo:", [x[:120] for x in m_prawo])
    print("  mowa w lewo: ", [x[:120] for x in m_lewo])
    print("  mowa w dol:  ", [x[:120] for x in m_dol])

    caly_30 = " ".join(m_prawo)
    caly_ala = " ".join(m_lewo)
    caly_puste = " ".join(m_dol)
    wszystko = caly_30 + " " + caly_ala + " " + caly_puste

    return {
        "wariant": wariant,
        "stabilne": ("WYPELNIONE OK" in stdout and "DOKLADANIE WIERSZA OK" in stdout
                     and "AWARIA" not in stdout),
        "mowa": bool(m_prawo and m_lewo),
        "powtorka_30": caly_30.count("30") > 1,
        "powtorka_ala": caly_ala.lower().count("ala") > 1,
        "zaznaczony": bool(RE_ZAZNACZ.search(wszystko)),
        "zerowy": bool(RE_ZERO.search(caly_puste)),
        "surowe": {"prawo": caly_30, "lewo": caly_ala, "dol": caly_puste},
    }


def main():
    wyniki = [zmierz(w) for w in ("a", "e", "f")]

    print("\n" + "=" * 62)
    print("PODSUMOWANIE")
    print("=" * 62)
    for r in wyniki:
        print(f"  wariant {r['wariant']}: stabilne={r['stabilne']} mowa={r['mowa']} "
              f"powtorka(30)={r['powtorka_30']} powtorka(Ala)={r['powtorka_ala']} "
              f"zaznaczony={r['zaznaczony']} zerowy={r['zerowy']}")

    a = wyniki[0]
    print("\nKONTROLA POZYTYWNA (wariant a MUSI odtworzyc objaw zgloszenia):")
    if not a["mowa"]:
        print("  SONDA GLUCHA - czytnik nic nie mowil w wariancie a. Reszta NIC NIE ZNACZY.")
        return 2
    if not (a["powtorka_30"] or a["powtorka_ala"]) or not a["zaznaczony"]:
        print("  SONDA GLUCHA - wariant a nie odtworzyl powtorki albo 'zaznaczony'. "
              "Reszta NIC NIE ZNACZY.")
        print("  surowe a:", a["surowe"])
        return 2
    print(f"  OK, objaw odtworzony: powtorka={a['powtorka_30'] or a['powtorka_ala']} "
          f"zaznaczony={a['zaznaczony']} zerowy={a['zerowy']}")

    print("\nWERDYKT:")
    for r in wyniki[1:]:
        dobry = (r["stabilne"] and r["mowa"] and not r["powtorka_30"]
                 and not r["powtorka_ala"] and not r["zaznaczony"] and not r["zerowy"])
        print(f"  wariant {r['wariant']}: {'NADAJE SIE' if dobry else 'ODPADA'} "
              f"(stabilne={r['stabilne']}, mowa={'jest' if r['mowa'] else 'BRAK'}, "
              f"powtorka={r['powtorka_30'] or r['powtorka_ala']}, "
              f"zaznaczony={r['zaznaczony']}, zerowy={r['zerowy']})")
        print(f"     w prawo: {r['surowe']['prawo'][:150]!r}")
        print(f"     w dol:   {r['surowe']['dol'][:150]!r}")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:
        print(f"AWARIA POMIARU: {type(e).__name__}: {e}")
        sys.exit(2)
