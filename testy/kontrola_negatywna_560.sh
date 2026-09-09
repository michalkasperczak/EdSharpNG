#!/usr/bin/env bash
# Kontrola negatywna dla 5.0.60: wklejanie w EdSharpie odzyskuje skladnie
# Markdown, zapis bogatego schowka przestaje wywracac program, a instalator nie
# proponuje juz instalacji skryptow JAWS i dodatkow NVDA.
# Mierzy ZRODLA wobec JAWNEJ rewizji odniesienia.
#
#   ./kontrola_negatywna_560.sh [rewizja_odniesienia] [rewizja_mierzona]
#
# Bez drugiego argumentu mierzy DRZEWO ROBOCZE (i mowi o tym wprost) - lekcja
# z 5.0.58: skrypt bez tego argumentu pilnowal zachowan, ktore user PO jego
# napisaniu kazal zmienic, i oblanie bylo POPRAWNYM wynikiem sondy.
set -uo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

REF="${1:-cf5cb64}"
CEL="${2:-}"
OK=0; ZLE=0

if [[ -n "$CEL" ]]; then
    NOWY=$(git show "$CEL:EdSharp.cs")
    NOWY_ISS=$(git show "$CEL:EdSharp_Setup.iss")
    echo "Mierze rewizje $CEL wobec odniesienia $REF"
else
    NOWY=$(cat EdSharp.cs)
    NOWY_ISS=$(cat EdSharp_Setup.iss)
    echo "UWAGA: mierze DRZEWO ROBOCZE wobec odniesienia $REF (podaj 2. argument, zeby przypiac rewizje)"
fi
STARY=$(git show "$REF:EdSharp.cs")
STARY_ISS=$(git show "$REF:EdSharp_Setup.iss")

ok()  { OK=$((OK+1));  echo "OK:  $1"; }
zle() { ZLE=$((ZLE+1)); echo "ZLE: $1"; }
jest()    { if grep -qF -- "$2" <<< "$NOWY"; then ok "$1"; else zle "$1"; fi; }
niema()   { if grep -qF -- "$2" <<< "$NOWY"; then zle "$1"; else ok "$1"; fi; }
bylo()    { if grep -qF -- "$2" <<< "$STARY"; then ok "$1"; else zle "$1 (sonda GLUCHA: tego nie bylo w $REF)"; fi; }
nie_bylo(){ if grep -qF -- "$2" <<< "$STARY"; then zle "$1 (to JUZ bylo w $REF, wiec asercja nie dowodzi zmiany)"; else ok "$1"; fi; }

echo "== 1. WLASNY FORMAT SCHOWKA: wszystkie warstwy =="
jest     "stala nazwy formatu istnieje" 'private const string EdSharpMarkdownFormat = "EdSharpNG.Markdown";'
nie_bylo "tego formatu NIE bylo w $REF" "EdSharpNG.Markdown"
jest     "odczyt ze schowka istnieje" "private static bool TryGetEdSharpMarkdownFromClipboard(out string sMarkdown)"
jest     "zapis do kontrolki istnieje" "private static bool PasteMarkdownTextInto(HomerRichTextBox rtb, string sMarkdown)"
jest     "warstwa mowiaca istnieje" "private bool PasteEdSharpMarkdownFormat(HomerRichTextBox rtb)"
jest     "komunikat po wklejeniu" 'AddMessage("Markdown pasted")'
jest     "pomocnik skladni listy istnieje" "private static string GetMarkdownListSourceText(HomerRichTextBox rtb, int iFirst, int iLast)"

echo "== 2. KAZDA METODA POMOCNICZA JEST WOLANA (osierocony helper = lekcja z 5.0.44) =="
for m in TryGetEdSharpMarkdownFromClipboard PasteMarkdownTextInto PasteEdSharpMarkdownFormat GetMarkdownListSourceText; do
    N=$(grep -cF "$m" <<< "$NOWY")
    if [[ "$N" -ge 2 ]]; then ok "$m jest WOLANA (wystapien: $N)"; else zle "$m jest osierocona (tylko deklaracja)"; fi
done
# Control+V MUSI wolac nasza droge PRZED rtb.Paste(), inaczej kontrolka wklei
# format tekstowy i skladnia znowu przepadnie.  Kolejnosc jest tu istotna.
if python3 -c '
import sys
s=open("EdSharp.cs",encoding="utf-8",errors="replace").read().replace("\r","")
i=s.find("if (menuItem == menuEditPaste) {")
if i<0: sys.exit(2)
blok=s[i:i+1200]
a=blok.find("PasteEdSharpMarkdownFormat(rtb)")
b=blok.find("rtb.Paste();")
sys.exit(0 if (a>=0 and b>=0 and a<b) else 1)
'; then
    ok "nasza droga stoi PRZED rtb.Paste() w obsludze Control+V"
else
    zle "nasza droga NIE stoi przed rtb.Paste() - skladnia znowu przepadnie"
fi

echo "== 3. WSZYSTKIE SIEDEM SCIEZEK KOPIOWANIA WYPELNIA WLASNY FORMAT =="
# Gdyby ktorakolwiek sciezka go nie wypelniala, wklejenie po niej oddawalo by
# sam tekst - czyli dokladnie defekt, ktory naprawiamy, tylko w innym miejscu.
#
# PROG MUSI BYC DOKLADNY, NIE "co najmniej".  Pierwsza wersja tej asercji
# pytala o ">= 6" i PRZEPUSCILA podstawiony blad (zdjecie wlasnego formatu z
# kopiowania odsylacza dalo 6 i wynik nadal byl zielony).  Zlapane wlasna
# kontrola gluchoty 01.09.2026.
N_FORMAT=$(grep -cF "data.SetData(EdSharpMarkdownFormat" <<< "$NOWY")
N_FORMAT_LINE=$(grep -cF "dataLine.SetData(EdSharpMarkdownFormat" <<< "$NOWY")
SUMA=$((N_FORMAT + N_FORMAT_LINE))
if [[ "$SUMA" -eq 7 ]]; then ok "wlasny format wypelniany w DOKLADNIE 7 sciezkach kopiowania"; else zle "wlasny format w $SUMA sciezkach, a ma byc w 7 - ktoras kopiuje bez niego albo doszla nowa"; fi
# Kazda sciezka OSOBNO, po nazwie metody - sama liczba nie mowi, KTORA wypadla.
for m in CopyMarkdownInlineLinkSpanAsRichText CopyMarkdownLinkAsRichText TryCopyMarkdownList TryCopyMarkdownRichLine TryCopyMarkdownSelection TryCopyMarkdownUrlAsRichText; do
    if python3 -c '
import sys,re
s=open("EdSharp.cs",encoding="utf-8",errors="replace").read().replace("\r","")
m=sys.argv[1]
i=s.find(m+"(")
# szukamy DEKLARACJI (linia z private), nie wywolania
for mm in re.finditer(r"private[^\n]*\b"+re.escape(m)+r"\s*\(", s):
    i=mm.start(); break
else:
    sys.exit(2)
# cialo metody: do znacznika zamykajacego "} // <nazwa> method"
j=s.find("} // "+m+" method", i)
if j<0: sys.exit(3)
sys.exit(0 if "SetData(EdSharpMarkdownFormat" in s[i:j] else 1)
' "$m"; then
        ok "sciezka $m wypelnia wlasny format"
    else
        zle "sciezka $m NIE wypelnia wlasnego formatu (wklejenie po niej odda sam tekst)"
    fi
done

echo "== 4. ZAPIS SCHOWKA Z PONOWIENIAMI ZAMIAST GOLEGO SetDataObject =="
jest     "Util.SetClipboardData istnieje" "public static bool SetClipboardData(DataObject data)"
nie_bylo "tej metody NIE bylo w $REF" "SetClipboardData"
jest     "petla ponowien w nowej metodzie" "for (iTry = 0; iTry < 10; iTry++)"
# ZADNE gole Clipboard.SetDataObject nie moze zostac POZA ta jedna metoda:
# to byl mechanizm awarii (wyjatek 0x800401d0 przy zajetym schowku).
N_GOLE=$(grep -cF "Clipboard.SetDataObject(" <<< "$NOWY")
if [[ "$N_GOLE" -eq 1 ]]; then
    ok "Clipboard.SetDataObject wystepuje DOKLADNIE raz (tylko w oslonie SetClipboardData)"
else
    zle "Clipboard.SetDataObject wystepuje $N_GOLE razy - czesc kopiowania nadal bez oslony"
fi
N_GOLE_STARE=$(grep -cF "Clipboard.SetDataObject(" <<< "$STARY")
if [[ "$N_GOLE_STARE" -ge 7 ]]; then
    ok "kontrola waznosci: w $REF bylo $N_GOLE_STARE golych wywolan (awaria byla realna)"
else
    zle "kontrola waznosci: w $REF bylo tylko $N_GOLE_STARE - asercja wyzej nic nie dowodzi"
fi
echo "== 4b. NIEUDANE KOPIOWANIE MOWI, A NIE MILCZY =="
for k in "link not copied" "list not copied" "selection not copied" "heading not copied" "text not copied"; do
    jest "komunikat: $k" "Clipboard is busy, $k!"
done

echo "== 5. INSTALATOR: strona Finish bez krokow do klikania =="
# FALSZYWY ALARM Z KOMENTARZA (ten sam rodzaj defektu co w 5.0.58): slowo
# "postinstall" wystepuje nadal w KOMENTARZU opisujacym, co i dlaczego zdjeto.
# Pytamy wiec o AKTYWNE wpisy, czyli linie NIE zaczynajace sie od srednika.
akt_iss() { grep -v '^[[:space:]]*;' <<< "$1"; }
if akt_iss "$NOWY_ISS" | grep -qF "postinstall"; then
    zle "instalator NADAL ma AKTYWNE kroki postinstall"
else
    ok "instalator nie ma zadnego aktywnego kroku postinstall"
fi
# Kontrola samego dopasowania: gdyby filtr komentarzy zjadal wszystko, ponizsza
# asercja tez by oblala - a ngen JEST aktywnym wpisem.
if akt_iss "$NOWY_ISS" | grep -qF "ngen.exe"; then
    ok "kontrola dopasowania: filtr komentarzy widzi AKTYWNE wpisy (ngen)"
else
    zle "kontrola dopasowania: filtr komentarzy zjada aktywne wpisy - asercja wyzej nic nie znaczy"
fi
if akt_iss "$STARY_ISS" | grep -qF "postinstall"; then
    ok "kontrola waznosci: w $REF kroki postinstall BYLY aktywne (zmiana jest realna)"
else
    zle "kontrola waznosci: w $REF nie bylo postinstall - asercja wyzej nic nie dowodzi"
fi
# Trzy zdjete kroki, kazdy osobno - zeby zdjecie tylko czesci nie przeszlo.
for d in "Install JAWS scripts for EdSharpNG" "Install NVDA add-on" "Install NVDA spelling-errors add-on"; do
    if akt_iss "$NOWY_ISS" | grep -qF "$d"; then zle "krok NADAL aktywny: $d"; else ok "krok zdjety: $d"; fi
    if akt_iss "$STARY_ISS" | grep -qF "$d"; then ok "kontrola waznosci: w $REF ten krok byl: $d"; else zle "kontrola waznosci: w $REF nie bylo kroku $d"; fi
done
# ngen ZOSTAJE: dziala ukryty, nie wymaga klikania i skraca start programu.
if grep -qF "ngen.exe" <<< "$NOWY_ISS"; then ok "ngen zostal (przyspiesza start, nic nie klika)"; else zle "ngen zniknal razem z krokami postinstall"; fi
# PLIKI zostaja - zdjete jest klikanie, nie funkcja.
for f in "EdSharpNG-spellcheck.nvda-addon" "EdSharp.nvda-addon" 'Scripts\*'; do
    if grep -qF "Source: \"$f\"" <<< "$NOWY_ISS"; then ok "plik nadal w instalatorze: $f"; else zle "plik WYPADL z instalatora: $f"; fi
done
# Droga reczna musi ISTNIEC w programie, inaczej zdjecie kroku odbieraloby funkcje.
jest "przelacznik --install-jaws-settings nadal obslugiwany w programie" '"--install-jaws-settings"'
jest "kod instalujacy skrypty JAWS nadal istnieje" "public static class JawsScripts"

echo "== 6. CZEGO NIE WOLNO BYLO RUSZYC =="
jest "kopiowanie do Worda nadal sklada RTF z polem HYPERLINK" "BuildRtfHyperlink"
jest "prawdziwa lista Worda nadal budowana" "BuildRtfListParagraph"
jest "kopiowanie zaznaczenia z 5.0.57 zostaje" "TryCopyMarkdownSelection"
jest "migracja rozszerzenia z 5.0.59 zostaje" "MigrateDefaultExtensionToMarkdown"
jest "komunikat poczty z 5.0.59 zostaje" "Mail client refused the attachment!"
jest "Control+Y nadal ponawia (5.0.58)" "HandleRedoAliasKey"
jest "zakladki z nazwa z 5.0.56 zostaja" "ReadNamedBookmarks"
jest "Control+U NIETKNIETY - jego slowa to pytanie, nie decyzja" '"Control+U"'
jest "Control+I NIETKNIETY - jego slowa to Nie wiem" '"Control+I"'
# W pliku RTF formatowanie jest PRAWDZIWE - tam nasza droga MUSI ustapic.
jest "w pliku RTF nasza droga ustepuje zwyklemu wklejaniu" "if (IsRichTextFile(this.Child)) return false;"
# Dokument zabezpieczony: nie pisze i MOWI (cisza = nierozroznialna od zapisu).
jest "bramka dokumentu zabezpieczonego mowi" 'AddMessage("Document is guarded!")'

echo "== 7. OPISY MOWIONE: trzy pliki zgodne co do znaku =="
OPIS="Paste=Control+V, Paste text from clipboard; text copied with formatting inside EdSharpNG comes back as Markdown, so a copied link keeps its address"
for f in Hotkeys.ini hotkeys.txt EdSharp.md; do
    if grep -qF "$OPIS" "$f"; then ok "$f ma nowy opis Control+V"; else zle "$f nie ma nowego opisu Control+V"; fi
done
A=$(grep -F "Paste=Control+V," Hotkeys.ini || true)
B=$(grep -F "Paste=Control+V," hotkeys.txt || true)
if [[ -n "$A" && "$A" == "$B" ]]; then ok "oba pliki opisow zgodne CO DO ZNAKU"; else zle "opisy rozjechaly sie miedzy plikami"; fi
if git show "$REF:Hotkeys.ini" | grep -qF "$OPIS"; then
    zle "kontrola waznosci: ten opis JUZ byl w $REF"
else
    ok "kontrola waznosci: w $REF tego opisu nie bylo"
fi

echo "== 8. HIGIENA PLIKU =="
# Mierzymy plik Z DYSKU: git normalizuje konce wiersza przy wydawaniu tresci.
LINII=$(wc -l < EdSharp.cs)
BEZ_CR=$(grep -cv $'\r$' EdSharp.cs || true)
if [[ "$BEZ_CR" -eq 0 ]]; then ok "wszystkie $LINII linii EdSharp.cs ma windowsowe konce wiersza"; else zle "$BEZ_CR linii bez CR"; fi
for f in Hotkeys.ini hotkeys.txt; do
    B=$(grep -cv $'\r$' "$f" || true)
    if [[ "$B" -eq 0 ]]; then ok "$f: wszystkie linie z CR"; else zle "$f: $B linii bez CR"; fi
done
# EdSharp.md: PRZEPISANE W 5.0.62, bo ta asercja zaczela KLAMAC po naprawie.
# Stara tresc porownywala liczbe CR w `git show` z liczba CR NA DYSKU i zielono
# bylo wtedy, gdy oba daja zero.  To bylo pilnowanie SAMEGO OBJAWU naprawionego
# w 5.0.61: plik lezal na dysku z 843 golymi LF i w tej postaci trafil do paczki,
# bo staging kopiuje Z DYSKU, a git normalizuje dopiero przy dotknieciu pliku.
# Po naprawie dysk ma CRLF, a `git show` nadal LF - wiec asercja dawala FAIL na
# kodzie POPRAWNYM, zaciemniajac prawdziwe regresje w tym samym przebiegu.
# Pilnujemy teraz tego samego, co kontrola 561: plik NA DYSKU ma CRLF w KAZDEJ
# linii, bo w takiej postaci dostaje go uzytkownik.
MD_LINII=$(wc -l < EdSharp.md)
MD_NOW=$(grep -c $'\r$' EdSharp.md || true)
if [[ "$MD_LINII" -eq "$MD_NOW" ]]; then ok "EdSharp.md NA DYSKU ma CRLF w kazdej z $MD_LINII linii"; else zle "EdSharp.md: $MD_LINII linii, ale tylko $MD_NOW z CRLF"; fi
# KONTROLA WAZNOSCI licznika: na probce z golymi LF musi pokazac zero.
printf 'a\nb\nc\n' > /tmp/kn560_probka_lf.txt
PROBKA=$(grep -c $'\r$' /tmp/kn560_probka_lf.txt || true)
if [[ "$PROBKA" -eq 0 ]]; then ok "kontrola waznosci: licznik CR pokazuje zero na probce z golymi LF"; else zle "licznik CR nie odroznia LF od CRLF (probka: $PROBKA)"; fi
if [[ -n "$CEL" ]]; then DODANE=$(git diff "$REF" "$CEL" -- EdSharp.cs | grep -c '^+' || true); else DODANE=$(git diff "$REF" -- EdSharp.cs | grep -c '^+' || true); fi
if [[ "$DODANE" -gt 0 ]]; then ok "diff ma dodane linie ($DODANE) - kontrola ma na czym pracowac"; else zle "diff nie ma dodanych linii"; fi

echo
echo "WYNIK: $OK OK / $ZLE ZLE"
[[ "$ZLE" -eq 0 ]] || exit 1
