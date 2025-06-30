using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240629NewPermissionsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "layers_bitfield",
                table: "user_restraintset_data",
                newName: "active_layers");

            migrationBuilder.RenameColumn(
                name: "chat_garbler_channels_bitfield",
                table: "user_global_permissions",
                newName: "allowed_garbler_channels");

            migrationBuilder.RenameColumn(
                name: "apply_restraint_layers_allowed",
                table: "client_pair_permissions_access",
                newName: "remove_layers_while_locked_allowed");

            migrationBuilder.RenameColumn(
                name: "apply_restraint_layers",
                table: "client_pair_permissions",
                newName: "remove_layers_while_locked");

            migrationBuilder.AddColumn<string>(
                name: "hypnosis_custom_effect",
                table: "user_global_permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "apply_layers_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "apply_layers_while_locked_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hypno_effect_sending_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "remove_layers_allowed",
                table: "client_pair_permissions_access",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_hypno_image_sending",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "apply_layers",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "apply_layers_while_locked",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hypno_effect_sending",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "remove_layers",
                table: "client_pair_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hypnosis_custom_effect",
                table: "user_global_permissions");

            migrationBuilder.DropColumn(
                name: "apply_layers_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "apply_layers_while_locked_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "hypno_effect_sending_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "remove_layers_allowed",
                table: "client_pair_permissions_access");

            migrationBuilder.DropColumn(
                name: "allow_hypno_image_sending",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "apply_layers",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "apply_layers_while_locked",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "hypno_effect_sending",
                table: "client_pair_permissions");

            migrationBuilder.DropColumn(
                name: "remove_layers",
                table: "client_pair_permissions");

            migrationBuilder.RenameColumn(
                name: "active_layers",
                table: "user_restraintset_data",
                newName: "layers_bitfield");

            migrationBuilder.RenameColumn(
                name: "allowed_garbler_channels",
                table: "user_global_permissions",
                newName: "chat_garbler_channels_bitfield");

            migrationBuilder.RenameColumn(
                name: "remove_layers_while_locked_allowed",
                table: "client_pair_permissions_access",
                newName: "apply_restraint_layers_allowed");

            migrationBuilder.RenameColumn(
                name: "remove_layers_while_locked",
                table: "client_pair_permissions",
                newName: "apply_restraint_layers");
        }
    }
}
