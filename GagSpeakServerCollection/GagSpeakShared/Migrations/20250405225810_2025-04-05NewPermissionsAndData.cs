using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20250405NewPermissionsAndData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    permanent_locks = table.Column<bool>(type: "boolean", nullable: false),
                    owner_locks = table.Column<bool>(type: "boolean", nullable: false),
                    devotional_locks = table.Column<bool>(type: "boolean", nullable: false),
                    apply_gags = table.Column<bool>(type: "boolean", nullable: false),
                    lock_gags = table.Column<bool>(type: "boolean", nullable: false),
                    max_gag_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    unlock_gags = table.Column<bool>(type: "boolean", nullable: false),
                    remove_gags = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restrictions = table.Column<bool>(type: "boolean", nullable: false),
                    lock_restrictions = table.Column<bool>(type: "boolean", nullable: false),
                    max_restriction_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    unlock_restrictions = table.Column<bool>(type: "boolean", nullable: false),
                    remove_restrictions = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restraint_layers = table.Column<bool>(type: "boolean", nullable: false),
                    lock_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    max_restraint_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    unlock_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    remove_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    trigger_phrase = table.Column<string>(type: "text", nullable: true),
                    start_char = table.Column<char>(type: "character(1)", nullable: false),
                    end_char = table.Column<char>(type: "character(1)", nullable: false),
                    puppet_perms = table.Column<byte>(type: "smallint", nullable: false),
                    moodle_perms = table.Column<byte>(type: "smallint", nullable: false),
                    max_moodle_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    toggle_toy_state = table.Column<bool>(type: "boolean", nullable: false),
                    remote_control_access = table.Column<bool>(type: "boolean", nullable: false),
                    execute_patterns = table.Column<bool>(type: "boolean", nullable: false),
                    stop_patterns = table.Column<bool>(type: "boolean", nullable: false),
                    toggle_alarms = table.Column<bool>(type: "boolean", nullable: false),
                    toggle_triggers = table.Column<bool>(type: "boolean", nullable: false),
                    in_hardcore = table.Column<bool>(type: "boolean", nullable: false),
                    pair_locked_states = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_follow = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_sit = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_emote = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_stay = table.Column<bool>(type: "boolean", nullable: false),
                    allow_garble_channel_editing = table.Column<bool>(type: "boolean", nullable: false),
                    allow_hiding_chat_boxes = table.Column<bool>(type: "boolean", nullable: false),
                    allow_hiding_chat_input = table.Column<bool>(type: "boolean", nullable: false),
                    allow_chat_input_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    pi_shock_share_code = table.Column<string>(type: "text", nullable: true),
                    allow_shocks = table.Column<bool>(type: "boolean", nullable: false),
                    allow_vibrations = table.Column<bool>(type: "boolean", nullable: false),
                    allow_beeps = table.Column<bool>(type: "boolean", nullable: false),
                    max_intensity = table.Column<int>(type: "integer", nullable: false),
                    max_duration = table.Column<int>(type: "integer", nullable: false),
                    max_vibrate_duration = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_pair_permissions", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_client_pair_permissions_users_other_user_uid",
                        column: x => x.other_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_pair_permissions_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_pair_permissions_access",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    chat_garbler_active_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    chat_garbler_locked_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    permanent_locks_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    owner_locks_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    devotional_locks_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    apply_gags_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_gags_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_gag_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    unlock_gags_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    remove_gags_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    wardrobe_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    gag_visuals_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    restriction_visuals_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    restraint_set_visuals_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restrictions_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restraint_layers_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_restrictions_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_restriction_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    unlock_restrictions_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    remove_restrictions_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_restraint_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    unlock_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    remove_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    puppeteer_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    puppet_perms_allowed = table.Column<byte>(type: "smallint", nullable: false),
                    moodles_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    moodle_perms_allowed = table.Column<byte>(type: "smallint", nullable: false),
                    max_moodle_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    toybox_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_toybox_ui_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    spatial_audio_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    toggle_toy_state_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    remote_control_access_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    execute_patterns_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    stop_patterns_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    toggle_alarms_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    toggle_triggers_allowed = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "user_gag_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    layer = table.Column<byte>(type: "smallint", nullable: false),
                    gag = table.Column<int>(type: "integer", nullable: false),
                    enabler = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    padlock = table.Column<int>(type: "integer", nullable: false),
                    password = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    padlock_assigner = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_gag_data", x => new { x.user_uid, x.layer });
                    table.ForeignKey(
                        name: "fk_user_gag_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_global_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    chat_garbler_channels_bitfield = table.Column<int>(type: "integer", nullable: false),
                    chat_garbler_active = table.Column<bool>(type: "boolean", nullable: false),
                    chat_garbler_locked = table.Column<bool>(type: "boolean", nullable: false),
                    wardrobe_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    gag_visuals = table.Column<bool>(type: "boolean", nullable: false),
                    restriction_visuals = table.Column<bool>(type: "boolean", nullable: false),
                    restraint_set_visuals = table.Column<bool>(type: "boolean", nullable: false),
                    puppeteer_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    trigger_phrase = table.Column<string>(type: "text", nullable: true),
                    puppet_perms = table.Column<byte>(type: "smallint", nullable: false),
                    toybox_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lock_toybox_ui = table.Column<bool>(type: "boolean", nullable: false),
                    toys_are_connected = table.Column<bool>(type: "boolean", nullable: false),
                    toys_are_in_use = table.Column<bool>(type: "boolean", nullable: false),
                    spatial_audio = table.Column<bool>(type: "boolean", nullable: false),
                    forced_follow = table.Column<string>(type: "text", nullable: true),
                    forced_emote_state = table.Column<string>(type: "text", nullable: true),
                    forced_stay = table.Column<string>(type: "text", nullable: true),
                    chat_boxes_hidden = table.Column<string>(type: "text", nullable: true),
                    chat_input_hidden = table.Column<string>(type: "text", nullable: true),
                    chat_input_blocked = table.Column<string>(type: "text", nullable: true),
                    global_shock_share_code = table.Column<string>(type: "text", nullable: true),
                    allow_shocks = table.Column<bool>(type: "boolean", nullable: false),
                    allow_vibrations = table.Column<bool>(type: "boolean", nullable: false),
                    allow_beeps = table.Column<bool>(type: "boolean", nullable: false),
                    max_intensity = table.Column<int>(type: "integer", nullable: false),
                    max_duration = table.Column<int>(type: "integer", nullable: false),
                    shock_vibrate_duration = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_global_permissions", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_global_permissions_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_restraintset_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    layers_bitfield = table.Column<byte>(type: "smallint", nullable: false),
                    enabler = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    padlock = table.Column<int>(type: "integer", nullable: false),
                    password = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    padlock_assigner = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_restraintset_data", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_restraintset_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "ix_client_pair_permissions_other_user_uid",
                table: "client_pair_permissions",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_user_uid",
                table: "client_pair_permissions",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_access_other_user_uid",
                table: "client_pair_permissions_access",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_access_user_uid",
                table: "client_pair_permissions_access",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_gag_data_user_uid_layer",
                table: "user_gag_data",
                columns: new[] { "user_uid", "layer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_restraintset_data_user_uid",
                table: "user_restraintset_data",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_restriction_data_user_uid_layer",
                table: "user_restriction_data",
                columns: new[] { "user_uid", "layer" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_pair_permissions");

            migrationBuilder.DropTable(
                name: "client_pair_permissions_access");

            migrationBuilder.DropTable(
                name: "user_gag_data");

            migrationBuilder.DropTable(
                name: "user_global_permissions");

            migrationBuilder.DropTable(
                name: "user_restraintset_data");

            migrationBuilder.DropTable(
                name: "user_restriction_data");
        }
    }
}
