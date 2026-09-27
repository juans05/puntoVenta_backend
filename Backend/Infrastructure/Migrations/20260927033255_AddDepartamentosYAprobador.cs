using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddDepartamentosYAprobador : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AprobadorAsignadoId",
                table: "OrdenCompra",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartamentoId",
                table: "OrdenCompra",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartamentoId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepartamentoAprobador",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DepartamentoId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartamentoAprobador", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartamentoAprobador_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DepartamentoAprobador_Departamento_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompra_AprobadorAsignadoId",
                table: "OrdenCompra",
                column: "AprobadorAsignadoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompra_DepartamentoId",
                table: "OrdenCompra",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DepartamentoId",
                table: "AspNetUsers",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartamentoAprobador_DepartamentoId",
                table: "DepartamentoAprobador",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartamentoAprobador_UserId",
                table: "DepartamentoAprobador",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Departamento_DepartamentoId",
                table: "AspNetUsers",
                column: "DepartamentoId",
                principalTable: "Departamento",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenCompra_AspNetUsers_AprobadorAsignadoId",
                table: "OrdenCompra",
                column: "AprobadorAsignadoId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenCompra_Departamento_DepartamentoId",
                table: "OrdenCompra",
                column: "DepartamentoId",
                principalTable: "Departamento",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Departamento_DepartamentoId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenCompra_AspNetUsers_AprobadorAsignadoId",
                table: "OrdenCompra");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenCompra_Departamento_DepartamentoId",
                table: "OrdenCompra");

            migrationBuilder.DropTable(
                name: "DepartamentoAprobador");

            migrationBuilder.DropTable(
                name: "Departamento");

            migrationBuilder.DropIndex(
                name: "IX_OrdenCompra_AprobadorAsignadoId",
                table: "OrdenCompra");

            migrationBuilder.DropIndex(
                name: "IX_OrdenCompra_DepartamentoId",
                table: "OrdenCompra");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DepartamentoId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AprobadorAsignadoId",
                table: "OrdenCompra");

            migrationBuilder.DropColumn(
                name: "DepartamentoId",
                table: "OrdenCompra");

            migrationBuilder.DropColumn(
                name: "DepartamentoId",
                table: "AspNetUsers");
        }
    }
}
