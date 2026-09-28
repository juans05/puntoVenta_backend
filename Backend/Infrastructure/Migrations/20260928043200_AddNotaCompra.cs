using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddNotaCompra : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotaCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CompraId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    MotivoNotaId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    EstadoNota = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Observacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaCompra_Compra_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotaCompra_MotivoNota_MotivoNotaId",
                        column: x => x.MotivoNotaId,
                        principalTable: "MotivoNota",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotaCompra_CompraId",
                table: "NotaCompra",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaCompra_MotivoNotaId",
                table: "NotaCompra",
                column: "MotivoNotaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotaCompra");
        }
    }
}
