using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaraEyes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addoutboxinbixtbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InBoxDeviceMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_InBoxDeviceMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InBoxDeviceMessages_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InBoxDeviceMessages_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutBoxDeviceMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommandType = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_OutBoxDeviceMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutBoxDeviceMessages_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutBoxDeviceMessages_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InBoxDeviceMessages_CreateDate",
                table: "InBoxDeviceMessages",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_InBoxDeviceMessages_CreatedByUserId_ModifiedById_Deleted",
                table: "InBoxDeviceMessages",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_InBoxDeviceMessages_Deleted",
                table: "InBoxDeviceMessages",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_InBoxDeviceMessages_DeviceIp_Processed",
                table: "InBoxDeviceMessages",
                columns: new[] { "DeviceIp", "Processed" });

            migrationBuilder.CreateIndex(
                name: "IX_InBoxDeviceMessages_ModifiedById",
                table: "InBoxDeviceMessages",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_OutBoxDeviceMessages_CreateDate",
                table: "OutBoxDeviceMessages",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_OutBoxDeviceMessages_CreatedByUserId_ModifiedById_Deleted",
                table: "OutBoxDeviceMessages",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_OutBoxDeviceMessages_Deleted",
                table: "OutBoxDeviceMessages",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_OutBoxDeviceMessages_DeviceIp_Processed",
                table: "OutBoxDeviceMessages",
                columns: new[] { "DeviceIp", "Processed" });

            migrationBuilder.CreateIndex(
                name: "IX_OutBoxDeviceMessages_ModifiedById",
                table: "OutBoxDeviceMessages",
                column: "ModifiedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InBoxDeviceMessages");

            migrationBuilder.DropTable(
                name: "OutBoxDeviceMessages");
        }
    }
}
