using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMaintenanceSchedulingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MantenimientoOcurrencia");

            migrationBuilder.DropTable(
                name: "MantenimientoPlantillaTarea");

            migrationBuilder.DropTable(
                name: "MantenimientoProgramado");

            migrationBuilder.DropTable(
                name: "SeguimientoMantenimientoTarea");

            migrationBuilder.DropTable(
                name: "MantenimientoPlantilla");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MantenimientoOcurrencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EquipoCodigo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaProgramada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRealizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProgramadoId = table.Column<int>(type: "int", nullable: true),
                    Resultado = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    CreadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Predeterminada = table.Column<bool>(type: "bit", nullable: false)
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
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    DiaSemana = table.Column<byte>(type: "tinyint", nullable: true),
                    EquipoCodigo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FrecuenciaValor = table.Column<int>(type: "int", nullable: true),
                    Hora = table.Column<TimeSpan>(type: "time", nullable: true),
                    PlantillaId = table.Column<int>(type: "int", nullable: true),
                    Sede = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TipoFrecuencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MantenimientoProgramado", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeguimientoMantenimientoTarea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Completada = table.Column<bool>(type: "bit", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MantenimientoPlantillaTareaId = table.Column<int>(type: "int", nullable: true),
                    NombreTarea = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RepuestoUsado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SeguimientoMantenimientoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeguimientoMantenimientoTarea", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MantenimientoPlantillaTarea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    PlantillaId = table.Column<int>(type: "int", nullable: true),
                    Predeterminada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MantenimientoPlantillaTarea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MantenimientoPlantillaTarea_MantenimientoPlantilla_PlantillaId",
                        column: x => x.PlantillaId,
                        principalTable: "MantenimientoPlantilla",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MantenimientoPlantillaTarea_PlantillaId",
                table: "MantenimientoPlantillaTarea",
                column: "PlantillaId");

            migrationBuilder.CreateIndex(
                name: "IX_SeguimientoMantenimientoTarea_SeguimientoMantenimientoId",
                table: "SeguimientoMantenimientoTarea",
                column: "SeguimientoMantenimientoId");
        }
    }
}
