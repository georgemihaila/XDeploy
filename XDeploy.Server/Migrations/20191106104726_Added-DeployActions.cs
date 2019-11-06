using Microsoft.EntityFrameworkCore.Migrations;

namespace XDeploy.Server.Migrations
{
    public partial class AddedDeployActions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostdeployActions",
                table: "Applications",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PredeployActions",
                table: "Applications",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostdeployActions",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "PredeployActions",
                table: "Applications");
        }
    }
}
