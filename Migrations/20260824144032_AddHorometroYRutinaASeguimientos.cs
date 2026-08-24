using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddHorometroYRutinaASeguimientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Horometro",
                table: "GestionMantenimientos_Seguimientos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rutina",
                table: "GestionMantenimientos_Seguimientos",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Horometro",
                table: "GestionMantenimientos_Seguimientos");

            migrationBuilder.DropColumn(
                name: "Rutina",
                table: "GestionMantenimientos_Seguimientos");
        }
    }
}
