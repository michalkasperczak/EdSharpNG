// RozwijanieSnippetow.cs - czysta logika ręcznego rozwijania snippetów, bez UI i bez I/O.
using System;
using System.Collections.Generic;
using System.IO;

public enum OdmowaRozwijaniaSnippetow {
Brak,
Zaznaczenie,
NiepoprawnyKursor
}

public sealed class WynikRozwijaniaSnippetow {
public int StartTokenu { get; private set; }
public int DlugoscTokenu { get; private set; }
public string Prefiks { get; private set; }
public IList<string> DopasowaneSciezki { get; private set; }
public OdmowaRozwijaniaSnippetow Odmowa { get; private set; }
public bool IsExact { get; private set; }

internal WynikRozwijaniaSnippetow(int startTokenu, int dlugoscTokenu, string prefiks, IList<string> dopasowaneSciezki, OdmowaRozwijaniaSnippetow odmowa, bool isExact) {
StartTokenu = startTokenu;
DlugoscTokenu = dlugoscTokenu;
Prefiks = prefiks;
DopasowaneSciezki = dopasowaneSciezki;
Odmowa = odmowa;
IsExact = isExact;
}
} // WynikRozwijaniaSnippetow class

public static class RozwijanieSnippetow {
sealed class Kandydat {
public string Sciezka;
public string Nazwa;
public Kandydat(string sciezka, string nazwa) { Sciezka = sciezka; Nazwa = nazwa; }
} // Kandydat class

static WynikRozwijaniaSnippetow Odmow(int pozycjaKursora, OdmowaRozwijaniaSnippetow powod) {
return new WynikRozwijaniaSnippetow(pozycjaKursora, 0, "", new List<string>().AsReadOnly(), powod, false);
} // Odmow method

static bool JestWewnatrzParySurogatow(string tekst, int pozycjaKursora) {
return pozycjaKursora > 0 && pozycjaKursora < tekst.Length && char.IsHighSurrogate(tekst[pozycjaKursora - 1]) && char.IsLowSurrogate(tekst[pozycjaKursora]);
} // JestWewnatrzParySurogatow method

static bool ZnakTokenu(char znak) {
return char.IsLetterOrDigit(znak) || znak == '_' || znak == '-';
} // ZnakTokenu method

static int CompareKandydatow(Kandydat x, Kandydat y) {
int wynik = StringComparer.Ordinal.Compare(x.Nazwa, y.Nazwa);
return wynik != 0 ? wynik : StringComparer.Ordinal.Compare(x.Sciezka, y.Sciezka);
} // CompareKandydatow method

static List<Kandydat> Kandydaci(IEnumerable<string> sciezki) {
List<Kandydat> wynik = new List<Kandydat>();
HashSet<string> widzianeSciezki = new HashSet<string>(StringComparer.Ordinal);
if (sciezki == null) return wynik;
foreach (string sciezka in sciezki) {
if (sciezka == null || !widzianeSciezki.Add(sciezka)) continue;
try { wynik.Add(new Kandydat(sciezka, Path.GetFileNameWithoutExtension(sciezka))); }
catch (ArgumentException) { }
}
return wynik;
} // Kandydaci method

static IList<string> Sciezki(List<Kandydat> kandydaci) {
kandydaci.Sort(CompareKandydatow);
List<string> wynik = new List<string>();
foreach (Kandydat kandydat in kandydaci) wynik.Add(kandydat.Sciezka);
return wynik.AsReadOnly();
} // Sciezki method

// Nie odczytuje plików ani nie zmienia tekstu: UI dostaje tylko zakres i dopasowania.
public static WynikRozwijaniaSnippetow Dopasuj(string tekst, int pozycjaKursora, int dlugoscZaznaczenia, IEnumerable<string> sciezki) {
if (tekst == null || pozycjaKursora < 0 || pozycjaKursora > (tekst == null ? 0 : tekst.Length) || (tekst != null && JestWewnatrzParySurogatow(tekst, pozycjaKursora))) return Odmow(0, OdmowaRozwijaniaSnippetow.NiepoprawnyKursor);
if (dlugoscZaznaczenia > 0) return Odmow(pozycjaKursora, OdmowaRozwijaniaSnippetow.Zaznaczenie);
if (dlugoscZaznaczenia < 0) return Odmow(pozycjaKursora, OdmowaRozwijaniaSnippetow.NiepoprawnyKursor);

int start = pozycjaKursora;
while (start > 0 && ZnakTokenu(tekst[start - 1])) start--;
string prefiks = tekst.Substring(start, pozycjaKursora - start);
List<Kandydat> wszystkie = Kandydaci(sciezki);
List<Kandydat> dokladne = new List<Kandydat>();
List<Kandydat> prefiksy = new List<Kandydat>();
foreach (Kandydat kandydat in wszystkie) {
if (String.Equals(kandydat.Nazwa, prefiks, StringComparison.OrdinalIgnoreCase)) dokladne.Add(kandydat);
else if (kandydat.Nazwa.StartsWith(prefiks, StringComparison.OrdinalIgnoreCase)) prefiksy.Add(kandydat);
}

bool isExact = dokladne.Count > 0;
IList<string> dopasowane = Sciezki(isExact ? dokladne : prefiksy);
return new WynikRozwijaniaSnippetow(start, pozycjaKursora - start, prefiks, dopasowane, OdmowaRozwijaniaSnippetow.Brak, isExact);
} // Dopasuj method
} // RozwijanieSnippetow class
