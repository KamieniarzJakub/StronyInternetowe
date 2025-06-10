using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentSystem.Migrations
{
    /// <inheritdoc />
    public partial class DiscrepancyMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscrepancyMessage",
                table: "Matches",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscrepancyMessage",
                table: "Matches");
        }
    }
}
