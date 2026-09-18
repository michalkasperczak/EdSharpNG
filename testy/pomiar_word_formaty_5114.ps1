$ErrorActionPreference='Stop'
$dir='C:\EdSharpRichQA'
$word=$null
$result=@()
try {
$word=New-Object -ComObject Word.Application
$word.Visible=$false; $word.DisplayAlerts=0; $word.AutomationSecurity=3
foreach($name in @('before.rtf','after.rtf','after.html')){
$doc=$null
try {
$doc=$word.Documents.Open("$dir\$name",$false,$true,$false)
$headings=@(); $listItems=@()
foreach($p in $doc.Paragraphs){
$t=$p.Range.Text.Trim()
if($p.OutlineLevel -ne 10){$headings+=@{text=$t; level=[int]$p.OutlineLevel}}
if($p.Range.ListFormat.ListType -ne 0){$listItems+=@{text=$t; type=[int]$p.Range.ListFormat.ListType}}
}
$links=@(); foreach($link in $doc.Hyperlinks){$links+=@{text=$link.TextToDisplay;url=$link.Address}}
$result+=@{file=$name; headings=$headings;listItems=$listItems;links=$links;text=$doc.Content.Text}
}finally{if($doc){$doc.Close(0)}}
}
@{ok=$true;results=$result} | ConvertTo-Json -Depth 8 | Set-Content "$dir\result.json" -Encoding UTF8
}catch{ @{ok=$false;error=$_.Exception.ToString();results=$result} | ConvertTo-Json -Depth 8 | Set-Content "$dir\result.json" -Encoding UTF8; exit 1
}finally{if($word){$word.Quit(); [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($word)}}
