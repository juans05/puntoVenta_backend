using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCredencialesSunat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaveSol",
                table: "ConfiguracionFiscal",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SunatClientId",
                table: "ConfiguracionFiscal",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SunatClientSecret",
                table: "ConfiguracionFiscal",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioSol",
                table: "ConfiguracionFiscal",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaveSol",
                table: "ConfiguracionFiscal");

            migrationBuilder.DropColumn(
                name: "SunatClientId",
                table: "ConfiguracionFiscal");

            migrationBuilder.DropColumn(
                name: "SunatClientSecret",
                table: "ConfiguracionFiscal");

            migrationBuilder.DropColumn(
                name: "UsuarioSol",
                table: "ConfiguracionFiscal");
        }
    }
}
