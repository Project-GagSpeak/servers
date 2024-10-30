using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241030KinkplateInjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "blocked_slot_border",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "blocked_slot_overlay",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "blocked_slots_background",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "blocked_slots_border",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "blocked_slots_overlay",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "description_background",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "description_border",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "description_overlay",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "gag_slot_background",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "gag_slot_border",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "gag_slot_overlay",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "padlock_background",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "padlock_border",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "padlock_overlay",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "plate_background",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "plate_border",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "profile_picture_border",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "profile_picture_overlay",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "blocked_slot_border",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "blocked_slot_overlay",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "blocked_slots_background",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "blocked_slots_border",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "blocked_slots_overlay",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "description_background",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "description_border",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "description_overlay",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "gag_slot_background",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "gag_slot_border",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "gag_slot_overlay",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "padlock_background",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "padlock_border",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "padlock_overlay",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "plate_background",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "plate_border",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "profile_picture_border",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "profile_picture_overlay",
                table: "user_profile_data");
        }
    }
}
