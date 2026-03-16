using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20260315TableRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_likes_moodles_moodle_status_loci_status_id",
                table: "likes_moodles");

            migrationBuilder.DropForeignKey(
                name: "fk_likes_moodles_users_user_uid",
                table: "likes_moodles");

            migrationBuilder.DropForeignKey(
                name: "fk_moodle_keywords_keywords_keyword_word",
                table: "moodle_keywords");

            migrationBuilder.DropForeignKey(
                name: "fk_moodle_keywords_loci_statuses_moodle_status_id",
                table: "moodle_keywords");

            migrationBuilder.DropPrimaryKey(
                name: "pk_moodle_status",
                table: "moodle_status");

            migrationBuilder.DropPrimaryKey(
                name: "pk_moodle_keywords",
                table: "moodle_keywords");

            migrationBuilder.DropPrimaryKey(
                name: "pk_likes_moodles",
                table: "likes_moodles");

            migrationBuilder.RenameTable(
                name: "moodle_status",
                newName: "loci_status");

            migrationBuilder.RenameTable(
                name: "moodle_keywords",
                newName: "loci_keywords");

            migrationBuilder.RenameTable(
                name: "likes_moodles",
                newName: "likes_loci");

            migrationBuilder.RenameIndex(
                name: "ix_moodle_status_title",
                table: "loci_status",
                newName: "ix_loci_status_title");

            migrationBuilder.RenameIndex(
                name: "ix_moodle_status_author",
                table: "loci_status",
                newName: "ix_loci_status_author");

            migrationBuilder.RenameIndex(
                name: "ix_moodle_keywords_moodle_status_id",
                table: "loci_keywords",
                newName: "ix_loci_keywords_moodle_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_moodle_keywords_keyword_word",
                table: "loci_keywords",
                newName: "ix_loci_keywords_keyword_word");

            migrationBuilder.RenameIndex(
                name: "ix_likes_moodles_user_uid",
                table: "likes_loci",
                newName: "ix_likes_loci_user_uid");

            migrationBuilder.RenameIndex(
                name: "ix_likes_moodles_loci_status_id",
                table: "likes_loci",
                newName: "ix_likes_loci_loci_status_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_loci_status",
                table: "loci_status",
                column: "identifier");

            migrationBuilder.AddPrimaryKey(
                name: "pk_loci_keywords",
                table: "loci_keywords",
                columns: new[] { "moodle_status_id", "keyword_word" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_likes_loci",
                table: "likes_loci",
                columns: new[] { "user_uid", "loci_status_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_likes_loci_loci_status_loci_status_id",
                table: "likes_loci",
                column: "loci_status_id",
                principalTable: "loci_status",
                principalColumn: "identifier",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_likes_loci_users_user_uid",
                table: "likes_loci",
                column: "user_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_loci_keywords_keywords_keyword_word",
                table: "loci_keywords",
                column: "keyword_word",
                principalTable: "keywords",
                principalColumn: "word",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_loci_keywords_loci_statuses_moodle_status_id",
                table: "loci_keywords",
                column: "moodle_status_id",
                principalTable: "loci_status",
                principalColumn: "identifier",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_likes_loci_loci_status_loci_status_id",
                table: "likes_loci");

            migrationBuilder.DropForeignKey(
                name: "fk_likes_loci_users_user_uid",
                table: "likes_loci");

            migrationBuilder.DropForeignKey(
                name: "fk_loci_keywords_keywords_keyword_word",
                table: "loci_keywords");

            migrationBuilder.DropForeignKey(
                name: "fk_loci_keywords_loci_statuses_moodle_status_id",
                table: "loci_keywords");

            migrationBuilder.DropPrimaryKey(
                name: "pk_loci_status",
                table: "loci_status");

            migrationBuilder.DropPrimaryKey(
                name: "pk_loci_keywords",
                table: "loci_keywords");

            migrationBuilder.DropPrimaryKey(
                name: "pk_likes_loci",
                table: "likes_loci");

            migrationBuilder.RenameTable(
                name: "loci_status",
                newName: "moodle_status");

            migrationBuilder.RenameTable(
                name: "loci_keywords",
                newName: "moodle_keywords");

            migrationBuilder.RenameTable(
                name: "likes_loci",
                newName: "likes_moodles");

            migrationBuilder.RenameIndex(
                name: "ix_loci_status_title",
                table: "moodle_status",
                newName: "ix_moodle_status_title");

            migrationBuilder.RenameIndex(
                name: "ix_loci_status_author",
                table: "moodle_status",
                newName: "ix_moodle_status_author");

            migrationBuilder.RenameIndex(
                name: "ix_loci_keywords_moodle_status_id",
                table: "moodle_keywords",
                newName: "ix_moodle_keywords_moodle_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_loci_keywords_keyword_word",
                table: "moodle_keywords",
                newName: "ix_moodle_keywords_keyword_word");

            migrationBuilder.RenameIndex(
                name: "ix_likes_loci_user_uid",
                table: "likes_moodles",
                newName: "ix_likes_moodles_user_uid");

            migrationBuilder.RenameIndex(
                name: "ix_likes_loci_loci_status_id",
                table: "likes_moodles",
                newName: "ix_likes_moodles_loci_status_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_moodle_status",
                table: "moodle_status",
                column: "identifier");

            migrationBuilder.AddPrimaryKey(
                name: "pk_moodle_keywords",
                table: "moodle_keywords",
                columns: new[] { "moodle_status_id", "keyword_word" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_likes_moodles",
                table: "likes_moodles",
                columns: new[] { "user_uid", "loci_status_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_likes_moodles_moodle_status_loci_status_id",
                table: "likes_moodles",
                column: "loci_status_id",
                principalTable: "moodle_status",
                principalColumn: "identifier",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_likes_moodles_users_user_uid",
                table: "likes_moodles",
                column: "user_uid",
                principalTable: "users",
                principalColumn: "uid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_moodle_keywords_keywords_keyword_word",
                table: "moodle_keywords",
                column: "keyword_word",
                principalTable: "keywords",
                principalColumn: "word",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_moodle_keywords_loci_statuses_moodle_status_id",
                table: "moodle_keywords",
                column: "moodle_status_id",
                principalTable: "moodle_status",
                principalColumn: "identifier",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
