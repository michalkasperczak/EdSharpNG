# pomiar_listy_checklista.ps1 - POMIAR NA ZYWYM PROGRAMIE: CONTROL+L I
# CONTROL+SHIFT+L NA WIERSZACH, KTORE SA CHECKLISTA.
#
# ZGLOSZENIE MK (18.09.2026, docs/UWAGI-MK-18.09.2026.md wiersz 90):
#   "Wyglada a to, ze usuwa wtedy nawiasy kwadratowe pozostawiajac znaki -
#    i cyfry z listy."
# Czyli odwrotnie niz mowi komentarz w kodzie (EdSharp.cs ~15407): tam stoi, ze
# pole stanu schodzi RAZEM z punktorem.  Jedno z dwoch jest nieprawda i nie
# rozstrzyga tego czytanie kodu - stad pomiar na zapisanym pliku.
#
# CO MIERZE.  Wejsciem jest pozycja checklisty "- [ ] kupic chleb".
#   Control+L        na checkliscie
#   Control+Shift+L  na checkliscie
# Po kazdym z nich patrze na TRESC ZAPISANEGO PLIKU i pytam o dwie rzeczy
# osobno, bo to dwa rozne bledy:
#   a) czy w tresci nie zostal goly nawias "[ ]" (to zglasza MK),
#   b) czy nie zostal goly punktor "-" albo cyfra bez pola (drugi wariant tego
#      samego zdania MK: "pozostawiajac znaki - i cyfry z listy").
#
# KONTROLA POZYTYWNA KLAWIATURY jest w kazdym wariancie (litera + zapis): bez
# niej "plik sie nie zmienil" znaczy to samo przy dzialajacej komendzie i przy
# klawiszach, ktore nie doszly do okna.
#
# KONTROLA NEGATYWNA: ten sam Control+L na pliku .txt NIE MA prawa nic zrobic
# (bramka Markdown) - gdyby zadzialal, pomiar reszty nie znaczy nic.
#
# KONTROLA ROZNICUJACA: Control+L na ZWYKLYM wierszu tekstu MA dodac punktor.
# Gdyby komenda w ogole nie dzialala, wszystkie testy "nie ma nawiasu" przeszly
# by falszywie - ten wariant to wychwytuje.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class FgL {
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
function Rowne($co, $oczek, $mam) {
	if ($oczek -eq $mam) { $global:iOk++; "OK   $co" }
	else { $global:iZle++; "ZLE  $co"; "       oczekiwane <$oczek>"; "       mam        <$mam>" }
}

function Mierz($nazwa, $rozsz, $tresc, $klawisze) {
	Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
	Start-Sleep 2
	$plik = "$env:TEMP\li_$nazwa.$rozsz"
	Set-Content -Path $plik -Value $tresc -Encoding UTF8 -NoNewline

	$proc = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
	Start-Sleep -Seconds 14
	$proc.Refresh()
	if (-not [FgL]::NaWierzch((Get-Process -Id $proc.Id).MainWindowHandle)) {
		"ODMOWA ($nazwa): aktywne <" + [FgL]::Tytul() + ">"
		Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
		return $null
	}
	Start-Sleep 2

	# KONTROLA POZYTYWNA KLAWIATURY.
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

"=================== POMIAR: LISTY NA CHECKLISCIE ==================="
""

# 0. ROZGRZEWKA.  PIERWSZE uruchomienie programu w przebiegu gubi pojedyncze
# znaki z SendKeys (zmierzone: trzy przebiegi pomiar_ctrl_l_warianty.ps1 dawaly
# "kupc chleb", "kupic cheb", raz klawisze wsypaly sie w tekst).  Okno przyjmuje
# klawisze, zanim skonczy inicjalizacje.  Pierwsze uruchomienie idzie wiec na
# wejscie, ktorego wyniku NIE licze do sumy - inaczej pierwszy wariant przepada
# losowo i pomiar klamie o programie.
$null = Mierz "rozgrzewka" "md" "- [ ] rozgrzewka" @("^l")

# 1. CONTROL+L NA CHECKLISCIE.  Wedlug komentarza w kodzie ma zostac sam tekst.
# Wedlug zgloszenia MK zostaje nawias.  Rozstrzyga zapisany plik.
$w = Mierz "ctrl_l_check" "md" "- [ ] kupic chleb" @("^l")
if ($w -ne $null) { Rowne "Control+L na checkliscie: zostaje sam tekst" "kupic chleb" $w.TrimEnd("`r","`n") }

# 2. CONTROL+SHIFT+L NA CHECKLISCIE.  To samo pytanie dla listy numerowanej.
$w = Mierz "ctrl_sl_check" "md" "- [ ] kupic chleb" @("^+l")
if ($w -ne $null) { Rowne "Control+Shift+L na checkliscie: numer zamiast pola" "1. kupic chleb" $w.TrimEnd("`r","`n") }

# 3. CHECKLISTA ODHACZONA.  Pole ze znakiem x to ten sam przypadek - mierze
# osobno, bo wzorzec dopuszcza [ ], [x] i [X], a blad moze siedziec w jednym.
$w = Mierz "ctrl_l_xcheck" "md" "- [x] kupic chleb" @("^l")
if ($w -ne $null) { Rowne "Control+L na odhaczonej checkliscie" "kupic chleb" $w.TrimEnd("`r","`n") }

# 4. KILKA WIERSZY ZAZNACZONYCH.  Zaznaczam caly dokument (Control+A) - tu
# zdejmowanie idzie petla po wierszach, wiec blad moze byc widoczny tylko tutaj.
$w = Mierz "ctrl_l_wiele" "md" "- [ ] chleb`r`n- [x] mleko" @("^a", "^l")
if ($w -ne $null) { Rowne "Control+L na dwoch pozycjach checklisty" "chleb`r`nmleko" $w.TrimEnd("`r","`n") }

# 5. KONTROLA ROZNICUJACA.  Na zwyklym wierszu Control+L MA dodac punktor.
# Bez tego wariantu nie wiem, czy komenda w ogole cokolwiek robi.
$w = Mierz "ctrl_l_zwykly" "md" "kupic chleb" @("^l")
if ($w -ne $null) { Rowne "KONTROLA ROZNICUJACA: punktor dodany do zwyklego wiersza" "- kupic chleb" $w.TrimEnd("`r","`n") }

# 6. KONTROLA NEGATYWNA.  Na .txt bramka Markdown ma nie przepuscic komendy.
$w = Mierz "ctrl_l_txt" "txt" "- [ ] kupic chleb" @("^l")
if ($w -ne $null) { Rowne "KONTROLA NEGATYWNA: .txt nietkniety" "- [ ] kupic chleb" $w.TrimEnd("`r","`n") }

# 7. KONTROLA NIEPOGORSZENIA: ODHACZANIE ZADAN.  Wzorzec luzny (PoleLuzneRegex)
# dodany 18.09.2026 dotyczy TYLKO zdejmowania listy.  Przelaczanie zrobione/
# niezrobione (Control+Shift+X) nadal ma isc waskim CzyZadanie - gdybym przez
# pomylke podmienil je tam, "- [-] tekst" zaczelby udawac zadanie.  Mierze wiec,
# ze zadanie normalne przelacza sie dalej, a "[-]" NIE jest przelaczane.
$w = Mierz "odhacz" "md" "- [ ] kupic chleb" @("^+x")
if ($w -ne $null) { Rowne "KONTROLA: Control+Shift+X odhacza zadanie" "- [x] kupic chleb" $w.TrimEnd("`r","`n") }

$w = Mierz "odhacz_minus" "md" "- [-] kupic chleb" @("^+x")
if ($w -ne $null) { Rowne "KONTROLA: pole [-] NIE jest przelaczane jak zadanie" "- [-] kupic chleb" $w.TrimEnd("`r","`n") }

""
"=================== WYNIK: $($global:iOk) OK, $($global:iZle) ZLE ==================="
if ($global:iZle -gt 0) { exit 1 }
