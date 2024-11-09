using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241109NewProfileWarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profile_timeout_time_stamp",
                table: "user_profile_data");

            migrationBuilder.AddColumn<int>(
                name: "warning_strike_count",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "warning_strike_count",
                table: "user_profile_data");

            migrationBuilder.AddColumn<DateTime>(
                name: "profile_timeout_time_stamp",
                table: "user_profile_data",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
