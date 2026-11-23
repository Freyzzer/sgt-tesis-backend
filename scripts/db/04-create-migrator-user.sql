/*
  Crea el usuario que aplica las migraciones (distinto del usuario de la aplicación; US-16).

    sqlcmd -S <servidor> -d <base> -i 04-create-migrator-user.sql ^
           -v MigratorLogin="sgt_migrator_<ambiente>" MigratorPassword="<secreto-del-gestor>"

  Solo lo usa el pipeline de despliegue (efbundle); la API NUNCA se conecta con esta cuenta.
  Permisos: crear y alterar objetos del esquema (db_ddladmin) y mantener la tabla de historial de
  migraciones (lectura y escritura). Sin db_owner ni sysadmin. Use credenciales distintas por ambiente.
*/
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$(MigratorLogin)')
    EXEC (N'CREATE LOGIN [$(MigratorLogin)] WITH PASSWORD = N''$(MigratorPassword)'', CHECK_POLICY = ON, CHECK_EXPIRATION = ON;');
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(MigratorLogin)')
    EXEC (N'CREATE USER [$(MigratorLogin)] FOR LOGIN [$(MigratorLogin)] WITH DEFAULT_SCHEMA = dbo;');
GO

EXEC (N'ALTER ROLE db_ddladmin ADD MEMBER [$(MigratorLogin)];');
EXEC (N'ALTER ROLE db_datareader ADD MEMBER [$(MigratorLogin)];');
EXEC (N'ALTER ROLE db_datawriter ADD MEMBER [$(MigratorLogin)];');
GO
