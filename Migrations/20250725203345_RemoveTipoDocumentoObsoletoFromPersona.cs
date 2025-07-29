using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTipoDocumentoObsoletoFromPersona : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Personas_Cargos_CargoId",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Personas");

            migrationBuilder.AddColumn<Guid>(
                name: "TipoDocumentoId",
                table: "Personas",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Personas_TipoDocumentoId",
                table: "Personas",
                column: "TipoDocumentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Personas_Cargos_CargoId",
                table: "Personas",
                column: "CargoId",
                principalTable: "Cargos",
                principalColumn: "IdCargo",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Personas_TipoDocumento_TipoDocumentoId",
                table: "Personas",
                column: "TipoDocumentoId",
                principalTable: "TipoDocumento",
                principalColumn: "IdTipoDocumento",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Personas_Cargos_CargoId",
                table: "Personas");

            migrationBuilder.DropForeignKey(
                name: "FK_Personas_TipoDocumento_TipoDocumentoId",
                table: "Personas");

            migrationBuilder.DropIndex(
                name: "IX_Personas_TipoDocumentoId",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "TipoDocumentoId",
                table: "Personas");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "Personas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Personas_Cargos_CargoId",
                table: "Personas",
                column: "CargoId",
                principalTable: "Cargos",
                principalColumn: "IdCargo",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
