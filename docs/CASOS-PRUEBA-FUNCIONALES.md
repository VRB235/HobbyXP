# HobbyXP — Casos de prueba funcionales

> **Versión:** 1.0 — 9 de julio de 2026  
> **Audiencia:** analista funcional con conocimiento de la aplicación de escritorio y verificación en SQLite.  
> **Alcance:** flujos principales por módulo, validaciones de formulario, avatar, XP/nivel, eliminación de registros y logros.

---

## 1. Convenciones

### 1.1 Identificadores

| Prefijo | Significado |
|---------|-------------|
| `CP-GEN` | General / arranque |
| `CP-PER` | Perfil (nombre, avatar) |
| `CP-VAL` | Validaciones de formulario |
| `CP-RUN` | Running |
| `CP-GYM` | Gimnasio |
| `CP-PUZ` | Rompecabezas |
| `CP-MED` | Series y películas |
| `CP-VG` | Videojuegos |
| `CP-LIB` | Libros |
| `CP-CUR` | Cursos |
| `CP-LOG` | Logros, medallas, premios |
| `CP-XP` | XP, nivel, dashboard |
| `CP-SET` | Configuración |

### 1.2 Columnas de cada caso

| Campo | Descripción |
|-------|-------------|
| **Prioridad** | Alta / Media / Baja |
| **Precondiciones** | Estado previo de app, BD o archivos |
| **Datos de prueba** | Valores concretos a ingresar |
| **Pasos** | Secuencia en la UI |
| **Resultado esperado (UI)** | Comportamiento visible |
| **Resultado esperado (BD)** | Consultas SQLite de verificación |
| **Resultado esperado (archivos)** | Solo cuando aplique (avatar, fotos de puzzles, etc.) |

### 1.3 Entorno de prueba

| Elemento | Valor |
|----------|-------|
| SO | Windows 10/11 |
| Runtime | .NET 8 SDK |
| Ejecución | `dotnet run` desde `src\HobbyXP` o `HobbyXP.exe` en `bin\Debug\net8.0-windows10.0.19041\` |
| Base de datos | `%LocalAppData%\HobbyXP\hobbyxp.db` |
| Carpeta de datos | `%LocalAppData%\HobbyXP\` |
| Avatar gestionado | `%LocalAppData%\HobbyXP\Avatar\profile.{ext}` |

**Herramientas recomendadas para BD:** [DB Browser for SQLite](https://sqlitebrowser.org/) o `sqlite3` en consola.  
**Nota:** HobbyXP no expone API HTTP; la verificación de persistencia es directa sobre SQLite y archivos locales.

### 1.4 Consultas útiles (plantillas)

```sql
-- Perfil
SELECT Id, DisplayName, AvatarPath, CurrentLevel, TotalXp, BaseXpPerLevel, UpdatedAt
FROM PlayerProfiles;

-- Últimas transacciones de XP
SELECT Id, Amount, Description, CreatedAt
FROM XpTransactions
ORDER BY Id DESC
LIMIT 10;

-- Conteo por tabla de actividad
SELECT 'RunningSessions' AS Tabla, COUNT(*) AS Total FROM RunningSessions
UNION ALL SELECT 'OfficialRaces', COUNT(*) FROM OfficialRaces
UNION ALL SELECT 'GymWorkouts', COUNT(*) FROM GymWorkouts
UNION ALL SELECT 'Puzzles', COUNT(*) FROM Puzzles
UNION ALL SELECT 'MediaEntries', COUNT(*) FROM MediaEntries
UNION ALL SELECT 'VideoGames', COUNT(*) FROM VideoGames
UNION ALL SELECT 'Books', COUNT(*) FROM Books
UNION ALL SELECT 'Courses', COUNT(*) FROM Courses;
```

### 1.5 Reglas de XP de referencia (seed inicial)

| Acción | Puntos |
|--------|--------|
| Running por km | 10 XP / km |
| Sesión de gimnasio | 25 XP |
| Sobrecarga progresiva (PR) | +150 XP (bono) |
| Carrera oficial completada | +500 XP (bono) |
| Rompecabezas completado | 50 XP |
| Serie/película terminada | 30 XP |
| Avance videojuego | 10 XP / % |
| Videojuego platinado (100 %) | +1000 XP (bono) |
| Página leída | 1 XP / página |
| Libro terminado | +200 XP (bono) |
| Sesión de curso | 10 XP / sesión |
| Curso terminado | +100 XP (bono) |

### 1.6 Criterios de aceptación globales

- La aplicación no debe cerrarse con excepción no controlada.
- Los mensajes de validación deben mostrarse en español, en banner rojo sobre el formulario.
- Tras guardar una actividad que otorga XP, la barra superior debe mostrar un mensaje de logro/XP.
- El sidebar y el dashboard deben reflejar el `TotalXp` y nivel actualizados sin reiniciar la app.
- Al eliminar un registro con XP asociado, debe pedirse confirmación y revertirse el XP correspondiente.

---

## 2. General y arranque

### CP-GEN-001 — Primera ejecución crea BD y perfil inicial

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | No existe `%LocalAppData%\HobbyXP\` (renombrar carpeta si hace falta). |
| **Datos** | N/A |
| **Pasos** | 1. Ejecutar la aplicación.<br>2. Esperar carga del dashboard. |
| **UI** | Ventana principal visible; sidebar con nombre «Aventurero», nivel 1, XP 0; icono de avatar por defecto (⚔). |
| **BD** | `SELECT COUNT(*) FROM PlayerProfiles;` → **1** fila con `DisplayName='Aventurero'`, `CurrentLevel=1`, `TotalXp=0`, `BaseXpPerLevel=1000`. |
| **Archivos** | Carpeta `%LocalAppData%\HobbyXP\` creada con `hobbyxp.db`. |

---

### CP-GEN-002 — Navegación entre secciones

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | App abierta. |
| **Pasos** | 1. Pulsar cada ítem del sidebar: Dashboard, Actividades Físicas, Entretenimiento, Crecimiento Personal, Logros y Premios.<br>2. En secciones con pestañas, alternar entre ellas. |
| **UI** | Cada sección carga sin error; el contenido cambia según la selección. |
| **BD** | Sin cambios. |

---

## 3. Perfil

### CP-PER-001 — Guardar nombre válido con botón

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | App abierta. |
| **Datos** | Nombre: `Sir Lancelot` |
| **Pasos** | 1. En sidebar, editar el TextBox del nombre.<br>2. Pulsar **💾 Nombre**. |
| **UI** | Nombre actualizado en sidebar y dashboard; mensaje de confirmación en barra superior; botón guardar deshabilitado si no hay cambios pendientes. |
| **BD** | `SELECT DisplayName FROM PlayerProfiles;` → `Sir Lancelot`. |

---

### CP-PER-002 — Guardar nombre con tecla Enter

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | App abierta. |
| **Datos** | Nombre: `Aldric` |
| **Pasos** | 1. Editar nombre en sidebar.<br>2. Pulsar **Enter** estando el foco en el TextBox. |
| **UI** | Mismo resultado que CP-PER-001. |
| **BD** | `DisplayName = 'Aldric'`. |

---

### CP-PER-003 — Validación nombre vacío

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | App abierta. |
| **Datos** | Nombre: `   ` (solo espacios) o vacío |
| **Pasos** | 1. Borrar todo el texto del nombre.<br>2. Observar banner y botón **💾 Nombre**. |
| **UI** | Banner: «Indique el nombre del personaje.»; botón guardar deshabilitado. |
| **BD** | `DisplayName` sin cambios respecto al valor anterior. |

---

### CP-PER-004 — Seleccionar avatar y copia local

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Imagen de prueba en disco, p. ej. `C:\Temp\avatar-test.jpg` (fuera de `%LocalAppData%\HobbyXP\`). |
| **Datos** | JPG o PNG válido |
| **Pasos** | 1. Sidebar → **📷 Avatar**.<br>2. Elegir la imagen de prueba.<br>3. Verificar visualización en sidebar y dashboard. |
| **UI** | Avatar personalizado visible; mensaje «Avatar actualizado.» |
| **BD** | `SELECT AvatarPath FROM PlayerProfiles;` → ruta **relativa** tipo `Avatar\profile.jpg` (no ruta absoluta de `C:\Temp\...`). |
| **Archivos** | Existe `%LocalAppData%\HobbyXP\Avatar\profile.jpg` (o extensión elegida). |

---

### CP-PER-005 — Avatar persiste tras borrar archivo original

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | CP-PER-004 ejecutado; se conoce la ruta original externa. |
| **Pasos** | 1. Eliminar `C:\Temp\avatar-test.jpg` del disco.<br>2. Cerrar y reabrir la aplicación.<br>3. Revisar sidebar y dashboard. |
| **UI** | Avatar personalizado sigue visible (carga desde copia local). |
| **BD** | `AvatarPath` sigue apuntando a `Avatar\profile.*`. |
| **Archivos** | Copia en `%LocalAppData%\HobbyXP\Avatar\` intacta. |

---

### CP-PER-006 — Fallback cuando no existe imagen gestionada

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Perfil con `AvatarPath` en BD pero archivo borrado manualmente de `Avatar\`. |
| **Pasos** | 1. Borrar `%LocalAppData%\HobbyXP\Avatar\profile.*`.<br>2. Reiniciar app. |
| **UI** | Muestra avatar por defecto (⚔); sin área en blanco ni excepción. |
| **BD** | Tras carga, `AvatarPath` debe quedar `NULL` (sanitización automática). |

---

### CP-PER-007 — Migración de avatar legacy (ruta externa antigua)

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | BD con `AvatarPath` absoluto externo que **sí existe** (simular con UPDATE manual si hace falta). |
| **Pasos** | 1. En SQLite: `UPDATE PlayerProfiles SET AvatarPath = 'C:\Temp\avatar-test.jpg' WHERE Id = 1;`<br>2. Reiniciar app (archivo debe existir). |
| **UI** | Avatar visible. |
| **BD** | `AvatarPath` migrado a `Avatar\profile.*` (ruta relativa). |
| **Archivos** | Copia creada en carpeta `Avatar\`. |

---

## 4. Validaciones de formulario

### CP-VAL-001 — Running: distancia inválida bloquea guardado

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Sección Actividades Físicas → pestaña Running. |
| **Datos** | Distancia: `0` o `-5` o texto `abc` |
| **Pasos** | 1. Completar formulario de sesión con distancia inválida.<br>2. Intentar guardar. |
| **UI** | Banner de error; botón guardar deshabilitado o sin efecto; no se agrega fila al historial. |
| **BD** | `COUNT(*)` en `RunningSessions` sin incremento. |

---

### CP-VAL-002 — Libro: páginas totales obligatorias y positivas

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Crecimiento Personal → Libros. |
| **Datos** | Título: `El nombre del viento`; Autor: `Rothfuss`; Páginas: `0` |
| **Pasos** | 1. Llenar título y autor.<br>2. Dejar páginas en 0.<br>3. Guardar. |
| **UI** | «Las páginas totales deben ser mayor que cero.» |
| **BD** | Sin nuevo registro en `Books`. |

---

### CP-VAL-003 — Libro: páginas leídas no superan total

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Libro existente con 300 páginas totales. |
| **Datos** | Páginas leídas: `350` |
| **Pasos** | 1. En fila del libro, ingresar páginas leídas > total.<br>2. Intentar actualizar progreso. |
| **UI** | Mensaje de validación en la fila; no se aplica el valor inválido. |
| **BD** | `PagesRead` (o equivalente) no supera `TotalPages`. |

---

### CP-VAL-004 — Curso: sesiones totales obligatorias

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Crecimiento Personal → Cursos. |
| **Datos** | Nombre: `Curso .NET`; Sesiones totales: vacío |
| **Pasos** | 1. Completar solo nombre.<br>2. Guardar. |
| **UI** | «Indique el nombre del curso.» o «Las sesiones totales deben ser mayor que cero.» |
| **BD** | Sin registro nuevo en `Courses`. |

---

### CP-VAL-005 — Rompecabezas: piezas > 0

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Entretenimiento → Rompecabezas. |
| **Datos** | Nombre: `Paisaje`; Piezas: `-10` |
| **Pasos** | 1. Registrar rompecabezas con piezas negativas.<br>2. Guardar. |
| **UI** | Banner de validación; no se guarda. |
| **BD** | Sin fila nueva en `Puzzles`. |

---

### CP-VAL-006 — Videojuego: porcentaje entre 0 y 100

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Entretenimiento → Videojuegos. |
| **Datos** | Título: `Hollow Knight`; Completitud: `150` |
| **Pasos** | 1. Registrar juego con % > 100.<br>2. Guardar o actualizar. |
| **UI** | Validación visible; valor no aceptado. |
| **BD** | Sin registro con completitud inválida. |

---

## 5. Running

### CP-RUN-001 — Registrar sesión de running con XP

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Anotar `TotalXp` inicial del perfil. |
| **Datos** | Fecha: hoy; Distancia: `5.0` km |
| **Pasos** | 1. Actividades Físicas → Running.<br>2. Registrar sesión.<br>3. Revisar historial y barra superior. |
| **UI** | Nueva fila en historial; mensaje de XP (+50 XP por 5 km × 10). |
| **BD** | `RunningSessions`: 1 fila con `DistanceKm = 5`.<br>`PlayerProfiles.TotalXp` = inicial + 50.<br>`XpTransactions`: movimiento positivo ~50. |

---

### CP-RUN-002 — Completar carrera oficial (medalla + bono XP)

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Ninguna carrera completada previamente. |
| **Datos** | Nombre: `Maratón UCAB`; Distancia: `21` km; marcar como completada |
| **Pasos** | 1. Registrar carrera oficial.<br>2. Marcar completada.<br>3. Ir a Logros → vitrina de medallas. |
| **UI** | Medalla «Medalla de Oro» desbloqueada; mensaje de logro; +500 XP bono. |
| **BD** | `OfficialRaces` con `IsCompleted = 1` (SQLite almacena bool como 0/1).<br>`EarnedMedals` con `MedalDefinitionId` de la medalla «Medalla de Oro» (`MedalDefinitions.Code = GoldRace`). |

---

### CP-RUN-003 — Eliminar sesión de running

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | CP-RUN-001 ejecutado; anotar XP actual. |
| **Pasos** | 1. En historial de sesiones, pulsar eliminar (🗑).<br>2. Confirmar en diálogo. |
| **UI** | Fila desaparece; XP del sidebar disminuye en la cantidad revertida. |
| **BD** | Registro ausente en `RunningSessions`.<br>`TotalXp` reducido; transacción de reversión en `XpTransactions`. |

---

### CP-RUN-004 — Eliminar carrera oficial

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Al menos una carrera en historial. |
| **Pasos** | 1. Eliminar carrera desde historial o botón de fila.<br>2. Confirmar. |
| **UI** | Carrera removida; sesiones vinculadas quedan sin carrera (según mensaje de confirmación). |
| **BD** | Fila eliminada de `OfficialRaces`. |

---

## 6. Gimnasio

### CP-GYM-001 — Guardar entrenamiento con ejercicios

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Pestaña Gym en Actividades Físicas. |
| **Datos** | Fecha: hoy; Ejercicio: `Press banca`; Peso: `60` kg; Reps: `10` |
| **Pasos** | 1. Agregar ejercicio a la sesión.<br>2. Guardar entrenamiento. |
| **UI** | Entrenamiento en historial; +25 XP base por sesión. |
| **BD** | `GymWorkouts` + `GymWorkoutEntries` creados. |

---

### CP-GYM-002 — Sobrecarga progresiva (PR) otorga medalla

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Registro previo de `Press banca` con 50 kg. |
| **Datos** | Mismo ejercicio con `60` kg (superior al máximo anterior) |
| **Pasos** | 1. Registrar segundo entrenamiento con peso mayor.<br>2. Guardar.<br>3. Revisar logros. |
| **UI** | Mensaje de sobrecarga progresiva; medalla «Sobrecarga Progresiva»; +150 XP bono. |
| **BD** | `EarnedMedals` con medalla de sobrecarga (`MedalDefinitions.Code = ProgressiveOverload`). |

---

### CP-GYM-003 — Eliminar entrenamiento

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Al menos un workout en historial. |
| **Pasos** | 1. Eliminar desde historial.<br>2. Confirmar. |
| **UI** | Entrenamiento removido; XP revertido. |
| **BD** | Sin fila en `GymWorkouts` (y entradas hijas eliminadas en cascada). |

---

## 7. Rompecabezas

### CP-PUZ-001 — Completar rompecabezas

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Datos** | Nombre: `Castillo`; Piezas: `1000`; marcar completado |
| **Pasos** | 1. Entretenimiento → Rompecabezas.<br>2. Registrar y marcar completado.<br>3. Opcional: adjuntar foto. |
| **UI** | +50 XP; entrada en listado. |
| **BD** | `Puzzles`: 1 fila con `Name`, `PieceCount`, `XpEarned = 50`. |

---

### CP-PUZ-002 — Eliminar rompecabezas

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Al menos un puzzle registrado. |
| **Pasos** | 1. Eliminar desde listado.<br>2. Confirmar. |
| **UI** | Registro eliminado; XP revertido. |
| **BD** | Sin fila en `Puzzles`. |

---

## 8. Series y películas

### CP-MED-001 — Completar obra audiovisual

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Datos** | Título: `Breaking Bad`; Tipo: Serie; marcar completada |
| **Pasos** | 1. Entretenimiento → Media.<br>2. Registrar y completar. |
| **UI** | +30 XP. |
| **BD** | `MediaEntries` con estado completado. |

---

### CP-MED-002 — Eliminar entrada de media

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Entrada existente. |
| **Pasos** | 1. Eliminar desde historial.<br>2. Confirmar. |
| **UI** | Entrada removida; XP revertido. |
| **BD** | Sin fila en `MediaEntries`. |

---

## 9. Videojuegos

### CP-VG-001 — Registrar avance por porcentaje

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Datos** | Título: `Zelda`; Completitud: `40` % |
| **Pasos** | 1. Registrar videojuego al 40 %. |
| **UI** | +400 XP (40 × 10). |
| **BD** | `VideoGames` con `CompletionPercentage = 40`. |

---

### CP-VG-002 — Platino (100 %) otorga medalla

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Datos** | Mismo juego actualizado a `100` % |
| **Pasos** | 1. Actualizar completitud a 100 %.<br>2. Revisar logros. |
| **UI** | Medalla «Medalla de Platino»; bono +1000 XP (además del XP por % según reglas activas). |
| **BD** | `EarnedMedals` con medalla platino (`MedalDefinitions.Code = PlatinumGame`). |

---

### CP-VG-003 — Eliminar videojuego

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Juego en historial. |
| **Pasos** | 1. Eliminar.<br>2. Confirmar. |
| **UI** | Juego removido; XP revertido. |
| **BD** | Sin fila en `VideoGames`. |

---

## 10. Libros

### CP-LIB-001 — Registrar lectura parcial

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Datos** | Título: `Dune`; Autor: `Herbert`; Total: `500`; Leídas: `50` |
| **Pasos** | 1. Registrar libro.<br>2. Actualizar páginas leídas a 50. |
| **UI** | +50 XP por páginas (1 XP/página). |
| **BD** | `Books` con `PagesRead = 50`, `TotalPages = 500`. |

---

### CP-LIB-002 — Completar libro

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Libro con 500 páginas, 450 leídas. |
| **Datos** | Páginas leídas: `500` |
| **Pasos** | 1. Marcar lectura completa. |
| **UI** | Bono +200 XP por libro terminado. |
| **BD** | `Books.Status` = completado; `CompletedAt` no nulo. |

---

## 11. Cursos

### CP-CUR-001 — Registrar sesión de curso

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Datos** | Nombre: `Azure Fundamentals`; Plataforma: `Microsoft Learn`; Sesiones totales: `10` |
| **Pasos** | 1. Crear curso.<br>2. Registrar 1 sesión completada desde la fila de progreso. |
| **UI** | Progreso 1/10; +10 XP por sesión. |
| **BD** | `Courses.SessionsCompleted = 1`; fila en `CourseSessionLogs`. |

---

### CP-CUR-002 — Completar curso

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Curso 9/10 sesiones. |
| **Pasos** | 1. Registrar sesión 10.<br>2. Verificar estado completado. |
| **UI** | Curso marcado terminado; bono +100 XP. |
| **BD** | `Courses.Status` = completado; `SessionsCompleted = TotalSessions`. |

---

### CP-CUR-003 — Validación sesiones no superan total

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Curso con 10 sesiones totales, 10 completadas. |
| **Pasos** | 1. Intentar registrar sesión adicional. |
| **UI** | Validación impide superar el total. |
| **BD** | `SessionsCompleted` permanece en 10. |

---

## 12. Logros, reglas y premios

### CP-LOG-001 — Vitrina de medallas muestra desbloqueadas

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Al menos una medalla ganada (CP-RUN-002, CP-GYM-002 o CP-VG-002). |
| **Pasos** | 1. Logros y Premios → pestaña Medallas. |
| **UI** | Medallas ganadas visibles con nombre y descripción. |
| **BD** | `SELECT * FROM EarnedMedals;` coherente con UI. |

---

### CP-LOG-002 — Editar regla de XP activa

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Datos** | Regla «Running por kilómetro»: cambiar a `15` XP/km |
| **Pasos** | 1. Logros y Premios → **Editor de reglas**.<br>2. Seleccionar regla en listado izquierdo.<br>3. Verificar que se muestra la acción del sistema y la fórmula de XP.<br>4. Modificar puntos por unidad.<br>5. Guardar. |
| **UI** | Mensaje verde de confirmación; cambio persistido en listado; botón deshabilitado si hay validación inválida (nombre vacío o valores negativos). |
| **BD** | `AchievementRules.PointsPerUnit = 15` para `ActionType = RunningKilometer` (0). |

---

### CP-LOG-005 — Editar catálogo de medallas

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | App con migración aplicada (catálogo de 7 medallas). |
| **Datos** | Medalla «Lector Voraz»: cambiar descripción a `Prueba de edición` |
| **Pasos** | 1. Logros y Premios → **Editor de medallas**.<br>2. Seleccionar medalla.<br>3. Editar descripción (y opcionalmente asignar icono con «Buscar…»).<br>4. Guardar.<br>5. Ir a pestaña **Vitrina** y verificar el texto actualizado (aunque la medalla siga bloqueada). |
| **UI** | Mensaje verde «Medalla '…' actualizada»; vitrina refleja el nuevo texto. |
| **BD** | `MedalDefinitions.Description` actualizado para `Code = BookCompleted` (3). `IconPath` opcional si se asignó imagen. |

---

### CP-LOG-006 — Nuevas medallas por módulo de entretenimiento y crecimiento

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Perfil sin esas medallas ganadas previamente. |
| **Pasos** | 1. Completar un libro (CP-LIB-002).<br>2. Completar un curso (CP-CUR-002).<br>3. Registrar un rompecabezas (CP-ENT-001).<br>4. Registrar una película/serie (CP-ENT-003).<br>5. Revisar vitrina. |
| **UI** | Medallas «Lector Voraz», «Graduado», «Maestro del Puzzle» y «Maratón Cultural» desbloqueadas con overlay de celebración. |
| **BD** | `EarnedMedals` con `MedalDefinitionId` para códigos `BookCompleted`, `CourseCompleted`, `PuzzleMaster`, `MediaMarathon`. |

---

### CP-LOG-007 — Medallas acumulativas por cantidad (10, 100…)

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Migración `AddMilestoneMedals` aplicada (catálogo de 52 medallas). |
| **Datos de ejemplo** | 10 libros completados → «Bibliófilo de Garra»; 100 km corridos → «Centurión del Kilómetro». |
| **Pasos** | 1. Acumular actividad hasta cruzar un umbral (p. ej. 10 libros o 10 series).<br>2. Al registrar la actividad que cruza el umbral, revisar overlay y vitrina.<br>3. En editor de medallas, verificar que la pista (`UnlockHint`) indica el conteo requerido. |
| **UI** | Se desbloquean todas las medallas del track cuyo umbral ≤ conteo actual y aún no estaban ganadas (p. ej. al llegar a 10 libros también se otorgan 5 si faltaban). |
| **BD** | `SELECT Code, Name FROM MedalDefinitions ORDER BY Id;` — 52 filas.<br>`EarnedMedals` coherente con conteos: libros `Status=Completed`, media `COUNT(*)`, km `SUM(RunningSessions.DistanceKm)`, etc. |

---

### CP-LOG-003 — Canjear premio deduce XP

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | `TotalXp` ≥ costo del premio (p. ej. ≥ 500). |
| **Datos** | Premio con costo conocido |
| **Pasos** | 1. Tienda de premios.<br>2. Canjear premio.<br>3. Confirmar. |
| **UI** | XP disminuye; mensaje de canje. |
| **BD** | `TotalXp` reducido; transacción negativa en `XpTransactions`. |

---

### CP-LOG-004 — Canje rechazado por XP insuficiente

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | `TotalXp` bajo (perfil nuevo o tras reset manual). |
| **Pasos** | 1. Intentar canjear premio costoso. |
| **UI** | Mensaje de error; XP sin cambios. |
| **BD** | `TotalXp` sin cambios. |

---

## 13. XP, nivel y dashboard

### CP-XP-001 — Subida de nivel muestra overlay

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | `TotalXp` cercano al umbral del siguiente nivel (p. ej. 950/1000 con `BaseXpPerLevel=1000`). |
| **Pasos** | 1. Registrar actividad que cruce el umbral (+50 XP o más).<br>2. Observar overlay. |
| **UI** | `LevelUpOverlay` visible con animación/confeti; muestra nuevo nivel; botón «Continuar la aventura» cierra overlay. |
| **BD** | `CurrentLevel` incrementado en 1. |

---

### CP-XP-002 — Dashboard refleja actividad reciente

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Varias actividades registradas en la semana. |
| **Pasos** | 1. Ir al Dashboard.<br>2. Revisar gráficos y sugerencias. |
| **UI** | Gráfico de XP semanal y distribución por hobby con datos; sugerencias de «subir de nivel» visibles. |
| **BD** | `XpTransactions` con fechas recientes alimentan los agregados. |

---

### CP-XP-003 — Barra de XP del sidebar sin crash

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Perfil con XP en distintos extremos: 0, mitad de nivel, casi lleno. |
| **Pasos** | 1. Probar con cada estado (puede simularse con UPDATE en BD y reinicio).<br>2. Navegar entre secciones. |
| **UI** | Barra de progreso renderiza correctamente; app estable. |
| **BD** | N/A |

---

## 14. Configuración

### CP-SET-001 — Cambiar XP base por nivel

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Perfil con XP acumulado; sidebar → **Configuración**. |
| **Datos** | XP base: `500` |
| **Pasos** | 1. Ingresar 500 en «XP base por nivel».<br>2. Pulsar **Guardar**.<br>3. Revisar sidebar y dashboard. |
| **UI** | Mensaje de confirmación; barra de progreso recalculada según nuevo umbral. |
| **BD** | `SELECT BaseXpPerLevel, CurrentLevel FROM PlayerProfiles;` → `BaseXpPerLevel = 500`; nivel recalculado coherente con `TotalXp`. |

---

### CP-SET-002 — Validación XP base inválido

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Datos** | XP base: `0` o `-100` |
| **Pasos** | 1. Ingresar valor inválido.<br>2. Observar banner y botón Guardar. |
| **UI** | «El XP base por nivel debe ser mayor que cero.»; botón deshabilitado. |
| **BD** | `BaseXpPerLevel` sin cambios. |

---

### CP-SET-003 — Exportar copia de base de datos

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Existen datos en la app. |
| **Pasos** | 1. Configuración → **Exportar copia de seguridad (.db)**.<br>2. Elegir ruta, p. ej. `C:\Temp\hobbyxp-backup.db`.<br>3. Abrir el archivo exportado en DB Browser. |
| **UI** | Mensaje con ruta de destino. |
| **Archivos** | Existe `hobbyxp-backup.db` con tablas y datos coherentes (`PlayerProfiles`, actividades, etc.). |

---

### CP-SET-004 — Restablecer todos los datos

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Perfil con actividades, XP y medallas; **exportar copia antes** si se desea conservar historial. |
| **Pasos** | 1. Configuración → **Restablecer todos los datos**.<br>2. Confirmar en diálogo.<br>3. Revisar dashboard y secciones. |
| **UI** | Vuelve a «Aventurero», nivel 1, 0 XP; listas vacías; reglas de XP seed intactas en editor. |
| **BD** | `PlayerProfiles`: 1 fila inicial; `RunningSessions`, `Books`, etc. en 0 filas; `AchievementRules` con seed. |
| **Archivos** | Carpetas `Avatar\` y `PuzzlePhotos\` eliminadas o vacías. |

---

## 15. Matriz de trazabilidad rápida

| Módulo UI | Tablas principales | Casos |
|-----------|-------------------|-------|
| Sidebar / Perfil | `PlayerProfiles` | CP-PER-*, CP-VAL (nombre) |
| Running | `RunningSessions`, `OfficialRaces` | CP-RUN-* |
| Gym | `GymWorkouts`, `GymWorkoutEntries`, `Exercises` | CP-GYM-* |
| Rompecabezas | `Puzzles` | CP-PUZ-* |
| Media | `MediaEntries` | CP-MED-* |
| Videojuegos | `VideoGames` | CP-VG-* |
| Libros | `Books` | CP-LIB-* |
| Cursos | `Courses`, `CourseSessionLogs` | CP-CUR-* |
| Logros | `MedalDefinitions`, `EarnedMedals`, `AchievementRules`, `Rewards` | CP-LOG-* |
| Dashboard / XP | `PlayerProfiles`, `XpTransactions`, `Milestones` | CP-XP-* |
| Configuración | `PlayerProfiles`, archivo `hobbyxp.db` | CP-SET-* |

---

## 16. Registro de ejecución (plantilla)

| ID caso | Ejecutado por | Fecha | Build/versión | Resultado (OK/FALLA) | Incidencia / notas |
|---------|---------------|-------|---------------|----------------------|--------------------|
| CP-GEN-001 | | | | | |
| CP-PER-004 | | | | | |
| … | | | | | |

---

## 17. Notas para el analista

1. **Aislamiento:** para casos de primera ejecución o avatar, conviene renombrar `%LocalAppData%\HobbyXP` a `HobbyXP_backup_YYYYMMDD` antes de probar.
2. **Cerrar la app** antes de inspeccionar o modificar `hobbyxp.db` para evitar bloqueos de archivo.
3. **Eliminaciones:** todos los flujos de borrado deben mostrar diálogo de confirmación; si el botón no responde, verificar que se está en la pestaña correcta y que el listado tiene foco.
4. **XP exacto:** el monto puede variar si se editaron reglas en CP-LOG-002; anotar reglas activas antes de validar montos.
5. **Incidencias:** documentar captura de pantalla, mensaje exacto y consulta SQL que demuestre la discrepancia.
