param(
    [string]$KspRoot = 'C:\Kerbal Space Program\1.12.5',
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'

$version = '1.0.2'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'KERRendezvousTools.sln'
$sourceMod = Join-Path $repoRoot 'GameData\KERRendezvousTools'
$kspProperty = "-p:KSPBT_GameRoot=$KspRoot"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'dist'
}

& (Join-Path $PSScriptRoot 'Test-Environment.ps1') `
    -KspRoot $KspRoot

New-Item `
    -ItemType Directory `
    -Path $OutputDirectory `
    -Force |
    Out-Null

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
    'Plugins\KERRendezvousTools.dll'

if (-not (Test-Path -LiteralPath $dll)) {
    throw "Expected release DLL was not found at: $dll"
}

$stagingRoot = Join-Path `
    $OutputDirectory `
    "KERRendezvousTools-v$version"

$stagingMod = Join-Path `
    $stagingRoot `
    'GameData\KERRendezvousTools'

$zipPath = Join-Path `
    $OutputDirectory `
    "KERRendezvousTools-v$version.zip"

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
    -Destination (Join-Path $stagingMod 'Plugins\KERRendezvousTools.dll')

Copy-Item `
    -LiteralPath (Join-Path $sourceMod 'KERRendezvousTools.version') `
    -Destination $stagingMod

foreach ($name in @(
    'README.md',
    'CHANGELOG.md',
    'LICENSE',
    'NOTICE.md',
    'MIGRATION.md'
)) {
    Copy-Item `
        -LiteralPath (Join-Path $repoRoot $name) `
        -Destination (Join-Path $stagingMod $name)
}

Compress-Archive `
    -Path (Join-Path $stagingRoot 'GameData') `
    -DestinationPath $zipPath `
    -CompressionLevel Optimal

$hash =
    (Get-FileHash `
        -LiteralPath $zipPath `
        -Algorithm SHA256).Hash.ToLowerInvariant()

$checksumPath = "$zipPath.sha256"
"$hash  $([IO.Path]::GetFileName($zipPath))" |
    Set-Content `
        -LiteralPath $checksumPath `
        -Encoding ASCII

Copy-Item `
    -LiteralPath (Join-Path $repoRoot 'publishing\GITHUB-RELEASE-NOTES-v1.0.2.md') `
    -Destination $OutputDirectory `
    -Force

Copy-Item `
    -LiteralPath (Join-Path $repoRoot 'publishing\SPACEDOCK-DESCRIPTION.md') `
    -Destination $OutputDirectory `
    -Force

Write-Host ""
Write-Host "Release package created:"
Write-Host $zipPath
Write-Host "SHA-256:"
Write-Host $hash
Write-Host ""
Write-Host "Upload the same binary ZIP to GitHub Releases and SpaceDock."
