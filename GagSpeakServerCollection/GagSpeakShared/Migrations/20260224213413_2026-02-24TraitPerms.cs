using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20260224TraitPerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "global_arousal",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "global_gag_traits",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "global_restraint_traits",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "global_restriction_traits",
                table: "user_global_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "arousal_manipulation",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "gag_traits",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "restraint_traits",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "restriction_traits",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "global_arousal",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_gag_traits",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_restraint_traits",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "global_restriction_traits",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "arousal_manipulation",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "gag_traits",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "restraint_traits",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "restriction_traits",
                table: "client_pair_permissions");
        }
    }
}
