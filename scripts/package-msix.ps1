# Empaqueta HobbyXP como MSIX (sideload / distribución local).
# Requiere Visual Studio o Build Tools con carga "Desarrollo de la plataforma universal de Windows"
# o "Herramientas de empaquetado MSIX".
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$packageDir = Join-Path $repoRoot "src\HobbyXP.Package"
$wapproj = Join-Path $packageDir "HobbyXP.Package.wapproj"
$pfxPath = Join-Path $packageDir "HobbyXP.Package_TemporaryKey.pfx"
$pfxPassword = "HobbyXP-Dev"
$artifactsDir = Join-Path $repoRoot "artifacts\msix"

& (Join-Path $PSScriptRoot "GeneratePackageAssets.ps1")

$bridgeProps = Join-Path $env:MSBuildExtensionsPath "Microsoft\DesktopBridge\Microsoft.DesktopBridge.props"
if (-not (Test-Path $bridgeProps)) {
    throw @"
No se encontró Microsoft Desktop Bridge (herramientas MSIX de Visual Studio).

Instale en Visual Studio Installer una de estas cargas:
  - Herramientas de empaquetado MSIX
  - Desarrollo de la plataforma universal de Windows (UWP)

Alternativa inmediata sin Visual Studio: .\scripts\package-portable.ps1
Guía completa: docs\DISTRIBUCION.md
"@
}

if (-not (Test-Path $pfxPath)) {
    Write-Host "Generando certificado de desarrollo para firmar el MSIX..."
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject "CN=HobbyXP Development" `
        -KeyUsage DigitalSignature `
        -FriendlyName "HobbyXP MSIX Development" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

    $securePassword = ConvertTo-SecureString -String $pfxPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword | Out-Null
    Write-Host "Certificado exportado: $pfxPath"
    Write-Host "Instale el certificado en 'Entidades de certificación raíz de confianza' del equipo destino para sideload."
}

$msbuild = $null
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $installationPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($installationPath) {
        $candidate = Join-Path $installationPath "MSBuild\Current\Bin\MSBuild.exe"
        if (Test-Path $candidate) { $msbuild = $candidate }
    }
}

if (-not $msbuild) {
    $msbuild = "dotnet"
    $msbuildArgs = @(
        "msbuild", $wapproj,
        "/restore",
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/p:AppxPackageSigningEnabled=true",
        "/p:PackageCertificateKeyFile=$pfxPath",
        "/p:PackageCertificatePassword=$pfxPassword",
        "/p:SolutionDir=$repoRoot\"
    )
}
else {
    $msbuildArgs = @(
        $wapproj,
        "/restore",
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/p:AppxPackageSigningEnabled=true",
        "/p:PackageCertificateKeyFile=$pfxPath",
        "/p:PackageCertificatePassword=$pfxPassword",
        "/p:SolutionDir=$repoRoot\"
    )
}

Write-Host "Compilando paquete MSIX..."
& $msbuild @msbuildArgs
if ($LASTEXITCODE -ne 0) {
    throw "Falló la generación del MSIX. Verifique que tiene instaladas las herramientas de empaquetado de Windows (Desktop Bridge)."
}

New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null
$generated = Get-ChildItem -Path (Join-Path $packageDir "AppPackages") -Recurse -Filter "*.msix" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($generated) {
    $target = Join-Path $artifactsDir $generated.Name
    Copy-Item $generated.FullName $target -Force
    Write-Host ""
    Write-Host "MSIX listo: $target"
}
else {
    Write-Host "Compilación finalizada. Busque el .msix en src\HobbyXP.Package\AppPackages\"
}
