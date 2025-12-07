using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241205MoodlesHubAndPairRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /* -------------- STEP 1: CREATE ALL NEW TABLES & RELATIONSHIPS -------------- */
            migrationBuilder.CreateTable(
                name: "keywords",
                columns: table => new
                {
                    word = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_keywords", x => x.word);
                });

            migrationBuilder.CreateTable(
                name: "kinkster_pair_requests",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kinkster_pair_requests", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_kinkster_pair_requests_users_other_user_uid",
                        column: x => x.other_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_kinkster_pair_requests_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moodle_status",
                columns: table => new
                {
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    publisher_uid = table.Column<string>(type: "text", nullable: false),
                    time_published = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    author = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moodle_status", x => x.identifier);
                });

            migrationBuilder.CreateTable(
                name: "pattern_keywords",
                columns: table => new
                {
                    pattern_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    keyword_word = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pattern_keywords", x => new { x.pattern_entry_id, x.keyword_word });
                    table.ForeignKey(
                        name: "fk_pattern_keywords_keywords_keyword_word",
                        column: x => x.keyword_word,
                        principalTable: "keywords",
                        principalColumn: "word",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pattern_keywords_patterns_pattern_entry_id",
                        column: x => x.pattern_entry_id,
                        principalTable: "pattern_entry",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "likes_moodles",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    moodle_status_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_likes_moodles", x => new { x.user_uid, x.moodle_status_id });
                    table.ForeignKey(
                        name: "fk_likes_moodles_moodles_moodle_status_id",
                        column: x => x.moodle_status_id,
                        principalTable: "moodle_status",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_likes_moodles_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "likes_patterns",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    pattern_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    moodle_status_identifier = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_likes_patterns", x => new { x.user_uid, x.pattern_entry_id });
                    table.ForeignKey(
                        name: "fk_likes_patterns_moodles_moodle_status_identifier",
                        column: x => x.moodle_status_identifier,
                        principalTable: "moodle_status",
                        principalColumn: "identifier");
                    table.ForeignKey(
                        name: "fk_likes_patterns_patterns_pattern_entry_id",
                        column: x => x.pattern_entry_id,
                        principalTable: "pattern_entry",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_likes_patterns_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moodle_keywords",
                columns: table => new
                {
                    moodle_status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    keyword_word = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moodle_keywords", x => new { x.moodle_status_id, x.keyword_word });
                    table.ForeignKey(
                        name: "fk_moodle_keywords_keywords_keyword_word",
                        column: x => x.keyword_word,
                        principalTable: "keywords",
                        principalColumn: "word",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_moodle_keywords_moodles_moodle_status_id",
                        column: x => x.moodle_status_id,
                        principalTable: "moodle_status",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_keywords_word",
                table: "keywords",
                column: "word",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kinkster_pair_requests_other_user_uid",
                table: "kinkster_pair_requests",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_kinkster_pair_requests_user_uid",
                table: "kinkster_pair_requests",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_likes_moodles_moodle_status_id",
                table: "likes_moodles",
                column: "moodle_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_likes_patterns_moodle_status_identifier",
                table: "likes_patterns",
                column: "moodle_status_identifier");

            migrationBuilder.CreateIndex(
                name: "ix_likes_patterns_pattern_entry_id",
                table: "likes_patterns",
                column: "pattern_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_moodle_keywords_keyword_word",
                table: "moodle_keywords",
                column: "keyword_word");

            migrationBuilder.CreateIndex(
                name: "ix_moodle_keywords_moodle_status_id",
                table: "moodle_keywords",
                column: "moodle_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_pattern_keywords_keyword_word",
                table: "pattern_keywords",
                column: "keyword_word");

            migrationBuilder.CreateIndex(
                name: "ix_pattern_keywords_pattern_entry_id",
                table: "pattern_keywords",
                column: "pattern_entry_id");

            /* --------------- STEP 2: COPY DATA FROM user_pattern_likes TO likes_patterns --------------- */
            migrationBuilder.Sql(@"
                INSERT INTO likes_patterns (user_uid, pattern_entry_id)
                SELECT user_uid, pattern_entry_id
                FROM user_pattern_likes
            ");

            /* --------------- STEP 3: COPY DATA FROM pattern_tags TO keywords --------------- */
            migrationBuilder.Sql(@"
                INSERT INTO keywords (word)
                SELECT name
                FROM pattern_tags
            ");

            /* --------------- STEP 4: COPY DATA FROM pattern_entry_tags TO pattern_keywords --------------- */
            migrationBuilder.Sql(@"
                INSERT INTO pattern_keywords (pattern_entry_id, keyword_word)
                SELECT pattern_entry_id, tag_name
                FROM pattern_entry_tags
            ");

            /* --------------- STEP5: REMOVE OLD TABLES (or maybe keep them there as backups? --------------- */
            migrationBuilder.RenameTable(name: "pattern_entry_tags", newName: "pattern_entry_tags_old");
            migrationBuilder.RenameTable(name: "user_pattern_likes", newName: "user_pattern_likes_old");
            migrationBuilder.RenameTable(name: "pattern_tags", newName: "pattern_tags_old");
            migrationBuilder.DropColumn(name: "uses_oscillation", table: "pattern_entry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /* --------------- STEP 1: RECREATE OLD TABLES --------------- */
            migrationBuilder.CreateTable(
                name: "pattern_tags",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pattern_tags", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "pattern_entry_tags",
                columns: table => new
                {
                    pattern_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pattern_entry_tags", x => new { x.pattern_entry_id, x.tag_name });
                    table.ForeignKey(
                        name: "fk_pattern_entry_tags_pattern_entry_pattern_entry_id",
                        column: x => x.pattern_entry_id,
                        principalTable: "pattern_entry",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pattern_entry_tags_pattern_tags_tag_name",
                        column: x => x.tag_name,
                        principalTable: "pattern_tags",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_pattern_likes",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    pattern_entry_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_pattern_likes", x => new { x.user_uid, x.pattern_entry_id });
                    table.ForeignKey(
                        name: "fk_user_pattern_likes_pattern_entry_pattern_entry_id",
                        column: x => x.pattern_entry_id,
                        principalTable: "pattern_entry",
                        principalColumn: "identifier",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_pattern_likes_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            /* --------------- STEP 2: COPY DATA BACK TO OLD TABLES --------------- */
            migrationBuilder.Sql(@"
                INSERT INTO user_pattern_likes (user_uid, pattern_entry_id)
                SELECT user_uid, pattern_entry_id
                FROM likes_patterns
            ");

            migrationBuilder.Sql(@"
                INSERT INTO pattern_tags (name)
                SELECT DISTINCT keyword_word
                FROM pattern_keywords
            ");

            migrationBuilder.Sql(@"
                INSERT INTO pattern_entry_tags (pattern_entry_id, tag_name)
                SELECT pattern_entry_id, keyword_word
                FROM pattern_keywords
            ");

            /* --------------- STEP 3: DROP NEW TABLES --------------- */
            migrationBuilder.DropTable(name: "moodle_keywords");
            migrationBuilder.DropTable(name: "likes_patterns");
            migrationBuilder.DropTable(name: "likes_moodles");
            migrationBuilder.DropTable(name: "pattern_keywords");
            migrationBuilder.DropTable(name: "kinkster_pair_requests");
            migrationBuilder.DropTable(name: "keywords");
            migrationBuilder.DropTable(name: "moodle_status");

            /* --------------- STEP 4: RENAME OLD TABLES BACK TO ORIGINAL NAMES --------------- */
            migrationBuilder.RenameTable(name: "pattern_entry_tags_old", newName: "pattern_entry_tags");
            migrationBuilder.RenameTable(name: "user_pattern_likes_old", newName: "user_pattern_likes");
            migrationBuilder.RenameTable(name: "pattern_tags_old", newName: "pattern_tags");

            /* --------------- STEP 5: RE-ADD DROPPED COLUMNS --------------- */
            migrationBuilder.AddColumn<bool>(
                name: "uses_oscillation",
                table: "pattern_entry",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
