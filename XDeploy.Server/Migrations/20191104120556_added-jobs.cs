using Microsoft.EntityFrameworkCore.Migrations;

namespace XDeploy.Server.Migrations
{
    public partial class addedjobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeploymentJobs",
                columns: table => new
                {
                    ID = table.Column<string>(nullable: false),
                    ApplicationID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentJobs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ExpectedFile",
                columns: table => new
                {
                    Checksum = table.Column<string>(nullable: false),
                    Filename = table.Column<string>(nullable: true),
                    ParentJobID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpectedFile", x => x.Checksum);
                    table.ForeignKey(
                        name: "FK_ExpectedFile_DeploymentJobs_ParentJobID",
                        column: x => x.ParentJobID,
                        principalTable: "DeploymentJobs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpectedFile_ParentJobID",
                table: "ExpectedFile",
                column: "ParentJobID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpectedFile");

            migrationBuilder.DropTable(
                name: "DeploymentJobs");
        }
    }
}
