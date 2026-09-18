# pomiar_paleta_enter.ps1 - CZY PALETA POLECEN NAPRAWDE URUCHAMIA POLECENIE.
#
# ZGLOSZENIE MK (18.09.2026, plik DO-PRZETESTOWANIA-5.0.108-5.0.112.md):
#   "Nowe skroty sa prawidlowo opisane, ale jak nacisniesz enter, nie wykonuje
#    sie nic, jak by paleta do nich nie doszla."
#
# CZEGO NIE MIERZE: czy okno palety sie otwiera - MK to potwierdzil ("Dziala").
# Mierze WYLACZNIE to, co dzieje sie po Enterze.
#
# ROZNICUJACY DOBOR POLECENIA: wybieram takie, ktore ZMIENIA PLIK NA DYSKU
# (Task List On Off dopisuje "- [ ] " na poczatku wiersza).  Zapisany plik jest
# dowodem niezaleznym od mowy i od tego, co widac na ekranie.
#
# TRZY PRZEBIEGI i to one czynia pomiar rozstrzygajacym:
#   A. z KLAWISZA (Control+Shift+F2) - punkt odniesienia.  Gdy A padnie, wina nie
#      lezy w palecie i szukanie w palecie byloby szukaniem w zlym miejscu.
#   B. z PALETY, ENTER W POLU FILTRA.
#   C. z PALETY, STRZALKA W DOL i Enter NA LISCIE.
# B i C osobno, bo w kodzie to DWIE rozne drogi (KeyDown pola filtra kontra
# KeyDown listy) i psuja sie niezaleznie.
#
# CZEGO ZABRAKLO PIERWSZEJ WERSJI TEGO POMIARU (18.09.2026): wymuszenia okna na
# wierzch (SetForegroundWindow z AttachThreadInput) i 14 sekund na start.  Bez
# tego WSZYSTKIE trzy przebiegi zglosily padnieta kontrole klawiatury - czyli
# kontrola zrobila swoje: nie pozwolila oglosic trzech bledow programu tam,
# gdzie byl jeden blad harnessu.  Baza jest teraz ta sama co w pomiar_zmiany_1b.

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -TypeDefinition @"
using System; using System.Text; using System.Runtime.InteropServices;
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

$global:iOk = 0
$global:iZle = 0

# Jeden przebieg.  $klawisze to lista klawiszy uruchamiajaca polecenie.
# Zwraca tresc pliku po zapisie albo $null, gdy pomiar byl gluchy.
function Mierz($nazwa, $klawisze) {
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2
	$plik = "$env:TEMP\pal_$nazwa.md"
	Set-Content -Path $plik -Value "wiersz probny" -Encoding UTF8 -NoNewline

	$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
	Start-Sleep -Seconds 14
	$proc.Refresh()
	if (-not [FgP]::NaWierzch((Get-Process -Id $proc.Id).MainWindowHandle)) {
		"ODMOWA ($nazwa): okna nie da sie postawic na wierzch, aktywne <" + [FgP]::Tytul() + ">"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return $null
	}
	Start-Sleep 2

	# KONTROLA POZYTYWNA KLAWIATURY: litera na koniec wiersza i zapis.
	K "^{END}" 500
	K "Z" 500
	K "^s" 1400
	if ((Get-Content $plik -Raw) -notmatch "wiersz probnyZ") {
		"ODMOWA ($nazwa): kontrola padla, klawisze nie dochodza do okna"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return $null
	}

	K "^{HOME}" 700
	foreach ($k in $klawisze) { K $k 1800 }
	K "^s" 1600
	Start-Sleep 1
	$wynik = Get-Content $plik -Raw
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2
	return $wynik
}

function Ocen($co, $wynik) {
	if ($wynik -eq $null) { $global:iZle++; "ZLE  $co (pomiar gluchy)"; return }
	if ($wynik -match "\- \[ \] wiersz probnyZ") { $global:iOk++; "OK   $co - polecenie wykonane" }
	else { $global:iZle++; "ZLE  $co - polecenie NIE wykonane, w pliku: <" + ($wynik -replace "`r?`n","\n").Trim() + ">" }
}

"=================== POMIAR: CZY PALETA URUCHAMIA POLECENIE ==================="
""

# A. Punkt odniesienia - to samo polecenie z klawisza.
Ocen "A. Control+Shift+F2 z klawisza (punkt odniesienia)" (Mierz "a_klaw" @("^+{F2}"))

# B. Paleta, Enter w polu filtra.
Ocen "B. Paleta (Control+Shift+F1), wpisane 'task list', ENTER W POLU" (Mierz "b_pole" @("^+{F1}", "task list", "{ENTER}"))

# C. Paleta, strzalka w dol i Enter na liscie.
Ocen "C. Paleta, wpisane 'task list', STRZALKA W DOL, Enter na liscie" (Mierz "c_lista" @("^+{F1}", "task list", "{DOWN}", "{ENTER}"))

""
"=================== WYNIK: $($global:iOk) OK, $($global:iZle) ZLE ==================="
