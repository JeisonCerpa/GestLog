using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class EjecucionSemanalRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId",
                table: "EjecucionSemanal");

            migrationBuilder.DropIndex(
                name: "IX_EjecucionSemanal_PlanId_AnioISO_SemanaISO",
                table: "EjecucionSemanal");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlanId",
                table: "EjecucionSemanal",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "CodigoEquipo",
                table: "EjecucionSemanal",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescripcionPlanSnapshot",
                table: "EjecucionSemanal",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);            migrationBuilder.AddColumn<string>(
                name: "ResponsablePlanSnapshot",
                table: "EjecucionSemanal",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // ✅ POBLAR CodigoEquipo desde PlanCronogramaEquipo
            migrationBuilder.Sql(@"
                UPDATE es
                SET es.CodigoEquipo = p.CodigoEquipo,
                    es.DescripcionPlanSnapshot = p.Descripcion,
                    es.ResponsablePlanSnapshot = p.Responsable
                FROM EjecucionSemanal es
                INNER JOIN PlanCronogramaEquipo p ON es.PlanId = p.PlanId
                WHERE es.CodigoEquipo = ''
            ");

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionSemanal_CodigoEquipo_AnioISO_SemanaISO",
                table: "EjecucionSemanal",
                columns: new[] { "CodigoEquipo", "AnioISO", "SemanaISO" });

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionSemanal_PlanId",
                table: "EjecucionSemanal",
                column: "PlanId");            // ✅ FK a Equipo: ON DELETE NO ACTION (permite que historial persista)
            migrationBuilder.AddForeignKey(
                name: "FK_EjecucionSemanal_EquiposInformaticos_CodigoEquipo",
                table: "EjecucionSemanal",
                column: "CodigoEquipo",
                principalTable: "EquiposInformaticos",
                principalColumn: "Codigo",
                onDelete: ReferentialAction.NoAction);

            // ✅ FK a Plan: ON DELETE SET NULL (desvincula sin borrar historial)
            migrationBuilder.AddForeignKey(
                name: "FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId",
                table: "EjecucionSemanal",
                column: "PlanId",
                principalTable: "PlanCronogramaEquipo",
                principalColumn: "PlanId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EjecucionSemanal_EquiposInformaticos_CodigoEquipo",
                table: "EjecucionSemanal");

            migrationBuilder.DropForeignKey(
                name: "FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId",
                table: "EjecucionSemanal");

            migrationBuilder.DropIndex(
                name: "IX_EjecucionSemanal_CodigoEquipo_AnioISO_SemanaISO",
                table: "EjecucionSemanal");

            migrationBuilder.DropIndex(
                name: "IX_EjecucionSemanal_PlanId",
                table: "EjecucionSemanal");

            migrationBuilder.DropColumn(
                name: "CodigoEquipo",
                table: "EjecucionSemanal");

            migrationBuilder.DropColumn(
                name: "DescripcionPlanSnapshot",
                table: "EjecucionSemanal");

            migrationBuilder.DropColumn(
                name: "ResponsablePlanSnapshot",
                table: "EjecucionSemanal");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlanId",
                table: "EjecucionSemanal",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EjecucionSemanal_PlanId_AnioISO_SemanaISO",
                table: "EjecucionSemanal",
                columns: new[] { "PlanId", "AnioISO", "SemanaISO" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EjecucionSemanal_PlanCronogramaEquipo_PlanId",
                table: "EjecucionSemanal",
                column: "PlanId",
                principalTable: "PlanCronogramaEquipo",
                principalColumn: "PlanId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
