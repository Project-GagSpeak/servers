using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240405OnePointThreeDbUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_active_state_data_users_user_uid",
                table: "user_active_state_data");

            migrationBuilder.DropForeignKey(
                name: "fk_user_appearance_data_users_user_uid",
                table: "user_appearance_data");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_appearance_data",
                table: "user_appearance_data");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_active_state_data",
                table: "user_active_state_data");

            migrationBuilder.DropColumn(
                name: "forced_blindfold",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_allow_alias_requests",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_allow_all_requests",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_allow_motion_requests",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "alias_requests_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "all_requests_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "allow_negative_status_types_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "allow_permanent_moodles_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "allow_positive_status_types_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "alias_requests",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "all_requests",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_blindfold",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_forced_to_stay",
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
                name: "slot_one_gag_assigner",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_one_gag_padlock",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_one_gag_password",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_one_gag_timer",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_one_gag_type",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_three_gag_assigner",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_three_gag_padlock",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_three_gag_password",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_three_gag_timer",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_three_gag_type",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_two_gag_assigner",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_two_gag_padlock",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_two_gag_password",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "slot_two_gag_type",
                table: "user_appearance_data");

            migrationBuilder.DropColumn(
                name: "active_set_enabler",
                table: "user_active_state_data");

            migrationBuilder.DropColumn(
                name: "assigner",
                table: "user_active_state_data");

            migrationBuilder.RenameTable(
                name: "user_appearance_data",
                newName: "user_gag_data");

            migrationBuilder.RenameTable(
                name: "user_active_state_data",
                newName: "user_restraint_data");

            migrationBuilder.RenameColumn(
                name: "toy_is_active",
                table: "user_global_permissions",
                newName: "toys_are_in_use");

            migrationBuilder.RenameColumn(
                name: "spatial_vibrator_audio",
                table: "user_global_permissions",
                newName: "toys_are_connected");

            migrationBuilder.RenameColumn(
                name: "restraint_set_auto_equip",
                table: "user_global_permissions",
                newName: "spatial_audio");

            migrationBuilder.RenameColumn(
                name: "moodles_enabled",
                table: "user_global_permissions",
                newName: "restriction_visuals");

            migrationBuilder.RenameColumn(
                name: "live_chat_garbler_locked",
                table: "user_global_permissions",
                newName: "restraint_set_visuals");

            migrationBuilder.RenameColumn(
                name: "live_chat_garbler_active",
                table: "user_global_permissions",
                newName: "gag_visuals");

            migrationBuilder.RenameColumn(
                name: "item_auto_equip",
                table: "user_global_permissions",
                newName: "chat_garbler_locked");

            migrationBuilder.RenameColumn(
                name: "global_trigger_phrase",
                table: "user_global_permissions",
                newName: "trigger_phrase");

            migrationBuilder.RenameColumn(
                name: "global_shock_vibrate_duration",
                table: "user_global_permissions",
                newName: "shock_vibrate_duration");

            migrationBuilder.RenameColumn(
                name: "global_allow_sit_requests",
                table: "user_global_permissions",
                newName: "chat_garbler_active");

            migrationBuilder.RenameColumn(
                name: "spatial_vibrator_audio_allowed",
                table: "client_pair_permissions_access",
                newName: "unlock_restrictions_allowed");

            migrationBuilder.RenameColumn(
                name: "sit_requests_allowed",
                table: "client_pair_permissions_access",
                newName: "toggle_triggers_allowed");

            migrationBuilder.RenameColumn(
                name: "restraint_set_auto_equip_allowed",
                table: "client_pair_permissions_access",
                newName: "toggle_toy_state_allowed");

            migrationBuilder.RenameColumn(
                name: "pair_can_apply_your_moodles_to_you_allowed",
                table: "client_pair_permissions_access",
                newName: "toggle_alarms_allowed");

            migrationBuilder.RenameColumn(
                name: "pair_can_apply_own_moodles_to_you_allowed",
                table: "client_pair_permissions_access",
                newName: "stop_patterns_allowed");

            migrationBuilder.RenameColumn(
                name: "motion_requests_allowed",
                table: "client_pair_permissions_access",
                newName: "spatial_audio_allowed");

            migrationBuilder.RenameColumn(
                name: "max_allowed_restraint_time_allowed",
                table: "client_pair_permissions_access",
                newName: "restriction_visuals_allowed");

            migrationBuilder.RenameColumn(
                name: "live_chat_garbler_locked_allowed",
                table: "client_pair_permissions_access",
                newName: "restraint_set_visuals_allowed");

            migrationBuilder.RenameColumn(
                name: "live_chat_garbler_active_allowed",
                table: "client_pair_permissions_access",
                newName: "remove_restrictions_allowed");

            migrationBuilder.RenameColumn(
                name: "item_auto_equip_allowed",
                table: "client_pair_permissions_access",
                newName: "remote_control_access_allowed");

            migrationBuilder.RenameColumn(
                name: "can_use_vibe_remote_allowed",
                table: "client_pair_permissions_access",
                newName: "max_restriction_time_allowed");

            migrationBuilder.RenameColumn(
                name: "can_toggle_triggers_allowed",
                table: "client_pair_permissions_access",
                newName: "max_restraint_time_allowed");

            migrationBuilder.RenameColumn(
                name: "can_toggle_toy_state_allowed",
                table: "client_pair_permissions_access",
                newName: "lock_restrictions_allowed");

            migrationBuilder.RenameColumn(
                name: "can_toggle_alarms_allowed",
                table: "client_pair_permissions_access",
                newName: "gag_visuals_allowed");

            migrationBuilder.RenameColumn(
                name: "can_stop_patterns_allowed",
                table: "client_pair_permissions_access",
                newName: "execute_patterns_allowed");

            migrationBuilder.RenameColumn(
                name: "can_send_alarms_allowed",
                table: "client_pair_permissions_access",
                newName: "chat_garbler_locked_allowed");

            migrationBuilder.RenameColumn(
                name: "can_execute_patterns_allowed",
                table: "client_pair_permissions_access",
                newName: "chat_garbler_active_allowed");

            migrationBuilder.RenameColumn(
                name: "allow_special_status_types_allowed",
                table: "client_pair_permissions_access",
                newName: "apply_restrictions_allowed");

            migrationBuilder.RenameColumn(
                name: "allow_removing_moodles_allowed",
                table: "client_pair_permissions_access",
                newName: "apply_restraint_layers_allowed");

            migrationBuilder.RenameColumn(
                name: "sit_requests",
                table: "client_pair_permissions",
                newName: "unlock_restrictions");

            migrationBuilder.RenameColumn(
                name: "shock_collar_share_code",
                table: "client_pair_permissions",
                newName: "pi_shock_share_code");

            migrationBuilder.RenameColumn(
                name: "pair_can_apply_your_moodles_to_you",
                table: "client_pair_permissions",
                newName: "toggle_triggers");

            migrationBuilder.RenameColumn(
                name: "pair_can_apply_own_moodles_to_you",
                table: "client_pair_permissions",
                newName: "toggle_toy_state");

            migrationBuilder.RenameColumn(
                name: "motion_requests",
                table: "client_pair_permissions",
                newName: "toggle_alarms");

            migrationBuilder.RenameColumn(
                name: "max_allowed_restraint_time",
                table: "client_pair_permissions",
                newName: "max_restriction_time");

            migrationBuilder.RenameColumn(
                name: "devotional_states_for_pair",
                table: "client_pair_permissions",
                newName: "stop_patterns");

            migrationBuilder.RenameColumn(
                name: "can_use_vibe_remote",
                table: "client_pair_permissions",
                newName: "remove_restrictions");

            migrationBuilder.RenameColumn(
                name: "can_toggle_triggers",
                table: "client_pair_permissions",
                newName: "remote_control_access");

            migrationBuilder.RenameColumn(
                name: "can_toggle_toy_state",
                table: "client_pair_permissions",
                newName: "pair_locked_states");

            migrationBuilder.RenameColumn(
                name: "can_toggle_alarms",
                table: "client_pair_permissions",
                newName: "lock_restrictions");

            migrationBuilder.RenameColumn(
                name: "can_stop_patterns",
                table: "client_pair_permissions",
                newName: "execute_patterns");

            migrationBuilder.RenameColumn(
                name: "can_send_alarms",
                table: "client_pair_permissions",
                newName: "apply_restrictions");

            migrationBuilder.RenameColumn(
                name: "can_execute_patterns",
                table: "client_pair_permissions",
                newName: "apply_restraint_layers");

            migrationBuilder.RenameColumn(
                name: "allow_special_status_types",
                table: "client_pair_permissions",
                newName: "allow_garble_channel_editing");

            migrationBuilder.RenameColumn(
                name: "allow_removing_moodles",
                table: "client_pair_permissions",
                newName: "allow_forced_stay");

            migrationBuilder.RenameColumn(
                name: "slot_two_gag_timer",
                table: "user_gag_data",
                newName: "timer");

            migrationBuilder.RenameColumn(
                name: "active_set_id",
                table: "user_restraint_data",
                newName: "identifier");

            migrationBuilder.AddColumn<int>(
                name: "chat_garbler_channels_bitfield",
                table: "user_global_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "puppet_perms",
                table: "user_global_permissions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "moodle_perms_allowed",
                table: "client_pair_permissions_access",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "puppet_perms_allowed",
                table: "client_pair_permissions_access",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "max_restraint_time",
                table: "client_pair_permissions",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<byte>(
                name: "moodle_perms",
                table: "client_pair_permissions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "puppet_perms",
                table: "client_pair_permissions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<string>(
                name: "user_uid",
                table: "user_gag_data",
                type: "character varying(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)")
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddColumn<byte>(
                name: "layer",
                table: "user_gag_data",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0)
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddColumn<string>(
                name: "enabler",
                table: "user_gag_data",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "gag",
                table: "user_gag_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "padlock",
                table: "user_gag_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "padlock_assigner",
                table: "user_gag_data",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password",
                table: "user_gag_data",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "user_restraint_data",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "padlock",
                table: "user_restraint_data",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "enabler",
                table: "user_restraint_data",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "layers_bitfield",
                table: "user_restraint_data",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "padlock_assigner",
                table: "user_restraint_data",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_gag_data",
                table: "user_gag_data",
                columns: new[] { "user_uid", "layer" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_restraint_data",
                table: "user_restraint_data",
                column: "user_uid");

            migrationBuilder.CreateTable(
                name: "user_restriction_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    layer = table.Column<byte>(type: "smallint", nullable: false),
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    enabler = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    padlock = table.Column<int>(type: "integer", nullable: false),
                    password = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    padlock_assigner = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_restriction_data", x => new { x.user_uid, x.layer });
                    table.ForeignKey(
                        name: "fk_user_restriction_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_profile_data_user_uid",
                table: "user_profile_data",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_achievement_data_user_uid",
                table: "user_achievement_data",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_gag_data_user_uid_layer",
                table: "user_gag_data",
                columns: new[] { "user_uid", "layer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_restraint_data_user_uid",
                table: "user_restraint_data",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_restriction_data_user_uid_layer",
                table: "user_restriction_data",
                columns: new[] { "user_uid", "layer" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_user_gag_data_users_user_uid",
                table: "user_gag_data",
                column: "user_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_restraint_data_users_user_uid",
                table: "user_restraint_data",
                column: "user_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_gag_data_users_user_uid",
                table: "user_gag_data");

            migrationBuilder.DropForeignKey(
                name: "fk_user_restraint_data_users_user_uid",
                table: "user_restraint_data");

            migrationBuilder.DropTable(
                name: "user_restriction_data");

            migrationBuilder.DropIndex(
                name: "ix_user_profile_data_user_uid",
                table: "user_profile_data");

            migrationBuilder.DropIndex(
                name: "ix_user_achievement_data_user_uid",
                table: "user_achievement_data");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_restraint_data",
                table: "user_restraint_data");

            migrationBuilder.DropIndex(
                name: "ix_user_restraint_data_user_uid",
                table: "user_restraint_data");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_gag_data",
                table: "user_gag_data");

            migrationBuilder.DropIndex(
                name: "ix_user_gag_data_user_uid_layer",
                table: "user_gag_data");

            migrationBuilder.DropColumn(
                name: "chat_garbler_channels_bitfield",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "puppet_perms",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "moodle_perms_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "puppet_perms_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "max_restraint_time",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "moodle_perms",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "puppet_perms",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "enabler",
                table: "user_restraint_data");

            migrationBuilder.DropColumn(
                name: "layers_bitfield",
                table: "user_restraint_data");

            migrationBuilder.DropColumn(
                name: "padlock_assigner",
                table: "user_restraint_data");

            migrationBuilder.DropColumn(
                name: "layer",
                table: "user_gag_data");

            migrationBuilder.DropColumn(
                name: "enabler",
                table: "user_gag_data");

            migrationBuilder.DropColumn(
                name: "gag",
                table: "user_gag_data");

            migrationBuilder.DropColumn(
                name: "padlock",
                table: "user_gag_data");

            migrationBuilder.DropColumn(
                name: "padlock_assigner",
                table: "user_gag_data");

            migrationBuilder.DropColumn(
                name: "password",
                table: "user_gag_data");

            migrationBuilder.RenameTable(
                name: "user_restraint_data",
                newName: "user_active_state_data");

            migrationBuilder.RenameTable(
                name: "user_gag_data",
                newName: "user_appearance_data");

            migrationBuilder.RenameColumn(
                name: "trigger_phrase",
                table: "user_global_permissions",
                newName: "global_trigger_phrase");

            migrationBuilder.RenameColumn(
                name: "toys_are_in_use",
                table: "user_global_permissions",
                newName: "toy_is_active");

            migrationBuilder.RenameColumn(
                name: "toys_are_connected",
                table: "user_global_permissions",
                newName: "spatial_vibrator_audio");

            migrationBuilder.RenameColumn(
                name: "spatial_audio",
                table: "user_global_permissions",
                newName: "restraint_set_auto_equip");

            migrationBuilder.RenameColumn(
                name: "shock_vibrate_duration",
                table: "user_global_permissions",
                newName: "global_shock_vibrate_duration");

            migrationBuilder.RenameColumn(
                name: "restriction_visuals",
                table: "user_global_permissions",
                newName: "moodles_enabled");

            migrationBuilder.RenameColumn(
                name: "restraint_set_visuals",
                table: "user_global_permissions",
                newName: "live_chat_garbler_locked");

            migrationBuilder.RenameColumn(
                name: "gag_visuals",
                table: "user_global_permissions",
                newName: "live_chat_garbler_active");

            migrationBuilder.RenameColumn(
                name: "chat_garbler_locked",
                table: "user_global_permissions",
                newName: "item_auto_equip");

            migrationBuilder.RenameColumn(
                name: "chat_garbler_active",
                table: "user_global_permissions",
                newName: "global_allow_sit_requests");

            migrationBuilder.RenameColumn(
                name: "unlock_restrictions_allowed",
                table: "client_pair_permissions_access",
                newName: "spatial_vibrator_audio_allowed");

            migrationBuilder.RenameColumn(
                name: "toggle_triggers_allowed",
                table: "client_pair_permissions_access",
                newName: "sit_requests_allowed");

            migrationBuilder.RenameColumn(
                name: "toggle_toy_state_allowed",
                table: "client_pair_permissions_access",
                newName: "restraint_set_auto_equip_allowed");

            migrationBuilder.RenameColumn(
                name: "toggle_alarms_allowed",
                table: "client_pair_permissions_access",
                newName: "pair_can_apply_your_moodles_to_you_allowed");

            migrationBuilder.RenameColumn(
                name: "stop_patterns_allowed",
                table: "client_pair_permissions_access",
                newName: "pair_can_apply_own_moodles_to_you_allowed");

            migrationBuilder.RenameColumn(
                name: "spatial_audio_allowed",
                table: "client_pair_permissions_access",
                newName: "motion_requests_allowed");

            migrationBuilder.RenameColumn(
                name: "restriction_visuals_allowed",
                table: "client_pair_permissions_access",
                newName: "max_allowed_restraint_time_allowed");

            migrationBuilder.RenameColumn(
                name: "restraint_set_visuals_allowed",
                table: "client_pair_permissions_access",
                newName: "live_chat_garbler_locked_allowed");

            migrationBuilder.RenameColumn(
                name: "remove_restrictions_allowed",
                table: "client_pair_permissions_access",
                newName: "live_chat_garbler_active_allowed");

            migrationBuilder.RenameColumn(
                name: "remote_control_access_allowed",
                table: "client_pair_permissions_access",
                newName: "item_auto_equip_allowed");

            migrationBuilder.RenameColumn(
                name: "max_restriction_time_allowed",
                table: "client_pair_permissions_access",
                newName: "can_use_vibe_remote_allowed");

            migrationBuilder.RenameColumn(
                name: "max_restraint_time_allowed",
                table: "client_pair_permissions_access",
                newName: "can_toggle_triggers_allowed");

            migrationBuilder.RenameColumn(
                name: "lock_restrictions_allowed",
                table: "client_pair_permissions_access",
                newName: "can_toggle_toy_state_allowed");

            migrationBuilder.RenameColumn(
                name: "gag_visuals_allowed",
                table: "client_pair_permissions_access",
                newName: "can_toggle_alarms_allowed");

            migrationBuilder.RenameColumn(
                name: "execute_patterns_allowed",
                table: "client_pair_permissions_access",
                newName: "can_stop_patterns_allowed");

            migrationBuilder.RenameColumn(
                name: "chat_garbler_locked_allowed",
                table: "client_pair_permissions_access",
                newName: "can_send_alarms_allowed");

            migrationBuilder.RenameColumn(
                name: "chat_garbler_active_allowed",
                table: "client_pair_permissions_access",
                newName: "can_execute_patterns_allowed");

            migrationBuilder.RenameColumn(
                name: "apply_restrictions_allowed",
                table: "client_pair_permissions_access",
                newName: "allow_special_status_types_allowed");

            migrationBuilder.RenameColumn(
                name: "apply_restraint_layers_allowed",
                table: "client_pair_permissions_access",
                newName: "allow_removing_moodles_allowed");

            migrationBuilder.RenameColumn(
                name: "unlock_restrictions",
                table: "client_pair_permissions",
                newName: "sit_requests");

            migrationBuilder.RenameColumn(
                name: "toggle_triggers",
                table: "client_pair_permissions",
                newName: "pair_can_apply_your_moodles_to_you");

            migrationBuilder.RenameColumn(
                name: "toggle_toy_state",
                table: "client_pair_permissions",
                newName: "pair_can_apply_own_moodles_to_you");

            migrationBuilder.RenameColumn(
                name: "toggle_alarms",
                table: "client_pair_permissions",
                newName: "motion_requests");

            migrationBuilder.RenameColumn(
                name: "stop_patterns",
                table: "client_pair_permissions",
                newName: "devotional_states_for_pair");

            migrationBuilder.RenameColumn(
                name: "remove_restrictions",
                table: "client_pair_permissions",
                newName: "can_use_vibe_remote");

            migrationBuilder.RenameColumn(
                name: "remote_control_access",
                table: "client_pair_permissions",
                newName: "can_toggle_triggers");

            migrationBuilder.RenameColumn(
                name: "pi_shock_share_code",
                table: "client_pair_permissions",
                newName: "shock_collar_share_code");

            migrationBuilder.RenameColumn(
                name: "pair_locked_states",
                table: "client_pair_permissions",
                newName: "can_toggle_toy_state");

            migrationBuilder.RenameColumn(
                name: "max_restriction_time",
                table: "client_pair_permissions",
                newName: "max_allowed_restraint_time");

            migrationBuilder.RenameColumn(
                name: "lock_restrictions",
                table: "client_pair_permissions",
                newName: "can_toggle_alarms");

            migrationBuilder.RenameColumn(
                name: "execute_patterns",
                table: "client_pair_permissions",
                newName: "can_stop_patterns");

            migrationBuilder.RenameColumn(
                name: "apply_restrictions",
                table: "client_pair_permissions",
                newName: "can_send_alarms");

            migrationBuilder.RenameColumn(
                name: "apply_restraint_layers",
                table: "client_pair_permissions",
                newName: "can_execute_patterns");

            migrationBuilder.RenameColumn(
                name: "allow_garble_channel_editing",
                table: "client_pair_permissions",
                newName: "allow_special_status_types");

            migrationBuilder.RenameColumn(
                name: "allow_forced_stay",
                table: "client_pair_permissions",
                newName: "allow_removing_moodles");

            migrationBuilder.RenameColumn(
                name: "identifier",
                table: "user_active_state_data",
                newName: "active_set_id");

            migrationBuilder.RenameColumn(
                name: "timer",
                table: "user_appearance_data",
                newName: "slot_two_gag_timer");

            migrationBuilder.AddColumn<string>(
                name: "forced_blindfold",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "global_allow_alias_requests",
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
                name: "allow_negative_status_types_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_permanent_moodles_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_positive_status_types_allowed",
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
                name: "allow_blindfold",
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

            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "user_active_state_data",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "padlock",
                table: "user_active_state_data",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "active_set_enabler",
                table: "user_active_state_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "assigner",
                table: "user_active_state_data",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_uid",
                table: "user_appearance_data",
                type: "character varying(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)")
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddColumn<string>(
                name: "slot_one_gag_assigner",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slot_one_gag_padlock",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slot_one_gag_password",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "slot_one_gag_timer",
                table: "user_appearance_data",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "slot_one_gag_type",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slot_three_gag_assigner",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slot_three_gag_padlock",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slot_three_gag_password",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "slot_three_gag_timer",
                table: "user_appearance_data",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "slot_three_gag_type",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slot_two_gag_assigner",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slot_two_gag_padlock",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slot_two_gag_password",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slot_two_gag_type",
                table: "user_appearance_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_active_state_data",
                table: "user_active_state_data",
                column: "user_uid");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_appearance_data",
                table: "user_appearance_data",
                column: "user_uid");

            migrationBuilder.AddForeignKey(
                name: "fk_user_active_state_data_users_user_uid",
                table: "user_active_state_data",
                column: "user_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_appearance_data_users_user_uid",
                table: "user_appearance_data",
                column: "user_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
