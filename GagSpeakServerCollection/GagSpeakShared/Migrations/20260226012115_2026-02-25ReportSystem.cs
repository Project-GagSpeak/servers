using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20260225ReportSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_entries");

            migrationBuilder.AddColumn<int>(
                name: "false_report_strikes",
                table: "account_reputation",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "reported_chats",
                columns: table => new
                {
                    report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<int>(type: "integer", nullable: false),
                    report_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    compressed_chat_history = table.Column<string>(type: "text", nullable: false),
                    reporting_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    reported_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    report_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reported_chats", x => x.report_id);
                    table.ForeignKey(
                        name: "fk_reported_chats_users_reported_user_uid",
                        column: x => x.reported_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                    table.ForeignKey(
                        name: "fk_reported_chats_users_reporting_user_uid",
                        column: x => x.reporting_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                });

            migrationBuilder.CreateTable(
                name: "reported_profiles",
                columns: table => new
                {
                    report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<int>(type: "integer", nullable: false),
                    report_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    snapshot_image = table.Column<string>(type: "text", nullable: false),
                    snapshot_description = table.Column<string>(type: "text", nullable: false),
                    reporting_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    reported_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    report_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reported_profiles", x => x.report_id);
                    table.ForeignKey(
                        name: "fk_reported_profiles_users_reported_user_uid",
                        column: x => x.reported_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                    table.ForeignKey(
                        name: "fk_reported_profiles_users_reporting_user_uid",
                        column: x => x.reporting_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                });

            migrationBuilder.CreateIndex(
                name: "ix_reported_chats_reported_user_uid",
                table: "reported_chats",
                column: "reported_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_reported_chats_reporting_user_uid",
                table: "reported_chats",
                column: "reporting_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_reported_profiles_reported_user_uid",
                table: "reported_profiles",
                column: "reported_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_reported_profiles_reporting_user_uid",
                table: "reported_profiles",
                column: "reporting_user_uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reported_chats");

            migrationBuilder.DropTable(
                name: "reported_profiles");

            migrationBuilder.DropColumn(
                name: "false_report_strikes",
                table: "account_reputation");

            migrationBuilder.CreateTable(
                name: "report_entries",
                columns: table => new
                {
                    report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reported_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    reporting_user_uid = table.Column<string>(type: "character varying(10)", nullable: true),
                    chat_log_history_base64 = table.Column<string>(type: "text", nullable: true),
                    report_reason = table.Column<string>(type: "text", nullable: true),
                    report_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    snapshot_description = table.Column<string>(type: "text", nullable: false),
                    snapshot_image = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_entries", x => x.report_id);
                    table.ForeignKey(
                        name: "fk_report_entries_users_reported_user_uid",
                        column: x => x.reported_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                    table.ForeignKey(
                        name: "fk_report_entries_users_reporting_user_uid",
                        column: x => x.reporting_user_uid,
                        principalTable: "users",
                        principalColumn: "uid");
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_entries_reported_user_uid",
                table: "report_entries",
                column: "reported_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_report_entries_reporting_user_uid",
                table: "report_entries",
                column: "reporting_user_uid");
        }
    }
}
