using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddRoleTenantAndRoleSubmodule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles");

            migrationBuilder.AddColumn<int>(
                name: "Prioridad",
                table: "AspNetRoles",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<string>(
                name: "RutaPorDefecto",
                table: "AspNetRoles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "AspNetRoles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoleSubmodule",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SubmoduleId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleSubmodule", x => new { x.RoleId, x.SubmoduleId });
                    table.ForeignKey(
                        name: "FK_RoleSubmodule_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleSubmodule_AspNetSubModule_SubmoduleId",
                        column: x => x.SubmoduleId,
                        principalTable: "AspNetSubModule",
                        principalColumn: "Identificador",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "RoleNameTenantIndex",
                table: "AspNetRoles",
                columns: new[] { "NormalizedName", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleSubmodule_SubmoduleId",
                table: "RoleSubmodule",
                column: "SubmoduleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleSubmodule");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "RoleNameTenantIndex",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "Prioridad",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "RutaPorDefecto",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetRoles");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);
        }
    }
}
