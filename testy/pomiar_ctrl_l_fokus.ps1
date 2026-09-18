# pomiar_ctrl_l_fokus.ps1 - POMIAR Z WYMUSZONYM FOKUSEM.
#
# WAZNA LEKCJA Z POPRZEDNIEGO POMIARU (pomiar_ctrl_kontrola.ps1): tytul okna
# pierwszoplanowego byl "michal@Hermes: /mnt/c/Users/Michal", czyli klawisze
# z SendKeys szly do OKNA TERMINALA, a nie do edytora.  Kazdy pomiar, ktory nie
# sprawdza fokusu, jest niewazny - i tak trzeba traktowac wczesniejsze wyniki
# "Control+L nie dziala".
#
# Tu: wymuszamy fokus przez SetForegroundWindow, POTWIERDZAMY go po tytule okna
# i tylko wtedy mierzymy.  Bez potwierdzenia - odmowa pomiaru.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class W {
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  public static string Tytul() {
    StringBuilder sb = new StringBuilder(512);
    GetWindowText(GetForegroundWindow(), sb, 512);
    return sb.ToString();
  }
  public static void NaWierzch(IntPtr h) {
    ShowWindow(h, 9);      // SW_RESTORE
    BringWindowToTop(h);
    SetForegroundWindow(h);
  }
}
"@

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2

function K($s, $ms = 800) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

function Fokus($proc) {
    $proc.Refresh()
    for ($i = 0; $i -lt 6; $i++) {
        [W]::NaWierzch($proc.MainWindowHandle)
        Start-Sleep -Milliseconds 700
        if ([W]::Tytul() -like "*EdSharp*") { return $true }
    }
    return $false
}

function Mierz($nazwa, $tresc, $skrot) {
    $plik = "$env:TEMP\fokus_$nazwa.md"
    Set-Content -Path $plik -Value $tresc -Encoding UTF8
    $p = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
    Start-Sleep -Seconds 14

    if (-not (Fokus $p)) {
        "ODMOWA POMIARU ($nazwa): nie udalo sie ustawic fokusu, okno na wierzchu to <" + [W]::Tytul() + ">"
        Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
        return
    }
    "fokus potwierdzony: <" + [W]::Tytul() + ">"

    K "^{HOME}" 500
    K "^a" 700
    K $skrot 1600
    K "^s" 1600

    Start-Sleep 1
    Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep 1

    "=== $nazwa (skrot $skrot) ==="
    foreach ($l in (Get-Content $plik)) { "  <" + $l + ">" }
}

# 1. Control+L na zwyklym tekscie - ma zrobic punktory.
Mierz "ctrl_l_zwykly" "alfa`r`nbeta`r`ngamma`r`n" "^l"

# 2. Control+L na checkliscie - ZGLOSZENIE MK: ma zdjac pole RAZEM z punktorem.
Mierz "ctrl_l_checklista" "- [ ] alfa`r`n- [x] beta`r`n" "^l"

# 3. Control+Shift+L na checkliscie - to samo dla listy numerowanej.
Mierz "ctrl_shift_l_checklista" "- [ ] alfa`r`n- [x] beta`r`n" "^+l"
