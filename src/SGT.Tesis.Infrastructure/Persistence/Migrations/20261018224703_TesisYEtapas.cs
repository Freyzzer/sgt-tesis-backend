using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGT.Tesis.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class TesisYEtapas : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Tesis_StudentId",
            table: "Tesis");

        migrationBuilder.AddColumn<int>(
            name: "Estado",
            table: "Tesis",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateTable(
            name: "Etapas",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TesisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Tipo = table.Column<int>(type: "int", nullable: false),
                Estado = table.Column<int>(type: "int", nullable: false),
                FechaApertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Etapas", x => x.Id);
                table.ForeignKey(
                    name: "FK_Etapas_Tesis_TesisId",
                    column: x => x.TesisId,
                    principalTable: "Tesis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "UX_Tesis_StudentId_EnCurso",
            table: "Tesis",
            column: "StudentId",
            unique: true,
            filter: "[Estado] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_Etapas_TesisId_Tipo",
            table: "Etapas",
            columns: new[] { "TesisId", "Tipo" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Etapas");

        migrationBuilder.DropIndex(
            name: "UX_Tesis_StudentId_EnCurso",
            table: "Tesis");

        migrationBuilder.DropColumn(
            name: "Estado",
            table: "Tesis");

        migrationBuilder.CreateIndex(
            name: "IX_Tesis_StudentId",
            table: "Tesis",
            column: "StudentId");
    }
}
