param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$KspRoot = 'C:\Kerbal Space Program\1.12.5',

    [switch]$MigrateLegacy
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'KERRendezvousTools.sln'
$sourceMod = Join-Path $repoRoot 'GameData\KERRendezvousTools'
$destinationMod = Join-Path $KspRoot 'GameData\KERRendezvousTools'
$kspProperty = "-p:KSPBT_GameRoot=$KspRoot"

$legacyDlls = @(
    Join-Path $KspRoot 'GameData\PlaneMatchKER\Plugins\PlaneMatchKER.dll'
    Join-Path $KspRoot 'GameData\LaunchWindowKER\Plugins\LaunchWindowKER.dll'
) | Where-Object { Test-Path -LiteralPath $_ }

if ($legacyDlls.Count -gt 0) {
    if (-not $MigrateLegacy) {
        throw @"
Legacy standalone DLLs are installed:
$($legacyDlls -join [Environment]::NewLine)

Run again with -MigrateLegacy so the old folders are removed before deployment.
"@
    }

    & (Join-Path $PSScriptRoot 'Migrate-Legacy-Install.ps1') `
        -KspRoot $KspRoot `
        -Force
}

& (Join-Path $PSScriptRoot 'Test-Environment.ps1') `
    -KspRoot $KspRoot

Write-Host "Cleaning old build outputs..."
& dotnet clean `
    $solution `
    --configuration $Configuration `
    $kspProperty

if ($LASTEXITCODE -ne 0) {
    throw "dotnet clean failed with exit code $LASTEXITCODE"
}

Write-Host "Restoring NuGet packages..."
& dotnet restore `
    $solution `
    $kspProperty

if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE"
}

Write-Host "Building $Configuration..."
& dotnet build `
    $solution `
    --configuration $Configuration `
    --no-restore `
    $kspProperty

if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

$dll = Join-Path `
    $sourceMod `
    'Plugins\KERRendezvousTools.dll'

if (-not (Test-Path -LiteralPath $dll)) {
    throw "Expected DLL was not found at: $dll"
}

Write-Host "Deploying KER Rendezvous Tools..."
New-Item `
    -ItemType Directory `
    -Path $destinationMod `
    -Force |
    Out-Null

Copy-Item `
    -Path (Join-Path $sourceMod '*') `
    -Destination $destinationMod `
    -Recurse `
    -Force

Write-Host ""
Write-Host "Build and deployment succeeded."
Write-Host "DLL: $destinationMod\Plugins\KERRendezvousTools.dll"
Write-Host "Restart KSP before testing."
