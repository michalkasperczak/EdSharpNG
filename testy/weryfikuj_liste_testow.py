#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Weryfikacja listy testow EdSharpNG wobec KODU.

Kazdy komunikat cytowany w liscie musi istniec jako literal w EdSharp.cs.
Kazdy skrot cytowany w liscie musi byc zarejestrowany w CreateMenuItem albo
obsluzony w Handle*Key. Kontrola negatywna: celowo bledne cytaty MUSZA oblac,
inaczej test przechodzi zawsze i nic nie dowodzi.
"""
import re, sys, pathlib, os

# KORZEN REPOZYTORIUM LICZYMY Z POLOZENIA TEGO PLIKU, nie z zaszytej sciezki.
# Do 13.09.2026 stalo tu na stalo "/mnt/d/projekty/edsharp-pr" - katalog, ktorego
# na tej maszynie NIE MA, wiec sonda wywalala sie wyjatkiem FileNotFoundError,
# zamiast cokolwiek zmierzyc.  Sonda, ktora nie startuje, jest gorsza od braku
# sondy: w przebiegu wyglada jak blad narzedzia, wiec sie ja pomija.
# Zmienna EDSHARP_REPO nadal pozwala wskazac inne repozytorium (np. przy
# porownywaniu dwoch kopii).
REPO = pathlib.Path(os.environ.get("EDSHARP_REPO", pathlib.Path(__file__).resolve().parent.parent))
CS = (REPO / "EdSharp.cs").read_text(encoding="utf-8", errors="replace")
HOT = (REPO / "Hotkeys.ini").read_text(encoding="utf-8", errors="replace")
LBC = (REPO / "Lbc.cs").read_text(encoding="utf-8", errors="replace")
LISTA = (REPO / "testy/EdSharpNG_testy_5.0-5.0.21.md").read_text(encoding="utf-8")
MD = (REPO / "EdSharp.md").read_text(encoding="utf-8", errors="replace")

# Wersja kodu BEZ komentarzy liniowych.  Bez tego trafienie w komentarzu
# ("a bare \"Removed\" is ambiguous...", "crashed the Control+H HTML") udaje
# realny literal i daje falszywy alarm - zmierzone.
CS_KOD = "\n".join(
    ("" if l.lstrip().startswith("//") else re.sub(r"//.*$", "", l))
    for l in CS.splitlines()
)

# Skladniki.cs - dociaganie brakujacych narzedzi konwersji (Pandoc itd.)
SKL = (REPO / "Skladniki.cs").read_text(encoding="utf-8", errors="replace")

# --- komunikaty cytowane w liscie testow (musza byc literalami w kodzie) ---
KOMUNIKATY = [
    "No headings!", "Last heading!", "First heading!",
    "Last heading at this level!", "First heading at this level!",
    "Above ", "Below ", ", heading ", "Heading ",
    "Document Navigation", "Enter goes to the heading",
    "Last bookmark!", "First bookmark!", "No bookmark!", "Bookmark removed",
    "Bookmarks",
    "Numbered file ", "Numbered Files", "No numbered files are assigned!",
    "Opening ", "No disk file is open for this command!",
    "Word wrap", "Unwrap", "Guard", "Clear formatting",
    # RODZINA L, 5.0.55: komunikaty brzmia "Bulleted list on/off", nie
    # "Toggle bulleted list". Kasperczak nie okreslil ich brzmienia (jego slowa:
    # "CTRL-l punktowana, cTRL-Shift-l numerowana, jak przelacznik wlacza/zamienia
    # na tekst zwykly"), a stan WLACZONY/WYLACZONY jest dla czytnika ekranu
    # informacja wazniejsza niz slowo "przelacz" - uzytkownik chce wiedziec, co
    # sie STALO, nie co komenda robi.
    "Bulleted list on", "Bulleted list off",
    "Numbered list on", "Numbered list off", "Insert Markdown link",
    "List copied",
    "Preview", "Preview detached", "Editing", "Synced", "Detached",
    "Elements list", "No link at cursor",
    "Removed from list", "List is now empty", "This is a folder, not a file",
    "Deleted from disk", "Delete canceled",
    "Close Window",
]
# celowo bledne - MUSZA oblac (kontrola waznosci testu)
KOMUNIKATY_KONTROLA = ["Removed", "No items", "Not a file", "Spis tresci", "Returning"]

# --- chordy cytowane w liscie: (chord, skad ma pochodzic) ---
# 'menu' = CreateMenuItem, 'handler' = obslugiwane w Handle*Key (bez wpisu w menu)
CHORDY_MENU = [
    ("F6", "Document Navigation ..."),
    # PRZYPISY PRZENIESIONE Z F6 DO RODZINY K (5.0.46, rozmowa 29.08.2026
    # 22:40-23:09).  Jego zasada: "Alt literki to byly pewne listy a z Ctrl sie
    # wstawiala reszte".  Powod przeniesienia: Control+F6 ma dostac LISTE
    # LINKOW ("to CTRL-F6 linki fajne"), a nie moze, dopoki siedzi na nim
    # wstawianie przypisu.  ODPOWIEDZIAL 30.08.2026 10:01, gdzie ma zamieszkac
    # kontekstowy skok: "Skok przypis tekst tekst przypis robimy Alt-CTRL-ka
    # (...) Tak z ego F6 w przypisach bysmy rezygnowali" - czyli cala rodzina F6
    # wychodzi z przypisow (5.0.47).
    ("Control+Shift+K", "Insert Footnote ..."),
    ("Control+Alt+K", "Go to Footnote"),
    ("Alt+K", "Footnote List ..."),
    ("Control+Enter", "Section Break"),
    ("Control+Shift+Enter", "Trim Blanks"),
    ("Control+PageDown", "Next Section"),
    ("Control+PageUp", "Prior Section"),
    ("Control+Shift+PageDown", "Next Section at Same Level"),
    ("Control+Shift+PageUp", "Prior Section at Same Level"),
    ("Alt+Down", "Next Sentence"),
    ("Alt+Up", "Prior Sentence"),
    ("Control+Down", "Next Paragraph"),
    ("Control+Up", "Prior Paragraph"),
    ("Alt+Right", "Next Chunk"),
    ("Alt+Left", "Prior Chunk"),
    # ZAKLADKI PRZENIESIONE NA B w 5.0.45 (ustalenia edsharpng-4 i -12).
    # Control+K wzielo wstawianie linku, a Clear Bookmark zeszlo do menu-only,
    # bo usuwanie zakladki idzie przez Delete na jej liscie.
    ("Control+B", "Set Bookmar&k"),
    ("Alt+B", "Go to Bookmark"),
    ("Shift+PageDown", "Next Bookmark"),
    ("Shift+PageUp", "Prior Bookmark"),
    ("Alt+Shift+F2", "Numbered Files ..."),
    ("Control+F4", "&Close Window"),
    ("Control+Shift+W", "Close All but Current Window"),
    # Dopisywanie do schowka wrocilo na Alt+Shift+C (5.0.33, jego wybor
    # 27.08.2026: "Alt+Shift+C wlacza i wylacza dopisywanie do schowka").
    # Alt+F9 bylo tylko NASZYM zastepnikiem po tym, jak cyfry poszly na okna.
    ("Alt+Shift+C", "Append from Clipboard"),
    ("Control+F9", "Compiler"),
    ("Alt+Shift+F6", "Baseline ..."),
    ("Alt+Shift+F10", "Reset Configuration"),
    # Next / Prior Baseline USUNIETE 29.08.2026 razem z cala rodzina skokow po
    # bogatym formatowaniu - asercje na te chordy sa nizej, w sekcji 4d, jako
    # kontrole NIEOBECNOSCI. Tu bylyby FAILem przy poprawnym kodzie.
    # RODZINA F4 uporzadkowana w 5.0.45 (ustalenia edsharpng-5, -6, -8):
    # F4 lista okien, Shift+F4 lista folderow, Control+Shift+F4 foldery
    # systemowe.  Stare Control+0 i Control+Alt+0 sa wolne.
    ("Shift+F4", "Go to Folder"),
    ("Control+Shift+F4", "Go to Special Folder"),
    ("F7", "Spell Check"),
    # 5.0.66: guard to JEDEN PRZELACZNIK na Control+F7 (jego decyzja
    # 03.09.2026: "wspolny skrot Ctrl-F7 wlacz/wylacz zabezpieczenie i tyle,
    # a z shiftem bedzie wolny").  Komenda No Guard zniknela ze wszystkich
    # warstw, wiec asercja o jej chordzie bylaby FAILem przy POPRAWNYM kodzie.
    # Control+Shift+F7 dolaczyl do listy zwolnionych w sekcji 5.
    ("Control+F7", "Guard Document"),
    ("Control+Shift+C", "Copy Rich Text"),
    ("Alt+C", "Copy Append"),
    ("Alt+X", "Cut Append"),
    ("Control+Space", "Select Chunk"),
    ("Shift+Back", "Chunk"),
    ("Alt+R", "Recent Files ..."),
    ("Alt+L", "List Favorites ..."),
    # RODZINA L, 5.0.55: Set Favorite i Clear Favorite byly DWIEMA komendami na
    # dwoch klawiszach. Teraz jest JEDEN przelacznik "Toggle Favorite" pod
    # Alt+Shift+L, ktory sam sprawdza, czy plik jest juz na liscie. Clear
    # Favorite ZOSTALA w menu bez skrotu (droga dla kogos, kto nie ufa
    # przelacznikowi), a Control+L i Control+Shift+L przeszly na listy Markdown.
    ("Alt+Shift+L", "Toggle Favorite"),
    ("Alt+Home", "Home Character"),
    ("Alt+End", "End Character"),
    ("Alt+T", "Topic"),
    ("F2", "Special Character ..."),
    ("F4", "Current Windows ..."),
    ("F1", "Documentation"),
    ("Control+F1", "Key Describer"),
    ("Alt+Shift+H", "Hotkey Summary"),
]

def menu_map():
    d = {}
    for m in re.finditer(r'CreateMenuItem\("([^"]*)",\s*"([^"]*)"', CS):
        # pomin zakomentowane linie
        start = CS.rfind("\n", 0, m.start()) + 1
        if CS[start:m.start()].lstrip().startswith("//"):
            continue
        d[m.group(1)] = m.group(2)
    return d

MENU = menu_map()
wyniki = []

def spr(nazwa, warunek):
    wyniki.append((bool(warunek), nazwa))

# 1. komunikaty obecne w kodzie
for s in KOMUNIKATY:
    spr("komunikat w kodzie: %r" % s, ('"%s"' % s) in CS)

# 2. kontrola waznosci: stare/nieistniejace komunikaty MUSZA byc nieobecne
for s in KOMUNIKATY_KONTROLA:
    spr("KONTROLA (musi byc NIEOBECNY): %r" % s, ('"%s"' % s) not in CS_KOD)

# 3. chordy: nazwa komendy istnieje i ma DOKLADNIE ten chord
for chord, nazwa in CHORDY_MENU:
    got = MENU.get(nazwa)
    spr("chord %s -> %r (kod: %r)" % (chord, nazwa, got), got == chord)

# 4. kontrola waznosci chordow: stare przypisania MUSZA byc nieaktualne
for chord, nazwa in [("Alt+7", "Append from Clipboard"), ("Alt+0", "Compiler"),
                     ("Control+W", "&Word Wrap"), ("Alt+Shift+D6", "Baseline ..."),
                     ("F6", "Go to Section"), ("Control+D4", "Format Code")]:
    spr("KONTROLA (stary chord NIE obowiazuje): %s != %r" % (chord, nazwa),
        MENU.get(nazwa) != chord)

# 4b. Format Code USUNIETA na polecenie Kasperczaka (Telegram 26.08.2026,
# zlecenie 1787700621195-7). Nie wystarczy, ze nie ma jej na starym skrocie -
# nie moze jej byc NIGDZIE: ani jako pozycji menu, ani jako pola, ani jako
# komunikatu. Ta komenda przepisywala caly dokument i kosztowala go plik.
spr("Format Code USUNIETA: brak pozycji menu", "Format Code" not in MENU)
spr("Format Code USUNIETA: brak pola menuMiscFormatCode",
    "menuMiscFormatCode" not in CS)
# Control+Shift+F6 byl wolny po usunieciu Format Code, a 27.08.2026 dostal
# Footnote List (zlecenie 1787832545532-3).  Wiec dowodem na usuniecie Format
# Code nie jest już "skrot wolny", tylko to, ze skrot nalezy do INNEJ komendy.
# AKTUALIZACJA 5.0.46: lista przypisow zeszla z Control+Shift+F6 na Alt+K, wiec
# dowodem na usuniecie Format Code jest znowu to, ze chord NIE nalezy do zadnej
# komendy - plus brak samej nazwy.  Kontrola pozytywna: Alt+K faktycznie zajete.
spr("Format Code USUNIETA: brak nazwy i Control+Shift+F6 bez komendy",
    'CreateMenuItem("Format Code' not in CS
    and "Control+Shift+F6" not in set(menu_map().values())
    and menu_map().get("Footnote List ...") == "Alt+K")

# 4c. Spis tresci w starej formie USUNIETY na polecenie Kasperczaka (Telegram
# 26.08.2026, zlecenie 1787700780170-8). Trzy komendy oparte na znaku Form Feed,
# ktorego w jego plikach Markdown nie ma ani jednego. Role spisu tresci pelni
# drzewo naglowkow pod F6.
# Ze starego modelu zostaje usunieta Go to Section (Control+Shift+F12 wolny)
# oraz sam mechanizm Form Feed. Dwie pozostale komendy dostaly 27.08.2026 NOWE
# zadanie w ramach markdownowego spisu tresci (zlecenie 1787793592380-0):
#   Alt+Shift+T -> Table of Contents (tworzy i odswieza spis)
#   Shift+F6    -> Go to Contents (kontekstowy skok w obie strony)
spr("Go to Section USUNIETA: brak pozycji menu", "Go to Section" not in MENU)
spr("Go to Section USUNIETA: brak pola menuNavigateGoToSection",
    "menuNavigateGoToSection" not in CS)

# 4d. SKOKI PO BOGATYM FORMATOWANIU I CALCULATE DATE USUNIETE na polecenie
# Kasperczaka (Telegram 29.08.2026, zlecenie 1788006241286-4, jego mini korekta
# naszej listy). Jego slowa: "usuwamy te wszystkie wyrownania, wciecia rozumiane
# jako bogate formatowanie strony i tak dalej".
# Mierzymy KAZDA warstwe osobno, bo jedna sonda po nazwie komendy daje zielone
# i przegapia slad (lekcja z osieroconego NavigatePart, 28.08.2026): pozycja
# menu, POLE klasy, HANDLER w menuItem_Click, opis mowiony.
USUNIETE_FORMATOWANIE = [
    ("Next Alignment", "menuNavigateNextJustify"),
    ("Prior Alignment", "menuNavigatePriorJustify"),
    ("Next Style", "menuNavigateNextStyle"),
    ("Prior Style", "menuNavigatePriorStyle"),
    ("Next Baseline", "menuNavigateNextBaseline"),
    ("Prior Baseline", "menuNavigatePriorBaseline"),
    ("Next Font", "menuNavigateNextFont"),
    ("Prior Font", "menuNavigatePriorFont"),
    ("Calculate Date ...", "menuMiscCalculateDate"),
]
for nazwa, pole in USUNIETE_FORMATOWANIE:
    spr("USUNIETE %r: brak pozycji menu" % nazwa, nazwa not in MENU)
    spr("USUNIETE %r: brak pola %s" % (nazwa, pole), pole not in CS)
    spr("USUNIETE %r: brak handlera" % nazwa,
        ("menuItem == %s" % pole) not in CS)

# Metoda CalculateDate i jej pola konfiguracji nie moga zostac osierocone:
# to dokladnie ta pulapka, ktora zlapal Kasperczak przy nawigacji po czesciach
# (wpis NavigatePart pytal i zapisywal, a zero miejsc go czytalo).
spr("CalculateDate: metoda usunieta", "public void CalculateDate()" not in CS)
spr("CalculateDate: brak wolania metody", "CalculateDate();" not in CS)
for pole_danych in ['"Year"', '"Month"', '"Week"', '"Day"']:
    spr("CalculateDate: pole danych %s nie zostalo osierocone" % pole_danych,
        pole_danych not in CS)

# KONTROLE, ZE NIE ZEPSULEM DZIALAJACEGO. Kasperczak nie kazal usuwac ani
# USTAWIACZY formatowania (program nadal otwiera pliki RTF), ani komend
# PYTAJACYCH o format pod kursorem, ani wstawiania biezacej daty.
for chord, nazwa in [("Alt+Shift+J", "Justify ..."),
                     ("Alt+Shift+OemQuestion", "Style ..."),
                     ("Alt+Shift+F6", "Baseline ..."),
                     ("Alt+Shift+OemSemicolon", "Insert Time")]:
    spr("KONTROLA (zostaje): %s -> %r" % (chord, nazwa), MENU.get(nazwa) == chord)
# Te dwie komendy w MENU nazywaja sie krotko "Styles" i "Font" - dluga nazwa
# "Say Styles" / "Say Font" wystepuje TYLKO w opisach mowionych (Hotkeys.ini).
# Moja pierwsza wersja tej asercji pytala o dluga nazwe i slusznie padla.
spr("KONTROLA (zostaje): Styles pyta o format pod kursorem",
    MENU.get("Styles") == "Alt+OemQuestion")
# Komendy czcionkowe USUNIETE 13.09.2026 na polecenie Kasperczaka ("NVDA czyta
# czcionki po swojemu NVDA-F").  Kontrola odwrocona: pilnuje, ze nie wrocily
# zadna z trzech warstw - menu, opis mowiony, handler.
spr("Font: brak pozycji w menu", "Font" not in MENU)
spr("Set Selection Font: brak pozycji w menu", "Set Selection Font ..." not in MENU)
spr("Font: brak handlera", "menuQueryFont" not in CS)
spr("Set Selection Font: brak handlera", "menuEditSetSelectionFont" not in CS)
spr("Font: helper GetFontText usuniety", "GetFontText" not in CS)
spr("Font: okno wyboru czcionki Dialog.GetFont usuniete", "FontDialog" not in CS)

# CONTROL+O JEDYNYM OTWIERANIEM (13.09.2026).  Osobna pozycja "Open Other
# Format" (Control+Shift+O) usunieta, jej dzialanie wchlonal Control+O.
spr("Open Other Format: brak pozycji w menu", "Open Other Format ..." not in MENU)
spr("Open Other Format: brak handlera", "menuFileOpenOtherFormat" not in CS)
spr("Open Other Format: chord Control+Shift+O wolny",
    "Control+Shift+O\"" not in CS)
# Klucz w MENU zawiera znak "&" (klawisz dostepu), stad "&Open ...".
spr("Control+O: zostaje w menu", MENU.get("&Open ...") == "Control+O")
# Polityke konwersji bierzemy z GetViewLevel, NIE z obecnosci konwertera w
# tabeli Import: konwertery istnieja tez dla .md, .rst i .tex, a te formaty maja
# otwierac sie wprost.  Ta asercja pilnuje wlasnie tego wyboru.
spr("Control+O: pyta o konwersje przez OfferConversionOnOpen",
    "OfferConversionOnOpen(sFile)" in CS)
spr("Control+O: polityka wspolna z otwieraniem z Eksploratora",
    "if (GetViewLevel(sFile) == 1) return true;" in CS)
spr("Control+O: odrzucone kryterium HasImportConverter nie wrocilo",
    "HasImportConverter" not in CS)
spr("Control+O: HTML dostaje wybor wprost",
    'sExt == "htm" || sExt == "html" || sExt == "xhtml"' in CS)
# Ustawiacze formatowania wolaja te same helpery, co usuniete skoki - gdyby
# helpery poszly razem ze skokami, okna ustawien i pytania o format zamilkly by.
for helper in ["GetJustifyText", "GetStyleText", "GetBaselineText"]:
    spr("KONTROLA (zostaje): helper %s nadal uzywany" % helper,
        CS.count(helper) >= 2)
# Util.Month2Num i Util.Day2Num byly uzywane TAKZE poza CalculateDate.
for helper in ["Month2Num", "Day2Num"]:
    spr("KONTROLA (zostaje): %s nie usuniety razem z Calculate Date" % helper,
        CS.count(helper) >= 2)
# Chordy zwolnione tą zmianą nie moga byc martwe w opisach mowionych: audyt
# skrotow (osobna sonda) liczy je jako zwolnione, tu pilnujemy samego kodu.
# CZTERY Z NICH WROCILY W 5.0.49 z inna funkcja - jego rozstrzygniecie z
# 30.08.2026 15:41 (ustalenia edsharpng-45 i edsharpng-46).  Asercja "zwolniony"
# zaczela wiec KLAMAC przy poprawnym kodzie i zaslanialaby prawdziwe regresje w
# tym samym przebiegu; poprawiona na stan FAKTYCZNY, z nazwa komendy, ktora
# klawisz zajela.
for chord in ["Control+OemCloseBrackets", "Control+OemOpenBrackets",
              "Control+Shift+OemSemicolon"]:
    spr("chord %s zwolniony: nie przypisany zadnej komendzie" % chord,
        chord not in MENU.values())
# 5.0.49: ukosnik i myslnik z Controlem NIE sa juz wolne - chodzi po nich
# nawigacja po elementach Markdown.  Mierzymy PRZYPISANIE do konkretnej komendy,
# nie samo "cos tam jest": zla komenda na tym chordzie to ten sam defekt.
for chord, nazwa in [("Control+OemQuestion", "Next Emphasis"),
                     ("Control+Shift+OemQuestion", "Prior Emphasis"),
                     ("Control+OemMinus", "Next List"),
                     ("Control+Shift+OemMinus", "Prior List")]:
    spr("5.0.49: chord %s nalezy do komendy %s" % (chord, nazwa),
        MENU.get(nazwa) == chord)
# Control+F2 i Control+Shift+F2 tez zwolnione, ale trzeba je liczyc osobno:
# Alt+Shift+F2 (Numbered Files) zawiera je jako PODCIAG.
spr("chord Control+F2 zwolniony", "Control+F2" not in MENU.values())
spr("chord Control+Shift+F2 zwolniony", "Control+Shift+F2" not in MENU.values())
spr("Text Contents USUNIETA: brak pola menuMiscTextContents",
    "menuMiscTextContents" not in CS)
spr("skrot Control+Shift+F12 wolny", "Control+Shift+F12" not in CS_KOD)

# 4d. Markdownowy spis tresci (zlecenie 1787793592380-0). Kazdy punkt to
# decyzja Kasperczaka z rozmowy 26.08.2026 22:26-23:07.
spr("Table of Contents pod Alt+Shift+T", MENU.get("Table of Contents") == "Alt+Shift+T")
spr("Go to Contents pod Shift+F6", MENU.get("Go to Contents") == "Shift+F6")
spr("spis pisze linki WEWNETRZNE (kotwica po nazwie naglowka)",
    "GetMarkdownAnchorText" in CS and '"](#"' in CS)
# Wycinek liczymy od DEFINICJI metody do jej komentarza zamykajacego, a nie
# do nazwy nastepnej metody: GetMarkdownPlainText jest zdefiniowany PONIZEJ,
# ale jego WYWOLANIE stoi w ciele Anchor, wiec przedzial wychodzil pusty.
_a = CS.find("private static string GetMarkdownAnchorText")
_b = CS.find("} // GetMarkdownAnchorText method")
spr("kotwica liczona jak w pandoc: male litery i spacja na minus",
    _a >= 0 and _b > _a and "ToLowerInvariant" in CS[_a:_b] and "\'-\'" in CS[_a:_b])
spr("spis obejmuje WSZYSTKIE poziomy (brak filtra poziomu)",
    "BuildMarkdownContentsText" in CS)
spr("komenda przepisujaca dokument ma bramke na typ pliku",
    "Table of Contents works only on Markdown files!" in CS)
spr("komenda przepisujaca dokument jest COFALNA (ReplaceRange, nie rtb.Text =)",
    "rtb.ReplaceRange(iBlockStart, iBlockEnd, sContents)" in CS)
spr("komunikat ROZSZERZONY podpowiada Alt+Shift+T",
    "No contents yet, press Alt+Shift+T to create it!" in CS)
spr("link wewnetrzny dziala w podgladzie pod Escape",
    "TryGoToInternalAnchorAtCursor" in CS)

# 4da. Przypisy W PODGLADZIE pod Escape (zlecenie 1787840875741-1, iteracja 3).
# Kasperczak: "on mowi numer przypisu, na tym numerze przypisu naciskam Enter
# (...) on przeskakuje do tresci przypisu (...) i wraca do tego tekstu".
# Decyzja o kursorze z 27.08 16:56: "chyba jednak bardziej naturalnie przeniesc".
spr("Enter w podgladzie skacze po przypisie",
    "TryGoToFootnoteInReview" in CS)
spr("skok w podgladzie PRZENOSI kursor (nie tylko czyta)",
    "MarkdownReview_GoToSourceIndex" in CS)
# KOLEJNOSC MA ZNACZENIE: znacznik przypisu NIE jest linkiem markdownowym, wiec
# gdyby spis tresci byl pytany pierwszy, na przypisie padloby "No link".
_e = CS.find("if (TryGoToFootnoteInReview(child)) return true;")
_f = CS.find("if (TryGoToInternalAnchorAtCursor(child)) return true;")
spr("przypis sprawdzany PRZED spisem tresci i przed adresem",
    _e >= 0 and _f > _e)
spr("znacznik w podgladzie brzmi po ludzku, nie nawiasem z daszkiem",
    "MarkdownReview_FootnoteMarkerText" in CS and 'return " footnote " + s;' in CS)
# SPACJA ROZDZIELAJACA I JEJ ZDJECIE - obie polowy, bo kazda wyszla z pomiaru
# NVDA: bez spacji czytnik mowil "przypisemfootnote 1" jednym slowem, a ze
# spacja BEZWARUNKOWA wiersz tresci na koncu dokumentu dostawal wciecie.
spr("spacja zdejmowana na poczatku wiersza (wiersz tresci bez wciecia)",
    "sShown = sShown.TrimStart();" in CS)
# Cytat w komunikacie powrotu tez nie moze wracac do surowej skladni.
spr("komunikat powrotu cytuje zdanie BEZ surowego znacznika",
    "MarkdownReview_FootnoteMarkerReplacement" in CS)
# Mapa powrotna musi byc lataana, bo napis jest DLUZSZY niz znacznik zrodla -
# bez tego kursor w srodku napisu wracal na poczatek dokumentu.
spr("mapa widok->zrodlo nie ma dziur po dluzszym napisie",
    "bool[] aKnown = new bool[iView + 1];" in CS)
# KONTROLA, ZE NIE ZEPSUTO EDYTORA: skok w edytorze to OSOBNA sciezka od
# podgladu i musi zostac.  Bez tego "podglad dziala" moglo by znaczyc
# "przenieslismy funkcje z edytora do podgladu".  Chord od 5.0.47 to
# Control+Alt+K, ale sciezka kodu jest ta sama.
spr("KONTROLA: skok w edytorze nadal ma wlasna sciezke (Control+Alt+K)",
    "GoToMarkdownFootnoteOrBack" in CS and MENU.get("Go to Footnote") == "Control+Alt+K")

# 4e. Nawigacja po CZESCIACH usunieta (zlecenie 1787793592388-1): dzialala
# tylko z wzorcem NavigatePart ustawianym per jezyk programowania.
#
# UWAGA NA TRZECIA ASERCJE ("skrot wolny"): sprawdza ona, ze KOMENDA zniknela,
# a NIE ze chord jest na zawsze niezajety. Alt+Shift+G zostal PONOWNIE UZYTY w
# 5.0.55: przeszla tam komenda "List Different Items", zeby zwolnic Alt+Shift+L
# na przelacznik ulubionych. Zwolniony klawisz to zasob do rozdania, wiec
# asercja na jego pustce byla po prostu zla - mierzyla cos, czego nikt nie
# obiecal. Rozstrzyga fakt, ze pola menuNavigateGoToPart nie ma w kodzie.
for nazwa, pole, chord in [("Go to Part", "menuNavigateGoToPart", "Alt+Shift+G"),
                           ("Next Part", "menuNavigateNextPart", "Alt+PageDown"),
                           ("Prior Part", "menuNavigatePriorPart", "Alt+PageUp")]:
    spr("%s USUNIETA: brak pozycji menu" % nazwa, nazwa not in MENU)
    spr("%s USUNIETA: brak pola %s" % (nazwa, pole), pole not in CS)
# KONTROLA, ZE NIE USUNIETO ZA DUZO: nawigacja po AKAPITACH i ZDANIACH stoi
# obok tamtej w kodzie i musi zostac.
for nazwa, chord in [("Next Paragraph", "Control+Down"), ("Prior Paragraph", "Control+Up"),
                     ("Next Sentence", "Alt+Down"), ("Prior Sentence", "Alt+Up"),
                     ("&Go to Percent ...", "Control+G"), ("Go to Percent Again", "Alt+G")]:
    spr("KONTROLA: %r NADAL w menu pod %s" % (nazwa, chord), MENU.get(nazwa) == chord)
# KONTROLA, ZE NIE USUNIETO ZA DUZO: te komendy uzywaja tej samej stalej
# Form Feed i Kasperczak zaliczyl je w testach.
# UWAGA (poprawka 27.08.2026, iteracja 2): ta kontrola zadala wczesniej, zeby
# Search for Topic NADAL bylo pod Control+F6 - a te dwie komendy zostaly tego
# samego dnia USUNIETE, zeby zwolnic klawisze przypisom.  Asercja byla wiec
# nieaktualna i produkowala FAIL na poprawnym kodzie.  Kontrola ma pilnowac
# tego, co MIALO zostac: Section Break i Topic uzywaja tej samej stalej Form
# Feed i Kasperczak je zaliczyl, wiec ich usuniecie byloby bledem.
for nazwa, chord in [("Section Break", "Control+Enter"), ("Topic", "Alt+T")]:
    spr("KONTROLA: %r NADAL jest w menu pod %s" % (nazwa, chord),
        MENU.get(nazwa) == chord)
# DOWOD USUNIECIA tamtych dwoch (odwrotnie niz wczesniej): ich nazw NIE MOZE
# byc w menu, a klawisze MUSZA nalezec do przypisow.
for nazwa in ["Search for Topic ...", "Search for Topic Again"]:
    spr("USUNIETA: %r nie ma pozycji w menu" % nazwa, nazwa not in MENU)
# AKTUALIZACJA 5.0.46: wstawianie przypisu jest na Control+Shift+K, a Control+F6
# mial byc WOLNY pod przyszla liste linkow.  Obie polowy sa tu sprawdzane, bo sam
# fakt "przypis na nowym klawiszu" nie dowodzi, ze stary sie zwolnil.
# AKTUALIZACJA 5.0.53: lista linkow POWSTALA, wiec asercja "Control+F6 jest
# WOLNY" zaczela klamac - klawisz zwolnilismy WLASNIE pod nia (edsharpng-47).
# Pilnujemy teraz tego, czego naprawde chcemy: na Control+F6 stoi lista linkow,
# a przypis tam NIE wrocil.
spr("Control+Shift+K nalezy do wstawiania przypisu",
    MENU.get("Insert Footnote ...") == "Control+Shift+K")
spr("Control+F6 nalezy do listy linkow (5.0.53)",
    MENU.get("Link List ...") == "Control+F6")
spr("Control+F6 NIE nalezy do zadnego przypisu",
    MENU.get("Insert Footnote ...") != "Control+F6" and MENU.get("Go to Footnote") != "Control+F6")
spr("Control+Alt+K nalezy do skoku do przypisu (5.0.47)",
    MENU.get("Go to Footnote") == "Control+Alt+K")
# CALA RODZINA F6 MUSI BYC WOLNA OD PRZYPISOW - to jest jego "z ego F6 w
# przypisach bysmy rezygnowali".  Sam nowy chord tego NIE dowodzi.
spr("Alt+F6 jest WOLNY (rodzina F6 wyszla z przypisow)",
    "Alt+F6" not in set(MENU.values()))
# MOWA PRZY CONTROL+ALT: Util.Say tlumi kazda wypowiedz, gdy trzymane sa oba
# modyfikatory, wiec komenda na Control+Alt+K MUSI mowic trybem globalnym.
# Build tego NIE odrzuci, a objawem jest cisza - dla niewidomego to samo co
# "klawisz nie dziala".
spr("skok przypisu mowi trybem globalnym (bramka Control+Alt)",
    'AddMessage("No footnotes!", true);' in CS)
spr("tresc przypisu wymawiana trybem globalnym",
    'Util.Say(defs[iDef].Text + ", footnote " + defs[iDef].Label, true);' in CS)
spr("KONTROLA ROZNICUJACA: lista przypisow (Alt+K) ZOSTAJE bez trybu globalnego",
    'AddMessage("No footnotes!");' in CS)
spr("podpowiedz Ctrl+F1 mowi takze dla komend Control+Alt",
    "bDescribeGlobal = true;" in CS)
spr("KONTROLA: stala SectionBreak NADAL w kodzie", "SectionBreak" in CS)

# 4f. Ustawienia na Control+przecinek i dopisywanie do schowka na Alt+Shift+C
# (5.0.33, jego decyzja 27.08.2026: "Ctrl+przecinek uaktywnia ustawienia",
# "Alt+Shift+C wlacza i wylacza dopisywanie do schowka").
spr("Configuration Options pod Control+Oemcomma",
    MENU.get("Configuration Options ...") == "Control+Oemcomma")
spr("KONTROLA: Alt+Shift+C NIE nalezy juz do ustawien",
    MENU.get("Configuration Options ...") != "Alt+Shift+C")

# 4g. Zawijanie i rozwijanie wierszy jako komendy MENU-ONLY (5.0.34, jego
# decyzja 27.08.2026 22:21: "Oba do menu.  Najwyzej bedziemy przywracac do
# klawiszy potem").  Pusty chord = Keys.None, czyli brak wpisu w hashKey.
for nazwa in ["&Word Wrap", "Unwrap"]:
    spr("%r jest komenda MENU-ONLY (pusty chord)" % nazwa, MENU.get(nazwa) == "")
# Zwolnione chordy nie moga wisiec nigdzie w kodzie ani w opisach mowionych -
# bledny opis jest gorszy niz brak opisu (jego uwaga, test 6.10).
spr("skrot Control+F12 wolny w kodzie", "Control+F12" not in CS_KOD)
spr("Hotkeys.ini: zawijanie i rozwijanie opisane jako menu only",
    re.search(r"^Word Wrap=, .*menu only", HOT, re.M) is not None and
    re.search(r"^Unwrap=, .*menu only", HOT, re.M) is not None)
# KONTROLA, ZE NIE USUNIETO ZA DUZO: Control+W nadal zamyka okno, a same
# komendy nadal istnieja (zeszly do menu, nie zniknely).
spr("KONTROLA: komendy zawijania NADAL istnieja jako pozycje menu",
    "&Word Wrap" in MENU and "Unwrap" in MENU)

# 4h. Kursor za znacznikiem listy w podgladzie (5.0.34, jego zgloszenie:
# Enter na pozycji spisu tresci odmawial, bo kursor stal na punktorze).
# Poprawka MUSI stac w obsludze Enter, PRZED wszystkimi trzema sciezkami -
# raz, nie w kazdym konsumencie osobno.
spr("kursor przeskakuje znacznik listy w podgladzie",
    "MarkdownReview_SkipListMarkerAt" in CS)
spr("korekta objela CALA liste celow nawigacji, nie tylko pozycje biezaca",
    "MarkdownReview_ShiftPastListMarkers" in CS)


# 4i. Kreator tabeli na Control+Shift+T (5.0.35, rozdzial 18 listy testow).
# Cala forma to ustalenia Kasperczaka z rozmowy 28.08.2026 17:00-17:06.
spr("Insert Table pod Control+Shift+T",
    MENU.get("Insert Table ...") == "Control+Shift+T")
# Text Combine ODDAL chord, ale NIE ZNIKNAL - jego decyzja: laczenie plikow
# wroci osobno "kiedys indziej", najpewniej pod Control+Shift+klawisz funkcyjny.
spr("Text Combine jest komenda MENU-ONLY (oddal chord, istnieje)",
    MENU.get("Text Combine") == "")
spr("KONTROLA: Text Combine NADAL jest pozycja menu", "Text Combine" in MENU)
spr("Hotkeys.ini: Text Combine opisany jako menu only",
    re.search(r"^Text Combine=, .*menu only", HOT, re.M) is not None)
spr("Hotkeys.ini: kreator tabeli opisany pod Control+Shift+T",
    re.search(r"^Insert Table=Control\+Shift\+T,", HOT, re.M) is not None)
# Trzy bramki, ktore lista testow obiecuje w punktach 18.19-18.22.
for komunikat in ["Tables work only on Markdown files!",
                  "Cannot put a table inside a code block!"]:
    spr("komunikat bramki obecny: %r" % komunikat, ('"%s"' % komunikat) in CS_KOD)
# Wstawianie przez ReplaceRange, wiec Control+Z cofa (punkt 18.10) - regula z
# utraty pliku przy Format Code.
body_tab = CS[CS.find("private void RunMarkdownTableWizard("):]
body_tab = body_tab[:body_tab.find("} // RunMarkdownTableWizard")]
spr("tabela wstawiana przez ReplaceRange (cofalna)", "ReplaceRange" in body_tab)
spr("KONTROLA: wstawianie tabeli NIE zapisuje na dysk",
    "File.WriteAllText" not in body_tab and "SaveFile" not in body_tab)
spr("Escape pyta o potwierdzenie, gdy cos wpisane",
    "Close the table without inserting it?" in CS)
spr("puste kolumny i wiersze na koncu ucinane",
    "GetMarkdownTableGridExtent" in CS)
spr("kreska pionowa w tresci komorki zabezpieczana",
    "EscapeMarkdownTableCell" in CS)
spr("siatka jako osobna kontrolka biblioteki okien",
    "addTableGrid" in LBC and "class LbcGrid" in LBC)


# 4j. POLSKIE LITERY W PLIKU ANSI (5.0.36, rozdzial 19 listy testow).
# Pierwsza czesc punktu 3 mapy drogowej.  Ude nie rozpoznaje windows-1250, wiec
# bez tej bramki polski plik ANSI byl czytany jako UTF-8, a kazdy ogonek stawal
# sie U+FFFD - bezpowrotnie, bo zapis utrwalal uszkodzenie.
spr("helper IsStrictUtf8 istnieje", "public static bool IsStrictUtf8(" in CS_KOD)
spr("IsStrictUtf8 uzywa STRICT dekodera (rzuca, nie podstawia U+FFFD)",
    "new UTF8Encoding(false, true)" in CS_KOD)
# WYKRYWANIE KODOWANIA CZYTAMY Z DWOCH METOD RAZEM (od 5.0.95).
#
# Wolanie Ude zostalo WYDZIELONE do DetectEncodingUde, bo brak Ude.dll rzucal
# wyjatek przy KOMPILACJI metody wolajacej - czyli przed jakimkolwiek try, wiec
# nie dawal sie zlapac i program nie otwieral ZADNEGO pliku.  Asercje pytaja o
# zachowanie, ktore rozklada sie teraz na obie metody, wiec mierzymy ich sume.
body_det = CS[CS.find("public static Encoding DetectEncodingNoBom("):]
body_det = body_det[:body_det.find("} // DetectEncodingNoBom method")]
_body_ude = CS[CS.find("private static Encoding DetectEncodingUde("):]
_body_ude = _body_ude[:_body_ude.find("} // DetectEncodingUde method")]
body_det = body_det + "\n" + _body_ude
# ZAKTUALIZOWANE 13.09.2026 (5.0.95).  Asercja pytala o "Encoding.Default"
# (systemowa strona kodowa ANSI).  Kod poszedl PROSCIEJ I DALEJ: bez
# rozstrzygniecia detektora i przy bajtach niedozwolonych w UTF-8 wola
# PickPolishLegacyEncoding, ktore rozstrzyga miedzy windows-1250 i pokrewnymi -
# a to wlasnie ratuje polskie ogonki, o ktore ta sonda walczy.  Pytanie o
# Encoding.Default oblewalo wiec na kodzie LEPSZYM niz opisany.
spr("brak rozstrzygniecia detektora + nie-UTF-8 idzie na polskie kodowanie",
    "IsStrictUtf8(aBytes)" in body_det
    and "PickPolishLegacyEncoding(aBytes)" in body_det)
# KONTROLA POZYTYWNA: dotychczasowe zachowanie MUSI zostac nietkniete dla
# plikow, ktore UTF-8 SA, i dla wykrytych stron kodowych.
spr("KONTROLA: poprawny UTF-8 nadal idzie na utf8b", "return enUtf8b;" in body_det)
spr("KONTROLA: wykryta strona kodowa nadal honorowana",
    "CharsetName2Encoding(sCharset, enUtf8b)" in body_det)
spr("KONTROLA: sprawdzenie arytmetyczne UTF-16 nietkniete",
    "bAnyZero" in body_det)
# Odczyt i zapis musza uzywac TEGO SAMEGO kodowania, inaczej naprawa odczytu
# psulaby plik przy zapisie.
spr("zapis dokumentu uzywa kodowania, ktorym plik otwarto",
    "Encoding en = this.YieldEncoding;" in CS_KOD)
# Punkt 19.7 obiecuje, ze komendy zamiany koncow wiersza w otwartym dokumencie
# NIE MA - to musi byc prawda, inaczej lista testow klamie.
spr("KONTROLA punktu 19.7: brak komendy zamiany koncow wiersza w dokumencie",
    not any(k in MENU for k in ("Line Endings ...", "Convert Line Endings",
                                "Line Breaks ...")))
spr("KONTROLA punktu 19.7: eksport do mac i unx nadal istnieje",
    'sExt == "mac"' in CS_KOD and 'sExt == "unx"' in CS_KOD)


# 5. chordy obslugiwane w handlerach, nie w menu (nie moga byc w CreateMenuItem)
for chord_opis, wzor in [
    ("Control+W (Close Window, handler)", r"keyData != \(Keys\.Control \| Keys\.W\)"),
    ("Alt+cyfra (pliki numerowane)", r"HandleFileSlotKey"),
    ("Control+cyfra (okna)", r"HandleWindowNumberKey"),
    ("Control+Shift+cyfra (naglowki/listy)", r"HandleEditorFormattingKey"),
    ("Control+Alt+strzalki (przenoszenie sekcji)", r"HandleSectionMoveKey"),
    ("F7 w podgladzie (lista elementow)", r"keyCodeF7 == Keys\.F7"),
]:
    spr("handler obsluguje: %s" % chord_opis, re.search(wzor, CS) is not None)

# 6. parser naglowkow JEST fence-aware (twierdzenie testu 1.9 i 2.6)
body = CS[CS.find("GetMarkdownSectionHeadings(string"):]
body = body[:body.find("} // GetMarkdownSectionHeadings")]
spr("parser naglowkow pomija bloki kodu (fence)", "MarkdownReview_FindFenceRanges" in body)

# 7. Ctrl+H usuniete (twierdzenie testu 7.8)
spr("Control+H NIE jest zadnym skrotem",
    "Control+H" not in CS_KOD and "Control+&H" not in CS_KOD)
spr('KONTROLA: brak komendy "HTML Format"', '"HTML Format"' not in CS_KOD)

# 8. F6 i przeniesiony Go to Section opisane w Hotkeys.ini (mowiony opis)
spr("Hotkeys.ini: Document Navigation=F6", re.search(r"^Document Navigation=F6,", HOT, re.M) is not None)
spr("Hotkeys.ini: ZERO wzmianek o komendach naprawde usunietych",
    not re.search(r"^(Go to Section|Text Contents|HTML Format|Next Part|Prior Part|Go to Part)=", HOT, re.M))
spr("Hotkeys.ini: opisane NOWE komendy spisu tresci",
    re.search(r"^Go to Contents=Shift\+F6,", HOT, re.M) is not None and
    re.search(r"^Table of Contents=Alt\+Shift\+T,", HOT, re.M) is not None)
spr("Hotkeys.ini: zdania NIE obiecuja, ze EdSharp czyta",
    re.search(r"^Next Sentence=Alt\+DownArrow, [^\n]*screen reader reads it", HOT, re.M) is not None)

# 9. pole pamieci miejsca w drzewie (test 1.7)
spr("pamiec miejsca w drzewie F6", "DocumentNavigationLastOffset" in CS)

# 11. ROZDZIAL 20 (5.0.37): mowa w siatce, konce wiersza, kodowanie zapisu.
# Kazde twierdzenie listy musi miec pokrycie w kodzie - inaczej wysylam mu test,
# ktory sprawdza rzecz nieistniejaca.
spr("20.1/20.2 komorka siatki podaje tresc przed pozycja", "class LbcGridCell" in LBC)
spr("20.1/20.2 kolejnosc wspolrzednych zalezy od kierunku ruchu",
    "LastMoveWasVertical" in LBC and "DescribeCell" in LBC)
spr("20.3 pierwszy wiersz mowi, ze jest naglowkiem", '"header row"' in LBC)
spr("20.4 pusta komorka nie milczy", '"blank"' in LBC)
spr("20.5 Delete czysci komorke i potwierdza", '"Cleared"' in CS_KOD)
spr("20.5 Delete na pustej komorce mowi swoje", '"Cell is already empty"' in CS_KOD)
spr("20.6 edycja po F2 odrozniana od pisania", "EditStartedByF2" in LBC)
spr("20.7 stara strona kodowa zapisywana jako UTF-8", "GetSaveEncoding" in CS_KOD)
spr("20.9/20.11 rodzaj koncow wiersza rozpoznawany", "DetectLineBreakKind" in CS_KOD)
spr("20.9 rodzaj koncow wiersza pamietany per okno", "FileLineBreakKind" in CS_KOD)
spr("20.9 Alt+Z mowi rodzaj koncow wiersza", "line breaks " in CS_KOD)
spr("20.10 zapis nadal robi konce wiersza Windows", "Convert2WinLineBreak(sText)" in CS_KOD)
spr("20.12 eksport z koncami mac i unx nadal istnieje",
    '"mac"' in CS_KOD and '"unx"' in CS_KOD)
# KONTROLE, ze nie zepsulem tego, co dzialalo:
spr("KONTROLA 20.8: kodowania szerokie NIE ida na UTF-8 (1200 i 1201 w wyjatkach)",
    "iCode == 1200" in CS_KOD and "iCode == 1201" in CS_KOD)
spr("KONTROLA: jawne ustawienie YieldEncoding ma pierwszenstwo przy zapisie",
    'App.ReadOption("YieldEncoding", "").Trim().Length == 0' in CS_KOD)
spr("KONTROLA: naglowki wierszy siatki NIE dubluja numeru wiersza",
    '"Row " + i' not in CS_KOD)
spr("KONTROLA: kolumny siatki dodawane metoda pilnujaca dostepnosci",
    "addGridColumn" in LBC and "grid.Columns.Add(" not in CS_KOD)
# 19.7 mowilo, ze koncow wiersza NIE MA - po 5.0.37 to bylo by klamstwo.
spr("lista NIE twierdzi juz, ze koncow wiersza nie zrobiono",
    "jej NIE zrobiłem" not in LISTA)

# 10. lista testow nie cytuje komunikatu, ktorego w kodzie nie ma
cytaty = set(re.findall(r'"([A-Z][^"\n]{2,60})"', LISTA))
# Cytaty zlozone (komunikat + wstawiona nazwa pliku/cyfra) sprawdzamy po
# STALEJ czesci, bo pelnego zdania w kodzie nie ma i byc nie moze.
POMIN = {"Otwórz za pomocą", "Zawsze używaj tej aplikacji", "Akapit z listą",
         "EdSharp przeczyta", "HTML Format",
         "Parafia Wszystkich Świętych oraz żarówka"}
CZLONY = {"Above": "Above ", "Below": "Below ", "Heading 3": "Heading ",
          "Table saved": "Table saved, ",
          "Instalacja, heading 3": ", heading ",
          "Installation, heading 3": ", heading ",
          "Opening": "Opening ",
          "Numbered file 3 is empty!": " is empty!",
          "Numbered file 3 not found!": " not found!",
          "Numbered file 3 is nazwa.md": "Numbered file ",
          "Contents, 15 items": "Contents, ",
          "Contents updated": "Contents updated, ",
          "Instalacja, heading 2": ", heading ",
          "Footnote 1 inserted": "Footnote ",
          "Footnote N inserted": "Footnote ",
          "Cannot put a footnote inside the text of footnote 1!":
              "Cannot put a footnote inside the text of footnote ",
          "Footnotes": "Footnotes"}
# CZLON, KTORY JEST PREFIKSEM LITERALU, A NIE CALYM LITERALEM.
#
# CZLONY dopasowuja sie z cudzyslowem ZAMYKAJACYM, wiec dzialaja tylko wtedy,
# gdy szukany czlon jest calym literalem.  Komunikat zajetego schowka (5.0.60)
# taki nie jest: w kodzie stoi "Clipboard is busy, <co> not copied!" w siedmiu
# wariantach, wiec pytanie o "Clipboard is busy, " z zamykajacym cudzyslowem
# NIE MOGLO trafic - to falszywe zero z ksztaltu wzorca, nie brak w kodzie.
# Wpis do POMIN byl by tu oslabieniem sondy: przestala by pilnowac czegokolwiek.
# Dlatego osobna tablica dopasowywana jako PREFIKS literalu.
PREFIKSY = {"Clipboard is busy": "Clipboard is busy, "}
# "Removed" wystepuje w liscie SWIADOMIE jako komunikat HISTORYCZNY
# ("dawniej mowil samo Removed") - jego BRAK w kodzie jest dowodem naprawy,
# nie bledem listy.  Sprawdzane osobno nizej.
POMIN.add("Removed")
# "Format Code" wystepuje w liscie jako komenda USUNIETA 26.08.2026 - jej BRAK
# w kodzie jest dowodem wykonania zlecenia -7, nie bledem listy. Sprawdzana
# osobno w punkcie 4b wyzej.
POMIN.add("Format Code")
# "Go to Section", "Go to Contents" i "Text Contents" wystepuja w liscie jako
# komendy USUNIETE 26.08.2026 - ich BRAK w kodzie jest dowodem wykonania
# zlecenia -8, nie bledem listy. Sprawdzane osobno w punkcie 4c wyzej.
# "Go to Section" i "Text Contents" nadal opisuja komendy USUNIETE, wiec ich
# brak w kodzie jest dowodem, nie bledem listy. "Go to Contents" WROCILA
# 27.08.2026 jako nowa komenda i jest sprawdzana w punkcie 4d wyzej.
POMIN.update({"Go to Section", "Text Contents", "Go to Contents"})
# Komendy nawigacji po czesciach usuniete 27.08.2026 (zlecenie -1), lista
# wspomina je jako nieaktualne. Sprawdzane w punkcie 4e wyzej.
POMIN.update({"Next Part", "Prior Part", "Go to Part"})
# Komendy bogatego formatowania i obliczania daty usuniete 29.08.2026 (zlecenie
# 1788006241286-4, jego mini korekta). Lista testow wymienia je w rozdziale 27
# WLASNIE po to, zeby sprawdzil, ze ich nie ma - ich brak w kodzie jest dowodem
# wykonania, nie bledem listy. Sprawdzane osobno w punkcie 4d wyzej, na czterech
# warstwach kazda.
POMIN.update({"Next Alignment", "Prior Alignment", "Next Style", "Prior Style",
              "Next Baseline", "Prior Baseline", "Next Font", "Prior Font",
              "Calculate Date"})
# CYTATY Z POMIARU MOWY NVDA, a nie komunikaty programu.  Lista testow zapisuje
# doslownie, co powiedzial czytnik ("... , drzewo", "... , heading 1"), oraz
# uzywa slowa ZMIERZONE jako etykiety.  Takich zdan w kodzie nie ma i byc nie
# moze - sklada je czytnik z nazwy pozycji, roli kontrolki i tresci pliku
# testowego.  Stale czlony tych zdan ("Enter goes to the heading", ", heading ")
# sa sprawdzane osobno na liscie KOMUNIKATY wyzej.
# 5.0.45: rozdzial 28 wymienia nazwy komend USUNIETYCH i cytuje jego wlasne
# zdanie z ustalenia edsharpng-5, ktorego celowo nie wykonalem. Sonda pilnuje,
# ze kazdy cytat ma pokrycie w kodzie - te pokrycia miec nie moga i nie
# oslabiamy sondy, tylko nazywamy wyjatki, tak jak przy Format Code.
POMIN.update({"Next Block", "Prior Block", "Block", "Text Convert",
              "Control+F4 zmiana folderu"})
POMIN.update({"Enter goes to the heading, drzewo",
              "Plik testowy EdSharpNG 5.0.21, heading 1",
              "ZMIERZONE", "Wszystkich"})
# To samo dotyczy przykladowej wypowiedzi czytnika w siatce: takiego zdania w
# kodzie NIE MA i byc nie moze, bo sklada je DescribeCell z tresci komorki i
# jej wspolrzednych.  Zamiast szukac calego zdania, sprawdzamy jego STALE
# CZLONY - to robi asercja tuz nizej.
POMIN.add("Ala, column 1, row 1")
# 5.0.58: rozdzial 35 CYTUJE JEGO WLASNE SLOWA z wiadomosci i z pliku
# "Do usunięcia.txt" ("To chyba warto", "Nie wiem", "To chyba bysmy
# zostawili?"), oraz przytacza NAZWE PLIKU, ktory przyslal.  Takich zdan w
# kodzie nie ma i byc nie moze - to jego wypowiedzi, nie komunikaty programu.
# Nie oslabiamy sondy, tylko nazywamy wyjatki, tak jak przy pomiarach NVDA.
POMIN.update({"To chyba warto", "Nie wiem", "To chyba bysmy zostawili?",
              "Do usunięcia.txt"})
# "Delete removes the bookmark" to napis USUNIETY z paska stanu w 5.0.58 na jego
# prosbe ("niepotrzebnie czyta").  Rozdzial 35 cytuje go WLASNIE po to, zeby
# sprawdzil, ze go NIE SLYSZY - brak w kodzie jest wiec DOWODEM wykonania, nie
# bledem listy.  Asercja ponizej pilnuje tego wprost, w obie strony.
POMIN.add("Delete removes the bookmark")

brakujace = []
for c in sorted(cytaty):
    if c in POMIN:
        continue
    if c in PREFIKSY:
        if ('"%s' % PREFIKSY[c]) not in CS_KOD:
            brakujace.append(c)
        continue
    szukaj = CZLONY.get(c, c)
    if ('"%s"' % szukaj) not in CS_KOD:
        brakujace.append(c)
spr("kazdy cytat z listy ma pokrycie w kodzie (brakuje: %s)" % (brakujace or "nic"), not brakujace)
# KONTROLA WAZNOSCI DOPASOWANIA PREFIKSOWEGO: musi znajdowac czlon, ktory w
# kodzie JEST, i NIE znajdowac wymyslonego.  Bez tej pary dopasowanie prefiksem
# moglo by byc zawsze prawdziwe i asercja wyzej nic by nie dowodzila.
spr("dopasowanie prefiksowe znajduje istniejacy czlon (kontrola pozytywna)",
    '"Clipboard is busy, ' in CS_KOD)
spr("dopasowanie prefiksowe NIE znajduje wymyslonego czlonu (kontrola rozlaczna)",
    '"Clipboard is exploding, ' not in CS_KOD)
# 5.0.58: podpowiedz Delete ZESZLA z paska stanu list zakladek, a klawisz
# dziala nadal.  Oba warunki naraz, bo kazdy z nich osobno przechodzi tez dla
# zlej zmiany: samo zniknieciе napisu byloby zielone po usunieciu calej obslugi,
# a sama obsluga byloby zielona bez zdjecia podpowiedzi.
spr("podpowiedz Delete zeszla z paska stanu listy zakladek",
    'addListBox(lDisp, "", "Delete removes the bookmark")' not in CS
    and 'addListBox(lDisp, "", "Delete or Backspace removes the bookmark")' not in CS)
spr("klawisz Delete na listach zakladek DZIALA nadal",
    '"Bookmark removed"' in CS and '"Named bookmark removed"' in CS)
spr("klawisze list zakladek sa w pomocy F1, nie w pasku stanu",
    'setHelpDetail(lst, "Keys: Delete or Backspace removes the bookmark' in CS
    and 'setHelpDetail(lst, "Keys: Delete or Backspace removes the named bookmark' in CS)
# Strzalka w lewo na trzech listach - kazda mowi INNA brakujaca informacje.
# ODWROCONE 13.09.2026 (5.0.95).  Ta asercja pilnowala, ze strzalka w lewo na
# liscie zakladek MOWI NUMER WIERSZA ("Line 42").  Uzytkownik kazal to usunac:
# strzalka w lewo ma czytac TYLKO tresc wiersza, a pusty wiersz nazwac "Empty
# line".  Numer wiersza w tym momencie zagaduje to, po co sie tam siega.
# Pytamy wiec o stan obecny, nie o poprzedni.  (Asercja o tresci wiersza jest
# nizej i pilnuje wlasciwego zachowania.)
spr("lista zakladek: strzalka w lewo NIE mowi numeru wiersza",
    'Say.sayForced("Line " + (rtbHere.GetLineFromCharIndex(iCharAt) + 1));' not in CS)
spr("lista zakladek z nazwa: strzalka w lewo czyta tresc wiersza",
    'Say.sayForced(sRowRead.Length == 0 ? "Empty line" : sRowRead);' in CS)
spr("lista przypisow: strzalka w lewo czyta zdanie ze znacznikiem",
    'Say.sayForced(sMarkLine.Length == 0 ? "Empty line" : sMarkLine);' in CS)
# Nawigacja przypisow rozdzielona (jego decyzja 01.09.2026).
spr("skok po przypisach na Control+Alt+PageDown i PageUp",
    'CreateMenuItem("Next Footnote", "Control+Alt+PageDown"' in CS_KOD
    and 'CreateMenuItem("Prior Footnote", "Control+Alt+PageUp"' in CS_KOD)
spr("chord Control+Alt+Shift+K nie nalezy do zadnej komendy",
    not re.search(r'CreateMenuItem\("[^"]*", *"Control\+Alt\+Shift\+K"', CS_KOD))
spr("KONTROLA: ten sam wzorzec widzi Control+Alt+K jako przypisany",
    re.search(r'CreateMenuItem\("[^"]*", *"Control\+Alt\+K"', CS_KOD) is not None)
spr("Control+Alt+K poza przypisem nie skacze, tylko mowi",
    '"Put the cursor in a line with a footnote marker, or use Control+Alt+PageDown to find one!"' in CS)
# Chordy zwolnione, komendy ZOSTAJA w menu.
#
# ZMIANA ZAKRESU W 5.0.65: z tej listy wyszly DWIE pozycje, bo Kasperczak kazal
# usunac same komendy, a nie tylko zdjac im klawisz.  Extract with Regular
# Expression wtopilo sie w polaczona komende Regular Expression Tool, a Extra
# Speech Toggle zniknelo calkiem razem z wpisem w pliku ustawien.  Asercje o
# nich zeszly TU, a ich intencje przejely asercje ponizej ("komenda usunieta
# calkiem") - stan zerowy tez trzeba pilnowac, inaczej cicha powrotka nikogo
# nie obudzi.  Doszedl Hard Line Break, ktory wlasnie zostal menu-only.
for _nazwa, _chord in [("Environment Variables", "Control+E"),
                       ("Go to Environment", "Control+Shift+G"),
                       ("Hard Line Break", "Control+Shift+H"),
                       ("Repeat Line", "Control+Y")]:
    # Nazwa moze miec ampersand w dowolnym miejscu ("&Environment Variables"),
    # wiec wzorzec dopuszcza go przed kazda litera - falszywe zero z ampersandu
    # to zmierzona pulapka tego weryfikatora.
    _wz = "".join("&?" + re.escape(ch) for ch in _nazwa)
    spr("komenda %s zostala w menu bez skrotu" % _nazwa,
        re.search(r'CreateMenuItem\("%s[^"]*", *""' % _wz, CS_KOD) is not None)
    spr("chord %s nie nalezy do zadnej komendy" % _chord,
        not re.search(r'CreateMenuItem\("[^"]*", *"%s"' % re.escape(_chord), CS_KOD))
spr("Control+Y ponawia (drugi klawisz Redo, wolany w lancuchu)",
    "private bool HandleRedoAliasKey(Keys keyData) {" in CS
    and "if (HandleRedoAliasKey(keyData)) return true;" in CS
    and "menuEditRedo.PerformClick();" in CS)
spr("Redo NADAL na Control+Shift+Z (ustalenie edsharpng-72)",
    'CreateMenuItem("Redo", "Control+Shift+Z"' in CS_KOD)
# INTENCJA ZACHOWANA, NOSNIK ZMIENIONY (jego decyzja 03.09.2026, wariant B):
# liczenie trafien wyrazenia regularnego nadal ma byc pod Control+Shift+Y, ale
# jest teraz jednym z DWOCH dzialan polaczonej komendy, wybieranym w okienku.
# Stara asercja pytala o pozycje menu, ktorej on kazal sie pozbyc.
spr("liczenie i wypisywanie trafien to JEDNA komenda na Control+Shift+Y",
    'CreateMenuItem("Regular Expression Tool ...", "Control+Shift+Y"' in CS_KOD
    and 'CreateMenuItem("Yield with Regular Expression ...", "Control+Shift+Y"' not in CS_KOD)
spr("OBA dzialania zyja dalej, tylko wybierane w okienku",
    '"Count matches"' in CS
    and '"Extract matches to a new window"' in CS
    and "Util.RegExpCountCase(sText, sResult)" in CS
    and "Util.RegExpExtractCase(sText, sResult)" in CS)
spr("komenda Extract with Regular Expression usunieta CALKIEM (pole, menu, obsluga)",
    "menuMiscExtractWithRegExp" not in CS_KOD
    and 'CreateMenuItem("Extract with Regular Expression' not in CS_KOD)
spr("komenda Extra Speech Toggle usunieta CALKIEM (pole, menu, obsluga)",
    "menuMiscExtraSpeechToggle" not in CS_KOD
    and 'CreateMenuItem("Extra Speech Toggle"' not in CS_KOD)
# NAJWAZNIEJSZE PRZY TYM USUNIECIU: bez czyszczenia wpisu user z wylaczona
# mowa zostalby z cisza na stale i bez wlacznika.  Klucz niesie TEZ segment
# wyciszenia zmian wciecia, wiec czyszczenie nie moze byc slepym DeleteKey.
spr("usuniecie przelacznika mowy czysci wpis w pliku ustawien",
    "public static void ClearExtraSpeechOption() {" in CS
    and "ClearExtraSpeechOption();" in CS
    and 'ReadData("ExtraSpeechDropped"' in CS)
spr("czyszczenie wpisu ExtraSpeech NIE zabiera wyciszenia zmian wciecia",
    'if (sValue.Contains("-")) WriteOption("E&xtraSpeech", "-");' in CS
    and 'App.IndentChange = App.ReadOption("E&xtraSpeech", "Y").Contains("-") ? false : true;' in CS)
spr("dziennik mowy (Extra Speech Log) ZOSTAJE - to osobna funkcja",
    'CreateMenuItem("Extra Speech Log", "Alt+Shift+X"' in CS_KOD)
spr("Control+U, Control+Shift+U, Control+I, Control+Shift+I NIETKNIETE (pytanie, nie decyzja)",
    'CreateMenuItem("&Upper Case", "Control+U"' in CS_KOD
    and 'CreateMenuItem("Lower Case", "Control+Shift+U"' in CS_KOD
    and 'CreateMenuItem("Next Indent", "Control+I"' in CS_KOD
    and 'CreateMenuItem("Prior Indent", "Control+Shift+I"' in CS_KOD)
# INTENCJA ZACHOWANA, NOSNIK ZMIENIONY DWA RAZY. Etap 1 (jego decyzja 03.09.2026,
# wariant B): adres wypadl z wiersza, dopowiada go strzalka w lewo. Etap 2 (jego
# zgloszenie 04.09.2026, DRUGIE w tej sprawie po 01.09): z wiersza wypadl TAKZE
# numer wiersza, bo czytnik wymawial "line" i liczbe przy KAZDEJ pozycji, a przy
# dwudziestu odsylaczach to dwadziescia razy ten sam balast. Wspolrzedna NIE
# zostala skasowana - zeszla do dopowiedzenia strzalka w lewo, razem z adresem.
# Te dwie asercje pilnowaly stanu z etapu 1 i oblewaly po etapie 2 - to byl
# POPRAWNY protest sondy, nie regresja. Pytanie zostalo WZMOCNIONE: nie tylko
# "wiersz ma nowy ksztalt", ale tez "wspolrzedna ma NOSNIK ZASTEPCZY" - inaczej
# ta sama zielen objelaby skasowanie numeru wiersza w ogole.
spr("lista linkow: wiersz to SAMA TRESC, bez adresu i bez numeru wiersza",
    'lsShow.Add(GetMarkdownLinkListLine(link));' in CS
    and 'lsShow.Add(GetMarkdownLinkListLine(link) + ", line "' not in CS
    and 'lsShow.Add("Line " + iLine' not in CS)
spr("lista linkow: strzalka w lewo dopowiada adres ORAZ numer wiersza",
    "private static string GetMarkdownLinkAddressSpeech(MarkdownLink link, int iLine) {" in CS
    and "Say.sayForced(GetMarkdownLinkAddressSpeech(link, GetTextLineNumberAtIndex(sText, link.Start)));" in CS
    and 'return link.Url + sLine;' in CS_KOD)
spr("numer wiersza NIE zostal skasowany, tylko przeniesiony (metoda liczaca zyje)",
    "private static int GetTextLineNumberAtIndex(string sText, int iIndex) {" in CS
    and "GetTextLineNumberAtIndex(sText, link.Start)" in CS_KOD)
spr("podpowiedz okienka listy linkow mowi o adresie I numerze wiersza",
    "Left Arrow reads the web address and the line number" in CS
    and "Left Arrow reads the address and the line number" in MD)
spr("wiersz listy linkow liczony OSOBNO od mowy po skoku",
    "private static string GetMarkdownLinkListLine(MarkdownLink link) {" in CS
    and "Util.Say(GetMarkdownLinkSpeech(pick));" in CS)
spr("adres e-mail jest odsylaczem tez w plikach nie-Markdown",
    "MarkdownMailAddressRegex" in CS
    and "MarkdownWwwUrlRegex, MarkdownMailAddressRegex}" in CS)
spr("kopiowanie z formatowaniem dopisuje schemat mailto",
    'sUrl = "mailto:" + sUrl;' in CS_KOD)
# Stale czlony wypowiedzi czytnika w siatce (za pominiety cytat wyzej).
# UWAGA na kształt asercji: przecinek jest w kodzie WSTAWIANY osobno
# ("... + \", \" + sCol"), wiec szukanie ", column " w jednym literale padalo
# przy DOBRYM kodzie.  Sonda musi pytac o to, co w kodzie stoi.
spr("wypowiedz komorki sklada sie z czlonu 'column '", '"column "' in LBC)
spr("wypowiedz komorki sklada sie z czlonu 'row '", '"row "' in LBC)
spr("pusta komorka mowi 'blank'", '"blank"' in LBC)

# 4m. ROZDZIAL 21: edycja GOTOWEJ tabeli + tabela jednokolumnowa (5.0.38).
# Domkniecie punktu 2 mapy drogowej.
spr("kreator obsluguje OBA tryby (jedna metoda, nie dwie komendy)",
    "private void RunMarkdownTableWizard(" in CS
    and "private void InsertMarkdownTable(" not in CS)
spr("rozpoznanie gotowej tabeli pod kursorem istnieje",
    "private bool FindMarkdownTableAtCursor(" in CS)
spr("wypelnianie siatki gotowa tabela istnieje",
    "private void FillGridFromMarkdownTable(" in CS)
spr("kolumny przy edycji dokladane TYLKO dostepnie (addGridColumn)",
    "grid.Columns.Add(" not in CS)
spr("tytul okna rozroznia tryb: Edit Table vs Insert Table",
    'bEdit ? "Edit Table" : "Insert Table"' in CS)
spr("edycja podmienia ZAKRES starej tabeli, nie wstawia obok",
    "rtb.ReplaceRange(iTableStart, iTableEnd, sNowa)" in CS)
spr("Escape przy edycji pyta tylko gdy tresc sie ROZNI od tej z pliku",
    "BuildMarkdownTableFromGrid(grid, alignFound) != sOryginal" in CS)
spr("wyrownania kolumn z pliku odczytywane",
    "private static List<string> ParseMarkdownTableAlignments(" in CS)
spr("wyrownania kolumn odtwarzane w wierszu kreskek",
    "private static string MarkdownTableDashCell(" in CS)
spr("parser komorek jest ODWROTNOSCIA ucieczki (kreska zaslonieta zostaje trescia)",
    "private static List<string> ParseMarkdownTableRowCells(" in CS
    and "s[i + 1] == '|'" in CS)
# Wiersz kreskek: z wiodaca kreska pionowa wystarcza JEDNA kolumna.  Bez tego
# tabela jednokolumnowa nie byla tabela dla podgladu ani dla naszego eksportu
# HTML, choc nasz konwerter 2htm robil z niej prawidlowa tabele.
# UWAGA: CS jest czytane read_text, ktore normalizuje konce wiersza do LF -
# dzielenie po CRLF dawalo JEDEN element (caly plik) i sonda czytala CUDZE
# wyrazenie regularne.  Zlapane kontrola: cztery asercje padaly przy dobrym kodzie.
SEP = [l for l in CS.split("\n") if "MarkdownTableSeparatorRegex = new Regex" in l]
spr("wyrazenie wiersza kreskek istnieje dokladnie raz", len(SEP) == 1)
if SEP:
    m = re.search(r'new Regex\(@"([^"]+)"', SEP[0])
    spr("wyrazenie wiersza kreskek daje sie odczytac", m is not None)
    if m:
        rx = re.compile(m.group(1))
        spr("jednokolumnowy wiersz kreskek rozpoznany", rx.match("| --- |") is not None)
        spr("jednokolumnowy z wyrownaniem rozpoznany", rx.match("| :---: |") is not None)
        spr("wielokolumnowy nadal rozpoznany", rx.match("| --- | --- |") is not None)
        spr("bez wiodacej kreski nadal wymagane dwie kolumny", rx.match("--- | ---") is not None)
        # KONTROLE NEGATYWNE: to jest glowne ryzyko tej zmiany.
        spr("KONTROLA: pozioma linia z trzech myslnikow NIE jest wierszem kreskek", rx.match("---") is None)
        spr("KONTROLA: pozioma linia z czterech myslnikow NIE jest wierszem kreskek", rx.match("----") is None)
        spr("KONTROLA: gwiazdki NIE sa wierszem kreskek", rx.match("***") is None)
        spr("KONTROLA: punktor listy NIE jest wierszem kreskek", rx.match("- punkt listy") is None)
        spr("KONTROLA: dwie kreski to za malo", rx.match("| -- |") is None)
        spr("KONTROLA: wiersz danych NIE jest wierszem kreskek", rx.match("| a | b |") is None)
spr("edycja gotowej tabeli nadal NIE zapisuje na dysk",
    "File.WriteAllText" not in body_tab and "SaveFile" not in body_tab)

# ---------------------------------------------------------------- rozdzial 22
# Wiersz zero w siatce (jego zgloszenie z 28.08.2026 23:53) oraz kodowanie
# zapisu z koncami Linuksa i Maca.
spr("rozdzial 22 istnieje w liscie testow", "## 22." in LISTA)
spr("22.1 opisuje brak wiersza zero", "22.1." in LISTA and "wiersz zero" in LISTA)
spr("22.8 opisuje zapis dla Linuksa i Maca", "22.8." in LISTA)

# Wiersz siatki MILCZY - inaczej czytnik oglasza "Wiersz 0" przed komorka.
spr("wiersz siatki jest podstawiony jako szablon (inaczej poprawka nie dziala)",
    "dgv.RowTemplate = new LbcGridRow()" in LBC)
spr("wiersz siatki wycisza nazwe", "public override string Name {get {return \"\";}}" in LBC)
spr("wiersz siatki wycisza takze wartosc",
    'public override string Value {get {return "";} set {}}' in LBC)
spr("wiersz siatki podmienia role (przy roli wiersza czytnik mowi slowo wiersz)",
    "public override AccessibleRole Role {get {return AccessibleRole.Pane;}}" in LBC)
# KLUCZOWE: Clone MUSI wolac base.Clone().  Wlasne Clone budowalo wiersz BEZ
# KOMOREK i okno kreatora wywalalo sie na "Unexpected Event" - build byl zielony.
LBCL = LBC.split("\n")
i_row = next((i for i, l in enumerate(LBCL) if "public class LbcGridRow" in l), -1)
spr("klasa wyciszonego wiersza istnieje", i_row >= 0)
if i_row >= 0:
    body_row = "\n".join(LBCL[i_row:i_row + 40])
    spr("KLUCZOWE: Clone wiersza wola base.Clone (inaczej okno sie wywala)",
        "return base.Clone();" in body_row)
spr("ostrzezenie NIE UZYWAC zdjete (klasa jest teraz uzywana)",
    "LbcGridRow: NIE UZYWAC" not in LBC)

# Eksport z koncami Maca i Linuksa idzie w UTF-8, nie w ANSI systemu.
spr("eksport unx i mac zapisuje w UTF-8 bez znacznika",
    'else File.WriteAllText(sFile, sText, new UTF8Encoding(false));' in CS)
spr("KONTROLA: pozycja ASCII zostaje na kodowaniu systemowym",
    'if (sExt == "asc") File.WriteAllText(sFile, sText, Encoding.Default);' in CS)
spr("KONTROLA: eksport nadal zamienia konce wiersza na Mac",
    'sText = Util.Convert2MacLineBreak(sText);' in CS)
spr("KONTROLA: eksport nadal zamienia konce wiersza na Unix",
    'sText = Util.Convert2UnixLineBreak(sText);' in CS)
spr("KONTROLA: eksport ASCII nadal zdejmuje ogonki",
    'sText = Util.Convert2Ascii(sText);' in CS)
spr("podrecznik opisuje kodowanie eksportu (inaczej klamalby)",
    "The Mac and Unix options write the file in UTF-8" in MD)

# ---------------------------------------------------------------- rozdzial 23
# JEDEN RUCH, JEDEN GLOS w siatce (jego zgloszenie z 29.08.2026: "gh, column 1,
# row 2 | gh | zaznaczony" - tresc dwa razy i stan na koncu).
spr("rozdzial 23 istnieje w liscie testow", "## 23." in LISTA)
spr("23.3 opisuje brak slowa zaznaczony", "23.3." in LISTA and "zaznaczony" in LISTA)
spr("23.4 opisuje pusta komorke bez 'zerowy'", "23.4." in LISTA and "zerowy" in LISTA)

# Obiekt dostepnosciowy KOMORKI musi wyciszyc wartosc, opis I zaznaczalnosc.
# Zmierzone na zywym NVDA: nazwa to nie wszystko, czytnik sklada wypowiedz
# takze z wartosci i stanu.
i_cell = next((i for i, l in enumerate(LBCL) if "public class LbcGridCell" in l), -1)
spr("klasa komorki siatki istnieje", i_cell >= 0)
if i_cell >= 0:
    body_cell = "\n".join(LBCL[i_cell:i_cell + 90])
    spr("komorka wycisza WARTOSC (inaczej tresc leci drugi raz)",
        'public override string Value' in body_cell and 'get {return "";}' in body_cell)
    spr("komorka wycisza OPIS", 'public override string Description {get {return "";}}' in body_cell)
    spr("KLUCZOWE: komorka zdejmuje Selected ORAZ Selectable",
        "AccessibleStates.Selected | AccessibleStates.Selectable" in body_cell)
    # KONTROLA, ze nie zepsulem dzialajacego: nazwa nadal daje tresc i kierunek.
    spr("KONTROLA: komorka nadal podaje tresc w nazwie",
        "return DescribeCell(cell, bVertical);" in body_cell)
    spr("KONTROLA: kolejnosc wspolrzednych nadal idzie za kierunkiem ruchu",
        "grid.LastMoveWasVertical" in body_cell)
    spr("KONTROLA: obiekt komorki nadal dziedziczy po domyslnym (inaczej czytnik milknie)",
        ": DataGridViewCellAccessibleObject" in body_cell)
# KONTROLA, ze samo zdjecie Selected NIE zostalo uzyte bez Selectable - to byl
# GORSZY wynik w pomiarze (czytnik zaczynal mowic "niezaznaczony").
spr("KONTROLA: nie zdejmujemy samego Selected bez Selectable",
    "~AccessibleStates.Selected;" not in LBC)
# KONTROLA, ze naprawa wiersza z 5.0.39 stoi nietknieta.
spr("KONTROLA: wiersz siatki nadal wyciszony (naprawa 5.0.39 nietknieta)",
    "dgv.RowTemplate = new LbcGridRow()" in LBC)

# ---------------------------------------------------------------------------
# 5.0.41: F2 W SIATCE, PRZYPIS W ROZDZIALE, CONTROL+T ZDJETY
# ---------------------------------------------------------------------------

# F2 nie moze mowic "Formant edytowania" - zmierzone na zywym NVDA, przyczyna
# to NAZWA kontrolki edycyjnej, ktora tworzy WinForms.
spr("kontrolka edycyjna komorki dostaje wlasna nazwe (F2 bez 'edytowania')",
    "EditingControlShowing" in LBC)
spr("nazwa kontrolki edycyjnej to WSPOLRZEDNE komorki",
    "e.Control.AccessibleName = sWhere;" in LBC)
spr("opis kontrolki edycyjnej wyciszony (inaczej powtarza nazwe)",
    'e.Control.AccessibleDescription = "";' in LBC)
# KONTROLA, ze NIE wyciszamy rodzaju pola: to jedyna informacja mowiaca
# niewidomemu, ze moze teraz pisac.  Gdyby ktos ja zdjal, F2 stalby sie cichy.
spr("KONTROLA: rodzaj pola edycyjnego NIE jest wyciszany",
    "e.Control.AccessibleRole" not in LBC)
# KONTROLA, ze rozroznienie pisania od F2 stoi - bez niego tekst wpada w srodek
# poprzedniej tresci (zmierzone: \"Ala\" w \"30\" dalo \"3Ala0\").
spr("KONTROLA: rozroznienie pisania od F2 nadal dziala",
    "EditStartedByF2" in LBC and "bF2Pending" in LBC)

# PRZYPIS NA KONCU ROZDZIALU (jego zlecenie z 29.08.2026 01:40).
spr("przypis zna koniec biezacego rozdzialu",
    "GetMarkdownFootnoteChapterEnd" in CS)
spr("tresc przypisu w srodku dokumentu ma wlasny budownik odstepow",
    "BuildMarkdownFootnoteDefinitionAt" in CS)
spr("koniec rozdzialu liczony TYM SAMYM helperem co przestawianie sekcji",
    "GetMarkdownSectionEnd(s, headings, iHeading)" in CS)
spr("okno przypisu pyta o MIEJSCE tresci",
    "Footnote text &goes to" in CS)
spr("miejsca do wyboru to koniec dokumentu i koniec rozdzialu",
    "End of document" in CS and "End of current chapter" in CS)
spr("komunikat mowi, ze tresc poszla na koniec rozdzialu",
    "inserted at end of chapter" in CS)
# KONTROLA NEGATYWNA W KODZIE: pozycja \"koniec rozdzialu\" NIE MOZE trafic na
# liste, gdy rozdzialu nie ma - martwa pozycja jest dla niewidomego gorsza niz
# jej brak, bo wybiera ja i nie dostaje skutku.
spr("KONTROLA: pozycja rozdzialu dokladana warunkowo",
    "if (bChapterPossible) lPlaces.Add" in CS)
# KONTROLA, ze przy rozdziale znacznik idzie PIERWSZY: tresc wstawiana w srodek
# przesuwa offsety za soba, wiec odwrotna kolejnosc kladzie znacznik zle.
spr("KONTROLA: przy rozdziale offset tresci poprawiany o dlugosc znacznika",
    "iShifted += sRef.Length;" in CS)
# KONTROLA, ze stara droga (koniec dokumentu) NIE zostala usunieta.
spr("KONTROLA: droga na koniec dokumentu nadal istnieje",
    "BuildMarkdownFootnoteDefinition(sText, sLabel, sContent)" in CS)

# CONTROL+T: KOMENDA USUNIETA W CALOSCI w 5.0.45 (ustalenie edsharpng-1).
# Etap pierwszy (5.0.44) zdjal tylko chord i zostawil pozycje w menu; on kazal
# usunac FUNKCJE: "zglasza blad Unexpected Event i jest zbedna".
spr("Text Convert nie ma juz chordu Control+T",
    '"&Text Convert", "Control+T"' not in CS)
spr("Text Convert NIE ISTNIEJE juz jako pozycja menu",
    '"&Text Convert"' not in CS)
spr("pole menuMiscTextConvert usuniete z klasy i z handlera",
    "menuMiscTextConvert" not in CS)
spr("Control+T jest wolny (zadna komenda go nie ma)",
    '"Control+T"' not in CS)
# Bramka na tresc, ktora nie jest sciezka - przyczyna "Unexpected Event".
spr("lista plikow ma bramke na tresc niebedaca sciezka",
    "try {sTempDir = Path.GetDirectoryName(s);}" in CS)
spr("bramka pomija wiersz zamiast rzucac okno bledu",
    "catch {continue;}" in CS)
# KONTROLA: zaden opis mowiony nie moze juz obiecywac Control+T dla tej komendy.
# Czytamy hotkeys.txt osobno - HOT to Hotkeys.ini, a oba pliki musza sie zgadzac
# (drugi jest tracked MALA litera, patrz pulapka gita w skillu).
# PLIK PODSUMOWANIA SKROTOW: "EdSharp_Hotkeys.txt", nie "hotkeys.txt".
# Stara nazwa nie istnieje w repozytorium od 5.0.73, wiec sonda wywalala sie
# wyjatkiem, zamiast zmierzyc cokolwiek.
_HOTTXT_SUROWY = (REPO / "EdSharp_Hotkeys.txt").read_text(encoding="utf-8", errors="replace")

# FORMATY OBU PLIKOW SIE ROZNIA, TRESC MUSI BYC TA SAMA.
#   Hotkeys.ini          "Nazwa=Skrot, opis"   (czyta program)
#   EdSharp_Hotkeys.txt  "Nazwa, Skrot, opis"  (czyta czlowiek pod Alt+Shift+H)
# Asercje nizej pisane sa w zapisie z Hotkeys.ini, wiec sprowadzamy tekst
# podsumowania do tego samego zapisu.  Inaczej kazde pytanie o podsumowanie
# oblewalo z powodu przecinka zamiast znaku rownosci - czyli sonda mierzyla
# format, a chcemy mierzyc TRESC.
def _na_zapis_ini(tekst):
    wyj = []
    for linia in tekst.replace("\r\n", "\n").split("\n"):
        czesci = linia.split(", ", 1)
        wyj.append(czesci[0] + "=" + czesci[1] if len(czesci) == 2 else linia)
    return "\n".join(wyj)

_HOTTXT = _na_zapis_ini(_HOTTXT_SUROWY)

# PODSUMOWANIE MUSI BYC WYGENEROWANE ZE ZRODLA, nie dopisane rekami.  To jedyna
# asercja, ktora pilnuje samego mechanizmu: gdyby ktos poprawil txt bez ini,
# przy nastepnym budowaniu jego zmiana zniknie - lepiej dowiedziec sie teraz.
import subprocess
_gen = subprocess.run(
    [sys.executable, str(REPO / "testy" / "generuj_podsumowanie_skrotow.py"), "--sprawdz"],
    capture_output=True, text=True, env={**os.environ, "EDSHARP_REPO": str(REPO)})
spr("podsumowanie skrotow zgadza sie z Hotkeys.ini (jest generowane)",
    _gen.returncode == 0)
for _plik, _tresc in (("Hotkeys.ini", HOT), ("hotkeys.txt", _HOTTXT)):
    _zle = [l for l in _tresc.split("\n")
            if "Text Convert" in l and "Control+T" in l]
    spr("KONTROLA: %s nie obiecuje Control+T dla Text Convert" % _plik, not _zle)
# Opis mowiony (Ctrl+F1 czyta go niewidomemu) musi zniknac razem z komenda -
# martwy opis jest gorszy niz brak opisu.
spr("opis mowiony Text Convert zniknal z Hotkeys.ini",
    "Text Convert=" not in HOT)
spr("opis mowiony Text Convert zniknal z hotkeys.txt",
    "Text Convert=" not in _HOTTXT)
# KONTROLA POZYTYWNA tej sondy: Text Combine ZOSTAJE (dzielila handler i
# sasiedni wiersz opisow), inaczej "nie ma Text Convert" znaczylo by tylko
# "wywalilem caly ten fragment".
spr("KONTROLA POZYTYWNA: Text Combine nadal jest w Hotkeys.ini",
    "Text Combine=" in HOT)
spr("KONTROLA POZYTYWNA: Text Combine nadal jest w hotkeys.txt",
    "Text Combine=" in _HOTTXT)
spr("KONTROLA POZYTYWNA: Text Combine nadal ma pozycje menu i handler",
    '"Text Combine", ""' in CS and "menuItem == menuMiscTextCombine" in CS)
# KONTROLA, ze proza podrecznika nie kaze juz naciskac Control+T.
spr("KONTROLA: podrecznik nie kaze naciskac Control+T na Text Convert",
    "Text Convert command, Control+T" not in MD)
# KONTROLA, ze Control+Shift+T (kreator tabeli) NIE zostal ruszony - to sasiedni
# chord i jego wlasny wybor.
spr("KONTROLA: kreator tabeli nadal na Control+Shift+T",
    '"Control+Shift+T"' in CS)

# ROZDZIAL 24 LISTY TESTOW dla 5.0.41.
spr("rozdzial 24 istnieje w liscie testow", "## 24." in LISTA)
spr("24.1 opisuje wybor miejsca tresci przypisu",
    "24.1." in LISTA and "koniec bieżącego rozdziału" in LISTA)
spr("24.7 opisuje mowe po F2 bez slowa o edytowaniu",
    "24.7." in LISTA and "formant edytowania" in LISTA)
spr("24.10 opisuje wolny Ctrl+T", "24.10." in LISTA)
spr("24.12 nazywa PRZYCZYNE bledu, nie tylko objaw",
    "24.12." in LISTA and "wiersz tabeli" in LISTA)
# KONTROLA, ze punkt 23.9 NIE KLAMIE juz po tej wersji: mowil, ze Ctrl+T nie
# ruszalem, a wlasnie zostal zdjety.  Bez tej asercji lista testow przeczylaby
# programowi, a on testuje wedlug listy.
spr("KONTROLA: 23.9 nie twierdzi juz, ze Ctrl+T nietkniety",
    "nie ruszałem funkcji pod Ctrl+T" not in LISTA)


# ---- 5.0.42: LITERY DOSTEPU W OKNACH DIALOGOWYCH ----
# Jego decyzja z 29.08.2026 12:00:31 ("Skroty alt-litera w oknach dialogowych
# wprowadzamy. Enter zatwierdza, Esc anuluuje") plus uscislenie z 12:05:55
# ("We wszystkich").
spr("mechanizm doboru litery istnieje w Lbc.cs", "claimAccessKey" in LBC)
spr("litery pol nadawane PO przyciskach", "assignQueuedAccessKeys" in LBC)
spr("etykiety pol ida przez kolejke liter", "queueAccessKey(lbl" in LBC)
spr("zbior zajetych liter istnieje", "sAccessKeysTaken" in LBC)
# ASERCJA KLUCZOWA: jawny ampersand wolajacego NIE MOZE byc honorowany
# bezwarunkowo.  Zmierzone: okno ustawien kompilatora prosi o "&Name" i
# "&NavigatePart", wiec bezwarunkowe honorowanie zostawialo dwa pola na Alt+N,
# a jedno z nich bylo klawiszem nieosiagalne.  Sam build tego nie odrzuci.
_claim = LBC.split("private string claimAccessKey")[1].split("private void")[0] \
    if "private string claimAccessKey" in LBC else ""
spr("jawna litera sprawdzana, czy jest wolna",
    "sAccessKeysTaken.IndexOf(cGiven) < 0" in _claim)
spr("kolizja jawnej litery spada na dobor automatyczny",
    'sLabel = sLabel.Replace("&", "")' in _claim)
# ASERCJA, ze litera idzie na POCZATEK slowa.  Litera ze srodka slowa czyta sie
# jak literowka i nie jest tym, czego uzytkownik szuka.
spr("litera tylko na inicjale slowa", "bAtWordStart" in _claim)
# OK i Cancel BEZ litery - to jest doslownie jego decyzja, wiec asercja pilnuje,
# zeby nikt tego nie "poprawil" z powrotem.
spr("OK i Cancel bez litery dostepu",
    'string.Equals(sPlain, "OK", StringComparison.OrdinalIgnoreCase)' in LBC
    and "bNoAccessKey" in LBC)
spr("KONTROLA: stary bezwarunkowy ampersand na przycisku juz nie istnieje",
    'btn.Text = "&" + sLabel.Replace' not in LBC)
# KONTROLE, ze nie zepsulem dzialajacego.
spr("KONTROLA: UseMnemonic wlaczony na etykiecie pola",
    "lbl.UseMnemonic = true" in LBC)
spr("KONTROLA: pole wyboru nadal dostaje litere na sobie",
    "queueAccessKey(cb" in LBC)
spr("KONTROLA: przycisk opcji nadal dostaje litere na sobie",
    "queueAccessKey(rb" in LBC)
spr("KONTROLA: nazwa dostepnosciowa nadal bez ampersanda",
    "cleanLabel" in LBC and 'Replace("&", "")' in LBC)
spr("KONTROLA: mowa w siatce (5.0.40) nietknieta",
    "LbcGridCellAccessibleObject" in LBC)
spr("KONTROLA: nazwa kontrolki edycyjnej komorki (5.0.41) nietknieta",
    "EditingControlShowing" in LBC)
spr("KONTROLA: Clone nadal wola base.Clone (5.0.39)",
    "base.Clone()" in LBC)

# ROZDZIAL 25 LISTY TESTOW dla 5.0.42.
spr("rozdzial 25 istnieje w liscie testow", "## 25." in LISTA)
spr("25.1 mowi o OGLASZANIU litery przez czytnik",
    "25.1." in LISTA and "niewidzialna" in LISTA)
spr("25.3 to kontrola litery, ktorej nie ma", "25.3." in LISTA)
spr("25.6 wyjasnia, dlaczego OK i Anuluj bez litery",
    "25.6." in LISTA and "OK i Anuluj" in LISTA)
spr("25.13 nie gubi tematu skryptow JAWS", "25.13." in LISTA)

# ==================== KOMENTARZE WEWNETRZNE (5.0.43) ====================
# Punkt 4 mapy drogowej.  Te asercje pilnuja rzeczy, ktorych sam build NIE
# odrzuci: klawisz moze byc martwy, bramka moze zniknac, a skladnia moze zostac
# podmieniona na taka, ktora wychodzi do gotowego dokumentu.
spr("komentarze: klasa MarkdownComment istnieje",
    "class MarkdownComment" in CS)
spr("komentarze: wzorzec rozpoznaje komentarz HTML",
    "MarkdownCommentRegex" in CS and "<!--(?<body>.*?)-->" in CS)
# Bez Singleline komentarz wielowierszowy nie byl by rozpoznany, a uwaga
# robocza czesto ma kilka zdan.
spr("komentarze: wzorzec obejmuje komentarz wielowierszowy (Singleline)",
    "MarkdownCommentRegex" in CS and "RegexOptions.Singleline" in CS)
spr("komentarze: wszystkie cztery metody obslugi istnieja",
    all(m in CS for m in ("GetMarkdownComments", "InsertOrEditMarkdownComment",
                          "GoToMarkdownComment", "ShowMarkdownCommentList")))
spr("komentarze: tresc jest zabezpieczana przed zamknieciem komentarza",
    "SanitizeMarkdownCommentBody" in CS)
# To jest ta jedna rzecz, ktorej zlamanie NIE wywali buildu, a rozsypie
# dokument: wstawienie tresci bez zabezpieczenia.
spr("komentarze: wstawiana tresc PRZECHODZI przez zabezpieczenie",
    "SanitizeMarkdownCommentBody(sBody)" in CS)
spr("komentarze: cztery pozycje menu zadeklarowane",
    all(m in CS for m in ("menuMiscInsertComment", "menuMiscNextComment",
                          "menuMiscPriorComment", "menuMiscCommentList")))
# PRZEPISANA W 5.0.62, bo zaczela KLAMAC przy poprawnym kodzie.  Do 5.0.61 ta
# asercja pilnowala, ze wszystkie cztery komendy komentarzy siedza w rodzinie
# F9 - i tak bylo.  Jego ustalenie edsharpng-36 przenioslo SKOKI na rodzine
# PageUp/PageDown, wiec stara tresc dawala FAIL na kodzie zrobionym zgodnie z
# jego decyzja, zaciemniajac prawdziwe regresje w tym samym przebiegu (ta sama
# pulapka, co trzy asercje Control+F6 przy 5.0.53).
# Pilnujemy teraz tego, czego naprawde chcemy: wstawianie i lista ZOSTAJA w
# rodzinie F9, skoki sa na Alt+Shift+PageUp/PageDown, a stare chordy skokow
# nie naleza do NICZEGO.
# ODWROCONA W 5.0.96 na jego slowo: "skoro pod Ctrl Shift F9 i tak dalej ich
# wlasciwie nie potrzebujemy na razie, to bym usunal te klawisze.  Moze do tego
# wrocimy."  Wczesniejsza tresc WYMAGALA tych chordow, wiec po zdjeciu klawiszy
# dawalaby FAIL na kodzie zrobionym zgodnie z jego decyzja - ta sama pulapka, co
# przy 5.0.63.  Teraz pilnujemy tego, czego chcemy: POLECENIA zostaja (zeby dalo
# sie wrocic), a KLAWISZY przy nich NIE MA.
# Zakres doprecyzowany 13.09.2026: bez klawisza tylko rodzina F9 (wstawianie,
# lista); skoki maja klawisze Alt+Shift+PageDown/PageUp.
spr("komentarze: wstawianie i lista bez klawisza, skoki z klawiszem",
    'CreateMenuItem("Insert Comment ...", ""' in CS
    and 'CreateMenuItem("Comment List ...", ""' in CS
    and 'CreateMenuItem("Next Comment", "Alt+Shift+PageDown"' in CS
    and 'CreateMenuItem("Prior Comment", "Alt+Shift+PageUp"' in CS)
spr("komentarze: stare chordy skokow (Control+Shift+F9, Alt+Shift+F9) nie naleza do zadnej komendy",
    '"Control+Shift+F9"' not in CS and '"Alt+Shift+F9"' not in CS)
# ODWROCONA W 5.0.63, bo pilnowala zachowania, ktorego on sam sie pozbyl.
# Do 5.0.62 goly F9 i Shift+F9 obslugiwaly czytanie do konca przez skrypt JAWS
# (SayAllTempFile) WPROST w ProcessCmdKey_Helper, przed tablica skrotow menu, i
# ta asercja pilnowala, ze ich nie zabieramy komentarzom - slusznie, bo raz juz
# dala martwy klawisz przy zielonym buildzie.
# Jego decyzja z 03.09.2026 ("z tego klawisza F dziewiec i skryptu raczej
# rezygnujemy") zdjela cala obsluge, wiec asercja w starej postaci oblewalaby
# na kodzie zrobionym zgodnie z nia i zaciemniala prawdziwe regresje w tym samym
# przebiegu.  Pilnujemy teraz DRUGIEJ strony tej samej sprawy: warunek zniknal
# CALY, razem z osieroconym helperem COM.JFWRunFunction.
spr("F9: warunek golego F9 zszedl z ProcessCmdKey_Helper",
    "keyData == Keys.F9" not in CS_KOD)
spr("F9: warunek Shift+F9 zszedl razem z nim",
    "keyData == (Keys.Shift | Keys.F9)" not in CS_KOD)
# CS_KOD, NIE CS: asercja na surowym CS oblewala na komentarzu, ktory OPISUJE
# usuniecie helpera ("COM.JFWRunFunction USUNIETE razem z golym F9").  To trzeci
# taki falszywy alarm w tym projekcie - pytanie o BRAK czegos zadawaj zawsze na
# kodzie aktywnym, bo komentarz o usunieciu zawiera usuniete nazwy z definicji.
spr("F9: nie zostal osierocony helper COM.JFWRunFunction (lekcja z 5.0.44)",
    "JFWRunFunction(sText, ref App.JAWS)" not in CS_KOD
    and 'COM.JFWRunFunction' not in CS_KOD)
# KONTROLA WAZNOSCI TEGO FILTRA: gdyby CS_KOD wycinal za duzo, trzy asercje
# powyzej bylyby zielone zawsze.  Pytamy wiec o linie AKTYWNA, ktora istniec MUSI.
spr("kontrola waznosci filtra komentarzy: CS_KOD nadal widzi kod aktywny",
    'menuMiscCommentList = CreateMenuItem("Comment List ...", ""' in CS_KOD)
# ROZLACZNOSC: zdjecie obslugi F9 nie moglo zabrac ANI mowienia przez JAWS-a
# (COM.JFWSay, zywa droga w rodzinie kanalow mowy), ANI drugiego, niezaleznego
# czytania calego tekstu, ktore siedzi na Alt+F8 i nie ma z JAWS-em nic wspolnego.
spr("KONTROLA ROZLACZNA: mowienie przez JAWS-a (COM.JFWSay) ZOSTAJE",
    "public static bool JFWSay(string sText, ref object oJFW)" in CS)
spr("KONTROLA ROZLACZNA: czytanie calego tekstu na Alt+F8 nietkniete",
    'menuQueryReadAll = CreateMenuItem("Read All", "Alt+F8"' in CS
    and "Read All=Alt+F8," in HOT)
spr("KONTROLA: komentarze NIE przejely zwolnionych chordow bez jego slowa",
    'CreateMenuItem("Insert Comment ...", "F9"' not in CS
    and 'CreateMenuItem("Next Comment", "Shift+F9"' not in CS)
spr("komentarze: bramka na typ pliku (tylko Markdown)",
    "Comments work only on Markdown files!" in CS)
spr("komentarze: bramka na blok kodu przy wstawianiu",
    "Cannot put a comment inside a code block!" in CS)
spr("komentarze: blok kodu pomijany TYM SAMYM parserem co przypisy",
    "GetMarkdownComments" in CS and "IsMarkdownIndexInFence(fences, m.Index)" in CS)
spr("komentarze: skok bez zawijania mowi o koncu i o poczatku",
    "Last comment!" in CS and "First comment!" in CS)
spr("komentarze: brak komentarzy nazwany wprost", "No comments!" in CS)
spr("komentarze: usuniecie pusta trescia jest zglaszane glosem",
    "Comment removed" in CS)
spr("komentarze: lista podaje NUMER WIERSZA, nie numer kolejny",
    "GetTextLineNumberAtIndex" in CS and '"Line " + iLine' in CS)
# KONTROLA: czytnik musi slyszec TRESC PRZED slowem comment - jego wlasny wybor
# formy wypowiedzi, powtarzany w calym programie.
spr("KONTROLA: wypowiedz komentarza ma tresc PRZED slowem comment",
    's + ", comment"' in CS)
spr("KONTROLA: pusty komentarz nazwany osobno", '"empty comment"' in CS)
# Po zdjeciu klawiszy (5.0.96) wiersz zostaje, ale zaczyna sie od przecinka -
# pusty klawisz.  Opis MUSI zostac, bo z niego czyta sie podsumowanie skrotow.
spr("KONTROLA: opisy mowione komentarzy w Hotkeys.ini, bez klawiszy",
    "Insert Comment=," in HOT and "Comment List=," in HOT
    and "internal comment" in HOT)
# UWAGA na postac: _HOTTXT to plik .txt PRZELICZONY przez _na_zapis_ini na
# zapis "Nazwa=klawisz,opis", a nie surowa tresc z przecinkami.  Szukanie w nim
# frazy "(no key assigned)" nie trafia - zmierzone.  Sprawdzamy wiec pusty
# klawisz w postaci przeliczonej, a fraze dla czlowieka w pliku SUROWYM.
# UWAGA na postac: _HOTTXT to plik .txt PRZELICZONY przez _na_zapis_ini, ktory
# zamienia PIERWSZY przecinek na znak rownosci.  Brak klawisza jest w nim
# napisany dla czlowieka slowami "(no key assigned)", wiec po przeliczeniu
# wychodzi "Insert Comment=(no key assigned)", a nie "Insert Comment=,".
# Zmierzone: wzorzec z golym przecinkiem nie trafia.
spr("KONTROLA: opisy mowione komentarzy w hotkeys.txt, bez klawiszy",
    "Insert Comment=(no key assigned)" in _HOTTXT
    and "Comment List=(no key assigned)" in _HOTTXT
    and "internal comment" in _HOTTXT_SUROWY)
spr("KONTROLA: podrecznik opisuje komentarze wewnetrzne",
    "Internal comments" in MD)
# KONTROLA, ze nie zabralem dzialajacej komendy: Control+F9 nalezy do mowienia
# kompilatora i ma zostac.
spr("KONTROLA: Control+F9 nadal nalezy do Say Compiler",
    "Say Compiler=Control+F9," in HOT)
# Zadne z poleceh komentarzy nie moze miec klawisza - to jest cala tresc
# zadania 5.  Sprawdzane po WIERSZU, nie po samym braku ciagu "Alt+F9" w pliku
# (Alt+F9 wystepuje w opisach innych komend).
spr("KONTROLA: rodzina F9 bez klawisza, skoki z klawiszem",
    all((l.split("=",1)[1].split(",")[0].strip() == "")
        for l in HOT.splitlines()
        for n in ("Insert Comment", "Comment List")
        if l.startswith(n + "="))
    and any(l.startswith("Next Comment=Alt+Shift+PageDown") for l in HOT.splitlines())
    and any(l.startswith("Prior Comment=Alt+Shift+PageUp") for l in HOT.splitlines()))
# KONTROLE, ze nie zepsulem funkcji dzielacych z komentarzami ten sam kod.
spr("KONTROLA: przypisy nadal dzialaja, na Control+Shift+K",
    'CreateMenuItem("Insert Footnote ...", "Control+Shift+K"' in CS)
spr("KONTROLA: kreator tabeli nadal na Control+Shift+T",
    "Control+Shift+T" in CS and "RunMarkdownTableWizard" in CS)

# ROZDZIAL 26 LISTY TESTOW dla 5.0.43.
spr("rozdzial 26 istnieje w liscie testow", "## 26." in LISTA)
spr("26.8 jest oznaczony jako najwazniejszy punkt (efekt w Wordzie)",
    "26.8." in LISTA and "NAJWAZNIEJSZY PUNKT" in LISTA)
spr("26.9 pilnuje zabezpieczenia tresci komentarza", "26.9." in LISTA)
spr("26.11 to kontrola, ze Control+F9 nietkniety", "26.11." in LISTA)
spr("26.15 to kontrola przypisow i tabel", "26.15." in LISTA)


# ============ WERSJA 5.0.45: PORZADKOWANIE SKROTOW (zlecenie 1788041000816-5)
# Piec rodzin klawiszy naraz, wiec kazda ma asercje na WSZYSTKICH warstwach
# osobno. Jedna sonda po nazwie komendy daje zielone i przegapia slad - lekcja
# z osieroconego NavigatePart, ktory zlapal sam Kasperczak.

# --- 1. NAWIGACJA PO BLOKACH KODU: USUNIETA (ustalenie edsharpng-4) ---
# UWAGA POMIAROWA z tresci zlecenia: komenda nazywa sie menuNavigateNextBlock,
# BEZ slowa "Code". Wzorzec "NextCodeBlock" daje zero trafien i wyglada jak
# "juz usuniete" - falszywe zero z wzorca, ktory nie mogl trafic.
for _pole in ("menuNavigateNextBlock", "menuNavigatePriorBlock", "menuQueryBlock"):
    spr("5.0.45: %s zniknelo z kodu w calosci" % _pole, _pole not in CS)
for _nazwa in ('"Next Block"', '"Prior Block"'):
    spr("5.0.45: pozycja menu %s zniknela" % _nazwa,
        "CreateMenuItem(%s" % _nazwa not in CS)
spr("5.0.45: pozycja menu Block (pytanie o blok kodu) zniknela",
    'CreateMenuItem("Block"' not in CS)
for _opis in ("Next Block=", "Prior Block=", "Say Block="):
    spr("5.0.45: opis mowiony %s zniknal z Hotkeys.ini" % _opis, _opis not in HOT)
    spr("5.0.45: opis mowiony %s zniknal z hotkeys.txt" % _opis, _opis not in _HOTTXT)
spr("5.0.45: podrecznik nie obiecuje juz skokow po blokach kodu",
    "Press Control+B to go to the next code block" not in MD)
# KONTROLA, ze nie wycialem sasiadow: skoki po WCIECIACH zostaja, bo wciecie ma
# znaczenie takze w tekscie, a on rozstrzygal tylko o blokach kodu.
spr("KONTROLA 5.0.45: Next/Prior Indent nietkniete",
    'CreateMenuItem("Next Indent", "Control+I"' in CS
    and 'CreateMenuItem("Prior Indent", "Control+Shift+I"' in CS)
spr("KONTROLA 5.0.45: pytanie o wciecie (Alt+I) nietkniete",
    'CreateMenuItem("Indentation", "Alt+I"' in CS)
spr("KONTROLA 5.0.45: skoki po nawiasach klamrowych nietkniete",
    "menuNavigateRightBrace" in CS and "menuNavigateLeftBrace" in CS)
spr("KONTROLA 5.0.45: proza nadal opisuje skoki po wcieciu",
    "Press Control+I to go to the next change in indentation" in MD)

# --- 2. ZAKLADKI NA B (ustalenia edsharpng-4 i edsharpng-12) ---
# Przy PRZENOSZENIU sam nowy chord nie wystarcza: bez asercji o zniknieciu
# starego nie odroznisz przeniesienia od dopisania drugiej komendy.
spr("5.0.45: Set Bookmark jest na Control+B",
    'CreateMenuItem("Set Bookmar&k", "Control+B"' in CS)
spr("5.0.45: Set Bookmark NIE jest juz na Control+K",
    'CreateMenuItem("Set Bookmar&k", "Control+K"' not in CS)
spr("5.0.45: Go to Bookmark jest na Alt+B",
    'CreateMenuItem("Go to Bookmark", "Alt+B"' in CS)
spr("5.0.45: Go to Bookmark NIE jest juz na Alt+K",
    'CreateMenuItem("Go to Bookmark", "Alt+K"' not in CS)
# Clear Bookmark schodzi do MENU-ONLY, nie ginie: usuwanie zakladki ma isc
# przez Delete na liscie ("Delete na liscie wystarcza"), ale droga w menu musi
# zostac dla kogos, kto listy nie otwiera.
spr("5.0.45: Clear Bookmark jest menu-only (pusty chord)",
    'CreateMenuItem("Clear Bookmark", ""' in CS)
spr("KONTROLA 5.0.45: Clear Bookmark nadal DZIALA (handler jest)",
    "menuItem == menuNavigateClearBookmark" in CS)
spr("KONTROLA 5.0.45: lista zakladek z Delete istnieje (zastepuje klawisz)",
    "public static string PickBookmark" in CS
    and "Bookmark removed" in CS)
spr("5.0.45: opisy mowione zakladek podaja NOWE chordy w Hotkeys.ini",
    "Set Bookmark=Control+B," in HOT and "Go to Bookmark=Alt+B," in HOT)
spr("5.0.45: opisy mowione zakladek podaja NOWE chordy w hotkeys.txt",
    "Set Bookmark=Control+B," in _HOTTXT and "Go to Bookmark=Alt+B," in _HOTTXT)
spr("5.0.45: opis Clear Bookmark mowi o Delete na liscie, bez chorda",
    "Clear Bookmark=," in HOT and "Delete on the bookmark list" in HOT)
spr("5.0.45: podrecznik mowi o zakladce pod Control+B",
    "Press Control+B to set a bookmark" in MD
    and "Press Control+K to set a bookmark" not in MD)
# KONTROLA: kolejne / poprzednie zakladki na Shift+PageDown/PageUp zostaja.
spr("KONTROLA 5.0.45: Next/Prior Bookmark nietkniete",
    'CreateMenuItem("Next Bookmark", "Shift+PageDown"' in CS
    and 'CreateMenuItem("Prior Bookmark", "Shift+PageUp"' in CS)

# --- 3. WSTAWIANIE LINKU POD CONTROL+K (ustalenie edsharpng-9) ---
spr("5.0.45: Insert Link jest na Control+K",
    'CreateMenuItem("Insert &Link ...", "Control+K"' in CS)
spr("5.0.45: Insert Link ma handler (nie jest martwa pozycja menu)",
    "menuItem == menuMiscInsertLink" in CS)
spr("5.0.45: metoda InsertMarkdownLink istnieje",
    "private void InsertMarkdownLink" in CS)
# KLUCZOWA ASERCJA tej wersji, ktorej sam build NIE odrzuci: kotwica linku
# wewnetrznego MUSI byc liczona ta sama metoda, co odsylacze w spisie tresci.
# Wlasna implementacja specyfikacji dalaby link martwy po eksporcie, czego
# niewidomy nie ma jak zobaczyc.
_link = CS[CS.find("private void InsertMarkdownLink"):]
_link = _link[:_link.find("InsertMarkdownLink method")]
spr("5.0.45 KLUCZOWE: link wewnetrzny liczy kotwice przez BuildMarkdownAnchors",
    "BuildMarkdownAnchors" in _link)
spr("5.0.45: kotwice pochodza z naglowkow dokumentu, nie z pola adresu",
    "GetMarkdownSectionHeadings" in _link)
spr("5.0.45: naglowek BEZ kotwicy nie wchodzi na liste celow",
    'if (anchors[i].Length == 0) continue;' in _link)
spr("5.0.45: Insert Link ma bramke na typ pliku",
    "Links work only on Markdown files!" in CS)
spr("5.0.45: Insert Link ma bramke na podglad",
    "IsMarkdownIndexInFence" in _link)
spr("5.0.45: Insert Link odmawia obrazka bez tekstu alternatywnego",
    "An image needs a description for screen reader users!" in CS)
spr("5.0.45: link bez tresci dostaje adres jako tresc (odsylacz musi cos mowic)",
    "sLabelText = sAddress;" in _link)
spr("5.0.45: zmiana idzie przez ReplaceRange, wiec Control+Z ja cofa",
    "rtb.ReplaceRange(iStart, iEnd, sMarkup);" in _link)
spr("5.0.45: trzy rodzaje linku sa w JEDNYM oknie",
    "Web or file link" in CS and '"Image"' in CS and "Place in this document" in CS)
spr("5.0.45: opis mowiony linku jest w obu plikach",
    "Insert Link=Control+K," in HOT and "Insert Link=Control+K," in _HOTTXT)
spr("5.0.45: podrecznik opisuje wstawianie linku",
    "Press Control+K to insert a link" in MD)
# KONTROLA: szkielet linku pod Control+Shift+9 to INNA droga i zostaje.
spr("KONTROLA 5.0.45: Control+Shift+9 (szkielet linku) nietkniety",
    "InsertMarkdownLinkShortcut" in CS)

# --- 4. RODZINA F4 (ustalenia edsharpng-5, -6, -7, -8) ---
spr("5.0.45: Windows Open (mowienie tytulow okien) usuniete w calosci",
    "menuQueryWindowsOpen" not in CS and 'CreateMenuItem("Windows Open"' not in CS)
spr("5.0.45: metoda WindowsOpen nie zostala osierocona",
    "WindowsOpen" not in CS)
spr("5.0.45: opis mowiony Windows Open zniknal z obu plikow",
    "Windows Open=" not in HOT and "Windows Open=" not in _HOTTXT)
spr("5.0.45: podrecznik nie obiecuje tytulow okien pod Shift+F4",
    "Press Shift+F4 to hear the titles of open document windows" not in MD)
spr("5.0.45: Close All but Current jest na Control+Shift+W",
    'CreateMenuItem("Close All but Current Window", "Control+Shift+W"' in CS)
spr("5.0.45: Close All but Current NIE jest juz na Control+Shift+F4",
    'CreateMenuItem("Close All but Current Window", "Control+Shift+F4"' not in CS)
spr("5.0.45: Go to Folder jest na Shift+F4",
    'CreateMenuItem("Go to Folder", "Shift+F4"' in CS)
spr("5.0.45: Go to Folder NIE jest juz na Control+D0",
    'CreateMenuItem("Go to Folder", "Control+D0"' not in CS)
spr("5.0.45: Go to Special Folder jest na Control+Shift+F4",
    'CreateMenuItem("Go to Special Folder", "Control+Shift+F4"' in CS)
spr("5.0.45: Go to Special Folder NIE jest juz na Control+Alt+D0",
    'CreateMenuItem("Go to Special Folder", "Control+Alt+D0"' not in CS)
spr("5.0.45: opisy folderow podaja nowe chordy w Hotkeys.ini",
    "Go to Folder=Shift+F4," in HOT
    and "Go to Special Folder=Control+Shift+F4," in HOT)
spr("5.0.45: opisy folderow podaja nowe chordy w hotkeys.txt",
    "Go to Folder=Shift+F4," in _HOTTXT
    and "Go to Special Folder=Control+Shift+F4," in _HOTTXT)
spr("5.0.45: opis zamykania okien podaje Control+Shift+W",
    "Close All but Current Window=Control+Shift+W," in HOT
    and "Close All but Current Window=Control+Shift+W," in _HOTTXT)
spr("5.0.45: podrecznik opisuje uporzadkowana rodzine F4",
    "Control+Shift+W to close all windows except the current one" in MD)
# KONTROLA: Control+F4 (zamknij okno) NIETKNIETY. Ustalenie edsharpng-5
# wymienia "Control+F4 zmiana folderu", ale to jest standardowy klawisz
# zamykania okna w Windows - nie oddajemy go bez jego slowa.
spr("KONTROLA 5.0.45: Control+F4 nadal zamyka okno",
    'CreateMenuItem("&Close Window", "Control+F4"' in CS)
spr("KONTROLA 5.0.45: F4 nadal daje liste okien",
    'CreateMenuItem("Current Windows ...", "F4"' in CS)
spr("KONTROLA 5.0.45: Control+W nadal zamyka biezace okno",
    "HandleCloseWindowKey" in CS and "keyData != (Keys.Control | Keys.W)" in CS)
spr("KONTROLA 5.0.45: Control+1..9 nadal chodzi po oknach",
    "HandleWindowNumberKey" in CS)

# --- 5. ZWOLNIONE CHORDY nie naleza do zadnej komendy ---
# Chord "zwolniony", ktory nadal siedzi w CreateMenuItem, dawalby modalny Alert
# na starcie apki i po cichu zabijal komende.
_chordy_map = menu_map
_chordy = set(menu_map().values())
# AKTUALIZACJA 5.0.46: Control+Shift+K i Alt+K zostaly ZAJETE przez przypisy,
# wiec wypadaja z listy zwolnionych, a na ich miejsce wchodzi Control+Shift+F6
# (zwolniony po zejsciu listy przypisow do rodziny K).
# AKTUALIZACJA 5.0.56: Control+Shift+B (i Alt+Shift+B, zwolniony po zdjeciu
# nagrywania plyt) zostaly ZAJETE przez zakladki z nazwa - to byl ich cel od
# ustalenia edsharpng-40, wiec wypadaja z listy zwolnionych.  Weryfikator
# slusznie zaprotestowal przy tej zmianie: twierdzenie "zwolniony" przestalo
# byc prawda i to POPRAWKA LISTY, nie oslabienie sondy.
for _wolny in ("Control+Shift+F6",
               "Control+Shift+F7",
               "Control+D0", "Control+Alt+D0", "Control+T"):
    spr("5.0.45 ZWOLNIONY: %s nie nalezy do zadnej komendy" % _wolny,
        _wolny not in _chordy)
# 5.0.66: Control+Shift+F7 zwolniony przez zamiane dwoch komend guardu na jeden
# przelacznik.  Sama komenda Guard Document ZOSTAJE i musi miec swoj chord -
# bez tej asercji "zwolniony" nie odroznialby zamiany na przelacznik od
# przypadkowego usuniecia calej ochrony dokumentu.
spr("5.0.66: Guard Document nadal ma chord Control+F7",
    _chordy_map().get("Guard Document") == "Control+F7")
spr("5.0.66: komenda No Guard nie istnieje w zadnej postaci",
    not any(k.replace("&", "").replace("...", "").strip() == "No Guard"
            for k in _chordy_map()))
# ZAKLADKI Z NAZWA (5.0.56): oba chordy MUSZA teraz miec komende.
# UWAGA NA KLUCZ: menu_map() trzyma nazwy DOKLADNIE jak w kodzie, czyli z
# ampersandem litery dostepu i z kropkami okna ("Set &Named Bookmark ...").
# Pytanie o "Set Named Bookmark" dawalo FALSZYWE ZERO - wzorzec nie mial jak
# trafic.  Porownujemy po nazwie oczyszczonej, tak jak robi to audyt opisow.
_czysto = dict((k.replace("&", "").replace("...", "").strip(), v)
               for k, v in _chordy_map().items())
spr("5.0.56: Control+Shift+B nalezy do zakladki z nazwa",
    _czysto.get("Set Named Bookmark") == "Control+Shift+B")
spr("5.0.56: Alt+Shift+B nalezy do listy zakladek z nazwa",
    _czysto.get("Named Bookmark List") == "Alt+Shift+B")
# KONTROLA POZYTYWNA tego oczyszczania: nazwa BEZ ampersandu tez musi sie
# znalezc, inaczej slownik oczyszczony moglby byc pusty i oba testy wyzej
# przechodzilyby przypadkiem.
spr("KONTROLA 5.0.56: oczyszczony slownik nazw widzi komende bez ampersandu",
    _czysto.get("Next Bookmark") == "Shift+PageDown")
# KONTROLA POZYTYWNA tej petli: chord ZAJETY musi byc widziany, inaczej
# "nie nalezy" znaczylo by tylko "sonda nic nie widzi". Ta kontrola zlapala
# blad w samej sondzie: menu_map() mapuje NAZWE na CHORD, wiec chordow szuka
# sie w WARTOSCIACH, a pierwsza wersja pytala o klucze i przechodzila zawsze.
spr("KONTROLA 5.0.45: sonda zwolnionych chordow widzi chord ZAJETY",
    "Control+B" in _chordy and "Alt+B" in _chordy and "Control+K" in _chordy)

# --- ROZDZIAL 28 LISTY TESTOW dla 5.0.45 ---
spr("rozdzial 28 istnieje w liscie testow", "## 28." in LISTA)
spr("28.3 pilnuje zakladki pod nowym klawiszem", "28.3." in LISTA)
spr("28.6 to najwazniejszy punkt: link wewnetrzny po eksporcie", "28.6." in LISTA)
spr("28.11 opisuje cala rodzine F4 po kolei", "28.11." in LISTA)
spr("28.13 to PYTANIE o Control+F4, ktorego nie rozstrzygalem sam",
    "28.13." in LISTA and "PYTANIE" in LISTA)
spr("28.15 to kontrola, ze Text Combine zostala", "28.15." in LISTA)
spr("rozdzial 28 ma co najmniej piec kontrol, ze nie zepsulem dzialajacego",
    LISTA[LISTA.find("## 28."):].count("KONTROLA") >= 5)

# --- ROZDZIAL 29 LISTY TESTOW dla 5.0.46 (przypisy na rodzine K) ---
spr("rozdzial 29 istnieje w liscie testow", "## 29." in LISTA)
_R29 = LISTA[LISTA.find("## 29."):]
spr("29.1 kaze nacisnac NOWY klawisz wstawiania", "Control+Shift+K" in _R29)
spr("29.2 kaze nacisnac NOWY klawisz listy", "Alt+K" in _R29)
# NAJWAZNIEJSZA asercja tego rozdzialu: przy PRZENIESIENIU sam nowy klawisz nie
# dowodzi niczego. Lista testow MUSI kazac mu sprawdzic, ze stary MILCZY -
# inaczej duplikat komendy przeszedlby niezauwazony (build go nie odrzuca).
spr("29.3 kaze sprawdzic, ze STARE klawisze nic nie robia",
    "29.3." in _R29 and "Control+F6" in _R29 and "nie dzieje" in _R29)
# 29.4 mowil "KONTROLA: Alt+F6 nadal dziala" - po 5.0.47 to NIEPRAWDA, bo
# skok przeniosl sie na Control+Alt+K.  Punkt zostaje kontrola, ale jego
# tresc rozstrzyga rozdzial 30.
spr("29.4 to kontrola dotyczaca kontekstowego skoku",
    "29.4." in _R29 and "KONTROLA" in _R29)
spr("29.5 to PYTANIE o miejsce kontekstowego skoku", "29.5." in _R29 and "PYTANIE" in _R29)
spr("rozdzial 29 ma co najmniej cztery kontrole, ze nie zepsulem dzialajacego",
    _R29.count("KONTROLA") >= 4)
spr("29.9 mowi wprost, czego u nas NIE sprawdzimy",
    "29.9." in _R29 and "NIE sprawdz" in _R29)
# SPROSTOWANIA w starych rozdzialach: instrukcja podajaca stary klawisz kazalaby
# mu naciskac martwy chord i zglosic blad tam, gdzie bledu nie ma.
spr("16.11 podaje nowy klawisz listy przypisow",
    "16.11." in LISTA and "16.11" in LISTA
    and LISTA[LISTA.find("16.11."):LISTA.find("16.11.") + 60].find("Alt+K") > 0)
spr("28.17 nie kaze juz sprawdzac przypisu na Control+F6",
    "Control+F6 wstawiac przypis" not in LISTA)
spr("KONTROLA SPROSTOWAN: historyczne wzmianki o Control+F6 ZOSTAJA",
    "1.10." in LISTA and "Control+F6" in LISTA)

# ---- ROZDZIAL 30: skok przypisu na Control+Alt+K, koniec nagrywania plyt ----
_i30 = LISTA.find("## 30.")
_R30 = LISTA[_i30:] if _i30 >= 0 else ""
spr("rozdzial 30 istnieje w liscie testow", "## 30." in LISTA)
spr("30.1 kaze nacisnac NOWY klawisz skoku", "30.1." in _R30 and "Control+Alt+K" in _R30)
spr("30.2 opisuje powrot tym samym klawiszem", "30.2." in _R30 and "wracasz" in _R30)
# NAJWAZNIEJSZA asercja tego rozdzialu.  Sonda NVDA puszcza klawisze szybciej
# niz reka czlowieka, wiec ryzyko ciszy przy DLUZSZYM przytrzymaniu jest realne
# i tylko on moze je zmierzyc.  Lista MUSI go o to poprosic wprost.
spr("30.3 kaze sprawdzic mowe TAKZE przy dluzszym przytrzymaniu klawiszy",
    "30.3." in _R30 and "TRZYMAJ" in _R30 and "isza" in _R30)
spr("30.4 kaze sprawdzic SLYSZALNA odmowe bez przypisow",
    "30.4." in _R30 and "No footnotes!" in _R30)
# Przy PRZENIESIENIU sam nowy klawisz nie dowodzi niczego - stary musi milczec.
spr("30.6 kaze sprawdzic, ze STARY Alt+F6 nic nie robi",
    "30.6." in _R30 and "Alt+F6" in _R30 and "NIC" in _R30)
spr("30.8 kaze sprawdzic tryb opisywania klawiszy pod Control+F1",
    "30.8." in _R30 and "Control+F1" in _R30)
# PULAPKA MOJEJ SONDY: nazwa komendy "Burn to CD" jest w tekscie ZLAMANA
# zawinieciem wiersza ("Burn to\r\nCD"), wiec asercja na ciagly napis padala przy
# dobrym tekscie.  Ta sama klasa bledu co wzorzec "alt+t" w mowie NVDA, gdzie
# czytnik wstawia osobny segment.  Mierze po elemencie, ktory zawiniecia nie
# przechodzi - po chordzie.
spr("30.9 kaze sprawdzic, ze nagrywanie plyt zniknelo",
    "30.9." in _R30 and "Alt+Shift+B" in _R30 and "Burn to" in _R30)
# Kontrola helpera WSPOLNEGO - najgrozniejsza klasa bledu przy usuwaniu.
spr("30.10 to KONTROLA, ze Path List nie poszla razem z nagrywaniem",
    "30.10." in _R30 and "Path List" in _R30 and "KONTROLA" in _R30)
spr("rozdzial 30 ma co najmniej cztery kontrole, ze nie zepsulem dzialajacego",
    _R30.count("KONTROLA") >= 4)
spr("30.13 mowi wprost, czego u nas NIE sprawdzimy",
    "30.13." in _R30 and "NIE sprawd" in _R30)
# Uczciwosc dowodu: rozdzial ma powiedziec, ze poprawka mowy zostala zmierzona
# ROZNICUJACO (bez niej byla cisza), a nie tylko ze "testy przechodza".
spr("30.13 podaje kontrole roznicujaca (bez poprawki byla CISZA)",
    "CISZ" in _R30)
# SPROSTOWANIA w rozdziale 29: 29.4 i 29.5 mowily o Alt+F6 jako aktualnym.
spr("29.5 nie jest juz otwartym pytaniem o miejsce skoku",
    "ROZSTRZYGNI" in _R29)
spr("29.4 nie obiecuje juz, ze Alt+F6 nadal skacze",
    "Alt+F6 ma nadal skaka" not in LISTA)

# ---- ROZDZIAL 31: straznik pisania (5.0.48) ----
# Odpowiedz na jego pytanie z 30.08 11:45 o to, czy Control+Alt nie dubluje sie
# z prawym Altem, czyli z polskimi literami.
_i31 = LISTA.find("## 31.")
# ODCIECIE NA NASTEPNYM ROZDZIALE: bez niego _R31 obejmowalby takze rozdzial 32
# i asercje "31 mowi o X" przechodzilyby na tekscie, ktory do 31 nie nalezy.
_i32 = LISTA.find("## 32.")
_R31 = (LISTA[_i31:_i32] if _i32 > _i31 else LISTA[_i31:]) if _i31 >= 0 else ""
spr("rozdzial 31 istnieje w liscie testow", "## 31." in LISTA)
# Rozdzial MUSI potwierdzic, ze mial racje co do klawiatury: prawy Alt naprawde
# jest Control+Alt.  Zaprzeczenie temu bylo by odpowiedzia na inne pytanie.
spr("31 przyznaje, ze prawy Alt to Control z Altem",
    "prawy Alt" in _R31 and "Control z Altem" in _R31)
# Konkretne pary klawisz-litera, nie ogolnik "polskie znaki": on ma nacisnac
# klawisz, wiec musi wiedzieć ktory.
spr("31 podaje KONKRETNE pary litera-klawisz, nie ogolnik",
    "ą na klawiszu A" in _R31 and "ź na X" in _R31)
# Najwazniejsza tresc odpowiedzi: w TYM miejscu kolizji NIE MA.
spr("31 odpowiada wprost, ze w tym miejscu nie dubluje sie",
    "Nie dublowało się" in _R31 and "Control+Alt+K" in _R31)
spr("31.1 kaze napisac wszystkie dziewiec malych liter",
    "31.1." in _R31 and "ż" in _R31)
spr("31.2 kaze sprawdzic TEZ wielkie litery (prawy Alt z Shiftem)",
    "31.2." in _R31 and "Shift" in _R31 and "Ż" in _R31)
spr("31.4 to kontrola, ze pozostale komendy Control+Alt zyja",
    "31.4." in _R31 and "Control+Alt+F9" in _R31)
# KONTROLA PRZEWRAZLIWIENIA - straznik zbyt szeroki jest gorszy niz jego brak,
# bo odbiera dzialajace skroty przy zielonym buildzie.  Tylko on to zobaczy,
# bo alarm pada u niego przy starcie programu.
# PULAPKA MOJEJ SONDY (ta sama klasa co "Burn to CD" w rozdziale 30): fraza
# "NIE powinien" jest w tekscie ZLAMANA zawinieciem wiersza, wiec asercja na
# ciagly napis padala przy dobrym tekscie.  Mierze slowa OSOBNO.
spr("31.5 prosi o zgloszenie, gdyby straznik zlapal za duzo",
    "31.5." in _R31 and "przewrażliwiony" in _R31 and "powiedz jakiego" in _R31)
spr("31.6 pilnuje, ze zwykle skroty z samym Control lub Alt dzialaja",
    "31.6." in _R31 and "Control+S" in _R31 and "Alt+K" in _R31)
spr("31.7 mowi wprost, czego u nas NIE sprawdzimy (jego uklad klawiatury)",
    "31.7." in _R31 and "NIE sprawdzę" in _R31 and "układ" in _R31)
# Uczciwosc dowodu: rozdzial ma powiedziec, ze straznika zmierzono ROZNICUJACO
# (celowo zajety chord odebral litere), a nie tylko ze "testy przechodza".
# Tu tez zawiniecie lamie fraze, wiec mierze dwa slowa niezaleznie.
spr("31 podaje kontrole roznicujaca (celowo zajety chord odebral litere)",
    "celowo" in _R31 and "przestało wchodzić" in _R31)
spr("31 mowi, ze straznik pyta UKLAD, a nie ma listy liter w kodzie",
    "pyta układ" in _R31 and "listy liter" in _R31)

# ---- ROZDZIAL 32: nawigacja po wyroznieniach i listach (5.0.49) ----
# Jego rozstrzygniecie z 30.08 15:41 i 15:42 (ustalenia edsharpng-45, -46).
_i32b = LISTA.find("## 32.")
_R32 = LISTA[_i32b:] if _i32b >= 0 else ""
spr("rozdzial 32 istnieje w liscie testow", "## 32." in LISTA)
# Kazdy z czterech chordow MUSI byc w rozdziale nazwany, inaczej nie wie, czego
# probowac.  Mierze po CHORDZIE, nie po nazwie komendy: nazwa moglaby byc
# zlamana zawinieciem wiersza (pulapka z "Burn to CD" i "NIE powinien").
for _ch in ["Control z ukośnikiem", "Control+Shift z ukośnikiem",
            "Control z myślnikiem", "Control+Shift z myślnikiem"]:
    spr("32 nazywa chord %s" % _ch, _ch in _R32)
# NAJWAZNIEJSZE PUNKTY: to one pilnuja zachowania, ktorego build nie odrzuci.
spr("32.2 pilnuje, ze slychac TRESC, nie gwiazdki",
    "32.2." in _R32 and "bez gwiazdek" in _R32)
spr("32.5 to KONTROLA, ze komenda nie skacze na punktor listy ani na mnozenie",
    "32.5." in _R32 and "mnożeniem" in _R32 and "nazwa_z_podkresleniem" in _R32)
spr("32.6 to kontrola bloku kodu", "32.6." in _R32 and "bloku kodu" in _R32)
spr("32.7 pilnuje, ze odmowa MOWI, a nie milczy",
    "32.7." in _R32 and "No bold or italic text" in _R32 and "nie ma milczeć" in _R32)
spr("32.8 pilnuje POCZATKU listy, nie kolejnej pozycji",
    "32.8." in _R32 and "POCZĄTEK" in _R32)
spr("32.9 liczy przystanki: dwie listy to dwa przystanki",
    "32.9." in _R32 and "DWA przystanki" in _R32)
spr("32.12 to kontrola, ze rodzina Alt (pytania i okna formatu) zyje",
    "32.12." in _R32 and "Alt z ukośnikiem" in _R32)
spr("32.13 to kontrola, ze wciecia i przypisy nietkniete",
    "32.13." in _R32 and "Control+I" in _R32 and "Control+Alt+K" in _R32)
# SPROSTOWANIE MUSI BYC W LISCIE, nie tylko w wiadomosci: powiedzielismy mu
# rzecz nieprawdziwa o Control+F6 i lista testow jest tym, do czego wraca.
spr("32.14 prostuje falszywa diagnoze o Control+F6",
    "32.14." in _R32 and "Control+F6" in _R32 and "nigdy nie" in _R32.lower())
spr("32.14 przyznaje, ze jego objaw byl poprawny, a diagnoza nasza bledna",
    "objaw był poprawny" in _R32)
spr("32.15 mowi wprost, czego u nas NIE sprawdzimy",
    "32.15." in _R32 and "NIE sprawdzę" in _R32)
# Zaden punkt rozdzialu nie moze obiecywac listy linkow pod Control+F6 jako
# dzialajacej - to wlasnie ten blad prostujemy.
spr("32 NIE obiecuje, ze lista linkow pod Control+F6 dziala",
    "Control+F6 pokazuje listę" not in _R32 and "lista linków działa" not in _R32)

# ---- ROZDZIAL 33: lista linkow pod Control+F6 (5.0.53) ----
# Wykonanie ustalen edsharpng-37 (lista linkow na Control+F6), edsharpng-38
# (trzy klawisze na liscie) i edsharpng-47 (odebrac klawisz systemowi).
_i33 = LISTA.find("## 33.")
_R33 = LISTA[_i33:] if _i33 >= 0 else ""
spr("rozdzial 33 istnieje w liscie testow", "## 33." in LISTA)
# ZAKRES ROZDZIALU 32 musi sie na 33 KONCZYC, inaczej asercje 32 czytalyby tekst
# rozdzialu 33 i przechodzilyby na cudzej tresci.
_R32 = LISTA[_i32b:_i33] if (_i32b >= 0 and _i33 > _i32b) else _R32
# Kazdy z trzech klawiszy listy MUSI byc w rozdziale nazwany - inaczej nie wie,
# czego probowac.  Mierze po CHORDZIE, bo nazwa komendy moglaby byc zlamana
# zawinieciem wiersza (pulapka z "Burn to CD").
for _ch in ["Control+F6", "Control+C", "Control+Shift+C", "F2"]:
    spr("33 nazywa klawisz %s" % _ch, _ch in _R33)
spr("33.1 opisuje uklad wiersza listy: numer wiersza, tresc, adres, rodzaj",
    "33.1." in _R33 and "numer wiersza" in _R33 and "image" in _R33)
spr("33.2 pilnuje, ze kursor staje na TRESCI, nie na nawiasie",
    "33.2." in _R33 and "TREŚCI" in _R33 and "nawiasie" in _R33)
spr("33.4 pilnuje, ze pusty dokument MOWI, a nie otwiera pustego okna",
    "33.4." in _R33 and "No links" in _R33 and "pustego okna" in _R33)
spr("33.5 kaze SPRAWDZIC wklejeniem, ze kopiuje sie link, nie wiersz listy",
    "33.5." in _R33 and "wklejenie" in _R33 and "numerem wiersza" in _R33)
spr("33.6 opisuje, po co jest kopiowanie z formatowaniem (Word)",
    "33.6." in _R33 and "Wordzie" in _R33)
spr("33.7 to kontrola odmowy przy linku wewnetrznym",
    "33.7." in _R33 and "wewnętrznym" in _R33)
spr("33.8 opisuje okienko edycji i przebudowe listy po zmianie",
    "33.8." in _R33 and "Edit Link" in _R33)
spr("33.10 pilnuje, ze Control+Z cofa edycje i nic nie idzie na dysk",
    "33.10." in _R33 and "Control+Z" in _R33 and "dysk" in _R33)
spr("33.11 to kontrola odmowy przy obrazku bez opisu",
    "33.11." in _R33 and "obrazek" in _R33.lower() and "niewidzialny" in _R33)
spr("33.12 pilnuje golych adresow i kropki konczacej zdanie",
    "33.12." in _R33 and "kropka" in _R33)
spr("33.13 to kontrola bloku kodu", "33.13." in _R33 and "blok kodu" in _R33)
# TA ROZNICA jest swiadoma i musi byc mu ZGLOSZONA jako pytanie, nie przemilczana.
spr("33.14 wyjasnia brak bramki tylko-Markdown i PYTA go o to",
    "33.14." in _R33 and ".txt" in _R33 and "Powiedz, gdybyś" in _R33)
spr("33.15 to kontrola bramki podgladu",
    "33.15." in _R33 and "Close the preview first" in _R33)
spr("33.16 to kontrola, ze pozostale listy i F6 zyja",
    "33.16." in _R33 and "Alt+K" in _R33 and "Control+Alt+F9" in _R33)
spr("33.17 to kontrola rodziny Control+F6 i przelaczania okien",
    "33.17." in _R33 and "Control+Shift+F6" in _R33 and "Control+Tab" in _R33)
spr("33.18 mowi wprost, czego u nas NIE sprawdzimy",
    "33.18." in _R33 and "NIE sprawdzę" in _R33)
# Rozdzial NIE MOZE powtarzac sprostowania z 32.14 jako biezacego stanu: lista
# linkow WLASNIE powstala, wiec zdanie "nigdy nie zostala napisana" tu klamalo by.
spr("33 NIE powtarza, ze listy linkow nie napisano",
    "nigdy nie została napisana" not in _R33)

# ==========================================================================
# ZADANIE 5 i 7 z listy Michala (5.0.96, 13.09.2026)
#
# 5. "Skoro pod Ctrl Shift F9 i tak dalej ich wlasciwie nie potrzebujemy na
#    razie, to bym usunal te klawisze.  Moze do tego wrocimy."
#    Czyli: KLAWISZE precz, POLECENIA ZOSTAJA (zeby dalo sie wrocic).
# 7. "Tam wszystkie ustawienia maja wartosci do wpisywania w polach
#    tekstowych.  Chodzi mi o to, zeby to byly normalne opcje w formie
#    checkbox, pola kombi, takie jak we wszystkich aplikacjach."
# ==========================================================================
UST = (REPO / "Ustawienia.cs").read_text(encoding="utf-8", errors="replace")
UST_KOD = "\n".join(
    ("" if l.lstrip().startswith("//") else re.sub(r"//.*$", "", l))
    for l in UST.splitlines()
)

# --- 5: skroty komentarzy zdjete, polecenia zostaja ---
# Klawisz idzie do programu z Hotkeys.ini, wiec tam musi byc PUSTO po znaku
# rownosci.  Sprawdzamy dokladnie te cztery wiersze, a nie samo "nie ma
# ciagu Alt+F9" - bo Alt+F9 moglby zniknac razem z cala komenda.
# ZAKRES DOPRECYZOWANY 13.09.2026.  Michal: "mowilem o zmianie, usunieciu tych
# klawiszy funkcyjnych F9, okolo F9 do komentarzy, ale nie mowilem, zebys Alt
# Shift Page Up Page Down likwidowal, jezeli chodzi o nawigacje po tych
# komentarzach".  Czyli: bez klawisza zostaja WSTAWIANIE i LISTA (rodzina F9),
# a SKOKI wracaja na Alt+Shift+PageDown/PageUp.
_bez_klawisza = ("Insert Comment", "Comment List")
_z_klawiszem = {"Next Comment": "Alt+Shift+PageDown", "Prior Comment": "Alt+Shift+PageUp"}
for _nazwa in tuple(_bez_klawisza) + tuple(_z_klawiszem):
    _w = [l for l in HOT.splitlines() if l.startswith(_nazwa + "=")]
    spr("5. wiersz %r jest w Hotkeys.ini" % _nazwa, len(_w) == 1)
    if _w:
        _po = _w[0].split("=", 1)[1]
        _klawisz = _po.split(",")[0].strip()
        if _nazwa in _bez_klawisza:
            spr("5. %r NIE ma przypisanego klawisza (rodzina F9)" % _nazwa, _klawisz == "")
        else:
            spr("5. %r ma klawisz %s" % (_nazwa, _z_klawiszem[_nazwa]),
                _klawisz == _z_klawiszem[_nazwa])
        spr("5. %r ma dalej opis (polecenie zostaje)" % _nazwa,
            len(_po.split(",", 1)[1].strip()) > 10 if "," in _po else False)

# Polecenia MUSZA zostac w kodzie - Michal chce moc do nich wrocic.
for _cmd in ("Insert Comment", "Next Comment", "Prior Comment", "Comment List"):
    spr("5. polecenie %r nadal istnieje w programie" % _cmd,
        ('"%s' % _cmd) in CS_KOD)

# Zdjeta jest TYLKO rodzina F9.  Skoki po komentarzach MUSZA byc w kodzie -
# wersja 5.0.96 zabrala je za szeroko i Michal to zakwestionowal.
spr("5. Next Comment ma w kodzie Alt+Shift+PageDown",
    '"Next Comment", "Alt+Shift+PageDown"' in CS_KOD)
spr("5. Prior Comment ma w kodzie Alt+Shift+PageUp",
    '"Prior Comment", "Alt+Shift+PageUp"' in CS_KOD)
spr("5. Alt+F9 nie jest przypisany do komentarza",
    '"Insert Comment", "Alt+F9"' not in CS_KOD)
# Control+Alt+F9 to byla lista komentarzy.  Uwaga: Michal ma AltGr=Control+Alt,
# wiec ten skrot i tak zjadal polskie znaki - tym bardziej ma zniknac.
spr("5. Control+Alt+F9 nie jest przypisany do komentarza",
    "Control+Alt+F9" not in CS_KOD)
# Podrecznik nie moze kazac naciskac klawiszy, ktorych nie ma.
spr("5. podrecznik nie kaze naciskac Alt+F9 przy komentarzach",
    "Alt+F9 either inserts" not in MD)
spr("5. podrecznik mowi, ze komentarze sa w menu Navigate",
    "Navigate menu" in MD or "menu Navigate" in MD)
# Podrecznik musi mowic o przywroconych klawiszach - rozjechanie sie kodu z
# podrecznikiem raz juz wypuscilo wersje z klamliwym podsumowaniem skrotow.
spr("5. podrecznik podaje Alt+Shift+PageDown przy komentarzach",
    "Alt+Shift+PageDown" in MD)
spr("5. podrecznik podaje Alt+Shift+PageUp przy komentarzach",
    "Alt+Shift+PageUp" in MD)

# --- CONTROL+ALT A POLSKIE LITERY: co jest zmierzone, a co bylo mitem ---
# Michal (13.09.2026): "w EdSharpie to juz dzialalo wczesniej bezblednie,
# Alt-Ctrl-s nie wchodzilo w konflikt z s i tak dalej."  Mial racje.
# Zmierzone na zywym programie: przy komendzie przypisanej do Control+Alt+S
# litere "s z kreska" wpisuje LEWY Control+Alt tak samo jak prawy Alt
# (spor_ctrl_alt_s.ps1), a Control+Alt+K dziala jako skrot (kontrola_ctrl_alt_k.ps1).
# Czyli litera nigdy nie ginie - bez skutku zostaje SKROT.
# Dlatego: (1) nie zabraniamy przypisania, (2) NIE MA blokady na prawy Alt, bo
# uderzalaby w dzialajace Control+Alt+PageUp/Up wystukane prawym Altem.
spr("A. NIE MA blokady prawego Alta w obsludze klawiszy",
    "bPrawyAlt" not in CS_KOD)
spr("A. kod tlumaczy, dlaczego tej blokady nie ma",
    "DLACZEGO TU NIE MA KODU BLOKUJACEGO PRAWY ALT" in CS)
spr("A. kod przywoluje pomiar rozstrzygajacy spor",
    "spor_ctrl_alt_s.ps1" in CS and "kontrola_ctrl_alt_k.ps1" in CS)
spr("A. kod odwoluje falszywe zdanie o odbieraniu litery",
    "NIEPRAWDZIWE" in CS and "e z ogonkiem po cichu" in CS)
spr("A. straznik przypisywania NIE zabrania juz calego Control+Alt",
    "Cannot assign" not in CS_KOD.split("IsTypingChord(keyData)")[-1][:400])
spr("A. zamiast alarmu na ekranie jest wpis do dziennika",
    "LogDiagnostic" in CS_KOD)

# --- 7: okno ustawien z pol wyboru ---
spr("7. jest osobny plik Ustawienia.cs ze spisem ustawien",
    "class Ustawienia" in UST_KOD)
spr("7. Control+przecinek otwiera NOWE okno, nie stara liste pol tekstowych",
    "PokazUstawienia()" in CS_KOD)
spr("7. stare wolanie MultiInput dla ustawien usuniete",
    "App.ReadDefaultOptions()" not in CS_KOD)
# Rodzaje pol: musza byc wszystkie trzy, inaczej to nadal wpisywanie.
for _rodzaj in ('"przelacznik"', '"lista"', '"liczba"'):
    spr("7. okno zna rodzaj pola %s" % _rodzaj, _rodzaj in UST_KOD)
spr("7. przelacznik to CheckBox", "addCheckBox(" in UST_KOD or "addCheckBox(" in CS_KOD)
spr("7. lista wyboru to ComboBox", "addComboPickBox(" in UST_KOD or "addComboPickBox(" in CS_KOD)
spr("7. licznik to NumericUpDown", "addNumericUpDown(" in UST_KOD or "addNumericUpDown(" in CS_KOD)
# Lista wyboru musi byc ZAMKNIETA - czlowiek wybiera, nie wpisuje.
spr("7. z listy wyboru nie da sie wpisac wlasnej wartosci",
    "DropDownList" in LBC)
# Zapis: Y/N, zeby stary plik ustawien dalej dzialal.
spr("7. wlaczone zapisuje sie jako Y",
    'return bWlaczone ? "Y" : "N"' in UST_KOD)
# Rozpoznawanie idzie po pierwszej literze (y/t) plus "1" i "on" - wiec
# wzorzec musi pytac o TO, a nie o pelne slowa "yes"/"tak", ktorych w kodzie
# nie ma.  Zachowanie samo jest sprawdzone sonda pomiar_ustawienia_596.cs na
# wydanej binarce, na 17 roznych zapisach.
spr("7. czytanie wlaczonego znosi tez y, yes, tak, 1, on",
    'StartsWith("y")' in UST_KOD and 'StartsWith("t")' in UST_KOD
    and 's == "1"' in UST_KOD and 's == "on"' in UST_KOD)
# Ustawienia, ktore maja WLASNE okna, nie moga sie dublowac tutaj.
for _swoje in ('"RestoreSession"', '"AutoSaveSeconds"', '"FontDefault"'):
    spr("7. %s ma swoje okno i jest tu pomijane" % _swoje, _swoje in UST_KOD)
# Wycofane polecenia nie moga wrocic bocznymi drzwiami jako ustawienie.
spr("7. wycofany ExtraSpeech pomijany", '"ExtraSpeech"' in UST_KOD)
# Kazde pole musi miec zdanie wyjasniajace - inaczej nazwa typu "JumpPosition"
# nic nie mowi.
spr("7. okno ma opisy czytane przy wejsciu w pole", "Podpowiedz" in UST_KOD)
spr("7. Escape zamyka okno ustawien", "runOkCancel" in CS_KOD)

# --- 5.0.103: konwersja bogatych formatow (zmierzone, nie z lektury) ---

# Docelowy format to Markdown, nie RTF ani txt - zgloszenie Michala 13.09.2026
spr("konwersja: docelowy format to markdown", 'PreferredImportKey' in CS_KOD)
spr("konwersja: docx idzie do md", '"docx"' in CS_KOD and 'sExt + "2md"' in CS_KOD or 'PreferredImportKey' in CS_KOD)

# Klucz wstawiany centralnie, aby WSZYSTKIE drogi wejscia zachowaly sie tak samo
spr("konwersja: klucz ustawiany w jednym miejscu", CS_KOD.count("PreferredImportKey") >= 2)

# Brak narzedzia konwersji NIE MOZE byc awaria programu
spr("konwersja: brak konwertera nie wywala programu", "Win32Exception" in CS_KOD)
spr("konwersja: brak konwertera otwiera surowo", "iConvert = 0" in CS_KOD or "iConvert=0" in CS_KOD)

# Stare przelaczniki Pandoca z ini uzytkownika naprawiane w locie
spr("konwersja: markdown_github zamieniane na gfm", 'Replace("markdown_github", "gfm")' in CS_KOD)
spr("konwersja: stary przelacznik -S usuwany", "-S" in CS_KOD and "Regex.Replace(sCommand" in CS_KOD)

# Wpis objety cudzyslowem od poczatku do konca (ini Michala) - zdejmowanie pary
spr("konwersja: zewnetrzny cudzyslow zdejmowany",
    "sCommand.EndsWith" in CS_KOD and "sCommand.Substring(1, sCommand.Length - 2)" in CS_KOD)

# E-book nie moze przeciekac znacznikami HTML do Markdowna
spr("konwersja: epub bez surowego HTML", "raw_html" in CS_KOD)

# any2txt.cmd jest w repo (byl tylko na dyskach - blad nigdy nie znikal)
import os as _os
_any = _os.path.join(_os.path.dirname(_os.path.dirname(_os.path.abspath(__file__))), "Convert", "any2txt.cmd")
spr("konwersja: any2txt.cmd jest w repozytorium", _os.path.exists(_any))
if _os.path.exists(_any):
    _a = open(_any, encoding="utf-8", errors="replace").read()
    spr("konwersja: any2txt zdejmuje koncowy ukosnik katalogu", "outdir:~-1" in _a)
    spr("konwersja: any2txt nie sklada sciezki z koncowym ukosnikiem",
        "outdir:~-1" in _a)
# AUTOINSTALACJA KONWERTERA (decyzja Michala 13.09.2026: "jezeli potrzeba, to
# powinien sobie zainstalowac sam").  Brak narzedzia ma dawac PYTANIE, nie
# sam komunikat.
spr("skladniki: brak narzedzia daje pytanie o pobranie",
    'Dialog.Confirm("Missing Conversion Tool"' in CS_KOD)
spr("skladniki: pytanie nazywa brakujace narzedzie",
    "Skladniki.BrakujaceDlaPolecenia" in CS_KOD)
spr("skladniki: po zgodzie program pobiera",
    "Skladniki.SprawdzIUzupelnij(true)" in CS_KOD)

# Zapis do Program Files jest ODRZUCANY (asInvoker) - dociaganie musi miec
# zapasowy katalog w profilu, inaczej cicho przepada.
spr("skladniki: jest katalog zapasowy w profilu uzytkownika",
    "KatalogConvertUzytkownika" in SKL)
spr("skladniki: prawo zapisu sprawdzane probnym zapisem",
    "MoznaPisac" in SKL and "File.WriteAllText(sProba" in SKL)
spr("skladniki: pobieranie idzie do katalogu, gdzie wolno pisac",
    "KatalogDoZapisu" in SKL)
spr("skladniki: narzedzie szukane w obu miejscach",
    "KatalogiSzukania" in SKL)

# Wpis w ini wskazuje %ProgDir%\Convert - po pobraniu do profilu sciezke
# trzeba przeliczyc, inaczej plik sie nie otwiera mimo udanego pobrania.
spr("skladniki: sciezka narzedzia naprawiana przed uruchomieniem",
    "NaprawSciezkeNarzedzia" in SKL and "NaprawSciezkeNarzedzia" in CS_KOD)
spr("skladniki: sciezka przeliczana ponownie po pobraniu",
    CS_KOD.count("Skladniki.NaprawSciezkeNarzedzia") >= 2)
spr("skladniki: instalacja systemowa ma pierwszenstwo",
    "if (File.Exists(sStara)) return sPolecenie" in SKL)

# ODMOWA OTWARCIA formatow spakowanych (decyzja Michala: "Moim zdaniem nie ma
# otwierac").  Surowy docx/epub to smiec dla czytnika, a Control+S nadpisalby
# oryginal.
spr("odmowa: formaty spakowane rozpoznawane", "FormatSpakowany" in CS_KOD)
spr("odmowa: docx i epub na liscie spakowanych",
    '"docx"' in CS_KOD and '"epub"' in CS_KOD and '"xlsx"' in CS_KOD)
spr("odmowa: jest sygnal zakazu otwarcia", "OdmowaOtwarcia" in CS_KOD)
spr("odmowa: strona otwierajaca respektuje zakaz",
    "if (COM.OdmowaOtwarcia)" in CS_KOD)
spr("odmowa: sygnal jest zerowany po uzyciu",
    "COM.OdmowaOtwarcia = false" in CS_KOD)
spr("odmowa: rtf i pdf NIE sa blokowane (surowa tresc czytelna)",
    '"rtf"' not in CS_KOD.split("FormatSpakowany")[1].split("return false")[0]
    if "FormatSpakowany" in CS_KOD else False)

# NumericUpDown nie dostaje fokusu sam - nazwa musi zejsc na jego dzieci,
# inaczej czytnik ekranu mowi sama liczbe.  ZMIERZONE zywym NVDA.
spr("7. licznik podaje nazwe takze swojemu wewnetrznemu polu",
    "ctlChild.AccessibleName" in LBC)

ok = sum(1 for w, _ in wyniki if w)
print("WYNIK: %d/%d PASS" % (ok, len(wyniki)))
for w, n in wyniki:
    if not w:
        print("  FAIL: %s" % n)
sys.exit(0 if ok == len(wyniki) else 1)
