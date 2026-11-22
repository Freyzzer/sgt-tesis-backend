using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGT.Tesis.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class EvaluacionYDecisiones : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Revision",
            table: "Etapas",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "AsignacionesEvaluador",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EtapaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EvaluadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AsignadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Activa = table.Column<bool>(type: "bit", nullable: false),
                FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                FechaRetiro = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AsignacionesEvaluador", x => x.Id);
                table.ForeignKey(
                    name: "FK_AsignacionesEvaluador_Etapas_EtapaId",
                    column: x => x.EtapaId,
                    principalTable: "Etapas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Decisiones",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EtapaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AdministradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Resultado = table.Column<int>(type: "int", nullable: false),
                Justificacion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Decisiones", x => x.Id);
                table.ForeignKey(
                    name: "FK_Decisiones_Etapas_EtapaId",
                    column: x => x.EtapaId,
                    principalTable: "Etapas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Veredictos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EtapaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EvaluadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Concepto = table.Column<int>(type: "int", nullable: false),
                Comentarios = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                Version = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Veredictos", x => x.Id);
                table.ForeignKey(
                    name: "FK_Veredictos_Etapas_EtapaId",
                    column: x => x.EtapaId,
                    principalTable: "Etapas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AsignacionesEvaluador_EvaluadorId",
            table: "AsignacionesEvaluador",
            column: "EvaluadorId");

        migrationBuilder.CreateIndex(
            name: "UX_Asignaciones_Etapa_Evaluador_Activa",
            table: "AsignacionesEvaluador",
            columns: new[] { "EtapaId", "EvaluadorId" },
            unique: true,
            filter: "[Activa] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_Decisiones_EtapaId",
            table: "Decisiones",
            column: "EtapaId");

        migrationBuilder.CreateIndex(
            name: "IX_Veredictos_EtapaId_EvaluadorId_Version",
            table: "Veredictos",
            columns: new[] { "EtapaId", "EvaluadorId", "Version" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AsignacionesEvaluador");

        migrationBuilder.DropTable(
            name: "Decisiones");

        migrationBuilder.DropTable(
            name: "Veredictos");

        migrationBuilder.DropColumn(
            name: "Revision",
            table: "Etapas");
    }
}
