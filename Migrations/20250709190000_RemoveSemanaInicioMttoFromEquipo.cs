using Microsoft.EntityFrameworkCore.Migrations;

namespace GestLog.Migrations
{
    public partial class RemoveSemanaInicioMttoFromEquipo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SemanaInicioMtto",
                table: "Equipos");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SemanaInicioMtto",
                table: "Equipos",
                type: "int",
                nullable: true);
        }
    }
}
