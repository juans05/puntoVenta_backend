using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddGuiaRemision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuiaRemision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    Numero = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EntregaId = table.Column<int>(type: "integer", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaTraslado = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ClienteNombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ClienteDocumento = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Motivo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ModTraslado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PesoTotal = table.Column<decimal>(type: "numeric(13,2)", nullable: true),
                    UndPesoTotal = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UbigeoPartida = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DireccionPartida = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UbigeoLlegada = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DireccionLlegada = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TransportistaRuc = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TransportistaRazonSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TransportistaMtc = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ChoferNombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ChoferDocumento = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Placa = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EstadoGuia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuiaRemision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuiaRemision_Entrega_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "Entrega",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuiaRemisionDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    GuiaRemisionId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Unidad = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuiaRemisionDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuiaRemisionDetalle_GuiaRemision_GuiaRemisionId",
                        column: x => x.GuiaRemisionId,
                        principalTable: "GuiaRemision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuiaRemisionDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuiaRemision_EntregaId",
                table: "GuiaRemision",
                column: "EntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiaRemisionDetalle_GuiaRemisionId",
                table: "GuiaRemisionDetalle",
                column: "GuiaRemisionId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiaRemisionDetalle_ProductoId",
                table: "GuiaRemisionDetalle",
                column: "ProductoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuiaRemisionDetalle");

            migrationBuilder.DropTable(
                name: "GuiaRemision");
        }
    }
}
