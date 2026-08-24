using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddUbigeoToProveedor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UbigeoId",
                table: "Proveedor",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_UbigeoId",
                table: "Proveedor",
                column: "UbigeoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proveedor_Ubigeo_UbigeoId",
                table: "Proveedor",
                column: "UbigeoId",
                principalTable: "Ubigeo",
                principalColumn: "UbigeoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proveedor_Ubigeo_UbigeoId",
                table: "Proveedor");

            migrationBuilder.DropIndex(
                name: "IX_Proveedor_UbigeoId",
                table: "Proveedor");

            migrationBuilder.DropColumn(
                name: "UbigeoId",
                table: "Proveedor");
        }
    }
}
