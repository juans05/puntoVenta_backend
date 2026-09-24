using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddFlujoVentasCompleto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlujoVentas",
                table: "ConfiguracionFlujo",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PedidoVentaId",
                table: "ComprobanteCabecera",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StockYaDescontado",
                table: "ComprobanteCabecera",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PedidoVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    Numero = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ClienteId = table.Column<int>(type: "integer", nullable: true),
                    NumeroDocumento = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RazonSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DireccionCliente = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaEmision = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    EstadoPedidoVenta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Observacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CotizacionOrigenId = table.Column<int>(type: "integer", nullable: true),
                    MotivoCierre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoVenta_Sucursal_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursal",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Entrega",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    Numero = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PedidoVentaId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EstadoEntrega = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Placa = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Observacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entrega", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entrega_PedidoVenta_PedidoVentaId",
                        column: x => x.PedidoVentaId,
                        principalTable: "PedidoVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoVentaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    PedidoVentaId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    CantidadPedida = table.Column<int>(type: "integer", nullable: false),
                    CantidadEntregada = table.Column<int>(type: "integer", nullable: false),
                    CantidadFacturada = table.Column<int>(type: "integer", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    TipoIgvId = table.Column<int>(type: "integer", nullable: true),
                    UnidadMedidaId = table.Column<int>(type: "integer", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoVentaDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoVentaDetalle_PedidoVenta_PedidoVentaId",
                        column: x => x.PedidoVentaId,
                        principalTable: "PedidoVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoVentaDetalle_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntregaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    EntregaId = table.Column<int>(type: "integer", nullable: false),
                    PedidoVentaDetalleId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregaDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntregaDetalle_Entrega_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "Entrega",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntregaDetalle_PedidoVentaDetalle_PedidoVentaDetalleId",
                        column: x => x.PedidoVentaDetalleId,
                        principalTable: "PedidoVentaDetalle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entrega_PedidoVentaId",
                table: "Entrega",
                column: "PedidoVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaDetalle_EntregaId",
                table: "EntregaDetalle",
                column: "EntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_EntregaDetalle_PedidoVentaDetalleId",
                table: "EntregaDetalle",
                column: "PedidoVentaDetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoVenta_SucursalId",
                table: "PedidoVenta",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoVentaDetalle_PedidoVentaId",
                table: "PedidoVentaDetalle",
                column: "PedidoVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoVentaDetalle_ProductoId",
                table: "PedidoVentaDetalle",
                column: "ProductoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntregaDetalle");

            migrationBuilder.DropTable(
                name: "Entrega");

            migrationBuilder.DropTable(
                name: "PedidoVentaDetalle");

            migrationBuilder.DropTable(
                name: "PedidoVenta");

            migrationBuilder.DropColumn(
                name: "FlujoVentas",
                table: "ConfiguracionFlujo");

            migrationBuilder.DropColumn(
                name: "PedidoVentaId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "StockYaDescontado",
                table: "ComprobanteCabecera");
        }
    }
}
