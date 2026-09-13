# Ustawia okno EdSharpNG na wierzchu i zostawia je aktywne, zeby zywy NVDA
# (mostek MCP) czytal MENU PROGRAMU, a nie przypadkowe okno na pulpicie.
#
# Sonda pomiarowa robi to sama w petli, ale przy sprawdzaniu czytnikiem
# potrzebny jest osobny, jednorazowy krok: NVDA mowi to, co ma fokus, wiec bez
# tego kroku pierwsze pytanie o fokus trafia w cudze okno (zmierzone: czytalo
# przycisk "OK" obcego dialogu).
$ErrorActionPreference = "Stop"
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class F {
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool f);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  public static bool NaWierzch(IntPtr h) {
    uint moj = GetCurrentThreadId();
    uint obcy = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
    if (obcy != 0 && obcy != moj) AttachThreadInput(moj, obcy, true);
    ShowWindow(h, 9); BringWindowToTop(h); SetForegroundWindow(h);
    System.Threading.Thread.Sleep(500);
    if (obcy != 0 && obcy != moj) AttachThreadInput(moj, obcy, false);
    return GetForegroundWindow() == h;
  }
}
"@
$p = Get-Process -Name EdSharpNG -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Output "BLAD: EdSharpNG nie dziala"; exit 1 }
$ok = [F]::NaWierzch($p.MainWindowHandle)
Write-Output ("na wierzchu: {0} | tytul: {1}" -f $ok, $p.MainWindowTitle)
