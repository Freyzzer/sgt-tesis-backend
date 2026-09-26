# SGT-Tesis — Backend

Sistema de Gestión de Tesis (SGT-Tesis) para seguimiento institucional de procesos de investigación y grado. Este repositorio contiene el **backend** del sistema, construido en .NET 10 siguiendo Clean Architecture.

## 📋 Tabla de contenidos

- [Descripción del proyecto](#descripción-del-proyecto)
- [Stack tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Requisitos previos](#requisitos-previos)
- [Instalación y puesta en marcha](#instalación-y-puesta-en-marcha)
- [Configuración de ambientes](#configuración-de-ambientes)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Roles del sistema](#roles-del-sistema)
- [Flujo de tesis](#flujo-de-tesis)
- [Comandos útiles](#comandos-útiles)
- [Testing](#testing)
- [Convenciones de Git](#convenciones-de-git)
- [Roadmap / Sprints](#roadmap--sprints)
- [Contribución](#contribución)

## Descripción del proyecto

SGT-Tesis digitaliza el proceso institucional de tesis de grado, gestionando el flujo completo en 7 etapas (Estudiante → Propuesta → Consejo → Evaluación → Pasantía → Artículo → Requisitos Finales), con tres roles diferenciados: **Estudiante**, **Administrador** y **Evaluador**.

Es un sistema en **producción real** utilizado a nivel institucional, por lo que prioriza trazabilidad, auditoría de decisiones y seguridad sobre los datos académicos de los estudiantes.

## Stack tecnológico

| Componente | Tecnología |
|---|---|
| Framework | .NET 10 (LTS) |
| Lenguaje | C# 14 |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core 10 |
| Base de datos | *(definir según ambiente — ver [Configuración de ambientes](#configuración-de-ambientes))* |
| Autenticación | JWT + Refresh Tokens |
| Mediación / CQRS | MediatR |
| Validaciones | FluentValidation |
| Mapeo de objetos | AutoMapper |
| Logging | Serilog |
| Documentación de API | Swashbuckle (Swagger) |
| Contenerización | Docker |
| CI/CD | GitHub Actions |

## Arquitectura

El proyecto sigue **Clean Architecture** en 4 capas, con la regla de que las dependencias siempre apuntan hacia adentro:

```
API  →  Infrastructure  →  Application  →  Domain
```

- **Domain**: entidades, enums y reglas de negocio puras. No depende de ninguna otra capa.
- **Application**: casos de uso (Commands/Queries vía MediatR), interfaces que Infrastructure implementa, DTOs y validaciones.
- **Infrastructure**: implementación técnica — EF Core, autenticación, almacenamiento de archivos, envío de correos.
- **API**: controllers, middlewares, configuración de arranque (`Program.cs`).

Documentación ampliada de decisiones de arquitectura en [`docs/backend-architecture.md`](docs/backend-architecture.md).

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Editor: Visual Studio 2026 (v18+) para targeting completo de .NET 10, Visual Studio Code con C# Dev Kit, o Visual Studio 2022 17.14+ (limitado a compilar, no a abrir proyectos net10.0 directamente en el IDE)
- Motor de base de datos configurado (ver sección de ambientes)
- Docker Desktop (opcional para desarrollo local, obligatorio para despliegue)
- Herramienta EF Core CLI:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Instalación y puesta en marcha

1. Clonar el repositorio:
   ```bash
   git clone https://github.com/<tu-organizacion>/sgt-tesis-backend.git
   cd sgt-tesis-backend
   ```

2. Restaurar dependencias:
   ```bash
   dotnet restore
   ```

3. Configurar la cadena de conexión en `src/SGT.Tesis.API/appsettings.Development.json` (ver [Configuración de ambientes](#configuración-de-ambientes)).

4. Aplicar migraciones a la base de datos:
   ```bash
   dotnet ef database update --project src/SGT.Tesis.Infrastructure --startup-project src/SGT.Tesis.API
   ```

5. Confiar en el certificado HTTPS de desarrollo (una sola vez por máquina):
   ```bash
   dotnet dev-certs https --trust
   ```

6. Ejecutar el proyecto:
   ```bash
   dotnet run --project src/SGT.Tesis.API --launch-profile https
   ```

7. Verificar que responde en:
   ```
   https://localhost:7261/swagger
   ```

## Configuración de ambientes

El proyecto maneja 3 ambientes con configuración separada:

| Archivo | Ambiente | Notas |
|---|---|---|
| `appsettings.Development.json` | Local / desarrollo | Puede versionarse con datos ficticios |
| `appsettings.Staging.json` | Pruebas / QA | Cadena de conexión vía variable de entorno |
| `appsettings.Production.json` | Producción | **Nunca** debe contener secretos reales en el repo |

**Los secretos de producción** (cadenas de conexión, claves JWT, credenciales de correo/almacenamiento) se gestionan mediante variables de entorno o un gestor de secretos (Azure Key Vault u equivalente institucional), nunca hardcodeados ni comiteados.

Para desarrollo local, se recomienda usar `dotnet user-secrets` en vez de escribir credenciales directamente en `appsettings.Development.json`:

```bash
dotnet user-secrets init --project src/SGT.Tesis.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "tu-cadena-local" --project src/SGT.Tesis.API
```

## Estructura del proyecto

```
sgt-tesis-backend/
├── src/
│   ├── SGT.Tesis.Domain/          # Entidades, enums, reglas de negocio
│   ├── SGT.Tesis.Application/     # Casos de uso (CQRS con MediatR)
│   ├── SGT.Tesis.Infrastructure/  # EF Core, JWT, almacenamiento, correo
│   └── SGT.Tesis.API/             # Controllers y arranque de la aplicación
├── tests/
│   ├── SGT.Tesis.UnitTests/
│   └── SGT.Tesis.IntegrationTests/
├── docs/                          # Documentación técnica y de arquitectura
└── docker-compose.yml
```

Detalle completo de carpetas internas por proyecto en [`docs/backend-architecture.md`](docs/backend-architecture.md).

## Roles del sistema

| Rol | Descripción |
|---|---|
| **Estudiante** | Registra su tesis, avanza por el flujo de 7 etapas, sube documentación y recibe retroalimentación consolidada. |
| **Administrador** | Única autoridad que aprueba/rechaza el avance de cada etapa. Asigna evaluadores, ve el dashboard de KPIs y la cola de validación institucional. |
| **Evaluador** | Emite su veredicto técnico solo sobre la(s) etapa(s) que le fueron asignadas. No tiene autoridad para mover el estado de la tesis. |

## Flujo de tesis

```
Estudiante → Propuesta → Consejo → Evaluación → Pasantía → Artículo → Requisitos Finales
```

Cada etapa tiene un estado propio (`Pendiente`, `EnRevision`, `Observado`, `Aprobado`, `Rechazado`). El avance de una etapa a la siguiente requiere siempre una decisión explícita del Administrador, registrada de forma inmutable para auditoría.

## Comandos útiles

```bash
# Compilar toda la solución
dotnet build

# Ejecutar la API
dotnet run --project src/SGT.Tesis.API

# Crear una nueva migración
dotnet ef migrations add NombreDeLaMigracion --project src/SGT.Tesis.Infrastructure --startup-project src/SGT.Tesis.API

# Aplicar migraciones pendientes
dotnet ef database update --project src/SGT.Tesis.Infrastructure --startup-project src/SGT.Tesis.API

# Ejecutar todas las pruebas
dotnet test

# Levantar contenedores (API + base de datos) con Docker Compose
docker-compose up -d
```

## Testing

- **Pruebas unitarias** (`SGT.Tesis.UnitTests`): reglas de negocio de Domain y lógica de casos de uso de Application, sin dependencias externas.
- **Pruebas de integración** (`SGT.Tesis.IntegrationTests`): endpoints de la API contra una base de datos de prueba.

```bash
dotnet test --filter "FullyQualifiedName~UnitTests"
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

## Convenciones de Git

- `main` — rama protegida, siempre desplegable. No se pushea directo.
- `develop` — integración de features antes de pasar a producción.
- `feature/<nombre-tarea>` — una rama por tarea del sprint (ej. `feature/auth-jwt`).
- `hotfix/<nombre>` — correcciones urgentes sobre `main`.

Formato de commits sugerido (Conventional Commits):

```
feat: agrega endpoint de asignación de evaluadores
fix: corrige validación de avance de etapa sin decisión previa
chore: configura Directory.Build.props
docs: actualiza README con pasos de instalación
```

Todo Pull Request hacia `develop` o `main` requiere al menos una revisión aprobada antes de mergear.

## Roadmap / Sprints

La planificación completa por sprints, con tareas y criterios de aceptación, está en [`docs/sprints.md`](docs/sprints.md).

## Contribución

1. Crea una rama desde `develop`: `git checkout -b feature/nombre-tarea`
2. Realiza tus cambios y agrega pruebas si aplica.
3. Verifica que `dotnet build` y `dotnet test` pasen sin errores.
4. Abre un Pull Request hacia `develop` describiendo el cambio y referenciando el Issue correspondiente.