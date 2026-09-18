# pomiar_ctrl_l_zwykly.ps1 - czy Control+L dziala W OGOLE.
#
# Pomiar pomiar_ctrl_l_checklista.ps1 pokazal, ze Control+Shift+L na checkliscie
# dziala poprawnie, a Control+L nie zmienil pliku WCALE.  Zanim uznamy to za blad
# checklisty, sprawdzamy, czy Control+L rusza cokolwiek na ZWYKLYCH wierszach.
#
# Podejrzenie: RichTextBox ma wbudowany Control+L (wyrownanie do lewej), wiec
# kontrolka moze zjadac skrot, zanim dojdzie do menu.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2

function K($s, $ms = 600) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

# --- A: Control+L na ZWYKLYCH wierszach (ma zrobic punktory) ---
$plikA = "$env:TEMP\ctrl_l_zwykly.md"
Set-Content -Path $plikA -Value "alfa`r`nbeta`r`ngamma`r`n" -Encoding UTF8
$p = Start-Process -FilePath $exe -ArgumentList "`"$plikA`"" -PassThru
Start-Sleep -Seconds 14
K "^{HOME}" 500
K "^a" 500
K "^l" 1200
K "^s" 1500
$p.CloseMainWindow() | Out-Null; Start-Sleep 2
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 1
"=== A: Control+L na zwyklych wierszach (oczekiwane: - alfa) ==="
foreach ($l in (Get-Content $plikA)) { "<" + $l + ">" }

# --- B: to samo, ale Z MENU, nie skrotem.  Menu Misc -> Bulleted List.
# Jesli z menu dziala, a skrotem nie - winna jest kontrolka zjadajaca Control+L.
$plikB = "$env:TEMP\ctrl_l_menu.md"
Set-Content -Path $plikB -Value "- [ ] alfa`r`n- [x] beta`r`n" -Encoding UTF8
$p2 = Start-Process -FilePath $exe -ArgumentList "`"$plikB`"" -PassThru
Start-Sleep -Seconds 14
K "^{HOME}" 500
K "^a" 700
# Alt otwiera pasek menu; szukamy pozycji Bulleted List w menu Misc.
K "%" 900
K "m" 1200
"--- menu Misc otwarte, szukam Bulleted List po nazwie ---"
K "b" 1400
K "^s" 1500
$p2.CloseMainWindow() | Out-Null; Start-Sleep 2
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
"=== B: checklista po probie z MENU ==="
foreach ($l in (Get-Content $plikB)) { "<" + $l + ">" }
