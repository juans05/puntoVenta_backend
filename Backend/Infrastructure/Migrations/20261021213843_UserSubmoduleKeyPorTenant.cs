using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class UserSubmoduleKeyPorTenant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserSubModule",
                table: "AspNetUserSubModule");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "AspNetUserSubModule",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserSubModule",
                table: "AspNetUserSubModule",
                columns: new[] { "UserId", "SubmoduleId", "TenantId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserSubModule",
                table: "AspNetUserSubModule");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "AspNetUserSubModule",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserSubModule",
                table: "AspNetUserSubModule",
                columns: new[] { "UserId", "SubmoduleId" });
        }
    }
}
