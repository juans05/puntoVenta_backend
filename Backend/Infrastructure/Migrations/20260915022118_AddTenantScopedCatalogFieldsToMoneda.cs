using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddTenantScopedCatalogFieldsToMoneda : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Moneda_Codigo",
                table: "Moneda");

            // Los 4 registros existentes (PEN/MXN/COP/USD) ya estan en uso por Sucursal.MonedaId --
            // deben nacer Estado=true (default false rompería el catalogo actual) y con una
            // FechaCreacion real en vez del DateTime.MinValue que generaba el scaffold.
            migrationBuilder.AddColumn<bool>(
                name: "Estado",
                table: "Moneda",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Moneda",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Moneda",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCreacion",
                table: "Moneda",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Moneda_TenantId_Codigo",
                table: "Moneda",
                columns: new[] { "TenantId", "Codigo" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Moneda_TenantId_Codigo",
                table: "Moneda");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Moneda");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Moneda");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Moneda");

            migrationBuilder.DropColumn(
                name: "UsuarioCreacion",
                table: "Moneda");

            migrationBuilder.CreateIndex(
                name: "IX_Moneda_Codigo",
                table: "Moneda",
                column: "Codigo",
                unique: true);
        }
    }
}
