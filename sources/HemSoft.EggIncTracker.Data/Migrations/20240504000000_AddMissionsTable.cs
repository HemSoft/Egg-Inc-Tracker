using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HemSoft.EggIncTracker.Data.Migrations
{
    /// <summary>
    /// Migration to add Missions table
    /// </summary>
    public partial class AddMissionsTable : Migration
    {
        /// <summary>
        /// Up migration
        /// </summary>
        /// <param name="migrationBuilder">Migration builder</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ship = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DurationType = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    QualityBump = table.Column<float>(type: "real", nullable: false),
                    TargetArtifact = table.Column<int>(type: "int", nullable: false),
                    SecondsRemaining = table.Column<float>(type: "real", nullable: false),
                    LaunchTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FuelList = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsStandby = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Missions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Missions_PlayerId",
                table: "Missions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_PlayerName",
                table: "Missions",
                column: "PlayerName");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_ReturnTime",
                table: "Missions",
                column: "ReturnTime");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_Ship",
                table: "Missions",
                column: "Ship");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_Status",
                table: "Missions",
                column: "Status");
        }

        /// <summary>
        /// Down migration
        /// </summary>
        /// <param name="migrationBuilder">Migration builder</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Missions");
        }
    }
}
