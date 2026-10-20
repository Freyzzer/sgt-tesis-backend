# ADR-005 — Políticas de etapa configurables, rondas de evaluación y reapertura

| Campo | Valor |
|---|---|
| Estado | **Aceptada** el 2026-10-20 por el responsable del proyecto. Las reglas concretas (valores por defecto) están pendientes de confirmación del Product Owner y se pueden cambiar sin reescribir código. |
| Fecha | 2026-10-20 |
| Relacionada | Decisiones #42, #43, #44, #46 · `docs/PROCESO_DE_ETAPAS.md` · `docs/DECISIONES_PRODUCT_OWNER.md` |

## Contexto

El Product Owner respondió (2026-10-20) que:

- se puede **reintentar** una etapa rechazada en las etapas 1 a 4 y en los requisitos finales, mientras que **Pasantía y Artículo** son definitivas (regla por confirmar);
- cada etapa admite **hasta tres evaluadores** y, para aprobar, deben dar *Favorable* **todos**; el administrador confirma;
- si hay observaciones, el estudiante corrige y la etapa **se vuelve a evaluar**;
- el estudiante debe **ver su proceso actualizado** y recibir una **notificación por cada evaluación** emitida;
- los documentos obligatorios son, por ahora, solo los de la propuesta y la pasantía;
- el *Consejo* es un trámite burocrático, sin actores propios en el sistema.

Hoy el dominio tiene reglas fijas en el código: **cualquier rechazo cierra la tesis**, no hay tope de evaluadores, el veredicto es solo informativo y el estudiante ve los veredictos únicamente después de la decisión.

Varias de esas respuestas son provisionales («por ahora», «por confirmar»). Si cada cambio exige tocar el dominio, el costo de que el Product Owner cambie de opinión es alto.

## Decisión

1. **Las reglas de cada etapa son una política configurable**, no código. Una política por etapa define: si se puede **reintentar** tras un rechazo, el **mínimo y máximo de evaluadores** y si **exige unanimidad** para aprobar. Los valores por defecto reflejan lo acordado y se cambian por configuración (`Flujo:Etapas:<Etapa>:…`). El dominio recibe la política como un valor y no conoce la configuración.
2. **Tres resultados con significado distinto:**
   - *Aprobada*: la etapa pasa; se abre la siguiente.
   - *Observada*: subsanable; el estudiante corrige y reenvía la misma etapa.
   - *Rechazada*: la etapa no cumple. Si la política permite reintento, el estudiante la corrige y reenvía y **la tesis sigue en curso**; si no, la tesis queda rechazada.
3. **Cada envío a revisión abre una nueva ronda.** La etapa lleva el número de **ronda**; cada veredicto y cada decisión pertenece a una ronda. La unanimidad se evalúa **solo con los veredictos vigentes de la ronda actual**, para no aprobar con opiniones sobre una versión que ya cambió. Los documentos y veredictos anteriores se conservan como historial.
4. **El administrador confirma; el sistema protege.** El administrador sigue siendo quien decide, pero no puede aprobar una etapa si alguno de sus evaluadores activos no emitió un veredicto *Favorable* en la ronda actual. Observar y rechazar no tienen esa restricción.
5. **Reapertura con justificación.** El administrador puede **reabrir** una etapa rechazada (también una definitiva) con una justificación obligatoria. Se registra como una decisión inmutable más (*Reabierta*) y devuelve la etapa al estudiante. Es la salida para corregir errores sin tocar la base de datos.
6. **Visibilidad y avisos:** el estudiante ve cada veredicto de la ronda actual apenas se emite y recibe un correo por cada uno. La identidad del evaluador es **configurable** (`Flujo:MostrarNombreEvaluador`, por defecto oculta).

## Consecuencias

- (+) Un cambio de criterio del Product Owner (otro tope de evaluadores, otra etapa definitiva, sin unanimidad) es un cambio de configuración, con pruebas que ya cubren ambas ramas.
- (+) La ronda hace explícito «se vuelve a evaluar» y mantiene un historial completo y auditable.
- (+) La reapertura evita las intervenciones manuales sobre datos inmutables.
- (−) Una columna nueva en `Etapas`, `Veredictos` y `Decisiones` (migración con datos existentes).
- (−) Más estados que probar (rechazada reintentable vs. definitiva, reapertura).
- (−) La política vive en configuración: cambiarla requiere reiniciar la API (no hay pantalla de administración de políticas).

## Alternativas consideradas

| Alternativa | Motivo de descarte |
|---|---|
| Reglas fijas por etapa en el código (un `switch` por tipo) | Es lo más rápido hoy y lo más caro si el PO cambia una regla: toca dominio, pruebas y documentación en cada cambio. |
| Políticas en una tabla de base de datos con pantalla de administración | Es la evolución natural, pero hoy no hay quien la administre ni requisito de cambiarlas sin despliegue. La interfaz de política (`IPoliticasDeEtapa`) permite cambiar de configuración a tabla sin tocar el dominio. |
| Unanimidad automática (el sistema aprueba solo) | El Product Owner pidió que **el administrador confirme**. |
| Veredicto no vinculante (como hoy) | No cumple «deben pasar todos los evaluadores». |
| Reabrir solo con acceso a la base de datos | Rompe la inmutabilidad y deja sin trazabilidad el cambio. |

## Cómo revertir o ajustar

- **Cambiar una regla** (p. ej., que Pasantía sí se pueda reintentar): editar `Flujo:Etapas:Pasantia:PermiteReintento` y reiniciar. Ver la matriz «Si el Product Owner cambia de opinión» en `docs/PROCESO_DE_ETAPAS.md`.
- **Pasar las políticas a base de datos:** implementar `IPoliticasDeEtapa` sobre una tabla; el dominio y los casos de uso no cambian.
