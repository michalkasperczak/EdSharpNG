param([string]$Stage = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
Set-Location -LiteralPath $Stage
$compiler = Join-Path $env:SystemRoot 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Nie znaleziono kompilatora .NET.' }
$fixtureDirectory = Join-Path $Stage 'fixtures'
$logDirectory = Join-Path $Stage 'test-results'
[IO.Directory]::CreateDirectory($fixtureDirectory) | Out-Null
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null
$env:TEMP = $fixtureDirectory
$env:TMP = $fixtureDirectory
$tests = @('test_synonimy', 'test_synonimy_dictionary', 'test_synonimy_presentation', 'test_synonimy_odmiany')
$failures = 0
foreach ($test in $tests) {
    $exe = Join-Path $Stage ($test + '.exe')
    $compile = & $compiler /nologo ('/out:' + $exe) Synonimy.cs SynonimyOdmiany.cs ($test + '.cs') 2>&1
    $compileCode = $LASTEXITCODE
    [IO.File]::WriteAllText((Join-Path $logDirectory ($test + '-compile.log')), ($compile -join "`n"), [Text.Encoding]::UTF8)
    if ($compileCode -ne 0) { $compile; Write-Output ('BUILD_FAILED ' + $test); exit 2 }
    if ($test -eq 'test_synonimy_odmiany') { $output = & $exe $fixtureDirectory 2>&1 }
    else { $output = & $exe 2>&1 }
    $testCode = $LASTEXITCODE
    [IO.File]::WriteAllText((Join-Path $logDirectory ($test + '.log')), ($output -join "`n"), [Text.Encoding]::UTF8)
    $output
    if ($testCode -ne 0 -or -not ($output -match '^FAILURES=0$')) { $failures++ }
}
Write-Output ('TESTS=' + $tests.Count + ' FAILED=' + $failures)
if ($failures -ne 0) { exit 1 }
exit 0
