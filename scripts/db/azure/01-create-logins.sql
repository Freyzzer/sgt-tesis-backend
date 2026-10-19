/*
  AZURE SQL DATABASE — paso 1 de 2: crea los logins del servidor lógico. Se ejecuta en la base MASTER del servidor.
  (En SQL Server tradicional no se usa: 01-create-app-user.sql y 04-create-migrator-user.sql hacen todo.)

  NOTA: estas variantes para Azure no se han probado contra un servicio real (no había una suscripción
  autorizada para crear recursos). Valídelas primero en un servidor de pruebas.

    sqlcmd -S <servidor>.database.windows.net -d master -U <admin> -P <clave> -i 01-create-logins.sql ^
           -v AppLogin="sgt_app_<amb>" AppPassword="<secreto>" MigratorLogin="sgt_migrator_<amb>" MigratorPassword="<secreto>"

  Azure SQL no admite CHECK_POLICY ni CHECK_EXPIRATION. Use contraseñas largas y aleatorias del gestor de secretos.
*/
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'$(AppLogin)')
    EXEC (N'CREATE LOGIN [$(AppLogin)] WITH PASSWORD = N''$(AppPassword)'';');
GO

IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'$(MigratorLogin)')
    EXEC (N'CREATE LOGIN [$(MigratorLogin)] WITH PASSWORD = N''$(MigratorPassword)'';');
GO
