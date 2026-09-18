# pomiar_ctrl_l_bez_zaznaczenia.ps1 - PRZYPADEK MK: KURSOR NA WIERSZU.
#
# Poprzedni wazny pomiar (pomiar_ctrl_l_fokus3.ps1) robil Control+A, czyli
# ZAZNACZAL cala tresc - i wyszlo czysto.  MK opisuje co innego: "usuwa nawiasy
# kwadratowe pozostawiajac znaki - i cyfry z listy".  Najpewniej stal KURSOREM
# na wierszu, bez zaznaczenia, i moze na pliku, ktory nie jest .md.
#
# Mierzone warianty:
#   A. .md, kursor na wierszu checklisty, Control+L
#   B. .md, kursor na wierszu checklisty, Control+Shift+L
#   C. .txt (nie markdown), kursor na checkliscie, Control+L
#   D. .md, kursor na checkliscie z WCIECIEM (zagniezdzona pozycja), Control+L

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class Fg2 {
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

function Mierz($nazwa, $rozsz, $tresc, $skrot) {
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2
	$plik = "$env:TEMP\bz_$nazwa.$rozsz"
	Set-Content -Path $plik -Value $tresc -Encoding UTF8

	$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
	Start-Sleep -Seconds 14
	$proc.Refresh()
	if (-not [Fg2]::NaWierzch((Get-Process -Id $proc.Id).MainWindowHandle)) {
		"ODMOWA ($nazwa): aktywne <" + [Fg2]::Tytul() + ">"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return
	}
	Start-Sleep 2

	# Kontrola pozytywna klawiatury.
	K "^{END}" 500
	K "Z" 500
	K "^s" 1200
	if ((Get-Content $plik -Raw) -notmatch "Z") {
		"ODMOWA ($nazwa): kontrola padla, klawisze nie dochodza"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return
	}
	K "{BACKSPACE}" 400
	K "^s" 1000

	# BEZ ZAZNACZENIA: kursor na PIERWSZY wiersz.
	K "^{HOME}" 700
	K $skrot 1600
	K "^s" 1600
	Start-Sleep 1
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2

	"=== $nazwa (.$rozsz, kursor na 1. wierszu, $skrot) ==="
	foreach ($l in (Get-Content $plik)) { "  <" + $l + ">" }
}

Mierz "A_md_ctrlL"      "md"  "- [ ] alfa`r`n- [x] beta`r`n"     "^l"
Mierz "B_md_ctrlShiftL" "md"  "- [ ] alfa`r`n- [x] beta`r`n"     "^+l"
Mierz "C_txt_ctrlL"     "txt" "- [ ] alfa`r`n- [x] beta`r`n"     "^l"
Mierz "D_wciecie_ctrlL" "md"  "  - [ ] alfa`r`n  - [x] beta`r`n" "^l"
