using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class MetodopagoCuentaContable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CuentaContableId",
                table: "Metodopago",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Metodopago_CuentaContableId",
                table: "Metodopago",
                column: "CuentaContableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Metodopago_CuentaContable_CuentaContableId",
                table: "Metodopago",
                column: "CuentaContableId",
                principalTable: "CuentaContable",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Metodopago_CuentaContable_CuentaContableId",
                table: "Metodopago");

            migrationBuilder.DropIndex(
                name: "IX_Metodopago_CuentaContableId",
                table: "Metodopago");

            migrationBuilder.DropColumn(
                name: "CuentaContableId",
                table: "Metodopago");
        }
    }
}
