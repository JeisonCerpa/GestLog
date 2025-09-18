using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaConexionesEquiposInformaticos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {            migrationBuilder.CreateTable(
                name: "ConexionesEquiposInformaticos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoEquipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Adaptador = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DireccionMAC = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: true),
                    DireccionIPv4 = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    MascaraSubred = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PuertoEnlace = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    DNS1 = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    DNS2 = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    TipoConexion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Velocidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DHCPHabilitado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {                    table.PrimaryKey("PK_ConexionesEquiposInformaticos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConexionesEquiposInformaticos_EquiposInformaticos_CodigoEquipo",
                        column: x => x.CodigoEquipo,
                        principalTable: "EquiposInformaticos",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConexionesEquiposInformaticos_CodigoEquipo",
                table: "ConexionesEquiposInformaticos",
                column: "CodigoEquipo");
        }        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConexionesEquiposInformaticos");
        }
    }
}
