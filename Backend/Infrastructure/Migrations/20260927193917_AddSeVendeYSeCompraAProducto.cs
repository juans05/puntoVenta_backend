using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddSeVendeYSeCompraAProducto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // default true: los productos existentes deben seguir apareciendo en Ventas y Compras
            // igual que antes de esta columna (comportamiento previo, sin el filtro).
            migrationBuilder.AddColumn<bool>(
                name: "SeCompra",
                table: "Producto",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "SeVende",
                table: "Producto",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeCompra",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "SeVende",
                table: "Producto");
        }
    }
}
