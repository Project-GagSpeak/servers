using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241018HardcorePermissionsV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "force_lock_first_person",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "is_forced_to_stay",
                table: "client_pair_permissions",
                newName: "devotional_states_for_pair");

            migrationBuilder.RenameColumn(
                name: "is_forced_to_sit",
                table: "client_pair_permissions",
                newName: "allow_hiding_chatboxes");

            migrationBuilder.RenameColumn(
                name: "is_forced_to_follow",
                table: "client_pair_permissions",
                newName: "allow_hiding_chat_input");

            migrationBuilder.RenameColumn(
                name: "is_blindfolded",
                table: "client_pair_permissions",
                newName: "allow_chat_input_blocking");

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
                name: "chatboxes_hidden",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "forced_blindfold",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "forced_follow",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "forced_groundsit",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "forced_sit",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "forced_stay",
                table: "user_global_permissions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chat_input_blocked",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "chat_input_hidden",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "chatboxes_hidden",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "forced_blindfold",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "forced_follow",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "forced_groundsit",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "forced_sit",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "forced_stay",
                table: "user_global_permissions");

            migrationBuilder.RenameColumn(
                name: "devotional_states_for_pair",
                table: "client_pair_permissions",
                newName: "is_forced_to_stay");

            migrationBuilder.RenameColumn(
                name: "allow_hiding_chatboxes",
                table: "client_pair_permissions",
                newName: "is_forced_to_sit");

            migrationBuilder.RenameColumn(
                name: "allow_hiding_chat_input",
                table: "client_pair_permissions",
                newName: "is_forced_to_follow");

            migrationBuilder.RenameColumn(
                name: "allow_chat_input_blocking",
                table: "client_pair_permissions",
                newName: "is_blindfolded");

            migrationBuilder.AddColumn<bool>(
                name: "force_lock_first_person",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
