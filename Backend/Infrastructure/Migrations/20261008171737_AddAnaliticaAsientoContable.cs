using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddAnaliticaAsientoContable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CentroCosto1Id",
                table: "AsientoContableDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CentroCosto2Id",
                table: "AsientoContableDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuentaAsociada",
                table: "AsientoContableDetalle",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CuentaDestinoId",
                table: "AsientoContableDetalle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "AsientoContableDetalle",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDocumento",
                table: "AsientoContable",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "AsientoContable",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AsientoContableDetalle_CentroCosto1Id",
                table: "AsientoContableDetalle",
                column: "CentroCosto1Id");

            migrationBuilder.CreateIndex(
                name: "IX_AsientoContableDetalle_CentroCosto2Id",
                table: "AsientoContableDetalle",
                column: "CentroCosto2Id");

            migrationBuilder.CreateIndex(
                name: "IX_AsientoContableDetalle_CuentaDestinoId",
                table: "AsientoContableDetalle",
                column: "CuentaDestinoId");

            migrationBuilder.AddForeignKey(
                name: "FK_AsientoContableDetalle_CentroCosto_CentroCosto1Id",
                table: "AsientoContableDetalle",
                column: "CentroCosto1Id",
                principalTable: "CentroCosto",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AsientoContableDetalle_CentroCosto_CentroCosto2Id",
                table: "AsientoContableDetalle",
                column: "CentroCosto2Id",
                principalTable: "CentroCosto",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AsientoContableDetalle_CuentaContable_CuentaDestinoId",
                table: "AsientoContableDetalle",
                column: "CuentaDestinoId",
                principalTable: "CuentaContable",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AsientoContableDetalle_CentroCosto_CentroCosto1Id",
                table: "AsientoContableDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_AsientoContableDetalle_CentroCosto_CentroCosto2Id",
                table: "AsientoContableDetalle");

            migrationBuilder.DropForeignKey(
                name: "FK_AsientoContableDetalle_CuentaContable_CuentaDestinoId",
                table: "AsientoContableDetalle");

            migrationBuilder.DropIndex(
                name: "IX_AsientoContableDetalle_CentroCosto1Id",
                table: "AsientoContableDetalle");

            migrationBuilder.DropIndex(
                name: "IX_AsientoContableDetalle_CentroCosto2Id",
                table: "AsientoContableDetalle");

            migrationBuilder.DropIndex(
                name: "IX_AsientoContableDetalle_CuentaDestinoId",
                table: "AsientoContableDetalle");

            migrationBuilder.DropColumn(
                name: "CentroCosto1Id",
                table: "AsientoContableDetalle");

            migrationBuilder.DropColumn(
                name: "CentroCosto2Id",
                table: "AsientoContableDetalle");

            migrationBuilder.DropColumn(
                name: "CuentaAsociada",
                table: "AsientoContableDetalle");

            migrationBuilder.DropColumn(
                name: "CuentaDestinoId",
                table: "AsientoContableDetalle");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "AsientoContableDetalle");

            migrationBuilder.DropColumn(
                name: "FechaDocumento",
                table: "AsientoContable");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "AsientoContable");
        }
    }
}
