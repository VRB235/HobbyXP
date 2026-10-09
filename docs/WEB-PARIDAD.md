# Paridad web (fases posteriores al MVP)

El MVP en `HobbyXP.Web` cubre el uso diario portable: **auth, dashboard, running, gym, dieta, perfil/XP**.

WPF sigue siendo el cliente con **paridad funcional completa** hasta cerrar estas fases. Luego se documenta como archivado/solo lectura histórica.

## Fase A — Entretenimiento y crecimiento

| Módulo | Servicio Core | UI web pendiente |
|--------|---------------|------------------|
| Rompecabezas | `IPuzzleService` | Alta + historial + fotos |
| Media / series | `IMediaService` | Formularios + capítulos |
| Videojuegos | `IVideoGameService` | Progreso + platino |
| Libros | `IBookService` | Lecturas |
| Cursos | `ICourseService` | Sesiones |

Placeholders actuales: `/entertainment`.

## Fase B — Premios, medallas y disciplina

| Módulo | Servicio Core | UI web pendiente |
|--------|---------------|------------------|
| Tienda / inventario | `IRewardService` | Grid + canje + equipar |
| Medallas | `IMedalService` / `IAchievementProgressService` | Vitrina + overlay |
| Cuotas | `IWeeklyQuotaService` / `IModuleDisciplineService` | Indicadores en dashboard |
| Sugerencias | `ISuggestionService` | CRUD + imágenes |

Placeholder: `/rewards`.

## Fase C — Gráficos e imágenes

- Gráficos dashboard (reemplazo LiveCharts WPF → chart.js / Blazor chart).
- Upload de avatar, portadas y fotos de premios/puzzles hacia `HOBBYXP_DATA_DIR` / volumen Docker.
- Endpoint estático o minimal API para servir media autenticada.

## Fase D — Cierre escritorio

1. Checklist de paridad funcional (casos de `docs/CASOS-PRUEBA-FUNCIONALES.md` adaptados a web).
2. README: URL del VPS como cliente oficial; WPF marcado deprecated.
3. Opcional: dejar de publicar ZIP/MSIX en releases siguientes.
4. Mantener Core único; no duplicar reglas de XP.

## Criterio de éxito del plan portable

Desde Android Chrome y PC Chrome: login → ver XP → registrar running/gym/dieta → mismo estado al refrescar en el otro dispositivo, con VPS ~3–8 USD/mes.
