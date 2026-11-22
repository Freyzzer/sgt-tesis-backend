#!/usr/bin/env bash
# Aprovisiona la base de datos de UN ambiente (Development, Staging o Producción) de forma repetible.
# Funciona contra cualquier SQL Server (en la nube, en un servidor propio o en un contenedor). Es idempotente:
# se puede ejecutar de nuevo sin dañar lo existente.
#
# Orden de uso (ver docs/DATABASE_PROVISIONING.md):
#   1) preparar    crea la base de datos (si procede) y los usuarios de la aplicación y de migraciones
#   2) (migraciones: imagen sgt-tesis-migrator o `dotnet ef database update`, con la cuenta de migraciones)
#   3) finalizar   restringe las tablas inmutables y verifica que el ambiente quedó correcto
#
# Variables (las contraseñas llegan por entorno; nunca se escriben en archivos ni en el repositorio):
#   DB_HOST                  servidor (obligatoria)
#   DB_PORT                  puerto (1433)
#   DB_NAME                  base de datos del ambiente, p. ej. SGTTesis_Staging (obligatoria)
#   ADMIN_USER               cuenta administrativa del servidor (sa)
#   ADMIN_PASSWORD           su contraseña (obligatoria; también se acepta MSSQL_SA_PASSWORD)
#   APP_LOGIN, APP_PASSWORD  usuario de la API  (APP_DB_PASSWORD también se acepta)
#   MIGRATOR_LOGIN, MIGRATOR_PASSWORD  usuario de migraciones (MIGRATOR_DB_PASSWORD también se acepta)
#   PLATFORM                 sqlserver (por defecto) | azuresql
#   TRUST_SERVER_CERT        true para certificados autofirmados (solo desarrollo); false por defecto
#   CREATE_DB                true | false (por defecto true en sqlserver; en azuresql la base se crea en el portal)
set -euo pipefail

PLATFORM="${PLATFORM:-sqlserver}"
DB_PORT="${DB_PORT:-1433}"
ADMIN_USER="${ADMIN_USER:-sa}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-${MSSQL_SA_PASSWORD:-}}"
APP_PASSWORD="${APP_PASSWORD:-${APP_DB_PASSWORD:-}}"
MIGRATOR_PASSWORD="${MIGRATOR_PASSWORD:-${MIGRATOR_DB_PASSWORD:-}}"
TRUST_SERVER_CERT="${TRUST_SERVER_CERT:-false}"
SCRIPTS_DIR="${SCRIPTS_DIR:-/scripts}"

if [[ "${PLATFORM}" == "azuresql" ]]; then
  CREATE_DB="${CREATE_DB:-false}"
else
  CREATE_DB="${CREATE_DB:-true}"
fi

falta() { echo "Falta la variable $1." >&2; exit 2; }
[[ -n "${DB_HOST:-}" ]] || falta DB_HOST
[[ -n "${DB_NAME:-}" ]] || falta DB_NAME
[[ -n "${ADMIN_PASSWORD}" ]] || falta ADMIN_PASSWORD
[[ "${PLATFORM}" == "sqlserver" || "${PLATFORM}" == "azuresql" ]] || { echo "PLATFORM debe ser sqlserver o azuresql." >&2; exit 2; }

SQLCMD_BIN="$(command -v sqlcmd || true)"
[[ -n "${SQLCMD_BIN}" ]] || SQLCMD_BIN=/opt/mssql-tools18/bin/sqlcmd
[[ -x "${SQLCMD_BIN}" ]] || { echo "No se encontró sqlcmd. Use la imagen 'dbtools' del Dockerfile." >&2; exit 2; }

sql() { # sql <base> <archivo> [variables -v...]
  local base="$1" archivo="$2"; shift 2
  local trust=(); [[ "${TRUST_SERVER_CERT}" == "true" ]] && trust=(-C)
  "${SQLCMD_BIN}" "${trust[@]}" -b -S "${DB_HOST},${DB_PORT}" -U "${ADMIN_USER}" -P "${ADMIN_PASSWORD}" \
    -d "${base}" -i "${archivo}" "$@"
}

comando="${1:-}"
case "${comando}" in
  preparar)
    [[ -n "${APP_LOGIN:-}" ]] || falta APP_LOGIN
    [[ -n "${APP_PASSWORD}" ]] || falta APP_PASSWORD
    [[ -n "${MIGRATOR_LOGIN:-}" ]] || falta MIGRATOR_LOGIN
    [[ -n "${MIGRATOR_PASSWORD}" ]] || falta MIGRATOR_PASSWORD
    [[ "${APP_LOGIN}" != "${MIGRATOR_LOGIN}" ]] || { echo "La cuenta de la API y la de migraciones deben ser distintas." >&2; exit 2; }

    if [[ "${CREATE_DB}" == "true" ]]; then
      sql master "${SCRIPTS_DIR}/00-create-database.sql" -v DbName="${DB_NAME}"
    else
      echo "Se omite la creación de la base: debe existir (${DB_NAME})."
    fi

    if [[ "${PLATFORM}" == "azuresql" ]]; then
      # En Azure SQL Database los logins se crean en master y los usuarios en la base.
      sql master "${SCRIPTS_DIR}/azure/01-create-logins.sql" \
        -v AppLogin="${APP_LOGIN}" AppPassword="${APP_PASSWORD}" MigratorLogin="${MIGRATOR_LOGIN}" MigratorPassword="${MIGRATOR_PASSWORD}"
      sql "${DB_NAME}" "${SCRIPTS_DIR}/azure/02-create-users.sql" -v AppLogin="${APP_LOGIN}" MigratorLogin="${MIGRATOR_LOGIN}"
    else
      sql "${DB_NAME}" "${SCRIPTS_DIR}/01-create-app-user.sql" -v AppLogin="${APP_LOGIN}" AppPassword="${APP_PASSWORD}"
      sql "${DB_NAME}" "${SCRIPTS_DIR}/04-create-migrator-user.sql" -v MigratorLogin="${MIGRATOR_LOGIN}" MigratorPassword="${MIGRATOR_PASSWORD}"
    fi
    echo "Base de datos y usuarios listos. Siguiente paso: aplicar las migraciones con la cuenta ${MIGRATOR_LOGIN}."
    ;;

  finalizar)
    [[ -n "${APP_LOGIN:-}" ]] || falta APP_LOGIN
    [[ -n "${MIGRATOR_LOGIN:-}" ]] || falta MIGRATOR_LOGIN
    sql "${DB_NAME}" "${SCRIPTS_DIR}/03-deny-immutable-tables.sql" -v AppLogin="${APP_LOGIN}"
    sql "${DB_NAME}" "${SCRIPTS_DIR}/05-verify-environment.sql" -v AppLogin="${APP_LOGIN}" MigratorLogin="${MIGRATOR_LOGIN}"
    echo "Ambiente ${DB_NAME} aprovisionado y verificado."
    ;;

  verificar)
    [[ -n "${APP_LOGIN:-}" ]] || falta APP_LOGIN
    [[ -n "${MIGRATOR_LOGIN:-}" ]] || falta MIGRATOR_LOGIN
    sql "${DB_NAME}" "${SCRIPTS_DIR}/05-verify-environment.sql" -v AppLogin="${APP_LOGIN}" MigratorLogin="${MIGRATOR_LOGIN}"
    ;;

  *)
    echo "Uso: provision.sh preparar | finalizar | verificar   (ver el encabezado del script)" >&2
    exit 2
    ;;
esac
