using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoBacktestingDashboard.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 3, 11, 22, 29, 832, DateTimeKind.Local).AddTicks(130));

            migrationBuilder.UpdateData(
                table: "CryptoPairs",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 3, 11, 22, 29, 832, DateTimeKind.Local).AddTicks(134));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 3, 11, 22, 29, 832, DateTimeKind.Local).AddTicks(258));

            migrationBuilder.UpdateData(
                table: "Indicators",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 3, 11, 22, 29, 832, DateTimeKind.Local).AddTicks(262));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
