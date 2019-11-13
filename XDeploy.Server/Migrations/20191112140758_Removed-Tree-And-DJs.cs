using Microsoft.EntityFrameworkCore.Migrations;

namespace XDeploy.Server.Migrations
{
    public partial class RemovedTreeAndDJs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpectedFile_DeploymentJobs_ParentJobID",
                table: "ExpectedFile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeploymentJobs",
                table: "DeploymentJobs");

            migrationBuilder.RenameTable(
                name: "DeploymentJobs",
                newName: "DeploymentJob");

            migrationBuilder.AddColumn<bool>(
                name: "Locked",
                table: "Applications",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeploymentJob",
                table: "DeploymentJob",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpectedFile_DeploymentJob_ParentJobID",
                table: "ExpectedFile",
                column: "ParentJobID",
                principalTable: "DeploymentJob",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpectedFile_DeploymentJob_ParentJobID",
                table: "ExpectedFile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeploymentJob",
                table: "DeploymentJob");

            migrationBuilder.DropColumn(
                name: "Locked",
                table: "Applications");

            migrationBuilder.RenameTable(
                name: "DeploymentJob",
                newName: "DeploymentJobs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeploymentJobs",
                table: "DeploymentJobs",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpectedFile_DeploymentJobs_ParentJobID",
                table: "ExpectedFile",
                column: "ParentJobID",
                principalTable: "DeploymentJobs",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
