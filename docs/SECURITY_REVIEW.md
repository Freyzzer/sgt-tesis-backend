# Revisión de seguridad

Fecha: 2026-10-19 · Alcance: backend en `develop` (272 pruebas automáticas) y el stack completo levantado con Docker contra SQL Server 2022. Es una **revisión de diseño y de código con pruebas automáticas**, no una prueba de penetración: no sustituye una auditoría externa antes de producción.

## 1. Controles implementados y cómo se verifican

Mapeo con OWASP API Security Top 10 (2023):

| Riesgo | Controles | Evidencia |
|---|---|---|
| **API1 – Autorización a nivel de objeto** | Un Estudiante solo ve su tesis, sus documentos y su retroalimentación; el Evaluador, solo las tesis con asignación activa. Para el resto el recurso "no existe" (404, sin revelar existencia). | Pruebas de acceso cruzado entre estudiantes, evaluadores sin asignación y descarga de documentos ajenos |
| **API2 – Autenticación rota** | JWT de 15 min firmado (HS256) con validación de emisor, audiencia y vigencia; refresh tokens aleatorios guardados solo como hash, **rotados** en cada uso con revocación de toda la cadena si se reutiliza uno; bloqueo de 15 min tras 5 fallos; mensaje y tiempo de respuesta iguales exista o no el correo; rate limiting en login y refresh; contraseñas con PBKDF2 y política de complejidad (≥ 12, mayúscula, minúscula, número, símbolo) | Pruebas de login, refresh, reutilización, bloqueo y límite de solicitudes |
| **API3 – Autorización a nivel de propiedad** | El `StudentId` sale del token, nunca del cuerpo; los comandos exponen solo los campos permitidos; el Administrador no puede cambiarse el rol ni desactivarse ni dejar al sistema sin Administrador | Pruebas de asignación masiva y de protección del último Administrador |
| **API4 – Consumo irrestricto de recursos** | Tamaño máximo de archivo (413), paginación con máximo 100, rate limiting, timeout de comandos de base de datos y reintentos acotados | Pruebas de límites |
| **API5 – Autorización a nivel de función** | Política de rol en cada endpoint **y** verificación del rol dentro de cada caso de uso (defensa en profundidad); 401/403 diferenciados | Pruebas por rol sobre todos los endpoints; pruebas de caso de uso que omiten la política |
| **API6 – Flujos de negocio sensibles** | Máquina de estados de etapas con invariantes en el dominio; decisión y cambio de estado en la misma transacción; **concurrencia optimista** (dos decisiones simultáneas → 409); decisiones y auditoría **inmutables** (aplicación + restricciones de base de datos) | Pruebas de dominio, de carrera y de inmutabilidad |
| **API7 – SSRF** | No hay llamadas salientes a URLs controladas por el usuario | Revisión de código |
| **API8 – Configuración incorrecta** | Cabeceras de seguridad (`nosniff`, `X-Frame-Options`, CSP restrictiva, `no-referrer`), HSTS fuera de Development, CORS por lista blanca sin comodines, `AllowedHosts` obligatorio fuera de Development, Swagger solo en Development, errores como `ProblemDetails` sin trazas, la API **no arranca** con configuración insegura (cuenta `sa`, modo de correo `Log` en producción, etc.) | Pruebas de cabeceras, CORS, arranque y validación de opciones |
| **API9 – Inventario inadecuado** | Versionado `/api/v1`, documentación de endpoints en `docs/CONFIGURATION.md` | Revisión |
| **API10 – Consumo inseguro de APIs** | El servidor SMTP es el único servicio externo; conexión con STARTTLS | Revisión de código |

Otros controles:

- **Archivos:** solo PDF y DOCX verificados por **contenido** (firma; un DOCX debe ser un paquete Office real) y extensión coincidente; nombre en disco generado (GUID), sin usar el nombre original; recorrido de rutas imposible (el almacenamiento solo acepta nombres generados); descarga con `Content-Disposition: attachment` y `nosniff`; hash SHA-256 por archivo.
- **Base de datos:** consultas parametrizadas (EF Core); usuario de la API sin `db_owner` (verificado contra SQL Server real); cuenta distinta para migraciones; `DENY UPDATE/DELETE` sobre decisiones y auditoría.
- **Secretos:** nada versionado (búsqueda de secretos con gitleaks en cada PR, 0 hallazgos); `.env` ignorado; los secretos se inyectan por variables de entorno o gestor de secretos.
- **Auditoría:** quién, qué, cuándo, desde qué IP y con qué ID de correlación, en la misma transacción que el cambio; sin contraseñas, tokens ni correos de intentos con usuarios desconocidos.
- **Privacidad en correos:** las plantillas no incluyen títulos, nombres ni textos de justificaciones o veredictos.
- **Anonimato de la evaluación:** el estudiante ve los veredictos como "Evaluador 1…" sin identificadores ni correos.
- **Cadena de suministro:** `dotnet list package --vulnerable` en cada PR (0 vulnerabilidades hoy), Dependabot semanal, imagen base oficial de Microsoft, contenedor sin privilegios.

## 2. Riesgos residuales y brechas conocidas

| # | Riesgo | Severidad | Recomendación |
|---|---|---|---|
| R-1 | **No hay cambio ni recuperación de contraseña** (FR-15). El Administrador define la contraseña inicial y no se fuerza su cambio | **Alta** | Implementarlo antes de producción (endpoint de cambio, restablecimiento por correo con token de un solo uso, cambio obligatorio en el primer acceso) o integrar SSO institucional |
| R-2 | No hay autenticación multifactor | Media | Evaluar SSO con OIDC (ver ADR-004) o MFA para el rol Administrador |
| R-3 | Los archivos no se analizan con antivirus | Media | Integrar un escáner detrás de `IFileStorage` si la institución lo exige |
| R-4 | La clave JWT es un secreto compartido simétrico | Media | Guardarla en el gestor de secretos con rotación planificada; valorar firma asimétrica |
| R-5 | El rate limiting es en memoria y por instancia, solo en login y refresh | Media | Con varias instancias, aplicarlo también en el proxy o con un almacén compartido |
| R-6 | El `DENY` de base de datos y la creación de usuarios de mínimo privilegio **son scripts que hay que ejecutar** en cada ambiente | Media | Incluirlos en el procedimiento de despliegue y verificarlos con `02-verify-app-user.sql` |
| R-7 | Sin política de retención de la auditoría ni de las notificaciones | Baja | Definir plazos con la institución |
| R-8 | No hay análisis de imágenes (Trivy/Grype) ni DAST en el CI; CodeQL no corre en repositorios privados sin Advanced Security | Media | Añadir escaneo de imágenes y, si se activa Advanced Security, habilitar CodeQL |
| R-9 | Los datos personales de estudiantes requieren evaluación legal (NFR-07): base legal, retención, derechos de los titulares | Alta | Revisión del área jurídica antes de producción |
| R-10 | Sin prueba de penetración independiente | Alta | Contratarla sobre Staging antes de producción |
| R-11 | El proceso de correos no tiene bloqueo entre instancias | Baja | Una sola instancia con el envío activo |
| R-12 | Aviso de DataProtection al arrancar (no se usan cookies ni antiforgery) | Informativa | Sin acción; documentado |

## 3. Lista de verificación previa a producción

- [ ] Resolver R-1 (y decidir R-2) y R-9.
- [ ] Ejecutar los scripts de base de datos en Staging y Producción y comprobar con `02-verify-app-user.sql`.
- [ ] Secretos únicos por ambiente en el gestor de secretos; retirar `Bootstrap__*` tras el primer arranque.
- [ ] `AllowedHosts`, CORS y `ForwardedHeaders` configurados con los valores reales; TLS en el proxy.
- [ ] Prueba de carga en Staging con el volumen esperado.
- [ ] Simulacro de restauración de respaldo.
- [ ] Prueba de penetración sobre Staging y corrección de hallazgos altos.
- [ ] Revisión de las preguntas abiertas de `PROJECT_STATUS.md` con el Product Owner.
