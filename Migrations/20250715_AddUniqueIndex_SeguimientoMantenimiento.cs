using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <summary>
    /// Agrega un índice único a Seguimientos (Codigo, Semana, Anio)
    /// </summary>
    public partial class AddUniqueIndex_SeguimientoMantenimiento : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Seguimientos_Codigo_Semana_Anio",
                table: "Seguimientos",
                columns: new[] { "Codigo", "Semana", "Anio" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seguimientos_Codigo_Semana_Anio",
                table: "Seguimientos");
        }
    }
}
