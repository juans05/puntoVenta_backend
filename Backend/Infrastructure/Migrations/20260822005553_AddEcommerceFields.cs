using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddEcommerceFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Distrito",
                table: "ComprobanteCabecera",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsEcommerce",
                table: "ComprobanteCabecera",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoEnvio",
                table: "ComprobanteCabecera",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Distrito",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "EsEcommerce",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "TipoEnvio",
                table: "ComprobanteCabecera");
        }
    }
}
