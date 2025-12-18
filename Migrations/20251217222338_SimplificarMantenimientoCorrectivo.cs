using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class SimplificarMantenimientoCorrectivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquiposInformaticos_MantenimientosCorrectivos_EquiposInformaticos_EquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.DropForeignKey(
                name: "FK_EquiposInformaticos_MantenimientosCorrectivos_PerifericosEquiposInformaticos_PerifericoEquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.DropIndex(
                name: "IX_EquiposInformaticos_MantenimientosCorrectivos_EquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.DropIndex(
                name: "IX_EquiposInformaticos_MantenimientosCorrectivos_PerifericoEquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.DropColumn(
                name: "DadoDeBaja",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.DropColumn(
                name: "EquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.DropColumn(
                name: "EquipoInformaticoId",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.DropColumn(
                name: "PerifericoEquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.DropColumn(
                name: "PerifericoEquipoInformaticoId",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.DropColumn(
                name: "UsuarioRegistroId",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos");

            migrationBuilder.AddColumn<bool>(
                name: "DadoDeBaja",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                type: "nvarchar(20)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EquipoInformaticoId",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PerifericoEquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                type: "nvarchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PerifericoEquipoInformaticoId",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioRegistroId",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquiposInformaticos_MantenimientosCorrectivos_EquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                column: "EquipoInformaticoCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_EquiposInformaticos_MantenimientosCorrectivos_PerifericoEquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                column: "PerifericoEquipoInformaticoCodigo");

            migrationBuilder.AddForeignKey(
                name: "FK_EquiposInformaticos_MantenimientosCorrectivos_EquiposInformaticos_EquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                column: "EquipoInformaticoCodigo",
                principalTable: "EquiposInformaticos",
                principalColumn: "Codigo");

            migrationBuilder.AddForeignKey(
                name: "FK_EquiposInformaticos_MantenimientosCorrectivos_PerifericosEquiposInformaticos_PerifericoEquipoInformaticoCodigo",
                table: "EquiposInformaticos_MantenimientosCorrectivos",
                column: "PerifericoEquipoInformaticoCodigo",
                principalTable: "PerifericosEquiposInformaticos",
                principalColumn: "Codigo");
        }
    }
}
