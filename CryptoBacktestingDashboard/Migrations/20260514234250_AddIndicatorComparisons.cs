using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoBacktestingDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicatorComparisons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndicatorComparisons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BacktestStrategyId = table.Column<int>(type: "int", nullable: false),
                    IndicatorAId = table.Column<int>(type: "int", nullable: false),
                    IndicatorBId = table.Column<int>(type: "int", nullable: false),
                    ComparisonType = table.Column<int>(type: "int", nullable: false),
                    TargetSignal = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicatorComparisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndicatorComparisons_BacktestStrategies_BacktestStrategyId",
                        column: x => x.BacktestStrategyId,
                        principalTable: "BacktestStrategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IndicatorComparisons_Indicators_IndicatorAId",
                        column: x => x.IndicatorAId,
                        principalTable: "Indicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IndicatorComparisons_Indicators_IndicatorBId",
                        column: x => x.IndicatorBId,
                        principalTable: "Indicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.UpdateData(
                table: "BacktestStrategies",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 15, 1, 42, 49, 600, DateTimeKind.Local).AddTicks(4844));

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

            migrationBuilder.UpdateData(
                table: "RiskManagements",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 15, 1, 42, 49, 600, DateTimeKind.Local).AddTicks(4810));

            migrationBuilder.UpdateData(
                table: "RiskManagements",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 15, 1, 42, 49, 600, DateTimeKind.Local).AddTicks(4814));

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorComparisons_BacktestStrategyId",
                table: "IndicatorComparisons",
                column: "BacktestStrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorComparisons_IndicatorAId",
                table: "IndicatorComparisons",
                column: "IndicatorAId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicatorComparisons_IndicatorBId",
                table: "IndicatorComparisons",
                column: "IndicatorBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndicatorComparisons");

            migrationBuilder.UpdateData(
                table: "BacktestStrategies",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 14, 17, 36, 6, 310, DateTimeKind.Local).AddTicks(2852));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 14, 17, 36, 6, 310, DateTimeKind.Local).AddTicks(2692));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 14, 17, 36, 6, 310, DateTimeKind.Local).AddTicks(2696));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 14, 17, 36, 6, 310, DateTimeKind.Local).AddTicks(2803));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 14, 17, 36, 6, 310, DateTimeKind.Local).AddTicks(2807));

            migrationBuilder.UpdateData(
                table: "RiskManagements",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 14, 17, 36, 6, 310, DateTimeKind.Local).AddTicks(2827));

            migrationBuilder.UpdateData(
                table: "RiskManagements",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 14, 17, 36, 6, 310, DateTimeKind.Local).AddTicks(2831));
        }
    }
}
