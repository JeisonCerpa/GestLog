using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class SimplificarConexiones : Migration
    {        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DHCPHabilitado",
                table: "ConexionesEquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "DNS1",
                table: "ConexionesEquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "DNS2",
                table: "ConexionesEquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "ConexionesEquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "TipoConexion",
                table: "ConexionesEquiposInformaticos");

            migrationBuilder.DropColumn(
                name: "Velocidad",
                table: "ConexionesEquiposInformaticos");
        }        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DHCPHabilitado",
                table: "ConexionesEquiposInformaticos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DNS1",
                table: "ConexionesEquiposInformaticos",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DNS2",
                table: "ConexionesEquiposInformaticos",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "ConexionesEquiposInformaticos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoConexion",
                table: "ConexionesEquiposInformaticos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Velocidad",
                table: "ConexionesEquiposInformaticos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
