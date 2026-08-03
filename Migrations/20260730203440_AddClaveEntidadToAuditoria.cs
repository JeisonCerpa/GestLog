using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddClaveEntidadToAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaveEntidad",
                table: "GestionUsuarios_Auditorias",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GestionUsuarios_Auditorias_EntidadAfectada_ClaveEntidad",
                table: "GestionUsuarios_Auditorias",
                columns: new[] { "EntidadAfectada", "ClaveEntidad" });

            migrationBuilder.CreateIndex(
                name: "IX_GestionUsuarios_Auditorias_EntidadAfectada_IdEntidad",
                table: "GestionUsuarios_Auditorias",
                columns: new[] { "EntidadAfectada", "IdEntidad" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GestionUsuarios_Auditorias_EntidadAfectada_ClaveEntidad",
                table: "GestionUsuarios_Auditorias");

            migrationBuilder.DropIndex(
                name: "IX_GestionUsuarios_Auditorias_EntidadAfectada_IdEntidad",
                table: "GestionUsuarios_Auditorias");

            migrationBuilder.DropColumn(
                name: "ClaveEntidad",
                table: "GestionUsuarios_Auditorias");
        }
    }
}
