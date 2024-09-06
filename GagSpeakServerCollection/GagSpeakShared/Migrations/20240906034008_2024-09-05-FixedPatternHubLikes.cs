using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240905FixedPatternHubLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "like_count",
                table: "pattern_entry");

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

            migrationBuilder.CreateIndex(
                name: "ix_user_pattern_likes_pattern_entry_id",
                table: "user_pattern_likes",
                column: "pattern_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_pattern_likes_user_uid",
                table: "user_pattern_likes",
                column: "user_uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_pattern_likes");

            migrationBuilder.AddColumn<int>(
                name: "like_count",
                table: "pattern_entry",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
