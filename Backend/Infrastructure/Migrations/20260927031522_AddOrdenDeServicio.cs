using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddOrdenDeServicio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompraDetalle_Producto_ProductoId",
                table: "CompraDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenCompraDetalle_Producto_ProductoId",
                table: "OrdenCompraDetalle");

            migrationBuilder.AlterColumn<int>(
                name: "ProductoId",
                table: "OrdenCompraDetalle",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "OrdenCompraDetalle",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoOrden",
                table: "OrdenCompra",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                defaultValue: "BIEN");

            // Ordenes ya existentes: todas eran de bien (el concepto de servicio no existia).
            migrationBuilder.Sql("UPDATE \"OrdenCompra\" SET \"TipoOrden\" = 'BIEN' WHERE \"TipoOrden\" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "ProductoId",
                table: "CompraDetalle",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "CompraDetalle",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrdenCompraDetalleId",
                table: "CompraDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CompraDetalle_Producto_ProductoId",
                table: "CompraDetalle",
                column: "ProductoId",
                principalTable: "Producto",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenCompraDetalle_Producto_ProductoId",
                table: "OrdenCompraDetalle",
                column: "ProductoId",
                principalTable: "Producto",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompraDetalle_Producto_ProductoId",
                table: "CompraDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenCompraDetalle_Producto_ProductoId",
                table: "OrdenCompraDetalle");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "OrdenCompraDetalle");

            migrationBuilder.DropColumn(
                name: "TipoOrden",
                table: "OrdenCompra");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "CompraDetalle");

            migrationBuilder.DropColumn(
                name: "OrdenCompraDetalleId",
                table: "CompraDetalle");

            migrationBuilder.AlterColumn<int>(
                name: "ProductoId",
                table: "OrdenCompraDetalle",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductoId",
                table: "CompraDetalle",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CompraDetalle_Producto_ProductoId",
                table: "CompraDetalle",
                column: "ProductoId",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenCompraDetalle_Producto_ProductoId",
                table: "OrdenCompraDetalle",
                column: "ProductoId",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
