using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240913ShockCollarUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "global_shock_share_code",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "global_shock_vibrate_duration",
                table: "user_global_permissions",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "max_vibrate_duration",
                table: "client_pair_permissions",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "shock_collar_share_code",
                table: "client_pair_permissions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "global_shock_share_code",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_shock_vibrate_duration",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "max_vibrate_duration",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "shock_collar_share_code",
                table: "client_pair_permissions");
        }
    }
}
