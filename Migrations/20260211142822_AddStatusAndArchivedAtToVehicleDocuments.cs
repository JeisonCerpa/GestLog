using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusAndArchivedAtToVehicleDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "GestionVehiculos_VehicleDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "GestionVehiculos_VehicleDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDocuments_VehicleId_DocumentType_Active",
                table: "GestionVehiculos_VehicleDocuments",
                columns: new[] { "VehicleId", "DocumentType" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleDocuments_VehicleId_DocumentType_Active",
                table: "GestionVehiculos_VehicleDocuments");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "GestionVehiculos_VehicleDocuments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "GestionVehiculos_VehicleDocuments");
        }
    }
}
