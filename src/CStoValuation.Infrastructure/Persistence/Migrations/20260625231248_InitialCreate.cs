using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CStoValuation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CachedInventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SteamId64 = table.Column<string>(type: "TEXT", nullable: false),
                    AssetId = table.Column<string>(type: "TEXT", nullable: false),
                    ClassId = table.Column<string>(type: "TEXT", nullable: false),
                    InstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    MarketHashName = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Tradable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Marketable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IconUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Weapon = table.Column<string>(type: "TEXT", nullable: true),
                    Rarity = table.Column<int>(type: "INTEGER", nullable: false),
                    Exterior = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    CachedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedInventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistoryPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MarketHashName = table.Column<string>(type: "TEXT", nullable: false),
                    DateUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Volume = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistoryPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MarketHashName = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Min = table.Column<decimal>(type: "TEXT", nullable: true),
                    Median = table.Column<decimal>(type: "TEXT", nullable: true),
                    Mean = table.Column<decimal>(type: "TEXT", nullable: true),
                    Listings = table.Column<int>(type: "INTEGER", nullable: true),
                    Volume = table.Column<int>(type: "INTEGER", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    TakenUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedInventoryItems_SteamId64",
                table: "CachedInventoryItems",
                column: "SteamId64");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistoryPoints_MarketHashName_DateUtc",
                table: "PriceHistoryPoints",
                columns: new[] { "MarketHashName", "DateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_MarketHashName_TakenUtc",
                table: "PriceSnapshots",
                columns: new[] { "MarketHashName", "TakenUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedInventoryItems");

            migrationBuilder.DropTable(
                name: "PriceHistoryPoints");

            migrationBuilder.DropTable(
                name: "PriceSnapshots");
        }
    }
}
