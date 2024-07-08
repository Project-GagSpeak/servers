using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240705RestructuredTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_client_pair_permissions_hardcore_pair_permissions_hardcore_",
                table: "client_pair_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_client_pair_permissions_puppeteer_pair_permissions_puppetee",
                table: "client_pair_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_client_pair_permissions_toybox_pair_permissions_toybox_pair",
                table: "client_pair_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_client_pair_permissions_wardrobe_pair_permissions_wardrobe_",
                table: "client_pair_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_user_settings_data_puppeteer_global_permissions_puppeteer_g",
                table: "user_settings_data");

            migrationBuilder.DropForeignKey(
                name: "fk_user_settings_data_toybox_global_permissions_toybox_global_",
                table: "user_settings_data");

            migrationBuilder.DropForeignKey(
                name: "fk_user_settings_data_user_apperance_data_user_apperance_data_",
                table: "user_settings_data");

            migrationBuilder.DropForeignKey(
                name: "fk_user_settings_data_users_user_uid",
                table: "user_settings_data");

            migrationBuilder.DropForeignKey(
                name: "fk_user_settings_data_wardrobe_global_permissions_wardrobe_glo",
                table: "user_settings_data");

            migrationBuilder.DropTable(
                name: "hardcore_pair_permissions");

            migrationBuilder.DropTable(
                name: "puppeteer_global_permissions");

            migrationBuilder.DropTable(
                name: "puppeteer_pair_permissions");

            migrationBuilder.DropTable(
                name: "toybox_global_permissions");

            migrationBuilder.DropTable(
                name: "toybox_pair_permissions");

            migrationBuilder.DropTable(
                name: "user_apperance_data");

            migrationBuilder.DropTable(
                name: "wardrobe_global_permissions");

            migrationBuilder.DropTable(
                name: "wardrobe_pair_permissions");

            migrationBuilder.DropIndex(
                name: "ix_client_pair_permissions_hardcore_pair_permissions_user_uid_",
                table: "client_pair_permissions");

            migrationBuilder.DropIndex(
                name: "ix_client_pair_permissions_puppeteer_pair_permissions_user_uid",
                table: "client_pair_permissions");

            migrationBuilder.DropIndex(
                name: "ix_client_pair_permissions_toybox_pair_permissions_user_uid_to",
                table: "client_pair_permissions");

            migrationBuilder.DropIndex(
                name: "ix_client_pair_permissions_wardrobe_pair_permissions_user_uid_",
                table: "client_pair_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_settings_data",
                table: "user_settings_data");

            migrationBuilder.DropIndex(
                name: "ix_user_settings_data_puppeteer_global_permissions_user_uid",
                table: "user_settings_data");

            migrationBuilder.DropIndex(
                name: "ix_user_settings_data_toybox_global_permissions_user_uid",
                table: "user_settings_data");

            migrationBuilder.DropIndex(
                name: "ix_user_settings_data_user_apperance_data_user_uid",
                table: "user_settings_data");

            migrationBuilder.DropIndex(
                name: "ix_user_settings_data_wardrobe_global_permissions_user_uid",
                table: "user_settings_data");

            migrationBuilder.DropColumn(
                name: "is_admin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_moderator",
                table: "users");

            migrationBuilder.DropColumn(
                name: "hardcore_pair_permissions_other_user_uid",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "hardcore_pair_permissions_user_uid",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "puppeteer_pair_permissions_other_user_uid",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "puppeteer_pair_permissions_user_uid",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "toybox_pair_permissions_other_user_uid",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "toybox_pair_permissions_user_uid",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "wardrobe_pair_permissions_other_user_uid",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "wardrobe_pair_permissions_user_uid",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "puppeteer_global_permissions_user_uid",
                table: "user_settings_data");

            migrationBuilder.DropColumn(
                name: "toybox_global_permissions_user_uid",
                table: "user_settings_data");

            migrationBuilder.DropColumn(
                name: "user_apperance_data_user_uid",
                table: "user_settings_data");

            migrationBuilder.RenameTable(
                name: "user_settings_data",
                newName: "user_global_permissions");

            migrationBuilder.RenameColumn(
                name: "wardrobe_global_permissions_user_uid",
                table: "user_global_permissions",
                newName: "global_trigger_phrase");

            migrationBuilder.RenameColumn(
                name: "revert_style",
                table: "user_global_permissions",
                newName: "toy_intensity");

            migrationBuilder.RenameColumn(
                name: "live_garbler_zone_change_warn",
                table: "user_global_permissions",
                newName: "wardrobe_enabled");

            migrationBuilder.RenameColumn(
                name: "direct_chat_garbler_locked",
                table: "user_global_permissions",
                newName: "toybox_enabled");

            migrationBuilder.RenameColumn(
                name: "direct_chat_garbler_active",
                table: "user_global_permissions",
                newName: "toy_is_active");

            migrationBuilder.RenameColumn(
                name: "cmds_from_party",
                table: "user_global_permissions",
                newName: "spatial_vibrator_audio");

            migrationBuilder.RenameColumn(
                name: "cmds_from_friends",
                table: "user_global_permissions",
                newName: "restraint_set_auto_equip");

            migrationBuilder.AddColumn<bool>(
                name: "allow_all_requests",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_blindfold",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_forced_follow",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_forced_sit",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_forced_to_stay",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_motion_requests",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_negative_status_types",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_permanent_moodles",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_positive_status_types",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_sit_requests",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_special_status_types",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "apply_restraint_sets",
                table: "client_pair_permissions",
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
                name: "can_create_triggers",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_execute_patterns",
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

            migrationBuilder.AddColumn<bool>(
                name: "can_send_triggers",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_use_realtime_vibe_remote",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "change_toy_state",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<char>(
                name: "end_char",
                table: "client_pair_permissions",
                type: "character(1)",
                nullable: false,
                defaultValue: ')');

            migrationBuilder.AddColumn<bool>(
                name: "force_lock_first_person",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_blindfolded",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_forced_to_follow",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_forced_to_sit",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_forced_to_stay",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_paused",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lock_restraint_sets",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "max_allowed_restraint_time",
                table: "client_pair_permissions",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "max_lock_time",
                table: "client_pair_permissions",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "max_moodle_time",
                table: "client_pair_permissions",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "pair_can_apply_own_moodles_to_you",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "pair_can_apply_your_moodles_to_you",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "remove_restraint_sets",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<char>(
                name: "start_char",
                table: "client_pair_permissions",
                type: "character(1)",
                nullable: false,
                defaultValue: '(');

            migrationBuilder.AddColumn<string>(
                name: "trigger_phrase",
                table: "client_pair_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "vibrator_alarms",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<bool>(
                name: "global_allow_all_requests",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "global_allow_motion_requests",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "global_allow_sit_requests",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "item_auto_equip",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "live_chat_garbler_active",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "live_chat_garbler_locked",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lock_gag_storage_on_gag_lock",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lock_toybox_ui",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "moodles_enabled",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "puppeteer_enabled",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_global_permissions",
                table: "user_global_permissions",
                column: "user_uid");

            migrationBuilder.CreateTable(
                name: "client_pair_permissions_access",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    commands_from_friends_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    commands_from_party_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    live_chat_garbler_active_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    live_chat_garbler_locked_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    extended_lock_times_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_lock_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    wardrobe_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    item_auto_equip_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    restraint_set_auto_equip_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_gag_storage_on_gag_lock_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_allowed_restraint_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    remove_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    puppeteer_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_sit_requests_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_motion_requests_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_all_requests_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    moodles_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_positive_status_types_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_negative_status_types_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_special_status_types_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    pair_can_apply_own_moodles_to_you_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    pair_can_apply_your_moodles_to_you_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_moodle_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_permanent_moodles_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    toybox_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_toybox_ui_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    toy_is_active_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    spatial_vibrator_audio_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    change_toy_state_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_control_intensity_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    vibrator_alarms_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_realtime_vibe_remote_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_execute_patterns_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_execute_triggers_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_create_triggers_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_send_triggers_allowed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_pair_permissions_access", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_client_pair_permissions_access_users_other_user_uid",
                        column: x => x.other_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_pair_permissions_access_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_appearance_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    slot_one_gag_type = table.Column<string>(type: "text", nullable: true),
                    slot_one_gag_padlock = table.Column<string>(type: "text", nullable: true),
                    slot_one_gag_password = table.Column<string>(type: "text", nullable: true),
                    slot_one_gag_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    slot_one_gag_assigner = table.Column<string>(type: "text", nullable: true),
                    slot_two_gag_type = table.Column<string>(type: "text", nullable: true),
                    slot_two_gag_padlock = table.Column<string>(type: "text", nullable: true),
                    slot_two_gag_password = table.Column<string>(type: "text", nullable: true),
                    slot_two_gag_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    slot_two_gag_assigner = table.Column<string>(type: "text", nullable: true),
                    slot_three_gag_type = table.Column<string>(type: "text", nullable: true),
                    slot_three_gag_padlock = table.Column<string>(type: "text", nullable: true),
                    slot_three_gag_password = table.Column<string>(type: "text", nullable: true),
                    slot_three_gag_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    slot_three_gag_assigner = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_appearance_data", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_appearance_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_access_other_user_uid",
                table: "client_pair_permissions_access",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_access_user_uid",
                table: "client_pair_permissions_access",
                column: "user_uid");

            migrationBuilder.AddForeignKey(
                name: "fk_user_global_permissions_users_user_uid",
                table: "user_global_permissions",
                column: "user_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_global_permissions_users_user_uid",
                table: "user_global_permissions");

            migrationBuilder.DropTable(
                name: "client_pair_permissions_access");

            migrationBuilder.DropTable(
                name: "user_appearance_data");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_global_permissions",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "allow_all_requests",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_blindfold",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_forced_follow",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_forced_sit",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_forced_to_stay",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_motion_requests",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_negative_status_types",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_permanent_moodles",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_positive_status_types",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_sit_requests",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_special_status_types",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "apply_restraint_sets",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "can_control_intensity",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "can_create_triggers",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "can_execute_patterns",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "can_execute_triggers",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "can_send_triggers",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "can_use_realtime_vibe_remote",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "change_toy_state",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "end_char",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "force_lock_first_person",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "is_blindfolded",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "is_forced_to_follow",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "is_forced_to_sit",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "is_forced_to_stay",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "is_paused",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "lock_restraint_sets",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "max_allowed_restraint_time",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "max_lock_time",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "max_moodle_time",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "pair_can_apply_own_moodles_to_you",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "pair_can_apply_your_moodles_to_you",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "remove_restraint_sets",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "start_char",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "trigger_phrase",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "vibrator_alarms",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "commands_from_friends",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "commands_from_party",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_allow_all_requests",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_allow_motion_requests",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_allow_sit_requests",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "item_auto_equip",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "live_chat_garbler_active",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "live_chat_garbler_locked",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "lock_gag_storage_on_gag_lock",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "lock_toybox_ui",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "moodles_enabled",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "puppeteer_enabled",
                table: "user_global_permissions");

            migrationBuilder.RenameTable(
                name: "user_global_permissions",
                newName: "user_settings_data");

            migrationBuilder.RenameColumn(
                name: "wardrobe_enabled",
                table: "user_settings_data",
                newName: "live_garbler_zone_change_warn");

            migrationBuilder.RenameColumn(
                name: "toybox_enabled",
                table: "user_settings_data",
                newName: "direct_chat_garbler_locked");

            migrationBuilder.RenameColumn(
                name: "toy_is_active",
                table: "user_settings_data",
                newName: "direct_chat_garbler_active");

            migrationBuilder.RenameColumn(
                name: "toy_intensity",
                table: "user_settings_data",
                newName: "revert_style");

            migrationBuilder.RenameColumn(
                name: "spatial_vibrator_audio",
                table: "user_settings_data",
                newName: "cmds_from_party");

            migrationBuilder.RenameColumn(
                name: "restraint_set_auto_equip",
                table: "user_settings_data",
                newName: "cmds_from_friends");

            migrationBuilder.RenameColumn(
                name: "global_trigger_phrase",
                table: "user_settings_data",
                newName: "wardrobe_global_permissions_user_uid");

            migrationBuilder.AddColumn<bool>(
                name: "is_admin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_moderator",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "hardcore_pair_permissions_other_user_uid",
                table: "client_pair_permissions",
                type: "character varying(10)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hardcore_pair_permissions_user_uid",
                table: "client_pair_permissions",
                type: "character varying(10)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "puppeteer_pair_permissions_other_user_uid",
                table: "client_pair_permissions",
                type: "character varying(10)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "puppeteer_pair_permissions_user_uid",
                table: "client_pair_permissions",
                type: "character varying(10)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "toybox_pair_permissions_other_user_uid",
                table: "client_pair_permissions",
                type: "character varying(10)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "toybox_pair_permissions_user_uid",
                table: "client_pair_permissions",
                type: "character varying(10)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wardrobe_pair_permissions_other_user_uid",
                table: "client_pair_permissions",
                type: "character varying(10)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wardrobe_pair_permissions_user_uid",
                table: "client_pair_permissions",
                type: "character varying(10)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "puppeteer_global_permissions_user_uid",
                table: "user_settings_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "toybox_global_permissions_user_uid",
                table: "user_settings_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_apperance_data_user_uid",
                table: "user_settings_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_settings_data",
                table: "user_settings_data",
                column: "user_uid");

            migrationBuilder.CreateTable(
                name: "hardcore_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    allow_blindfold = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_follow = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_sit = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_to_stay = table.Column<bool>(type: "boolean", nullable: false),
                    force_lock_first_person = table.Column<bool>(type: "boolean", nullable: false),
                    is_blindfoldeded = table.Column<bool>(type: "boolean", nullable: false),
                    is_forced_to_follow = table.Column<bool>(type: "boolean", nullable: false),
                    is_forced_to_sit = table.Column<bool>(type: "boolean", nullable: false),
                    is_forced_to_stay = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hardcore_pair_permissions", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_hardcore_pair_permissions_users_other_user_uid",
                        column: x => x.other_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_hardcore_pair_permissions_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "puppeteer_global_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "text", nullable: false),
                    enable_puppeteer = table.Column<bool>(type: "boolean", nullable: false),
                    global_allow_all_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_allow_motion_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_allow_sit_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_trigger_phrase = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_puppeteer_global_permissions", x => x.user_uid);
                });

            migrationBuilder.CreateTable(
                name: "puppeteer_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    allow_all_requests = table.Column<bool>(type: "boolean", nullable: false),
                    allow_motion_requests = table.Column<bool>(type: "boolean", nullable: false),
                    allow_sit_requests = table.Column<bool>(type: "boolean", nullable: false),
                    end_char = table.Column<char>(type: "character(1)", nullable: false),
                    start_char = table.Column<char>(type: "character(1)", nullable: false),
                    trigger_phrase = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_puppeteer_pair_permissions", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_puppeteer_pair_permissions_users_other_user_uid",
                        column: x => x.other_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_puppeteer_pair_permissions_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "toybox_global_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "text", nullable: false),
                    enable_toybox = table.Column<bool>(type: "boolean", nullable: false),
                    lock_toybox_ui = table.Column<bool>(type: "boolean", nullable: false),
                    toy_intensity = table.Column<int>(type: "integer", nullable: false),
                    toy_is_active = table.Column<bool>(type: "boolean", nullable: false),
                    using_simulated_vibrator = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_toybox_global_permissions", x => x.user_uid);
                });

            migrationBuilder.CreateTable(
                name: "toybox_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    can_change_toy_state = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_patterns = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_triggers = table.Column<bool>(type: "boolean", nullable: false),
                    intensity_control = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_toybox_pair_permissions", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_toybox_pair_permissions_users_other_user_uid",
                        column: x => x.other_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_toybox_pair_permissions_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_apperance_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "text", nullable: false),
                    slot_one_gag_assigner = table.Column<string>(type: "text", nullable: true),
                    slot_one_gag_padlock = table.Column<string>(type: "text", nullable: true),
                    slot_one_gag_password = table.Column<string>(type: "text", nullable: true),
                    slot_one_gag_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    slot_one_gag_type = table.Column<string>(type: "text", nullable: true),
                    slot_three_gag_assigner = table.Column<string>(type: "text", nullable: true),
                    slot_three_gag_padlock = table.Column<string>(type: "text", nullable: true),
                    slot_three_gag_password = table.Column<string>(type: "text", nullable: true),
                    slot_three_gag_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    slot_three_gag_type = table.Column<string>(type: "text", nullable: true),
                    slot_two_gag_assigner = table.Column<string>(type: "text", nullable: true),
                    slot_two_gag_padlock = table.Column<string>(type: "text", nullable: true),
                    slot_two_gag_password = table.Column<string>(type: "text", nullable: true),
                    slot_two_gag_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    slot_two_gag_type = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_apperance_data", x => x.user_uid);
                });

            migrationBuilder.CreateTable(
                name: "wardrobe_global_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "text", nullable: false),
                    enable_wardrobe = table.Column<bool>(type: "boolean", nullable: false),
                    item_auto_equip = table.Column<bool>(type: "boolean", nullable: false),
                    lock_gag_storage_on_gag_lock = table.Column<bool>(type: "boolean", nullable: false),
                    restraint_set_auto_equip = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wardrobe_global_permissions", x => x.user_uid);
                });

            migrationBuilder.CreateTable(
                name: "wardrobe_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    lock_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    toggle_restraint_sets = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wardrobe_pair_permissions", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_wardrobe_pair_permissions_users_other_user_uid",
                        column: x => x.other_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_wardrobe_pair_permissions_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_hardcore_pair_permissions_user_uid_",
                table: "client_pair_permissions",
                columns: new[] { "hardcore_pair_permissions_user_uid", "hardcore_pair_permissions_other_user_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_puppeteer_pair_permissions_user_uid",
                table: "client_pair_permissions",
                columns: new[] { "puppeteer_pair_permissions_user_uid", "puppeteer_pair_permissions_other_user_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_toybox_pair_permissions_user_uid_to",
                table: "client_pair_permissions",
                columns: new[] { "toybox_pair_permissions_user_uid", "toybox_pair_permissions_other_user_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_wardrobe_pair_permissions_user_uid_",
                table: "client_pair_permissions",
                columns: new[] { "wardrobe_pair_permissions_user_uid", "wardrobe_pair_permissions_other_user_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_user_settings_data_puppeteer_global_permissions_user_uid",
                table: "user_settings_data",
                column: "puppeteer_global_permissions_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_settings_data_toybox_global_permissions_user_uid",
                table: "user_settings_data",
                column: "toybox_global_permissions_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_settings_data_user_apperance_data_user_uid",
                table: "user_settings_data",
                column: "user_apperance_data_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_settings_data_wardrobe_global_permissions_user_uid",
                table: "user_settings_data",
                column: "wardrobe_global_permissions_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_hardcore_pair_permissions_other_user_uid",
                table: "hardcore_pair_permissions",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_hardcore_pair_permissions_user_uid",
                table: "hardcore_pair_permissions",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_puppeteer_pair_permissions_other_user_uid",
                table: "puppeteer_pair_permissions",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_puppeteer_pair_permissions_user_uid",
                table: "puppeteer_pair_permissions",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_toybox_pair_permissions_other_user_uid",
                table: "toybox_pair_permissions",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_toybox_pair_permissions_user_uid",
                table: "toybox_pair_permissions",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_wardrobe_pair_permissions_other_user_uid",
                table: "wardrobe_pair_permissions",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_wardrobe_pair_permissions_user_uid",
                table: "wardrobe_pair_permissions",
                column: "user_uid");

            migrationBuilder.AddForeignKey(
                name: "fk_client_pair_permissions_hardcore_pair_permissions_hardcore_",
                table: "client_pair_permissions",
                columns: new[] { "hardcore_pair_permissions_user_uid", "hardcore_pair_permissions_other_user_uid" },
                principalTable: "hardcore_pair_permissions",
                principalColumns: new[] { "user_uid", "other_user_uid" });

            migrationBuilder.AddForeignKey(
                name: "fk_client_pair_permissions_puppeteer_pair_permissions_puppetee",
                table: "client_pair_permissions",
                columns: new[] { "puppeteer_pair_permissions_user_uid", "puppeteer_pair_permissions_other_user_uid" },
                principalTable: "puppeteer_pair_permissions",
                principalColumns: new[] { "user_uid", "other_user_uid" });

            migrationBuilder.AddForeignKey(
                name: "fk_client_pair_permissions_toybox_pair_permissions_toybox_pair",
                table: "client_pair_permissions",
                columns: new[] { "toybox_pair_permissions_user_uid", "toybox_pair_permissions_other_user_uid" },
                principalTable: "toybox_pair_permissions",
                principalColumns: new[] { "user_uid", "other_user_uid" });

            migrationBuilder.AddForeignKey(
                name: "fk_client_pair_permissions_wardrobe_pair_permissions_wardrobe_",
                table: "client_pair_permissions",
                columns: new[] { "wardrobe_pair_permissions_user_uid", "wardrobe_pair_permissions_other_user_uid" },
                principalTable: "wardrobe_pair_permissions",
                principalColumns: new[] { "user_uid", "other_user_uid" });

            migrationBuilder.AddForeignKey(
                name: "fk_user_settings_data_puppeteer_global_permissions_puppeteer_g",
                table: "user_settings_data",
                column: "puppeteer_global_permissions_user_uid",
                principalTable: "puppeteer_global_permissions",
                principalColumn: "user_uid");

            migrationBuilder.AddForeignKey(
                name: "fk_user_settings_data_toybox_global_permissions_toybox_global_",
                table: "user_settings_data",
                column: "toybox_global_permissions_user_uid",
                principalTable: "toybox_global_permissions",
                principalColumn: "user_uid");

            migrationBuilder.AddForeignKey(
                name: "fk_user_settings_data_user_apperance_data_user_apperance_data_",
                table: "user_settings_data",
                column: "user_apperance_data_user_uid",
                principalTable: "user_apperance_data",
                principalColumn: "user_uid");

            migrationBuilder.AddForeignKey(
                name: "fk_user_settings_data_users_user_uid",
                table: "user_settings_data",
                column: "user_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_settings_data_wardrobe_global_permissions_wardrobe_glo",
                table: "user_settings_data",
                column: "wardrobe_global_permissions_user_uid",
                principalTable: "wardrobe_global_permissions",
                principalColumn: "user_uid");
        }
    }
}
