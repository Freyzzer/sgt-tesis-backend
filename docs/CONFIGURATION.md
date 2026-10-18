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
