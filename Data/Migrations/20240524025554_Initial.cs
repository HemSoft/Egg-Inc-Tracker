using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalCraftsThatCanBeLegendary = table.Column<int>(type: "int", nullable: false),
                    ExpectedLegendaryCrafts = table.Column<float>(type: "real", nullable: false),
                    ExpectedLegendaryDropsFromShips = table.Column<float>(type: "real", nullable: false),
                    ExpectedLegendaries = table.Column<float>(type: "real", nullable: false),
                    PlayerLegendaries = table.Column<float>(type: "real", nullable: false),
                    PlayerLegendariesExcludingLunarTotem = table.Column<float>(type: "real", nullable: false),
                    LLC = table.Column<float>(type: "real", nullable: false),
                    ProphecyEggs = table.Column<int>(type: "int", nullable: false),
                    SoulEggs = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MER = table.Column<float>(type: "real", nullable: false),
                    JER = table.Column<float>(type: "real", nullable: false),
                    CraftingLevel = table.Column<int>(type: "int", nullable: false),
                    PiggyConsumeValue = table.Column<int>(type: "int", nullable: false),
                    ShipLaunchPoints = table.Column<float>(type: "real", nullable: false),
                    HoarderScore = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
