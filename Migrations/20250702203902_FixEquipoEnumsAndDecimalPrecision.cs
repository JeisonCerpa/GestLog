using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class FixEquipoEnumsAndDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Forzar conversión de Sede, Estado y FrecuenciaMtto a int si hay residuos string
            migrationBuilder.Sql(@"
                -- Sede: solo si aún hay valores string
                UPDATE Equipos SET Sede = 1 WHERE Sede = 'Administrativa';
                UPDATE Equipos SET Sede = 2 WHERE Sede = 'Taller';
                UPDATE Equipos SET Sede = 3 WHERE Sede = 'Bayunca';
                -- EstadoEquipo: ejemplo de valores posibles
                UPDATE Equipos SET Estado = 0 WHERE Estado = 'Activo';
                UPDATE Equipos SET Estado = 1 WHERE Estado = 'Inactivo';
                UPDATE Equipos SET Estado = 2 WHERE Estado = 'Baja';
                -- FrecuenciaMtto: si hay valores string numéricos
                UPDATE Equipos SET FrecuenciaMtto = 1 WHERE FrecuenciaMtto = '1';
                UPDATE Equipos SET FrecuenciaMtto = 2 WHERE FrecuenciaMtto = '2';
                UPDATE Equipos SET FrecuenciaMtto = 3 WHERE FrecuenciaMtto = '3';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
