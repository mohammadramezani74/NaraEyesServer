using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NaraEyes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addFieldStartDateAndEndDateToMessageOutBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndDate",
                table: "OutBoxDeviceMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartDate",
                table: "OutBoxDeviceMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "OutBoxDeviceMessages");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "OutBoxDeviceMessages");
        }
    }
}
