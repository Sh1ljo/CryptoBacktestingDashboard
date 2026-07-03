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
        public DbSet<OptimizationRun> OptimizationRuns { get; set; }
        public DbSet<OptimizationResult> OptimizationResults { get; set; }
        public DbSet<AiChatLog> AiChatLogs { get; set; }

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

            // BacktestStrategy and BacktestSession are owned per user — no seed data.
            // New users start with empty strategies and sessions.
            // Pairs and indicators are shared globally.

            // BacktestStrategy -> AppUser
            modelBuilder.Entity<BacktestStrategy>()
                .HasOne(s => s.AppUser)
                .WithMany()
                .HasForeignKey(s => s.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // BacktestSession -> AppUser
            modelBuilder.Entity<BacktestSession>()
                .HasOne(s => s.AppUser)
                .WithMany()
                .HasForeignKey(s => s.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // AiChatLogs: index on (UserId, DateKey) for daily-count queries
            modelBuilder.Entity<AiChatLog>()
                .HasIndex(l => new { l.UserId, l.DateKey });
        }
    }
}
