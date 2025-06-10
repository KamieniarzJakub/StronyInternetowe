using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentSystem.Migrations
{
    /// <inheritdoc />
    public partial class MapURLDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleMapsUrl",
                table: "Tournaments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleMapsUrl",
                table: "Tournaments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
