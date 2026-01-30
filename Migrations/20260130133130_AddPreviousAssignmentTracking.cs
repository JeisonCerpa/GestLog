using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddPreviousAssignmentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoEquipoAsignadoAnterior",
                table: "PerifericosEquiposInformaticos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioAsignadoAnterior",
                table: "PerifericosEquiposInformaticos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioAsignadoAnterior",
                table: "EquiposInformaticos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoEquipoAsignadoAnterior",
                table: "PerifericosEquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "UsuarioAsignadoAnterior",
                table: "PerifericosEquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "UsuarioAsignadoAnterior",
                table: "EquiposInformaticos");
        }
    }
}
