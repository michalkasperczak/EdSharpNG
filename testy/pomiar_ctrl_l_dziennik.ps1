# pomiar_ctrl_l_dziennik.ps1 - CO PROGRAM MOWI NA Control+L (z dziennika mowy).
#
# Program zapisuje kazdy swoj komunikat do pliku Speech.log w katalogu danych
# (EdSharp.cs 22834).  Plik jest czyszczony przy starcie, wiec po jednym
# uruchomieniu widzimy DOKLADNIE, co program powiedzial.
#
# Pytanie pomiaru: czy po Control+L w dzienniku pojawia sie cokolwiek (nazwa
# komendy "Bulleted List", komunikat "Bulleted list on", albo bramka typu
# "Lists work only on Markdown files!") - czy CISZA, co znaczyloby, ze chord
# nie dochodzi do polecenia.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2

function K($s, $ms = 800) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

# Gdzie program trzyma dane.  Szukamy Speech.log pod profilem uzytkownika.
$kandydaci = @(
    "$env:APPDATA\EdSharpNG\Speech.log",
    "$env:APPDATA\EdSharp\Speech.log",
    "$env:LOCALAPPDATA\EdSharpNG\Speech.log",
    "$env:USERPROFILE\EdSharpNG\Speech.log"
)

$plik = "$env:TEMP\dziennik.md"
Set-Content -Path $plik -Value "alfa`r`nbeta`r`ngamma`r`n" -Encoding UTF8

$p = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 14

# Zaznacz wszystko, nacisnij Control+L, potem Control+Shift+L.
K "^{HOME}" 500
K "^a" 600
K "^l" 1800
K "^+l" 1800

Start-Sleep 2
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2

"=== gdzie lezy dziennik mowy ==="
$log = $null
foreach ($k in $kandydaci) {
    if (Test-Path $k) { "znaleziony: $k"; $log = $k; break }
}
if ($log -eq $null) {
    "nie znaleziony w typowych miejscach - szukam pod profilem"
    $szukaj = Get-ChildItem -Path $env:APPDATA, $env:LOCALAPPDATA -Filter "Speech.log" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($szukaj) { $log = $szukaj.FullName; "znaleziony: " + $log }
}

if ($log) {
    "=== TRESC DZIENNIKA (co program powiedzial) ==="
    Get-Content $log | ForEach-Object { "  " + $_ }
} else {
    "BRAK DZIENNIKA - pomiar nierozstrzygajacy"
}

"=== plik po obu skrotach ==="
foreach ($l in (Get-Content $plik)) { "<" + $l + ">" }
