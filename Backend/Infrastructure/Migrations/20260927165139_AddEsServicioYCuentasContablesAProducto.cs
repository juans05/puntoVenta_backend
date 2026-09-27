using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddEsServicioYCuentasContablesAProducto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CuentaCostoId",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaIngresoId",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaInventarioId",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsServicio",
                table: "Producto",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CuentaCostoId",
                table: "Producto",
                column: "CuentaCostoId");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CuentaIngresoId",
                table: "Producto",
                column: "CuentaIngresoId");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CuentaInventarioId",
                table: "Producto",
                column: "CuentaInventarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_CuentaContable_CuentaCostoId",
                table: "Producto",
                column: "CuentaCostoId",
                principalTable: "CuentaContable",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_CuentaContable_CuentaIngresoId",
                table: "Producto",
                column: "CuentaIngresoId",
                principalTable: "CuentaContable",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_CuentaContable_CuentaInventarioId",
                table: "Producto",
                column: "CuentaInventarioId",
                principalTable: "CuentaContable",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Producto_CuentaContable_CuentaCostoId",
                table: "Producto");

            migrationBuilder.DropForeignKey(
                name: "FK_Producto_CuentaContable_CuentaIngresoId",
                table: "Producto");

            migrationBuilder.DropForeignKey(
                name: "FK_Producto_CuentaContable_CuentaInventarioId",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_CuentaCostoId",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_CuentaIngresoId",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_CuentaInventarioId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "CuentaCostoId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "CuentaIngresoId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "CuentaInventarioId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "EsServicio",
                table: "Producto");
        }
    }
}
