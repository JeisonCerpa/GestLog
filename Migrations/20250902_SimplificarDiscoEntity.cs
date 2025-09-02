using Microsoft.EntityFrameworkCore.Migrations;

namespace GestLog.Migrations
{
    public partial class SimplificarDiscoEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Serial",
                table: "Discos");
            migrationBuilder.DropColumn(
                name: "Interfaz",
                table: "Discos");
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Discos");
            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Discos");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Serial",
                table: "Discos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "Interfaz",
                table: "Discos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Discos",
                type: "bit",
                nullable: false,
                defaultValue: true);
            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Discos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
