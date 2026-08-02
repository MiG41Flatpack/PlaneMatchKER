param(
    [string]$KspRoot = 'C:\Kerbal Space Program\1.12.5'
)

$ErrorActionPreference = 'Stop'

function Assert-Path {
    param(
        [string]$Path,
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description was not found at: $Path"
    }

    Write-Host "[OK] $Description"
}

Write-Host "Checking PlaneMatchKER development environment..."
Write-Host "KSP root: $KspRoot"

Assert-Path $KspRoot 'KSP root directory'
Assert-Path (Join-Path $KspRoot 'KSP_x64.exe') 'KSP 64-bit executable'
Assert-Path `
    (Join-Path $KspRoot 'KSP_x64_Data\Managed\Assembly-CSharp.dll') `
    'KSP Assembly-CSharp.dll'

$kerDll = Join-Path `
    $KspRoot `
    'GameData\KerbalEngineer\KerbalEngineer.dll'

$kerUnityDll = Join-Path `
    $KspRoot `
    'GameData\KerbalEngineer\KerbalEngineer.Unity.dll'

Assert-Path $kerDll 'KerbalEngineer.dll'
Assert-Path $kerUnityDll 'KerbalEngineer.Unity.dll'

$kerVersion =
    [System.Diagnostics.FileVersionInfo]::GetVersionInfo($kerDll)

Write-Host "[INFO] KER file version: $($kerVersion.FileVersion)"

if ($kerVersion.FileVersion -notlike '1.1.9.5*') {
    Write-Warning `
        "This project was prepared and tested against KER 1.1.9.5."
}

try {
    $dotnetVersion = & dotnet --version
    Write-Host "[OK] dotnet SDK: $dotnetVersion"
}
catch {
    throw "The dotnet SDK was not found."
}

Write-Host "Environment check passed."
