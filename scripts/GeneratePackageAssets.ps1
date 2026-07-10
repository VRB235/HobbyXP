# Genera los PNG requeridos por Package.appxmanifest a partir de Assets/HobbyXP.ico
param(
    [string]$IconPath = (Join-Path $PSScriptRoot "..\src\HobbyXP\Assets\HobbyXP.ico"),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\src\HobbyXP.Package\Images")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $IconPath)) {
    throw "No se encontró el icono: $IconPath"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
Add-Type -AssemblyName System.Drawing

$icon = New-Object System.Drawing.Icon $IconPath

function Save-SquareLogo {
    param([int]$Size, [string]$FileName)
    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::FromArgb(26, 32, 48))
    $rect = New-Object System.Drawing.Rectangle 0, 0, $Size, $Size
    $graphics.DrawIcon($icon, $rect)
    $target = Join-Path $OutputDirectory $FileName
    $bitmap.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
}

Save-SquareLogo -Size 44 -FileName "Square44x44Logo.png"
Save-SquareLogo -Size 50 -FileName "StoreLogo.png"
Save-SquareLogo -Size 150 -FileName "Square150x150Logo.png"

$wide = New-Object System.Drawing.Bitmap 310, 150
$wideGraphics = [System.Drawing.Graphics]::FromImage($wide)
$wideGraphics.Clear([System.Drawing.Color]::FromArgb(26, 32, 48))
$wideRect = New-Object System.Drawing.Rectangle 80, 0, 150, 150
$wideGraphics.DrawIcon($icon, $wideRect)
$wide.Save((Join-Path $OutputDirectory "Wide310x150Logo.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$wideGraphics.Dispose()
$wide.Dispose()
$icon.Dispose()

Write-Host "Logos MSIX generados en $OutputDirectory"
