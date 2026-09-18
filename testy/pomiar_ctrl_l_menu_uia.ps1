# pomiar_ctrl_l_menu_uia.ps1 - WYWOLANIE "Bulleted List" WPROST Z MENU.
#
# Rozstrzygniecie: czy nie dziala SAMA KOMENDA, czy tylko jej SKROT Control+L.
# Poprzednie pomiary robily to strzalkami i literami w menu, wiec mogly trafic
# w inna pozycje.  Tu szukamy pozycji PO NAZWIE przez UI Automation i wywolujemy
# ja wzorcem Invoke - dokladnie tak, jak zrobilby to czytnik ekranu.
#
# Jesli z menu tekst sie zmieni, a skrotem nie - winny jest router klawiszy.
# Jesli z menu tez nie - winna jest sama komenda.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2

function K($s, $ms = 700) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

function ZnajdzPoNazwie($root, $nazwa) {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $nazwa)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

$plik = "$env:TEMP\menu_uia.md"
Set-Content -Path $plik -Value "- [ ] alfa`r`n- [x] beta`r`n" -Encoding UTF8

$p = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 14
$p.Refresh()

$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
if ($root -eq $null) { "BRAK OKNA"; exit 1 }

# Zaznacz cala tresc.
K "^{HOME}" 500
K "^a" 700

# Otworz menu Misc, zeby pozycje powstaly w drzewie dostepnosci.
K "%" 900
K "m" 1200

"=== szukam pozycji menu po nazwie ==="
$item = ZnajdzPoNazwie $root "Bulleted List"
if ($item -eq $null) {
    # AccessibleName ma doklejony skrot, wiec probujemy po czesci nazwy.
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::MenuItem)
    $wszystkie = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
    "pozycji menu w drzewie: " + $wszystkie.Count
    foreach ($el in $wszystkie) {
        $n = $el.Current.Name
        if ($n -like "*ullet*" -or $n -like "*umbered*") { "  KANDYDAT: <" + $n + ">"; $item = $el }
    }
}

if ($item -ne $null) {
    "znaleziona pozycja: <" + $item.Current.Name + ">"
    "wlaczona (IsEnabled): " + $item.Current.IsEnabled
    try {
        $inv = $item.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $inv.Invoke()
        "WYWOLANE wzorcem Invoke"
    } catch {
        "Invoke niedostepny: " + $_.Exception.Message
    }
    Start-Sleep 2
} else {
    "NIE ZNALAZLEM pozycji Bulleted List w drzewie dostepnosci"
    K "{ESC}" 600
    K "{ESC}" 600
}

K "^s" 1800
Start-Sleep 1
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force

"=== plik po wywolaniu Z MENU ==="
foreach ($l in (Get-Content $plik)) { "<" + $l + ">" }
