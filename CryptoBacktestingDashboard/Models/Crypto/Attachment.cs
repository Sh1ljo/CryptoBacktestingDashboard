namespace CryptoBacktestingDashboard.Models.Crypto
{
    public class Attachment
    {
        public int Id { get; set; }

        public int StrategyId { get; set; }
        public BacktestStrategy Strategy { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}