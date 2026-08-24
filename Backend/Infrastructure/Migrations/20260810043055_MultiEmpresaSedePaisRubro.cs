using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class MultiEmpresaSedePaisRubro : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonedaId",
                table: "Tenant",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaisId",
                table: "Tenant",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Seriecorrelativo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Retiros",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Producto",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Pago",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Grupo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "CorrelativoAnulacion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "ComprobanteDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "ComprobanteCabecera",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Comentario",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Cliente",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Categoria",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Caja",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Idioma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MonedaCodigo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EsquemaFiscal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RubroModulo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RubroId = table.Column<int>(type: "integer", nullable: false),
                    CodigoModulo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubroModulo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubroModulo_Rubro_RubroId",
                        column: x => x.RubroId,
                        principalTable: "Rubro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Impuesto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Porcentaje = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AplicableA = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaisId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Impuesto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Impuesto_Pais_PaisId",
                        column: x => x.PaisId,
                        principalTable: "Pais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Moneda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Simbolo = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Locale = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaisId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moneda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Moneda_Pais_PaisId",
                        column: x => x.PaisId,
                        principalTable: "Pais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sucursal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UbigeoId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Latitud = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    Longitud = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    MonedaId = table.Column<int>(type: "integer", nullable: false),
                    PaisId = table.Column<int>(type: "integer", nullable: false),
                    RubroId = table.Column<int>(type: "integer", nullable: false),
                    TenantIdentificador = table.Column<int>(type: "integer", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sucursal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sucursal_Moneda_MonedaId",
                        column: x => x.MonedaId,
                        principalTable: "Moneda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sucursal_Pais_PaisId",
                        column: x => x.PaisId,
                        principalTable: "Pais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sucursal_Rubro_RubroId",
                        column: x => x.RubroId,
                        principalTable: "Rubro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sucursal_Tenant_TenantIdentificador",
                        column: x => x.TenantIdentificador,
                        principalTable: "Tenant",
                        principalColumn: "Identificador");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_MonedaId",
                table: "Tenant",
                column: "MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_PaisId",
                table: "Tenant",
                column: "PaisId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SucursalId",
                table: "AspNetUsers",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_Impuesto_PaisId",
                table: "Impuesto",
                column: "PaisId");

            migrationBuilder.CreateIndex(
                name: "IX_Moneda_Codigo",
                table: "Moneda",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Moneda_PaisId",
                table: "Moneda",
                column: "PaisId");

            migrationBuilder.CreateIndex(
                name: "IX_Pais_Codigo",
                table: "Pais",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RubroModulo_RubroId_CodigoModulo",
                table: "RubroModulo",
                columns: new[] { "RubroId", "CodigoModulo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_MonedaId",
                table: "Sucursal",
                column: "MonedaId");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_PaisId",
                table: "Sucursal",
                column: "PaisId");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_RubroId",
                table: "Sucursal",
                column: "RubroId");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursal_TenantIdentificador",
                table: "Sucursal",
                column: "TenantIdentificador");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Sucursal_SucursalId",
                table: "AspNetUsers",
                column: "SucursalId",
                principalTable: "Sucursal",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenant_Moneda_MonedaId",
                table: "Tenant",
                column: "MonedaId",
                principalTable: "Moneda",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenant_Pais_PaisId",
                table: "Tenant",
                column: "PaisId",
                principalTable: "Pais",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Sucursal_SucursalId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenant_Moneda_MonedaId",
                table: "Tenant");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenant_Pais_PaisId",
                table: "Tenant");

            migrationBuilder.DropTable(
                name: "Impuesto");

            migrationBuilder.DropTable(
                name: "RubroModulo");

            migrationBuilder.DropTable(
                name: "Sucursal");

            migrationBuilder.DropTable(
                name: "Moneda");

            migrationBuilder.DropTable(
                name: "Pais");

            migrationBuilder.DropIndex(
                name: "IX_Tenant_MonedaId",
                table: "Tenant");

            migrationBuilder.DropIndex(
                name: "IX_Tenant_PaisId",
                table: "Tenant");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SucursalId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MonedaId",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "PaisId",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Seriecorrelativo");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Retiros");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Pago");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Grupo");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "CorrelativoAnulacion");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "ComprobanteDetalle");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "ComprobanteCabecera");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Comentario");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Cliente");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Categoria");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Caja");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "AspNetUsers");
        }
    }
}
