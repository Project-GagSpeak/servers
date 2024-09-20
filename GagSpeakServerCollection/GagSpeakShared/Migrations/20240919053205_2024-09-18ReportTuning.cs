using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240918ReportTuning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "profile_reporting_timed_out",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "reported_base64picture",
                table: "user_profile_data_reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "profile_timeout_time_stamp",
                table: "user_profile_data",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profile_reporting_timed_out",
                table: "users");

            migrationBuilder.DropColumn(
                name: "reported_base64picture",
                table: "user_profile_data_reports");

            migrationBuilder.DropColumn(
                name: "profile_timeout_time_stamp",
                table: "user_profile_data");
        }
    }
}
