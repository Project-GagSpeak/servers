using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240714NewPatternsAndPerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "private_room_users");

            migrationBuilder.DropTable(
                name: "private_rooms");

            migrationBuilder.DropColumn(
                name: "toys_are_connected",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "toys_are_in_use",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "uses_rotations",
                table: "pattern_entry");

            migrationBuilder.DropColumn(
                name: "uses_vibrations",
                table: "pattern_entry");

            migrationBuilder.AddColumn<int>(
                name: "active_remote_mode",
                table: "user_global_permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "motors_used",
                table: "pattern_entry",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "primary_device_used",
                table: "pattern_entry",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "secondary_device_used",
                table: "pattern_entry",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "pattern_entry",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "active_remote_mode",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "motors_used",
                table: "pattern_entry");

            migrationBuilder.DropColumn(
                name: "primary_device_used",
                table: "pattern_entry");

            migrationBuilder.DropColumn(
                name: "secondary_device_used",
                table: "pattern_entry");

            migrationBuilder.DropColumn(
                name: "version",
                table: "pattern_entry");

            migrationBuilder.AddColumn<bool>(
                name: "toys_are_connected",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "toys_are_in_use",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "uses_rotations",
                table: "pattern_entry",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "uses_vibrations",
                table: "pattern_entry",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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
                name: "private_room_users",
                columns: table => new
                {
                    private_room_name_id = table.Column<string>(type: "character varying(50)", nullable: false),
                    private_room_user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    allowing_vibe = table.Column<bool>(type: "boolean", nullable: false),
                    chat_alias = table.Column<string>(type: "text", nullable: true),
                    in_room = table.Column<bool>(type: "boolean", nullable: false)
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
        }
    }
}
