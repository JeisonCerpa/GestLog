using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class SinCamposRamDiscoEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantidadDiscos",
                table: "EquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "CapacidadTotalDiscosGB",
                table: "EquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "CapacidadTotalRamGB",
                table: "EquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "SlotsTotales",
                table: "EquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "SlotsUtilizados",
                table: "EquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "TipoRam",
                table: "EquiposInformaticos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CantidadDiscos",
                table: "EquiposInformaticos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CapacidadTotalDiscosGB",
                table: "EquiposInformaticos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CapacidadTotalRamGB",
                table: "EquiposInformaticos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlotsTotales",
                table: "EquiposInformaticos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlotsUtilizados",
                table: "EquiposInformaticos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoRam",
                table: "EquiposInformaticos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
