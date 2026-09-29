using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class FixCuentaContableSelectores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CuentaAbono1",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaAbono2",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaAbono3",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCargo1",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCargo2",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCargo3",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "RequiereCentroCosto",
                table: "CuentaContable");

            migrationBuilder.RenameColumn(
                name: "CuentaCierre",
                table: "CuentaContable",
                newName: "CodigoEeffNiif");

            migrationBuilder.AddColumn<int>(
                name: "CentroCostoId",
                table: "CuentaContable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaAbono1Id",
                table: "CuentaContable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaAbono2Id",
                table: "CuentaContable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaAbono3Id",
                table: "CuentaContable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaCargo1Id",
                table: "CuentaContable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaCargo2Id",
                table: "CuentaContable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaCargo3Id",
                table: "CuentaContable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaCierreId",
                table: "CuentaContable",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentaContable_CentroCostoId",
                table: "CuentaContable",
                column: "CentroCostoId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentaContable_CuentaAbono1Id",
                table: "CuentaContable",
                column: "CuentaAbono1Id");

            migrationBuilder.CreateIndex(
                name: "IX_CuentaContable_CuentaAbono2Id",
                table: "CuentaContable",
                column: "CuentaAbono2Id");

            migrationBuilder.CreateIndex(
                name: "IX_CuentaContable_CuentaAbono3Id",
                table: "CuentaContable",
                column: "CuentaAbono3Id");

            migrationBuilder.CreateIndex(
                name: "IX_CuentaContable_CuentaCargo1Id",
                table: "CuentaContable",
                column: "CuentaCargo1Id");

            migrationBuilder.CreateIndex(
                name: "IX_CuentaContable_CuentaCargo2Id",
                table: "CuentaContable",
                column: "CuentaCargo2Id");

            migrationBuilder.CreateIndex(
                name: "IX_CuentaContable_CuentaCargo3Id",
                table: "CuentaContable",
                column: "CuentaCargo3Id");

            migrationBuilder.CreateIndex(
                name: "IX_CuentaContable_CuentaCierreId",
                table: "CuentaContable",
                column: "CuentaCierreId");

            migrationBuilder.AddForeignKey(
                name: "FK_CuentaContable_CentroCosto_CentroCostoId",
                table: "CuentaContable",
                column: "CentroCostoId",
                principalTable: "CentroCosto",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaAbono1Id",
                table: "CuentaContable",
                column: "CuentaAbono1Id",
                principalTable: "CuentaContable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaAbono2Id",
                table: "CuentaContable",
                column: "CuentaAbono2Id",
                principalTable: "CuentaContable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaAbono3Id",
                table: "CuentaContable",
                column: "CuentaAbono3Id",
                principalTable: "CuentaContable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaCargo1Id",
                table: "CuentaContable",
                column: "CuentaCargo1Id",
                principalTable: "CuentaContable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaCargo2Id",
                table: "CuentaContable",
                column: "CuentaCargo2Id",
                principalTable: "CuentaContable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaCargo3Id",
                table: "CuentaContable",
                column: "CuentaCargo3Id",
                principalTable: "CuentaContable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaCierreId",
                table: "CuentaContable",
                column: "CuentaCierreId",
                principalTable: "CuentaContable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuentaContable_CentroCosto_CentroCostoId",
                table: "CuentaContable");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaAbono1Id",
                table: "CuentaContable");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaAbono2Id",
                table: "CuentaContable");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaAbono3Id",
                table: "CuentaContable");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaCargo1Id",
                table: "CuentaContable");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaCargo2Id",
                table: "CuentaContable");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaCargo3Id",
                table: "CuentaContable");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentaContable_CuentaContable_CuentaCierreId",
                table: "CuentaContable");

            migrationBuilder.DropIndex(
                name: "IX_CuentaContable_CentroCostoId",
                table: "CuentaContable");

            migrationBuilder.DropIndex(
                name: "IX_CuentaContable_CuentaAbono1Id",
                table: "CuentaContable");

            migrationBuilder.DropIndex(
                name: "IX_CuentaContable_CuentaAbono2Id",
                table: "CuentaContable");

            migrationBuilder.DropIndex(
                name: "IX_CuentaContable_CuentaAbono3Id",
                table: "CuentaContable");

            migrationBuilder.DropIndex(
                name: "IX_CuentaContable_CuentaCargo1Id",
                table: "CuentaContable");

            migrationBuilder.DropIndex(
                name: "IX_CuentaContable_CuentaCargo2Id",
                table: "CuentaContable");

            migrationBuilder.DropIndex(
                name: "IX_CuentaContable_CuentaCargo3Id",
                table: "CuentaContable");

            migrationBuilder.DropIndex(
                name: "IX_CuentaContable_CuentaCierreId",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CentroCostoId",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaAbono1Id",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaAbono2Id",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaAbono3Id",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCargo1Id",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCargo2Id",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCargo3Id",
                table: "CuentaContable");

            migrationBuilder.DropColumn(
                name: "CuentaCierreId",
                table: "CuentaContable");

            migrationBuilder.RenameColumn(
                name: "CodigoEeffNiif",
                table: "CuentaContable",
                newName: "CuentaCierre");

            migrationBuilder.AddColumn<string>(
                name: "CuentaAbono1",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaAbono2",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaAbono3",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaCargo1",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaCargo2",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaCargo3",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereCentroCosto",
                table: "CuentaContable",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
