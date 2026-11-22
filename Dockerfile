# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Compilación
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Se copian primero solo los .csproj para aprovechar la caché de restore.
COPY global.json Directory.Build.props .editorconfig ./
COPY src/SGT.Tesis.Domain/SGT.Tesis.Domain.csproj src/SGT.Tesis.Domain/
COPY src/SGT.Tesis.Application/SGT.Tesis.Application.csproj src/SGT.Tesis.Application/
COPY src/SGT.Tesis.Infrastructure/SGT.Tesis.Infrastructure.csproj src/SGT.Tesis.Infrastructure/
COPY src/SGT.Tesis.API/SGT.Tesis.API.csproj src/SGT.Tesis.API/
RUN dotnet restore src/SGT.Tesis.API/SGT.Tesis.API.csproj

COPY src/ src/
RUN dotnet publish src/SGT.Tesis.API/SGT.Tesis.API.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Migraciones: genera un ejecutable autónomo (efbundle) para aplicarlas desde el pipeline,
# con una cuenta de base de datos distinta a la de la aplicación.
# ---------------------------------------------------------------------------
FROM build AS migrator-build
ARG RID=linux-x64
RUN dotnet tool install --global dotnet-ef --version 10.0.*
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
    --project src/SGT.Tesis.Infrastructure \
    --self-contained -r ${RID} \
    --output /app/efbundle --force

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrator
WORKDIR /app
COPY --from=migrator-build /app/efbundle ./efbundle
USER $APP_UID
# Uso: efbundle --connection "<cadena del usuario con permisos DDL>"
ENTRYPOINT ["./efbundle"]

# ---------------------------------------------------------------------------
# Herramientas de base de datos: sqlcmd + scripts de scripts/db (para db-init y db-harden en compose
# y para tareas operativas puntuales). Los scripts van dentro de la imagen: no dependen de montajes del host.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/mssql/server:2022-latest AS dbtools
USER root
COPY scripts/db/ /scripts/
RUN chmod -R a+rX /scripts
USER mssql
# Uso: provision.sh preparar | finalizar | verificar (ver docs/DATABASE_PROVISIONING.md)
ENTRYPOINT ["bash", "/scripts/provision.sh"]

# ---------------------------------------------------------------------------
# Imagen de ejecución (la última etapa es la que se construye por defecto)
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Carpeta de archivos cargados: debe montarse como volumen persistente.
RUN mkdir -p /data/documentos && chown -R $APP_UID /data

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__RootPath=/data/documentos \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080
VOLUME ["/data/documentos"]

# Sin privilegios de administrador dentro del contenedor.
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD ["dotnet", "SGT.Tesis.API.dll", "--healthcheck"]

ENTRYPOINT ["dotnet", "SGT.Tesis.API.dll"]
