# Despliegue HobbyXP Web en VPS barato

Cliente oficial portable: **navegador** (PC + Android) contra un VPS con Docker.

## Qué incluye `deploy/`

| Pieza | Rol |
|-------|-----|
| `docker-compose.yml` | `web` (Blazor) + `db` (Postgres 16) + `caddy` (HTTPS) + `backup` |
| `Dockerfile` | Publish de `HobbyXP.Web` |
| `caddy/Caddyfile` | Reverse proxy + TLS automático |
| `backup/backup-loop.sh` | `pg_dump` diario → `deploy/backups/` |
| `.env.example` | Secretos y dominio |

**No use hosting compartido PHP** (planes Hostinger “web”). Use **VPS/KVM** (Hostinger VPS, Contabo, Hetzner, DigitalOcean, etc.) con Docker.

## Arranque en el VPS

```bash
git clone <repo> && cd HobbyXP
cp deploy/.env.example deploy/.env
# Editar POSTGRES_PASSWORD, HOBBYXP_PASSWORD, HOBBYXP_DOMAIN
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build
```

- DNS A/AAAA del dominio → IP del VPS (puertos 80/443).
- Login: usuario/contraseña de `HobbyXp__Auth_*` / `.env`.
- Media/fotos: volumen `hobbyxp_media` montado en `/data/media` (`HOBBYXP_DATA_DIR`).

## Desarrollo local (sin Docker)

```powershell
cd src\HobbyXP.Web
$env:HOBBYXP_DATA_DIR = "$env:LOCALAPPDATA\HobbyXP-Web-Dev"
dotnet run
```

Sin `ConnectionStrings:Default` usa **SQLite** local. Con cadena Postgres (`Host=...`) usa Npgsql.

## Importar datos desde el escritorio (SQLite → Postgres)

1. Subir el VPS y anotar la connection string (misma que en `.env`).
2. En el PC (con acceso a la BD del VPS o túnel):

```powershell
dotnet run --project src/HobbyXP.Tools/SqliteToPostgres -- `
  --sqlite "$env:LOCALAPPDATA\HobbyXP\hobbyxp.db" `
  --postgres "Host=IP;Port=5432;Database=hobbyxp;Username=hobbyxp;Password=..."
```

3. Copiar carpetas de fotos del `%LocalAppData%\HobbyXP\` al volumen media del VPS si las necesita.

El import **borra** el destino y copia entidades; resincroniza secuencias de identidad en Postgres.

## Backups

El servicio `backup` escribe `deploy/backups/hobbyxp-*.sql.gz` cada ~24 h y elimina los más antiguos que `BACKUP_KEEP_DAYS`.

Restaurar (ejemplo):

```bash
gunzip -c deploy/backups/hobbyxp-XXXX.sql.gz | docker compose -f deploy/docker-compose.yml exec -T db \
  psql -U hobbyxp -d hobbyxp
```

## Coste orientativo

- VPS: ~3–8 USD/mes (1 usuario sobra).
- Dominio: opcional ~10–15 USD/año.
