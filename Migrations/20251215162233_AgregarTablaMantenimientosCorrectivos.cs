using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaMantenimientosCorrectivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquiposInformaticos_MantenimientosCorrectivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoEntidad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EquipoInformaticoId = table.Column<int>(type: "int", nullable: true),
                    EquipoInformaticoCodigo = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    PerifericoEquipoInformaticoId = table.Column<int>(type: "int", nullable: true),
                    PerifericoEquipoInformaticoCodigo = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    FechaFalla = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DescripcionFalla = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProveedorAsignado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCompletado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DadoDeBaja = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioRegistroId = table.Column<int>(type: "int", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquiposInformaticos_MantenimientosCorrectivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquiposInformaticos_MantenimientosCorrectivos_EquiposInformaticos_EquipoInformaticoCodigo",
                        column: x => x.EquipoInformaticoCodigo,
                        principalTable: "EquiposInformaticos",
                        principalColumn: "Codigo");
                    table.ForeignKey(
                        name: "FK_EquiposInformaticos_MantenimientosCorrectivos_PerifericosEquiposInformaticos_PerifericoEquipoInformaticoCodigo",
                        column: x => x.PerifericoEquipoInformaticoCodigo,
                        principalTable: "PerifericosEquiposInformaticos",
                        principalColumn: "Codigo");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquiposInformaticos_MantenimientosCorrectivos_EquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                column: "EquipoInformaticoCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_EquiposInformaticos_MantenimientosCorrectivos_PerifericoEquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                column: "PerifericoEquipoInformaticoCodigo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquiposInformaticos_MantenimientosCorrectivos");
        }
    }
}
