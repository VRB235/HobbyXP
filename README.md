## HobbyXP

Aplicativo de escritorio **WPF (.NET 8)** para gamificar hobbies personales (running, gimnasio, **dieta**, entretenimiento, libros/cursos) con sistema de **XP, niveles, medallas, premios y disciplina semanal**.

**Versión actual: 1.3.0** (producción: rama `main`, tag [`v1.3.0`](https://github.com/VRB235/HobbyXP/releases/tag/v1.3.0)).

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
  - Servicios de dominio (XP, running, gym, **dieta**, entretenimiento, libros/cursos, logros, premios, dashboard, **cuota semanal**).
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
  - Registro de actividades por dominio (running, gym, **dieta**, puzzles, media/series, videojuegos, libros, cursos).
  - **Pools de XP por hobby** + nivel global meta (`HobbyLevelUp`); títulos alegóricos por nivel (`HobbyLevelTitles`).
  - **Saldo canjeable** (`SpendableXp`) independiente del XP de progresión; tienda de premios con **inventario**, **equipar reliquia** y costo **base × nivel**.
  - **Logros visibles**: siguiente medalla en cada hobby, widget en Dashboard, overlay al desbloquear, badge en sidebar. Cada medalla otorga saldo, título de honor e **inmunidad de disciplina 7 días**.
  - **Disciplina semanal** (lun–dom): cuota por hobby; incumplimiento baja un nivel del hobby; actividad atrasada puede restaurar el castigo. Libros: ≥20% de páginas del libro actual (o terminarlo). Cursos: 5 sesiones. Series: 1 serie terminada si hay serie en progreso, más 2 películas.
  - Gimnasio: ejercicios con **grupo muscular** opcional (Pecho, Tríceps, Bíceps, Hombros, Core, Espalda, Cuádriceps, Gemelos, Glúteos, Abductores, Aductores, Isquiotibiales); catálogo agrupado, filtro al armar el entrenamiento, **preservar ejercicios al filtrar**, carga de referencia del último entreno y asignación a ejercicios legacy.
  - **Dieta**: adherencia por 4 comidas (En plan / Fuera de plan); día bueno ≥ 3/4; cuota 5 días buenos/semana.
  - Otorgamiento y deducción de XP mediante `XpService` y registro de transacciones/hitos.
  - Motor de medallas `AchievementEngineService` basado en reglas (`AchievementActionType`).
  - Sistema de recompensas (`Reward`) canjeables por XP (inventario y reliquia equipable).
- **Interfaz**:
  - Sidebar con perfil, navegación lateral y estado de XP/nivel.
  - Dashboard con gráficos de XP, **hub de logros/premios**, cuotas semanales y hitos recientes.
  - Overlay de celebración al subir de nivel (`LevelUpOverlay`) y al desbloquear medalla (`MedalUnlockOverlay`).
  - Vitrina de medallas por hobby, con secciones colapsables.
  - Tablas de historial (running, gimnasio, **dieta**, media, libros, cursos, logros) con **ordenación por columna** (clic en cabecera Asc/Desc; helper `GridViewSortHelper`).
  - Historiales de actividad física con **altura mínima** para ~10 filas visibles y scroll de página si no caben formularios + tablas.
  - Fechas atrasables al registrar (running, gym, dieta, lecturas, etc.) para disciplina semanal.
  - En Debug, título de ventana `HobbyXP [DEV]` para distinguir el entorno de desarrollo.
  - Configuración: **Restablecer progreso** (borra historial/XP/niveles; conserva catálogo de ejercicios y personalización del perfil).

Para un listado completo de entidades, servicios y controles reutilizables, ver secciones 5–8 de `docs/ESTADO-PROYECTO.md`.

---

## Flujos del aplicativo (alto nivel)

- **Registrar actividad y ganar XP**:
  1. Navegar a la sección (p. ej. Físico → Running, Entretenimiento, Crecimiento, etc.).
  2. Crear/editar la actividad correspondiente (sesión de running, workout, libro leído, curso completado, etc.).
  3. El servicio de dominio invoca `XpService`, que acredita el **pool del hobby** (barra del módulo) y puede generar `Milestone` y medallas.
  4. Se notifica al usuario mediante barra de mensajes y se actualiza dashboard/XP.
- **Subir de nivel (hobby → global)**:
  1. Al cruzar el umbral geométrico del hobby (`BaseXpPerLevel × (2^(N−1) − 1)`), el hobby sube de nivel.
  2. El global recibe un bonus meta (`BaseXpPerLevel` por cada nivel de hobby ganado). Si el global también sube, `XpService` emite level-up.
  3. `MainViewModel` muestra `LevelUpOverlay` para el nivel **global**; el avance de hobby aparece en la barra de logros.
  4. Al cerrar el overlay, se refresca sidebar y dashboard.
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

## Mejoras en `develop` respecto a `main` (incluidas en 1.3.0)

| Área | Qué cambió |
|------|------------|
| **Disciplina semanal** | Libros: ≥20 % de páginas del libro actual (terminarlo también cumple). Cursos: 5 sesiones. Series: hay que **terminar** al menos una si hay serie en progreso (+ 2 películas). Sin libro/curso/serie activa la cuota queda **No aplica**. Medalla nueva otorga inmunidad 7 días (`WeeklyQuotaStatus.Waived`). |
| **Logros** | Siguiente medalla en cada hobby; hub en Dashboard; overlay y badge en sidebar. Desbloquear medalla otorga saldo `max(50, umbral×10)`, título de honor e inmunidad. |
| **Premios** | Tienda con Disponibles + Inventario; equipar reliquia; costo efectivo = base × nivel global. |
| **Vitrina** | Secciones por hobby, desbloqueadas primero; cada hobby es un menú colapsable (abiertas las que tienen progreso). |
| **Versión** | 1.2.1 → **1.3.0** (csproj, Inno Setup, manifiesto MSIX). |

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

Salida: `artifacts\HobbyXP-win-x64-Release.zip`. Release GitHub: [`v1.3.0`](https://github.com/VRB235/HobbyXP/releases/tag/v1.3.0).

MSIX e instalador Inno Setup: ver [`docs/DISTRIBUCION.md`](docs/DISTRIBUCION.md).

### Despliegue a producción (obligatorio)

1. Publicar Release (`package-portable.ps1`).
2. Commit en la rama de trabajo (`develop`).
3. Actualizar este README con **todo** lo que esa rama tiene y `main` aún no.
4. Merge a `main` y push de ambas ramas.
5. Tag anotado `vX.Y.Z` y GitHub Release con el ZIP.

---

## Mantenimiento de la documentación

- **`README.md`**: debe permanecer **actualizado** con:
  - Resumen de arquitectura.
  - Lista de funcionalidades disponibles.
  - Flujos funcionales principales para entender el producto.
  - Requisitos y forma de ejecución.
  - **Tabla «Mejoras en develop respecto a main»**: cada mejora de UX, ambientes, tablas o comportamiento visible al usuario que aún no esté en `main`.
- En cada **despliegue a producción**: esa tabla debe cubrir el delta completo de la rama a fusionar, merge a `main`, y el flujo de la sección Distribución.
- **`docs/ESTADO-PROYECTO.md`**: fuente de verdad detallada (modelo de dominio, servicios, migraciones, decisiones de diseño, checklist de pruebas, pendientes).

Al introducir nuevas pantallas, servicios, flujos o cambios de arquitectura **o UX**, actualizar **ambos** archivos:
- Añadir/ajustar el resumen (y la fila en «Mejoras recientes») en este `README.md`.
- Registrar el detalle técnico y de pruebas en `docs/ESTADO-PROYECTO.md`.
