/*
  Verifica que el ambiente cumple el modelo de seguridad de la base de datos (US-16). Falla (código de salida
  distinto de cero con sqlcmd -b) si algo no cumple; no modifica nada.

    sqlcmd -S <servidor> -d <base> -i 05-verify-environment.sql -b ^
           -v AppLogin="sgt_app_<ambiente>" MigratorLogin="sgt_migrator_<ambiente>"

  Ejecutar con una cuenta administrativa DESPUÉS de las migraciones y de 03-deny-immutable-tables.sql.
*/
SET NOCOUNT ON;

DECLARE @errores nvarchar(max) = N'';

-- 1) Existen las migraciones y las tablas inmutables
DECLARE @migraciones int = 0;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NOT NULL
    EXEC sp_executesql N'SELECT @n = COUNT(*) FROM dbo.__EFMigrationsHistory', N'@n int OUTPUT', @n = @migraciones OUTPUT;
IF @migraciones = 0
    SET @errores += N'- No hay migraciones aplicadas. ';
IF OBJECT_ID(N'dbo.Decisiones') IS NULL
    SET @errores += N'- Falta la tabla dbo.Decisiones. ';
IF OBJECT_ID(N'dbo.RegistrosAuditoria') IS NULL
    SET @errores += N'- Falta la tabla dbo.RegistrosAuditoria. ';

-- 2) Usuario de la aplicación: solo lectura y escritura de datos
IF USER_ID(N'$(AppLogin)') IS NULL
    SET @errores += N'- No existe el usuario de la aplicación $(AppLogin). ';
ELSE
BEGIN
    IF IS_ROLEMEMBER(N'db_owner', N'$(AppLogin)') = 1 SET @errores += N'- $(AppLogin) es db_owner (debe ser solo lectura/escritura). ';
    IF IS_ROLEMEMBER(N'db_ddladmin', N'$(AppLogin)') = 1 SET @errores += N'- $(AppLogin) es db_ddladmin. ';
    IF IS_ROLEMEMBER(N'db_datareader', N'$(AppLogin)') <> 1 SET @errores += N'- $(AppLogin) no puede leer datos. ';
    IF IS_ROLEMEMBER(N'db_datawriter', N'$(AppLogin)') <> 1 SET @errores += N'- $(AppLogin) no puede escribir datos. ';

    -- 3) Tablas inmutables: UPDATE y DELETE denegados a la aplicación
    DECLARE @tabla sysname, @permiso sysname;
    DECLARE c CURSOR LOCAL FAST_FORWARD FOR
        SELECT t.n, p.n
        FROM (VALUES (N'dbo.Decisiones'), (N'dbo.RegistrosAuditoria')) AS t(n)
        CROSS JOIN (VALUES (N'UPDATE'), (N'DELETE')) AS p(n);
    OPEN c;
    FETCH NEXT FROM c INTO @tabla, @permiso;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF OBJECT_ID(@tabla) IS NOT NULL AND NOT EXISTS (
            SELECT 1
            FROM sys.database_permissions dp
            JOIN sys.database_principals u ON u.principal_id = dp.grantee_principal_id
            WHERE u.name = N'$(AppLogin)' AND dp.state_desc = N'DENY'
              AND dp.permission_name = @permiso AND dp.major_id = OBJECT_ID(@tabla))
            SET @errores += CONCAT(N'- Falta DENY ', @permiso, N' sobre ', @tabla, N' para $(AppLogin). ');
        FETCH NEXT FROM c INTO @tabla, @permiso;
    END;
    CLOSE c;
    DEALLOCATE c;
END;

-- 4) Usuario de migraciones: puede cambiar el esquema, pero no es db_owner
IF USER_ID(N'$(MigratorLogin)') IS NULL
    SET @errores += N'- No existe el usuario de migraciones $(MigratorLogin). ';
ELSE
BEGIN
    IF IS_ROLEMEMBER(N'db_owner', N'$(MigratorLogin)') = 1 SET @errores += N'- $(MigratorLogin) es db_owner. ';
    IF IS_ROLEMEMBER(N'db_ddladmin', N'$(MigratorLogin)') <> 1 SET @errores += N'- $(MigratorLogin) no puede alterar el esquema. ';
END;

IF N'$(AppLogin)' = N'$(MigratorLogin)'
    SET @errores += N'- La cuenta de la API y la de migraciones son la misma. ';

IF @errores <> N''
BEGIN
    DECLARE @mensaje nvarchar(max) = CONCAT(N'Verificación FALLIDA: ', @errores);
    RAISERROR(@mensaje, 16, 1);
END
ELSE
    PRINT N'Verificación correcta: usuarios de mínimo privilegio, tablas inmutables protegidas y migraciones aplicadas.';
GO
