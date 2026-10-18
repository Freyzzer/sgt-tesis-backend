# ADR-004 — Identidad con cuentas propias, JWT y refresh tokens rotados

| Campo | Valor |
|---|---|
| Estado | **Propuesta** (supone cuentas propias; si la institución exige SSO institucional se revisa) |
| Fecha | 2026-10-18 |
| Historias | US-01, US-02, US-03 |

## Decisión

- **Cuentas propias** en la tabla `Usuarios` (rol único por usuario). Contraseñas con PBKDF2 (hasher de ASP.NET Identity) y sal aleatoria; no se usa el modelo completo de Identity.
- **Access token JWT** (HS256, 15 min) con solo `sub`, `role`, `jti`, `iat/nbf/exp`, emisor y audiencia. Sin datos personales.
- **Refresh token** opaco aleatorio (48 bytes), vigencia 7 días, persistido **solo como hash SHA-256**. Cada uso lo **rota**; reutilizar uno ya rotado revoca **toda la cadena** (detección de robo).
- **Bloqueo** de cuenta 15 minutos tras 5 fallos consecutivos; el contador se reinicia con un acceso exitoso.
- Respuestas de login **genéricas** (401) y con tiempo equivalente exista o no el correo.
- Límite de solicitudes por IP en login y refresh.
- Primer Administrador creado por configuración (`Bootstrap__*`) solo si no existe ninguno.

## Consecuencias

- (+) Sin dependencias externas; control total de sesiones y revocación.
- (−) La institución debe confiar en la gestión de credenciales del sistema; no hay MFA ni recuperación de contraseña todavía (FR-15, pendiente).
- (−) La clave de firma HMAC es un secreto compartido; debe rotarse de forma planificada (la rotación invalida los access tokens vigentes, de 15 min).

## Alternativa

SSO con OpenID Connect (Azure AD / institucional): elimina la gestión de contraseñas pero requiere que la institución provea el IdP. Si se confirma, se sustituye el login por validación del token del IdP y se mantiene la tabla `Usuarios` para roles locales.
