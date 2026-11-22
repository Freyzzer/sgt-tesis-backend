using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGT.Tesis.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class EtapaEnRevisionDesde : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "EnRevisionDesde",
            table: "Etapas",
            type: "datetime2",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EnRevisionDesde",
            table: "Etapas");
    }
}
