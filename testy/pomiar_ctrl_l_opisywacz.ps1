# pomiar_ctrl_l_opisywacz.ps1 - CZY Control+L DOCHODZI DO POLECENIA.
#
# Zmierzone (pomiar_ctrl_l_zwykly.ps1): Control+L nie zmienia pliku ani na
# checkliscie, ani na zwyklym tekscie; Control+Shift+L dziala.  Lektura kodu nie
# pokazuje nikogo, kto by ten chord zabieral: hashKey ma Control+L -> "Bulleted
# List", zaden Handle*Key przed tablica go nie lapie.
#
# Ten pomiar pyta SAM PROGRAM.  Control+F1 wlacza "opisywacz klawiszy": kazdy
# nacisniety skrot ma POWIEDZIEC swoja nazwe zamiast sie wykonac.  Komunikaty
# programu ida do dziennika mowy (Extra Speech Log), wiec porownujemy, co program
# mowi na Control+L, a co na Control+Shift+L.
#
# Jesli opisywacz mowi "Bulleted List" - chord dochodzi i wina jest w wykonaniu.
# Jesli MILCZY - chord ginie po drodze i wina jest w routerze klawiszy.

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Windows.Forms

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"
Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 2

function K($s, $ms = 700) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds $ms }

$plik = "$env:TEMP\opisywacz.md"
Set-Content -Path $plik -Value "alfa`r`nbeta`r`n" -Encoding UTF8

$p = Start-Process -FilePath $exe -ArgumentList "`"$plik`"" -PassThru
Start-Sleep -Seconds 14

# Wlacz opisywacz klawiszy (Control+F1), potem nacisnij oba skroty.
K "^{F1}" 1200
K "^l" 1200
K "^+l" 1200
# Wylacz opisywacz.
K "^{F1}" 1000

# Otworz dziennik mowy: menu Misc -> Extra Speech Log.  Skrotu nie ma, wiec
# idziemy menu.  Dziennik otwiera sie jako nowe okno edycji, wiec zapisujemy je
# do pliku i czytamy z dysku.
$log = "$env:TEMP\opisywacz_log.txt"
if (Test-Path $log) { Remove-Item $log -Force }

K "%" 900
K "m" 1200
# W menu Misc szukamy pozycji "Extra Speech Log" - litera E moze trafic w inna
# pozycje, wiec przechodzimy strzalkami i czytamy tytul okna po wejsciu.
for ($i = 0; $i -lt 12; $i++) { K "{DOWN}" 180 }
"--- zrzut: co jest w menu (tytul okna po Escape) ---"
K "{ESC}" 600
K "{ESC}" 600

"=== ile okien ma program (dziennik = osobne okno) ==="
$p2 = Get-Process EdSharpNG -ErrorAction SilentlyContinue
if ($p2) { "tytul glownego okna: " + $p2.MainWindowTitle }

Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
"=== plik po obu skrotach (opisywacz mial NIE zmieniac tekstu) ==="
foreach ($l in (Get-Content $plik)) { "<" + $l + ">" }
