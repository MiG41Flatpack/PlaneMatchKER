param(
    [string]$KspRoot = 'C:\Kerbal Space Program\1.12.5',
    [switch]$RemoveExperimentalFolders,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$gameData = Join-Path $KspRoot 'GameData'

$folders = @(
    'PlaneMatchKER',
    'LaunchWindowKER'
)

if ($RemoveExperimentalFolders) {
    $folders += @(
        'PlaneMatchGuidance',
        'PlaneMatchDisplay'
    )
}

$existing = @(
    foreach ($folder in $folders) {
        $path = Join-Path $gameData $folder

        if (Test-Path -LiteralPath $path) {
            $path
        }
    }
)

if ($existing.Count -eq 0) {
    Write-Host "No legacy KER Rendezvous Tools folders were found."
    exit 0
}

Write-Host "Legacy folders:"
$existing | ForEach-Object { Write-Host "  $_" }

if (-not $Force) {
    $answer = Read-Host "Delete these folders? Type YES to continue"

    if ($answer -ne 'YES') {
        throw "Migration cancelled."
    }
}

foreach ($path in $existing) {
    Remove-Item `
        -LiteralPath $path `
        -Recurse `
        -Force

    Write-Host "Removed: $path"
}

Write-Host "Legacy migration complete."
