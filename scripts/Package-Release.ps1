param(
    [string]$KspRoot = 'C:\Kerbal Space Program\1.12.5',
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'

$version = '1.0.2'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'PlaneMatchKER.sln'
$sourceMod = Join-Path $repoRoot 'GameData\PlaneMatchKER'
$kspProperty = "-p:KSPBT_GameRoot=$KspRoot"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'dist'
}

& (Join-Path $PSScriptRoot 'Test-Environment.ps1') `
    -KspRoot $KspRoot

Write-Host "Building release..."
& dotnet clean `
    $solution `
    --configuration Release `
    $kspProperty

if ($LASTEXITCODE -ne 0) {
    throw "dotnet clean failed with exit code $LASTEXITCODE"
}

& dotnet restore `
    $solution `
    $kspProperty

if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE"
}

& dotnet build `
    $solution `
    --configuration Release `
    --no-restore `
    $kspProperty

if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

$dll = Join-Path `
    $sourceMod `
    'Plugins\PlaneMatchKER.dll'

if (-not (Test-Path -LiteralPath $dll)) {
    throw "Expected release DLL was not found at: $dll"
}

$stagingRoot = Join-Path `
    $OutputDirectory `
    "PlaneMatchKER-v$version"

$stagingMod = Join-Path `
    $stagingRoot `
    'GameData\PlaneMatchKER'

$zipPath = Join-Path `
    $OutputDirectory `
    "PlaneMatchKER-v$version.zip"

if (Test-Path -LiteralPath $stagingRoot) {
    Remove-Item `
        -LiteralPath $stagingRoot `
        -Recurse `
        -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item `
        -LiteralPath $zipPath `
        -Force
}

New-Item `
    -ItemType Directory `
    -Path (Join-Path $stagingMod 'Plugins') `
    -Force |
    Out-Null

Copy-Item `
    -LiteralPath $dll `
    -Destination (Join-Path $stagingMod 'Plugins\PlaneMatchKER.dll')

Copy-Item `
    -LiteralPath (Join-Path $sourceMod 'PlaneMatchKER.version') `
    -Destination $stagingMod

Copy-Item `
    -LiteralPath (Join-Path $repoRoot 'README.md') `
    -Destination (Join-Path $stagingMod 'README.md')

Copy-Item `
    -LiteralPath (Join-Path $repoRoot 'CHANGELOG.md') `
    -Destination (Join-Path $stagingMod 'CHANGELOG.md')

Copy-Item `
    -LiteralPath (Join-Path $repoRoot 'LICENSE') `
    -Destination (Join-Path $stagingMod 'LICENSE')

Copy-Item `
    -LiteralPath (Join-Path $repoRoot 'NOTICE.md') `
    -Destination (Join-Path $stagingMod 'NOTICE.md')

Compress-Archive `
    -Path (Join-Path $stagingRoot 'GameData') `
    -DestinationPath $zipPath `
    -CompressionLevel Optimal

Write-Host ""
Write-Host "Release package created:"
Write-Host $zipPath
Write-Host ""
Write-Host "ZIP root contains GameData\PlaneMatchKER."
