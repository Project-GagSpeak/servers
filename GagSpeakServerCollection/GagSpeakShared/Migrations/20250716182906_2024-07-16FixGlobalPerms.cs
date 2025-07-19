using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240716FixGlobalPerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "active_remote_mode",
                table: "user_global_permissions");

            migrationBuilder.RenameColumn(
                name: "lock_toybox_ui",
                table: "user_global_permissions",
                newName: "toys_are_interactable");

            migrationBuilder.AddColumn<bool>(
                name: "in_vibe_room",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "in_vibe_room",
                table: "user_global_permissions");

            migrationBuilder.RenameColumn(
                name: "toys_are_interactable",
                table: "user_global_permissions",
                newName: "lock_toybox_ui");

            migrationBuilder.AddColumn<int>(
                name: "active_remote_mode",
                table: "user_global_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
