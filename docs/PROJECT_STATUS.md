# Estado del proyecto

Actualizado: 2026-10-19 · Rama `develop` · 307 pruebas automáticas en verde (las 222 de integración también contra **SQL Server 2022 real**) · build sin advertencias · 0 paquetes vulnerables · 0 secretos en el historial.

## 1. Resumen

El backend implementa las **17 historias de usuario** de la especificación ([`SDD_SPECIFICATION.md`](SDD_SPECIFICATION.md)), el cambio y la recuperación de contraseña (FR-15) y las tareas técnicas y de operación de los sprints 0 al 8. Es un producto **funcional de punta a punta a nivel de API**, verificado con pruebas automáticas y con el stack completo en Docker contra SQL Server real.

**No está listo para producción todavía.** Lo que falta **no es código del backend**: son decisiones del Product Owner, un frontend, infraestructura de la institución y verificaciones externas. Todo está listado, con responsable, en la sección 5 y como issues en GitHub (etiqueta `bloquea-produccion`).

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
| 8 | Docker, health checks, CI/CD, Dependabot, k6 y rendimiento | NFR operativos | #38 |
| Cierre | Documentación, aprovisionamiento repetible de bases de datos | #16 (parcial), #20 | #39, #40 |
| Cierre | **Cambio y recuperación de contraseña**, con cambio obligatorio de la temporal | FR-15 | #41 |
| Cierre | **Pruebas de integración contra SQL Server real en el CI** y verificación de migraciones | — | PR de SQL Server |

## 3. Qué se verificó y con qué alcance

| Verificación | Resultado | Límite |
|---|---|---|
| Pruebas automáticas (unitarias, de API real, de arquitectura) | 307 en verde; cada criterio de aceptación tiene pruebas etiquetadas | — |
| **Pruebas de integración contra SQL Server 2022 real** | Las 222 pasan (job `pruebas-sql-server` del CI y en local); incluyen la aplicación de todas las migraciones a una base nueva, los índices únicos filtrados y la ausencia de columnas de texto ilimitadas | Contenedor efímero, no el servidor de la institución |
| Stack completo con Docker contra SQL Server | Migraciones con el usuario de mínimo privilegio, API `healthy`, aprovisionamiento repetible verificado (incluidos los casos de fallo) | Entorno local, no Staging |
| Carga (k6, 10 000 tesis) | p95 de 270 ms (KPIs) y 597 ms (cola) con 20 usuarios simultáneos; 361/797 ms con 50 | Portátil de 4 vCPU compartidas; repetir en Staging |
| Búsqueda de secretos, paquetes vulnerables, formato | Sin hallazgos | — |
| Correo real (SMTP) | **No probado**: solo el modo registro y un doble en pruebas | Probar con el servidor de la institución |
| Variantes de Azure SQL de los scripts de base de datos | **No probadas** | No se creó ningún recurso |

Defecto real hallado gracias a SQL Server (ya corregido): el desempate por `Guid` no sigue el orden de creación, por lo que los eventos de auditoría de una misma solicitud ahora llevan fechas estrictamente crecientes.

## 4. Supuestos aplicados (a confirmar por el Product Owner)

Decisiones que tomé donde la especificación no definía la regla. Cada una está marcada en su PR y tiene su issue de decisión (sección 5).

1. Una tesis en curso por estudiante; si una tesis es rechazada, puede registrar otra.
2. Al registrar la tesis solo existe la etapa 1; las siguientes se abren al aprobar la anterior.
3. **Rechazar una etapa rechaza la tesis** completa (no hay reapertura).
4. Documentos: solo PDF y DOCX, 20 MB; la categoría la escribe el estudiante (no hay catálogo todavía); una etapa **en revisión** tampoco admite cambios de documentos.
5. Los requisitos documentales por etapa se configuran con `Documentos:Requisitos:<Etapa>` y por defecto **no se exige ninguno**.
6. Reenviar una etapa observada **no exige** un documento nuevo (la spec 3.4 lo sugería).
7. El estudiante ve los veredictos solo **después** de la decisión y de forma **anónima**.
8. Cuentas propias con JWT (no SSO). Las cuentas creadas por el Administrador tienen contraseña **temporal** y deben cambiarla en el primer acceso; la recuperación es por correo con un enlace de un solo uso (30 minutos).
9. Los administradores activos reciben un correo por cada etapa enviada a revisión.
10. Estancamiento = más de 15 días en revisión (configurable por consulta).

## 5. Lo que falta — mapa completo

Cada fila es un issue en GitHub. **Bloquea producción** = no se debe salir a producción sin resolverlo.

### 5.1 Decisiones del Product Owner (etiqueta `decision-po`)

| Issue | Decisión | Bloquea producción | Qué cambia al responder |
|---|---|:---:|---|
| #42 | Documentos obligatorios por etapa (formatos y tamaños) | **Sí** | Configuración, sin código |
| #43 | Etapa rechazada: ¿terminal o reabrible? | **Sí** | Regla de negocio (supuesto 3) |
| #44 | Actores adicionales; evaluadores y veredictos requeridos por etapa | **Sí** | Posible cambio de roles y de la decisión |
| #45 | Autenticación propia o SSO institucional (y MFA) | **Sí** | Con SSO se reemplaza el login |
| #46 | Visibilidad y anonimato de los veredictos | No | Regla de retroalimentación |
| #47 | Plazos por etapa y alertas por vencimiento | No | Nueva funcionalidad |
| #48 | Reenviar una etapa observada ¿exige documento nuevo? | No | Mejora pequeña |

### 5.2 Desarrollo (etiqueta `mejora`)

| Issue | Tarea | Bloquea producción | Depende de |
|---|---|:---:|---|
| #49 | **Frontend**: cliente web (incluye pantalla de restablecimiento de contraseña) | **Sí** | Construido en [sgt-tesis-frontend](https://github.com/CSA-DanielVillamizar/sgt-tesis-frontend) (Angular 21, los tres roles, probado de extremo a extremo contra esta API). Falta validación con usuarios reales, despliegue y definir el dominio para `Frontend__UrlRestablecerPassword` |
| #50 | Exportar el dashboard a CSV (FR-16) | No | — |
| #51 | Administrar notificaciones fallidas y enlace a la tesis en los correos | No | URL del frontend (#49) |
| #52 | Política de retención y archivado de auditoría y notificaciones | No | Área jurídica |
| #53 | Escalado horizontal: caché de KPIs y límite de solicitudes compartidos | No | Solo si hay varias instancias |
| #54 | Análisis de imágenes en el CI y antivirus de archivos | No | Exigencia de la institución |

### 5.3 Infraestructura y verificaciones externas

| Issue | Tarea | Bloquea producción | Depende de |
|---|---|:---:|---|
| #16 | **Crear las bases de datos** de Staging y Producción (procedimiento listo) | **Sí** | Decisión de la institución sobre dónde alojarlas |
| #55 | Elegir la plataforma y **automatizar el despliegue** (proxy con TLS, dominio, `AllowedHosts`, CORS) | **Sí** | Decisión de la institución |
| #56 | **Probar el correo con un SMTP real** | **Sí** | Servidor y credenciales de la institución |
| #57 | **Prueba de penetración** independiente | **Sí** | Staging disponible |
| #58 | **Revisión legal** de protección de datos personales | **Sí** | Área jurídica |
| #59 | Simulacro de restauración y prueba de carga en el hardware de Staging | **Sí** | Staging disponible (#16, #55) |
| #20 | Aceptar el ADR-003 (sin MediatR ni AutoMapper) | No | Arquitecto / Product Owner |

### 5.4 Orden sugerido para llegar a producción

1. **El Product Owner responde #42–#45** (y el resto cuando pueda) → ajustes de configuración o de reglas.
2. **La institución decide la plataforma** (#16, #55) → se crean los ambientes con `DATABASE_PROVISIONING.md` y se automatiza el despliegue.
3. **Se construye el frontend** (#49) en paralelo y se conecta el correo real (#56).
4. **Con Staging disponible:** simulacro de restauración y carga (#59), prueba de penetración (#57) y revisión legal (#58).
5. **Lista de verificación previa a producción** de [`SECURITY_REVIEW.md`](SECURITY_REVIEW.md); aprobación final.
6. Transferir el repositorio a quienes lo operarán (`CONTRIBUTING.md`, `OPERATIONS.md`).

## 6. Estado de los issues

Los issues de historias y tareas técnicas (**US-01 a US-15, US-17 y #18–#23**) están entregados en `develop` y cerrados con referencia al PR (GitHub solo cierra automáticamente al integrar en la rama predeterminada, `main`). Siguen abiertos a propósito:

- **#16 (US-16):** la parte técnica está completa y verificada (`DATABASE_PROVISIONING.md`: script repetible, verificación automática, probado en una base nueva y con pruebas contra SQL Server). Falta únicamente que la institución **decida dónde alojar** Staging y Producción y se ejecute el procedimiento.
- **#20 (ADR-003):** la decisión de no usar MediatR/AutoMapper está implementada; sigue *propuesta* hasta que el Arquitecto y el Product Owner la acepten.
- **#42–#59:** el backlog anterior.

## 7. Cómo seguir

Este documento es la fuente de verdad del estado; se actualiza con cada cambio relevante. Para ver el trabajo pendiente: issues abiertos en GitHub filtrados por las etiquetas `decision-po`, `mejora`, `infraestructura`, `seguridad` y `bloquea-produccion`.
