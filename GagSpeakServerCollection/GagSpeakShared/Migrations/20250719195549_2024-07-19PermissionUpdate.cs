using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240719PermissionUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lock_toybox_ui_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "remote_control_access_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "toggle_toy_state_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "remote_control_access",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "toggle_toy_state",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "toybox_enabled_allowed",
                table: "client_pair_permissions_access",
                newName: "gagged_nameplate_allowed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "gagged_nameplate_allowed",
                table: "client_pair_permissions_access",
                newName: "toybox_enabled_allowed");

            migrationBuilder.AddColumn<bool>(
                name: "lock_toybox_ui_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "remote_control_access_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "toggle_toy_state_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "remote_control_access",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "toggle_toy_state",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
