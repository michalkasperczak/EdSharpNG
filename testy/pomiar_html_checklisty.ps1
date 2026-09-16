# pomiar_html_checklisty.ps1 - EKSPORT HTML CHECKLIST (5.0.112).
#
# Wola MarkdownDocumentToHtml WPROST na zbudowanej binarce (refleksja), zamiast
# klikac przez GUI: metoda jest publiczna i statyczna, wiec da sie zmierzyc
# dokladnie to, co program wypisuje, bez okien i bez czekania na przegladarke.
#
# KONTROLE ROZNICUJACE:
#   - zwykly punktor musi dalej dawac golе <li> BEZ pola wyboru (asercja 4),
#   - w wyniku nie moze byc nawiasow "[x]" ani "[ ]" (asercja 5): samo obecne
#     <input> nie dowodzi, ze stary zapis zniknal z tresci.

$ErrorActionPreference = "Stop"
$dll = "C:\EdSharpBuild\EdSharpNG.exe"
$asm = [System.Reflection.Assembly]::LoadFrom($dll)

$flagi = [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static
$m = $null
foreach ($t in $asm.GetTypes()) {
	$kand = $t.GetMethod("MarkdownDocumentToHtml", $flagi)
	if ($kand) { $m = $kand; $typ = $t; break }
}
if (-not $m) { Write-Output "ZLE  0 nie znalazlem MarkdownDocumentToHtml w binarce"; exit 1 }
Write-Output "OK   0 metoda znaleziona w typie $($typ.FullName)"

$zrodlo = "# Zadania`n`n- [ ] kupic chleb`n- [x] zaplacic rachunek`n- zwykly punktor`n"
$html = $m.Invoke($null, @([object]$zrodlo, [object]"Zadania"))

Write-Output "--- WYNIK ---"
Write-Output $html
Write-Output "--- ASERCJE ---"

$ok = 0; $zle = 0
function A($opis, $war) {
	if ($war) { Write-Output "OK   $opis"; $script:ok++ } else { Write-Output "ZLE  $opis"; $script:zle++ }
}

A "1 pozycja niezrobiona ma pole wyboru NIEzaznaczone" ($html -match '<input type="checkbox" id="task1" disabled />')
A "2 pozycja zrobiona ma pole wyboru ZAZNACZONE" ($html -match '<input type="checkbox" id="task2" disabled checked />')
A "3 etykieta wiaze tekst z polem przez for" ($html -match '<label for="task1">kupic chleb</label>' -and $html -match '<label for="task2">zaplacic rachunek</label>')
A "4 KONTROLA: zwykly punktor bez pola wyboru" ($html -match '<li>zwykly punktor</li>')
A "5 KONTROLA: nawiasy stanu NIE zostaly w tresci" (-not ($html -match '\[x\]') -and -not ($html -match '\[ \]'))
A "6 identyfikatory pol sa ROZNE (label wskazuje wlasciwe pole)" ($html -match 'id="task1"' -and $html -match 'id="task2"')

Write-Output ""
Write-Output "WYNIK: $ok OK, $zle ZLE"
if ($zle -gt 0) { exit 1 }
