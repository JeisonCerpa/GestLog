using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddVehiculosMaintenanceTypeAndCorrectiveFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsExtraordinario",
                table: "GestionVehiculos_EjecucionesMantenimiento",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EstadoCorrectivo",
                table: "GestionVehiculos_EjecucionesMantenimiento",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoMantenimiento",
                table: "GestionVehiculos_EjecucionesMantenimiento",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "TituloActividad",
                table: "GestionVehiculos_EjecucionesMantenimiento",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsExtraordinario",
                table: "GestionVehiculos_EjecucionesMantenimiento");

            migrationBuilder.DropColumn(
                name: "EstadoCorrectivo",
                table: "GestionVehiculos_EjecucionesMantenimiento");

            migrationBuilder.DropColumn(
                name: "TipoMantenimiento",
                table: "GestionVehiculos_EjecucionesMantenimiento");

            migrationBuilder.DropColumn(
                name: "TituloActividad",
                table: "GestionVehiculos_EjecucionesMantenimiento");
        }
    }
}
