# SGT-Tesis — Backend

Sistema de Gestión de Tesis (SGT-Tesis) para el seguimiento institucional de los procesos de investigación y grado. Este repositorio contiene el **backend**, construido en .NET 10 con Clean Architecture. Reglas de trabajo del equipo en [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Contenido

- [Qué hace](#qué-hace)
- [Documentación](#documentación)
- [Stack tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Puesta en marcha](#puesta-en-marcha)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Roles y flujo de una tesis](#roles-y-flujo-de-una-tesis)
- [Pruebas](#pruebas)
- [Flujo de trabajo con Git](#flujo-de-trabajo-con-git)

## Qué hace

Digitaliza el proceso de tesis de grado en **7 etapas** (Estudiante → Propuesta → Consejo → Evaluación → Pasantía → Artículo → Requisitos finales) con tres roles: **Estudiante**, **Evaluador** y **Administrador**. Prioriza trazabilidad, auditoría de decisiones y seguridad de los datos académicos.

Funcionalidad disponible (API REST, `/api/v1`):

| Área | Qué permite |
|---|---|
| Identidad | Login con JWT de corta vida, refresh tokens rotados, cierre de sesión, bloqueo tras 5 intentos fallidos |
| Usuarios | El Administrador crea, activa/desactiva y cambia el rol de usuarios |
| Tesis | El Estudiante registra su tesis, la consulta y envía cada etapa a revisión |
| Documentos | Carga de PDF/DOCX validados por contenido, versionado por categoría, descarga segura |
| Evaluación | Asignación de evaluadores por etapa y veredictos versionados |
| Decisiones | Aprobar, observar o rechazar una etapa (inmutables, con justificación y concurrencia optimista) |
| Retroalimentación | El estudiante ve decisiones y veredictos anónimos por etapa |
| Auditoría | Registro inmutable de las acciones relevantes, consultable por el Administrador |
| Notificaciones | Correos mediante outbox transaccional con reintentos |
| Dashboard | KPIs y cola de validación para el Administrador |
| Operación | Health checks, imágenes Docker, CI/CD, pruebas de carga |

## Documentación

| Documento | Contenido |
|---|---|
| [`docs/PROJECT_STATUS.md`](docs/PROJECT_STATUS.md) | **Estado del proyecto**, supuestos aplicados, pendientes y preguntas para el Product Owner |
| [`docs/SDD_SPECIFICATION.md`](docs/SDD_SPECIFICATION.md) (y `.docx`) | Especificación: requerimientos, historias de usuario, criterios de aceptación y arquitectura |
| [`docs/CONFIGURATION.md`](docs/CONFIGURATION.md) | Variables de configuración por ambiente, endpoints por área, resultados de carga |
| [`docs/OPERATIONS.md`](docs/OPERATIONS.md) | Runbook: despliegue, migraciones, respaldo y restauración, incidentes |
| [`docs/SECURITY_REVIEW.md`](docs/SECURITY_REVIEW.md) | Revisión de seguridad, controles existentes y riesgos residuales |
| [`docs/DATABASE.md`](docs/DATABASE.md) | Base de datos por ambiente, usuarios de mínimo privilegio |
| [`docs/adr/`](docs/adr) | Decisiones de arquitectura (ADR-003 mediación y mapeo, ADR-004 identidad) |
| [`docs/SYSTEM_SPEC_AND_GAP_ANALYSIS.md`](docs/SYSTEM_SPEC_AND_GAP_ANALYSIS.md) | Auditoría inicial del repositorio (histórica) |
| [`docs/git-workflow.pdf`](docs/git-workflow.pdf) | Guía de ramas, commits y Pull Requests |

## Stack tecnológico

| Componente | Tecnología |
|---|---|
| Plataforma | .NET 10, C# 14, ASP.NET Core (minimal APIs) |
| Datos | Entity Framework Core 10 sobre SQL Server |
| Autenticación | JWT (HS256) + refresh tokens propios; PBKDF2 para contraseñas |
| Validación | FluentValidation |
| Logging | Serilog (consola, con correlación por solicitud) |
| Correo | MailKit (SMTP) |
| Documentación de API | OpenAPI + Swagger (solo en Development) |
| Pruebas | xUnit v3 sobre Microsoft Testing Platform, NetArchTest, SQLite en memoria; k6 para carga |
| Contenedores y CI/CD | Docker, GitHub Actions, GHCR |

> No se usan MediatR ni AutoMapper (licenciamiento comercial); ver [ADR-003](docs/adr/ADR-003-mediacion-y-mapeo.md).

## Arquitectura

Clean Architecture en 4 capas; las dependencias solo apuntan hacia adentro y una prueba de arquitectura lo verifica en cada compilación:

```
API  →  Infrastructure  →  Application  →  Domain
```

- **Domain:** entidades, invariantes y reglas de negocio puras (`TesisGrado`, `Etapa`, `Decision`…).
- **Application:** casos de uso (`ICommandHandler`/`IQueryHandler`), validadores, DTOs e interfaces.
- **Infrastructure:** EF Core, repositorios, JWT, almacenamiento de archivos, correo, outbox.
- **API:** endpoints, middleware y composición.

## Puesta en marcha

### Opción A: todo en contenedores (recomendada)

Requiere Docker Desktop.

```bash
cp .env.example .env        # complete los secretos (el .env no se versiona)
docker compose up --build
```

Levanta SQL Server, crea la base y los usuarios de mínimo privilegio, aplica migraciones, arranca la API en http://localhost:8080 (Swagger en `/swagger`) y Mailpit en http://localhost:8025. El primer Administrador se crea con `ADMIN_CORREO` y `ADMIN_PASSWORD`.

### Opción B: con el SDK de .NET

Requiere el [SDK de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) y una instancia de SQL Server.

```bash
dotnet restore
dotnet user-secrets init --project src/SGT.Tesis.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena>" --project src/SGT.Tesis.API
dotnet user-secrets set "Jwt:SigningKey" "<32 o más caracteres aleatorios>" --project src/SGT.Tesis.API
dotnet user-secrets set "Jwt:Issuer" "sgt-tesis-api" --project src/SGT.Tesis.API
dotnet user-secrets set "Jwt:Audience" "sgt-tesis-clientes" --project src/SGT.Tesis.API
dotnet user-secrets set "Bootstrap:AdminCorreo" "admin@institucion.edu" --project src/SGT.Tesis.API
dotnet user-secrets set "Bootstrap:AdminPassword" "<contraseña inicial>" --project src/SGT.Tesis.API
dotnet ef database update --project src/SGT.Tesis.Infrastructure
dotnet run --project src/SGT.Tesis.API
```

Todas las variables, por ambiente, están en [`docs/CONFIGURATION.md`](docs/CONFIGURATION.md). La API **no arranca** sin cadena de conexión, clave JWT y ruta de almacenamiento, y fuera de Development tampoco sin `AllowedHosts` y correo SMTP: es intencional.

## Estructura del repositorio

```
├── src/
│   ├── SGT.Tesis.Domain/          # Entidades y reglas de negocio
│   ├── SGT.Tesis.Application/     # Casos de uso, validadores, interfaces
│   ├── SGT.Tesis.Infrastructure/  # EF Core, repositorios, JWT, correo, archivos
│   └── SGT.Tesis.API/             # Endpoints, middleware, arranque
├── tests/
│   ├── SGT.Tesis.UnitTests/           # Dominio y casos de uso
│   ├── SGT.Tesis.IntegrationTests/    # API real (WebApplicationFactory + SQLite en memoria)
│   ├── SGT.Tesis.ArchitectureTests/   # Reglas de dependencias entre capas
│   └── load/                          # Pruebas de carga k6
├── scripts/db/, scripts/load/     # Scripts SQL (usuarios, restricciones, datos de carga)
├── docs/                          # Documentación técnica y de operación
├── Dockerfile, docker-compose.yml # Imágenes y entorno local
└── .github/                       # CI, publicación de imágenes, Dependabot
```

## Roles y flujo de una tesis

| Rol | Descripción |
|---|---|
| **Estudiante** | Registra su tesis, sube documentos, envía cada etapa a revisión y ve la retroalimentación. |
| **Evaluador** | Emite su veredicto técnico solo en las etapas que le asignaron; no puede cambiar estados. |
| **Administrador** | Única autoridad que aprueba, observa o rechaza una etapa; gestiona usuarios, asigna evaluadores y ve auditoría y dashboard. |

```
Estudiante → Propuesta → Consejo → Evaluación → Pasantía → Artículo → Requisitos finales
```

Cada etapa pasa por `Pendiente → EnRevision → (Aprobado | Observado | Rechazado)`. Aprobar abre la siguiente; observar la devuelve al estudiante para corregir y reenviar; rechazar rechaza la tesis. Cada decisión se registra de forma inmutable.

## Pruebas

```bash
dotnet test --solution SGT.Tesis.slnx                                                 # todas
dotnet test --solution SGT.Tesis.slnx --coverage --coverage-output-format cobertura   # con cobertura
dotnet format SGT.Tesis.slnx --verify-no-changes                                      # estilo
dotnet list SGT.Tesis.slnx package --vulnerable --include-transitive                  # paquetes vulnerables
```

Cada criterio de aceptación de la especificación tiene pruebas etiquetadas (`[Trait("AC", "11.4")]`). Las pruebas de API usan SQLite en memoria y **no se ejecutan contra SQL Server en el CI** (el stack completo sí se verificó con Docker; ver [`docs/PROJECT_STATUS.md`](docs/PROJECT_STATUS.md)).

## Flujo de trabajo con Git

- `main`: producción, protegida. `develop`: integración, base de las ramas de trabajo.
- Ramas `feature/<n-issue>-<descripcion>`; commits en formato Conventional Commits.
- Pull Requests hacia `develop` con una aprobación y CI en verde; *Squash and merge*.
- Detalle en [`CONTRIBUTING.md`](CONTRIBUTING.md) y [`docs/git-workflow.pdf`](docs/git-workflow.pdf).
