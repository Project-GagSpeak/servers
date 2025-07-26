using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GagSpeakShared.Migrations
{
    /// <inheritdoc />
    public partial class _20240725FixHypnoEditName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "hypno_effect_sending_allowed",
                table: "client_pair_permissions_access",
                newName: "hypnosis_sending_allowed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "hypnosis_sending_allowed",
                table: "client_pair_permissions_access",
                newName: "hypno_effect_sending_allowed");
        }
    }
}
