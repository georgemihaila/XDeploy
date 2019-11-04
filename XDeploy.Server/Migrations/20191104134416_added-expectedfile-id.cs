using Microsoft.EntityFrameworkCore.Migrations;

namespace XDeploy.Server.Migrations
{
    public partial class addedexpectedfileid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpectedFile",
                table: "ExpectedFile");

            migrationBuilder.AlterColumn<string>(
                name: "Checksum",
                table: "ExpectedFile",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "ID",
                table: "ExpectedFile",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpectedFile",
                table: "ExpectedFile",
                column: "ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpectedFile",
                table: "ExpectedFile");

            migrationBuilder.DropColumn(
                name: "ID",
                table: "ExpectedFile");

            migrationBuilder.AlterColumn<string>(
                name: "Checksum",
                table: "ExpectedFile",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpectedFile",
                table: "ExpectedFile",
                column: "Checksum");
        }
    }
}
