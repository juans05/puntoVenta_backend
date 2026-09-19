using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddProductoExtraFieldsAndPresentacionesYPrecios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Producto",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinoPreparacion",
                table: "Producto",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GestionLotes",
                table: "Producto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Icbper",
                table: "Producto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "Producto",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonedaId",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MultiPrecioActivo",
                table: "Producto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoKg",
                table: "Producto",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDetraccion",
                table: "Producto",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioMinimo",
                table: "Producto",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoIgvId",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnidadMedidaId",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrecioAlternativo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PrecioVenta = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrecioAlternativo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrecioAlternativo_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Presentacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Codigo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UnidadMedidaId = table.Column<int>(type: "integer", nullable: false),
                    Factor = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    PrecioMinimo = table.Column<decimal>(type: "numeric(13,2)", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presentacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Presentacion_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Presentacion_UnidadMedida_UnidadMedidaId",
                        column: x => x.UnidadMedidaId,
                        principalTable: "UnidadMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Producto_MonedaId",
                table: "Producto",
                column: "MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_TipoIgvId",
                table: "Producto",
                column: "TipoIgvId");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_UnidadMedidaId",
                table: "Producto",
                column: "UnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_PrecioAlternativo_ProductoId",
                table: "PrecioAlternativo",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Presentacion_ProductoId",
                table: "Presentacion",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Presentacion_UnidadMedidaId",
                table: "Presentacion",
                column: "UnidadMedidaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_Moneda_MonedaId",
                table: "Producto",
                column: "MonedaId",
                principalTable: "Moneda",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_TipoIgv_TipoIgvId",
                table: "Producto",
                column: "TipoIgvId",
                principalTable: "TipoIgv",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_UnidadMedida_UnidadMedidaId",
                table: "Producto",
                column: "UnidadMedidaId",
                principalTable: "UnidadMedida",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Producto_Moneda_MonedaId",
                table: "Producto");

            migrationBuilder.DropForeignKey(
                name: "FK_Producto_TipoIgv_TipoIgvId",
                table: "Producto");

            migrationBuilder.DropForeignKey(
                name: "FK_Producto_UnidadMedida_UnidadMedidaId",
                table: "Producto");

            migrationBuilder.DropTable(
                name: "PrecioAlternativo");

            migrationBuilder.DropTable(
                name: "Presentacion");

            migrationBuilder.DropIndex(
                name: "IX_Producto_MonedaId",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_TipoIgvId",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_UnidadMedidaId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "DestinoPreparacion",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "GestionLotes",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "Icbper",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "MonedaId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "MultiPrecioActivo",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "PesoKg",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "PorcentajeDetraccion",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "PrecioMinimo",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "TipoIgvId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "UnidadMedidaId",
                table: "Producto");
        }
    }
}
