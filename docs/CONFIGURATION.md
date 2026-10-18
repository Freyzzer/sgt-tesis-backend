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
| `Serilog:*` | `Serilog__MinimumLevel__Default` | No | Nivel y destinos de log (consola por defecto). |

## Comportamiento transversal de la API

- Toda excepción no controlada se devuelve como `ProblemDetails` (RFC 9457) con `traceId` y sin detalles internos; las reglas de negocio incumplidas (`DomainException`) responden 422.
- Fuera de Development se activa HSTS; siempre hay redirección a HTTPS.
- Cabeceras de seguridad en toda respuesta: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy`, `Cross-Origin-Resource-Policy`.
- `GET /health/live` indica que el proceso está vivo (sin consultar la base de datos).
- Swagger y OpenAPI solo se exponen en Development.
