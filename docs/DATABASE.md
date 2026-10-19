# Base de datos por ambiente (US-16)

> **Para crear un ambiente nuevo** (Staging, Producción u otro) con un procedimiento repetible y verificable, ver [`DATABASE_PROVISIONING.md`](DATABASE_PROVISIONING.md). Este documento describe el modelo de seguridad y la configuración de la conexión.

Motor: **SQL Server** (ADR-002, pendiente de confirmación por el Product Owner).

## 1. Aislamiento por ambiente

Cada ambiente tiene su propia instancia o base de datos y credenciales propias. Nunca se reutiliza una cuenta entre ambientes.

| Ambiente | Base de datos | Login de aplicación | Origen de la cadena de conexión |
|---|---|---|---|
| Development | `SGTTesis_Dev` (LocalDB o contenedor) | Autenticación integrada de Windows o `sgt_app_dev` | `appsettings.Development.json` (sin contraseña) o `dotnet user-secrets` |
| Staging | `SGTTesis_Stg` (instancia propia) | `sgt_app_stg` | Variable de entorno / gestor de secretos |
| Production | `SGTTesis_Prod` (instancia propia) | `sgt_app_prod` | Variable de entorno / gestor de secretos |

Los nombres de base de datos son propuestas; deben ajustarse a la infraestructura institucional.

## 2. Cadena de conexión

La aplicación lee `ConnectionStrings:DefaultConnection`. **No se versiona ninguna credencial.**

```bash
# Desarrollo local
dotnet user-secrets init --project src/SGT.Tesis.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena>" --project src/SGT.Tesis.API

# Staging / Producción (variable de entorno del contenedor o servicio)
ConnectionStrings__DefaultConnection=<cadena-desde-el-gestor-de-secretos>
```

Si falta la cadena, la aplicación **no arranca** y el mensaje indica qué configurar. Tampoco arranca si la cadena usa una cuenta administrativa (`sa`, `dbo`, `admin`, `administrator`).

## 3. Mínimo privilegio (AC-16.2)

Hay dos identidades distintas por ambiente:

| Identidad | Uso | Permisos |
|---|---|---|
| `sgt_app_<ambiente>` | La API en ejecución | `db_datareader` y `db_datawriter`. **Sin** `db_owner`, `db_ddladmin` ni `sysadmin`. |
| `sgt_migrator_<ambiente>` | Aplicar migraciones desde el pipeline | Permisos DDL; solo la usa el pipeline de despliegue, nunca la API. |

Pasos:

1. Crear el usuario de la aplicación con [`scripts/db/01-create-app-user.sql`](../scripts/db/01-create-app-user.sql), pasando login y contraseña por variables de `sqlcmd` (la contraseña viene del gestor de secretos).
2. Conectarse **como** ese usuario y ejecutar [`scripts/db/02-verify-app-user.sql`](../scripts/db/02-verify-app-user.sql). Los indicadores `DebeSer0` deben dar 0 y `DebeSer1` deben dar 1.
3. Cuando existan las tablas inmutables (`Decision`, `RegistroAuditoria`) se aplica `DENY UPDATE, DELETE` (AC-11.6).

## 4. Timeout y reintentos (AC-16.3)

Configurables en la sección `Database` (valores por defecto en `appsettings.Staging.json` y `appsettings.Production.json`):

| Clave | Valor por defecto | Rango válido |
|---|---|---|
| `Database:CommandTimeoutSeconds` | 30 | 5–300 |
| `Database:MaxRetryCount` | 5 | 0–10 |
| `Database:MaxRetryDelaySeconds` | 10 | 1–60 |

Además, la cadena de conexión debe incluir `Connect Timeout` (la de desarrollo usa 15 s). Los reintentos usan la estrategia de EF Core para SQL Server, que solo reintenta errores transitorios.

Verificación manual de resiliencia en Staging: detener la base de datos unos segundos durante una petición y comprobar en los logs que se reintenta y se recupera sin error; con una caída mayor al tiempo total de reintentos, la petición debe fallar de forma controlada.

## 5. Estado de los criterios de aceptación

| Criterio | Estado |
|---|---|
| AC-16.1 Instancias separadas con credenciales distintas | **Requiere acción de infraestructura** (crear instancias y secretos) |
| AC-16.2 Usuario sin `db_owner` | Script y verificación listos; **falta ejecutarlos** en cada ambiente. La API además rechaza cuentas administrativas. |
| AC-16.3 Timeout y reintentos | Implementado y probado en código; falta la prueba manual de caída en Staging |
| AC-16.4 Sin cadenas reales versionadas | Implementado y verificado por pruebas automáticas |
