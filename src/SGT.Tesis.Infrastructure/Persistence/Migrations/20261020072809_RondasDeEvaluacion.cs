using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGT.Tesis.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RondasDeEvaluacion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Ronda",
            table: "Veredictos",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "Ronda",
            table: "Etapas",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "Ronda",
            table: "Decisiones",
            type: "int",
            nullable: false,
            defaultValue: 1);

        // Datos existentes (ADR-005): todo veredicto y decisión previo pertenece a la ronda 1 (valor por defecto de arriba);
        // una etapa que ya salió de Pendiente (Estado 1) se envió al menos una vez, así que está en la ronda 1.
        migrationBuilder.Sql("UPDATE [Etapas] SET [Ronda] = 1 WHERE [Estado] <> 1");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Ronda",
            table: "Veredictos");

        migrationBuilder.DropColumn(
            name: "Ronda",
            table: "Etapas");

        migrationBuilder.DropColumn(
            name: "Ronda",
            table: "Decisiones");
    }
}
