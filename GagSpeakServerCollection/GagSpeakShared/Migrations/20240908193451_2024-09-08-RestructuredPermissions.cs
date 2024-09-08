using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240908RestructuredPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "commands_from_friends",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "commands_from_party",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "toy_intensity",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "can_control_intensity_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "can_execute_triggers_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "can_use_realtime_vibe_remote_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "change_toy_state_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "commands_from_friends_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "can_control_intensity",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "can_execute_triggers",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "vibrator_alarms_toggle_allowed",
                table: "client_pair_permissions_access",
                newName: "can_use_vibe_remote_allowed");

            migrationBuilder.RenameColumn(
                name: "vibrator_alarms_allowed",
                table: "client_pair_permissions_access",
                newName: "can_toggle_triggers_allowed");

            migrationBuilder.RenameColumn(
                name: "toy_is_active_allowed",
                table: "client_pair_permissions_access",
                newName: "can_toggle_toy_state_allowed");

            migrationBuilder.RenameColumn(
                name: "commands_from_party_allowed",
                table: "client_pair_permissions_access",
                newName: "can_toggle_alarms_allowed");

            migrationBuilder.RenameColumn(
                name: "vibrator_alarms_toggle",
                table: "client_pair_permissions",
                newName: "can_use_vibe_remote");

            migrationBuilder.RenameColumn(
                name: "vibrator_alarms",
                table: "client_pair_permissions",
                newName: "can_toggle_triggers");

            migrationBuilder.RenameColumn(
                name: "change_toy_state",
                table: "client_pair_permissions",
                newName: "can_toggle_toy_state");

            migrationBuilder.RenameColumn(
                name: "can_use_realtime_vibe_remote",
                table: "client_pair_permissions",
                newName: "can_toggle_alarms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "can_use_vibe_remote_allowed",
                table: "client_pair_permissions_access",
                newName: "vibrator_alarms_toggle_allowed");

            migrationBuilder.RenameColumn(
                name: "can_toggle_triggers_allowed",
                table: "client_pair_permissions_access",
                newName: "vibrator_alarms_allowed");

            migrationBuilder.RenameColumn(
                name: "can_toggle_toy_state_allowed",
                table: "client_pair_permissions_access",
                newName: "toy_is_active_allowed");

            migrationBuilder.RenameColumn(
                name: "can_toggle_alarms_allowed",
                table: "client_pair_permissions_access",
                newName: "commands_from_party_allowed");

            migrationBuilder.RenameColumn(
                name: "can_use_vibe_remote",
                table: "client_pair_permissions",
                newName: "vibrator_alarms_toggle");

            migrationBuilder.RenameColumn(
                name: "can_toggle_triggers",
                table: "client_pair_permissions",
                newName: "vibrator_alarms");

            migrationBuilder.RenameColumn(
                name: "can_toggle_toy_state",
                table: "client_pair_permissions",
                newName: "change_toy_state");

            migrationBuilder.RenameColumn(
                name: "can_toggle_alarms",
                table: "client_pair_permissions",
                newName: "can_use_realtime_vibe_remote");

            migrationBuilder.AddColumn<bool>(
                name: "commands_from_friends",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "commands_from_party",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "toy_intensity",
                table: "user_global_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "can_control_intensity_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_execute_triggers_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_use_realtime_vibe_remote_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "change_toy_state_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "commands_from_friends_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_control_intensity",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_execute_triggers",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
