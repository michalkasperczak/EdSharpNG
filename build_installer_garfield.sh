#!/usr/bin/env bash
# Budowa EdSharpNG + instalatora Inno Setup na Garfieldzie, BEZ maszyny glownej.
#
# Uzycie:
#   ./build_installer_garfield.sh 5.0.2
#
# Wynik:
#   dist/EdSharpNG_Setup_<wersja>.exe
#
# Wymaga (sprawdzone 13.08.2026 na Garfieldzie):
# - Windows .NET Framework csc.exe + jsc.exe (sa w C:\Windows\Microsoft.NET),
# - UIAutomationProvider/Types w GAC,
# - Inno Setup 6 per-user (sciezka szukana automatycznie w profilach uzytkownikow;
#   inna na Garfieldzie "g", inna na Hermesie "Michal"),
# - uruchomienie z WSL, repo na /mnt/d/projekty/edsharp-pr.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION="${1:-}"
STAGE="/mnt/c/EdSharp"
# SCIEZKA DO INNO SETUP SZUKANA, NIE WPISANA NA SZTYWNO (11.09.2026).  Stalo tu
# C:\Users\g\... - profil Garfielda.  Na Hermesie uzytkownik nazywa sie Michal,
# wiec skrypt konczyl sie "brak Inno Setup" mimo poprawnie zainstalowanego
# programu.  Przeszukujemy profile i standardowe katalogi Program Files;
# zmienna ISCC z otoczenia ma pierwszenstwo, gdyby ktos mial go gdzie indziej.
ISCC="${ISCC:-}"
if [[ -z "$ISCC" || ! -x "$ISCC" ]]; then
    for kandydat in \
        /mnt/c/Users/*/AppData/Local/Programs/"Inno Setup 6"/ISCC.exe \
        "/mnt/c/Program Files (x86)/Inno Setup 6/ISCC.exe" \
        "/mnt/c/Program Files/Inno Setup 6/ISCC.exe"; do
        if [[ -x "$kandydat" ]]; then ISCC="$kandydat"; break; fi
    done
fi
LOG_BUILD="$ROOT/BuildEdSharp.log"
LOG_ISCC="/tmp/edsharp-iscc-${VERSION:-brak}.log"

if [[ -z "$VERSION" || ! "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Uzycie: $0 <wersja, np. 5.0.2>" >&2
    exit 2
fi
if [[ ! -x /mnt/c/Windows/System32/cmd.exe ]]; then
    echo "BLAD: brak cmd.exe Windows pod /mnt/c/Windows/System32/cmd.exe" >&2
    exit 1
fi
if [[ ! -x "$ISCC" ]]; then
    echo "BLAD: brak Inno Setup: $ISCC" >&2
    echo "Zainstaluj: winget install --id JRSoftware.InnoSetup -e" >&2
    exit 1
fi

cd "$ROOT"
echo "[1/6] Budowa EdSharpNG.exe..."
# INTEROP WSL->cmd.exe PADA NIEZALEZNIE OD BUILDU (zmierzone 28.08 i 31.08.2026):
# polecenie zwraca kod 1 z "UtilAcceptVsock: accept4 failed 110", a kompilacja
# albo sie wykonala, albo w ogole nie wystartowala.  Kod wyjscia nie rozstrzyga
# NICZEGO, wiec go nie sprawdzamy - rozstrzyga SWIEZOSC BINARKI wobec zrodla,
# a to jest kontrola mocniejsza: lapie tez przebieg, w ktorym cmd.exe milczal.
# Do trzech prob; binarka swiezsza od EdSharp.cs przerywa petle od razu, wiec
# gotowy build z tej samej sesji nie jest budowany po raz drugi.
for proba in 1 2 3; do
    if [[ EdSharpNG.exe -nt EdSharp.cs ]]; then
        echo "  binarka jest swiezsza niz zrodla - nie buduje ponownie"
        break
    fi
    echo "  proba buildu $proba..."
    /mnt/c/Windows/System32/cmd.exe /c BuildEdSharp.cmd || true
    sleep 5
done
if [[ ! EdSharpNG.exe -nt EdSharp.cs ]]; then
    echo "BLAD: binarka STARSZA niz EdSharp.cs po trzech probach - NIE WYSYLAJ paczki." >&2
    exit 3
fi
if grep -aEq 'error (CS|JS)[0-9]+' "$LOG_BUILD"; then
    echo "BLAD kompilacji - patrz $LOG_BUILD" >&2
    grep -aE 'error (CS|JS)[0-9]+' "$LOG_BUILD" >&2 || true
    exit 1
fi
[[ -s EdSharpNG.exe ]] || {
    echo "BLAD: build nie utworzyl EdSharpNG.exe" >&2
    exit 1
}

# Nie wydawaj dwoch roznych buildow pod tym samym numerem.
OUT="$ROOT/dist/EdSharpNG_Setup_${VERSION}.exe"
if [[ -e "$OUT" ]]; then
    echo "BLAD: $OUT juz istnieje. Podbij wersje." >&2
    exit 1
fi

echo "[2/6] Budowa dodatku NVDA ze sprawdzaniem pisowni..."
ADDON_SRC="$ROOT/NVDAAddon/edsharpng-spellcheck"
ADDON_OUT="$ROOT/EdSharpNG-spellcheck.nvda-addon"
[[ -f "$ADDON_SRC/manifest.ini" && -f "$ADDON_SRC/readme.html" \
   && -f "$ADDON_SRC/appModules/edsharpng.py" ]] || {
    echo "BLAD: niekompletne zrodla dodatku NVDA w $ADDON_SRC" >&2
    exit 1
}
rm -f "$ADDON_OUT"
(
    cd "$ADDON_SRC"
    zip -q -r "$ADDON_OUT" manifest.ini readme.html appModules \
        -x '*/__pycache__/*' '*.pyc'
)
# Minimalna kontrola struktury dodatku - te trzy pliki sa wymagane przez NVDA.
for rel in manifest.ini readme.html appModules/edsharpng.py; do
    unzip -Z1 "$ADDON_OUT" | grep -Fxq "$rel" || {
        echo "BLAD: dodatek NVDA nie zawiera $rel" >&2
        exit 1
    }
done

echo "[3/6] Staging do C:\\EdSharp (SourceDir zaszyty w .iss)..."
rm -rf "$STAGE"
mkdir -p "$STAGE"
# Kopiujemy sledzone pliki, zeby staging nie dostal .git, logow ani starych buildow.
# Pliki, ktorych NIE MA na dysku, pomijamy z ostrzezeniem, a nie wywalamy build:
# repo dziedziczy z upstream dwie binarki-smieci o nazwach z GUID
# (<guid>_EdSharp.exe, commit "Initial 5.0 beta."), a antywirus Windows kasuje
# je z dysku - 26.08.2026 build 5.0.25 padl na "cp: cannot open ... Invalid
# argument" wlasnie z tego powodu. Zadna z nich nie jest wymieniona w
# EdSharp_Setup.iss, wiec instalatorowi nie sa potrzebne. Kompletnosc
# artefaktow i tak sprawdza krok [4/6] po liscie z .iss.
while IFS= read -r -d '' rel; do
    src="$ROOT/$rel"
    dst="$STAGE/$rel"
    if [[ ! -e "$src" ]]; then
        echo "  UWAGA: pomijam brakujacy plik z repo: $rel" >&2
        continue
    fi
    mkdir -p "$(dirname "$dst")"
    cp -a "$src" "$dst"
done < <(git ls-files -z)
# Artefakty buildu sa ignorowane, ale wymagane przez instalator.
cp -a EdSharpNG.exe EdSharpNG-spellcheck.nvda-addon "$STAGE/"
# Zasoby pobrane best-effort przez BuildEdSharp.cmd (ignorowane przez git).
for rel in Ude.dll Convert; do
    [[ -e "$ROOT/$rel" ]] && cp -a "$ROOT/$rel" "$STAGE/"
done

# Wersje zmieniamy TYLKO w stagingu - build nie brudzi repo.
python3 - "$STAGE/EdSharp_Setup.iss" "$VERSION" <<'PY'
import re, sys
path, ver = sys.argv[1], sys.argv[2]
raw = open(path, 'rb').read()
# Zachowaj CRLF i kodowanie bajt-w-bajt; wersje sa ASCII.
for key in (b'AppVersion', b'VersionInfoVersion'):
    raw, n = re.subn(rb'(?m)^' + key + rb'=.*?\r?$', key + b'=' + ver.encode(), raw)
    if n != 1:
        raise SystemExit(f'BLAD: oczekiwano jednej linii {key.decode()}, znaleziono {n}')
# AppVerName to nazwa, ktora WIDZI uzytkownik (instalator, Panel sterowania).
# Bez tego instalator 5.0.12 przedstawialby sie jako poprzednia wersja.
raw, n = re.subn(rb'(?m)^AppVerName=.*?\r?$', b'AppVerName=EdSharpNG ' + ver.encode() + b' (beta)', raw)
if n != 1:
    raise SystemExit(f'BLAD: oczekiwano jednej linii AppVerName, znaleziono {n}')
open(path, 'wb').write(raw)
PY

echo "[4/6] Kontrola wymaganych plikow instalatora..."
python3 - "$STAGE" <<'PY'
import os, re, sys
stage = sys.argv[1]
iss = open(os.path.join(stage, 'EdSharp_Setup.iss'), encoding='utf-8', errors='replace').read()
missing = []
for line in iss.splitlines():
    s = line.strip()
    if s.startswith(';') or 'Source:' not in s or 'skipifsourcedoesntexist' in s.lower():
        continue
    m = re.search(r'Source:\s*"([^"]+)"', s)
    if not m:
        continue
    rel = m.group(1).replace('\\', '/')
    if '*' in rel:
        continue
    if not os.path.isfile(os.path.join(stage, rel)):
        missing.append(rel)
if missing:
    raise SystemExit('BLAD: brak wymaganych plikow: ' + ', '.join(missing))
print('Wszystkie wymagane pliki sa obecne.')
PY
# Ten addon jest oznaczony w .iss jako opcjonalny (dla zgodnosci z wersja
# upstream), ale w NASZYM EdSharpNG jest funkcja produktu - pipeline traktuje
# go jako wymagany. Bez tego ISCC konczy sukcesem i cicho buduje wybrakowany
# instalator (znalezione pomiarem na pierwszym buildzie Garfielda 5.0.3).
[[ -s "$STAGE/EdSharpNG-spellcheck.nvda-addon" ]] || {
    echo "BLAD: brak wymaganego dodatku EdSharpNG-spellcheck.nvda-addon w stagingu" >&2
    exit 1
}

echo "[5/6] Kompilacja instalatora Inno Setup $VERSION..."
"$ISCC" 'C:\EdSharp\EdSharp_Setup.iss' >"$LOG_ISCC" 2>&1
if ! grep -q 'Successful compile' "$LOG_ISCC"; then
    echo "BLAD Inno Setup - patrz $LOG_ISCC" >&2
    tail -30 "$LOG_ISCC" >&2
    exit 1
fi

mkdir -p "$ROOT/dist"
cp -a "$STAGE/EdSharpNG_Setup.exe" "$OUT"

echo "[6/6] Weryfikacja swiezosci i tozsamosci binarki..."
cmp -s "$ROOT/EdSharpNG.exe" "$STAGE/EdSharpNG.exe" || {
    echo "BLAD: staging zawiera inna binarke niz swiezy build" >&2
    exit 1
}
[[ "$OUT" -nt "$ROOT/EdSharpNG.exe" ]] || {
    echo "BLAD: instalator nie jest nowszy niz binarka" >&2
    exit 1
}

sha256sum "$ROOT/EdSharpNG.exe" "$OUT"
echo "GOTOWE: $OUT"
