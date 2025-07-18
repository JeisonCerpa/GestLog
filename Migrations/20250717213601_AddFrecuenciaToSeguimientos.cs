using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddFrecuenciaToSeguimientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Frecuencia",
                table: "Seguimientos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Frecuencia",
                table: "Seguimientos");
        }
    }
}
