# pomiar_uwagi_mk_113.ps1 - POMIAR ZGLOSZEN MICHALA z DO-PRZETESTOWANIA-5.0.108-5.0.112.md
#
# Mierzy TRZY rzeczy, ktorych lektura kodu nie rozstrzyga:
#   A. paleta polecen (Control+Shift+F1): czy Enter W POLU FILTRA uruchamia polecenie
#      (zgloszenie: "skroty sa prawidlowo opisane, ale jak nacisniesz enter, nie
#      wykonuje sie nic"),
#   B. Control+L i Control+Shift+L na wierszach, ktore SA checklista - co dokladnie
#      zostaje w pliku (zgloszenie: "usuwa nawiasy kwadratowe pozostawiajac znaki -
#      i cyfry z listy"),
#   C. Alt+lewy nawias kwadratowy - czy otwiera menu File (chord po usunietym PyDent).
#
# KONTROLA POZYTYWNA: przed kazda grupa asercji wpisujemy znak i zapisujemy plik.
# Gdy klawisze nie dochodza, sonda ODMAWIA pomiaru zamiast zwracac wynik.

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms

$exe = "C:\EdSharpBuild\EdSharpNG.exe"
$plik = "$env:TEMP\pomiar_mk113.md"

$trescStart = @"
# Pomiar

- [ ] kupic chleb
- [ ] kupic mleko
zwykly wiersz
"@
Set-Content -Path $plik -Value $trescStart -Encoding UTF8

function Klawisze($s, $ms = 700) {
	[System.Windows.Forms.SendKeys]::SendWait($s)
	Start-Sleep -Milliseconds $ms
}
function Zapisz() {
	Klawisze "^s" 1800
	return (Get-Content -Path $plik -Raw)
}

Write-Output "== uruchamiam $exe"
$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 12

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Fg2 {
	[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
	[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
	[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
	[DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
	[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
	[DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool f);
	[DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);
	[DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
	public static bool NaWierzch(IntPtr h) {
		uint moj = GetCurrentThreadId();
		uint obcy = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
		if (obcy != 0 && obcy != moj) AttachThreadInput(moj, obcy, true);
		ShowWindow(h, 9); BringWindowToTop(h); SetForegroundWindow(h);
		System.Threading.Thread.Sleep(500);
		if (obcy != 0 && obcy != moj) AttachThreadInput(moj, obcy, false);
		return GetForegroundWindow() == h;
	}
	public static string TytulNaWierzchu() {
		var sb = new System.Text.StringBuilder(512);
		GetWindowText(GetForegroundWindow(), sb, 512);
		return sb.ToString();
	}
}
"@
$hwnd = (Get-Process -Id $proc.Id).MainWindowHandle
Write-Output ("na wierzchu: " + [Fg2]::NaWierzch($hwnd))
Start-Sleep -Seconds 2

# --- KONTROLA POZYTYWNA ---
Klawisze "^{END}" 400
Klawisze "QQ" 500
$po = Zapisz
if ($po -notmatch "QQ") {
	Write-Output "ZLE  0 KONTROLA POZYTYWNA PADLA - klawisze nie dochodza. POMIAR NIEWAZNY."
	try { Stop-Process -Id $proc.Id -Force } catch {}
	exit 1
}
Write-Output "OK   0 kontrola pozytywna: klawisze dochodza"
Klawisze "{BACKSPACE}{BACKSPACE}" 400
$null = Zapisz

# ================= A. PALETA POLECEN =================
# Wybieramy polecenie o skutku WIDOCZNYM W PLIKU: "Insert Time" wstawia znacznik
# czasu do dokumentu.  Gdyby paleta nie uruchamiala niczego, plik sie nie zmieni.
Klawisze "^{END}" 400
Klawisze "{ENTER}" 400
$przedPaleta = Get-Content -Path $plik -Raw
Klawisze "^+{F1}" 2500
$tytulPalety = [Fg2]::TytulNaWierzchu()
Write-Output "TYTUL OKNA po Control+Shift+F1: $tytulPalety"
Klawisze "insert time" 1200
Klawisze "{ENTER}" 2000
$tytulPo = [Fg2]::TytulNaWierzchu()
Write-Output "TYTUL OKNA po Enter: $tytulPo"
$poPalecie = Zapisz
if ($poPalecie -ne $przedPaleta) {
	Write-Output "OK   A1 Enter w polu filtra URUCHOMIL polecenie (plik sie zmienil)"
} else {
	Write-Output "ZLE  A1 Enter w polu filtra NIC nie uruchomil (plik bez zmian) - zgloszenie MK potwierdzone"
}
# Jesli okno palety zostalo otwarte, zamykamy je.
if ($tytulPo -match "Command Palette") {
	Write-Output "     UWAGA: okno palety NADAL bylo otwarte po Enter"
	Klawisze "{ESC}" 1000
}

# --- A2: to samo, ale ze schodzeniem do listy strzalka i Enterem NA LISCIE ---
$przedPaleta2 = Get-Content -Path $plik -Raw
Klawisze "^+{F1}" 2500
Klawisze "insert time" 1200
Klawisze "{DOWN}" 700
Klawisze "{ENTER}" 2000
$tytulPo2 = [Fg2]::TytulNaWierzchu()
$poPalecie2 = Zapisz
if ($poPalecie2 -ne $przedPaleta2) {
	Write-Output "OK   A2 Enter NA LISCIE uruchomil polecenie"
} else {
	Write-Output "ZLE  A2 Enter NA LISCIE tez nic nie uruchomil (okno teraz: $tytulPo2)"
}
if ([Fg2]::TytulNaWierzchu() -match "Command Palette") { Klawisze "{ESC}" 1000 }

# ================= B. Control+L i Control+Shift+L na checkliscie =================
# Ustawiamy kursor na wierszu "- [ ] kupic chleb" (trzeci wiersz pliku).
Klawisze "^{HOME}" 500
Klawisze "{DOWN}{DOWN}" 700
Klawisze "+{DOWN}" 600      # zaznaczamy dwie pozycje checklisty
Klawisze "^l" 1200
$poCtrlL = Zapisz
Write-Output "--- plik po Control+L na dwoch pozycjach checklisty ---"
Write-Output $poCtrlL
Write-Output "--- koniec ---"

# przywracamy checkliste tym samym Control+Shift+F2 i mierzymy Control+Shift+L
Klawisze "^{HOME}" 500
Klawisze "{DOWN}{DOWN}" 700
Klawisze "+{DOWN}" 600
Klawisze "^+{F2}" 1200
$null = Zapisz
Klawisze "^{HOME}" 500
Klawisze "{DOWN}{DOWN}" 700
Klawisze "+{DOWN}" 600
Klawisze "^+l" 1200
$poCtrlShiftL = Zapisz
Write-Output "--- plik po Control+Shift+L na dwoch pozycjach checklisty ---"
Write-Output $poCtrlShiftL
Write-Output "--- koniec ---"

# ================= C. Alt+lewy nawias kwadratowy =================
Klawisze "^{END}" 500
$tytulPrzed = [Fg2]::TytulNaWierzchu()
Klawisze "%([)" 1200
$tytulPoAlt = [Fg2]::TytulNaWierzchu()
Write-Output "TYTUL po Alt+lewy nawias: $tytulPoAlt (przed: $tytulPrzed)"
# Sprawdzamy, czy pasek menu przejal klawiature: wysylamy strzalke w dol i litere.
Klawisze "{ESC}" 700
Klawisze "XY" 700
$poAlt = Zapisz
if ($poAlt -match "XY") {
	Write-Output "OK   C1 po Escape klawiatura wrocila do tekstu (XY trafilo do pliku)"
} else {
	Write-Output "ZLE  C1 po Escape tekst NIE dostal klawiszy - menu trzyma fokus (zgloszenie MK potwierdzone)"
}

Write-Output "== zamykam program"
try { $proc.CloseMainWindow() | Out-Null; Start-Sleep -Seconds 3 } catch {}
try { if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force } } catch {}
Write-Output "== koniec"
