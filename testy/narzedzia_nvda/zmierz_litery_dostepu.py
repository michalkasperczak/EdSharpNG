#!/usr/bin/env python3
"""Pomiar na ZYWYM NVDA: litery dostepu (Alt+litera) w oknach dialogowych.

DECYZJA KASPERCZAKA (29.08.2026 12:00:31, wiadomosc 403, doslownie):

    "Skroty alt-litera w oknach dialogowych wprowadzamy. Enter zatwierdza,
     Esc anuluuje, czyli jak u niego."

Uscislenie z 12:05:55 (wiadomosc 410): "We wszystkich."

CO MIERZYMY na oknie wstawiania przypisu (Alt+F6 - dwa pola z etykietami):
1. KONTROLA POZYTYWNA: czytnik cokolwiek mowi przy otwarciu okna. Bez tego
   "nie slyszalem litery" jest nie do odroznienia od gluchej sondy.
2. Czy czytnik OGLASZA litere dostepu przy polu. To jest sedno: litera, ktorej
   niewidomy nie uslyszy, jest dla niego niewidzialna.
3. Czy Alt+litera REALNIE przenosi fokus na to pole - mierzone tym, ze po
   Alt+litera czytnik oglasza inne pole niz przed nia.
4. KONTROLA NEGATYWNA W TYM SAMYM PRZEBIEGU: Alt+litera, ktorej w oknie NIE MA,
   nie moze przeniesc fokusu nigdzie. Bez niej "fokus sie zmienil" moglo by
   znaczyc "cokolwiek nacisne, cos sie rusza".

Sonda NIE ZAKLADA wyniku: wypisuje pelna mowe, zeby dalo sie rozstrzygnac,
co czytnik naprawde powiedzial.
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
PROBKA = r"D:\projekty\edsharp-pr\testy\probka_przypis_rozdzial.md"

# NVDA oglasza litere dostepu jako "Alt+" a POTEM sama litere w trybie
# znakowym, w OSOBNYM segmencie wypowiedzi:
#   'Footnote text | pole edycji | Alt+ | <CharacterModeCommand> | t | ...'
# Wzorzec wymagajacy ciaglego "alt+t" tego NIE ZLAPIE - pierwsza wersja
# tej sondy zglaszala z tego powodu brak litery, choc czytnik ja mowil.
# Dlatego dopuszczamy dowolne segmenty i znaczniki pomiedzy.
RE_ALT = re.compile(
    r"alt\s*\+\s*(?:\|\s*(?:<[^>]*>|\s)*\|?\s*)*([a-z0-9])\b",
    re.IGNORECASE)


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

    print("\n=== krok 1: okno wstawiania przypisu (Control+F6) ===")
    # UWAGA: wstawianie przypisu to Control+F6.  Alt+F6 to SKOK do
    # przypisu i zadnego okna nie otwiera - pierwsza wersja tej sondy
    # mierzyla wlasnie jego i "brak litery" znaczyl tylko "brak okna".
    z = znacznik()
    klaw("control+f6", 2.8)
    m_otw = mowa_od(z)
    for x in m_otw:
        print("   MOWA:", repr(x[:180]))
    if not m_otw:
        zapisz("KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna", False,
               "BRAK MOWY - pomiar NIEROZSTRZYGAJACY")
        zamknij_edsharp()
        return 2
    zapisz("KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna", True,
           f"pierwsza wypowiedz: {m_otw[0][:100]!r}")

    caly_otw = " | ".join(m_otw)
    trafienia = RE_ALT.findall(caly_otw)
    zapisz("czytnik OGLASZA litere dostepu przy polu", bool(trafienia),
           f"litery uslyszane: {trafienia}; mowa: {caly_otw[:220]!r}"
           if trafienia else f"mowa BEZ litery: {caly_otw[:260]!r}")

    print("\n=== krok 2: Tab na drugie pole (miejsce przypisu) ===")
    z = znacznik()
    klaw("tab", 1.6)
    m_tab = mowa_od(z)
    for x in m_tab:
        print("   MOWA(tab):", repr(x[:180]))
    zapisz("KONTROLA POZYTYWNA: Tab przenosi i czytnik to mowi", bool(m_tab),
           f"{len(m_tab)} wypowiedzi: {' | '.join(m_tab)[:200]!r}")
    mowa_drugiego = " | ".join(m_tab).lower()

    print("\n=== krok 3: Alt+T - TO JEST MIERZONA RZECZ ===")
    # Pole tresci przypisu ma jawna litere T ("Footnote &text"), nadana
    # jeszcze w 5.0.41.  Fokus stoi teraz na DRUGIM polu, wiec skuteczne
    # Alt+T musi wrocic na pierwsze.
    z = znacznik()
    klaw("alt+t", 2.0)
    m_alt = mowa_od(z)
    print(f"   liczba wypowiedzi na Alt+T: {len(m_alt)}")
    for x in m_alt:
        print("   MOWA(alt+t):", repr(x[:180]))
    zapisz("KONTROLA POZYTYWNA: Alt+T w ogole cos mowi", bool(m_alt),
           f"{len(m_alt)} wypowiedzi"
           + ("" if m_alt else " - BEZ TEGO POMIAR JEST NIEROZSTRZYGAJACY"))

    if not m_alt:
        zamknij_edsharp()
        print("\nPOMIAR NIEROZSTRZYGAJACY - czytnik milczy na Alt+T")
        return 2

    mowa_alt = " | ".join(m_alt).lower()
    # Fokus poszedl gdzie indziej wtedy, gdy czytnik oglasza INNE pole
    # niz to, ktore oglaszal po Tab.
    zapisz("Alt+T przenosi fokus na INNE pole niz po Tab",
           mowa_alt != mowa_drugiego,
           f"po Tab: {mowa_drugiego[:110]!r}\n        po Alt+T: {mowa_alt[:110]!r}")

    print("\n=== krok 4: KONTROLA NEGATYWNA - litera, ktorej w oknie nie ma ===")
    # Alt+Z: zadne pole ani przycisk tego okna nie ma litery Z, wiec
    # fokus NIE MOZE sie ruszyc.  Bez tej kontroli krok 3 moglby znaczyc
    # tylko "cokolwiek nacisne, cos sie zmienia".
    # Litere kontrolna wybieraj z dala od menu glownego programu: Alt+Q
    # w pierwszej wersji sondy otwieral pasek menu i sonda mierzyla
    # menu, nie okno.
    z = znacznik()
    klaw("alt+z", 2.0)
    m_q = mowa_od(z)
    for x in m_q:
        print("   MOWA(alt+z):", repr(x[:180]))
    mowa_q = " | ".join(m_q).lower()
    zapisz("KONTROLA NEGATYWNA: Alt+Z nie przenosi fokusu",
           (not m_q) or (mowa_q == mowa_alt),
           f"po Alt+Z: {mowa_q[:150]!r} (po Alt+T bylo: {mowa_alt[:80]!r})")

    print("\n=== krok 5: Escape zamyka okno (jego wlasna decyzja) ===")
    z = znacznik()
    klaw("escape", 2.0)
    m_esc = mowa_od(z)
    for x in m_esc:
        print("   MOWA(escape):", repr(x[:180]))
    zapisz("Escape zamyka okno i czytnik wraca do dokumentu", bool(m_esc),
           f"{len(m_esc)} wypowiedzi: {' | '.join(m_esc)[:180]!r}")

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
