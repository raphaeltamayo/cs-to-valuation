using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CStoValuation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TotalGross = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalNet = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    TakenUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioSnapshots_TakenUtc",
                table: "PortfolioSnapshots",
                column: "TakenUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioSnapshots");
        }
    }
}
