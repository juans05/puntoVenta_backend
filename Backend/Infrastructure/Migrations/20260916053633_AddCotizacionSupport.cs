using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCotizacionSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SerieCotizacion",
                table: "ConfiguracionFiscal",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CotizacionOrigenId",
                table: "ComprobanteCabecera",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVigencia",
                table: "ComprobanteCabecera",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteCabecera_CotizacionOrigenId",
                table: "ComprobanteCabecera",
                column: "CotizacionOrigenId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprobanteCabecera_ComprobanteCabecera_CotizacionOrigenId",
                table: "ComprobanteCabecera",
                column: "CotizacionOrigenId",
                principalTable: "ComprobanteCabecera",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComprobanteCabecera_ComprobanteCabecera_CotizacionOrigenId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropIndex(
                name: "IX_ComprobanteCabecera_CotizacionOrigenId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "SerieCotizacion",
                table: "ConfiguracionFiscal");

            migrationBuilder.DropColumn(
                name: "CotizacionOrigenId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "FechaVigencia",
                table: "ComprobanteCabecera");
        }
    }
}
