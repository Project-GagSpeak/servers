using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241171UnbindUsersFromPatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pattern_entry_users_publisher_uid",
                table: "pattern_entry");

            migrationBuilder.DropIndex(
                name: "ix_pattern_entry_publisher_uid",
                table: "pattern_entry");

            migrationBuilder.DropColumn(
                name: "safeword",
                table: "user_global_permissions");

            migrationBuilder.AlterColumn<string>(
                name: "publisher_uid",
                table: "pattern_entry",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "safeword",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "publisher_uid",
                table: "pattern_entry",
                type: "character varying(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "ix_pattern_entry_publisher_uid",
                table: "pattern_entry",
                column: "publisher_uid");

            migrationBuilder.AddForeignKey(
                name: "fk_pattern_entry_users_publisher_uid",
                table: "pattern_entry",
                column: "publisher_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
