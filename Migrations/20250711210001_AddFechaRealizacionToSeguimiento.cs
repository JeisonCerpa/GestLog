using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddFechaRealizacionToSeguimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Fecha",
                table: "Seguimientos",
                newName: "FechaRealizacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FechaRealizacion",
                table: "Seguimientos",
                newName: "Fecha");
        }
    }
}
