using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaraEyes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleFaultLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModuleFaultLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<int>(type: "int", nullable: false),
                    StartStatus = table.Column<int>(type: "int", nullable: false),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false),
                    RawStatus = table.Column<int>(type: "int", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    TransitionCount = table.Column<int>(type: "int", nullable: false),
                    CreatedByBrowserName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModifiedByBrowserName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModifiedByIp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleFaultLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleFaultLogs_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ModuleFaultLogs_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ModuleFaultLogs_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleFaultLogs_CreatedByUserId",
                table: "ModuleFaultLogs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleFaultLogs_Device",
                table: "ModuleFaultLogs",
                columns: new[] { "DeviceId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleFaultLogs_ModifiedById",
                table: "ModuleFaultLogs",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleFaultLogs_Open",
                table: "ModuleFaultLogs",
                columns: new[] { "DeviceId", "Module", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleFaultLogs_Report",
                table: "ModuleFaultLogs",
                columns: new[] { "StartedAt", "Module" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleFaultLogs");
        }
    }
}
