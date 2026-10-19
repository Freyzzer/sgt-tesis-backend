# ADR-003 — Mediación (CQRS) y mapeo sin dependencias de licencia comercial

| Campo | Valor |
|---|---|
| Estado | **Propuesta** (pendiente de validación por el Arquitecto y el Product Owner) |
| Fecha | 2026-10-18 |
| Issue | #20 |

## Contexto

El `Readme.md` y las specs contemplan MediatR (CQRS) y AutoMapper. Ambos pasaron a un modelo de licencia dual:

- **MediatR** desde la versión 13.0.0 y **AutoMapper** desde la 15.0.0 requieren licencia comercial de Lucky Penny Software.
- Existe una *Community License* gratuita, incluso para uso comercial, si la organización tiene **ingresos anuales (o presupuesto, en entidades sin fines de lucro) inferiores a 5 000 000 USD**. Para trabajo de cliente, el cliente final debe calificar.
- Fuente: https://luckypennysoftware.com/faq (consultada el 2026-10-18). Los términos pueden cambiar; deben revisarse antes de producción.

El proyecto está pensado para entregarse a una institución, por lo que el cumplimiento de licencias recae en un tercero que hoy desconocemos. Además, ambas librerías se declararon en el proyecto pero **aún no se usan**.

## Decisión

1. **No usar MediatR ni AutoMapper.** Se retiraron las referencias de `SGT.Tesis.Application`.
2. **Casos de uso**: interfaces propias (`ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`) y *decorators* para validación, logging y transacción. Los handlers se inyectan directamente en los endpoints; no hay despachador global.
3. **Mapeo**: manual (métodos de extensión o constructores estáticos `FromEntity`), explícito y verificable por pruebas.
4. **Validación**: se mantiene FluentValidation (licencia Apache-2.0).

## Consecuencias

- (+) Sin riesgo de licencia ni advertencias en el build; menos dependencias.
- (+) Flujo de ejecución explícito y fácil de depurar; los mapeos fallan en compilación, no en tiempo de ejecución.
- (−) Algo más de código repetitivo que con MediatR.
- (−) Hay que construir el pipeline de decorators (validación, logging, transacción) en Application.

## Alternativas consideradas

| Alternativa | Motivo de descarte |
|---|---|
| Seguir con MediatR/AutoMapper bajo Community License | Depende de que la institución califique (< 5 M USD) y de mantener el cumplimiento a futuro. |
| MediatR/AutoMapper ≤ 12.x / ≤ 14.x (sin licencia) | Versiones congeladas sin parches de seguridad. |
| Mapperly (generación en compilación) / Mediator (generador de código) | Opciones válidas, pero añaden dependencias; se reconsideran si el volumen de mapeos lo justifica. |

## Cómo revertir

Si el Product Owner confirma que la institución califica para la Community License y prefiere MediatR, se reintroduce el paquete y se reemplazan las interfaces propias; el costo de cambio es bajo porque los handlers no dependen del despachador.

## Anexo — Qué hacían estas librerías y qué alternativas gratuitas existen

### Qué hace cada una (y qué hace hoy el proyecto en su lugar)

| Librería | Qué hace | En este proyecto |
|---|---|---|
| **MediatR** | *Mediador* en memoria: el endpoint envía un objeto "comando" o "consulta" y MediatR busca y ejecuta su manejador. Permite además "comportamientos" transversales (validación, logging, transacciones) alrededor de cada manejador. | Los endpoints reciben directamente el manejador por inyección de dependencias (`ICommandHandler<,>`, `IQueryHandler<,>`). La validación se hace en el endpoint (`ValidationFilter`), la auditoría con `IAuditTrail` y la transacción con el `SaveChanges` único de cada caso de uso. |
| **AutoMapper** | Copia propiedades entre objetos por convención (por ejemplo, de una entidad a un DTO) con un mapeo configurado. | Cada DTO tiene un método estático `Desde(entidad)` escrito a mano. Es explícito, los errores aparecen al compilar y no hay mapeos "mágicos". |

Ambas figuraban en `Readme.md` y en el `.csproj` de Application pero **nunca se usaron en el código**; por eso retirarlas no cambió ningún comportamiento.

### Versiones y alternativas gratuitas (verificado el 2026-10-19 en las preguntas frecuentes del proveedor)

| Opción | Licencia | Comentario |
|---|---|---|
| **Implementación propia (la actual)** | Propia, sin costo | Cero dependencias; un poco más de código repetitivo |
| **MediatR ≤ 12.x** y **AutoMapper ≤ 14.x** | Apache-2.0 / MIT (las versiones anteriores a las comerciales siguen siendo libres) | Gratis sin condiciones, pero **congeladas**: no reciben correcciones de seguridad |
| **MediatR ≥ 13 / AutoMapper ≥ 15 con licencia Community** | Comercial con plan gratuito | Gratis si la organización factura o tiene presupuesto anual por debajo de 5 M USD; la institución debe confirmar que califica y renovar la clave |
| **Mediator** (martinothamar, generador de código) | MIT | API muy parecida a MediatR, sin reflexión; mantenida. Verificar la licencia vigente al adoptarla |
| **Mapperly** (generador de código de mapeos) | Apache-2.0 | Reemplazo moderno de AutoMapper: genera el código al compilar. Verificar la licencia vigente al adoptarla |

**Recomendación:** mantener la implementación propia (no hay necesidad que justifique una dependencia). Si el equipo prefiere una librería, la mejor relación entre mantenimiento y costo es **Mediator + Mapperly**, ambas libres.

El estado de este ADR sigue **Propuesto** hasta que el Arquitecto y el Product Owner lo acepten.
