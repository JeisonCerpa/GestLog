using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class _20260304_RenameLitrosToGalonesConsumoCombustible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Litros",
                table: "GestionVehiculos_ConsumosCombustible",
                newName: "Galones");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Galones",
                table: "GestionVehiculos_ConsumosCombustible",
                newName: "Litros");
        }
    }
}
