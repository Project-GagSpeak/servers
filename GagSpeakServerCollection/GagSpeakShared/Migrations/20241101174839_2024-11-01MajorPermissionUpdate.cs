using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241101MajorPermissionUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "active_pattern_id",
                table: "user_active_state_data");

            migrationBuilder.DropColumn(
                name: "active_set_name",
                table: "user_active_state_data");

            migrationBuilder.RenameColumn(
                name: "chatboxes_hidden",
                table: "user_global_permissions",
                newName: "chat_boxes_hidden");

            migrationBuilder.RenameColumn(
                name: "allow_hiding_chatboxes",
                table: "client_pair_permissions",
                newName: "allow_hiding_chat_boxes");

            migrationBuilder.AddColumn<bool>(
                name: "allow_beeps",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_shocks",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_vibrations",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_duration",
                table: "user_global_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "max_intensity",
                table: "user_global_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "allow_beeps",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_shocks",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_vibrations",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_duration",
                table: "client_pair_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "max_intensity",
                table: "client_pair_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_beeps",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "allow_shocks",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "allow_vibrations",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "max_duration",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "max_intensity",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "allow_beeps",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_shocks",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_vibrations",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "max_duration",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "max_intensity",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "chat_boxes_hidden",
                table: "user_global_permissions",
                newName: "chatboxes_hidden");

            migrationBuilder.RenameColumn(
                name: "allow_hiding_chat_boxes",
                table: "client_pair_permissions",
                newName: "allow_hiding_chatboxes");

            migrationBuilder.AddColumn<Guid>(
                name: "active_pattern_id",
                table: "user_active_state_data",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "active_set_name",
                table: "user_active_state_data",
                type: "text",
                nullable: true);
        }
    }
}
