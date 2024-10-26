using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable MA0051, MA0048 // Method is too long

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                    reason = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_banned_users", x => x.character_identification);
                });

            migrationBuilder.CreateTable(
                name: "puppeteer_global_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "text", nullable: false),
                    enable_puppeteer = table.Column<bool>(type: "boolean", nullable: false),
                    global_trigger_phrase = table.Column<string>(type: "text", nullable: true),
                    global_allow_sit_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_allow_motion_requests = table.Column<bool>(type: "boolean", nullable: false),
                    global_allow_all_requests = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_puppeteer_global_permissions", x => x.user_uid);
                });

            migrationBuilder.CreateTable(
                name: "toybox_global_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "text", nullable: false),
                    enable_toybox = table.Column<bool>(type: "boolean", nullable: false),
                    lock_toybox_ui = table.Column<bool>(type: "boolean", nullable: false),
                    toy_is_active = table.Column<bool>(type: "boolean", nullable: false),
                    toy_intensity = table.Column<int>(type: "integer", nullable: false),
                    using_simulated_vibrator = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_toybox_global_permissions", x => x.user_uid);
                });

            migrationBuilder.CreateTable(
                name: "user_apperance_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_user_apperance_data", x => x.user_uid);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    timestamp = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    is_moderator = table.Column<bool>(type: "boolean", nullable: false),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false),
                    last_logged_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    alias = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.uid);
                });

            migrationBuilder.CreateTable(
                name: "wardrobe_global_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "text", nullable: false),
                    enable_wardrobe = table.Column<bool>(type: "boolean", nullable: false),
                    item_auto_equip = table.Column<bool>(type: "boolean", nullable: false),
                    restraint_set_auto_equip = table.Column<bool>(type: "boolean", nullable: false),
                    lock_gag_storage_on_gag_lock = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wardrobe_global_permissions", x => x.user_uid);
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
                name: "hardcore_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    allow_forced_follow = table.Column<bool>(type: "boolean", nullable: false),
                    is_forced_to_follow = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_sit = table.Column<bool>(type: "boolean", nullable: false),
                    is_forced_to_sit = table.Column<bool>(type: "boolean", nullable: false),
                    allow_forced_to_stay = table.Column<bool>(type: "boolean", nullable: false),
                    is_forced_to_stay = table.Column<bool>(type: "boolean", nullable: false),
                    allow_blindfold = table.Column<bool>(type: "boolean", nullable: false),
                    force_lock_first_person = table.Column<bool>(type: "boolean", nullable: false),
                    is_blindfoldeded = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "puppeteer_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    trigger_phrase = table.Column<string>(type: "text", nullable: true),
                    start_char = table.Column<char>(type: "character(1)", nullable: false),
                    end_char = table.Column<char>(type: "character(1)", nullable: false),
                    allow_sit_requests = table.Column<bool>(type: "boolean", nullable: false),
                    allow_motion_requests = table.Column<bool>(type: "boolean", nullable: false),
                    allow_all_requests = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "toybox_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    can_change_toy_state = table.Column<bool>(type: "boolean", nullable: false),
                    intensity_control = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_patterns = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_triggers = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "user_profile_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    base64profile_pic = table.Column<string>(type: "text", nullable: true),
                    flagged_for_report = table.Column<bool>(type: "boolean", nullable: false),
                    profile_disabled = table.Column<bool>(type: "boolean", nullable: false),
                    user_description = table.Column<string>(type: "text", nullable: true)
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
                name: "wardrobe_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    toggle_restraint_sets = table.Column<bool>(type: "boolean", nullable: false),
                    lock_restraint_sets = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "user_settings_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    safeword = table.Column<string>(type: "text", nullable: true),
                    safeword_used = table.Column<bool>(type: "boolean", nullable: false),
                    cmds_from_friends = table.Column<bool>(type: "boolean", nullable: false),
                    cmds_from_party = table.Column<bool>(type: "boolean", nullable: false),
                    direct_chat_garbler_active = table.Column<bool>(type: "boolean", nullable: false),
                    direct_chat_garbler_locked = table.Column<bool>(type: "boolean", nullable: false),
                    live_garbler_zone_change_warn = table.Column<bool>(type: "boolean", nullable: false),
                    revert_style = table.Column<int>(type: "integer", nullable: false),
                    user_apperance_data_user_uid = table.Column<string>(type: "text", nullable: true),
                    wardrobe_global_permissions_user_uid = table.Column<string>(type: "text", nullable: true),
                    puppeteer_global_permissions_user_uid = table.Column<string>(type: "text", nullable: true),
                    toybox_global_permissions_user_uid = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_settings_data", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_settings_data_puppeteer_global_permissions_puppeteer_g",
                        column: x => x.puppeteer_global_permissions_user_uid,
                        principalTable: "puppeteer_global_permissions",
                        principalColumn: "user_uid");
                    table.ForeignKey(
                        name: "fk_user_settings_data_toybox_global_permissions_toybox_global_",
                        column: x => x.toybox_global_permissions_user_uid,
                        principalTable: "toybox_global_permissions",
                        principalColumn: "user_uid");
                    table.ForeignKey(
                        name: "fk_user_settings_data_user_apperance_data_user_apperance_data_",
                        column: x => x.user_apperance_data_user_uid,
                        principalTable: "user_apperance_data",
                        principalColumn: "user_uid");
                    table.ForeignKey(
                        name: "fk_user_settings_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_settings_data_wardrobe_global_permissions_wardrobe_glo",
                        column: x => x.wardrobe_global_permissions_user_uid,
                        principalTable: "wardrobe_global_permissions",
                        principalColumn: "user_uid");
                });

            migrationBuilder.CreateTable(
                name: "client_pair_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    extended_lock_times = table.Column<bool>(type: "boolean", nullable: false),
                    in_hardcore = table.Column<bool>(type: "boolean", nullable: false),
                    wardrobe_pair_permissions_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    wardrobe_pair_permissions_other_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    puppeteer_pair_permissions_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    puppeteer_pair_permissions_other_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    toybox_pair_permissions_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    toybox_pair_permissions_other_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    hardcore_pair_permissions_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    hardcore_pair_permissions_other_user_uid = table.Column<string>(type: "character varying(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_pair_permissions", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_client_pair_permissions_hardcore_pair_permissions_hardcore_",
                        columns: x => new { x.hardcore_pair_permissions_user_uid, x.hardcore_pair_permissions_other_user_uid },
                        principalTable: "hardcore_pair_permissions",
                        principalColumns: new[] { "user_uid", "other_user_uid" });
                    table.ForeignKey(
                        name: "fk_client_pair_permissions_puppeteer_pair_permissions_puppetee",
                        columns: x => new { x.puppeteer_pair_permissions_user_uid, x.puppeteer_pair_permissions_other_user_uid },
                        principalTable: "puppeteer_pair_permissions",
                        principalColumns: new[] { "user_uid", "other_user_uid" });
                    table.ForeignKey(
                        name: "fk_client_pair_permissions_toybox_pair_permissions_toybox_pair",
                        columns: x => new { x.toybox_pair_permissions_user_uid, x.toybox_pair_permissions_other_user_uid },
                        principalTable: "toybox_pair_permissions",
                        principalColumns: new[] { "user_uid", "other_user_uid" });
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
                    table.ForeignKey(
                        name: "fk_client_pair_permissions_wardrobe_pair_permissions_wardrobe_",
                        columns: x => new { x.wardrobe_pair_permissions_user_uid, x.wardrobe_pair_permissions_other_user_uid },
                        principalTable: "wardrobe_pair_permissions",
                        principalColumns: new[] { "user_uid", "other_user_uid" });
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
                name: "ix_client_pair_permissions_hardcore_pair_permissions_user_uid_",
                table: "client_pair_permissions",
                columns: new[] { "hardcore_pair_permissions_user_uid", "hardcore_pair_permissions_other_user_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_other_user_uid",
                table: "client_pair_permissions",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_puppeteer_pair_permissions_user_uid",
                table: "client_pair_permissions",
                columns: new[] { "puppeteer_pair_permissions_user_uid", "puppeteer_pair_permissions_other_user_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_toybox_pair_permissions_user_uid_to",
                table: "client_pair_permissions",
                columns: new[] { "toybox_pair_permissions_user_uid", "toybox_pair_permissions_other_user_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_user_uid",
                table: "client_pair_permissions",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_client_pair_permissions_wardrobe_pair_permissions_user_uid_",
                table: "client_pair_permissions",
                columns: new[] { "wardrobe_pair_permissions_user_uid", "wardrobe_pair_permissions_other_user_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_client_pairs_other_user_uid",
                table: "client_pairs",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_client_pairs_user_uid",
                table: "client_pairs",
                column: "user_uid");

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
                name: "ix_wardrobe_pair_permissions_other_user_uid",
                table: "wardrobe_pair_permissions",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_wardrobe_pair_permissions_user_uid",
                table: "wardrobe_pair_permissions",
                column: "user_uid");
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
                name: "client_pairs");

            migrationBuilder.DropTable(
                name: "user_profile_data");

            migrationBuilder.DropTable(
                name: "user_settings_data");

            migrationBuilder.DropTable(
                name: "hardcore_pair_permissions");

            migrationBuilder.DropTable(
                name: "puppeteer_pair_permissions");

            migrationBuilder.DropTable(
                name: "toybox_pair_permissions");

            migrationBuilder.DropTable(
                name: "wardrobe_pair_permissions");

            migrationBuilder.DropTable(
                name: "puppeteer_global_permissions");

            migrationBuilder.DropTable(
                name: "toybox_global_permissions");

            migrationBuilder.DropTable(
                name: "user_apperance_data");

            migrationBuilder.DropTable(
                name: "wardrobe_global_permissions");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}

#pragma warning restore MA0051, MA0048 // Method is too long

