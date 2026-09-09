#!/usr/bin/env python3
"""Pomiar MOWY NVDA dla rozdzialu 15 listy testow (spis tresci markdownowy).

CZEGO DOTYCZY: punkty 15.1, 15.2, 15.6, 15.7, 15.8 - te, ktore poprzednia
iteracja zapisala jako "czeka na sprawdzenie u Kasperczaka (brzmienie
czytnika)". Kod komunikatu zna tylko literalny napis; ten skrypt mierzy,
CO NVDA wypowiada na zywo, na zbudowanej binarce.

WZORCE PULAPEK odziedziczone z zmierz_mowe_nvda.py (nie powtarzaj bledow):
- SetForegroundWindow NIE daje fokusu klawiatury z tla: aktywacja przez
  syntetyczne klikniecie (klik.ps1).
- Pauza po kazdym klawiszu obowiazkowa, inaczej czytasz stan sprzed zmiany.
- BRAK MOWY to UWAGA, nie OK: pusty pomiar nie jest dowodem poprawnosci.
- Polskie ogonki w mowie NVDA: dopasowanie po normalizacji, inaczej
  falszywy negatyw. Tu komunikaty sa angielskie, ale mowa NVDA miesza
  jezyki, wiec normalizujemy tak samo.

KONTROLA WAZNOSCI: skrypt sprawdza takze przypadek, ktory MUSI zawiesc
(Shift+F6 w dokumencie bez spisu ma NIE powiedziec "contents item"), zeby
zaliczenie nie bylo skutkiem samego faktu, ze cokolwiek zostalo wymowione.
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
    for a, b in (("ą", "a"), ("ć", "c"), ("ę", "e"), ("ł", "l"), ("ń", "n"),
                 ("ó", "o"), ("ś", "s"), ("ź", "z"), ("ż", "z")):
        s = s.replace(a, b)
    return s


def klaw(k, pauza=0.6):
    akcja("send_keys", keys=k)
    time.sleep(pauza)


def aktywuj(proby=4):
    """Aktywacja z PONOWIENIEM.

    CZEMU PONOWIENIE: po swiezym starcie EdSharpa okno bywa jeszcze nie
    gotowe (MainWindowHandle jest, ale okno nie przyjmuje jeszcze klikniecia)
    i pierwsza proba zwraca EDSHARP_AKTYWNY=False. Zmierzone 27.08.2026:
    ta sama komenda uruchomiona rece pozniej dawala True. Bez ponowienia
    skrypt konczy sie "BLAD: nie aktywowalem", co wyglada jak wada programu,
    a jest wyscigiem w moim pomiarze.
    """
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


def linia_biezaca():
    """Tresc wiersza pod kursorem, czytana przez NVDA (read_current_line)."""
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


def main():
    wyniki = []

    def zapisz(opis, ok, detal):
        wyniki.append((opis, ok, detal))
        print(f"  -> [{'OK   ' if ok else 'UWAGA'}] {detal}")

    # ================= CZESC A: dokument z naglowkami, bez spisu =========
    print("=== przygotowanie: dokument Z NAGLOWKAMI, BEZ spisu (punkt 15.8) ===")
    zamknij_edsharp()
    uruchom_edsharp(r"D:\projekty\edsharp-pr\testy\spis_probka_bez_spisu.md")
    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa (dokument bez spisu)")
        return 2
    if not w_edytorze():
        print("BLAD: fokus nie jest w polu edycji")
        return 2

    print("\n=== TEST 15.8: Shift+F6 gdy naglowki SA, a spisu NIE MA ===")
    klaw("control+home", 0.8)
    z = znacznik()
    klaw("shift+f6", 1.4)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    ma_podpowiedz = "alt+shift+t" in caly.replace(" ", "") or "alt shift t" in caly
    ma_no_contents = "no contents yet" in caly
    if not m:
        zapisz("15.8 komunikat rozszerzony", False, "BRAK MOWY - nie mierzalne")
    else:
        zapisz("15.8 komunikat rozszerzony podpowiada Alt+Shift+T",
               ma_no_contents and ma_podpowiedz,
               f"'no contents yet'={ma_no_contents}, podpowiedz klawisza={ma_podpowiedz}")
        # KONTROLA WAZNOSCI: nie wolno tu uslyszec skoku do pozycji spisu
        zapisz("15.8 kontrola waznosci: NIE slychac skoku do spisu",
               "contents item" not in caly and ", contents" not in caly,
               "brak formuly skoku - komunikat, nie skok")

    # ================= CZESC B: dokument z naglowkami, tworzymy spis =====
    print("\n=== przygotowanie: dokument probny (punkty 15.1, 15.2, 15.6, 15.7) ===")
    zamknij_edsharp()
    uruchom_edsharp(r"D:\projekty\edsharp-pr\testy\spis_probka.md")
    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa (dokument probny)")
        return 2
    if not w_edytorze():
        print("BLAD: fokus nie jest w polu edycji")
        return 2

    print("\n=== TEST 15.1: Alt+Shift+T tworzy spis, mowi 'Contents, N items' ===")
    klaw("control+home", 0.8)
    z = znacznik()
    klaw("alt+shift+t", 1.5)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    liczba = re.search(r"contents,\s*(\d+)\s*item", caly)
    if not m:
        zapisz("15.1 komunikat po utworzeniu", False, "BRAK MOWY - nie mierzalne")
    else:
        zapisz("15.1 slychac 'Contents' i liczbe pozycji",
               liczba is not None,
               f"dopasowanie: {liczba.group(0) if liczba else 'BRAK'}")
        if liczba:
            # dokument probny ma 5 naglowkow (H1 + 3xH2 + 1xH3)
            zapisz("15.1 liczba pozycji zgadza sie z liczba naglowkow (5)",
                   liczba.group(1) == "5",
                   f"wymowiono {liczba.group(1)}, w dokumencie 5 naglowkow")

    print("\n=== TEST 15.2: drugie Alt+Shift+T mowi 'Contents updated' ===")
    z = znacznik()
    klaw("alt+shift+t", 1.5)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    if not m:
        zapisz("15.2 komunikat po odswiezeniu", False, "BRAK MOWY - nie mierzalne")
    else:
        zapisz("15.2 slychac 'Contents updated' (nie 'Contents,')",
               "contents updated" in caly,
               f"'contents updated' w mowie: {'contents updated' in caly}")

    print("\n=== TEST 15.6: Shift+F6 z tresci -> pozycja spisu, mowa '<tytul>, contents' ===")
    # zejdz do tresci: na koniec dokumentu, tam jest rozdzial Podsumowanie
    klaw("control+end", 0.9)
    klaw("upArrow", 0.6)
    z = znacznik()
    klaw("shift+f6", 1.5)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    if not m:
        zapisz("15.6 mowa przy skoku do spisu", False, "BRAK MOWY - nie mierzalne")
    else:
        zapisz("15.6 slychac slowo 'contents' przy skoku w gore",
               "contents" in caly, f"fragment: {caly[:120]!r}")
        zapisz("15.6 slychac TYTUL rozdzialu razem z 'contents'",
               "podsumowanie" in caly,
               "tytul 'Podsumowanie' obecny w wypowiedzi"
               if "podsumowanie" in caly else
               "tytulu NIE slychac - to realna uwaga")

    print("\n=== TEST 15.7: Shift+F6 z pozycji spisu -> rozdzial, mowa '<tytul>, heading N' ===")
    z = znacznik()
    klaw("shift+f6", 1.5)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    if not m:
        zapisz("15.7 mowa przy skoku do rozdzialu", False, "BRAK MOWY - nie mierzalne")
    else:
        ma_heading = re.search(r"heading\s*\d", caly) is not None
        zapisz("15.7 slychac formule 'heading N' (ta sama co Control+PageDown)",
               ma_heading, f"fragment: {caly[:120]!r}")
        zapisz("15.7 kontrola waznosci: to NIE jest ta sama wypowiedz co 15.6",
               "contents" not in caly.replace("table of contents", ""),
               "wypowiedz rozni sie od skoku w gore"
               if "contents" not in caly else
               "slowo 'contents' nadal obecne - sprawdz kierunek skoku")

    print("\n=== TEST 15.3: Control+Z po Alt+Shift+T cofa spis ===")
    # Mierzymy TRESC pierwszego wiersza przez NVDA, nie sam komunikat:
    # komunikat moglby brzmiec dobrze przy dokumencie, ktory sie nie zmienil.
    klaw("control+home", 0.8)
    przed_cofnieciem = linia_biezaca()
    print(f"   pierwszy wiersz PRZED cofnieciem: {przed_cofnieciem!r}")
    klaw("control+z", 1.3)
    klaw("control+home", 0.8)
    po_cofnieciu = linia_biezaca()
    print(f"   pierwszy wiersz PO cofnieciu:     {po_cofnieciu!r}")
    klaw("control+z", 1.3)
    klaw("control+home", 0.8)
    po_drugim = linia_biezaca()
    print(f"   pierwszy wiersz po DRUGIM cofnieciu: {po_drugim!r}")
    wrocil = "dokument probny" in norm(po_drugim) or "dokument probny" in norm(po_cofnieciu)
    zapisz("15.3 Control+Z przywraca oryginalny pierwszy wiersz dokumentu",
           wrocil,
           f"po cofnieciu widac naglowek dokumentu: {wrocil}"
           f" (przed: {przed_cofnieciem[:40]!r})")
    zapisz("15.3 kontrola waznosci: przed cofnieciem NIE bylo oryginalu",
           "dokument probny" not in norm(przed_cofnieciem),
           "stan przed i po roznia sie - pomiar rozroznia cofniecie"
           if "dokument probny" not in norm(przed_cofnieciem)
           else "przed cofnieciem tez byl original - pomiar NIC nie rozroznia")

    # ============ CZESC C: kontrola DYSKRYMINACYJNA komunikatow ==========
    # Bez tego 15.8 dowodzi tylko, ze cokolwiek zostalo wymowione. Tu ten sam
    # klawisz w dokumencie BEZ naglowkow MUSI powiedziec co INNEGO.
    print("\n=== KONTROLA DYSKRYMINACYJNA: Shift+F6 w dokumencie BEZ naglowkow ===")
    zamknij_edsharp()
    uruchom_edsharp(r"D:\projekty\edsharp-pr\testy\spis_probka_bez_naglowkow.md")
    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa (dokument bez naglowkow)")
    else:
        klaw("control+home", 0.8)
        z = znacznik()
        klaw("shift+f6", 1.4)
        m = mowa_od(z)
        for x in m:
            print("   MOWA:", x[:160])
        caly = norm(" ".join(m))
        zapisz("15.5/15.8 bez naglowkow slychac KROTKI 'No headings!'",
               "no headings" in caly,
               f"'no headings' w mowie: {'no headings' in caly}")
        zapisz("kontrola dyskryminacyjna: NIE slychac tu komunikatu rozszerzonego",
               "no contents yet" not in caly,
               "komunikaty sa rozne w dwoch przypadkach - sonda rozroznia"
               if "no contents yet" not in caly
               else "OBA komunikaty identyczne - punkt 5 zlecenia NIE jest spelniony")

    print("\n" + "=" * 62)
    print("PODSUMOWANIE POMIARU MOWY - ROZDZIAL 15")
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
