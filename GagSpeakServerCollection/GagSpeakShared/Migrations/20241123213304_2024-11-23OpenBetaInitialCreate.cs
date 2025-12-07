using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241123OpenBetaInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banned_registrations",
                columns: table => new
                {
                    discord_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_banned_registrations", x => x.discord_id);
                });

            migrationBuilder.CreateTable(
                name: "banned_users",
                columns: table => new
                {
                    character_identification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_uid = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_banned_users", x => x.character_identification);
                });

            migrationBuilder.CreateTable(
                name: "pattern_entry",
                columns: table => new
                {
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    publisher_uid = table.Column<string>(type: "text", nullable: false),
                    time_published = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    author = table.Column<string>(type: "text", nullable: true),
                    download_count = table.Column<int>(type: "integer", nullable: false),
                    should_loop = table.Column<bool>(type: "boolean", nullable: false),
                    length = table.Column<TimeSpan>(type: "interval", nullable: false),
                    uses_vibrations = table.Column<bool>(type: "boolean", nullable: false),
                    uses_rotations = table.Column<bool>(type: "boolean", nullable: false),
                    uses_oscillation = table.Column<bool>(type: "boolean", nullable: false),
                    base64pattern_data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pattern_entry", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "pattern_tags",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pattern_tags", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_logged_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    alias = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    vanity_tier = table.Column<int>(type: "integer", nullable: false),
                    profile_reporting_timed_out = table.Column<bool>(type: "boolean", nullable: false),
                    upload_limit_counter = table.Column<int>(type: "integer", nullable: false),
                    first_upload_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.uid);
                });

            migrationBuilder.CreateTable(
                name: "pattern_entry_tags",
                columns: table => new
                {
                    pattern_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pattern_entry_tags", x => new { x.pattern_entry_id, x.tag_name });
                    table.ForeignKey(
                        name: "fk_pattern_entry_tags_pattern_tags_tag_name",
                        column: x => x.tag_name,
                        principalTable: "pattern_tags",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pattern_entry_tags_patterns_pattern_entry_id",
                        column: x => x.pattern_entry_id,
                        principalTable: "pattern_entry",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_claim_auth",
                columns: table => new
                {
                    discord_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    initial_generated_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    verification_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_claim_auth", x => x.discord_id);
                    table.ForeignKey(
                        name: "fk_account_claim_auth_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                });

            migrationBuilder.CreateTable(
                name: "auth",
                columns: table => new
                {
                    hashed_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false),
                    primary_user_uid = table.Column<string>(type: "character varying(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth", x => x.hashed_key);
                    table.ForeignKey(
                        name: "fk_auth_users_primary_user_uid",
                        column: x => x.primary_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                    table.ForeignKey(
                        name: "fk_auth_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                });

            migrationBuilder.CreateTable(
                name: "client_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    gag_features = table.Column<bool>(type: "boolean", nullable: false),
                    owner_locks = table.Column<bool>(type: "boolean", nullable: false),
                    devotional_locks = table.Column<bool>(type: "boolean", nullable: false),
                    extended_lock_times = table.Column<bool>(type: "boolean", nullable: false),
                    max_lock_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    in_hardcore = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    lock_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    max_allowed_restraint_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    unlock_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    remove_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    trigger_phrase = table.Column<string>(type: "text", nullable: true),
                    start_char = table.Column<char>(type: "character(1)", nullable: false),
                    end_char = table.Column<char>(type: "character(1)", nullable: false),
                    allow_sit_requests = table.Column<bool>(type: "boolean", nullable: false),
                    allow_motion_requests = table.Column<bool>(type: "boolean", nullable: false),
                    allow_all_requests = table.Column<bool>(type: "boolean", nullable: false),
                    allow_positive_status_types = table.Column<bool>(type: "boolean", nullable: false),
                    allow_negative_status_types = table.Column<bool>(type: "boolean", nullable: false),
                    allow_special_status_types = table.Column<bool>(type: "boolean", nullable: false),
                    pair_can_apply_own_moodles_to_you = table.Column<bool>(type: "boolean", nullable: false),
                    pair_can_apply_your_moodles_to_you = table.Column<bool>(type: "boolean", nullable: false),
                    max_moodle_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    allow_permanent_moodles = table.Column<bool>(type: "boolean", nullable: false),
                    allow_removing_moodles = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_toy_state = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_vibe_remote = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_alarms = table.Column<bool>(type: "boolean", nullable: false),
                    can_send_alarms = table.Column<bool>(type: "boolean", nullable: false),
                    can_execute_patterns = table.Column<bool>(type: "boolean", nullable: false),
                    can_stop_patterns = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_triggers = table.Column<bool>(type: "boolean", nullable: false),
                    devotional_states_for_pair = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_follow = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_sit = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_emote = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_to_stay = table.Column<bool>(type: "boolean", nullable: false),
                    allow_blindfold = table.Column<bool>(type: "boolean", nullable: false),
                    allow_hiding_chat_boxes = table.Column<bool>(type: "boolean", nullable: false),
                    allow_hiding_chat_input = table.Column<bool>(type: "boolean", nullable: false),
                    allow_chat_input_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    shock_collar_share_code = table.Column<string>(type: "text", nullable: true),
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
                    live_chat_garbler_active_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    live_chat_garbler_locked_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    gag_features_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    owner_locks_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    devotional_locks_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    extended_lock_times_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_lock_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    wardrobe_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    item_auto_equip_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    restraint_set_auto_equip_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    apply_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    max_allowed_restraint_time_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    unlock_restraint_sets_allowed = table.Column<bool>(type: "boolean", nullable: false),
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
                    allow_removing_moodles_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    toybox_enabled_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    lock_toybox_ui_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    spatial_vibrator_audio_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_toy_state_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_vibe_remote_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_alarms_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_send_alarms_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_execute_patterns_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_stop_patterns_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    can_toggle_triggers_allowed = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "client_pairs",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    timestamp = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_pairs", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_client_pairs_users_other_user_uid",
                        column: x => x.other_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_pairs_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "private_rooms",
                columns: table => new
                {
                    name_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    host_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    time_made = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_private_rooms", x => x.name_id);
                    table.ForeignKey(
                        name: "fk_private_rooms_users_host_uid",
                        column: x => x.host_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                });

            migrationBuilder.CreateTable(
                name: "user_achievement_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    base64achievement_data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_achievement_data", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_achievement_data_users_user_uid",
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
                    active_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    active_set_enabler = table.Column<string>(type: "text", nullable: true),
                    active_set_pad_lock = table.Column<string>(type: "text", nullable: true),
                    active_set_password = table.Column<string>(type: "text", nullable: true),
                    active_set_lock_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    active_set_lock_assigner = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "user_global_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    safeword_used = table.Column<bool>(type: "boolean", nullable: false),
                    hardcore_safeword_used = table.Column<bool>(type: "boolean", nullable: false),
                    live_chat_garbler_active = table.Column<bool>(type: "boolean", nullable: false),
                    live_chat_garbler_locked = table.Column<bool>(type: "boolean", nullable: false),
                    wardrobe_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    item_auto_equip = table.Column<bool>(type: "boolean", nullable: false),
                    restraint_set_auto_equip = table.Column<bool>(type: "boolean", nullable: false),
                    puppeteer_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    global_trigger_phrase = table.Column<string>(type: "text", nullable: true),
                    global_allow_sit_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_allow_motion_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_allow_all_requests = table.Column<bool>(type: "boolean", nullable: false),
                    moodles_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    toybox_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lock_toybox_ui = table.Column<bool>(type: "boolean", nullable: false),
                    toy_is_active = table.Column<bool>(type: "boolean", nullable: false),
                    spatial_vibrator_audio = table.Column<bool>(type: "boolean", nullable: false),
                    forced_follow = table.Column<string>(type: "text", nullable: true),
                    forced_emote_state = table.Column<string>(type: "text", nullable: true),
                    forced_stay = table.Column<string>(type: "text", nullable: true),
                    forced_blindfold = table.Column<string>(type: "text", nullable: true),
                    chat_boxes_hidden = table.Column<string>(type: "text", nullable: true),
                    chat_input_hidden = table.Column<string>(type: "text", nullable: true),
                    chat_input_blocked = table.Column<string>(type: "text", nullable: true),
                    global_shock_share_code = table.Column<string>(type: "text", nullable: true),
                    allow_shocks = table.Column<bool>(type: "boolean", nullable: false),
                    allow_vibrations = table.Column<bool>(type: "boolean", nullable: false),
                    allow_beeps = table.Column<bool>(type: "boolean", nullable: false),
                    max_intensity = table.Column<int>(type: "integer", nullable: false),
                    max_duration = table.Column<int>(type: "integer", nullable: false),
                    global_shock_vibrate_duration = table.Column<TimeSpan>(type: "interval", nullable: false)
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
                name: "user_pattern_likes",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    pattern_entry_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_pattern_likes", x => new { x.user_uid, x.pattern_entry_id });
                    table.ForeignKey(
                        name: "fk_user_pattern_likes_patterns_pattern_entry_id",
                        column: x => x.pattern_entry_id,
                        principalTable: "pattern_entry",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_pattern_likes_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profile_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    profile_is_public = table.Column<bool>(type: "boolean", nullable: false),
                    flagged_for_report = table.Column<bool>(type: "boolean", nullable: false),
                    profile_disabled = table.Column<bool>(type: "boolean", nullable: false),
                    warning_strike_count = table.Column<int>(type: "integer", nullable: false),
                    base64profile_pic = table.Column<string>(type: "text", nullable: true),
                    user_description = table.Column<string>(type: "text", nullable: true),
                    completed_achievements_total = table.Column<int>(type: "integer", nullable: false),
                    chosen_title_id = table.Column<int>(type: "integer", nullable: false),
                    plate_background = table.Column<int>(type: "integer", nullable: false),
                    plate_border = table.Column<int>(type: "integer", nullable: false),
                    profile_picture_border = table.Column<int>(type: "integer", nullable: false),
                    profile_picture_overlay = table.Column<int>(type: "integer", nullable: false),
                    description_background = table.Column<int>(type: "integer", nullable: false),
                    description_border = table.Column<int>(type: "integer", nullable: false),
                    description_overlay = table.Column<int>(type: "integer", nullable: false),
                    gag_slot_background = table.Column<int>(type: "integer", nullable: false),
                    gag_slot_border = table.Column<int>(type: "integer", nullable: false),
                    gag_slot_overlay = table.Column<int>(type: "integer", nullable: false),
                    padlock_background = table.Column<int>(type: "integer", nullable: false),
                    padlock_border = table.Column<int>(type: "integer", nullable: false),
                    padlock_overlay = table.Column<int>(type: "integer", nullable: false),
                    blocked_slots_background = table.Column<int>(type: "integer", nullable: false),
                    blocked_slots_border = table.Column<int>(type: "integer", nullable: false),
                    blocked_slots_overlay = table.Column<int>(type: "integer", nullable: false),
                    blocked_slot_border = table.Column<int>(type: "integer", nullable: false),
                    blocked_slot_overlay = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profile_data", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_profile_data_users_user_uid",
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
                    reported_base64picture = table.Column<string>(type: "text", nullable: true),
                    reported_description = table.Column<string>(type: "text", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "private_room_users",
                columns: table => new
                {
                    private_room_name_id = table.Column<string>(type: "character varying(50)", nullable: false),
                    private_room_user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    chat_alias = table.Column<string>(type: "text", nullable: true),
                    in_room = table.Column<bool>(type: "boolean", nullable: false),
                    allowing_vibe = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_private_room_users", x => new { x.private_room_name_id, x.private_room_user_uid });
                    table.ForeignKey(
                        name: "fk_private_room_users_private_rooms_private_room_name_id",
                        column: x => x.private_room_name_id,
                        principalTable: "private_rooms",
                        principalColumn: "name_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_private_room_users_users_private_room_user_uid",
                        column: x => x.private_room_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_claim_auth_user_uid",
                table: "account_claim_auth",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_auth_primary_user_uid",
                table: "auth",
                column: "primary_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_auth_user_uid",
                table: "auth",
                column: "user_uid");

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
                name: "ix_client_pairs_other_user_uid",
                table: "client_pairs",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_client_pairs_user_uid",
                table: "client_pairs",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_pattern_entry_tags_pattern_entry_id",
                table: "pattern_entry_tags",
                column: "pattern_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_pattern_entry_tags_tag_name",
                table: "pattern_entry_tags",
                column: "tag_name");

            migrationBuilder.CreateIndex(
                name: "ix_pattern_tags_name",
                table: "pattern_tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_private_room_users_private_room_name_id",
                table: "private_room_users",
                column: "private_room_name_id");

            migrationBuilder.CreateIndex(
                name: "ix_private_room_users_private_room_user_uid",
                table: "private_room_users",
                column: "private_room_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_private_rooms_host_uid",
                table: "private_rooms",
                column: "host_uid");

            migrationBuilder.CreateIndex(
                name: "ix_private_rooms_name_id",
                table: "private_rooms",
                column: "name_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_pattern_likes_pattern_entry_id",
                table: "user_pattern_likes",
                column: "pattern_entry_id");

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
                name: "account_claim_auth");

            migrationBuilder.DropTable(
                name: "auth");

            migrationBuilder.DropTable(
                name: "banned_registrations");

            migrationBuilder.DropTable(
                name: "banned_users");

            migrationBuilder.DropTable(
                name: "client_pair_permissions");

            migrationBuilder.DropTable(
                name: "client_pair_permissions_access");

            migrationBuilder.DropTable(
                name: "client_pairs");

            migrationBuilder.DropTable(
                name: "pattern_entry_tags");

            migrationBuilder.DropTable(
                name: "private_room_users");

            migrationBuilder.DropTable(
                name: "user_achievement_data");

            migrationBuilder.DropTable(
                name: "user_active_state_data");

            migrationBuilder.DropTable(
                name: "user_appearance_data");

            migrationBuilder.DropTable(
                name: "user_global_permissions");

            migrationBuilder.DropTable(
                name: "user_pattern_likes");

            migrationBuilder.DropTable(
                name: "user_profile_data");

            migrationBuilder.DropTable(
                name: "user_profile_data_reports");

            migrationBuilder.DropTable(
                name: "pattern_tags");

            migrationBuilder.DropTable(
                name: "private_rooms");

            migrationBuilder.DropTable(
                name: "pattern_entry");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
