using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20260217DeleteCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_profile_data_active_collar_data_user_uid",
                table: "user_profile_data");

            migrationBuilder.AddForeignKey(
                name: "fk_user_profile_data_active_collar_data_user_uid",
                table: "user_profile_data",
                column: "user_uid",
                principalTable: "user_collar_data",
                principalColumn: "user_uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_profile_data_active_collar_data_user_uid",
                table: "user_profile_data");

            migrationBuilder.AddForeignKey(
                name: "fk_user_profile_data_active_collar_data_user_uid",
                table: "user_profile_data",
                column: "user_uid",
                principalTable: "user_collar_data",
                principalColumn: "user_uid",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
