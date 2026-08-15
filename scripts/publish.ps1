param(
    [Parameter(Mandatory = $false)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:[-.][0-9A-Za-z.-]+)?$')]
    [string]$Version = '0.1.0'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$releaseRoot = Join-Path $repoRoot 'artifacts\release'
$publishRoot = Join-Path $releaseRoot "StrataShell-$Version-win-x64"
$archivePath = "$publishRoot.zip"
$checksumPath = "$archivePath.sha256"
$releaseRootFull = [System.IO.Path]::GetFullPath($releaseRoot).TrimEnd('\') + '\'
$publishRootFull = [System.IO.Path]::GetFullPath($publishRoot)
if (-not $publishRootFull.StartsWith($releaseRootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe publish path: $publishRootFull"
}

if (Test-Path -LiteralPath $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}
if (Test-Path -LiteralPath $checksumPath) {
    Remove-Item -LiteralPath $checksumPath -Force
}

New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
dotnet publish (Join-Path $repoRoot 'src\StrataShell.App\StrataShell.App.csproj') `
    -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=false -p:Version=$Version `
    -o $publishRoot
dotnet publish (Join-Path $repoRoot 'src\StrataShell.Watchdog\StrataShell.Watchdog.csproj') `
    -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=false -p:Version=$Version `
    -o $publishRoot

foreach ($requiredFile in @('StrataShell.exe', 'StrataShell.dll', 'StrataShell.Watchdog.exe', 'StrataShell.Watchdog.dll')) {
    $requiredPath = Join-Path $publishRoot $requiredFile
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Publish output is incomplete: $requiredPath"
    }
}

Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $publishRoot
Copy-Item -LiteralPath (Join-Path $repoRoot 'THIRD_PARTY_NOTICES.md') -Destination $publishRoot
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $publishRoot

Compress-Archive -Path (Join-Path $publishRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $checksumPath -Value "$hash  $(Split-Path -Leaf $archivePath)" -Encoding ascii

[pscustomobject]@{
    Version = $Version
    Archive = $archivePath
    Sha256 = $hash
}
