using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddSalonAndCurrierToPedido : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currier",
                table: "Pedido",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalonId",
                table: "Pedido",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Salon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UbigeoId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Salon_Ubigeo_UbigeoId",
                        column: x => x.UbigeoId,
                        principalTable: "Ubigeo",
                        principalColumn: "UbigeoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_SalonId",
                table: "Pedido",
                column: "SalonId");

            migrationBuilder.CreateIndex(
                name: "IX_Salon_UbigeoId",
                table: "Salon",
                column: "UbigeoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedido_Salon_SalonId",
                table: "Pedido",
                column: "SalonId",
                principalTable: "Salon",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Semilla inicial: un salón Shalom por capital de departamento (25). El admin
            // agrega el resto (agencias adicionales) desde la pantalla de gestión cuando se
            // implemente; no se carga el listado completo de ~475 agencias de Shalom.
            migrationBuilder.InsertData(
                table: "Salon",
                columns: new[] { "Nombre", "UbigeoId", "Activo" },
                values: new object[,]
                {
                    { "Shalom Chachapoyas", "010101", true },
                    { "Shalom Huaraz", "020101", true },
                    { "Shalom Abancay", "030101", true },
                    { "Shalom Arequipa", "040101", true },
                    { "Shalom Ayacucho", "050101", true },
                    { "Shalom Cajamarca", "060101", true },
                    { "Shalom Callao", "070101", true },
                    { "Shalom Cusco", "080101", true },
                    { "Shalom Huancavelica", "090101", true },
                    { "Shalom Huanuco", "100101", true },
                    { "Shalom Ica", "110101", true },
                    { "Shalom Huancayo", "120101", true },
                    { "Shalom Trujillo", "130101", true },
                    { "Shalom Chiclayo", "140101", true },
                    { "Shalom Lima", "150101", true },
                    { "Shalom Iquitos", "160101", true },
                    { "Shalom Puerto Maldonado", "170101", true },
                    { "Shalom Moquegua", "180101", true },
                    { "Shalom Cerro de Pasco", "190101", true },
                    { "Shalom Piura", "200101", true },
                    { "Shalom Puno", "210101", true },
                    { "Shalom Moyobamba", "220101", true },
                    { "Shalom Tacna", "230101", true },
                    { "Shalom Tumbes", "240101", true },
                    { "Shalom Pucallpa", "250101", true },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedido_Salon_SalonId",
                table: "Pedido");

            migrationBuilder.DropTable(
                name: "Salon");

            migrationBuilder.DropIndex(
                name: "IX_Pedido_SalonId",
                table: "Pedido");

            migrationBuilder.DropColumn(
                name: "Currier",
                table: "Pedido");

            migrationBuilder.DropColumn(
                name: "SalonId",
                table: "Pedido");
        }
    }
}
