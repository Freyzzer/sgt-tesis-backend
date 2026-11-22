using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGT.Tesis.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Notificaciones : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Notificaciones",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Destinatario = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                Asunto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Cuerpo = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                Estado = table.Column<int>(type: "int", nullable: false),
                Intentos = table.Column<int>(type: "int", nullable: false),
                ProximoIntentoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                EnviadaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                UltimoError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notificaciones", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Notificaciones_Estado_ProximoIntentoEn",
            table: "Notificaciones",
            columns: new[] { "Estado", "ProximoIntentoEn" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Notificaciones");
    }
}
