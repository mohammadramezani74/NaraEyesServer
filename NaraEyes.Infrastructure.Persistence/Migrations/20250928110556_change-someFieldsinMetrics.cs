using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaraEyes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class changesomeFieldsinMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CpuTemp",
                table: "MetricSnapshots",
                newName: "TotalRamGb");

            migrationBuilder.AlterColumn<string>(
                name: "ExtraJson",
                table: "MetricSnapshots",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CpuModel",
                table: "MetricSnapshots",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CpuModel",
                table: "MetricSnapshots");

            migrationBuilder.RenameColumn(
                name: "TotalRamGb",
                table: "MetricSnapshots",
                newName: "CpuTemp");

            migrationBuilder.AlterColumn<string>(
                name: "ExtraJson",
                table: "MetricSnapshots",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
