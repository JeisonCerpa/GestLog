using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanCronogramaEquipo",
                columns: table => new
                {
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoEquipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiaProgramado = table.Column<byte>(type: "tinyint", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChecklistJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EquipoCodigo = table.Column<string>(type: "nvarchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanCronogramaEquipo", x => x.PlanId);
                    table.ForeignKey(
                        name: "FK_PlanCronogramaEquipo_EquiposInformaticos_EquipoCodigo",
                        column: x => x.EquipoCodigo,
                        principalTable: "EquiposInformaticos",
                        principalColumn: "Codigo");
                });

            migrationBuilder.CreateTable(
                name: "EjecucionSemanal",
                columns: table => new
                {
                    EjecucionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnioISO = table.Column<short>(type: "smallint", nullable: false),
                    SemanaISO = table.Column<byte>(type: "tinyint", nullable: false),
                    FechaObjetivo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEjecucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<byte>(type: "tinyint", nullable: false),
                    UsuarioEjecuta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResultadoJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjecucionSemanal", x => x.EjecucionId);
                    table.ForeignKey(
                        name: "FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId",
                        column: x => x.PlanId,
                        principalTable: "PlanCronogramaEquipo",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionSemanal_PlanId_AnioISO_SemanaISO",
                table: "EjecucionSemanal",
                columns: new[] { "PlanId", "AnioISO", "SemanaISO" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanCronogramaEquipo_EquipoCodigo",
                table: "PlanCronogramaEquipo",
                column: "EquipoCodigo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EjecucionSemanal");

            migrationBuilder.DropTable(
                name: "PlanCronogramaEquipo");
        }
    }
}
