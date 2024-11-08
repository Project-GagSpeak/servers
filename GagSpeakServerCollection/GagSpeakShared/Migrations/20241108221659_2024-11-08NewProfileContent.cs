using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241108NewProfileContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reported_description",
                table: "user_profile_data_reports",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reported_description",
                table: "user_profile_data_reports");
        }
    }
}
