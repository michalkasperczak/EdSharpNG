$ErrorActionPreference = "Stop"
# Aktywacja okna EdSharpNG do pomiaru - wersja 2 (27.08.2026).
#
# CZEMU WERSJA 2: wersja 1 robila ShowWindow(SW_RESTORE) i od razu klikala
# w srodek prostokata okna. Gdy na wierzchu bylo CUDZE okno (u nas Edge
# z otwarta strona), ShowWindow nie podnosilo EdSharpa nad nie i klikniecie
# szlo W CUDZA APLIKACJE. Skrypt wypisywal wtedy EDSHARP_AKTYWNY=False,
# ale klik juz poszedl - czyli pomiar nie tylko nie dzialal, ale MIESZAL
# w oknie uzytkownika.
#
# CO ROBI WERSJA 2:
#  1. podnosi okno przez SetWindowPos(HWND_TOPMOST) - to nie wymaga fokusu,
#     wiec dziala z procesu w tle,
#  2. SPRAWDZA przez WindowFromPoint, czyj jest punkt, w ktory ma kliknac,
#  3. klika TYLKO wtedy, gdy ten punkt nalezy do procesu EdSharpNG,
#  4. na koniec zdejmuje TOPMOST i przywraca pozycje myszy.
Add-Type @"
using System; using System.Runtime.InteropServices; using System.Text;
public class M {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern bool GetCursorPos(out System.Drawing.Point p);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, IntPtr e);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll", CharSet=CharSet.Auto)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int c);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(System.Drawing.Point p);
  [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint ga);
}
"@ -ReferencedAssemblies System.Drawing

$p = Get-Process EdSharpNG
$h = $p.MainWindowHandle
[void][M]::ShowWindow($h, 9)          # SW_RESTORE
Start-Sleep -Milliseconds 300

$TOPMOST    = [IntPtr]::new(-1)
$NOTOPMOST  = [IntPtr]::new(-2)
$NOMOVE_NOSIZE = 0x0001 -bor 0x0002   # SWP_NOSIZE | SWP_NOMOVE
[void][M]::SetWindowPos($h, $TOPMOST, 0, 0, 0, 0, $NOMOVE_NOSIZE)
Start-Sleep -Milliseconds 450

$r = New-Object M+RECT
[void][M]::GetWindowRect($h, [ref]$r)
Write-Output ("okno EdSharp: " + $r.L + "," + $r.T + " - " + $r.R + "," + $r.B)

$old = New-Object System.Drawing.Point
[void][M]::GetCursorPos([ref]$old)

$cx = [int](($r.L + $r.R) / 2)
$cy = [int]($r.T + (($r.B - $r.T) * 0.6))

# BRAMKA: czyj jest punkt, w ktory chcemy kliknac?
$pt = New-Object System.Drawing.Point
$pt.X = $cx; $pt.Y = $cy
$hUnder = [M]::WindowFromPoint($pt)
$hRoot  = [M]::GetAncestor($hUnder, 2)   # GA_ROOT
$pidUnder = 0
[void][M]::GetWindowThreadProcessId($hRoot, [ref]$pidUnder)
Write-Output ("pod punktem " + $cx + "," + $cy + " jest pid=" + $pidUnder + " (EdSharp=" + $p.Id + ")")

if ($pidUnder -ne $p.Id) {
  [void][M]::SetWindowPos($h, $NOTOPMOST, 0, 0, 0, 0, $NOMOVE_NOSIZE)
  Write-Output "NIE KLIKAM: pod kursorem jest cudze okno - przerywam bez klikania"
  Write-Output "EDSHARP_AKTYWNY=False"
  exit 3
}

[void][M]::SetCursorPos($cx, $cy)
Start-Sleep -Milliseconds 250
[M]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)   # LEFTDOWN
[M]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)   # LEFTUP
Start-Sleep -Milliseconds 900

[void][M]::SetCursorPos($old.X, $old.Y)

$fg = [M]::GetForegroundWindow()
$sb = New-Object System.Text.StringBuilder 512
[void][M]::GetWindowText($fg, $sb, 512)
$pidNow = 0
[void][M]::GetWindowThreadProcessId($fg, [ref]$pidNow)
Write-Output ("foreground: " + $sb.ToString() + " pid=" + $pidNow)
Write-Output ("EDSHARP_AKTYWNY=" + ($pidNow -eq $p.Id))
