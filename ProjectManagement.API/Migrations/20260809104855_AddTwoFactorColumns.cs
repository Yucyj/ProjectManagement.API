using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTwoFactorEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwoFactorCodeExpiry",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTwoFactorEnabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TwoFactorCodeExpiry",
                table: "AspNetUsers");
        }
    }
}
