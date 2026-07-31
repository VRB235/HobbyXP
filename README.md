## HobbyXP

Aplicativo de escritorio **WPF (.NET 8)** para gamificar hobbies personales (running, gimnasio, entretenimiento, libros/cursos) con sistema de **XP, niveles, medallas y premios**.

- **Plataforma**: Windows 10/11, `net8.0-windows10.0.19041`.
- **Arquitectura**: MVVM con inyección de dependencias (`Microsoft.Extensions.Hosting`).
- **Persistencia**: SQLite local vía **EF Core 8**.
- **UI**: tema oscuro estilo RPG, gráficos con **LiveCharts2 (SkiaSharp)**.

Para detalle profundo (entidades, servicios, migraciones, decisiones de diseño) consulte `docs/ESTADO-PROYECTO.md`.  
El `README.md` se mantiene como **vista ejecutiva y técnica resumida** del estado actual.

---

## Arquitectura técnica (resumen)

- **Capas**:
  - Views (XAML) + controles (`MainWindow`, `DashboardView`, `PhysicalActivitiesView`, etc.).
  - ViewModels por sección (`MainViewModel` + hijos con `LoadAsync()`).
  - Servicios de dominio (XP, running, gym, entretenimiento, libros/cursos, logros, premios, dashboard).
  - Capa de datos (`HobbyXpDbContext`, configuraciones EF, migraciones, seeder, inicializador).
  - Modelos de dominio (perfil, actividades, logros, recompensas, enums).
- **Patrones clave**:
  - Host genérico en `App.xaml.cs` con registros `AddHobbyXpData()`, `AddHobbyXpServices()`, `AddHobbyXpViewModels()`.
  - Scope por ventana: `MainWindow` crea `IServiceScope` y resuelve `MainViewModel`.
  - Navegación lazy por secciones a través de un `NavigationService`.
  - Mensajería para logros y subida de nivel (`IAchievementMessenger`, `ILevelUpMessenger`).

---

## Funcionalidades principales

- **Perfil de jugador**:
  - Nivel, XP total y progreso visual con control `XpProgressBar`.
  - Nombre de aventurero editable.
  - Avatar configurable a partir de imagen local.
- **Gamificación**:
  - Registro de actividades por dominio (running, gym, puzzles, media, videojuegos, libros, cursos).
  - Otorgamiento y deducción de XP mediante `XpService` y registro de transacciones/hitos.
  - Motor de medallas `AchievementEngineService` basado en reglas (`AchievementActionType`).
  - Sistema de recompensas (`Reward`) canjeables por XP.
- **Interfaz**:
  - Sidebar con perfil, navegación lateral y estado de XP/nivel.
  - Dashboard con gráficos de XP (semanal y por hobby), sugerencias de actividades para subir de nivel y lista de hitos recientes.
  - Overlay de celebración al subir de nivel (`LevelUpOverlay`).
  - Tablas de historial (running, gimnasio, media, libros, cursos, logros) con **ordenación por columna** (clic en cabecera Asc/Desc; helper `GridViewSortHelper`).
  - Historiales de actividad física con **altura mínima** para ~10 filas visibles y scroll de página si no caben formularios + tablas.
  - En Debug, título de ventana `HobbyXP [DEV]` para distinguir el entorno de desarrollo.

Para un listado completo de entidades, servicios y controles reutilizables, ver secciones 5–8 de `docs/ESTADO-PROYECTO.md`.

---

## Flujos del aplicativo (alto nivel)

- **Registrar actividad y ganar XP**:
  1. Navegar a la sección (p. ej. Físico → Running, Entretenimiento, Crecimiento, etc.).
  2. Crear/editar la actividad correspondiente (sesión de running, workout, libro leído, curso completado, etc.).
  3. El servicio de dominio invoca `XpService`, actualiza perfil y puede generar `Milestone` y medallas.
  4. Se notifica al usuario mediante barra de mensajes y se actualiza dashboard/XP.
- **Subir de nivel**:
  1. Al cruzar el umbral de XP configurado (`BaseXpPerLevel`), `XpService` emite un evento.
  2. `MainViewModel` muestra `LevelUpOverlay` con nuevo nivel y resumen de progreso.
  3. Al cerrar el overlay, se refresca el estado visual (sidebar y dashboard).
- **Personalizar perfil**:
  1. Cambiar avatar desde el sidebar usando un cuadro de diálogo de archivo.
  2. Editar nombre de aventurero y guardar cambios.
  3. Las modificaciones se persisten en SQLite y se reflejan en la UI tras recarga.

Estos flujos están descritos con mayor detalle y con pasos para pruebas manuales en la sección 12 de `docs/ESTADO-PROYECTO.md`.

---

## Requisitos de entorno

- **Sistema operativo**: Windows 10/11.
- **SDK**: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (mínimo **8.0.404**; el repo fija la banda 8.0 vía `global.json`).
  - Puede coexistir con SDK 9/10 instalados; `dotnet` en la raíz del repo usará 8.0.x.
  - Verificar: `dotnet --version` desde la raíz (debe mostrar `8.0.x`).

Base de datos SQLite (ambientes separados):

| Entorno | Carpeta de datos |
|---------|------------------|
| **Producción** (exe publicado / Release) | `%LocalAppData%\HobbyXP\` |
| **Desarrollo** (`dotnet run` / Debug) | `%LocalAppData%\HobbyXP-Dev\` |

Override opcional: variable de entorno `HOBBYXP_DATA_DIR` (nombre bajo LocalAppData o ruta absoluta).

`dotnet run` / F5 **no escribe** sobre la BD del ejecutable de producción. Para sembrar Dev con una copia de Prod (sin modificar Prod):

```powershell
Copy-Item "$env:LOCALAPPDATA\HobbyXP\*" "$env:LOCALAPPDATA\HobbyXP-Dev\" -Recurse -Force
```

---

## Mejoras recientes (desarrollar en develop)

| Área | Qué cambió |
|------|------------|
| **Ambientes** | Datos Debug en `HobbyXP-Dev`; Release/prod en `HobbyXP`. Override `HOBBYXP_DATA_DIR`. Título `[DEV]` en Debug. |
| **Tablas físico** | Historiales de running/gimnasio con más alto útil (~10 filas) y scroll de página. |
| **Ordenación** | Clic en cabeceras de historiales (y catálogos de logros) para ordenar Asc/Desc (`GridViewSortHelper`). |

Las siguientes mejoras de producto/UX deben reflejarse aquí en cuanto se implementen (ver sección de mantenimiento).

---

## Cómo ejecutar en desarrollo

Desde la raíz del repositorio:

```powershell
cd src\HobbyXP
dotnet run
```

Comandos útiles adicionales (`dotnet` CLI, EF Core y empaquetado) están en `docs/ESTADO-PROYECTO.md` (secciones 9 y 17) y `docs/DISTRIBUCION.md`.

---

## Distribución

Para generar un ZIP portable autocontenido:

```powershell
.\scripts\package-portable.ps1
```

MSIX e instalador Inno Setup: ver [`docs/DISTRIBUCION.md`](docs/DISTRIBUCION.md).

---

## Mantenimiento de la documentación

- **`README.md`**: debe permanecer **actualizado** con:
  - Resumen de arquitectura.
  - Lista de funcionalidades disponibles.
  - Flujos funcionales principales para entender el producto.
  - Requisitos y forma de ejecución.
  - **Tabla «Mejoras recientes»**: cada mejora de UX, ambientes, tablas o comportamiento visible al usuario.
- **`docs/ESTADO-PROYECTO.md`**: fuente de verdad detallada (modelo de dominio, servicios, migraciones, decisiones de diseño, checklist de pruebas, pendientes).

Al introducir nuevas pantallas, servicios, flujos o cambios de arquitectura **o UX**, actualizar **ambos** archivos:
- Añadir/ajustar el resumen (y la fila en «Mejoras recientes») en este `README.md`.
- Registrar el detalle técnico y de pruebas en `docs/ESTADO-PROYECTO.md`.
