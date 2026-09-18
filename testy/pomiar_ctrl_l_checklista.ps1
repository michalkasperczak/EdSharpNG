# pomiar_ctrl_l_checklista.ps1 - ZGLOSZENIE MK z pkt 1.6
#
# MK: "Control+L (lista punktowana) na wierszach, ktore SA checklista - czy nie
# zjada pol wyboru.  Control+Shift+L (lista numerowana) na checkliscie - to samo."
# MK odpowiedzial: "Wyglada na to, ze usuwa wtedy nawiasy kwadratowe pozostawiajac
# znaki - i cyfry z listy."
#
# W kodzie 5.0.112 poprawka JEST (EdSharp.cs 15278 i 15618: Zadania.ZdejmijPole).
# Wiec albo MK testowal starsza wersje, albo poprawka nie dziala.  MIERZYMY.
#
# Oczekiwane po Control+L na czystej checkliscie "- [ ] a":
#   wg kodu: pole SCHODZI razem z punktorem -> zostaje goly tekst "a"
#   MK widzi: "- a" (czyli zdjete tylko nawiasy, punktor zostal)

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"
if (-not (Test-Path $exe)) { "BRAK PROGRAMU: $exe"; exit 1 }

# Sprzatanie: zamknij poprzednie okna programu i samouczek w przegladarce.
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2

function K($s, $ms = 600) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

# --- POMIAR A: Control+L na checkliscie ---
$plikA = "$env:TEMP\pomiar_ctrl_l_A.md"
Set-Content -Path $plikA -Value "- [ ] alfa`r`n- [x] beta`r`n- [ ] gamma`r`n" -Encoding UTF8

$procA = Start-Process -FilePath $exe -ArgumentList "`"$plikA`"" -PassThru
Start-Sleep -Seconds 14

# Zaznacz wszystko i nacisnij Control+L
K "^{HOME}" 500
K "^a" 500
K "^l" 1200
K "^s" 1500

$procA.CloseMainWindow() | Out-Null
Start-Sleep 2
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 1

"=== POMIAR A: Control+L na checkliscie ==="
"--- plik po Control+L (kazdy wiersz w nawiasach ostrych) ---"
foreach ($l in (Get-Content $plikA)) { "<" + $l + ">" }

# --- POMIAR B: Control+Shift+L na checkliscie ---
$plikB = "$env:TEMP\pomiar_ctrl_l_B.md"
Set-Content -Path $plikB -Value "- [ ] alfa`r`n- [x] beta`r`n- [ ] gamma`r`n" -Encoding UTF8

$procB = Start-Process -FilePath $exe -ArgumentList "`"$plikB`"" -PassThru
Start-Sleep -Seconds 14

K "^{HOME}" 500
K "^a" 500
K "^+l" 1200
K "^s" 1500

$procB.CloseMainWindow() | Out-Null
Start-Sleep 2
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force

"=== POMIAR B: Control+Shift+L na checkliscie ==="
"--- plik po Control+Shift+L ---"
foreach ($l in (Get-Content $plikB)) { "<" + $l + ">" }

"=== wersja programu ==="
(Get-Item $exe).VersionInfo.FileVersion
