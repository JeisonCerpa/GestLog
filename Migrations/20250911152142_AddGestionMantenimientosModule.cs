using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddGestionMantenimientosModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlantillaId",
                table: "MantenimientoPlantillaTarea",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MantenimientoOcurrencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramadoId = table.Column<int>(type: "int", nullable: true),
                    EquipoCodigo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaProgramada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRealizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MantenimientoOcurrencia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MantenimientoPlantilla",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Predeterminada = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MantenimientoPlantilla", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MantenimientoProgramado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipoCodigo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PlantillaId = table.Column<int>(type: "int", nullable: true),
                    TipoFrecuencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FrecuenciaValor = table.Column<int>(type: "int", nullable: true),
                    DiaSemana = table.Column<byte>(type: "tinyint", nullable: true),
                    Hora = table.Column<TimeSpan>(type: "time", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Sede = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MantenimientoProgramado", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MantenimientoPlantillaTarea_PlantillaId",
                table: "MantenimientoPlantillaTarea",
                column: "PlantillaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MantenimientoPlantillaTarea_MantenimientoPlantilla_PlantillaId",
                table: "MantenimientoPlantillaTarea",
                column: "PlantillaId",
                principalTable: "MantenimientoPlantilla",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MantenimientoPlantillaTarea_MantenimientoPlantilla_PlantillaId",
                table: "MantenimientoPlantillaTarea");

            migrationBuilder.DropTable(
                name: "MantenimientoOcurrencia");

            migrationBuilder.DropTable(
                name: "MantenimientoPlantilla");

            migrationBuilder.DropTable(
                name: "MantenimientoProgramado");

            migrationBuilder.DropIndex(
                name: "IX_MantenimientoPlantillaTarea_PlantillaId",
                table: "MantenimientoPlantillaTarea");

            migrationBuilder.DropColumn(
                name: "PlantillaId",
                table: "MantenimientoPlantillaTarea");
        }
    }
}
