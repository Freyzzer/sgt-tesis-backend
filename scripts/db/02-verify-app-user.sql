/*
  Verifica que el usuario de la aplicación NO tenga privilegios excesivos (AC-16.2).
  Ejecutar conectado COMO el usuario de la aplicación. Todos los resultados deben cumplir lo indicado.
*/
SELECT SUSER_SNAME()                         AS UsuarioActual;
SELECT IS_MEMBER('db_owner')                 AS EsDbOwner_DebeSer0;       -- 0
SELECT IS_MEMBER('db_ddladmin')              AS EsDdlAdmin_DebeSer0;      -- 0
SELECT IS_SRVROLEMEMBER('sysadmin')          AS EsSysadmin_DebeSer0;      -- 0
SELECT IS_MEMBER('db_datareader')            AS PuedeLeer_DebeSer1;       -- 1
SELECT IS_MEMBER('db_datawriter')            AS PuedeEscribir_DebeSer1;   -- 1
