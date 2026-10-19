#!/usr/bin/env bash
# Inicializa la base de datos del entorno local con docker compose.
#   crear      -> crea la base y los usuarios de la aplicación y de migraciones
#   restringir -> aplica los DENY sobre las tablas inmutables (después de las migraciones)
# Las contraseñas llegan por variables de entorno; nunca se escriben en los scripts.
set -euo pipefail

SQLCMD=(/opt/mssql-tools18/bin/sqlcmd -C -b -S "${DB_HOST}" -U sa -P "${MSSQL_SA_PASSWORD}")

case "${1:-}" in
  crear)
    "${SQLCMD[@]}" -i /scripts/00-create-database.sql -v DbName="${DB_NAME}"
    "${SQLCMD[@]}" -d "${DB_NAME}" -i /scripts/01-create-app-user.sql -v AppLogin="${APP_LOGIN}" AppPassword="${APP_DB_PASSWORD}"
    "${SQLCMD[@]}" -d "${DB_NAME}" -i /scripts/04-create-migrator-user.sql -v MigratorLogin="${MIGRATOR_LOGIN}" MigratorPassword="${MIGRATOR_DB_PASSWORD}"
    echo "Base de datos y usuarios listos."
    ;;
  restringir)
    "${SQLCMD[@]}" -d "${DB_NAME}" -i /scripts/03-deny-immutable-tables.sql -v AppLogin="${APP_LOGIN}"
    echo "Tablas inmutables protegidas."
    ;;
  *)
    echo "Uso: dev-init.sh crear|restringir" >&2
    exit 2
    ;;
esac
