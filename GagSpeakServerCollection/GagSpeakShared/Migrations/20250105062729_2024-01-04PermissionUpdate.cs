using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240104PermissionUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hardcore_safeword_used",
                table: "user_global_permissions");

            migrationBuilder.RenameColumn(
                name: "safeword_used",
                table: "user_global_permissions",
                newName: "global_allow_alias_requests");

            migrationBuilder.RenameColumn(
                name: "max_lock_time_allowed",
                table: "client_pair_permissions_access",
                newName: "unlock_gags_allowed");

            migrationBuilder.RenameColumn(
                name: "gag_features_allowed",
                table: "client_pair_permissions_access",
                newName: "sit_requests_allowed");

            migrationBuilder.RenameColumn(
                name: "extended_lock_times_allowed",
                table: "client_pair_permissions_access",
                newName: "remove_gags_allowed");

            migrationBuilder.RenameColumn(
                name: "allow_sit_requests_allowed",
                table: "client_pair_permissions_access",
                newName: "permanent_locks_allowed");

            migrationBuilder.RenameColumn(
                name: "allow_motion_requests_allowed",
                table: "client_pair_permissions_access",
                newName: "motion_requests_allowed");

            migrationBuilder.RenameColumn(
                name: "allow_all_requests_allowed",
                table: "client_pair_permissions_access",
                newName: "max_gag_time_allowed");

            migrationBuilder.RenameColumn(
                name: "max_lock_time",
                table: "client_pair_permissions",
                newName: "max_gag_time");

            migrationBuilder.RenameColumn(
                name: "gag_features",
                table: "client_pair_permissions",
                newName: "unlock_gags");

            migrationBuilder.RenameColumn(
                name: "extended_lock_times",
                table: "client_pair_permissions",
                newName: "sit_requests");

            migrationBuilder.RenameColumn(
                name: "allow_sit_requests",
                table: "client_pair_permissions",
                newName: "remove_gags");

            migrationBuilder.RenameColumn(
                name: "allow_motion_requests",
                table: "client_pair_permissions",
                newName: "permanent_locks");

            migrationBuilder.RenameColumn(
                name: "allow_all_requests",
                table: "client_pair_permissions",
                newName: "motion_requests");

            migrationBuilder.AddColumn<bool>(
                name: "alias_requests_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "all_requests_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "apply_gags_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lock_gags_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "alias_requests",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "all_requests",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "apply_gags",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lock_gags",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "alias_requests_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "all_requests_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "apply_gags_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "lock_gags_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "alias_requests",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "all_requests",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "apply_gags",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "lock_gags",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "global_allow_alias_requests",
                table: "user_global_permissions",
                newName: "safeword_used");

            migrationBuilder.RenameColumn(
                name: "unlock_gags_allowed",
                table: "client_pair_permissions_access",
                newName: "max_lock_time_allowed");

            migrationBuilder.RenameColumn(
                name: "sit_requests_allowed",
                table: "client_pair_permissions_access",
                newName: "gag_features_allowed");

            migrationBuilder.RenameColumn(
                name: "remove_gags_allowed",
                table: "client_pair_permissions_access",
                newName: "extended_lock_times_allowed");

            migrationBuilder.RenameColumn(
                name: "permanent_locks_allowed",
                table: "client_pair_permissions_access",
                newName: "allow_sit_requests_allowed");

            migrationBuilder.RenameColumn(
                name: "motion_requests_allowed",
                table: "client_pair_permissions_access",
                newName: "allow_motion_requests_allowed");

            migrationBuilder.RenameColumn(
                name: "max_gag_time_allowed",
                table: "client_pair_permissions_access",
                newName: "allow_all_requests_allowed");

            migrationBuilder.RenameColumn(
                name: "unlock_gags",
                table: "client_pair_permissions",
                newName: "gag_features");

            migrationBuilder.RenameColumn(
                name: "sit_requests",
                table: "client_pair_permissions",
                newName: "extended_lock_times");

            migrationBuilder.RenameColumn(
                name: "remove_gags",
                table: "client_pair_permissions",
                newName: "allow_sit_requests");

            migrationBuilder.RenameColumn(
                name: "permanent_locks",
                table: "client_pair_permissions",
                newName: "allow_motion_requests");

            migrationBuilder.RenameColumn(
                name: "motion_requests",
                table: "client_pair_permissions",
                newName: "allow_all_requests");

            migrationBuilder.RenameColumn(
                name: "max_gag_time",
                table: "client_pair_permissions",
                newName: "max_lock_time");

            migrationBuilder.AddColumn<bool>(
                name: "hardcore_safeword_used",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
