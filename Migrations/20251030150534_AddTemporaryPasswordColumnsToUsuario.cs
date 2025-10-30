using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddTemporaryPasswordColumnsToUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TemporaryPasswordExpiration",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemporaryPasswordHash",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemporaryPasswordSalt",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemporaryPasswordExpiration",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TemporaryPasswordHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TemporaryPasswordSalt",
                table: "Usuarios");
        }
    }
}
