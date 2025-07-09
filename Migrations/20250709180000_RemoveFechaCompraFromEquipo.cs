using Microsoft.EntityFrameworkCore.Migrations;

namespace GestLog.Migrations
{
    public partial class RemoveFechaCompraFromEquipo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaCompra",
                table: "Equipos");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCompra",
                table: "Equipos",
                type: "datetime2",
                nullable: true);
        }
    }
}
