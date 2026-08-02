param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$KspRoot = 'C:\Kerbal Space Program\1.12.5'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'PlaneMatchKER.sln'
$sourceMod = Join-Path $repoRoot 'GameData\PlaneMatchKER'
$destinationMod = Join-Path $KspRoot 'GameData\PlaneMatchKER'
$kspProperty = "-p:KSPBT_GameRoot=$KspRoot"

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
    'Plugins\PlaneMatchKER.dll'

if (-not (Test-Path -LiteralPath $dll)) {
    throw "Expected DLL was not found at: $dll"
}

Write-Host "Deploying PlaneMatchKER..."
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
Write-Host "DLL: $destinationMod\Plugins\PlaneMatchKER.dll"
Write-Host "Restart KSP before testing."
