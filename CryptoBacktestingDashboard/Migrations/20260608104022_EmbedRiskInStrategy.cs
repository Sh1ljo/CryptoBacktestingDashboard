using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CryptoBacktestingDashboard.Migrations
{
    /// <inheritdoc />
    public partial class EmbedRiskInStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BacktestStrategies_RiskManagements_RiskManagementId",
                table: "BacktestStrategies");

            migrationBuilder.DropForeignKey(
                name: "FK_IndicatorComparisons_Indicators_IndicatorAId",
                table: "IndicatorComparisons");

            migrationBuilder.DropForeignKey(
                name: "FK_IndicatorComparisons_Indicators_IndicatorBId",
                table: "IndicatorComparisons");

            migrationBuilder.DropTable(
                name: "RiskManagements");

            migrationBuilder.DropIndex(
                name: "IX_BacktestStrategies_RiskManagementId",
                table: "BacktestStrategies");

            migrationBuilder.DropColumn(
                name: "RiskManagementId",
                table: "BacktestStrategies");

            migrationBuilder.AddColumn<decimal>(
                name: "PositionSizePercent",
                table: "BacktestStrategies",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StopLossPercent",
                table: "BacktestStrategies",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 5m); // backfill existing strategies with a sensible 5% stop

            migrationBuilder.AddColumn<decimal>(
                name: "TakeProfitPercent",
                table: "BacktestStrategies",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 10m); // backfill existing strategies with a sensible 10% target

            migrationBuilder.AddColumn<decimal>(
                name: "TrailingStopPercent",
                table: "BacktestStrategies",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BacktestStrategies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PositionSizePercent", "StopLossPercent", "TakeProfitPercent", "TrailingStopPercent" },
                values: new object[] { new DateTime(2026, 6, 8, 12, 40, 21, 830, DateTimeKind.Local).AddTicks(5983), 100m, 5m, 10m, null });

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 12, 40, 21, 830, DateTimeKind.Local).AddTicks(5817));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 12, 40, 21, 830, DateTimeKind.Local).AddTicks(5822));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 12, 40, 21, 830, DateTimeKind.Local).AddTicks(5953));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 12, 40, 21, 830, DateTimeKind.Local).AddTicks(5956));

            migrationBuilder.AddForeignKey(
                name: "FK_IndicatorComparisons_Indicators_IndicatorAId",
                table: "IndicatorComparisons",
                column: "IndicatorAId",
                principalTable: "Indicators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IndicatorComparisons_Indicators_IndicatorBId",
                table: "IndicatorComparisons",
                column: "IndicatorBId",
                principalTable: "Indicators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IndicatorComparisons_Indicators_IndicatorAId",
                table: "IndicatorComparisons");

            migrationBuilder.DropForeignKey(
                name: "FK_IndicatorComparisons_Indicators_IndicatorBId",
                table: "IndicatorComparisons");

            migrationBuilder.DropColumn(
                name: "PositionSizePercent",
                table: "BacktestStrategies");

            migrationBuilder.DropColumn(
                name: "StopLossPercent",
                table: "BacktestStrategies");

            migrationBuilder.DropColumn(
                name: "TakeProfitPercent",
                table: "BacktestStrategies");

            migrationBuilder.DropColumn(
                name: "TrailingStopPercent",
                table: "BacktestStrategies");

            migrationBuilder.AddColumn<int>(
                name: "RiskManagementId",
                table: "BacktestStrategies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RiskManagements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskManagements", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "BacktestStrategies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "RiskManagementId" },
                values: new object[] { new DateTime(2026, 5, 15, 1, 42, 49, 600, DateTimeKind.Local).AddTicks(4844), 1 });

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 15, 1, 42, 49, 600, DateTimeKind.Local).AddTicks(4673));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 15, 1, 42, 49, 600, DateTimeKind.Local).AddTicks(4677));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 15, 1, 42, 49, 600, DateTimeKind.Local).AddTicks(4784));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 15, 1, 42, 49, 600, DateTimeKind.Local).AddTicks(4788));

            migrationBuilder.InsertData(
                table: "RiskManagements",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "Type", "Value" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 5, 15, 1, 42, 49, 600, DateTimeKind.Local).AddTicks(4810), "2% Stop Loss", "Stop Loss", 0, 2m },
                    { 2, new DateTime(2026, 5, 15, 1, 42, 49, 600, DateTimeKind.Local).AddTicks(4814), "5% Take Profit", "Take Profit", 1, 5m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BacktestStrategies_RiskManagementId",
                table: "BacktestStrategies",
                column: "RiskManagementId");

            migrationBuilder.AddForeignKey(
                name: "FK_BacktestStrategies_RiskManagements_RiskManagementId",
                table: "BacktestStrategies",
                column: "RiskManagementId",
                principalTable: "RiskManagements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IndicatorComparisons_Indicators_IndicatorAId",
                table: "IndicatorComparisons",
                column: "IndicatorAId",
                principalTable: "Indicators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IndicatorComparisons_Indicators_IndicatorBId",
                table: "IndicatorComparisons",
                column: "IndicatorBId",
                principalTable: "Indicators",
                principalColumn: "Id");
        }
    }
}
