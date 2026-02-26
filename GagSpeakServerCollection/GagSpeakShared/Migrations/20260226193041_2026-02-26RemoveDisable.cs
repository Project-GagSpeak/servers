using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20260226RemoveDisable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profile_disabled",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "warning_strike_count",
                table: "user_profile_data");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "profile_disabled",
                table: "user_profile_data",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "warning_strike_count",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
