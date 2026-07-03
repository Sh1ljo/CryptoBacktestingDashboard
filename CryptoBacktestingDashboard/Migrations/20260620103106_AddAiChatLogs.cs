using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoBacktestingDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddAiChatLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiChatLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateKey = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiChatLogs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "BacktestStrategies",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 20, 12, 31, 5, 737, DateTimeKind.Local).AddTicks(4195));

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

            migrationBuilder.CreateIndex(
                name: "IX_AiChatLogs_UserId_DateKey",
                table: "AiChatLogs",
                columns: new[] { "UserId", "DateKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiChatLogs");

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
        }
    }
}
