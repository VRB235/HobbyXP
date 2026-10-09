#!/bin/sh
# Dump diario de PostgreSQL. Retiene BACKUP_KEEP_DAYS días.
set -eu

KEEP_DAYS="${BACKUP_KEEP_DAYS:-14}"
DB_HOST="${DB_HOST:-db}"
DB_NAME="${POSTGRES_DB:-hobbyxp}"
DB_USER="${POSTGRES_USER:-hobbyxp}"

mkdir -p /backups

echo "[hobbyxp-backup] started; keep=${KEEP_DAYS}d"

while true; do
  STAMP=$(date -u +%Y%m%dT%H%M%SZ)
  OUT="/backups/hobbyxp-${STAMP}.sql.gz"
  echo "[hobbyxp-backup] dumping ${DB_NAME} → ${OUT}"
  if pg_dump -h "$DB_HOST" -U "$DB_USER" -d "$DB_NAME" --no-owner --no-acl | gzip -c > "$OUT"; then
    echo "[hobbyxp-backup] ok"
  else
    echo "[hobbyxp-backup] FAILED" >&2
    rm -f "$OUT"
  fi

  find /backups -name 'hobbyxp-*.sql.gz' -type f -mtime "+${KEEP_DAYS}" -delete 2>/dev/null || true

  # ~24 h
  sleep 86400
done
