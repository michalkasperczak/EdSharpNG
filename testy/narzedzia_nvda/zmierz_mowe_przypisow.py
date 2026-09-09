#!/usr/bin/env python3
"""Pomiar MOWY NVDA dla rozdzialu 16 listy testow (przypisy markdownowe).

Zlecenie 1787832545532-3. Kod zna tylko literalny napis komunikatu; ten skrypt
mierzy, CO NVDA REALNIE WYMAWIA na zywo, na zbudowanej binarce EdSharpNG.exe.

CO MIERZY (punkty listy testow):
  16.1  Ctrl+Shift+K wstawia: okienko, tresc, Enter -> znacznik w zdaniu, tresc na
        koncu, KURSOR ZOSTAJE w zdaniu, mowa "Footnote 1 inserted".
  16.6  Alt+F6 ze zdania -> tresc przypisu, mowa z trescia i numerem.
  16.7  Alt+F6 z tresci -> powrot do zdania.
  16.8  to samo z kursorem na SAMYM KONCU wiersza (tam byl blad o jeden wiersz).
  16.10 Alt+F6 bez przypisow -> "No footnotes!".
  16.11 Alt+K -> okno listy "Footnotes".
  16.13 bramka na typ pliku (.txt).

NAJWAZNIEJSZY DOWOD, KTOREGO NIE DA SIE ZROBIC INACZEJ: po podmianie klawiszy
Control+Shift+K i Alt+F6 program mogl przy starcie pokazac okno alarmu "Cannot
assign ... since already assigned to". Refleksja tego nie wykryje, bo KeyMap
wypelnia sie dopiero przy budowaniu menu. Tu sprawdzamy to WPROST: po starcie
fokus MUSI byc w polu edycji, a nie w okienku dialogowym, i mowa NIE MOZE
zawierac slowa "Cannot assign".

KONTROLA WAZNOSCI: dokument BEZ przypisow musi dac inny komunikat niz dokument
z przypisami. Bez tego zaliczenie moglo wynikac z faktu, ze cokolwiek zostalo
wymowione. Druga kontrola: kursor po wstawieniu ma NIE stac na koncu dokumentu.

PULAPKA NAZW KLAWISZY (zmierzona 27.08.2026): mostek NVDA przyjmuje nazwy
gestow NVDA, wiec strzalka to "downArrow"/"upArrow", a NIE "down" - na "down"
mostek zwraca HTTP 500 i skrypt przewraca sie w polowie pomiaru.

PULAPKI ODZIEDZICZONE (nie powtarzaj): SetForegroundWindow nie daje fokusu z
tla - aktywacja przez klik.ps1 z ponowieniem; pauza po kazdym klawiszu; BRAK
MOWY to UWAGA, nie OK.
"""
import io
import os
import shutil
import subprocess
import sys
import time

sys.path.insert(0, "/mnt/d/projekty/edsharp-pr/testy/narzedzia_nvda")
from zmierz_mowe_spisu_tresci import (akcja, aktywuj, fokus, klaw,
                                      linia_biezaca, mowa_od, norm,
                                      uruchom_edsharp, w_edytorze,
                                      zamknij_edsharp, znacznik)

PS = "/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe"
# Pracujemy na KOPIACH: pliki probne w repo musza zostac nietkniete, a te
# komendy zmieniaja dokument.
ZRODLA = "/mnt/d/projekty/edsharp-pr/testy"
ROBOCZY = "/mnt/c/tmp/fn_nvda"
ROBOCZY_WIN = r"C:\tmp\fn_nvda"

wyniki = []


def zapisz(opis, ok, detal):
    wyniki.append((opis, ok, detal))
    print("  -> [{}] {}".format("OK   " if ok else "UWAGA", detal))


def przygotuj():
    if os.path.isdir(ROBOCZY):
        shutil.rmtree(ROBOCZY)
    os.makedirs(ROBOCZY)
    doc = ("# Dokument probny przypisow\r\n"
           "\r\n"
           "Pierwsze zdanie dokumentu.\r\n"
           "\r\n"
           "## Rozdzial drugi\r\n"
           "\r\n"
           "Drugie zdanie dokumentu.\r\n")
    with io.open(os.path.join(ROBOCZY, "przypisy.md"), "w", encoding="utf-8", newline="") as f:
        f.write(doc)
    # Dokument, ktory JUZ ma przypis - do pomiaru skokow bez zaleznosci od
    # tego, czy wstawianie zadzialalo.
    doc2 = ("# Dokument ze przypisem\r\n"
            "\r\n"
            "Zdanie z przypisem[^1] w tresci.\r\n"
            "\r\n"
            "Inne zdanie bez niczego.\r\n"
            "\r\n"
            "[^1]: Tresc pierwszego przypisu.\r\n")
    with io.open(os.path.join(ROBOCZY, "przypisy_gotowe.md"), "w", encoding="utf-8", newline="") as f:
        f.write(doc2)
    with io.open(os.path.join(ROBOCZY, "zwykly.txt"), "w", encoding="utf-8", newline="") as f:
        f.write("Zwykly plik tekstowy.\r\nDrugi wiersz.\r\n")
    # Dokument do pomiaru BRAMEK MIEJSCA KURSORA (iteracja 2): ma blok kodu,
    # tresc przypisu i zwykle zdanie - trzy miejsca, ktore maja zachowac sie
    # ROZNIE.  Bez zwyklego zdania w tym samym pliku pomiar nie odroznilby
    # "bramka dziala" od "wstawianie w ogole przestalo dzialac".
    doc3 = ("# Dokument z blokiem kodu\r\n"
            "\r\n"
            "Zwykle zdanie do wstawienia.\r\n"
            "\r\n"
            "```\r\n"
            "linia kodu w bloku\r\n"
            "```\r\n"
            "\r\n"
            "Zdanie z przypisem[^1] w tresci.\r\n"
            "\r\n"
            "[^1]: Tresc pierwszego przypisu.\r\n")
    with io.open(os.path.join(ROBOCZY, "przypisy_bramki.md"), "w", encoding="utf-8", newline="") as f:
        f.write(doc3)


def caly_dokument():
    """Cala tresc pliku z DYSKU - do sprawdzenia, ze nic nie zapisano."""
    return io.open(os.path.join(ROBOCZY, "przypisy.md"),
                   encoding="utf-8", errors="replace").read()


def start(nazwa):
    zamknij_edsharp()
    uruchom_edsharp(ROBOCZY_WIN + "\\" + nazwa)
    if not aktywuj():
        print("BLAD: nie aktywowalem EdSharpa ({})".format(nazwa))
        return False
    return True


def main():
    przygotuj()

    # ===== 0. DOWOD, ZE PODMIANA KLAWISZY NIE DALA ALARMU PRZY STARCIE =====
    print("=== TEST 0: start programu po podmianie Control+Shift+K i Alt+F6 ===")
    z = znacznik()
    if not start("przypisy.md"):
        return 2
    m_start = mowa_od(z, 1.5)
    for x in m_start:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m_start))
    zapisz("0. brak alarmu 'Cannot assign' przy starcie",
           "cannot assign" not in caly and "already assigned" not in caly,
           "mowa startowa bez alarmu o zajetym skrocie")
    w_polu = w_edytorze()
    zapisz("0. po starcie fokus jest w POLU EDYCJI, nie w okienku alarmu",
           w_polu, "fokus w edytorze: {}".format(w_polu))
    if not w_polu:
        print("BLAD: fokus nie w polu edycji - dalszy pomiar bylby nieuczciwy")
        return 2

    # ===== 16.1 WSTAWIANIE =====
    print("\n=== TEST 16.1: Control+Shift+K wstawia przypis ===")
    klaw("control+home", 0.8)
    klaw("downArrow", 0.5)
    klaw("downArrow", 0.5)
    klaw("end", 0.6)          # koniec zdania "Pierwsze zdanie dokumentu."
    wiersz_przed = linia_biezaca()
    print("   wiersz przed:", repr(wiersz_przed))
    z = znacznik()
    klaw("control+shift+k", 1.6)
    m_okno = mowa_od(z, 1.0)
    for x in m_okno:
        print("   MOWA(okno):", x[:160])
    f = fokus()
    kl = str(f.get("windowClassName") or "")
    zapisz("16.1 otwiera sie okienko na tresc przypisu",
           "EDIT" in kl.upper() or "Insert Footnote" in str(f.get("name") or ""),
           "fokus po Control+Shift+K: klasa={} nazwa={}".format(kl, str(f.get("name"))[:60]))

    # Wpisujemy tresc i zatwierdzamy.
    z = znacznik()
    akcja("send_keys", keys="t")
    time.sleep(0.3)
    akcja("send_keys", keys="e")
    time.sleep(0.3)
    akcja("send_keys", keys="s")
    time.sleep(0.3)
    akcja("send_keys", keys="t")
    time.sleep(0.5)
    klaw("enter", 2.0)
    m = mowa_od(z, 1.4)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    zapisz("16.1 slychac potwierdzenie wstawienia z numerem",
           "footnote 1 inserted" in caly or ("footnote" in caly and "inserted" in caly),
           "komunikat: {}".format([x for x in m if "ootnote" in x][:2] or "BRAK"))

    wiersz_po = linia_biezaca()
    print("   wiersz po:  ", repr(wiersz_po))
    zapisz("16.1 KURSOR ZOSTAL W ZDANIU (nie skoczyl na koniec dokumentu)",
           "[^1]" in wiersz_po,
           "wiersz pod kursorem zawiera znacznik: {!r}".format(wiersz_po))
    # KONTROLA: kursor nie stoi na tresci przypisu (ta jest na koncu pliku).
    zapisz("16.1 kontrola: kursor NIE stoi na tresci przypisu",
           not wiersz_po.strip().startswith("[^1]:"),
           "wiersz nie jest definicja przypisu")

    # ===== 16.4 COFNIECIE =====
    print("\n=== TEST 16.4: Control+Z po wstawieniu ===")
    klaw("control+z", 1.0)
    klaw("control+z", 1.0)
    w = linia_biezaca()
    klaw("control+home", 0.6)
    zapisz("16.4 cofniecie usuwa znacznik ze zdania",
           "[^1]" not in w,
           "wiersz po dwoch cofnieciach: {!r}".format(w))

    # ===== 16.6 i 16.7 SKOK W OBIE STRONY (na gotowym dokumencie) =====
    print("\n=== TEST 16.6: Alt+F6 ze zdania do tresci przypisu ===")
    if not start("przypisy_gotowe.md"):
        return 2
    klaw("control+home", 0.8)
    klaw("downArrow", 0.5)
    klaw("downArrow", 0.6)          # wiersz "Zdanie z przypisem[^1] w tresci."
    print("   wiersz startowy:", repr(linia_biezaca()))
    z = znacznik()
    klaw("alt+f6", 1.5)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    w_docelowy = linia_biezaca()
    print("   wiersz po skoku:", repr(w_docelowy))
    zapisz("16.6 kursor stoi na TRESCI przypisu",
           w_docelowy.strip().startswith("[^1]:"),
           "wiersz docelowy: {!r}".format(w_docelowy))
    zapisz("16.6 slychac tresc przypisu i numer",
           "footnote" in caly and ("tresc" in caly or "1" in caly),
           "mowa: {}".format([x for x in m if x.strip()][:2] or "BRAK MOWY"))

    print("\n=== TEST 16.7: Alt+F6 z tresci wraca do zdania ===")
    z = znacznik()
    klaw("alt+f6", 1.5)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    w_powrot = linia_biezaca()
    print("   wiersz po powrocie:", repr(w_powrot))
    zapisz("16.7 kursor WROCIL do zdania ze znacznikiem",
           "[^1]" in w_powrot and not w_powrot.strip().startswith("[^1]:"),
           "wiersz: {!r}".format(w_powrot))

    # ===== 16.8 KURSOR NA SAMYM KONCU WIERSZA =====
    print("\n=== TEST 16.8: Alt+F6 z kursorem na KONCU wiersza ===")
    klaw("end", 0.7)
    z = znacznik()
    klaw("alt+f6", 1.5)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    w_end = linia_biezaca()
    print("   wiersz po skoku z konca:", repr(w_end))
    zapisz("16.8 skok dziala takze z kursorem na koncu wiersza",
           w_end.strip().startswith("[^1]:"),
           "wiersz docelowy: {!r}".format(w_end))

    # ===== 16.11 LISTA =====
    print("\n=== TEST 16.11: Alt+K otwiera liste ===")
    klaw("control+home", 0.8)
    z = znacznik()
    klaw("alt+k", 1.8)
    m = mowa_od(z, 1.2)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    f = fokus()
    zapisz("16.11 otwiera sie okno listy przypisow",
           "footnote" in caly or "footnote" in norm(str(f.get("name") or "")),
           "mowa/fokus: {} | {}".format([x for x in m][:2] or "BRAK", str(f.get("name"))[:50]))
    zapisz("16.11 na liscie slychac tresc przypisu",
           "tresc" in caly or "1." in " ".join(m),
           "pozycja listy wymowiona: {}".format([x for x in m if "resc" in x][:1] or "brak"))
    klaw("escape", 1.2)

    # ===== 16.10 BEZ PRZYPISOW - KONTROLA ROZROZNIALNOSCI =====
    print("\n=== TEST 16.10: Alt+F6 w dokumencie BEZ przypisow ===")
    if not start("przypisy.md"):
        return 2
    klaw("control+home", 0.8)
    z = znacznik()
    klaw("alt+f6", 1.5)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    zapisz("16.10 slychac 'No footnotes!'",
           "no footnotes" in caly,
           "komunikat: {}".format([x for x in m][:2] or "BRAK MOWY"))
    zapisz("16.10 KONTROLA ROZROZNIALNOSCI: to INNY komunikat niz przy skoku",
           "no footnotes" in caly and "inserted" not in caly,
           "brak komunikatu wstawienia w tym samym miejscu")

    # ===== 16.13 BRAMKA NA TYP PLIKU + KONTROLA DYSKRYMINACYJNA =====
    print("\n=== TEST 16.13: Control+Shift+K na pliku .txt ===")
    if not start("zwykly.txt"):
        return 2
    klaw("control+home", 0.8)
    przed = linia_biezaca()
    z = znacznik()
    klaw("control+shift+k", 1.6)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    po = linia_biezaca()
    zapisz("16.13 slychac odmowe 'work only on Markdown files'",
           "only on markdown files" in caly,
           "komunikat: {}".format([x for x in m][:2] or "BRAK MOWY"))
    zapisz("16.13 dokument NIETKNIETY",
           norm(po).strip() == norm(przed).strip(),
           "wiersz przed i po taki sam" if norm(po).strip() == norm(przed).strip()
           else "ZMIENIONY: {!r} -> {!r}".format(przed, po))
    # DETAL MUSI POKAZYWAC ZMIERZONA WARTOSC, nie stale zdanie: pierwsza wersja
    # pisala "fokus nadal w polu edycji" TAKZE gdy asercja padla, wiec z logu
    # nie dalo sie dowiedziec, gdzie fokus naprawde byl (zmierzone 27.08,
    # iteracja 3 - punkt zaraportowal UWAGA i nie powiedzial dlaczego).
    _f13 = fokus()
    _w13 = w_edytorze()
    zapisz("16.13 kontrola: NIE otworzylo sie okienko na tresc",
           _w13,
           "fokus: klasa={} nazwa={} rola={}".format(
               str(_f13.get("windowClassName")), str(_f13.get("name"))[:40],
               _f13.get("role")))

    # ===== 16.17 BRAMKA: BLOK KODU (iteracja 2) =====
    # Zmierzone naszym pandokiem: znacznik wstawiony w bloku kodu daje ZERO
    # przypisow Worda i zostaje golym tekstem, a program mowil "inserted".
    print("\n=== TEST 16.17: Control+Shift+K z kursorem W BLOKU KODU ===")
    if not start("przypisy_bramki.md"):
        return 2
    klaw("control+home", 0.8)
    for _ in range(5):
        klaw("downArrow", 0.4)
    klaw("end", 0.6)
    wiersz = linia_biezaca()
    print("   wiersz pod kursorem:", repr(wiersz))
    zapisz("16.17 kursor faktycznie stoi w bloku kodu",
           "linia kodu" in wiersz,
           "wiersz: {!r}".format(wiersz))
    z = znacznik()
    klaw("control+shift+k", 1.6)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    zapisz("16.17 slychac odmowe o bloku kodu",
           "inside a code block" in caly,
           "komunikat: {}".format([x for x in m][:2] or "BRAK MOWY"))
    zapisz("16.17 NIE otworzylo sie okienko na tresc",
           w_edytorze(), "fokus nadal w polu edycji")
    zapisz("16.17 kontrola rozroznialnosci: NIE slychac 'inserted'",
           "inserted" not in caly, "brak falszywego potwierdzenia")

    # ===== 16.18 BRAMKA: TRESC INNEGO PRZYPISU =====
    # Zmierzone: tresc przypisu wstawionego w tresci innego przypisu ZNIKA
    # z docx calkowicie - to utrata tego, co czlowiek napisal.
    print("\n=== TEST 16.18: Control+Shift+K z kursorem W TRESCI PRZYPISU ===")
    # PULAPKA POMIARU (zlapana 27.08, iteracja 2): Control+End NIE laduje na
    # tresci przypisu, tylko na PUSTYM wierszu za nia, bo plik konczy sie
    # znakiem konca wiersza.  Bramka slusznie przepuszczala, a test krzyczal
    # na kod.  Dlatego wchodzimy na wiersz definicji strzalka w gore i
    # SPRAWDZAMY, ze naprawde tam stoimy, zanim cokolwiek zmierzymy.
    klaw("control+end", 0.9)
    wiersz = linia_biezaca()
    for _ in range(3):
        if wiersz.strip().startswith("[^"):
            break
        klaw("upArrow", 0.5)
        wiersz = linia_biezaca()
    print("   wiersz pod kursorem:", repr(wiersz))
    zapisz("16.18 kursor faktycznie stoi na tresci przypisu",
           wiersz.strip().startswith("[^"),
           "wiersz: {!r}".format(wiersz))
    z = znacznik()
    klaw("control+shift+k", 1.6)
    m = mowa_od(z)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    zapisz("16.18 slychac odmowe o tresci przypisu",
           "inside the text of footnote" in caly,
           "komunikat: {}".format([x for x in m][:2] or "BRAK MOWY"))
    w_polu_18 = w_edytorze()
    zapisz("16.18 NIE otworzylo sie okienko na tresc",
           w_polu_18, "fokus nadal w polu edycji")
    # Gdyby jednak okienko wyskoczylo, MUSIMY je zamknac - inaczej nastepny
    # test mierzylby zanieczyszczony stan (tak sie wlasnie stalo za pierwszym
    # razem: klawisze poszly do okienka, a nie do dokumentu).
    if not w_polu_18:
        klaw("escape", 1.0)

    # ===== 16.19 KONTROLA DYSKRYMINACYJNA: ZWYKLE ZDANIE NADAL WSTAWIA =====
    # NAJWAZNIEJSZY test tej trojki.  Bez niego "bramka odmawia" moglo by
    # znaczyc "wstawianie zepsute wszedzie", a funkcja bylaby bezuzyteczna.
    print("\n=== TEST 16.19: KONTROLA - zwykle zdanie w TYM SAMYM pliku ===")
    klaw("control+home", 0.8)
    klaw("downArrow", 0.5)
    klaw("downArrow", 0.5)
    klaw("end", 0.6)
    wiersz = linia_biezaca()
    print("   wiersz pod kursorem:", repr(wiersz))
    z = znacznik()
    klaw("control+shift+k", 1.8)
    f = fokus()
    kl = str(f.get("windowClassName") or "")
    otwarte = "EDIT" in kl.upper() or "Insert Footnote" in str(f.get("name") or "")
    zapisz("16.19 KONTROLA: w zwyklym zdaniu okienko JEDNAK sie otwiera",
           otwarte,
           "fokus po Control+Shift+K: klasa={} nazwa={}".format(kl, str(f.get("name"))[:60]))
    if otwarte:
        z = znacznik()
        akcja("send_keys", keys="o")
        time.sleep(0.3)
        akcja("send_keys", keys="k")
        time.sleep(0.5)
        klaw("enter", 2.0)
        m = mowa_od(z, 1.4)
        for x in m:
            print("   MOWA:", x[:160])
        caly = norm(" ".join(m))
        zapisz("16.19 KONTROLA: przypis w zwyklym zdaniu ZOSTAL wstawiony",
               "inserted" in caly,
               "komunikat: {}".format([x for x in m if "ootnote" in x][:2] or "BRAK"))
    else:
        klaw("escape", 1.0)

    # ===== 16.20-16.23 PRZYPISY W PODGLADZIE POD ESCAPE (zlecenie -1) =====
    # Kasperczak: "on mowi numer przypisu, na tym numerze przypisu naciskam
    # Enter (...) on przeskakuje do tresci przypisu (...) i wraca do tego
    # tekstu".  Kursor PRZENOSIMY - jego decyzja z 27.08 16:56.
    #
    # DLACZEGO TO MUSI BYC MIERZONE NA ZYWO, a nie refleksja: podglad to
    # DRUGA kontrolka, ktora ma fokus, i to ona czyta wiersz po przesunieciu
    # kursora.  Refleksja nie powie, czy nasz komunikat nie przepadl pod
    # czytaniem wiersza przez czytnik.
    print("\n=== TEST 16.20: podglad wymawia znacznik jako 'footnote 1' ===")
    if not start("przypisy_gotowe.md"):
        return 2
    # PULAPKA POMIARU (zlapana 27.08, iteracja 3): po wejsciu w podglad kursor
    # NIE musi stac tam, gdzie stal w edytorze - podglad to druga kontrolka i
    # jej pozycja startowa jest wlasna.  Pierwsza wersja tego testu ustawiala
    # kursor w edytorze, wchodzila w podglad i mierzyla WIERSZ PIERWSZY,
    # krzyczac na kod, ktory dzialal.  Wiec: wchodzimy w podglad, a POTEM
    # DOCHODZIMY strzalka do wiersza ze znacznikiem i ASERTUJEMY, ze tam
    # jestesmy, zanim cokolwiek zmierzymy.  Tak samo robi czlowiek.
    klaw("control+home", 0.8)
    z = znacznik()
    klaw("escape", 2.2)        # wejscie w podglad
    m = mowa_od(z, 1.2)
    for x in m:
        print("   MOWA(podglad):", x[:160])
    zapisz("16.20 podglad sie otworzyl",
           any("preview" in norm(x) for x in m),
           "mowa startowa podgladu: {}".format([x[:60] for x in m][:2] or "BRAK MOWY"))

    klaw("control+home", 0.9)
    wiersz = linia_biezaca()
    for _ in range(8):
        if "footnote 1" in norm(wiersz) or "[^" in wiersz:
            break
        klaw("downArrow", 0.45)
        wiersz = linia_biezaca()
    print("   wiersz w podgladzie:", repr(wiersz))
    zapisz("16.20 w podgladzie znacznik brzmi 'footnote 1', nie '[^1]'",
           "footnote 1" in norm(wiersz) and "[^" not in wiersz,
           "wiersz: {!r}".format(wiersz))
    zapisz("16.20 KONTROLA: to wiersz ZDANIA, nie wiersz tresci na koncu",
           "zdanie z przypisem" in norm(wiersz),
           "wiersz: {!r}".format(wiersz))

    print("\n=== TEST 16.21: Enter na przypisie PRZENOSI do tresci ===")
    klaw("end", 0.6)           # kursor na koncu zdania ze znacznikiem
    z = znacznik()
    klaw("enter", 1.8)
    m = mowa_od(z, 1.4)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    wiersz_po = linia_biezaca()
    print("   wiersz po Enter:", repr(wiersz_po))
    zapisz("16.21 slychac tresc przypisu z numerem",
           "tresc pierwszego przypisu" in caly and "footnote 1" in caly,
           "komunikat: {}".format([x for x in m if "ootnote" in x][:2] or "BRAK MOWY"))
    zapisz("16.21 kursor PRZENIOSL sie na tresc przypisu",
           "tresc pierwszego przypisu" in norm(wiersz_po),
           "wiersz: {!r}".format(wiersz_po))

    print("\n=== TEST 16.22: ponowny Enter WRACA do zdania ===")
    z = znacznik()
    klaw("enter", 1.8)
    m = mowa_od(z, 1.4)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    wiersz_wroc = linia_biezaca()
    print("   wiersz po powrocie:", repr(wiersz_wroc))
    zapisz("16.22 slychac zdanie i 'footnote 1 in text'",
           "in text" in caly,
           "komunikat: {}".format([x for x in m if "in text" in norm(x)][:2] or "BRAK MOWY"))
    zapisz("16.22 kursor WROCIL do zdania ze znacznikiem",
           "zdanie z przypisem" in norm(wiersz_wroc),
           "wiersz: {!r}".format(wiersz_wroc))

    # KONTROLA DYSKRYMINACYJNA: Enter w wierszu BEZ przypisu nie moze udawac
    # skoku.  Bez tego "Enter dziala" moglo by znaczyc "Enter dziala wszedzie
    # tak samo", czyli funkcja nie rozpoznaje niczego.
    print("\n=== TEST 16.23: KONTROLA - Enter w wierszu BEZ przypisu ===")
    klaw("control+home", 0.8)
    for _ in range(4):
        klaw("downArrow", 0.4)
    wiersz = linia_biezaca()
    print("   wiersz pod kursorem:", repr(wiersz))
    z = znacznik()
    klaw("enter", 1.6)
    m = mowa_od(z, 1.2)
    for x in m:
        print("   MOWA:", x[:160])
    caly = norm(" ".join(m))
    wiersz_po_k = linia_biezaca()
    zapisz("16.23 KONTROLA: nie slychac tresci przypisu",
           "tresc pierwszego przypisu" not in caly,
           "komunikat: {}".format([x for x in m][:2] or "BRAK MOWY"))
    zapisz("16.23 KONTROLA: kursor NIE poszedl na tresc przypisu",
           "tresc pierwszego przypisu" not in norm(wiersz_po_k),
           "wiersz: {!r}".format(wiersz_po_k))
    # Wyjscie z podgladu, zeby nie zanieczyscic niczego dalej.
    klaw("escape", 1.2)

    zamknij_edsharp()

    print("\n" + "=" * 60)
    ok = sum(1 for _, w, _ in wyniki if w)
    print("WYNIK: {}/{} OK".format(ok, len(wyniki)))
    for opis, w, detal in wyniki:
        if not w:
            print("  UWAGA: {} -> {}".format(opis, detal))
    return 0 if ok == len(wyniki) else 1


if __name__ == "__main__":
    sys.exit(main())
