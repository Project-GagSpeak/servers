using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20250405ResetTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_pair_permissions");

            migrationBuilder.DropTable(
                name: "client_pair_permissions_access");

            migrationBuilder.DropTable(
                name: "user_active_state_data");

            migrationBuilder.DropTable(
                name: "user_appearance_data");

            migrationBuilder.DropTable(
                name: "user_global_permissions");
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

            migrationBuilder.CreateTable(
                name: "client_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    alias_requests = table.Column<bool>(type: "boolean", nullable: false),
                    all_requests = table.Column<bool>(type: "boolean", nullable: false),
                    allow_beeps = table.Column<bool>(type: "boolean", nullable: false),
                    allow_blindfold = table.Column<bool>(type: "boolean", nullable: false),
                    allow_chat_input_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_emote = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_follow = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_sit = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_to_stay = table.Column<bool>(type: "boolean", nullable: false),
                    allow_hiding_chat_boxes = table.Column<bool>(type: "boolean", nullable: false),
                    allow_hiding_chat_input = table.Column<bool>(type: "boolean", nullable: false),
                    allow_negative_status_types = table.Column<bool>(type: "boolean", nullable: false),
                    allow_permanent_moodles = table.Column<bool>(type: "boolean", nullable: false),
                    allow_positive_status_types = table.Column<bool>(type: "boolean", nullable: false),
                    allow_removing_moodles = table.Column<bool>(type: "boolean", nullable: false),
                    allow_shocks = table.Column<bool>(type: "boolean", nullable: false),
                    allow_special_status_types = table.Column<bool>(type: "boolean", nullable: false),
                    allow_vibrations = table.Column<bool>(type: "boolean", nullable: false),
                    apply_gags = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    can_execute_patterns = table.Column<bool>(type: "boolean", nullable: false),
                    can_send_alarms = table.Column<bool>(type: "boolean", nullable: false),
                    can_stop_patterns = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_alarms = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_toy_state = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_triggers = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_vibe_remote = table.Column<bool>(type: "boolean", nullable: false),
                    devotional_locks = table.Column<bool>(type: "boolean", nullable: false),
                    devotional_states_for_pair = table.Column<bool>(type: "boolean", nullable: false),
                    end_char = table.Column<char>(type: "character(1)", nullable: false),
                    in_hardcore = table.Column<bool>(type: "boolean", nullable: false),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    lock_gags = table.Column<bool>(type: "boolean", nullable: false),
                    lock_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    max_allowed_restraint_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    max_duration = table.Column<int>(type: "integer", nullable: false),
                    max_gag_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    max_intensity = table.Column<int>(type: "integer", nullable: false),
                    max_moodle_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    max_vibrate_duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    motion_requests = table.Column<bool>(type: "boolean", nullable: false),
                    owner_locks = table.Column<bool>(type: "boolean", nullable: false),
                    pair_can_apply_own_moodles_to_you = table.Column<bool>(type: "boolean", nullable: false),
                    pair_can_apply_your_moodles_to_you = table.Column<bool>(type: "boolean", nullable: false),
                    permanent_locks = table.Column<bool>(type: "boolean", nullable: false),
                    remove_gags = table.Column<bool>(type: "boolean", nullable: false),
                    remove_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    shock_collar_share_code = table.Column<string>(type: "text", nullable: true),
                    sit_requests = table.Column<bool>(type: "boolean", nullable: false),
                    start_char = table.Column<char>(type: "character(1)", nullable: false),
                    trigger_phrase = table.Column<string>(type: "text", nullable: true),
                    unlock_gags = table.Column<bool>(type: "boolean", nullable: false),
                    unlock_restraint_sets = table.Column<bool>(type: "boolean", nullable: false)
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
                    alias_requests_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    all_requests_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_negative_status_types_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_permanent_moodles_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_positive_status_types_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_removing_moodles_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    allow_special_status_types_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    apply_gags_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_execute_patterns_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_send_alarms_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_stop_patterns_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_alarms_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_toy_state_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_triggers_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_vibe_remote_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    devotional_locks_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    item_auto_equip_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    live_chat_garbler_active_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    live_chat_garbler_locked_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_gags_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_toybox_ui_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_allowed_restraint_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_gag_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_moodle_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    moodles_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    motion_requests_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    owner_locks_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    pair_can_apply_own_moodles_to_you_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    pair_can_apply_your_moodles_to_you_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    permanent_locks_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    puppeteer_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    remove_gags_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    remove_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    restraint_set_auto_equip_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    sit_requests_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    spatial_vibrator_audio_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    toybox_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    unlock_gags_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    unlock_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    wardrobe_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "user_global_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    allow_beeps = table.Column<bool>(type: "boolean", nullable: false),
                    allow_shocks = table.Column<bool>(type: "boolean", nullable: false),
                    allow_vibrations = table.Column<bool>(type: "boolean", nullable: false),
                    chat_boxes_hidden = table.Column<string>(type: "text", nullable: true),
                    chat_input_blocked = table.Column<string>(type: "text", nullable: true),
                    chat_input_hidden = table.Column<string>(type: "text", nullable: true),
                    forced_blindfold = table.Column<string>(type: "text", nullable: true),
                    forced_emote_state = table.Column<string>(type: "text", nullable: true),
                    forced_follow = table.Column<string>(type: "text", nullable: true),
                    forced_stay = table.Column<string>(type: "text", nullable: true),
                    global_allow_alias_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_allow_all_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_allow_motion_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_allow_sit_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_shock_share_code = table.Column<string>(type: "text", nullable: true),
                    global_shock_vibrate_duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    global_trigger_phrase = table.Column<string>(type: "text", nullable: true),
                    item_auto_equip = table.Column<bool>(type: "boolean", nullable: false),
                    live_chat_garbler_active = table.Column<bool>(type: "boolean", nullable: false),
                    live_chat_garbler_locked = table.Column<bool>(type: "boolean", nullable: false),
                    lock_toybox_ui = table.Column<bool>(type: "boolean", nullable: false),
                    max_duration = table.Column<int>(type: "integer", nullable: false),
                    max_intensity = table.Column<int>(type: "integer", nullable: false),
                    moodles_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    puppeteer_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    restraint_set_auto_equip = table.Column<bool>(type: "boolean", nullable: false),
                    spatial_vibrator_audio = table.Column<bool>(type: "boolean", nullable: false),
                    toy_is_active = table.Column<bool>(type: "boolean", nullable: false),
                    toybox_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    wardrobe_enabled = table.Column<bool>(type: "boolean", nullable: false)
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
        }
    }
}
