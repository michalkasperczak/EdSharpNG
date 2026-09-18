# pomiar_ctrl_kontrola.ps1 - KONTROLA POMIARU: czy Control+litera dochodzi.
#
# Control+L nie robi nic, Control+Shift+L dziala.  Zanim uznam Control+L za
# zepsuty w programie, musze wykluczyc, ze to MOJ pomiar nie umie wyslac
# Control+litera (SendKeys bywa gubiony).
#
# Kontrola: Control+J to "Jump to Line ..." - otwiera OKNO DIALOGOWE.  Jesli po
# Control+J tytul aktywnego okna sie zmieni, znaczy ze Control+litera dochodzi
# i pomiar jest wazny.  Wtedy cisza po Control+L jest wina programu.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class W {
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  public static string Tytul() {
    StringBuilder sb = new StringBuilder(512);
    GetWindowText(GetForegroundWindow(), sb, 512);
    return sb.ToString();
  }
}
"@

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2

function K($s, $ms = 900) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

$plik = "$env:TEMP\kontrola.md"
Set-Content -Path $plik -Value "alfa`r`nbeta`r`n" -Encoding UTF8

$p = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 14

"tytul na starcie:            <" + [W]::Tytul() + ">"

# KONTROLA 1: Control+J - ma otworzyc okno "Jump to Line".
K "^j" 1500
"tytul po Control+J:          <" + [W]::Tytul() + ">"
K "{ESC}" 900
"tytul po Escape:             <" + [W]::Tytul() + ">"

# KONTROLA 2: Control+G (Go to Line/inne) - drugi swiadek na Control+litera.
K "^r" 1500
"tytul po Control+R (Replace):<" + [W]::Tytul() + ">"
K "{ESC}" 900

# POMIAR: Control+L na zaznaczonym tekscie.
K "^{HOME}" 500
K "^a" 700
K "^l" 1500
"tytul po Control+L:          <" + [W]::Tytul() + ">"
K "^s" 1500

Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
"=== plik po Control+L (oczekiwane punktory: - alfa) ==="
foreach ($l in (Get-Content $plik)) { "<" + $l + ">" }
