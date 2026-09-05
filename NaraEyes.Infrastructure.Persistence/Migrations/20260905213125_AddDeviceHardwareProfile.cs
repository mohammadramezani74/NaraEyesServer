using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaraEyes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceHardwareProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceHardwareChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Component = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_DeviceHardwareChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceHardwareChanges_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceHardwareChanges_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceHardwareChanges_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceHardwareProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RamTotalMb = table.Column<int>(type: "int", nullable: false),
                    RamSignature = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RamModulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CpuName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CpuCores = table.Column<int>(type: "int", nullable: false),
                    CpuMaxClockMhz = table.Column<int>(type: "int", nullable: false),
                    CpuId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DiskModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DiskSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DiskSerial = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BoardManufacturer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BoardProduct = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BoardSerial = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BiosVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_DeviceHardwareProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceHardwareProfiles_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceHardwareProfiles_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceHardwareProfiles_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHardwareChanges_CreatedByUserId",
                table: "DeviceHardwareChanges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHardwareChanges_Device",
                table: "DeviceHardwareChanges",
                columns: new[] { "DeviceId", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHardwareChanges_ModifiedById",
                table: "DeviceHardwareChanges",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHardwareChanges_Window",
                table: "DeviceHardwareChanges",
                columns: new[] { "DetectedAt", "Component" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHardwareProfiles_CreatedByUserId",
                table: "DeviceHardwareProfiles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHardwareProfiles_Device",
                table: "DeviceHardwareProfiles",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHardwareProfiles_ModifiedById",
                table: "DeviceHardwareProfiles",
                column: "ModifiedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceHardwareChanges");

            migrationBuilder.DropTable(
                name: "DeviceHardwareProfiles");
        }
    }
}
