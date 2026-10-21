# ADR-006 — Ingreso con la cuenta institucional (SSO con Microsoft Entra ID)

| Campo | Valor |
|---|---|
| Estado | **Borrador para revisión de TI** — no implementado. Los puntos marcados como **ABIERTO** esperan una respuesta. |
| Fecha | 2026-10-20 |
| Relación | Complementa a ADR-004: reutiliza sus sesiones (JWT + refresh rotado) y la tabla `Usuarios`; sustituye solo la forma de comprobar quién es la persona. |
| Historias | US-01, US-02, US-03 |

## 1. Contexto

La institución quiere que el ingreso sea con la cuenta de correo institucional (`correo.itm.edu.co`). Hoy el sistema tiene cuentas propias con contraseña (ADR-004). El Product Owner indicó que el proveedor probablemente es Microsoft; **TI debe confirmarlo** (punto A1).

Lo que no cambia: roles, permisos, estados de la tesis, auditoría y notificaciones. Solo cambia cómo se autentica la persona.

## 2. Opciones consideradas

| Opción | Descripción | Veredicto |
|---|---|---|
| **A. Intercambio de token (recomendada)** | La interfaz inicia sesión contra Entra ID (código de autorización con PKCE). Envía el token de identidad a la API, que lo valida, lo relaciona con un usuario local y emite **sus propias** sesiones (ADR-004). | Cambio acotado; la API y el resto de pantallas no se tocan; revocación y rotación siguen bajo control del sistema. |
| B. La API valida directamente los tokens de Entra | La API se registra como recurso protegido y acepta el token de acceso de Entra en cada llamada. | Delega la duración de la sesión a Entra y obliga a cambiar toda la autorización. Más invasivo. |
| C. Inicio de sesión del lado del servidor (cliente confidencial + cookie) | La API gestiona el flujo OIDC y una cookie de sesión. | Cambia el modelo de sesión completo y exige un secreto de cliente. Descartada. |

**Decisión propuesta: A.**

## 3. Diseño propuesto (opción A)

### 3.1 Flujo

1. En el login aparece **«Ingresar con cuenta institucional»**. La interfaz usa `@azure/msal-browser` (código de autorización con PKCE; cliente público, **sin secreto**).
2. Entra autentica a la persona (con la MFA que la institución ya tenga configurada) y devuelve un token de identidad a la interfaz.
3. La interfaz llama a `POST /api/v1/auth/sso/intercambio` con ese token y el `nonce` usado.
4. La API valida el token (ver 3.2), busca o vincula al usuario (3.3) y responde igual que el login actual: access token propio de 15 min y refresh token rotado.
5. A partir de ahí todo funciona como hoy. Cerrar sesión revoca la cadena de refresh del sistema y, opcionalmente, cierra la sesión en Entra.

### 3.2 Validaciones del token de identidad

- Firma contra las claves públicas publicadas por el tenant (metadatos OIDC, con caché y rotación automática).
- `iss` igual a `https://login.microsoftonline.com/{tenant}/v2.0` con **el tenant configurado** (un solo tenant permitido).
- `aud` igual al Client ID de la aplicación; `exp`/`nbf` vigentes (tolerancia de 2 min); `nonce` coincide.
- Se exige el claim `tid` y `oid`. **La identidad es el par (tenant, `oid`), nunca el correo**: el correo del token puede cambiar y confiar solo en él permite suplantación entre tenants.
- Nada del token se guarda; solo se usa para identificar.

### 3.3 Relación con el usuario local

Nueva tabla `IdentidadesExternas` (`Proveedor`, `TenantId`, `ObjectId`, `UsuarioId`, `CreadaEn`) con unicidad por (`Proveedor`, `TenantId`, `ObjectId`).

Primer ingreso de una persona sin vínculo:

- **Si el administrador ya registró su correo en el sistema** (misma dirección que `preferred_username`/`upn` del token, dentro de un dominio permitido): se vincula y se conserva su rol.
- **Si no existe y el aprovisionamiento automático está activado** (**ABIERTO**, punto C1): se crea como **Estudiante** y queda auditado. Evaluadores y Administradores nunca se crean automáticamente: los asigna un administrador.
- **Si no existe y está desactivado:** se rechaza con un mensaje que indica que debe solicitar acceso.

Una cuenta inactiva en el sistema no puede ingresar aunque Entra la autentique.

### 3.4 Roles

Propuesta: **el rol lo sigue decidiendo el sistema** (`Usuarios.Rol`, asignado por un administrador). No se leen grupos ni roles de Entra en la primera versión. Si TI prefiere que el rol venga de grupos de Entra, se agrega después un mapeo configurable grupo → rol sin cambiar el resto (punto D1).

### 3.5 Cuentas locales

- Las cuentas con SSO no usan contraseña: se deshabilitan para ellas el cambio y la recuperación de contraseña.
- Se conserva el ingreso con contraseña para el **administrador inicial** como acceso de emergencia (si Entra no está disponible). Se controla con `Autenticacion:PermitirPasswordLocal` y se recomienda dejarlo activo solo para administradores (punto E1).
- Evaluadores **externos** a la institución (sin cuenta en el tenant) necesitan otra vía: invitación como usuario invitado en Entra o cuenta local (punto D3).

### 3.6 Configuración nueva

| Clave | Descripción |
|---|---|
| `Autenticacion:Sso:Habilitado` | Activa el endpoint de intercambio y el botón. |
| `Autenticacion:Sso:TenantId` | Identificador del directorio. |
| `Autenticacion:Sso:ClientId` | Id de la aplicación registrada. |
| `Autenticacion:Sso:DominiosPermitidos` | Dominios de correo aceptados (por ejemplo `correo.itm.edu.co`). |
| `Autenticacion:Sso:AprovisionamientoAutomatico` | `true` crea Estudiantes en el primer ingreso. |
| `Autenticacion:PermitirPasswordLocal` | `true` mantiene el ingreso con contraseña. |

Ninguna es un secreto. El `config.json` de la interfaz recibe `sso.clientId`, `sso.authority` y `sso.redirectUri`.

### 3.7 Seguridad

- Un solo tenant permitido; `nonce` y PKCE obligatorios; límite de solicitudes por IP en el intercambio (igual que login).
- Respuestas de error genéricas; el motivo detallado queda en la auditoría (`Auth.SsoRechazado`).
- Auditoría de cada ingreso, vínculo y creación (`Auth.SsoLogin`, `Auth.SsoVinculo`, `Usuario.CreadoPorSso`).
- CSP de la interfaz: permitir `login.microsoftonline.com` en `connect-src` y `frame-src`.
- **Desprovisionamiento:** si TI desactiva la cuenta en Entra, la persona no podrá iniciar sesión nueva, pero un refresh token ya emitido vive hasta 7 días. Mitigación propuesta: el administrador puede desactivar al usuario en el sistema, con efecto inmediato. **ABIERTO** si se necesita sincronización automática (punto E2).

## 4. Lo que TI debe confirmar

| # | Pregunta | Por qué importa | Si no hay respuesta |
|---|---|---|---|
| A1 | ¿`correo.itm.edu.co` es Microsoft 365 / Entra ID? | Define todo el diseño. | Se asume Entra ID. |
| A2 | ¿Cuál es el **Tenant ID**? ¿Es uno solo? | Se valida el emisor contra él. | Bloquea la implementación. |
| B1 | ¿Pueden registrar la aplicación **SGT-Tesis** como *Single-page application* (cliente público)? | Entrega el Client ID. **No se necesita secreto.** | Bloquea la implementación. |
| B2 | URLs de redirección por ambiente (local, pruebas, producción). | Entra solo acepta las registradas. | Se usa `http://localhost:4200` en desarrollo. |
| B3 | ¿Se puede restringir la app a usuarios asignados o a un grupo? | Evita que cualquier cuenta del tenant ingrese. | Se filtra por dominio en la API. |
| C1 | ¿El primer ingreso de un estudiante crea la cuenta (aprovisionamiento automático) o el administrador las precarga? | Define el proceso de alta. | Propuesta: automático solo para el dominio de estudiantes. |
| D1 | ¿Cómo se identifica a Evaluadores y Administradores? ¿Usan el mismo dominio o uno distinto (por ejemplo, de docentes)? | El filtro por dominio y el alta dependen de eso. | Los asigna un administrador en el sistema. |
| D2 | ¿Se prefieren roles por grupos de Entra o por asignación en el sistema? | Define si hay mapeo de grupos. | Asignación en el sistema (3.4). |
| D3 | ¿Habrá evaluadores externos sin cuenta institucional? ¿Cómo ingresan (invitado de Entra o cuenta local)? | Afecta el diseño del acceso. | Cuenta local por ahora. |
| E1 | ¿Se mantiene un acceso de emergencia con contraseña para administradores? | Define `PermitirPasswordLocal`. | Sí, solo administradores. |
| E2 | ¿Se necesita que desactivar la cuenta en Entra cierre las sesiones del sistema de inmediato? | Exige sincronización o consulta periódica. | Se desactiva a mano en el sistema. |
| E3 | ¿Hay políticas de acceso condicional o MFA que apliquen a esta aplicación? | Afecta la experiencia de ingreso. | Se hereda lo que tenga el tenant. |

Para el **Product Owner**: confirmar que ya no se requiere ingreso con contraseña para estudiantes y docentes, y si hay usuarios que no pertenecerán al tenant institucional.

## 5. Consecuencias

- (+) Sin contraseñas que custodiar para la mayoría de las personas; se aprovechan MFA y políticas de la institución.
- (+) Cambio acotado: un endpoint, una tabla, una pantalla; el resto de la API no se modifica.
- (+) No hay secretos nuevos que custodiar (cliente público).
- (−) Nueva dependencia externa en el ingreso; el acceso de emergencia mitiga una caída de Entra.
- (−) El correo debe mantenerse coherente entre Entra y el sistema (las notificaciones usan el del sistema).
- (−) Las cuentas existentes con contraseña deben vincularse en su primer ingreso por SSO.

## 6. Plan de implementación (una vez resueltos A1, A2, B1)

1. **Backend:** tabla `IdentidadesExternas` y migración; validador de token detrás de una interfaz; endpoint de intercambio; auditoría; configuración; pruebas con un validador simulado (no se llama a Microsoft en CI).
2. **Frontend:** botón y flujo con MSAL, `config.json`, CSP, pruebas unitarias; pruebas E2E con el ingreso local.
3. **Documentación:** `CONFIGURATION.md`, `OPERATIONS.md` (cómo rotar, qué hacer si Entra cae) y el recorrido de las pantallas.
4. **Puesta en marcha:** probar en el ambiente de pruebas con cuentas reales y precargar o vincular a los administradores antes de activar `PermitirPasswordLocal=false` para los demás.

## 7. Fuera de alcance

Sincronización masiva de usuarios desde el directorio, aprovisionamiento SCIM, ingreso con otros proveedores (Google u otros) y cualquier cambio en roles o permisos.
