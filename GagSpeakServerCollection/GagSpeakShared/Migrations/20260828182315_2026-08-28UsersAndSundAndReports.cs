using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20260828UsersAndSundAndReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reported_chats_users_reported_user_uid",
                table: "reported_chats");

            migrationBuilder.DropForeignKey(
                name: "fk_reported_chats_users_reporting_user_uid",
                table: "reported_chats");

            migrationBuilder.DropIndex(
                name: "ix_reported_chats_reported_user_uid",
                table: "reported_chats");

            migrationBuilder.DropColumn(
                name: "profile_is_public",
                table: "user_profile_data");

            migrationBuilder.DropColumn(
                name: "type",
                table: "reported_profiles");

            migrationBuilder.DropColumn(
                name: "compressed_chat_history",
                table: "reported_chats");

            migrationBuilder.DropColumn(
                name: "preferred_nickname",
                table: "kinkster_pair_requests");

            migrationBuilder.RenameColumn(
                name: "snapshot_image",
                table: "reported_profiles",
                newName: "snapshot_json");

            migrationBuilder.RenameColumn(
                name: "snapshot_description",
                table: "reported_profiles",
                newName: "snapshot_icon");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "reported_chats",
                newName: "chat");

            migrationBuilder.RenameColumn(
                name: "reporting_user_uid",
                table: "reported_chats",
                newName: "reporter_uid");

            migrationBuilder.RenameIndex(
                name: "ix_reported_chats_reporting_user_uid",
                table: "reported_chats",
                newName: "ix_reported_chats_reporter_uid");

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "name_color",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "name_glow_color",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_bg",
                table: "reported_profiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "snapshot_is_nsfw",
                table: "reported_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "reported_user_uid",
                table: "reported_chats",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "chat_context_json",
                table: "reported_chats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "chat_log_id",
                table: "reported_chats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "message",
                table: "reported_chats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "message_id",
                table: "reported_chats",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "other_user_uid",
                table: "kinkster_pair_requests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10)
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<string>(
                name: "user_uid",
                table: "kinkster_pair_requests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10)
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddColumn<int>(
                name: "radar_strikes",
                table: "account_reputation",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "radar_timeout",
                table: "account_reputation",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "radar_usage",
                table: "account_reputation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_reported_chats_chat",
                table: "reported_chats",
                column: "chat");

            migrationBuilder.CreateIndex(
                name: "ix_reported_chats_chat_log_id",
                table: "reported_chats",
                column: "chat_log_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reported_chats_users_reporter_uid",
                table: "reported_chats",
                column: "reporter_uid",
                principalTable: "users",
                principalColumn: "uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reported_chats_users_reporter_uid",
                table: "reported_chats");

            migrationBuilder.DropIndex(
                name: "ix_reported_chats_chat",
                table: "reported_chats");

            migrationBuilder.DropIndex(
                name: "ix_reported_chats_chat_log_id",
                table: "reported_chats");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "name_color",
                table: "users");

            migrationBuilder.DropColumn(
                name: "name_glow_color",
                table: "users");

            migrationBuilder.DropColumn(
                name: "snapshot_bg",
                table: "reported_profiles");

            migrationBuilder.DropColumn(
                name: "snapshot_is_nsfw",
                table: "reported_profiles");

            migrationBuilder.DropColumn(
                name: "chat_context_json",
                table: "reported_chats");

            migrationBuilder.DropColumn(
                name: "chat_log_id",
                table: "reported_chats");

            migrationBuilder.DropColumn(
                name: "message",
                table: "reported_chats");

            migrationBuilder.DropColumn(
                name: "message_id",
                table: "reported_chats");

            migrationBuilder.DropColumn(
                name: "radar_strikes",
                table: "account_reputation");

            migrationBuilder.DropColumn(
                name: "radar_timeout",
                table: "account_reputation");

            migrationBuilder.DropColumn(
                name: "radar_usage",
                table: "account_reputation");

            migrationBuilder.RenameColumn(
                name: "snapshot_json",
                table: "reported_profiles",
                newName: "snapshot_image");

            migrationBuilder.RenameColumn(
                name: "snapshot_icon",
                table: "reported_profiles",
                newName: "snapshot_description");

            migrationBuilder.RenameColumn(
                name: "reporter_uid",
                table: "reported_chats",
                newName: "reporting_user_uid");

            migrationBuilder.RenameColumn(
                name: "chat",
                table: "reported_chats",
                newName: "type");

            migrationBuilder.RenameIndex(
                name: "ix_reported_chats_reporter_uid",
                table: "reported_chats",
                newName: "ix_reported_chats_reporting_user_uid");

            migrationBuilder.AddColumn<bool>(
                name: "profile_is_public",
                table: "user_profile_data",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "reported_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "reported_user_uid",
                table: "reported_chats",
                type: "character varying(10)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "compressed_chat_history",
                table: "reported_chats",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "other_user_uid",
                table: "kinkster_pair_requests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10)
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<string>(
                name: "user_uid",
                table: "kinkster_pair_requests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10)
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddColumn<string>(
                name: "preferred_nickname",
                table: "kinkster_pair_requests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_reported_chats_reported_user_uid",
                table: "reported_chats",
                column: "reported_user_uid");

            migrationBuilder.AddForeignKey(
                name: "fk_reported_chats_users_reported_user_uid",
                table: "reported_chats",
                column: "reported_user_uid",
                principalTable: "users",
                principalColumn: "uid");

            migrationBuilder.AddForeignKey(
                name: "fk_reported_chats_users_reporting_user_uid",
                table: "reported_chats",
                column: "reporting_user_uid",
                principalTable: "users",
                principalColumn: "uid");
        }
    }
}
