using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class RentaGenerico : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anfitriona",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombres = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Apellidos = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    NacionalidadId = table.Column<int>(type: "integer", nullable: true),
                    Direccion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Celular = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Foto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anfitriona", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anfitriona_Nacionalidad_NacionalidadId",
                        column: x => x.NacionalidadId,
                        principalTable: "Nacionalidad",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionRenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TurnosJson = table.Column<string>(type: "text", maxLength: 150, nullable: true),
                    TarifasJson = table.Column<string>(type: "text", maxLength: 150, nullable: true),
                    RecursosJson = table.Column<string>(type: "text", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionRenta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recurso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Zona = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recurso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Renta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    RecursoId = table.Column<int>(type: "integer", nullable: false),
                    AnfitrionaId = table.Column<int>(type: "integer", nullable: false),
                    Turno = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaIngreso = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaSalida = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TarifaCuarto = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    MontoCuarto = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    MontoPendiente = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Renta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Renta_Anfitriona_AnfitrionaId",
                        column: x => x.AnfitrionaId,
                        principalTable: "Anfitriona",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Renta_Recurso_RecursoId",
                        column: x => x.RecursoId,
                        principalTable: "Recurso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RentaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SucursalId = table.Column<int>(type: "integer", nullable: true),
                    RentaId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: true),
                    NombreProducto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RutaImagen = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Precio = table.Column<decimal>(type: "numeric(13,2)", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentaDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentaDetalle_Renta_RentaId",
                        column: x => x.RentaId,
                        principalTable: "Renta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anfitriona_NacionalidadId",
                table: "Anfitriona",
                column: "NacionalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Renta_AnfitrionaId",
                table: "Renta",
                column: "AnfitrionaId");

            migrationBuilder.CreateIndex(
                name: "IX_Renta_RecursoId",
                table: "Renta",
                column: "RecursoId");

            migrationBuilder.CreateIndex(
                name: "IX_RentaDetalle_RentaId",
                table: "RentaDetalle",
                column: "RentaId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionRenta");

            migrationBuilder.DropTable(
                name: "RentaDetalle");

            migrationBuilder.DropTable(
                name: "Renta");

            migrationBuilder.DropTable(
                name: "Anfitriona");

            migrationBuilder.DropTable(
                name: "Recurso");
        }
    }
}
