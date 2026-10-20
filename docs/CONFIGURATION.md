# Configuración por ambiente

Las claves se leen de `appsettings.json`, `appsettings.<Ambiente>.json` y variables de entorno (estas últimas tienen prioridad; `__` equivale a `:`). **Ningún secreto se versiona.**

| Clave | Variable de entorno | Obligatoria en | Descripción |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Todos | Cadena de conexión de SQL Server. Ver `docs/DATABASE.md`. |
| `AllowedHosts` | `AllowedHosts` | Staging y Producción | Hosts públicos de la API separados por `;`. No admite `*` fuera de Development; si no se configura, la aplicación no arranca. |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`… | Si hay frontend en otro origen | Lista blanca de orígenes. Vacía = ningún origen externo. No admite `*`. |
| `Database:CommandTimeoutSeconds` | `Database__CommandTimeoutSeconds` | No (30) | Entre 5 y 300. |
| `Database:MaxRetryCount` | `Database__MaxRetryCount` | No (5) | Entre 0 y 10. |
| `Database:MaxRetryDelaySeconds` | `Database__MaxRetryDelaySeconds` | No (10) | Entre 1 y 60. |
| `Jwt:Issuer` / `Jwt:Audience` | `Jwt__Issuer` / `Jwt__Audience` | Todos | Emisor y audiencia de los tokens (p. ej. `sgt-tesis-api` / `sgt-tesis-clientes`). |
| `Jwt:SigningKey` | `Jwt__SigningKey` | Todos | **Secreto.** Clave HMAC de al menos 32 bytes. Distinta por ambiente. Nunca se versiona; la app no arranca sin ella. |
| `Jwt:AccessTokenMinutes` | `Jwt__AccessTokenMinutes` | No (15) | Entre 1 y 60. |
| `Jwt:RefreshTokenDays` | `Jwt__RefreshTokenDays` | No (7) | Entre 1 y 30. |
| `Bootstrap:AdminCorreo` / `Bootstrap:AdminPassword` | `Bootstrap__AdminCorreo` / `Bootstrap__AdminPassword` | Solo el primer arranque | Crea el primer Administrador si no existe ninguno. **Retirar las variables después.** |
| `RateLimit:LoginPermitPerMinute` | `RateLimit__LoginPermitPerMinute` | No (10) | Solicitudes por minuto e IP a login y refresh. |
| `Storage:RootPath` | `Storage__RootPath` | Todos | Carpeta de los archivos cargados. Fuera del repositorio, con volumen persistente y respaldo en Staging/Producción. La app no arranca sin ella. |
| `Documentos:MaxBytes` | `Documentos__MaxBytes` | No (20 MB) | Tamaño máximo por archivo; entre 1 KB y 100 MB. |
| `Documentos:Requisitos:<Etapa>:<n>` | `Documentos__Requisitos__Estudiante__0` | No | Categorías obligatorias para enviar la etapa (p. ej. `Propuesta firmada`). **Por defecto** (`appsettings.json`, decisión del Product Owner 2026-10-20): `Propuesta` → `Propuesta` y `Pasantia` → `Pasantía`; las demás etapas no exigen documentos. Una entrada vacía (`Documentos__Requisitos__Propuesta__0=`) anula el requisito por defecto. `<Etapa>`: Estudiante, Propuesta, Consejo, Evaluacion, Pasantia, Articulo o RequisitosFinales. |
| `Email:Modo` | `Email__Modo` | Todos | `Smtp` (correo real) o `Log` (solo registra; **únicamente Development**). Fuera de Development la app no arranca con `Log`. |
| `Email:Host`, `Email:Port`, `Email:UseStartTls`, `Email:User`, `Email:FromAddress`, `Email:FromName` | `Email__Host`… | Con `Smtp` | Servidor SMTP (STARTTLS por defecto, puerto 587). `Host` y `FromAddress` son obligatorios. |
| `Email:Password` | `Email__Password` | Si el servidor autentica | **Secreto.** Nunca se versiona. |
| `Email:MaxIntentos`, `Email:EsperaBaseSegundos`, `Email:IntervaloSegundos`, `Email:LoteMaximo`, `Email:WorkerHabilitado` | `Email__…` | No (5, 60, 30, 20, true) | Reintentos con espera exponencial, frecuencia del proceso y tamaño de lote. |
| `Dashboard:KpisCacheSegundos` | `Dashboard__KpisCacheSegundos` | No (30) | Vigencia en memoria de los KPIs (0 = sin caché; máx. 600). La cola de validación nunca se cachea. |
| `Flujo:Etapas:<Etapa>:PermiteReintento` | `Flujo__Etapas__Pasantia__PermiteReintento` | No (`true`, salvo Pasantia y Articulo: `false`) | Si rechazar la etapa permite corregir y reenviar (la tesis sigue en curso) o es definitivo (la tesis queda rechazada). Ver `docs/PROCESO_DE_ETAPAS.md`. |
| `Flujo:Etapas:<Etapa>:MaxEvaluadores` | `Flujo__Etapas__Consejo__MaxEvaluadores` | No (3) | Tope de evaluadores activos por etapa (1 a 10). |
| `Flujo:Etapas:<Etapa>:ExigeUnanimidad` | `Flujo__Etapas__Consejo__ExigeUnanimidad` | No (`true`) | Si aprobar exige un veredicto *Favorable* de todos los evaluadores (se aplica con las rondas). |
| `ForwardedHeaders:Habilitado`, `ForwardedHeaders:RedesConfiables` | `ForwardedHeaders__Habilitado`, `ForwardedHeaders__RedesConfiables__0` | Detrás de un proxy inverso | Con `true` exige las redes (CIDR) del proxy y solo confía en `X-Forwarded-For/Proto` de ellas. Desactivado por defecto. |
| `Frontend:UrlRestablecerPassword` | `Frontend__UrlRestablecerPassword` | Recomendada | Plantilla de la pantalla de restablecimiento, con `{token}` (p. ej. `https://app.institucion.edu/restablecer?token={token}`). Vacía: el correo incluye solo el código. Debe ser http(s) absoluta. |
| `Bootstrap:ExigirCambioPassword` | `Bootstrap__ExigirCambioPassword` | No (true) | Obliga al Administrador inicial a cambiar la contraseña en su primer acceso. |
| `Serilog:*` | `Serilog__MinimumLevel__Default` | No | Nivel y destinos de log (consola por defecto). |

## Comportamiento transversal de la API

- Toda excepción no controlada se devuelve como `ProblemDetails` (RFC 9457) con `traceId` y sin detalles internos; las reglas de negocio incumplidas (`DomainException`) responden 422.
- Fuera de Development se activa HSTS; siempre hay redirección a HTTPS.
- Cabeceras de seguridad en toda respuesta: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy`, `Cross-Origin-Resource-Policy`.
- `GET /health/live` indica que el proceso está vivo (sin consultar la base de datos).
- Swagger y OpenAPI solo se exponen en Development.

## Autenticación

- `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh` y `POST /api/v1/auth/logout`.
- Access token JWT (HS256) con solo `sub`, `role`, `jti` y vigencia; los refresh tokens rotan en cada uso y solo se guardan como hash SHA-256.
- Cinco intentos fallidos consecutivos bloquean la cuenta 15 minutos.

Primer arranque (desarrollo local):

```bash
dotnet user-secrets set "Jwt:SigningKey" "<cadena-aleatoria-de-32-o-mas-caracteres>" --project src/SGT.Tesis.API
dotnet user-secrets set "Jwt:Issuer" "sgt-tesis-api" --project src/SGT.Tesis.API
dotnet user-secrets set "Jwt:Audience" "sgt-tesis-clientes" --project src/SGT.Tesis.API
dotnet user-secrets set "Bootstrap:AdminCorreo" "admin@institucion.edu" --project src/SGT.Tesis.API
dotnet user-secrets set "Bootstrap:AdminPassword" "<contraseña-inicial>" --project src/SGT.Tesis.API
dotnet ef database update --project src/SGT.Tesis.Infrastructure --startup-project src/SGT.Tesis.API
```

## Gestión de usuarios (solo Administrador)

- `POST /api/v1/usuarios`, `GET /api/v1/usuarios?page&pageSize&rol&activo`, `GET /api/v1/usuarios/{id}`, `PATCH /api/v1/usuarios/{id}/estado` y `PATCH /api/v1/usuarios/{id}/rol`.
- Política de contraseña: mínimo 12 caracteres con mayúscula, minúscula, número y símbolo.
- Correo único (sin distinguir mayúsculas). No se puede desactivar ni cambiar el rol del propio usuario, ni dejar al sistema sin Administrador activo.
- Desactivar un usuario revoca sus refresh tokens y, además, el estado y el rol se verifican en cada petición: un access token vigente de una cuenta desactivada o con rol cambiado deja de valer de inmediato.

## Documentos

- `POST /api/v1/tesis/{id}/etapas/{1-7}/documentos` (multipart: `categoria` y `archivo`), `GET` de la misma ruta para listar y `GET /api/v1/documentos/{id}/contenido` para descargar.
- Solo PDF y DOCX, verificados por el **contenido** (firma; un DOCX debe ser un paquete Office real) y por la extensión. Otros tipos responden 415; el exceso de tamaño, 413.
- El archivo se guarda con nombre GUID (nunca el original), con hash SHA-256 en la base de datos. Subir de nuevo la misma categoría crea una versión nueva y conserva las anteriores.
- Solo se admiten documentos mientras la etapa está Pendiente u Observada (en revisión, aprobada o rechazada → 409).
- Descarga con `Content-Disposition: attachment` y `X-Content-Type-Options: nosniff`; la ven el dueño y el Administrador (los evaluadores asignados, con US-09).
- No hay antivirus; si la institución lo exige, se integra detrás de `IFileStorage` antes de guardar.

## Evaluación y decisiones

- **Asignaciones** (solo Administrador): `POST|GET /api/v1/tesis/{id}/etapas/{n}/evaluadores` y `DELETE .../evaluadores/{evaluadorId}`. El asignado debe ser un Evaluador activo; una asignación activa por evaluador y etapa (índice único filtrado). Al asignar, el evaluador ve la tesis y sus documentos; al retirarlo, deja de verlos.
- **Veredictos** (solo el Evaluador asignado, con la etapa en revisión): `PUT .../etapas/{n}/veredicto` crea una **versión nueva** y conserva las anteriores; `GET .../veredictos[?soloVigentes=true]` (el Administrador ve todos; cada Evaluador, los suyos). Un veredicto **nunca** cambia el estado de la etapa.
- **Decisiones** (solo Administrador): `POST .../etapas/{n}/decisiones` con `Aprobada`, `Observada` o `Rechazada`.
  - Solo desde `EnRevision` y sobre la etapa actual; observar y rechazar exigen justificación (≤ 2000).
  - `Aprobada` abre la etapa siguiente en `Pendiente`; tras la 7.ª la tesis queda `Finalizada`. `Observada` devuelve la etapa al estudiante, que corrige y reenvía. `Rechazada` rechaza la tesis (el estudiante puede registrar otra).
  - La decisión y el cambio de estado se guardan en la misma transacción. La concurrencia es optimista (`Etapa.Revision`): la segunda de dos decisiones simultáneas recibe 409.
  - Las decisiones son **inmutables**: la aplicación rechaza modificarlas o borrarlas y `scripts/db/03-deny-immutable-tables.sql` lo impide también en la base de datos.
  - La tesis expone su línea de tiempo de decisiones con justificación (la ve el estudiante).

## Auditoría

- `GET /api/v1/auditoria` (solo Administrador) con filtros `tesisId`, `actorId`, `accion` (coincidencia por prefijo, p. ej. `Auth.`), `desde`, `hasta` y paginación (`page`, `pageSize` ≤ 100); orden: más reciente primero.
- Cada registro guarda actor, acción, entidad, tesis, fecha UTC, IP, identificador de correlación y un detalle breve. Se escribe **en la misma transacción** que el cambio de negocio y es **inmutable** (la aplicación rechaza modificarlo o borrarlo; `scripts/db/03-deny-immutable-tables.sql` lo refuerza en la base de datos).
- Eventos: `Auth.Login`, `Auth.LoginFallido`, `Auth.LoginRechazado`, `Auth.CuentaBloqueada`, `Auth.RefreshReutilizado`, `Auth.Logout`, `Usuario.Creado/Activado/Desactivado/RolCambiado/AdminInicialCreado`, `Tesis.Registrada`, `Etapa.Enviada`, `Documento.Cargado`, `Evaluador.Asignado/Retirado`, `Veredicto.Emitido` y `Decision.Registrada`.
- No se guardan contraseñas, tokens ni el correo de intentos con usuarios desconocidos.
- **Correlación:** cada respuesta incluye `X-Correlation-Id` (se acepta el de la solicitud si es `[A-Za-z0-9_-]{1,64}`, si no se genera uno); también aparece en los logs.
- No hay política de retención ni archivado de la auditoría (a definir con la institución).

## Notificaciones por correo (outbox)

- Los correos se **encolan en la misma transacción** que el cambio de negocio (tabla `Notificaciones`) y un proceso en segundo plano los envía; si el servidor de correo falla, la operación original **no se revierte** y el envío se reintenta con espera exponencial (60 s, 120 s, 240 s…) hasta `Email:MaxIntentos`, tras lo cual queda `Fallida` (revisar la tabla; sin interfaz de administración todavía).
- Eventos: etapa enviada a revisión → administradores activos; evaluador asignado → el evaluador; decisión (aprobada, observada o rechazada) → el estudiante.
- **Privacidad:** los correos solo mencionan la etapa y el tipo de evento; nunca el título de la tesis, nombres de personas ni el texto de justificaciones o veredictos (se consultan dentro del sistema).
- Ejecutar **una sola instancia** del proceso de envío por despliegue (no hay bloqueo entre instancias; con varias podrían duplicarse correos).
- Pendiente: enlace directo a la tesis en el correo (falta definir la URL pública del frontend) y plantillas HTML.

## Retroalimentación consolidada

`GET /api/v1/tesis/{id}/retroalimentacion` (dueño y Administrador): por etapa, las decisiones con su justificación y, **una vez decidida la etapa**, el veredicto vigente de cada evaluador de forma **anónima** (`Evaluador 1`, `Evaluador 2`…, sin identificadores ni correos). Antes de la decisión no se muestran veredictos.

## Dashboard del Administrador

- `GET /api/v1/dashboard/kpis[?diasEstancamiento=15]` (1–365): totales de tesis por estado; tesis en curso por etapa; tiempo promedio en días por etapa cerrada; decisiones (total, aprobadas, observadas, rechazadas y sus tasas en %); etapas en revisión y **estancadas** (más de N días esperando validación); carga por evaluador (asignaciones activas en etapas en revisión y veredictos pendientes).
- `GET /api/v1/dashboard/cola-validacion[?page&pageSize]` (≤ 100): etapas en revisión de la que más tiempo lleva esperando a la más reciente, con estudiante, título, días en revisión, evaluadores asignados y cuántos ya emitieron veredicto.
- `GET /api/v1/dashboard/kpis/exportar[?diasEstancamiento&separador]` y `GET /api/v1/dashboard/cola-validacion/exportar[?separador]` (FR-16): descargas **CSV** (UTF-8 con marca de orden, `Content-Disposition: attachment`). Los indicadores salen en formato largo (`Sección, Concepto, Métrica, Valor`) listo para una tabla dinámica; la cola, completa (hasta 10 000 filas). `separador`: `Coma` (por defecto, RFC 4180) o `PuntoYComa` (con coma decimal, lo que espera Excel en configuración regional hispana). Las celdas que empiezan con `= + - @` se neutralizan con un apóstrofo (inyección de fórmulas) y cada exportación queda en la auditoría como `Reporte.Exportado`.
- `GET /api/v1/auth/yo`: perfil de la persona autenticada (`id, nombres, apellidos, correo, rol, debeCambiarPassword`). Se permite aun con contraseña temporal, para que el cliente decida a dónde redirigir.
- `GET /api/v1/evaluaciones/mias[?soloPendientes&page&pageSize]` (Evaluador): sus asignaciones activas con tesis, estudiante, etapa, estado y si ya emitió veredicto; `soloPendientes` deja las etapas en revisión sin veredicto. Primero las que están en revisión, más antiguas primero.
- `GET /api/v1/tesis[?estado&etapa&texto&page&pageSize]` (Administrador): listado paginado con la etapa vigente; `texto` busca en título y en nombre o correo del estudiante.
- Solo Administrador. Se calculan con proyecciones (sin cargar entidades completas); solo el promedio de días se calcula en memoria sobre dos fechas por etapa cerrada.
- Rendimiento: la prueba de humo con 10 000 tesis responde en menos de 2 s sobre SQLite en memoria. El objetivo p95 < 800 ms debe medirse contra SQL Server con pruebas de carga (Sprint 8). Si el volumen crece, conviene materializar los promedios.
- `Etapa.EnRevisionDesde` registra el último envío a revisión (las etapas ya en revisión antes de esta versión usan su fecha de apertura).

## Pruebas contra SQL Server

Las pruebas de integración usan SQLite en memoria salvo que se defina `SGT_TEST_SQLSERVER` (cadena de conexión a un SQL Server con permiso para crear bases de datos). En ese modo cada instancia de la API de prueba crea su base `sgt_test_<guid>` y la elimina al terminar. El job `pruebas-sql-server` del CI lo hace con un contenedor efímero. Una prueba adicional aplica todas las migraciones a una base nueva y verifica tablas, índices únicos filtrados y la ausencia de columnas de texto ilimitadas; otra (también en modo SQLite) falla si el modelo cambia sin su migración.

**Diferencias entre SQLite y SQL Server que ya encontraron un defecto real:** el desempate por `Guid` no sigue el orden de creación en SQL Server; por eso los eventos de auditoría de una misma solicitud llevan fechas estrictamente crecientes.

## Disponibilidad (health checks)

- `GET /health/live`: el proceso responde (no consulta dependencias).
- `GET /health/ready`: además la **base de datos** responde (tope 5 s) y la carpeta de **archivos** es escribible. Responde `200 {"status":"Healthy"}` o `503 {"status":"Unhealthy"}`; los motivos solo quedan en el log, nunca en la respuesta.
- La imagen Docker incluye `HEALTHCHECK` (`dotnet SGT.Tesis.API.dll --healthcheck`, sin curl). Si usa `AllowedHosts`, el comprobador envía el primer host configurado.
- Use `live` para reiniciar el contenedor y `ready` para decidir si recibe tráfico.

## Contenedores

- `Dockerfile` con cuatro destinos: `runtime` (API, usuario sin privilegios, puerto 8080, volumen `/data/documentos`), `migrator` (ejecutable `efbundle` para aplicar migraciones con una cuenta DDL distinta de la de la API), `dbtools` (sqlcmd + `scripts/db`) y la compilación intermedia.
- `docker-compose.yml` levanta un entorno **local**: SQL Server 2022, creación de la base y de los usuarios de mínimo privilegio, migraciones, protección de las tablas inmutables, la API y Mailpit (correos de prueba en http://localhost:8025). Copie `.env.example` a `.env`, complete los secretos y ejecute `docker compose up --build`. No es una configuración de producción.
- El puerto local de SQL Server es 14330 (`DB_HOST_PORT`) para no chocar con una instancia instalada en 1433.

## Prueba de carga (k6)

`k6 run -e BASE_URL=… -e ADMIN_CORREO=… -e ADMIN_PASSWORD=… tests/load/dashboard.js` (variables `VUS`, `DURACION`, `PENSAR`). Para 10 000 tesis cargue antes `scripts/load/seed-10000.sql` en una base **desechable**.

Resultado medido el 2026-10-19 contra **SQL Server 2022 en Docker** (portátil, 4 vCPU compartidas con k6 y la API), 10 000 tesis, p95 en ms:

| Escenario | KPIs | Cola de validación |
|---|---:|---:|
| Sin caché, 10 usuarios simultáneos | 760 | 336 |
| Sin caché, 20 usuarios simultáneos | 1 420 | 614 |
| **Caché 30 s (por defecto), 20 usuarios** | **270** | **597** |
| Caché 30 s, 50 usuarios | 361 | 797 |

Con un usuario, los KPIs tardan ~120–250 ms. El objetivo p95 < 800 ms (AC-14.3) se cumple hasta ~20 usuarios simultáneos del panel; con 50, la cola queda en el límite. Es una medición en un portátil: repetirla en el hardware real de Staging antes de salir a producción.

## Cambio y recuperación de contraseña

| Endpoint | Acceso | Qué hace |
|---|---|---|
| `POST /api/v1/auth/cambiar-password` `{passwordActual, passwordNueva}` | Sesión iniciada | Verifica la actual (con el mismo bloqueo por intentos fallidos que el login), exige la política de contraseña y que la nueva sea distinta, **cierra todas las sesiones** y avisa por correo. 204; el cliente debe iniciar sesión de nuevo. |
| `POST /api/v1/auth/olvide-password` `{correo}` | Anónimo (con límite de solicitudes) | Envía un enlace (o código) de un solo uso. Responde **202 siempre**, exista o no la cuenta. Máximo una solicitud por minuto y cuenta; un enlace nuevo invalida el anterior. |
| `POST /api/v1/auth/restablecer-password` `{token, passwordNueva}` | Anónimo (con límite de solicitudes) | Cambia la contraseña con el token, levanta bloqueos, cierra todas las sesiones y avisa por correo. Token inválido, vencido o ya usado: 400 genérico. |

- **Token:** 48 bytes aleatorios; solo se guarda su **hash**; vence a los **30 minutos** y es de **un solo uso**. El correo que lo contiene se marca como *sensible*: su cuerpo se **borra de la base de datos** al enviarse (o al agotar los intentos).
- **Contraseña temporal:** las cuentas creadas por el Administrador (y el Administrador inicial, salvo `Bootstrap:ExigirCambioPassword=false`) tienen `DebeCambiarPassword`. El login lo indica (`debeCambiarPassword: true`) y mientras dure **toda la API responde 403** con `codigo: "cambio_password_obligatorio"`, salvo `cambiar-password`, `logout` y `yo`.
- Eventos de auditoría: `Auth.PasswordCambiada`, `Auth.PasswordRestablecimientoSolicitado` y `Auth.PasswordRestablecida`.
- Sin `Frontend:UrlRestablecerPassword` el correo trae el código en texto: aceptable en desarrollo, pero en producción conviene el enlace al frontend.
