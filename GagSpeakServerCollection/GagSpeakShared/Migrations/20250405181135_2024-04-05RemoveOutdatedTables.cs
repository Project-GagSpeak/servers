using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240405RemoveOutdatedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_active_state_data");

            migrationBuilder.DropTable(
                name: "user_appearance_data");

            // Global permissions.
            migrationBuilder.AddColumn<int>(
                name: "chat_garbler_channels_bitfield",
                table: "user_global_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.RenameColumn(
                name: "live_chat_garbler_active",
                table: "user_global_permissions",
                newName: "chat_garbler_active");

            migrationBuilder.RenameColumn(
                name: "live_chat_garbler_locked",
                table: "user_global_permissions",
                newName: "chat_garbler_locked");
            /////
            migrationBuilder.RenameColumn(
                name: "item_auto_equip",
                table: "user_global_permissions",
                newName: "gag_visuals");

            migrationBuilder.AddColumn<bool>(
                name: "restriction_visuals",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "restraint_set_auto_equip",
                table: "user_global_permissions",
                newName: "restraint_set_visuals");
            ////
            migrationBuilder.RenameColumn(
                name: "global_trigger_phrase",
                table: "user_global_permissions",
                newName: "trigger_phrase");

            migrationBuilder.DropColumn(
                name: "global_allow_sit_requests",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_allow_motion_requests",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_allow_alias_requests",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_allow_all_requests",
                table: "user_global_permissions");

            migrationBuilder.AddColumn<byte>(
                name: "puppet_perms",
                table: "user_global_permissions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
            ///
            migrationBuilder.DropColumn(
                name: "moodles_enabled",
                table: "user_global_permissions");
            ///
            migrationBuilder.RenameColumn(
                name: "toy_is_active",
                table: "user_global_permissions",
                newName: "toys_are_connected");

            migrationBuilder.AddColumn<bool>(
                name: "toys_are_in_use",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "spatial_vibrator_audio",
                table: "user_global_permissions",
                newName: "spatial_audio");
            ///

            migrationBuilder.DropColumn(
                name: "forced_blindfold",
                table: "user_global_permissions");
            ///
            migrationBuilder.RenameColumn(
                name: "global_shock_vibrate_duration",
                table: "user_global_permissions",
                newName: "shock_vibrate_duration");

            /////////////////////////////////////////////
            migrationBuilder.AddColumn<bool>(
                name: "apply_restrictions",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lock_restrictions",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "max_restriction_time",
                table: "client_pair_permissions",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "unlock_restrictions",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "remove_restrictions",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
            ////////
            migrationBuilder.AddColumn<bool>(
                name: "apply_restraint_layers",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "max_allowed_restraint_time",
                table: "client_pair_permissions",
                newName: "max_restraint_time");
            /////
            migrationBuilder.DropColumn(
                name: "sit_requests",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "motion_requests",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "alias_requests",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "all_requests",
                table: "client_pair_permissions");

            migrationBuilder.AddColumn<byte>(
                name: "puppet_perms",
                table: "client_pair_permissions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
            ///
            migrationBuilder.DropColumn(
                name: "allow_positive_status_types",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_negative_status_types",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_special_status_types",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "pair_can_apply_your_moodles_to_you",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "pair_can_apply_own_moodles_to_you",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_permanent_moodles",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "allow_removing_moodles",
                table: "client_pair_permissions");

            migrationBuilder.AddColumn<byte>(
                name: "moodle_perms",
                table: "client_pair_permissions",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
            ///
            migrationBuilder.RenameColumn(
                name: "can_toggle_toy_state",
                table: "client_pair_permissions",
                newName: "toggle_toy_state");

            migrationBuilder.RenameColumn(
                name: "can_use_vibe_remote",
                table: "client_pair_permissions",
                newName: "remote_control_access");

            migrationBuilder.RenameColumn(
                name: "can_execute_patterns",
                table: "client_pair_permissions",
                newName: "execute_patterns");

            migrationBuilder.RenameColumn(
                name: "can_stop_patterns",
                table: "client_pair_permissions",
                newName: "stop_patterns");

            migrationBuilder.RenameColumn(
                name: "can_toggle_alarms",
                table: "client_pair_permissions",
                newName: "toggle_alarms");

            migrationBuilder.DropColumn(
                name: "can_send_alarms",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "can_toggle_triggers",
                table: "client_pair_permissions",
                newName: "toggle_triggers");

            /////
            migrationBuilder.RenameColumn(
                name: "devotional_states_for_pair",
                table: "client_pair_permissions",
                newName: "pair_locked_states");

            migrationBuilder.DropColumn(
                name: "allow_blindfold",
                table: "client_pair_permissions");

            migrationBuilder.AddColumn<bool>(
                name: "allow_garble_channel_editing",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            ////
            migrationBuilder.RenameColumn(
                name: "shock_collar_share_code",
                table: "client_pair_permissions",
                newName: "pi_shock_share_code");

            ////////////////////////////////////

            migrationBuilder.RenameColumn(
                name: "live_chat_garbler_active_allowed",
                table: "client_pair_permissions_access",
                newName: "chat_garbler_active_allowed");

            migrationBuilder.RenameColumn(
                name: "live_chat_garbler_locked_allowed",
                table: "client_pair_permissions_access",
                newName: "chat_garbler_locked_allowed");

            ////
            migrationBuilder.RenameColumn(
                name: "item_auto_equip_allowed",
                table: "client_pair_permissions_access",
                newName: "gag_visuals_allowed");

            migrationBuilder.AddColumn<bool>(
                name: "restriction_visuals_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "restraint_set_auto_equip_allowed",
                table: "client_pair_permissions_access",
                newName: "restraint_set_visuals_allowed");

            ////
            migrationBuilder.AddColumn<bool>(
                name: "apply_restrictions_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "lock_restrictions_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "max_restriction_time_allowed",
                table: "client_pair_permissions_access",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "unlock_restrictions_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "remove_restrictions_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            ////
            migrationBuilder.RenameColumn(
                name: "max_allowed_restraint_time_allowed",
                table: "client_pair_permissions_access",
                newName: "max_restraint_time_allowed");

            ////
            migrationBuilder.DropColumn(
                name: "sit_requests_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "motion_requests_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "alias_requests_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "all_requests_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.AddColumn<byte>(
                name: "puppet_perms_allowed",
                table: "client_pair_permissions_access",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            /////
            migrationBuilder.DropColumn(
                name: "allow_positive_status_types_allowed",
                table: "client_pair_permissions_access");
            migrationBuilder.DropColumn(
                name: "allow_negative_status_types_allowed",
                table: "client_pair_permissions_access");
            migrationBuilder.DropColumn(
                name: "allow_special_status_types_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "pair_can_apply_your_moodles_to_you_allowed",
                table: "client_pair_permissions_access");
            migrationBuilder.DropColumn(
                name: "pair_can_apply_own_moodles_to_you_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "allow_permanent_moodles_allowed",
                table: "client_pair_permissions_access");
            migrationBuilder.DropColumn(
                name: "allow_removing_moodles_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.AddColumn<byte>(
                name: "moodle_perms_allowed",
                table: "client_pair_permissions_access",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            /////
            migrationBuilder.RenameColumn(
                name: "spatial_vibrator_audio_allowed",
                table: "client_pair_permissions_access",
                newName: "spatial_audio_allowed");

            migrationBuilder.RenameColumn(
                name: "can_toggle_toy_state_allowed",
                table: "client_pair_permissions_access",
                newName: "toggle_toy_state_allowed");

            migrationBuilder.RenameColumn(
                name: "can_use_vibe_remote_allowed",
                table: "client_pair_permissions_access",
                newName: "remote_control_access_allowed");

            migrationBuilder.RenameColumn(
                name: "can_execute_patterns_allowed",
                table: "client_pair_permissions_access",
                newName: "execute_patterns_allowed");

            migrationBuilder.RenameColumn(
                name: "can_stop_patterns_allowed",
                table: "client_pair_permissions_access",
                newName: "stop_patterns_allowed");

            migrationBuilder.RenameColumn(
                name: "can_toggle_alarms_allowed",
                table: "client_pair_permissions_access",
                newName: "toggle_alarms_allowed");

            migrationBuilder.DropColumn(
                name: "can_send_alarms_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.RenameColumn(
                name: "can_toggle_triggers_allowed",
                table: "client_pair_permissions_access",
                newName: "toggle_triggers_allowed");

            //////////////
            migrationBuilder.CreateIndex(
                name: "ix_user_profile_data_user_uid",
                table: "user_profile_data",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_achievement_data_user_uid",
                table: "user_achievement_data",
                column: "user_uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_profile_data_user_uid",
                table: "user_profile_data");

            migrationBuilder.DropIndex(
                name: "ix_user_achievement_data_user_uid",
                table: "user_achievement_data");

            migrationBuilder.RenameColumn(
                name: "chat_garbler_active",
                table: "user_global_permissions",
                newName: "live_chat_garbler_active");

            migrationBuilder.RenameColumn(
                name: "chat_garbler_locked",
                table: "user_global_permissions",
                newName: "live_chat_garbler_locked");

            migrationBuilder.DropColumn(
                name: "chat_garbler_channels_bitfield",
                table: "user_global_permissions");

            migrationBuilder.RenameColumn(
                name: "gag_visuals",
                table: "user_global_permissions",
                newName: "item_auto_equip");

            migrationBuilder.DropColumn(
                name: "restriction_visuals",
                table: "user_global_permissions");

            migrationBuilder.RenameColumn(
                name: "restraint_set_visuals",
                table: "user_global_permissions",
                newName: "restraint_set_auto_equip");

            migrationBuilder.RenameColumn(
                name: "trigger_phrase",
                table: "user_global_permissions",
                newName: "global_trigger_phrase");

            migrationBuilder.AddColumn<bool>(
                name: "global_allow_sit_requests",
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

            migrationBuilder.DropColumn(
                name: "puppet_perms",
                table: "user_global_permissions");

            migrationBuilder.AddColumn<bool>(
                name: "moodles_enabled",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "toys_are_connected",
                table: "user_global_permissions",
                newName: "toy_is_active");

            migrationBuilder.DropColumn(
                name: "toys_are_in_use",
                table: "user_global_permissions");

            migrationBuilder.RenameColumn(
                name: "spatial_audio",
                table: "user_global_permissions",
                newName: "spatial_vibrator_audio");

            migrationBuilder.AddColumn<bool>(
                name: "forced_blindfold",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "shock_vibrate_duration",
                table: "user_global_permissions",
                newName: "global_shock_vibrate_duration");

            /////////////////////////////////////////////
            migrationBuilder.DropColumn(
                name: "apply_restrictions",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "lock_restrictions",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "max_restriction_time",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "unlock_restrictions",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "remove_restrictions",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "apply_restraint_layers",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "max_restraint_time",
                table: "client_pair_permissions",
                newName: "max_allowed_restraint_time");

            migrationBuilder.AddColumn<bool>(
                name: "sit_requests",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "motion_requests",
                table: "client_pair_permissions",
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

            migrationBuilder.DropColumn(
                name: "puppet_perms",
                table: "client_pair_permissions");

            migrationBuilder.AddColumn<bool>(
                name: "allow_positive_status_types",
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
                name: "allow_special_status_types",
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
                name: "pair_can_apply_own_moodles_to_you",
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
                name: "allow_removing_moodles",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropColumn(
                name: "moodle_perms",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "toggle_toy_state",
                table: "client_pair_permissions",
                newName: "can_toggle_toy_state");

            migrationBuilder.RenameColumn(
                name: "remote_control_access",
                table: "client_pair_permissions",
                newName: "can_use_vibe_remote");

            migrationBuilder.RenameColumn(
                name: "execute_patterns",
                table: "client_pair_permissions",
                newName: "can_execute_patterns");

            migrationBuilder.RenameColumn(
                name: "stop_patterns",
                table: "client_pair_permissions",
                newName: "can_stop_patterns");

            migrationBuilder.RenameColumn(
                name: "toggle_alarms",
                table: "client_pair_permissions",
                newName: "can_toggle_alarms");

            migrationBuilder.AddColumn<bool>(
                name: "can_send_alarms",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "toggle_triggers",
                table: "client_pair_permissions",
                newName: "can_toggle_triggers");

            //////////////////////////////////
            migrationBuilder.RenameColumn(
                name: "pair_locked_states",
                table: "client_pair_permissions",
                newName: "devotional_states_for_pair");

            migrationBuilder.AddColumn<bool>(
                name: "allow_blindfold",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropColumn(
                name: "allow_garble_channel_editing",
                table: "client_pair_permissions");

            ////
            migrationBuilder.RenameColumn(
                name: "pi_shock_share_code",
                table: "client_pair_permissions",
                newName: "shock_collar_share_code");

            ////////////////////////////////////

            migrationBuilder.RenameColumn(
                name: "chat_garbler_active_allowed",
                table: "client_pair_permissions_access",
                newName: "live_chat_garbler_active_allowed");

            migrationBuilder.RenameColumn(
                name: "chat_garbler_locked_allowed",
                table: "client_pair_permissions_access",
                newName: "live_chat_garbler_locked_allowed");

            ////
            migrationBuilder.RenameColumn(
                name: "gag_visuals_allowed",
                table: "client_pair_permissions_access",
                newName: "item_auto_equip_allowed");

            migrationBuilder.DropColumn(
                name: "restriction_visuals_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.RenameColumn(
                name: "restraint_set_visuals_allowed",
                table: "client_pair_permissions_access",
                newName: "restraint_set_auto_equip_allowed");

            ////
            migrationBuilder.DropColumn(
                name: "apply_restrictions_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "lock_restrictions_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "max_restriction_time_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "unlock_restrictions_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "remove_restrictions_allowed",
                table: "client_pair_permissions_access");

            ////
            migrationBuilder.RenameColumn(
                name: "max_restraint_time_allowed",
                table: "client_pair_permissions_access",
                newName: "max_allowed_restraint_time_allowed");

            ////
            migrationBuilder.AddColumn<bool>(
                name: "sit_requests_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "motion_requests_allowed",
                table: "client_pair_permissions_access",
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

            migrationBuilder.DropColumn(
                name: "puppet_perms_allowed",
                table: "client_pair_permissions_access");

            /////
            migrationBuilder.AddColumn<bool>(
                name: "allow_positive_status_types_allowed",
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
                name: "allow_special_status_types_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "pair_can_apply_your_moodles_to_you_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "pair_can_apply_own_moodles_to_you_allowed",
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
                name: "allow_removing_moodles_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropColumn(
                name: "moodle_perms_allowed",
                table: "client_pair_permissions_access");

            /////
            migrationBuilder.RenameColumn(
                name: "spatial_audio_allowed",
                table: "client_pair_permissions_access",
                newName: "spatial_vibrator_audio_allowed");

            migrationBuilder.RenameColumn(
                name: "toggle_toy_state_allowed",
                table: "client_pair_permissions_access",
                newName: "can_toggle_toy_state_allowed");

            migrationBuilder.RenameColumn(
                name: "remote_control_access_allowed",
                table: "client_pair_permissions_access",
                newName: "can_use_vibe_remote_allowed");

            migrationBuilder.RenameColumn(
                name: "execute_patterns_allowed",
                table: "client_pair_permissions_access",
                newName: "can_execute_patterns_allowed");

            migrationBuilder.RenameColumn(
                name: "stop_patterns_allowed",
                table: "client_pair_permissions_access",
                newName: "can_stop_patterns_allowed");

            migrationBuilder.RenameColumn(
                name: "gag_visuals_allowed",
                table: "client_pair_permissions_access",
                newName: "can_toggle_alarms_allowed");

            migrationBuilder.AddColumn<bool>(
                name: "can_send_alarms_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "toggle_triggers_allowed",
                table: "client_pair_permissions_access",
                newName: "can_toggle_triggers_allowed");
        
            migrationBuilder.CreateTable(
                name: "user_active_state_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    active_set_enabler = table.Column<string>(type: "text", nullable: true),
                    active_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigner = table.Column<string>(type: "text", nullable: true),
                    padlock = table.Column<string>(type: "text", nullable: true),
                    password = table.Column<string>(type: "text", nullable: true),
                    timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "user_appearance_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
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
                    table.PrimaryKey("pk_user_appearance_data", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_appearance_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
