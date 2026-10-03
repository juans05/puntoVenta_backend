using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCuentasProductoDetraccionVenta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CuentaGastoHaberId",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaIngresoDebeId",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentasInventarioMovimiento",
                table: "Producto",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDetraccion",
                table: "ComprobanteCabecera",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoDetraccionId",
                table: "ComprobanteCabecera",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CuentaGastoHaberId",
                table: "Producto",
                column: "CuentaGastoHaberId");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CuentaIngresoDebeId",
                table: "Producto",
                column: "CuentaIngresoDebeId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobanteCabecera_TipoDetraccionId",
                table: "ComprobanteCabecera",
                column: "TipoDetraccionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComprobanteCabecera_TipoDetraccion_TipoDetraccionId",
                table: "ComprobanteCabecera",
                column: "TipoDetraccionId",
                principalTable: "TipoDetraccion",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_CuentaContable_CuentaGastoHaberId",
                table: "Producto",
                column: "CuentaGastoHaberId",
                principalTable: "CuentaContable",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Producto_CuentaContable_CuentaIngresoDebeId",
                table: "Producto",
                column: "CuentaIngresoDebeId",
                principalTable: "CuentaContable",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComprobanteCabecera_TipoDetraccion_TipoDetraccionId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropForeignKey(
                name: "FK_Producto_CuentaContable_CuentaGastoHaberId",
                table: "Producto");

            migrationBuilder.DropForeignKey(
                name: "FK_Producto_CuentaContable_CuentaIngresoDebeId",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_CuentaGastoHaberId",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_CuentaIngresoDebeId",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_ComprobanteCabecera_TipoDetraccionId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "CuentaGastoHaberId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "CuentaIngresoDebeId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "CuentasInventarioMovimiento",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "MontoDetraccion",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "TipoDetraccionId",
                table: "ComprobanteCabecera");
        }
    }
}
