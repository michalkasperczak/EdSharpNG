#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Cotygodniowe sprawdzenie, co nowego w ORYGINALNYM EdSharpie 5 Jamala Mazrui.

Zlecone przez Michala Kasperczaka 12.09.2026: "Zapisz na stale, co tydzien
sprawdz co nowego w EdSharpie 5 tym oryginalnym."

WAZNE USTALENIA, ktore ten skrypt utrwala:

* Zywe zrodlo to jamalmazrui/EdSharp (pushed_at 2026-09-08 i dalej).
  EmpowermentZone/EdSharp jest MARTWE od 2017-10-30 - nie sprawdzac go,
  poza jednym czujnikiem na wypadek, gdyby ozylo.
* Nie wolno pobierac tarballa ani robic git clone - urywa sie w polowie
  (5 prob, EXIT 28/33/143). Pojedyncze pliki z raw.githubusercontent.com
  schodza bez problemu.
* Listy plikow pobierac BEZ recursive=1 - z recursive odpowiedz jest za
  duza i rwie sie na IncompleteRead.

Raport idzie na wyjscie standardowe. Cron dostawia go Michalowi na Telegram.
Skrypt NIE zmienia niczego w naszym kodzie - tylko melduje.
"""

import json
import os
import re
import subprocess
import sys
import time
import urllib.request

REPO_ZYWE = "jamalmazrui/EdSharp"
REPO_MARTWE = "EmpowermentZone/EdSharp"
STAN = os.path.expanduser("~/.hermes/stan/edsharp_oryginal.json")
NASZ_KOD = os.path.expanduser("~/projekty/edsharp")

# Pliki zrodlowe warte sledzenia. Reszta repo to binarki i dokumentacja.
PLIKI_KODU = ["EdSharp.cs", "Lbc.cs", "Say.cs", "Inix.cs", "Web.cs", "KeyMap.cs"]


def siec(url, naglowki=None, proby=4, limit_s=60):
    """Pobierz adres, znoszac zerwania - sciagniecia z GitHuba rwa sie tu regularnie."""
    ostatni = ""
    for i in range(proby):
        try:
            h = {"User-Agent": "curl"}
            if naglowki:
                h.update(naglowki)
            r = urllib.request.Request(url, headers=h)
            return urllib.request.urlopen(r, timeout=limit_s).read()
        except Exception as e:
            ostatni = str(e)
            time.sleep(3)
    raise IOError("nie zeszlo po %d probach: %s" % (proby, ostatni))


def api(sciezka):
    b = siec("https://api.github.com/" + sciezka,
             {"Accept": "application/vnd.github+json"})
    return json.loads(b.decode())


def raw(repo, plik):
    b = siec("https://raw.githubusercontent.com/%s/master/%s" % (repo, plik))
    return b.decode("utf-8", errors="replace")


def wczytaj_stan():
    if os.path.exists(STAN):
        try:
            return json.load(open(STAN, encoding="utf-8"))
        except Exception:
            pass
    return {}


def zapisz_stan(d):
    os.makedirs(os.path.dirname(STAN), exist_ok=True)
    json.dump(d, open(STAN, "w", encoding="utf-8"), ensure_ascii=False, indent=1)


def naglowki_historii(tekst):
    """Naglowki wersji z History.md - to jego dziennik zmian."""
    return re.findall(r'^##\s+(.+?)\s*$', tekst, flags=re.M)


def linie(sciezka):
    try:
        return len(open(sciezka, encoding="utf-8", errors="replace").read().splitlines())
    except Exception:
        return None


def main():
    stary = wczytaj_stan()
    nowy = {"sprawdzono": time.strftime("%Y-%m-%d %H:%M")}
    mowa = []          # zdania do raportu
    cos_nowego = False

    # --- 1. Czy w ogole cos ruszyl ---
    try:
        d = api("repos/" + REPO_ZYWE)
    except Exception as e:
        print("Nie udalo sie sprawdzic oryginalnego EdSharpa: %s" % e)
        print("(Adres: github.com/%s - sprawdzic recznie przy okazji.)" % REPO_ZYWE)
        return 1

    nowy["pushed_at"] = d.get("pushed_at")
    poprzednie = stary.get("pushed_at")

    if poprzednie and nowy["pushed_at"] == poprzednie:
        # CISZA jest tu celowa. Skrypt chodzi co tydzien pod cronem i jego
        # wyjscie idzie prosto do Michala; cotygodniowe "bez zmian" byloby
        # samym halasem, a on wyraznie prosi o efekty, nie o meldunki z
        # przebiegu. Brak zmian = brak wiadomosci. Do pliku stanu i tak
        # dopisujemy date sprawdzenia, zeby bylo widac, ze czujnik zyje.
        stary.update(nowy)
        zapisz_stan(stary)
        if os.environ.get("EDSHARP_GADAJ"):   # do recznego sprawdzenia czujnika
            print("Oryginalny EdSharp Jamala: bez zmian od %s." % (poprzednie or "?")[:10])
        return 0

    # --- 2. Commity od ostatniego razu ---
    try:
        cs = api("repos/%s/commits?per_page=15" % REPO_ZYWE)
        widziane = set(stary.get("commity", []))
        swieze = [c for c in cs if c["sha"] not in widziane]
        nowy["commity"] = [c["sha"] for c in cs]
        if swieze and widziane:
            cos_nowego = True
            mowa.append("Nowe zmiany w oryginale (%d):" % len(swieze))
            for c in swieze[:10]:
                mowa.append("  %s  %s" % (c["commit"]["author"]["date"][:10],
                                          c["commit"]["message"].split("\n")[0][:80]))
        elif not widziane:
            nowy["commity"] = [c["sha"] for c in cs]
    except Exception as e:
        mowa.append("Listy zmian nie udalo sie pobrac (%s)." % e)

    # --- 3. Nowy wpis w jego dzienniku - najtresciwsze zrodlo ---
    try:
        h = raw(REPO_ZYWE, "History.md")
        nagl = naglowki_historii(h)
        nowy["historia"] = nagl[:12]
        stare_nagl = stary.get("historia", [])
        swieze_nagl = [x for x in nagl if x not in stare_nagl]
        if swieze_nagl and stare_nagl:
            cos_nowego = True
            mowa.append("")
            mowa.append("Nowe wpisy w jego dzienniku zmian:")
            for x in swieze_nagl:
                mowa.append("  " + x)
            # Dolacz tresc najnowszego wpisu - tam jest CO i DLACZEGO.
            i = h.find("## " + swieze_nagl[0])
            if i >= 0:
                j = h.find("\n## ", i + 4)
                tresc = h[i:j if j > 0 else len(h)]
                tresc = re.sub(r'\n{3,}', '\n\n', tresc).strip()
                mowa.append("")
                mowa.append("Najnowszy wpis w calosci:")
                mowa.append(tresc[:2500])
    except Exception as e:
        mowa.append("Dziennika History.md nie udalo sie pobrac (%s)." % e)

    # --- 4. Ktore pliki kodu urosly albo sie zmienily ---
    try:
        t = api("repos/%s/git/trees/master" % REPO_ZYWE)   # BEZ recursive
        rozmiary = dict((x["path"], x.get("size"))
                        for x in t.get("tree", []) if x["type"] == "blob")
        nowy["rozmiary"] = dict((p, rozmiary.get(p)) for p in PLIKI_KODU)
        stare_r = stary.get("rozmiary", {})
        zmienione = [p for p in PLIKI_KODU
                     if stare_r.get(p) is not None
                     and rozmiary.get(p) is not None
                     and rozmiary[p] != stare_r[p]]
        if zmienione:
            cos_nowego = True
            mowa.append("")
            mowa.append("Zmienione pliki kodu (jego rozmiar, nasze linie dla porownania):")
            for p in zmienione:
                nl = linie(os.path.join(NASZ_KOD, p))
                mowa.append("  %-12s %d bajtow (bylo %d)%s"
                            % (p, rozmiary[p], stare_r[p],
                               "; u nas %d linii" % nl if nl else "; u nas TEGO PLIKU NIE MA"))
    except Exception as e:
        mowa.append("Listy plikow nie udalo sie pobrac (%s)." % e)

    # --- 5. Czujnik na martwe repo - gdyby kiedys ozylo ---
    try:
        dm = api("repos/" + REPO_MARTWE)
        nowy["martwe_pushed_at"] = dm.get("pushed_at")
        if (dm.get("pushed_at") or "")[:4] > "2017":
            cos_nowego = True
            mowa.append("")
            mowa.append("UWAGA: stare repozytorium %s ozylo (%s) - warto zajrzec."
                        % (REPO_MARTWE, dm.get("pushed_at")))
    except Exception:
        pass

    zapisz_stan(nowy if not stary else dict(list(stary.items()) + list(nowy.items())))

    if not stary:
        print("Zalozony pierwszy zapis stanu oryginalnego EdSharpa Jamala.")
        print("Ostatnia jego zmiana: %s. Od nastepnego tygodnia melduje tylko roznice."
              % (nowy.get("pushed_at") or "?")[:10])
        return 0

    if not cos_nowego and not mowa:
        print("Oryginalny EdSharp Jamala: nic nowego.")
        return 0

    print("Co nowego w oryginalnym EdSharpie Jamala (%s):"
          % (nowy.get("pushed_at") or "?")[:10])
    print("")
    print("\n".join(mowa))
    print("")
    print("Zrodlo: github.com/%s" % REPO_ZYWE)
    return 0


if __name__ == "__main__":
    sys.exit(main())
