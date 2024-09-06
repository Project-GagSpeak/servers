using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240905NewPatternHubIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "first_upload_timestamp",
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

            migrationBuilder.CreateTable(
                name: "pattern_entry",
                columns: table => new
                {
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    publisher_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    time_published = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    author = table.Column<string>(type: "text", nullable: true),
                    download_count = table.Column<int>(type: "integer", nullable: false),
                    like_count = table.Column<int>(type: "integer", nullable: false),
                    length = table.Column<TimeSpan>(type: "interval", nullable: false),
                    uses_vibrations = table.Column<bool>(type: "boolean", nullable: false),
                    uses_rotations = table.Column<bool>(type: "boolean", nullable: false),
                    uses_oscillation = table.Column<bool>(type: "boolean", nullable: false),
                    base64pattern_data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pattern_entry", x => x.identifier);
                    table.ForeignKey(
                        name: "fk_pattern_entry_users_publisher_uid",
                        column: x => x.publisher_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "ix_pattern_entry_publisher_uid",
                table: "pattern_entry",
                column: "publisher_uid");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pattern_entry_tags");

            migrationBuilder.DropTable(
                name: "pattern_tags");

            migrationBuilder.DropTable(
                name: "pattern_entry");

            migrationBuilder.DropColumn(
                name: "first_upload_timestamp",
                table: "users");

            migrationBuilder.DropColumn(
                name: "upload_limit_counter",
                table: "users");
        }
    }
}
