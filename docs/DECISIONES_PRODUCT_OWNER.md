# Decisiones que necesitamos del Product Owner

**Para:** Product Owner · **De:** equipo de desarrollo · **Fecha:** 2026-10-19  
**Lectura:** 10 minutos · **Respuesta:** una reunión de 45 minutos

## Por qué lo estamos pidiendo

El sistema ya funciona de punta a punta (estudiante, evaluador y administrador) y está probado. Para pasar a producción faltan **cuatro decisiones de negocio** que hoy están resueltas con supuestos nuestros. Si los supuestos son correctos, no hay nada que cambiar; si no, es mejor saberlo ahora y no después de la puesta en marcha.

Cada decisión trae: la pregunta, las opciones, **nuestra recomendación**, qué cambia en el sistema y el esfuerzo estimado. Al final hay un cuadro para responder marcando una opción.

> **Si no hay respuesta**, el sistema sale con lo que hace hoy (columna «Hoy»). Eso es seguro, pero puede no coincidir con el reglamento de la institución.

| # | Decisión | Hoy | Recomendación | Esfuerzo |
|---|---|---|---|---|
| 1 | Documentos obligatorios por etapa | No se exige ninguno | Exigir un documento principal por etapa | Bajo (configuración) |
| 2 | Qué pasa cuando se rechaza una etapa | Se rechaza toda la tesis | Reapertura controlada por el administrador | Medio |
| 3 | Actores y peso de los veredictos | 3 roles; el veredicto informa, no decide | Mantener 3 roles; avisar si faltan veredictos | Bajo |
| 4 | Autenticación: cuentas propias o SSO | Cuentas propias con contraseña | SSO institucional (si existe) | Medio-alto |

---

## 1. ¿Qué documentos son obligatorios en cada etapa?

**Situación actual.** El estudiante puede enviar una etapa a revisión sin haber subido nada, y la «categoría» del documento es texto libre. Los tipos permitidos son PDF y DOCX.

**Qué necesitamos.** Para cada una de las 7 etapas: qué documentos son obligatorios, en qué formato y de qué tamaño máximo.

| Etapa | Documentos obligatorios | Formato | Tamaño máximo |
|---|---|---|---|
| 1. Inscripción del estudiante | | | |
| 2. Propuesta | | | |
| 3. Consejo | | | |
| 4. Evaluación | | | |
| 5. Pasantía | | | |
| 6. Artículo | | | |
| 7. Requisitos finales | | | |

**Opciones**

- **A. Sin requisitos** (como hoy). El estudiante envía cuando quiera. Riesgo: llegan etapas vacías a los evaluadores.
- **B. Un documento principal obligatorio por etapa** y el resto opcional. *Recomendada.*
- **C. Lista cerrada de documentos por etapa**, cada uno obligatorio u opcional. Es lo más estricto y exige que el reglamento esté muy definido.

**Recomendación: B**, con PDF y DOCX y un máximo de 25 MB por archivo. Es suficiente para evitar envíos vacíos y se puede endurecer a C más adelante.

**Qué cambia.** Con A o B es solo configuración (sin programar). Con C se agrega un catálogo de categorías cerrado (mejora pequeña).

---

## 2. Si se rechaza una etapa, ¿es definitivo?

**Situación actual.** Rechazar una etapa **rechaza toda la tesis** y el estudiante puede registrar una tesis nueva. La tesis rechazada queda como historial.

**Opciones**

- **A. Definitivo** (como hoy). Simple y claro. Un error de la administración no se puede corregir sin intervenir la base de datos.
- **B. Reapertura por el administrador.** Solo el administrador puede reabrir una etapa rechazada, con justificación obligatoria; el rechazo y la reapertura quedan en el historial y en la auditoría. *Recomendada.*
- **C. Nueva oportunidad automática.** El estudiante puede reenviar la etapa rechazada sin intervención. Es lo más flexible pero debilita el rechazo como decisión.

**Recomendación: B.** Mantiene el peso del rechazo, permite corregir errores y deja trazabilidad completa.

**Preguntas que ayudan a decidir**

1. ¿Hay un número máximo de reaperturas por tesis (por ejemplo, una)?
2. ¿Hay un plazo para solicitar la reapertura?
3. ¿Quién puede pedirla: solo el administrador o también el estudiante?

**Qué cambia.** Un caso de uso nuevo («reabrir etapa»), una pantalla en el detalle de la tesis y las pruebas correspondientes.

---

## 3. ¿Hay más actores y los veredictos vinculan la decisión?

**Situación actual.** Hay tres roles: Estudiante, Evaluador y Administrador. Un evaluador emite un veredicto (*Favorable*, *Con observaciones* o *Desfavorable*) con comentarios, pero **el veredicto no cambia el estado de la etapa**: la decisión la toma el administrador.

**Preguntas**

1. ¿Existen otros actores con permisos propios (director de tesis, jurados, consejo académico)?
2. ¿Cuántos evaluadores participan por etapa y cuántos veredictos hacen falta para poder decidir?
3. ¿El veredicto es **vinculante** (por ejemplo, mayoría) o **informativo**?

**Opciones**

- **A. Mantener tres roles; veredictos informativos** (como hoy). La administración decide con la información disponible.
- **B. Igual que A, pero el sistema advierte** si se intenta decidir sin veredictos, o con un veredicto *Desfavorable* sin tratar. La decisión sigue siendo del administrador. *Recomendada.*
- **C. Veredictos vinculantes** con regla de mayoría o unanimidad y roles adicionales (director, jurado).

**Recomendación: B para la primera versión**, y dejar los roles adicionales (director, jurados) para una segunda fase cuando el reglamento los defina con precisión. Agregar un rol después no obliga a rehacer lo construido.

**Qué cambia.** Con B, una advertencia en la pantalla de decisión. Con C, una fase completa de diseño: roles, reglas de votación, pantallas y pruebas.

---

## 4. ¿Autenticación propia o inicio de sesión institucional (SSO)?

**Situación actual.** Cada persona tiene una cuenta propia en el sistema con contraseña (mínimo 12 caracteres), cambio obligatorio de la clave temporal y recuperación por correo.

**Qué necesitamos saber de TI.**

1. ¿La institución tiene un proveedor de identidad (Microsoft Entra ID / Azure AD, Google Workspace u otro compatible con OpenID Connect)?
2. ¿Exige verificación en dos pasos (MFA) para sus sistemas?
3. ¿Los roles (estudiante, evaluador, administrador) se pueden derivar de grupos del proveedor, o los asigna el administrador del sistema?

**Opciones**

- **A. Cuentas propias** (como hoy). Funciona sin depender de nadie. La institución gestiona otra contraseña más y el sistema debe proteger las credenciales.
- **B. SSO institucional (OpenID Connect).** Las personas entran con su cuenta de siempre y la seguridad (contraseñas, MFA, bajas) la gestiona TI. El sistema deja de guardar contraseñas. *Recomendada si existe un proveedor.*
- **C. SSO con una cuenta de emergencia propia** para el administrador, para el caso de que el proveedor falle.

**Recomendación: B (o C) si la institución tiene proveedor de identidad.** Es lo habitual en universidades, reduce el riesgo de seguridad y elimina el soporte de «olvidé mi contraseña». Si no existe, A es válido y se puede añadir MFA después.

**Qué cambia.** Es la decisión de mayor impacto: el inicio de sesión se reemplaza por la validación del token del proveedor, se define cómo se crean las cuentas (al primer ingreso o precargadas) y cómo se asignan los roles, y se retiran las pantallas de contraseñas. Requiere el registro de la aplicación en el proveedor (lo hace TI).

---

## Otras decisiones menores (no bloquean la salida)

Se pueden responder más adelante; hoy el sistema usa el valor indicado.

| Tema | Hoy | Pregunta |
|---|---|---|
| Visibilidad de los veredictos (#46) | El estudiante los ve después de la decisión y sin nombre del evaluador | ¿Se mantiene, o se muestran antes, o con nombre? |
| Plazos por etapa (#47) | No hay plazos; el panel marca «estancadas» tras 15 días en revisión | ¿Qué plazos tiene cada etapa y quién recibe las alertas? |
| Reenvío de una etapa observada (#48) | Se puede reenviar sin subir un documento nuevo | ¿Debe exigirse al menos un documento nuevo? |

---

## Cuadro de respuesta

Marque una opción por decisión y devuelva este documento.

| # | Decisión | Opción elegida | Observaciones |
|---|---|---|---|
| 1 | Documentos obligatorios | ☐ A  ☐ B  ☐ C | |
| 2 | Rechazo de una etapa | ☐ A  ☐ B  ☐ C | |
| 3 | Actores y veredictos | ☐ A  ☐ B  ☐ C | |
| 4 | Autenticación | ☐ A  ☐ B  ☐ C | |

**Respondido por:** ______________________  **Fecha:** ______________

---

*Referencias técnicas: issues #42, #43, #44 y #45 del repositorio; contexto general en `docs/PROJECT_STATUS.md` y supuestos en `docs/SDD_SPECIFICATION.md`.*
