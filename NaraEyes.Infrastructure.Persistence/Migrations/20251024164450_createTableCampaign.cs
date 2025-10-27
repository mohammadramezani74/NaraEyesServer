using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaraEyes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class createTableCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Campaign",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ManifestJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OutBoxDeviceMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByBrowserName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModifiedByBrowserName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModifiedByIp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaign", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campaign_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Campaign_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Campaign_OutBoxDeviceMessages_OutBoxDeviceMessageId",
                        column: x => x.OutBoxDeviceMessageId,
                        principalTable: "OutBoxDeviceMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignTarget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceIp = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: false),
                    IsProccessed = table.Column<bool>(type: "bit", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByBrowserName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModifiedByBrowserName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModifiedByIp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignTarget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignTarget_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignTarget_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignTarget_Campaign_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaign",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campaign_CreateDate",
                table: "Campaign",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_Campaign_CreatedByUserId_ModifiedById_Deleted",
                table: "Campaign",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaign_Deleted",
                table: "Campaign",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Campaign_ModifiedById",
                table: "Campaign",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Campaign_OutBoxDeviceMessageId",
                table: "Campaign",
                column: "OutBoxDeviceMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTarget_CampaignId",
                table: "CampaignTarget",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTarget_CreateDate",
                table: "CampaignTarget",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTarget_CreatedByUserId_ModifiedById_Deleted",
                table: "CampaignTarget",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTarget_Deleted",
                table: "CampaignTarget",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTarget_ModifiedById",
                table: "CampaignTarget",
                column: "ModifiedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignTarget");

            migrationBuilder.DropTable(
                name: "Campaign");
        }
    }
}
