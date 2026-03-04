using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class _20260304_AddConsumoCombustibleVehiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GestionVehiculos_ConsumosCombustible",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlacaVehiculo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaTanqueada = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    KMAlMomento = table.Column<long>(type: "bigint", nullable: false),
                    Litros = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RutaFactura = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GestionVehiculos_ConsumosCombustible", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosCombustible_FechaTanqueada",
                table: "GestionVehiculos_ConsumosCombustible",
                column: "FechaTanqueada");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosCombustible_PlacaFechaKm",
                table: "GestionVehiculos_ConsumosCombustible",
                columns: new[] { "PlacaVehiculo", "FechaTanqueada", "KMAlMomento" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosCombustible_PlacaVehiculo",
                table: "GestionVehiculos_ConsumosCombustible",
                column: "PlacaVehiculo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GestionVehiculos_ConsumosCombustible");
        }
    }
}
