# pomiar_ctrl_l_fokus2.ps1 - OBEJSCIE BLOKADY FOKUSU.
#
# Windows nie pozwala procesowi w tle przestawic okna na wierzch
# (SetForegroundWindow milczaco odmawia) - dlatego poprzedni pomiar dostawal
# tytul okna terminala i sam sie odrzucil.
#
# Obejscie: przypinamy watek wejsciowy naszego procesu do watku okna edytora
# (AttachThreadInput), dopiero wtedy SetForegroundWindow ma prawo dzialac.
# Dodatkowo najpierw minimalizujemy okno terminala, zeby nie walczylo o wierzch.
#
# Pomiar bez POTWIERDZONEGO fokusu jest odrzucany - tak jak poprzednio.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class F {
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint from, uint to, bool attach);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool SetFocus(IntPtr h);

  public static string Tytul() {
    StringBuilder sb = new StringBuilder(512);
    GetWindowText(GetForegroundWindow(), sb, 512);
    return sb.ToString();
  }

  // Przypina nasz watek do watku okna docelowego i przestawia je na wierzch.
  public static void Przypnij(IntPtr cel) {
    uint watekCelu = GetWindowThreadProcessId(cel, IntPtr.Zero);
    uint watekNasz = GetCurrentThreadId();
    AttachThreadInput(watekNasz, watekCelu, true);
    ShowWindow(cel, 9);        // SW_RESTORE
    BringWindowToTop(cel);
    SetForegroundWindow(cel);
    SetFocus(cel);
    AttachThreadInput(watekNasz, watekCelu, false);
  }

  // Minimalizuje okno, ktore aktualnie jest na wierzchu (np. terminal).
  public static void SchowajWierzch() {
    ShowWindow(GetForegroundWindow(), 6);   // SW_MINIMIZE
  }
}
"@

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2

function K($s, $ms = 800) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

function Fokus($proc) {
    $proc.Refresh()
    for ($i = 0; $i -lt 8; $i++) {
        if ($proc.MainWindowHandle -ne [IntPtr]::Zero) { [F]::Przypnij($proc.MainWindowHandle) }
        Start-Sleep -Milliseconds 800
        if ([F]::Tytul() -like "*EdSharp*") { return $true }
        $proc.Refresh()
    }
    return $false
}

function Mierz($nazwa, $tresc, $skrot) {
    $plik = "$env:TEMP\f2_$nazwa.md"
    Set-Content -Path $plik -Value $tresc -Encoding UTF8
    $p = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
    Start-Sleep -Seconds 14

    if (-not (Fokus $p)) {
        "ODMOWA POMIARU ($nazwa): na wierzchu <" + [F]::Tytul() + ">"
        Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
        Start-Sleep 2
        return
    }
    "fokus OK (<" + [F]::Tytul() + ">) - mierze $nazwa"

    K "^{HOME}" 500
    K "^a" 700
    K $skrot 1600
    K "^s" 1600
    Start-Sleep 1
    Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep 2

    "=== WYNIK $nazwa (skrot $skrot) ==="
    foreach ($l in (Get-Content $plik)) { "  <" + $l + ">" }
}

# Schowaj okno terminala, zeby nie walczylo o wierzch.
[F]::SchowajWierzch()
Start-Sleep 2

# KONTROLA: czy pomiar w ogole umie wyslac Control+litera do programu.
# Control+A (zaznacz wszystko) + Control+X (wytnij) ma OPROZNIC plik.
Mierz "kontrola_ctrl_x" "alfa`r`nbeta`r`n" "^x"

# ZGLOSZENIE MK 1.6: Control+L na checkliscie.
Mierz "ctrl_l_checklista" "- [ ] alfa`r`n- [x] beta`r`n" "^l"

# Control+L na zwyklym tekscie - punkt odniesienia.
Mierz "ctrl_l_zwykly" "alfa`r`nbeta`r`n" "^l"

# Control+Shift+L na checkliscie.
Mierz "ctrl_shift_l_checklista" "- [ ] alfa`r`n- [x] beta`r`n" "^+l"
