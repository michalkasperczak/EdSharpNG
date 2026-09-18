# pomiar_ctrl_l_pasek.ps1 - CO PROGRAM MOWI NA Control+L.
#
# Poprzednie pomiary: Control+L nie zmienia tekstu, Control+Shift+L zmienia.
# Lektura kodu nie wskazala winnego.  Ten pomiar czyta PASEK STANU programu
# (tam ida komunikaty: nazwa komendy, "Bulleted list on", "Lists work only on
# Markdown files!" itd.) przez UI Automation - czyli dowiadujemy sie, czy
# komenda w ogole wystartowala i co powiedziala.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2

function K($s, $ms = 800) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

# Czyta WSZYSTKIE teksty z okna programu - pasek stanu jest jednym z nich.
function CzytajTeksty($proc) {
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
    if ($root -eq $null) { return @("BRAK OKNA") }
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text)
    $found = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
    $wynik = @()
    foreach ($el in $found) {
        $n = $el.Current.Name
        if ($n -and $n.Trim().Length -gt 0) { $wynik += $n }
    }
    return $wynik
}

$plik = "$env:TEMP\pasek.md"
Set-Content -Path $plik -Value "alfa`r`nbeta`r`n" -Encoding UTF8

$p = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 14
$p.Refresh()

"=== pasek stanu PRZED czymkolwiek ==="
CzytajTeksty $p | ForEach-Object { "  <" + $_ + ">" }

K "^{HOME}" 500
K "^a" 600
K "^l" 1500
$p.Refresh()
"=== pasek stanu PO Control+L ==="
CzytajTeksty $p | ForEach-Object { "  <" + $_ + ">" }

K "^+l" 1500
$p.Refresh()
"=== pasek stanu PO Control+Shift+L ==="
CzytajTeksty $p | ForEach-Object { "  <" + $_ + ">" }

Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
