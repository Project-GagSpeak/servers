using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240725HardcoreChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "forced_stay",
                table: "user_global_permissions",
                newName: "indoor_confinement");

            migrationBuilder.RenameColumn(
                name: "forced_follow",
                table: "user_global_permissions",
                newName: "locked_following");

            migrationBuilder.RenameColumn(
                name: "forced_emote_state",
                table: "user_global_permissions",
                newName: "locked_emote_state");

            migrationBuilder.RenameColumn(
                name: "allow_forced_stay",
                table: "client_pair_permissions",
                newName: "allow_indoor_confinement");

            migrationBuilder.RenameColumn(
                name: "allow_forced_follow",
                table: "client_pair_permissions",
                newName: "allow_locked_following");

            migrationBuilder.RenameColumn(
                name: "allow_forced_sit",
                table: "client_pair_permissions",
                newName: "allow_locked_sitting");

            migrationBuilder.RenameColumn(
                name: "allow_forced_emote",
                table: "client_pair_permissions",
                newName: "allow_locked_emoting");

            migrationBuilder.AddColumn<string>(
                name: "imprisonment",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "in_confinement_task",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hypnosis_max_time_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_imprisonment",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "max_hypnosis_time",
                table: "client_pair_permissions",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "imprisonment",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "in_confinement_task",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "hypnosis_max_time_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "allow_imprisonment",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "max_hypnosis_time",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "locked_following",
                table: "user_global_permissions",
                newName: "forced_follow");

            migrationBuilder.RenameColumn(
                name: "locked_emote_state",
                table: "user_global_permissions",
                newName: "forced_emote_state");

            migrationBuilder.RenameColumn(
                name: "indoor_confinement",
                table: "user_global_permissions",
                newName: "forced_stay");

            migrationBuilder.RenameColumn(
                name: "allow_locked_following",
                table: "client_pair_permissions",
                newName: "allow_forced_follow");

            migrationBuilder.RenameColumn(
                name: "allow_indoor_confinement",
                table: "client_pair_permissions",
                newName: "allow_forced_stay");

            migrationBuilder.RenameColumn(
                name: "allow_locked_sitting",
                table: "client_pair_permissions",
                newName: "allow_forced_sit");

            migrationBuilder.RenameColumn(
                name: "allow_locked_emoting",
                table: "client_pair_permissions",
                newName: "allow_forced_emote");
        }
    }
}
