using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCamposAvanzadosComprobante : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColaboradorId",
                table: "ComprobanteCabecera",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Etiquetas",
                table: "ComprobanteCabecera",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "ComprobanteCabecera",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuiaRemisionElectronica",
                table: "ComprobanteCabecera",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuiaRemisionManual",
                table: "ComprobanteCabecera",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonedaId",
                table: "ComprobanteCabecera",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoAnticipo",
                table: "ComprobanteCabecera",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoRetencion",
                table: "ComprobanteCabecera",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroOrden",
                table: "ComprobanteCabecera",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlacaVehiculo",
                table: "ComprobanteCabecera",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TipoCambio",
                table: "ComprobanteCabecera",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoOperacionId",
                table: "ComprobanteCabecera",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TipoOperacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoOperacion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteCabecera_MonedaId",
                table: "ComprobanteCabecera",
                column: "MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteCabecera_TipoOperacionId",
                table: "ComprobanteCabecera",
                column: "TipoOperacionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprobanteCabecera_Moneda_MonedaId",
                table: "ComprobanteCabecera",
                column: "MonedaId",
                principalTable: "Moneda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ComprobanteCabecera_TipoOperacion_TipoOperacionId",
                table: "ComprobanteCabecera",
                column: "TipoOperacionId",
                principalTable: "TipoOperacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComprobanteCabecera_Moneda_MonedaId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropForeignKey(
                name: "FK_ComprobanteCabecera_TipoOperacion_TipoOperacionId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropTable(
                name: "TipoOperacion");

            migrationBuilder.DropIndex(
                name: "IX_ComprobanteCabecera_MonedaId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropIndex(
                name: "IX_ComprobanteCabecera_TipoOperacionId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "ColaboradorId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "Etiquetas",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "GuiaRemisionElectronica",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "GuiaRemisionManual",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "MonedaId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "MontoAnticipo",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "MontoRetencion",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "NumeroOrden",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "PlacaVehiculo",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "TipoCambio",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "TipoOperacionId",
                table: "ComprobanteCabecera");
        }
    }
}
