# Guía de contribución — SGT-Tesis Backend

Reglas de trabajo del equipo. Complementa el `Readme.md` y la guía `docs/git-workflow.pdf`.

## 1. Principio rector: desarrollo guiado por especificación (SDD)

- La fuente de verdad es [`docs/SDD_SPECIFICATION.md`](docs/SDD_SPECIFICATION.md).
- No se desarrolla una historia sin criterios de aceptación (`AC-xx`) verificables.
- Cada criterio de aceptación tiene al menos una prueba automatizada etiquetada con su ID: `[Trait("AC", "11.4")]`.
- Los puntos marcados **[SUPUESTO]** en la especificación deben estar confirmados por el Product Owner antes de implementar la historia que dependa de ellos.
- Si el código y la especificación divergen, se corrige la especificación mediante PR, no se ignora.

## 2. Arquitectura

Clean Architecture; las dependencias solo apuntan hacia adentro:

```
API → Infrastructure → Application → Domain
```

| Capa | Regla |
|---|---|
| Domain | Sin referencias a EF, ASP.NET, MediatR ni Serilog. Contiene invariantes y reglas de negocio. |
| Application | Casos de uso, validadores, DTOs e interfaces. Sin detalles de EF, HTTP ni SMTP. |
| Infrastructure | Implementa las interfaces de Application. Sin reglas de negocio. |
| API | Solo composición, endpoints y middleware. Sin lógica de negocio. |

Las reglas se verifican con pruebas de arquitectura en CI.

## 3. Flujo de Git

Ver `docs/git-workflow.pdf`. Resumen:

- `main`: producción, protegida, sin push directo.
- `develop`: integración, base de las ramas de trabajo.
- `feature/<n-issue>-<descripcion-corta>` y `hotfix/<n-issue>-<descripcion-corta>`.
- Commits en formato Conventional Commits: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`, `security`.
- Pull Requests con *Squash and merge*, mínimo una aprobación y CI en verde. Nadie aprueba su propio PR.
- Los cambios de seguridad, autorización o modelo de datos requieren dos revisiones cuando el equipo lo permita.

## 4. Definition of Done

- [ ] Criterios de aceptación cubiertos por pruebas en verde.
- [ ] `dotnet build`, `dotnet test` y `dotnet format --verify-no-changes` sin errores.
- [ ] Sin paquetes vulnerables (`dotnet list package --vulnerable --include-transitive`).
- [ ] Sin regresión de cobertura (objetivo: Domain ≥ 90 %, Application ≥ 80 %).
- [ ] Migraciones revisadas, contrato OpenAPI y documentación actualizados.
- [ ] Auditoría y logs verificados para acciones críticas.

## 5. Seguridad

- Nunca versionar secretos: cadenas de conexión, claves JWT ni credenciales. Usar variables de entorno, `dotnet user-secrets` en local y el gestor de secretos institucional en Staging y Producción.
- Autorización siempre en la capa Application, además de los atributos de los endpoints.
- Validar toda entrada; devolver errores como `ProblemDetails` sin trazas.
- Las decisiones y los registros de auditoría son inmutables.

## 6. Estilo de código

- `.editorconfig` es obligatorio; `dotnet format` debe pasar.
- Todo `string` persistido declara `MaxLength`.
- Entidades con `Id` GUID generado en el dominio y campos de auditoría heredados de `BaseEntity`.
- Nombres de dominio en español según la especificación; código de infraestructura en inglés técnico estándar.

## 7. Comandos habituales

```bash
dotnet restore
dotnet build
dotnet test --solution SGT.Tesis.slnx --coverage --coverage-output-format cobertura

# Pruebas de integración contra SQL Server real (el CI también lo hace): defina antes SGT_TEST_SQLSERVER,
# ver la sección "Pruebas" del README.
dotnet test --project tests/SGT.Tesis.IntegrationTests
dotnet format --verify-no-changes
dotnet list package --vulnerable --include-transitive
```
