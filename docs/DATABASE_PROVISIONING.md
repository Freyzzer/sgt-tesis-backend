# Aprovisionamiento de la base de datos por ambiente

> **Estado:** la institución aún **no ha decidido dónde se alojarán** las bases de datos de Staging y Producción. Este documento reúne las opciones y deja un procedimiento **repetible y verificable** para crear cada ambiente en cuanto se decida, sin importar la plataforma. Cierra, junto con la decisión, el issue #16 (US-16).

## 1. Qué ya está resuelto

| Pieza | Dónde | Estado |
|---|---|---|
| Modelo de seguridad: usuario de la API con solo lectura/escritura, cuenta distinta para migraciones, tablas inmutables protegidas | `scripts/db/` | Probado contra SQL Server 2022 real |
| Script único y repetible por ambiente (`provision.sh`) | `scripts/db/provision.sh` | Probado (ver sección 6) |
| Verificación automática del ambiente (falla si algo no cumple) | `scripts/db/05-verify-environment.sql` | Probada, incluidos los casos de fallo |
| Migraciones aplicables con la cuenta de migraciones | imagen `sgt-tesis-migrator` | Probada |
| Variantes para **Azure SQL Database** | `scripts/db/azure/` | **No probadas** (no se creó ningún recurso en Azure) |

Lo único que falta es **elegir la plataforma y crear el servidor**; todo lo demás se ejecuta con los mismos comandos en cualquier ambiente.

## 2. Opciones de alojamiento

Datos tomados de la documentación oficial de Microsoft (consultada el 2026-10-19; las condiciones pueden cambiar).

| Opción | Costo de licencia | Límites relevantes | Adecuada para |
|---|---|---|---|
| **Servidor SQL Server de la institución** | Ya cubierto por su licenciamiento | Los de su edición | **Producción** (lo más probable) |
| **Azure SQL Database (oferta gratuita)** | Gratis de por vida | 100 000 vCore-segundos al mes, 32 GB por base, hasta 10 bases por suscripción; **sin SLA**; se auto-pausa al agotar el límite y se reanuda lento | Desarrollo y Staging; **no** recomendada para Producción |
| **Azure SQL Database (pago)** | Por uso (serverless o aprovisionado) | Escalable; con SLA, respaldos y recuperación a un punto en el tiempo | Producción si la institución usa Azure |
| **SQL Server Express** | Gratis, **permitido en producción** | 10 GB por base (50 GB en SQL Server 2025), 1,4 GB de RAM, 4 núcleos; **sin SQL Agent** (los respaldos se programan aparte); sin alta disponibilidad | Producción pequeña (decenas de usuarios simultáneos) |
| **SQL Server Developer** | Gratis, **prohibido en producción** | Todas las funciones de Enterprise | Desarrollo y Staging |
| **Contenedor de SQL Server** (`docker compose`) | Developer/Express según la imagen | Solo desarrollo local en este repositorio | Desarrollo local |

Datos del sistema para dimensionar: la base solo guarda **metadatos** (los documentos van a disco, no a la base). Con 10 000 tesis la base ocupa decenas de megabytes, así que el límite de 10 GB de Express no es el problema; sí lo son la memoria (1,4 GB) y la concurrencia.

### Recomendación por ambiente

| Ambiente | Recomendación |
|---|---|
| Development | Contenedor local (`docker compose up`) — ya funciona |
| Staging | **El mismo tipo de plataforma que Producción**, para que las pruebas (carga, restauración, migraciones) sean representativas. Si Producción será un servidor institucional, Staging debería ser otra instancia o base en ese servidor o en uno equivalente. |
| Production | Servidor institucional, o Azure SQL de pago. Evitar la oferta gratuita de Azure (sin SLA) y Express si se esperan más de unas decenas de usuarios simultáneos. |

### Requisitos que cualquier opción debe cumplir

- SQL Server 2019 o superior, o Azure SQL Database.
- Conexión cifrada (TLS) y acceso de red desde donde corre la API y el contenedor de migraciones.
- Una **base de datos y credenciales propias por ambiente**: nunca compartir cuentas ni la cuenta administrativa entre ambientes.
- Respaldos automáticos con restauración probada (RPO ≤ 24 h, RTO ≤ 4 h propuestos; ver `OPERATIONS.md`).
- Intercalación (collation) por defecto, no sensible a mayúsculas (la aplicación normaliza los correos a minúsculas).

## 3. Nombres propuestos por ambiente

| | Development | Staging | Production |
|---|---|---|---|
| Base de datos | `SGTTesis_Dev` | `SGTTesis_Stg` | `SGTTesis_Prod` |
| Usuario de la API | `sgt_app_dev` | `sgt_app_stg` | `sgt_app_prod` |
| Usuario de migraciones | `sgt_migrator_dev` | `sgt_migrator_stg` | `sgt_migrator_prod` |
| Contraseñas | distintas en cada ambiente y cuenta, guardadas en el gestor de secretos | | |

Ajustar los nombres a la convención de la institución; solo hay que usar los mismos en los comandos de abajo.

## 4. Procedimiento (idéntico en todos los ambientes)

### Requisitos previos

1. Un servidor/instancia SQL Server accesible y una cuenta **administrativa** del servidor (solo para este procedimiento).
2. Docker (recomendado) **o** las herramientas `sqlcmd` y el SDK de .NET.
3. Las imágenes del repositorio. Se publican en GHCR (`sgt-tesis-migrator`, `sgt-tesis-dbtools`) o se construyen localmente:

   ```bash
   docker build --target dbtools  -t sgt-tesis-dbtools  .
   docker build --target migrator -t sgt-tesis-migrator .
   ```

### Paso 1 — Preparar: base de datos y usuarios

```bash
docker run --rm \
  -e DB_HOST=<servidor> -e DB_NAME=SGTTesis_Stg \
  -e ADMIN_USER=<admin> -e ADMIN_PASSWORD=<secreto-admin> \
  -e APP_LOGIN=sgt_app_stg       -e APP_PASSWORD=<secreto-app> \
  -e MIGRATOR_LOGIN=sgt_migrator_stg -e MIGRATOR_PASSWORD=<secreto-migrator> \
  sgt-tesis-dbtools preparar
```

Crea la base (si no existe) y los dos usuarios con permisos mínimos. Es idempotente. En plataformas donde la base la crea el proveedor (por ejemplo Azure SQL), añadir `-e CREATE_DB=false` o `-e PLATFORM=azuresql`.

### Paso 2 — Migraciones (con la cuenta de migraciones, nunca la de la API)

```bash
docker run --rm sgt-tesis-migrator \
  --connection "Server=<servidor>;Database=SGTTesis_Stg;User Id=sgt_migrator_stg;Password=<secreto-migrator>;Encrypt=True"
```

Sin Docker: `dotnet ef database update --project src/SGT.Tesis.Infrastructure --connection "<misma cadena>"`.

### Paso 3 — Finalizar: restricciones y verificación

```bash
docker run --rm \
  -e DB_HOST=<servidor> -e DB_NAME=SGTTesis_Stg \
  -e ADMIN_USER=<admin> -e ADMIN_PASSWORD=<secreto-admin> \
  -e APP_LOGIN=sgt_app_stg -e MIGRATOR_LOGIN=sgt_migrator_stg \
  sgt-tesis-dbtools finalizar
```

Protege las tablas inmutables (decisiones y auditoría) y **verifica** el ambiente. Termina con código de salida distinto de cero y un mensaje claro si algo no cumple (por ejemplo, si la API fuera `db_owner`, si faltan los `DENY` o si no hay migraciones). `provision.sh verificar` repite solo la verificación y puede ejecutarse en cualquier momento, por ejemplo tras cada despliegue.

### Paso 4 — Configurar la API

Variables de entorno (valores desde el gestor de secretos; ver `CONFIGURATION.md`):

```
ConnectionStrings__DefaultConnection=Server=<servidor>;Database=SGTTesis_Stg;User Id=sgt_app_stg;Password=<secreto-app>;Encrypt=True;Connect Timeout=15
```

La API usa **siempre** la cuenta `sgt_app_*`. Después, el primer arranque con `Bootstrap__*` crea el Administrador inicial (ver `OPERATIONS.md`).

### Paso 5 — Respaldos y registro

Configurar los respaldos de la base de datos y del volumen de archivos (`OPERATIONS.md`, sección 5), realizar un primer simulacro de restauración y anotar en la tabla de la sección 7 el ambiente creado.

### Variantes

- **Azure SQL Database:** crear primero la base en el portal (en el servidor lógico propio del ambiente), luego `-e PLATFORM=azuresql`. Los logins se crean en `master` y los usuarios en la base (`scripts/db/azure/`). Esta variante **no se ha probado**: validarla primero en un servidor de pruebas. La oferta gratuita se auto-pausa; la API reintenta la conexión (5 intentos con espera), pero la primera petición tras una pausa puede tardar.
- **SQL Server con certificado autofirmado (solo desarrollo):** `-e TRUST_SERVER_CERT=true` y `TrustServerCertificate=True` en las cadenas. No usar en Staging ni Producción.
- **SQL Server Express:** igual que SQL Server. Programar los respaldos fuera de la base (no hay SQL Agent): por ejemplo, una tarea programada que ejecute `BACKUP DATABASE` con `sqlcmd`.
- **Rotar una contraseña:** cambiarla en el servidor (`ALTER LOGIN … WITH PASSWORD`), actualizar el gestor de secretos y reiniciar la API. No requiere repetir el procedimiento.

## 5. Qué entregar a la institución al decidir

1. Este documento y la carpeta `scripts/db/`.
2. Las imágenes `sgt-tesis-dbtools` y `sgt-tesis-migrator` (o el `Dockerfile` para construirlas).
3. La tabla de la sección 3 completada con sus nombres reales y la plataforma elegida por ambiente.
4. Un responsable de crear las cuentas administrativas y los secretos (no se comparten por correo ni se guardan en el repositorio).

## 6. Evidencia de verificación

El 2026-10-19 se aprovisionó una base **nueva** en un SQL Server 2022 (contenedor) con exactamente los pasos 1 a 3:

| Prueba | Resultado |
|---|---|
| `preparar` sobre una base inexistente | Base y dos usuarios creados |
| `verificar` antes de migrar | **Falla** (código 1): "No hay migraciones aplicadas", faltan las tablas |
| Migraciones con la cuenta de migraciones | Las 7 migraciones aplicadas |
| `verificar` tras migrar y antes de `finalizar` | **Falla**: "Falta DENY UPDATE/DELETE sobre Decisiones y RegistrosAuditoria" |
| `finalizar` | Correcto (código 0): usuarios de mínimo privilegio, tablas protegidas, migraciones aplicadas |
| `finalizar` repetido | Correcto (idempotente) |
| `DELETE FROM dbo.Decisiones` ejecutado como el usuario de la API | **Denegado** por la base de datos |

No probado: Azure SQL, SQL Server Express como servicio de Windows y cualquier servidor real de la institución.

## 7. Registro de ambientes (completar al crearlos)

| Ambiente | Plataforma | Servidor | Base | Fecha de creación | Responsable | Última verificación |
|---|---|---|---|---|---|---|
| Development | Contenedor local | `docker compose` | `SGTTesis_Dev` | — | cada desarrollador | — |
| Staging | _por definir_ | | | | | |
| Production | _por definir_ | | | | | |
