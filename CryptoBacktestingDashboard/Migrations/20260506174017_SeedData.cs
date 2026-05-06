using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CryptoBacktestingDashboard.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CryptoPairs",
                columns: new[] { "Id", "BaseAsset", "CreatedAt", "CurrentPrice", "QuoteAsset", "Symbol" },
                values: new object[,]
                {
                    { 1, "BTC", new DateTime(2026, 5, 6, 19, 40, 17, 579, DateTimeKind.Local).AddTicks(5368), 60000m, "USD", "BTC/USD" },
                    { 2, "ETH", new DateTime(2026, 5, 6, 19, 40, 17, 579, DateTimeKind.Local).AddTicks(5373), 2000m, "USD", "ETH/USD" }
                });

            migrationBuilder.InsertData(
                table: "Indicators",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "Period", "Threshold", "Type" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 5, 6, 19, 40, 17, 579, DateTimeKind.Local).AddTicks(5492), "Relative Strength Index", "RSI", 14, 70m, 0 },
                    { 2, new DateTime(2026, 5, 6, 19, 40, 17, 579, DateTimeKind.Local).AddTicks(5496), "Moving Average Convergence Divergence", "MACD", 12, 26m, 1 }
                });

            migrationBuilder.InsertData(
                table: "RiskManagements",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "Type", "Value" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 5, 6, 19, 40, 17, 579, DateTimeKind.Local).AddTicks(5518), "2% Stop Loss", "Stop Loss", 0, 2m },
                    { 2, new DateTime(2026, 5, 6, 19, 40, 17, 579, DateTimeKind.Local).AddTicks(5521), "5% Take Profit", "Take Profit", 1, 5m }
                });

            migrationBuilder.InsertData(
                table: "BacktestStrategies",
                columns: new[] { "Id", "CreatedAt", "Description", "InitialCapital", "IsActive", "LastModifiedAt", "LookbackPeriod", "Name", "RiskManagementId" },
                values: new object[] { 1, new DateTime(2026, 5, 6, 19, 40, 17, 579, DateTimeKind.Local).AddTicks(5544), "A simple RSI strategy", 10000m, true, null, 100, "RSI Strategy", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BacktestStrategies",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RiskManagements",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RiskManagements",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
