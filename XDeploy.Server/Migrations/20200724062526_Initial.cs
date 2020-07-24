using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace XDeploy.Server.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APIKeys",
                columns: table => new
                {
                    KeyHash = table.Column<string>(nullable: false),
                    FirstChars = table.Column<string>(nullable: true),
                    UserEmail = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APIKeys", x => x.KeyHash);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    ID = table.Column<string>(nullable: false),
                    OwnerEmail = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    LastUpdate = table.Column<DateTime>(nullable: false),
                    IPRestrictedDeployer = table.Column<bool>(nullable: false),
                    DeployerIP = table.Column<string>(nullable: true),
                    Encrypted = table.Column<bool>(nullable: false),
                    EncryptionAlgorithm = table.Column<int>(nullable: false),
                    PredeployActions = table.Column<string>(nullable: true),
                    PostdeployActions = table.Column<string>(nullable: true),
                    Locked = table.Column<bool>(nullable: false),
                    Size_Bytes = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentJob",
                columns: table => new
                {
                    ID = table.Column<string>(nullable: false),
                    ApplicationID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentJob", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ExpectedFile",
                columns: table => new
                {
                    ID = table.Column<string>(nullable: false),
                    Filename = table.Column<string>(nullable: true),
                    Checksum = table.Column<string>(nullable: true),
                    ParentJobID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpectedFile", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ExpectedFile_DeploymentJob_ParentJobID",
                        column: x => x.ParentJobID,
                        principalTable: "DeploymentJob",
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
                name: "APIKeys");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "ExpectedFile");

            migrationBuilder.DropTable(
                name: "DeploymentJob");
        }
    }
}
