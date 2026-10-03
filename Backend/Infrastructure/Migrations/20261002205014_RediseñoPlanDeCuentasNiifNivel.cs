using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class RediseñoPlanDeCuentasNiifNivel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuentaContable_CentroCosto_CentroCostoId",
                table: "CuentaContable");

            migrationBuilder.RenameColumn(
                name: "CodigoEeffNiif",
                table: "CuentaContable",
                newName: "TipoAnexoClase");

            migrationBuilder.RenameColumn(
                name: "CentroCostoId",
                table: "CuentaContable",
                newName: "CodigoEeffNiifId");

            migrationBuilder.RenameIndex(
                name: "IX_CuentaContable_CentroCostoId",
                table: "CuentaContable",
                newName: "IX_CuentaContable_CodigoEeffNiifId");

            migrationBuilder.AlterColumn<int>(
                name: "Nivel",
                table: "CuentaContable",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModoCentroCosto",
                table: "CuentaContable",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CodigoEeffNiif",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Categoria = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigoEeffNiif", x => x.Id);
                });

            // Catalogo NIIF (Estado de Situacion Financiera + Estado de Resultados) de
            // "configuracion plan de cuentas ERPdocx.docx": global, no editable por tenant.
            migrationBuilder.InsertData(
                table: "CodigoEeffNiif",
                columns: new[] { "Id", "Codigo", "Nombre", "Categoria", "Estado", "FechaCreacion" },
                values: new object[,]
                {
                    { 1, "ACT-COR-01", "Efectivo y Equivalentes de Efectivo", "Activo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "ACT-COR-02", "Inversiones Financieras (Corto Plazo)", "Activo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "ACT-COR-03", "Cuentas por Cobrar Comerciales (Corto Plazo)", "Activo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "ACT-COR-04", "Cuentas por Cobrar a Partes Relacionadas (Corto Plazo)", "Activo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "ACT-COR-05", "Otras Cuentas por Cobrar (Corto Plazo)", "Activo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "ACT-COR-06", "Existencias (Inventarios)", "Activo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "ACT-COR-07", "Activos por Impuestos Corrientes", "Activo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "ACT-COR-08", "Otros Activos Corrientes", "Activo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "ACT-NCO-01", "Cuentas por Cobrar a Largo Plazo", "Activo No Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "ACT-NCO-02", "Inversiones Financieras (Largo Plazo)", "Activo No Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "ACT-NCO-03", "Propiedades de Inversión", "Activo No Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, "ACT-NCO-04", "Propiedades, Planta y Equipo", "Activo No Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, "ACT-NCO-05", "Activos Intangibles", "Activo No Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, "ACT-NCO-06", "Activos por Impuesto Diferido", "Activo No Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, "PAS-COR-01", "Sobregiros y Obligaciones Financieras (Corto Plazo)", "Pasivo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, "PAS-COR-02", "Cuentas por Pagar Comerciales (Corto Plazo)", "Pasivo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, "PAS-COR-03", "Cuentas por Pagar a Partes Relacionadas (Corto Plazo)", "Pasivo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, "PAS-COR-04", "Tributos, Contraprestaciones y Aportes por Pagar", "Pasivo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, "PAS-COR-05", "Remuneraciones y Participaciones por Pagar", "Pasivo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, "PAS-COR-06", "Otras Cuentas por Pagar (Corto Plazo)", "Pasivo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 21, "PAS-COR-07", "Provisiones (Corto Plazo)", "Pasivo Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 22, "PAS-NCO-01", "Obligaciones Financieras (Largo Plazo)", "Pasivo No Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 23, "PAS-NCO-02", "Cuentas por Pagar a Largo Plazo", "Pasivo No Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, "PAS-NCO-03", "Pasivos por Impuesto Diferido", "Pasivo No Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 25, "PAS-NCO-04", "Provisiones a Largo Plazo", "Pasivo No Corriente", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 26, "PAT-NET-01", "Capital Social", "Patrimonio Neto", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 27, "PAT-NET-02", "Acciones de Inversión y Capital Adicional", "Patrimonio Neto", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 28, "PAT-NET-03", "Reservas Legales y Estatutarias", "Patrimonio Neto", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 29, "PAT-NET-04", "Resultados Acumulados", "Patrimonio Neto", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 30, "PAT-NET-05", "Resultado del Ejercicio", "Patrimonio Neto", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 31, "OP-ING-01", "Ingresos de Actividades Ordinarias", "Estado de Resultados - Explotación", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 32, "OP-COS-01", "Costo de Ventas / Costo de Producción", "Estado de Resultados - Explotación", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 33, "OP-GAS-ADM", "Gastos de Administración", "Estado de Resultados - Explotación", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 34, "OP-GAS-VEN", "Gastos de Distribución y Comercialización", "Estado de Resultados - Explotación", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 35, "OP-OTR-01", "Otros Ingresos y Gastos Operativos", "Estado de Resultados - Explotación", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 36, "INV-REN-01", "Rendimientos de Inversiones y Asociadas", "Estado de Resultados - Inversión", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 37, "INV-GAS-01", "Gastos por Deterioro o Pérdida de Inversiones", "Estado de Resultados - Inversión", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 38, "FIN-GAS-01", "Gastos Financieros", "Estado de Resultados - Financiación", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 39, "FIN-ING-01", "Ingresos Financieros", "Estado de Resultados - Financiación", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 40, "IMP-REN-01", "Impuesto a las Ganancias", "Impuestos", true, new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc) },
                });

            migrationBuilder.AddForeignKey(
                name: "FK_CuentaContable_CodigoEeffNiif_CodigoEeffNiifId",
                table: "CuentaContable",
                column: "CodigoEeffNiifId",
                principalTable: "CodigoEeffNiif",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuentaContable_CodigoEeffNiif_CodigoEeffNiifId",
                table: "CuentaContable");

            migrationBuilder.DropTable(
                name: "CodigoEeffNiif");

            migrationBuilder.DropColumn(
                name: "ModoCentroCosto",
                table: "CuentaContable");

            migrationBuilder.RenameColumn(
                name: "TipoAnexoClase",
                table: "CuentaContable",
                newName: "CodigoEeffNiif");

            migrationBuilder.RenameColumn(
                name: "CodigoEeffNiifId",
                table: "CuentaContable",
                newName: "CentroCostoId");

            migrationBuilder.RenameIndex(
                name: "IX_CuentaContable_CodigoEeffNiifId",
                table: "CuentaContable",
                newName: "IX_CuentaContable_CentroCostoId");

            migrationBuilder.AlterColumn<int>(
                name: "Nivel",
                table: "CuentaContable",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_CuentaContable_CentroCosto_CentroCostoId",
                table: "CuentaContable",
                column: "CentroCostoId",
                principalTable: "CentroCosto",
                principalColumn: "Id");
        }
    }
}
