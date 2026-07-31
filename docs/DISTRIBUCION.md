# Distribución de HobbyXP

HobbyXP ofrece **tres vías** de distribución en Windows 10/11:

| Método | Script / artefacto | Uso recomendado |
|--------|-------------------|-----------------|
| **Portable (ZIP)** | `scripts/package-portable.ps1` | Pruebas rápidas, compartir sin instalador |
| **Instalador clásico** | `installer/HobbyXP.iss` (Inno Setup 6) | Usuarios que prefieren asistente `.exe` |
| **MSIX** | `scripts/package-msix.ps1` | Sideload, actualizaciones empaquetadas, Microsoft Store (con certificado de producción) |

Los datos de usuario (SQLite, avatar, fotos) **no van dentro del paquete**.

| Binario | Carpeta de datos |
|---------|------------------|
| Publicación **Release** (portable, Inno, MSIX) | `%LocalAppData%\HobbyXP\` |
| Compilación **Debug** (`dotnet run`, F5) | `%LocalAppData%\HobbyXP-Dev\` |

Así las pruebas en desarrollo no modifican la BD del ejecutable de producción en la misma máquina.

---

## 1. Portable (ZIP)

Requisitos: .NET 8 SDK.

```powershell
# Desde la raíz del repositorio
.\scripts\package-portable.ps1
```

Salida:

- `artifacts\publish\win-x64\` — carpeta autocontenida (`HobbyXP.exe` + dependencias).
- `artifacts\HobbyXP-win-x64-Release.zip` — archivo para distribuir.

El destinatario descomprime y ejecuta `HobbyXP.exe`. No requiere instalar .NET en el equipo.

---

## 2. Instalador Inno Setup

Requisitos: [Inno Setup 6](https://jrsoftware.org/isinfo.php) y haber ejecutado antes `package-portable.ps1`.

1. `.\scripts\package-portable.ps1`
2. Abrir `installer\HobbyXP.iss` en **Inno Setup Compiler**.
3. Compilar (F9).

Salida: `artifacts\installer\HobbyXP-Setup-1.0.0.exe`

Crea acceso directo en menú Inicio y opcionalmente en el escritorio. Desinstalación estándar desde Configuración de Windows.

---

## 3. MSIX (sideload)

Requisitos adicionales:

- Visual Studio 2022+ o Build Tools con **MSIX Packaging Tools** / **Desktop Bridge**.
- Windows 10 SDK (10.0.19041+).

```powershell
.\scripts\package-msix.ps1
```

El script:

1. Regenera los logos PNG del manifiesto (`scripts/GeneratePackageAssets.ps1`).
2. Crea un certificado de desarrollo `HobbyXP.Package_TemporaryKey.pfx` si no existe (password local: `HobbyXP-Dev`).
3. Compila `src\HobbyXP.Package\HobbyXP.Package.wapproj`.
4. Copia el `.msix` a `artifacts\msix\`.

### Instalar el MSIX en otro PC (sideload)

1. Instalar el certificado de desarrollo en el equipo destino:
   - Doble clic en `src\HobbyXP.Package\HobbyXP.Package_TemporaryKey.pfx`
   - Importar en **Entidades de certificación raíz de confianza** (usuario local o equipo).
2. Habilitar *Instalar aplicaciones de cualquier origen* o sideload según política del equipo.
3. Doble clic en el `.msix` generado.

Para **Microsoft Store** o distribución empresarial, sustituya el certificado temporal por uno de una CA o del Partner Center y actualice `Publisher` en `Package.appxmanifest` para que coincida con el certificado.

### Versión del paquete

Actualice en sincronía:

- `src/HobbyXP/HobbyXP.csproj` → `<Version>`, `<AssemblyVersion>`, `<FileVersion>`
- `src/HobbyXP.Package/Package.appxmanifest` → atributo `Version` (formato `a.b.c.d`)
- `installer/HobbyXP.iss` → `#define MyAppVersion`

---

## CI (GitLab)

El job manual `package` en `.gitlab-ci.yml` genera el ZIP portable en el runner `windows`. El MSIX requiere las herramientas de empaquetado de Visual Studio en ese mismo runner.

---

## Estructura de empaquetado

```
src/HobbyXP.Package/
  HobbyXP.Package.wapproj    # Proyecto de empaquetado Windows (Desktop Bridge)
  Package.appxmanifest       # Identidad, capacidades, logos
  Images/                    # Logos para el manifiesto MSIX
scripts/
  package-portable.ps1
  package-msix.ps1
  GeneratePackageAssets.ps1
installer/
  HobbyXP.iss                # Plantilla Inno Setup
artifacts/                   # Salida local (ignorada por git)
```
