using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentSystem.Migrations
{
    /// <inheritdoc />
    public partial class PickWinner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Player1ReportedWinnerId",
                table: "Matches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Player2ReportedWinnerId",
                table: "Matches",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Player1ReportedWinnerId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Player2ReportedWinnerId",
                table: "Matches");
        }
    }
}
