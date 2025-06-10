using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentSystem.Migrations
{
    /// <inheritdoc />
    public partial class TournamentWinner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Winner",
                table: "Tournaments",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Winner",
                table: "Tournaments");
        }
    }
}
