using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240801ProfilesAndUserStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lock_gag_storage_on_gag_lock",
                table: "user_global_permissions");

            migrationBuilder.RenameColumn(
                name: "lock_gag_storage_on_gag_lock_allowed",
                table: "client_pair_permissions_access",
                newName: "vibrator_alarms_toggle_allowed");

            migrationBuilder.RenameColumn(
                name: "can_create_triggers_allowed",
                table: "client_pair_permissions_access",
                newName: "unlock_restraint_sets_allowed");

            migrationBuilder.RenameColumn(
                name: "can_create_triggers",
                table: "client_pair_permissions",
                newName: "vibrator_alarms_toggle");

            migrationBuilder.AddColumn<bool>(
                name: "allow_removing_moodles_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "gag_features_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "owner_locks_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_removing_moodles",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "gag_features",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "owner_locks",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "unlock_restraint_sets",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "user_active_state_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    wardrobe_active_set_name = table.Column<string>(type: "text", nullable: true),
                    wardrobe_active_set_assigner = table.Column<string>(type: "text", nullable: true),
                    wardrobe_active_set_locked = table.Column<bool>(type: "boolean", nullable: false),
                    wardrobe_active_set_lock_assigner = table.Column<string>(type: "text", nullable: true),
                    toybox_active_pattern_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_active_state_data", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_active_state_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profile_data_reports",
                columns: table => new
                {
                    report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reported_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    reporting_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    report_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profile_data_reports", x => x.report_id);
                    table.ForeignKey(
                        name: "fk_user_profile_data_reports_users_reported_user_uid",
                        column: x => x.reported_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                    table.ForeignKey(
                        name: "fk_user_profile_data_reports_users_reporting_user_uid",
                        column: x => x.reporting_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_profile_data_reports_reported_user_uid",
                table: "user_profile_data_reports",
                column: "reported_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_profile_data_reports_reporting_user_uid",
                table: "user_profile_data_reports",
                column: "reporting_user_uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_active_state_data");

            migrationBuilder.DropTable(
                name: "user_profile_data_reports");

            migrationBuilder.DropColumn(
                name: "allow_removing_moodles_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "gag_features_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "owner_locks_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "allow_removing_moodles",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "gag_features",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "owner_locks",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "unlock_restraint_sets",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "vibrator_alarms_toggle_allowed",
                table: "client_pair_permissions_access",
                newName: "lock_gag_storage_on_gag_lock_allowed");

            migrationBuilder.RenameColumn(
                name: "unlock_restraint_sets_allowed",
                table: "client_pair_permissions_access",
                newName: "can_create_triggers_allowed");

            migrationBuilder.RenameColumn(
                name: "vibrator_alarms_toggle",
                table: "client_pair_permissions",
                newName: "can_create_triggers");

            migrationBuilder.AddColumn<bool>(
                name: "lock_gag_storage_on_gag_lock",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
