using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCentroCostoDetraccionCambioCuentaContable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CentroCostoId",
                table: "OrdenCompraDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaContableId",
                table: "OrdenCompraDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CentroCostoId",
                table: "CompraDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaContableId",
                table: "CompraDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDetraccion",
                table: "Compra",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDetraccion",
                table: "Compra",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TipoCambio",
                table: "Compra",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoDetraccionId",
                table: "Compra",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CentroCosto",
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
                    table.PrimaryKey("PK_CentroCosto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductoSucursalStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    SucursalId = table.Column<int>(type: "integer", nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoSucursalStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductoSucursalStock_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductoSucursalStock_Sucursal_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompraDetalle_CentroCostoId",
                table: "OrdenCompraDetalle",
                column: "CentroCostoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompraDetalle_CuentaContableId",
                table: "OrdenCompraDetalle",
                column: "CuentaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalle_CentroCostoId",
                table: "CompraDetalle",
                column: "CentroCostoId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalle_CuentaContableId",
                table: "CompraDetalle",
                column: "CuentaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_TipoDetraccionId",
                table: "Compra",
                column: "TipoDetraccionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoSucursalStock_ProductoId_SucursalId",
                table: "ProductoSucursalStock",
                columns: new[] { "ProductoId", "SucursalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductoSucursalStock_SucursalId",
                table: "ProductoSucursalStock",
                column: "SucursalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compra_TipoDetraccion_TipoDetraccionId",
                table: "Compra",
                column: "TipoDetraccionId",
                principalTable: "TipoDetraccion",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CompraDetalle_CentroCosto_CentroCostoId",
                table: "CompraDetalle",
                column: "CentroCostoId",
                principalTable: "CentroCosto",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CompraDetalle_CuentaContable_CuentaContableId",
                table: "CompraDetalle",
                column: "CuentaContableId",
                principalTable: "CuentaContable",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenCompraDetalle_CentroCosto_CentroCostoId",
                table: "OrdenCompraDetalle",
                column: "CentroCostoId",
                principalTable: "CentroCosto",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenCompraDetalle_CuentaContable_CuentaContableId",
                table: "OrdenCompraDetalle",
                column: "CuentaContableId",
                principalTable: "CuentaContable",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compra_TipoDetraccion_TipoDetraccionId",
                table: "Compra");

            migrationBuilder.DropForeignKey(
                name: "FK_CompraDetalle_CentroCosto_CentroCostoId",
                table: "CompraDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_CompraDetalle_CuentaContable_CuentaContableId",
                table: "CompraDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenCompraDetalle_CentroCosto_CentroCostoId",
                table: "OrdenCompraDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenCompraDetalle_CuentaContable_CuentaContableId",
                table: "OrdenCompraDetalle");

            migrationBuilder.DropTable(
                name: "CentroCosto");

            migrationBuilder.DropTable(
                name: "ProductoSucursalStock");

            migrationBuilder.DropIndex(
                name: "IX_OrdenCompraDetalle_CentroCostoId",
                table: "OrdenCompraDetalle");

            migrationBuilder.DropIndex(
                name: "IX_OrdenCompraDetalle_CuentaContableId",
                table: "OrdenCompraDetalle");

            migrationBuilder.DropIndex(
                name: "IX_CompraDetalle_CentroCostoId",
                table: "CompraDetalle");

            migrationBuilder.DropIndex(
                name: "IX_CompraDetalle_CuentaContableId",
                table: "CompraDetalle");

            migrationBuilder.DropIndex(
                name: "IX_Compra_TipoDetraccionId",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "CentroCostoId",
                table: "OrdenCompraDetalle");

            migrationBuilder.DropColumn(
                name: "CuentaContableId",
                table: "OrdenCompraDetalle");

            migrationBuilder.DropColumn(
                name: "CentroCostoId",
                table: "CompraDetalle");

            migrationBuilder.DropColumn(
                name: "CuentaContableId",
                table: "CompraDetalle");

            migrationBuilder.DropColumn(
                name: "FechaDetraccion",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "NumeroDetraccion",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "TipoCambio",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "TipoDetraccionId",
                table: "Compra");
        }
    }
}
