using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCuentaPorCobrarYPagarASocios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CuentaPorPagarId",
                table: "Proveedor",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaPorCobrarId",
                table: "Cliente",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_CuentaPorPagarId",
                table: "Proveedor",
                column: "CuentaPorPagarId");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_CuentaPorCobrarId",
                table: "Cliente",
                column: "CuentaPorCobrarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cliente_CuentaContable_CuentaPorCobrarId",
                table: "Cliente",
                column: "CuentaPorCobrarId",
                principalTable: "CuentaContable",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Proveedor_CuentaContable_CuentaPorPagarId",
                table: "Proveedor",
                column: "CuentaPorPagarId",
                principalTable: "CuentaContable",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cliente_CuentaContable_CuentaPorCobrarId",
                table: "Cliente");

            migrationBuilder.DropForeignKey(
                name: "FK_Proveedor_CuentaContable_CuentaPorPagarId",
                table: "Proveedor");

            migrationBuilder.DropIndex(
                name: "IX_Proveedor_CuentaPorPagarId",
                table: "Proveedor");

            migrationBuilder.DropIndex(
                name: "IX_Cliente_CuentaPorCobrarId",
                table: "Cliente");

            migrationBuilder.DropColumn(
                name: "CuentaPorPagarId",
                table: "Proveedor");

            migrationBuilder.DropColumn(
                name: "CuentaPorCobrarId",
                table: "Cliente");
        }
    }
}
