using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HemSoft.News.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DiscoveredDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdditionalData = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Query = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    CheckFrequencyMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    LastChecked = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Configuration = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsSources", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "NewsSources",
                columns: new[] { "Id", "CheckFrequencyMinutes", "Configuration", "IsActive", "LastChecked", "Name", "Query", "Type", "Url" },
                values: new object[] { 1, 360, null, true, null, "Microsoft.Extensions.AI NuGet Packages", "Microsoft.Extensions.AI", "NuGet", "https://www.nuget.org/packages?q=Microsoft.Extensions.AI" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsItems_Source_Title_PublishedDate",
                table: "NewsItems",
                columns: new[] { "Source", "Title", "PublishedDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsSources_Name",
                table: "NewsSources",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsItems");

            migrationBuilder.DropTable(
                name: "NewsSources");
        }
    }
}
