using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramDetailsAndTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachedDocumentUrls",
                table: "Programs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "Programs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SponsorName",
                table: "Programs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachedDocumentUrls",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "SponsorName",
                table: "Programs");
        }
    }
}
