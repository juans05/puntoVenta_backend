using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddDetalleSucursalYCorrelativoInicial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoEstablecimiento",
                table: "Sucursal",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Correo",
                table: "Sucursal",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Sucursal",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Urbanizacion",
                table: "Sucursal",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoEstablecimiento",
                table: "Sucursal");

            migrationBuilder.DropColumn(
                name: "Correo",
                table: "Sucursal");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Sucursal");

            migrationBuilder.DropColumn(
                name: "Urbanizacion",
                table: "Sucursal");
        }
    }
}
