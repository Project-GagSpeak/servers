using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240114DevloperHellUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "active_set_password",
                table: "user_active_state_data",
                newName: "password");

            migrationBuilder.RenameColumn(
                name: "active_set_pad_lock",
                table: "user_active_state_data",
                newName: "padlock");

            migrationBuilder.RenameColumn(
                name: "active_set_lock_time",
                table: "user_active_state_data",
                newName: "timer");

            migrationBuilder.RenameColumn(
                name: "active_set_lock_assigner",
                table: "user_active_state_data",
                newName: "assigner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "timer",
                table: "user_active_state_data",
                newName: "active_set_lock_time");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "user_active_state_data",
                newName: "active_set_password");

            migrationBuilder.RenameColumn(
                name: "padlock",
                table: "user_active_state_data",
                newName: "active_set_pad_lock");

            migrationBuilder.RenameColumn(
                name: "assigner",
                table: "user_active_state_data",
                newName: "active_set_lock_assigner");
        }
    }
}
