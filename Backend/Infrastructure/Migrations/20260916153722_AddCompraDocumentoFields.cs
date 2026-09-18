using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCompraDocumentoFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsCredito",
                table: "Compra",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEmision",
                table: "Compra",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonedaId",
                table: "Compra",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDescuento",
                table: "Compra",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Numero",
                table: "Compra",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtrosCargos",
                table: "Compra",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDescuento",
                table: "Compra",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Serie",
                table: "Compra",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoIgvId",
                table: "Compra",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorGravada",
                table: "Compra",
                type: "numeric(13,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIgv",
                table: "Compra",
                type: "numeric(13,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Compra_MonedaId",
                table: "Compra",
                column: "MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_TipoIgvId",
                table: "Compra",
                column: "TipoIgvId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compra_Moneda_MonedaId",
                table: "Compra",
                column: "MonedaId",
                principalTable: "Moneda",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Compra_TipoIgv_TipoIgvId",
                table: "Compra",
                column: "TipoIgvId",
                principalTable: "TipoIgv",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compra_Moneda_MonedaId",
                table: "Compra");

            migrationBuilder.DropForeignKey(
                name: "FK_Compra_TipoIgv_TipoIgvId",
                table: "Compra");

            migrationBuilder.DropIndex(
                name: "IX_Compra_MonedaId",
                table: "Compra");

            migrationBuilder.DropIndex(
                name: "IX_Compra_TipoIgvId",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "EsCredito",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "FechaEmision",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "MonedaId",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "MontoDescuento",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "OtrosCargos",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "PorcentajeDescuento",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "Serie",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "TipoIgvId",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "ValorGravada",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "ValorIgv",
                table: "Compra");
        }
    }
}
