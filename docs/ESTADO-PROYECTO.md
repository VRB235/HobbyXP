# HobbyXP — Documentación de estado del proyecto

> **Última actualización:** 7 de julio de 2026  
> **Propósito de este documento:** punto de partida para retomar el desarrollo. Resume qué se construyó, cómo está organizado, qué funciona hoy y qué queda pendiente.

---

## 1. Visión general

**HobbyXP** es una aplicación de escritorio **WPF (.NET 8)** para gamificar hobbies personales: running, gimnasio, entretenimiento, libros/cursos y un sistema de logros/premios.

| Aspecto | Decisión |
|---------|----------|
| Plataforma | Windows, WPF (`net8.0-windows10.0.19041`) |
| Arquitectura | MVVM + inyección de dependencias |
| Persistencia | SQLite local con EF Core 8 |
| Autenticación / nube | No (app 100 % local) |
| Estilo visual | Tema oscuro RPG (mockups de referencia) |
| Gráficos | LiveCharts2 (SkiaSharp) en el dashboard |

**Solución:** `HobbyXP.slnx` → proyecto `src/HobbyXP/HobbyXP.csproj`

**Base de datos:** `%LocalAppData%\HobbyXP\hobbyxp.db`  
En Windows: `C:\Users\<usuario>\AppData\Local\HobbyXP\hobbyxp.db`

---

## 2. Arquitectura en capas

```
┌─────────────────────────────────────────────────────────┐
│  Views (XAML) + Controls                                │
│  MainWindow, DashboardView, PhysicalActivitiesView…     │
├─────────────────────────────────────────────────────────┤
│  ViewModels (MVVM)                                      │
│  MainViewModel, *ViewModel por sección, Commands        │
├─────────────────────────────────────────────────────────┤
│  Services (lógica de negocio)                           │
│  XpService, RunningService, AchievementEngine…            │
├─────────────────────────────────────────────────────────┤
│  Data (EF Core)                                         │
│  HobbyXpDbContext, migraciones, seeder, inicializador   │
├─────────────────────────────────────────────────────────┤
│  Models (entidades de dominio)                          │
└─────────────────────────────────────────────────────────┘
```

### Patrones clave

- **Host genérico** (`Microsoft.Extensions.Hosting`) en `App.xaml.cs` con tres extensiones DI.
- **Scope por ventana:** `MainWindow` crea un `IServiceScope`; los ViewModels y servicios scoped viven por sesión.
- **Navegación lazy:** `NavigationService` resuelve el ViewModel de cada sección y llama `LoadAsync()` al entrar.
- **Logros reactivos:** los servicios devuelven `OperationResult<T>` con `AchievementEvent`; los ViewModels hijos publican vía `IAchievementMessenger`; `MainViewModel` muestra el mensaje en la barra superior.
- **Subida de nivel:** `XpService` detecta level-up → `ILevelUpMessenger` → overlay `LevelUpOverlay` en `MainWindow`.

---

## 3. Trabajo completado (por fases)

### Fase 1 — Capa de datos ✅

- **18 entidades** en `Models/` agrupadas por dominio: Core, Physical, Entertainment, PersonalGrowth, Achievements, Enums.
- **`HobbyXpDbContext`** con configuraciones EF en `Data/Configurations/`.
- **Migración inicial** `20260707222215_InitialCreate`: esquema completo + seed de 3 medallas y 11 reglas de XP.
- **Seeder** `HobbyXpDbSeeder`: medallas (GoldRace, PlatinumGame, ProgressiveOverload) y reglas por `AchievementActionType`.
- **Inicializador** `HobbyXpDatabaseInitializer`: crea `PlayerProfile` si no existe (`Nivel 1`, `0 XP`, `BaseXpPerLevel = 1000`).
- **Factory design-time** para `dotnet ef migrations`.

### Fase 2 — Capa de servicios ✅

- **14 interfaces** en `Services/Abstractions/`.
- **Implementaciones** por dominio: XP, dashboard, running, gym, puzzles, media, videojuegos, libros, cursos, medallas, premios, perfil, diálogo de archivos.
- **`XpService`** como motor central: calcula puntos, otorga XP, recalcula nivel, registra transacciones/hitos, publica level-up.
- **`AchievementEngineService`**: evalúa y otorga medallas según reglas.
- **`OperationResult<T>`** y DTOs en `Services/Results/`.
- **`XpLevelCalculator`** (internal): fórmula de nivel lineal por `BaseXpPerLevel`.
- **`ServiceCollectionExtensions.AddHobbyXpServices()`**.

### Fase 3 — ViewModels y navegación ✅

- Infraestructura: `ViewModelBase`, `BusyViewModelBase`, `LoadableViewModelBase`, `RelayCommand`, `AsyncRelayCommand`.
- **6 secciones** del sidebar: Dashboard, Actividades Físicas, Entretenimiento, Crecimiento Personal, Logros y Premios, Configuración.
- ViewModels anidados con **tabs** (p. ej. Running + Gym dentro de Físico).
- **`AchievementAwareViewModel`** para propagar eventos de logro tras operaciones.
- **`MainViewModel`**: orquestación, perfil en sidebar, navegación, overlay de level-up.
- **`ViewModelServiceCollectionExtensions.AddHobbyXpViewModels()`**.

### Fase 4 — Views y tema oscuro RPG ✅

- **`Themes/DarkRpgTheme.xaml`**: paleta, gradientes, tarjetas, botones, tabs, inputs.
- **Views** por sección enlazadas vía `DataTemplate` en `App.xaml`.
- **`MainWindow`**: sidebar con perfil, navegación, área principal con fondo geométrico.
- **`DashboardView`**: hero de nivel/XP, gráficos LiveCharts (XP semanal + distribución por hobby), sugerencias de actividades para subir de nivel, hitos recientes.
- Paquete **LiveChartsCore.SkiaSharpView.WPF** `2.0.4` (GA estable).

### Fase 5 — Personalización y refinamientos UI ✅

| Funcionalidad | Implementación |
|---------------|----------------|
| **Avatar personalizable** | `PlayerProfile.AvatarPath`, `IPlayerProfileService.UpdateAvatarPathAsync`, `IFileDialogService`, picker en sidebar |
| **Nombre de aventurero** | `PlayerProfile.DisplayName` (default `"Aventurero"`), edición en sidebar + botón guardar |
| **Tipografía RPG** | Orbitron (display) + Rajdhani (cuerpo) en `Assets/Fonts/`, embebidas en `.csproj` |
| **Fondo geométrico** | `Views/Controls/GeometricBackground.xaml` detrás del contenido principal |
| **Celebración level-up** | `ILevelUpMessenger` + `LevelUpOverlay` (animación, partículas, botón continuar) |
| **Barra XP segura** | `Views/Controls/XpProgressBar` — control custom sin `ProgressBar`/`RangeBase` (evita crash WPF) |

**Migración asociada:** `20260708003429_AddPlayerProfileCustomization` (`DisplayName`, `AvatarPath`).

### Fase 6 — Correcciones de build/runtime ✅

| Problema | Solución |
|----------|----------|
| `App.g.cs` no encontrado (Visual Studio) | Simplificación de `HobbyXP.csproj` (quitar `ProduceReferenceAssembly=false` y `ApplicationDefinition Update` manuales); `IncludePackageReferencesDuringMarkupCompilation=true`; limpiar `obj/`/`bin/` |
| `XamlParseException` en `RangeBase.Value` (línea 61 `MainWindow`) | Reemplazo de `ProgressBar` con plantilla custom por `XpProgressBar`; `RpgProgressBar` vuelve a plantilla WPF estándar; converters defensivos (`ProgressValueClampConverter`, etc.) |

**Estado al cierre de sesión:** la aplicación **arranca y funciona** según validación del usuario.

---

## 4. Estructura de carpetas (referencia rápida)

```
src/HobbyXP/
├── App.xaml(.cs)              # Host, DI, DataTemplates globales
├── MainWindow.xaml(.cs)       # Shell + scope DI
├── Assets/Fonts/              # Orbitron, Rajdhani
├── Converters/                # Visibility, progress, iconos de hitos
├── Data/                      # DbContext, migraciones, seeder, DI data
├── Helpers/                   # AvatarImageLoader
├── Models/                    # Entidades y enums
├── Services/                  # Lógica de negocio + Results + Messaging
├── Themes/DarkRpgTheme.xaml
├── ViewModels/                # MVVM por sección + Common + Navigation
└── Views/                     # UserControls + Controls (XpProgressBar, etc.)
```

---

## 5. Modelo de dominio (resumen)

### Perfil y progresión

- **`PlayerProfile`**: `CurrentLevel`, `TotalXp`, `BaseXpPerLevel`, `DisplayName`, `AvatarPath`.
- **`XpTransaction`**: historial de movimientos de XP.
- **`Milestone`**: hitos narrativos mostrados en dashboard.

### Físico

- **Running:** `RunningSession`, `OfficialRace` (carreras con bonus XP al completar).
- **Gym:** `GymWorkout`, `GymWorkoutEntry`, `Exercise` (incluye detección de sobrecarga progresiva → medalla).

### Entretenimiento

- `Puzzle`, `MediaEntry` (serie/película), `VideoGame` (% completitud y platino).

### Crecimiento personal

- `Book` (páginas leídas / completado), `Course`.

### Logros

- `MedalDefinition`, `EarnedMedal`, `AchievementRule`, `Reward` (tienda de canje por XP).

### Enum importante: `AchievementActionType`

`RunningKilometer`, `GymWorkoutSaved`, `ProgressiveOverload`, `OfficialRaceCompleted`, `PuzzleCompleted`, `MediaCompleted`, `VideoGamePercent`, `VideoGamePlatinum`, `BookPageRead`, `BookCompleted`, `CourseCompleted`, `RewardRedeemed`.

---

## 6. Servicios y responsabilidades

| Servicio | Rol principal |
|----------|---------------|
| `IXpService` | Otorgar/deducir XP, progreso de nivel, XP diario para gráficos |
| `IPlayerProfileService` | Perfil, progreso, nombre, avatar, XP base por nivel |
| `IDatabaseMaintenanceService` | Exportar BD, restablecer datos de la aplicación |
| `IDashboardService` | Resumen agregado para dashboard |
| `IRunningService` | Sesiones y carreras oficiales |
| `IGymService` | Workouts, ejercicios, PR |
| `IPuzzleService` | Rompecabezas completados |
| `IMediaService` | Series/películas |
| `IVideoGameService` | Videojuegos en progreso/platino |
| `IBookService` | Libros y páginas |
| `ICourseService` | Cursos completados |
| `IAchievementEngineService` | Motor de medallas y reglas |
| `IMedalService` | Vitrina de medallas |
| `IRewardService` | Premios y canje |
| `IFileDialogService` | Selector de imagen para avatar |

### Fórmula de nivel (actual)

```
XP umbral nivel N = (N - 1) * BaseXpPerLevel
Progreso % = XP dentro del nivel actual / BaseXpPerLevel * 100
```

`XpLevelCalculator` recalcula nivel al subir/bajar XP y acota el porcentaje 0–100.

---

## 7. UI — pantallas y controles

### MainWindow (shell)

- Sidebar: branding, tarjeta de perfil (avatar, nombre editable, nivel, XP, barra `XpProgressBar`), botones avatar/nombre, navegación con indicador verde activo.
- Área principal: `GeometricBackground`, barra de último logro, `ContentControl` con ViewModel actual.
- Overlay global: `LevelUpOverlay` (`Panel.ZIndex=1000`).

### Secciones

| Sección | View | Tabs / contenido |
|---------|------|------------------|
| Dashboard | `DashboardView` | Hero XP, gráficos, sugerencias para subir de nivel, hitos |
| Físico | `PhysicalActivitiesView` | Running (sesiones + **alta de carreras oficiales** + completar carrera), Gimnasio |
| Entretenimiento | `EntertainmentView` | Rompecabezas, Media, Videojuegos |
| Crecimiento | `PersonalGrowthView` | Libros, Cursos |
| Logros | `AchievementsView` | Vitrina, Reglas, Tienda premios |
| Configuración | `SettingsView` | XP base por nivel, exportar BD, restablecer datos |

### Controles reutilizables

| Control | Uso |
|---------|-----|
| `XpProgressBar` | Barras de XP 0–100 (sidebar + dashboard hero) |
| `GeometricBackground` | Patrón de cuadrícula sutil |
| `LevelUpOverlay` | Modal de celebración al subir de nivel |

### Tema

- Recurso principal: `Themes/DarkRpgTheme.xaml` (merge en `App.xaml`).
- Fuentes: `Font.Display` (Orbitron), `Font.Body` / `Font.BodySemiBold` (Rajdhani).
- Estilos: `DarkCard`, `HeroCard`, `PrimaryButton`, `AccentButton`, `NavButton`, `DisplayHeroText`, etc.

---

## 8. Registro DI y arranque

```csharp
// App.xaml.cs
services.AddHobbyXpData();      // DbContext + factory SQLite
services.AddHobbyXpServices();  // Servicios de dominio
services.AddHobbyXpViewModels(); // ViewModels + navegación + mensajería

await _host.Services.EnsureHobbyXpDatabaseAsync(); // Migrate + perfil inicial
```

```csharp
// MainWindow.xaml.cs
_scope = serviceProvider.CreateScope();
DataContext = _scope.GetRequiredService<MainViewModel>();
// Loaded → mainViewModel.InitializeAsync()
```

### Lifetimes relevantes

| Tipo | Lifetime |
|------|----------|
| `IDbContextFactory<>`, servicios dominio, ViewModels | Scoped (por ventana) |
| `IFileDialogService`, `ILevelUpMessenger`, `IAchievementMessenger` | Singleton |

---

## 9. Migraciones EF Core

| Migración | Descripción |
|-----------|-------------|
| `20260707222215_InitialCreate` | Esquema completo + seed medallas/reglas |
| `20260708003429_AddPlayerProfileCustomization` | `DisplayName`, `AvatarPath` en `PlayerProfiles` |

**Comandos útiles** (desde `src/HobbyXP`):

```powershell
dotnet ef migrations add NombreMigracion
dotnet ef database update
dotnet build
dotnet run
```

**Si Visual Studio falla con `App.g.cs`:**

```powershell
Remove-Item -Recurse -Force obj, bin
dotnet build
```

---

## 10. Converters y helpers

| Archivo | Converters |
|---------|------------|
| `MilestoneSourceToIconConverter.cs` | Emoji por tipo de hito |
| `ValueConverters.cs` | Bool/Null visibility, progress clamp, ancho de barra, min int, etc. |

| Helper | Función |
|--------|---------|
| `AvatarImageLoader` | Carga imagen local o fallback si no hay avatar |

---

## 11. Paquetes NuGet

| Paquete | Versión | Notas |
|---------|---------|-------|
| `Microsoft.EntityFrameworkCore.Sqlite` | 8.0.11 | |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.11 | Solo design-time |
| `Microsoft.Extensions.Hosting` | 8.0.1 | |
| `Microsoft.Extensions.DependencyInjection` | 8.0.1 | |
| `LiveChartsCore.SkiaSharpView.WPF` | 2.0.4 | Dashboard: línea XP semanal + pie hobbies |

---

## 12. Flujos funcionales (para pruebas)

### Registrar actividad y ganar XP

1. Ir a una sección (p. ej. Físico → Running).
2. Registrar sesión / workout / libro / etc.
3. El servicio llama `XpService` → actualiza perfil → puede crear `Milestone`.
4. `AchievementAwareViewModel` publica evento → barra superior muestra mensaje.
5. Si hay medalla nueva, se indica en el mensaje.
6. Sidebar y dashboard refrescan XP/nivel.

### Subir de nivel

1. Acumular XP suficiente para cruzar umbral.
2. `XpService` publica en `ILevelUpMessenger`.
3. `MainViewModel` muestra `LevelUpOverlay` con nivel y XP total.
4. Usuario pulsa «Continuar la aventura».

### Personalizar perfil

1. Sidebar → **📷 Avatar** → elegir imagen local.
2. Editar nombre → **💾 Nombre**.
3. Cambios persisten en SQLite; dashboard se refresca vía `RefreshDashboardAsync`.

---

## 13. Git y repositorio

- **Estado:** proyecto nuevo; la mayoría de archivos están **sin commitear** (solo `.gitignore` modificado en tracking inicial).
- **`README.md`:** prácticamente vacío (solo título).
- **Ignorar:** `bin/`, `obj/`, `.vs/`, `*.db` ya están en `.gitignore`.
- **Pendiente:** primer commit estructurado, posible `.gitlab-ci.yml`, issue/MR en GitLab.

---

## 14. Pendiente para próxima sesión (priorizado)

### Alta prioridad — estabilización

- [x] **Primer commit** con estructura limpia (excluir `bin/`, `obj/`, `.vs/`).
- [x] **README.md** básico: requisitos (.NET 8 SDK, Windows), cómo compilar/ejecutar, ruta de BD.
- [x] **Prueba funcional completa** por sección (checklist abajo) y anotar bugs.
- [x] Revisar que **todas las barras de progreso** (libros, videojuegos con `ProgressBar` estándar) no fallen con datos extremos.

### Media prioridad — UX y pulido

- [x] Aplicar **`Font.Display`** de forma consistente en títulos de todas las secciones (hoy está fuerte en dashboard/sidebar).
- [x] **Validaciones en formularios** (campos vacíos, números negativos, páginas > total, etc.) con mensajes claros en UI.
- [x] Pulir **animación level-up** (opcional: sonido, más partículas/confeti).
- [x] **Guardar nombre** al pulsar Enter en el `TextBox` del sidebar (hoy solo botón).
- [x] Refrescar `SaveDisplayNameCommand.CanExecute` cuando cambia `DisplayName`.
- [x] Avatar: validar que rutas inválidas o archivos borrados no rompan la UI (fallback).

### Media prioridad — producto

- [x] **Casos de prueba funcionales** documentados (formato para analista: pasos, datos, BD esperada) por módulo → ver [`docs/CASOS-PRUEBA-FUNCIONALES.md`](CASOS-PRUEBA-FUNCIONALES.md).
- [x] Pantalla o flujo de **configuración** (`BaseXpPerLevel`, reset de perfil, exportar BD).
- [x] Más **medallas / reglas** editables desde UI (el editor de reglas existe en ViewModel; validar UX completa).
- [x] **Iconos reales** para medallas (`IconPath`) en lugar de solo emoji.

### Baja prioridad — ingeniería

- [x] Evaluar estabilizar LiveCharts (salir de RC) o fijar versión estable → **2.0.4** GA + TFM `net8.0-windows10.0.19041` (SkiaSharp 3 nativo, sin NU1701).
- [x] Tests unitarios: `XpLevelCalculator`, `XpService` (cálculo de puntos), servicios críticos (`AchievementEngineService`, `PlayerProfileService`, `RewardService`) → `tests/HobbyXP.Tests` (42 pruebas, xUnit + SQLite in-memory).
- [ ] **GitLab CI:** `dotnet build` en pipeline.
- [ ] Empaquetado (MSIX / instalador) si se desea distribución.
- [ ] `global.json` para fijar SDK si hay discrepancia VS CLI (se observó SDK 10.0.301 con target `net8.0`).

---

## 15. Checklist de prueba manual sugerido

### Arranque
- [x] App abre sin excepciones.
- [x] BD se crea en `%LocalAppData%\HobbyXP\`.
- [x] Dashboard carga con perfil inicial (Aventurero, Nivel 1).

### Perfil
- [x] Cambiar avatar (PNG/JPG).
- [x] Cambiar nombre y verificar persistencia tras reiniciar app.

### XP y nivel
- [x] Registrar actividad que otorgue XP.
- [x] Ver mensaje en barra superior.
- [x] Forzar subida de nivel y ver overlay.
- [x] Verificar barra XP sidebar y dashboard.

### Por módulo
- [x] Running: sesión + carrera oficial completada.
- [x] Gym: workout con PR (medalla sobrecarga progresiva).
- [x] Puzzle, media, videojuego (% y platino).
- [x] Libro: páginas y completado.
- [x] Curso completado.
- [x] Logros: vitrina, editar regla, editar medalla, canjear premio (deducción XP).

---

## 16. Notas para el agente / desarrollador (continuidad)

1. **No usar `ProgressBar` con plantilla custom que bindee `TemplatedParent.Value`** para XP principal; usar `XpProgressBar`.
2. **`MainWindow` debe crear scope DI** antes de resolver `MainViewModel` (ViewModels scoped).
3. **`AchievementAwareViewModel`** debe llamar `LoadCoreAsync` en hijos, no confundir con `LoadAsync` del base.
4. **Migraciones:** tras cambiar modelos, crear migración y probar en BD limpia y existente.
5. **Convención de commits:** conventional commits + referencia a issues GitLab (`#N`) según reglas del repo.
6. **Idioma UI:** español. Código/comentarios: español en dominio, inglés aceptable en infra técnica.
7. El usuario prefiere documentación y casos de prueba **útiles para un analista funcional** con conocimiento de BD.

---

## 17. Comandos de desarrollo rápidos

```powershell
# Desde la raíz del repo
cd src\HobbyXP

# Compilar
dotnet build

# Tests unitarios (desde la raíz del repo)
cd ..\..
dotnet test tests\HobbyXP.Tests\HobbyXP.Tests.csproj

# Ejecutar
cd src\HobbyXP
dotnet run

# Limpiar artefactos WPF (si hay errores raros)
Remove-Item -Recurse -Force obj, bin
dotnet build

# Cerrar instancia bloqueada
Stop-Process -Name HobbyXP -Force -ErrorAction SilentlyContinue
```

---

## 18. Resumen ejecutivo (una línea)

**HobbyXP es un MVP funcional de escritorio** con persistencia SQLite, gamificación XP/nivel/medallas/premios, UI oscura RPG, perfil personalizable y navegación por 5 secciones; listo para estabilizar, documentar pruebas y pulir UX en la siguiente iteración.
