using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class CambioTipoDiscoAString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaRegistro",
                table: "Equipos",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCompra",
                table: "Equipos",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.CreateTable(
                name: "EquiposInformaticos",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsuarioAsignado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NombreEquipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Costo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FechaCompra = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Sede = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CodigoAnydesk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SO = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Marca = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Procesador = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SlotsTotales = table.Column<int>(type: "int", nullable: true),
                    SlotsUtilizados = table.Column<int>(type: "int", nullable: true),
                    TipoRam = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CapacidadTotalRamGB = table.Column<int>(type: "int", nullable: true),
                    CantidadDiscos = table.Column<int>(type: "int", nullable: true),
                    CapacidadTotalDiscosGB = table.Column<int>(type: "int", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaBaja = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquiposInformaticos", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "Discos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoEquipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroDisco = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CapacidadGB = table.Column<int>(type: "int", nullable: true),
                    Marca = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Discos_EquiposInformaticos_CodigoEquipo",
                        column: x => x.CodigoEquipo,
                        principalTable: "EquiposInformaticos",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlotsRam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoEquipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroSlot = table.Column<int>(type: "int", nullable: false),
                    CapacidadGB = table.Column<int>(type: "int", nullable: true),
                    TipoMemoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Marca = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Frecuencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Ocupado = table.Column<bool>(type: "bit", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotsRam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlotsRam_EquiposInformaticos_CodigoEquipo",
                        column: x => x.CodigoEquipo,
                        principalTable: "EquiposInformaticos",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Discos_CodigoEquipo_NumeroDisco",
                table: "Discos",
                columns: new[] { "CodigoEquipo", "NumeroDisco" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlotsRam_CodigoEquipo_NumeroSlot",
                table: "SlotsRam",
                columns: new[] { "CodigoEquipo", "NumeroSlot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Discos");

            migrationBuilder.DropTable(
                name: "SlotsRam");

            migrationBuilder.DropTable(
                name: "EquiposInformaticos");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaRegistro",
                table: "Equipos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCompra",
                table: "Equipos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
