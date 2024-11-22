using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241111AccountBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "timestamp",
                table: "banned_users");

            migrationBuilder.AddColumn<string>(
                name: "user_uid",
                table: "banned_users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user_uid",
                table: "banned_users");

            migrationBuilder.AddColumn<byte[]>(
                name: "timestamp",
                table: "banned_users",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }
    }
}
