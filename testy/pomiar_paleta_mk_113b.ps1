# pomiar_paleta_mk_113b.ps1 - KTORE polecenia z palety nie odpalaja.
#
# Zgloszenie MK: "Nowe skroty sa prawidlowo opisane, ale jak nacisniesz enter,
# nie wykonuje sie nic, jak by paleta do nich nie doszla".  Pierwszy pomiar
# (pomiar_uwagi_mk_113.ps1) pokazal, ze Enter DZIALA dla Insert Time - wiec
# problem nie jest w samej palecie, a w KONKRETNYCH poleceniach.  Tutaj
# sprawdzamy te, o ktorych mowila lista testowa: samouczek (Tutorial) i
# okno listy zadan (Task List Window), oba na przeniesionych skrotach.
#
# MIERNIK: TYTUL okna na wierzchu po Enter.  Kazde z tych polecen otwiera
# WLASNE okno, wiec brak zmiany tytulu = polecenie nie wykonalo sie.

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms

$exe = "C:\EdSharpBuild\EdSharpNG.exe"
$plik = "$env:TEMP\pomiar_mk113b.md"
Set-Content -Path $plik -Value "# Pomiar`r`n`r`n- [ ] kupic chleb`r`n- [x] kupic mleko`r`n" -Encoding UTF8

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Fg3 {
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
	public static string Tytul() {
		var sb = new System.Text.StringBuilder(512);
		GetWindowText(GetForegroundWindow(), sb, 512);
		return sb.ToString();
	}
}
"@

function K($s, $ms = 700) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 12
$hwnd = (Get-Process -Id $proc.Id).MainWindowHandle
Write-Output ("na wierzchu: " + [Fg3]::NaWierzch($hwnd))
Start-Sleep -Seconds 2

# Kontrola pozytywna: skrot Control+Shift+F7 WPROST musi otworzyc okno zadan.
K "^{HOME}" 400
K "^+{F7}" 2500
$t = [Fg3]::Tytul()
if ($t -match "Task List") { Write-Output "OK   0 skrot Control+Shift+F7 WPROST otwiera okno: $t" }
else { Write-Output "ZLE  0 skrot wprost nie otworzyl okna zadan (tytul: $t) - POMIAR DALSZY BEZ WARTOSCI" }
K "{ESC}" 1500

function ProbujZPalety($fraza, $oczekiwanyTytul) {
	K "^+{F1}" 2500
	$tp = [Fg3]::Tytul()
	if ($tp -notmatch "Command Palette") { Write-Output "   ODMOWA: paleta sie nie otworzyla (tytul: $tp)"; return }
	K $fraza 1400
	K "{ENTER}" 3000
	$tpo = [Fg3]::Tytul()
	if ($tpo -match $oczekiwanyTytul) { Write-Output "OK   z palety '$fraza' -> otwarto '$tpo'" }
	elseif ($tpo -match "Command Palette") { Write-Output "ZLE  z palety '$fraza' -> OKNO PALETY NADAL OTWARTE (Enter nic nie zrobil)" }
	else { Write-Output "ZLE  z palety '$fraza' -> nic nie otwarto, okno na wierzchu: '$tpo'" }
	# sprzatanie: zamykamy co bylo otwarte
	K "{ESC}" 1500
	if ([Fg3]::Tytul() -match "Command Palette") { K "{ESC}" 1200 }
}

ProbujZPalety "task list window" "Task List"
ProbujZPalety "tutorial" "Tutorial|Help|EdSharp"
ProbujZPalety "hot key summary" "Hot|Summary|Key"
ProbujZPalety "bookmark list" "Bookmark"

Write-Output "== zamykam"
try { $proc.CloseMainWindow() | Out-Null; Start-Sleep -Seconds 3 } catch {}
try { if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force } } catch {}
Write-Output "== koniec"
