using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CryptoBacktestingDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear all existing per-user data (sessions, results, strategies)
            // to avoid FK conflicts when adding AppUserId.
            migrationBuilder.Sql("DELETE FROM [BacktestResults]");
            migrationBuilder.Sql("DELETE FROM [BacktestSessions]");
            migrationBuilder.Sql("DELETE FROM [BacktestStrategies]");

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "BacktestStrategies",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "BacktestSessions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 16, 1, 59, 895, DateTimeKind.Local).AddTicks(1613));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 16, 1, 59, 895, DateTimeKind.Local).AddTicks(1617));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 16, 1, 59, 895, DateTimeKind.Local).AddTicks(1728));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 16, 1, 59, 895, DateTimeKind.Local).AddTicks(1732));

            migrationBuilder.CreateIndex(
                name: "IX_BacktestStrategies_AppUserId",
                table: "BacktestStrategies",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BacktestSessions_AppUserId",
                table: "BacktestSessions",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BacktestSessions_AspNetUsers_AppUserId",
                table: "BacktestSessions",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BacktestStrategies_AspNetUsers_AppUserId",
                table: "BacktestStrategies",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BacktestSessions_AspNetUsers_AppUserId",
                table: "BacktestSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_BacktestStrategies_AspNetUsers_AppUserId",
                table: "BacktestStrategies");

            migrationBuilder.DropIndex(
                name: "IX_BacktestStrategies_AppUserId",
                table: "BacktestStrategies");

            migrationBuilder.DropIndex(
                name: "IX_BacktestSessions_AppUserId",
                table: "BacktestSessions");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "BacktestStrategies");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "BacktestSessions");

            migrationBuilder.InsertData(
                table: "BacktestStrategies",
                columns: new[] { "Id", "CreatedAt", "Description", "InitialCapital", "IsActive", "LastModifiedAt", "LookbackPeriod", "Name", "PositionSizePercent", "StopLossPercent", "TakeProfitPercent", "TradeDirection", "TrailingStopPercent" },
                values: new object[] { 1, new DateTime(2026, 6, 20, 12, 31, 5, 737, DateTimeKind.Local).AddTicks(4195), "A simple RSI strategy", 10000m, true, null, 100, "RSI Strategy", 100m, 5m, 10m, 0, null });

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 12, 31, 5, 737, DateTimeKind.Local).AddTicks(3763));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 12, 31, 5, 737, DateTimeKind.Local).AddTicks(3769));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 12, 31, 5, 737, DateTimeKind.Local).AddTicks(4098));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 12, 31, 5, 737, DateTimeKind.Local).AddTicks(4104));

            migrationBuilder.InsertData(
                table: "BacktestSessions",
                columns: new[] { "Id", "CryptoPairId", "EndDate", "ExecutedAt", "FinalBalance", "InitialBalance", "IsOptimized", "StartDate", "StrategyId" },
                values: new object[] { 1, 1, new DateTime(2026, 3, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 12350m, 10000m, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1 });

            migrationBuilder.InsertData(
                table: "BacktestResults",
                columns: new[] { "Id", "BacktestSessionId", "Commission", "EntryPrice", "EntryTime", "ExitPrice", "ExitTime", "IsWinningTrade", "Quantity", "TradeType" },
                values: new object[,]
                {
                    { 1, 1, 10m, 63000m, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 65000m, new DateTime(2026, 1, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 0.1m, 0 },
                    { 2, 1, 10m, 64500m, new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 64000m, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 0.1m, 0 }
                });
        }
    }
}
