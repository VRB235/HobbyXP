# HobbyXP — Casos de prueba funcionales

> **Versión:** 1.1 — 18 de agosto de 2026  
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
| `CP-DIE` | Dieta |
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
| Ejecución (pruebas) | `dotnet run` desde `src\HobbyXP` (Debug) — **no** usar el exe de producción |
| Base de datos (Dev) | `%LocalAppData%\HobbyXP-Dev\hobbyxp.db` |
| Carpeta de datos (Dev) | `%LocalAppData%\HobbyXP-Dev\` |
| Avatar gestionado (Dev) | `%LocalAppData%\HobbyXP-Dev\Avatar\profile.{ext}` |
| Producción (no tocar en pruebas) | `%LocalAppData%\HobbyXP\` |

**Herramientas recomendadas para BD:** [DB Browser for SQLite](https://sqlitebrowser.org/) o `sqlite3` en consola.  
**Nota:** HobbyXP no expone API HTTP; la verificación de persistencia es directa sobre SQLite y archivos locales.

### 1.4 Consultas útiles (plantillas)

```sql
-- Perfil
SELECT Id, DisplayName, AvatarPath, CurrentLevel, TotalXp, SpendableXp, SpendableLedgerInitialized, BaseXpPerLevel, UpdatedAt
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
UNION ALL SELECT 'DietDayLogs', COUNT(*) FROM DietDayLogs
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
| Comida en plan | 15 XP / comida |
| Día perfecto de dieta (4/4) | +40 XP (bono) |

### 1.6 Criterios de aceptación globales

- La aplicación no debe cerrarse con excepción no controlada.
- Los mensajes de validación deben mostrarse en español, en banner rojo sobre el formulario.
- Tras guardar una actividad que otorga XP, la barra superior debe mostrar un mensaje de logro/XP.
- El sidebar y el dashboard deben reflejar progresión global, saldo canjeable y niveles de hobby actualizados sin reiniciar la app.
- Al eliminar un registro con XP asociado, debe pedirse confirmación y revertirse el XP correspondiente.

---

## 2. General y arranque

### CP-GEN-001 — Primera ejecución crea BD y perfil inicial

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | No existe `%LocalAppData%\HobbyXP-Dev\` (renombrar carpeta si hace falta). |
| **Datos** | N/A |
| **Pasos** | 1. Ejecutar la aplicación.<br>2. Esperar carga del dashboard. |
| **UI** | Ventana principal visible; sidebar con nombre «Aventurero», nivel 1, XP 0, saldo 0; icono de avatar por defecto (⚔). |
| **BD** | `SELECT COUNT(*) FROM PlayerProfiles;` → **1** fila con `DisplayName='Aventurero'`, `CurrentLevel=1`, `TotalXp=0`, `SpendableXp=0`, `SpendableLedgerInitialized=1`, `BaseXpPerLevel=1000`. |
| **Archivos** | Carpeta `%LocalAppData%\HobbyXP-Dev\` creada con `hobbyxp.db`. |

---

### CP-GEN-002 — Navegación entre secciones

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | App abierta. |
| **Pasos** | 1. Pulsar cada ítem del sidebar: Dashboard, Actividades Físicas, Entretenimiento, Crecimiento Personal, Logros y Premios.<br>2. En secciones con pestañas, alternar entre ellas (Running / Gimnasio / Dieta; etc.). |
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
| **Precondiciones** | Imagen de prueba en disco, p. ej. `C:\Temp\avatar-test.jpg` (fuera de `%LocalAppData%\HobbyXP-Dev\`). |
| **Datos** | JPG o PNG válido |
| **Pasos** | 1. Sidebar → **📷 Avatar**.<br>2. Elegir la imagen de prueba.<br>3. Verificar visualización en sidebar y dashboard. |
| **UI** | Avatar personalizado visible; mensaje «Avatar actualizado.» |
| **BD** | `SELECT AvatarPath FROM PlayerProfiles;` → ruta **relativa** tipo `Avatar\profile.jpg` (no ruta absoluta de `C:\Temp\...`). |
| **Archivos** | Existe `%LocalAppData%\HobbyXP-Dev\Avatar\profile.jpg` (o extensión elegida). |

---

### CP-PER-005 — Avatar persiste tras borrar archivo original

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | CP-PER-004 ejecutado; se conoce la ruta original externa. |
| **Pasos** | 1. Eliminar `C:\Temp\avatar-test.jpg` del disco.<br>2. Cerrar y reabrir la aplicación.<br>3. Revisar sidebar y dashboard. |
| **UI** | Avatar personalizado sigue visible (carga desde copia local). |
| **BD** | `AvatarPath` sigue apuntando a `Avatar\profile.*`. |
| **Archivos** | Copia en `%LocalAppData%\HobbyXP-Dev\Avatar\` intacta. |

---

### CP-PER-006 — Fallback cuando no existe imagen gestionada

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Perfil con `AvatarPath` en BD pero archivo borrado manualmente de `Avatar\`. |
| **Pasos** | 1. Borrar `%LocalAppData%\HobbyXP-Dev\Avatar\profile.*`.<br>2. Reiniciar app. |
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

### CP-VAL-007 — Dieta: no guardar el día sin comidas marcadas

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Actividades Físicas → Dieta. |
| **Datos** | Las 4 comidas en «—» (sin marcar). |
| **Pasos** | 1. No pulsar En plan ni Fuera de plan, o pulsar de nuevo para desmarcar.<br>2. Intentar Guardar día. |
| **UI** | «Marque al menos una comida (En plan o Fuera de plan).» |
| **BD** | Sin fila nueva en `DietDayLogs`. |

---

## 5. Running

### CP-RUN-001 — Registrar sesión de running con XP

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Anotar `SpendableXp` y XP del hobby Running (`HobbyProgresses`). |
| **Datos** | Fecha: editable (default hoy; probar también una fecha pasada); Distancia: `5.0` km |
| **Pasos** | 1. Actividades Físicas → Running.<br>2. Elegir fecha si no es hoy.<br>3. Registrar sesión.<br>4. Revisar historial, banner del hobby y saldo del sidebar. |
| **UI** | Nueva fila en historial con la fecha elegida; mensaje de XP (+50 XP por 5 km × 10); saldo canjeable +50; progresión global sin cambios si no hubo level-up de hobby. |
| **BD** | `RunningSessions`: 1 fila con `DistanceKm = 5` y `RecordedAt` = fecha elegida (UTC día local).<br>`HobbyProgresses` (Running): +50 XP.<br>`PlayerProfiles.SpendableXp` = inicial + 50.<br>`XpTransactions`: movimiento positivo ~50 (`IsGlobal=0`). |

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
| **Precondiciones** | CP-RUN-001 ejecutado; anotar `SpendableXp` y XP del hobby Running. |
| **Pasos** | 1. En historial de sesiones, pulsar eliminar (🗑).<br>2. Confirmar en diálogo. |
| **UI** | Fila desaparece; saldo canjeable y barra del hobby disminuyen en la cantidad revertida. |
| **BD** | Registro ausente en `RunningSessions`.<br>`HobbyProgresses` y `SpendableXp` reducidos; transacción de reversión en `XpTransactions`. |

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
| **Datos** | Fecha: editable (default hoy; probar también una fecha pasada); Ejercicio: `Press banca`; Peso: `60` kg; Reps: `10` |
| **Pasos** | 1. Elegir fecha del entrenamiento.<br>2. Agregar ejercicio a la sesión.<br>3. Guardar entrenamiento. |
| **UI** | Entrenamiento en historial con la fecha elegida; +25 XP base por sesión. |
| **BD** | `GymWorkouts` con `WorkoutDate` = fecha elegida (UTC día local) + `GymWorkoutEntries` creados. |

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

## 6.1 Dieta

Contrato: 4 comidas (Desayuno, Almuerzo, Cena, Snack). Cada una: En plan / Fuera de plan / sin marcar. **Día bueno** = al menos 3 comidas en plan. **Día perfecto** = 4/4. Cuota semanal (lun–dom): **5 días buenos**. Las comidas sin marcar no suman. Un desliz (fuera de plan) no anula el día si quedan ≥3 en plan.

XP de referencia: 15 XP por comida en plan; +40 XP si el día queda 4/4. Fuera de plan = 0 XP, pero sí se persiste.

### CP-DIE-001 — Día bueno con 3 comidas en plan

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Actividades Físicas → pestaña **Dieta**. |
| **Datos** | Fecha: hoy. Desayuno, Almuerzo y Cena = **En plan**. Snack = sin marcar. |
| **Pasos** | 1. Abrir Dieta.<br>2. Marcar las 3 comidas indicadas.<br>3. Verificar resumen `3/4 · Día bueno`.<br>4. Guardar día. |
| **UI** | Historial con fecha de hoy, resultado `3/4`, tipo «Día bueno»; +45 XP; medalla «Primer Plato» la primera vez. Dashboard: cuota Dieta avanza 1/5 días buenos (si es la semana actual). |
| **BD** | `SELECT DayDate, BreakfastStatus, LunchStatus, DinnerStatus, SnackStatus, OnPlanCount, XpEarned FROM DietDayLogs ORDER BY Id DESC LIMIT 1;` → `OnPlanCount=3`, `SnackStatus='Unlogged'`, `XpEarned=45`. `HobbyProgresses` de `SourceType='Diet'` incrementa 45. |

---

### CP-DIE-002 — Snack fuera de plan no tumba un día bueno

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | CP-DIE-001 guardado el mismo día (o repetir 3 comidas en plan). |
| **Datos** | Snack = **Fuera de plan**. |
| **Pasos** | 1. En el mismo día, pulsar **Fuera de plan** en Snack.<br>2. Guardar día (upsert, no debe crear otra fila). |
| **UI** | Sigue `3/4 · Día bueno`; XP permanece 45 (no suma el snack). Historial: 1 sola fila para esa fecha; Snack = «Fuera de plan». |
| **BD** | `SELECT COUNT(*) FROM DietDayLogs WHERE date(DayDate)=date('ahora-local-en-utc');` → **1**. `OnPlanCount=3`, `SnackStatus='OffPlan'`, `XpEarned=45`. |

---

### CP-DIE-003 — Día perfecto otorga bono

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Pestaña Dieta. Usar una **fecha distinta** a CP-DIE-001 (p. ej. ayer) para no pisar el upsert. |
| **Datos** | Las 4 comidas = **En plan**. |
| **Pasos** | 1. Cambiar el DatePicker a ayer.<br>2. Marcar las 4 comidas En plan.<br>3. Guardar. |
| **UI** | `4/4 · Día perfecto`; +100 XP (60 de comidas + 40 bono); mensaje de día perfecto. |
| **BD** | `OnPlanCount=4`, `XpEarned=100`. Transacciones: una de `DietMealOnPlan` (60) y una de `DietPerfectDay` (40). |

---

### CP-DIE-004 — Semana con 4 días buenos no cumple la cuota

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Semana lun–dom actual. Saber interpretar `WeeklyQuotaEvaluations` (la cuota de Dieta **no castiga** semanas anteriores al primer `DietDayLogs`; el dashboard de la semana abierta sí cuenta). |
| **Datos** | 4 fechas de la semana actual, cada una con ≥3 comidas En plan. No registrar un 5.º día bueno. |
| **Pasos** | 1. Guardar 4 días buenos en la semana (usar DatePicker atrasable).<br>2. Abrir Dashboard y localizar la cuota de **Dieta**. |
| **UI** | Progreso `4/5 días buenos`; la cuota **no** aparece como cumplida. |
| **BD** | `SELECT COUNT(*) FROM DietDayLogs WHERE OnPlanCount >= 3 AND DayDate >= <lunes-utc> AND DayDate < <lunes-siguiente-utc>;` → **4**. |

---

### CP-DIE-005 — Guardar sin marcar ninguna comida

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Pestaña Dieta; día sin marcas (o pulsar de nuevo En plan/Fuera de plan para dejar «—»). |
| **Pasos** | 1. Dejar las 4 comidas sin marcar.<br>2. Intentar Guardar día. |
| **UI** | Banner: «Marque al menos una comida (En plan o Fuera de plan).»; botón deshabilitado o sin efecto. |
| **BD** | Sin nueva fila en `DietDayLogs` (si el día no existía). |

---

### CP-DIE-006 — Eliminar día de dieta revierte XP

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Al menos un día en el historial de Dieta con XP > 0. Anotar `HobbyProgresses.TotalXp` de Dieta antes. |
| **Pasos** | 1. En historial, pulsar ✕.<br>2. Confirmar el diálogo. |
| **UI** | Fila desaparece; XP de Dieta disminuye; saldo canjeable se ajusta. |
| **BD** | Sin esa fila en `DietDayLogs`. `HobbyProgresses` de Dieta baja el `XpEarned` de ese día. |

---

## 7. Rompecabezas

### CP-PUZ-001 — Completar rompecabezas

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Datos** | Nombre: `Castillo`; Piezas: `1000`; marcar completado |
| **Pasos** | 1. Entretenimiento → Rompecabezas.<br>2. Registrar y marcar completado.<br>3. Opcional: adjuntar foto. |
| **UI** | +50 XP; fila en historial tabular (ordenable). |
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

### CP-PUZ-003 — Filtrar y ordenar historial de rompecabezas

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Varios puzzles con distintas categorías y fechas. |
| **Pasos** | 1. Filtrar por texto, categoría y rango de fechas.<br>2. Pulsar cabeceras (Nombre, Piezas, Categoría, Fecha, XP).<br>3. Limpiar filtros. |
| **UI** | El listado se reduce según filtros; cabeceras muestran ▲/▼; Limpiar restaura todo. |

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

### CP-MED-003 — Filtrar y ordenar historial de media

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Varias películas/series en distintas fechas. |
| **Pasos** | 1. Filtrar por título, tipo y fechas.<br>2. Ordenar por cabeceras.<br>3. Limpiar. |
| **UI** | Listado coherente con filtros; ▲/▼ en cabeceras. |

---

### CP-MED-004 — Cuota semanal: capítulos no bastan; hay que terminar la serie

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Semana lun–dom actual. Serie en progreso (p. ej. 10 capítulos). Dashboard abierto en Disciplina semanal. |
| **Pasos** | 1. Registrar 2 capítulos de la serie (sin terminarla).<br>2. Completar 2 películas en la misma semana.<br>3. Abrir Dashboard → cuota de **Media**.<br>4. Terminar la serie (capítulos restantes o registrar serie completada).<br>5. Volver al Dashboard. |
| **UI** | Tras pasos 1–3: progreso `0/1 series terminadas · 2/2 películas`; cuota **no** cumplida.<br>Tras paso 5: `1/1 series terminadas · 2/2 películas`; **Cumplida**. |
| **BD** | `MediaSeriesChapterLogs` con capítulos; `MediaEntries` tipo Series con `CompletedAt` en la semana. Sin evaluación de castigo en semana abierta. |
| **Notas** | Si **no** hay serie en progreso ni terminada esa semana, la cuota de series **no aplica**; siguen haciendo falta 2 películas. |

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
| **Precondiciones** | Juego en progreso o platinado. |
| **Pasos** | 1. Eliminar.<br>2. Confirmar. |
| **UI** | Removido; XP revertido. |
| **BD** | Sin fila en `VideoGames`. |

---

### CP-VG-004 — Filtrar historial y ordenar platinados

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Varios juegos en progreso y platinados. |
| **Pasos** | 1. Filtrar por título, plataforma y fechas.<br>2. Ordenar tabla de platinados por cabeceras.<br>3. Limpiar. |
| **UI** | Ambas secciones respetan filtros; platinados ordenables con ▲/▼. |

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

### CP-LIB-003 — Cuota semanal: 20% de páginas del libro actual

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Semana lun–dom actual. Libro en lectura `Dune`, `TotalPages = 500` (cuota = 100 páginas, techo del 20%). |
| **Pasos** | 1. Registrar 50 páginas leídas en la semana.<br>2. Dashboard → Disciplina → **Libros**.<br>3. Registrar 50 páginas más (acumulado 100 en la semana).<br>4. Volver al Dashboard. |
| **UI** | Paso 2: progreso `50/100 páginas`; cuota **no** cumplida; etiqueta indica 20% de «Dune».<br>Paso 4: `100/100 páginas`; **Cumplida**. |
| **BD** | `BookReadingLogs.PagesDone` suma 100 en el rango lun–dom (UTC de fecha local). Sin fila de castigo en semana abierta. |
| **Notas** | Terminar el libro en la semana **sí cumple** aunque las páginas de esa semana sean &lt; 20%. Sin libro en lectura y sin uno terminado esa semana: cuota **No aplica** (no hay castigo). |

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

### CP-CUR-004 — Cuota semanal: 5 sesiones

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Semana lun–dom actual. Curso en progreso con al menos 10 sesiones totales. |
| **Pasos** | 1. Registrar 4 sesiones en la semana (pueden ser varios logs).<br>2. Dashboard → Disciplina → **Cursos**.<br>3. Registrar 1 sesión más (total 5).<br>4. Volver al Dashboard. |
| **UI** | Paso 2: progreso `4/5 sesiones`; cuota **no** cumplida.<br>Paso 4: `5/5 sesiones`; **Cumplida**. |
| **BD** | `CourseSessionLogs.SessionsDone` suma 5 en el rango lun–dom. |
| **Notas** | Sin curso en progreso y sin sesiones esa semana: cuota **No aplica**. Terminar un curso de 3 sesiones en la semana **no** cumple si no se llega a 5 sesiones. |

---

## 12. Logros, reglas y premios

### CP-LOG-001 — Vitrina de medallas muestra desbloqueadas

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | Al menos una medalla ganada (CP-RUN-002, CP-GYM-002 o CP-VG-002). |
| **Pasos** | 1. Logros y Premios → pestaña **Vitrina**.<br>2. Pulsar la cabecera de un hobby colapsado para expandirlo; pulsar de nuevo para plegarlo. |
| **UI** | Cada hobby es un menú colapsable (☰). Los hobbies con al menos una medalla desbloqueada arrancan **abiertos**; el resto, **cerrados**. Cabecera: nombre + `n/m desbloqueadas`. Dentro: desbloqueadas primero (borde dorado), luego pendientes (opacidad baja). |
| **BD** | `SELECT * FROM EarnedMedals;` coherente con las tarjetas doradas. |

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

### CP-LOG-003 — Canjear premio deduce saldo canjeable

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | `SpendableXp` ≥ costo del premio (p. ej. ≥ 500). Anotar también `TotalXp` / `CurrentLevel` globales. |
| **Datos** | Premio con costo conocido |
| **Pasos** | 1. Tienda de premios (etiqueta «Saldo canjeable»).<br>2. Canjear premio.<br>3. Confirmar. |
| **UI** | Saldo canjeable disminuye; nivel/XP de progresión global sin cambios; mensaje de canje. |
| **BD** | `SpendableXp` reducido; `TotalXp` y `CurrentLevel` intactos; transacción negativa en `XpTransactions`. |

---

### CP-LOG-004 — Canje rechazado por saldo insuficiente

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Precondiciones** | `SpendableXp` bajo (perfil nuevo o tras canjes). |
| **Pasos** | 1. Intentar canjear premio costoso. |
| **UI** | Mensaje de error; saldo sin cambios. |
| **BD** | `SpendableXp` y `TotalXp` sin cambios. |

---

### CP-LOG-005 — Migración one-shot a saldo canjeable

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | BD previa a `AddSpendableXpLedger` con XP en hobbies y/o global (`SpendableLedgerInitialized = 0`). Anotar sumas. |
| **Pasos** | 1. Arrancar la app (aplica migración + `EnsureSpendableLedgerAsync`).<br>2. Revisar sidebar (nivel 1, saldo) y banners de hobby.<br>3. Cerrar y volver a abrir. |
| **UI** | Todos los hobbies y el global en nivel 1 / 0 XP de progresión; «Saldo: N» = suma previa de hobbies + global. |
| **BD** | `SpendableXp` = suma anotada; `SpendableLedgerInitialized = 1`; `SpendableProgressBaselineApplied = 1`; `HobbyProgresses.TotalXp = 0`, `CurrentLevel = 1`; global `TotalXp = 0`, `CurrentLevel = 1`. Segundo arranque: mismos valores (no vuelve a sumar ni a reconstruir desde `XpTransactions`). |

---

### CP-LOG-008 — Desbloquear medalla otorga bonus, título e inmunidad

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Perfil sin medalla «Lector Voraz». Anotar `SpendableXp`. |
| **Pasos** | 1. Completar el primer libro (CP-LIB-002).<br>2. Observar overlay de medalla.<br>3. Revisar sidebar (título de honor, inmunidad) y saldo. |
| **UI** | Overlay «¡MEDALLA DESBLOQUEADA!» con +50 XP canjeable; sidebar muestra título «Lector Voraz» e inmunidad ~7 días; badge dorado en **Logros y Premios**. |
| **BD** | `PlayerProfiles.HonorTitle = 'Lector Voraz'`; `SpendableXp` +50; `DisciplineImmunityUntilUtc` ≈ ahora+7d; transacción `ActionType = MedalPrivilegeBonus`. |

---

### CP-LOG-009 — Tienda: costo × nivel, inventario y equipar

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Nivel global ≥ 2; `SpendableXp` suficiente; premio base `300` XP. |
| **Pasos** | 1. Logros → **Tienda**.<br>2. Verificar costo efectivo `600` (300 × 2).<br>3. Canjear.<br>4. En **Inventario**, seleccionar y **Equipar reliquia**. |
| **UI** | Premio pasa a inventario; sidebar muestra marco dorado del avatar y «Reliquia: …». |
| **BD** | `Rewards.Status = Redeemed`; `RedeemedCostInPoints = 600`; `PlayerProfiles.EquippedRewardId` = id del premio. |

---

### CP-LOG-010 — Dashboard: hub de logros y siguiente logro en hobby

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Media |
| **Pasos** | 1. Abrir Dashboard: bloque **Logros y premios** (última medalla, siguiente logro, premio destacado).<br>2. Ir a Crecimiento → Libros y revisar el banner XP. |
| **UI** | Hub con datos; banner de libros muestra «Siguiente logro: … (n/m …)» y barra. Botón **Abrir módulo** navega a Logros. |

---

## 13. XP, nivel y dashboard

### CP-XP-001 — Subida de nivel de hobby y bonus global

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Hobby Running cerca del umbral 1→2 (p. ej. ~950 XP de hobby con `BaseXpPerLevel=1000`). Anotar `TotalXp` global y `SpendableXp`. |
| **Pasos** | 1. Registrar sesión de running que cruce el umbral del hobby.<br>2. Observar banner de Running y sidebar/dashboard global. |
| **UI** | Banner del hobby muestra nivel 2; mensaje de logro de hobby; si el global sube, `LevelUpOverlay` con nuevo nivel global; saldo canjeable sube por la actividad **y** por el bonus meta. |
| **BD** | `HobbyProgresses` (Running): `CurrentLevel` +1; `PlayerProfiles.TotalXp` += `BaseXpPerLevel`; `SpendableXp` += XP de actividad + bonus meta; txs con `IsGlobal=0` (actividad) y `IsGlobal=1` (`HobbyLevelUp`). |

---

### CP-XP-002 — Dashboard refleja actividad reciente

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Varias actividades registradas en la semana. |
| **Pasos** | 1. Tras registrar actividades, abrir Dashboard.<br>2. Revisar hero global, sección «Progreso por hobby», gráfico semanal e hitos. |
| **UI** | Hero = nivel/XP **global**; lista de barras por hobby; gráfico semanal usa XP de actividades (no bonuses globales); sugerencias de «subir de nivel» visibles. |
| **BD** | `XpTransactions` con fechas recientes alimentan los agregados; `HobbyProgresses` refleja pools por módulo. |

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

### CP-SET-001 — Cambiar XP base (tramo 1→2)

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Perfil con XP acumulado y nivel ≥ 2; sidebar → **Configuración**. |
| **Datos** | XP base (nivel 1→2): `500` |
| **Pasos** | 1. Anotar `CurrentLevel` y `TotalXp` actuales.<br>2. Ingresar 500 en «XP base (nivel 1→2)».<br>3. Pulsar **Guardar**.<br>4. Revisar sidebar y dashboard. |
| **UI** | Mensaje de confirmación; nivel y barra recalculados según el XP total y la nueva base. |
| **BD** | `SELECT BaseXpPerLevel, CurrentLevel, TotalXp FROM PlayerProfiles;` → `BaseXpPerLevel = 500`; `TotalXp` sin cambios; `CurrentLevel` coherente con la fórmula geométrica (`umbral N = Base × (2^(N−1) − 1)`). |

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

### CP-SET-004 — Restablecer progreso (conserva catálogo de ejercicios)

| Campo | Detalle |
|-------|---------|
| **Prioridad** | Alta |
| **Precondiciones** | Perfil con actividades, XP, medallas y al menos un ejercicio en catálogo gym; **exportar copia antes** si se desea conservar historial. |
| **Pasos** | 1. Anotar nombres del catálogo `Exercises` y el `DisplayName` del perfil.<br>2. Configuración → **Restablecer progreso**.<br>3. Confirmar en diálogo.<br>4. Revisar dashboard, gym (catálogo) y secciones de historial. |
| **UI** | Nivel 1 / 0 XP (global y hobbies); listas de actividades vacías; catálogo de ejercicios intacto; nombre/avatar y XP base sin cambio; reglas de XP y definiciones de medallas intactas; premios canjeados vuelven a «Disponible». |
| **BD** | `Exercises` sin pérdida de filas; `GymWorkouts`/`GymWorkoutEntries`/`RunningSessions`/`Books`/etc. en 0; `XpTransactions`/`EarnedMedals`/`Milestones` en 0; `PlayerProfiles.TotalXp=0`, `CurrentLevel=1`, `SpendableXp=0`; `HobbyProgresses` en nivel 1 / 0 XP. |
| **Archivos** | `PuzzlePhotos\` eliminada o vacía; `Avatar\` se conserva. |

---

## 15. Matriz de trazabilidad rápida

| Módulo UI | Tablas principales | Casos |
|-----------|-------------------|-------|
| Sidebar / Perfil | `PlayerProfiles` | CP-PER-*, CP-VAL (nombre) |
| Running | `RunningSessions`, `OfficialRaces` | CP-RUN-* |
| Gym | `GymWorkouts`, `GymWorkoutEntries`, `Exercises` | CP-GYM-* |
| Dieta | `DietDayLogs` | CP-DIE-* |
| Rompecabezas | `Puzzles` | CP-PUZ-* |
| Media | `MediaEntries`, `MediaSeries`, `WeeklyQuotaEvaluations` | CP-MED-* |
| Videojuegos | `VideoGames` | CP-VG-* |
| Libros | `Books`, `BookReadingLogs`, `WeeklyQuotaEvaluations` | CP-LIB-* |
| Cursos | `Courses`, `CourseSessionLogs`, `WeeklyQuotaEvaluations` | CP-CUR-* |
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

1. **Aislamiento:** `dotnet run` (Debug) usa `%LocalAppData%\HobbyXP-Dev\`; el exe de producción usa `%LocalAppData%\HobbyXP\`. No mezclar. Para casos de primera ejecución, renombrar `HobbyXP-Dev` a `HobbyXP-Dev_backup_YYYYMMDD`.
2. **Cerrar la app** antes de inspeccionar o modificar `hobbyxp.db` para evitar bloqueos de archivo.
3. **Eliminaciones:** todos los flujos de borrado deben mostrar diálogo de confirmación; si el botón no responde, verificar que se está en la pestaña correcta y que el listado tiene foco.
4. **XP exacto:** el monto puede variar si se editaron reglas en CP-LOG-002; anotar reglas activas antes de validar montos.
5. **Incidencias:** documentar captura de pantalla, mensaje exacto y consulta SQL que demuestre la discrepancia.
