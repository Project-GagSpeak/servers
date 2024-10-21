using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241021WardrobeDtoChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "wardrobe_active_set_password",
                table: "user_active_state_data",
                newName: "active_set_password");

            migrationBuilder.RenameColumn(
                name: "wardrobe_active_set_pad_lock",
                table: "user_active_state_data",
                newName: "active_set_pad_lock");

            migrationBuilder.RenameColumn(
                name: "wardrobe_active_set_name",
                table: "user_active_state_data",
                newName: "active_set_name");

            migrationBuilder.RenameColumn(
                name: "wardrobe_active_set_lock_time",
                table: "user_active_state_data",
                newName: "active_set_lock_time");

            migrationBuilder.RenameColumn(
                name: "wardrobe_active_set_lock_assigner",
                table: "user_active_state_data",
                newName: "active_set_lock_assigner");

            migrationBuilder.RenameColumn(
                name: "wardrobe_active_set_assigner",
                table: "user_active_state_data",
                newName: "active_set_enabler");

            migrationBuilder.RenameColumn(
                name: "toybox_active_pattern_id",
                table: "user_active_state_data",
                newName: "active_set_id");

            migrationBuilder.AddColumn<Guid>(
                name: "active_pattern_id",
                table: "user_active_state_data",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "active_pattern_id",
                table: "user_active_state_data");

            migrationBuilder.RenameColumn(
                name: "active_set_password",
                table: "user_active_state_data",
                newName: "wardrobe_active_set_password");

            migrationBuilder.RenameColumn(
                name: "active_set_pad_lock",
                table: "user_active_state_data",
                newName: "wardrobe_active_set_pad_lock");

            migrationBuilder.RenameColumn(
                name: "active_set_name",
                table: "user_active_state_data",
                newName: "wardrobe_active_set_name");

            migrationBuilder.RenameColumn(
                name: "active_set_lock_time",
                table: "user_active_state_data",
                newName: "wardrobe_active_set_lock_time");

            migrationBuilder.RenameColumn(
                name: "active_set_lock_assigner",
                table: "user_active_state_data",
                newName: "wardrobe_active_set_lock_assigner");

            migrationBuilder.RenameColumn(
                name: "active_set_enabler",
                table: "user_active_state_data",
                newName: "wardrobe_active_set_assigner");

            migrationBuilder.RenameColumn(
                name: "active_set_id",
                table: "user_active_state_data",
                newName: "toybox_active_pattern_id");
        }
    }
}
