# Sistema de Gestión de Tesis (SGT) — Manual de Usuario

> **Documentación elaborada por:** Sara Yulieth Quimbayo Osorio y Freyzzer Valencia.

| | |
|---|---|
| **Documento** | Manual de Usuario |
| **Sistema** | Sistema de Gestión de Tesis de Grado (SGT-Tesis) |
| **Dirigido a** | Docente asesora, estudiantes, evaluadores y administradores |
| **Versión del documento** | 1.0 |
| **Fecha** | 21 de octubre de 2026 |
| **Documento complementario** | [Manual Técnico y de Arquitectura](MANUAL_TECNICO_Y_ARQUITECTURA.md) |

---

## Contenido

1. [Presentación del sistema](#1-presentación-del-sistema)
2. [Actores y roles](#2-actores-y-roles)
3. [Conceptos que conviene conocer](#3-conceptos-que-conviene-conocer)
4. [Acceso al sistema y gestión de credenciales](#4-acceso-al-sistema-y-gestión-de-credenciales)
5. [Guía del estudiante](#5-guía-del-estudiante)
6. [Guía del evaluador](#6-guía-del-evaluador)
7. [Guía del administrador](#7-guía-del-administrador)
8. [Avisos por correo electrónico](#8-avisos-por-correo-electrónico)
9. [Mensajes frecuentes y cómo resolverlos](#9-mensajes-frecuentes-y-cómo-resolverlos)
10. [Glosario](#10-glosario)
11. [Aspectos pendientes de confirmación institucional](#11-aspectos-pendientes-de-confirmación-institucional)

---

## 1. Presentación del sistema

### 1.1 ¿Qué es el SGT?

El **Sistema de Gestión de Tesis (SGT)** es una aplicación web que acompaña el trabajo de grado de un estudiante desde que registra su tesis hasta que se aprueban todos los requisitos de grado. En un mismo lugar, el estudiante entrega sus documentos, los evaluadores emiten su concepto y la administración decide y deja constancia de cada decisión.

### 1.2 ¿Qué problema resuelve?

Antes, el seguimiento de una tesis dependía de correos, archivos sueltos y registros manuales. Esto dificultaba saber en qué etapa estaba cada trabajo, quién debía actuar, qué versión de un documento era la vigente y por qué se tomó una decisión. El sistema resuelve esto con:

- **Un solo expediente por tesis**, con todas las versiones de los documentos.
- **Un recorrido fijo de siete etapas**, de modo que siempre se sabe cuál es la etapa abierta y qué se espera de cada persona.
- **Trazabilidad completa**: cada decisión y cada acción importante quedan registradas y no pueden alterarse.
- **Comunicación inmediata**: el estudiante ve su avance en todo momento y recibe un aviso por correo cada vez que hay novedades.
- **Visión de conjunto para la administración**, con indicadores y una cola de etapas pendientes de validar.

### 1.3 Alcance

El sistema cubre el registro de la tesis, la carga y el versionado de documentos, la asignación de evaluadores, la emisión de conceptos, las decisiones de la administración, la retroalimentación al estudiante, las notificaciones por correo, la auditoría y los indicadores de seguimiento.

Quedan **fuera de su alcance** actual: el manejo de plazos y alertas por vencimiento (el Product Owner indicó que no se manejan), la firma digital de documentos y la integración con otros sistemas académicos.

---

## 2. Actores y roles

Cada persona tiene **un solo rol** y el sistema solo le muestra lo que ese rol puede hacer. Aunque alguien intente entrar a una pantalla que no le corresponde, el sistema lo redirige a su pantalla de inicio.

| Rol | Quién es | Qué puede hacer |
|---|---|---|
| **Estudiante** | Dueño de una tesis | Registrar su tesis (una activa a la vez), subir documentos, enviar la etapa a revisión, consultar su avance y leer la retroalimentación. Solo ve **su** tesis. |
| **Evaluador** | Docente o jurado que revisa | Ver las etapas que le asignaron, leer los documentos y emitir (o actualizar) su veredicto mientras la etapa está en revisión. Solo ve las tesis que tiene **asignadas**. |
| **Administrador** | Autoridad institucional | Asignar evaluadores, **decidir** (aprobar, observar, rechazar), reabrir etapas rechazadas, gestionar usuarios, consultar el panel de indicadores y la auditoría. |

> **Importante.** Los evaluadores emiten su concepto pero **no deciden**: la decisión sobre cada etapa es siempre del administrador. El sistema nunca aprueba una etapa por sí solo.

### 2.1 Resumen de permisos

| Capacidad | Estudiante | Evaluador | Administrador |
|---|:---:|:---:|:---:|
| Registrar y ver su tesis | Propia | — | Todas |
| Subir documentos | Propia, etapa abierta | — | — |
| Ver tesis y documentos | Propia | Solo asignadas | Todas |
| Emitir veredicto | — | Etapa asignada | — |
| Decidir sobre una etapa | — | — | Sí |
| Reabrir una etapa rechazada | — | — | Sí |
| Asignar y retirar evaluadores | — | — | Sí |
| Gestionar usuarios | — | — | Sí |
| Panel de indicadores y cola de validación | — | — | Sí |
| Consultar la auditoría | — | — | Sí |
| Ver retroalimentación consolidada | Propia | — | Todas |

---

## 3. Conceptos que conviene conocer

### 3.1 Las siete etapas

Una tesis recorre **siete etapas, en orden**. Solo hay una abierta a la vez y no se pasa a la siguiente hasta que la actual se aprueba.

| N.º | Etapa | En qué consiste |
|---|---|---|
| 1 | Inscripción del estudiante | El estudiante registra su tesis. |
| 2 | Propuesta | Presenta su propuesta. **Exige el documento de categoría «Propuesta».** |
| 3 | Consejo | Trámite administrativo; lo gestiona la administración. |
| 4 | Evaluación | Evaluación del trabajo por los evaluadores. |
| 5 | Pasantía | Modalidad de trabajo de grado similar a las prácticas. **Exige el documento de categoría «Pasantía».** |
| 6 | Artículo | Artículo del que se toma información para referenciar la propuesta. |
| 7 | Requisitos finales | Validación de que se cumple todo lo exigido para el grado. |

Cuando se aprueba la etapa 7, la tesis queda **Finalizada**.

### 3.2 Estados de una etapa

| Estado | Significado | Quién actúa ahora |
|---|---|---|
| **Pendiente** | La etapa está abierta y aún no se envía. | Estudiante |
| **En revisión** | Se envió; los evaluadores opinan y la administración decide. | Evaluadores y administrador |
| **Con observaciones** | La administración pidió correcciones. | Estudiante |
| **Aprobada** | La etapa cumplió; se abre la siguiente. | — |
| **Rechazada** | La etapa no cumplió. Según la etapa, el estudiante puede corregir y reenviar, o el proceso se cierra. | Estudiante (si se permite reintento) |

El estado siempre se muestra con **texto y color**, nunca solo con color.

### 3.3 Rondas

Cada vez que el estudiante **envía** una etapa a revisión comienza una **ronda** (ronda 1, ronda 2…). Los veredictos y las decisiones pertenecen a una ronda. Si la etapa vuelve al estudiante y la reenvía, los evaluadores **vuelven a evaluar** la versión nueva. Los comentarios de rondas anteriores se conservan como historial, pero no cuentan para la ronda actual.

### 3.4 Veredicto y decisión: no son lo mismo

| | **Veredicto** | **Decisión** |
|---|---|---|
| Quién lo emite | Un evaluador asignado | El administrador |
| Valores | *Favorable*, *Con observaciones*, *Desfavorable* | *Aprobada*, *Observada*, *Rechazada* (y *Reabierta*) |
| Efecto | Informa; **no cambia el estado** de la etapa | Cambia el estado de la etapa |
| ¿Se puede modificar? | Sí, mientras la etapa esté en revisión (cada cambio queda como versión nueva) | **No**: es inmutable |

### 3.5 Cuándo se puede aprobar una etapa

Cada etapa admite **hasta tres evaluadores**. Para **aprobar**, todos los evaluadores asignados deben haber dado un veredicto **Favorable en la ronda actual**. Si falta alguno o hay uno que no es favorable, el sistema no deja aprobar y lo explica (por ejemplo, «2 de 3 favorables»). Observar y rechazar no tienen esa restricción. Una etapa sin evaluadores asignados (como el Consejo) la aprueba directamente el administrador.

### 3.6 Si una etapa se rechaza

| Etapas | ¿Se puede reintentar? |
|---|---|
| 1 Inscripción, 2 Propuesta, 3 Consejo, 4 Evaluación | **Sí**: el estudiante corrige y reenvía la misma etapa, sin perder la tesis y sin límite de intentos. |
| 5 Pasantía, 6 Artículo | **No**: el rechazo es definitivo y la tesis queda rechazada. El estudiante puede registrar una tesis nueva. |
| 7 Requisitos finales | Se reintenta por ahora; **falta confirmación** (ver [sección 11](#11-aspectos-pendientes-de-confirmación-institucional)). |

Aun en un rechazo definitivo, el **administrador puede reabrir** la etapa con una justificación (ver [7.6](#76-reabrir-una-etapa-rechazada)).

---

## 4. Acceso al sistema y gestión de credenciales

### 4.1 Cómo obtener una cuenta

Las cuentas las crea el **administrador**. Quien la recibe obtiene su correo institucional y una **contraseña temporal**. No existe registro público: si usted necesita acceso, solicítelo a la administración.

> Está previsto que el ingreso sea con la cuenta de correo institucional (inicio de sesión único). Mientras esa integración se confirma con el área de tecnología, el ingreso es con correo y contraseña, como se describe a continuación.

### 4.2 Iniciar sesión

1. Abra la dirección del sistema en su navegador.
2. En **«Correo institucional»** escriba su correo.
3. En **«Contraseña»** escriba su contraseña.
4. Pulse **«Ingresar»**.

El sistema lo lleva a la pantalla inicial de su rol. Si antes intentaba abrir una página concreta, el sistema lo devuelve allí después de ingresar.

**Qué debe saber:**

- Si el correo o la contraseña no son correctos, el sistema muestra un **mensaje genérico**; no indica cuál de los dos falló. Es una medida de seguridad.
- Tras **5 intentos fallidos consecutivos**, la cuenta se **bloquea durante 15 minutos**. Espere ese tiempo o use la recuperación de contraseña.
- La sesión es de corta duración y se renueva sola mientras usted trabaja. Si permanece inactivo mucho tiempo, el sistema le pedirá ingresar de nuevo.

### 4.3 Contraseña temporal: primer ingreso

Si su cuenta fue creada por el administrador, el sistema le exigirá **cambiar la contraseña antes de hacer cualquier otra cosa**: mientras sea temporal, todas las pantallas lo redirigen a **«Cambio de contraseña»** y solo puede cambiarla o salir.

### 4.4 Política de contraseñas

La nueva contraseña debe tener, como mínimo:

- **12 caracteres**,
- una **mayúscula**,
- una **minúscula**,
- un **número** y
- un **símbolo**.

Además, debe ser **distinta de la actual**. El formulario le indica qué requisito falta mientras escribe.

### 4.5 Cambiar la contraseña voluntariamente

1. Pulse **«Contraseña»** en el menú superior.
2. Escriba su contraseña actual, la contraseña nueva y repítala.
3. Pulse **«Cambiar contraseña»** (o **«Cancelar»** para volver).

Al guardar, **se cierran todas sus sesiones abiertas** (por seguridad) y debe ingresar de nuevo. Recibirá un correo avisando del cambio. Si usted no lo hizo, avise de inmediato a la administración.

### 4.6 Recuperar una contraseña olvidada

1. En la pantalla de ingreso, elija **«Olvidé mi contraseña»**.
2. Escriba su correo institucional y envíe el formulario.
3. El sistema responde siempre lo mismo, **exista o no la cuenta**, para no revelar qué correos están registrados.
4. Si la cuenta existe, recibirá un correo con un **enlace de un solo uso**.
5. Abra el enlace, escriba la nueva contraseña y confirme.

**Detalles:**

- El enlace **vence a los 30 minutos** y solo sirve **una vez**.
- Solo es válido el **último** enlace solicitado, y puede pedir uno por minuto como máximo.
- Si el correo trae un **código** en lugar de un enlace, abra «Restablecer contraseña» y péguelo allí.
- Al restablecer, se levantan los bloqueos y se cierran todas las sesiones.

### 4.7 Cerrar sesión

Pulse **«Salir»** en la esquina superior derecha. Hágalo siempre que use un equipo compartido.

---

## 5. Guía del estudiante

### 5.1 Registrar mi tesis

La primera vez que ingresa, el sistema le pide registrar su tesis.

1. Escriba el **título** (máximo 300 caracteres).
2. Escriba el **resumen** (máximo 3000 caracteres).
3. Pulse **«Registrar tesis»**.

Se crea la tesis con la **etapa 1** abierta en estado **Pendiente**. Solo puede tener **una tesis en curso a la vez**; si su tesis queda rechazada de forma definitiva, puede registrar otra.

### 5.2 Mi tesis: la pantalla principal

Muestra el título, el estado general de la tesis (*En curso*, *Finalizada* o *Rechazada*), la etapa actual y la **lista de las siete etapas**, cada una con su estado. Si una etapa va en la ronda 2 o más, se indica; si fue rechazada pero puede corregirse, el sistema se lo dice («Puede corregirla y reenviarla»). Desde cada etapa, el enlace **«Ver documentos y retroalimentación»** abre su detalle.

### 5.3 Subir documentos

Solo puede subir documentos cuando la etapa es la **actual** y está **Pendiente**, **Con observaciones** o **Rechazada con reintento**. Mientras está **en revisión**, **aprobada** o rechazada de forma definitiva, está bloqueada.

1. Abra la etapa.
2. En **«Subir documento»**, elija la **categoría** (*Propuesta*, *Pasantía*, *Informe*, *Anexo*, *Carta*, *Artículo* u *Otro*).
3. Pulse **«Elegir archivo»** y seleccione el archivo.
4. Pulse **«Subir»**.

**Reglas de los archivos:**

| Regla | Detalle |
|---|---|
| Formatos | **PDF** y **DOCX**. El sistema verifica el contenido real del archivo, no solo la extensión: un archivo con la extensión cambiada es rechazado. |
| Tamaño | Hasta **20 MB** por archivo. |
| Versiones | Si sube un archivo de la **misma categoría** que uno anterior, se registra como **versión nueva** (v2, v3…). **Nada se sobrescribe**: las versiones anteriores se conservan. |
| Descarga | Puede descargar cualquiera de sus documentos desde la tabla de la etapa. |

### 5.4 Documentos obligatorios

Para poder enviar una etapa a revisión debe haber cargado sus documentos obligatorios:

| Etapa | Documento obligatorio |
|---|---|
| 2. Propuesta | Categoría **Propuesta** |
| 5. Pasantía | Categoría **Pasantía** |
| Las demás | Ninguno por ahora |

Si falta alguno, el sistema **no envía** la etapa y le indica cuáles faltan. La administración puede ajustar esta lista.

### 5.5 Enviar la etapa a revisión

1. Con sus documentos cargados, pulse **«Enviar etapa a revisión»** (si es un reenvío, el botón dice **«Reenviar etapa a revisión»**).
2. Confirme el aviso. **Mientras esté en revisión no podrá modificarla.**

La etapa pasa a **En revisión** y comienza una ronda nueva. Los evaluadores asignados y la administración reciben un aviso por correo.

### 5.6 Seguir mi proceso y leer la retroalimentación

La sección **«Retroalimentación»** de cada etapa se organiza por **ronda** y muestra:

- el **comentario de cada evaluador**, con su concepto (*Favorable*, *Con observaciones*, *Desfavorable*). Se ve **apenas el evaluador lo emite**, sin esperar la decisión de la administración;
- la **decisión de la administración** (*Aprobada*, *Observada*, *Rechazada* o *Reabierta*) con su justificación y la fecha.

**Anonimato.** Por defecto los evaluadores aparecen como **«Evaluador 1», «Evaluador 2»…**, sin nombre ni correo. Si el evaluador cambia su concepto, usted ve solo la versión más reciente.

Mientras la etapa está en revisión, la pantalla indica la ronda en curso y que recibirá un correo cada vez que un evaluador emita su veredicto.

### 5.7 Cuando la etapa tiene observaciones

1. Lea la retroalimentación de la ronda.
2. Suba los documentos corregidos (se registran como versiones nuevas).
3. Pulse **«Reenviar etapa a revisión»**. Comienza una ronda nueva y los evaluadores **vuelven a evaluar**.

### 5.8 Cuando la etapa es rechazada

- **Si se permite el reintento** (por ejemplo, en las etapas 1 a 4), la pantalla dice: *«Esta etapa fue rechazada, pero puede corregirla y volver a enviarla»*. Proceda como con las observaciones. La tesis sigue en curso y no hay límite de intentos.
- **Si el rechazo es definitivo**, la pantalla informa que el proceso se cerró y que, si cree que hubo un error, debe comunicarse con la coordinación: el administrador puede **reabrir** la etapa. Si la reabren, recibirá un correo y podrá corregir y reenviar.

### 5.9 Cuando la etapa es aprobada

El sistema abre automáticamente la **etapa siguiente** en estado Pendiente y le envía un correo. Al aprobarse la etapa 7, la tesis queda **Finalizada**.

---

## 6. Guía del evaluador

### 6.1 Mis evaluaciones

Al ingresar verá **«Mis evaluaciones»**: las etapas que le asignó la administración, con la tesis, el estudiante, la etapa y su estado. Por defecto se muestran **solo las pendientes de su veredicto** y primero las que llevan más tiempo en revisión. Recibirá un **correo** cuando se le asigne una etapa y cada vez que el estudiante la envíe o reenvíe.

### 6.2 Revisar una etapa

Pulse **«Abrir»** en la etapa que desea evaluar. Verá:

- el **título** y el **resumen** de la tesis,
- la etapa y su estado, y la **ronda** actual,
- los **documentos** de la etapa, que puede descargar.

Si el estudiante corrigió y reenvió la etapa (ronda 2 o más), el sistema se lo recuerda: **debe emitir un veredicto nuevo para esa ronda**, aunque ya hubiera emitido uno antes.

### 6.3 Emitir su veredicto

En **«Mi veredicto»**:

1. Elija el **concepto**: *Favorable*, *Con observaciones* o *Desfavorable*.
2. Escriba los **comentarios para el estudiante** (obligatorios, hasta 4000 caracteres). Sea claro y concreto: serán lo que el estudiante use para corregir.
3. Pulse **«Emitir veredicto»**.

**Qué debe tener presente:**

- El estudiante **ve su veredicto apenas lo emite** y recibe un aviso por correo (el correo no incluye su concepto ni sus comentarios; esos se leen en el sistema).
- Por defecto, el estudiante **no ve su nombre**: lo ve como «Evaluador 1», «2» o «3».
- Puede **actualizar** su veredicto mientras la etapa esté en revisión; cada cambio queda como versión nueva y el estudiante ve la más reciente.
- Su veredicto **no cambia el estado** de la etapa: la decisión es del administrador. Pero **condiciona la aprobación**: para aprobar, todos los evaluadores deben ser favorables en la ronda.
- Cuando la etapa deja de estar en revisión, ya no puede emitir más veredictos.

---

## 7. Guía del administrador

### 7.1 Panel de seguimiento

La pantalla inicial presenta:

- **Indicadores:** tesis registradas, tesis por estado, tesis en curso por etapa, tiempo promedio (en días) por etapa cerrada, tasas de aprobación, observación y rechazo, y etapas **en revisión**.
- **Etapas estancadas:** las que llevan más de **15 días** esperando validación.
- **Carga por evaluador:** asignaciones activas y veredictos pendientes de cada uno.
- **Cola de validación:** las etapas en revisión, **de la que más tiempo espera a la más reciente**, con el estudiante, el título, los días en revisión y cuántos evaluadores asignados ya emitieron su veredicto.

**Exportar a CSV.** Desde la parte superior puede exportar los indicadores y la cola de validación a archivos CSV, en el formato estándar (separador coma) o en el formato para Excel en español (separador punto y coma). Los indicadores salen en un formato listo para una tabla dinámica.

### 7.2 Gestionar usuarios

En **«Usuarios»** puede listar, filtrar (por rol y estado), **crear** cuentas, **activar o desactivar** usuarios y **cambiar su rol**.

- Al crear un usuario, ingrese nombres, apellidos, correo, rol y una **contraseña temporal** que cumpla la política. La persona deberá cambiarla en su primer ingreso. Comparta esa contraseña por un medio seguro.
- El **correo es único** (sin distinguir mayúsculas).
- **Desactivar** una cuenta impide su acceso de inmediato y cierra sus sesiones.
- Por seguridad, **no puede cambiar su propio rol ni desactivarse**, y el sistema **no deja que quede sin ningún administrador activo**.

### 7.3 Buscar una tesis

En **«Tesis»** puede buscar por **título, nombre o correo del estudiante** y filtrar por **estado** y por **etapa**. Pulse **«Abrir»** para ver el detalle.

### 7.4 Detalle de una tesis y asignación de evaluadores

El detalle muestra el estudiante, el resumen y un selector de **etapa**. Para la etapa elegida se ven los documentos, los evaluadores asignados, los veredictos (**con nombre**, a diferencia de lo que ve el estudiante) y las decisiones, con su ronda.

**Asignar un evaluador** (con la etapa en revisión):

1. En **«Asignar evaluador»** elija una persona (solo aparecen evaluadores activos que aún no están asignados).
2. Pulse **«Asignar»**. El evaluador recibe un correo y la acción queda auditada.

**Retirar un evaluador:** pulse **«Retirar»** junto a su nombre. Deja de ver la tesis y su veredicto deja de contar para la unanimidad. Cada etapa admite **hasta tres** evaluadores.

### 7.5 Decidir sobre una etapa

Solo puede decidir sobre la etapa **actual** y mientras esté **En revisión**.

1. Mire el indicador **«N de M evaluadores favorables»** y los veredictos de la ronda.
2. En **«Registrar decisión»**, elija el **resultado**:
   - **Aprobada:** abre la etapa siguiente (o finaliza la tesis tras la 7). **Exige que todos los evaluadores asignados sean favorables en la ronda.** Si no es así, el sistema se lo advierte antes y rechaza la operación explicando cuántos faltan.
   - **Observada:** la etapa vuelve al estudiante para correcciones. **Justificación obligatoria.**
   - **Rechazada:** según la etapa, vuelve al estudiante (reintento) o cierra la tesis. **Justificación obligatoria.**
3. Escriba la **justificación** (hasta 2000 caracteres; es lo que lee el estudiante).
4. Pulse **«Registrar decisión»** y confirme.

> **Las decisiones no se pueden modificar ni eliminar.** Para corregir un error en un rechazo, use la reapertura. Si dos personas deciden a la vez sobre la misma etapa, solo se registra la primera y la otra recibe un aviso de conflicto.

Si una etapa no tiene evaluadores asignados, usted puede aprobarla directamente.

### 7.6 Reabrir una etapa rechazada

Sirve para corregir un error sin intervenir la base de datos, incluso en un rechazo definitivo.

1. Abra la tesis y elija la etapa rechazada (debe ser la etapa **actual**).
2. En **«Reabrir etapa»**, escriba la **justificación** (obligatoria).
3. Pulse **«Reabrir etapa»** y confirme.

**Qué ocurre:** queda registrada una decisión inmutable *Reabierta* con su justificación, la tesis vuelve a estar **en curso**, la etapa queda **con observaciones** y se avisa al estudiante por correo (sin la justificación). Al reenviar el estudiante, comienza una ronda nueva.

**Límite:** si el rechazo cerró la tesis y el estudiante **ya registró otra tesis en curso**, el sistema no permite reabrir (solo puede haber una tesis en curso a la vez).

### 7.7 Auditoría

En **«Auditoría»** puede consultar el registro de las acciones importantes del sistema, con filtros por tesis, actor, tipo de acción y rango de fechas. Cada registro indica la **fecha (UTC)**, el **actor**, la **acción**, la **entidad**, la **dirección IP** y un **identificador de correlación** que permite seguir una misma operación. Entre otras, se registran los ingresos (correctos, fallidos y bloqueos), la creación y cambios de usuarios, el registro de tesis, los envíos a revisión, la carga de documentos, las asignaciones, los veredictos, las decisiones y las reaperturas.

El registro es **inmutable** (no se puede modificar ni borrar) y no contiene contraseñas ni claves.

---

## 8. Avisos por correo electrónico

| Cuándo | Quién lo recibe |
|---|---|
| El estudiante envía o reenvía una etapa | Administradores y evaluadores asignados (indica la ronda) |
| Se asigna un evaluador | El evaluador |
| Un evaluador emite o actualiza su veredicto | El estudiante |
| El administrador decide (aprobada, observada o rechazada) | El estudiante |
| El administrador reabre una etapa | El estudiante |
| Se cambia o restablece una contraseña | El titular de la cuenta |

**Privacidad.** Los correos solo mencionan la etapa y el tipo de novedad. **Nunca** incluyen el título de la tesis, nombres de personas ni el texto de las justificaciones o los veredictos: esos se consultan dentro del sistema.

Si un correo no llega, revise la carpeta de correo no deseado. Un fallo en el envío **no impide** que la operación se haya registrado; el sistema reintenta el envío.

---

## 9. Mensajes frecuentes y cómo resolverlos

| Situación | Qué significa | Qué hacer |
|---|---|---|
| Mensaje de credenciales incorrectas al ingresar | Correo o contraseña incorrectos (el sistema no dice cuál) | Revise los datos o recupere la contraseña |
| Cuenta bloqueada | 5 intentos fallidos | Espere 15 minutos o recupere la contraseña |
| La aplicación solo me deja cambiar la contraseña | Su contraseña es temporal | Cámbiela; luego tendrá acceso completo |
| El enlace de recuperación no funciona | Venció (30 min), ya se usó o hay uno más reciente | Solicite uno nuevo |
| «Ya tiene una tesis en curso» | Solo se permite una tesis en curso | Trabaje con la existente |
| No puedo enviar la etapa | Faltan documentos obligatorios | Suba los que se indican |
| No puedo subir un documento | La etapa no está abierta para usted (en revisión, aprobada o rechazada definitiva) | Espere la decisión o consulte a la administración |
| Archivo rechazado | Formato distinto de PDF/DOCX o más de 20 MB | Conviértalo o redúzcalo |
| Aviso de que no se puede aprobar, con el avance «N de M» (administrador) | Falta algún veredicto favorable en la ronda | Espere los veredictos o decida observar o rechazar |
| Conflicto al decidir (administrador) | Otra persona decidió antes | Recargue la pantalla y revise el estado actual |
| No puedo reabrir una etapa | La tesis ya fue reemplazada por otra en curso, o la etapa no es la actual | Consulte la tesis vigente del estudiante |
| No veo una tesis (evaluador) | No le han asignado esa etapa o fue retirado | Consulte a la administración |

---

## 10. Glosario

| Término | Definición |
|---|---|
| **Etapa** | Cada uno de los siete pasos del proceso de una tesis |
| **Ronda** | Ciclo de evaluación que comienza cada vez que el estudiante envía una etapa |
| **Veredicto** | Concepto de un evaluador sobre una etapa (favorable, con observaciones, desfavorable) |
| **Decisión** | Resolución del administrador sobre una etapa; es inmutable |
| **Unanimidad** | Regla por la que, para aprobar una etapa, todos sus evaluadores deben ser favorables en la ronda |
| **Observada** | Decisión que devuelve la etapa al estudiante para correcciones |
| **Reapertura** | Decisión del administrador que devuelve una etapa rechazada al estudiante |
| **Cola de validación** | Lista de etapas en revisión pendientes de decisión |
| **Etapa estancada** | Etapa que lleva más de 15 días en revisión |
| **Auditoría** | Registro inmutable de las acciones importantes del sistema |
| **Contraseña temporal** | Contraseña inicial que debe cambiarse en el primer ingreso |

---

## 11. Aspectos pendientes de confirmación institucional

Estos puntos funcionan hoy con una regla provisional que puede cambiar sin modificar el programa:

| Tema | Regla actual | Pendiente |
|---|---|---|
| Rechazo en **Requisitos finales** | Se puede reintentar | Confirmar si es reintentable o definitivo |
| Nombre del evaluador | El estudiante **no** lo ve | Confirmar si debe mostrarse |
| Inicio de sesión | Correo y contraseña propios | Inicio de sesión único con la cuenta institucional (el área de tecnología debe confirmar el proveedor y los datos de configuración) |
| Documentos obligatorios | Solo Propuesta y Pasantía | Revisar si se agregan otros |
| Disponibilidad, respaldos y plataforma de despliegue | Sin definir | Acordar con la docente y con la institución |

Cualquier ajuste se comunicará y se reflejará en una nueva versión de este manual.
