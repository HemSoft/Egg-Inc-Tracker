using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HemSoft.EggIncTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEarningsBonus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EarningsBonusPercentage",
                table: "Players",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoulEggsFull",
                table: "Players",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EarningsBonusPercentage",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "SoulEggsFull",
                table: "Players");
        }
    }
}
