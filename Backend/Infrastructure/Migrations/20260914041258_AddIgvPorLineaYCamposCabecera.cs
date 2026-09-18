using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddIgvPorLineaYCamposCabecera : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoIgvId",
                table: "ComprobanteDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnidadMedidaId",
                table: "ComprobanteDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsCredito",
                table: "ComprobanteCabecera",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDescuento",
                table: "ComprobanteCabecera",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoRecibido",
                table: "ComprobanteCabecera",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observacion",
                table: "ComprobanteCabecera",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDescuento",
                table: "ComprobanteCabecera",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Vuelto",
                table: "ComprobanteCabecera",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TipoIgv",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    AplicaPorcentajeImpuesto = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoIgv", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnidadMedida",
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
                    table.PrimaryKey("PK_UnidadMedida", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteDetalle_TipoIgvId",
                table: "ComprobanteDetalle",
                column: "TipoIgvId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteDetalle_UnidadMedidaId",
                table: "ComprobanteDetalle",
                column: "UnidadMedidaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprobanteDetalle_TipoIgv_TipoIgvId",
                table: "ComprobanteDetalle",
                column: "TipoIgvId",
                principalTable: "TipoIgv",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprobanteDetalle_UnidadMedida_UnidadMedidaId",
                table: "ComprobanteDetalle",
                column: "UnidadMedidaId",
                principalTable: "UnidadMedida",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComprobanteDetalle_TipoIgv_TipoIgvId",
                table: "ComprobanteDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_ComprobanteDetalle_UnidadMedida_UnidadMedidaId",
                table: "ComprobanteDetalle");

            migrationBuilder.DropTable(
                name: "TipoIgv");

            migrationBuilder.DropTable(
                name: "UnidadMedida");

            migrationBuilder.DropIndex(
                name: "IX_ComprobanteDetalle_TipoIgvId",
                table: "ComprobanteDetalle");

            migrationBuilder.DropIndex(
                name: "IX_ComprobanteDetalle_UnidadMedidaId",
                table: "ComprobanteDetalle");

            migrationBuilder.DropColumn(
                name: "TipoIgvId",
                table: "ComprobanteDetalle");

            migrationBuilder.DropColumn(
                name: "UnidadMedidaId",
                table: "ComprobanteDetalle");

            migrationBuilder.DropColumn(
                name: "EsCredito",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "MontoDescuento",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "MontoRecibido",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "Observacion",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "PorcentajeDescuento",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "Vuelto",
                table: "ComprobanteCabecera");
        }
    }
}
