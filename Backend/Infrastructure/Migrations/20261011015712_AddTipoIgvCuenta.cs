using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddTipoIgvCuenta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TipoIgvCuenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoIgvId = table.Column<int>(type: "integer", nullable: false),
                    CuentaContableId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoIgvCuenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TipoIgvCuenta_CuentaContable_CuentaContableId",
                        column: x => x.CuentaContableId,
                        principalTable: "CuentaContable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TipoIgvCuenta_TipoIgv_TipoIgvId",
                        column: x => x.TipoIgvId,
                        principalTable: "TipoIgv",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TipoIgvCuenta_CuentaContableId",
                table: "TipoIgvCuenta",
                column: "CuentaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_TipoIgvCuenta_TenantId_TipoIgvId",
                table: "TipoIgvCuenta",
                columns: new[] { "TenantId", "TipoIgvId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TipoIgvCuenta_TipoIgvId",
                table: "TipoIgvCuenta",
                column: "TipoIgvId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TipoIgvCuenta");
        }
    }
}
