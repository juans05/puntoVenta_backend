using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCuentasPorCobrarYPagar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "Compra",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cobranza",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    Numero = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MetodoPagoId = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    Referencia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Observacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EstadoCobranza = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cobranza", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cobranza_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cobranza_Metodopago_MetodoPagoId",
                        column: x => x.MetodoPagoId,
                        principalTable: "Metodopago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PagoProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    Numero = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ProveedorId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MetodoPagoId = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    Referencia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Observacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EstadoPago = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoProveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagoProveedor_Metodopago_MetodoPagoId",
                        column: x => x.MetodoPagoId,
                        principalTable: "Metodopago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagoProveedor_Proveedor_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CobranzaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    CobranzaId = table.Column<int>(type: "integer", nullable: false),
                    ComprobanteCabeceraId = table.Column<int>(type: "integer", nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CobranzaDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CobranzaDetalle_Cobranza_CobranzaId",
                        column: x => x.CobranzaId,
                        principalTable: "Cobranza",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CobranzaDetalle_ComprobanteCabecera_ComprobanteCabeceraId",
                        column: x => x.ComprobanteCabeceraId,
                        principalTable: "ComprobanteCabecera",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PagoProveedorDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    PagoProveedorId = table.Column<int>(type: "integer", nullable: false),
                    CompraId = table.Column<int>(type: "integer", nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoProveedorDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagoProveedorDetalle_Compra_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagoProveedorDetalle_PagoProveedor_PagoProveedorId",
                        column: x => x.PagoProveedorId,
                        principalTable: "PagoProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cobranza_ClienteId",
                table: "Cobranza",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Cobranza_MetodoPagoId",
                table: "Cobranza",
                column: "MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_CobranzaDetalle_CobranzaId",
                table: "CobranzaDetalle",
                column: "CobranzaId");

            migrationBuilder.CreateIndex(
                name: "IX_CobranzaDetalle_ComprobanteCabeceraId",
                table: "CobranzaDetalle",
                column: "ComprobanteCabeceraId");

            migrationBuilder.CreateIndex(
                name: "IX_PagoProveedor_MetodoPagoId",
                table: "PagoProveedor",
                column: "MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_PagoProveedor_ProveedorId",
                table: "PagoProveedor",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PagoProveedorDetalle_CompraId",
                table: "PagoProveedorDetalle",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_PagoProveedorDetalle_PagoProveedorId",
                table: "PagoProveedorDetalle",
                column: "PagoProveedorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CobranzaDetalle");

            migrationBuilder.DropTable(
                name: "PagoProveedorDetalle");

            migrationBuilder.DropTable(
                name: "Cobranza");

            migrationBuilder.DropTable(
                name: "PagoProveedor");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Compra");
        }
    }
}
