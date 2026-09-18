# pomiar_ctrl_l_warianty.ps1 - CZEGO MK MOGL DOTKNAC, SKORO PRZYPADEK
# PODSTAWOWY DZIALA.
#
# Zmierzone juz (pomiar_ctrl_l_pojedynczy.ps1, 5.0.113): "- [ ] kupic chleb"
# + Control+L daje "kupic chleb" - bez nawiasu i bez punktora.  Zgloszenie MK
# ("usuwa nawiasy kwadratowe pozostawiajac znaki - i cyfry z listy") na tym
# wejsciu sie NIE ODTWARZA.  Zamiast uznac zgloszenie za nieprawdziwe, mierze
# wejscia POKREWNE: wzorzec checklisty w Zadania.cs (PrefiksRegex) jest waski
# i wystarczy drobne odstepstwo, zeby wiersz przestal byc dla programu
# checklista, a zostal zwyklym punktorem z tekstem "[ ] ..." - i wtedy nawias
# ZOSTAJE w tresci dokladnie tak, jak MK opisal.
#
# WZORZEC (Zadania.cs): ^[ \t]*[-*+][ \t]+\[[ xX]\](?:[ \t]+|$)
# Czyli poza nim sa miedzy innymi:
#   - pole z innym znakiem niz spacja/x/X (czesty zapis "[-]" = w trakcie),
#   - brak spacji po nawiasie zamykajacym,
#   - wciecie zbudowane inaczej niz spacja/tabulator,
#   - punktor numerowany z polem ("1. [ ] ...") - wzorzec wymaga -, * albo +.
# Kazdy z nich mierze OSOBNO, bo kazdy to inna poprawka.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class FgW {
	[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
	[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
	[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
	[DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
	[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
	[DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool f);
	[DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
	[DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
	public static string Tytul() {
		StringBuilder sb = new StringBuilder(512);
		GetWindowText(GetForegroundWindow(), sb, 512);
		return sb.ToString();
	}
	public static bool NaWierzch(IntPtr h) {
		uint moj = GetCurrentThreadId();
		uint obcy = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
		if (obcy != 0 && obcy != moj) AttachThreadInput(moj, obcy, true);
		ShowWindow(h, 9); BringWindowToTop(h); SetForegroundWindow(h);
		System.Threading.Thread.Sleep(500);
		if (obcy != 0 && obcy != moj) AttachThreadInput(moj, obcy, false);
		return GetForegroundWindow() == h;
	}
}
"@

$exe = "C:\EdSharpBuild\EdSharpNG.exe"
function K($s, $ms = 800) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

function Widoczna($s) { return $s.Replace("`r", "<CR>").Replace("`n", "<LF>") }

# Mierzy JEDNO wejscie i zwraca tresc pliku po Control+L.
# Kontrola klawiatury NIE przerywa pomiaru - jej wynik idzie do wypisania,
# bo pierwsze uruchomienie po kompilacji potrafi zgubic pierwsze znaki, a
# nastepne juz nie.
function Mierz($nazwa, $rozsz, $tresc, $klawisze) {
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2
	$plik = "$env:TEMP\lw_$nazwa.$rozsz"
	Set-Content -Path $plik -Value $tresc -Encoding UTF8 -NoNewline

	$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
	Start-Sleep -Seconds 14
	$proc.Refresh()
	if (-not [FgW]::NaWierzch((Get-Process -Id $proc.Id).MainWindowHandle)) {
		"ODMOWA ($nazwa): aktywne <" + [FgW]::Tytul() + ">"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return $null
	}
	Start-Sleep 2

	K "^{END}" 500
	K "Z" 500
	K "^s" 1400
	$bKontrola = ([System.IO.File]::ReadAllText($plik) -match "Z$")
	K "{BACKSPACE}" 400
	K "^s" 1200

	K "^{HOME}" 700
	foreach ($k in $klawisze) { K $k 1500 }
	K "^s" 1600
	Start-Sleep 1
	$wynik = [System.IO.File]::ReadAllText($plik)
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2
	if (-not $bKontrola) { "   (kontrola klawiatury nie przeszla - patrz na tresc ponizej)" }
	return $wynik
}

"=================== CONTROL+L: WEJSCIA POKREWNE ==================="
""

# PIERWSZA POZYCJA JEST NA ROZGRZEWKE I NIE LICZY SIE DO WYNIKU.  Zmierzone
# trzema przebiegami: PIERWSZE uruchomienie programu w danym przebiegu gubi
# pojedyncze znaki z SendKeys (wychodzilo "kupc chleb", "kupic cheb", raz
# klawisze wsypaly sie w tekst jako "chl2. =->"), kolejne uruchomienia sa
# czyste.  To wada harnessu, nie programu - okno przyjmuje klawisze, zanim
# skonczy sie jego inicjalizacja.  Zamiast wydluzac czekanie w nieskonczonosc
# palimy pierwsze uruchomienie na wejsciu, ktorego wyniku nie czytam.
$aWejscia = @(
	@("rozgrzewka",  "- [ ] rozgrzewka",    "ROZGRZEWKA (wynik nieistotny)"),
	@("pole_minus",  "- [-] kupic chleb",   "pole ze znakiem minus (w trakcie)"),
	@("bez_spacji",  "- [ ]kupic chleb",    "brak spacji po nawiasie"),
	@("wciecie",     "  - [ ] kupic chleb", "pozycja wcieta o dwie spacje"),
	@("gwiazdka",    "* [ ] kupic chleb",   "punktor gwiazdka zamiast minusa"),
	@("numer_pole",  "1. [ ] kupic chleb",  "lista numerowana z polem wyboru"),
	@("duze_X",      "- [X] kupic chleb",   "pole z duza litera X")
)

foreach ($w in $aWejscia) {
	$nazwa = $w[0]; $wejscie = $w[1]; $opis = $w[2]
	$wynik = Mierz $nazwa "md" $wejscie @("^l")
	if ($wynik -eq $null) { continue }
	$wynik = $wynik.TrimEnd("`r", "`n")
	$sUwaga = ""
	if ($wynik -match "\[") { $sUwaga = "  <<< NAWIAS ZOSTAL W TRESCI" }
	"$opis"
	"   przed <$(Widoczna $wejscie)>"
	"   po ^L <$(Widoczna $wynik)>$sUwaga"
	""
}

"=================== KONIEC ==================="
