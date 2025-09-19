using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaPerifericosEquiposInformaticos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerifericosEquiposInformaticos",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Dispositivo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaCompra = table.Column<DateTime>(type: "date", nullable: true),
                    Costo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Marca = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodigoEquipoAsignado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UsuarioAsignado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sede = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerifericosEquiposInformaticos", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_PerifericosEquiposInformaticos_EquiposInformaticos_CodigoEquipoAsignado",
                        column: x => x.CodigoEquipoAsignado,
                        principalTable: "EquiposInformaticos",
                        principalColumn: "Codigo");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerifericosEquiposInformaticos_CodigoEquipoAsignado",
                table: "PerifericosEquiposInformaticos",
                column: "CodigoEquipoAsignado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerifericosEquiposInformaticos");
        }
    }
}
