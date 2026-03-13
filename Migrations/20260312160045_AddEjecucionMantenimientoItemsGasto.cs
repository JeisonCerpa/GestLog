using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddEjecucionMantenimientoItemsGasto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GestionVehiculos_EjecucionItemsGasto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EjecucionMantenimientoId = table.Column<int>(type: "int", nullable: false),
                    TipoGasto = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NumeroFactura = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RutaFactura = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaDocumento = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GestionVehiculos_EjecucionItemsGasto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GestionVehiculos_EjecucionItemsGasto_GestionVehiculos_EjecucionesMantenimiento_EjecucionMantenimientoId",
                        column: x => x.EjecucionMantenimientoId,
                        principalTable: "GestionVehiculos_EjecucionesMantenimiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionItemsGasto_EjecucionId",
                table: "GestionVehiculos_EjecucionItemsGasto",
                column: "EjecucionMantenimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionItemsGasto_EjecucionTipo",
                table: "GestionVehiculos_EjecucionItemsGasto",
                columns: new[] { "EjecucionMantenimientoId", "TipoGasto" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GestionVehiculos_EjecucionItemsGasto");
        }
    }
}
