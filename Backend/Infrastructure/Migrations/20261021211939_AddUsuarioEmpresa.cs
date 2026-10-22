using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddUsuarioEmpresa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PlanCuentasEnBlanco",
                table: "Tenant",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UsuarioEmpresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    EsPropietario = table.Column<bool>(type: "boolean", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioEmpresa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioEmpresa_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioEmpresa_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Identificador",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioEmpresa_TenantId",
                table: "UsuarioEmpresa",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioEmpresa_UserId_TenantId",
                table: "UsuarioEmpresa",
                columns: new[] { "UserId", "TenantId" },
                unique: true);

            // Cada usuario existente conserva el acceso a su empresa. Propietario = el admin fundador
            // que crea TenantRepository.CreateUserAdmin (su usuario es el TenantKey o el Name del tenant).
            migrationBuilder.Sql(@"
                INSERT INTO ""UsuarioEmpresa"" (""UserId"", ""TenantId"", ""EsPropietario"", ""Estado"", ""FechaCreacion"")
                SELECT u.""Id"", u.""TenantId"",
                       u.""NormalizedUserName"" IN (UPPER(t.""TenantKey""), UPPER(t.""Name"")),
                       TRUE, NOW()
                FROM ""AspNetUsers"" u
                JOIN ""Tenant"" t ON t.""Identificador"" = u.""TenantId"";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuarioEmpresa");

            migrationBuilder.DropColumn(
                name: "PlanCuentasEnBlanco",
                table: "Tenant");
        }
    }
}
