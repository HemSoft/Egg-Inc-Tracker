using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HemSoft.EggIncTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Players",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Players");
        }
    }
}
