using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoBacktestingDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OptimizationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BacktestSessionId = table.Column<int>(type: "int", nullable: false),
                    ParameterGridJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BestParamsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BestCompositeScore = table.Column<double>(type: "float", nullable: false),
                    TotalCombinations = table.Column<int>(type: "int", nullable: false),
                    CompletedCombinations = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RanAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptimizationRuns_BacktestSessions_BacktestSessionId",
                        column: x => x.BacktestSessionId,
                        principalTable: "BacktestSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OptimizationResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OptimizationRunId = table.Column<int>(type: "int", nullable: false),
                    ParamsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompositeScore = table.Column<double>(type: "float", nullable: false),
                    Profit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitFactor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WinRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxDrawdownPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTrades = table.Column<int>(type: "int", nullable: false),
                    SharpeApprox = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptimizationResults_OptimizationRuns_OptimizationRunId",
                        column: x => x.OptimizationRunId,
                        principalTable: "OptimizationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BacktestStrategies",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 11, 19, 20, 15, 350, DateTimeKind.Local).AddTicks(8132));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 11, 19, 20, 15, 350, DateTimeKind.Local).AddTicks(7976));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 11, 19, 20, 15, 350, DateTimeKind.Local).AddTicks(7980));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 11, 19, 20, 15, 350, DateTimeKind.Local).AddTicks(8104));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 11, 19, 20, 15, 350, DateTimeKind.Local).AddTicks(8108));

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationResults_OptimizationRunId",
                table: "OptimizationResults",
                column: "OptimizationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRuns_BacktestSessionId",
                table: "OptimizationRuns",
                column: "BacktestSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OptimizationResults");

            migrationBuilder.DropTable(
                name: "OptimizationRuns");

            migrationBuilder.UpdateData(
                table: "BacktestStrategies",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 18, 28, 1, 418, DateTimeKind.Local).AddTicks(7373));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 18, 28, 1, 418, DateTimeKind.Local).AddTicks(7101));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 18, 28, 1, 418, DateTimeKind.Local).AddTicks(7105));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 18, 28, 1, 418, DateTimeKind.Local).AddTicks(7338));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 18, 28, 1, 418, DateTimeKind.Local).AddTicks(7342));
        }
    }
}
