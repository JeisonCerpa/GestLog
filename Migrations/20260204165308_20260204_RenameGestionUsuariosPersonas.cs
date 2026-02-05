using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestLog.Migrations
{
    /// <inheritdoc />
    public partial class _20260204_RenameGestionUsuariosPersonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permisos_Permisos_PermisoPadreId",
                table: "Permisos");

            migrationBuilder.DropForeignKey(
                name: "FK_Permisos_Roles_RolIdRol",
                table: "Permisos");

            migrationBuilder.DropForeignKey(
                name: "FK_Personas_Cargos_CargoId",
                table: "Personas");

            migrationBuilder.DropForeignKey(
                name: "FK_Personas_TipoDocumento_TipoDocumentoId",
                table: "Personas");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Permisos_IdPermiso",
                table: "RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_IdRol",
                table: "RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioPermisos_Permisos_IdPermiso",
                table: "UsuarioPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioPermisos_Usuarios_IdUsuario",
                table: "UsuarioPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioRoles_Roles_IdRol",
                table: "UsuarioRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuarioRoles_Usuarios_IdUsuario",
                table: "UsuarioRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuarioRoles",
                table: "UsuarioRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UsuarioPermisos",
                table: "UsuarioPermisos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TipoDocumento",
                table: "TipoDocumento");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolPermisos",
                table: "RolPermisos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Personas",
                table: "Personas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permisos",
                table: "Permisos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cargos",
                table: "Cargos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Auditoria",
                table: "Auditoria");

            migrationBuilder.RenameTable(
                name: "Usuarios",
                newName: "GestionUsuarios_Usuarios");

            migrationBuilder.RenameTable(
                name: "UsuarioRoles",
                newName: "GestionUsuarios_UsuarioRoles");

            migrationBuilder.RenameTable(
                name: "UsuarioPermisos",
                newName: "GestionUsuarios_UsuarioPermisos");

            migrationBuilder.RenameTable(
                name: "TipoDocumento",
                newName: "GestionUsuarios_TiposDocumento");

            migrationBuilder.RenameTable(
                name: "RolPermisos",
                newName: "GestionUsuarios_RolPermisos");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "GestionUsuarios_Roles");

            migrationBuilder.RenameTable(
                name: "Personas",
                newName: "GestionPersonas_Personas");

            migrationBuilder.RenameTable(
                name: "Permisos",
                newName: "GestionUsuarios_Permisos");

            migrationBuilder.RenameTable(
                name: "Cargos",
                newName: "GestionUsuarios_Cargos");

            migrationBuilder.RenameTable(
                name: "Auditoria",
                newName: "GestionUsuarios_Auditorias");

            migrationBuilder.RenameIndex(
                name: "IX_UsuarioRoles_IdRol",
                table: "GestionUsuarios_UsuarioRoles",
                newName: "IX_GestionUsuarios_UsuarioRoles_IdRol");

            migrationBuilder.RenameIndex(
                name: "IX_UsuarioPermisos_IdPermiso",
                table: "GestionUsuarios_UsuarioPermisos",
                newName: "IX_GestionUsuarios_UsuarioPermisos_IdPermiso");

            migrationBuilder.RenameIndex(
                name: "IX_TipoDocumento_Codigo",
                table: "GestionUsuarios_TiposDocumento",
                newName: "IX_GestionUsuarios_TiposDocumento_Codigo");

            migrationBuilder.RenameIndex(
                name: "IX_RolPermisos_IdPermiso",
                table: "GestionUsuarios_RolPermisos",
                newName: "IX_GestionUsuarios_RolPermisos_IdPermiso");

            migrationBuilder.RenameIndex(
                name: "IX_Personas_TipoDocumentoId",
                table: "GestionPersonas_Personas",
                newName: "IX_GestionPersonas_Personas_TipoDocumentoId");

            migrationBuilder.RenameIndex(
                name: "IX_Personas_CargoId",
                table: "GestionPersonas_Personas",
                newName: "IX_GestionPersonas_Personas_CargoId");

            migrationBuilder.RenameIndex(
                name: "IX_Permisos_RolIdRol",
                table: "GestionUsuarios_Permisos",
                newName: "IX_GestionUsuarios_Permisos_RolIdRol");

            migrationBuilder.RenameIndex(
                name: "IX_Permisos_PermisoPadreId",
                table: "GestionUsuarios_Permisos",
                newName: "IX_GestionUsuarios_Permisos_PermisoPadreId");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "GestionUsuarios_Cargos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GestionUsuarios_Usuarios",
                table: "GestionUsuarios_Usuarios",
                column: "IdUsuario");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GestionUsuarios_UsuarioRoles",
                table: "GestionUsuarios_UsuarioRoles",
                columns: new[] { "IdUsuario", "IdRol" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_GestionUsuarios_UsuarioPermisos",
                table: "GestionUsuarios_UsuarioPermisos",
                columns: new[] { "IdUsuario", "IdPermiso" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_GestionUsuarios_TiposDocumento",
                table: "GestionUsuarios_TiposDocumento",
                column: "IdTipoDocumento");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GestionUsuarios_RolPermisos",
                table: "GestionUsuarios_RolPermisos",
                columns: new[] { "IdRol", "IdPermiso" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_GestionUsuarios_Roles",
                table: "GestionUsuarios_Roles",
                column: "IdRol");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GestionPersonas_Personas",
                table: "GestionPersonas_Personas",
                column: "IdPersona");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GestionUsuarios_Permisos",
                table: "GestionUsuarios_Permisos",
                column: "IdPermiso");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GestionUsuarios_Cargos",
                table: "GestionUsuarios_Cargos",
                column: "IdCargo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GestionUsuarios_Auditorias",
                table: "GestionUsuarios_Auditorias",
                column: "IdAuditoria");

            migrationBuilder.AddForeignKey(
                name: "FK_GestionPersonas_Personas_GestionUsuarios_Cargos_CargoId",
                table: "GestionPersonas_Personas",
                column: "CargoId",
                principalTable: "GestionUsuarios_Cargos",
                principalColumn: "IdCargo",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GestionPersonas_Personas_GestionUsuarios_TiposDocumento_TipoDocumentoId",
                table: "GestionPersonas_Personas",
                column: "TipoDocumentoId",
                principalTable: "GestionUsuarios_TiposDocumento",
                principalColumn: "IdTipoDocumento",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GestionUsuarios_Permisos_GestionUsuarios_Permisos_PermisoPadreId",
                table: "GestionUsuarios_Permisos",
                column: "PermisoPadreId",
                principalTable: "GestionUsuarios_Permisos",
                principalColumn: "IdPermiso",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GestionUsuarios_Permisos_GestionUsuarios_Roles_RolIdRol",
                table: "GestionUsuarios_Permisos",
                column: "RolIdRol",
                principalTable: "GestionUsuarios_Roles",
                principalColumn: "IdRol");

            migrationBuilder.AddForeignKey(
                name: "FK_GestionUsuarios_RolPermisos_GestionUsuarios_Permisos_IdPermiso",
                table: "GestionUsuarios_RolPermisos",
                column: "IdPermiso",
                principalTable: "GestionUsuarios_Permisos",
                principalColumn: "IdPermiso",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GestionUsuarios_RolPermisos_GestionUsuarios_Roles_IdRol",
                table: "GestionUsuarios_RolPermisos",
                column: "IdRol",
                principalTable: "GestionUsuarios_Roles",
                principalColumn: "IdRol",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GestionUsuarios_UsuarioPermisos_GestionUsuarios_Permisos_IdPermiso",
                table: "GestionUsuarios_UsuarioPermisos",
                column: "IdPermiso",
                principalTable: "GestionUsuarios_Permisos",
                principalColumn: "IdPermiso",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GestionUsuarios_UsuarioPermisos_GestionUsuarios_Usuarios_IdUsuario",
                table: "GestionUsuarios_UsuarioPermisos",
                column: "IdUsuario",
                principalTable: "GestionUsuarios_Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GestionUsuarios_UsuarioRoles_GestionUsuarios_Roles_IdRol",
                table: "GestionUsuarios_UsuarioRoles",
                column: "IdRol",
                principalTable: "GestionUsuarios_Roles",
                principalColumn: "IdRol",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GestionUsuarios_UsuarioRoles_GestionUsuarios_Usuarios_IdUsuario",
                table: "GestionUsuarios_UsuarioRoles",
                column: "IdUsuario",
                principalTable: "GestionUsuarios_Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GestionPersonas_Personas_GestionUsuarios_Cargos_CargoId",
                table: "GestionPersonas_Personas");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionPersonas_Personas_GestionUsuarios_TiposDocumento_TipoDocumentoId",
                table: "GestionPersonas_Personas");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionUsuarios_Permisos_GestionUsuarios_Permisos_PermisoPadreId",
                table: "GestionUsuarios_Permisos");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionUsuarios_Permisos_GestionUsuarios_Roles_RolIdRol",
                table: "GestionUsuarios_Permisos");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionUsuarios_RolPermisos_GestionUsuarios_Permisos_IdPermiso",
                table: "GestionUsuarios_RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionUsuarios_RolPermisos_GestionUsuarios_Roles_IdRol",
                table: "GestionUsuarios_RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionUsuarios_UsuarioPermisos_GestionUsuarios_Permisos_IdPermiso",
                table: "GestionUsuarios_UsuarioPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionUsuarios_UsuarioPermisos_GestionUsuarios_Usuarios_IdUsuario",
                table: "GestionUsuarios_UsuarioPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionUsuarios_UsuarioRoles_GestionUsuarios_Roles_IdRol",
                table: "GestionUsuarios_UsuarioRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_GestionUsuarios_UsuarioRoles_GestionUsuarios_Usuarios_IdUsuario",
                table: "GestionUsuarios_UsuarioRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionUsuarios_Usuarios",
                table: "GestionUsuarios_Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionUsuarios_UsuarioRoles",
                table: "GestionUsuarios_UsuarioRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionUsuarios_UsuarioPermisos",
                table: "GestionUsuarios_UsuarioPermisos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionUsuarios_TiposDocumento",
                table: "GestionUsuarios_TiposDocumento");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionUsuarios_RolPermisos",
                table: "GestionUsuarios_RolPermisos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionUsuarios_Roles",
                table: "GestionUsuarios_Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionUsuarios_Permisos",
                table: "GestionUsuarios_Permisos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionUsuarios_Cargos",
                table: "GestionUsuarios_Cargos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionUsuarios_Auditorias",
                table: "GestionUsuarios_Auditorias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GestionPersonas_Personas",
                table: "GestionPersonas_Personas");

            migrationBuilder.RenameTable(
                name: "GestionUsuarios_Usuarios",
                newName: "Usuarios");

            migrationBuilder.RenameTable(
                name: "GestionUsuarios_UsuarioRoles",
                newName: "UsuarioRoles");

            migrationBuilder.RenameTable(
                name: "GestionUsuarios_UsuarioPermisos",
                newName: "UsuarioPermisos");

            migrationBuilder.RenameTable(
                name: "GestionUsuarios_TiposDocumento",
                newName: "TipoDocumento");

            migrationBuilder.RenameTable(
                name: "GestionUsuarios_RolPermisos",
                newName: "RolPermisos");

            migrationBuilder.RenameTable(
                name: "GestionUsuarios_Roles",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "GestionUsuarios_Permisos",
                newName: "Permisos");

            migrationBuilder.RenameTable(
                name: "GestionUsuarios_Cargos",
                newName: "Cargos");

            migrationBuilder.RenameTable(
                name: "GestionUsuarios_Auditorias",
                newName: "Auditoria");

            migrationBuilder.RenameTable(
                name: "GestionPersonas_Personas",
                newName: "Personas");

            migrationBuilder.RenameIndex(
                name: "IX_GestionUsuarios_UsuarioRoles_IdRol",
                table: "UsuarioRoles",
                newName: "IX_UsuarioRoles_IdRol");

            migrationBuilder.RenameIndex(
                name: "IX_GestionUsuarios_UsuarioPermisos_IdPermiso",
                table: "UsuarioPermisos",
                newName: "IX_UsuarioPermisos_IdPermiso");

            migrationBuilder.RenameIndex(
                name: "IX_GestionUsuarios_TiposDocumento_Codigo",
                table: "TipoDocumento",
                newName: "IX_TipoDocumento_Codigo");

            migrationBuilder.RenameIndex(
                name: "IX_GestionUsuarios_RolPermisos_IdPermiso",
                table: "RolPermisos",
                newName: "IX_RolPermisos_IdPermiso");

            migrationBuilder.RenameIndex(
                name: "IX_GestionUsuarios_Permisos_RolIdRol",
                table: "Permisos",
                newName: "IX_Permisos_RolIdRol");

            migrationBuilder.RenameIndex(
                name: "IX_GestionUsuarios_Permisos_PermisoPadreId",
                table: "Permisos",
                newName: "IX_Permisos_PermisoPadreId");

            migrationBuilder.RenameIndex(
                name: "IX_GestionPersonas_Personas_TipoDocumentoId",
                table: "Personas",
                newName: "IX_Personas_TipoDocumentoId");

            migrationBuilder.RenameIndex(
                name: "IX_GestionPersonas_Personas_CargoId",
                table: "Personas",
                newName: "IX_Personas_CargoId");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Cargos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios",
                column: "IdUsuario");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuarioRoles",
                table: "UsuarioRoles",
                columns: new[] { "IdUsuario", "IdRol" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsuarioPermisos",
                table: "UsuarioPermisos",
                columns: new[] { "IdUsuario", "IdPermiso" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TipoDocumento",
                table: "TipoDocumento",
                column: "IdTipoDocumento");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolPermisos",
                table: "RolPermisos",
                columns: new[] { "IdRol", "IdPermiso" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "IdRol");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permisos",
                table: "Permisos",
                column: "IdPermiso");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cargos",
                table: "Cargos",
                column: "IdCargo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Auditoria",
                table: "Auditoria",
                column: "IdAuditoria");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Personas",
                table: "Personas",
                column: "IdPersona");

            migrationBuilder.AddForeignKey(
                name: "FK_Permisos_Permisos_PermisoPadreId",
                table: "Permisos",
                column: "PermisoPadreId",
                principalTable: "Permisos",
                principalColumn: "IdPermiso",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Permisos_Roles_RolIdRol",
                table: "Permisos",
                column: "RolIdRol",
                principalTable: "Roles",
                principalColumn: "IdRol");

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

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Permisos_IdPermiso",
                table: "RolPermisos",
                column: "IdPermiso",
                principalTable: "Permisos",
                principalColumn: "IdPermiso",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_IdRol",
                table: "RolPermisos",
                column: "IdRol",
                principalTable: "Roles",
                principalColumn: "IdRol",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioPermisos_Permisos_IdPermiso",
                table: "UsuarioPermisos",
                column: "IdPermiso",
                principalTable: "Permisos",
                principalColumn: "IdPermiso",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioPermisos_Usuarios_IdUsuario",
                table: "UsuarioPermisos",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioRoles_Roles_IdRol",
                table: "UsuarioRoles",
                column: "IdRol",
                principalTable: "Roles",
                principalColumn: "IdRol",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuarioRoles_Usuarios_IdUsuario",
                table: "UsuarioRoles",
                column: "IdUsuario",
                principalTable: "Usuarios",
                principalColumn: "IdUsuario",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
