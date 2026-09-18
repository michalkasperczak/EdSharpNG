# pomiar_zmiany_1b.ps1 - POMIAR NA ZYWYM PROGRAMIE: KLAWISZE SLEDZENIA ZMIAN.
#
# Krok 1a (Zmiany.cs) byl zmierzony golym kompilatorem: 90/90 asercji na czystych
# funkcjach.  TEGO TU NIE POWTARZAM.  Mierze DOKLADNIE to, czego tamten pomiar
# nie mogl dotknac: czy klawisze RODZINY F9 dochodza do komend w zywym oknie i
# czy zapisany plik wyglada tak, jak powinien po przyjeciu i po odrzuceniu.
#
# UKLAD KLAWISZY KASPERCZAKA (18.09.2026):
#   F9            nastepna zmiana
#   Shift+F9      poprzednia zmiana
#   Alt+F9        przyjmij zmiane pod kursorem
#   Alt+Shift+F9  odrzuc zmiane pod kursorem
#   Control+F9    okno "Zmiany"
#
# CO CZYNI TEN POMIAR ROZNICUJACYM.  Kazdy wariant ma KONTROLE POZYTYWNA
# KLAWIATURY (wpisanie litery i zapis) - bez niej "plik sie nie zmienil" znaczy
# tyle samo przy dzialajacej komendzie i przy klawiszach, ktore w ogole nie
# dochodza do okna.  Oddzielnie mierzony jest przypadek NEGATYWNY: Alt+F9 na
# pliku .txt NIE MA prawa nic zrobic (bramka Markdown).

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class FgZ {
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
	public static string TytulOkna(IntPtr h) {
		StringBuilder sb = new StringBuilder(512);
		GetWindowText(h, sb, 512);
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
function Rowne($co, $oczek, $mam) {
	if ($oczek -eq $mam) { $global:iOk++; "OK   $co" }
	else { $global:iZle++; "ZLE  $co"; "       oczekiwane <$oczek>"; "       mam        <$mam>" }
}

# Uruchamia program na pliku, wysyla klawisze, zwraca tresc pliku po zapisie.
# Zwraca $null, gdy kontrola klawiatury padla - wtedy pomiar jest gluchy i
# WOLNO to powiedziec wprost, a nie zaliczyc jako wynik.
function Mierz($nazwa, $rozsz, $tresc, $klawisze) {
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2
	$plik = "$env:TEMP\zm_$nazwa.$rozsz"
	Set-Content -Path $plik -Value $tresc -Encoding UTF8 -NoNewline

	$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
	Start-Sleep -Seconds 14
	$proc.Refresh()
	if (-not [FgZ]::NaWierzch((Get-Process -Id $proc.Id).MainWindowHandle)) {
		"ODMOWA ($nazwa): aktywne <" + [FgZ]::Tytul() + ">"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return $null
	}
	Start-Sleep 2

	# KONTROLA POZYTYWNA KLAWIATURY: litera na koniec i zapis.
	K "^{END}" 500
	K "Z" 500
	K "^s" 1400
	if ((Get-Content $plik -Raw) -notmatch "Z") {
		"ODMOWA ($nazwa): kontrola padla, klawisze nie dochodza do okna"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return $null
	}
	K "{BACKSPACE}" 400
	K "^s" 1200

	K "^{HOME}" 700
	foreach ($k in $klawisze) { K $k 1500 }
	K "^s" 1600
	Start-Sleep 1
	$wynik = Get-Content $plik -Raw
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2
	return $wynik
}

"=================== POMIAR KLAWISZY SLEDZENIA ZMIAN (krok 1b) ==================="
""

# 1. PRZYJMIJ DOPISANIE.  F9 skacze na zmiane, Alt+F9 ja przyjmuje.
# Po przyjeciu dopisania zostaje sama tresc: "ala ma kota".
$w = Mierz "przyjmij_dop" "md" "ala ma {++bardzo ++}kota" @("{F9}", "%{F9}")
if ($w -ne $null) { Rowne "F9 + Alt+F9: przyjete dopisanie" "ala ma bardzo kota" $w.TrimEnd("`r","`n") }

# 2. ODRZUC DOPISANIE.  Alt+Shift+F9 - dopisanie znika calkiem.
$w = Mierz "odrzuc_dop" "md" "ala ma {++bardzo ++}kota" @("{F9}", "%+{F9}")
if ($w -ne $null) { Rowne "F9 + Alt+Shift+F9: odrzucone dopisanie" "ala ma kota" $w.TrimEnd("`r","`n") }

# 3. PRZYJMIJ USUNIECIE - tresc ma ZNIKNAC (odwrotnie niz przy dopisaniu).
$w = Mierz "przyjmij_usu" "md" "ala ma {--bardzo --}kota" @("{F9}", "%{F9}")
if ($w -ne $null) { Rowne "Alt+F9: przyjete usuniecie (tresc znika)" "ala ma kota" $w.TrimEnd("`r","`n") }

# 4. ODRZUC USUNIECIE - tresc ma ZOSTAC.
$w = Mierz "odrzuc_usu" "md" "ala ma {--bardzo --}kota" @("{F9}", "%+{F9}")
if ($w -ne $null) { Rowne "Alt+Shift+F9: odrzucone usuniecie (tresc zostaje)" "ala ma bardzo kota" $w.TrimEnd("`r","`n") }

# 5. PODMIANA PRZYJETA - zostaje NOWE.
$w = Mierz "przyjmij_pod" "md" "ala ma {~~psa~>kota~~} w domu" @("{F9}", "%{F9}")
if ($w -ne $null) { Rowne "Alt+F9: przyjeta podmiana (zostaje nowe)" "ala ma kota w domu" $w.TrimEnd("`r","`n") }

# 6. PODMIANA ODRZUCONA - zostaje STARE.
$w = Mierz "odrzuc_pod" "md" "ala ma {~~psa~>kota~~} w domu" @("{F9}", "%+{F9}")
if ($w -ne $null) { Rowne "Alt+Shift+F9: odrzucona podmiana (zostaje stare)" "ala ma psa w domu" $w.TrimEnd("`r","`n") }

# 7. DRUGA ZMIANA PRZEZ DWA SKOKI.  F9 dwa razy ma stanac na DRUGIEJ zmianie,
# nie zostac na pierwszej - to mierzy sam skok, nie samo przyjmowanie.
$w = Mierz "drugi_skok" "md" "{++raz ++}dwa {++trzy ++}cztery" @("{F9}", "{F9}", "%{F9}")
if ($w -ne $null) { Rowne "F9 dwa razy: przyjeta DRUGA zmiana" "{++raz ++}dwa trzy cztery" $w.TrimEnd("`r","`n") }

# 8. SKOK W TYL.  F9 F9 wchodzi na druga, Shift+F9 wraca na pierwsza.
$w = Mierz "skok_wtyl" "md" "{++raz ++}dwa {++trzy ++}cztery" @("{F9}", "{F9}", "+{F9}", "%{F9}")
if ($w -ne $null) { Rowne "Shift+F9: wrocil na PIERWSZA zmiane" "raz dwa {++trzy ++}cztery" $w.TrimEnd("`r","`n") }

# 9. KONTROLA NEGATYWNA - BRAMKA MARKDOWN.  Na pliku .txt Alt+F9 nie ma prawa
# nic zrobic.  Gdyby plik sie zmienil, znaczy to, ze bramka nie dziala.
$w = Mierz "negatyw_txt" "txt" "ala ma {++bardzo ++}kota" @("{F9}", "%{F9}")
if ($w -ne $null) { Rowne "KONTROLA NEGATYWNA: .txt nietkniety" "ala ma {++bardzo ++}kota" $w.TrimEnd("`r","`n") }

# 10. KOMENTARZ RECENZENTA.  Przyjety i odrzucony znika tak samo - to nie tresc
# dokumentu.
$w = Mierz "komentarz" "md" "ala ma kota{>>sprawdz to<<}" @("{F9}", "%{F9}")
if ($w -ne $null) { Rowne "Alt+F9: komentarz znika" "ala ma kota" $w.TrimEnd("`r","`n") }

""
"=================== WYNIK: $($global:iOk) OK, $($global:iZle) ZLE ==================="
if ($global:iZle -gt 0) { exit 1 }
