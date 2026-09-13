# KONTROLA POZYTYWNA do pomiaru sporu: czy skrot na LEWYM Control+Alt w ogole
# dochodzi do komendy, gdy litera NIE ma polskiego odpowiednika.
#
# PO CO: pomiar spor_ctrl_alt_s.ps1 pokazal, ze Control+Alt+S (S ma "s z kreska")
# wpisuje litere z OBU Altow i komenda nie rusza.  To moze znaczyc dwie rzeczy:
#   (a) uklad klawiatury sam robi znak i zaden skrot Control+Alt+litera nie ma
#       szans - wtedy problem nie istnieje, bo litery dzialaja,
#   (b) albo moj bezpiecznik blokuje wszystko, tez lewy Alt - wtedy zepsulem
#       Control+Alt+K, ktore dzialalo.
# Bez tej kontroli nie wiem, ktora mozliwosc zachodzi, a to zmienia wniosek.
#
# MIERZE: lewy Control+Alt+K (komenda "Go to Footnote").  Jesli komenda dziala,
# litera nie zostaje wpisana i zachodzi (a).  Jesli K tez sie wpisuje jako znak,
# zachodzi (b) i bezpiecznik trzeba wycofac.

$ErrorActionPreference = "Stop"
$DOK = "C:\EdSharpBuild\kontrola_k.txt"
$EXE = "C:\EdSharpSpor\EdSharpNG.exe"

Add-Type -Namespace W2 -Name K2 -MemberDefinition @'
[DllImport("user32.dll")] public static extern void keybd_event(byte k, byte s, uint f, int e);
'@
$VK_LCTRL=0xA2; $VK_LMENU=0xA4; $VK_K=0x4B
function Dol([byte]$k){[W2.K2]::keybd_event($k,0,0,0)}
function Gora([byte]$k){[W2.K2]::keybd_event($k,0,2,0)}

Set-Content $DOK "linia" -Encoding UTF8
Start-Process $EXE -ArgumentList "`"$DOK`""
Start-Sleep -Seconds 9
$sh = New-Object -ComObject WScript.Shell
$p = Get-Process EdSharpNG -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Output "POMIAR NIEWAZNY: program nie wstal"; exit 1 }
$sh.AppActivate($p.Id) | Out-Null
Start-Sleep -Seconds 2
$sh.SendKeys("^{END}")
Start-Sleep -Milliseconds 700
$sh.SendKeys("^s"); Start-Sleep -Seconds 2
$baza = (Get-Content $DOK -Raw).Length

Dol $VK_LCTRL; Dol $VK_LMENU; Start-Sleep -Milliseconds 120
Dol $VK_K; Start-Sleep -Milliseconds 120; Gora $VK_K
Start-Sleep -Milliseconds 120; Gora $VK_LMENU; Gora $VK_LCTRL
Start-Sleep -Milliseconds 900
$sh.SendKeys("^s"); Start-Sleep -Seconds 2
$po = Get-Content $DOK -Raw
$dodane = $po.Substring($baza)
$kody = ($dodane.ToCharArray() | ForEach-Object {[int]$_}) -join ","

Write-Output ("lewy Control+Alt+K wpisal kody: " + ($kody -replace '^$','brak - chord poszedl na komende'))
if ($kody -match '107|75') { Write-Output "WNIOSEK: ZLE - bezpiecznik zjadl tez lewy Control+Alt, K wpadlo jako litera." }
else { Write-Output "WNIOSEK: OK - lewy Control+Alt+K nie wpisuje znaku, chord dziala jako skrot." }

Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
