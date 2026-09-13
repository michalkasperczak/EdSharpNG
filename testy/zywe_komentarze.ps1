# CZY PRZYWROCONE Alt+Shift+PageDown/PageUp NAPRAWDE SKACZE PO KOMENTARZACH.
#
# Michal: "nie mowilem, zebys Alt Shift Page Up Page Down likwidowal, jezeli
# chodzi o nawigacje po tych komentarzach, czyli nazwanych zakladkach".
# W 5.0.96 zdjalem te klawisze za szeroko; w 5.0.97 wrocily.  Wpis w Hotkeys.ini
# to jeszcze nie dzialanie - tu sprawdzamy skok w zywym dokumencie.
#
# JAK: robimy plik z trzema komentarzami markdown rozdzielonymi tekstem, stawiamy
# kursor na poczatku, naciskamy Alt+Shift+PageDown i pytamy program, w ktorym
# wierszu jest kursor (Control+Shift+G mowi pozycje... nie polegamy na mowie -
# czytamy zaznaczenie).  Prostsza i pewniejsza droga: po skoku zaznaczamy do konca
# wiersza (Shift+End) i kopiujemy - jesli kursor stoi w komentarzu, w schowku
# bedzie tresc tego komentarza.

Add-Type -AssemblyName System.Windows.Forms
$sig = @'
using System;
using System.Runtime.InteropServices;
public class K4 {
  [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
}
'@
if (-not ("K4" -as [type])) { Add-Type -TypeDefinition $sig }
$LALT=0xA4; $LSHIFT=0xA0; $PGDN=0x22; $PGUP=0x21; $DOWN=0; $UP=2; $EXT=1

# plik do pomiaru
$sciezka = 'C:\EdSharpBuild\komentarze_pomiar.md'
$tekst = @"
Pierwszy wiersz zwyklego tekstu.
<!-- PIERWSZY KOMENTARZ -->
Drugi wiersz zwyklego tekstu.
Trzeci wiersz zwyklego tekstu.
<!-- DRUGI KOMENTARZ -->
Czwarty wiersz zwyklego tekstu.
<!-- TRZECI KOMENTARZ -->
Ostatni wiersz.
"@
Set-Content -Path $sciezka -Value $tekst -Encoding UTF8

# CZYSTY START: zamykamy program, zeby nie zostal otwarty ten sam plik z
# poprzedniego przebiegu (patrz pulapka "Open Again?" wyzej).
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
$p = Start-Process -FilePath 'C:\Program Files\EdSharpNG\EdSharpNG.exe' -PassThru
Start-Sleep -Seconds 10
$p = Get-Process -Id $p.Id
if (-not $p) { Write-Output "BLAD: program nie wystartowal"; exit 2 }
[K4]::keybd_event([byte]$LALT,0,$DOWN,[UIntPtr]::Zero)
[K4]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
[K4]::keybd_event([byte]$LALT,0,$UP,[UIntPtr]::Zero)
Start-Sleep -Milliseconds 500
$fg=[K4]::GetForegroundWindow(); $pid2=0
[K4]::GetWindowThreadProcessId($fg,[ref]$pid2) | Out-Null
if ($pid2 -ne $p.Id) { Write-Output "BLAD: okno nie na wierzchu"; exit 2 }

# otwarcie pliku: Control+O, wpisanie sciezki, Enter
[System.Windows.Forms.SendKeys]::SendWait("^o")
Start-Sleep -Milliseconds 1200
[System.Windows.Forms.SendKeys]::SendWait($sciezka.Replace("\","\"))
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
Start-Sleep -Milliseconds 1800
Write-Output ("tytul okna: [" + (Get-Process -Id $p.Id).MainWindowTitle + "]")

# kursor na sam poczatek
[System.Windows.Forms.SendKeys]::SendWait("^{HOME}")
Start-Sleep -Milliseconds 300

function Skok([byte]$vk) {
  [K4]::keybd_event([byte]$LALT,0,$DOWN,[UIntPtr]::Zero)
  [K4]::keybd_event([byte]$LSHIFT,0,$DOWN,[UIntPtr]::Zero)
  Start-Sleep -Milliseconds 50
  [K4]::keybd_event($vk,0,($EXT -bor $DOWN),[UIntPtr]::Zero)
  Start-Sleep -Milliseconds 50
  [K4]::keybd_event($vk,0,($EXT -bor $UP),[UIntPtr]::Zero)
  [K4]::keybd_event([byte]$LSHIFT,0,$UP,[UIntPtr]::Zero)
  [K4]::keybd_event([byte]$LALT,0,$UP,[UIntPtr]::Zero)
  Start-Sleep -Milliseconds 700
}

function CzytajWiersz() {
  [System.Windows.Forms.SendKeys]::SendWait("{HOME}")
  Start-Sleep -Milliseconds 150
  [System.Windows.Forms.SendKeys]::SendWait("+{END}")
  Start-Sleep -Milliseconds 200
  [System.Windows.Forms.SendKeys]::SendWait("^c")
  Start-Sleep -Milliseconds 450
  $w = ""
  try { $w = [System.Windows.Forms.Clipboard]::GetText() } catch { $w = "" }
  [System.Windows.Forms.SendKeys]::SendWait("{HOME}")
  Start-Sleep -Milliseconds 120
  return $w.Trim()
}

Write-Output "--- Alt+Shift+PageDown (nastepny komentarz):"
Skok ([byte]$PGDN); $w1 = CzytajWiersz; Write-Output ("  wiersz: [" + $w1 + "]")
Skok ([byte]$PGDN); $w2 = CzytajWiersz; Write-Output ("  wiersz: [" + $w2 + "]")
Skok ([byte]$PGDN); $w3 = CzytajWiersz; Write-Output ("  wiersz: [" + $w3 + "]")
# NAPRAWDE bez czytania miedzy skokami: CzytajWiersz rusza kursorem (HOME,
# Shift+End), wiec wolane MIEDZY skokami zmienia to, co program uznaje za
# "biezaca pozycje".  Poprzednia wersja tej sondy klamala nazwa.
Write-Output "--- Alt+Shift+PageUp, dwa skoki pod rzad, odczyt tylko na koncu:"
Skok ([byte]$PGUP)
Skok ([byte]$PGUP)
$w4 = CzytajWiersz; Write-Output ("  po dwoch PageUp: [" + $w4 + "]")
$w5 = $w4

$dobre = 0
if ($w1 -like "*PIERWSZY KOMENTARZ*") { $dobre++ }
if ($w2 -like "*DRUGI KOMENTARZ*")    { $dobre++ }
if ($w3 -like "*TRZECI KOMENTARZ*")   { $dobre++ }
if ($w4 -like "*DRUGI KOMENTARZ*" -or $w5 -like "*DRUGI KOMENTARZ*") { $dobre++ }
Write-Output ("WYNIK: trafien " + $dobre + " z 4")
if ($dobre -eq 4) { Write-Output "OK: skoki po komentarzach dzialaja w oba kierunki" }
else { Write-Output "ZLE: skoki nie trafiaja w komentarze" }
