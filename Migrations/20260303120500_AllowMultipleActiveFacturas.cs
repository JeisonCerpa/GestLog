using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    public partial class AllowMultipleActiveFacturas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleDocuments_VehicleId_DocumentType_Active",
                table: "GestionVehiculos_VehicleDocuments");

            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX [IX_VehicleDocuments_VehicleId_DocumentType_Active]
ON [GestionVehiculos_VehicleDocuments] ([VehicleId], [DocumentType])
WHERE [IsActive] = 1 AND [DocumentType] IN (N'SOAT', N'Tecno-Mecánica')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleDocuments_VehicleId_DocumentType_Active",
                table: "GestionVehiculos_VehicleDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDocuments_VehicleId_DocumentType_Active",
                table: "GestionVehiculos_VehicleDocuments",
                columns: new[] { "VehicleId", "DocumentType" },
                unique: true,
                filter: "[IsActive] = 1");
        }
    }
}
