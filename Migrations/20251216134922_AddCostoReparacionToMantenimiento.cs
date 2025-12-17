using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddCostoReparacionToMantenimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoReparacion",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoReparacion",
                table: "EquiposInformaticos_MantenimientosCorrectivos");
        }
    }
}
