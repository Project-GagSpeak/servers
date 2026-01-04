using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20260104RefactorForBigUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            #region Valid Drops For UP
            // VALID (I Dont care enough to migrate this anyways)
            migrationBuilder.DropTable(
                name: "user_profile_data_reports");

            // VALID (Bombed)
            migrationBuilder.DropColumn(
                name: "first_upload_timestamp",
                table: "users");

            // VALID (migrated to AccountReputation)
            migrationBuilder.DropColumn(
                name: "upload_limit_counter",
                table: "users");

            // VALID (Migrated this over to AccountReputation)
            migrationBuilder.DropColumn(
                name: "verified",
                table: "users");

            // VALID (Banish Pausing to the shadow realm)
            migrationBuilder.DropColumn(
                name: "is_paused",
                table: "client_pair_permissions");

            // Valid
            migrationBuilder.DropColumn(
                name: "is_banned",
                table: "auth");
            #endregion Valid Drops For UP

            // Guessing this is because i made them non-nullable?
            migrationBuilder.DropForeignKey(
                name: "fk_auth_users_primary_user_uid",
                table: "auth");

            // Guessing this is because i made them non-nullable?
            migrationBuilder.DropForeignKey(
                name: "fk_auth_users_user_uid",
                table: "auth");

            // Guessing this is because i made them non-nullable?
            migrationBuilder.DropForeignKey(
                name: "fk_collar_owner_user_collar_data_collared_user_uid",
                table: "collar_owner");

            // Guessing this is because i made them non-nullable?
            migrationBuilder.DropForeignKey(
                name: "fk_user_profile_data_user_collar_data_user_uid",
                table: "user_profile_data");

            #region Valid Renames For UP

            // Valid
            migrationBuilder.RenameColumn(
                name: "vanity_tier",
                table: "users",
                newName: "tier");
            // Valid
            migrationBuilder.RenameColumn(
                name: "last_logged_in",
                table: "users",
                newName: "last_login");

            // Valid
            migrationBuilder.RenameColumn(
                name: "user_description",
                table: "user_profile_data",
                newName: "description");

            // Valid
            migrationBuilder.RenameColumn(
                name: "plate_background",
                table: "user_profile_data",
                newName: "plate_bg");

            // Valid
            migrationBuilder.RenameColumn(
                name: "padlock_background",
                table: "user_profile_data",
                newName: "padlock_bg");

            // Valid
            migrationBuilder.RenameColumn(
                name: "gag_slot_background",
                table: "user_profile_data",
                newName: "gag_slot_bg");

            // Valid
            migrationBuilder.RenameColumn(
                name: "description_background",
                table: "user_profile_data",
                newName: "description_bg");

            // VALID
            migrationBuilder.RenameColumn(
                name: "status_on_dispell",
                table: "moodle_status",
                newName: "chained_status");

            // VALID
            migrationBuilder.RenameColumn(
                name: "persistent",
                table: "moodle_status",
                newName: "permanent");

            // VALID
            migrationBuilder.RenameColumn(
                name: "custom_vfx_path",
                table: "moodle_status",
                newName: "custom_fx_path");

            // VALID
            migrationBuilder.RenameColumn(
                name: "moodle_perms_allowed",
                table: "client_pair_permissions_access",
                newName: "moodle_access_allowed");

            // VALID
            migrationBuilder.RenameColumn(
                name: "moodle_perms",
                table: "client_pair_permissions",
                newName: "moodle_access");

            // VALID
            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "users",
                newName: "created_at");

            // VALID
            migrationBuilder.RenameColumn(
                name: "completed_achievements_total",
                table: "user_profile_data",
                newName: "achievements_earned");

            migrationBuilder.RenameColumn(
                name: "profile_picture_border",
                table: "user_profile_data",
                newName: "avatar_border");

            migrationBuilder.RenameColumn(
                name: "profile_picture_overlay",
                table: "user_profile_data",
                newName: "avatar_overlay");

            // VALID (Migrated name)
            migrationBuilder.RenameColumn(
                name: "stacks_inc_on_reapply",
                table: "moodle_status",
                newName: "stack_steps");

            // VALID (Migrated Name)
            migrationBuilder.RenameColumn(
                name: "blocked_slots_background",
                table: "user_profile_data",
                newName: "blocked_slots_bg");
            #endregion Valid Renames For UP

            #region Valid Add-Alter-Create For UP

            migrationBuilder.AddColumn<int>(
                name: "plate_light_bg",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "plate_light_border",
                table: "user_profile_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);


            // VALID (new addition for MyStatus v2.0)
            migrationBuilder.AddColumn<int>(
                name: "chain_trigger",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // VALID (new addition for MyStatus v2.0)
            migrationBuilder.AddColumn<long>(
                name: "modifiers",
                table: "moodle_status",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // VALID
            migrationBuilder.AddColumn<bool>(
                name: "is_temporary",
                table: "kinkster_pair_requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // VALID
            migrationBuilder.AddColumn<string>(
                name: "preferred_nickname",
                table: "kinkster_pair_requests",
                type: "text",
                nullable: true);

            // VALID
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "client_pairs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // VALID (But wont really be used yet)
            migrationBuilder.AddColumn<string>(
                name: "temp_accepter_uid",
                table: "client_pairs",
                type: "text",
                nullable: true);

            // Valid (Version is new)
            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);


            // VALID (This is new for GagSpeak 2.0)
            migrationBuilder.CreateTable(
                name: "account_reputation",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false),
                    upload_allowances = table.Column<int>(type: "integer", nullable: false),
                    profile_viewing = table.Column<bool>(type: "boolean", nullable: false),
                    profile_view_timeout = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    profile_view_strikes = table.Column<int>(type: "integer", nullable: false),
                    profile_editing = table.Column<bool>(type: "boolean", nullable: false),
                    profile_edit_timeout = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    profile_edit_strikes = table.Column<int>(type: "integer", nullable: false),
                    chat_usage = table.Column<bool>(type: "boolean", nullable: false),
                    chat_timeout = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    chat_strikes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_reputation", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_account_reputation_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            // Populate account_reputation for existing auth.primary_user_uid
            migrationBuilder.Sql(@"
                INSERT INTO account_reputation (user_uid, is_verified, is_banned, upload_allowances, profile_viewing,
                    profile_view_timeout, profile_view_strikes, profile_editing, profile_edit_timeout,
                    profile_edit_strikes, chat_usage, chat_timeout, chat_strikes)
                SELECT DISTINCT primary_user_uid, false, false, 0, false,
                        now(), 0, false, now(),
                        0, false, now(), 0
                FROM auth
                WHERE primary_user_uid IS NOT NULL;
            ");

            // VALID (UserUID Will be required now)
            migrationBuilder.AlterColumn<string>(
                name: "user_uid",
                table: "auth",
                type: "character varying(10)",
                nullable: false,
                defaultValue: "");

            // VALID (PrimaryUserUID will be required now)
            migrationBuilder.AlterColumn<string>(
                name: "primary_user_uid",
                table: "auth",
                type: "character varying(10)",
                nullable: false,
                defaultValue: "");

            // VALID (We now link Account Reputation in a User's Auth entry)
            migrationBuilder.AddForeignKey(
                name: "fk_auth_account_reputation_primary_user_uid",
                table: "auth",
                column: "primary_user_uid",
                principalTable: "account_reputation",
                principalColumn: "user_uid",
                onDelete: ReferentialAction.Cascade);

            // VALID (We now link an Auth's Primary User as a required, non-nullable field)
            migrationBuilder.AddForeignKey(
                name: "fk_auth_users_primary_user_uid",
                table: "auth",
                column: "primary_user_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);

            // VALID (We now link an Auth's User as a required, non-nullable field)
            migrationBuilder.AddForeignKey(
                name: "fk_auth_users_user_uid",
                table: "auth",
                column: "user_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);


            // Honestly fine with just creating a new table for this over migrating it anyways, we have no entries
            // in it anyways.
            migrationBuilder.CreateTable(
                name: "report_entries",
                columns: table => new
                {
                    report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<int>(type: "integer", nullable: false),
                    report_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    snapshot_image = table.Column<string>(type: "text", nullable: false),
                    snapshot_description = table.Column<string>(type: "text", nullable: false),
                    reporting_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    reported_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    chat_log_history_base64 = table.Column<string>(type: "text", nullable: true),
                    report_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_entries", x => x.report_id);
                    table.ForeignKey(
                        name: "fk_report_entries_users_reported_user_uid",
                        column: x => x.reported_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                    table.ForeignKey(
                        name: "fk_report_entries_users_reporting_user_uid",
                        column: x => x.reporting_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                });
            // VALID
            migrationBuilder.CreateIndex(
                name: "ix_report_entries_reported_user_uid",
                table: "report_entries",
                column: "reported_user_uid");
            // VALID
            migrationBuilder.CreateIndex(
                name: "ix_report_entries_reporting_user_uid",
                table: "report_entries",
                column: "reporting_user_uid");
            #endregion Valid Add-Alter-Create For UP

            // VALID (A CollarOwner's collared_user_uid is required, referencing to their UserCollarData)
            migrationBuilder.AddForeignKey(
                name: "fk_collar_owner_active_collar_data_collared_user_uid",
                table: "collar_owner",
                column: "collared_user_uid",
                principalTable: "user_collar_data",
                principalColumn: "user_uid",
                onDelete: ReferentialAction.Cascade);

            // VALID (A UserProfileData's user_uid references to their UserCollarData)
            migrationBuilder.AddForeignKey(
                name: "fk_user_profile_data_active_collar_data_user_uid",
                table: "user_profile_data",
                column: "user_uid",
                principalTable: "user_collar_data",
                principalColumn: "user_uid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_auth_account_reputation_primary_user_uid",
                table: "auth");

            migrationBuilder.DropForeignKey(
                name: "fk_auth_users_primary_user_uid",
                table: "auth");

            migrationBuilder.DropForeignKey(
                name: "fk_auth_users_user_uid",
                table: "auth");

            migrationBuilder.DropForeignKey(
                name: "fk_collar_owner_active_collar_data_collared_user_uid",
                table: "collar_owner");

            migrationBuilder.DropForeignKey(
                name: "fk_user_profile_data_active_collar_data_user_uid",
                table: "user_profile_data");

            migrationBuilder.DropTable(
                name: "account_reputation");

            migrationBuilder.DropTable(
                name: "report_entries");

            migrationBuilder.DropColumn(
                name: "achievements_earned",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "avatar_border",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "chain_trigger",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "modifiers",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "stack_steps",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "is_temporary",
                table: "kinkster_pair_requests");

            migrationBuilder.DropColumn(
                name: "preferred_nickname",
                table: "kinkster_pair_requests");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "client_pairs");

            migrationBuilder.DropColumn(
                name: "temp_accepter_uid",
                table: "client_pairs");

            migrationBuilder.RenameColumn(
                name: "tier",
                table: "users",
                newName: "vanity_tier");

            migrationBuilder.RenameColumn(
                name: "last_login",
                table: "users",
                newName: "last_logged_in");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "users",
                newName: "first_upload_timestamp");

            migrationBuilder.RenameColumn(
                name: "plate_light_border",
                table: "user_profile_data",
                newName: "profile_picture_overlay");

            migrationBuilder.RenameColumn(
                name: "plate_light_bg",
                table: "user_profile_data",
                newName: "profile_picture_border");

            migrationBuilder.RenameColumn(
                name: "plate_bg",
                table: "user_profile_data",
                newName: "plate_background");

            migrationBuilder.RenameColumn(
                name: "padlock_bg",
                table: "user_profile_data",
                newName: "padlock_background");

            migrationBuilder.RenameColumn(
                name: "gag_slot_bg",
                table: "user_profile_data",
                newName: "gag_slot_background");

            migrationBuilder.RenameColumn(
                name: "description_bg",
                table: "user_profile_data",
                newName: "description_background");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "user_profile_data",
                newName: "user_description");

            migrationBuilder.RenameColumn(
                name: "blocked_slots_bg",
                table: "user_profile_data",
                newName: "completed_achievements_total");

            migrationBuilder.RenameColumn(
                name: "avatar_overlay",
                table: "user_profile_data",
                newName: "blocked_slots_background");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "moodle_status",
                newName: "stacks_inc_on_reapply");

            migrationBuilder.RenameColumn(
                name: "permanent",
                table: "moodle_status",
                newName: "persistent");

            migrationBuilder.RenameColumn(
                name: "custom_fx_path",
                table: "moodle_status",
                newName: "custom_vfx_path");

            migrationBuilder.RenameColumn(
                name: "chained_status",
                table: "moodle_status",
                newName: "status_on_dispell");

            migrationBuilder.RenameColumn(
                name: "moodle_access_allowed",
                table: "client_pair_permissions_access",
                newName: "moodle_perms_allowed");

            migrationBuilder.RenameColumn(
                name: "moodle_access",
                table: "client_pair_permissions",
                newName: "moodle_perms");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_date",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "upload_limit_counter",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "verified",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<byte>(
                name: "moodle_type",
                table: "user_collar_data",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "is_paused",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "user_uid",
                table: "auth",
                type: "character varying(10)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)");

            migrationBuilder.AlterColumn<string>(
                name: "primary_user_uid",
                table: "auth",
                type: "character varying(10)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)");

            migrationBuilder.AddColumn<bool>(
                name: "is_banned",
                table: "auth",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "user_profile_data_reports",
                columns: table => new
                {
                    report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reported_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    reporting_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    report_reason = table.Column<string>(type: "text", nullable: true),
                    report_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reported_base64picture = table.Column<string>(type: "text", nullable: true),
                    reported_description = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.AddForeignKey(
                name: "fk_auth_users_primary_user_uid",
                table: "auth",
                column: "primary_user_uid",
                principalTable: "users",
                principalColumn: "uid");

            migrationBuilder.AddForeignKey(
                name: "fk_auth_users_user_uid",
                table: "auth",
                column: "user_uid",
                principalTable: "users",
                principalColumn: "uid");

            migrationBuilder.AddForeignKey(
                name: "fk_collar_owner_user_collar_data_collared_user_uid",
                table: "collar_owner",
                column: "collared_user_uid",
                principalTable: "user_collar_data",
                principalColumn: "user_uid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_profile_data_user_collar_data_user_uid",
                table: "user_profile_data",
                column: "user_uid",
                principalTable: "user_collar_data",
                principalColumn: "user_uid",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
