using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using CryptoBacktestingDashboard.Models;
using CryptoBacktestingDashboard.Models.Crypto;

namespace CryptoBacktestingDashboard.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BacktestResult> BacktestResults { get; set; }
        public DbSet<BacktestSession> BacktestSessions { get; set; }
        public DbSet<BacktestStrategy> BacktestStrategies { get; set; }
        public DbSet<CandleData> CandleData { get; set; }
        public DbSet<CryptoPair> CryptoPairs { get; set; }
        public DbSet<Indicator> Indicators { get; set; }
        public DbSet<IndicatorComparison> IndicatorComparisons { get; set; }
        public DbSet<Attachment> Attachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // An IndicatorComparison references two Indicators. Cascade-delete on both
            // would create multiple cascade paths to the Indicators table (SQL error 1785),
            // so pin both to Restrict (no cascade).
            modelBuilder.Entity<IndicatorComparison>()
                .HasOne(c => c.IndicatorA)
                .WithMany()
                .HasForeignKey(c => c.IndicatorAId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IndicatorComparison>()
                .HasOne(c => c.IndicatorB)
                .WithMany()
                .HasForeignKey(c => c.IndicatorBId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CryptoPair>().HasData(
                new CryptoPair { Id = 1, Symbol = "BTC/USD", BaseAsset = "BTC", QuoteAsset = "USD", CurrentPrice = 60000, CreatedAt = System.DateTime.Now },
                new CryptoPair { Id = 2, Symbol = "ETH/USD", BaseAsset = "ETH", QuoteAsset = "USD", CurrentPrice = 2000, CreatedAt = System.DateTime.Now }
            );

            modelBuilder.Entity<Indicator>().HasData(
                new Indicator { Id = 1, Name = "RSI", Type = IndicatorType.RSI, Period = 14, Threshold = 70, Description = "Relative Strength Index", CreatedAt = System.DateTime.Now },
                new Indicator { Id = 2, Name = "MACD", Type = IndicatorType.MACD, Period = 12, Threshold = 26, Description = "Moving Average Convergence Divergence", CreatedAt = System.DateTime.Now }
            );

            modelBuilder.Entity<BacktestStrategy>().HasData(
                new BacktestStrategy { Id = 1, Name = "RSI Strategy", Description = "A simple RSI strategy", IsActive = true, InitialCapital = 10000, LookbackPeriod = 100, StopLossPercent = 5m, TakeProfitPercent = 10m, PositionSizePercent = 100m, CreatedAt = System.DateTime.Now }
            );

            modelBuilder.Entity<BacktestSession>().HasData(
                new BacktestSession { Id = 1, StrategyId = 1, CryptoPairId = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 3, 31), ExecutedAt = new DateTime(2026, 4, 1), InitialBalance = 10000, FinalBalance = 12350, IsOptimized = true }
            );

            modelBuilder.Entity<BacktestResult>().HasData(
                new BacktestResult { Id = 1, BacktestSessionId = 1, TradeType = TradeType.Long, EntryTime = new DateTime(2026, 1, 5), ExitTime = new DateTime(2026, 1, 12), EntryPrice = 63000, ExitPrice = 65000, Quantity = 0.1m, Commission = 10, IsWinningTrade = true },
                new BacktestResult { Id = 2, BacktestSessionId = 1, TradeType = TradeType.Long, EntryTime = new DateTime(2026, 1, 20), ExitTime = new DateTime(2026, 2, 1), EntryPrice = 64500, ExitPrice = 64000, Quantity = 0.1m, Commission = 10, IsWinningTrade = false }
            );
        }
    }
}
