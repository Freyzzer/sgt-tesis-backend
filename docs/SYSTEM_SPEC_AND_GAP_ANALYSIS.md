# SGT-Tesis Backend — Especificación Inversa, Auditoría de Hardening y Análisis de Brechas

| Campo | Valor |
|---|---|
| Repositorio | `sgt-tesis-backend` (clon local en `D:\sgt-tesis-backend`, rama `main`, 3 commits; ramas remotas `main` y `develop`) |
| Fecha de auditoría | 2026-10-18 |
| SDK / Runtime | .NET SDK 10.0.400, target `net10.0` |
| Alcance | Código fuente, configuración, dependencias, documentación (`Readme.md`) y dos capturas de issues (#2, #3) presentes en la raíz |
| Naturaleza del hallazgo principal | **El repositorio es un andamiaje (scaffold) inicial; no contiene lógica de negocio implementada.** |

> **Convención de este documento.** Cada afirmación se etiqueta con su origen:
> **[CÓDIGO]** verificado en el código/configuración, **[ARNÉS]** salida de un comando ejecutado, **[README]** intención declarada en `Readme.md`, **[ISSUE]** criterio de aceptación visible en las capturas de los issues #2 y #3, **[PROPUESTA]** especificación recomendada que aún no existe.
> Las secciones 1.2–1.5 describen el sistema **objetivo** (README + issues) y no el sistema implementado.

---

## 0. RESULTADOS DEL ARNÉS DE EJECUCIÓN

### 0.1 Resumen

| Arnés | Comando | Resultado |
|---|---|---|
| Compilación | `dotnet build SGT.Tesis.slnx` | **Éxito, 0 errores, 12 warnings** (todos `NU1902`/`NU1903`) |
| Pruebas | `dotnet test --collect:"XPlat Code Coverage"` | **2/2 pasan** (1 unitaria + 1 integración), pero ambas están **vacías** (cuerpo `{ }` sin aserciones) |
| Cobertura | Cobertura Cobertura (coverlet) | **Líneas 0 %, ramas 0 %** |
| Estilo / análisis estático | `dotnet format --verify-no-changes` | **Falla**: `Program.cs(28,20) WHITESPACE: Delete 1 characters` |
| Vulnerabilidades | `dotnet list package --vulnerable --include-transitive` | **1 paquete transitivo vulnerable (severidad ALTA)** |
| Paquetes obsoletos | `dotnet list package --outdated` | Parches menores pendientes (ver 0.4) |
| Paquetes deprecados | `dotnet list package --deprecated` | `xunit 2.9.3` marcado *Legacy* (alternativa: `xunit.v3`) |
| Linter adicional | No configurado | No existe `.editorconfig`, analizadores Roslyn ni reglas de estilo |

Nota operativa: la restauración inicial tardó ~1,6 min y el build completo ~8 min en el entorno de auditoría (descarga fría de paquetes); no es un indicador de rendimiento del proyecto.

### 0.2 Warnings de compilación **[ARNÉS]**

```
NU1903: System.Security.Cryptography.Pkcs 9.0.11 — severidad ALTA     GHSA-846f-f764-76mx
NU1902: System.Security.Cryptography.Pkcs 9.0.11 — severidad MODERADA GHSA-2fmv-rv2c-59x7
```

Afecta a `SGT.Tesis.API`, `SGT.Tesis.Infrastructure` y `SGT.Tesis.IntegrationTests` (dependencia **transitiva**, resuelta en 9.0.11 mientras el resto del stack es 10.0.x). `TreatWarningsAsErrors=false` permite que el pipeline compile pese a ello.

### 0.3 Formato **[ARNÉS]**

Una violación de whitespace en `src/SGT.Tesis.API/Program.cs` línea 28 (espacio doble en `var forecast =  Enumerable...`). Es código de plantilla; se elimina junto con el endpoint de ejemplo.

### 0.4 Dependencias directas desactualizadas **[ARNÉS]**

| Paquete | Actual | Última |
|---|---|---|
| Microsoft.AspNetCore.* / EF Core* (10.0.x) | 10.0.12 | 10.0.13 |
| Swashbuckle.AspNetCore | 10.2.3 | 10.3.0 |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | 12.2.0 |
| Microsoft.NET.Test.Sdk | 17.14.1 | 18.11.0 |
| coverlet.collector | 6.0.4 | 10.1.0 |
| xunit.runner.visualstudio | 3.1.4 | 4.0.1 |
| xunit | 2.9.3 (deprecado) | migrar a `xunit.v3` |

### 0.5 Retroalimentación del arnés aplicada a la auditoría

- El **warning de vulnerabilidad** motiva fijar explícitamente `System.Security.Cryptography.Pkcs >= 10.0.x` (o la versión parcheada) como `PackageReference` directo o vía Central Package Management, y activar `NuGetAudit` con `TreatWarningsAsErrors` para `NU1902/NU1903`.
- La **cobertura 0 %** y las pruebas vacías evidencian que el arnés de pruebas existe estructuralmente pero **no ofrece ninguna garantía** (un test vacío siempre pasa).
- El **fallo de `dotnet format`** confirma ausencia de `.editorconfig` y de verificación de estilo en CI.

---

## 1. ESPECIFICACIÓN INVERSA (SDD)

### 1.1 Estado real del código frente a la intención

| Aspecto | Intención **[README]** | Implementado **[CÓDIGO]** |
|---|---|---|
| Domain | Entidades, enums, reglas de negocio | `Class1.cs` vacía |
| Application | Casos de uso CQRS (MediatR), DTOs, validaciones | `Class1.cs` vacía; paquetes MediatR, AutoMapper y FluentValidation referenciados pero sin uso |
| Infrastructure | EF Core, JWT, almacenamiento, correo | `Class1.cs` vacía; paquetes EF Core SqlServer, Identity y Serilog referenciados sin uso |
| API | Controllers, middlewares, arranque | `Program.cs` con plantilla `weatherforecast` + OpenAPI + Swagger |
| Persistencia | EF Core + migraciones | Sin `DbContext`, sin migraciones, sin cadena de conexión |
| Autenticación | JWT + Refresh Tokens | Sin configuración; solo el paquete `JwtBearer` |
| Pruebas | Unitarias + integración | 1 test vacío por proyecto |

Por tanto, lo siguiente es una **especificación objetivo reconstruida desde la documentación**, no una descripción de comportamiento existente.

### 1.2 Propósito y visión del sistema **[README]**

Sistema de Gestión de Tesis (SGT-Tesis): digitaliza el proceso institucional de tesis de grado como un flujo de **7 etapas** (Estudiante → Propuesta → Consejo → Evaluación → Pasantía → Artículo → Requisitos Finales). Valor de negocio: trazabilidad, auditoría de decisiones y seguridad sobre datos académicos, en un sistema declarado como de **producción real** a nivel institucional.

### 1.3 Contextos delimitados y modelo de dominio **[PROPUESTA derivada de README/ISSUE]**

| Bounded context | Responsabilidad |
|---|---|
| Identidad y Acceso | Usuarios, roles, autenticación JWT, refresh tokens |
| Gestión de Tesis | Tesis, etapas, estados, documentos |
| Evaluación | Asignación de evaluadores y veredictos técnicos por etapa |
| Decisiones y Auditoría | Registro inmutable de decisiones del Administrador |
| Reportería | Dashboard de KPIs y cola de validación institucional |

Modelo conceptual (nombres tentativos):

- **BaseEntity** **[ISSUE #3]**: `Id` (GUID, no autoincremental), `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`.
- **Agregado Tesis** (raíz): `StudentId` obligatorio y válido, colección de **Etapas**, estado global.
- **Etapa**: tipo (una de 7), estado `Pendiente | EnRevision | Observado | Aprobado | Rechazado`.
- **Decisión** (inmutable): etapa, administrador, resultado, justificación, fecha.
- **Asignación de Evaluador**: evaluador ↔ etapa(s) específica(s).
- **Veredicto**: evaluador, etapa, concepto técnico, comentarios.
- **Documento**: metadatos de archivo asociados a una etapa.
- **Objetos de valor sugeridos**: `EtapaTipo`, `EstadoEtapa`, `TituloTesis`, `CorreoInstitucional`.

Reglas de negocio declaradas:

1. El avance de etapa exige decisión explícita del Administrador **[README]**.
2. Las decisiones se registran de forma inmutable **[README]**.
3. El Evaluador solo emite veredicto sobre las etapas asignadas y **no** puede cambiar el estado de la tesis **[README]**.
4. Una tesis debe tener un `StudentId` válido, validado en Domain y no solo por BD **[ISSUE #3]**.
5. Ningún campo de texto libre admite longitud ilimitada (`MaxLength` explícito) **[ISSUE #3]**.

### 1.4 Requerimientos funcionales (FR) **[PROPUESTA desde README]**

| ID | Requerimiento |
|---|---|
| FR-01 | El sistema debe autenticar usuarios mediante JWT de corta vida y refresh tokens revocables. |
| FR-02 | El sistema debe permitir al Estudiante registrar una tesis y asociarla a su identidad. |
| FR-03 | El sistema debe modelar las 7 etapas con estados `Pendiente`, `EnRevision`, `Observado`, `Aprobado`, `Rechazado`. |
| FR-04 | El sistema debe permitir al Estudiante cargar documentación por etapa. |
| FR-05 | El sistema debe permitir al Administrador asignar Evaluadores a etapas concretas. |
| FR-06 | El sistema debe permitir al Evaluador registrar un veredicto solo sobre etapas asignadas. |
| FR-07 | El sistema debe permitir únicamente al Administrador aprobar/rechazar el avance de una etapa. |
| FR-08 | El sistema debe registrar cada decisión de forma inmutable con autor, fecha y justificación. |
| FR-09 | El sistema debe consolidar y exponer al Estudiante la retroalimentación recibida. |
| FR-10 | El sistema debe exponer al Administrador un dashboard de KPIs y una cola de validación. |
| FR-11 | El sistema debe notificar por correo los cambios de estado relevantes. **[inferido de "correo" en Infrastructure]** |

### 1.5 Requerimientos no funcionales (NFR) **[PROPUESTA]**

| ID | Atributo | Especificación |
|---|---|---|
| NFR-01 | Seguridad | Secretos fuera del repositorio; TLS obligatorio; mínimo privilegio en BD (sin `db_owner` en producción) **[ISSUE #2]**. |
| NFR-02 | Resiliencia | Conexión a BD con timeout y reintentos configurados (`EnableRetryOnFailure`) **[ISSUE #2]**. |
| NFR-03 | Aislamiento de ambientes | Instancia, credenciales y usuarios de BD distintos por ambiente **[ISSUE #2]**. |
| NFR-04 | Trazabilidad | Auditoría de autoría/fechas en toda entidad y decisiones inmutables. |
| NFR-05 | Observabilidad | Logging estructurado (Serilog), correlación por petición, health checks. |
| NFR-06 | Mantenibilidad | Clean Architecture, dependencias hacia el interior, cobertura mínima objetivo ≥ 80 % en Domain/Application. |
| NFR-07 | Rendimiento | Paginación obligatoria en listados; p95 < 500 ms en lecturas (objetivo a validar). |
| NFR-08 | Privacidad | Protección de datos académicos de estudiantes; control de acceso por recurso. |

### 1.6 Actores y matriz de permisos **[README + PROPUESTA]**

| Capacidad | Estudiante | Evaluador | Administrador |
|---|:---:|:---:|:---:|
| Registrar/ver su propia tesis | ✔ (solo propia) | — | ✔ (todas) |
| Cargar documentos de su tesis | ✔ | — | — |
| Ver tesis asignadas | — | ✔ (solo asignadas) | ✔ |
| Emitir veredicto técnico | — | ✔ (etapa asignada) | — |
| Aprobar/rechazar avance de etapa | — | ✗ | ✔ (única autoridad) |
| Asignar evaluadores | — | — | ✔ |
| Ver dashboard KPI / cola de validación | — | — | ✔ |
| Consultar auditoría de decisiones | — | — | ✔ |

Control requerido: **RBAC** por rol más **ABAC/propiedad de recurso** (un Estudiante solo accede a su tesis; un Evaluador solo a etapas asignadas). Ninguno está implementado **[CÓDIGO]**.

---

## 2. AUDITORÍA DE HARDENING Y ANÁLISIS DE BRECHAS

### 2.1 Estado actual (qué está listo)

- Solución con 4 proyectos `src` + 2 de pruebas, referencias entre capas **correctas** respecto a la regla de dependencias (`API → Infrastructure → Application → Domain`; Domain sin dependencias) **[CÓDIGO]**.
- Target `net10.0`, `Nullable` e `ImplicitUsings` habilitados vía `Directory.Build.props` **[CÓDIGO]**.
- Build verde; ejecución de tests operativa; coverlet instalado **[ARNÉS]**.
- Archivos `appsettings` por ambiente (Development/Staging/Production) y `launchSettings.json` **[CÓDIGO]**.
- `.gitignore` y `Readme.md` extensos **[CÓDIGO]**.
- **Nada más está operativo**: no hay endpoints de negocio, ni persistencia, ni autenticación.

### 2.2 Revisión de seguridad

| # | Hallazgo | Severidad | Evidencia |
|---|---|---|---|
| S-01 | Dependencia transitiva `System.Security.Cryptography.Pkcs 9.0.11` con CVE/GHSA **alta** y moderada | **Alta** | Arnés 0.2 |
| S-02 | Ninguna autenticación/autorización configurada: sin `AddAuthentication`, `UseAuthentication`, `UseAuthorization` ni `[Authorize]`; todo endpoint sería público | **Alta** (al implementar) | `Program.cs` |
| S-03 | `AllowedHosts: "*"` en `appsettings.json` (sin restricción de Host header) | Media | `appsettings.json` |
| S-04 | Sin política CORS definida | Media | `Program.cs` |
| S-05 | Sin manejador global de excepciones (`UseExceptionHandler`/`ProblemDetails`) ni `UseHsts` para no-Development; riesgo de filtrar trazas | Media | `Program.cs` |
| S-06 | Sin rate limiting, límites de tamaño de request ni cabeceras de seguridad | Media | `Program.cs` |
| S-07 | Swagger/OpenAPI restringido a Development (**correcto**), pero sin esquema de seguridad Bearer documentado | Baja | `Program.cs` |
| S-08 | Sin gestión de secretos: `appsettings.Production.json` y `Staging.json` vacíos (0 bytes, JSON inválido si se cargan) | Media | Archivos |
| S-09 | Archivos multimedia personales (capturas e audio de WhatsApp) están en la raíz del clon como archivos **sin versionar** (`git status`: untracked; no figuran en `git ls-files`). Riesgo: un `git add .` los publicaría; moverlos fuera del repo y reforzar `.gitignore` | Baja | `git ls-files` |
| S-10 | Sin validación/sanitización de entradas (FluentValidation referenciado sin validadores ni *pipeline behavior*) | Media (al implementar) | Código |
| S-11 | Se referencia `Microsoft.AspNetCore.Identity.EntityFrameworkCore` pero el README declara JWT propio: definir una sola estrategia de identidad para evitar doble modelo de usuarios | Baja | `.csproj` |

Aspectos positivos: `UseHttpsRedirection` activo; endpoint de ejemplo no expone datos sensibles.

### 2.3 Revisión de arquitectura

| # | Hallazgo | Severidad |
|---|---|---|
| A-01 | Las capas existen solo como carcasa; no hay entidades, casos de uso, repositorios ni interfaces que permitan evaluar SOLID | Informativo |
| A-02 | **Inconsistencia de capas**: `Serilog.AspNetCore` y `Microsoft.AspNetCore.Identity.EntityFrameworkCore` están en Infrastructure, que no es un proyecto web; la configuración de logging pertenece a la API (composición raíz) | Media |
| A-03 | `SGT.Tesis.API` referencia directamente a `Application` e `Infrastructure`; es aceptable en la composición raíz, pero conviene impedir uso directo de tipos de Infrastructure en controladores (regla de arquitectura testeable) | Baja |
| A-04 | `Microsoft.EntityFrameworkCore.Design` duplicado en API e Infrastructure | Baja |
| A-05 | No hay inyección de dependencias por capa (`AddApplication()`, `AddInfrastructure()`) | Media |
| A-06 | **Licenciamiento**: MediatR (≥ 13) y AutoMapper (≥ 15) pasaron a licencia comercial/clave de licencia para uso empresarial. Versiones declaradas 14.2.0 y 16.2.0: verificar requisito de licencia o sustituir (p. ej. *handlers* propios, mapeo manual / Mapperly) antes de producción | Media — **validar con la fuente oficial del proveedor** |
| A-07 | Duplicidad de propiedades de build entre `Directory.Build.props` y cada `.csproj` | Baja |
| A-08 | Plantilla `weatherforecast` y `record WeatherForecast` en `Program.cs` sin valor de negocio | Baja |
| A-09 | Sin Central Package Management (`Directory.Packages.props`): versiones dispersas | Baja |
| A-10 | El `Readme.md` referencia `docs/backend-architecture.md`, `docs/sprints.md` y `docker-compose.yml`, que **no existen** en el repositorio | Media |
| A-11 | URL de clonación del README usa marcador `<tu-organizacion>` | Baja |

### 2.4 Infraestructura y CI/CD

| # | Hallazgo | Severidad |
|---|---|---|
| I-01 | **No existe Dockerfile ni `docker-compose.yml`** pese a que el README los declara "obligatorio para despliegue" | Alta |
| I-02 | **No existe pipeline CI/CD** (`.github/workflows` ausente) aunque el README declara GitHub Actions | Alta |
| I-03 | Sin proveedor de BD parametrizado por ambiente: el README dice "definir" el motor; el código referencia SQL Server | Media |
| I-04 | Sin cadena de conexión, sin `UseSqlServer`, sin política de reintentos (ver issue #2) | Alta |
| I-05 | Sin *health checks* (`/health/live`, `/health/ready`) para orquestación | Media |
| I-06 | Sin estrategia de migraciones en despliegue (pipeline o *bundle*, no `Migrate()` en arranque de producción) | Media |
| I-07 | Sin estrategia de respaldo/restauración de BD ni de gestión de logs | Media |
| I-08 | Sin Dependabot/Renovate ni escaneo SAST/SCA en CI | Media |

### 2.5 Análisis de brechas (qué falta)

**Deuda técnica y vacíos funcionales**

- Todo el dominio (BaseEntity, Tesis, Etapas, Decisiones, Evaluadores, Documentos).
- `DbContext`, configuraciones EF (`MaxLength`, índices, GUID), migraciones y auditoría automática de `CreatedBy/UpdatedBy`.
- Identidad: registro/login, emisión/rotación/revocación de refresh tokens, políticas de autorización.
- Casos de uso CQRS, validadores, mapeos, *pipeline behaviors* (validación, logging, transacciones).
- API: controllers/endpoints, versionado, paginación, contrato de errores (`ProblemDetails`).
- Almacenamiento de archivos y envío de correo (abstracciones + adaptadores).
- Dashboard y cola de validación.

**Arnés de pruebas**

- 0 % de cobertura; los dos tests son vacíos y dan **falsa sensación de verde**.
- Falta: pruebas de Domain (regla `StudentId`, transiciones de etapa, inmutabilidad de decisiones), pruebas de Application, pruebas de integración con `WebApplicationFactory` + BD de prueba (Testcontainers), pruebas de autorización por rol/recurso, pruebas de arquitectura (NetArchTest/ArchUnitNET).
- Falta umbral de cobertura exigido en CI.

**Documentación**

- Faltan `docs/backend-architecture.md`, `docs/sprints.md`, ADRs, contrato OpenAPI versionado, guía de operación y *runbooks*.

**Criterios de aceptación de los issues vigentes (estado)**

| Issue | Criterio | Estado |
|---|---|---|
| #2 | Una instancia de BD por ambiente con credenciales distintas | No iniciado |
| #2 | Usuario de aplicación con permisos mínimos (sin `db_owner` en prod) | No iniciado |
| #2 | Conexión verificada con timeout y reintentos | No iniciado |
| #3 | BaseEntity con Id GUID + auditoría (CreatedAt/By, UpdatedAt/By) | No iniciado |
| #3 | Todo texto libre con `MaxLength` explícito | No iniciado |
| #3 | Reglas básicas (p. ej. `StudentId` válido) cubiertas por pruebas unitarias en Domain | No iniciado |

---

## 3. GOBIERNO, RACI Y ROADMAP

### 3.1 Matriz RACI

R = Responsable · A = Aprobador final · C = Consultado · I = Informado

| Componente / tarea | Arquitecto | Backend Lead | QA Engineer | DevOps/SRE | Product Owner |
|---|:---:|:---:|:---:|:---:|:---:|
| Modelo de dominio y reglas de negocio | A | R | C | I | C |
| Persistencia, migraciones, BD por ambiente (#2, #3) | A | R | C | R | I |
| Autenticación/autorización (JWT, RBAC/ABAC) | A | R | C | C | C |
| Remediación de vulnerabilidades y dependencias | C | R | I | A | I |
| Arnés de pruebas y umbral de cobertura | C | C | A/R | C | I |
| Pruebas de arquitectura y estilo (`.editorconfig`, analizadores) | A | R | C | I | I |
| Dockerfile, compose, variables por ambiente | C | C | I | A/R | I |
| Pipeline CI/CD y escaneo SAST/SCA | C | C | C | A/R | I |
| Observabilidad (Serilog, health checks, métricas) | C | R | I | A | I |
| Gestión de secretos (Key Vault / variables de entorno) | A | C | I | R | I |
| Requisitos funcionales y criterios de aceptación | C | C | C | I | A/R |
| Documentación técnica y ADRs | A/R | R | C | C | I |
| Decisión de licenciamiento de librerías | A | R | I | I | C |

### 3.2 Roadmap de hardening (3 fases)

**Fase 1 — Estabilización, seguridad y reparación del arnés**

1. Corregir S-01: fijar `System.Security.Cryptography.Pkcs` a versión parcheada; actualizar paquetes 10.0.13; migrar a Central Package Management.
2. Activar `TreatWarningsAsErrors` (al menos `NU1902/NU1903`) y `NuGetAudit`.
3. Crear `.editorconfig`, habilitar analizadores y corregir el fallo de `dotnet format`.
4. Eliminar `WeatherForecast` y `Class1.cs`; reforzar `.gitignore` (S-09).
5. Cerrar issues #2 y #3 con pruebas: motor de BD, instancia por ambiente, usuario de mínimo privilegio, `BaseEntity`, `MaxLength`, reglas de dominio con pruebas unitarias.
6. Reemplazar tests vacíos por pruebas reales; fijar umbral de cobertura inicial.
7. Resolver A-06 (licenciamiento MediatR/AutoMapper) y A-02 (mover Serilog a la API).
8. Crear los documentos referenciados en el README (A-10) o corregir los enlaces.
9. Restringir `AllowedHosts`, definir CORS por ambiente, añadir `UseExceptionHandler` + `ProblemDetails` y `UseHsts`.

**Fase 2 — Completitud funcional y cobertura**

1. Implementar identidad (JWT + refresh tokens con rotación/revocación) y políticas RBAC + propiedad de recurso.
2. Implementar agregado Tesis, etapas, decisiones inmutables, asignaciones y veredictos (FR-02…FR-09).
3. Implementar casos de uso CQRS con validación y *behaviors* transversales.
4. Almacenamiento de documentos y notificaciones por correo (FR-04, FR-11).
5. Dashboard KPI y cola de validación (FR-10).
6. Pruebas de integración con Testcontainers, pruebas de autorización y pruebas de arquitectura; objetivo ≥ 80 % en Domain/Application.
7. Rate limiting, límites de tamaño de carga, cabeceras de seguridad, paginación.

**Fase 3 — Despliegue y operación en producción**

1. Dockerfile multi-etapa, usuario no-root, imagen mínima; `docker-compose` para desarrollo.
2. Pipeline CI/CD (build, test, cobertura, format, `--vulnerable`, SAST, publicación de imagen) con *environments* protegidos y aprobación manual a producción.
3. Secretos en gestor institucional; configuración por ambiente por variables de entorno.
4. Migraciones aplicadas vía *bundle* en el pipeline; plan de rollback.
5. Health checks, métricas, trazas, alertas y *runbooks*; política de respaldo y restauración probada.
6. Pruebas de carga y revisión de seguridad previas a salida; Dependabot/Renovate activo.
7. Definición de SLO/SLA y proceso de respuesta a incidentes.

---

## 4. LIMITACIONES DE LA AUDITORÍA

- Se auditó el clon local `D:\sgt-tesis-backend` (contenido idéntico a la copia ZIP inicial salvo finales de línea y los archivos multimedia ajenos al repo). Historial: 3 commits del autor `Freyzzer`; `develop` difiere de `main` solo en una línea de `SGT.Tesis.slnx`. No se consultó GitHub en línea (issues, protección de ramas, Actions); los issues #2 y #3 se leyeron únicamente desde capturas de imagen.
- Un archivo de audio recibido junto con la copia inicial no fue analizado.
- La especificación funcional (sección 1.2–1.6) proviene de `Readme.md` y los issues; al no existir código de negocio, no hay comportamiento real que contrastar.
- La evaluación de licenciamiento de MediatR/AutoMapper (A-06) debe confirmarse en los términos vigentes de sus proveedores.
- No se ejecutó la API ni se realizaron pruebas dinámicas (DAST), dado que no existen endpoints de negocio.
