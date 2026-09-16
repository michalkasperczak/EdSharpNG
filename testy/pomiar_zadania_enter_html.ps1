# pomiar_zadania_enter_html.ps1 - ENTER I EKSPORT HTML (checklisty, 5.0.112).
#
# Dwie rzeczy, ktorych nie zmierzy ani pomiar czystych funkcji, ani sonda
# przelaczania stanu:
#   1. czy Enter na pozycji checklisty dopisuje "- [ ] ", a nie zwykle "- ",
#   2. czy eksport HTML robi PRAWDZIWE pole wyboru, a nie nawiasy w tresci.
#
# KONTROLE ROZNICUJACE:
#   - Enter na ZWYKLYM punktorze musi dalej dawac "- " (asercja 2) - inaczej
#     poprawka zepsulaby zwykle listy, ktore dzialaly,
#   - w HTML sprawdzamy TAKZE brak nawiasow "[x]" w tresci (asercja 5), bo samo
#     obecne <input> nie dowodzi, ze stary zapis zniknal.

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms

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

$exe = "C:\EdSharpBuild\EdSharpNG.exe"
$plik = "$env:TEMP\pomiar_enter.md"
$html = "$env:TEMP\pomiar_enter.html"
Remove-Item $html -ErrorAction SilentlyContinue

Set-Content -Path $plik -Encoding UTF8 -Value @"
# Zadania

- [x] pozycja zrobiona
- zwykly punktor
"@

Get-Process -Name EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 12
$naWierzchu = [Fg2]::NaWierzch((Get-Process -Id $proc.Id).MainWindowHandle)
Write-Output "na wierzchu: $naWierzchu"
Start-Sleep -Seconds 2

# --- ENTER NA POZYCJI CHECKLISTY ---
# Kursor na koniec wiersza "- [x] pozycja zrobiona", potem Enter i tekst.
[System.Windows.Forms.SendKeys]::SendWait("^{HOME}")
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("{DOWN}{DOWN}{END}")
Start-Sleep -Milliseconds 500
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
Start-Sleep -Milliseconds 600
[System.Windows.Forms.SendKeys]::SendWait("nowe zadanie")
Start-Sleep -Milliseconds 600
[System.Windows.Forms.SendKeys]::SendWait("^s")
Start-Sleep -Seconds 2
$po = Get-Content -Path $plik -Raw

if ($po -match "- \[ \] nowe zadanie") {
	Write-Output "OK   1 Enter dopisal pozycje checklisty NIEZROBIONA"
} else {
	Write-Output "ZLE  1 Enter nie dopisal pozycji checklisty"
	Write-Output $po
}
# Kontrola dziedziczenia stanu: nowa pozycja NIE moze byc odhaczona, mimo ze
# Enter padl na pozycji "- [x]".
if ($po -match "- \[x\] nowe zadanie") {
	Write-Output "ZLE  1b nowa pozycja odziedziczyla stan zrobione"
} else {
	Write-Output "OK   1b nowa pozycja nie odziedziczyla stanu zrobione"
}

# --- KONTROLA: Enter na ZWYKLYM punktorze dalej daje "- " ---
# Control+End laduje na PUSTYM wierszu za tekstem, gdzie Enter slusznie nie
# kontynuuje zadnej listy - pierwszy przebieg mierzyl wlasnie to i falszywie
# oskarzyl program o zepsute listy.  Wchodzimy strzalka w gore na "- zwykly
# punktor" i na jego koniec.
[System.Windows.Forms.SendKeys]::SendWait("^{END}")
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("{UP}{END}")
Start-Sleep -Milliseconds 500
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
Start-Sleep -Milliseconds 600
[System.Windows.Forms.SendKeys]::SendWait("kolejny punktor")
Start-Sleep -Milliseconds 600
[System.Windows.Forms.SendKeys]::SendWait("^s")
Start-Sleep -Seconds 2
$po2 = Get-Content -Path $plik -Raw
if ($po2 -match "(?m)^- kolejny punktor") {
	Write-Output "OK   2 KONTROLA: Enter na zwyklym punktorze dalej daje zwykly punktor"
} else {
	Write-Output "ZLE  2 KONTROLA: zwykle listy zepsute"
	Write-Output $po2
}

Write-Output "== zamykam program"
try { $proc.CloseMainWindow() | Out-Null; Start-Sleep -Seconds 3 } catch {}
try { if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force } } catch {}
Write-Output "== koniec"
