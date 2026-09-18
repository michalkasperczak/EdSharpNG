# pomiar_ctrl_l_pojedynczy.ps1 - POWTORKA WARIANTU 1 Z SUROWYM PODGLADEM.
#
# W pomiarze zbiorczym (pomiar_listy_checklista.ps1) wariant 1 nie dal wyniku:
# PowerShell zglosil "You cannot call a method on a null-valued expression" na
# $w.TrimEnd, czyli funkcja Mierz zwrocila null TAM, gdzie pozostale piec
# wariantow zwrocilo tresc.  To NIE JEST wynik pomiaru - to awaria harnessu i
# tak trzeba ja traktowac, bo z niej nie wynika ani ze program dziala, ani ze
# nie dziala.
#
# NAJBARDZIEJ PRAWDOPODOBNA PRZYCZYNA (do potwierdzenia, nie do zalozenia):
# Get-Content -Raw na pliku PUSTYM zwraca $null.  Jesli Control+L zostawil w
# pliku pusty tekst, harness dostal null i wywrocil sie na TrimEnd.  Ale rownie
# dobrze plik mogl zostac skasowany albo zapis nie doszedl.  Dlatego tu NIE
# porownuje napisow, tylko WYPISUJE stan pliku: czy istnieje, ile ma bajtow i
# jak wyglada jego tresc w postaci widocznej (z nawiasami ostrymi na brzegach).

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class FgP {
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

# Pokazuje stan pliku BEZ zadnych zalozen o tym, co w nim jest.
function Pokaz($etykieta, $plik) {
	if (-not (Test-Path $plik)) { "$etykieta : PLIKU NIE MA"; return }
	$bajty = (Get-Item $plik).Length
	$tresc = [System.IO.File]::ReadAllText($plik)
	$widoczna = $tresc.Replace("`r", "<CR>").Replace("`n", "<LF>")
	"$etykieta : $bajty bajtow, tresc <$widoczna>"
}

"=================== CONTROL+L NA JEDNEJ POZYCJI CHECKLISTY ==================="
""

Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2
$plik = "$env:TEMP\li_pojedynczy.md"
Set-Content -Path $plik -Value "- [ ] kupic chleb" -Encoding UTF8 -NoNewline
Pokaz "PRZED         " $plik

$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 14
$proc.Refresh()
if (-not [FgP]::NaWierzch((Get-Process -Id $proc.Id).MainWindowHandle)) {
	"ODMOWA: okno nie weszlo na wierzch, aktywne <" + [FgP]::Tytul() + ">"
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	exit 2
}
Start-Sleep 2

# KONTROLA POZYTYWNA KLAWIATURY.
K "^{END}" 500
K "Z" 500
K "^s" 1400
# KONTROLA, KTORA NIE PRZERYWA POMIARU.  Wczesniej wychodzilem tu z bledem, a
# to zamykalo pomiar bez zadnej wiedzy o PRZYCZYNIE.  Teraz wypisuje tytul
# aktywnego okna i stan pliku, i mierze dalej: jesli klawisze nie dochodza,
# dalsze linie pokaza plik nietkniety i bedzie to widac wprost.
$sPoKontroli = [System.IO.File]::ReadAllText($plik)
if ($sPoKontroli -notmatch "Z") {
	"UWAGA: kontrola klawiatury nie przeszla."
	"       aktywne okno <" + [FgP]::Tytul() + ">"
	Pokaz "       plik    " $plik
	"       mierze dalej - stan pliku nizej rozstrzyga, czy klawisze dochodza."
}
K "{BACKSPACE}" 400
K "^s" 1200
Pokaz "PO KONTROLI   " $plik

K "^{HOME}" 700
K "^l" 1500
K "^s" 1600
Start-Sleep 1
Pokaz "PO CONTROL+L  " $plik

# Drugie nacisniecie: jesli pierwsze zdjelo liste, drugie ma ja zalozyc z
# powrotem jako ZWYKLY punktor (bez pola wyboru).  To mowi, czy stan po
# pierwszym nacisnieciu jest tym, za co program go uwaza.
K "^{HOME}" 700
K "^l" 1500
K "^s" 1600
Start-Sleep 1
Pokaz "PO DRUGIM ^L  " $plik

Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
""
"Ocena: w wierszu PO CONTROL+L nie ma prawa zostac ani nawias kwadratowy,"
"ani goly punktor - ma zostac sama tresc 'kupic chleb'."
