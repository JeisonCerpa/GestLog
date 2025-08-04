using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRolIdRolFromPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RolIdRol",
                table: "Permisos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_RolIdRol",
                table: "Permisos",
                column: "RolIdRol");

            migrationBuilder.AddForeignKey(
                name: "FK_Permisos_Roles_RolIdRol",
                table: "Permisos",
                column: "RolIdRol",
                principalTable: "Roles",
                principalColumn: "IdRol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permisos_Roles_RolIdRol",
                table: "Permisos");

            migrationBuilder.DropIndex(
                name: "IX_Permisos_RolIdRol",
                table: "Permisos");

            migrationBuilder.DropColumn(
                name: "RolIdRol",
                table: "Permisos");
        }
    }
}
