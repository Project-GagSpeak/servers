using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241025HardcoreEmoteState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "forced_groundsit",
                table: "user_global_permissions");

            migrationBuilder.RenameColumn(
                name: "forced_sit",
                table: "user_global_permissions",
                newName: "forced_emote_state");

            migrationBuilder.AddColumn<bool>(
                name: "allow_forced_emote",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_forced_emote",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "forced_emote_state",
                table: "user_global_permissions",
                newName: "forced_sit");

            migrationBuilder.AddColumn<string>(
                name: "forced_groundsit",
                table: "user_global_permissions",
                type: "text",
                nullable: true);
        }
    }
}
