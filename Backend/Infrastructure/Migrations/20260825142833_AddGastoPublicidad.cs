using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddGastoPublicidad : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GastoPublicidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    NombreAnuncio = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    NombreConjuntoAnuncios = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ImporteGastado = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    Impresiones = table.Column<int>(type: "integer", nullable: true),
                    Alcance = table.Column<int>(type: "integer", nullable: true),
                    Resultados = table.Column<int>(type: "integer", nullable: true),
                    CostoPorResultado = table.Column<decimal>(type: "numeric(13,2)", nullable: true),
                    LoteImportacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    HashAnuncio = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GastoPublicidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GastoPublicidad_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GastoPublicidad_ProductoId",
                table: "GastoPublicidad",
                column: "ProductoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GastoPublicidad");
        }
    }
}
