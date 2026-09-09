#!/usr/bin/env python3
"""Pomiar na ZYWYM NVDA: co czytnik mowi przy komentarzach wewnetrznych (5.0.43).

PUNKT 4 MAPY DROGOWEJ.  Zlecenie Kasperczaka z 28.08.2026 00:20 i 00:24: "A
komentarze, nie da sie robic Markdown komentarzy zgodnych potem z Wordem?
Jakas tu ich sensowna obsluga?" -> "To na razie komentarze wewnetrzne".

CO MIERZYMY (mowa czytnika, nie kod):
1. KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna wstawiania (bez tego
   kazde "nie slyszalem" znaczy tylko "sonda gluchа").
2. Czy czytnik OGLASZA litere dostepu pola tresci - litera, ktorej niewidomy
   nie uslyszy, jest dla niego niewidzialna (zasada z 5.0.42).
3. Czy przy skoku Control+Shift+F9 czytnik mowi TRESC uwagi PRZED slowem comment - to
   jest jego wlasny wybor formy wypowiedzi, powtarzany w calym programie.
4. KONTROLA NEGATYWNA: Alt+F9 w pliku, ktory NIE jest Markdownem, nie moze otworzyc
   okna; bez tej kontroli "okno sie otworzylo" nie odroznia sie od "cokolwiek
   nacisne, cos sie dzieje".

Sonda NIE ZAKLADA wyniku: wypisuje pelna mowe, zeby dalo sie rozstrzygnac, co
naprawde leci do uzytkownika.

DLACZEGO NIE GOLY F9 (pulapka zlapana wlasnie ta sonda, 29.08.2026): pierwsza
wersja tej funkcji siedziala na F9 i Shift+F9, bo w Hotkeys.ini tych chordow nie
ma.  Ta sonda pokazala CISZE na F9 przy dzialajacej kontroli (Control+F6 mowil
poprawnie), czyli klawisz byl MARTWY przy zielonym buildzie.  Przyczyna: goly F9
i Shift+F9 sa przechwytywane WPROST w ProcessCmdKey_Helper i obsluguja skrypt
JAWS czytajacy tekst do konca.  Uklad przeniesiony na Alt+F9, Control+Shift+F9,
Alt+Shift+F9 i Control+Alt+F9.
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
PROBKA = r"C:\tmp\kom\probka_nvda.md"
PROBKA_TXT = r"C:\tmp\kom\probka_nvda.txt"

# NVDA mowi litere dostepu w trybie znakowym jako OSOBNY segment
# ("Alt+ | <CharacterModeCommand> | t"), wiec ciagle "alt+t" nie zadziala -
# pulapka zlapana przy 5.0.42.
RE_LITERA = re.compile(r"alt\s*\+", re.IGNORECASE)


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


def przygotuj_probki():
    os.makedirs("/mnt/c/tmp/kom", exist_ok=True)
    tresc = ("# Rozdzial pierwszy\r\n\r\nPierwszy akapit do pracy.\r\n\r\n"
             "Drugi akapit ponizej.\r\n")
    with io.open("/mnt/c/tmp/kom/probka_nvda.md", "w", encoding="utf-8",
                 newline="") as f:
        f.write(tresc)
    with io.open("/mnt/c/tmp/kom/probka_nvda.txt", "w", encoding="utf-8",
                 newline="") as f:
        f.write("Zwykly plik tekstowy, nie Markdown.\r\n")


def main():
    wyniki = []

    def zapisz(opis, ok, detal):
        wyniki.append((opis, ok, detal))
        print(f"  -> [{'OK   ' if ok else 'UWAGA'}] {opis}\n        {detal}")

    print("=== przygotowanie ===")
    przygotuj_probki()
    zamknij_edsharp()
    uruchom_edsharp(PROBKA)
    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa - pomiar NIEROZSTRZYGAJACY")
        return 2

    print("\n=== krok 1: Alt+F9 otwiera okno wstawiania komentarza ===")
    z = znacznik()
    klaw("alt+f9", 2.6)
    m_otw = mowa_od(z)
    for x in m_otw:
        print("   MOWA:", repr(x[:170]))
    if not m_otw:
        zapisz("KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna", False,
               "BRAK MOWY - pomiar NIEROZSTRZYGAJACY")
        zamknij_edsharp()
        return 2
    zapisz("KONTROLA POZYTYWNA: czytnik mowi przy otwarciu okna", True,
           f"pierwsza wypowiedz: {m_otw[0][:90]!r}")

    caly_otw = " | ".join(m_otw)
    zapisz("czytnik OGLASZA litere dostepu pola tresci",
           bool(RE_LITERA.search(caly_otw)),
           f"mowa okna: {caly_otw[:220]!r}")

    print("\n=== krok 2: wpisuje tresc uwagi i zatwierdzam ===")
    pisz("uwaga robocza")
    z = znacznik()
    klaw("enter", 2.0)
    m_wst = mowa_od(z)
    for x in m_wst:
        print("   MOWA(wstawienie):", repr(x[:170]))
    zapisz("program potwierdza wstawienie komentarza",
           any("comment" in x.lower() for x in m_wst),
           f"mowa: {' | '.join(m_wst)[:200]!r}")

    print("\n=== krok 3: Alt+F9 na tym samym miejscu ma otworzyc POPRAWKE ===")
    z = znacznik()
    klaw("alt+f9", 2.6)
    m_edit = mowa_od(z)
    for x in m_edit:
        print("   MOWA(poprawka):", repr(x[:170]))
    caly_edit = " | ".join(m_edit)
    zapisz("tytul okna mowi o POPRAWCE, nie o wstawianiu",
           "edit comment" in caly_edit.lower(),
           f"mowa: {caly_edit[:220]!r}")
    klaw("escape", 1.6)

    print("\n=== krok 4: skok Control+Shift+F9 - TO JEST MIERZONA FORMA WYPOWIEDZI ===")
    klaw("control+home", 1.2)
    z = znacznik()
    klaw("control+shift+f9", 2.2)
    m_skok = mowa_od(z)
    for x in m_skok:
        print("   MOWA(skok):", repr(x[:170]))
    zapisz("KONTROLA POZYTYWNA: skok w ogole cos mowi", bool(m_skok),
           f"{len(m_skok)} wypowiedzi"
           + ("" if m_skok else " - BEZ TEGO PONIZSZE NIC NIE ZNACZY"))
    if m_skok:
        caly_skok = " | ".join(m_skok)
        low = caly_skok.lower()
        i_tresc = low.find("uwaga")
        i_slowo = low.find("comment")
        zapisz("czytnik mowi TRESC uwagi PRZED slowem comment",
               i_tresc >= 0 and i_slowo > i_tresc,
               f"tresc na {i_tresc}, slowo comment na {i_slowo}; "
               f"mowa: {caly_skok[:200]!r}")

    print("\n=== krok 5: Control+Shift+F9 na ostatnim komentarzu NIE zawija ===")
    z = znacznik()
    klaw("control+shift+f9", 2.2)
    m_kon = mowa_od(z)
    for x in m_kon:
        print("   MOWA(koniec):", repr(x[:170]))
    zapisz("na ostatnim komentarzu program mowi, ze to ostatni",
           any("last comment" in x.lower() for x in m_kon),
           f"mowa: {' | '.join(m_kon)[:200]!r}")

    print("\n=== krok 6: KONTROLA NEGATYWNA - plik NIE-Markdown ===")
    zamknij_edsharp()
    uruchom_edsharp(PROBKA_TXT)
    if not aktywuj():
        print("   (nie aktywowalem okna dla pliku txt - kontrola pominieta)")
    else:
        z = znacznik()
        klaw("alt+f9", 2.4)
        m_txt = mowa_od(z)
        for x in m_txt:
            print("   MOWA(txt):", repr(x[:170]))
        caly_txt = " | ".join(m_txt).lower()
        # Okno wstawiania NIE MOZE sie otworzyc, a program ma powiedziec dlaczego.
        zapisz("KONTROLA NEG: w pliku txt okno komentarza sie NIE otwiera",
               "insert comment" not in caly_txt,
               f"mowa: {caly_txt[:200]!r}")

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
