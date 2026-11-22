# SGT-Tesis Backend — Especificación SDD (Specification-Driven Development)

| Campo | Valor |
|---|---|
| Versión | 0.1 (borrador para validación del Product Owner) |
| Fecha | 2026-10-18 |
| Fuentes | `Readme.md`, issues #2 y #3, auditoría `SYSTEM_SPEC_AND_GAP_ANALYSIS.md` |
| Estado | Borrador. Los puntos marcados **[SUPUESTO]** son decisiones propuestas que el Product Owner debe confirmar; la sección 14 los lista. |

> Principio SDD: **la especificación es la fuente de verdad**. Ninguna historia pasa a desarrollo sin criterios de aceptación verificables, y cada criterio se traduce en al menos una prueba automatizada trazable por ID (`FR-xx`, `US-xx`, `AC-xx`).

---

## 1. Visión, objetivos y alcance

### 1.1 Visión
Digitalizar el proceso institucional de tesis de grado en un sistema único, trazable y auditable, donde cada avance académico queda respaldado por una decisión explícita y registrada.

### 1.2 Objetivos medibles

| ID | Objetivo | Indicador |
|---|---|---|
| OBJ-1 | Centralizar el seguimiento de las 7 etapas | 100 % de tesis activas gestionadas en el sistema |
| OBJ-2 | Garantizar trazabilidad de decisiones | 100 % de cambios de estado con autor, fecha y justificación inmutables |
| OBJ-3 | Reducir tiempos de validación | Tiempo medio de espera en cola de validación (KPI de dashboard) |
| OBJ-4 | Proteger datos académicos | 0 accesos cruzados entre estudiantes; 0 secretos en repositorio |
| OBJ-5 | Operación confiable | Disponibilidad ≥ 99,5 % mensual **[SUPUESTO]** |

### 1.3 Dentro del alcance (backend)
Autenticación y autorización; gestión de tesis y etapas; carga de documentos; asignación de evaluadores y veredictos; decisiones del Administrador; retroalimentación consolidada; dashboard/cola de validación; notificaciones por correo; auditoría; operación (logs, health, despliegue).

### 1.4 Fuera de alcance (esta versión)
Interfaz de usuario (repositorio frontend aparte); firma digital con validez legal; integración con sistemas académicos externos (SIA/ERP); detección de plagio; videoconferencia de sustentación. **[SUPUESTO]**

### 1.5 Glosario

| Término | Definición |
|---|---|
| Tesis | Proceso de grado de un estudiante; agregado raíz |
| Etapa | Cada uno de los 7 pasos del flujo |
| Decisión | Acto del Administrador que aprueba/rechaza/observa una etapa; inmutable |
| Veredicto | Concepto técnico no vinculante emitido por un Evaluador |
| Observado | La etapa requiere correcciones del estudiante |

---

## 2. Actores y roles

| Actor | Descripción | Autoridad |
|---|---|---|
| Estudiante | Dueño de una tesis | Sobre su propia tesis únicamente |
| Evaluador | Revisor técnico | Solo etapas que le fueron asignadas; **no** cambia estados |
| Administrador | Autoridad institucional | Única autoridad de decisión; gestiona asignaciones y usuarios |
| Sistema | Procesos automáticos | Notificaciones, auditoría, expiración de tokens |

Matriz de permisos (RBAC + propiedad del recurso):

| Capacidad | Estudiante | Evaluador | Admin |
|---|:---:|:---:|:---:|
| Crear/ver su tesis | ✔ propia | — | ✔ todas |
| Subir documentos | ✔ propia, etapa abierta | — | — |
| Ver tesis y documentos | ✔ propia | ✔ solo asignadas | ✔ |
| Emitir veredicto | — | ✔ etapa asignada | — |
| Decidir (aprobar/rechazar/observar) | — | ✗ | ✔ |
| Asignar/retirar evaluadores | — | — | ✔ |
| Gestionar usuarios | — | — | ✔ |
| Dashboard / cola de validación | — | — | ✔ |
| Consultar auditoría | — | — | ✔ |
| Ver retroalimentación consolidada | ✔ propia | — | ✔ |

---

## 3. Modelo de dominio

### 3.1 Contextos delimitados

| Contexto | Responsabilidad | Agregados |
|---|---|---|
| Identidad y Acceso | Usuarios, roles, tokens | `Usuario`, `RefreshToken` |
| Gestión de Tesis | Flujo de etapas y documentos | `Tesis` (raíz) → `Etapa` → `Documento` |
| Evaluación | Asignaciones y veredictos | `AsignacionEvaluador`, `Veredicto` |
| Decisiones y Auditoría | Registro inmutable | `Decision`, `RegistroAuditoria` |
| Reportería | KPIs y colas (solo lectura) | Vistas/proyecciones |

### 3.2 Entidades base (issue #3)
`BaseEntity`: `Id` (GUID, generado en dominio, no secuencial), `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`. La auditoría se llena automáticamente en `SaveChanges` desde el usuario del contexto actual.

### 3.3 Entidades principales (propuesta)

| Entidad | Atributos clave | MaxLength (propuesto) |
|---|---|---|
| Usuario | Id, Nombres, Apellidos, Correo (único), Rol, Activo, PasswordHash | Nombres 100, Apellidos 100, Correo 254 |
| Tesis | Id, StudentId, Titulo, Resumen, EstadoGlobal, EtapaActual | Titulo 300, Resumen 3000 |
| Etapa | Id, TesisId, Tipo (1–7), Estado, FechaApertura, FechaCierre | — |
| Documento | Id, EtapaId, NombreOriginal, NombreAlmacenado, ContentType, TamañoBytes, Hash SHA-256, Version | Nombre 255, ContentType 100 |
| AsignacionEvaluador | Id, EtapaId, EvaluadorId, AsignadoPor, Activa | — |
| Veredicto | Id, EtapaId, EvaluadorId, Concepto (Favorable/Observaciones/Desfavorable), Comentarios | Comentarios 4000 |
| Decision | Id, EtapaId, AdminId, Resultado, Justificacion, FechaUtc | Justificación 2000 |
| RefreshToken | Id, UsuarioId, HashToken, Expira, Revocado, ReemplazadoPor | — |
| RegistroAuditoria | Id, Actor, Accion, Entidad, EntidadId, Fecha, IP, Correlacion | — |

Regla transversal (issue #3): **ningún `string` sin `MaxLength`**; configurado en EF y validado en Application.

### 3.4 Máquina de estados de Etapa

Estados: `Pendiente`, `EnRevision`, `Observado`, `Aprobado`, `Rechazado`.

| Desde | Evento | Actor | Hacia | Condición |
|---|---|---|---|---|
| Pendiente | Enviar a revisión | Estudiante | EnRevision | Etapa abierta y documentos requeridos cargados |
| EnRevision | Observar | Admin | Observado | Justificación obligatoria |
| EnRevision | Aprobar | Admin | Aprobado | Etapa previa aprobada |
| EnRevision | Rechazar | Admin | Rechazado | Justificación obligatoria |
| Observado | Reenviar | Estudiante | EnRevision | Al menos un documento nuevo o versión nueva |
| Aprobado | — | — | (terminal) | Abre la siguiente etapa en `Pendiente` |
| Rechazado | — | — | (terminal) **[SUPUESTO]** | Reapertura solo por Admin con decisión registrada |

Invariantes:
- INV-1: Solo existe una etapa abierta (`Pendiente`/`EnRevision`/`Observado`) por tesis a la vez.
- INV-2: No se puede aprobar la etapa N si la N-1 no está `Aprobado`.
- INV-3: Toda transición de estado produce exactamente una `Decision` (si la ejecuta el Admin) y un `RegistroAuditoria`.
- INV-4: `Decision` y `RegistroAuditoria` no se actualizan ni eliminan.
- INV-5: Un Evaluador no puede crear `Decision` ni mutar `Etapa.Estado`.
- INV-6: Una `Tesis` requiere un `StudentId` válido (no vacío y existente con rol Estudiante).
- INV-7: Una etapa `Aprobada` o `Rechazada` no acepta nuevos documentos.

---

## 4. Requerimientos funcionales

| ID | Requerimiento | Prioridad |
|---|---|---|
| FR-01 | Autenticación por credenciales con emisión de access token JWT (vida corta) y refresh token | Must |
| FR-02 | Rotación y revocación de refresh tokens; cierre de sesión | Must |
| FR-03 | Administración de usuarios y roles (alta, baja lógica, cambio de rol) | Must |
| FR-04 | Registro de tesis por el Estudiante (una tesis activa por estudiante **[SUPUESTO]**) | Must |
| FR-05 | Modelado y control de las 7 etapas con la máquina de estados 3.4 | Must |
| FR-06 | Carga, versionado y descarga de documentos por etapa con validación de tipo/tamaño | Must |
| FR-07 | Asignación y retiro de evaluadores por etapa | Must |
| FR-08 | Emisión de veredictos por evaluadores asignados | Must |
| FR-09 | Decisión del Administrador (aprobar/observar/rechazar) con justificación | Must |
| FR-10 | Registro inmutable de decisiones y auditoría de acciones críticas | Must |
| FR-11 | Retroalimentación consolidada para el estudiante (decisión + veredictos relevantes) | Must |
| FR-12 | Dashboard de KPIs y cola de validación institucional | Should |
| FR-13 | Notificaciones por correo ante cambios de estado y asignaciones | Should |
| FR-14 | Consulta histórica/línea de tiempo de una tesis | Should |
| FR-15 | Recuperación de contraseña por correo | Should |
| FR-16 | Exportación de reportes (CSV) para el Admin | Could |

KPIs sugeridos (FR-12): tesis por etapa, tiempo promedio por etapa, tasa de aprobación/observación/rechazo, tesis estancadas > N días, carga por evaluador.

---

## 5. Historias de usuario y criterios de aceptación

Formato: *Como … quiero … para …*. Criterios en Gherkin. Cada criterio `AC-xx` debe mapearse a una prueba.

### Épica E1 — Identidad y acceso

**US-01 Iniciar sesión** (FR-01)
Como usuario quiero autenticarme para acceder según mi rol.
- AC-01.1 Dado credenciales válidas, cuando hago `POST /auth/login`, entonces recibo access token (≤ 15 min) y refresh token (≤ 7 días **[SUPUESTO]**).
- AC-01.2 Dado credenciales inválidas, entonces recibo 401 con mensaje genérico (sin revelar si el correo existe).
- AC-01.3 Dado 5 intentos fallidos consecutivos, entonces la cuenta se bloquea temporalmente 15 min y queda auditado.
- AC-01.4 El token contiene `sub`, `role`, `exp`, `jti`; no contiene datos personales sensibles.

**US-02 Renovar y cerrar sesión** (FR-02)
- AC-02.1 Dado un refresh token válido, cuando lo uso, entonces recibo un par nuevo y el anterior queda revocado (rotación).
- AC-02.2 Dado un refresh token ya usado, entonces se revoca toda su cadena y respondo 401 (detección de reutilización).
- AC-02.3 Al cerrar sesión el refresh token queda inválido.

**US-03 Gestionar usuarios** (FR-03)
Como Administrador quiero crear y desactivar usuarios.
- AC-03.1 Solo el rol Admin accede; otros reciben 403.
- AC-03.2 El correo es único; duplicado → 409.
- AC-03.3 Un usuario desactivado no puede autenticarse y sus tokens se revocan.

### Épica E2 — Gestión de tesis

**US-04 Registrar tesis** (FR-04)
Como Estudiante quiero registrar mi tesis para iniciar el proceso.
- AC-04.1 Dado título (≤ 300) y resumen (≤ 3000) válidos, se crea la tesis con la etapa "Estudiante" abierta en `Pendiente`.
- AC-04.2 El `StudentId` se toma del token, nunca del cuerpo de la petición.
- AC-04.3 Si ya existe una tesis activa para ese estudiante → 409.
- AC-04.4 Título vacío o fuera de longitud → 400 con detalle por campo (`ProblemDetails`).
- AC-04.5 (Domain) Crear `Tesis` con `StudentId` vacío lanza excepción de dominio; cubierto por prueba unitaria.

**US-05 Consultar mi tesis** (FR-04, FR-14)
- AC-05.1 Un Estudiante solo ve su tesis; pedir la de otro → 404 (no 403, para no revelar existencia).
- AC-05.2 La respuesta incluye etapa actual, estado y línea de tiempo de decisiones.

**US-06 Enviar etapa a revisión** (FR-05)
- AC-06.1 Con documentos requeridos cargados, la etapa pasa a `EnRevision`.
- AC-06.2 Sin documentos requeridos → 422 con la lista de faltantes.
- AC-06.3 Una etapa que no es la actual → 409.

### Épica E3 — Documentos

**US-07 Subir documento** (FR-06)
- AC-07.1 Solo tipos permitidos (PDF, DOCX **[SUPUESTO]**) verificados por contenido (firma), no solo extensión.
- AC-07.2 Tamaño máximo configurable (p. ej. 20 MB); superior → 413.
- AC-07.3 El archivo se almacena con nombre generado (GUID), no el original; se guarda hash SHA-256.
- AC-07.4 Subir un documento de la misma categoría crea una nueva versión sin sobrescribir la anterior.
- AC-07.5 En etapas `Aprobado`/`Rechazado` → 409 (INV-7).

**US-08 Descargar documento** (FR-06)
- AC-08.1 Autorizado solo el dueño, evaluadores asignados y Admin.
- AC-08.2 La descarga fuerza `Content-Disposition: attachment` y `X-Content-Type-Options: nosniff`.

### Épica E4 — Evaluación

**US-09 Asignar evaluador** (FR-07)
- AC-09.1 Solo Admin; el usuario asignado debe tener rol Evaluador y estar activo.
- AC-09.2 Asignación duplicada activa → 409.
- AC-09.3 Se notifica por correo al evaluador y queda auditado.

**US-10 Emitir veredicto** (FR-08)
- AC-10.1 Un Evaluador sin asignación activa a esa etapa recibe 403.
- AC-10.2 El veredicto (concepto + comentarios ≤ 4000) se guarda y es visible al Admin.
- AC-10.3 Emitir veredicto **no** modifica `Etapa.Estado` (INV-5); verificado por prueba.
- AC-10.4 Un evaluador puede actualizar su veredicto mientras la etapa esté `EnRevision`; el historial se conserva.

### Épica E5 — Decisiones y trazabilidad

**US-11 Decidir sobre una etapa** (FR-09, FR-10)
Como Administrador quiero aprobar, observar o rechazar una etapa.
- AC-11.1 Solo Admin; cualquier otro rol → 403.
- AC-11.2 Solo desde `EnRevision`; otro estado → 409.
- AC-11.3 Observar/Rechazar exige justificación (≤ 2000); omitirla → 422.
- AC-11.4 Aprobar la etapa N exige N-1 aprobada (INV-2) y abre la N+1 en `Pendiente`.
- AC-11.5 Se crea una `Decision` inmutable con admin, fecha UTC y justificación, en la misma transacción que el cambio de estado.
- AC-11.6 Intentar modificar o borrar una `Decision` falla a nivel de dominio y de base de datos (sin permisos UPDATE/DELETE para el usuario de aplicación sobre esa tabla **[SUPUESTO]**).
- AC-11.7 Decisiones concurrentes sobre la misma etapa: la segunda falla por control de concurrencia optimista (`RowVersion`) → 409.

**US-12 Consultar auditoría** (FR-10)
- AC-12.1 Solo Admin; filtros por tesis, actor, acción y rango de fechas, con paginación.
- AC-12.2 Cada registro incluye actor, acción, entidad, IP e ID de correlación.

### Épica E6 — Retroalimentación y reportería

**US-13 Ver retroalimentación consolidada** (FR-11)
- AC-13.1 El estudiante ve, por etapa, la decisión, su justificación y los comentarios de veredictos marcados como visibles **[SUPUESTO]**.
- AC-13.2 No ve la identidad del evaluador **[SUPUESTO: evaluación anónima]**.

**US-14 Dashboard y cola de validación** (FR-12)
- AC-14.1 Solo Admin.
- AC-14.2 La cola lista etapas en `EnRevision` ordenadas por antigüedad, con paginación.
- AC-14.3 Los KPIs se calculan sin cargar entidades completas (proyección/consulta de lectura); respuesta p95 < 800 ms con 10 000 tesis.

### Épica E7 — Notificaciones

**US-15 Notificar cambios** (FR-13)
- AC-15.1 Cambio de estado, asignación y observación disparan un correo a los afectados.
- AC-15.2 Un fallo de correo no revierte la transacción de negocio; se reintenta con *outbox* y queda registrado.
- AC-15.3 Las plantillas no incluyen datos sensibles más allá del mínimo necesario.

### Épica E8 — Fundaciones técnicas (issues vigentes)

**US-16 Base de datos por ambiente** (issue #2)
- AC-16.1 Existen instancias separadas Dev/Staging/Prod con credenciales y usuarios distintos.
- AC-16.2 El usuario de aplicación no tiene `db_owner` en producción; permisos mínimos documentados (`SELECT/INSERT/UPDATE/DELETE` según tabla; migraciones con cuenta separada).
- AC-16.3 La conexión usa timeout de comando/conexión explícito y `EnableRetryOnFailure`; probado simulando caída transitoria.
- AC-16.4 Ninguna cadena de conexión real se versiona.

**US-17 Entidades base** (issue #3)
- AC-17.1 Todas las entidades heredan `BaseEntity` con Id GUID y campos de auditoría; prueba de arquitectura lo verifica.
- AC-17.2 Todos los `string` tienen `MaxLength`; prueba que recorre el modelo EF y falla si falta alguno.
- AC-17.3 Reglas de negocio básicas cubiertas por pruebas unitarias de Domain sin base de datos.

---

## 6. Requerimientos no funcionales

| ID | Categoría | Requisito | Verificación |
|---|---|---|---|
| NFR-01 | Seguridad | Hash de contraseñas con Argon2id o PBKDF2 (ASP.NET Identity) ; política de complejidad | Prueba + revisión |
| NFR-02 | Seguridad | TLS 1.2+; HSTS; cabeceras de seguridad | Prueba de integración |
| NFR-03 | Seguridad | Secretos solo en variables de entorno / gestor de secretos | Escaneo de secretos en CI |
| NFR-04 | Seguridad | Rate limiting en login y endpoints sensibles | Prueba de integración |
| NFR-05 | Seguridad | CORS por lista blanca de orígenes por ambiente | Prueba |
| NFR-06 | Seguridad | Cumplimiento OWASP ASVS nivel 2 y API Security Top 10 | Checklist de revisión |
| NFR-07 | Privacidad | Tratamiento de datos personales conforme a la normativa local aplicable **[validar con área legal]** | Revisión legal |
| NFR-08 | Rendimiento | p95 < 500 ms en lecturas, < 1 s en escrituras (sin carga de archivos) | Pruebas de carga |
| NFR-09 | Escalabilidad | API sin estado; escalado horizontal; archivos fuera del filesystem local | Revisión de diseño |
| NFR-10 | Disponibilidad | ≥ 99,5 % mensual; *health checks* live/ready | Monitoreo |
| NFR-11 | Observabilidad | Logs estructurados (Serilog), ID de correlación, métricas y trazas (OpenTelemetry) | Revisión |
| NFR-12 | Mantenibilidad | Cobertura ≥ 80 % en Domain y Application; analizadores sin warnings | CI |
| NFR-13 | Mantenibilidad | Reglas de arquitectura automatizadas (dependencias entre capas) | Pruebas de arquitectura |
| NFR-14 | Integridad | Transacciones, concurrencia optimista, borrado lógico para entidades de negocio | Pruebas |
| NFR-15 | Recuperación | RPO ≤ 24 h, RTO ≤ 4 h **[SUPUESTO]**; restauración probada trimestralmente | Simulacro |
| NFR-16 | Portabilidad | Imagen Docker reproducible; configuración 12-factor | CI |
| NFR-17 | Accesibilidad de API | Contrato OpenAPI versionado, errores en `ProblemDetails` (RFC 9457) | Revisión |

---

## 7. Arquitectura

### 7.1 Estilo
Clean Architecture con CQRS ligero. Dependencias solo hacia adentro:

```
API ─► Infrastructure ─► Application ─► Domain
 └──────────────────────► Application
```

| Capa | Contiene | No debe contener |
|---|---|---|
| Domain | Entidades, agregados, objetos de valor, eventos de dominio, excepciones de dominio, enums | Referencias a EF, ASP.NET, MediatR, Serilog |
| Application | Comandos/consultas y handlers, validadores, DTOs, interfaces (`ITesisRepository`, `IUnitOfWork`, `ICurrentUser`, `IFileStorage`, `IEmailSender`, `IClock`), *behaviors* | Detalles de EF, HTTP, SMTP |
| Infrastructure | `DbContext`, configuraciones EF, repositorios, migraciones, Identity/JWT, almacenamiento de archivos, correo, outbox | Reglas de negocio |
| API | Endpoints/controllers, middleware, autenticación, Swagger, composición (`Program.cs`) | Lógica de negocio |

### 7.2 Estructura propuesta

```
src/
  SGT.Tesis.Domain/
    Common/ (BaseEntity, IDomainEvent, DomainException)
    Tesis/ (Tesis, Etapa, EtapaTipo, EstadoEtapa, Documento)
    Evaluacion/ (AsignacionEvaluador, Veredicto)
    Decisiones/ (Decision)
    Identidad/ (Rol)
  SGT.Tesis.Application/
    Common/ (Behaviors, Interfaces, Exceptions, Models)
    Auth/ Tesis/ Documentos/ Evaluacion/ Decisiones/ Reportes/ Usuarios/
      (Commands, Queries, Validators, Dtos)
    DependencyInjection.cs
  SGT.Tesis.Infrastructure/
    Persistence/ (AppDbContext, Configurations, Migrations, Interceptors)
    Identity/ Storage/ Email/ Outbox/ Time/
    DependencyInjection.cs
  SGT.Tesis.API/
    Endpoints|Controllers/ Middleware/ Extensions/ Program.cs
tests/
  SGT.Tesis.UnitTests/ (Domain, Application)
  SGT.Tesis.IntegrationTests/ (API + BD de prueba)
  SGT.Tesis.ArchitectureTests/
docs/  (SDD_SPECIFICATION.md, ADRs, runbooks)
```

### 7.3 Patrones y decisiones clave

| Tema | Decisión |
|---|---|
| Casos de uso | CQRS con handlers; pipeline: logging → validación → autorización → transacción |
| Mediación | Evaluar licenciamiento de MediatR (versiones recientes con licencia comercial); alternativa: handlers propios con un despachador mínimo — ver ADR-003 |
| Mapeo | Mapeo manual o generado en compilación (Mapperly); evitar AutoMapper por licencia y por mapeos implícitos |
| Validación | FluentValidation en Application; invariantes en Domain |
| Persistencia | EF Core; configuración con `IEntityTypeConfiguration`; GUID generado en dominio; `RowVersion` en agregados mutables |
| Auditoría | `SaveChangesInterceptor` completa `CreatedAt/By`, `UpdatedAt/By` desde `ICurrentUser` e `IClock` |
| Inmutabilidad | `Decision` y `RegistroAuditoria` sin setters públicos; sin UPDATE/DELETE a nivel de permisos de BD |
| Errores | Excepciones de dominio/aplicación → `ProblemDetails` vía manejador global |
| Eventos | Eventos de dominio → *outbox* transaccional → correo asíncrono |
| Archivos | `IFileStorage` (disco/Blob Storage); fuera de la BD |
| Autorización | Políticas por rol + *resource-based authorization* (propiedad/asignación) |
| Versionado de API | Prefijo `/api/v1` |
| Configuración | Opciones tipadas (`IOptions`) validadas al arranque (`ValidateOnStart`) |

### 7.4 Contrato de API (borrador `/api/v1`)

| Método | Ruta | Rol | Historia |
|---|---|---|---|
| POST | `/auth/login`, `/auth/refresh`, `/auth/logout` | Anónimo/Autenticado | US-01/02 |
| POST/GET/PATCH | `/usuarios` | Admin | US-03 |
| POST | `/tesis` | Estudiante | US-04 |
| GET | `/tesis/mia` | Estudiante | US-05 |
| GET | `/tesis/{id}` | Dueño/Asignado/Admin | US-05 |
| POST | `/tesis/{id}/etapas/{n}/enviar` | Estudiante | US-06 |
| POST/GET | `/tesis/{id}/etapas/{n}/documentos` | Según 2 | US-07/08 |
| POST/DELETE | `/etapas/{id}/evaluadores` | Admin | US-09 |
| PUT | `/etapas/{id}/veredicto` | Evaluador asignado | US-10 |
| POST | `/etapas/{id}/decisiones` | Admin | US-11 |
| GET | `/auditoria` | Admin | US-12 |
| GET | `/tesis/{id}/retroalimentacion` | Dueño/Admin | US-13 |
| GET | `/dashboard/kpis`, `/cola-validacion` | Admin | US-14 |
| GET | `/health/live`, `/health/ready` | Anónimo (red interna) | NFR-10 |

Convenciones: IDs GUID; paginación `page`/`pageSize` (máx. 100); errores `application/problem+json`; sin exponer entidades de dominio.

### 7.5 Seguridad por diseño
- JWT firmado con clave desde gestor de secretos; validación de emisor, audiencia, expiración y *clock skew* ≤ 1 min.
- Refresh tokens almacenados solo como hash.
- Autorización en la capa Application, no solo en controladores.
- Entradas validadas; salidas sin datos innecesarios; mensajes de error sin trazas.
- Subida de archivos: validación de firma, límite de tamaño, nombre generado, sin ejecución, escaneo antivirus si la institución lo exige **[SUPUESTO]**.
- Registro de auditoría de acciones críticas; Swagger solo fuera de producción.

### 7.6 Infraestructura y ambientes

| Ambiente | BD | Secretos | Observaciones |
|---|---|---|---|
| Development | Local/contenedor | User-secrets | Datos ficticios |
| Staging | Instancia propia | Variables/Key Vault | Espejo de producción, datos anonimizados |
| Production | Instancia propia, usuario de mínimo privilegio | Key Vault | Migraciones con cuenta separada |

Docker multi-etapa, usuario no-root; `docker-compose` para desarrollo (API + BD). CI/CD con GitHub Actions: restore → build → format → test+cobertura → auditoría de paquetes → escaneo de secretos/SAST → imagen → despliegue a Staging → aprobación manual → Producción.

---

## 8. Estrategia de pruebas (trazabilidad SDD)

| Nivel | Alcance | Herramientas | Meta |
|---|---|---|---|
| Unitarias — Domain | Invariantes INV-1…7, máquina de estados, BaseEntity | xUnit/xunit.v3, FluentAssertions | ≥ 90 % |
| Unitarias — Application | Handlers, validadores, autorización | xUnit, NSubstitute | ≥ 80 % |
| Arquitectura | Dependencias entre capas, herencia de `BaseEntity`, `MaxLength` | NetArchTest | 100 % reglas |
| Integración | Endpoints, EF, autorización por rol/propiedad, concurrencia | `WebApplicationFactory`, Testcontainers (SQL Server) | Todos los AC de API |
| Seguridad | Acceso cruzado entre estudiantes, escalada de rol, tokens reutilizados | Pruebas de integración | 100 % de AC de seguridad |
| Carga | Lecturas, dashboard | k6 | NFR-08 |
| Contrato | OpenAPI versionado sin cambios incompatibles | Diff de OpenAPI en CI | Sin *breaking changes* |

Matriz de trazabilidad: cada prueba declara el ID del criterio (`[Trait("AC","11.4")]`); un script de CI falla si un `AC-xx` no tiene prueba.

---

## 9. Definition of Ready / Definition of Done

**Ready**: historia con criterios Gherkin, permisos definidos, errores esperados, impacto en datos identificado, sin supuestos abiertos.

**Done**: código revisado por al menos una persona; pruebas por cada AC en verde; cobertura sin regresión; `dotnet format` y analizadores limpios; 0 paquetes vulnerables; migración revisada; OpenAPI actualizado; logs y auditoría verificados; documentación actualizada; desplegado en Staging.

---

## 10. Plan por sprints (propuesto)

| Sprint | Contenido | Historias |
|---|---|---|
| 0 | Reparación del arnés: vulnerabilidad Pkcs, `.editorconfig`, CI mínimo, retiro de plantilla | — |
| 1 | BD por ambiente, `BaseEntity`, `DbContext`, resiliencia | US-16, US-17 |
| 2 | Identidad y usuarios | US-01, US-02, US-03 |
| 3 | Tesis y etapas | US-04, US-05, US-06 |
| 4 | Documentos | US-07, US-08 |
| 5 | Evaluación y decisiones | US-09, US-10, US-11 |
| 6 | Auditoría, retroalimentación, notificaciones | US-12, US-13, US-15 |
| 7 | Dashboard y reportes | US-14, FR-16 |
| 8 | Endurecimiento, carga, Docker/CI-CD, salida a producción | NFR-* |

---

## 11. Riesgos

| ID | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|---|
| R-1 | Reglas de etapas no confirmadas (supuestos) | Alta | Alto | Taller con Product Owner antes del Sprint 3 |
| R-2 | Licenciamiento de MediatR/AutoMapper | Media | Medio | ADR-003; alternativa sin licencia |
| R-3 | Fuga de datos académicos | Baja | Muy alto | RBAC+propiedad, pruebas de acceso cruzado, auditoría |
| R-4 | Archivos maliciosos | Media | Alto | Validación de firma, almacenamiento aislado, antivirus |
| R-5 | Pérdida de datos | Baja | Muy alto | Respaldos y simulacros de restauración |
| R-6 | Equipo pequeño frente a alcance de producción real | Media | Alto | Priorización MoSCoW, CI estricto desde el inicio |

---

## 12. Registro de decisiones de arquitectura (ADR iniciales)

| ADR | Decisión | Estado |
|---|---|---|
| ADR-001 | Clean Architecture en 4 capas | Aceptada |
| ADR-002 | SQL Server como motor (ya referenciado en el proyecto) | Aceptada (confirmado por el Product Owner el 2026-10-20) |
| ADR-003 | Mediación y mapeo sin dependencias con licencia comercial | Aceptada |
| ADR-004 | Identidad: ASP.NET Identity + JWT propio con refresh tokens rotados | Propuesta; el Product Owner decidió SSO institucional (#45), que la sustituirá |
| ADR-005 | Políticas de etapa configurables, rondas de evaluación y reapertura | Aceptada |
| ADR-005 | Auditoría por interceptor; `Decision` inmutable | Propuesta |
| ADR-006 | Outbox transaccional para correo | Propuesta |
| ADR-007 | Almacenamiento de archivos tras `IFileStorage` | Propuesta |

---

## 13. Trazabilidad requisito → historia

| Requisito | Historias |
|---|---|
| FR-01, FR-02 | US-01, US-02 |
| FR-03 | US-03 |
| FR-04 | US-04, US-05 |
| FR-05 | US-06, US-11 |
| FR-06 | US-07, US-08 |
| FR-07 | US-09 |
| FR-08 | US-10 |
| FR-09, FR-10 | US-11, US-12 |
| FR-11 | US-13 |
| FR-12 | US-14 |
| FR-13 | US-15 |
| Issues #2, #3 | US-16, US-17 |

---

## 14. Preguntas abiertas para el Product Owner

1. ¿Un estudiante puede tener más de una tesis activa o intentos sucesivos tras un rechazo?
2. ¿`Rechazado` es terminal o se puede reabrir? ¿Quién y bajo qué condición?
3. ¿Qué documentos son obligatorios en cada una de las 7 etapas? ¿Con qué formatos y tamaños?
4. ¿Qué se evalúa exactamente en "Consejo", "Pasantía" y "Artículo"? ¿Hay más actores (director de tesis, jurados, consejo)?
5. ¿Hay plazos por etapa y alertas por vencimiento?
6. ¿La evaluación es anónima para el estudiante? ¿Los veredictos son visibles tal cual o solo la decisión del Administrador?
7. ¿Puede haber varios evaluadores por etapa y cuántos veredictos se requieren?
8. ¿Cómo se autentican los usuarios: cuentas propias o SSO institucional (Azure AD/OIDC)?
9. ¿Motor de base de datos definitivo y plataforma de despliegue (nube, servidores propios)?
10. ¿Normativa de protección de datos y retención aplicable? ¿Requisitos de accesibilidad y de idioma (solo español)?
11. ¿Requisitos de disponibilidad, RPO y RTO acordados con la institución?
12. ¿El contenido del audio de WhatsApp aporta requisitos adicionales? (no pudo ser procesado).
