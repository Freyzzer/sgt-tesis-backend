/*
  DATOS DE PRUEBA DE CARGA (solo para bases DESECHABLES; nunca en producción).

  Crea 10 000 tesis para medir el dashboard (AC-14.3):
    - 2 000 estudiantes, cada uno con una tesis en curso y la etapa 1 en revisión (cola de validación),
      con antigüedades entre 0 y 39 días.
    - 8 000 tesis rechazadas (con su etapa cerrada y su decisión) repartidas entre los primeros 100 estudiantes.

  Requiere que exista al menos un Administrador (el que crea el arranque con Bootstrap__*).
  Es idempotente: si ya hay datos con correo '%@carga.test' no vuelve a insertar.

    sqlcmd -S <servidor> -d <base> -U <usuario> -P <clave> -C -i seed-10000.sql
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON; -- requerido por los índices filtrados
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Correo LIKE N'%@carga.test')
BEGIN
    PRINT 'Los datos de carga ya existen; no se insertó nada.';
    RETURN;
END;

DECLARE @admin uniqueidentifier = (SELECT TOP 1 Id FROM dbo.Usuarios WHERE Rol = 3 AND Activo = 1);
IF @admin IS NULL
BEGIN
    RAISERROR('No hay un Administrador activo. Inicie la API con Bootstrap__* primero.', 16, 1);
    RETURN;
END;

DECLARE @ahora datetime2 = SYSUTCDATETIME();

BEGIN TRANSACTION;

-- Números del 1 al 10 000
SELECT TOP (10000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
INTO #n
FROM sys.all_objects a CROSS JOIN sys.all_objects b;

-- Estudiantes
SELECT TOP (2000) n, NEWID() AS Id
INTO #est
FROM #n ORDER BY n;

INSERT dbo.Usuarios (Id, Nombres, Apellidos, Correo, PasswordHash, Rol, Activo, IntentosFallidos, CreatedAt)
SELECT Id, CONCAT(N'Carga', n), N'Estudiante', CONCAT(N'estudiante', n, N'@carga.test'), N'sin-acceso', 1, 1, 0, @ahora
FROM #est;

-- Tesis en curso con la etapa 1 en revisión
SELECT e.n, e.Id AS StudentId, NEWID() AS TesisId, NEWID() AS EtapaId
INTO #curso
FROM #est e;

INSERT dbo.Tesis (Id, StudentId, Titulo, Resumen, Estado, CreatedAt)
SELECT TesisId, StudentId, CONCAT(N'Tesis de carga ', n), N'Resumen de prueba de carga.', 1, @ahora
FROM #curso;

INSERT dbo.Etapas (Id, TesisId, Tipo, Estado, FechaApertura, EnRevisionDesde, Revision, CreatedAt)
SELECT EtapaId, TesisId, 1, 2,
       DATEADD(DAY, -60, @ahora),
       DATEADD(DAY, -(n % 40), @ahora),
       1, @ahora
FROM #curso;

-- Tesis rechazadas (con etapa cerrada y decisión)
SELECT n, NEWID() AS TesisId, NEWID() AS EtapaId, NEWID() AS DecisionId,
       (SELECT Id FROM #est WHERE n = ((x.n - 1) % 100) + 1) AS StudentId
INTO #rech
FROM (SELECT TOP (8000) n FROM #n ORDER BY n) x;

INSERT dbo.Tesis (Id, StudentId, Titulo, Resumen, Estado, CreatedAt)
SELECT TesisId, StudentId, CONCAT(N'Tesis rechazada de carga ', n), N'Resumen de prueba de carga.', 3, @ahora
FROM #rech;

INSERT dbo.Etapas (Id, TesisId, Tipo, Estado, FechaApertura, FechaCierre, EnRevisionDesde, Revision, CreatedAt)
SELECT EtapaId, TesisId, 1, 5,
       DATEADD(DAY, -60, @ahora),
       DATEADD(DAY, -(n % 30), @ahora),
       DATEADD(DAY, -50, @ahora),
       2, @ahora
FROM #rech;

INSERT dbo.Decisiones (Id, EtapaId, AdministradorId, Resultado, Justificacion, FechaUtc, CreatedAt)
SELECT DecisionId, EtapaId, @admin, 3, N'No cumple los requisitos.', DATEADD(DAY, -(n % 30), @ahora), @ahora
FROM #rech;

DROP TABLE #n, #est, #curso, #rech;

COMMIT TRANSACTION;

SELECT (SELECT COUNT(*) FROM dbo.Tesis) AS Tesis,
       (SELECT COUNT(*) FROM dbo.Etapas WHERE Estado = 2) AS EtapasEnRevision,
       (SELECT COUNT(*) FROM dbo.Decisiones) AS Decisiones;
