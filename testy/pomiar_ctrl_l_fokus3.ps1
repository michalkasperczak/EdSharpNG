# pomiar_ctrl_l_fokus3.ps1 - POMIAR Control+L SPRAWDZONYM SPOSOBEM NA FOKUS.
#
# LEKCJA: dwa poprzednie podejscia (pomiar_ctrl_kontrola.ps1, ...fokus2.ps1) daly
# ODMOWE - klawisze szly do okna terminala.  Blad byl w kierunku podczepienia:
# AttachThreadInput trzeba wolac z watkiem OKNA AKTYWNEGO (terminala), nie okna
# docelowego.  Tu jest doslowna kopia dzialajacego kodu z pomiar_zadania_zywe.ps1.
#
# Mierzone zgloszenie MK (pkt 1.6): Control+L (lista punktowana) i Control+Shift+L
# (numerowana) na wierszach, ktore SA checklista - czy zjadaja pola wyboru.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class Fg {
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

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"

function K($s, $ms = 800) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

function Mierz($nazwa, $tresc, $skrot) {
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2

	$plik = "$env:TEMP\f3_$nazwa.md"
	Set-Content -Path $plik -Value $tresc -Encoding UTF8

	$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
	Start-Sleep -Seconds 14
	$proc.Refresh()

	$hwnd = (Get-Process -Id $proc.Id).MainWindowHandle
	$naWierzchu = [Fg]::NaWierzch($hwnd)
	if (-not $naWierzchu) {
		"ODMOWA POMIARU ($nazwa): nie na wierzchu, aktywne <" + [Fg]::Tytul() + ">"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return
	}
	Start-Sleep -Seconds 2

	# KONTROLA POZYTYWNA: wpisz znak i zapisz - klawisze musza dochodzic.
	K "^{END}" 500
	K "Z" 500
	K "^s" 1200
	$poKontroli = Get-Content $plik -Raw
	if ($poKontroli -notmatch "Z") {
		"ODMOWA POMIARU ($nazwa): kontrola padla, klawisze nie dochodza"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return
	}
	# Cofnij znak kontrolny.
	K "{BACKSPACE}" 400
	K "^s" 1000

	# POMIAR.
	K "^{HOME}" 500
	K "^a" 700
	K $skrot 1600
	K "^s" 1600
	Start-Sleep 1
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2

	"=== WYNIK $nazwa (skrot $skrot) ==="
	foreach ($l in (Get-Content $plik)) { "  <" + $l + ">" }
}

Mierz "ctrl_l_zwykly" "alfa`r`nbeta`r`n" "^l"
Mierz "ctrl_l_checklista" "- [ ] alfa`r`n- [x] beta`r`n" "^l"
Mierz "ctrl_shift_l_checklista" "- [ ] alfa`r`n- [x] beta`r`n" "^+l"
