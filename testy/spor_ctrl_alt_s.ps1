# POMIAR ROZSTRZYGAJACY SPOR O Control+Alt+S kontra "s z kreska".
#
# STAN SPORU.  Michal (13.09.2026): "w EdSharpie to juz dzialalo wczesniej
# bezblednie, w AMC w sumie tez.  Alt-Ctrl-s nie wchodzilo w konflikt z s i tak
# dalej."  Komentarz w kodzie z 30.08.2026 twierdzi ODWROTNIE: zmierzono, ze
# przypisanie komendy do Control+Alt+E "odbiera litere e z ogonkiem po cichu".
# Jedno z tych zdan jest nieprawdziwe i trzeba to zmierzyc, a nie przegadac.
#
# CO MIERZE.  Przypisuje istniejaca komende do Control+Alt+S w Hotkeys.ini
# (kopia zapasowa i przywrocenie na koncu), potem w jednym uruchomieniu:
#   A. prawym Altem + S       -> czy w dokumencie pojawia sie "s z kreska" (347),
#   B. lewym Control+Alt + S  -> czy litera NIE wchodzi (czyli chord zjadl skrot).
#
# DLACZEGO POPRZEDNIA PROBA NIC NIE ZMIERZYLA: Hotkeys.ini w Program Files ma
# atrybut "tylko do czytania" (zmierzone: -r-xr-xr-x), wiec zapis skrotu cicho
# padal i program przez caly pomiar mial STARE skroty.  Tutaj atrybut jest
# zdejmowany i zapis jest SPRAWDZANY odczytem.
#
# WARTOSC ODNIESIENIA: przed chordami licze znaki w swiezym pliku, zeby "2 znaki"
# z konca wiersza nie zostaly wziete za wpisana litere.

$ErrorActionPreference = "Stop"
$INI  = "C:\EdSharpSpor\Hotkeys.ini"
$KOP  = "C:\EdSharpBuild\Hotkeys_kopia.ini"
$DOK  = "C:\EdSharpBuild\spor_s.txt"
$EXE  = "C:\EdSharpSpor\EdSharpNG.exe"

Add-Type -Namespace W -Name K -MemberDefinition @'
[DllImport("user32.dll")] public static extern void keybd_event(byte k, byte s, uint f, int e);
'@
$VK_LCTRL=0xA2; $VK_LMENU=0xA4; $VK_RMENU=0xA5; $VK_S=0x53
function Dol([byte]$k){[W.K]::keybd_event($k,0,0,0)}
function Gora([byte]$k){[W.K]::keybd_event($k,0,2,0)}

# --- przygotowanie pliku skrotow
Copy-Item $INI $KOP -Force
attrib -R $INI
$tresc = Get-Content $INI -Raw
# Go to Footnote siedzi na Control+Alt+K - dokladam mu DRUGI chord Control+Alt+S,
# bo format dopuszcza liste chordow rozdzielonych przecinkiem.
$nowa = $tresc -replace 'Go to Footnote=Control\+Alt\+K,', 'Go to Footnote=Control+Alt+K,Control+Alt+S,'
Set-Content $INI $nowa -Encoding UTF8 -NoNewline
$po = Get-Content $INI -Raw
if ($po -notmatch 'Control\+Alt\+S') {
  Write-Output "POMIAR NIEWAZNY: nie udalo sie zapisac skrotu do Hotkeys.ini"
  Copy-Item $KOP $INI -Force; exit 1
}
Write-Output "skrot Control+Alt+S zapisany i odczytany z pliku: OK"

# --- czysty dokument
Set-Content $DOK "linia" -Encoding UTF8
Start-Process $EXE -ArgumentList "`"$DOK`""
Start-Sleep -Seconds 9

$sh = New-Object -ComObject WScript.Shell
$p = Get-Process EdSharpNG -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Output "POMIAR NIEWAZNY: program nie wstal"; Copy-Item $KOP $INI -Force; exit 1 }
$sh.AppActivate($p.Id) | Out-Null
Start-Sleep -Seconds 2
$sh.SendKeys("^{END}")
Start-Sleep -Milliseconds 700

function Zapisz-I-Policz {
  $sh.SendKeys("^s"); Start-Sleep -Seconds 2
  $t = Get-Content $DOK -Raw
  return $t
}
$odniesienie = Zapisz-I-Policz
$lenBaza = $odniesienie.Length

# --- A. PRAWY Alt + S  (pisanie polskiej litery)
Dol $VK_RMENU; Start-Sleep -Milliseconds 120
Dol $VK_S; Start-Sleep -Milliseconds 120; Gora $VK_S
Start-Sleep -Milliseconds 120; Gora $VK_RMENU
Start-Sleep -Milliseconds 600
$poPrawym = Zapisz-I-Policz
$dodanePrawym = $poPrawym.Substring($lenBaza)
$kodyPrawy = ($dodanePrawym.ToCharArray() | ForEach-Object {[int]$_}) -join ","
$lenBaza2 = $poPrawym.Length

# --- B. LEWY Control+Alt + S  (chord skrotu)
Dol $VK_LCTRL; Dol $VK_LMENU; Start-Sleep -Milliseconds 120
Dol $VK_S; Start-Sleep -Milliseconds 120; Gora $VK_S
Start-Sleep -Milliseconds 120; Gora $VK_LMENU; Gora $VK_LCTRL
Start-Sleep -Milliseconds 900
$poLewym = Zapisz-I-Policz
$dodaneLewym = $poLewym.Substring($lenBaza2)
$kodyLewy = ($dodaneLewym.ToCharArray() | ForEach-Object {[int]$_}) -join ","

Write-Output "--- WYNIK"
Write-Output ("A. prawy Alt+S wpisal kody: " + ($kodyPrawy -replace '^$','brak'))
Write-Output ("B. lewy Control+Alt+S wpisal kody: " + ($kodyLewy -replace '^$','brak'))

$aPisze = ($kodyPrawy -match '347')
$bNiePisze = ($kodyLewy -notmatch '347')
if ($aPisze -and $bNiePisze) {
  Write-Output "WNIOSEK: WSPOLISTNIEJA - prawy Alt pisze s z kreska, lewy Control+Alt nie pisze (idzie na skrot)."
} elseif ($aPisze -and -not $bNiePisze) {
  Write-Output "WNIOSEK: OBA PISZA litere - skrot na Control+Alt+S jest zjadany przez pisanie."
} elseif (-not $aPisze) {
  Write-Output "WNIOSEK: prawy Alt NIE wpisal litery - skrot ODEBRAL pisanie (to potwierdza komentarz z 30.08)."
}

# --- porzadki: zamknij program i przywroc plik skrotow
Start-Sleep -Milliseconds 300
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
Copy-Item $KOP $INI -Force
attrib +R $INI
$sprawdz = Get-Content $INI -Raw
if ($sprawdz -match 'Control\+Alt\+S') { Write-Output "UWAGA: nie przywrocilem Hotkeys.ini!" }
else { Write-Output "Hotkeys.ini przywrocony do stanu wyjsciowego." }
