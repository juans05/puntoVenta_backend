using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddTiendaConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TiendaConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Publicada = table.Column<bool>(type: "boolean", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ColorPrimario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ColorFondo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ColorTexto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Whatsapp = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiendaConfig", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TiendaConfig");
        }
    }
}
