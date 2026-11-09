# Przygotowanie zasobu TYLKO podczas budowania, bez sieci i zewnetrznych modulow.
param([string]$Root = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$folder = Join-Path $Root 'Synonyms'
$output = Join-Path $folder 'plwordnet-forms.bin'
$packed = Join-Path $folder 'plwordnet-forms.bin.gz'
$manifest = Get-Content -LiteralPath (Join-Path $folder 'forms-resource.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.schema -ne 'edsharp-synonym-forms-v1' -or $manifest.binary_bytes -lt 28 -or $manifest.binary_bytes -gt 1073741824 -or $manifest.binary_sha256 -notmatch '^[a-f0-9]{64}$' -or $manifest.packed_sha256 -notmatch '^[a-f0-9]{64}$') { throw 'Niepoprawny manifest indeksu odmiany.' }
if ((Test-Path -LiteralPath $output) -and (Get-Item -LiteralPath $output).Length -eq $manifest.binary_bytes -and (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash -eq $manifest.binary_sha256) {
    Write-Output 'Indeks odmiany jest gotowy.'
    return
}
if (!(Test-Path -LiteralPath $packed) -or (Get-Item -LiteralPath $packed).Length -ne $manifest.packed_bytes -or (Get-FileHash -LiteralPath $packed -Algorithm SHA256).Hash -ne $manifest.packed_sha256) { throw 'Brak lub uszkodzenie spakowanego indeksu odmiany.' }
$temporary = $output + '.building-' + $PID
$inputStream = $null
$gzip = $null
$destination = $null
try {
    $inputStream = [IO.File]::OpenRead($packed)
    $gzip = New-Object IO.Compression.GZipStream($inputStream, [IO.Compression.CompressionMode]::Decompress)
    $destination = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
    $buffer = New-Object byte[] 65536
    [long]$total = 0
    while (($read = $gzip.Read($buffer, 0, $buffer.Length)) -gt 0) {
        $total += $read
        if ($total -gt $manifest.binary_bytes) { throw 'Indeks odmiany przekracza zadeklarowany rozmiar.' }
        $destination.Write($buffer, 0, $read)
    }
    $destination.Dispose(); $destination = $null
    $gzip.Dispose(); $gzip = $null
    $inputStream.Dispose(); $inputStream = $null
    if ($total -ne $manifest.binary_bytes -or (Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash -ne $manifest.binary_sha256) { throw 'Suma kontrolna indeksu odmiany jest niepoprawna.' }
    Move-Item -LiteralPath $temporary -Destination $output -Force
    Write-Output 'Przygotowano zweryfikowany indeks odmiany.'
}
finally {
    if ($destination) { $destination.Dispose() }
    if ($gzip) { $gzip.Dispose() }
    if ($inputStream) { $inputStream.Dispose() }
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
}
