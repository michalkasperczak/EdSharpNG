# pomiar_ctrl_alt_x.ps1 - ROZSTRZYGNIECIE SPORU O Control+Alt+X (17.09.2026)
#
# MK, uwaga w DO-PRZETESTOWANIA-5.0.108-5.0.112.md: "Proponuje jednak zmienic
# skrot na Alt-CTRL-x.  Dopilnowac by nie bylo kolizji z polska z, ale nie
# powinno do tego dojsc, z innymi testowalismy i bylo OK." oraz "Mogloby raczej
# dzialac jednak."
#
# Pomiar z 13.09.2026 dotyczyl litery S (s z kreska).  X ma pod prawym Altem
# litere z z kreska (U+017A), ale to INNA litera i INNY klawisz - nie wolno
# przenosic tamtego wyniku.  Mierzymy DOKLADNIE ten chord.
#
# PIERWSZA WERSJA TEJ SONDY BYLA BEZ WARTOSCI: podmieniala chord w Hotkeys.ini,
# a ten plik trzyma tylko OPISY SLOWNE - sekcja [Keys] nie jest juz czytana
# (EdSharp.cs linia 1974), przypisania siedza w kodzie.  Podmiana nic nie
# zmieniala w programie.
#
# CO MIERZYMY TERAZ - pytanie wezsze, ale wystarczajace i uczciwe:
# czy chord Control+Alt+X WPISUJE do dokumentu polska litere.
#   litera wpisana  -> chord zjada uklad klawiatury, komenda by sie nie odpalila,
#   litera NIE wpisana -> chord jest wolny i da sie na nim postawic polecenie.
# Nie trzeba do tego przypisywac komendy ani przebudowywac programu.
#
# KONTROLA POZYTYWNA: to samo dla Control+Alt+S, gdzie 13.09.2026 zmierzylismy,
# ze litera "s z kreska" WPISUJE SIE.  Gdyby dzis nie wpisala sie, sonda klamie.
# KONTROLA NEGATYWNA: Control+Alt+Q - Q nie ma polskiego odpowiednika, wiec
# nie moze wpisac nic.

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms

$exe = "C:\EdSharpBuild\EdSharpNG.exe"

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Fg4 {
	[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
	[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
	[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
	[DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
	[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
	[DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool f);
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
}
"@

function K($s, $ms = 700) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

# SPRZATANIE PRZED POMIAREM.  Wcześniejsza sonda (pomiar_paleta_mk_113b.ps1)
# otwierala samouczek EdSharpa w przegladarce Edge, a to okno ZOSTAWALO na
# wierzchu i przejmowalo klawiature - kolejny pomiar odmawial pracy, mimo ze
# program dzialal poprawnie.  Zamykamy stare EdSharpy i okna samouczka.
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process msedge -ErrorAction SilentlyContinue |
	Where-Object { $_.MainWindowTitle -match "EdSharp" } |
	ForEach-Object { $_.CloseMainWindow() | Out-Null }
Start-Sleep -Seconds 3

$plik = "$env:TEMP\pomiar_chordy.md"
Set-Content -Path $plik -Value "start`r`n" -Encoding UTF8

$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 14
$proc.Refresh()
$hwnd = $proc.MainWindowHandle
Write-Output ("tytul okna programu: '" + $proc.MainWindowTitle + "'")
if (-not [Fg4]::NaWierzch($hwnd)) {
	Write-Output "ODMOWA: okna nie postawiono na wierzchu - POMIAR NIEWAZNY"
	try { Stop-Process -Id $proc.Id -Force } catch {}
	exit 1
}
Start-Sleep -Seconds 3

# Kontrola pozytywna z trzema podejsciami: pierwsze naciskiecie po starcie
# programu bywa gubione (okno jeszcze ustawia fokus w polu tekstowym), a
# odpisanie tego jako "flaky" bez sprawdzenia byloby bledem.  Dopiero trzy
# nieudane podejscia znacza, ze klawisze naprawde nie dochodza.
$ok = $false
for ($i = 1; $i -le 3; $i++) {
	K "^{END}" 500
	K "AA" 700
	K "^s" 2000
	if ((Get-Content -Path $plik -Raw) -match "AA") {
		Write-Output "OK   kontrola pozytywna klawiatury (podejscie $i): klawisze dochodza"
		$ok = $true
		K "{BACKSPACE}{BACKSPACE}" 400
		K "^s" 1500
		break
	}
	Write-Output "     podejscie $i nieudane, probuje ponownie"
	Start-Sleep -Seconds 2
}
if (-not $ok) {
	Write-Output "ODMOWA: klawisze nie dochodza do programu po 3 podejsciach - POMIAR NIEWAZNY"
	try { Stop-Process -Id $proc.Id -Force } catch {}
	exit 1
}

function ZmierzChord($opis, $sendKeys, $kodLitery) {
	# Czysty wiersz na koncu dokumentu; po chordzie patrzymy, CO w nim jest.
	K "^{END}" 400
	K "{ENTER}" 400
	K "^s" 1500
	$przed = Get-Content -Path $plik -Raw

	K $sendKeys 1200
	K "^s" 1800
	$po = Get-Content -Path $plik -Raw

	$dodane = ""
	if ($po.Length -gt $przed.Length) { $dodane = $po.Substring($przed.Length) }
	$litera = [char]$kodLitery
	$bLitera = $dodane.Contains($litera)
	$kody = ($dodane.ToCharArray() | ForEach-Object { [int]$_ }) -join ","
	Write-Output "--- $opis ---"
	Write-Output ("   dodane znaki (kody): [" + $kody + "]")
	Write-Output ("   litera o kodzie $kodLitery wpisana: " + $bLitera)
}

# BADANY: Control+Alt+X.  Prawy Alt+X w polskim ukladzie daje z z kreska (378 = 0x17A).
ZmierzChord "Control+Alt+X (badany chord MK)" "^%x" 378
# KONTROLA POZYTYWNA: Control+Alt+S - 13.09.2026 zmierzone, ze wpisuje 347.
ZmierzChord "Control+Alt+S (kontrola pozytywna, ma wpisac 347)" "^%s" 347
# KONTROLA NEGATYWNA: Q bez polskiego odpowiednika.
ZmierzChord "Control+Alt+Q (kontrola negatywna, nie ma nic wpisac)" "^%q" 113

Write-Output "== zamykam"
try { $proc.CloseMainWindow() | Out-Null; Start-Sleep -Seconds 3 } catch {}
try { if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force } } catch {}
Write-Output "== koniec"
