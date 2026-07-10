# Publica HobbyXP como carpeta portable (win-x64) y genera un ZIP listo para distribuir.
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $repoRoot "src\HobbyXP\HobbyXP.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish\$Runtime"
$zipPath = Join-Path $repoRoot "artifacts\HobbyXP-$Runtime-$Configuration.zip"

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "Publicando $project ($Configuration, $Runtime, autocontenido)..."
dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló." }

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "Portable listo:"
Write-Host "  Carpeta: $publishDir"
Write-Host "  ZIP:     $zipPath"
Write-Host ""
Write-Host "Ejecute HobbyXP.exe desde la carpeta publicada. Los datos de usuario se guardan en %LocalAppData%\HobbyXP\."
