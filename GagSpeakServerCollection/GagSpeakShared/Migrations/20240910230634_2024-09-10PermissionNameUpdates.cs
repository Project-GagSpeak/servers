using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240910PermissionNameUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "toybox_active_pattern_name",
                table: "user_active_state_data");

            migrationBuilder.RenameColumn(
                name: "can_send_triggers_allowed",
                table: "client_pair_permissions_access",
                newName: "can_stop_patterns_allowed");

            migrationBuilder.RenameColumn(
                name: "can_send_triggers",
                table: "client_pair_permissions",
                newName: "can_stop_patterns");

            migrationBuilder.AddColumn<Guid>(
                name: "toybox_active_pattern_id",
                table: "user_active_state_data",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "can_send_alarms_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_send_alarms",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "toybox_active_pattern_id",
                table: "user_active_state_data");

            migrationBuilder.DropColumn(
                name: "can_send_alarms_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "can_send_alarms",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "can_stop_patterns_allowed",
                table: "client_pair_permissions_access",
                newName: "can_send_triggers_allowed");

            migrationBuilder.RenameColumn(
                name: "can_stop_patterns",
                table: "client_pair_permissions",
                newName: "can_send_triggers");

            migrationBuilder.AddColumn<string>(
                name: "toybox_active_pattern_name",
                table: "user_active_state_data",
                type: "text",
                nullable: true);
        }
    }
}
