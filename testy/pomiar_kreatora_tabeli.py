#!/usr/bin/env python3
"""Pomiar na BINARCE: Control+Shift+T nalezy do kreatora tabeli, a Text Combine
zostal bez skrotu (menu-only), ale ISTNIEJE.

Kazde twierdzenie ma kontrole POZYTYWNA - sonda musi znalezc rzeczy, ktorych
nie ruszalismy. Bez tego "nie widze" jest nie do odroznienia od "nie ma".
Pulapka .NET: nazwy metod/typow siedza w metadanych jako UTF-8, literaly z
kodu jako UTF-16LE, i to w OBU wyrownaniach bajtowych - patrz skill.
"""
import sys, pathlib

exe = pathlib.Path("/mnt/d/projekty/edsharp-pr/EdSharpNG.exe")
d = exe.read_bytes()
tekst = (d.decode("utf-16-le", "ignore") + "\n"
         + d[1:].decode("utf-16-le", "ignore") + "\n"
         + d.decode("utf-8", "ignore") + "\n"
         + d.decode("latin-1", "ignore"))

wyniki = []
def sprawdz(nazwa, warunek):
    wyniki.append((nazwa, bool(warunek)))
    print(("PASS  " if warunek else "FAIL  ") + nazwa)

print("Binarka:", exe, exe.stat().st_size, "B")

# --- KONTROLE POZYTYWNE: sonda widzi rzeczy nietkniete ---
sprawdz("kontrola pozytywna: chord Alt+Shift+T (spis tresci) obecny",
        "Alt+Shift+T" in tekst)
sprawdz("kontrola pozytywna: chord Control+F6 (przypis) obecny",
        "Control+F6" in tekst)
sprawdz("kontrola pozytywna: nazwa komendy Repeat Line obecna",
        "Repeat Line" in tekst)

# --- WLASCIWY POMIAR ---
sprawdz("nowa komenda Insert Table ... obecna w binarce",
        "Insert Table ..." in tekst)
sprawdz("chord Control+Shift+T nadal w binarce (przeszedl do tabeli)",
        "Control+Shift+T" in tekst)
sprawdz("Text Combine NIE zniknal - komenda istnieje jako menu-only",
        "Text Combine" in tekst)
sprawdz("metoda kreatora tabeli w metadanych",
        "RunMarkdownTableWizard" in tekst)
sprawdz("stara nazwa InsertMarkdownTable JUZ NIE ISTNIEJE (kreator obsluguje oba tryby)",
        "InsertMarkdownTable" not in tekst)
sprawdz("rozpoznanie gotowej tabeli pod kursorem w metadanych",
        "FindMarkdownTableAtCursor" in tekst)
sprawdz("wypelnianie siatki gotowa tabela w metadanych",
        "FillGridFromMarkdownTable" in tekst)
sprawdz("parser komorek wiersza tabeli w metadanych",
        "ParseMarkdownTableRowCells" in tekst)
sprawdz("odczyt wyrownan kolumn w metadanych",
        "ParseMarkdownTableAlignments" in tekst)
sprawdz("metoda BuildMarkdownTableFromGrid w metadanych",
        "BuildMarkdownTableFromGrid" in tekst)
sprawdz("klasa siatki LbcGrid w metadanych", "LbcGrid" in tekst)
sprawdz("komunikat bramki na typ pliku obecny",
        "Tables work only on Markdown files!" in tekst)
sprawdz("komunikat bramki na blok kodu obecny",
        "Cannot put a table inside a code block!" in tekst)
sprawdz("pytanie potwierdzenia przy Escape obecne",
        "Close the table without inserting it?" in tekst)
sprawdz("tytul okna edycji gotowej tabeli obecny",
        "Edit Table" in tekst)
sprawdz("pytanie potwierdzenia przy edycji gotowej tabeli obecne",
        "Close the table without saving your changes?" in tekst)
sprawdz("komunikat po zapisaniu edytowanej tabeli obecny",
        "Table saved, " in tekst)
sprawdz("komunikat o niezmienionej tabeli obecny",
        "Table not changed" in tekst)

# --- POMIAR W ZRODLE: Text Combine ma PUSTY chord w CreateMenuItem ---
src = pathlib.Path("/mnt/d/projekty/edsharp-pr/EdSharp.cs").read_text(encoding="utf-8")
sprawdz('Text Combine w kodzie ma pusty chord (menu-only)',
        'CreateMenuItem("Text Combine", "", menuItem_Click' in src)
sprawdz('Insert Table w kodzie ma chord Control+Shift+T',
        'CreateMenuItem("Insert Table ...", "Control+Shift+T", menuItem_Click' in src)
sprawdz("Insert Table jest w AddRange menu Misc",
        "menuMiscTextCombine, menuMiscInsertTable, menuMiscTableOfContents" in src)
sprawdz("Insert Table ma zadeklarowane pole ToolStripMenuItem",
        "menuMiscInferIndent, menuMiscInsertTable" in src or "menuMiscInsertTable," in src)

zle = [n for n, ok in wyniki if not ok]
print()
print("WYNIK: %d/%d PASS" % (len(wyniki) - len(zle), len(wyniki)))
sys.exit(1 if zle else 0)
