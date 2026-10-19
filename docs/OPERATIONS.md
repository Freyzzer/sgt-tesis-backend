# Runbook de operación

Guía para quien despliega y opera el backend. Complementa [`CONFIGURATION.md`](CONFIGURATION.md) (variables) y [`DATABASE.md`](DATABASE.md) (base de datos).

> **Estado:** el despliegue a Staging y Producción **no está automatizado**: la plataforma de destino aún no está decidida. El pipeline entrega imágenes versionadas; estos pasos describen cómo usarlas en cualquier plataforma de contenedores. Ajustar a la elegida cuando se defina.

## 1. Artefactos

El flujo `Publicar imágenes` (GitHub Actions) publica en GitHub Container Registry:

| Imagen | Contenido | Etiquetas |
|---|---|---|
| `ghcr.io/<organización>/sgt-tesis` | API | `develop` (Staging), `main` (Producción), `X.Y.Z`, `sha-<commit>` |
| `ghcr.io/<organización>/sgt-tesis-migrator` | Ejecutable de migraciones (`efbundle`) | las mismas |

En producción se despliega siempre una etiqueta **inmutable** (`X.Y.Z` o `sha-…`), nunca `develop` ni `main`.

## 2. Requisitos previos por ambiente

1. **Base de datos** SQL Server dedicada al ambiente (ver `DATABASE.md`), con dos cuentas distintas:
   - `sgt_app_<ambiente>`: la API; solo `db_datareader` y `db_datawriter` (`scripts/db/01-create-app-user.sql`).
   - `sgt_migrator_<ambiente>`: solo el pipeline; `db_ddladmin` + lectura/escritura (`scripts/db/04-create-migrator-user.sql`).
2. **Secretos** en el gestor institucional (Key Vault o equivalente), inyectados como variables de entorno: `ConnectionStrings__DefaultConnection`, `Jwt__SigningKey`, `Email__Password`, `Bootstrap__AdminPassword` (solo el primer arranque). Distintos por ambiente.
3. **Volumen persistente** montado en `/data/documentos` (archivos cargados) con respaldo.
4. **Servidor SMTP** (`Email__Modo=Smtp`, `Email__Host`, `Email__FromAddress`).
5. **Proxy inverso o balanceador con TLS** delante de la API; activar `ForwardedHeaders__Habilitado=true` con `ForwardedHeaders__RedesConfiables__0=<CIDR del proxy>` y definir `AllowedHosts` con el host público.
6. `Cors__AllowedOrigins__0` con el origen del frontend.

La API **no arranca** si falta algo de lo anterior; el mensaje de error indica qué configurar.

## 3. Despliegue de una versión

1. Verificar que CI está en verde en el commit/etiqueta y elegir la etiqueta de imagen.
2. **Respaldar** la base de datos y el volumen de archivos (sección 5).
3. **Migraciones** (con la cuenta migrator, antes de arrancar la nueva versión de la API):

   ```bash
   docker run --rm ghcr.io/<org>/sgt-tesis-migrator:<etiqueta> \
     --connection "Server=<host>;Database=<base>;User Id=sgt_migrator_<amb>;Password=<secreto>;Encrypt=True"
   ```

   Si hay tablas nuevas inmutables o cambios de permisos, ejecutar también `scripts/db/03-deny-immutable-tables.sql` con una cuenta administrativa (`-v AppLogin=sgt_app_<amb>`). Es idempotente.
4. **Desplegar la API** con la nueva imagen y las variables de entorno. Esperar a que `GET /health/ready` responda 200.
5. **Verificar:** login de un usuario de prueba, `GET /api/v1/dashboard/kpis` como Administrador y revisar el log en busca de errores.
6. **Primer despliegue:** definir `Bootstrap__AdminCorreo` y `Bootstrap__AdminPassword`, arrancar, comprobar el login y **retirar** esas variables.

Estrategia de actualización: las migraciones son aditivas y compatibles hacia atrás cuando es posible, de modo que la versión anterior de la API siga funcionando mientras se actualiza (despliegue gradual). Una migración destructiva requiere ventana de mantenimiento y se anuncia en el PR.

## 4. Reversión (rollback)

- **Solo aplicación** (sin migración nueva o con migración compatible): volver a desplegar la etiqueta anterior.
- **Con migración incompatible:** restaurar la base de datos desde el respaldo previo al despliegue (sección 5) y desplegar la etiqueta anterior. EF Core permite revertir una migración (`dotnet ef database update <migración anterior>`), pero **no** recupera datos eliminados por ella: el respaldo es la garantía.
- Tras revertir, abrir un incidente (sección 8) y corregir hacia adelante.

## 5. Respaldo y restauración

Objetivos propuestos (a confirmar con la institución): **RPO ≤ 24 h**, **RTO ≤ 4 h**.

| Qué | Cómo | Frecuencia mínima |
|---|---|---|
| Base de datos | Respaldo completo diario + registros de transacciones o diferenciales según la plataforma; copia fuera del servidor | Diario, y siempre antes de un despliegue |
| Archivos (`/data/documentos`) | Instantánea del volumen o copia incremental | Diario |
| Secretos | Los guarda el gestor de secretos; documentar el procedimiento de recuperación | — |

La base de datos y los archivos deben restaurarse **al mismo punto en el tiempo**: la tabla `Documentos` guarda el nombre del archivo y su hash SHA-256.

Restauración:
1. Detener la API (o ponerla en mantenimiento).
2. Restaurar la base de datos y el volumen al mismo punto.
3. Arrancar la API y comprobar `GET /health/ready`.
4. Verificar un documento: descargarlo y comparar su SHA-256 con `Documentos.HashSha256`.

**Simulacro de restauración:** realizarlo al menos cada trimestre en Staging y registrar el resultado y el tiempo real (RTO).

## 6. Operación diaria

| Tema | Qué hacer |
|---|---|
| Salud | Monitorear `GET /health/ready` (200/503). `live` solo indica que el proceso responde. |
| Logs | Serilog a consola (texto) con `CorrelationId`; recolectar con la plataforma. El ID de correlación viaja en la cabecera `X-Correlation-Id` y se guarda en la auditoría. |
| Correos fallidos | Notificaciones con estado `Fallida` en la tabla `Notificaciones` (`UltimoError`): revisar el servidor SMTP y reintentar (cambiar su estado a `Pendiente` y `ProximoIntentoEn` a ahora). No hay interfaz de administración todavía. |
| Proceso de correos | Ejecutar **una sola instancia** de la API con el envío activo (o desactivarlo en las demás con `Email__WorkerHabilitado=false`). |
| Cuentas bloqueadas | Se desbloquean solas a los 15 minutos; el Administrador puede desactivar y reactivar un usuario. |
| Rotación de la clave JWT | Cambiar `Jwt__SigningKey` invalida los tokens de acceso vigentes (duran 15 min). Los refresh tokens no dependen de la clave: un cliente que renueve al recibir un 401 sigue sin interrupción; si no, los usuarios deberán iniciar sesión de nuevo. Planificar fuera de horario. |
| Rotación de contraseñas de base de datos | Cambiar la contraseña en SQL Server y en el gestor de secretos; reiniciar la API. |
| Auditoría | `GET /api/v1/auditoria`. No hay política de retención: acordar plazos con la institución. |

## 7. Capacidad y rendimiento

- Referencia medida (portátil, SQL Server en Docker, 10 000 tesis): p95 de 270 ms (KPIs) y 597 ms (cola) con 20 usuarios simultáneos del panel; ver `CONFIGURATION.md`.
- Antes de salir a producción: ejecutar `tests/load/dashboard.js` en Staging con el volumen esperado y registrar el resultado.
- La API es **sin estado** (los tokens son autocontenidos; los archivos están en el volumen compartido), por lo que puede escalarse horizontalmente. Requisitos: volumen compartido entre instancias o un almacenamiento de objetos (cambio en `IFileStorage`), una sola instancia con el proceso de correos activo, y recordar que la caché de KPIs es por instancia.

## 8. Incidentes

| Síntoma | Primera revisión |
|---|---|
| `GET /health/ready` = 503 | Log de la API (`Falló la verificación de la base de datos` o `…del almacenamiento`); conectividad y credenciales de la base de datos; volumen montado y con permisos de escritura. |
| La API no arranca | El mensaje indica la variable faltante o inválida (cadena de conexión, `Jwt__*`, `Storage__RootPath`, `AllowedHosts`, `Email__*`). |
| Muchos 401 tras un despliegue | ¿Cambió `Jwt__SigningKey` o `Jwt__Issuer/Audience`? Los usuarios deben volver a iniciar sesión. |
| Muchos 429 en login | Límite de solicitudes por IP (`RateLimit__LoginPermitPerMinute`); detrás de un proxy, comprobar `ForwardedHeaders` (si no, todos comparten la IP del proxy). |
| 409 en decisiones | Concurrencia optimista: otra decisión se registró primero; consultar la tesis y reintentar. |
| Correos que no llegan | Tabla `Notificaciones` (estado, intentos, error); configuración SMTP. |
| Sospecha de acceso indebido | Consultar `GET /api/v1/auditoria` (`Auth.LoginFallido`, `Auth.CuentaBloqueada`, `Auth.RefreshReutilizado`), desactivar la cuenta, rotar secretos si procede. |

Registrar cada incidente con: hora, impacto, causa, acción y seguimiento (post-mortem sin culpables).

## 9. Entorno local

`docker compose up --build` (ver `README`). Para medir carga: `scripts/load/seed-10000.sql` y `tests/load/dashboard.js` en una base desechable. No reutilizar jamás los valores de `.env.example` en otros ambientes.
