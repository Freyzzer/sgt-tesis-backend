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
| `Documentos:Requisitos:<Etapa>:<n>` | `Documentos__Requisitos__Estudiante__0` | No | Categorías obligatorias para enviar la etapa (p. ej. `Propuesta firmada`). Vacío hasta que el Product Owner las defina. `<Etapa>`: Estudiante, Propuesta, Consejo, Evaluacion, Pasantia, Articulo o RequisitosFinales. |
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
