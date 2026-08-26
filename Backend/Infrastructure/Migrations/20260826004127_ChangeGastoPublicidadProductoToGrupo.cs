using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class ChangeGastoPublicidadProductoToGrupo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GastoPublicidad_Producto_ProductoId",
                table: "GastoPublicidad");

            migrationBuilder.RenameColumn(
                name: "ProductoId",
                table: "GastoPublicidad",
                newName: "GrupoId");

            migrationBuilder.RenameIndex(
                name: "IX_GastoPublicidad_ProductoId",
                table: "GastoPublicidad",
                newName: "IX_GastoPublicidad_GrupoId");

            migrationBuilder.AddForeignKey(
                name: "FK_GastoPublicidad_Grupo_GrupoId",
                table: "GastoPublicidad",
                column: "GrupoId",
                principalTable: "Grupo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GastoPublicidad_Grupo_GrupoId",
                table: "GastoPublicidad");

            migrationBuilder.RenameColumn(
                name: "GrupoId",
                table: "GastoPublicidad",
                newName: "ProductoId");

            migrationBuilder.RenameIndex(
                name: "IX_GastoPublicidad_GrupoId",
                table: "GastoPublicidad",
                newName: "IX_GastoPublicidad_ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_GastoPublicidad_Producto_ProductoId",
                table: "GastoPublicidad",
                column: "ProductoId",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
