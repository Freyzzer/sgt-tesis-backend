# Estado del proyecto

Actualizado: 2026-10-19 · Rama `develop` · 272 pruebas automáticas en verde · build sin advertencias · 0 paquetes vulnerables.

## 1. Resumen

El backend implementa las **17 historias de usuario** de la especificación ([`SDD_SPECIFICATION.md`](SDD_SPECIFICATION.md)) y las tareas técnicas del Sprint 0 al 8. Es un producto **funcional de punta a punta a nivel de API**, verificado con pruebas automáticas y con el stack completo en Docker contra SQL Server real. **No está listo para producción todavía**: faltan decisiones del Product Owner, tareas de infraestructura, cambio/recuperación de contraseña y una prueba de penetración (sección 5).

## 2. Entregado por sprint

| Sprint | Contenido | Historias / issues | PR |
|---|---|---|---|
| 0 | Gobierno del repositorio, CI, `.editorconfig`, parche de dependencia vulnerable | — | #24 |
| 1 | Entidades base auditables; base de datos por ambiente con timeout y reintentos; Serilog, errores globales, CORS, HSTS; pruebas de arquitectura; xunit.v3; decisión sobre licencias (ADR-003) | US-16 (parcial), US-17, #18–#23 | #25–#28 |
| 2 | Login JWT, refresh tokens rotados, bloqueo; gestión de usuarios (ADR-004) | US-01, US-02, US-03 | #29, #30 |
| 3 | Registro de tesis, consulta y envío de etapa a revisión | US-04, US-05, US-06 | #31 |
| 4 | Carga, versionado y descarga de documentos | US-07, US-08 | #32 |
| 5 | Asignación de evaluadores, veredictos, decisiones inmutables y máquina de estados | US-09, US-10, US-11 | #33 |
| 6 | Auditoría con correlación; notificaciones por correo (outbox); retroalimentación anónima | US-12, US-13, US-15 | #34, #36 |
| 7 | Dashboard de KPIs y cola de validación | US-14 | #37 |
| 8 | Docker, health checks, CI/CD, CodeQL (condicional), Dependabot, k6 y rendimiento | NFR operativos | #38 |

## 3. Qué se verificó y con qué alcance

| Verificación | Resultado | Límite |
|---|---|---|
| Pruebas automáticas (unitarias, de API real, de arquitectura) | 272 en verde; cada criterio de aceptación tiene pruebas etiquetadas | La API se prueba sobre **SQLite en memoria**, no SQL Server |
| Stack completo con Docker contra SQL Server 2022 | Las 7 migraciones se aplicaron con el usuario de migraciones de mínimo privilegio; la API quedó `healthy`; login y KPIs correctos | Entorno local, no Staging |
| Carga (k6, 10 000 tesis) | p95 de 270 ms (KPIs) y 597 ms (cola) con 20 usuarios simultáneos; 361/797 ms con 50 | Portátil de 4 vCPU compartidas; repetir en Staging |
| Búsqueda de secretos, paquetes vulnerables, formato | Sin hallazgos | — |
| Correo real (SMTP) | **No probado**: solo el modo registro y un doble en pruebas | Probar con el servidor de la institución |

## 4. Supuestos aplicados (a confirmar por el Product Owner)

Decisiones que tomé donde la especificación no definía la regla. Cada una está marcada en su PR.

1. Una tesis en curso por estudiante; si una tesis es rechazada, puede registrar otra.
2. Al registrar la tesis solo existe la etapa 1; las siguientes se abren al aprobar la anterior.
3. **Rechazar una etapa rechaza la tesis** completa (no hay reapertura).
4. Documentos: solo PDF y DOCX, 20 MB; la categoría la escribe el estudiante (no hay catálogo todavía); una etapa **en revisión** tampoco admite cambios de documentos.
5. Los requisitos documentales por etapa se configuran con `Documentos:Requisitos:<Etapa>` y por defecto **no se exige ninguno**.
6. Reenviar una etapa observada **no exige** un documento nuevo (la spec 3.4 lo sugería).
7. El estudiante ve los veredictos solo **después** de la decisión y de forma **anónima**.
8. Cuentas propias con JWT (no SSO); contraseña inicial definida por el Administrador.
9. Los administradores activos reciben un correo por cada etapa enviada a revisión.
10. Estancamiento = más de 15 días en revisión (configurable por consulta).

## 5. Pendientes antes de producción

### Decisiones del Product Owner (bloquean o cambian reglas)

- ¿Qué documentos son obligatorios en cada una de las 7 etapas, con qué formatos y tamaños? (sustituye el supuesto 5)
- ¿Una etapa rechazada es terminal o se puede reabrir? ¿Quién y bajo qué condición? (supuesto 3)
- ¿Hay más actores (director de tesis, jurados, consejo)? ¿Varios evaluadores por etapa y cuántos veredictos se requieren?
- ¿La evaluación es anónima para el estudiante? ¿Se muestran los veredictos antes de la decisión? (supuesto 7)
- ¿Autenticación propia o SSO institucional (OIDC)? (supuesto 8; condiciona R-1 y R-2 de la revisión de seguridad)
- ¿Plazos por etapa y alertas por vencimiento?
- ¿Motor de base de datos definitivo (el código usa SQL Server) y **plataforma de despliegue**?
- ¿Normativa de protección de datos, retención de la auditoría y requisitos de idioma/accesibilidad?
- ¿RPO/RTO y disponibilidad acordados? (propuestos: 24 h, 4 h, 99,5 %)

### Tareas de desarrollo

| Prioridad | Tarea | Referencia |
|---|---|---|
| Alta | Cambio y recuperación de contraseña; cambio obligatorio en el primer acceso | FR-15, revisión de seguridad R-1 |
| Alta | Pruebas de integración contra **SQL Server real** en el CI (Testcontainers) | Límite de la sección 3 |
| Media | Exportación del dashboard a CSV | FR-16 |
| Media | Exigir un documento nuevo al reenviar una etapa observada | Spec 3.4 |
| Media | Interfaz de administración de notificaciones fallidas; enlace a la tesis en los correos | `OPERATIONS.md` |
| Media | Escaneo de imágenes en el CI; antivirus de archivos si se exige | Revisión de seguridad R-3, R-8 |
| Baja | Política de retención y archivado de auditoría y notificaciones | R-7 |
| Baja | Cache distribuida de KPIs y rate limiting compartido si se escala a varias instancias | `OPERATIONS.md` |

### Infraestructura y operación

- Crear las instancias de base de datos por ambiente y ejecutar los scripts de `scripts/db` (usuarios de mínimo privilegio y `DENY` de tablas inmutables): cierra **US-16** (AC-16.1 y AC-16.2) en Staging y Producción.
- Elegir la plataforma de despliegue y automatizar el paso a Staging/Producción (hoy el flujo solo publica imágenes en GHCR).
- Configurar el servidor SMTP real y probarlo.
- Configurar proxy inverso con TLS, `AllowedHosts`, CORS y `ForwardedHeaders`.
- Primer simulacro de restauración de respaldo; prueba de carga en Staging.
- Prueba de penetración independiente y revisión legal (ver [`SECURITY_REVIEW.md`](SECURITY_REVIEW.md)).

## 6. Estado de los issues

Los issues **US-01 a US-15, US-17 y #18–#23** están entregados en `develop`. Quedan abiertos a propósito:

- **#16 (US-16):** los criterios de infraestructura (instancias y usuarios por ambiente) requieren las tareas de la sección 5.
- **#20 (ADR-003):** la decisión de no usar MediatR/AutoMapper está implementada, pero sigue **propuesta** hasta que el Arquitecto y el Product Owner la validen.

GitHub no cierra los issues al integrar a `develop` (solo al hacerlo en la rama predeterminada, `main`), por eso se cerraron a mano con referencia al PR.

## 7. Cómo seguir

1. El Product Owner revisa la especificación (`SDD_SPECIFICATION.docx`) y responde las preguntas de la sección 5.
2. Se implementan las tareas de prioridad alta y se ajustan los supuestos según las respuestas.
3. Se completa la infraestructura de Staging, se ejecutan la prueba de carga, el simulacro de restauración y la prueba de penetración.
4. Se aprueba el paso a producción con la lista de verificación de `SECURITY_REVIEW.md`.
5. Cuando todo lo anterior esté listo, se transfiere el repositorio a quienes lo operarán (ver `CONTRIBUTING.md` y `OPERATIONS.md`).
