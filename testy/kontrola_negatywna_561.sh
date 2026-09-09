#!/usr/bin/env bash
# Kontrola negatywna dla 5.0.61: zapis .rtf po PROWENIENCJI dokumentu, nie po
# rozszerzeniu; rozroznienie pozycji listy eksportu o tej samej nazwie;
# konwerter rtf2md w tablicy Import.
#
# Uzycie:
#   ./kontrola_negatywna_561.sh [rewizja_odniesienia] [rewizja_mierzona]
# Bez drugiego argumentu mierzone jest DRZEWO ROBOCZE (i skrypt to mowi).
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ODNIESIENIE="${1:-eff0c94}"
MIERZONA="${2:-}"

OK=0; ZLE=0
ok() { OK=$((OK+1)); echo "OK: $1"; }
zle() { ZLE=$((ZLE+1)); echo "ZLE: $1"; }
sprawdz() { if [[ "$1" == "tak" ]]; then ok "$2"; else zle "$2"; fi }

if [[ -n "$MIERZONA" ]]; then
    KAT="/tmp/kn561_mierzona"
    rm -rf "$KAT"; mkdir -p "$KAT"
    for plik in EdSharp.cs EdSharp.ini; do
        git -C "$ROOT" show "$MIERZONA:$plik" > "$KAT/$plik" || exit 1
    done
    cd "$KAT"
    echo "MIERZONA REWIZJA: $MIERZONA"
else
    cd "$ROOT"
    echo "UWAGA: mierze DRZEWO ROBOCZE (rewizja odniesienia: $ODNIESIENIE)"
fi
echo "REWIZJA ODNIESIENIA: $ODNIESIENIE"
echo

echo "--- 1. Pole proweniencji istnieje i jest USTAWIANE, nie tylko zadeklarowane"
# Osierocone pole (zadeklarowane, nigdy nie ustawione) dalo by zielony build i
# zachowanie BEZ ZMIAN - lekcja z 5.0.44.
sprawdz "$( [[ $(grep -c 'public bool IsRichTextDocument' EdSharp.cs) -eq 1 ]] && echo tak )" \
    "pole IsRichTextDocument zadeklarowane dokladnie raz"
LICZBA_USTAWIEN=$(grep -c 'IsRichTextDocument = \(true\|false\)' EdSharp.cs)
sprawdz "$( [[ $LICZBA_USTAWIEN -ge 4 ]] && echo tak )" \
    "pole jest USTAWIANE na co najmniej czterech drogach wczytania (jest: $LICZBA_USTAWIEN)"
sprawdz "$(grep -q 'this.IsRichTextDocument = true;' EdSharp.cs && echo tak)" \
    "galaz rich text ustawia flage na true"
sprawdz "$(grep -q 'this.IsRichTextDocument = false;' EdSharp.cs && echo tak)" \
    "galaz tekstowa ustawia flage na false - bez tego dokument raz otwarty bogato zostawal bogaty na zawsze"

echo
echo "--- 2. Zapis PYTA O FLAGE, nie tylko o rozszerzenie"
sprawdz "$(grep -q 'Path.GetExtension(sFile).ToLower() == ".rtf" \&\& this.IsRichTextDocument' EdSharp.cs && echo tak)" \
    "zwykly zapis rozstrzyga po rozszerzeniu ORAZ po flagach"
sprawdz "$(grep -q 'Path.GetExtension(sFile).ToLower() == ".rtf" \&\& child.IsRichTextDocument' EdSharp.cs && echo tak)" \
    "Save Copy rozstrzyga tak samo"
# KONTROLA WAZNOSCI: w rewizji odniesienia warunek pytal WYLACZNIE o rozszerzenie.
STARY=$(git -C "$ROOT" show "$ODNIESIENIE:EdSharp.cs" | grep -c 'if (Path.GetExtension(sFile).ToLower() == ".rtf") this.RTB.SaveFile')
sprawdz "$( [[ $STARY -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE zapis pytal TYLKO o rozszerzenie (trafien: $STARY)"
sprawdz "$( [[ $(grep -c 'if (Path.GetExtension(sFile).ToLower() == ".rtf") this.RTB.SaveFile' EdSharp.cs) -eq 0 ]] && echo tak )" \
    "stary warunek bez flagi ZNIKNAL"
# Save Copy pytal o PODCIAG ".rtf" w calej sciezce - lapal tez "notatki.rtf.txt".
STARY_KOPIA=$(git -C "$ROOT" show "$ODNIESIENIE:EdSharp.cs" | grep -c 'sFile.ToLower().IndexOf(".rtf") >= 0')
sprawdz "$( [[ $STARY_KOPIA -eq 1 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE Save Copy pytal o PODCIAG (trafien: $STARY_KOPIA)"
sprawdz "$( [[ $(grep -c 'sFile.ToLower().IndexOf(".rtf") >= 0' EdSharp.cs) -eq 0 ]] && echo tak )" \
    "dopasowanie po podciagu w Save Copy zniknelo"

echo
echo "--- 3. Droga tekstowa dla pliku .rtf ISTNIEJE i MOWI"
# Opis mowiony musi opisywac ZMIENIONE zachowanie, inaczej program mowi jedno,
# a pomoc drugie.
sprawdz "$(grep -q 'Saving as plain text, not rich text' EdSharp.md && echo tak)" \
    "EdSharp.md opisuje nowy komunikat zapisu"
sprawdz "$(grep -q 'bundled Pandoc' EdSharp.md && echo tak)" \
    "EdSharp.md opisuje konwersje .rtf do Markdown przez pandoca"
sprawdz "$(grep -q 'Saving as plain text, not rich text' EdSharp.cs && echo tak)" \
    "komunikat o zapisie tekstem jest w kodzie"
sprawdz "$(grep -q 'else if (Path.GetExtension(sFile).ToLower() == ".rtf") {' EdSharp.cs && echo tak)" \
    "istnieje osobna galaz dla pliku .rtf z trescia tekstowa"
# Bez faktycznego zapisu komunikat byl by klamstwem: mowilby o zapisie, ktory
# sie nie stal.  Ta asercja pilnuje, ze galaz naprawde zapisuje plik.
LINIA_KOM=$(grep -n 'Saving as plain text, not rich text' EdSharp.cs | head -1 | cut -d: -f1)
OGON=$(sed -n "${LINIA_KOM},$((LINIA_KOM+12))p" EdSharp.cs)
sprawdz "$(echo "$OGON" | grep -q 'Util.String2File(sPlain, sFile, ref enPlain);' && echo tak)" \
    "galaz z komunikatem NAPRAWDE zapisuje plik (komunikat nie jest sam)"
sprawdz "$(echo "$OGON" | grep -q 'Util.Convert2WinLineBreak(sPlain)' && echo tak)" \
    "ta galaz zachowuje windowsowe konce wiersza (ustalenie edsharpng-16)"

echo
echo "--- 4. Rozroznienie dwoch pozycji listy eksportu o tej samej nazwie"
sprawdz "$(grep -q '" (converted)"' EdSharp.cs && echo tak)" "przyrostek (converted) jest w kodzie"
sprawdz "$(grep -q '" (as shown)"' EdSharp.cs && echo tak)" "przyrostek (as shown) jest w kodzie"
# Rozroznienie musi trafiac w NAZWE widoczna dla czytnika, nie w wartosc, bo
# wartosc idzie do wyboru konwertera i jej zmiana zepsula by eksport.
LINIA_ROZ=$(grep -n '" (converted)"' EdSharp.cs | head -1 | cut -d: -f1)
BLOK=$(sed -n "$((LINIA_ROZ-12)),$((LINIA_ROZ+3))p" EdSharp.cs)
sprawdz "$(echo "$BLOK" | grep -q 'aDisplay\[i\] = aDisplay\[i\] + " (converted)"' && echo tak)" \
    "przyrostek trafia w aDisplay (nazwa), NIE w aValues (wartosc konwertera)"
sprawdz "$(echo "$BLOK" | grep -q 'if (iSame < 2) continue;' && echo tak)" \
    "przyrostek dostaja WYLACZNIE pozycje o powtorzonej nazwie - inaczej kazda pozycja zmienila by nazwe"
sprawdz "$( [[ $(grep -c 'aValues\[i\].IndexOf(.2.) >= 0' EdSharp.cs) -eq 1 ]] && echo tak )" \
    "rozroznienie idzie po obecnosci cyfry 2 w wartosci, czyli po tym, czy jest konwerter"

echo
echo "--- 5. Konwerter rtf2md w tablicy Import (ustalenie edsharpng-21)"
IMPORT=$(sed -n '/^\[Import\]/,/^\[Export\]/p' EdSharp.ini)
sprawdz "$(echo "$IMPORT" | grep -q '^rtf2md=' && echo tak)" "rtf2md jest w sekcji Import"
sprawdz "$(echo "$IMPORT" | grep -q '^rtf2md=.*-f rtf -t gfm' && echo tak)" "rtf2md konwertuje rtf na gfm"
sprawdz "$(echo "$IMPORT" | grep -q '^rtf2md=.*pandoc.exe' && echo tak)" "rtf2md uzywa pandoca, ktory jest w paczce"
# KONTROLA WAZNOSCI: w rewizji odniesienia tego wpisu NIE BYLO.
STARY_IMP=$(git -C "$ROOT" show "$ODNIESIENIE:EdSharp.ini" | sed -n '/^\[Import\]/,/^\[Export\]/p' | grep -c '^rtf2md=')
sprawdz "$( [[ $STARY_IMP -eq 0 ]] && echo tak )" \
    "kontrola waznosci: w $ODNIESIENIE rtf2md NIE BYLO (trafien: $STARY_IMP)"
# rtf2txt SWIADOMIE nie dodane: dalo by DRUGA pozycje o nazwie "txt" na liscie
# importu .rtf, czyli dokladnie defekt naprawiany w sekcji 4.
sprawdz "$( [[ $(echo "$IMPORT" | grep -c '^rtf2txt=') -eq 0 ]] && echo tak )" \
    "rtf2txt swiadomie NIE dodane (dalo by druga pozycje o nazwie txt)"

echo
echo "--- 6. Nienaruszalnosc: nic z 5.0.55-5.0.60 nie wrocilo ani nie wypadlo"
sprawdz "$(grep -q 'MigrateDefaultExtensionToMarkdown' EdSharp.cs && echo tak)" "migracja rozszerzenia z 5.0.59 nadal jest"
sprawdz "$(grep -q 'HandleRedoAliasKey' EdSharp.cs && echo tak)" "Control+Y nadal ponawia (5.0.58)"
sprawdz "$(grep -q 'PasteEdSharpMarkdownFormat' EdSharp.cs && echo tak)" "wklejanie Markdown z 5.0.60 nadal jest"
sprawdz "$(grep -q 'FindMarkdownFootnoteMarkerIndex' EdSharp.cs && echo tak)" "helper skoku po przypisach nadal jest"
sprawdz "$(grep -q 'ExtensionDefault="md"' EdSharp.ini && echo tak)" "domyslne rozszerzenie nadal md"

echo
echo "--- 7. Konce wiersza pliku zrodlowego (mierzone na DYSKU, nie z git show)"
if [[ -z "$MIERZONA" ]]; then
    LINII=$(wc -l < EdSharp.cs)
    CR=$(grep -c $'\r$' EdSharp.cs)
    sprawdz "$( [[ "$LINII" -eq "$CR" ]] && echo tak )" "wszystkie $LINII linii EdSharp.cs maja CRLF (CR: $CR)"
    LINII_INI=$(wc -l < EdSharp.ini)
    CR_INI=$(grep -c $'\r$' EdSharp.ini)
    sprawdz "$( [[ "$LINII_INI" -eq "$CR_INI" ]] && echo tak )" "wszystkie $LINII_INI linii EdSharp.ini maja CRLF (CR: $CR_INI)"
    # EdSharp.md TRAFIA DO PACZKI PROSTO Z DYSKU, nie z gita, wiec plik z golymi
    # LF laduje u uzytkownika w tej postaci.  Zmierzone 02.09.2026: paczka 5.0.60
    # dostala ten plik z 843 golymi LF (staging C:\EdSharp/EdSharp.md), mimo
    # eol=crlf w .gitattributes - git normalizuje dopiero, gdy sam plik dotknie.
    # Dlatego pomiar idzie po pliku Z DYSKU, tak jak dla EdSharp.cs.
    LINII_MD=$(wc -l < EdSharp.md)
    CR_MD=$(grep -c $'\r$' EdSharp.md)
    sprawdz "$( [[ "$LINII_MD" -eq "$CR_MD" ]] && echo tak )" "wszystkie $LINII_MD linii EdSharp.md maja CRLF (CR: $CR_MD)"
    # KONTROLA WAZNOSCI tego pomiaru: sposob liczenia musi UMIEC pokazac brak CRLF.
    printf 'a\nb\n' > /tmp/kn561_probka_lf
    sprawdz "$( [[ $(grep -c $'\r$' /tmp/kn561_probka_lf) -eq 0 ]] && echo tak )" \
        "kontrola waznosci: licznik CR daje zero na probce z golymi LF"
else
    echo "  pomijam: git normalizuje konce wiersza, ten pomiar ma sens tylko na dysku"
fi

echo
echo "PODSUMOWANIE: $OK OK / $ZLE ZLE"
[[ $ZLE -eq 0 ]] || exit 1
