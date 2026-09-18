using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddNotaCreditoDebitoSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SerieNotaCredito",
                table: "ConfiguracionFiscal",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerieNotaDebito",
                table: "ConfiguracionFiscal",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ComprobanteAfectadoId",
                table: "ComprobanteCabecera",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MotivoNotaId",
                table: "ComprobanteCabecera",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MotivoNota",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoDocumentoVentaId = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RevierteStock = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotivoNota", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteCabecera_ComprobanteAfectadoId",
                table: "ComprobanteCabecera",
                column: "ComprobanteAfectadoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteCabecera_MotivoNotaId",
                table: "ComprobanteCabecera",
                column: "MotivoNotaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprobanteCabecera_ComprobanteCabecera_ComprobanteAfectado~",
                table: "ComprobanteCabecera",
                column: "ComprobanteAfectadoId",
                principalTable: "ComprobanteCabecera",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ComprobanteCabecera_MotivoNota_MotivoNotaId",
                table: "ComprobanteCabecera",
                column: "MotivoNotaId",
                principalTable: "MotivoNota",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComprobanteCabecera_ComprobanteCabecera_ComprobanteAfectado~",
                table: "ComprobanteCabecera");

            migrationBuilder.DropForeignKey(
                name: "FK_ComprobanteCabecera_MotivoNota_MotivoNotaId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropTable(
                name: "MotivoNota");

            migrationBuilder.DropIndex(
                name: "IX_ComprobanteCabecera_ComprobanteAfectadoId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropIndex(
                name: "IX_ComprobanteCabecera_MotivoNotaId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "SerieNotaCredito",
                table: "ConfiguracionFiscal");

            migrationBuilder.DropColumn(
                name: "SerieNotaDebito",
                table: "ConfiguracionFiscal");

            migrationBuilder.DropColumn(
                name: "ComprobanteAfectadoId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "MotivoNotaId",
                table: "ComprobanteCabecera");
        }
    }
}
