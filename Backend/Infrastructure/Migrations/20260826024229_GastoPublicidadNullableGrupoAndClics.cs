using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class GastoPublicidadNullableGrupoAndClics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GastoPublicidad_Grupo_GrupoId",
                table: "GastoPublicidad");

            migrationBuilder.AlterColumn<int>(
                name: "GrupoId",
                table: "GastoPublicidad",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Clics",
                table: "GastoPublicidad",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoPorClic",
                table: "GastoPublicidad",
                type: "numeric(13,2)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GastoPublicidad_Grupo_GrupoId",
                table: "GastoPublicidad",
                column: "GrupoId",
                principalTable: "Grupo",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GastoPublicidad_Grupo_GrupoId",
                table: "GastoPublicidad");

            migrationBuilder.DropColumn(
                name: "Clics",
                table: "GastoPublicidad");

            migrationBuilder.DropColumn(
                name: "CostoPorClic",
                table: "GastoPublicidad");

            migrationBuilder.AlterColumn<int>(
                name: "GrupoId",
                table: "GastoPublicidad",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GastoPublicidad_Grupo_GrupoId",
                table: "GastoPublicidad",
                column: "GrupoId",
                principalTable: "Grupo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
