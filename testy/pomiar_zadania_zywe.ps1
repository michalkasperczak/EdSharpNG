# pomiar_zadania_zywe.ps1 - POMIAR CHECKLIST NA ZYWYM PROGRAMIE (5.0.112).
#
# Czego NIE zmierzy pomiar czystych funkcji (testy/pomiar_zadania_611.cs):
#   1. czy skroty sa NAPRAWDE przypisane, a nie tylko wpisane do menu,
#   2. czy program nie alarmuje na starcie o kolizji klawiszy,
#   3. czy Control+Shift+X zmienia TRESC PLIKU na dysku.
# Tylko zywy program to rozstrzyga - stad ta sonda.
#
# KONTROLA ROZNICUJACA: na koncu ten sam klawisz jest wysylany na wierszu, ktory
# NIE jest zadaniem.  Plik nie moze sie wtedy zmienic.  Bez tej kontroli pomiar
# "plik sie zmienil" przeszedlby tez wtedy, gdyby klawisz robil cokolwiek innego.

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms

$exe = "C:\EdSharpBuild\EdSharpNG.exe"
$plik = "$env:TEMP\pomiar_zadania.md"

$trescStart = @"
# Lista zakupow

- [ ] kupic chleb
- [ ] kupic mleko
- [x] zaplacic rachunek
zwykly wiersz bez zadania
"@
Set-Content -Path $plik -Value $trescStart -Encoding UTF8
$przedZapisem = Get-Content -Path $plik -Raw

Write-Output "== uruchamiam program z plikiem"
$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 12

# OKNO NA PIERWSZY PLAN.  SendKeys idzie do okna AKTYWNEGO, a program
# uruchomiony z WSL nie jest nim sam z siebie - bez tego kazdy klawisz trafia w
# pustke, a sonda pokazuje "nic sie nie stalo" niezaleznie od tego, czy kod
# dziala.  To bylo zrodlo pierwszego falszywego wyniku tej sondy.
#
# SetForegroundWindow SAMO NIE WYSTARCZA (zmierzone: kontrola pozytywna padla).
# Windows odmawia oddania pierwszego planu procesowi, ktory nie ma "wejscia" -
# trzeba na chwile PODCZEPIC SIE pod watek okna aktywnego (AttachThreadInput).
# Ten sam sposob dziala w testy/na_wierzch.ps1, wiec tu jest jego kopia.
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Fg {
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
$hwnd = (Get-Process -Id $proc.Id).MainWindowHandle
$naWierzchu = [Fg]::NaWierzch($hwnd)
Write-Output "na wierzchu: $naWierzchu"
Start-Sleep -Seconds 2

# KONTROLA POZYTYWNA KLAWIATURY.  Zanim zmierzymy komendy, sprawdzamy, ze
# klawisze w ogole DOCHODZA do programu: wpisujemy znak i zapisujemy plik.
# Gdy ta kontrola padnie, wynik wszystkich dalszych asercji jest bez wartosci.
[System.Windows.Forms.SendKeys]::SendWait("^{END}")
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("ZZ")
Start-Sleep -Milliseconds 600
[System.Windows.Forms.SendKeys]::SendWait("^s")
Start-Sleep -Seconds 2
$poWpisaniu = Get-Content -Path $plik -Raw
if ($poWpisaniu -match "ZZ") {
	Write-Output "OK   0 KONTROLA POZYTYWNA: klawisze dochodza do programu"
	# Sprzatamy znak kontrolny, zeby nie mieszal sie w dalszych pomiarach.
	[System.Windows.Forms.SendKeys]::SendWait("{BACKSPACE}{BACKSPACE}")
	Start-Sleep -Milliseconds 400
	[System.Windows.Forms.SendKeys]::SendWait("^s")
	Start-Sleep -Seconds 2
} else {
	Write-Output "ZLE  0 KONTROLA POZYTYWNA PADLA: klawisze NIE dochodza do programu."
	Write-Output "     Dalsze asercje nie znacza nic - przerywam."
	try { Stop-Process -Id $proc.Id -Force } catch {}
	exit 1
}

# KOLIZJA KLAWISZY.  Program przy duplikacie pokazuje MODALNE okno "Alert" z
# trescia "Cannot assign ... since already assigned to ...".  Szukamy go po
# tytule wsrod okien procesu - gdyby bylo, kazdy dalszy klawisz szedlby do niego.
$okna = @()
Get-Process -Id $proc.Id | ForEach-Object { $okna += $_.MainWindowTitle }
$tytul = $okna -join " | "
Write-Output "TYTUL OKNA: $tytul"
if ($tytul -match "Alert") {
	Write-Output "ZLE  1 alarm kolizji klawiszy na starcie"
} else {
	Write-Output "OK   1 brak alarmu kolizji klawiszy na starcie"
}

# --- Control+Shift+X na pozycji niezrobionej ---
# Kursor po otwarciu stoi na poczatku; schodzimy na wiersz z "- [ ] kupic chleb".
[System.Windows.Forms.SendKeys]::SendWait("^{HOME}")
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("{DOWN}{DOWN}")
Start-Sleep -Milliseconds 600
[System.Windows.Forms.SendKeys]::SendWait("^+x")
Start-Sleep -Milliseconds 900
[System.Windows.Forms.SendKeys]::SendWait("^s")
Start-Sleep -Seconds 2

$poPrzelaczeniu = Get-Content -Path $plik -Raw
if ($poPrzelaczeniu -match "- \[x\] kupic chleb") {
	Write-Output "OK   2 Control+Shift+X odhaczyl zadanie w PLIKU na dysku"
} else {
	Write-Output "ZLE  2 zadanie nie zostalo odhaczone"
	Write-Output "     plik teraz:"
	Write-Output $poPrzelaczeniu
}

# --- ten sam klawisz DRUGI RAZ: musi wrocic do niezrobionego ---
[System.Windows.Forms.SendKeys]::SendWait("^+x")
Start-Sleep -Milliseconds 900
[System.Windows.Forms.SendKeys]::SendWait("^s")
Start-Sleep -Seconds 2
$poPowrocie = Get-Content -Path $plik -Raw
if ($poPowrocie -match "- \[ \] kupic chleb") {
	Write-Output "OK   3 drugie nacisniecie wrocilo do niezrobionego (nie jest komenda jednokierunkowa)"
} else {
	Write-Output "ZLE  3 drugie nacisniecie nie wrocilo do stanu poczatkowego"
}

# --- KONTROLA: ten sam klawisz na wierszu, ktory NIE jest zadaniem ---
# Wchodzimy na "zwykly wiersz bez zadania": Control+End laduje na pustym wierszu
# za tekstem, a pusty wiersz to slaba kontrola - komenda pominelaby go tez z
# innego powodu.  Kontrola ma stac na wierszu, ktory MA tresc, ale nie jest zadaniem.
[System.Windows.Forms.SendKeys]::SendWait("^{END}")
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("{UP}")
Start-Sleep -Milliseconds 500
$przedKontrola = Get-Content -Path $plik -Raw
[System.Windows.Forms.SendKeys]::SendWait("^+x")
Start-Sleep -Milliseconds 900
[System.Windows.Forms.SendKeys]::SendWait("^s")
Start-Sleep -Seconds 2
$poKontroli = Get-Content -Path $plik -Raw
if ($przedKontrola -eq $poKontroli) {
	Write-Output "OK   4 KONTROLA: na wierszu bez zadania plik sie NIE zmienil"
} else {
	Write-Output "ZLE  4 KONTROLA: plik zmienil sie tam, gdzie nie ma zadania"
}

# --- Control+Shift+F2 robi checkliste ze zwyklego wiersza ---
# Kursor stoi juz na "zwykly wiersz bez zadania" po kontroli wyzej.
[System.Windows.Forms.SendKeys]::SendWait("^+{F2}")
Start-Sleep -Milliseconds 900
[System.Windows.Forms.SendKeys]::SendWait("^s")
Start-Sleep -Seconds 2
$poKreatorze = Get-Content -Path $plik -Raw
if ($poKreatorze -match "- \[ \] zwykly wiersz bez zadania") {
	Write-Output "OK   5 Control+Shift+F2 zrobil z wiersza pozycje checklisty"
} else {
	Write-Output "ZLE  5 Control+Shift+F2 nic nie zrobil"
}

Write-Output "== zamykam program"
try { $proc.CloseMainWindow() | Out-Null; Start-Sleep -Seconds 3 } catch {}
try { if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force } } catch {}
Write-Output "== koniec"
