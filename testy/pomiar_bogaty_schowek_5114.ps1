param([string]$Label='live')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
$data=$null
for($i=0;$i -lt 15;$i++){try{$data=[Windows.Forms.Clipboard]::GetDataObject(); if($data){break}}catch{}; Start-Sleep -Milliseconds 80}
if(!$data){throw 'Clipboard unavailable'}
$fail=0; $pass=0
function Check([bool]$ok,[string]$name){if($ok){$script:pass++; "PASS $name"}else{$script:fail++; "FAIL $name"}}
$rtf=[string]$data.GetData('Rich Text Format')
$html=[string]$data.GetData('HTML Format')
$plain=[string]$data.GetData('UnicodeText')
$md=[string]$data.GetData('EdSharpNG.Markdown')
Check ($plain.Contains('Koniec dokumentu.')) 'entire selected document copied'
Check ($md.Replace("`r`n","`n") -ceq [IO.File]::ReadAllText('C:\EdSharpBuild\rich-copy-fixture.md').Replace("`r`n","`n")) 'original Markdown preserved exactly'
Check ($rtf.Contains('\outlinelevel0') -and $rtf.Contains('\outlinelevel1')) 'RTF contains semantic heading levels'
Check ($rtf.Contains('HYPERLINK "https://example.org/test?q=1&x=2"')) 'RTF preserves hyperlink destination'
Check ($html.Length -gt 0) 'HTML clipboard flavor available'
if($html.Length -gt 0){
$bytes=[Text.Encoding]::UTF8.GetBytes($html)
$start=[int]([regex]::Match($html,'StartFragment:(\d+)').Groups[1].Value)
$end=[int]([regex]::Match($html,'EndFragment:(\d+)').Groups[1].Value)
$frag=[Text.Encoding]::UTF8.GetString($bytes,$start,$end-$start)
Check ($frag -match '<h1>.*?</h1>' -and $frag -match '<h2>.*?</h2>') 'HTML semantic headings'
Check ($frag.Contains('href="https://example.org/test?q=1&amp;x=2"')) 'HTML hyperlink destination'
Check ($frag.Contains('<ul>') -and ([regex]::Matches($frag,'<li>').Count -eq 2)) 'HTML list has two real items'
Check ([Net.WebUtility]::HtmlDecode($frag).Contains('Żółty nagłówek') -and $frag.Contains('łączem') -and [Text.Encoding]::UTF8.GetString($bytes,$end,18) -eq '<!--EndFragment-->') 'UTF-8 clipboard byte offsets preserve Polish'
[IO.File]::WriteAllText("C:\EdSharpBuild\rich-$Label-fragment.html",$frag,[Text.UTF8Encoding]::new($false))
}
"RESULT $Label : $pass PASS / $fail FAIL"
if($fail -ne 0){exit 1}
