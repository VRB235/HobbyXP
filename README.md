## HobbyXP

Aplicativo de escritorio **WPF (.NET 8)** para gamificar hobbies personales (running, gimnasio, entretenimiento, libros/cursos) con sistema de **XP, niveles, medallas y premios**.

- **Plataforma**: Windows 10/11, `net8.0-windows`.
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
- **SDK**: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

Base de datos SQLite:

- Ruta: `%LocalAppData%\HobbyXP\hobbyxp.db`
- En Windows: `C:\Users\<usuario>\AppData\Local\HobbyXP\hobbyxp.db`

---

## Cómo ejecutar en desarrollo

Desde la raíz del repositorio:

```powershell
cd src\HobbyXP
dotnet run
```

Comandos útiles adicionales (`dotnet` CLI y EF Core) están documentados en `docs/ESTADO-PROYECTO.md` (sección 9 y 17).

---

## Mantenimiento de la documentación

- **`README.md`**: debe permanecer **actualizado** con:
  - Resumen de arquitectura.
  - Lista de funcionalidades disponibles.
  - Flujos funcionales principales para entender el producto.
  - Requisitos y forma de ejecución.
- **`docs/ESTADO-PROYECTO.md`**: fuente de verdad detallada (modelo de dominio, servicios, migraciones, decisiones de diseño, checklist de pruebas, pendientes).

Al introducir nuevas pantallas, servicios, flujos o cambios de arquitectura, actualizar **ambos** archivos:
- Añadir/ajustar el resumen en este `README.md`.
- Registrar el detalle técnico y de pruebas en `docs/ESTADO-PROYECTO.md`.
