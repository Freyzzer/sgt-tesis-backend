/*
  Refuerza a nivel de base de datos la inmutabilidad de las decisiones y de la auditoría (US-11 AC-11.6, US-12).

  Ejecutar con una cuenta administrativa DESPUÉS de aplicar las migraciones, en cada ambiente:

    sqlcmd -S <servidor> -d <base> -i 03-deny-immutable-tables.sql -v AppLogin="sgt_app_<ambiente>"

  La aplicación ya rechaza modificar o borrar una Decision antes de llegar a la base de datos; esto
  impide además que alguien con las credenciales de la aplicación lo haga por otra vía.
*/
EXEC (N'DENY UPDATE, DELETE ON dbo.Decisiones TO [$(AppLogin)];');
GO

-- Los registros de auditoría tampoco se pueden alterar ni borrar.
EXEC (N'DENY UPDATE, DELETE ON dbo.RegistrosAuditoria TO [$(AppLogin)];');
GO
