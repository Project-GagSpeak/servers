using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20260315RefactorForLoci : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_likes_moodles_moodles_moodle_status_id",
                table: "likes_moodles");

            migrationBuilder.DropForeignKey(
                name: "fk_likes_patterns_patterns_pattern_entry_id",
                table: "likes_patterns");

            migrationBuilder.DropForeignKey(
                name: "fk_moodle_keywords_moodles_moodle_status_id",
                table: "moodle_keywords");

            migrationBuilder.DropColumn(
                name: "moodle_icon_id",
                table: "user_collar_data");

            migrationBuilder.DropColumn(
                name: "as_permanent",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "dispelable",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "no_expire",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "stack_on_reapply",
                table: "moodle_status");

            migrationBuilder.RenameColumn(
                name: "moodle_vfx_path",
                table: "user_collar_data",
                newName: "loci_vfx_path");

            migrationBuilder.RenameColumn(
                name: "moodle_type",
                table: "user_collar_data",
                newName: "loci_data_type");

            migrationBuilder.RenameColumn(
                name: "moodle_title",
                table: "user_collar_data",
                newName: "loci_title");

            migrationBuilder.RenameColumn(
                name: "moodle_id",
                table: "user_collar_data",
                newName: "loci_status_id");

            migrationBuilder.RenameColumn(
                name: "moodle_description",
                table: "user_collar_data",
                newName: "loci_description");

            migrationBuilder.RenameColumn(
                name: "chained_status",
                table: "moodle_status",
                newName: "chained_guid");

            migrationBuilder.RenameColumn(
                name: "moodle_status_id",
                table: "likes_moodles",
                newName: "loci_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_likes_moodles_moodle_status_id",
                table: "likes_moodles",
                newName: "ix_likes_moodles_loci_status_id");

            migrationBuilder.RenameColumn(
                name: "moodles_enabled_allowed",
                table: "client_pair_permissions_access",
                newName: "loci_enabled_allowed");

            migrationBuilder.RenameColumn(
                name: "moodle_access_allowed",
                table: "client_pair_permissions_access",
                newName: "loci_access_allowed");

            migrationBuilder.RenameColumn(
                name: "max_moodle_time_allowed",
                table: "client_pair_permissions_access",
                newName: "max_loci_time_allowed");

            migrationBuilder.RenameColumn(
                name: "moodle_access",
                table: "client_pair_permissions",
                newName: "loci_access");

            migrationBuilder.RenameColumn(
                name: "max_moodle_time",
                table: "client_pair_permissions",
                newName: "max_loci_time");

            migrationBuilder.AddColumn<long>(
                name: "loci_icon_id",
                table: "user_collar_data",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "icon_id",
                table: "moodle_status",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<byte>(
                name: "chain_type",
                table: "moodle_status",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "stack_to_chain",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "fk_likes_moodles_moodle_status_loci_status_id",
                table: "likes_moodles",
                column: "loci_status_id",
                principalTable: "moodle_status",
                principalColumn: "identifier",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_likes_patterns_pattern_entry_pattern_entry_id",
                table: "likes_patterns",
                column: "pattern_entry_id",
                principalTable: "pattern_entry",
                principalColumn: "identifier",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_moodle_keywords_loci_statuses_moodle_status_id",
                table: "moodle_keywords",
                column: "moodle_status_id",
                principalTable: "moodle_status",
                principalColumn: "identifier",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_likes_moodles_moodle_status_loci_status_id",
                table: "likes_moodles");

            migrationBuilder.DropForeignKey(
                name: "fk_likes_patterns_pattern_entry_pattern_entry_id",
                table: "likes_patterns");

            migrationBuilder.DropForeignKey(
                name: "fk_moodle_keywords_loci_statuses_moodle_status_id",
                table: "moodle_keywords");

            migrationBuilder.DropColumn(
                name: "loci_icon_id",
                table: "user_collar_data");

            migrationBuilder.DropColumn(
                name: "chain_type",
                table: "moodle_status");

            migrationBuilder.DropColumn(
                name: "stack_to_chain",
                table: "moodle_status");

            migrationBuilder.RenameColumn(
                name: "loci_vfx_path",
                table: "user_collar_data",
                newName: "moodle_vfx_path");

            migrationBuilder.RenameColumn(
                name: "loci_title",
                table: "user_collar_data",
                newName: "moodle_title");

            migrationBuilder.RenameColumn(
                name: "loci_status_id",
                table: "user_collar_data",
                newName: "moodle_id");

            migrationBuilder.RenameColumn(
                name: "loci_description",
                table: "user_collar_data",
                newName: "moodle_description");

            migrationBuilder.RenameColumn(
                name: "loci_data_type",
                table: "user_collar_data",
                newName: "moodle_type");

            migrationBuilder.RenameColumn(
                name: "chained_guid",
                table: "moodle_status",
                newName: "chained_status");

            migrationBuilder.RenameColumn(
                name: "loci_status_id",
                table: "likes_moodles",
                newName: "moodle_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_likes_moodles_loci_status_id",
                table: "likes_moodles",
                newName: "ix_likes_moodles_moodle_status_id");

            migrationBuilder.RenameColumn(
                name: "max_loci_time_allowed",
                table: "client_pair_permissions_access",
                newName: "max_moodle_time_allowed");

            migrationBuilder.RenameColumn(
                name: "loci_enabled_allowed",
                table: "client_pair_permissions_access",
                newName: "moodles_enabled_allowed");

            migrationBuilder.RenameColumn(
                name: "loci_access_allowed",
                table: "client_pair_permissions_access",
                newName: "moodle_access_allowed");

            migrationBuilder.RenameColumn(
                name: "max_loci_time",
                table: "client_pair_permissions",
                newName: "max_moodle_time");

            migrationBuilder.RenameColumn(
                name: "loci_access",
                table: "client_pair_permissions",
                newName: "moodle_access");

            migrationBuilder.AddColumn<int>(
                name: "moodle_icon_id",
                table: "user_collar_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "icon_id",
                table: "moodle_status",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<bool>(
                name: "as_permanent",
                table: "moodle_status",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "dispelable",
                table: "moodle_status",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "no_expire",
                table: "moodle_status",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "stack_on_reapply",
                table: "moodle_status",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "fk_likes_moodles_moodles_moodle_status_id",
                table: "likes_moodles",
                column: "moodle_status_id",
                principalTable: "moodle_status",
                principalColumn: "identifier",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_likes_patterns_patterns_pattern_entry_id",
                table: "likes_patterns",
                column: "pattern_entry_id",
                principalTable: "pattern_entry",
                principalColumn: "identifier",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_moodle_keywords_moodles_moodle_status_id",
                table: "moodle_keywords",
                column: "moodle_status_id",
                principalTable: "moodle_status",
                principalColumn: "identifier",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
