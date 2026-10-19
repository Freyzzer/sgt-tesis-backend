using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGT.Tesis.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Contrasenas : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "DebeCambiarPassword",
            table: "Usuarios",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "Sensible",
            table: "Notificaciones",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "PasswordResetTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ExpiraEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                UsadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_PasswordResetTokens_Usuarios_UsuarioId",
                    column: x => x.UsuarioId,
                    principalTable: "Usuarios",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PasswordResetTokens_TokenHash",
            table: "PasswordResetTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PasswordResetTokens_UsuarioId",
            table: "PasswordResetTokens",
            column: "UsuarioId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PasswordResetTokens");

        migrationBuilder.DropColumn(
            name: "DebeCambiarPassword",
            table: "Usuarios");

        migrationBuilder.DropColumn(
            name: "Sensible",
            table: "Notificaciones");
    }
}
