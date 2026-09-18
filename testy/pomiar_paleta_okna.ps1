# pomiar_paleta_okna.ps1 - DRUGA CZESC ZGLOSZENIA O PALECIE.
#
# Pierwszy pomiar (pomiar_paleta_enter.ps1) dal 3 OK: paleta uruchamia polecenie,
# ktore ZMIENIA TEKST (Task List On Off), i to obiema drogami - Enterem w polu
# filtra i Enterem na liscie.
#
# Ale MK pisal to zdanie DOKLADNIE POD punktem o dwoch przeniesionych skrotach:
# palecie polecen i samouczku.  Czyli szukal w palecie tych dwoch pozycji.  A one
# nie zmieniaja tekstu - OTWIERAJA OKNO.  To inna droga: PerformClick na pozycji
# menu, ktora pokazuje okno modalne, w chwili gdy okno palety wlasnie sie zamyka.
# Dlatego mierze to osobno, po TYTULE OKNA, nie po tresci pliku.
#
# TRZY PRZEBIEGI:
#   A. Samouczek z KLAWISZA (Control+Alt+F1) - punkt odniesienia.
#   B. Samouczek Z PALETY.
#   C. "Command Palette" wpisane w palecie - sprawdzam, czy pozycja jest w ogole
#      na liscie (kod ma ja jawnie POMIJAC, wiec spodziewam sie braku - i to
#      moze byc cale zgloszenie: MK szukal palety w palecie i nie znalazl).

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -TypeDefinition @"
using System; using System.Text; using System.Runtime.InteropServices;
public class FgO {
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
function K($s, $ms = 900) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }
$global:iOk = 0
$global:iZle = 0

# Uruchamia program, wysyla klawisze, ZWRACA TYTUL OKNA na wierzchu.
function Mierz($nazwa, $klawisze) {
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2
	$plik = "$env:TEMP\po_$nazwa.md"
	Set-Content -Path $plik -Value "wiersz probny" -Encoding UTF8 -NoNewline

	$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
	Start-Sleep -Seconds 14
	$proc.Refresh()
	if (-not [FgO]::NaWierzch((Get-Process -Id $proc.Id).MainWindowHandle)) {
		"ODMOWA ($nazwa): okna nie da sie postawic na wierzch"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return $null
	}
	Start-Sleep 2

	# KONTROLA POZYTYWNA KLAWIATURY: litera i zapis, potem cofniecie.
	K "^{END}" 500
	K "Z" 500
	K "^s" 1400
	if ((Get-Content $plik -Raw) -notmatch "wiersz probnyZ") {
		"ODMOWA ($nazwa): kontrola padla, klawisze nie dochodza do okna"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return $null
	}
	$sGlowne = [FgO]::Tytul()

	foreach ($k in $klawisze) { K $k 2000 }
	Start-Sleep 2
	$sPo = [FgO]::Tytul()
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2
	return @($sGlowne, $sPo)
}

# Zdalo sie, gdy tytul okna na wierzchu ZMIENIL sie na inny niz glowne okno.
function OcenOkno($co, $para) {
	if ($para -eq $null) { $global:iZle++; "ZLE  $co (pomiar gluchy)"; return }
	$przed = $para[0]; $po = $para[1]
	if ($po -ne $przed -and $po.Length -gt 0) { $global:iOk++; "OK   $co - otwarlo sie okno <$po>" }
	else { $global:iZle++; "ZLE  $co - NIC sie nie otwarlo, na wierzchu nadal <$po>" }
}

"=================== POMIAR: PALETA A POLECENIA OTWIERAJACE OKNO ==================="
""

# A. Samouczek z klawisza - punkt odniesienia.
OcenOkno "A. Samouczek z klawisza Control+Alt+F1" (Mierz "a_klaw" @("^%{F1}"))

# B. Samouczek z palety.
OcenOkno "B. Samouczek Z PALETY (wpisane 'tutorial', Enter)" (Mierz "b_paleta" @("^+{F1}", "tutorial", "{ENTER}"))

# C. Czy w palecie jest pozycja "Command Palette".  Kod ja jawnie pomija, wiec
# oczekuje BRAKU - filtr powinien powiedziec "No matching command", a Enter nie
# ma czego uruchomic.  Mierze to jako pytanie, nie jako blad.
$para = Mierz "c_sama_paleta" @("^+{F1}", "command palette", "{ENTER}")
if ($para -eq $null) { "?    C. pomiar gluchy" }
else { "?    C. po wpisaniu 'command palette' i Enterze na wierzchu jest <" + $para[1] + "> (przed: <" + $para[0] + ">)" }

""
"=================== WYNIK: $($global:iOk) OK, $($global:iZle) ZLE ==================="
