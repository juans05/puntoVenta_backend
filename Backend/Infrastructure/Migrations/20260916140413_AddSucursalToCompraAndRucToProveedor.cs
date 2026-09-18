using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddSucursalToCompraAndRucToProveedor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ruc",
                table: "Proveedor",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compra_SucursalId",
                table: "Compra",
                column: "SucursalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compra_Sucursal_SucursalId",
                table: "Compra",
                column: "SucursalId",
                principalTable: "Sucursal",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compra_Sucursal_SucursalId",
                table: "Compra");

            migrationBuilder.DropIndex(
                name: "IX_Compra_SucursalId",
                table: "Compra");

            migrationBuilder.DropColumn(
                name: "Ruc",
                table: "Proveedor");
        }
    }
}
