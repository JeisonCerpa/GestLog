using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultiplePlansPerVehicleTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('GestionVehiculos_PlanesMantenimiento', 'UltimaFechaMantenimiento') IS NULL
BEGIN
    ALTER TABLE [GestionVehiculos_PlanesMantenimiento]
    ADD [UltimaFechaMantenimiento] datetimeoffset NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('GestionVehiculos_PlanesMantenimiento', 'UltimoKMRegistrado') IS NULL
BEGIN
    ALTER TABLE [GestionVehiculos_PlanesMantenimiento]
    ADD [UltimoKMRegistrado] bigint NULL;
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlanesMantenimiento_PlacaVehiculo' AND object_id = OBJECT_ID('GestionVehiculos_PlanesMantenimiento'))
BEGIN
    DROP INDEX [IX_PlanesMantenimiento_PlacaVehiculo] ON [GestionVehiculos_PlanesMantenimiento];
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlanesMantenimiento_PlacaPlantilla_Activa' AND object_id = OBJECT_ID('GestionVehiculos_PlanesMantenimiento'))
BEGIN
    CREATE UNIQUE INDEX [IX_PlanesMantenimiento_PlacaPlantilla_Activa]
    ON [GestionVehiculos_PlanesMantenimiento]([PlacaVehiculo], [PlantillaId])
    WHERE [IsDeleted] = 0;
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlanesMantenimiento_PlacaVehiculo' AND object_id = OBJECT_ID('GestionVehiculos_PlanesMantenimiento'))
BEGIN
    CREATE INDEX [IX_PlanesMantenimiento_PlacaVehiculo]
    ON [GestionVehiculos_PlanesMantenimiento]([PlacaVehiculo]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlanesMantenimiento_PlacaPlantilla_Activa' AND object_id = OBJECT_ID('GestionVehiculos_PlanesMantenimiento'))
BEGIN
    DROP INDEX [IX_PlanesMantenimiento_PlacaPlantilla_Activa] ON [GestionVehiculos_PlanesMantenimiento];
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlanesMantenimiento_PlacaVehiculo' AND object_id = OBJECT_ID('GestionVehiculos_PlanesMantenimiento'))
BEGIN
    DROP INDEX [IX_PlanesMantenimiento_PlacaVehiculo] ON [GestionVehiculos_PlanesMantenimiento];
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PlanesMantenimiento_PlacaVehiculo' AND object_id = OBJECT_ID('GestionVehiculos_PlanesMantenimiento'))
BEGIN
    CREATE UNIQUE INDEX [IX_PlanesMantenimiento_PlacaVehiculo]
    ON [GestionVehiculos_PlanesMantenimiento]([PlacaVehiculo]);
END
");
        }
    }
}
