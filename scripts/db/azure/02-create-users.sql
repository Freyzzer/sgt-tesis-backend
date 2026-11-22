/*
  AZURE SQL DATABASE — paso 2 de 2: crea los usuarios en la base del ambiente y les asigna los permisos
  mínimos. Se ejecuta en la base DEL AMBIENTE (no en master), después de 01-create-logins.sql.

  NOTA: no se ha probado contra un servicio real de Azure (ver 01-create-logins.sql).

    sqlcmd -S <servidor>.database.windows.net -d <base> -U <admin> -P <clave> -i 02-create-users.sql ^
           -v AppLogin="sgt_app_<amb>" MigratorLogin="sgt_migrator_<amb>"
*/
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(AppLogin)')
    EXEC (N'CREATE USER [$(AppLogin)] FOR LOGIN [$(AppLogin)] WITH DEFAULT_SCHEMA = dbo;');
GO
EXEC (N'ALTER ROLE db_datareader ADD MEMBER [$(AppLogin)];');
EXEC (N'ALTER ROLE db_datawriter ADD MEMBER [$(AppLogin)];');
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(MigratorLogin)')
    EXEC (N'CREATE USER [$(MigratorLogin)] FOR LOGIN [$(MigratorLogin)] WITH DEFAULT_SCHEMA = dbo;');
GO
EXEC (N'ALTER ROLE db_ddladmin ADD MEMBER [$(MigratorLogin)];');
EXEC (N'ALTER ROLE db_datareader ADD MEMBER [$(MigratorLogin)];');
EXEC (N'ALTER ROLE db_datawriter ADD MEMBER [$(MigratorLogin)];');
GO
