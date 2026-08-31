using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCostoRealToComprobanteDetalle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoReal",
                table: "ComprobanteDetalle",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Prioridad",
                table: "AspNetRoles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 100);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoReal",
                table: "ComprobanteDetalle");

            migrationBuilder.AlterColumn<int>(
                name: "Prioridad",
                table: "AspNetRoles",
                type: "integer",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
