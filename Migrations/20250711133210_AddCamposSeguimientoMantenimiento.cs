using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposSeguimientoMantenimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Anio",
                table: "Seguimientos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Atrasado",
                table: "Seguimientos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Realizado",
                table: "Seguimientos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Semana",
                table: "Seguimientos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anio",
                table: "Seguimientos");

            migrationBuilder.DropColumn(
                name: "Atrasado",
                table: "Seguimientos");

            migrationBuilder.DropColumn(
                name: "Realizado",
                table: "Seguimientos");

            migrationBuilder.DropColumn(
                name: "Semana",
                table: "Seguimientos");
        }
    }
}
