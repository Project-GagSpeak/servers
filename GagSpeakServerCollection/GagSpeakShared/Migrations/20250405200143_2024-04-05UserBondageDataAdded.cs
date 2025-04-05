using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240405UserBondageDataAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_gag_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    layer = table.Column<byte>(type: "smallint", nullable: false),
                    gag = table.Column<int>(type: "integer", nullable: false),
                    enabler = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    padlock = table.Column<int>(type: "integer", nullable: false),
                    password = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    padlock_assigner = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_gag_data", x => new { x.user_uid, x.layer });
                    table.ForeignKey(
                        name: "fk_user_gag_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_restraintset_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    layers_bitfield = table.Column<byte>(type: "smallint", nullable: false),
                    enabler = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    padlock = table.Column<int>(type: "integer", nullable: false),
                    password = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    padlock_assigner = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_restraintset_data", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_restraintset_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_restriction_data",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    layer = table.Column<byte>(type: "smallint", nullable: false),
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    enabler = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    padlock = table.Column<int>(type: "integer", nullable: false),
                    password = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    timer = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    padlock_assigner = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_restriction_data", x => new { x.user_uid, x.layer });
                    table.ForeignKey(
                        name: "fk_user_restriction_data_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_gag_data_user_uid_layer",
                table: "user_gag_data",
                columns: new[] { "user_uid", "layer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_restraintset_data_user_uid",
                table: "user_restraintset_data",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_restriction_data_user_uid_layer",
                table: "user_restriction_data",
                columns: new[] { "user_uid", "layer" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_gag_data");

            migrationBuilder.DropTable(
                name: "user_restraintset_data");

            migrationBuilder.DropTable(
                name: "user_restriction_data");
        }
    }
}
