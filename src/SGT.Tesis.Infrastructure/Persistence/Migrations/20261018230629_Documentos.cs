using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGT.Tesis.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Documentos : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Documentos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EtapaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Categoria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CategoriaClave = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                NombreOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                NombreAlmacenado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                HashSha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                Version = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Documentos", x => x.Id);
                table.ForeignKey(
                    name: "FK_Documentos_Etapas_EtapaId",
                    column: x => x.EtapaId,
                    principalTable: "Etapas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Documentos_EtapaId_CategoriaClave_Version",
            table: "Documentos",
            columns: new[] { "EtapaId", "CategoriaClave", "Version" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Documentos_NombreAlmacenado",
            table: "Documentos",
            column: "NombreAlmacenado",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Documentos");
    }
}
