using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCuentaContableYAsientoContable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsientoContable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Glosa = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    OrigenTipo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    OrigenId = table.Column<int>(type: "integer", nullable: false),
                    EstadoAsiento = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsientoContable", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CuentaContable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CuentaPadreId = table.Column<int>(type: "integer", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentaContable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuentaContable_CuentaContable_CuentaPadreId",
                        column: x => x.CuentaPadreId,
                        principalTable: "CuentaContable",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AsientoContableDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AsientoContableId = table.Column<int>(type: "integer", nullable: false),
                    CuentaContableId = table.Column<int>(type: "integer", nullable: false),
                    Debe = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    Haber = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsientoContableDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsientoContableDetalle_AsientoContable_AsientoContableId",
                        column: x => x.AsientoContableId,
                        principalTable: "AsientoContable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsientoContableDetalle_CuentaContable_CuentaContableId",
                        column: x => x.CuentaContableId,
                        principalTable: "CuentaContable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsientoContableDetalle_AsientoContableId",
                table: "AsientoContableDetalle",
                column: "AsientoContableId");

            migrationBuilder.CreateIndex(
                name: "IX_AsientoContableDetalle_CuentaContableId",
                table: "AsientoContableDetalle",
                column: "CuentaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentaContable_CuentaPadreId",
                table: "CuentaContable",
                column: "CuentaPadreId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsientoContableDetalle");

            migrationBuilder.DropTable(
                name: "AsientoContable");

            migrationBuilder.DropTable(
                name: "CuentaContable");
        }
    }
}
