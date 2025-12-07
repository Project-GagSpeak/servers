using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20250817PermissionOverhaul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chat_boxes_hidden",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "chat_input_blocked",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "chat_input_hidden",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "hypnosis_custom_effect",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "imprisonment",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "indoor_confinement",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "locked_emote_state",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "locked_following",
                table: "user_global_permissions");

            migrationBuilder.CreateTable(
                name: "kinkster_collar_requests",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    initial_writing = table.Column<string>(type: "text", nullable: true),
                    other_user_access = table.Column<byte>(type: "smallint", nullable: false),
                    owner_access = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kinkster_collar_requests", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_kinkster_collar_requests_users_other_user_uid",
                        column: x => x.other_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_kinkster_collar_requests_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_collar_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    visuals = table.Column<bool>(type: "boolean", nullable: false),
                    dye1 = table.Column<byte>(type: "smallint", nullable: false),
                    dye2 = table.Column<byte>(type: "smallint", nullable: false),
                    moodle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    moodle_icon_id = table.Column<int>(type: "integer", nullable: false),
                    moodle_title = table.Column<string>(type: "text", nullable: true),
                    moodle_description = table.Column<string>(type: "text", nullable: true),
                    moodle_type = table.Column<byte>(type: "smallint", nullable: false),
                    moodle_vfx_path = table.Column<string>(type: "text", nullable: true),
                    writing = table.Column<string>(type: "text", nullable: true),
                    edit_access = table.Column<byte>(type: "smallint", nullable: false),
                    owner_edit_access = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_collar_data", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_collar_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_hardcore_state",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    locked_following = table.Column<string>(type: "text", nullable: true),
                    locked_emote_state = table.Column<string>(type: "text", nullable: true),
                    emote_expire_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    emote_id = table.Column<int>(type: "integer", nullable: false),
                    emote_cycle_pose = table.Column<byte>(type: "smallint", nullable: false),
                    indoor_confinement = table.Column<string>(type: "text", nullable: true),
                    confinement_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confined_world = table.Column<int>(type: "integer", nullable: false),
                    confined_city = table.Column<int>(type: "integer", nullable: false),
                    confined_ward = table.Column<int>(type: "integer", nullable: false),
                    confined_place_id = table.Column<int>(type: "integer", nullable: false),
                    confined_in_apartment = table.Column<bool>(type: "boolean", nullable: false),
                    confined_in_subdivision = table.Column<bool>(type: "boolean", nullable: false),
                    imprisonment = table.Column<string>(type: "text", nullable: true),
                    imprisonment_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    imprisoned_territory = table.Column<short>(type: "smallint", nullable: false),
                    imprisoned_pos_x = table.Column<float>(type: "real", nullable: false),
                    imprisoned_pos_y = table.Column<float>(type: "real", nullable: false),
                    imprisoned_pos_z = table.Column<float>(type: "real", nullable: false),
                    imprisoned_radius = table.Column<float>(type: "real", nullable: false),
                    chat_boxes_hidden = table.Column<string>(type: "text", nullable: true),
                    chat_boxes_hidden_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    chat_input_hidden = table.Column<string>(type: "text", nullable: true),
                    chat_input_hidden_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    chat_input_blocked = table.Column<string>(type: "text", nullable: true),
                    chat_input_blocked_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    hypnotic_effect = table.Column<string>(type: "text", nullable: true),
                    hypnotic_effect_timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_hardcore_state", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_hardcore_state_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "collar_owner",
                columns: table => new
                {
                    owner_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    collared_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collar_owner", x => new { x.owner_uid, x.collared_user_uid });
                    table.ForeignKey(
                        name: "fk_collar_owner_user_collar_data_collared_user_uid",
                        column: x => x.collared_user_uid,
                        principalTable: "user_collar_data",
                        principalColumn: "user_uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_collar_owner_users_owner_uid",
                        column: x => x.owner_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_global_permissions_user_uid",
                table: "user_global_permissions",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_likes_patterns_user_uid",
                table: "likes_patterns",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_likes_moodles_user_uid",
                table: "likes_moodles",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_collar_owner_collared_user_uid",
                table: "collar_owner",
                column: "collared_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_collar_owner_owner_uid",
                table: "collar_owner",
                column: "owner_uid");

            migrationBuilder.CreateIndex(
                name: "ix_kinkster_collar_requests_other_user_uid",
                table: "kinkster_collar_requests",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_kinkster_collar_requests_user_uid",
                table: "kinkster_collar_requests",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_collar_data_user_uid",
                table: "user_collar_data",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_hardcore_state_user_uid",
                table: "user_hardcore_state",
                column: "user_uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "collar_owner");

            migrationBuilder.DropTable(
                name: "kinkster_collar_requests");

            migrationBuilder.DropTable(
                name: "user_hardcore_state");

            migrationBuilder.DropTable(
                name: "user_collar_data");

            migrationBuilder.DropIndex(
                name: "ix_user_global_permissions_user_uid",
                table: "user_global_permissions");

            migrationBuilder.DropIndex(
                name: "ix_likes_patterns_user_uid",
                table: "likes_patterns");

            migrationBuilder.DropIndex(
                name: "ix_likes_moodles_user_uid",
                table: "likes_moodles");

            migrationBuilder.AddColumn<string>(
                name: "chat_boxes_hidden",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "chat_input_blocked",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "chat_input_hidden",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hypnosis_custom_effect",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imprisonment",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "indoor_confinement",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locked_emote_state",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locked_following",
                table: "user_global_permissions",
                type: "text",
                nullable: true);
        }
    }
}
