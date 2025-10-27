using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaraEyes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addDevicesContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Tel = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_ContactInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactInfos_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContactInfos_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Serial = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentCount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalCount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Denomination = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_CashUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashUnits_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashUnits_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Module = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Acknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_DeviceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceEvents_AspNetUsers_AcknowledgedById",
                        column: x => x.AcknowledgedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeviceEvents_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceEvents_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_DeviceModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceModules_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceModules_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceModuleStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DeviceModuleStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceModuleStatuses_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceModuleStatuses_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceModuleStatuses_DeviceModules_DeviceModuleId",
                        column: x => x.DeviceModuleId,
                        principalTable: "DeviceModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceModuleStatusSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DeviceModuleStatusSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceModuleStatusSnapshots_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceModuleStatusSnapshots_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceModuleStatusSnapshots_DeviceModules_DeviceModuleId",
                        column: x => x.DeviceModuleId,
                        principalTable: "DeviceModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceSupplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LevelPercent = table.Column<int>(type: "int", nullable: true),
                    Count = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_DeviceSupplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceSupplies_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceSupplies_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceSupplies_DeviceModules_DeviceModuleId",
                        column: x => x.DeviceModuleId,
                        principalTable: "DeviceModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: true),
                    Ip = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstallationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MobileNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CurrentMetricsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devices_ContactInfos_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "ContactInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetricSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CpuUsage = table.Column<double>(type: "float", nullable: true),
                    RamUsage = table.Column<double>(type: "float", nullable: true),
                    DiskUsage = table.Column<double>(type: "float", nullable: true),
                    CpuTemp = table.Column<double>(type: "float", nullable: true),
                    NetworkLatencyMs = table.Column<int>(type: "int", nullable: true),
                    PingOk = table.Column<bool>(type: "bit", nullable: false),
                    AgentAlive = table.Column<bool>(type: "bit", nullable: false),
                    AgentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExtraJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_MetricSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricSnapshots_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetricSnapshots_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetricSnapshots_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashUnit_Device_Denomination",
                table: "CashUnits",
                columns: new[] { "DeviceId", "Denomination" });

            migrationBuilder.CreateIndex(
                name: "IX_CashUnits_CreateDate",
                table: "CashUnits",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_CashUnits_CreatedByUserId_ModifiedById_Deleted",
                table: "CashUnits",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CashUnits_Deleted",
                table: "CashUnits",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_CashUnits_ModifiedById",
                table: "CashUnits",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContactInfos_CreateDate",
                table: "ContactInfos",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContactInfos_CreatedByUserId_ModifiedById_Deleted",
                table: "ContactInfos",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactInfos_Deleted",
                table: "ContactInfos",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ContactInfos_ModifiedById",
                table: "ContactInfos",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvent_Device_Time",
                table: "DeviceEvents",
                columns: new[] { "DeviceId", "EventTime" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvent_Severity",
                table: "DeviceEvents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_AcknowledgedById",
                table: "DeviceEvents",
                column: "AcknowledgedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_CreateDate",
                table: "DeviceEvents",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_CreatedByUserId_ModifiedById_Deleted",
                table: "DeviceEvents",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_Deleted",
                table: "DeviceEvents",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_ModifiedById",
                table: "DeviceEvents",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModules_CreateDate",
                table: "DeviceModules",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModules_CreatedByUserId_ModifiedById_Deleted",
                table: "DeviceModules",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModules_Deleted",
                table: "DeviceModules",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModules_ModifiedById",
                table: "DeviceModules",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "UX_DeviceModule_Device_Type",
                table: "DeviceModules",
                columns: new[] { "DeviceId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModuleStatuses_CreateDate",
                table: "DeviceModuleStatuses",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModuleStatuses_CreatedByUserId_ModifiedById_Deleted",
                table: "DeviceModuleStatuses",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModuleStatuses_Deleted",
                table: "DeviceModuleStatuses",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModuleStatuses_ModifiedById",
                table: "DeviceModuleStatuses",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleStatus_Severity",
                table: "DeviceModuleStatuses",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "UX_ModuleStatus_Module",
                table: "DeviceModuleStatuses",
                column: "DeviceModuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModuleStatusSnapshots_CreatedByUserId",
                table: "DeviceModuleStatusSnapshots",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModuleStatusSnapshots_ModifiedById",
                table: "DeviceModuleStatusSnapshots",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleSnapshot_Module_Time",
                table: "DeviceModuleStatusSnapshots",
                columns: new[] { "DeviceModuleId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleSnapshot_Severity",
                table: "DeviceModuleStatusSnapshots",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Device_Code",
                table: "Devices",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Device_SerialNo",
                table: "Devices",
                column: "SerialNo",
                unique: true,
                filter: "[SerialNo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_BranchId",
                table: "Devices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_CreateDate",
                table: "Devices",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_CreatedByUserId_ModifiedById_Deleted",
                table: "Devices",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_CurrentMetricsId",
                table: "Devices",
                column: "CurrentMetricsId",
                unique: true,
                filter: "[CurrentMetricsId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Deleted",
                table: "Devices",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ModifiedById",
                table: "Devices",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_OperatorId",
                table: "Devices",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSupplies_CreatedByUserId",
                table: "DeviceSupplies",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSupplies_ModifiedById",
                table: "DeviceSupplies",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSupply_ModifiedDate",
                table: "DeviceSupplies",
                column: "ModifiedDate");

            migrationBuilder.CreateIndex(
                name: "UX_DeviceSupply_Module_Type",
                table: "DeviceSupplies",
                columns: new[] { "DeviceModuleId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricSnapshot_Device_Time",
                table: "MetricSnapshots",
                columns: new[] { "DeviceId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricSnapshots_CreateDate",
                table: "MetricSnapshots",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_MetricSnapshots_CreatedByUserId_ModifiedById_Deleted",
                table: "MetricSnapshots",
                columns: new[] { "CreatedByUserId", "ModifiedById", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricSnapshots_Deleted",
                table: "MetricSnapshots",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_MetricSnapshots_ModifiedById",
                table: "MetricSnapshots",
                column: "ModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_CashUnits_Devices_DeviceId",
                table: "CashUnits",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceEvents_Devices_DeviceId",
                table: "DeviceEvents",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceModules_Devices_DeviceId",
                table: "DeviceModules",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_MetricSnapshots_CurrentMetricsId",
                table: "Devices",
                column: "CurrentMetricsId",
                principalTable: "MetricSnapshots",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MetricSnapshots_Devices_DeviceId",
                table: "MetricSnapshots");

            migrationBuilder.DropTable(
                name: "CashUnits");

            migrationBuilder.DropTable(
                name: "DeviceEvents");

            migrationBuilder.DropTable(
                name: "DeviceModuleStatuses");

            migrationBuilder.DropTable(
                name: "DeviceModuleStatusSnapshots");

            migrationBuilder.DropTable(
                name: "DeviceSupplies");

            migrationBuilder.DropTable(
                name: "DeviceModules");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "ContactInfos");

            migrationBuilder.DropTable(
                name: "MetricSnapshots");
        }
    }
}
