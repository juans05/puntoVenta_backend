using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddProveedorExtraFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Proveedor",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetalleAdicional",
                table: "Proveedor",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoDocumentoId",
                table: "Proveedor",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_TipoDocumentoId",
                table: "Proveedor",
                column: "TipoDocumentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proveedor_TipoDocumento_TipoDocumentoId",
                table: "Proveedor",
                column: "TipoDocumentoId",
                principalTable: "TipoDocumento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proveedor_TipoDocumento_TipoDocumentoId",
                table: "Proveedor");

            migrationBuilder.DropIndex(
                name: "IX_Proveedor_TipoDocumentoId",
                table: "Proveedor");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Proveedor");

            migrationBuilder.DropColumn(
                name: "DetalleAdicional",
                table: "Proveedor");

            migrationBuilder.DropColumn(
                name: "TipoDocumentoId",
                table: "Proveedor");
        }
    }
}
