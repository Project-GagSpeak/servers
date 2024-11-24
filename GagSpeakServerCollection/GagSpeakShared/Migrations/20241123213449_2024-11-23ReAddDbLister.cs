using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20241123ReAddDbLister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION notify_accountclaimauth_insert()
                RETURNS TRIGGER AS $$
                BEGIN
                  PERFORM pg_notify('accountclaimauth_insert', row_to_json(NEW)::text);
                  RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                ");

                    migrationBuilder.Sql(@"
                CREATE TRIGGER accountclaimauth_after_insert
                AFTER INSERT ON account_claim_auth
                FOR EACH ROW EXECUTE FUNCTION notify_accountclaimauth_insert();
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS accountclaimauth_after_insert ON account_claim_auth;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS notify_accountclaimauth_insert;");
        }
    }
}