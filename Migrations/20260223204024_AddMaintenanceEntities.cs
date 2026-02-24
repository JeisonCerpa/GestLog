using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GestionVehiculos_EjecucionesMantenimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlacaVehiculo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PlanMantenimientoId = table.Column<int>(type: "int", nullable: true),
                    FechaEjecucion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    KMAlMomento = table.Column<long>(type: "bigint", nullable: false),
                    ObservacionesTecnico = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Costo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RutaFactura = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResponsableEjecucion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Proveedor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GestionVehiculos_EjecucionesMantenimiento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GestionVehiculos_PlanesMantenimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlacaVehiculo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PlantillaId = table.Column<int>(type: "int", nullable: false),
                    IntervaloKMPersonalizado = table.Column<int>(type: "int", nullable: true),
                    IntervaloDiasPersonalizado = table.Column<int>(type: "int", nullable: true),
                    FechaInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaFin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GestionVehiculos_PlanesMantenimiento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GestionVehiculos_PlantillasMantenimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IntervaloKM = table.Column<int>(type: "int", nullable: false),
                    IntervaloDias = table.Column<int>(type: "int", nullable: false),
                    TipoIntervalo = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GestionVehiculos_PlantillasMantenimiento", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionesMantenimiento_FechaEjecucion",
                table: "GestionVehiculos_EjecucionesMantenimiento",
                column: "FechaEjecucion");

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionesMantenimiento_PlacaVehiculo",
                table: "GestionVehiculos_EjecucionesMantenimiento",
                column: "PlacaVehiculo");

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionesMantenimiento_PlanMantenimientoId",
                table: "GestionVehiculos_EjecucionesMantenimiento",
                column: "PlanMantenimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesMantenimiento_PlacaVehiculo",
                table: "GestionVehiculos_PlanesMantenimiento",
                column: "PlacaVehiculo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanesMantenimiento_PlantillaId",
                table: "GestionVehiculos_PlanesMantenimiento",
                column: "PlantillaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasMantenimiento_Nombre",
                table: "GestionVehiculos_PlantillasMantenimiento",
                column: "Nombre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GestionVehiculos_EjecucionesMantenimiento");

            migrationBuilder.DropTable(
                name: "GestionVehiculos_PlanesMantenimiento");

            migrationBuilder.DropTable(
                name: "GestionVehiculos_PlantillasMantenimiento");
        }
    }
}
