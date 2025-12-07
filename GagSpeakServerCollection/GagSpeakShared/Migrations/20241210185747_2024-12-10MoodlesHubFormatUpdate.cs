using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241210MoodlesHubFormatUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_likes_patterns_moodles_moodle_status_identifier",
                table: "likes_patterns");

            migrationBuilder.DropPrimaryKey(
                name: "pk_pattern_keywords",
                table: "pattern_keywords");

            migrationBuilder.DropPrimaryKey(
                name: "pk_moodle_keywords",
                table: "moodle_keywords");

            migrationBuilder.DropIndex(
                name: "ix_likes_patterns_moodle_status_identifier",
                table: "likes_patterns");

            migrationBuilder.DropColumn(
                name: "moodle_status_identifier",
                table: "likes_patterns");

            migrationBuilder.AddColumn<bool>(
                name: "as_permanent",
                table: "moodle_status",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "custom_vfx_path",
                table: "moodle_status",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "days",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "moodle_status",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "dispelable",
                table: "moodle_status",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "hours",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "icon_id",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "minutes",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "no_expire",
                table: "moodle_status",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "persistent",
                table: "moodle_status",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "seconds",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "stack_on_reapply",
                table: "moodle_status",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "stacks",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "status_on_dispell",
                table: "moodle_status",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "moodle_status",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "attached_message",
                table: "kinkster_pair_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_pattern_keywords",
                table: "pattern_keywords",
                columns: new[] { "pattern_entry_id", "keyword_word" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_moodle_keywords",
                table: "moodle_keywords",
                columns: new[] { "moodle_status_id", "keyword_word" });

            migrationBuilder.CreateIndex(
                name: "ix_pattern_entry_author",
                table: "pattern_entry",
                column: "author");

            migrationBuilder.CreateIndex(
                name: "ix_pattern_entry_name",
                table: "pattern_entry",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_moodle_status_author",
                table: "moodle_status",
                column: "author");

            migrationBuilder.CreateIndex(
                name: "ix_moodle_status_title",
                table: "moodle_status",
                column: "title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_pattern_keywords",
                table: "pattern_keywords");

            migrationBuilder.DropIndex(
                name: "ix_pattern_entry_author",
                table: "pattern_entry");

            migrationBuilder.DropIndex(
                name: "ix_pattern_entry_name",
                table: "pattern_entry");

            migrationBuilder.DropIndex(
                name: "ix_moodle_status_author",
                table: "moodle_status");

            migrationBuilder.DropIndex(
                name: "ix_moodle_status_title",
                table: "moodle_status");

            migrationBuilder.DropPrimaryKey(
                name: "pk_moodle_keywords",
                table: "moodle_keywords");

            migrationBuilder.DropColumn(
                name: "as_permanent",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "custom_vfx_path",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "days",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "description",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "dispelable",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "hours",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "icon_id",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "minutes",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "no_expire",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "persistent",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "seconds",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "stack_on_reapply",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "stacks",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "status_on_dispell",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "title",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "type",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "attached_message",
                table: "kinkster_pair_requests");

            migrationBuilder.AddColumn<Guid>(
                name: "moodle_status_identifier",
                table: "likes_patterns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_pattern_keywords",
                table: "pattern_keywords",
                column: "pattern_entry_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_moodle_keywords",
                table: "moodle_keywords",
                column: "moodle_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_likes_patterns_moodle_status_identifier",
                table: "likes_patterns",
                column: "moodle_status_identifier");

            migrationBuilder.AddForeignKey(
                name: "fk_likes_patterns_moodles_moodle_status_identifier",
                table: "likes_patterns",
                column: "moodle_status_identifier",
                principalTable: "moodle_status",
                principalColumn: "identifier");
        }
    }
}
