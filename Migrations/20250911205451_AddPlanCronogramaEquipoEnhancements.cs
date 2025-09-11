using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanCronogramaEquipoEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "PlanCronogramaEquipo",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "PlanCronogramaEquipo",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Responsable",
                table: "PlanCronogramaEquipo",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "PlanCronogramaEquipo");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "PlanCronogramaEquipo");

            migrationBuilder.DropColumn(
                name: "Responsable",
                table: "PlanCronogramaEquipo");
        }
    }
}
