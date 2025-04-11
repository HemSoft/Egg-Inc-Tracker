using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HemSoft.EggIncTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNextTitleandTitleProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NextTitle",
                table: "Players",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "TitleProgress",
                table: "Players",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextTitle",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TitleProgress",
                table: "Players");
        }
    }
}
