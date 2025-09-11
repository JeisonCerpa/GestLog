using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceTemplatesAndTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_SlotsRam_CodigoEquipo_NumeroSlot') BEGIN DROP INDEX [IX_SlotsRam_CodigoEquipo_NumeroSlot] ON [SlotsRam] END;"
            );

            migrationBuilder.Sql(
                "IF EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_Discos_CodigoEquipo_NumeroDisco') BEGIN DROP INDEX [IX_Discos_CodigoEquipo_NumeroDisco] ON [Discos] END;"
            );

            migrationBuilder.CreateTable(
                name: "MantenimientoPlantillaTarea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Predeterminada = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MantenimientoPlantillaTarea", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeguimientoMantenimientoTarea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeguimientoMantenimientoId = table.Column<int>(type: "int", nullable: false),
                    MantenimientoPlantillaTareaId = table.Column<int>(type: "int", nullable: true),
                    NombreTarea = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Completada = table.Column<bool>(type: "bit", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RepuestoUsado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeguimientoMantenimientoTarea", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlotsRam_CodigoEquipo",
                table: "SlotsRam",
                column: "CodigoEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Discos_CodigoEquipo",
                table: "Discos",
                column: "CodigoEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_SeguimientoMantenimientoTarea_SeguimientoMantenimientoId",
                table: "SeguimientoMantenimientoTarea",
                column: "SeguimientoMantenimientoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MantenimientoPlantillaTarea");

            migrationBuilder.DropTable(
                name: "SeguimientoMantenimientoTarea");

            migrationBuilder.DropIndex(
                name: "IX_SlotsRam_CodigoEquipo",
                table: "SlotsRam");

            migrationBuilder.DropIndex(
                name: "IX_Discos_CodigoEquipo",
                table: "Discos");

            migrationBuilder.CreateIndex(
                name: "IX_SlotsRam_CodigoEquipo_NumeroSlot",
                table: "SlotsRam",
                columns: new[] { "CodigoEquipo", "NumeroSlot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discos_CodigoEquipo_NumeroDisco",
                table: "Discos",
                columns: new[] { "CodigoEquipo", "NumeroDisco" },
                unique: true);
        }
    }
}
