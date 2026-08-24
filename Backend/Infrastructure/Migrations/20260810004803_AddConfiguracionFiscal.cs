using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddConfiguracionFiscal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionFiscal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: true),
                    Pais = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Ruc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NombreComercial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UbigeoId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Departamento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Provincia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Distrito = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SerieFactura = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    SerieBoleta = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    SerieNota = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    CodigoAdaptador = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Token = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    PorcentajeImpuesto = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionFiscal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionFiscal_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionFiscal_EmpresaId",
                table: "ConfiguracionFiscal",
                column: "EmpresaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionFiscal");
        }
    }
}
