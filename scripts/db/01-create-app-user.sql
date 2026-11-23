/*
  Crea el usuario de la aplicación con permisos mínimos (US-16, AC-16.2).

  Ejecutar con una cuenta administrativa, UNA VEZ por ambiente y por base de datos:

    sqlcmd -S <servidor> -d <base> -i 01-create-app-user.sql ^
           -v AppLogin="sgt_app_<ambiente>" AppPassword="<secreto-del-gestor>"

  El secreto NO se escribe en este archivo ni se versiona: se genera y se guarda
  en el gestor de secretos del ambiente (Key Vault o equivalente institucional).
  Use un login y una contraseña distintos en Dev, Staging y Producción.
*/

-- Login a nivel de servidor (ejecutar en master si el login no existe).
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$(AppLogin)')
    EXEC (N'CREATE LOGIN [$(AppLogin)] WITH PASSWORD = N''$(AppPassword)'', CHECK_POLICY = ON, CHECK_EXPIRATION = ON;');
GO

-- Usuario a nivel de base de datos.
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(AppLogin)')
    EXEC (N'CREATE USER [$(AppLogin)] FOR LOGIN [$(AppLogin)] WITH DEFAULT_SCHEMA = dbo;');
GO

-- Permisos mínimos: lectura y escritura de datos. Sin DDL, sin db_owner.
EXEC (N'ALTER ROLE db_datareader ADD MEMBER [$(AppLogin)];');
EXEC (N'ALTER ROLE db_datawriter ADD MEMBER [$(AppLogin)];');
GO

-- Las tablas inmutables (Decision, RegistroAuditoria) se restringen cuando existan
-- (US-11, AC-11.6):
--   DENY UPDATE, DELETE ON dbo.Decision TO [$(AppLogin)];
--   DENY UPDATE, DELETE ON dbo.RegistroAuditoria TO [$(AppLogin)];
