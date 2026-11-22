/*
  Crea la base de datos del ambiente si no existe.

    sqlcmd -S <servidor> -i 00-create-database.sql -v DbName="SGTTesis_<ambiente>"

  Ejecutar con una cuenta administrativa. En Staging y Producción la base suele crearla la plataforma
  (por ejemplo, Azure SQL); este script es principalmente para desarrollo local.
*/
IF DB_ID(N'$(DbName)') IS NULL
    EXEC (N'CREATE DATABASE [$(DbName)];');
GO
