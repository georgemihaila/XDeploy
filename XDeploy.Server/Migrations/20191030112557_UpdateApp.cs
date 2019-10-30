using Microsoft.EntityFrameworkCore.Migrations;

namespace XDeploy.Server.Migrations
{
    public partial class UpdateApp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeployerIP",
                table: "Applications",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Encrypted",
                table: "Applications",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EncryptionAlgorithm",
                table: "Applications",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IPRestrictedDeployer",
                table: "Applications",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeployerIP",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "Encrypted",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "EncryptionAlgorithm",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "IPRestrictedDeployer",
                table: "Applications");
        }
    }
}
