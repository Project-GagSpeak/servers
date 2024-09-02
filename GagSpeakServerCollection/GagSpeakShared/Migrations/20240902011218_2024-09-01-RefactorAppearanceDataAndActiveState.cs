using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240901RefactorAppearanceDataAndActiveState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "wardrobe_active_set_locked",
                table: "user_active_state_data");

            migrationBuilder.AddColumn<string>(
                name: "wardrobe_active_set_pad_lock",
                table: "user_active_state_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wardrobe_active_set_password",
                table: "user_active_state_data",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "wardrobe_active_set_pad_lock",
                table: "user_active_state_data");

            migrationBuilder.DropColumn(
                name: "wardrobe_active_set_password",
                table: "user_active_state_data");

            migrationBuilder.AddColumn<bool>(
                name: "wardrobe_active_set_locked",
                table: "user_active_state_data",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
