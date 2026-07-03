using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoBacktestingDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeDirectionToStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TradeDirection",
                table: "BacktestStrategies",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TradeDirection",
                table: "BacktestStrategies");
        }
    }
}
