using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCuentaContableCamposEeff : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AjusteDifCambio",
                table: "CuentaContable",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ClaseCuenta",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClasificacionBienServicio",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoEeff",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoEeffTributario",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaAbono1",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaAbono2",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaAbono3",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaCargo1",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaCargo2",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaCargo3",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaCierre",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CuentaMonetaria",
                table: "CuentaContable",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Destino",
                table: "CuentaContable",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Nivel",
                table: "CuentaContable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDestino1",
                table: "CuentaContable",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDestino2",
                table: "CuentaContable",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDestino3",
                table: "CuentaContable",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereCentroCosto",
                table: "CuentaContable",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TipoAnexo",
                table: "CuentaContable",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AjusteDifCambio",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "ClaseCuenta",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "ClasificacionBienServicio",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CodigoEeff",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CodigoEeffTributario",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaAbono1",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaAbono2",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaAbono3",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCargo1",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCargo2",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCargo3",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCierre",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaMonetaria",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "Destino",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "Nivel",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "PorcentajeDestino1",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "PorcentajeDestino2",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "PorcentajeDestino3",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "RequiereCentroCosto",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "TipoAnexo",
                table: "CuentaContable");
        }
    }
}
