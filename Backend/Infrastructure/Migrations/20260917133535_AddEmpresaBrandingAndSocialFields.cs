using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddEmpresaBrandingAndSocialFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Facebook",
                table: "Empresa",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instagram",
                table: "Empresa",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoCuadrado",
                table: "Empresa",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoRectangular",
                table: "Empresa",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegimenTributario",
                table: "Empresa",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tiktok",
                table: "Empresa",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Urbanizacion",
                table: "Empresa",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "X",
                table: "Empresa",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Youtube",
                table: "Empresa",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Facebook",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "Instagram",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "LogoCuadrado",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "LogoRectangular",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "RegimenTributario",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "Tiktok",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "Urbanizacion",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "X",
                table: "Empresa");

            migrationBuilder.DropColumn(
                name: "Youtube",
                table: "Empresa");
        }
    }
}
