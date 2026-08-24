using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddGestionModulos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockMinimo",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CierreDiario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    Ingresos = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    Egresos = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    SaldoEsperado = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    SaldoReal = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    Diferencia = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CierreDiario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Compra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    NumeroCompra = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ProveedorId = table.Column<int>(type: "integer", nullable: true),
                    Total = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    MetodoPagoId = table.Column<int>(type: "integer", nullable: true),
                    Estado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCompra = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compra_Metodopago_MetodoPagoId",
                        column: x => x.MetodoPagoId,
                        principalTable: "Metodopago",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Compra_Proveedor_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedor",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gasto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    Categoria = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Monto = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    MetodoPagoId = table.Column<int>(type: "integer", nullable: true),
                    Observacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Estado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaGasto = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gasto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gasto_Metodopago_MetodoPagoId",
                        column: x => x.MetodoPagoId,
                        principalTable: "Metodopago",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Ingreso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Monto = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    MetodoPagoId = table.Column<int>(type: "integer", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Estado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaIngreso = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingreso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ingreso_Metodopago_MetodoPagoId",
                        column: x => x.MetodoPagoId,
                        principalTable: "Metodopago",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WhatsappConversation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Estado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ContextoJson = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UltimoMensaje = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsappConversation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WhatsappMessage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    NumeroOrigen = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Texto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Intencion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Estado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Respuesta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsappMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompraDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    CompraId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompraDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompraDetalle_Compra_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompraDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Compra_MetodoPagoId",
                table: "Compra",
                column: "MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_Compra_ProveedorId",
                table: "Compra",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalle_CompraId",
                table: "CompraDetalle",
                column: "CompraId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalle_ProductoId",
                table: "CompraDetalle",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Gasto_MetodoPagoId",
                table: "Gasto",
                column: "MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingreso_MetodoPagoId",
                table: "Ingreso",
                column: "MetodoPagoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CierreDiario");

            migrationBuilder.DropTable(
                name: "CompraDetalle");

            migrationBuilder.DropTable(
                name: "Gasto");

            migrationBuilder.DropTable(
                name: "Ingreso");

            migrationBuilder.DropTable(
                name: "WhatsappConversation");

            migrationBuilder.DropTable(
                name: "WhatsappMessage");

            migrationBuilder.DropTable(
                name: "Compra");

            migrationBuilder.DropColumn(
                name: "StockMinimo",
                table: "Producto");
        }
    }
}
