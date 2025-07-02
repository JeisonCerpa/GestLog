using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    public partial class UpdateEquipoEnums_Custom : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear columna temporal para Sede (int)
            migrationBuilder.AddColumn<int>(
                name: "SedeTemp",
                table: "Equipos",
                type: "int",
                nullable: true);

            // 2. Mapear los valores string existentes a int (enum Sede)
            migrationBuilder.Sql(@"
                UPDATE Equipos SET SedeTemp = 1 WHERE Sede = 'Administrativa';
                UPDATE Equipos SET SedeTemp = 2 WHERE Sede = 'Taller';
                UPDATE Equipos SET SedeTemp = 3 WHERE Sede = 'Bayunca';
            ");

            // 3. Eliminar columna Sede original
            migrationBuilder.DropColumn(
                name: "Sede",
                table: "Equipos");

            // 4. Renombrar columna temporal a Sede
            migrationBuilder.RenameColumn(
                name: "SedeTemp",
                table: "Equipos",
                newName: "Sede");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Crear columna temporal para Sede (string)
            migrationBuilder.AddColumn<string>(
                name: "SedeTemp",
                table: "Equipos",
                type: "nvarchar(max)",
                nullable: true);

            // 2. Mapear los valores int a string
            migrationBuilder.Sql(@"
                UPDATE Equipos SET SedeTemp = 'Administrativa' WHERE Sede = 1;
                UPDATE Equipos SET SedeTemp = 'Taller' WHERE Sede = 2;
                UPDATE Equipos SET SedeTemp = 'Bayunca' WHERE Sede = 3;
            ");

            // 3. Eliminar columna Sede original
            migrationBuilder.DropColumn(
                name: "Sede",
                table: "Equipos");

            // 4. Renombrar columna temporal a Sede
            migrationBuilder.RenameColumn(
                name: "SedeTemp",
                table: "Equipos",
                newName: "Sede");
        }
    }
}
