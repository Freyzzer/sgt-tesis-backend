using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGT.Tesis.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Auditoria : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RegistrosAuditoria",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Accion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Entidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EntidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                TesisId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                CorrelacionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                Detalle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RegistrosAuditoria_Accion",
            table: "RegistrosAuditoria",
            column: "Accion");

        migrationBuilder.CreateIndex(
            name: "IX_RegistrosAuditoria_ActorId_FechaUtc",
            table: "RegistrosAuditoria",
            columns: new[] { "ActorId", "FechaUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_RegistrosAuditoria_FechaUtc",
            table: "RegistrosAuditoria",
            column: "FechaUtc");

        migrationBuilder.CreateIndex(
            name: "IX_RegistrosAuditoria_TesisId_FechaUtc",
            table: "RegistrosAuditoria",
            columns: new[] { "TesisId", "FechaUtc" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RegistrosAuditoria");
    }
}
